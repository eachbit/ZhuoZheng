using System;
using System.Collections;
using UnityEngine;

namespace ZhuozhengYuan
{
    [RequireComponent(typeof(Collider))]
    public class Chapter02Director : MonoBehaviour
    {
        public GardenGameManager manager;
        public Chapter02Question[] questionBank;
        public int questionsRequiredToUnlock = 4;
        public string chapterTitle = "\u7b2c\u4e8c\u7ae0\uff1a\u5c0f\u98de\u8679\u7b54\u9898";
        public string objectiveReachTrigger = "\u524d\u5f80\u5c0f\u98de\u8679\u533a\u57df\uff0c\u89e6\u53d1\u7b54\u9898\u6311\u6218\u3002";
        public string objectiveAnswerQuestions = "\u7b54\u5bf9 4 \u9053\u4e0e\u62d9\u653f\u56ed\u5c0f\u98de\u8679\u6709\u5173\u7684\u9898\u76ee\uff0c\u624d\u80fd\u7ee7\u7eed\u524d\u8fdb\u3002";
        public string objectiveCompleted = "\u7b2c\u4e8c\u7ae0\u8282\u7b54\u9898\u5b8c\u6210\uff0c\u524d\u65b9\u9053\u8def\u5df2\u5f00\u542f\u3002";
        public string quizStartedToast = "\u4f60\u5df2\u8fdb\u5165\u5c0f\u98de\u8679\u7b54\u9898\u73af\u8282\u3002";
        public string unlockToast = "\u56db\u9898\u5168\u90e8\u7b54\u5bf9\uff0c\u524d\u65b9\u9053\u8def\u5df2\u5f00\u542f\u3002";
        public string pageRewardTitle = "\u83b7\u5f97\u6b8b\u9875";
        public string pageRewardMessage = "\u5df2\u83b7\u5f97\u300a\u957f\u7269\u5fd7\u300b\u7b2c\u4e8c\u5f20\u6b8b\u9875";
        public string progressFormat = "\u7b54\u9898\u8fdb\u5ea6 {0}/{1}";
        public bool startWhenPlayerEntersTrigger = true;
        public bool disableTriggerAfterCompletion = true;
        public GameObject[] blockersToDisable;
        public Collider[] blockerCollidersToDisable;

        public Chapter02State CurrentState { get; private set; }

        private Chapter02QuizSession _session;
        private Coroutine _reopenQuizCoroutine;

        private void Awake()
        {
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        public void Initialize(GardenGameManager gameManager, SaveData saveData)
        {
            manager = gameManager;
            EnsureQuestionBankDefaults();
            ApplySaveState(saveData);
        }

        public void OnIntroFinished()
        {
            UpdateObjective();
        }

        public void ApplySaveState(SaveData saveData)
        {
            if (saveData == null)
            {
                saveData = SaveData.CreateDefault();
            }

            EnsureQuestionBankDefaults();

            CurrentState = saveData.chapter02State;
            _session = null;

            if (CurrentState == Chapter02State.Completed)
            {
                ApplyCompletionState();
                UpdateObjective();
                return;
            }

            if (CurrentState == Chapter02State.InProgress)
            {
                _session = Chapter02QuizSession.Restore(questionBank, saveData.chapter02QuestionOrder, saveData.chapter02AnsweredCorrectCount);
                if (_session == null)
                {
                    CurrentState = Chapter02State.NotStarted;
                    saveData.chapter02State = Chapter02State.NotStarted;
                    saveData.chapter02AnsweredCorrectCount = 0;
                    saveData.chapter02QuestionOrder = Array.Empty<string>();
                }
            }

            SetBlockersUnlocked(false);
            UpdateObjective();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!startWhenPlayerEntersTrigger || CurrentState == Chapter02State.Completed || !IsPlayerCollider(other))
            {
                return;
            }

            StartQuizIfNeeded();
        }

