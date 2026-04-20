using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter05JSLUITests
    {
        [Test]
        public void ApplyChapter05UIStyle_ShouldUsePlaqueFramesAndKeepFontsInsidePanels()
        {
            Type jslType = Type.GetType("JSL, Assembly-CSharp");
            Assert.IsNotNull(jslType, "JSL was not found.");

            GameObject root = new GameObject("JSLRoot");
            GameObject systemPanel = CreatePanel("SystemPromptPanel");
            GameObject systemTextObject = CreateTmpTextChild("SystemPromptText", systemPanel.transform);
            GameObject culturePanel = CreatePanel("CultureCardPanel");
            GameObject cultureTextObject = CreateTmpTextChild("CultureCardText", culturePanel.transform);
            GameObject hintPanel = CreatePanel("UIHintPanel");
            GameObject hintTextObject = CreateLegacyTextChild("UIHintText", hintPanel.transform);
            TMP_FontAsset originalTmpFont = ScriptableObject.CreateInstance<TMP_FontAsset>();
            Font originalLegacyFont = Font.CreateDynamicFontFromOSFont("Arial", 18);

            try
            {
                MonoBehaviour jsl = (MonoBehaviour)root.AddComponent(jslType);
                SetField(jsl, "systemPromptPanel", systemPanel);
                SetField(jsl, "systemPromptTextObject", systemTextObject);
                SetField(jsl, "cultureCardPanel", culturePanel);
                SetField(jsl, "uiHintPanel", hintPanel);
                SetField(jsl, "uiHintTextObject", hintTextObject);

                TextMeshProUGUI systemText = systemTextObject.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI cultureText = cultureTextObject.GetComponent<TextMeshProUGUI>();
                Text hintText = hintTextObject.GetComponent<Text>();
                systemText.font = originalTmpFont;
                cultureText.font = originalTmpFont;
                hintText.font = originalLegacyFont;

                Invoke(jsl, "ApplyChapter05UIStyle");

                AssertFrameChild(systemPanel.transform, "GoldFrameTop");
                AssertFrameChild(culturePanel.transform, "GoldFrameTop");
                AssertFrameChild(hintPanel.transform, "GoldFrameTop");

                RectTransform systemRect = systemPanel.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.24f, 0.55f), systemRect.anchorMin);
                Assert.AreEqual(new Vector2(0.76f, 0.75f), systemRect.anchorMax);

                RectTransform cultureRect = culturePanel.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.23f, 0.20f), cultureRect.anchorMin);
                Assert.AreEqual(new Vector2(0.77f, 0.68f), cultureRect.anchorMax);

                RectTransform hintRect = hintPanel.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.30f, 0.08f), hintRect.anchorMin);
                Assert.AreEqual(new Vector2(0.70f, 0.18f), hintRect.anchorMax);

                Assert.AreSame(originalTmpFont, systemText.font, "Chapter 05 styling should not replace the TMP font.");
                Assert.AreSame(originalTmpFont, cultureText.font, "Chapter 05 styling should not replace culture-card fonts.");
                Assert.AreSame(originalLegacyFont, hintText.font, "Chapter 05 styling should not replace legacy Text fonts.");

                Assert.IsTrue(systemText.enableWordWrapping);
                Assert.IsTrue(systemText.enableAutoSizing);
                Assert.AreEqual(TextOverflowModes.Truncate, systemText.overflowMode);
                AssertTextRectIsInset(systemText.rectTransform);

                Assert.IsTrue(cultureText.enableWordWrapping);
                Assert.IsTrue(cultureText.enableAutoSizing);
                Assert.AreEqual(TextOverflowModes.Truncate, cultureText.overflowMode);

                Assert.IsTrue(hintText.resizeTextForBestFit);
                Assert.AreEqual(HorizontalWrapMode.Wrap, hintText.horizontalOverflow);
                Assert.AreEqual(VerticalWrapMode.Truncate, hintText.verticalOverflow);
                AssertTextRectIsInset(hintText.rectTransform);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(originalTmpFont);
                UnityEngine.Object.DestroyImmediate(originalLegacyFont);
                UnityEngine.Object.DestroyImmediate(hintPanel);
                UnityEngine.Object.DestroyImmediate(culturePanel);
                UnityEngine.Object.DestroyImmediate(systemPanel);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DropChapter05PageAtLastTrigger_ShouldCreatePickupAndKeepEmptyCultureCardHidden()
        {
            Type jslType = Type.GetType("JSL, Assembly-CSharp");
            Assert.IsNotNull(jslType, "JSL was not found.");

            GameObject root = new GameObject("JSLRoot");
            GameObject culturePanel = CreatePanel("CultureCardPanel");
            GameObject hintPanel = CreatePanel("UIHintPanel");
            GameObject hintTextObject = CreateLegacyTextChild("UIHintText", hintPanel.transform);
            GameObject page = null;

            try
            {
                MonoBehaviour jsl = (MonoBehaviour)root.AddComponent(jslType);
                SetField(jsl, "cultureCardPanel", culturePanel);
                SetField(jsl, "uiHintPanel", hintPanel);
                SetField(jsl, "uiHintTextObject", hintTextObject);
                SetField(jsl, "lastTriggeredDirectionPosition", new Vector3(3f, 1f, -7f));
                SetField(jsl, "hasLastTriggeredDirectionPosition", true);

                culturePanel.SetActive(true);

                Invoke(jsl, "DropChapter05PageAtLastTrigger");

                page = (GameObject)GetField(jsl, "activeDroppedPage");
                Assert.IsNotNull(page, "Chapter 05 should create a dropped page after the fourth trigger.");
                Assert.AreEqual("Chapter05_DroppedPage", page.name);
                Assert.AreEqual(new Vector3(3f, 1f, -7f), page.transform.position);
                Assert.IsFalse(culturePanel.activeSelf, "The empty culture-card UI should stay hidden.");
                Assert.IsTrue(hintPanel.activeSelf, "The player should see the E pickup hint.");
            }
            finally
            {
                if (page != null)
                {
                    UnityEngine.Object.DestroyImmediate(page);
                }

                UnityEngine.Object.DestroyImmediate(hintPanel);
                UnityEngine.Object.DestroyImmediate(culturePanel);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TryAwardChapter05Page_ShouldIncrementFifthPageOnlyOnce()
        {
            Type jslType = Type.GetType("JSL, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Assert.IsNotNull(jslType, "JSL was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "collectedPages", 4);

            bool firstAwarded = InvokeTryAwardChapter05Page(jslType, saveData, 5);
            bool secondAwarded = InvokeTryAwardChapter05Page(jslType, saveData, 5);

            Assert.IsTrue(firstAwarded);
            Assert.IsFalse(secondAwarded);
            Assert.AreEqual(5, GetField(saveData, "collectedPages"));
            Assert.AreEqual(true, GetField(saveData, "chapter05PageCollected"));
        }

        [Test]
        public void ShowChapter06RouteGuide_ShouldUseChapter01WorldSpaceVisualStyle()
        {
            Type jslType = Type.GetType("JSL, Assembly-CSharp");
            Assert.IsNotNull(jslType, "JSL was not found.");

            GameObject jslObject = new GameObject("JSL");
            GameObject target = new GameObject("Chaper06_TestTrigger");
            Transform guideRoot = null;

            try
            {
                object jsl = jslObject.AddComponent(jslType);
                Vector3 pickupPosition = new Vector3(-141.5f, 3.2f, -119.8f);
                target.transform.position = new Vector3(-188.23f, 3.56f, -109.78f);

                SetField(jsl, "chapter06GuideTargetOverride", target.transform);
                SetField(jsl, "chapter06RouteGuideAutoPointCount", 5);

                MethodInfo method = jslType.GetMethod("ShowChapter06RouteGuide", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(method, "JSL should create the post-Chapter-5 route guide.");
                method.Invoke(jsl, new object[] { pickupPosition });

                guideRoot = GameObject.Find("Chapter05ToChapter06RouteGuide")?.transform;
                Assert.IsNotNull(guideRoot, "The Chapter 06 route guide root should be created.");
                Assert.IsNull(guideRoot.parent, "The Chapter 06 route guide should live in world space.");

                Transform runtimeRoot = guideRoot.Find("Chapter01AuthoredGuideRoot");
                Assert.IsNotNull(runtimeRoot, "The Chapter 06 route should reuse Chapter01AuthoredRouteGuide visuals.");
                Assert.IsNotNull(runtimeRoot.Find("DecorationsRoot"), "Chapter 1 style decorations should be present.");
                Assert.IsNotNull(runtimeRoot.Find("DestinationMarkerRoot"), "Chapter 1 style destination marker should be present.");
                Assert.IsNotNull(runtimeRoot.Find("GuideSegment_00"), "The guide should create visible Chapter 1 ribbon segments.");
            }
            finally
            {
                if (guideRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(guideRoot.gameObject);
                }

                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(jslObject);
            }
        }

        private static GameObject CreatePanel(string name)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(600f, 160f);
            return panel;
        }

        private static GameObject CreateTmpTextChild(string name, Transform parent)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            return textObject;
        }

        private static GameObject CreateLegacyTextChild(string name, Transform parent)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            return textObject;
        }

        private static void AssertFrameChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            Assert.IsNotNull(child, childName + " was not created.");

            Image image = child.GetComponent<Image>();
            Assert.IsNotNull(image, childName + " is missing Image.");
            Assert.IsFalse(image.raycastTarget, childName + " should not block UI interaction.");
        }

        private static void AssertTextRectIsInset(RectTransform rectTransform)
        {
            Assert.AreEqual(Vector2.zero, rectTransform.anchorMin);
            Assert.AreEqual(Vector2.one, rectTransform.anchorMax);
            Assert.GreaterOrEqual(rectTransform.offsetMin.x, 18f);
            Assert.GreaterOrEqual(rectTransform.offsetMin.y, 14f);
            Assert.LessOrEqual(rectTransform.offsetMax.x, -18f);
            Assert.LessOrEqual(rectTransform.offsetMax.y, -14f);
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

        private static bool InvokeTryAwardChapter05Page(Type jslType, object saveData, int totalPages)
        {
            MethodInfo method = jslType.GetMethod("TryAwardChapter05Page", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "TryAwardChapter05Page was not found.");
            return (bool)method.Invoke(null, new[] { saveData, totalPages });
        }

        private static object GetField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field {fieldName} does not exist.");
            return field.GetValue(target);
        }
    }
}
