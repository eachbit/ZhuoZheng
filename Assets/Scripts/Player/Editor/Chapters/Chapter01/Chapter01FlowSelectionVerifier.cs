#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace ZhuozhengYuan.EditorTools
{
    public static class Chapter01FlowSelectionVerifier
    {
        private const string Chapter01EnvironmentName = "Chapter01Environment";
        private const string FlowCenterRootName = "FlowCenterVisuals";
        private const string RunMenuPath = "Tools/Zhuozhengyuan/Run Chapter01 Flow Selection Verifier";

        [MenuItem(RunMenuPath)]
        public static void RunFromMenu()
        {
            Run();
        }

        public static void Run()
        {
            GameObject environmentObject = null;
            GameObject visualsRoot = null;
            GameObject flowRoot = null;
            GameObject flowStateProbe = null;
            GameObject cleanupGeneratedSurface = null;
            GameObject cleanupGeneratedAnchor = null;
            GameObject cleanupManualHelper = null;
            GameObject largeSelection = null;
            Material cleanupManualMaterial = null;

            try
            {
                environmentObject = new GameObject(Chapter01EnvironmentName);
                Chapter01EnvironmentController controller = environmentObject.AddComponent<Chapter01EnvironmentController>();

                visualsRoot = new GameObject("_32_Chapter01_Visuals");
                flowRoot = new GameObject(FlowCenterRootName);
                flowRoot.transform.SetParent(visualsRoot.transform, false);

                flowStateProbe = GameObject.CreatePrimitive(PrimitiveType.Quad);
                flowStateProbe.name = "CenterFlowStateProbe";
                flowStateProbe.transform.SetParent(flowRoot.transform, false);

                flowRoot.SetActive(true);
                controller.SetDormant();
                AssertCondition(!flowRoot.activeSelf, "SetDormant should disable FlowCenterVisuals when no flowingObjects are bound.");

                controller.SetDirectionPreview(Chapter01FlowDirection.West);
                AssertCondition(!flowRoot.activeSelf, "West preview should not enable the true center flow root.");

                controller.SetDirectionPreview(Chapter01FlowDirection.South);
                AssertCondition(!flowRoot.activeSelf, "South preview should not enable the true center flow root.");

                controller.SetFlowingSolved();
                AssertCondition(flowRoot.activeSelf, "SetFlowingSolved should enable FlowCenterVisuals as the true center flow.");

                largeSelection = GameObject.CreatePrimitive(PrimitiveType.Quad);
                largeSelection.name = "LargeWaterSelection";
                Bounds largeSelectionBounds = new Bounds(Vector3.zero, new Vector3(120f, 4f, 120f));
                AssertCondition(
                    Chapter01ManualWaterEffectTools.ShouldBlockLargeSingleMeshSelectionForVerifier(largeSelection, largeSelectionBounds),
                    "Large one-piece meshes should be blocked from auto center-flow generation.");

                cleanupGeneratedSurface = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cleanupGeneratedSurface.name = "CenterFlowSurface";
                cleanupGeneratedSurface.transform.SetParent(flowRoot.transform, false);

                cleanupGeneratedAnchor = new GameObject("CenterFlowAnchor");
                cleanupGeneratedAnchor.transform.SetParent(visualsRoot.transform, false);

                cleanupManualHelper = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cleanupManualHelper.name = "ManualRaisedHelper";
                cleanupManualHelper.transform.SetParent(flowRoot.transform, false);

                Shader cleanupManualShader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                AssertCondition(cleanupManualShader != null, "Could not find a shader for the manual helper test material.");
                cleanupManualMaterial = new Material(cleanupManualShader);
                cleanupManualMaterial.name = "WaterPlaneMaterialManualHelper";
                cleanupManualHelper.GetComponent<MeshRenderer>().sharedMaterial = cleanupManualMaterial;

                controller.flowingObjects = new[] { cleanupGeneratedSurface, cleanupGeneratedAnchor, cleanupManualHelper };

                int cleanupRemovedCount = Chapter01ManualWaterEffectTools.CleanupSceneForVerifier(environmentObject.scene);
                AssertCondition(cleanupRemovedCount > 0, "Cleanup should remove the generated center-flow surface.");
                AssertCondition(cleanupGeneratedSurface == null, "Cleanup should destroy the generated CenterFlowSurface object.");
                AssertCondition(cleanupGeneratedAnchor == null, "Cleanup should destroy the generated CenterFlowAnchor object.");
                AssertCondition(cleanupManualHelper != null, "Cleanup should preserve manual helper objects under FlowCenterVisuals.");
                AssertCondition(cleanupManualHelper.transform.parent == flowRoot.transform, "Cleanup should not detach the manual helper from FlowCenterVisuals.");
                AssertCondition(flowRoot.transform.parent == visualsRoot.transform, "FlowCenterVisuals should be nested under _32_Chapter01_Visuals in the verifier.");
                AssertCondition(flowRoot.transform.childCount == 2, "Cleanup should only remove the generated surface and leave the state probe plus the helper.");
                AssertCondition(controller.flowingObjects != null && controller.flowingObjects.Length == 1 && controller.flowingObjects[0] == cleanupManualHelper, "Cleanup should keep manual helper objects in the environment bindings.");

                Debug.Log("Chapter01FlowSelectionVerifier passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError("Chapter01FlowSelectionVerifier failed: " + exception.Message);
                throw;
            }
            finally
            {
                DestroyImmediateSafe(flowStateProbe);
                DestroyImmediateSafe(cleanupGeneratedSurface);
                DestroyImmediateSafe(cleanupGeneratedAnchor);
                DestroyImmediateSafe(cleanupManualHelper);
                DestroyImmediateSafe(largeSelection);
                DestroyImmediateSafe(cleanupManualMaterial);
                DestroyImmediateSafe(flowRoot);
                DestroyImmediateSafe(visualsRoot);
                DestroyImmediateSafe(environmentObject);
            }

            EditorApplication.Exit(0);
        }

        private static void AssertCondition(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void DestroyImmediateSafe(UnityEngine.Object target)
        {
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
