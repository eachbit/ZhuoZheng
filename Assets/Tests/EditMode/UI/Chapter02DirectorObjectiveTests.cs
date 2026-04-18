using System;
using System.Reflection;
using NUnit.Framework;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter02DirectorObjectiveTests
    {
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

        private static bool InvokeShouldShowReachTriggerObjective(Type directorType, object saveData)
        {
            MethodInfo method = directorType.GetMethod("ShouldShowReachTriggerObjective", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "ShouldShowReachTriggerObjective was not found.");
            return (bool)method.Invoke(null, new[] { saveData });
        }

        private static bool InvokeTryAwardChapter02Page(Type directorType, object saveData, int totalPages)
        {
            MethodInfo method = directorType.GetMethod("TryAwardChapter02Page", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "TryAwardChapter02Page was not found.");
            return (bool)method.Invoke(null, new[] { saveData, totalPages });
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
    }
}
