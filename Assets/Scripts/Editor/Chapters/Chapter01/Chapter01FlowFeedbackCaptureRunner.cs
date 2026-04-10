#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZhuozhengYuan.EditorTools
{
    [InitializeOnLoad]
    public static class Chapter01FlowFeedbackCaptureRunner
    {
        private const string MenuPath = "Tools/Zhuozhengyuan/Run Chapter01 Flow Feedback Capture";
        private const string ScenePath = "Assets/Scenes/Garden_Main.unity";

        private const string ActiveKey = "ZhuozhengYuan.FlowCapture.Active";
        private const string StepKey = "ZhuozhengYuan.FlowCapture.Step";
        private const string NextActionTimeKey = "ZhuozhengYuan.FlowCapture.NextActionTime";
        private const string TriggerKey = "ZhuozhengYuan.FlowCapture.Pending";

        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static readonly string TriggerFilePath = Path.Combine(ProjectRoot, "Temp", "CodexChapter01FlowFeedbackCapture.trigger");
        private static readonly string OutputDirectory = Path.Combine(ProjectRoot, "Artifacts", "Chapter01FlowFeedbackCapture");
        private static readonly string SummaryPath = Path.Combine(OutputDirectory, "summary.txt");
        private static Camera _showcaseCamera;
        private static Camera _disabledMainCamera;

        private enum CaptureStep
        {
            Idle = 0,
            PrepareScene = 1,
            WaitForPlayMode = 2,
            WaitForManager = 3,
            CaptureStartState = 4,
            PrepareFlowState = 5,
            OpenUiForWest = 6,
            CaptureUi = 7,
            ChooseWest = 8,
            CaptureWest = 9,
            OpenUiForSouth = 10,
            ChooseSouth = 11,
            CaptureSouth = 12,
            OpenUiForCenter = 13,
            ChooseCenter = 14,
            CaptureCenter = 15,
            PrepareCenterShowcase = 16,
            CaptureCenterShowcase = 17,
            ExitPlayMode = 18,
            Completed = 19
        }

        static Chapter01FlowFeedbackCaptureRunner()
        {
            Debug.Log("Chapter01FlowFeedbackCaptureRunner initialized.");
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem(MenuPath)]
        public static void RunCaptureFromMenu()
        {
            PrepareOutputDirectory();
            File.WriteAllText(SummaryPath, "Chapter01 flow feedback capture started at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine);
            SessionState.SetBool(TriggerKey, true);
            WriteTriggerFile();
            TryStartTriggeredRun();
        }

        private static void OnEditorUpdate()
        {
            TryStartTriggeredRun();
            if (!SessionState.GetBool(ActiveKey, false))
            {
                return;
            }

            CaptureStep step = (CaptureStep)SessionState.GetInt(StepKey, (int)CaptureStep.Idle);
            if (step == CaptureStep.Idle || step == CaptureStep.Completed)
            {
                return;
            }

            if (!IsTimeReady())
            {
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                HandleEditModeStep(step);
                return;
            }

            HandlePlayModeStep(step);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SetStep(CaptureStep.WaitForManager, 0.35d);
                AppendSummary("Entered Play Mode.");
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                CaptureStep currentStep = (CaptureStep)SessionState.GetInt(StepKey, (int)CaptureStep.Idle);
                if (currentStep == CaptureStep.ExitPlayMode)
                {
                    SetStep(CaptureStep.Completed, 0d);
                    SessionState.SetBool(ActiveKey, false);
                    AppendSummary("Capture completed.");
                    AssetDatabase.Refresh();
                }
            }
        }

        private static void TryStartTriggeredRun()
        {
            bool hasSessionTrigger = SessionState.GetBool(TriggerKey, false);
            bool hasFileTrigger = File.Exists(TriggerFilePath);
            if ((!hasSessionTrigger && !hasFileTrigger) || SessionState.GetBool(ActiveKey, false))
            {
                return;
            }

            SessionState.SetBool(TriggerKey, false);
            DeleteTriggerFileIfExists();
            SessionState.SetBool(ActiveKey, true);
            SetStep(CaptureStep.PrepareScene, 0d);
            Debug.Log("Chapter01FlowFeedbackCaptureRunner detected trigger and is starting.");
            AppendSummary("Preparing scene for capture.");
        }

        private static void HandleEditModeStep(CaptureStep step)
        {
            switch (step)
            {
                case CaptureStep.PrepareScene:
                    PrepareSceneForCapture();
                    break;
                case CaptureStep.WaitForPlayMode:
                    break;
                case CaptureStep.ExitPlayMode:
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        return;
                    }

                    SessionState.SetBool(ActiveKey, false);
                    SetStep(CaptureStep.Completed, 0d);
                    AppendSummary("Capture exited without entering Play Mode.");
                    break;
            }
        }

        private static void HandlePlayModeStep(CaptureStep step)
        {
            GardenGameManager manager = GardenGameManager.Instance;
            if (manager == null || manager.chapter01Director == null || manager.runtimeUI == null)
            {
                if (step == CaptureStep.WaitForManager)
                {
                    SetStep(CaptureStep.WaitForManager, 0.25d);
                }

                return;
            }

            switch (step)
            {
                case CaptureStep.WaitForManager:
                    PrepareStartState(manager);
                    PositionStartCaptureCamera();
                    SetStep(CaptureStep.CaptureStartState, 0.3d);
                    AppendSummary("Prepared runtime start state.");
                    break;
                case CaptureStep.CaptureStartState:
                    CaptureScreenshot("00_start_hidden_objective.png");
                    SetStep(CaptureStep.PrepareFlowState, 0.25d);
                    break;
                case CaptureStep.PrepareFlowState:
                    PrepareRuntimeState(manager);
                    PositionCaptureCamera(manager, 0.6f);
                    SetStep(CaptureStep.OpenUiForWest, 0.25d);
                    AppendSummary("Prepared runtime state for flow selection.");
                    break;
                case CaptureStep.OpenUiForWest:
                    manager.chapter01Director.HandleFlowSelectorInteraction();
                    PositionCaptureCamera(manager, 0.6f);
                    SetStep(CaptureStep.CaptureUi, 0.3d);
                    break;
                case CaptureStep.CaptureUi:
                    CaptureScreenshot("01_direction_ui.png");
                    SetStep(CaptureStep.ChooseWest, 0.3d);
                    break;
                case CaptureStep.ChooseWest:
                    ChooseDirection(manager.runtimeUI, 0);
                    PositionCaptureCamera(manager, 0.44f);
                    SetStep(CaptureStep.CaptureWest, 0.46d);
                    break;
                case CaptureStep.CaptureWest:
                    CaptureScreenshot("02_west_result.png");
                    SetStep(CaptureStep.OpenUiForSouth, 0.35d);
                    break;
                case CaptureStep.OpenUiForSouth:
                    manager.chapter01Director.HandleFlowSelectorInteraction();
                    SetStep(CaptureStep.ChooseSouth, 0.25d);
                    break;
                case CaptureStep.ChooseSouth:
                    ChooseDirection(manager.runtimeUI, 1);
                    PositionCaptureCamera(manager, 0.54f);
                    SetStep(CaptureStep.CaptureSouth, 0.5d);
                    break;
                case CaptureStep.CaptureSouth:
                    CaptureScreenshot("03_south_result.png");
                    SetStep(CaptureStep.OpenUiForCenter, 0.35d);
                    break;
                case CaptureStep.OpenUiForCenter:
                    manager.chapter01Director.HandleFlowSelectorInteraction();
                    SetStep(CaptureStep.ChooseCenter, 0.25d);
                    break;
                case CaptureStep.ChooseCenter:
                    ChooseDirection(manager.runtimeUI, 2);
                    PositionCaptureCamera(manager, 0.68f);
                    SetStep(CaptureStep.CaptureCenter, 0.56d);
                    break;
                case CaptureStep.CaptureCenter:
                    CaptureScreenshot("04_center_result.png");
                    SetStep(CaptureStep.PrepareCenterShowcase, 0.1d);
                    break;
                case CaptureStep.PrepareCenterShowcase:
                    if (manager.runtimeUI != null)
                    {
                        manager.runtimeUI.enabled = false;
                    }

                    PositionShowcaseCamera(manager);
                    SetStep(CaptureStep.CaptureCenterShowcase, 0.12d);
                    break;
                case CaptureStep.CaptureCenterShowcase:
                    CaptureShowcaseCameraScreenshot("05_center_world_effect.png");
                    RestoreMainCameraAfterShowcase();
                    if (manager.runtimeUI != null)
                    {
                        manager.runtimeUI.enabled = true;
                    }

                    SetStep(CaptureStep.ExitPlayMode, 0.45d);
                    break;
                case CaptureStep.ExitPlayMode:
                    EditorApplication.isPlaying = false;
                    break;
            }
        }

        private static void PrepareSceneForCapture()
        {
            PrepareOutputDirectory();

            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.Equals(activeScene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                AppendSummary("Opened Garden_Main scene.");
            }

            SetStep(CaptureStep.WaitForPlayMode, 0d);
            EditorApplication.isPlaying = true;
        }

        private static void PrepareStartState(GardenGameManager manager)
        {
            SaveData saveData = manager.CurrentSaveData ?? SaveData.CreateDefault();
            saveData.introPlayed = true;
            saveData.collectedPages = 0;
            saveData.leftGateOpened = false;
            saveData.rightGateOpened = false;
            saveData.selectedFlowDirection = string.Empty;
            saveData.chapter01RejectedFlowDirections = 0;
            saveData.chapter01PageCollected = false;
            saveData.chapter01State = Chapter01State.NeedOpenGates;

            if (manager.runtimeUI != null)
            {
                manager.runtimeUI.SetPageCount(saveData.collectedPages, manager.totalPages);
            }

            manager.chapter01Director.ApplySaveState(saveData);
            manager.SetDirectionChoiceActive(false);
            manager.SetDialogueActive(false);
        }

        private static void PrepareRuntimeState(GardenGameManager manager)
        {
            SaveData saveData = manager.CurrentSaveData ?? SaveData.CreateDefault();
            saveData.introPlayed = true;
            saveData.collectedPages = 0;
            saveData.leftGateOpened = true;
            saveData.rightGateOpened = true;
            saveData.selectedFlowDirection = string.Empty;
            saveData.chapter01RejectedFlowDirections = 0;
            saveData.chapter01PageCollected = false;
            saveData.chapter01State = Chapter01State.NeedChooseFlow;

            if (manager.runtimeUI != null)
            {
                manager.runtimeUI.SetPageCount(saveData.collectedPages, manager.totalPages);
            }

            manager.chapter01Director.ApplySaveState(saveData);
            manager.SetDirectionChoiceActive(false);
            manager.SetDialogueActive(false);
        }

        private static void PositionStartCaptureCamera()
        {
            Camera targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Object.FindObjectOfType<Camera>();
            }

            if (targetCamera == null)
            {
                return;
            }

            targetCamera.fieldOfView = 60f;
        }

        private static void PositionCaptureCamera(GardenGameManager manager, float lookRatio)
        {
            if (manager == null || manager.chapter01Director == null || manager.chapter01Director.flowSelector == null)
            {
                return;
            }

            Camera targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Object.FindObjectOfType<Camera>();
            }

            if (targetCamera == null)
            {
                return;
            }

            Vector3 source = manager.chapter01Director.flowSelector.transform.position + (Vector3.up * 1.4f);
            Vector3 target = manager.chapter01Director.pagePickup != null
                ? manager.chapter01Director.pagePickup.transform.position + (Vector3.up * 0.9f)
                : source + (Vector3.forward * 5f);

            Vector3 horizontalDirection = target - source;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude < 0.01f)
            {
                horizontalDirection = Vector3.forward;
            }

            horizontalDirection.Normalize();
            Vector3 lateral = Vector3.Cross(Vector3.up, horizontalDirection).normalized;
            Vector3 focusPoint = Vector3.Lerp(source, target, Mathf.Clamp01(lookRatio)) + (Vector3.up * 1.2f);
            Vector3 cameraPosition = source - (horizontalDirection * 1.2f) + (lateral * 6.4f) + (Vector3.up * 6.1f);

            targetCamera.transform.position = cameraPosition;
            targetCamera.transform.rotation = Quaternion.LookRotation((focusPoint - cameraPosition).normalized, Vector3.up);
            targetCamera.fieldOfView = 42f;
        }

        private static void PositionShowcaseCamera(GardenGameManager manager)
        {
            if (manager == null || manager.chapter01Director == null || manager.chapter01Director.flowSelector == null)
            {
                return;
            }

            Camera sourceCamera = Camera.main;
            if (sourceCamera == null)
            {
                sourceCamera = UnityEngine.Object.FindObjectOfType<Camera>();
            }

            if (sourceCamera == null)
            {
                return;
            }

            EnsureShowcaseCamera(sourceCamera);
            if (_showcaseCamera == null)
            {
                return;
            }

            Vector3 source = manager.chapter01Director.flowSelector.transform.position + (Vector3.up * 2.6f);
            Vector3 target = manager.chapter01Director.pagePickup != null
                ? manager.chapter01Director.pagePickup.transform.position + (Vector3.up * 2.8f)
                : source + (Vector3.forward * 7f);

            Vector3 horizontalDirection = target - source;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude < 0.01f)
            {
                horizontalDirection = Vector3.forward;
            }

            horizontalDirection.Normalize();
            Vector3 lateral = Vector3.Cross(Vector3.up, horizontalDirection).normalized;
            Vector3 midPoint = Vector3.Lerp(source, target, 0.52f);
            Vector3 focusPoint = midPoint + (Vector3.up * 1.1f);
            Vector3 cameraPosition = midPoint - (horizontalDirection * 6.2f) - (lateral * 11.5f) + (Vector3.up * 14.8f);

            _showcaseCamera.transform.position = cameraPosition;
            _showcaseCamera.transform.rotation = Quaternion.LookRotation((focusPoint - cameraPosition).normalized, Vector3.up);
            _showcaseCamera.fieldOfView = 30f;
        }

        private static void EnsureShowcaseCamera(Camera sourceCamera)
        {
            if (_showcaseCamera == null)
            {
                GameObject showcaseObject = new GameObject("CodexFlowFeedbackShowcaseCamera");
                _showcaseCamera = showcaseObject.AddComponent<Camera>();
                AudioListener listener = showcaseObject.AddComponent<AudioListener>();
                listener.enabled = false;
            }

            _showcaseCamera.CopyFrom(sourceCamera);
            _showcaseCamera.enabled = true;
            _showcaseCamera.depth = sourceCamera.depth + 10f;

            _disabledMainCamera = sourceCamera;
            _disabledMainCamera.enabled = false;
        }

        private static void RestoreMainCameraAfterShowcase()
        {
            if (_disabledMainCamera != null)
            {
                _disabledMainCamera.enabled = true;
                _disabledMainCamera = null;
            }

            if (_showcaseCamera != null)
            {
                UnityEngine.Object.DestroyImmediate(_showcaseCamera.gameObject);
                _showcaseCamera = null;
            }
        }

        private static void ChooseDirection(PrototypeRuntimeUI runtimeUi, int index)
        {
            if (runtimeUi == null)
            {
                return;
            }

            MethodInfo method = typeof(PrototypeRuntimeUI).GetMethod("ChooseDirectionByIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null)
            {
                method.Invoke(runtimeUi, new object[] { index });
            }
        }

        private static void CaptureScreenshot(string fileName)
        {
            PrepareOutputDirectory();
            string screenshotPath = Path.Combine(OutputDirectory, fileName);
            if (File.Exists(screenshotPath))
            {
                File.Delete(screenshotPath);
            }

            ScreenCapture.CaptureScreenshot(screenshotPath);
            AppendSummary("Requested screenshot: " + screenshotPath);
        }

        private static void CaptureShowcaseCameraScreenshot(string fileName)
        {
            if (_showcaseCamera == null)
            {
                return;
            }

            PrepareOutputDirectory();
            string screenshotPath = Path.Combine(OutputDirectory, fileName);
            if (File.Exists(screenshotPath))
            {
                File.Delete(screenshotPath);
            }

            const int width = 1280;
            const int height = 720;

            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = _showcaseCamera.targetTexture;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                _showcaseCamera.targetTexture = renderTexture;
                _showcaseCamera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(screenshotPath, texture.EncodeToPNG());
                AppendSummary("Rendered showcase screenshot: " + screenshotPath);
            }
            finally
            {
                _showcaseCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void PrepareOutputDirectory()
        {
            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(TriggerFilePath));
        }

        private static void AppendSummary(string message)
        {
            PrepareOutputDirectory();
            File.AppendAllText(SummaryPath, "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
        }

        private static bool IsTimeReady()
        {
            string stored = SessionState.GetString(NextActionTimeKey, string.Empty);
            if (string.IsNullOrEmpty(stored))
            {
                return true;
            }

            double scheduledTime;
            if (!double.TryParse(stored, out scheduledTime))
            {
                return true;
            }

            return EditorApplication.timeSinceStartup >= scheduledTime;
        }

        private static void SetStep(CaptureStep step, double delaySeconds)
        {
            SessionState.SetInt(StepKey, (int)step);
            SessionState.SetString(NextActionTimeKey, (EditorApplication.timeSinceStartup + Math.Max(0d, delaySeconds)).ToString("R"));
        }

        private static void WriteTriggerFile()
        {
            PrepareOutputDirectory();
            File.WriteAllText(TriggerFilePath, DateTime.Now.ToString("O"));
        }

        private static void DeleteTriggerFileIfExists()
        {
            if (File.Exists(TriggerFilePath))
            {
                File.Delete(TriggerFilePath);
            }
        }
    }
}
#endif
