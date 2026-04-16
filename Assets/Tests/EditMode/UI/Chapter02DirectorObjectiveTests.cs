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
            Assert.IsNotNull(directorType, "Chapter02Director 尚未创建。");
            Assert.IsNotNull(saveDataType, "SaveData 尚未创建。");

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
            Assert.IsNotNull(directorType, "Chapter02Director 尚未创建。");
            Assert.IsNotNull(saveDataType, "SaveData 尚未创建。");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "leftGateOpened", true);
            SetField(saveData, "rightGateOpened", true);
            SetField(saveData, "selectedFlowDirection", "center");

            Assert.IsTrue(InvokeShouldShowReachTriggerObjective(directorType, saveData));
        }

        private static bool InvokeShouldShowReachTriggerObjective(Type directorType, object saveData)
        {
            MethodInfo method = directorType.GetMethod("ShouldShowReachTriggerObjective", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "ShouldShowReachTriggerObjective 不存在。");
            return (bool)method.Invoke(null, new[] { saveData });
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"字段 {fieldName} 不存在。");
            field.SetValue(target, value);
        }
    }
}
