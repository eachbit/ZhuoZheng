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
        public int chapter01RejectedFlowDirections;
        public bool chapter01PageCollected;
        public Chapter02State chapter02State = Chapter02State.NotStarted;
        public int chapter02AnsweredCorrectCount;
        public string[] chapter02QuestionOrder = Array.Empty<string>();
        public bool chapter02PageCollected;
        public bool chapter04PageCollected;

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
                chapter01RejectedFlowDirections = 0,
                chapter01PageCollected = false,
                chapter02State = Chapter02State.NotStarted,
                chapter02AnsweredCorrectCount = 0,
                chapter02QuestionOrder = Array.Empty<string>(),
                chapter02PageCollected = false,
                chapter04PageCollected = false
            };
        }
    }
}
