using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter01RouteGuideVisualTests
    {
        [Test]
        public void RebuildGuide_ShouldCreateRibbonAndDecorationRoots()
        {
            Type guideType = Type.GetType("ZhuozhengYuan.Chapter01AuthoredRouteGuide, Assembly-CSharp");
            Assert.IsNotNull(guideType, "Chapter01AuthoredRouteGuide it still missing.");

            GameObject root = new GameObject("RouteGuideRoot");
            GameObject start = null;
            GameObject routePoint = null;
            GameObject gate = null;

            try
            {
                MonoBehaviour guide = (MonoBehaviour)root.AddComponent(guideType);

                start = new GameObject("Start");
                routePoint = new GameObject("GuidePoint_00");
                gate = new GameObject("Gate");

                start.transform.position = new Vector3(0f, 0f, 0f);
                routePoint.transform.position = new Vector3(2f, 0f, 4f);
                gate.transform.position = new Vector3(6f, 0f, 8f);

                SetField(guide, "showGuideOnStart", true);
                SetField(guide, "playerStartPose", start.transform);
                SetField(guide, "targetGate", gate.transform);
                SetField(guide, "routePoints", new[] { routePoint.transform });
                SetField(guide, "trimSegmentsAgainstObstacles", false);
                SetField(guide, "groundOffset", 0f);

                Invoke(guide, "RebuildGuide");

                Transform runtimeRoot = root.transform.Find("Chapter01AuthoredGuideRoot");
                Assert.IsNotNull(runtimeRoot, "Chapter01AuthoredGuideRoot was not created.");
                // Task 2 intentionally introduces these child roots so the hybrid guide
                // can keep ribbon, node decorations, and destination marker separated.
                Assert.IsNotNull(runtimeRoot.Find("DecorationsRoot"), "DecorationsRoot was not created.");
                Assert.IsNotNull(runtimeRoot.Find("DestinationMarkerRoot"), "DestinationMarkerRoot was not created.");
            }
            finally
            {
                DestroyImmediateIfExists(routePoint);
                DestroyImmediateIfExists(gate);
                DestroyImmediateIfExists(start);
                DestroyImmediateIfExists(root);
            }
        }

        [Test]
        public void RebuildGuide_ShouldCreateLimitedDecorationMarkers()
        {
            Type guideType = Type.GetType("ZhuozhengYuan.Chapter01AuthoredRouteGuide, Assembly-CSharp");
            Assert.IsNotNull(guideType, "Chapter01AuthoredRouteGuide it still missing.");

            GameObject root = new GameObject("RouteGuideRoot");
            GameObject[] points = new GameObject[4];

            try
            {
                MonoBehaviour guide = (MonoBehaviour)root.AddComponent(guideType);

                for (int index = 0; index < points.Length; index++)
                {
                    points[index] = new GameObject("RoutePoint_" + index.ToString("00"));
                    points[index].transform.position = new Vector3(index * 3f, 0f, index * 4f);
                }

                SetField(guide, "showGuideOnStart", true);
                SetField(guide, "playerStartPose", points[0].transform);
                SetField(guide, "targetGate", points[3].transform);
                SetField(guide, "routePoints", new[] { points[1].transform, points[2].transform });
                SetField(guide, "trimSegmentsAgainstObstacles", false);
                SetField(guide, "groundOffset", 0f);

                Invoke(guide, "RebuildGuide");

                Transform runtimeRoot = root.transform.Find("Chapter01AuthoredGuideRoot");
                Assert.IsNotNull(runtimeRoot, "Chapter01AuthoredGuideRoot was not created.");

                // The decoration root is a future contract for the hybrid guide pass.
                Transform decorationsRoot = runtimeRoot.Find("DecorationsRoot");
                Assert.IsNotNull(decorationsRoot, "DecorationsRoot was not created.");
                Assert.LessOrEqual(decorationsRoot.childCount, 6, "DecorationsRoot should stay limited to a small number of markers.");
            }
            finally
            {
                for (int index = 0; index < points.Length; index++)
                {
                    DestroyImmediateIfExists(points[index]);
                }

                DestroyImmediateIfExists(root);
            }
        }

        [Test]
        public void RebuildGuide_ShouldCreateDestinationMarkerChild()
        {
            Type guideType = Type.GetType("ZhuozhengYuan.Chapter01AuthoredRouteGuide, Assembly-CSharp");
            Assert.IsNotNull(guideType, "Chapter01AuthoredRouteGuide it still missing.");

            GameObject root = new GameObject("RouteGuideRoot");
            GameObject start = null;
            GameObject end = null;

            try
            {
                MonoBehaviour guide = (MonoBehaviour)root.AddComponent(guideType);

                start = new GameObject("Start");
                end = new GameObject("End");

                start.transform.position = new Vector3(0f, 0f, 0f);
                end.transform.position = new Vector3(10f, 0f, 4f);

                SetField(guide, "showGuideOnStart", true);
                SetField(guide, "playerStartPose", start.transform);
                SetField(guide, "targetGate", end.transform);
                SetField(guide, "trimSegmentsAgainstObstacles", false);
                SetField(guide, "groundOffset", 0f);

                Invoke(guide, "RebuildGuide");

                Transform markerRoot = root.transform.Find("Chapter01AuthoredGuideRoot/DestinationMarkerRoot");
                Assert.IsNotNull(markerRoot, "DestinationMarkerRoot was not created.");
                Assert.Greater(markerRoot.childCount, 0, "Destination marker content was not created.");
            }
            finally
            {
                DestroyImmediateIfExists(end);
                DestroyImmediateIfExists(start);
                DestroyImmediateIfExists(root);
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field {fieldName} does not exist.");
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method {methodName} does not exist.");
            method.Invoke(target, Array.Empty<object>());
        }

        private static void DestroyImmediateIfExists(UnityEngine.Object target)
        {
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
