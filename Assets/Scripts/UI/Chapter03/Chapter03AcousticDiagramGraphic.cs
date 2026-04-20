using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZhuozhengYuan
{
    [AddComponentMenu("ZhuozhengYuan/UI/Chapter 03 Acoustic Diagram Graphic")]
    public class Chapter03AcousticDiagramGraphic : MonoBehaviour
    {
        public enum AcousticLineKind
        {
            Hall,
            Roof,
            Reflection,
            Water,
            Underground
        }

        public struct AcousticLineSegment
        {
            public AcousticLineSegment(Vector2 start, Vector2 end, AcousticLineKind kind, float width)
            {
                Start = start;
                End = end;
                Kind = kind;
                Width = width;
            }

            public Vector2 Start { get; }
            public Vector2 End { get; }
            public AcousticLineKind Kind { get; }
            public float Width { get; }
        }

        [SerializeField]
        private float animationDuration = 2.4f;

        [SerializeField]
        private bool autoPlay = true;

        [SerializeField]
        private bool loop = true;

        [SerializeField]
        [Range(0f, 1f)]
        private float animationProgress;

        [SerializeField]
        private float lineThickness = 3.8f;

        [SerializeField]
        private Color activeLineColor = new Color(1f, 0.96f, 0.74f, 1f);

        [SerializeField]
        private Color staticLineColor = new Color(0.6f, 0.7f, 0.6f, 0.55f);

        [SerializeField]
        private Color roofAccentColor = new Color(0.98f, 0.78f, 0.32f, 1f);

        [SerializeField]
        private Color reflectionColor = new Color(1f, 0.95f, 0.74f, 0.95f);

        [SerializeField]
        private Color waterColor = new Color(0.52f, 0.9f, 0.86f, 0.9f);

        [SerializeField]
        private Color undergroundColor = new Color(0.96f, 0.55f, 0.3f, 0.9f);

        private readonly List<Image> _staticLines = new List<Image>();
        private readonly List<Image> _animatedLines = new List<Image>();
        private readonly List<AcousticLineSegment> _segments = new List<AcousticLineSegment>();
        private RectTransform _rectTransform;
        private RectTransform _staticLayer;
        private RectTransform _animatedLayer;
        private Image _pulseImage;
        private float _animationElapsed;
        private Rect _lastBuiltRect;
        private bool _isBuilding;

        public Color color
        {
            get { return activeLineColor; }
            set
            {
                activeLineColor = value;
                UpdateAnimatedLines();
            }
        }

        public bool raycastTarget { get; set; }

        public float AnimationProgress
        {
            get
            {
                return animationProgress;
            }
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(animationProgress, clamped))
                {
                    return;
                }

                animationProgress = clamped;
                UpdateAnimatedLines();
            }
        }

        public void Replay()
        {
            _animationElapsed = 0f;
            animationProgress = 0f;
            enabled = true;
            BuildForCurrentRect();
        }

        public void BuildForCurrentRect()
        {
            if (_isBuilding)
            {
                return;
            }

            _isBuilding = true;
            try
            {
                EnsureReferences();

                Rect rect = ResolveDrawableRect();
                _lastBuiltRect = rect;
                _segments.Clear();
                _segments.AddRange(BuildStaticSegments(rect));

                EnsureLinePool(_staticLines, _staticLayer, "StaticLine", _segments.Count);
                EnsureLinePool(_animatedLines, _animatedLayer, "AnimatedLine", _segments.Count);
                EnsurePulse();

                for (int index = 0; index < _segments.Count; index++)
                {
                    SetLineVisual(_staticLines[index], _segments[index], ResolveStaticColor(_segments[index].Kind), 0.72f);
                }

                DisableExtraLines(_staticLines, _segments.Count);
                UpdateAnimatedLines();
            }
            finally
            {
                _isBuilding = false;
            }
        }

        public static List<AcousticLineSegment> BuildVisibleSegments(Rect rect, float progress)
        {
            List<AcousticLineSegment> allSegments = BuildAllSegments(rect);
            float clampedProgress = Mathf.Clamp01(progress);
            if (clampedProgress <= 0f || allSegments.Count == 0)
            {
                return new List<AcousticLineSegment>();
            }

            float visibleCount = allSegments.Count * clampedProgress;
            int fullCount = Mathf.FloorToInt(visibleCount);
            float partial = visibleCount - fullCount;
            List<AcousticLineSegment> visibleSegments = new List<AcousticLineSegment>(Mathf.Min(allSegments.Count, fullCount + 1));

            for (int index = 0; index < fullCount && index < allSegments.Count; index++)
            {
                visibleSegments.Add(allSegments[index]);
            }

            if (fullCount < allSegments.Count && partial > 0f)
            {
                AcousticLineSegment segment = allSegments[fullCount];
                Vector2 end = Vector2.Lerp(segment.Start, segment.End, partial);
                visibleSegments.Add(new AcousticLineSegment(segment.Start, end, segment.Kind, segment.Width));
            }

            return visibleSegments;
        }

        public static List<AcousticLineSegment> BuildStaticSegments(Rect rect)
        {
            return BuildAllSegments(rect);
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            if (autoPlay)
            {
                Replay();
            }
            else
            {
                BuildForCurrentRect();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            animationDuration = Mathf.Max(0.05f, animationDuration);
            lineThickness = Mathf.Max(0.1f, lineThickness);
            animationProgress = Mathf.Clamp01(animationProgress);
            if (isActiveAndEnabled)
            {
                BuildForCurrentRect();
            }
        }
#endif

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Rect rect = ResolveDrawableRect();
            if (!Mathf.Approximately(rect.width, _lastBuiltRect.width) ||
                !Mathf.Approximately(rect.height, _lastBuiltRect.height))
            {
                BuildForCurrentRect();
            }
        }

        private void Update()
        {
            Rect rect = ResolveDrawableRect();
            if (!Mathf.Approximately(rect.width, _lastBuiltRect.width) ||
                !Mathf.Approximately(rect.height, _lastBuiltRect.height) ||
                _segments.Count == 0)
            {
                BuildForCurrentRect();
            }

            if (!autoPlay || animationDuration <= 0f)
            {
                return;
            }

            _animationElapsed += Time.unscaledDeltaTime;
            float rawProgress = _animationElapsed / animationDuration;
            if (loop)
            {
                rawProgress = rawProgress - Mathf.Floor(rawProgress);
            }

            AnimationProgress = EaseOutCubic(rawProgress);
        }

        private void EnsureReferences()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            if (_staticLayer == null)
            {
                _staticLayer = EnsureLayer("StaticAcousticLines");
            }

            if (_animatedLayer == null)
            {
                _animatedLayer = EnsureLayer("AnimatedAcousticLines");
            }
        }

        private RectTransform EnsureLayer(string layerName)
        {
            Transform existing = transform.Find(layerName);
            GameObject layerObject = existing != null
                ? existing.gameObject
                : new GameObject(layerName, typeof(RectTransform));
            RectTransform layer = layerObject.GetComponent<RectTransform>();
            layer.SetParent(transform, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            layer.pivot = new Vector2(0.5f, 0.5f);
            return layer;
        }

        private void EnsurePulse()
        {
            if (_pulseImage != null)
            {
                return;
            }

            GameObject pulseObject = new GameObject("SoundPulse", typeof(RectTransform), typeof(Image));
            pulseObject.transform.SetParent(_animatedLayer, false);
            RectTransform rect = pulseObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(14f, 14f);

            _pulseImage = pulseObject.GetComponent<Image>();
            _pulseImage.color = new Color(1f, 0.9f, 0.28f, 1f);
            _pulseImage.raycastTarget = false;
        }

        private Rect ResolveDrawableRect()
        {
            EnsureReferences();

            Rect rect = _rectTransform.rect;
            if (rect.width > 1f && rect.height > 1f)
            {
                return rect;
            }

            Vector2 size = _rectTransform.sizeDelta;
            if (size.x <= 1f)
            {
                size.x = 420f;
            }

            if (size.y <= 1f)
            {
                size.y = 280f;
            }

            return new Rect(size.x * -0.5f, size.y * -0.5f, size.x, size.y);
        }

        private void EnsureLinePool(List<Image> pool, RectTransform parent, string prefix, int count)
        {
            for (int index = pool.Count; index < count; index++)
            {
                GameObject lineObject = new GameObject(prefix + "_" + index.ToString("00"), typeof(RectTransform), typeof(Image));
                lineObject.transform.SetParent(parent, false);

                RectTransform rect = lineObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                Image image = lineObject.GetComponent<Image>();
                image.raycastTarget = false;
                pool.Add(image);
            }
        }

        private static void DisableExtraLines(List<Image> pool, int activeCount)
        {
            for (int index = activeCount; index < pool.Count; index++)
            {
                pool[index].gameObject.SetActive(false);
            }
        }

        private void UpdateAnimatedLines()
        {
            if (_segments.Count == 0 || _animatedLines.Count == 0)
            {
                return;
            }

            List<AcousticLineSegment> visibleSegments = BuildVisibleSegments(_lastBuiltRect, animationProgress);
            int index = 0;
            for (; index < visibleSegments.Count && index < _animatedLines.Count; index++)
            {
                SetLineVisual(_animatedLines[index], visibleSegments[index], ResolveActiveColor(visibleSegments[index].Kind), 1.28f);
            }

            DisableExtraLines(_animatedLines, index);
            UpdatePulse(visibleSegments);
        }

        private void UpdatePulse(List<AcousticLineSegment> visibleSegments)
        {
            if (_pulseImage == null)
            {
                return;
            }

            if (visibleSegments.Count == 0)
            {
                _pulseImage.gameObject.SetActive(false);
                return;
            }

            AcousticLineSegment segment = visibleSegments[visibleSegments.Count - 1];
            Vector2 position = segment.End;
            RectTransform pulseRect = _pulseImage.GetComponent<RectTransform>();
            pulseRect.anchoredPosition = position;
            pulseRect.sizeDelta = Vector2.one * Mathf.Lerp(10f, 17f, Mathf.PingPong(Time.unscaledTime * 3f, 1f));
            _pulseImage.color = new Color(1f, 0.88f, 0.24f, 0.9f);
            _pulseImage.gameObject.SetActive(true);
        }

        private void SetLineVisual(Image image, AcousticLineSegment segment, Color colorValue, float widthMultiplier)
        {
            Vector2 direction = segment.End - segment.Start;
            float length = direction.magnitude;
            if (length <= 0.01f)
            {
                image.gameObject.SetActive(false);
                return;
            }

            RectTransform rect = image.GetComponent<RectTransform>();
            rect.anchoredPosition = (segment.Start + segment.End) * 0.5f;
            rect.sizeDelta = new Vector2(length, lineThickness * segment.Width * widthMultiplier);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            image.color = colorValue;
            image.raycastTarget = false;
            image.gameObject.SetActive(true);
        }

        private static List<AcousticLineSegment> BuildAllSegments(Rect rect)
        {
            List<AcousticLineSegment> segments = new List<AcousticLineSegment>(48);

            AddPolyline(segments, rect, AcousticLineKind.Roof, 1.45f, BuildArcPoints(0.18f, 0.52f, 0.5f, 0.52f, 205f, 335f, 14));
            AddSegment(segments, rect, AcousticLineKind.Hall, 1.15f, new Vector2(0.13f, 0.24f), new Vector2(0.87f, 0.24f));
            AddSegment(segments, rect, AcousticLineKind.Hall, 1f, new Vector2(0.18f, 0.74f), new Vector2(0.82f, 0.72f));
            AddSegment(segments, rect, AcousticLineKind.Hall, 1f, new Vector2(0.18f, 0.62f), new Vector2(0.82f, 0.61f));
            AddSegment(segments, rect, AcousticLineKind.Hall, 1f, new Vector2(0.18f, 0.5f), new Vector2(0.82f, 0.5f));
            AddSegment(segments, rect, AcousticLineKind.Hall, 1f, new Vector2(0.18f, 0.38f), new Vector2(0.82f, 0.39f));

            for (int index = 0; index < 6; index++)
            {
                float x = Mathf.Lerp(0.28f, 0.72f, index / 5f);
                AddSegment(segments, rect, AcousticLineKind.Reflection, 0.9f, new Vector2(x, 0.76f), new Vector2(x + 0.045f, 0.29f));
            }

            AddEarRoom(segments, rect, new Vector2(0.1f, 0.6f));
            AddEarRoom(segments, rect, new Vector2(0.79f, 0.6f));
            AddEarRoom(segments, rect, new Vector2(0.1f, 0.28f));
            AddEarRoom(segments, rect, new Vector2(0.79f, 0.28f));

            AddPolyline(segments, rect, AcousticLineKind.Water, 0.85f, BuildArcPoints(0.26f, 0.1f, 0.48f, 0.08f, 12f, 168f, 8));
            AddPolyline(segments, rect, AcousticLineKind.Water, 0.85f, BuildArcPoints(0.26f, 0.075f, 0.48f, 0.08f, 12f, 168f, 8));
            AddPolyline(segments, rect, AcousticLineKind.Water, 0.85f, BuildArcPoints(0.26f, 0.05f, 0.48f, 0.08f, 12f, 168f, 8));

            Rect underground = new Rect(0.32f, -0.03f, 0.42f, 0.09f);
            AddRect(segments, rect, AcousticLineKind.Underground, 0.75f, underground);
            for (int index = 1; index < 6; index++)
            {
                float x = underground.xMin + underground.width * (index / 6f);
                AddSegment(segments, rect, AcousticLineKind.Underground, 0.55f, new Vector2(x, underground.yMin), new Vector2(x, underground.yMax));
            }

            return segments;
        }

        private static void AddEarRoom(List<AcousticLineSegment> segments, Rect rect, Vector2 origin)
        {
            AddRect(segments, rect, AcousticLineKind.Roof, 0.8f, new Rect(origin.x, origin.y, 0.1f, 0.12f));
        }

        private static void AddRect(List<AcousticLineSegment> segments, Rect rect, AcousticLineKind kind, float widthScale, Rect normalizedRect)
        {
            Vector2 a = new Vector2(normalizedRect.xMin, normalizedRect.yMin);
            Vector2 b = new Vector2(normalizedRect.xMax, normalizedRect.yMin);
            Vector2 c = new Vector2(normalizedRect.xMax, normalizedRect.yMax);
            Vector2 d = new Vector2(normalizedRect.xMin, normalizedRect.yMax);
            AddSegment(segments, rect, kind, widthScale, a, b);
            AddSegment(segments, rect, kind, widthScale, b, c);
            AddSegment(segments, rect, kind, widthScale, c, d);
            AddSegment(segments, rect, kind, widthScale, d, a);
        }

        private static Vector2[] BuildArcPoints(float centerX, float centerY, float width, float height, float startDegrees, float endDegrees, int steps)
        {
            Vector2[] points = new Vector2[steps + 1];
            for (int index = 0; index <= steps; index++)
            {
                float t = index / (float)steps;
                float degrees = Mathf.Lerp(startDegrees, endDegrees, t);
                float radians = degrees * Mathf.Deg2Rad;
                points[index] = new Vector2(centerX + Mathf.Cos(radians) * width, centerY + Mathf.Sin(radians) * height);
            }

            return points;
        }

        private static void AddPolyline(List<AcousticLineSegment> segments, Rect rect, AcousticLineKind kind, float widthScale, Vector2[] normalizedPoints)
        {
            for (int index = 0; index < normalizedPoints.Length - 1; index++)
            {
                AddSegment(segments, rect, kind, widthScale, normalizedPoints[index], normalizedPoints[index + 1]);
            }
        }

        private static void AddSegment(List<AcousticLineSegment> segments, Rect rect, AcousticLineKind kind, float widthScale, Vector2 normalizedStart, Vector2 normalizedEnd)
        {
            Vector2 start = NormalizedToRect(rect, normalizedStart);
            Vector2 end = NormalizedToRect(rect, normalizedEnd);
            segments.Add(new AcousticLineSegment(start, end, kind, Mathf.Max(0.1f, widthScale)));
        }

        private static Vector2 NormalizedToRect(Rect rect, Vector2 normalized)
        {
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalized.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalized.y));
        }

        private static float EaseOutCubic(float value)
        {
            float clamped = Mathf.Clamp01(value);
            float inverse = 1f - clamped;
            return 1f - inverse * inverse * inverse;
        }

        private Color ResolveStaticColor(AcousticLineKind kind)
        {
            Color resolved = staticLineColor;
            switch (kind)
            {
                case AcousticLineKind.Roof:
                    resolved = Color.Lerp(staticLineColor, roofAccentColor, 0.36f);
                    resolved.a = 0.62f;
                    break;
                case AcousticLineKind.Reflection:
                    resolved = Color.Lerp(staticLineColor, reflectionColor, 0.34f);
                    resolved.a = 0.48f;
                    break;
                case AcousticLineKind.Water:
                    resolved = Color.Lerp(staticLineColor, waterColor, 0.48f);
                    resolved.a = 0.58f;
                    break;
                case AcousticLineKind.Underground:
                    resolved = Color.Lerp(staticLineColor, undergroundColor, 0.48f);
                    resolved.a = 0.58f;
                    break;
                default:
                    break;
            }

            return resolved;
        }

        private Color ResolveActiveColor(AcousticLineKind kind)
        {
            Color resolved = activeLineColor;
            switch (kind)
            {
                case AcousticLineKind.Roof:
                    resolved = Color.Lerp(activeLineColor, roofAccentColor, 0.55f);
                    break;
                case AcousticLineKind.Reflection:
                    resolved = Color.Lerp(activeLineColor, reflectionColor, 0.45f);
                    break;
                case AcousticLineKind.Water:
                    resolved = Color.Lerp(activeLineColor, waterColor, 0.55f);
                    break;
                case AcousticLineKind.Underground:
                    resolved = Color.Lerp(activeLineColor, undergroundColor, 0.5f);
                    break;
                default:
                    break;
            }

            resolved.a = 1f;
            return resolved;
        }
    }
}
