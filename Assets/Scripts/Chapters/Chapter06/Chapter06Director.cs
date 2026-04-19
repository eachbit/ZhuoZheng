using System;
using System.Collections;
using UnityEngine;

namespace ZhuozhengYuan
{
    public enum Chapter06TriggerRole
    {
        QuizStart = 0,
        FinaleView = 1
    }

    [RequireComponent(typeof(Collider))]
    public class Chapter06Director : MonoBehaviour
    {
        public GardenGameManager manager;
        public Chapter06TriggerRole triggerRole = Chapter06TriggerRole.QuizStart;
        public Chapter02Question[] questionBank;
        public int questionsRequiredToUnlock = 6;
        public string chapterTitle = "第六章：雪香云蔚亭";
        public string objectiveReachTrigger = "前往雪香云蔚亭区域，触发最后的答题挑战。";
        public string objectiveAnswerQuestions = "答对 6 道与雪香云蔚亭有关的题目，完成最后的园林知识考验。";
        public string objectiveFinalView = "雪香云蔚已明，请登亭回望来路。";
        public string objectiveCompleted = "游历完成。拙政园记忆已合卷。";
        public string quizStartedToast = "你已进入雪香云蔚亭答题环节。";
        public string quizCompletedToast = "六题全部答对，请登亭回望来路。";
        public string finaleTitle = "游历完成";
        public string finaleMessage = "一亭纳雪，一园藏心。你走过水路、廊桥与山亭，所答的不只是景名，而是园林把自然、诗意与人的脚步织在一起的方式。";
        public string progressFormat = "答题进度 {0}/{1}";
        public bool playFinaleSequenceAfterQuiz = true;
        public bool playFinaleSequenceOnFinaleTrigger = true;
        public float finaleWhiteFadeDuration = 2.6f;
        public float finaleMusicDuration = 20f;
        [Range(0f, 1f)]
        public float finaleMusicVolume = 0.18f;
        public int finaleMusicSampleRate = 22050;
        public bool startWhenPlayerEntersTrigger = true;
        public bool disableTriggerAfterCompletion = true;
        public GameObject[] blockersToDisableAfterQuiz;
        public Collider[] blockerCollidersToDisableAfterQuiz;
        public GameObject[] blockersToDisableAfterFinale;
        public Collider[] blockerCollidersToDisableAfterFinale;

        public Chapter06State CurrentState { get; private set; }

        private Chapter02QuizSession _session;
        private Coroutine _reopenQuizCoroutine;
        private Coroutine _finaleSequenceCoroutine;
        private AudioSource _finaleMusicSource;
        private AudioClip _finaleRuntimeMusicClip;

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

            CurrentState = saveData.chapter06State;
            _session = null;

            if (CurrentState == Chapter06State.Completed)
            {
                ApplyCompletionState();
                UpdateObjective();
                return;
            }

            if (CurrentState == Chapter06State.AwaitingFinalView)
            {
                SetQuizBlockersUnlocked(true);
                UpdateObjective();
                return;
            }

            if (CurrentState == Chapter06State.InProgress)
            {
                _session = Chapter02QuizSession.Restore(questionBank, saveData.chapter06QuestionOrder, saveData.chapter06AnsweredCorrectCount);
                if (_session == null)
                {
                    CurrentState = Chapter06State.NotStarted;
                    saveData.chapter06State = Chapter06State.NotStarted;
                    saveData.chapter06AnsweredCorrectCount = 0;
                    saveData.chapter06QuestionOrder = Array.Empty<string>();
                }
            }

            SetQuizBlockersUnlocked(false);
            SetFinaleBlockersUnlocked(false);
            UpdateObjective();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!startWhenPlayerEntersTrigger || !IsPlayerCollider(other))
            {
                return;
            }

            if (triggerRole == Chapter06TriggerRole.FinaleView)
            {
                CompleteFinaleIfReady();
                return;
            }

