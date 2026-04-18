using UnityEditor;
using UnityEngine;

public static class WenxinAssistantEditorMenu
{
    [MenuItem("GameObject/UI/Wenxin Assistant/Editable Assistant", false, 2050)]
    private static void CreateEditableAssistant(MenuCommand menuCommand)
    {
        GameObject root = new GameObject("WenxinAssistantEditable");
        Undo.RegisterCreatedObjectUndo(root, "Create Wenxin Assistant Editable");
        GameObjectUtility.SetParentAndAlign(root, menuCommand.context as GameObject);

        WenxinAssistantController controller = root.AddComponent<WenxinAssistantController>();
        controller.EnableSceneHierarchyLayout();
        controller.BuildEditableHierarchyInEditor();

        Selection.activeGameObject = root;
    }
}
