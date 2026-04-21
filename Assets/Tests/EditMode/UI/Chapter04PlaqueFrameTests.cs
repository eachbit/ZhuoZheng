using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter04PlaqueFrameTests
    {
        [Test]
        public void ApplyPanel_ShouldAddChapter01StyleGoldFrameWithoutBlockingClicks()
        {
            Type frameType = Type.GetType("Chapter04PlaqueFrame, Assembly-CSharp");
            Assert.IsNotNull(frameType, "Chapter04PlaqueFrame was not found.");

            GameObject panel = new GameObject("Chapter04Panel", typeof(RectTransform), typeof(Image));

            try
            {
                InvokeStatic(frameType, "ApplyPanel", panel);

                AssertFrameChild(panel.transform, "GoldFrameTop");
                AssertFrameChild(panel.transform, "GoldFrameBottom");
                AssertFrameChild(panel.transform, "GoldFrameLeft");
                AssertFrameChild(panel.transform, "GoldFrameRight");
                AssertFrameChild(panel.transform, "GoldCornerUpperLeft");
                AssertFrameChild(panel.transform, "GoldCornerUpperRight");
                AssertFrameChild(panel.transform, "GoldCornerLowerLeft");
                AssertFrameChild(panel.transform, "GoldCornerLowerRight");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(panel);
            }
        }

        [Test]
        public void ChatSetupUIPositions_ShouldFramePanelsAndButtons()
        {
            Type chatType = Type.GetType("Chat, Assembly-CSharp");
            Assert.IsNotNull(chatType, "Chat was not found.");

            GameObject root = new GameObject("ChatRoot");
            GameObject dialoguePanel = CreateUiObject("DialoguePanel");
            GameObject choicesPanel = CreateUiObject("ChoicesPanel");
            GameObject systemPromptPanel = CreateUiObject("SystemPromptPanel");
            GameObject systemPromptText = CreateTextChild("SystemPromptText", systemPromptPanel.transform);
            GameObject itemGetPanel = CreateUiObject("ItemGetPanel");
            GameObject itemGetText = CreateTextChild("ItemGetText", itemGetPanel.transform);
            GameObject nextButton = CreateButton("NextButton");
            GameObject choiceButtonA = CreateButton("ChoiceButtonA");
            GameObject choiceButtonB = CreateButton("ChoiceButtonB");

            try
            {
                MonoBehaviour chat = (MonoBehaviour)root.AddComponent(chatType);
                SetField(chat, "dialoguePanel", dialoguePanel);
                SetField(chat, "choicesPanel", choicesPanel);
                SetField(chat, "systemPromptPanel", systemPromptPanel);
                SetField(chat, "systemPromptText", systemPromptText.GetComponent<TextMeshProUGUI>());
                SetField(chat, "itemGetPanel", itemGetPanel);
                SetField(chat, "itemGetText", itemGetText.GetComponent<TextMeshProUGUI>());
                SetField(chat, "nextButton", nextButton.GetComponent<Button>());
                SetField(chat, "choiceButtonA", choiceButtonA.GetComponent<Button>());
                SetField(chat, "choiceButtonB", choiceButtonB.GetComponent<Button>());

                Invoke(chat, "SetupUIPositions");

                AssertFrameChild(dialoguePanel.transform, "GoldFrameTop");
                AssertFrameChild(choicesPanel.transform, "GoldFrameTop");
                AssertFrameChild(systemPromptPanel.transform, "GoldFrameTop");
                AssertFrameChild(itemGetPanel.transform, "GoldFrameTop");
                AssertFrameChild(nextButton.transform, "GoldFrameTop");
                AssertFrameChild(choiceButtonA.transform, "GoldFrameTop");
                AssertFrameChild(choiceButtonB.transform, "GoldFrameTop");

                RectTransform dialogueRect = dialoguePanel.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.12f, 0.06f), dialogueRect.anchorMin);
                Assert.AreEqual(new Vector2(0.88f, 0.31f), dialogueRect.anchorMax);

                RectTransform systemRect = systemPromptPanel.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.18f, 0.68f), systemRect.anchorMin);
                Assert.AreEqual(new Vector2(0.82f, 0.88f), systemRect.anchorMax);

                TextMeshProUGUI systemText = systemPromptText.GetComponent<TextMeshProUGUI>();
                Assert.IsTrue(systemText.enableAutoSizing);
                Assert.AreEqual(TextOverflowModes.Truncate, systemText.overflowMode);

                RectTransform itemRect = itemGetPanel.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.24f, 0.38f), itemRect.anchorMin);
                Assert.AreEqual(new Vector2(0.76f, 0.62f), itemRect.anchorMax);

                TextMeshProUGUI itemText = itemGetText.GetComponent<TextMeshProUGUI>();
                Assert.IsTrue(itemText.enableAutoSizing);
                Assert.AreEqual(TextOverflowModes.Truncate, itemText.overflowMode);

                RectTransform nextRect = nextButton.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.72f, 0.08f), nextRect.anchorMin);
                Assert.AreEqual(new Vector2(0.90f, 0.24f), nextRect.anchorMax);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(choiceButtonB);
                UnityEngine.Object.DestroyImmediate(choiceButtonA);
                UnityEngine.Object.DestroyImmediate(nextButton);
                UnityEngine.Object.DestroyImmediate(itemGetPanel);
                UnityEngine.Object.DestroyImmediate(systemPromptPanel);
                UnityEngine.Object.DestroyImmediate(choicesPanel);
                UnityEngine.Object.DestroyImmediate(dialoguePanel);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GuideSetupUIPosition_ShouldFrameWeatherPrompt()
        {
            Type guideType = Type.GetType("Guide, Assembly-CSharp");
            Assert.IsNotNull(guideType, "Guide was not found.");

            GameObject root = new GameObject("GuideRoot");
            GameObject weatherPromptPanel = CreateUiObject("WeatherPromptPanel");

            try
            {
                MonoBehaviour guide = (MonoBehaviour)root.AddComponent(guideType);
                SetField(guide, "weatherPromptPanel", weatherPromptPanel);
                SetField(guide, "weatherPromptText", weatherPromptPanel.AddComponent<TextMeshProUGUI>());

                Invoke(guide, "SetupUIPosition");

                RectTransform panelRect = weatherPromptPanel.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0.22f, 0.72f), panelRect.anchorMin);
                Assert.AreEqual(new Vector2(0.78f, 0.88f), panelRect.anchorMax);
                AssertFrameChild(weatherPromptPanel.transform, "GoldFrameTop");
                AssertFrameChild(weatherPromptPanel.transform, "GoldCornerUpperLeft");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(weatherPromptPanel);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TryAwardChapter04Page_ShouldSetCollectedPagesToFourthPageOnce()
        {
            Type chatType = Type.GetType("Chat, Assembly-CSharp");
            Type saveDataType = Type.GetType("ZhuozhengYuan.SaveData, Assembly-CSharp");
            Assert.IsNotNull(chatType, "Chat was not found.");
            Assert.IsNotNull(saveDataType, "SaveData was not found.");

            object saveData = Activator.CreateInstance(saveDataType);
            SetField(saveData, "collectedPages", 2);

            bool firstAwarded = InvokeTryAwardChapter04Page(chatType, saveData, 5);
            bool secondAwarded = InvokeTryAwardChapter04Page(chatType, saveData, 5);

            Assert.IsTrue(firstAwarded);
            Assert.IsFalse(secondAwarded);
            Assert.AreEqual(4, GetField(saveData, "collectedPages"));
            Assert.AreEqual(true, GetField(saveData, "chapter04PageCollected"));
        }

        [Test]
        public void ShowChapter05RouteGuide_ShouldUseChapter01WorldSpaceVisualStyle()
        {
            Type chatType = Type.GetType("Chat, Assembly-CSharp");
            Assert.IsNotNull(chatType, "Chat was not found.");

            GameObject chatObject = new GameObject("Chat");
            GameObject scholar = new GameObject("shusheng");
            GameObject target = new GameObject("westTrigger");
            Transform guideRoot = null;

            try
            {
                object chat = chatObject.AddComponent(chatType);
                scholar.transform.position = new Vector3(-45f, 1f, -35f);
                target.transform.position = new Vector3(-141.5f, 3.2f, -119.8f);

                SetField(chat, "chapter05GuideTargetOverride", target.transform);
                SetField(chat, "chapter05RouteGuideAutoPointCount", 5);

                MethodInfo method = chatType.GetMethod("ShowChapter05RouteGuide", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(method, "Chat should create the post-Chapter-4 route guide.");
                method.Invoke(chat, new object[] { scholar.transform.position });

                guideRoot = GameObject.Find("Chapter04ToChapter05RouteGuide")?.transform;
                Assert.IsNotNull(guideRoot, "The Chapter 05 route guide root should be created.");
                Assert.IsNull(guideRoot.parent, "The Chapter 05 route guide should live in world space.");

                Transform runtimeRoot = guideRoot.Find("Chapter01AuthoredGuideRoot");
                Assert.IsNotNull(runtimeRoot, "The Chapter 05 route should reuse Chapter01AuthoredRouteGuide visuals.");
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
                UnityEngine.Object.DestroyImmediate(scholar);
                UnityEngine.Object.DestroyImmediate(chatObject);
            }
        }

        [Test]
        public void Chapter04DialogueLines_ShouldAlwaysHaveVisibleSpeakerNames()
        {
            Type chatType = Type.GetType("Chat, Assembly-CSharp");
            Assert.IsNotNull(chatType, "Chat was not found.");

            GameObject root = new GameObject("ChatRoot");

            try
            {
                object chat = root.AddComponent(chatType);
                Invoke(chat, "InitializeDialogueData");

                Array stages = (Array)GetField(chat, "stages");
                Assert.IsNotNull(stages, "Dialogue stages should be initialized.");

                for (int stageIndex = 0; stageIndex < stages.Length; stageIndex++)
                {
                    object stage = stages.GetValue(stageIndex);
                    Array lines = (Array)GetField(stage, "lines");
                    Assert.IsNotNull(lines, $"Stage {stageIndex} should have dialogue lines.");

                    for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                    {
                        object line = lines.GetValue(lineIndex);
                        string speaker = (string)GetField(line, "speaker");
                        string text = (string)GetField(line, "text");

                        if (string.Equals(speaker, "SYSTEM", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        Assert.IsFalse(string.IsNullOrWhiteSpace(speaker), $"Stage {stageIndex}, line {lineIndex} is missing a speaker: {text}");
                        Assert.IsTrue(speaker == "听雨书生" || speaker == "玩家" || speaker == "系统", $"Unexpected Chapter 04 speaker '{speaker}' at stage {stageIndex}, line {lineIndex}.");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShowCurrentLine_ShouldCreateVisibleSpeakerNameWhenSceneReferenceIsMissing()
        {
            Type chatType = Type.GetType("Chat, Assembly-CSharp");
            Assert.IsNotNull(chatType, "Chat was not found.");

            GameObject root = new GameObject("ChatRoot");
            GameObject dialoguePanel = CreateUiObject("DialoguePanel");
            GameObject dialogueTextObject = CreateTextChild("DialogueText", dialoguePanel.transform);

            try
            {
                MonoBehaviour chat = (MonoBehaviour)root.AddComponent(chatType);
                SetField(chat, "dialoguePanel", dialoguePanel);
                SetField(chat, "dialogueText", dialogueTextObject.GetComponent<TextMeshProUGUI>());
                SetField(chat, "speakerNameText", null);

                Invoke(chat, "InitializeDialogueData");
                SetField(chat, "currentStage", 0);
                SetField(chat, "currentLineIndex", 0);

                Invoke(chat, "ShowCurrentLine");

                Transform speakerObject = dialoguePanel.transform.Find("SpeakerNameText");
                Assert.IsNotNull(speakerObject, "Chat should create a speaker name text when the scene reference is missing.");

                TextMeshProUGUI speakerText = speakerObject.GetComponent<TextMeshProUGUI>();
                Assert.IsNotNull(speakerText, "The generated speaker name object should contain TextMeshProUGUI.");
                Assert.AreEqual("听雨书生", speakerText.text);
                Assert.IsTrue(speakerText.gameObject.activeSelf);
                Assert.AreSame(speakerText, GetField(chat, "speakerNameText"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dialoguePanel);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateUiObject(string name)
        {
            return new GameObject(name, typeof(RectTransform), typeof(Image));
        }

        private static GameObject CreateButton(string name)
        {
            return new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        }

        private static GameObject CreateTextChild(string name, Transform parent)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            return textObject;
        }

        private static void AssertFrameChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            Assert.IsNotNull(child, childName + " was not created.");

            Image image = child.GetComponent<Image>();
            Assert.IsNotNull(image, childName + " is missing Image.");
            Assert.IsFalse(image.raycastTarget, childName + " should not block button clicks.");
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

        private static void InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method {methodName} does not exist.");
            method.Invoke(null, args);
        }

        private static bool InvokeTryAwardChapter04Page(Type chatType, object saveData, int totalPages)
        {
            MethodInfo method = chatType.GetMethod("TryAwardChapter04Page", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "TryAwardChapter04Page was not found.");
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