            if (CurrentState == Chapter06State.NotStarted || CurrentState == Chapter06State.InProgress)
            {
                StartQuizIfNeeded();
            }
        }

        public void StartQuizIfNeeded()
        {
            if (manager == null
                || CurrentState == Chapter06State.AwaitingFinalView
                || CurrentState == Chapter06State.Completed)
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

            CurrentState = Chapter06State.InProgress;
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
            if (_session == null || CurrentState != Chapter06State.InProgress)
            {
                return;
            }

            Chapter02QuizSession.AnswerResult result = _session.SubmitAnswer(selectedOptionIndex);
            string feedback = result.isCorrect ? result.question.correctFeedback : result.question.wrongFeedback;
            if (string.IsNullOrWhiteSpace(feedback))
            {
                feedback = result.isCorrect ? "回答正确。" : "还不对，再试一次。";
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
            CurrentState = Chapter06State.AwaitingFinalView;
            SetQuizBlockersUnlocked(true);

            if (disableTriggerAfterCompletion && triggerRole == Chapter06TriggerRole.QuizStart)
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
            }

            WriteBackSaveState();
            if (manager != null)
            {
                manager.SaveProgress();
            }

            UpdateObjective();

            if (playFinaleSequenceAfterQuiz)
            {
                StartFinaleSequenceIfNeeded();
                return;
            }

            if (manager != null)
            {
                manager.ShowToast(quizCompletedToast, 3f);
            }
        }

        public void CompleteFinaleIfReady()
        {
            if (manager == null || manager.CurrentSaveData == null)
            {
                return;
            }

            if (CurrentState != Chapter06State.AwaitingFinalView
                && manager.CurrentSaveData.chapter06State != Chapter06State.AwaitingFinalView)
            {
                UpdateObjective();
                return;
            }

            if (playFinaleSequenceOnFinaleTrigger)
            {
                StartFinaleSequenceIfNeeded();
                return;
            }

            if (!TryCompleteFinale(manager.CurrentSaveData))
            {
                UpdateObjective();
                return;
            }

            CurrentState = Chapter06State.Completed;
            SetQuizBlockersUnlocked(true);
            SetFinaleBlockersUnlocked(true);

            if (disableTriggerAfterCompletion && triggerRole == Chapter06TriggerRole.FinaleView)
            {
                Collider triggerCollider = GetComponent<Collider>();
                if (triggerCollider != null)
                {
                    triggerCollider.enabled = false;
                }
            }

            manager.HideChapter02Quiz();
            manager.ShowToast(finaleTitle + "\n" + finaleMessage, 6f);
            manager.SaveProgress();
            UpdateObjective();
        }

        private void StartFinaleSequenceIfNeeded()
        {
            if (_finaleSequenceCoroutine != null || manager == null || manager.CurrentSaveData == null)
            {
                return;
            }

            manager.CurrentSaveData.chapter06State = Chapter06State.AwaitingFinalView;
            CurrentState = Chapter06State.AwaitingFinalView;

            if (!TryCompleteFinale(manager.CurrentSaveData))
            {
                UpdateObjective();
                return;
            }

            CurrentState = Chapter06State.Completed;
            SetQuizBlockersUnlocked(true);
            SetFinaleBlockersUnlocked(true);
            WriteBackSaveState();
            manager.SaveProgress();
            UpdateObjective();

            if (disableTriggerAfterCompletion)
            {
                Collider triggerCollider = GetComponent<Collider>();
                if (triggerCollider != null)
                {
                    triggerCollider.enabled = false;
                }
            }

            _finaleSequenceCoroutine = StartCoroutine(PlayFinaleSequenceRoutine());
        }

        private IEnumerator PlayFinaleSequenceRoutine()
        {
            PlayFinaleMusic();
            manager.HideChapter02Quiz();
            manager.SetFadeColor(Color.white);
            manager.SetFadeAlpha(0f);

            bool dialogueCompleted = false;
            manager.ShowDialogue(CreateFinaleDialogueLines(), () => dialogueCompleted = true);
            while (!dialogueCompleted)
            {
                yield return null;
            }

            float duration = Mathf.Max(0.05f, finaleWhiteFadeDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                manager.SetFadeAlpha(Mathf.SmoothStep(0f, 1f, normalized));
                yield return null;
            }

            manager.SetFadeAlpha(1f);
            manager.ShowFinaleHistory(CreateFinaleHistoryTitle(), CreateFinaleHistoryBody());
            _finaleSequenceCoroutine = null;
        }

        private void PlayFinaleMusic()
        {
            if (finaleMusicVolume <= 0.001f)
            {
                return;
            }

            if (_finaleMusicSource == null)
            {
                _finaleMusicSource = GetComponent<AudioSource>();
                if (_finaleMusicSource == null)
                {
                    _finaleMusicSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (_finaleRuntimeMusicClip == null)
            {
                _finaleRuntimeMusicClip = CreateFinaleMusicClip(Mathf.Max(8000, finaleMusicSampleRate), Mathf.Max(6f, finaleMusicDuration));
            }

            _finaleMusicSource.playOnAwake = false;
            _finaleMusicSource.loop = true;
            _finaleMusicSource.spatialBlend = 0f;
            _finaleMusicSource.volume = Mathf.Clamp01(finaleMusicVolume);
            _finaleMusicSource.clip = _finaleRuntimeMusicClip;
            _finaleMusicSource.Play();
        }

        private void ApplyCompletionState()
        {
            _session = null;
            SetQuizBlockersUnlocked(true);
            SetFinaleBlockersUnlocked(true);

            if (disableTriggerAfterCompletion)
            {
                Collider triggerCollider = GetComponent<Collider>();
                if (triggerCollider != null)
                {
                    triggerCollider.enabled = false;
                }
            }
        }

        private void SetQuizBlockersUnlocked(bool unlocked)
        {
            SetObjectsUnlocked(blockersToDisableAfterQuiz, blockerCollidersToDisableAfterQuiz, unlocked);
        }

        private void SetFinaleBlockersUnlocked(bool unlocked)
        {
            SetObjectsUnlocked(blockersToDisableAfterFinale, blockerCollidersToDisableAfterFinale, unlocked);
        }

        private static void SetObjectsUnlocked(GameObject[] objectsToDisable, Collider[] collidersToDisable, bool unlocked)
        {
            if (objectsToDisable != null)
            {
                for (int index = 0; index < objectsToDisable.Length; index++)
                {
                    if (objectsToDisable[index] != null)
                    {
                        objectsToDisable[index].SetActive(!unlocked);
                    }
                }
            }

            if (collidersToDisable != null)
            {
                for (int index = 0; index < collidersToDisable.Length; index++)
                {
                    if (collidersToDisable[index] != null)
                    {
                        collidersToDisable[index].enabled = !unlocked;
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

            if (CurrentState == Chapter06State.Completed)
            {
                manager.SetChapter06Objective(objectiveCompleted);
                return;
            }

            if (CurrentState == Chapter06State.AwaitingFinalView)
            {
                manager.SetChapter06Objective(objectiveFinalView);
                return;
            }

            if (CurrentState == Chapter06State.InProgress && _session != null)
            {
                manager.SetChapter06Objective(string.Format(progressFormat, _session.AnsweredCorrectCount, _session.TotalQuestionCount) + " - " + objectiveAnswerQuestions);
                return;
            }

            manager.SetChapter06Objective(objectiveReachTrigger);
        }

        private static bool ShouldShowFinaleObjective(SaveData saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            return saveData.chapter06State == Chapter06State.AwaitingFinalView && !saveData.chapter06FinaleViewed;
        }

        private static bool TryCompleteFinale(SaveData saveData)
        {
            if (saveData == null
                || saveData.chapter06FinaleViewed
                || saveData.chapter06State != Chapter06State.AwaitingFinalView)
            {
                return false;
            }

            saveData.chapter06FinaleViewed = true;
            saveData.chapter06State = Chapter06State.Completed;
            saveData.projectCompleted = true;
            return true;
        }

        private static DialogueLine[] CreateFinaleDialogueLines()
        {
            return new[]
            {
                new DialogueLine
                {
                    speaker = "系统提示",
                    text = "水动、影合、声传、雨观、山续。"
                },
                new DialogueLine
                {
                    speaker = "系统提示",
                    text = "子之所为，非修园也。"
                },
                new DialogueLine
                {
                    speaker = "系统提示",
                    text = "乃以己心，印古人之心。"
                }
            };
        }

        private static AudioClip CreateFinaleMusicClip(int sampleRate, float durationSeconds)
        {
            int safeSampleRate = Mathf.Max(8000, sampleRate);
            int sampleCount = Mathf.Max(safeSampleRate, Mathf.CeilToInt(safeSampleRate * Mathf.Max(1f, durationSeconds)));
            float[] samples = new float[sampleCount];
            int[] pentatonicSemitones = { 0, 2, 5, 7, 9, 12, 9, 7, 5, 2 };
            float rootFrequency = 196f;
            float beatSeconds = 1.65f;

            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)safeSampleRate;
                int noteIndex = Mathf.FloorToInt(time / beatSeconds) % pentatonicSemitones.Length;
                float noteTime = time - (Mathf.Floor(time / beatSeconds) * beatSeconds);
                float noteFrequency = rootFrequency * Mathf.Pow(2f, pentatonicSemitones[noteIndex] / 12f);
                float attack = Mathf.Clamp01(noteTime / 0.08f);
                float decay = Mathf.Exp(-1.75f * noteTime);
                float envelope = attack * decay;

                float pluck = Mathf.Sin(2f * Mathf.PI * noteFrequency * time) * 0.32f;
                pluck += Mathf.Sin(2f * Mathf.PI * noteFrequency * 2f * time) * 0.08f;
                pluck += Mathf.Sin(2f * Mathf.PI * noteFrequency * 3f * time) * 0.035f;

                float drone = Mathf.Sin(2f * Mathf.PI * rootFrequency * 0.5f * time) * 0.08f;
                drone += Mathf.Sin(2f * Mathf.PI * rootFrequency * time) * 0.025f;

                float breath = Mathf.Sin(2f * Mathf.PI * 0.08f * time) * 0.025f;
                samples[index] = Mathf.Clamp((pluck * envelope) + drone + breath, -0.85f, 0.85f) * 0.42f;
            }

            AudioClip clip = AudioClip.Create("Chapter06_FinalElegantBgm_Runtime", sampleCount, 1, safeSampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static string CreateFinaleHistoryTitle()
        {
            return "拙政园";
        }

        private static string CreateFinaleHistoryBody()
        {
            return "拙政园始建于明正德四年（1509），为御史王献臣退隐苏州后，在大弘寺旧址一带营建的私家园林。园名取意于西晋潘岳《闲居赋》：“灌园鬻蔬，以供朝夕之膳，此亦拙者之为政也。”\n\n数百年间，拙政园几经兴废、分合与重修，至清末逐渐形成东、中、西三部并置的格局。它以水为脉，以山石、亭榭、廊桥与花木组织游线，是江南文人园林“虽由人作，宛自天开”的代表。\n\n1961年，拙政园被列为第一批全国重点文物保护单位；1997年，作为苏州古典园林的重要组成部分列入《世界文化遗产名录》。今日一游至此落幕，园中山水仍把古人的心事，交还给后来人的脚步。";
        }

        private void WriteBackSaveState()
        {
            if (manager == null || manager.CurrentSaveData == null)
            {
                return;
            }

            manager.CurrentSaveData.chapter06State = CurrentState;
            manager.CurrentSaveData.chapter06AnsweredCorrectCount = _session != null ? _session.AnsweredCorrectCount : 0;
            manager.CurrentSaveData.chapter06QuestionOrder = _session != null ? _session.GetOrderedQuestionIds() : Array.Empty<string>();
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
            if (questionBank != null && questionBank.Length >= 6)
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
                    questionId = "xuexiang-yunwei-01",
                    questionText = "雪香云蔚亭主要位于拙政园的哪一部分？",
                    options = new[] { "东花园", "中部花园", "西花园", "住宅区" },
                    correctOptionIndex = 1,
                    correctFeedback = "回答正确。雪香云蔚亭位于拙政园中部花园，是中部赏景的重要节点。",
                    wrongFeedback = "再想想，雪香云蔚亭处在拙政园最精华、以水面和山岛组织空间的那一部分。"
                },
                new Chapter02Question
                {
                    questionId = "xuexiang-yunwei-02",
                    questionText = "“雪香”在亭名中主要指向哪一种植物意象？",
                    options = new[] { "梅花", "荷花", "枇杷", "竹子" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。“雪香”点出梅花如雪、暗香浮动的意象。",
                    wrongFeedback = "提示：这里的香气与冬春赏花有关，花色常被联想到雪。"
                },
                new Chapter02Question
                {
                    questionId = "xuexiang-yunwei-03",
                    questionText = "“云蔚”最贴近下列哪一种景象？",
                    options = new[] { "水面结冰", "花木繁盛，如云蒸腾", "亭子临街", "屋檐积雨" },
                    correctOptionIndex = 1,
                    correctFeedback = "回答正确。“云蔚”写出花木繁盛、云气蒸腾般的景象。",
                    wrongFeedback = "提示：这个词不是说天气，而是在写花木盛放时的蓬勃气象。"
                },
                new Chapter02Question
                {
                    questionId = "xuexiang-yunwei-04",
                    questionText = "雪香云蔚亭又常被理解为哪一个季节的赏景亭？",
                    options = new[] { "春亭", "夏亭", "秋亭", "冬亭" },
                    correctOptionIndex = 3,
                    correctFeedback = "回答正确。雪香云蔚亭常与冬日赏梅、雪意清香联系在一起。",
                    wrongFeedback = "提示：亭名里的“雪”和梅香，指向的是最清寒的一季。"
                },
                new Chapter02Question
                {
                    questionId = "xuexiang-yunwei-05",
                    questionText = "从空间关系看，雪香云蔚亭所在的山岛与哪座亭常被并列观看？",
                    options = new[] { "待霜亭", "天泉亭", "塔影亭", "沧浪亭" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。中部水池山岛上，雪香云蔚亭与待霜亭形成呼应。",
                    wrongFeedback = "提示：另一座亭也在中部山岛体系中，名字带有秋意。"
                },
                new Chapter02Question
                {
                    questionId = "xuexiang-yunwei-06",
                    questionText = "玩家最后“登亭回望”最能呼应雪香云蔚亭的哪种游园价值？",
                    options = new[] { "储物", "登高观景、回望全园", "售票检票", "封闭道路" },
                    correctOptionIndex = 1,
                    correctFeedback = "回答正确。登亭回望把知识转化成游园动作，让结尾自然落在观景体验上。",
                    wrongFeedback = "提示：亭不仅是建筑名称，也是让人停步、观看、回味的地点。"
                },
                new Chapter02Question
                {
                    questionId = "xuexiang-yunwei-07",
                    questionText = "雪香云蔚亭周边常以哪类植物来强化题名意境？",
                    options = new[] { "梅树", "棕榈", "仙人掌", "芭蕉" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。亭旁植梅，能强化梅花似雪、暗香浮动的意境。",
                    wrongFeedback = "提示：题名中的“雪香”已经藏着答案。"
                },
                new Chapter02Question
                {
                    questionId = "xuexiang-yunwei-08",
                    questionText = "雪香云蔚亭的形制描述，哪一项更贴近常见介绍？",
                    options = new[] { "质朴轻快的方亭或矩形亭", "高层佛塔", "水上戏台", "城门箭楼" },
                    correctOptionIndex = 0,
                    correctFeedback = "回答正确。常见介绍会强调它质朴轻快、立于山岛上的亭式建筑。",
                    wrongFeedback = "提示：它是园林中的亭，不是塔、戏台或城门。"
                }
            };
        }
    }
}
