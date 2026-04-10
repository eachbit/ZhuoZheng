#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZhuozhengYuan.EditorTools
{
    public static class GardenSceneHierarchyOrganizer
    {
        private const string CoreRootName = "_00_Core";
        private const string WorldRootName = "_10_World";
        private const string StoryRootName = "_20_Story";
        private const string ChaptersRootName = "_30_Chapters";
        private const string Chapter01GameplayRootName = "_31_Chapter01_Gameplay";
        private const string Chapter01VisualsRootName = "_32_Chapter01_Visuals";

        [MenuItem("Tools/Zhuozhengyuan/Organize Current Scene Hierarchy")]
        public static void OrganizeCurrentSceneHierarchy()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("No Scene", "Open the scene you want to organize first.", "OK");
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Organize Scene Hierarchy");

            GameObject coreRoot = GetOrCreateSceneRoot(scene, CoreRootName);
            GameObject worldRoot = GetOrCreateSceneRoot(scene, WorldRootName);
            GameObject storyRoot = GetOrCreateSceneRoot(scene, StoryRootName);
            GameObject chaptersRoot = GetOrCreateSceneRoot(scene, ChaptersRootName);
            GameObject chapter01GameplayRoot = GetOrCreateChild(chaptersRoot.transform, Chapter01GameplayRootName);
            GameObject chapter01VisualsRoot = GetOrCreateChild(chaptersRoot.transform, Chapter01VisualsRootName);

            ReparentIfFound(scene, "GardenGame", coreRoot.transform);
            ReparentIfFound(scene, "Player", coreRoot.transform);

            ReparentIfFound(scene, "Directional Light", worldRoot.transform);
            ReparentIfFound(scene, "GardenModel", worldRoot.transform);
            ReparentIfFound(scene, "WalkableGroundRoot", worldRoot.transform);
            ReparentIfFound(scene, "InvisibleBlockerRoot", worldRoot.transform);

            ReparentIfFound(scene, "ReferencePoints", storyRoot.transform);
            ReparentIfFound(scene, "OldGardenerPlaceholder", storyRoot.transform);

            ReparentIfFound(scene, "Chapter01Environment", chapter01VisualsRoot.transform);
            ReparentIfFound(scene, "Chapter01Markers", chapter01GameplayRoot.transform);

            coreRoot.transform.SetSiblingIndex(0);
            worldRoot.transform.SetSiblingIndex(1);
            storyRoot.transform.SetSiblingIndex(2);
            chaptersRoot.transform.SetSiblingIndex(3);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog(
                "Hierarchy Organized",
                "The current scene hierarchy has been grouped into Core, World, Story, and Chapters, with Chapter01 split into Gameplay and Visuals.\n\nAll objects keep their world positions.",
                "OK");
        }

        private static GameObject GetOrCreateSceneRoot(Scene scene, string objectName)
        {
            GameObject existing = FindSceneObject(scene, objectName);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(root, "Create Scene Group Root");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static GameObject GetOrCreateChild(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(objectName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(child, "Create Scene Group Child");
            child.transform.SetParent(parent, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child;
        }

        private static void ReparentIfFound(Scene scene, string objectName, Transform newParent)
        {
            if (newParent == null)
            {
                return;
            }

            GameObject target = FindSceneObject(scene, objectName);
            if (target == null || target.transform == newParent)
            {
                return;
            }

            if (target.transform.parent == newParent)
            {
                return;
            }

            Vector3 worldPosition = target.transform.position;
            Quaternion worldRotation = target.transform.rotation;
            Vector3 worldScale = target.transform.lossyScale;

            Undo.SetTransformParent(target.transform, newParent, "Organize Scene Hierarchy");
            target.transform.position = worldPosition;
            target.transform.rotation = worldRotation;

            if (target.transform.parent != null && target.transform.parent.lossyScale != Vector3.zero)
            {
                Vector3 parentScale = target.transform.parent.lossyScale;
                target.transform.localScale = new Vector3(
                    SafeDivide(worldScale.x, parentScale.x),
                    SafeDivide(worldScale.y, parentScale.y),
                    SafeDivide(worldScale.z, parentScale.z));
            }
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root.name == objectName)
                {
                    return root;
                }

                Transform[] children = root.GetComponentsInChildren<Transform>(true);
                for (int childIndex = 0; childIndex < children.Length; childIndex++)
                {
                    Transform child = children[childIndex];
                    if (child != null && child.name == objectName)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        private static float SafeDivide(float value, float divisor)
        {
            if (Mathf.Approximately(divisor, 0f))
            {
                return value;
            }

            return value / divisor;
        }
    }
}
#endif
