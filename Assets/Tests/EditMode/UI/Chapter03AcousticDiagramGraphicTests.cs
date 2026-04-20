using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter03AcousticDiagramGraphicTests
    {
        [Test]
        public void BuildVisibleSegments_ShouldRevealMoreSegmentsAsProgressIncreases()
        {
            Type graphicType = Type.GetType("ZhuozhengYuan.Chapter03AcousticDiagramGraphic, Assembly-CSharp");
            Assert.IsNotNull(graphicType, "Chapter03AcousticDiagramGraphic was not found.");

            MethodInfo method = graphicType.GetMethod("BuildVisibleSegments", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(method, "BuildVisibleSegments should expose the line reveal contract for tests and runtime previews.");

            Rect rect = new Rect(-300f, -210f, 600f, 420f);
            ICollection empty = InvokeBuild(method, rect, 0f);
            ICollection partial = InvokeBuild(method, rect, 0.35f);
            ICollection complete = InvokeBuild(method, rect, 1f);

            Assert.AreEqual(0, empty.Count, "Progress 0 should draw no animated lines.");
            Assert.Greater(partial.Count, empty.Count, "Partial progress should reveal the first acoustic strokes.");
            Assert.Greater(complete.Count, partial.Count, "Full progress should reveal more strokes than a partial animation.");
            Assert.GreaterOrEqual(complete.Count, 16, "The full diagram should include roof, reflection, water, and underground resonance strokes.");
        }

        [Test]
        public void BuildStaticSegments_ShouldKeepDiagramReadableBeforeAnimationStarts()
        {
            Type graphicType = Type.GetType("ZhuozhengYuan.Chapter03AcousticDiagramGraphic, Assembly-CSharp");
            Assert.IsNotNull(graphicType, "Chapter03AcousticDiagramGraphic was not found.");

            MethodInfo method = graphicType.GetMethod("BuildStaticSegments", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(method, "BuildStaticSegments should expose the always-visible reference diagram.");

            Rect rect = new Rect(-300f, -210f, 600f, 420f);
            ICollection staticSegments = InvokeBuild(method, rect);

            Assert.GreaterOrEqual(staticSegments.Count, 16,
                "The right-side acoustic panel should show a complete static diagram even at animation progress 0.");
        }

        [Test]
        public void Component_ShouldClampProgressAndExposeReplay()
        {
            Type graphicType = Type.GetType("ZhuozhengYuan.Chapter03AcousticDiagramGraphic, Assembly-CSharp");
            Assert.IsNotNull(graphicType, "Chapter03AcousticDiagramGraphic was not found.");

            GameObject host = new GameObject("AcousticDiagramHost", typeof(RectTransform));

            try
            {
                Component graphic = host.AddComponent(graphicType);
                PropertyInfo progressProperty = graphicType.GetProperty("AnimationProgress", BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(progressProperty, "AnimationProgress should be public so the presentation UI can scrub or reset it.");

                progressProperty.SetValue(graphic, 2f, null);
                Assert.AreEqual(1f, (float)progressProperty.GetValue(graphic, null), 0.0001f);

                progressProperty.SetValue(graphic, -1f, null);
                Assert.AreEqual(0f, (float)progressProperty.GetValue(graphic, null), 0.0001f);

                MethodInfo replayMethod = graphicType.GetMethod("Replay", BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(replayMethod, "Replay should reset the draw-on animation when a knowledge panel opens.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void BuildForCurrentRect_ShouldCreateVisibleUnityUiLineImages()
        {
            Type graphicType = Type.GetType("ZhuozhengYuan.Chapter03AcousticDiagramGraphic, Assembly-CSharp");
            Assert.IsNotNull(graphicType, "Chapter03AcousticDiagramGraphic was not found.");

            GameObject host = new GameObject("AcousticDiagramHost", typeof(RectTransform));
            RectTransform rectTransform = host.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(420f, 280f);

            try
            {
                Component graphic = host.AddComponent(graphicType);
                MethodInfo buildMethod = graphicType.GetMethod("BuildForCurrentRect", BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(buildMethod,
                    "The diagram should build ordinary Unity UI Image lines instead of relying only on a custom mesh Graphic.");

                buildMethod.Invoke(graphic, null);

                Image[] images = host.GetComponentsInChildren<Image>(true);
                Assert.GreaterOrEqual(images.Length, 32,
                    "The acoustic diagram should create visible Image-based line segments for the static layer and animated layer.");
                Assert.IsNotNull(host.transform.Find("StaticAcousticLines"),
                    "The diagram should include an always-visible static line layer.");
                Assert.IsNotNull(host.transform.Find("AnimatedAcousticLines"),
                    "The diagram should include an animated highlight line layer.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static ICollection InvokeBuild(MethodInfo method, Rect rect, float progress)
        {
            object result = method.Invoke(null, new object[] { rect, progress });
            ICollection collection = result as ICollection;
            Assert.IsNotNull(collection, "BuildVisibleSegments should return a collection of visible line segments.");
            return collection;
        }

        private static ICollection InvokeBuild(MethodInfo method, Rect rect)
        {
            object result = method.Invoke(null, new object[] { rect });
            ICollection collection = result as ICollection;
            Assert.IsNotNull(collection, "The diagram builder should return a collection of line segments.");
            return collection;
        }
    }
}
