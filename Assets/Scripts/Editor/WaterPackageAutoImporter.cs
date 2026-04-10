#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZhuozhengYuan.EditorTools
{
    [InitializeOnLoad]
    public static class WaterPackageAutoImporter
    {
        private const string PackagePath = @"C:\Users\32757\Desktop\water.unitypackage";
        private const string MenuPath = "Tools/Zhuozhengyuan/Import Desktop Water Package";
        private const string SessionKey = "ZhuozhengYuan.WaterPackageAutoImporter.Pending";

        static WaterPackageAutoImporter()
        {
            EditorApplication.delayCall += TryImportOnLoad;
        }

        [MenuItem(MenuPath)]
        public static void ImportFromMenu()
        {
            ImportPackage(showDialogs: true);
        }

        private static void TryImportOnLoad()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            ImportPackage(showDialogs: false);
        }

        private static void ImportPackage(bool showDialogs)
        {
            if (!File.Exists(PackagePath))
            {
                if (showDialogs)
                {
                    EditorUtility.DisplayDialog("Water package missing", "Could not find C:\\Users\\32757\\Desktop\\water.unitypackage.", "OK");
                }

                return;
            }

            string importKey = BuildImportKey();
            if (EditorPrefs.GetBool(importKey, false))
            {
                if (showDialogs)
                {
                    EditorUtility.DisplayDialog("Water package already imported", "This desktop water package has already been imported into the current project.", "OK");
                }

                return;
            }

            AssetDatabase.ImportPackage(PackagePath, false);
            EditorPrefs.SetBool(importKey, true);

            if (showDialogs)
            {
                EditorUtility.DisplayDialog("Water package imported", "Imported water.unitypackage into the current Unity project.", "OK");
            }
            else
            {
                Debug.Log("Imported desktop water package: " + PackagePath);
            }
        }

        private static string BuildImportKey()
        {
            FileInfo fileInfo = new FileInfo(PackagePath);
            string projectKey = Application.dataPath.Replace('\\', '/');
            return "ZhuozhengYuan.WaterPackageImported." + projectKey + "." + fileInfo.Length + "." + fileInfo.LastWriteTimeUtc.Ticks;
        }
    }
}
#endif
