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
        public global::South chapter03SouthTarget;
        public Transform chapter03FireTargetOverride;
        public string chapter03RouteGuideObjectName = "Chapter02ToChapter03RouteGuide";
        public string chapter03RouteGuideRootName = "Chapter02ToChapter03GuidePath";
        public float chapter03RouteGuideReachedRadius = 4f;
        public int chapter03RouteGuideMaxDecorations = 6;
        public int chapter03RouteGuideAutoPointCount = 5;

        public Chapter02State CurrentState { get; private set; }

        private Chapter02QuizSession _session;
        private Coroutine _reopenQuizCoroutine;
        private Transform _chapter03RouteGuideRoot;

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
            ShowChapter03RouteGuide(ResolveChapter03GuideStartPosition());
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

        public static bool TryResolveChapter03GuideTarget(global::South southTarget, out Vector3 targetPosition)
        {
            targetPosition = Vector3.zero;
            if (southTarget == null)
            {
                return false;
            }

            if (southTarget.fireParticleSystem != null)
            {
                targetPosition = southTarget.fireParticleSystem.transform.position;
                return true;
            }

            if (southTarget.stoveAreaTrigger != null)
            {
                Collider triggerCollider = southTarget.stoveAreaTrigger.GetComponent<Collider>();
                targetPosition = triggerCollider != null
                    ? triggerCollider.bounds.center
                    : southTarget.stoveAreaTrigger.transform.position;
                return true;
            }

            targetPosition = southTarget.transform.position;
            return true;
        }

        private void ShowChapter03RouteGuide(Vector3 startPosition)
        {
            Vector3 chapter03TargetPosition;
            if (chapter03FireTargetOverride != null)
            {
                chapter03TargetPosition = chapter03FireTargetOverride.position;
            }
            else
            {
                global::South southTarget = ResolveChapter03SouthTarget();
                if (!TryResolveChapter03GuideTarget(southTarget, out chapter03TargetPosition))
                {
                    return;
                }
            }

            DestroyChapter03RouteGuide();

            GameObject routeGuideObject = new GameObject(string.IsNullOrWhiteSpace(chapter03RouteGuideObjectName)
                ? "Chapter02ToChapter03RouteGuide"
                : chapter03RouteGuideObjectName);
            _chapter03RouteGuideRoot = routeGuideObject.transform;
            _chapter03RouteGuideRoot.SetParent(transform, false);

            Transform startMarker = CreateRouteGuideMarker("Chapter02CompletedStart", _chapter03RouteGuideRoot, startPosition);
            Transform targetMarker = CreateRouteGuideMarker("Chapter03FireTarget", _chapter03RouteGuideRoot, chapter03TargetPosition);
            Transform[] routeMarkers = ResolveChapter03RouteMarkers(startPosition, chapter03TargetPosition, _chapter03RouteGuideRoot);

            Chapter01AuthoredRouteGuide routeGuide = routeGuideObject.AddComponent<Chapter01AuthoredRouteGuide>();
            routeGuide.manager = manager;
            routeGuide.director = null;
            routeGuide.introController = null;
            routeGuide.playerStartPose = startMarker;
            routeGuide.targetGate = targetMarker;
            routeGuide.authoredRouteRootName = chapter03RouteGuideRootName;
            routeGuide.routePoints = routeMarkers;
            routeGuide.showGuideOnStart = true;
            routeGuide.useResolvedRouteFallback = false;
            routeGuide.smoothControlPoints = true;
            routeGuide.reachedRadius = Mathf.Max(1.5f, chapter03RouteGuideReachedRadius);
            routeGuide.maxDecorationMarkers = Mathf.Max(1, chapter03RouteGuideMaxDecorations);
            routeGuide.RebuildGuide();
        }

        private Transform[] ResolveChapter03RouteMarkers(Vector3 startPosition, Vector3 targetPosition, Transform fallbackParent)
        {
            Transform authoredRoot = FindChapter03RouteRoot();
            if (authoredRoot != null)
            {
                Transform[] authoredMarkers = new Transform[authoredRoot.childCount];
                for (int index = 0; index < authoredRoot.childCount; index++)
                {
                    authoredMarkers[index] = authoredRoot.GetChild(index);
                }

                Array.Sort(authoredMarkers, (left, right) => string.CompareOrdinal(left.name, right.name));
                if (authoredMarkers.Length > 0)
                {
                    return authoredMarkers;
                }
            }

            return CreateChapter03FallbackRouteMarkers(startPosition, targetPosition, fallbackParent);
        }

        private Transform[] CreateChapter03FallbackRouteMarkers(Vector3 startPosition, Vector3 targetPosition, Transform fallbackParent)
        {
            int pointCount = Mathf.Max(5, chapter03RouteGuideAutoPointCount);
            Transform[] markers = new Transform[pointCount];
            Vector3 flatDelta = targetPosition - startPosition;
            flatDelta.y = 0f;

            Vector3 forward = flatDelta.sqrMagnitude > 0.01f
                ? flatDelta.normalized
                : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            if (side.sqrMagnitude < 0.001f)
            {
                side = Vector3.right;
            }
            side.Normalize();

            float routeLength = flatDelta.magnitude;
            float bendOffset = Mathf.Clamp(routeLength * 0.16f, 2.4f, 10f);
            float[] bendSigns = { 1f, 1f, 0.55f, -0.35f, -0.65f };

            for (int index = 0; index < pointCount; index++)
            {
                float t = (index + 1f) / (pointCount + 1f);
                float centeredT = (t - 0.5f) * 2f;
                float offsetStrength = 0.55f + (1f - Mathf.Abs(centeredT)) * 0.45f;
                float sign = bendSigns[Mathf.Min(index, bendSigns.Length - 1)];
                Vector3 markerPosition = Vector3.Lerp(startPosition, targetPosition, t) + side * sign * bendOffset * offsetStrength;
                markers[index] = CreateRouteGuideMarker("AutoRoutePoint_" + index.ToString("00"), fallbackParent, markerPosition);
            }

            return markers;
        }

        private Transform FindChapter03RouteRoot()
        {
            if (string.IsNullOrWhiteSpace(chapter03RouteGuideRootName))
            {
                return null;
            }

            Transform localRoot = transform.Find(chapter03RouteGuideRootName);
            if (localRoot != null)
            {
                return localRoot;
            }

            GameObject rootObject = GameObject.Find(chapter03RouteGuideRootName);
            return rootObject != null ? rootObject.transform : null;
        }

        private global::South ResolveChapter03SouthTarget()
        {
            if (chapter03SouthTarget != null)
            {
                return chapter03SouthTarget;
            }

            chapter03SouthTarget = FindObjectOfType<global::South>();
            return chapter03SouthTarget;
        }

        private Vector3 ResolveChapter03GuideStartPosition()
        {
            Transform playerTransform = null;
            if (manager != null)
            {
                if (manager.playerViewModeController != null)
                {
                    playerTransform = manager.playerViewModeController.transform;
                }
                else if (manager.playerController != null)
                {
                    playerTransform = manager.playerController.transform;
                }
            }

            if (playerTransform != null)
            {
                return playerTransform.position;
            }

            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                return triggerCollider.bounds.center;
            }

            return transform.position;
        }

        private static Transform CreateRouteGuideMarker(string markerName, Transform parent, Vector3 position)
        {
            GameObject markerObject = new GameObject(markerName);
            Transform marker = markerObject.transform;
            marker.SetParent(parent, false);
            marker.position = position;
            return marker;
        }

        private void DestroyChapter03RouteGuide()
        {
            if (_chapter03RouteGuideRoot == null)
            {
                Transform existingRoot = transform.Find(string.IsNullOrWhiteSpace(chapter03RouteGuideObjectName)
                    ? "Chapter02ToChapter03RouteGuide"
                    : chapter03RouteGuideObjectName);
                if (existingRoot != null)
                {
                    _chapter03RouteGuideRoot = existingRoot;
                }
            }

            if (_chapter03RouteGuideRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_chapter03RouteGuideRoot.gameObject);
            }
            else
            {
                DestroyImmediate(_chapter03RouteGuideRoot.gameObject);
            }

            _chapter03RouteGuideRoot = null;
        }

        private void UpdateObjective()
        {
            if (manager == null)
            {
                return;
            }

            if (CurrentState == Chapter02State.Completed)
            {
                manager.RefreshGlobalObjective();
                return;
            }

            if (CurrentState == Chapter02State.InProgress && _session != null)
            {
                manager.SetChapter02Objective(string.Format(progressFormat, _session.AnsweredCorrectCount, _session.TotalQuestionCount) + " - " + objectiveAnswerQuestions);
                return;
            }

            if (!ShouldShowReachTriggerObjective(manager.CurrentSaveData))
            {
                manager.RefreshGlobalObjective();
                return;
            }

            manager.RefreshGlobalObjective();
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
                    questionText = "小飞虹在拙政园中部水院中，最典型的建筑类型是什么？",
                    options = new[] { "跨水而设的廊桥", "临水伸出的水榭", "叠石山上的方亭", "分隔庭院的漏窗墙" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。小飞虹是拙政园中部水面上的廊桥，兼具通行、遮蔽和临水观景功能。",
                    wrongFeedback = "再想想，它不是单独的亭榭，而是把“桥”和“廊”结合在水面上的建筑。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-02",
                    questionText = "“小飞虹”之名与当前水面场景最直接的审美联系是什么？",
                    options = new[] { "桥影入水，形成倒影如虹的画面", "桥面铺色鲜艳，所以像彩虹", "屋顶高度很高，形似拱门", "栏杆数量多，形成重复纹样" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。小飞虹的美感来自桥身、廊影与水面倒影相合，形成倒影如虹的诗意。",
                    wrongFeedback = "提示：题名里的“虹”更多来自桥与水中倒影的关系，而不是单纯颜色。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-03",
                    questionText = "小飞虹为什么能体现江南水乡园林的建筑特色？",
                    options = new[] { "桥、廊、水面和倒影共同构成可行可看的空间", "主要依靠高墙形成封闭防御感", "以大体量殿堂突出礼仪等级", "把水面作为不可接近的背景" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。它把江南水乡的水、桥、廊、影结合起来，让通行和赏景同时发生。",
                    wrongFeedback = "提示：江南水乡园林的重点常在水面与建筑之间的互相成景。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-04",
                    questionText = "从游线安排看，小飞虹在拙政园中部空间里的作用更接近哪一种？",
                    options = new[] { "连接水岸并引导游人边走边转换视角", "把中部水面完全切断，形成封闭院落", "强化正殿前的礼仪轴线", "只作为远处背景，不参与行走路径" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。小飞虹既组织游线，也让玩家在经过时不断获得新的水面与建筑视角。",
                    wrongFeedback = "提示：它不是只摆在那里被观看，也参与了玩家如何穿行这个场景。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-05",
                    questionText = "小飞虹的“廊桥”结构，比普通石桥多出了哪一层建筑体验？",
                    options = new[] { "人在桥上可被廊屋包裹，同时透过开敞处看水景", "桥面更高，可以直接俯瞰城墙", "桥身封闭，不再与外部景色交流", "主要用于陈列家具和书画" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。廊桥让人既在桥上通过，又在廊下停留、避雨、观水，空间体验更丰富。",
                    wrongFeedback = "提示：普通桥重在跨越，廊桥还增加了停留和观看的层次。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-06",
                    questionText = "小飞虹放在拙政园这样的历史园林中，最能说明哪种造园观念？",
                    options = new[] { "小尺度建筑也能承载诗意、游线和水景组织", "园林价值主要来自建筑越高越宏大", "桥梁只解决交通，不参与文化表达", "水面应尽量与建筑分离" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。历史园林的底蕴常在小尺度处显现，小飞虹就是以桥廊组织诗意空间的例子。",
                    wrongFeedback = "提示：江南园林并不靠宏大取胜，而靠小中见大、步移景换。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-07",
                    questionText = "站在小飞虹附近看水面时，桥、水、影共同形成了哪种文化意味？",
                    options = new[] { "把实在建筑转化成带有诗画感的水上意象", "突出桥梁工程的防洪功能", "强调建筑材料的厚重坚固", "让水面成为完全空白的分隔带" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。小飞虹借水成景，桥影入水后让真实建筑带上诗画气质。",
                    wrongFeedback = "提示：这里的水不是背景板，而是让桥产生文化意象的重要部分。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-08",
                    questionText = "如果把小飞虹作为第二章场景的文化考点，最核心的不应只是记住名字，而是什么？",
                    options = new[] { "理解它如何以廊桥、水面、倒影和游线构成园林体验", "知道它旁边所有植物的数量", "判断桥面是否足够宽阔", "比较它和现代公路桥的承重能力" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。这个场景的重点，是小飞虹如何把建筑特色、江南水乡气质和游线体验结合起来。",
                    wrongFeedback = "提示：这道题考的是场景的文化和建筑特色，不是单纯背景点名称。"
                }
            };
        }
    }
}
