using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter06DirectorObjectiveTests
    {
        [Test]
        public void DefaultQuestionsRequiredToUnlock_ShouldRequireSixAnswers()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter06Director, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter06Director was not found.");

            GameObject trigger = new GameObject("Chapter06Trigger");
            trigger.AddComponent<BoxCollider>();
            object director = trigger.AddComponent(directorType);

            Assert.AreEqual(6, GetField(director, "questionsRequiredToUnlock"));

            UnityEngine.Object.DestroyImmediate(trigger);
        }

        [Test]
        public void FinaleObjective_ShouldStayHiddenBeforeQuizIsComplete()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter06Director, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Type stateType = Type.GetType("ZhuozhengYuan.Chapter06State, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter06Director was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");
            Assert.IsNotNull(stateType, "Chapter06State was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            object inProgress = Enum.Parse(stateType, "InProgress");
            SetField(saveData, "chapter06State", inProgress);

            Assert.IsFalse(InvokeShouldShowFinaleObjective(directorType, saveData));
        }

        [Test]
        public void FinaleObjective_ShouldAppearAfterQuizIsCompleteAndBeforeFinaleView()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter06Director, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Type stateType = Type.GetType("ZhuozhengYuan.Chapter06State, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter06Director was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");
            Assert.IsNotNull(stateType, "Chapter06State was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            object awaitingFinalView = Enum.Parse(stateType, "AwaitingFinalView");
            SetField(saveData, "chapter06State", awaitingFinalView);
            SetField(saveData, "chapter06FinaleViewed", false);

            Assert.IsTrue(InvokeShouldShowFinaleObjective(directorType, saveData));
        }

        [Test]
        public void TryCompleteFinale_ShouldMarkSaveDataOnce()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter06Director, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Type stateType = Type.GetType("ZhuozhengYuan.Chapter06State, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter06Director was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");
            Assert.IsNotNull(stateType, "Chapter06State was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            object awaitingFinalView = Enum.Parse(stateType, "AwaitingFinalView");
            SetField(saveData, "chapter06State", awaitingFinalView);

            bool firstCompleted = InvokeTryCompleteFinale(directorType, saveData);
            bool secondCompleted = InvokeTryCompleteFinale(directorType, saveData);

            object completed = Enum.Parse(stateType, "Completed");
            Assert.IsTrue(firstCompleted);
            Assert.IsFalse(secondCompleted);
            Assert.AreEqual(completed, GetField(saveData, "chapter06State"));
            Assert.AreEqual(true, GetField(saveData, "chapter06FinaleViewed"));
        }

        [Test]
        public void DefaultFinaleDialogueLines_ShouldUseRequestedSystemPrompt()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter06Director, Assembly-CSharp");
            Type dialogueLineType = Type.GetType("ZhuozhengYuan.DialogueLine, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter06Director was not found.");
            Assert.IsNotNull(dialogueLineType, "DialogueLine was not found.");

            Array lines = InvokeCreateFinaleDialogueLines(directorType);

            Assert.AreEqual(3, lines.Length);
            Assert.AreEqual("系统提示", GetField(lines.GetValue(0), "speaker"));
            Assert.AreEqual("水动、影合、声传、雨观、山续。", GetField(lines.GetValue(0), "text"));
            Assert.AreEqual("子之所为，非修园也。", GetField(lines.GetValue(1), "text"));
            Assert.AreEqual("乃以己心，印古人之心。", GetField(lines.GetValue(2), "text"));
        }

        [Test]
        public void DefaultFinaleMusicClip_ShouldProvideGentleRuntimeBgm()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter06Director, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter06Director was not found.");

            AudioClip clip = InvokeCreateFinaleMusicClip(directorType, 22050, 18f);

            Assert.IsNotNull(clip);
            Assert.AreEqual("Chapter06_FinalElegantBgm_Runtime", clip.name);
            Assert.GreaterOrEqual(clip.length, 17.9f);
            Assert.AreEqual(1, clip.channels);
        }

        [Test]
        public void DefaultFinaleHistoryBody_ShouldSummarizeZhuozhengGardenHistory()
        {
            Type directorType = Type.GetType("ZhuozhengYuan.Chapter06Director, Assembly-CSharp");
            Assert.IsNotNull(directorType, "Chapter06Director was not found.");

            string title = InvokeStringMethod(directorType, "CreateFinaleHistoryTitle");
            string body = InvokeStringMethod(directorType, "CreateFinaleHistoryBody");

            Assert.AreEqual("拙政园", title);
            StringAssert.Contains("明正德四年", body);
            StringAssert.Contains("1509", body);
            StringAssert.Contains("王献臣", body);
            StringAssert.Contains("潘岳《闲居赋》", body);
            StringAssert.Contains("清末形成东、中、西", body);
            StringAssert.Contains("1961", body);
            StringAssert.Contains("1997", body);
            StringAssert.Contains("世界文化遗产", body);
        }

        private static bool InvokeShouldShowFinaleObjective(Type directorType, object saveData)
        {
            MethodInfo method = directorType.GetMethod("ShouldShowFinaleObjective", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "ShouldShowFinaleObjective was not found.");
            return (bool)method.Invoke(null, new[] { saveData });
        }

        private static bool InvokeTryCompleteFinale(Type directorType, object saveData)
        {
            MethodInfo method = directorType.GetMethod("TryCompleteFinale", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "TryCompleteFinale was not found.");
            return (bool)method.Invoke(null, new[] { saveData });
        }

        private static Array InvokeCreateFinaleDialogueLines(Type directorType)
        {
            MethodInfo method = directorType.GetMethod("CreateFinaleDialogueLines", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "CreateFinaleDialogueLines was not found.");
            return method.Invoke(null, null) as Array;
        }

        private static AudioClip InvokeCreateFinaleMusicClip(Type directorType, int sampleRate, float durationSeconds)
        {
            MethodInfo method = directorType.GetMethod("CreateFinaleMusicClip", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "CreateFinaleMusicClip was not found.");
            return method.Invoke(null, new object[] { sampleRate, durationSeconds }) as AudioClip;
        }

        private static string InvokeStringMethod(Type directorType, string methodName)
        {
            MethodInfo method = directorType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName + " was not found.");
            return method.Invoke(null, null) as string;
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
