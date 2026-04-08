using System;

namespace ZhuozhengYuan
{
    [Serializable]
    public class SaveData
    {
        public bool introPlayed;
        public int collectedPages;
        public Chapter01State chapter01State = Chapter01State.NotStarted;
        public bool leftGateOpened;
        public bool rightGateOpened;
        public string selectedFlowDirection = string.Empty;
        public bool chapter01PageCollected;

        public static SaveData CreateDefault()
        {
            return new SaveData
            {
                introPlayed = false,
                collectedPages = 0,
                chapter01State = Chapter01State.NotStarted,
                leftGateOpened = false,
                rightGateOpened = false,
                selectedFlowDirection = string.Empty,
                chapter01PageCollected = false
            };
        }
    }
}
