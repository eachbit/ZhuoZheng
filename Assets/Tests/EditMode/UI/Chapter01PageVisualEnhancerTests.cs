using System;
using NUnit.Framework;
using UnityEngine;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter01PageVisualEnhancerTests
    {
        [Test]
        public void SetAvailability_ShouldCreateEnhancedPageVisualWithoutExtraColliders()
        {
            Type pickupType = Type.GetType("ZhuozhengYuan.PagePickupInteractable, Assembly-CSharp");
            Type visualType = Type.GetType("ZhuozhengYuan.Chapter01PageVisualEnhancer, Assembly-CSharp");
            Assert.IsNotNull(pickupType, "PagePickupInteractable was not found.");
            Assert.IsNotNull(visualType, "Chapter01PageVisualEnhancer was not found.");

            GameObject pickupObject = new GameObject("Chapter01PagePickup");
            try
            {
                MeshRenderer legacyRenderer = pickupObject.AddComponent<MeshRenderer>();
                Component pickup = pickupObject.AddComponent(pickupType);

                pickupType.GetMethod("SetAvailability")?.Invoke(pickup, new object[] { true });

                Component enhancer = pickupObject.GetComponent(visualType);
                Transform visualRoot = pickupObject.transform.Find("Chapter01EnhancedPageVisual");

                Assert.NotNull(enhancer);
                Assert.NotNull(visualRoot);
                Assert.NotNull(visualRoot.GetComponentInChildren<MeshRenderer>());
                Assert.AreEqual(0, visualRoot.GetComponentsInChildren<Collider>(true).Length);
                Assert.False(legacyRenderer.enabled);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pickupObject);
            }
        }

        [Test]
        public void SetAvailability_WhenHidden_ShouldKeepInteractionObjectInactive()
        {
            Type pickupType = Type.GetType("ZhuozhengYuan.PagePickupInteractable, Assembly-CSharp");
            Assert.IsNotNull(pickupType, "PagePickupInteractable was not found.");

            GameObject pickupObject = new GameObject("Chapter01PagePickup");
            try
            {
                Component pickup = pickupObject.AddComponent(pickupType);

                pickupType.GetMethod("SetAvailability")?.Invoke(pickup, new object[] { true });
                pickupType.GetMethod("SetAvailability")?.Invoke(pickup, new object[] { false });

                Assert.False(pickupObject.activeSelf);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pickupObject);
            }
        }
    }
}
