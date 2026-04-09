#if UNITY_EDITOR
using Cinemachine;
using StarterAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;

namespace ZhuozhengYuan.EditorTools
{
    public static class StarterAssetsThirdPersonSetupTool
    {
        private const string InputActionsPath = "Assets/StarterAssets/InputSystem/StarterAssets.inputactions";
        private const string FollowCameraPrefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
        private const string ThirdPersonControllerAssetPath = "Assets/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";

        [MenuItem("Tools/Zhuozhengyuan/Setup Starter Assets Third Person For Current Player")]
        public static void SetupStarterAssetsThirdPersonForCurrentPlayer()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("No active scene", "Open Garden_Main first.", "OK");
                return;
            }

            GameObject playerRoot = GameObject.Find("Player");
            GardenGameManager manager = Object.FindObjectOfType<GardenGameManager>();
            if (playerRoot == null || manager == null)
            {
                EditorUtility.DisplayDialog("Missing player", "Current scene is missing Player or GardenGameManager.", "OK");
                return;
            }

            FirstPersonPlayerController firstPersonController = playerRoot.GetComponent<FirstPersonPlayerController>();
            PlayerInteractor interactor = playerRoot.GetComponent<PlayerInteractor>();
            PlayerViewModeController viewModeController = playerRoot.GetComponent<PlayerViewModeController>();

            if (firstPersonController == null || interactor == null)
            {
                EditorUtility.DisplayDialog("Missing gameplay scripts", "Player is missing FirstPersonPlayerController or PlayerInteractor.", "OK");
                return;
            }

            if (viewModeController == null)
            {
                viewModeController = Undo.AddComponent<PlayerViewModeController>(playerRoot);
            }

            Transform cameraPivot = firstPersonController.cameraPivot;
            Camera gameplayCamera = cameraPivot != null ? cameraPivot.GetComponentInChildren<Camera>(true) : null;
            if (cameraPivot == null || gameplayCamera == null)
            {
                EditorUtility.DisplayDialog("Missing camera", "Player needs a CameraPivot child with a Camera first.", "OK");
                return;
            }

            StarterAssetsInputs starterInputs = playerRoot.GetComponent<StarterAssetsInputs>();
            if (starterInputs == null)
            {
                starterInputs = Undo.AddComponent<StarterAssetsInputs>(playerRoot);
            }

