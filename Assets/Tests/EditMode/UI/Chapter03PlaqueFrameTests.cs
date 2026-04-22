using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter03PlaqueFrameTests
    {
        [Test]
        public void ApplyPanel_ShouldPreserveActiveStateAndAddNonBlockingFrame()
        {
            Type frameType = Type.GetType("Chapter03PlaqueFrame, Assembly-CSharp");
            Assert.IsNotNull(frameType, "Chapter03PlaqueFrame was not found.");

            GameObject panel = new GameObject("Chapter03Panel", typeof(RectTransform), typeof(Image));
            panel.SetActive(false);

            try
            {
                InvokeStatic(frameType, "ApplyPanel", panel);

                Assert.IsFalse(panel.activeSelf, "Styling a hidden gameplay panel must not show it.");
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
        public void ApplyPanel_ShouldNotAddImageWhenTargetAlreadyHasTextGraphic()
        {
            Type frameType = Type.GetType("Chapter03PlaqueFrame, Assembly-CSharp");
            Assert.IsNotNull(frameType, "Chapter03PlaqueFrame was not found.");

            GameObject textGraphic = new GameObject("Chapter03TextGraphic", typeof(RectTransform), typeof(TextMeshProUGUI));

            try
            {
                Assert.DoesNotThrow(() => InvokeStatic(frameType, "ApplyPanel", textGraphic));
                Assert.IsNull(textGraphic.GetComponent<Image>(), "A text Graphic cannot also contain an Image component.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(textGraphic);
            }
        }

        [Test]
        public void ApplyHintFrame_ShouldCreateSiblingFrameBehindText()
        {
            Type frameType = Type.GetType("Chapter03PlaqueFrame, Assembly-CSharp");
            Assert.IsNotNull(frameType, "Chapter03PlaqueFrame was not found.");

            GameObject parent = new GameObject("HintParent", typeof(RectTransform));
            GameObject hint = new GameObject("SouthHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
            hint.transform.SetParent(parent.transform, false);

            try
            {
                RectTransform hintRect = hint.GetComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0.5f, 0f);
                hintRect.anchorMax = new Vector2(0.5f, 0f);
                hintRect.pivot = new Vector2(0.5f, 0.5f);
                hintRect.sizeDelta = new Vector2(820f, 108f);
                hintRect.anchoredPosition = new Vector2(12f, 150f);

                InvokeStatic(frameType, "ApplyHintFrame", hint.GetComponent<TextMeshProUGUI>(), true);

                Transform frame = parent.transform.Find("Chapter03PlaqueFrame_SouthHintText");
                Assert.IsNotNull(frame, "Hint frame was not created next to the text.");
                Assert.Less(frame.GetSiblingIndex(), hint.transform.GetSiblingIndex(), "Hint frame should render behind the hint text.");

                RectTransform frameRect = frame.GetComponent<RectTransform>();
                Assert.AreEqual(hintRect.anchorMin, frameRect.anchorMin);
                Assert.AreEqual(hintRect.anchorMax, frameRect.anchorMax);
                Assert.AreEqual(new Vector2(876f, 148f), frameRect.sizeDelta);
                Assert.AreEqual(hintRect.anchoredPosition, frameRect.anchoredPosition);
                AssertFrameChild(frame, "GoldFrameTop");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void ApplyHintFrame_ShouldReuseExistingFrame()
        {
            Type frameType = Type.GetType("Chapter03PlaqueFrame, Assembly-CSharp");
            Assert.IsNotNull(frameType, "Chapter03PlaqueFrame was not found.");

            GameObject parent = new GameObject("HintParent", typeof(RectTransform));
            GameObject hint = new GameObject("NorthHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
            hint.transform.SetParent(parent.transform, false);

            try
            {
                TextMeshProUGUI text = hint.GetComponent<TextMeshProUGUI>();
                InvokeStatic(frameType, "ApplyHintFrame", text, true);
                InvokeStatic(frameType, "ApplyHintFrame", text, false);

                int frameCount = 0;
                foreach (Transform child in parent.transform)
                {
                    if (child.name == "Chapter03PlaqueFrame_NorthHintText")
                    {
                        frameCount++;
                        Assert.IsFalse(child.gameObject.activeSelf, "Hidden hint text should hide only the companion frame.");
                    }
                }

                Assert.AreEqual(1, frameCount, "Repeated styling should update the same hint frame.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void ApplyHintFrame_ShouldNormalizeTextScaleAndAddInnerMargins()
        {
            Type frameType = Type.GetType("Chapter03PlaqueFrame, Assembly-CSharp");
            Assert.IsNotNull(frameType, "Chapter03PlaqueFrame was not found.");

            GameObject parent = new GameObject("HintParent", typeof(RectTransform));
            GameObject hint = new GameObject("SouthHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
            hint.transform.SetParent(parent.transform, false);

            try
            {
                RectTransform hintRect = hint.GetComponent<RectTransform>();
                hintRect.localScale = new Vector3(1.91f, 1.91f, 1f);

                TextMeshProUGUI text = hint.GetComponent<TextMeshProUGUI>();
                text.enableAutoSizing = false;
                text.enableWordWrapping = false;
                text.margin = Vector4.zero;

                InvokeStatic(frameType, "ApplyHintFrame", text, true);

                Assert.AreEqual(Vector3.one, hintRect.localScale, "Hint text should use normal scale so it stays inside the plaque frame.");
                Assert.IsTrue(text.enableAutoSizing, "Hint text should auto-size to stay inside the plaque frame.");
                Assert.IsTrue(text.enableWordWrapping, "Hint text should wrap inside the plaque frame.");
                Assert.AreEqual(new Vector4(24f, 12f, 24f, 12f), text.margin, "Hint text should reserve inner padding instead of touching the frame edges.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        private static void AssertFrameChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            Assert.IsNotNull(child, childName + " was not created.");

            Image image = child.GetComponent<Image>();
            Assert.IsNotNull(image, childName + " is missing Image.");
            Assert.IsFalse(image.raycastTarget, childName + " should not block gameplay UI clicks.");
        }

        private static void InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method {methodName} does not exist.");
            method.Invoke(null, args);
        }
    }
}
