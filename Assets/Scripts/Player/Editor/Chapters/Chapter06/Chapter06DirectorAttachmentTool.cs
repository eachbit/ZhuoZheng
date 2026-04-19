#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZhuozhengYuan.EditorTools
{
    public static class Chapter06DirectorAttachmentTool
    {
        private const string AttachQuizMenuPath = "Tools/Zhuozhengyuan/Chapter06/Attach Quiz Trigger To Selected";
        private const string AttachFinaleMenuPath = "Tools/Zhuozhengyuan/Chapter06/Attach Finale View Trigger To Selected";

        [MenuItem(AttachQuizMenuPath)]
        public static void AttachQuizTriggerToSelected()
        {
            AttachToSelectedTrigger(Chapter06TriggerRole.QuizStart);
        }

        [MenuItem(AttachFinaleMenuPath)]
        public static void AttachFinaleViewTriggerToSelected()
        {
            AttachToSelectedTrigger(Chapter06TriggerRole.FinaleView);
        }

        [MenuItem(AttachQuizMenuPath, true)]
        [MenuItem(AttachFinaleMenuPath, true)]
        private static bool ValidateAttachToSelectedTrigger()
        {
            return Selection.activeGameObject != null;
        }

        private static void AttachToSelectedTrigger(Chapter06TriggerRole role)
        {
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                Debug.LogWarning("Select a trigger object first.");
                return;
            }

            Collider collider = selectedObject.GetComponent<Collider>();
            if (collider == null)
            {
                collider = Undo.AddComponent<BoxCollider>(selectedObject);
            }

            Undo.RecordObject(collider, "Configure Chapter06 Trigger Collider");
            collider.isTrigger = true;

            Chapter06Director director = selectedObject.GetComponent<Chapter06Director>();
            if (director == null)
            {
                director = Undo.AddComponent<Chapter06Director>(selectedObject);
            }

            Undo.RecordObject(director, "Configure Chapter06 Director");
            director.manager = Object.FindObjectOfType<GardenGameManager>();
            director.triggerRole = role;
            director.questionsRequiredToUnlock = 6;
            director.startWhenPlayerEntersTrigger = true;
            director.disableTriggerAfterCompletion = true;

            EditorUtility.SetDirty(collider);
            EditorUtility.SetDirty(director);
            if (selectedObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(selectedObject.scene);
            }

            Debug.Log(role == Chapter06TriggerRole.QuizStart
                ? "Chapter06 quiz trigger attached to selected object."
                : "Chapter06 finale view trigger attached to selected object.");
        }
    }
}
#endif
