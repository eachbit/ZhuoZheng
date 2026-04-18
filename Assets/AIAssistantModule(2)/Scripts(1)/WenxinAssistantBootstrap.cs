using UnityEngine;

public static class WenxinAssistantBootstrap
{
    private const string AssistantObjectName = "__WenxinAssistantRuntime__";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateAssistant()
    {
        if (Object.FindObjectOfType<WenxinAssistantController>() != null)
        {
            return;
        }

        GameObject root = new GameObject(AssistantObjectName);
        root.AddComponent<WenxinAssistantController>();
        Object.DontDestroyOnLoad(root);
    }
}
