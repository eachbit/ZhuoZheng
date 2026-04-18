using UnityEngine;
using UnityEngine.UI;

internal static class Chapter04PlaqueFrame
{
    private static readonly Color PanelInkColor = new Color(0.06f, 0.09f, 0.08f, 0.9f);
    private static readonly Color PanelInkSoftColor = new Color(0.08f, 0.11f, 0.1f, 0.82f);
    private static readonly Color GoldLineColor = new Color(0.78f, 0.64f, 0.28f, 0.92f);
    private static readonly Color GoldCornerColor = new Color(0.78f, 0.64f, 0.28f, 0.72f);

    public static void ApplyPanel(GameObject panel)
    {
        Apply(panel, PanelInkColor, 5f, 18f);
    }

    public static void ApplySoftPanel(GameObject panel)
    {
        Apply(panel, PanelInkSoftColor, 5f, 18f);
    }

    public static void ApplyButton(GameObject buttonObject)
    {
        Apply(buttonObject, new Color(0.04f, 0.06f, 0.05f, 0.96f), 4f, 14f);
    }

    private static void Apply(GameObject target, Color backgroundColor, float lineThickness, float cornerSize)
    {
        if (target == null)
        {
            return;
        }

        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.AddComponent<Image>();
        }

        image.color = backgroundColor;
        EnsureFrameStrip("GoldFrameTop", target.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -lineThickness), Vector2.zero, lineThickness);
        EnsureFrameStrip("GoldFrameBottom", target.transform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, lineThickness), lineThickness);
        EnsureFrameStrip("GoldFrameLeft", target.transform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(lineThickness, 0f), lineThickness);
        EnsureFrameStrip("GoldFrameRight", target.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-lineThickness, 0f), Vector2.zero, lineThickness);

        EnsureCornerBlock("GoldCornerUpperLeft", target.transform, new Vector2(0f, 1f), new Vector2(cornerSize, -cornerSize), cornerSize);
        EnsureCornerBlock("GoldCornerUpperRight", target.transform, Vector2.one, new Vector2(-cornerSize, -cornerSize), cornerSize);
        EnsureCornerBlock("GoldCornerLowerLeft", target.transform, Vector2.zero, new Vector2(cornerSize, cornerSize), cornerSize);
        EnsureCornerBlock("GoldCornerLowerRight", target.transform, new Vector2(1f, 0f), new Vector2(-cornerSize, cornerSize), cornerSize);
    }

    private static void EnsureFrameStrip(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, float fallbackThickness)
    {
        GameObject strip = FindOrCreateChild(name, parent);
        RectTransform rectTransform = EnsureRectTransform(strip);
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        if (anchorMin.x == anchorMax.x)
        {
            rectTransform.sizeDelta = new Vector2(fallbackThickness, rectTransform.sizeDelta.y);
        }

        Image image = EnsureImage(strip);
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

        Image image = EnsureImage(corner);
        image.color = GoldCornerColor;
        image.raycastTarget = false;
        corner.transform.SetAsFirstSibling();
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

    private static Image EnsureImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.AddComponent<Image>();
        }

        return image;
    }
}
