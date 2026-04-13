using System;

namespace ZhuozhengYuan
{
    [Serializable]
    public class Chapter02Question
    {
        public string questionId = string.Empty;
        public string questionText = string.Empty;
        public string[] options = Array.Empty<string>();
        public int correctOptionIndex;
        public string correctFeedback = string.Empty;
        public string wrongFeedback = string.Empty;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(questionId)
                && !string.IsNullOrWhiteSpace(questionText)
                && options != null
                && options.Length == 4
                && correctOptionIndex >= 0
                && correctOptionIndex < options.Length;
        }
    }
}
