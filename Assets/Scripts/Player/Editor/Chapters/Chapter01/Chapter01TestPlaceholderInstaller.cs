#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZhuozhengYuan.EditorTools
{
    [InitializeOnLoad]
    public static class Chapter01TestPlaceholderInstaller
    {
        private const string ScenePath = "Assets/Scenes/Garden_Main.unity";
        private const string LeftGateName = "LeftGateInteractable";
        private const string RightGateName = "RightGateInteractable";
        private const string FlowSelectorName = "FlowSelectorInteractable";
        private const string AutoGateVisualName = "__TestPlaceholderGateVisual";
        private const string AutoFlowVisualName = "__TestPlaceholderFlowVisual";
        private const string GateAssetPath = "Assets/Figure/Chapters/Chapter01/TestPlaceholders/KenneyModularSpaceKit/Models/FBX format/gate-door-window.fbx";
        private const string FlowAssetPath = "Assets/Figure/Chapters/Chapter01/TestPlaceholders/KenneyPrototypeKit/Models/FBX format/button-floor-round-small.fbx";

        static Chapter01TestPlaceholderInstaller()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += TryAttachToActiveScene;
        }

        [MenuItem("Tools/Zhuozhengyuan/Attach Chapter01 Test Placeholder Visuals")]
        public static void AttachToActiveSceneMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open Garden_Main first", "This tool only attaches test placeholder visuals to Assets/Scenes/Garden_Main.unity.", "OK");
                return;
            }

            AttachToScene(scene, true);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path != ScenePath)
            {
                return;
            }

            EditorApplication.delayCall += TryAttachToActiveScene;
        }

        private static void TryAttachToActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            {
                return;
            }

            AttachToScene(scene, false);
        }

        private static void AttachToScene(Scene scene, bool showResultDialog)
        {
            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GateAssetPath);
            GameObject flowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FlowAssetPath);
            if (gatePrefab == null || flowPrefab == null)
            {
                if (showResultDialog)
                {
                    EditorUtility.DisplayDialog("Placeholder models missing", "Could not find the imported Chapter01 test placeholder assets under Assets/Figure/Chapters/Chapter01/TestPlaceholders.", "OK");
                }

                return;
            }

            int attachedCount = 0;
            int skippedCount = 0;

            if (TryAttachGatePlaceholder(FindSceneObject(LeftGateName), gatePrefab, scene))
            {
                attachedCount++;
            }
            else
            {
                skippedCount++;
            }

            if (TryAttachGatePlaceholder(FindSceneObject(RightGateName), gatePrefab, scene))
            {
                attachedCount++;
            }
            else
            {
                skippedCount++;
            }

            if (TryAttachFlowPlaceholder(FindSceneObject(FlowSelectorName), flowPrefab, scene))
            {
                attachedCount++;
            }
            else
            {
                skippedCount++;
            }

            if (attachedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            if (showResultDialog)
            {
                EditorUtility.DisplayDialog(
                    "Chapter01 placeholders attached",
                    "Attached " + attachedCount + " test visuals. Skipped " + skippedCount + " object(s) that already had child visuals or could not be found.",
                    "OK");
            }
        }

        private static GameObject FindSceneObject(string objectName)
        {
            return GameObject.Find(objectName);
        }

        private static bool TryAttachGatePlaceholder(GameObject targetObject, GameObject gatePrefab, Scene scene)
        {
            return TryAttachPlaceholder(targetObject, gatePrefab, AutoGateVisualName, scene, 0.92f, 1.05f);
        }

        private static bool TryAttachFlowPlaceholder(GameObject targetObject, GameObject flowPrefab, Scene scene)
        {
            return TryAttachPlaceholder(targetObject, flowPrefab, AutoFlowVisualName, scene, 0.72f, 0.5f);
        }

        private static bool TryAttachPlaceholder(GameObject targetObject, GameObject prefab, string autoVisualName, Scene scene, float widthScale, float heightScale)
        {
            if (targetObject == null || prefab == null)
            {
                return false;
            }

            if (targetObject.transform.Find(autoVisualName) != null)
            {
                DisableRootRenderer(targetObject);
                return false;
            }

            if (HasManualVisualChild(targetObject.transform, autoVisualName))
            {
                return false;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                return false;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Attach Chapter01 test placeholder");
            instance.name = autoVisualName;
            instance.transform.SetParent(targetObject.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            FitInstanceToTarget(instance.transform, targetObject, widthScale, heightScale);
            DisableRootRenderer(targetObject);
            EditorUtility.SetDirty(targetObject);
            EditorUtility.SetDirty(instance);
            return true;
        }

        private static bool HasManualVisualChild(Transform target, string autoVisualName)
        {
            for (int index = 0; index < target.childCount; index++)
            {
                Transform child = target.GetChild(index);
                if (child == null || child.name == autoVisualName)
                {
                    continue;
                }

                if (child.GetComponentInChildren<Renderer>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static void DisableRootRenderer(GameObject targetObject)
        {
            Renderer[] renderers = targetObject.GetComponents<Renderer>();
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null)
                {
                    renderers[index].enabled = false;
                }
            }
        }

        private static void FitInstanceToTarget(Transform instanceTransform, GameObject targetObject, float widthScale, float heightScale)
        {
            instanceTransform.localPosition = Vector3.zero;
            instanceTransform.localRotation = Quaternion.identity;
            instanceTransform.localScale = Vector3.one;

            Bounds targetBounds = GetTargetBounds(targetObject);
            Bounds sourceBounds = CalculateRendererBounds(instanceTransform);

            float targetWidth = Mathf.Max(0.1f, Mathf.Max(targetBounds.size.x, targetBounds.size.z) * widthScale);
            float targetHeight = Mathf.Max(0.1f, targetBounds.size.y * heightScale);
            float sourceWidth = Mathf.Max(0.01f, Mathf.Max(sourceBounds.size.x, sourceBounds.size.z));
            float sourceHeight = Mathf.Max(0.01f, sourceBounds.size.y);
            float uniformScale = Mathf.Min(targetWidth / sourceWidth, targetHeight / sourceHeight);

            instanceTransform.localScale = Vector3.one * Mathf.Max(0.01f, uniformScale);

            sourceBounds = CalculateRendererBounds(instanceTransform);
            Vector3 offset = new Vector3(
                targetBounds.center.x - sourceBounds.center.x,
                targetBounds.min.y - sourceBounds.min.y,
                targetBounds.center.z - sourceBounds.center.z);

            instanceTransform.position += offset;
        }

        private static Bounds GetTargetBounds(GameObject targetObject)
        {
            Collider collider = targetObject.GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds;
            }

            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds;
            }

            return new Bounds(targetObject.transform.position, Vector3.one);
        }

        private static Bounds CalculateRendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(root.position, Vector3.one * 0.1f);
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }
    }
}
#endif
