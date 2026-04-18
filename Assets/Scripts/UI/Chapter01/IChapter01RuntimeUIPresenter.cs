using System;
using UnityEngine;

namespace ZhuozhengYuan
{
    public interface IChapter01RuntimeUIPresenter
    {
        bool IsDialogueOpen { get; }

        void SetPageCount(int currentPages, int maxPages);
        void SetInteractionPrompt(string prompt);
        void SetObjective(string objective);
        void ShowToast(string message, float duration = 2.2f);
        void ShowPageReward(string title, string message, float duration = 3.4f);
        void ShowDirectionResult(string title, string message, Color accentColor, float duration = 2.6f);
        void ShowDialogue(DialogueLine[] dialogueLines, Action onCompleted);
        void ShowDirectionChoice(string[] options, Action<string> onSelected);
        void ShowGateCalibration(Chapter01GateCalibrationViewData data);
        void HideGateCalibration();
        void SetFadeAlpha(float alpha);
    }
}
