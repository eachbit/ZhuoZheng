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
                    questionText = "小飞虹在拙政园中部水院中，为什么适合用来表现倒影美学？",
                    options = new[] { "廊桥横跨水面，实体建筑和水中倒影能共同成景", "建筑远离水面，主要依靠墙面装饰成景", "桥体完全封闭，不能与水面发生视觉关系", "只靠桥面颜色形成彩虹效果" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。小飞虹是跨水而设的廊桥，桥廊实体和水中倒影一起构成完整的园林画面。",
                    wrongFeedback = "提示：倒影美学的关键，是廊桥与水面发生视觉关系，而不是单独看桥体。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-02",
                    questionText = "平静水面能让小飞虹形成较清晰倒影，主要依靠哪种光学现象？",
                    options = new[] { "水面反射接近镜面反射，能形成桥廊的虚像", "水面吸收全部光线，使桥体消失", "水面主动发光，照亮桥底结构", "水面折射后把桥体变成真实建筑" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。水面越接近平静镜面，越能把桥廊的光线反射到视线中，形成水中的虚像。",
                    wrongFeedback = "提示：倒影不是水面自己发光，而是桥廊光线经水面反射后进入眼睛。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-03",
                    questionText = "“倒影如虹”的美感，主要来自小飞虹和水面形成的哪种视觉关系？",
                    options = new[] { "实体廊桥与水中虚像上下呼应，形成虚实相生的弧形意象", "桥面铺色鲜艳，所以水面一定呈现彩虹颜色", "桥越厚重，倒影就越不会被看见", "只要有栏杆数量重复，就能形成虹影" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。小飞虹的实体和倒影在水面上下互相补足，让廊桥产生虚实相生、倒影如虹的诗意。",
                    wrongFeedback = "提示：这里的“虹”重点在桥和倒影的形态关系，不是简单的彩色装饰。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-04",
                    questionText = "游人沿小飞虹移动时，为什么会感觉倒影和水面画面不断变化？",
                    options = new[] { "观察位置改变后，视线与反射方向变化，倒影的完整度和位置也会变化", "桥体结构会自动移动，所以倒影跟着移动", "水面会固定显示同一张图像，不受视角影响", "廊桥越封闭，倒影变化越明显" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。小飞虹把游线和光学观看结合起来，人的位置一变，看到的水面反射关系也会变化。",
                    wrongFeedback = "提示：倒影画面与观察角度有关，游线会改变你和水面、桥廊之间的视线关系。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-05",
                    questionText = "小飞虹的廊屋、栏杆和开敞处，会怎样增强水面倒影的光影层次？",
                    options = new[] { "明暗构件在水中被反射，使桥影、廊影和亮水面形成对比", "让所有光线完全消失，水面变成纯黑", "只增加桥面承重，与视觉效果无关", "把水面倒影全部遮住，避免出现虚像" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。廊桥的屋面、栏杆和开口会形成明暗变化，倒映到水中后加强光影层次。",
                    wrongFeedback = "提示：小飞虹的倒影不只是轮廓，还包含廊屋和栏杆带来的明暗关系。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-06",
                    questionText = "水面出现细小波纹时，小飞虹倒影会产生怎样的美学变化？",
                    options = new[] { "波纹会扰动镜面反射，使倒影边缘轻微破碎并产生流动感", "波纹会让倒影完全变成真实桥体", "波纹越多，倒影一定越清晰笔直", "波纹只影响声音，不影响光线反射" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。波纹改变局部水面角度，让镜面反射被轻微打散，使小飞虹倒影更有流动的诗意。",
                    wrongFeedback = "提示：水面不是静止画布，波纹会改变光线反射方向，从而改变倒影形态。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-07",
                    questionText = "小飞虹为什么能体现江南水乡园林“桥、水、影”合一的建筑特色？",
                    options = new[] { "廊桥可通行，水面可反射，倒影又把实景转化成诗画意象", "桥梁只解决交通，水面不参与审美", "倒影会削弱建筑存在感，所以应尽量避免", "建筑越远离水面，倒影美学越明显" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。小飞虹把廊桥、江南水乡水面和倒影如虹的虚像组织在一起，让历史园林的小尺度建筑具有诗画感。",
                    wrongFeedback = "提示：水面不是背景板，它通过反射让桥廊产生倒影美学。"
                },
                new Chapter02Question
                {
                    questionId = "xiaofeihong-08",
                    questionText = "观察小飞虹倒影时，最能说明其倒影美学的判断方式是什么？",
                    options = new[] { "同时看廊桥实体、水面反射、虚像位置和游线视角", "只记住桥名，不观察水面关系", "只比较桥面宽度和现代桥梁承重", "只统计旁边植物数量" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。理解小飞虹的倒影美学，要把建筑实体、水面反射形成的虚像，以及游线中的观看角度一起看。",
                    wrongFeedback = "提示：小飞虹的重点不只是名称，而是廊桥如何借水面反射形成虚实相生的园林体验。"
                }
            };
        }
    }
}
