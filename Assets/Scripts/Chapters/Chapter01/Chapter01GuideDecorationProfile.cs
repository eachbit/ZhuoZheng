using UnityEngine;

namespace ZhuozhengYuan
{
    [System.Serializable]
    public struct Chapter01GuideDecorationProfile
    {
        public Color ribbonBaseColor;
        public Color ribbonHighlightColor;
        public Color decorationPrimaryColor;
        public Color decorationSecondaryColor;
        public Color destinationMarkerColor;
        public float decorationSpacing;
        public float decorationSize;
        public float destinationMarkerHeight;
        public float destinationMarkerScale;
        public float animationSpeed;

        public static Chapter01GuideDecorationProfile CreateDefault()
        {
            return new Chapter01GuideDecorationProfile
            {
                ribbonBaseColor = new Color(0.34f, 0.68f, 0.62f, 0.78f),
                ribbonHighlightColor = new Color(0.95f, 0.88f, 0.66f, 0.92f),
                decorationPrimaryColor = new Color(0.66f, 0.86f, 0.82f, 0.42f),
                decorationSecondaryColor = new Color(0.92f, 0.84f, 0.58f, 0.28f),
                destinationMarkerColor = new Color(0.98f, 0.92f, 0.74f, 0.88f),
                decorationSpacing = 4f,
                decorationSize = 1.05f,
                destinationMarkerHeight = 1.6f,
                destinationMarkerScale = 0.72f,
                animationSpeed = 1.1f
            };
        }
    }
}