        public void StartQuizIfNeeded()
        {
            if (manager == null || CurrentState == Chapter02State.Completed)
            {
                return;
            }

            EnsureQuestionBankDefaults();
            bool isFreshSession = false;

            if (_session == null)
            {
                int questionCount = Mathf.Clamp(questionsRequiredToUnlock, 1, questionBank.Length);
                int seed = GenerateSessionSeed();
                _session = Chapter02QuizSession.CreateRandomized(questionBank, questionCount, seed);
                isFreshSession = true;
            }

            CurrentState = Chapter02State.InProgress;
            if (isFreshSession)
            {
                manager.ShowToast(quizStartedToast, 2.5f);
            }
            WriteBackSaveState();
            manager.SaveProgress();
            ShowCurrentQuestion();
            UpdateObjective();
        }

        public void HandleQuizAnswer(int selectedOptionIndex)
        {
            if (_session == null || CurrentState != Chapter02State.InProgress)
            {
                return;
            }

            Chapter02QuizSession.AnswerResult result = _session.SubmitAnswer(selectedOptionIndex);
            string feedback = result.isCorrect ? result.question.correctFeedback : result.question.wrongFeedback;
            if (string.IsNullOrWhiteSpace(feedback))
            {
                feedback = result.isCorrect ? "Correct answer." : "That is not correct. Try again.";
            }

            if (manager != null)
            {
                manager.ShowToast(feedback, 2.2f);
            }

            if (result.isCompleted)
            {
                CompleteQuiz();
                return;
            }

            WriteBackSaveState();
            if (manager != null)
            {
                manager.SaveProgress();
            }

            if (_reopenQuizCoroutine != null)
            {
                StopCoroutine(_reopenQuizCoroutine);
            }

            _reopenQuizCoroutine = StartCoroutine(ReopenQuizNextFrame());
            UpdateObjective();
        }

        private IEnumerator ReopenQuizNextFrame()
        {
            yield return null;
            _reopenQuizCoroutine = null;
            ShowCurrentQuestion();
        }

        private void ShowCurrentQuestion()
        {
            if (manager == null || _session == null || _session.CurrentQuestion == null)
            {
                return;
            }

            Chapter02Question question = _session.CurrentQuestion;
            string progress = string.Format(progressFormat, _session.AnsweredCorrectCount + 1, _session.TotalQuestionCount);
            manager.ShowChapter02Quiz(
                chapterTitle,
                progress,
                question.questionText,
                question.options,
                HandleQuizAnswer);
        }

        private void CompleteQuiz()
        {
            CurrentState = Chapter02State.Completed;
            SetBlockersUnlocked(true);

            if (disableTriggerAfterCompletion)
            {
                Collider triggerCollider = GetComponent<Collider>();
                if (triggerCollider != null)
                {
                    triggerCollider.enabled = false;
                }
            }

            if (manager != null)
            {
                manager.HideChapter02Quiz();
                manager.ShowToast(unlockToast, 2.8f);
                if (TryAwardChapter02Page(manager.CurrentSaveData, manager.totalPages))
                {
                    manager.RefreshCollectedPagesDisplay();
                    manager.ShowPageReward(pageRewardTitle, pageRewardMessage, 3.8f);
                }
            }

            WriteBackSaveState();
            if (manager != null)
            {
                manager.SaveProgress();
            }

            UpdateObjective();
        }

        private void ApplyCompletionState()
        {
            _session = null;
            SetBlockersUnlocked(true);

            if (disableTriggerAfterCompletion)
            {
                Collider triggerCollider = GetComponent<Collider>();
                if (triggerCollider != null)
                {
                    triggerCollider.enabled = false;
                }
            }
        }

        private void SetBlockersUnlocked(bool unlocked)
        {
            if (blockersToDisable != null)
            {
                for (int index = 0; index < blockersToDisable.Length; index++)
                {
                    if (blockersToDisable[index] != null)
                    {
                        blockersToDisable[index].SetActive(!unlocked);
                    }
                }
            }

            if (blockerCollidersToDisable != null)
            {
                for (int index = 0; index < blockerCollidersToDisable.Length; index++)
                {
                    if (blockerCollidersToDisable[index] != null)
                    {
                        blockerCollidersToDisable[index].enabled = !unlocked;
                    }
                }
            }
        }

