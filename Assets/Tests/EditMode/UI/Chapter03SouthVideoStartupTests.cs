using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter03SouthVideoStartupTests
    {
        [Test]
        public void Awake_ShouldKeepCinematicVideoHiddenUntilGameplayTrigger()
        {
            Type southType = Type.GetType("South, Assembly-CSharp");
            Assert.IsNotNull(southType, "South was not found.");

            GameObject southObject = new GameObject("South");
            GameObject videoObject = new GameObject("Video Player", typeof(VideoPlayer));
            GameObject renderObject = new GameObject("VideoRenderTexture", typeof(RectTransform), typeof(RawImage));
            renderObject.SetActive(true);

            try
            {
                Component south = southObject.AddComponent(southType);
                VideoPlayer videoPlayer = videoObject.GetComponent<VideoPlayer>();
                videoPlayer.playOnAwake = true;

                southType.GetField("videoPlayer", BindingFlags.Instance | BindingFlags.Public)
                    .SetValue(south, videoPlayer);
                southType.GetField("videoRenderTexture", BindingFlags.Instance | BindingFlags.Public)
                    .SetValue(south, renderObject.GetComponent<RawImage>());

                MethodInfo awake = southType.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(awake, "South should hide video UI before the first gameplay frame.");

                awake.Invoke(south, null);

                Assert.IsFalse(renderObject.activeSelf, "Video RawImage must stay hidden until the heating trigger starts playback.");
                Assert.IsFalse(videoPlayer.playOnAwake, "VideoPlayer should not auto-start when the chapter begins.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(renderObject);
                UnityEngine.Object.DestroyImmediate(videoObject);
                UnityEngine.Object.DestroyImmediate(southObject);
            }
        }

        [Test]
        public void Awake_ShouldKeepFireAndSteamParticlesStoppedUntilGameplayTrigger()
        {
            Type southType = Type.GetType("South, Assembly-CSharp");
            Assert.IsNotNull(southType, "South was not found.");

            GameObject southObject = new GameObject("South");
            GameObject fireObject = new GameObject("FireParticle", typeof(ParticleSystem));
            GameObject steamObject = new GameObject("SteamParticle", typeof(ParticleSystem));

            try
            {
                Component south = southObject.AddComponent(southType);
                ParticleSystem fireParticle = fireObject.GetComponent<ParticleSystem>();
                ParticleSystem steamParticle = steamObject.GetComponent<ParticleSystem>();

                ParticleSystem.MainModule fireMain = fireParticle.main;
                fireMain.playOnAwake = true;
                ParticleSystem.MainModule steamMain = steamParticle.main;
                steamMain.playOnAwake = true;

                southType.GetField("fireParticleSystem", BindingFlags.Instance | BindingFlags.Public)
                    .SetValue(south, fireParticle);
                southType.GetField("steamParticleSystem", BindingFlags.Instance | BindingFlags.Public)
                    .SetValue(south, steamParticle);

                MethodInfo awake = southType.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(awake, "South should stop chapter particles before the first gameplay frame.");

                awake.Invoke(south, null);

                Assert.IsFalse(fireParticle.main.playOnAwake, "Fire should not auto-start when the chapter begins.");
                Assert.IsFalse(steamParticle.main.playOnAwake, "Steam should not auto-start when the chapter begins.");
                Assert.IsFalse(fireParticle.isPlaying, "Fire should stay stopped until the heating trigger.");
                Assert.IsFalse(steamParticle.isPlaying, "Steam should stay stopped until the heating trigger.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(steamObject);
                UnityEngine.Object.DestroyImmediate(fireObject);
                UnityEngine.Object.DestroyImmediate(southObject);
            }
        }

        [Test]
        public void SetChapterPresentationVisible_ShouldHideHintAndCultureUiDuringVideo()
        {
            Type southType = Type.GetType("South, Assembly-CSharp");
            Type frameType = Type.GetType("Chapter03PlaqueFrame, Assembly-CSharp");
            Assert.IsNotNull(southType, "South was not found.");
            Assert.IsNotNull(frameType, "Chapter03PlaqueFrame was not found.");

            GameObject southObject = new GameObject("South");
            GameObject hintParent = new GameObject("HintParent", typeof(RectTransform));
            GameObject hintObject = new GameObject("SouthHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
            GameObject culturePanel = new GameObject("CultureTipPanel");
            hintObject.transform.SetParent(hintParent.transform, false);

            try
            {
                Component south = southObject.AddComponent(southType);
                TextMeshProUGUI hintText = hintObject.GetComponent<TextMeshProUGUI>();

                InvokeStatic(frameType, "ApplyHintFrame", hintText, true);

                southType.GetField("hintText", BindingFlags.Instance | BindingFlags.Public)
                    .SetValue(south, hintText);
                southType.GetField("cultureTipPanel", BindingFlags.Instance | BindingFlags.Public)
                    .SetValue(south, culturePanel);

                MethodInfo method = southType.GetMethod("SetChapterPresentationVisible", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(method, "South should expose a helper that hides chapter UI while the cinematic video is playing.");

                method.Invoke(south, new object[] { false });

                Transform frame = hintParent.transform.Find("Chapter03PlaqueFrame_SouthHintText");
                Assert.IsNotNull(frame, "Hint frame should exist for the South hint text.");
                Assert.IsFalse(hintObject.activeSelf, "Hint text should hide before the cinematic starts.");
                Assert.IsFalse(frame.gameObject.activeSelf, "Hint plaque frame should hide with the hint text.");
                Assert.IsFalse(culturePanel.activeSelf, "Culture tip panel should hide during the cinematic.");

                method.Invoke(south, new object[] { true });

                Assert.IsTrue(hintObject.activeSelf, "Hint text should come back after the cinematic.");
                Assert.IsTrue(frame.gameObject.activeSelf, "Hint plaque frame should come back after the cinematic.");
                Assert.IsTrue(culturePanel.activeSelf, "Culture tip panel should come back after the cinematic.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(culturePanel);
                UnityEngine.Object.DestroyImmediate(hintParent);
                UnityEngine.Object.DestroyImmediate(southObject);
            }
        }

        private static void InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method {methodName} does not exist.");
            method.Invoke(null, args);
        }
    }
}
