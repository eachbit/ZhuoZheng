using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class Chapter03PlaqueFrame
{
    private const string HintFramePrefix = "Chapter03PlaqueFrame_";
    private static readonly Color PanelInkColor = new Color(0.065f, 0.105f, 0.08f, 0.78f);
    private static readonly Color PanelInkSoftColor = new Color(0.075f, 0.12f, 0.085f, 0.66f);
    private static readonly Color GoldLineColor = new Color(0.82f, 0.68f, 0.36f, 0.9f);
    private static readonly Color GoldCornerColor = new Color(0.82f, 0.68f, 0.36f, 0.7f);
    private static Sprite s_plaqueBackgroundSprite;

    public static void ApplyPanel(GameObject panel)
    {
        Apply(panel, PanelInkColor, 5f, 18f, true);
    }

    public static void ApplySoftPanel(GameObject panel)
    {
        Apply(panel, PanelInkSoftColor, 5f, 18f, true);
    }

    public static void ApplyHintFrame(Component textComponent, bool isVisible)
    {
        if (textComponent == null)
        {
            return;
        }

        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        if (textRect == null || textRect.parent == null)
        {
            return;
        }

        Transform parent = textRect.parent;
        GameObject frame = FindOrCreateSibling(HintFramePrefix + textComponent.gameObject.name, parent);
        RectTransform frameRect = EnsureRectTransform(frame);
        frameRect.anchorMin = textRect.anchorMin;
        frameRect.anchorMax = textRect.anchorMax;
        frameRect.pivot = textRect.pivot;
        frameRect.anchoredPosition = textRect.anchoredPosition;
        frameRect.sizeDelta = textRect.sizeDelta + new Vector2(56f, 40f);
        frameRect.localRotation = Quaternion.identity;
        frameRect.localScale = Vector3.one;

        Apply(frame, PanelInkColor, 5f, 18f, false);
        frame.SetActive(isVisible && textComponent.gameObject.activeSelf);
        PutSiblingImmediatelyBehind(frame.transform, textRect);
        ApplyTextStyle(textComponent);
    }

    private static void Apply(GameObject target, Color backgroundColor, float lineThickness, float cornerSize, bool keepPanelRaycast)
    {
        if (target == null)
        {
            return;
        }

        Image image;
        if (!TryGetOrAddImage(target, out image))
        {
            return;
        }

        image.sprite = GetPlaqueBackgroundSprite();
        image.type = Image.Type.Sliced;
        image.color = backgroundColor;
        image.raycastTarget = keepPanelRaycast && image.raycastTarget;

        EnsureFrameStrip("GoldFrameTop", target.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -lineThickness), Vector2.zero, lineThickness);
        EnsureFrameStrip("GoldFrameBottom", target.transform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, lineThickness), lineThickness);
        EnsureFrameStrip("GoldFrameLeft", target.transform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(lineThickness, 0f), lineThickness);
        EnsureFrameStrip("GoldFrameRight", target.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-lineThickness, 0f), Vector2.zero, lineThickness);

        EnsureCornerBlock("GoldCornerUpperLeft", target.transform, new Vector2(0f, 1f), new Vector2(cornerSize, -cornerSize), cornerSize);
        EnsureCornerBlock("GoldCornerUpperRight", target.transform, Vector2.one, new Vector2(-cornerSize, -cornerSize), cornerSize);
        EnsureCornerBlock("GoldCornerLowerLeft", target.transform, Vector2.zero, new Vector2(cornerSize, cornerSize), cornerSize);
        EnsureCornerBlock("GoldCornerLowerRight", target.transform, new Vector2(1f, 0f), new Vector2(-cornerSize, cornerSize), cornerSize);
    }

    private static void ApplyTextStyle(Component textComponent)
    {
        TextMeshProUGUI tmpText = textComponent as TextMeshProUGUI;
        if (tmpText != null)
        {
            RectTransform textRect = tmpText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.localScale = Vector3.one;
            }

            tmpText.enableWordWrapping = true;
            tmpText.enableAutoSizing = true;
            tmpText.fontSizeMin = 18f;
            tmpText.fontSizeMax = Mathf.Max(tmpText.fontSizeMax, 34f);
            tmpText.overflowMode = TextOverflowModes.Ellipsis;
            tmpText.margin = new Vector4(24f, 12f, 24f, 12f);
            tmpText.alignment = TextAlignmentOptions.Center;
            return;
        }

        Text uiText = textComponent as Text;
        if (uiText != null)
        {
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }

    private static void EnsureFrameStrip(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, float fallbackThickness)
    {
        GameObject strip = FindOrCreateChild(name, parent);
        RectTransform rectTransform = EnsureRectTransform(strip);
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        if (Mathf.Approximately(anchorMin.x, anchorMax.x))
        {
            rectTransform.sizeDelta = new Vector2(fallbackThickness, rectTransform.sizeDelta.y);
        }

        Image image;
        if (!TryGetOrAddImage(strip, out image))
        {
            return;
        }

        image.color = GoldLineColor;
        image.raycastTarget = false;
        strip.transform.SetAsFirstSibling();
    }

    private static void EnsureCornerBlock(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float size)
    {
        GameObject corner = FindOrCreateChild(name, parent);
        RectTransform rectTransform = EnsureRectTransform(corner);
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = anchor;
        rectTransform.sizeDelta = new Vector2(size, size);
        rectTransform.anchoredPosition = anchoredPosition;

        Image image;
        if (!TryGetOrAddImage(corner, out image))
        {
            return;
        }

        image.color = GoldCornerColor;
        image.raycastTarget = false;
        corner.transform.SetAsFirstSibling();
    }

    private static void PutSiblingImmediatelyBehind(Transform frameTransform, RectTransform textRect)
    {
        int targetIndex = textRect.GetSiblingIndex();
        frameTransform.SetSiblingIndex(targetIndex);
        textRect.SetSiblingIndex(Mathf.Min(frameTransform.GetSiblingIndex() + 1, textRect.parent.childCount - 1));
    }

    private static GameObject FindOrCreateSibling(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject sibling = new GameObject(name, typeof(RectTransform));
        sibling.transform.SetParent(parent, false);
        return sibling;
    }

    private static GameObject FindOrCreateChild(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private static RectTransform EnsureRectTransform(GameObject target)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = target.AddComponent<RectTransform>();
        }

        return rectTransform;
    }

    private static bool TryGetOrAddImage(GameObject target, out Image image)
    {
        image = target.GetComponent<Image>();
        if (image == null)
        {
            Graphic existingGraphic = target.GetComponent<Graphic>();
            if (existingGraphic != null)
            {
                return false;
            }

            image = target.AddComponent<Image>();
        }

        return image != null;
    }

    private static Sprite GetPlaqueBackgroundSprite()
    {
        if (s_plaqueBackgroundSprite != null)
        {
            return s_plaqueBackgroundSprite;
        }

        const int size = 48;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Chapter03_PlaqueBackground_Runtime",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float vertical = y / (float)(size - 1);
                float horizontal = Mathf.Abs((x / (float)(size - 1)) - 0.5f) * 2f;
                float edgeFade = Mathf.Clamp01(Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y)) / 8f);
                float softGrain = (((x * 17 + y * 31) % 11) - 5) / 255f;
                float shade = Mathf.Lerp(0.72f, 1.12f, vertical) - (horizontal * 0.08f) + softGrain;
                Color color = new Color(0.055f * shade, 0.11f * shade, 0.075f * shade, Mathf.Lerp(0.64f, 0.86f, edgeFade));
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        s_plaqueBackgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(12f, 12f, 12f, 12f));
        return s_plaqueBackgroundSprite;
    }
}