        private void UpdateObjective()
        {
            if (manager == null)
            {
                return;
            }

            if (CurrentState == Chapter02State.Completed)
            {
                manager.SetChapter02Objective(objectiveCompleted);
                return;
            }

            if (CurrentState == Chapter02State.InProgress && _session != null)
            {
                manager.SetChapter02Objective(string.Format(progressFormat, _session.AnsweredCorrectCount, _session.TotalQuestionCount) + " - " + objectiveAnswerQuestions);
                return;
            }

            if (!ShouldShowReachTriggerObjective(manager.CurrentSaveData))
            {
                return;
            }

            manager.SetChapter02Objective(objectiveReachTrigger);
        }

        private static bool ShouldShowReachTriggerObjective(SaveData saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            return saveData.leftGateOpened
                && saveData.rightGateOpened
                && !string.IsNullOrWhiteSpace(saveData.selectedFlowDirection);
        }

        private static bool TryAwardChapter02Page(SaveData saveData, int totalPages)
        {
            if (saveData == null || saveData.chapter02PageCollected)
            {
                return false;
            }

            saveData.chapter02PageCollected = true;
            saveData.collectedPages = Mathf.Clamp(saveData.collectedPages + 1, 0, Mathf.Max(1, totalPages));
            return true;
        }

        private void WriteBackSaveState()
        {
            if (manager == null || manager.CurrentSaveData == null)
            {
                return;
            }

            manager.CurrentSaveData.chapter02State = CurrentState;
            manager.CurrentSaveData.chapter02AnsweredCorrectCount = _session != null ? _session.AnsweredCorrectCount : 0;
            manager.CurrentSaveData.chapter02QuestionOrder = _session != null ? _session.GetOrderedQuestionIds() : Array.Empty<string>();
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            if (manager != null)
            {
                if (manager.playerViewModeController != null && IsSameTransformTree(other.transform, manager.playerViewModeController.transform))
                {
                    return true;
                }

                if (manager.playerController != null && IsSameTransformTree(other.transform, manager.playerController.transform))
                {
                    return true;
                }
            }

            return other.GetComponentInParent<PlayerInteractor>() != null
                || other.GetComponentInParent<StarterAssetsThirdPersonBridge>() != null;
        }

        private static bool IsSameTransformTree(Transform left, Transform right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left == right || left.IsChildOf(right) || right.IsChildOf(left);
        }

        private int GenerateSessionSeed()
        {
            unchecked
            {
                int seed = DateTime.Now.Millisecond;
                seed = (seed * 397) ^ Mathf.RoundToInt(transform.position.x * 10f);
                seed = (seed * 397) ^ Mathf.RoundToInt(transform.position.z * 10f);
                seed = (seed * 397) ^ questionBank.Length;
                return seed;
            }
        }

        private void EnsureQuestionBankDefaults()
        {
            if (questionBank != null && questionBank.Length >= 4)
            {
                return;
            }

            questionBank = CreateDefaultQuestionBank();
        }

