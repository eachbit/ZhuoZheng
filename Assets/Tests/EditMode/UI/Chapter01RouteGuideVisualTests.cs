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
        public void RebuildGuide_ShouldUseNarrowRibbonWidths()
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
                start.transform.position = Vector3.zero;
                end.transform.position = new Vector3(8f, 0f, 0f);

                SetField(guide, "showGuideOnStart", true);
                SetField(guide, "playerStartPose", start.transform);
                SetField(guide, "targetGate", end.transform);
                SetField(guide, "trimSegmentsAgainstObstacles", false);
                SetField(guide, "groundOffset", 0f);

                Invoke(guide, "RebuildGuide");

                Transform segment = root.transform.Find("Chapter01AuthoredGuideRoot/GuideSegment_00");
                Assert.IsNotNull(segment, "GuideSegment_00 was not created.");

                Assert.LessOrEqual(segment.Find("Glow").localScale.x, 1.65f, "Glow strip should be narrower than the previous broad guide.");
                Assert.LessOrEqual(segment.Find("Main").localScale.x, 0.75f, "Main strip should stay visually slim.");
                Assert.LessOrEqual(segment.Find("Crest").localScale.x, 0.2f, "Crest strip should remain a thin highlight.");
            }
            finally
            {
                DestroyImmediateIfExists(end);
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

        [Test]
        public void HideGuide_ShouldFadeDestinationMarkerContent()
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
                end.transform.position = new Vector3(10f, 0f, 6f);

                SetField(guide, "showGuideOnStart", true);
                SetField(guide, "playerStartPose", start.transform);
                SetField(guide, "targetGate", end.transform);
                SetField(guide, "trimSegmentsAgainstObstacles", false);
                SetField(guide, "groundOffset", 0f);

                Invoke(guide, "RebuildGuide");

                Transform marker = root.transform.Find("Chapter01AuthoredGuideRoot/DestinationMarkerRoot/DestinationMarker_Ring");
                Assert.IsNotNull(marker, "Destination marker ring was not created.");

                MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
                Assert.IsNotNull(renderer, "Destination marker renderer is missing.");

                float beforeAlpha = ReadRendererAlpha(renderer);
                Invoke(guide, "HideGuide");
                InvokeWithArgs(guide, "ApplyGuideFade", 0.5f);
                float afterAlpha = ReadRendererAlpha(renderer);

                Assert.Less(afterAlpha, beforeAlpha, "Destination marker alpha should be reduced after guide fade.");
            }
            finally
            {
                DestroyImmediateIfExists(end);
                DestroyImmediateIfExists(start);
                DestroyImmediateIfExists(root);
            }
        }

        [Test]
        public void TryResolveChapter02GuideTarget_ShouldUseTriggerColliderCenter()
        {
            Type chapter01DirectorType = Type.GetType("ZhuozhengYuan.Chapter01Director, Assembly-CSharp");
            Type chapter02DirectorType = Type.GetType("ZhuozhengYuan.Chapter02Director, Assembly-CSharp");
            Assert.IsNotNull(chapter01DirectorType, "Chapter01Director is still missing.");
            Assert.IsNotNull(chapter02DirectorType, "Chapter02Director is still missing.");

            GameObject target = new GameObject("Chapter02Trigger");

            try
            {
                BoxCollider trigger = target.AddComponent<BoxCollider>();
                MonoBehaviour chapter02Director = (MonoBehaviour)target.AddComponent(chapter02DirectorType);
                target.transform.position = new Vector3(4f, 1f, 6f);
                trigger.center = new Vector3(2f, 0f, -1f);

                MethodInfo method = chapter01DirectorType.GetMethod("TryResolveChapter02GuideTarget", BindingFlags.Static | BindingFlags.Public);
                Assert.IsNotNull(method, "TryResolveChapter02GuideTarget should stay public for route target tests.");

                object[] arguments = { chapter02Director, Vector3.zero };
                bool resolved = (bool)method.Invoke(null, arguments);
                Vector3 targetPosition = (Vector3)arguments[1];

                Assert.IsTrue(resolved, "Chapter 2 guide target should resolve when the chapter 2 trigger exists.");
                Assert.AreEqual(trigger.bounds.center.x, targetPosition.x, 0.001f);
                Assert.AreEqual(trigger.bounds.center.y, targetPosition.y, 0.001f);
                Assert.AreEqual(trigger.bounds.center.z, targetPosition.z, 0.001f);
            }
            finally
            {
                DestroyImmediateIfExists(target);
            }
        }

        [Test]
        public void ResolveChapter02RouteMarkers_ShouldCreateFourCurvedFallbackPoints()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter01Director, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter01Director is still missing.");

            GameObject directorObject = new GameObject("Chapter01Director");
            GameObject markerRoot = new GameObject("MarkerRoot");

            try
            {
                MonoBehaviour director = (MonoBehaviour)directorObject.AddComponent(directorType);
                markerRoot.transform.SetParent(directorObject.transform, false);

                MethodInfo method = directorType.GetMethod("ResolveChapter02RouteMarkers", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(method, "ResolveChapter02RouteMarkers should exist for the post-page guide.");

                Vector3 start = new Vector3(0f, 0f, 0f);
                Vector3 target = new Vector3(20f, 0f, 0f);
                Transform[] markers = (Transform[])method.Invoke(director, new object[] { start, target, markerRoot.transform });

                Assert.AreEqual(4, markers.Length, "The fallback route should use four intermediate guide points.");
                Assert.IsTrue(HasPointOffStraightLine(markers, start, target), "The fallback points should bend the guide instead of forming one straight line.");
            }
            finally
            {
                DestroyImmediateIfExists(markerRoot);
                DestroyImmediateIfExists(directorObject);
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

        private static void InvokeWithArgs(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method {methodName} does not exist.");
            method.Invoke(target, args);
        }

        private static float ReadRendererAlpha(MeshRenderer renderer)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            Color propertyColor = propertyBlock.GetColor("_BaseColor");
            if (propertyColor.a > 0.001f || propertyColor.maxColorComponent > 0.001f)
            {
                return propertyColor.a;
            }

            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                return renderer.sharedMaterial.GetColor("_BaseColor").a;
            }

            return renderer.sharedMaterial != null ? renderer.sharedMaterial.color.a : 0f;
        }

        private static bool HasPointOffStraightLine(Transform[] markers, Vector3 start, Vector3 target)
        {
            Vector3 route = target - start;
            route.y = 0f;
            float routeLength = route.magnitude;
            if (routeLength < 0.001f)
            {
                return false;
            }

            Vector3 routeDirection = route / routeLength;
            for (int index = 0; index < markers.Length; index++)
            {
                Vector3 offset = markers[index].position - start;
                offset.y = 0f;
                Vector3 projected = routeDirection * Vector3.Dot(offset, routeDirection);
                if ((offset - projected).magnitude > 0.2f)
                {
                    return true;
                }
            }

            return false;
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
