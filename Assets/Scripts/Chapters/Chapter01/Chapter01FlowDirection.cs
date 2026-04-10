using System;

namespace ZhuozhengYuan
{
    public static class Chapter01FlowDirection
    {
        public const string West = "West";
        public const string South = "South";
        public const string Center = "Center";

        private const string WestShortLabel = "\u897f";
        private const string SouthShortLabel = "\u5357";
        private const string CenterShortLabel = "\u4e2d";
        private const string LegacyEastShortLabel = "\u4e1c";
        private const string LegacyEastTraditionalShortLabel = "\u6771";

        private static readonly string[] DirectionIds =
        {
            West,
            South,
            Center
        };

        private static readonly string[] DirectionLabels =
        {
            "\u897f\u6e20",
            "\u5357\u6e20",
            "\u4e2d\u6c60"
        };

        public static string[] CreateOptionLabels()
        {
            string[] copy = new string[DirectionLabels.Length];
            Array.Copy(DirectionLabels, copy, DirectionLabels.Length);
            return copy;
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string trimmed = value.Trim();
            switch (trimmed)
            {
                case West:
                case WestShortLabel:
                case "\u897f\u6e20":
                    return West;
                case South:
                case SouthShortLabel:
                case "\u5357\u6e20":
                    return South;
                case Center:
                case CenterShortLabel:
                case "\u4e2d\u6c60":
                case LegacyEastShortLabel:
                case LegacyEastTraditionalShortLabel:
                    return Center;
                default:
                    return string.Empty;
            }
        }

        public static string GetLabel(string directionId)
        {
            switch (Normalize(directionId))
            {
                case West:
                    return DirectionLabels[0];
                case South:
                    return DirectionLabels[1];
                case Center:
                    return DirectionLabels[2];
                default:
                    return string.Empty;
            }
        }

        public static string GetIdFromOptionLabel(string optionLabel)
        {
            if (string.IsNullOrWhiteSpace(optionLabel))
            {
                return string.Empty;
            }

            for (int index = 0; index < DirectionLabels.Length; index++)
            {
                if (string.Equals(DirectionLabels[index], optionLabel, StringComparison.Ordinal))
                {
                    return DirectionIds[index];
                }

                if (optionLabel.StartsWith(DirectionLabels[index], StringComparison.Ordinal))
                {
                    return DirectionIds[index];
                }
            }

            return Normalize(optionLabel);
        }

        public static string GetIdByIndex(int index)
        {
            if (index < 0 || index >= DirectionIds.Length)
            {
                return string.Empty;
            }

            return DirectionIds[index];
        }

        public static int GetIndex(string directionId)
        {
            string normalized = Normalize(directionId);
            for (int index = 0; index < DirectionIds.Length; index++)
            {
                if (string.Equals(DirectionIds[index], normalized, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
