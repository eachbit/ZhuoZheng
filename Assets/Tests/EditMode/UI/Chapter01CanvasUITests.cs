using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter01CanvasUITests
    {
        [Test]
        public void SetObjective_ShouldWriteObjectiveText()
        {
            Type uiType = Type.GetType("ZhuozhengYuan.Chapter01CanvasUI, Assembly-CSharp");
            Assert.IsNotNull(uiType, "Chapter01CanvasUI 尚未创建。");

            GameObject root = new GameObject("UI");
            MonoBehaviour ui = (MonoBehaviour)root.AddComponent(uiType);
            GameObject objectivePanel = new GameObject("ObjectivePanel");
            TextMeshProUGUI objectiveText = objectivePanel.AddComponent<TextMeshProUGUI>();
            SetField(ui, "objectivePanel", objectivePanel);
            SetField(ui, "objectiveText", objectiveText);

            Invoke(ui, "SetObjective", "前往廊桥尽头");

            Assert.AreEqual("前往廊桥尽头", objectiveText.text);

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(objectivePanel);
        }

        [Test]
        public void ShowGateCalibration_ShouldDisplayPanelAndAngles()
        {
            Type uiType = Type.GetType("ZhuozhengYuan.Chapter01CanvasUI, Assembly-CSharp");
            Type dataType = Type.GetType("ZhuozhengYuan.Chapter01GateCalibrationViewData, Assembly-CSharp");
            Assert.IsNotNull(uiType, "Chapter01CanvasUI 尚未创建。");
            Assert.IsNotNull(dataType, "Chapter01GateCalibrationViewData 尚未创建。");

            GameObject root = new GameObject("UI");
            MonoBehaviour ui = (MonoBehaviour)root.AddComponent(uiType);
            GameObject gatePanel = new GameObject("GatePanel");
            TextMeshProUGUI titleText = gatePanel.AddComponent<TextMeshProUGUI>();
            TextMeshProUGUI angleText = new GameObject("CurrentAngle").AddComponent<TextMeshProUGUI>();
            TextMeshProUGUI targetText = new GameObject("TargetRange").AddComponent<TextMeshProUGUI>();
            TextMeshProUGUI hintText = new GameObject("Hint").AddComponent<TextMeshProUGUI>();
            SetField(ui, "gateCalibrationPanel", gatePanel);
            SetField(ui, "gateCalibrationTitleText", titleText);
            SetField(ui, "gateCalibrationCurrentAngleText", angleText);
            SetField(ui, "gateCalibrationTargetRangeText", targetText);
            SetField(ui, "gateCalibrationHintText", hintText);

            object data = Activator.CreateInstance(dataType);
            SetField(data, "gateName", "左暗闸");
            SetField(data, "currentAngle", 40f);
            SetField(data, "targetAngle", 55f);
            SetField(data, "validAngleTolerance", 9f);
            SetField(data, "canConfirm", false);
            SetField(data, "rotationHint", "请继续向右旋转");
            SetField(data, "negativeKey", KeyCode.A);
            SetField(data, "positiveKey", KeyCode.D);
            SetField(data, "confirmKey", KeyCode.E);
            SetField(data, "cancelKey", KeyCode.Escape);

            Invoke(ui, "ShowGateCalibration", data);

            Assert.IsTrue(gatePanel.activeSelf);
            StringAssert.Contains("左暗闸", titleText.text);
            StringAssert.Contains("40", angleText.text);
            StringAssert.Contains("46", targetText.text);
            StringAssert.Contains("请继续向右旋转", hintText.text);

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(gatePanel);
            UnityEngine.Object.DestroyImmediate(angleText.gameObject);
            UnityEngine.Object.DestroyImmediate(targetText.gameObject);
            UnityEngine.Object.DestroyImmediate(hintText.gameObject);
        }

        [Test]
        public void CreateDefault_ShouldKeepPromptAndGateTextInsidePanels()
        {
            Type uiType = Type.GetType("ZhuozhengYuan.Chapter01CanvasUI, Assembly-CSharp");
            Assert.IsNotNull(uiType, "Chapter01CanvasUI 尚未创建。");

            MonoBehaviour ui = (MonoBehaviour)InvokeStatic(uiType, "CreateDefault");

            AssertTextRectIsInset(ui, "interactionPromptText");
            AssertTextRectIsInset(ui, "gateCalibrationTitleText");
            AssertTextRectIsInset(ui, "gateCalibrationHintText");
            AssertTextRectIsInset(ui, "gateCalibrationControlsText");

            UnityEngine.Object.DestroyImmediate(ui.gameObject);
        }

        [Test]
        public void CreateDefault_ShouldUseLargeReadableChapter01Layout()
        {
            Type uiType = Type.GetType("ZhuozhengYuan.Chapter01CanvasUI, Assembly-CSharp");
            Assert.IsNotNull(uiType, "Chapter01CanvasUI 尚未创建。");

            MonoBehaviour ui = (MonoBehaviour)InvokeStatic(uiType, "CreateDefault");

            AssertPanelSizeAtLeast(ui, "pageCounterPanel", 330f, 100f);
            AssertPanelSizeAtLeast(ui, "objectivePanel", 780f, 140f);
            AssertPanelSizeAtLeast(ui, "interactionPromptPanel", 920f, 110f);
            AssertPanelSizeAtLeast(ui, "dialoguePanel", 1320f, 330f);
            AssertPanelSizeAtLeast(ui, "gateCalibrationPanel", 1320f, 430f);
            AssertFontSizeAtLeast(ui, "pageCounterText", 46f);
            AssertFontSizeAtLeast(ui, "objectiveText", 32f);
            AssertFontSizeAtLeast(ui, "interactionPromptText", 40f);
            AssertFontSizeAtLeast(ui, "gateCalibrationTitleText", 54f);
            AssertFontSizeAtLeast(ui, "gateCalibrationHintText", 44f);

            UnityEngine.Object.DestroyImmediate(ui.gameObject);
        }

        [Test]
        public void CreateDefault_ShouldBuildReadableChapter02QuizLayout()
        {
            Type uiType = Type.GetType("ZhuozhengYuan.Chapter01CanvasUI, Assembly-CSharp");
            Assert.IsNotNull(uiType, "Chapter01CanvasUI 尚未创建。");

            MonoBehaviour ui = (MonoBehaviour)InvokeStatic(uiType, "CreateDefault");

            Assert.IsInstanceOf(Type.GetType("ZhuozhengYuan.IChapter02QuizPresenter, Assembly-CSharp"), ui);
            AssertPanelSizeAtLeast(ui, "chapter02QuizPanel", 1160f, 600f);
            AssertFontSizeAtLeast(ui, "chapter02QuizTitleText", 48f);
            AssertFontSizeAtLeast(ui, "chapter02QuizQuestionText", 34f);

            Array optionTexts = GetField(ui, "chapter02QuizOptionTexts") as Array;
            Assert.IsNotNull(optionTexts, "chapter02QuizOptionTexts 不存在。");
            Assert.GreaterOrEqual(optionTexts.Length, 4, "第二章需要 4 个答案按钮。");
            TextMeshProUGUI firstOptionText = optionTexts.GetValue(0) as TextMeshProUGUI;
            Assert.IsNotNull(firstOptionText, "第一个答案文字不存在。");
            Assert.GreaterOrEqual(firstOptionText.fontSize, 32f, "第二章答案按钮字体不能小于 32。");

            UnityEngine.Object.DestroyImmediate(ui.gameObject);
        }

        [Test]
        public void ShowChapter02Quiz_ShouldPopulateFormalQuizPanel()
        {
            Type uiType = Type.GetType("ZhuozhengYuan.Chapter01CanvasUI, Assembly-CSharp");
            Assert.IsNotNull(uiType, "Chapter01CanvasUI 尚未创建。");

            MonoBehaviour ui = (MonoBehaviour)InvokeStatic(uiType, "CreateDefault");
            string[] options = { "A", "B", "C", "D" };

            Invoke(ui, "ShowChapter02Quiz", "第二章", "答题进度 1/4", "小飞虹连接哪两段水廊？", options, null);

            GameObject panel = GetField(ui, "chapter02QuizPanel") as GameObject;
            TextMeshProUGUI titleText = GetField(ui, "chapter02QuizTitleText") as TextMeshProUGUI;
            TextMeshProUGUI progressText = GetField(ui, "chapter02QuizProgressText") as TextMeshProUGUI;
            TextMeshProUGUI questionText = GetField(ui, "chapter02QuizQuestionText") as TextMeshProUGUI;
            Array optionTexts = GetField(ui, "chapter02QuizOptionTexts") as Array;
            TextMeshProUGUI firstOptionText = optionTexts.GetValue(0) as TextMeshProUGUI;

            Assert.IsTrue(panel.activeSelf);
            Assert.AreEqual("第二章", titleText.text);
            Assert.AreEqual("答题进度 1/4", progressText.text);
            StringAssert.Contains("小飞虹", questionText.text);
            StringAssert.Contains("1. A", firstOptionText.text);

            UnityEngine.Object.DestroyImmediate(ui.gameObject);
        }

        [Test]
        public void ShowPageReward_ShouldDisplaySecondPageRewardPanel()
        {
            Type uiType = Type.GetType("ZhuozhengYuan.Chapter01CanvasUI, Assembly-CSharp");
            Assert.IsNotNull(uiType, "Chapter01CanvasUI was not found.");

            MonoBehaviour ui = (MonoBehaviour)InvokeStatic(uiType, "CreateDefault");

            Invoke(ui, "ShowPageReward", "获得残页", "已获得《长物志》第二张残页", 3.4f);

            GameObject panel = GetField(ui, "pageRewardPanel") as GameObject;
            TextMeshProUGUI titleText = GetField(ui, "pageRewardTitleText") as TextMeshProUGUI;
            TextMeshProUGUI bodyText = GetField(ui, "pageRewardBodyText") as TextMeshProUGUI;

            Assert.IsTrue(panel.activeSelf);
            Assert.AreEqual("获得残页", titleText.text);
            StringAssert.Contains("第二张残页", bodyText.text);

            UnityEngine.Object.DestroyImmediate(ui.gameObject);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"字段 {fieldName} 不存在。");
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"方法 {methodName} 不存在。");
            method.Invoke(target, args);
        }

        private static object InvokeStatic(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"静态方法 {methodName} 不存在。");
            return method.Invoke(null, new object[0]);
        }

        private static object GetField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"字段 {fieldName} 不存在。");
            return field.GetValue(target);
        }

        private static void AssertTextRectIsInset(object ui, string fieldName)
        {
            TextMeshProUGUI text = GetField(ui, fieldName) as TextMeshProUGUI;
            Assert.IsNotNull(text, $"{fieldName} 不存在。");

            RectTransform rectTransform = text.GetComponent<RectTransform>();
            Assert.IsNotNull(rectTransform, $"{fieldName} 缺少 RectTransform。");
            Assert.GreaterOrEqual(rectTransform.offsetMin.y, 0f, $"{fieldName} 底部内边距不能为负，否则文字会偏出框体。");
            Assert.LessOrEqual(rectTransform.offsetMax.y, 0f, $"{fieldName} 顶部内边距不能为正，否则文字会偏出框体。");
        }

        private static void AssertPanelSizeAtLeast(object ui, string fieldName, float minWidth, float minHeight)
        {
            GameObject panel = GetField(ui, fieldName) as GameObject;
            Assert.IsNotNull(panel, $"{fieldName} 不存在。");

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            Assert.IsNotNull(rectTransform, $"{fieldName} 缺少 RectTransform。");
            Assert.GreaterOrEqual(rectTransform.sizeDelta.x, minWidth, $"{fieldName} 宽度过小。");
            Assert.GreaterOrEqual(rectTransform.sizeDelta.y, minHeight, $"{fieldName} 高度过小。");
        }

        private static void AssertFontSizeAtLeast(object ui, string fieldName, float minFontSize)
        {
            TextMeshProUGUI text = GetField(ui, fieldName) as TextMeshProUGUI;
            Assert.IsNotNull(text, $"{fieldName} 不存在。");
            Assert.GreaterOrEqual(text.fontSize, minFontSize, $"{fieldName} 字体过小。");
        }
    }
}
