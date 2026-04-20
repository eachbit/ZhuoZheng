using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter03NorthPagePickupTests
    {
        [Test]
        public void TryAwardChapter03Page_ShouldAwardOnceAfterNorthEchoPickup()
        {
            Type northType = Type.GetType("North, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Assert.IsNotNull(northType, "North was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "collectedPages", 2);

            MethodInfo method = northType.GetMethod("TryAwardChapter03Page", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "North should award the Chapter 03 page only after the returning page is picked up.");

            bool firstAwarded = (bool)method.Invoke(null, new[] { saveData, 5 });
            bool secondAwarded = (bool)method.Invoke(null, new[] { saveData, 5 });

            Assert.IsTrue(firstAwarded);
            Assert.IsFalse(secondAwarded);
            Assert.AreEqual(3, GetField(saveData, "collectedPages"));
            Assert.AreEqual(true, GetField(saveData, "chapter03PageCollected"));
        }

        [Test]
        public void TryResolveScholarGuideTarget_ShouldUseExplicitScholarTarget()
        {
            Type northType = Type.GetType("North, Assembly-CSharp");
            Assert.IsNotNull(northType, "North was not found.");

            GameObject scholar = new GameObject("shusheng");
            try
            {
                scholar.transform.position = new Vector3(-12f, 1.5f, 8f);

                MethodInfo method = northType.GetMethod("TryResolveScholarGuideTarget", BindingFlags.Static | BindingFlags.Public);
                Assert.IsNotNull(method, "North should expose the scholar route target resolver for tests.");

                object[] arguments = { scholar.transform, Vector3.zero };
                bool resolved = (bool)method.Invoke(null, arguments);
                Vector3 targetPosition = (Vector3)arguments[1];

                Assert.IsTrue(resolved);
                Assert.AreEqual(scholar.transform.position.x, targetPosition.x, 0.001f);
                Assert.AreEqual(scholar.transform.position.y, targetPosition.y, 0.001f);
                Assert.AreEqual(scholar.transform.position.z, targetPosition.z, 0.001f);
            }
            finally
            {
                DestroyImmediateIfExists(scholar);
            }
        }

        [Test]
        public void ResolveScholarRouteMarkers_ShouldCreateCurvedFallbackPoints()
        {
            Type northType = Type.GetType("North, Assembly-CSharp");
            Assert.IsNotNull(northType, "North was not found.");

            GameObject northObject = new GameObject("North");
            GameObject markerRoot = new GameObject("MarkerRoot");

            try
            {
                object north = northObject.AddComponent(northType);
                markerRoot.transform.SetParent(northObject.transform, false);

                MethodInfo method = northType.GetMethod("ResolveScholarRouteMarkers", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(method, "North should create adjustable route markers for the scholar guide.");

                Vector3 start = new Vector3(0f, 0f, 0f);
                Vector3 target = new Vector3(28f, 0f, 0f);
                Transform[] markers = (Transform[])method.Invoke(north, new object[] { start, target, markerRoot.transform });

                Assert.AreEqual(5, markers.Length, "The scholar fallback route should create five intermediate points for later manual editing.");
                Assert.IsTrue(HasPointOffStraightLine(markers, start, target), "The scholar guide should bend instead of using one straight line.");
            }
            finally
            {
                DestroyImmediateIfExists(markerRoot);
                DestroyImmediateIfExists(northObject);
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field {fieldName} does not exist.");
            field.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field {fieldName} does not exist.");
            return field.GetValue(target);
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
