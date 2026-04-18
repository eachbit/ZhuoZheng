using System;
using System.IO;
using UnityEngine;

[Serializable]
public class WenxinAssistantConfigData
{
    public string appId = "ed8fc9b8-f99d-4186-9d26-2e3e2693c1c6";
    public string appBuilderApiKey = "bce-v3/ALTAK-0BZyJFsXEWP7BdP65F5c0/65f47c5a159b049010d275ab80540fa49a1089c7";
    public string speechApiKey = "bce-v3/ALTAK-0BZyJFsXEWP7BdP65F5c0/65f47c5a159b049010d275ab80540fa49a1089c7";
    public string endUserId = "heart-lab-user";
    public string assistantName = "实验AI助手";
    public string assistantProjectName = "";
    public string assistantExperimentType = "";
    public string assistantWelcomeText = "";
    public string questionInputPlaceholder = "";
    public bool showAssistantName = true;
    public bool showAssistantProjectName = false;
    public bool showAssistantExperimentType = true;
    public bool showStatusBar = false;
    public bool showImageInfo = false;
    public string assistantAvatarImage = "WenxinAssistantAvatar.png";
    public string assistantAvatarFramesFolder = "WenxinAssistantFrames";
    public string assistantAvatarPresentation = "portrait";
    public string assistantMovementMode = "fixed";
    public string assistantMovementEdge = "random";
    public string assistantIdleAnchor = "bottom_right";
    public float assistantMoveSpeed = 180f;
    public float assistantAvatarSize = 96f;
    public float assistantAvatarFrameRate = 6f;
    public string visionModel = "qwen2.5-vl-7b-instruct";
    public int speechSampleRate = 16000;
    public string speechFormat = "wav";
    public int speechDevPid = 1537;
}

public static class WenxinAssistantConfig
{
    private const string ConfigFileName = "WenxinAssistantConfig.json";

    public static string GetConfigPath()
    {
        return Path.Combine(Application.streamingAssetsPath, ConfigFileName);
    }

    public static WenxinAssistantConfigData Load()
    {
        string path = GetConfigPath();
        try
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                WenxinAssistantConfigData data = JsonUtility.FromJson<WenxinAssistantConfigData>(json);
                if (data != null)
                {
                    if (string.IsNullOrEmpty(data.endUserId))
                    {
                        data.endUserId = "heart-lab-user";
                    }
                    if (string.IsNullOrEmpty(data.assistantName))
                    {
                        data.assistantName = "实验AI助手";
                    }
                    if (data.assistantProjectName == null)
                    {
                        data.assistantProjectName = "";
                    }
                    if (data.assistantExperimentType == null)
                    {
                        data.assistantExperimentType = "";
                    }
                    if (data.assistantWelcomeText == null)
                    {
                        data.assistantWelcomeText = "";
                    }
                    if (data.questionInputPlaceholder == null)
                    {
                        data.questionInputPlaceholder = "";
                    }
                    if (!data.showStatusBar)
                    {
                        data.showStatusBar = false;
                    }
                    if (!data.showImageInfo)
                    {
                        data.showImageInfo = false;
                    }
                    if (string.IsNullOrEmpty(data.assistantAvatarImage))
                    {
                        data.assistantAvatarImage = "WenxinAssistantAvatar.png";
                    }
                    if (string.IsNullOrEmpty(data.assistantAvatarFramesFolder))
                    {
                        data.assistantAvatarFramesFolder = "WenxinAssistantFrames";
                    }
                    if (string.IsNullOrEmpty(data.assistantAvatarPresentation))
                    {
                        data.assistantAvatarPresentation = "portrait";
                    }
                    if (string.IsNullOrEmpty(data.assistantMovementMode))
                    {
                        data.assistantMovementMode = "fixed";
                    }
                    if (string.IsNullOrEmpty(data.assistantMovementEdge))
                    {
                        data.assistantMovementEdge = "random";
                    }
                    if (string.IsNullOrEmpty(data.assistantIdleAnchor))
                    {
                        data.assistantIdleAnchor = "bottom_right";
                    }
                    if (data.assistantMoveSpeed <= 0f)
                    {
                        data.assistantMoveSpeed = 180f;
                    }
                    if (data.assistantAvatarSize <= 0f)
                    {
                        data.assistantAvatarSize = 96f;
                    }
                    if (data.assistantAvatarFrameRate <= 0f)
                    {
                        data.assistantAvatarFrameRate = 6f;
                    }
                    if (string.IsNullOrEmpty(data.speechApiKey))
                    {
                        data.speechApiKey = data.appBuilderApiKey;
                    }
                    if (string.IsNullOrEmpty(data.visionModel))
                    {
                        data.visionModel = "qwen2.5-vl-7b-instruct";
                    }
                    if (data.speechSampleRate <= 0)
                    {
                        data.speechSampleRate = 16000;
                    }
                    if (string.IsNullOrEmpty(data.speechFormat))
                    {
                        data.speechFormat = "wav";
                    }
                    if (data.speechDevPid <= 0)
                    {
                        data.speechDevPid = 1537;
                    }
                    return data;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Load Wenxin assistant config failed: " + ex.Message);
        }

        return new WenxinAssistantConfigData();
    }
}