        private static Chapter02Question[] CreateDefaultQuestionBank()
        {
            return new[]
            {
                new Chapter02Question
                {
                    questionId = "xiaofeihong-01",
                    questionText = "\u62d9\u653f\u56ed\u91cc\u8fd9\u5ea7\u8457\u540d\u7684\u5eca\u6865\uff0c\u5e38\u89c1\u7684\u6b63\u5f0f\u540d\u79f0\u662f\u4ec0\u4e48\uff1f",
                    options = new[] { "\u5c0f\u98de\u8679", "\u5c0f\u98de\u4ead", "\u5c0f\u98de\u9601", "\u5c0f\u98de\u5eca" },
                    correctOptionIndex = 0,
                    correctFeedback = "\u56de\u7b54\u6b63\u786e\u3002\u62d9\u653f\u56ed\u4e2d\u8fd9\u5ea7\u8457\u540d\u5eca\u6865\u5e38\u79f0\u201c\u5c0f\u98de\u8679\u201d\u3002",
                    wrongFeedback = "\u518d\u60f3\u60f3\uff0c\u540d\u5b57\u91cc\u70b9\u51fa\u4e86\u5b83\u5982\u8679\u51cc\u6ce2\u7684\u610f\u8c61\u3002"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-02",
                    questionText = "\u5c0f\u98de\u8679\u5728\u56ed\u6797\u5efa\u7b51\u7c7b\u578b\u4e0a\uff0c\u6700\u51c6\u786e\u7684\u8bf4\u6cd5\u662f\u54ea\u4e00\u9879\uff1f",
                    options = new[] { "\u724c\u574a", "\u5eca\u6865", "\u620f\u53f0", "\u6c34\u69ad" },
                    correctOptionIndex = 1,
                    correctFeedback = "\u56de\u7b54\u6b63\u786e\u3002\u5c0f\u98de\u8679\u5c5e\u4e8e\u8de8\u6c34\u800c\u5efa\u7684\u5eca\u6865\u3002",
                    wrongFeedback = "\u63d0\u793a\uff1a\u5b83\u65e2\u80fd\u901a\u884c\uff0c\u4e5f\u80fd\u906e\u853d\u3001\u89c2\u666f\u3002"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-03",
                    questionText = "\u5c0f\u98de\u8679\u4e3b\u8981\u4f4d\u4e8e\u62d9\u653f\u56ed\u7684\u54ea\u4e2a\u90e8\u5206\uff1f",
                    options = new[] { "\u4e1c\u56ed", "\u4e2d\u56ed", "\u897f\u56ed", "\u51fa\u53e3\u5916\u8857\u5df7" },
                    correctOptionIndex = 1,
                    correctFeedback = "\u56de\u7b54\u6b63\u786e\u3002\u5c0f\u98de\u8679\u4f4d\u4e8e\u62d9\u653f\u56ed\u7684\u4e2d\u56ed\u533a\u57df\u3002",
                    wrongFeedback = "\u60f3\u60f3\u62d9\u653f\u56ed\u6700\u7cbe\u534e\u3001\u4ee5\u6c34\u9762\u4e3a\u4e2d\u5fc3\u5c55\u5f00\u7684\u90a3\u4e00\u90e8\u5206\u3002"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-04",
                    questionText = "\u636e\u56ed\u6797\u5bfc\u89c8\u4ecb\u7ecd\uff0c\u6e38\u5ba2\u201c\u8fc7\u5c0f\u98de\u8679\u201d\u4e4b\u540e\uff0c\u53ef\u4ee5\u524d\u5f80\u54ea\u4e00\u5904\u666f\u70b9\uff1f",
                    options = new[] { "\u8fdc\u9999\u5802", "\u9999\u6d32", "\u5170\u96ea\u5802", "\u89c1\u5c71\u697c" },
                    correctOptionIndex = 1,
                    correctFeedback = "\u56de\u7b54\u6b63\u786e\u3002\u8fc7\u5c0f\u98de\u8679\u540e\uff0c\u53ef\u524d\u5f80\u9999\u6d32\u4e00\u5e26\u89c2\u666f\u3002",
                    wrongFeedback = "\u63d0\u793a\uff1a\u5b83\u8fde\u63a5\u7740\u90a3\u8258\u50cf\u753b\u822b\u4e00\u6837\u505c\u6cca\u5728\u6c34\u8fb9\u7684\u5efa\u7b51\u3002"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-05",
                    questionText = "\u201c\u5c0f\u98de\u8679\u201d\u8fd9\u4e2a\u540d\u5b57\uff0c\u6700\u5bb9\u6613\u8ba9\u4eba\u8054\u60f3\u5230\u54ea\u79cd\u666f\u8c61\uff1f",
                    options = new[] { "\u6865\u5f71\u5165\u6c34\uff0c\u5982\u8679\u8f7b\u98de", "\u6865\u4e0a\u6652\u8c37\uff0c\u91d1\u9ec4\u904d\u5730", "\u6865\u4e0b\u5de8\u6d6a\u7ffb\u6eda", "\u6865\u9876\u79ef\u96ea\u6210\u5c71" },
                    correctOptionIndex = 0,
                    correctFeedback = "\u56de\u7b54\u6b63\u786e\u3002\u5c0f\u98de\u8679\u7684\u540d\u5b57\u6b63\u5e26\u6709\u6865\u5f71\u5982\u8679\u7684\u8bd7\u610f\u8054\u60f3\u3002",
                    wrongFeedback = "\u60f3\u60f3\u5b83\u6a2a\u8de8\u6c34\u9762\u65f6\uff0c\u4e0e\u5012\u5f71\u4e00\u8d77\u5f62\u6210\u7684\u753b\u9762\u3002"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-06",
                    questionText = "\u4ece\u6e38\u56ed\u4f53\u9a8c\u6765\u770b\uff0c\u5c0f\u98de\u8679\u6700\u80fd\u4f53\u73b0\u4e0b\u9762\u54ea\u79cd\u9020\u56ed\u7279\u70b9\uff1f",
                    options = new[] { "\u79fb\u6b65\u6362\u666f", "\u6574\u9f50\u5bf9\u79f0\u5230\u5904\u76f8\u540c", "\u53ea\u91cd\u56f4\u5899\u4e0d\u91cd\u6c34\u9762", "\u5b8c\u5168\u5c01\u95ed\u4e0d\u8ba9\u89c2\u666f" },
                    correctOptionIndex = 0,
                    correctFeedback = "\u56de\u7b54\u6b63\u786e\u3002\u8d70\u4e0a\u5c0f\u98de\u8679\uff0c\u666f\u8272\u4f1a\u968f\u7740\u6b65\u4f10\u4e0d\u65ad\u53d8\u5316\u3002",
                    wrongFeedback = "\u63d0\u793a\uff1a\u4eba\u5728\u6865\u4e0a\u884c\u8d70\u65f6\uff0c\u773c\u524d\u666f\u8272\u4f1a\u4e00\u5c42\u5c42\u5c55\u5f00\u3002"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-07",
                    questionText = "\u5c0f\u98de\u8679\u8de8\u8d8a\u7684\u4e3b\u8981\u7a7a\u95f4\u662f\u4ec0\u4e48\uff1f",
                    options = new[] { "\u5c71\u5761", "\u6c60\u6c34", "\u8857\u5df7", "\u82b1\u5703" },
                    correctOptionIndex = 1,
                    correctFeedback = "\u56de\u7b54\u6b63\u786e\u3002\u5c0f\u98de\u8679\u662f\u8de8\u8d8a\u6c60\u6c34\u800c\u5efa\u7684\u5eca\u6865\u3002",
                    wrongFeedback = "\u518d\u89c2\u5bdf\u4e00\u4e0b\u5b83\u6240\u5728\u7684\u4f4d\u7f6e\uff0c\u91cd\u70b9\u5728\u6c34\u9762\u4e4b\u4e0a\u3002"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-08",
                    questionText = "\u5728\u62d9\u653f\u56ed\u4e2d\uff0c\u5c0f\u98de\u8679\u9664\u4e86\u4f9b\u4eba\u901a\u884c\uff0c\u8fd8\u627f\u62c5\u4ec0\u4e48\u4f5c\u7528\uff1f",
                    options = new[] { "\u7ec4\u7ec7\u6e38\u7ebf\u5e76\u5f15\u5bfc\u89c2\u666f", "\u5b58\u653e\u5668\u7269", "\u9972\u517b\u9c7c\u9e1f", "\u906e\u6321\u5168\u90e8\u666f\u8272" },
                    correctOptionIndex = 0,
                    correctFeedback = "\u56de\u7b54\u6b63\u786e\u3002\u5b83\u628a\u901a\u884c\u4e0e\u89c2\u666f\u7ed3\u5408\u5728\u4e86\u4e00\u8d77\u3002",
                    wrongFeedback = "\u63d0\u793a\uff1a\u82cf\u5dde\u56ed\u6797\u91cc\u7684\u6865\u5eca\u5e38\u5e38\u517c\u5177\u201c\u8d70\u201d\u548c\u201c\u770b\u201d\u4e24\u79cd\u529f\u80fd\u3002"
                }
            };
        }
    }
}
