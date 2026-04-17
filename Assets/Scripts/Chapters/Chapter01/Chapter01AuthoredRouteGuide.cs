using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZhuozhengYuan
{
    [DisallowMultipleComponent]
    public class Chapter01AuthoredRouteGuide : MonoBehaviour
    {
        private class GuideSegment
        {
            public Transform root;
            public Transform glowStrip;
            public Transform mainStrip;
            public Transform crestStrip;
            public float length;
        }

        public GardenGameManager manager;
        public Chapter01Director director;
        public IntroSequenceController introController;
        public Transform playerStartPose;
        public Transform targetGate;
        public string authoredRouteRootName = "Chapter01GuidePath";
        public Transform[] routePoints;
        public bool showGuideOnStart = true;
        public bool useResolvedRouteFallback = true;
        public float guideWidth = 2.4f;
        public float guideThickness = 0.04f;
        public float groundOffset = 0.08f;
        public float sampleSpacing = 1.2f;
        public bool smoothControlPoints = true;
        public int minimumCurveSubdivisions = 6;
        public LayerMask groundLayers = Physics.DefaultRaycastLayers;
        public LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
        public bool trimSegmentsAgainstObstacles = true;
        public float obstacleProbeHeight = 0.38f;
        public float obstacleProbeRadius = 0.1f;
        public float obstacleClearance = 0.18f;
        public float minimumVisibleSegmentLength = 0.3f;
        public float revealSpeed = 18f;
        public float reachedRadius = 4f;
        public Color mainGuideColor = new Color(0.88f, 0.82f, 0.52f, 0.92f);
        public Color mistGuideColor = new Color(0.53f, 0.78f, 0.72f, 0.2f);
        public float idleAlpha = 0.72f;
        public bool useHybridDecorations = true;
        public bool useDestinationMarker = true;
        public Chapter01GuideDecorationProfile decorationProfile = default;
        public int maxDecorationMarkers = 6;

        private readonly List<GuideSegment> _segments = new List<GuideSegment>();
        private Transform _runtimeRoot;
        private Transform _decorationsRoot;
        private Transform _destinationMarkerRoot;
        private Material _mainGuideMaterial;
        private Material _mistGuideMaterial;
        private Material _crestGuideMaterial;
        private Material _decorationMaterial;
        private Material _destinationMarkerMaterial;
        private Coroutine _revealRoutine;
        private Coroutine _fadeRoutine;
        private bool _guideHidden;

        private void Update()
        {
            if (_guideHidden || !showGuideOnStart)
            {
                return;
            }

            if (director != null && director.IsGateSolved(GateId.Left))
            {
                HideGuide();
                return;
            }

            if (targetGate == null)
            {
                return;
            }

            Transform playerTransform = manager != null && manager.playerController != null
                ? manager.playerController.transform
                : null;

            if (playerTransform == null)
            {
                return;
            }

            Vector3 playerPosition = playerTransform.position;
            playerPosition.y = 0f;
            Vector3 gatePosition = targetGate.position;
            gatePosition.y = 0f;

            if (Vector3.Distance(playerPosition, gatePosition) <= Mathf.Max(1f, reachedRadius))
            {
                HideGuide();
            }
        }

        public void Initialize(GardenGameManager gameManager, Chapter01Director chapterDirector, IntroSequenceController introSequenceController)
        {
            manager = gameManager;
            director = chapterDirector;
            introController = introSequenceController;
            EnsureDecorationProfileInitialized();

            if (playerStartPose == null && introController != null)
            {
                playerStartPose = introController.playerPostIntroPose;
            }

            if (targetGate == null && director != null && director.leftGate != null)
            {
                targetGate = director.leftGate.transform;
            }

            RebuildGuide();
        }

        public void RebuildGuide()
        {
            DestroyRuntimeGuide();
            EnsureDecorationProfileInitialized();

            if (!showGuideOnStart || (director != null && director.IsGateSolved(GateId.Left)))
            {
                _guideHidden = true;
                return;
            }

            List<Vector3> worldPoints = ResolveRouteWorldPoints();
            List<Vector3> displayPoints = BuildDisplayPath(worldPoints);
            if (displayPoints == null || displayPoints.Count < 2)
            {
                return;
            }

            EnsureMaterials();
            _runtimeRoot = new GameObject("Chapter01AuthoredGuideRoot").transform;
            _runtimeRoot.SetParent(transform, false);
            _decorationsRoot = new GameObject("DecorationsRoot").transform;
            _decorationsRoot.SetParent(_runtimeRoot, false);
            _destinationMarkerRoot = new GameObject("DestinationMarkerRoot").transform;
            _destinationMarkerRoot.SetParent(_runtimeRoot, false);

            for (int index = 0; index < displayPoints.Count - 1; index++)
            {
                Vector3 from = displayPoints[index];
                Vector3 to = displayPoints[index + 1];
                if (!TryResolveVisibleSegment(from, to, out from, out to))
                {
                    continue;
                }

                Vector3 segment = to - from;
                float segmentLength = segment.magnitude;
                if (segmentLength < 0.1f)
                {
                    continue;
                }

                Quaternion rotation = Quaternion.LookRotation(segment.normalized, Vector3.up);
                Transform segmentRoot = new GameObject("GuideSegment_" + index.ToString("00")).transform;
                segmentRoot.SetParent(_runtimeRoot, false);
                segmentRoot.position = from;
                segmentRoot.rotation = rotation;

                GuideSegment guideSegment = new GuideSegment
                {
                    root = segmentRoot,
                    length = segmentLength
                };

                guideSegment.glowStrip = CreateRibbonVisual(
                    "Glow",
                    segmentRoot,
                    segmentLength,
                    Mathf.Max(0.8f, guideWidth * 1.2f),
                    -0.012f,
                    _mistGuideMaterial);
                guideSegment.mainStrip = CreateRibbonVisual(
                    "Main",
                    segmentRoot,
                    segmentLength,
                    Mathf.Max(0.5f, guideWidth * 0.55f),
                    0.002f,
                    _mainGuideMaterial);
                guideSegment.crestStrip = CreateRibbonVisual(
                    "Crest",
                    segmentRoot,
                    segmentLength,
                    Mathf.Max(0.14f, guideWidth * 0.14f),
                    0.006f,
                    _crestGuideMaterial);

                SetSegmentReveal(guideSegment, 0f);
                _segments.Add(guideSegment);
            }

            _guideHidden = _segments.Count == 0;
            if (_guideHidden)
            {
                return;
            }

            BuildDecorations(displayPoints);
            BuildDestinationMarker();
            _revealRoutine = StartCoroutine(RevealGuideRoutine());
        }

        public void HideGuide()
        {
            if (_guideHidden)
            {
                return;
            }

            _guideHidden = true;
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeOutGuideRoutine());
        }

        private List<Vector3> ResolveRouteWorldPoints()
        {
            List<Vector3> points = new List<Vector3>();

            if (playerStartPose != null)
            {
                points.Add(GetGuidePoint(playerStartPose.position));
            }

            Transform[] authoredPoints = ResolveAuthoredRoutePoints();
            if (authoredPoints != null && authoredPoints.Length > 0)
            {
                for (int index = 0; index < authoredPoints.Length; index++)
                {
                    if (authoredPoints[index] != null)
                    {
                        AddUniquePoint(points, authoredPoints[index].position);
                    }
                }
            }
            else if (useResolvedRouteFallback
                && manager != null
                && manager.TryGetResolvedChapter01RoutePathCopy(out List<Vector3> resolvedRoutePath, out _, out _))
            {
                for (int index = 0; index < resolvedRoutePath.Count; index++)
                {
                    AddUniquePoint(points, resolvedRoutePath[index]);
                }
            }

            if (targetGate != null)
            {
                AddUniquePoint(points, targetGate.position);
            }

            return points.Count >= 2 ? points : null;
        }

        private List<Vector3> BuildDisplayPath(List<Vector3> controlPoints)
        {
            if (controlPoints == null || controlPoints.Count < 2)
            {
                return controlPoints;
            }

            if (smoothControlPoints && controlPoints.Count >= 3)
            {
                return BuildSmoothedDisplayPath(controlPoints);
            }

            List<Vector3> displayPoints = new List<Vector3>();
            AddUniqueResolvedPoint(displayPoints, controlPoints[0], true);

            float spacing = Mathf.Max(0.35f, sampleSpacing);
            for (int index = 0; index < controlPoints.Count - 1; index++)
            {
                Vector3 from = controlPoints[index];
                Vector3 to = controlPoints[index + 1];
                float distance = Vector3.Distance(from, to);
                int steps = Mathf.Max(1, Mathf.CeilToInt(distance / spacing));

                for (int step = 1; step <= steps; step++)
                {
                    float t = step / (float)steps;
                    Vector3 sampledPoint = Vector3.Lerp(from, to, t);
                    AddUniqueResolvedPoint(displayPoints, sampledPoint, step == steps);
                }
            }

            return displayPoints.Count >= 2 ? displayPoints : null;
        }

        private List<Vector3> BuildSmoothedDisplayPath(List<Vector3> controlPoints)
        {
            List<Vector3> displayPoints = new List<Vector3>();
            AddUniqueResolvedPoint(displayPoints, controlPoints[0], true);

            float spacing = Mathf.Max(0.35f, sampleSpacing);
            int minimumSteps = Mathf.Max(3, minimumCurveSubdivisions);

            for (int index = 0; index < controlPoints.Count - 1; index++)
            {
                Vector3 p0 = index > 0 ? controlPoints[index - 1] : controlPoints[index];
                Vector3 p1 = controlPoints[index];
                Vector3 p2 = controlPoints[index + 1];
                Vector3 p3 = index + 2 < controlPoints.Count ? controlPoints[index + 2] : controlPoints[index + 1];

                float distance = Vector3.Distance(p1, p2);
                int steps = Mathf.Max(minimumSteps, Mathf.CeilToInt(distance / spacing));

                for (int step = 1; step <= steps; step++)
                {
                    float t = step / (float)steps;
                    Vector3 smoothedPoint = CatmullRom(p0, p1, p2, p3, t);
                    AddUniqueResolvedPoint(displayPoints, smoothedPoint, index == controlPoints.Count - 2 && step == steps);
                }
            }

            return displayPoints.Count >= 2 ? displayPoints : null;
        }

        private Transform[] ResolveAuthoredRoutePoints()
        {
            if (routePoints != null && routePoints.Length > 0)
            {
                return routePoints;
            }

            Transform authoredRoot = FindAuthoredRouteRoot();
            if (authoredRoot == null)
            {
                return null;
            }

            List<Transform> collectedPoints = new List<Transform>();
            for (int index = 0; index < authoredRoot.childCount; index++)
            {
                collectedPoints.Add(authoredRoot.GetChild(index));
            }

            collectedPoints.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            routePoints = collectedPoints.ToArray();
            return routePoints;
        }

        private Transform FindAuthoredRouteRoot()
        {
            Transform markersRoot = director != null && director.leftGate != null && director.leftGate.transform.parent != null
                ? director.leftGate.transform.parent
                : null;

            if (markersRoot != null)
            {
                Transform localRoot = markersRoot.Find(authoredRouteRootName);
                if (localRoot != null)
                {
                    return localRoot;
                }
            }

            GameObject rootObject = GameObject.Find(authoredRouteRootName);
            return rootObject != null ? rootObject.transform : null;
        }

        private void EnsureMaterials()
        {
            Shader shader = Shader.Find("ZhuozhengYuan/GuideRibbon");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (_mainGuideMaterial == null)
            {
                _mainGuideMaterial = new Material(shader);
                _mainGuideMaterial.name = "Chapter01GuideMainMat";
            }

            if (_mistGuideMaterial == null)
            {
                _mistGuideMaterial = new Material(shader);
                _mistGuideMaterial.name = "Chapter01GuideMistMat";
            }

            if (_crestGuideMaterial == null)
            {
                _crestGuideMaterial = new Material(shader);
                _crestGuideMaterial.name = "Chapter01GuideCrestMat";
            }

            if (_decorationMaterial == null)
            {
                _decorationMaterial = new Material(shader);
                _decorationMaterial.name = "Chapter01GuideDecorationMat";
            }

            if (_destinationMarkerMaterial == null)
            {
                _destinationMarkerMaterial = new Material(shader);
                _destinationMarkerMaterial.name = "Chapter01GuideDestinationMarkerMat";
            }

            Color mainColor = decorationProfile.ribbonBaseColor;
            mainColor.a = Mathf.Clamp01(idleAlpha);
            ApplyMaterialColors(_mainGuideMaterial, mainColor, decorationProfile.ribbonHighlightColor, 1f, 2.8f);
            ApplyMaterialColors(_mistGuideMaterial, decorationProfile.decorationPrimaryColor, decorationProfile.decorationSecondaryColor, 0.72f, 1.8f);
            ApplyMaterialColors(_crestGuideMaterial, decorationProfile.ribbonHighlightColor, Color.white, 1f, 3.4f);
            ApplyMaterialColors(_decorationMaterial, decorationProfile.decorationPrimaryColor, decorationProfile.decorationSecondaryColor, 0.88f, 1.45f);
            ApplyMaterialColors(_destinationMarkerMaterial, decorationProfile.destinationMarkerColor, decorationProfile.ribbonHighlightColor, 0.96f, 1.92f);
            ApplyMaterialMotion(_mainGuideMaterial, decorationProfile.animationSpeed * 1.05f, decorationProfile.animationSpeed * 0.82f, 1.55f);
            ApplyMaterialMotion(_mistGuideMaterial, decorationProfile.animationSpeed * 0.75f, decorationProfile.animationSpeed * 0.62f, 1.1f);
            ApplyMaterialMotion(_crestGuideMaterial, decorationProfile.animationSpeed * 1.25f, decorationProfile.animationSpeed, 1.85f);
            ApplyMaterialMotion(_decorationMaterial, decorationProfile.animationSpeed * 0.58f, decorationProfile.animationSpeed * 0.48f, 0.95f);
            ApplyMaterialMotion(_destinationMarkerMaterial, decorationProfile.animationSpeed * 0.9f, decorationProfile.animationSpeed * 0.72f, 1.24f);
        }

        private void BuildDecorations(List<Vector3> displayPoints)
        {
            if (!useHybridDecorations || _decorationsRoot == null || displayPoints == null || displayPoints.Count < 2)
            {
                return;
            }

            int maxMarkers = Mathf.Max(1, maxDecorationMarkers);
            float desiredSpacing = Mathf.Max(1.5f, decorationProfile.decorationSpacing);
            float distanceSincePlacement = desiredSpacing;
            int created = 0;

            for (int index = 1; index < displayPoints.Count - 1 && created < maxMarkers; index++)
            {
                Vector3 previousPoint = displayPoints[index - 1];
                Vector3 currentPoint = displayPoints[index];
                Vector3 nextPoint = displayPoints[index + 1];
                distanceSincePlacement += Vector3.Distance(previousPoint, currentPoint);

                Vector3 incoming = (currentPoint - previousPoint).normalized;
                Vector3 outgoing = (nextPoint - currentPoint).normalized;
                float turnAngle = Vector3.Angle(incoming, outgoing);
                bool isKeyTurn = turnAngle >= 16f;
                bool isNearGoal = index >= displayPoints.Count - 3;
                bool shouldPlace = distanceSincePlacement >= desiredSpacing && (created == 0 || isKeyTurn || isNearGoal);
                if (!shouldPlace)
                {
                    continue;
                }

                CreateDecorationMarker(currentPoint, incoming, outgoing, created);
                created++;
                distanceSincePlacement = 0f;
            }

            if (created == 0 && displayPoints.Count > 2)
            {
                int fallbackIndex = Mathf.Clamp(displayPoints.Count / 2, 1, displayPoints.Count - 2);
                Vector3 previousPoint = displayPoints[fallbackIndex - 1];
                Vector3 currentPoint = displayPoints[fallbackIndex];
                Vector3 nextPoint = displayPoints[fallbackIndex + 1];
                CreateDecorationMarker(currentPoint, (currentPoint - previousPoint).normalized, (nextPoint - currentPoint).normalized, created);
            }
        }

        private Transform CreateRibbonVisual(
            string visualName,
            Transform parent,
            float length,
            float width,
            float verticalOffset,
            Material material)
        {
            GameObject stripObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            stripObject.name = visualName;
            stripObject.transform.SetParent(parent, false);
            stripObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            stripObject.transform.localPosition = new Vector3(0f, verticalOffset, length * 0.5f);
            stripObject.transform.localScale = new Vector3(Mathf.Max(0.08f, width), Mathf.Max(0.1f, length), 1f);

            Collider collider = stripObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            MeshRenderer renderer = stripObject.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }

            return stripObject.transform;
        }

        private IEnumerator RevealGuideRoutine()
        {
            for (int index = 0; index < _segments.Count; index++)
            {
                if (_guideHidden)
                {
                    yield break;
                }

                GuideSegment segment = _segments[index];
                float duration = segment.length / Mathf.Max(4f, revealSpeed);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    if (_guideHidden)
                    {
                        yield break;
                    }

                    elapsed += Time.deltaTime;
                    float t = duration <= 0.001f ? 1f : Mathf.Clamp01(elapsed / duration);
                    SetSegmentReveal(segment, t);
                    yield return null;
                }

                SetSegmentReveal(segment, 1f);
            }
        }

        private IEnumerator FadeOutGuideRoutine()
        {
            float elapsed = 0f;
            const float duration = 0.55f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alphaMultiplier = 1f - Mathf.Clamp01(elapsed / duration);
                ApplyGuideFade(alphaMultiplier);

                yield return null;
            }

            DestroyRuntimeGuide();
        }

        private void DestroyRuntimeGuide()
        {
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            if (_runtimeRoot != null)
            {
                Destroy(_runtimeRoot.gameObject);
                _runtimeRoot = null;
                _decorationsRoot = null;
                _destinationMarkerRoot = null;
            }

            _segments.Clear();
        }

        private void CreateDecorationMarker(Vector3 point, Vector3 incoming, Vector3 outgoing, int markerIndex)
        {
            GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            markerObject.name = "DecorationMarker_" + markerIndex.ToString("00");
            markerObject.transform.SetParent(_decorationsRoot, false);
            markerObject.transform.position = point + Vector3.up * 0.06f;
            markerObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            float baseSize = Mathf.Max(0.4f, decorationProfile.decorationSize);
            float turnAmount = Mathf.Clamp01(Vector3.Angle(incoming, outgoing) / 65f);
            float width = baseSize * Mathf.Lerp(0.72f, 1.08f, turnAmount);
            float length = baseSize * Mathf.Lerp(0.92f, 1.35f, turnAmount);
            markerObject.transform.localScale = new Vector3(width, length, 1f);

            Collider collider = markerObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            MeshRenderer renderer = markerObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = _decorationMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

                Color baseColor = Color.Lerp(decorationProfile.decorationPrimaryColor, decorationProfile.decorationSecondaryColor, markerIndex % 2 == 0 ? 0.18f : 0.62f);
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", baseColor);
                propertyBlock.SetColor("_BaseColor", baseColor);
                propertyBlock.SetColor("_AccentColor", decorationProfile.ribbonHighlightColor);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ApplyGuideFade(float alphaMultiplier)
        {
            Color glowColor = decorationProfile.decorationPrimaryColor;
            Color mainColor = decorationProfile.ribbonBaseColor;
            Color crestColor = decorationProfile.ribbonHighlightColor;

            for (int index = 0; index < _segments.Count; index++)
            {
                ApplyRendererAlpha(_segments[index].glowStrip, glowColor.a * alphaMultiplier, glowColor);
                ApplyRendererAlpha(_segments[index].mainStrip, idleAlpha * alphaMultiplier, mainColor);
                ApplyRendererAlpha(_segments[index].crestStrip, Mathf.Min(1f, idleAlpha * 1.12f) * alphaMultiplier, crestColor);
            }

            ApplyChildRendererAlpha(_decorationsRoot, alphaMultiplier);
            ApplyChildRendererAlpha(_destinationMarkerRoot, alphaMultiplier);
        }

        private void BuildDestinationMarker()
        {
            if (!useDestinationMarker || _destinationMarkerRoot == null || targetGate == null)
            {
                return;
            }

            Vector3 anchorPosition = GetGuidePoint(targetGate.position) + Vector3.up * decorationProfile.destinationMarkerHeight;
            CreateDestinationMarkerQuad(
                "DestinationMarker_Ring",
                anchorPosition,
                new Vector3(decorationProfile.destinationMarkerScale, decorationProfile.destinationMarkerScale, 1f),
                decorationProfile.destinationMarkerColor,
                decorationProfile.ribbonHighlightColor);

            CreateDestinationMarkerQuad(
                "DestinationMarker_Glow",
                anchorPosition + Vector3.up * 0.08f,
                new Vector3(decorationProfile.destinationMarkerScale * 1.28f, decorationProfile.destinationMarkerScale * 1.28f, 1f),
                new Color(
                    decorationProfile.destinationMarkerColor.r,
                    decorationProfile.destinationMarkerColor.g,
                    decorationProfile.destinationMarkerColor.b,
                    decorationProfile.destinationMarkerColor.a * 0.52f),
                decorationProfile.ribbonHighlightColor);
        }

        private Transform CreateDestinationMarkerQuad(string markerName, Vector3 worldPosition, Vector3 scale, Color baseColor, Color accentColor)
        {
            GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            markerObject.name = markerName;
            markerObject.transform.SetParent(_destinationMarkerRoot, false);
            markerObject.transform.position = worldPosition;
            markerObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            markerObject.transform.localScale = scale;

            Collider collider = markerObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            MeshRenderer renderer = markerObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = _destinationMarkerMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", baseColor);
                propertyBlock.SetColor("_BaseColor", baseColor);
                propertyBlock.SetColor("_AccentColor", accentColor);
                renderer.SetPropertyBlock(propertyBlock);
            }

            return markerObject.transform;
        }

        private void EnsureDecorationProfileInitialized()
        {
            if (decorationProfile.decorationSpacing <= 0.01f)
            {
                decorationProfile = Chapter01GuideDecorationProfile.CreateDefault();
            }
        }

        private void SetSegmentReveal(GuideSegment segment, float normalizedReveal)
        {
            float reveal = Mathf.Clamp01(normalizedReveal);
            SetStripReveal(segment.glowStrip, segment.length, reveal, guideWidth * 1.2f);
            SetStripReveal(segment.mainStrip, segment.length, reveal, guideWidth * 0.55f);
            SetStripReveal(segment.crestStrip, segment.length, reveal, guideWidth * 0.14f);

            Color glowColor = decorationProfile.decorationPrimaryColor;
            Color mainColor = decorationProfile.ribbonBaseColor;
            Color crestColor = decorationProfile.ribbonHighlightColor;
            ApplyRendererAlpha(segment.glowStrip, glowColor.a, glowColor);
            ApplyRendererAlpha(segment.mainStrip, idleAlpha, mainColor);
            ApplyRendererAlpha(segment.crestStrip, Mathf.Min(1f, idleAlpha * 1.12f), crestColor);
        }

        private static void SetStripReveal(Transform stripTransform, float fullLength, float reveal, float width)
        {
            if (stripTransform == null)
            {
                return;
            }

            float visibleLength = Mathf.Max(0.01f, fullLength * reveal);
            stripTransform.localScale = new Vector3(Mathf.Max(0.08f, width), visibleLength, 1f);

            Vector3 localPosition = stripTransform.localPosition;
            localPosition.z = visibleLength * 0.5f;
            stripTransform.localPosition = localPosition;
        }

        private static void ApplyRendererAlpha(Transform stripTransform, float alpha, Color baseColor)
        {
            if (stripTransform == null)
            {
                return;
            }

            MeshRenderer renderer = stripTransform.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return;
            }

            Color color = baseColor;
            color.a = alpha;
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", color);
            propertyBlock.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void ApplyChildRendererAlpha(Transform root, float alphaMultiplier)
        {
            if (root == null)
            {
                return;
            }

            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
            for (int index = 0; index < renderers.Length; index++)
            {
                MeshRenderer renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);
                Color baseColor = ResolveOverlayBaseColor(renderer);

                baseColor.a *= alphaMultiplier;
                propertyBlock.SetColor("_Color", baseColor);
                propertyBlock.SetColor("_BaseColor", baseColor);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private Color ResolveOverlayBaseColor(MeshRenderer renderer)
        {
            if (renderer == null)
            {
                return Color.white;
            }

            string rendererName = renderer.transform.name;
            if (rendererName.StartsWith("DecorationMarker_"))
            {
                int markerIndex = 0;
                if (rendererName.Length >= 2)
                {
                    int.TryParse(rendererName.Substring(rendererName.Length - 2), out markerIndex);
                }

                return Color.Lerp(
                    decorationProfile.decorationPrimaryColor,
                    decorationProfile.decorationSecondaryColor,
                    markerIndex % 2 == 0 ? 0.18f : 0.62f);
            }

            if (rendererName == "DestinationMarker_Glow")
            {
                return new Color(
                    decorationProfile.destinationMarkerColor.r,
                    decorationProfile.destinationMarkerColor.g,
                    decorationProfile.destinationMarkerColor.b,
                    decorationProfile.destinationMarkerColor.a * 0.52f);
            }

            if (rendererName == "DestinationMarker_Ring")
            {
                return decorationProfile.destinationMarkerColor;
            }

            return renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor")
                ? renderer.sharedMaterial.GetColor("_BaseColor")
                : Color.white;
        }

        private float GetGuideSurfaceY(Vector3 worldPosition)
        {
            if (Physics.Raycast(
                worldPosition + Vector3.up * 60f,
                Vector3.down,
                out RaycastHit hit,
                140f,
                groundLayers,
                QueryTriggerInteraction.Ignore))
            {
                return hit.point.y + groundOffset;
            }

            return worldPosition.y + groundOffset;
        }

        private Vector3 GetGuidePoint(Vector3 worldPosition)
        {
            Vector3 point = worldPosition;
            point.y = GetGuideSurfaceY(worldPosition);
            return point;
        }

        private void AddUniquePoint(List<Vector3> points, Vector3 worldPosition)
        {
            Vector3 guidePoint = GetGuidePoint(worldPosition);
            if (points.Count == 0)
            {
                points.Add(guidePoint);
                return;
            }

            if (Vector3.Distance(points[points.Count - 1], guidePoint) > 0.2f)
            {
                points.Add(guidePoint);
            }
        }

        private void AddUniqueResolvedPoint(List<Vector3> points, Vector3 worldPosition, bool forceAdd)
        {
            Vector3 guidePoint = GetGuidePoint(worldPosition);
            if (points.Count == 0)
            {
                points.Add(guidePoint);
                return;
            }

            if (forceAdd || Vector3.Distance(points[points.Count - 1], guidePoint) > 0.08f)
            {
                points.Add(guidePoint);
            }
        }

        private bool TryResolveVisibleSegment(Vector3 from, Vector3 to, out Vector3 visibleFrom, out Vector3 visibleTo)
        {
            visibleFrom = from;
            visibleTo = to;

            Vector3 segment = to - from;
            float segmentLength = segment.magnitude;
            if (segmentLength < Mathf.Max(0.1f, minimumVisibleSegmentLength))
            {
                return false;
            }

            if (!trimSegmentsAgainstObstacles)
            {
                return true;
            }

            Vector3 direction = segment / segmentLength;
            Vector3 castOrigin = from + Vector3.up * obstacleProbeHeight;
            float castDistance = Mathf.Max(0f, segmentLength - 0.02f);
            RaycastHit[] hits = Physics.SphereCastAll(
                castOrigin,
                Mathf.Max(0.01f, obstacleProbeRadius),
                direction,
                castDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            RaycastHit? firstBlockingHit = GetFirstBlockingHit(hits);
            if (!firstBlockingHit.HasValue)
            {
                return true;
            }

            float safeDistance = Mathf.Max(0f, firstBlockingHit.Value.distance - Mathf.Max(0.02f, obstacleClearance));
            if (safeDistance < Mathf.Max(0.08f, minimumVisibleSegmentLength))
            {
                return false;
            }

            Vector3 safePoint = castOrigin + direction * safeDistance;
            visibleTo = GetGuidePoint(new Vector3(safePoint.x, Mathf.Lerp(from.y, to.y, safeDistance / segmentLength), safePoint.z));
            return Vector3.Distance(visibleFrom, visibleTo) >= Mathf.Max(0.08f, minimumVisibleSegmentLength);
        }

        private RaycastHit? GetFirstBlockingHit(RaycastHit[] hits)
        {
            if (hits == null || hits.Length == 0)
            {
                return null;
            }

            RaycastHit? bestHit = null;
            float bestDistance = float.PositiveInfinity;
            for (int index = 0; index < hits.Length; index++)
            {
                Collider collider = hits[index].collider;
                if (ShouldIgnoreObstacle(collider))
                {
                    continue;
                }

                if (hits[index].distance < bestDistance)
                {
                    bestDistance = hits[index].distance;
                    bestHit = hits[index];
                }
            }

            return bestHit;
        }

        private bool ShouldIgnoreObstacle(Collider collider)
        {
            if (collider == null)
            {
                return true;
            }

            Transform hitTransform = collider.transform;
            if (_runtimeRoot != null && hitTransform.IsChildOf(_runtimeRoot))
            {
                return true;
            }

            if (manager != null && manager.playerController != null && hitTransform.IsChildOf(manager.playerController.transform))
            {
                return true;
            }

            Transform authoredRoot = FindAuthoredRouteRoot();
            if (authoredRoot != null && hitTransform.IsChildOf(authoredRoot))
            {
                return true;
            }

            return false;
        }

        private static void ApplyMaterialColors(Material material, Color baseColor, Color accentColor, float alphaScale, float glowBoost)
        {
            if (material == null)
            {
                return;
            }

            material.color = baseColor;
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_AccentColor"))
            {
                material.SetColor("_AccentColor", accentColor);
            }

            if (material.HasProperty("_AlphaScale"))
            {
                material.SetFloat("_AlphaScale", alphaScale);
            }

            if (material.HasProperty("_GlowBoost"))
            {
                material.SetFloat("_GlowBoost", glowBoost);
            }
        }

        private static void ApplyMaterialMotion(Material material, float flowSpeed, float pulseSpeed, float edgeSoftness)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_FlowSpeed"))
            {
                material.SetFloat("_FlowSpeed", flowSpeed);
            }

            if (material.HasProperty("_PulseSpeed"))
            {
                material.SetFloat("_PulseSpeed", pulseSpeed);
            }

            if (material.HasProperty("_EdgeSoftness"))
            {
                material.SetFloat("_EdgeSoftness", edgeSoftness);
            }
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
