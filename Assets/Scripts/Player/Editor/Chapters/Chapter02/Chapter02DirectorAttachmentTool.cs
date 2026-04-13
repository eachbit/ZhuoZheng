#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZhuozhengYuan.EditorTools
{
    public static class Chapter02DirectorAttachmentTool
    {
        private const string AttachMenuPath = "Tools/Zhuozhengyuan/Attach Chapter02 Director To Selected Trigger";

        [MenuItem(AttachMenuPath)]
        public static void AttachToSelectedTrigger()
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

            Undo.RecordObject(collider, "Configure Chapter02 Trigger Collider");
            collider.isTrigger = true;

            Chapter02Director director = selectedObject.GetComponent<Chapter02Director>();
            if (director == null)
            {
                director = Undo.AddComponent<Chapter02Director>(selectedObject);
            }

            Undo.RecordObject(director, "Configure Chapter02 Director");
            director.manager = Object.FindObjectOfType<GardenGameManager>();
            director.questionsRequiredToUnlock = 4;
            director.startWhenPlayerEntersTrigger = true;
            director.disableTriggerAfterCompletion = true;

            EditorUtility.SetDirty(collider);
            EditorUtility.SetDirty(director);
            if (selectedObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(selectedObject.scene);
            }

            Debug.Log("Chapter02Director attached to selected trigger.");
        }

        [MenuItem(AttachMenuPath, true)]
        private static bool ValidateAttachToSelectedTrigger()
        {
            return Selection.activeGameObject != null;
        }
    }
}
#endif
