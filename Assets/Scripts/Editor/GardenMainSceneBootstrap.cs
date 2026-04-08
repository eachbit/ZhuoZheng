#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZhuozhengYuan.EditorTools
{
    public static class GardenMainSceneBootstrap
    {
        private const string ScenePath = "Assets/Scenes/Garden_Main.unity";

        [MenuItem("Tools/Zhuozhengyuan/Create Garden_Main Starter Scene")]
        public static void CreateGardenMainStarterScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorUtility.DisplayDialog("Garden_Main already exists", "To avoid overwriting your manual scene adjustments, this tool will not rebuild an existing Garden_Main scene.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLighting();

            GameObject gardenModel = InstantiateAssetIfPossible(ResolveGardenModelPath(), "GardenModel");
            GameObject playerVisual = InstantiateAssetIfPossible(ResolveYouthModelPath(), "PlayerVisual");
            Bounds sceneBounds = CalculateSceneBounds(gardenModel);
            if (sceneBounds.size == Vector3.zero)
            {
                sceneBounds = new Bounds(Vector3.zero, new Vector3(200f, 10f, 200f));
            }

            GameObject managerRoot = new GameObject("GardenGame");
            PrototypeRuntimeUI runtimeUI = managerRoot.AddComponent<PrototypeRuntimeUI>();
            GardenGameManager gameManager = managerRoot.AddComponent<GardenGameManager>();
            GardenModelHiddenCollisionBuilder collisionBuilder = managerRoot.AddComponent<GardenModelHiddenCollisionBuilder>();
            IntroSequenceController introController = managerRoot.AddComponent<IntroSequenceController>();
            Chapter01Director chapterDirector = managerRoot.AddComponent<Chapter01Director>();

            GameObject playerRoot = CreatePlayer(playerVisual, sceneBounds);
            FirstPersonPlayerController playerController = playerRoot.GetComponent<FirstPersonPlayerController>();
            PlayerInteractor interactor = playerRoot.GetComponent<PlayerInteractor>();

            GameObject environmentRoot = new GameObject("Chapter01Environment");
            Chapter01EnvironmentController environmentController = environmentRoot.AddComponent<Chapter01EnvironmentController>();

            GameObject walkableGroundRoot = new GameObject("WalkableGroundRoot");
            CreateGlobalInvisibleGround(walkableGroundRoot.transform, sceneBounds);

            GameObject blockerRoot = new GameObject("InvisibleBlockerRoot");
            blockerRoot.transform.position = sceneBounds.center;

            GameObject referencesRoot = new GameObject("ReferencePoints");
            Transform playerIntroPose = CreateMarker(referencesRoot.transform, "PlayerIntroPose", sceneBounds.center + new Vector3(0f, 1.8f, -8f));
            Transform playerPostIntroPose = CreateMarker(referencesRoot.transform, "PlayerPostIntroPose", sceneBounds.center + new Vector3(0f, 1.8f, -4f));
            Transform gardenerEntrance = CreateMarker(referencesRoot.transform, "OldGardenerEntrance", sceneBounds.center + new Vector3(2f, 0f, -7f));
            Transform gardenerDialogue = CreateMarker(referencesRoot.transform, "OldGardenerDialogue", sceneBounds.center + new Vector3(1f, 0f, -5f));
            Transform gardenerExit = CreateMarker(referencesRoot.transform, "OldGardenerExit", sceneBounds.center + new Vector3(6f, 0f, -2f));

            GameObject oldGardener = CreatePlaceholderCapsule("OldGardenerPlaceholder", gardenerEntrance.position + Vector3.up, new Color(0.82f, 0.82f, 0.82f));

            GameObject chapterMarkers = new GameObject("Chapter01Markers");
            chapterMarkers.transform.position = sceneBounds.center;
            GateInteractable leftGate = CreateGate(chapterMarkers.transform, "LeftGateInteractable", sceneBounds.center + new Vector3(-4f, 0.5f, 4f), GateId.Left);
            GateInteractable rightGate = CreateGate(chapterMarkers.transform, "RightGateInteractable", sceneBounds.center + new Vector3(4f, 0.5f, 4f), GateId.Right);
            WaterDirectionInteractable flowSelector = CreateFlowSelector(chapterMarkers.transform, "FlowSelectorInteractable", sceneBounds.center + new Vector3(0f, 0.5f, 8f));
            PagePickupInteractable pagePickup = CreatePagePickup(chapterMarkers.transform, "Chapter01PagePickup", sceneBounds.center + new Vector3(2f, 0.5f, 10f));

            environmentController.pageRevealObjects = new[] { pagePickup.gameObject };

            gameManager.playerController = playerController;
            gameManager.playerInteractor = interactor;
            gameManager.runtimeUI = runtimeUI;
            gameManager.introController = introController;
            gameManager.chapter01Director = chapterDirector;
            gameManager.hiddenCollisionBuilder = collisionBuilder;

            collisionBuilder.targetRoot = gardenModel != null ? gardenModel.transform : null;

            runtimeUI.gameManager = gameManager;
            interactor.gameManager = gameManager;

            introController.manager = gameManager;
            introController.playerIntroPose = playerIntroPose;
            introController.playerPostIntroPose = playerPostIntroPose;
            introController.oldGardenerActor = oldGardener.transform;
            introController.oldGardenerEntrancePoint = gardenerEntrance;
            introController.oldGardenerDialoguePoint = gardenerDialogue;
            introController.oldGardenerExitPoint = gardenerExit;

            chapterDirector.manager = gameManager;
            chapterDirector.environmentController = environmentController;
            chapterDirector.leftGate = leftGate;
            chapterDirector.rightGate = rightGate;
            chapterDirector.flowSelector = flowSelector;
            chapterDirector.pagePickup = pagePickup;

            leftGate.director = chapterDirector;
            rightGate.director = chapterDirector;
            flowSelector.director = chapterDirector;
            pagePickup.director = chapterDirector;

            AddSceneToBuildSettings(ScenePath);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
            Selection.activeObject = managerRoot;

            EditorUtility.DisplayDialog(
                "Garden_Main created",
                "A starter scene, global invisible ground, placeholder player, placeholder elder, and chapter one interaction markers were created.\n\nNext, manually adjust positions, add InvisibleBlockerRoot blockers, bind water effects, and move the chapter markers to the real chapter one locations.",
                "OK");
        }

        private static void CreateLighting()
        {
            GameObject directionalLight = new GameObject("Directional Light");
            Light light = directionalLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static GameObject CreatePlayer(GameObject playerVisualAsset, Bounds sceneBounds)
        {
            GameObject playerRoot = new GameObject("Player");
            playerRoot.transform.position = sceneBounds.center + new Vector3(0f, 2f, -6f);

            CharacterController controller = playerRoot.AddComponent<CharacterController>();
            controller.height = 1.75f;
            controller.radius = 0.28f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            FirstPersonPlayerController playerController = playerRoot.AddComponent<FirstPersonPlayerController>();
            playerController.characterController = controller;

            PlayerInteractor interactor = playerRoot.AddComponent<PlayerInteractor>();

            GameObject pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(playerRoot.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(pivot.transform, false);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();

            playerController.cameraPivot = pivot.transform;
            interactor.playerCamera = cameraObject.GetComponent<Camera>();

            if (playerVisualAsset != null)
            {
                playerVisualAsset.transform.SetParent(playerRoot.transform, false);
                playerVisualAsset.transform.localPosition = Vector3.zero;
                playerVisualAsset.transform.localRotation = Quaternion.identity;
            }
            else
            {
                GameObject body = CreatePlaceholderCapsule("PlayerBodyPlaceholder", playerRoot.transform.position + Vector3.up, new Color(0.55f, 0.7f, 0.95f));
                body.transform.SetParent(playerRoot.transform, true);
            }

            return playerRoot;
        }

        private static void CreateGlobalInvisibleGround(Transform parent, Bounds bounds)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "GlobalInvisibleGround";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.6f, bounds.center.z);
            ground.transform.localScale = new Vector3(
                Mathf.Max(bounds.size.x + 20f, 50f),
                1f,
                Mathf.Max(bounds.size.z + 20f, 50f));

            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private static GateInteractable CreateGate(Transform parent, string objectName, Vector3 position, GateId gateId)
        {
            GameObject gateObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gateObject.name = objectName;
            gateObject.transform.SetParent(parent, false);
            gateObject.transform.position = position;
            gateObject.transform.localScale = new Vector3(0.7f, 1.2f, 0.7f);

            GateInteractable interactable = gateObject.AddComponent<GateInteractable>();
            interactable.gateId = gateId;
            interactable.gateDisplayName = gateId == GateId.Left ? "左暗闸" : "右暗闸";
            return interactable;
        }

        private static WaterDirectionInteractable CreateFlowSelector(Transform parent, string objectName, Vector3 position)
        {
            GameObject selectorObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            selectorObject.name = objectName;
            selectorObject.transform.SetParent(parent, false);
            selectorObject.transform.position = position;
            selectorObject.transform.localScale = new Vector3(0.7f, 0.15f, 0.7f);

            WaterDirectionInteractable interactable = selectorObject.AddComponent<WaterDirectionInteractable>();
            interactable.interactionLabel = "调定水流方向";
            return interactable;
        }

        private static PagePickupInteractable CreatePagePickup(Transform parent, string objectName, Vector3 position)
        {
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pickupObject.name = objectName;
            pickupObject.transform.SetParent(parent, false);
            pickupObject.transform.position = position;
            pickupObject.transform.localScale = new Vector3(0.45f, 0.6f, 1f);
            pickupObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            PagePickupInteractable interactable = pickupObject.AddComponent<PagePickupInteractable>();
            interactable.pageDisplayName = "《长物志》残页";
            interactable.SetAvailability(false);
            return interactable;
        }

        private static Transform CreateMarker(Transform parent, string objectName, Vector3 position)
        {
            GameObject marker = new GameObject(objectName);
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            return marker.transform;
        }

        private static GameObject InstantiateAssetIfPossible(string assetPath, string fallbackName)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null)
            {
                instance.name = fallbackName;
            }

            return instance;
        }

        private static GameObject CreatePlaceholderCapsule(string objectName, Vector3 position, Color color)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = objectName;
            capsule.transform.position = position;

            Renderer renderer = capsule.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = color;
                renderer.sharedMaterial = material;
            }

            return capsule;
        }

        private static string ResolveGardenModelPath()
        {
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Assets/Assets" });
            for (int index = 0; index < modelGuids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(modelGuids[index]);
                if (!assetPath.EndsWith(".fbx"))
                {
                    continue;
                }

                string directory = Path.GetDirectoryName(assetPath).Replace("\\", "/");
                if (string.Equals(directory, "Assets/Assets/Assets"))
                {
                    return assetPath.Replace("\\", "/");
                }
            }

            return null;
        }

        private static string ResolveYouthModelPath()
        {
            string[] modelGuids = AssetDatabase.FindAssets("3d66.com_JFH5455106455 t:Model", new[] { "Assets/Figure" });
            if (modelGuids.Length == 0)
            {
                return null;
            }

            return AssetDatabase.GUIDToAssetPath(modelGuids[0]);
        }

        private static Bounds CalculateSceneBounds(GameObject rootObject)
        {
            if (rootObject == null)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(rootObject.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int index = 0; index < scenes.Count; index++)
            {
                if (scenes[index].path == scenePath)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