            PlayerInput playerInput = playerRoot.GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                playerInput = Undo.AddComponent<PlayerInput>(playerRoot);
            }

            ThirdPersonController thirdPersonController = playerRoot.GetComponent<ThirdPersonController>();
            if (thirdPersonController == null)
            {
                thirdPersonController = Undo.AddComponent<ThirdPersonController>(playerRoot);
            }

            StarterAssetsThirdPersonBridge bridge = playerRoot.GetComponent<StarterAssetsThirdPersonBridge>();
            if (bridge == null)
            {
                bridge = Undo.AddComponent<StarterAssetsThirdPersonBridge>(playerRoot);
            }

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                EditorUtility.DisplayDialog("Missing input actions", "Could not load StarterAssets.inputactions.", "OK");
                return;
            }

            playerInput.actions = inputActions;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.SendMessages;

            GameObject cameraTarget = GetOrCreateChild(playerRoot.transform, "CinemachineCameraTarget");
            cameraTarget.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            cameraTarget.transform.localRotation = Quaternion.identity;

            GameObject followCameraObject = GameObject.Find("PlayerFollowCamera");
            if (followCameraObject == null)
            {
                GameObject followCameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FollowCameraPrefabPath);
                if (followCameraPrefab == null)
                {
                    EditorUtility.DisplayDialog("Missing camera prefab", "Could not load PlayerFollowCamera prefab.", "OK");
                    return;
                }

                followCameraObject = PrefabUtility.InstantiatePrefab(followCameraPrefab, scene) as GameObject;
                if (followCameraObject != null)
                {
                    followCameraObject.name = "PlayerFollowCamera";
                    Undo.RegisterCreatedObjectUndo(followCameraObject, "Create PlayerFollowCamera");
                }
            }

            CinemachineVirtualCamera virtualCamera = followCameraObject != null
                ? followCameraObject.GetComponent<CinemachineVirtualCamera>()
                : null;
            if (virtualCamera == null)
            {
                EditorUtility.DisplayDialog("Invalid camera prefab", "PlayerFollowCamera does not have a CinemachineVirtualCamera.", "OK");
                return;
            }

            virtualCamera.Follow = cameraTarget.transform;
            virtualCamera.LookAt = cameraTarget.transform;
            ApplyPresentationCameraDefaults(virtualCamera);

            if (gameplayCamera.GetComponent<CinemachineBrain>() == null)
            {
                Undo.AddComponent<CinemachineBrain>(gameplayCamera.gameObject);
            }

            thirdPersonController.CinemachineCameraTarget = cameraTarget;
            thirdPersonController.GroundLayers = ~0;
            thirdPersonController.MoveSpeed = firstPersonController.walkSpeed;
            thirdPersonController.SprintSpeed = Mathf.Max(firstPersonController.runSpeed, firstPersonController.walkSpeed + 1f);

            bridge.playerController = firstPersonController;
            bridge.playerInteractor = interactor;
            bridge.cameraPivot = cameraPivot;
            bridge.gameplayCamera = gameplayCamera;
            bridge.thirdPersonController = thirdPersonController;
            bridge.starterAssetsInputs = starterInputs;
            bridge.playerInput = playerInput;
            bridge.cinemachineCameraTarget = cameraTarget.transform;
            bridge.followVirtualCamera = virtualCamera;
            bridge.startInThirdPerson = true;

            viewModeController.playerController = firstPersonController;
            viewModeController.thirdPersonBridge = bridge;
            viewModeController.playerInteractor = interactor;
            viewModeController.cameraPivot = cameraPivot;
            viewModeController.gameplayCamera = gameplayCamera;
            viewModeController.initialViewMode = ViewMode.ThirdPerson;

            manager.playerController = firstPersonController;
            manager.playerViewModeController = viewModeController;
            manager.playerInteractor = interactor;

            Transform existingVisual = playerRoot.transform.Find("PlayerVisual");
            if (existingVisual != null)
            {
                bridge.playerVisualRoot = existingVisual;
                viewModeController.playerVisualRoot = existingVisual;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = playerRoot;
            EditorUtility.DisplayDialog(
                "Third person setup complete",
                "Starter Assets third-person components are now connected to the current Player.\n\nNext: select your character model and run the replace-model tool.",
                "OK");
        }

        [MenuItem("Tools/Zhuozhengyuan/Replace Player Visual With Selected Model")]
        public static void ReplacePlayerVisualWithSelectedModel()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject playerRoot = GameObject.Find("Player");
            if (!scene.IsValid() || !scene.isLoaded || playerRoot == null)
            {
                EditorUtility.DisplayDialog("Missing player", "Open the scene and make sure Player exists first.", "OK");
                return;
            }

            GameObject selectedPrefab = ResolveSelectedPrefabOrSceneRoot();
            if (selectedPrefab == null)
            {
                EditorUtility.DisplayDialog("No model selected", "Select your model prefab or an instance in the hierarchy first.", "OK");
                return;
            }

            Transform oldVisual = playerRoot.transform.Find("PlayerVisual");
            if (oldVisual != null)
            {
                Undo.DestroyObjectImmediate(oldVisual.gameObject);
            }

            GameObject visualInstance = PrefabUtility.InstantiatePrefab(selectedPrefab, scene) as GameObject;
            if (visualInstance == null)
            {
                EditorUtility.DisplayDialog("Replace failed", "Could not instantiate the selected model.", "OK");
                return;
            }

            visualInstance.name = "PlayerVisual";
            Undo.RegisterCreatedObjectUndo(visualInstance, "Replace Player Visual");
            visualInstance.transform.SetParent(playerRoot.transform, false);
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;

            ConfigureRootAnimator(playerRoot, visualInstance);

            PlayerViewModeController viewModeController = playerRoot.GetComponent<PlayerViewModeController>();
            StarterAssetsThirdPersonBridge bridge = playerRoot.GetComponent<StarterAssetsThirdPersonBridge>();
            if (viewModeController != null)
            {
                viewModeController.playerVisualRoot = visualInstance.transform;
            }

            if (bridge != null)
            {
                bridge.playerVisualRoot = visualInstance.transform;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = visualInstance;
            Debug.Log("Replaced PlayerVisual. If the model appears too large or too small, hand-adjust PlayerVisual local scale.");
        }

        private static GameObject ResolveSelectedPrefabOrSceneRoot()
        {
            Object activeObject = Selection.activeObject;
            if (activeObject is GameObject selectedGameObject)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(selectedGameObject))
                {
                    return selectedGameObject;
                }

                GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(selectedGameObject);
                if (prefabSource != null)
                {
                    return prefabSource;
                }

                return selectedGameObject;
            }

            return null;
        }

        private static GameObject GetOrCreateChild(Transform parent, string objectName)
        {
            Transform existing = parent.Find(objectName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(child, "Create child");
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void ApplyPresentationCameraDefaults(CinemachineVirtualCamera virtualCamera)
        {
            if (virtualCamera == null)
            {
                return;
            }

            Cinemachine3rdPersonFollow thirdPersonFollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (thirdPersonFollow != null)
            {
                thirdPersonFollow.ShoulderOffset = new Vector3(0f, 0.25f, 0f);
                thirdPersonFollow.VerticalArmLength = 0.45f;
                thirdPersonFollow.CameraSide = 0.5f;
                thirdPersonFollow.CameraDistance = 4.8f;
                thirdPersonFollow.CameraRadius = 0.18f;
                thirdPersonFollow.Damping = new Vector3(0.15f, 0.2f, 0.18f);
            }
        }

        private static void ConfigureRootAnimator(GameObject playerRoot, GameObject visualInstance)
        {
            if (playerRoot == null || visualInstance == null)
            {
                return;
            }

            Animator childAnimator = visualInstance.GetComponentInChildren<Animator>(true);
            Animator rootAnimator = playerRoot.GetComponent<Animator>();
            if (rootAnimator == null)
            {
                rootAnimator = Undo.AddComponent<Animator>(playerRoot);
            }

            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ThirdPersonControllerAssetPath);
            if (controller != null)
            {
                rootAnimator.runtimeAnimatorController = controller;
            }

            if (childAnimator != null)
            {
                rootAnimator.avatar = childAnimator.avatar;
                rootAnimator.applyRootMotion = false;
                rootAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                childAnimator.enabled = false;
            }
        }
    }
}
#endif
