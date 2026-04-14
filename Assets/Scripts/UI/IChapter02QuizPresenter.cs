using System;

namespace ZhuozhengYuan
{
    // UI teammates can replace the Chapter02 presentation by implementing this interface.
    public interface IChapter02QuizPresenter
    {
        void ShowChapter02Quiz(string title, string progressText, string questionText, string[] options, Action<int> onSelected);
        void HideChapter02Quiz();
    }
}
