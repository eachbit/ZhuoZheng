using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter03KnowledgePanelTests
    {
        [Test]
        public void ShowOrCreate_ShouldBuildOpenPanelWithDynamicDiagram()
        {
            Type panelType = Type.GetType("ZhuozhengYuan.Chapter03KnowledgePanel, Assembly-CSharp");
            Type diagramType = Type.GetType("ZhuozhengYuan.Chapter03AcousticDiagramGraphic, Assembly-CSharp");
            Assert.IsNotNull(panelType, "Chapter03KnowledgePanel was not found.");
            Assert.IsNotNull(diagramType, "Chapter03AcousticDiagramGraphic was not found.");

            Component panel = null;
            bool closed = false;

            try
            {
                MethodInfo showOrCreate = panelType.GetMethod("ShowOrCreate", BindingFlags.Static | BindingFlags.Public);
                Assert.IsNotNull(showOrCreate, "ShowOrCreate should open or create the runtime knowledge panel.");

                panel = showOrCreate.Invoke(null, new object[] { null, (Action)(() => closed = true) }) as Component;

                Assert.IsNotNull(panel, "The Chapter 03 knowledge panel should be created when no scene panel is wired.");
                Assert.IsTrue((bool)panelType.GetProperty("IsOpen").GetValue(panel, null),
                    "The panel should open immediately after the returning page is collected.");
                Assert.IsNotNull(panel.GetComponentInChildren(diagramType, true),
                    "The right-side acoustic diagram should use the dynamic line graphic.");
                Assert.IsNotNull(panel.GetComponentInChildren<Button>(true),
                    "The generated panel should include an in-UI close button.");

                CanvasScaler scaler = panel.transform.root.GetComponent<CanvasScaler>();
                Assert.IsNotNull(scaler, "The generated presentation canvas should use a CanvasScaler.");
                Assert.AreEqual(new Vector2(1280f, 720f), scaler.referenceResolution,
                    "The knowledge panel should target the project's 16:9 game view directly instead of shrinking 1080p UI into a blurry screenshot.");

                Text[] textElements = panel.GetComponentsInChildren<Text>(true);
                AssertHasReadableText(textElements, "\u5345\u516d\u9e33\u9e2f\u9986\u7684\u6606\u66f2\u4f20\u97f3", 38);
                AssertHasReadableText(textElements, "\u4e00\u53e5\u8bdd\u603b\u7ed3", 22);

                panelType.GetMethod("Close", BindingFlags.Instance | BindingFlags.Public).Invoke(panel, null);

                Assert.IsFalse((bool)panelType.GetProperty("IsOpen").GetValue(panel, null),
                    "Closing the panel should hide it instead of leaving it over gameplay.");
                Assert.IsTrue(closed, "Closing should notify the pickup flow so Chapter 3 can continue.");
            }
            finally
            {
                if (panel != null)
                {
                    UnityEngine.Object.DestroyImmediate(panel.transform.root.gameObject);
                }
            }
        }

        [Test]
        public void CreateDefaultSections_ShouldCoverFourAcousticPrinciples()
        {
            Type panelType = Type.GetType("ZhuozhengYuan.Chapter03KnowledgePanel, Assembly-CSharp");
            Assert.IsNotNull(panelType, "Chapter03KnowledgePanel was not found.");

            MethodInfo createDefaultSections = panelType.GetMethod("CreateDefaultSections", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(createDefaultSections, "CreateDefaultSections should expose the default teaching copy.");

            Array sections = createDefaultSections.Invoke(null, null) as Array;

            Assert.GreaterOrEqual(sections.Length, 4);
            AssertSectionExists(sections, "\u5f27\u5f62\u5377\u68da\u9876");
            AssertSectionExists(sections, "\u65b9\u5f62\u4e0e\u8033\u623f");
            AssertSectionExists(sections, "\u8377\u6c60\u6c34\u9762");
            AssertSectionExists(sections, "\u5730\u9f99\u7a7a\u5c42");
        }

        private static void AssertSectionExists(Array sections, string expectedTitle)
        {
            for (int index = 0; index < sections.Length; index++)
            {
                object section = sections.GetValue(index);
                PropertyInfo titleProperty = section.GetType().GetProperty("Title");
                Assert.IsNotNull(titleProperty, "KnowledgeSection should expose a Title property.");

                if ((string)titleProperty.GetValue(section, null) == expectedTitle)
                {
                    return;
                }
            }

            Assert.Fail("Missing Chapter 03 knowledge section: " + expectedTitle);
        }

        private static void AssertHasReadableText(Text[] textElements, string expectedText, int minimumFontSize)
        {
            for (int index = 0; index < textElements.Length; index++)
            {
                Text text = textElements[index];
                if (text.text == expectedText)
                {
                    Assert.GreaterOrEqual(text.fontSize, minimumFontSize,
                        "Important knowledge panel copy should stay readable in the 1024x576 game view.");
                    return;
                }
            }

            Assert.Fail("Missing expected knowledge panel text: " + expectedText);
        }
    }
}
