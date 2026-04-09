#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZhuozhengYuan.EditorTools
{
    public static class Chapter01RouteGuideAuthoringTools
    {
        [MenuItem("Tools/Zhuozhengyuan/Ensure Chapter01 Guide Path Markers")]
        public static void EnsureChapter01GuidePathMarkers()
        {
            GardenGameManager manager = Object.FindObjectOfType<GardenGameManager>();
            if (manager == null)
            {
                Debug.LogWarning("No GardenGameManager was found in the current scene.");
                return;
            }

            Chapter01Director director = manager.chapter01Director;
            if (director == null || director.leftGate == null)
            {
                Debug.LogWarning("Chapter01Director or left gate is missing.");
                return;
            }

            Transform markersRoot = director.leftGate.transform.parent;
            if (markersRoot == null)
            {
                Debug.LogWarning("Chapter 01 markers root could not be resolved.");
                return;
            }

            Transform guideRoot = markersRoot.Find("Chapter01GuidePath");
            if (guideRoot == null)
            {
                GameObject guideRootObject = new GameObject("Chapter01GuidePath");
                Undo.RegisterCreatedObjectUndo(guideRootObject, "Create Chapter01 Guide Path");
                guideRoot = guideRootObject.transform;
                guideRoot.SetParent(markersRoot, false);
            }

            if (guideRoot.childCount > 0)
            {
                Selection.activeGameObject = guideRoot.gameObject;
                Debug.Log("Chapter01GuidePath already exists. Adjust its GuidePoint_* children by hand.");
                return;
            }

            List<Vector3> points = new List<Vector3>();
            if (manager.TryGetResolvedChapter01RoutePathCopy(out List<Vector3> resolvedRoute, out _, out _))
            {
                points.AddRange(resolvedRoute);
            }
            else
            {
                Vector3 start = manager.introController != null && manager.introController.playerPostIntroPose != null
                    ? manager.introController.playerPostIntroPose.position
                    : manager.transform.position;
                Vector3 end = director.leftGate.transform.position;
                points.Add(start);
                points.Add(Vector3.Lerp(start, end, 0.33f));
                points.Add(Vector3.Lerp(start, end, 0.66f));
                points.Add(end);
            }

            for (int index = 0; index < points.Count; index++)
            {
                GameObject pointObject = new GameObject("GuidePoint_" + index.ToString("00"));
                Undo.RegisterCreatedObjectUndo(pointObject, "Create Guide Point");
                pointObject.transform.SetParent(guideRoot, false);
                pointObject.transform.position = points[index];
            }

            Selection.activeGameObject = guideRoot.gameObject;
            Debug.Log("Created Chapter01GuidePath using the current route as a starter. You can now hand-adjust the points.");
        }
    }
}
#endif
