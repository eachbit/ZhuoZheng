using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter02DirectorObjectiveTests
    {
        [Test]
        public void GlobalObjective_ShouldGuideToChapter01GatesBeforeWaterGateTriggered()
        {
            Type managerType = Type.GetType("ZhuozhengYuan.GardenGameManager, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Assert.IsNotNull(managerType, "GardenGameManager was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");

            object saveData = Activator.CreateInstance(saveDataType);

            Assert.AreEqual("chapter01-gates", InvokeResolveGlobalObjectiveText(managerType, saveData));
        }

        [Test]
        public void GlobalObjective_ShouldGuideToXiaoFeihongAfterWaterGateTriggered()
        {
            Type managerType = Type.GetType("ZhuozhengYuan.GardenGameManager, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Assert.IsNotNull(managerType, "GardenGameManager was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "selectedFlowDirection", "center");

            Assert.AreEqual("xiao-feihong", InvokeResolveGlobalObjectiveText(managerType, saveData));
        }

        [Test]
        public void GlobalObjective_ShouldGuideToYuanyangHallAfterChapter02Completed()
        {
            Type managerType = Type.GetType("ZhuozhengYuan.GardenGameManager, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Type chapter02StateType = Type.GetType("ZhuozhengYuan.Chapter02State, Assembly-CSharp");
            Assert.IsNotNull(managerType, "GardenGameManager was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");
            Assert.IsNotNull(chapter02StateType, "Chapter02State was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "selectedFlowDirection", "center");
            SetField(saveData, "chapter02State", Enum.Parse(chapter02StateType, "Completed"));

            Assert.AreEqual("yuanyang-hall", InvokeResolveGlobalObjectiveText(managerType, saveData));
        }

        [Test]
        public void GlobalObjective_ShouldGuideToWithWhomSitPavilionAfterChapter03PageCollected()
        {
            Type managerType = Type.GetType("ZhuozhengYuan.GardenGameManager, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Type chapter02StateType = Type.GetType("ZhuozhengYuan.Chapter02State, Assembly-CSharp");
            Assert.IsNotNull(managerType, "GardenGameManager was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");
            Assert.IsNotNull(chapter02StateType, "Chapter02State was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "selectedFlowDirection", "center");
            SetField(saveData, "chapter02State", Enum.Parse(chapter02StateType, "Completed"));
            SetField(saveData, "chapter03PageCollected", true);

            Assert.AreEqual("with-whom-sit", InvokeResolveGlobalObjectiveText(managerType, saveData));
        }

        [Test]
        public void GlobalObjective_ShouldGuideToJianshanLouWhenFourPagesCollected()
        {
            Type managerType = Type.GetType("ZhuozhengYuan.GardenGameManager, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Assert.IsNotNull(managerType, "GardenGameManager was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "chapter03PageCollected", true);
            SetField(saveData, "chapter04PageCollected", true);
            SetField(saveData, "collectedPages", 4);

            Assert.AreEqual("jianshan-lou", InvokeResolveGlobalObjectiveText(managerType, saveData));
        }

        [Test]
        public void GlobalObjective_ShouldGuideToXuexiangYunweiWhenFivePagesCollected()
        {
            Type managerType = Type.GetType("ZhuozhengYuan.GardenGameManager, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Assert.IsNotNull(managerType, "GardenGameManager was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "chapter03PageCollected", true);
            SetField(saveData, "chapter04PageCollected", true);
            SetField(saveData, "chapter05PageCollected", true);
            SetField(saveData, "collectedPages", 5);

            Assert.AreEqual("xuexiang-yunwei", InvokeResolveGlobalObjectiveText(managerType, saveData));
        }

        [Test]
        public void ReachTriggerObjective_ShouldStayHiddenBeforeBothGatesAndFlowInteraction()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter02Director, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter02Director was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "leftGateOpened", true);
            SetField(saveData, "rightGateOpened", false);
            SetField(saveData, "selectedFlowDirection", string.Empty);

            Assert.IsFalse(InvokeShouldShowReachTriggerObjective(directorType, saveData));

            SetField(saveData, "rightGateOpened", true);
            Assert.IsFalse(InvokeShouldShowReachTriggerObjective(directorType, saveData));
        }

        [Test]
        public void ReachTriggerObjective_ShouldAppearAfterBothGatesAndFlowInteraction()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter02Director, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter02Director was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "leftGateOpened", true);
            SetField(saveData, "rightGateOpened", true);
            SetField(saveData, "selectedFlowDirection", "center");

            Assert.IsTrue(InvokeShouldShowReachTriggerObjective(directorType, saveData));
        }

        [Test]
        public void TryAwardChapter02Page_ShouldAwardOnceAndMarkSaveData()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter02Director, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter02Director was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "collectedPages", 1);

            bool firstAwarded = InvokeTryAwardChapter02Page(directorType, saveData, 5);
            bool secondAwarded = InvokeTryAwardChapter02Page(directorType, saveData, 5);

            Assert.IsTrue(firstAwarded);
            Assert.IsFalse(secondAwarded);
            Assert.AreEqual(2, GetField(saveData, "collectedPages"));
            Assert.AreEqual(true, GetField(saveData, "chapter02PageCollected"));
        }

        [Test]
        public void DefaultQuestionBank_ShouldFocusOnXiaoFeihongArchitecturalCulture()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter02Director, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter02Director was not found.");

            Array questionBank = InvokeCreateDefaultQuestionBank(directorType);

            Assert.GreaterOrEqual(questionBank.Length, 8);
            AssertQuestionBankContains(questionBank, "\u5c0f\u98de\u8679");
            AssertQuestionBankContains(questionBank, "\u5eca\u6865");
            AssertQuestionBankContains(questionBank, "\u62d9\u653f\u56ed\u4e2d\u90e8");
            AssertQuestionBankContains(questionBank, "\u6c5f\u5357\u6c34\u4e61");
            AssertQuestionBankContains(questionBank, "\u5012\u5f71\u5982\u8679");
            AssertQuestionBankContains(questionBank, "\u6e38\u7ebf");
            AssertQuestionBankContains(questionBank, "\u5386\u53f2\u56ed\u6797");
        }

        [Test]
        public void TryResolveChapter03GuideTarget_ShouldUseSouthFireParticlePosition()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter02Director, Assembly-CSharp");
            Type southType = Type.GetType("South, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter02Director was not found.");
            Assert.IsNotNull(southType, "South was not found.");

            GameObject southObject = new GameObject("South");
            GameObject fireObject = new GameObject("FireParticle", typeof(ParticleSystem));

            try
            {
                Component south = southObject.AddComponent(southType);
                fireObject.transform.position = new Vector3(8f, 1.5f, -3f);
                southType.GetField("fireParticleSystem", BindingFlags.Instance | BindingFlags.Public)
                    .SetValue(south, fireObject.GetComponent<ParticleSystem>());

                MethodInfo method = directorType.GetMethod("TryResolveChapter03GuideTarget", BindingFlags.Static | BindingFlags.Public);
                Assert.IsNotNull(method, "TryResolveChapter03GuideTarget should stay public for route target tests.");

                object[] arguments = { south, Vector3.zero };
                bool resolved = (bool)method.Invoke(null, arguments);
                Vector3 targetPosition = (Vector3)arguments[1];

                Assert.IsTrue(resolved, "Chapter 3 guide target should resolve from the South fire particle.");
                Assert.AreEqual(fireObject.transform.position.x, targetPosition.x, 0.001f);
                Assert.AreEqual(fireObject.transform.position.y, targetPosition.y, 0.001f);
                Assert.AreEqual(fireObject.transform.position.z, targetPosition.z, 0.001f);
            }
            finally
            {
                DestroyImmediateIfExists(fireObject);
                DestroyImmediateIfExists(southObject);
            }
        }

        [Test]
        public void ResolveChapter03RouteMarkers_ShouldCreateCurvedFallbackPoints()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter02Director, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter02Director was not found.");

            GameObject directorObject = new GameObject("Chapter02Director");
            GameObject markerRoot = new GameObject("MarkerRoot");

            try
            {
                object director = directorObject.AddComponent(directorType);
                markerRoot.transform.SetParent(directorObject.transform, false);

                MethodInfo method = directorType.GetMethod("ResolveChapter03RouteMarkers", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(method, "ResolveChapter03RouteMarkers should exist for the post-quiz guide.");

                Vector3 start = new Vector3(0f, 0f, 0f);
                Vector3 target = new Vector3(24f, 0f, 0f);
                Transform[] markers = (Transform[])method.Invoke(director, new object[] { start, target, markerRoot.transform });

                Assert.AreEqual(5, markers.Length, "The fallback route should use five intermediate guide points for later obstacle authoring.");
                Assert.IsTrue(HasPointOffStraightLine(markers, start, target), "The fallback points should bend the guide instead of forming one straight line.");
            }
            finally
            {
                DestroyImmediateIfExists(markerRoot);
                DestroyImmediateIfExists(directorObject);
            }
        }

        private static bool InvokeShouldShowReachTriggerObjective(Type directorType, object saveData)
        {
            MethodInfo method = directorType.GetMethod("ShouldShowReachTriggerObjective", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "ShouldShowReachTriggerObjective was not found.");
            return (bool)method.Invoke(null, new[] { saveData });
        }

        private static string InvokeResolveGlobalObjectiveText(Type managerType, object saveData)
        {
            MethodInfo method = managerType.GetMethod("ResolveGlobalObjectiveText", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(method, "ResolveGlobalObjectiveText should stay public for objective flow tests.");
            return (string)method.Invoke(null, new[]
            {
                saveData,
                "chapter01-gates",
                "xiao-feihong",
                "yuanyang-hall",
                "with-whom-sit",
                "jianshan-lou",
                "xuexiang-yunwei",
                "project-completed"
            });
        }

        private static bool InvokeTryAwardChapter02Page(Type directorType, object saveData, int totalPages)
        {
            MethodInfo method = directorType.GetMethod("TryAwardChapter02Page", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "TryAwardChapter02Page was not found.");
            return (bool)method.Invoke(null, new[] { saveData, totalPages });
        }

        private static Array InvokeCreateDefaultQuestionBank(Type directorType)
        {
            MethodInfo method = directorType.GetMethod("CreateDefaultQuestionBank", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "CreateDefaultQuestionBank was not found.");
            return method.Invoke(null, null) as Array;
        }

        private static void AssertQuestionBankContains(Array questionBank, string expectedText)
        {
            foreach (object question in questionBank)
            {
                if (Contains(GetField(question, "questionText"), expectedText)
                    || Contains(GetField(question, "correctFeedback"), expectedText)
                    || Contains(GetField(question, "wrongFeedback"), expectedText)
                    || OptionsContain(GetField(question, "options") as string[], expectedText))
                {
                    return;
                }
            }

            Assert.Fail("Question bank should contain architectural culture text: " + expectedText);
        }

        private static bool OptionsContain(string[] options, string expectedText)
        {
            if (options == null)
            {
                return false;
            }

            for (int index = 0; index < options.Length; index++)
            {
                if (Contains(options[index], expectedText))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(object value, string expectedText)
        {
            string text = value as string;
            return !string.IsNullOrEmpty(text) && text.Contains(expectedText);
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
