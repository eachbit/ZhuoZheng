using System;
using System.IO;
using UnityEngine;

namespace ZhuozhengYuan
{
    public static class SaveSystem
    {
        private const string SaveDirectoryName = "SaveData";
        private const string SaveFileName = "savegame.json";

        public static string ProjectRootPath
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); }
        }

        public static string SaveDirectoryPath
        {
            get { return Path.Combine(ProjectRootPath, SaveDirectoryName); }
        }

        public static string SavePath
        {
            get { return Path.Combine(SaveDirectoryPath, SaveFileName); }
        }

        private static string LegacySavePath
        {
            get { return Path.Combine(Application.persistentDataPath, SaveFileName); }
        }

        public static SaveData Load()
        {
            TryMigrateLegacySave();

            if (!File.Exists(SavePath))
            {
                return SaveData.CreateDefault();
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return SaveData.CreateDefault();
                }

                SaveData loaded = JsonUtility.FromJson<SaveData>(json);
                return loaded ?? SaveData.CreateDefault();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("读取存档失败，将回退到新游戏状态。原因: " + exception.Message);
                return SaveData.CreateDefault();
            }
        }

        public static void Save(SaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(SaveDirectoryPath))
                {
                    Directory.CreateDirectory(SaveDirectoryPath);
                }

                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogError("写入存档失败: " + exception.Message);
            }
        }

        private static void TryMigrateLegacySave()
        {
            if (File.Exists(SavePath) || !File.Exists(LegacySavePath))
            {
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(SaveDirectoryPath))
                {
                    Directory.CreateDirectory(SaveDirectoryPath);
                }

                File.Copy(LegacySavePath, SavePath, false);
                Debug.Log("SaveSystem migrated save file to project path: " + SavePath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("迁移旧版存档失败: " + exception.Message);
            }
        }
    }
}
