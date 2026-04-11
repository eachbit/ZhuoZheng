#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ZhuozhengYuan.EditorTools
{
    [InitializeOnLoad]
    public static class Chapter01ManualWaterEffectTools
    {
        private const string RemoveMenuPath = "Tools/Zhuozhengyuan/Remove Current Purple Water Effect";
        private const string CreateCenterFlowMenuPath = "Tools/Zhuozhengyuan/Create Center Flow Effect From Selected Area";
        private const string ApplyGlobalFlowToSelectedRendererMenuPath = "Tools/Zhuozhengyuan/Apply Chapter01 Global Water Flow To Selected Renderer";
        private const string CreateManualCenterFlowSegmentMenuPath = "Tools/Zhuozhengyuan/Create Center Flow Segment";
        private const string RemoveCenterFlowMenuPath = "Tools/Zhuozhengyuan/Remove Current Center Flow Effect";
        private const string BindCenterFlowMenuPath = "Tools/Zhuozhengyuan/Bind Selected Object To Center Flow Visuals";
        private const string LegacyOverlayName = "__Chapter01WaterFlowOverlay";
        private const string LegacyEffectName = "CenterWaterEffect";
        private const string FlowRootName = "FlowCenterVisuals";
        private const string FlowSurfaceName = "CenterFlowSurface";
        private const string FlowAnchorName = "CenterFlowAnchor";
        private const string Chapter01VisualsRootName = "_32_Chapter01_Visuals";
        private const string FlowSelectorName = "FlowSelectorInteractable";
        private const string PagePickupName = "Chapter01PagePickup";
        private const string WaterPoolLightMaterialName = "Water_Pool_Light";
        private const string WaterPoolLightFbxPath = "Assets/Assets/Assets/拙政园-无树.fbx";
        private const string LegacyMaterialFolder = "Assets/Standard Assets/Environment/Water/Water/Materials/";
        private const string FlowShaderPath = "Assets/Shaders/Chapters/Chapter01/Chapter01SimpleFlowWater.shader";
        private const string FlowTexturePath = "Assets/Assets/Assets/01 FBX \u62D9\u653F\u56ED-\u65E0\u6811/\u62D9\u653F\u56ED-\u65E0\u6811/Water_Pool_Light.jpg";
        private const string GeneratedMaterialsFolder = "Assets/Materials";
        private const string GeneratedChapterWaterFolder = "Assets/Materials/Chapters/Chapter01/Water";
        private const string GeneratedFlowMaterialPath = "Assets/Materials/Chapters/Chapter01/Water/Chapter01CenterFlow.mat";
        private const string CenterFlowSegmentNamePrefix = "CenterFlowSegment_";
        private const float LargeSelectionWidthThreshold = 80f;
        private const float LargeSelectionDepthThreshold = 80f;
        private const float LargeSelectionAreaThreshold = 3200f;

        static Chapter01ManualWaterEffectTools()
        {
            EditorApplication.delayCall += CleanupLoadedScenesSilently;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        [MenuItem(RemoveMenuPath)]
        public static void RemoveCurrentPurpleWaterEffect()
        {
            int removedCount = CleanupLoadedScenes();
            string message = removedCount > 0
                ? "Removed the current legacy purple water effect from the loaded scene."
                : "No legacy purple water effect was found in the loaded scene.";
            EditorUtility.DisplayDialog("Chapter01 Water Cleanup", message, "OK");
        }

        [MenuItem(CreateCenterFlowMenuPath)]
        public static void CreateCenterFlowEffectFromSelectedArea()
        {
            CleanupLoadedScenes();

            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("No selection", "Select the water region you want to turn into a flow effect first.", "OK");
                return;
            }

            if (!TryGetSelectionBounds(selected, out Bounds selectionBounds, out string selectionError))
            {
                EditorUtility.DisplayDialog("Selection unsupported", selectionError, "OK");
                return;
            }

            if (ShouldBlockLargeSingleMeshSelection(selected, selectionBounds))
            {
                EditorUtility.DisplayDialog(
                    "Selection too large",
                    "This is a large single-piece water mesh, so the auto center-flow generator is blocked. Please split it into manual center segments inside FlowCenterVisuals instead.",
                    "OK");
                return;
            }

            float flowYaw = selected.transform.eulerAngles.y;
            bool usedFallbackAnchor = false;
            if (ShouldUseFallbackCenterAnchor(selected, selectionBounds))
            {
                if (!TryCreateFallbackCenterAnchor(selected.scene, selectionBounds, out selectionBounds, out flowYaw, out string fallbackError))
                {
                    EditorUtility.DisplayDialog("Center flow fallback failed", fallbackError, "OK");
                    return;
                }

                usedFallbackAnchor = true;
            }

            Material flowMaterial = GetOrCreateCenterFlowMaterial();
            if (flowMaterial == null)
            {
                EditorUtility.DisplayDialog("Flow material missing", "Could not create the Chapter01 flow material. Check whether the shader and water texture imported successfully.", "OK");
                return;
            }

            Scene scene = selected.scene;
            GameObject flowRoot = FindOrCreateSceneRoot(scene, FlowRootName);
            RemoveGeneratedFlowSurface(flowRoot);

            GameObject flowSurface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flowSurface.name = FlowSurfaceName;
            SceneManager.MoveGameObjectToScene(flowSurface, scene);
            flowSurface.transform.SetParent(flowRoot.transform, false);
            flowSurface.layer = selected.layer;

            Collider collider = flowSurface.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            float offset = Mathf.Max(0.02f, selectionBounds.size.y * 0.05f);
            flowSurface.transform.position = new Vector3(selectionBounds.center.x, selectionBounds.max.y + offset, selectionBounds.center.z);
            flowSurface.transform.rotation = Quaternion.Euler(90f, flowYaw, 0f);
            flowSurface.transform.localScale = new Vector3(
                Mathf.Max(0.5f, selectionBounds.size.x),
                Mathf.Max(0.5f, selectionBounds.size.z),
                1f);

            MeshRenderer renderer = flowSurface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = flowMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            flowRoot.SetActive(true);
            BindCenterFlowObject(scene, flowRoot);

            Selection.activeGameObject = flowSurface;
            EditorGUIUtility.PingObject(flowSurface);
            EditorSceneManager.MarkSceneDirty(scene);

            if (usedFallbackAnchor)
            {
                EditorUtility.DisplayDialog("Center flow area created", "Detected a large combined water mesh. The tool created a dedicated center-flow anchor automatically and used that safer area for the flow surface.", "OK");
            }
        }

        [MenuItem(ApplyGlobalFlowToSelectedRendererMenuPath)]
        public static void ApplyChapter01GlobalWaterFlowToSelectedRenderer()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("No selection", "Select the renderer you want to update first.", "OK");
                return;
            }

            Renderer renderer = selected.GetComponent<Renderer>();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("No renderer", "Select a GameObject with a Renderer component.", "OK");
                return;
            }

            Material globalFlowMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Chapters/Chapter01/Water/Chapter01GlobalFlow.mat");
            if (globalFlowMaterial == null)
            {
                EditorUtility.DisplayDialog("Global flow material missing", "Could not load Assets/Materials/Chapters/Chapter01/Water/Chapter01GlobalFlow.mat.", "OK");
                return;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;
            bool replacedAny = false;
            for (int index = 0; index < sharedMaterials.Length; index++)
            {
                if (!IsWaterPoolLightMaterial(sharedMaterials[index]))
                {
                    continue;
                }

                sharedMaterials[index] = globalFlowMaterial;
                replacedAny = true;
            }

            if (!replacedAny)
            {
                EditorUtility.DisplayDialog("No matching materials", "No Water_Pool_Light material slots were found on the selected renderer.", "OK");
                return;
            }

            renderer.sharedMaterials = sharedMaterials;
            EditorUtility.SetDirty(renderer);
            EditorSceneManager.MarkSceneDirty(selected.scene);
            Selection.activeGameObject = selected;
            EditorGUIUtility.PingObject(selected);
        }

        [MenuItem(CreateManualCenterFlowSegmentMenuPath)]
        public static void CreateManualCenterFlowSegment()
        {
            Scene scene = Selection.activeGameObject != null
                ? Selection.activeGameObject.scene
                : SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("No loaded scene", "Open and select the scene where FlowCenterVisuals should be created first.", "OK");
                return;
            }

            Material flowMaterial = GetOrCreateCenterFlowMaterial();
            if (flowMaterial == null)
            {
                EditorUtility.DisplayDialog("Flow material missing", "Could not create the Chapter01 flow material. Check whether the shader and water texture imported successfully.", "OK");
                return;
            }

            GameObject flowRoot = FindOrCreateSceneRoot(scene, FlowRootName);
            flowRoot.SetActive(true);
            string segmentName = GetNextCenterFlowSegmentName(flowRoot.transform);

            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Quad);
            segment.name = segmentName;
            SceneManager.MoveGameObjectToScene(segment, scene);
            segment.transform.SetParent(flowRoot.transform, false);
            segment.transform.localPosition = Vector3.zero;
            segment.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            segment.transform.localScale = Vector3.one;
            segment.layer = flowRoot.layer;

            Collider collider = segment.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            MeshRenderer renderer = segment.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = flowMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            BindCenterFlowObject(scene, flowRoot);
            EditorUtility.SetDirty(segment);
            EditorSceneManager.MarkSceneDirty(scene);

            Selection.activeGameObject = segment;
            EditorGUIUtility.PingObject(segment);
        }

        [MenuItem(RemoveCenterFlowMenuPath)]
        public static void RemoveCurrentCenterFlowEffect()
        {
            int removedCount = 0;
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                removedCount += RemoveCurrentCenterFlowEffect(SceneManager.GetSceneAt(index));
            }

            string message = removedCount > 0
                ? "Removed the current generated center flow effect from the loaded scene."
                : "No generated center flow effect was found in the loaded scene.";
            EditorUtility.DisplayDialog("Chapter01 Center Flow Cleanup", message, "OK");
        }

        [MenuItem(BindCenterFlowMenuPath)]
        public static void BindSelectedObjectToCenterFlowVisuals()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("No selection", "Select the object you want to register as the center flow visual first.", "OK");
                return;
            }

            BindCenterFlowObject(selected.scene, selected);
            Selection.activeGameObject = selected;
            EditorGUIUtility.PingObject(selected);
            EditorSceneManager.MarkSceneDirty(selected.scene);
        }

        [MenuItem(CreateCenterFlowMenuPath, true)]
        [MenuItem(BindCenterFlowMenuPath, true)]
        private static bool ValidateSelectionDependentMenuItems()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem(ApplyGlobalFlowToSelectedRendererMenuPath, true)]
        private static bool ValidateApplyGlobalFlowMenuItem()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Renderer>() != null;
        }

        [MenuItem(RemoveCenterFlowMenuPath, true)]
        private static bool ValidateRemoveCenterFlowMenuItem()
        {
            return HasGeneratedCenterFlowInLoadedScenes();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            CleanupScene(scene);
        }

        private static void CleanupLoadedScenesSilently()
        {
            CleanupLoadedScenes();
        }

        private static int CleanupLoadedScenes()
        {
            int removedCount = 0;
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                removedCount += CleanupScene(SceneManager.GetSceneAt(index));
            }

            return removedCount;
        }

        private static bool TryGetSelectionBounds(GameObject selected, out Bounds bounds, out string errorMessage)
        {
            if (selected == null)
            {
                bounds = default;
                errorMessage = "Select the exact water surface object first.";
                return false;
            }

            Renderer[] ownRenderers = selected.GetComponents<Renderer>();
            if (TryEncapsulateRendererBounds(ownRenderers, out bounds))
            {
                errorMessage = string.Empty;
                return true;
            }

            Collider[] ownColliders = selected.GetComponents<Collider>();
            if (TryEncapsulateColliderBounds(ownColliders, out bounds))
            {
                errorMessage = string.Empty;
                return true;
            }

            Renderer[] childRenderers = selected.GetComponentsInChildren<Renderer>(true);
            Collider[] childColliders = selected.GetComponentsInChildren<Collider>(true);
            int childRendererCount = CountDescendantComponents(selected.transform, childRenderers);
            int childColliderCount = CountDescendantComponents(selected.transform, childColliders);
            if (childRendererCount > 1 || childColliderCount > 1)
            {
                bounds = default;
                errorMessage = "当前选中的是父物体或组合物体，工具会把整棵层级一起算进范围，所以会出现整片场景都被水面覆盖。请改选具体的水面模型，或者单独放一个只表示这块水域的平面/碰撞体再执行。";
                return false;
            }

            if (TryGetSingleDescendantRendererBounds(selected.transform, childRenderers, out bounds)
                || TryGetSingleDescendantColliderBounds(selected.transform, childColliders, out bounds))
            {
                errorMessage = string.Empty;
                return true;
            }

            bounds = default;
            errorMessage = "当前选中对象本身没有可用于定位的 Renderer 或 Collider。请直接选中具体水面模型，或先给目标区域单独建一个平面/碰撞体。";
            return false;
        }

        private static bool ShouldUseFallbackCenterAnchor(GameObject selected, Bounds selectionBounds)
        {
            if (selected == null)
            {
                return false;
            }

            float horizontalArea = selectionBounds.size.x * selectionBounds.size.z;
            if (selectionBounds.size.x >= LargeSelectionWidthThreshold
                || selectionBounds.size.z >= LargeSelectionDepthThreshold
                || horizontalArea >= LargeSelectionAreaThreshold)
            {
                return true;
            }

            Renderer[] childRenderers = selected.GetComponentsInChildren<Renderer>(true);
            return CountDescendantComponents(selected.transform, childRenderers) > 8;
        }

        private static bool TryCreateFallbackCenterAnchor(Scene scene, Bounds referenceBounds, out Bounds anchorBounds, out float flowYaw, out string errorMessage)
        {
            GameObject flowSelector = FindSceneObject(scene, FlowSelectorName);
            GameObject pagePickup = FindSceneObject(scene, PagePickupName);
            if (flowSelector == null || pagePickup == null)
            {
                anchorBounds = default;
                flowYaw = 0f;
                errorMessage = "Could not find FlowSelectorInteractable or Chapter01PagePickup in the scene, so a safe center-flow anchor could not be created automatically.";
                return false;
            }

            GameObject chapterVisualsRoot = FindSceneRoot(scene, Chapter01VisualsRootName);
            GameObject anchorObject = FindGeneratedFlowAnchor(scene);
            if (anchorObject == null)
            {
                anchorObject = new GameObject(FlowAnchorName);
                SceneManager.MoveGameObjectToScene(anchorObject, scene);
            }

            if (chapterVisualsRoot != null)
            {
                anchorObject.transform.SetParent(chapterVisualsRoot.transform, false);
            }

            BoxCollider anchorCollider = anchorObject.GetComponent<BoxCollider>();
            if (anchorCollider == null)
            {
                anchorCollider = anchorObject.AddComponent<BoxCollider>();
            }

            Vector3 sourcePosition = flowSelector.transform.position;
            Vector3 targetPosition = pagePickup.transform.position;
            Vector3 flatDirection = targetPosition - sourcePosition;
            flatDirection.y = 0f;

            float distance = Mathf.Max(6f, flatDirection.magnitude);
            float length = Mathf.Clamp(distance * 0.55f, 8f, 18f);
            float width = Mathf.Clamp(distance * 0.3f, 5f, 12f);
            flowYaw = flatDirection.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(flatDirection.normalized, Vector3.up).eulerAngles.y
                : 0f;

            Vector3 center = Vector3.Lerp(sourcePosition, targetPosition, 0.58f);
            center.y = referenceBounds.center.y;

            anchorObject.transform.position = center;
            anchorObject.transform.rotation = Quaternion.Euler(0f, flowYaw, 0f);
            anchorObject.transform.localScale = Vector3.one;

            anchorCollider.center = Vector3.zero;
            anchorCollider.size = new Vector3(width, Mathf.Max(0.2f, referenceBounds.size.y), length);
            anchorBounds = new Bounds(
                anchorObject.transform.position,
                new Vector3(width, Mathf.Max(0.2f, referenceBounds.size.y), length));
            errorMessage = string.Empty;
            EditorUtility.SetDirty(anchorObject);
            return true;
        }

        private static bool TryEncapsulateRendererBounds(Renderer[] renderers, out Bounds bounds)
        {
            if (renderers == null || renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bool found = false;
            bounds = default;
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        private static bool TryEncapsulateColliderBounds(Collider[] colliders, out Bounds bounds)
        {
            if (colliders == null || colliders.Length == 0)
            {
                bounds = default;
                return false;
            }

            bool found = false;
            bounds = default;
            for (int index = 0; index < colliders.Length; index++)
            {
                Collider collider = colliders[index];
                if (collider == null)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = collider.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return found;
        }

        private static bool TryGetSingleDescendantRendererBounds(Transform selectedTransform, Renderer[] renderers, out Bounds bounds)
        {
            Renderer matchedRenderer = null;
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null || renderer.transform == selectedTransform)
                {
                    continue;
                }

                if (matchedRenderer != null)
                {
                    bounds = default;
                    return false;
                }

                matchedRenderer = renderer;
            }

            if (matchedRenderer != null)
            {
                bounds = matchedRenderer.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        private static bool TryGetSingleDescendantColliderBounds(Transform selectedTransform, Collider[] colliders, out Bounds bounds)
        {
            Collider matchedCollider = null;
            for (int index = 0; index < colliders.Length; index++)
            {
                Collider collider = colliders[index];
                if (collider == null || collider.transform == selectedTransform)
                {
                    continue;
                }

                if (matchedCollider != null)
                {
                    bounds = default;
                    return false;
                }

                matchedCollider = collider;
            }

            if (matchedCollider != null)
            {
                bounds = matchedCollider.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        private static int CountDescendantComponents<T>(Transform selectedTransform, T[] components) where T : Component
        {
            if (components == null || components.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < components.Length; index++)
            {
                T component = components[index];
                if (component == null || component.transform == selectedTransform)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private static bool HasGeneratedCenterFlowInLoadedScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                if (HasGeneratedCenterFlow(SceneManager.GetSceneAt(index)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasGeneratedCenterFlow(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject flowSurface = FindGeneratedFlowSurface(scene);
            return IsGeneratedCenterFlowSurface(flowSurface);
        }

        private static int RemoveCurrentCenterFlowEffect(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }

            GameObject flowSurfaceObject = FindGeneratedFlowSurface(scene);
            if (!IsGeneratedCenterFlowSurface(flowSurfaceObject))
            {
                return 0;
            }

            GameObject flowAnchorObject = FindGeneratedFlowAnchor(scene);
            Transform flowRoot = flowSurfaceObject.transform.parent;
            if (flowRoot == null || !string.Equals(flowRoot.name, FlowRootName, StringComparison.Ordinal))
            {
                return 0;
            }

            HashSet<GameObject> removedTargets = new HashSet<GameObject>();
            removedTargets.Add(flowSurfaceObject);
            if (flowAnchorObject != null)
            {
                removedTargets.Add(flowAnchorObject);
            }
            Object.DestroyImmediate(flowSurfaceObject);
            if (flowAnchorObject != null)
            {
                Object.DestroyImmediate(flowAnchorObject);
            }

            if (flowRoot.childCount == 0)
            {
                removedTargets.Add(flowRoot.gameObject);
                Object.DestroyImmediate(flowRoot.gameObject);
            }

            if (CleanupEnvironmentBindings(scene, removedTargets))
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            return removedTargets.Count;
        }

        private static int CleanupScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || !ShouldInspectScene(scene))
            {
                return 0;
            }

            HashSet<GameObject> targets = CollectLegacyWaterTargets(scene);
            HashSet<GameObject> generatedCenterFlowTargets = CollectGeneratedCenterFlowTargets(scene);
            foreach (GameObject generatedTarget in generatedCenterFlowTargets)
            {
                targets.Add(generatedTarget);
            }

            if (targets.Count == 0)
            {
                if (CleanupEnvironmentBindings(scene, targets))
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }

                return 0;
            }

            HashSet<GameObject> flowRootsToCheck = new HashSet<GameObject>();
            foreach (GameObject target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                Transform parent = target.transform.parent;
                if (parent != null && string.Equals(parent.name, FlowRootName, StringComparison.Ordinal))
                {
                    flowRootsToCheck.Add(parent.gameObject);
                }
            }

            int removedCount = 0;
            foreach (GameObject target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                Object.DestroyImmediate(target);
                removedCount++;
            }

            foreach (GameObject flowRoot in flowRootsToCheck)
            {
                if (flowRoot == null || flowRoot.transform.childCount > 0)
                {
                    continue;
                }

                targets.Add(flowRoot);
                Object.DestroyImmediate(flowRoot);
                removedCount++;
            }

            bool bindingsChanged = CleanupEnvironmentBindings(scene, targets);
            if (removedCount > 0 || bindingsChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            return removedCount;
        }

        private static HashSet<GameObject> CollectLegacyWaterTargets(Scene scene)
        {
            HashSet<GameObject> targets = new HashSet<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int index = 0; index < roots.Length; index++)
            {
                Transform[] transforms = roots[index].GetComponentsInChildren<Transform>(true);
                for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    GameObject current = transforms[transformIndex].gameObject;
                    if (IsWithinFlowCenterVisuals(current))
                    {
                        continue;
                    }

                    if (string.Equals(current.name, LegacyOverlayName, StringComparison.Ordinal))
                    {
                        targets.Add(current);
                    }
                }

                Renderer[] renderers = roots[index].GetComponentsInChildren<Renderer>(true);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer == null || IsWithinFlowCenterVisuals(renderer.gameObject))
                    {
                        continue;
                    }

                    if (!IsLegacyWaterRenderer(renderer))
                    {
                        continue;
                    }

                    GameObject cleanupTarget = ResolveCleanupTarget(renderer.gameObject);
                    if (cleanupTarget != null)
                    {
                        targets.Add(cleanupTarget);
                    }
                }
            }

            return targets;
        }

        private static HashSet<GameObject> CollectGeneratedCenterFlowTargets(Scene scene)
        {
            HashSet<GameObject> targets = new HashSet<GameObject>();
            GameObject flowSurface = FindGeneratedFlowSurface(scene);
            if (IsGeneratedCenterFlowSurface(flowSurface))
            {
                targets.Add(flowSurface);
            }

            GameObject flowAnchor = FindGeneratedFlowAnchor(scene);
            if (flowAnchor != null)
            {
                targets.Add(flowAnchor);
            }

            return targets;
        }

        private static GameObject ResolveCleanupTarget(GameObject source)
        {
            if (source == null)
            {
                return null;
            }

            Transform current = source.transform;
            while (current != null)
            {
                if (string.Equals(current.name, LegacyOverlayName, StringComparison.Ordinal))
                {
                    return current.gameObject;
                }

                if (string.Equals(current.name, LegacyEffectName, StringComparison.Ordinal))
                {
                    return current.gameObject;
                }

                current = current.parent;
            }

            return source;
        }

        private static void RemoveGeneratedFlowSurface(GameObject flowRoot)
        {
            if (flowRoot == null)
            {
                return;
            }

            Transform generatedSurface = flowRoot.transform.Find(FlowSurfaceName);
            if (generatedSurface != null)
            {
                Object.DestroyImmediate(generatedSurface.gameObject);
            }
        }

        public static int CleanupSceneForVerifier(Scene scene)
        {
            return CleanupScene(scene);
        }

        public static bool ShouldBlockLargeSingleMeshSelectionForVerifier(GameObject selected, Bounds selectionBounds)
        {
            return ShouldBlockLargeSingleMeshSelection(selected, selectionBounds);
        }

        private static GameObject FindGeneratedFlowSurface(Scene scene)
        {
            GameObject flowRoot = FindGeneratedFlowRoot(scene);
            if (flowRoot == null)
            {
                return null;
            }

            Transform generatedSurface = FindChildByExactName(flowRoot.transform, FlowSurfaceName);
            return generatedSurface != null ? generatedSurface.gameObject : null;
        }

        private static GameObject FindGeneratedFlowAnchor(Scene scene)
        {
            GameObject chapterVisualsRoot = FindSceneRoot(scene, Chapter01VisualsRootName);
            if (chapterVisualsRoot != null)
            {
                Transform generatedAnchor = FindChildByExactName(chapterVisualsRoot.transform, FlowAnchorName);
                if (generatedAnchor != null)
                {
                    return generatedAnchor.gameObject;
                }
            }

            return FindSceneRoot(scene, FlowAnchorName);
        }

        private static GameObject FindGeneratedFlowRoot(Scene scene)
        {
            GameObject chapterVisualsRoot = FindSceneRoot(scene, Chapter01VisualsRootName);
            if (chapterVisualsRoot != null)
            {
                Transform nestedFlowRoot = FindChildByExactName(chapterVisualsRoot.transform, FlowRootName);
                if (nestedFlowRoot != null)
                {
                    return nestedFlowRoot.gameObject;
                }
            }

            return FindSceneRoot(scene, FlowRootName);
        }

        private static Transform FindChildByExactName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (string.Equals(root.name, childName, StringComparison.Ordinal))
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                Transform match = FindChildByExactName(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static GameObject FindOrCreateSceneRoot(Scene scene, string rootName)
        {
            GameObject existingRoot = FindSceneRoot(scene, rootName);
            if (existingRoot != null)
            {
                return existingRoot;
            }

            GameObject chapterVisualsRoot = FindSceneRoot(scene, Chapter01VisualsRootName);
            if (chapterVisualsRoot != null)
            {
                Transform nestedRoot = FindChildByExactName(chapterVisualsRoot.transform, rootName);
                if (nestedRoot != null)
                {
                    return nestedRoot.gameObject;
                }
            }

            GameObject createdRoot = new GameObject(rootName);
            SceneManager.MoveGameObjectToScene(createdRoot, scene);
            if (chapterVisualsRoot != null)
            {
                createdRoot.transform.SetParent(chapterVisualsRoot.transform, false);
            }

            return createdRoot;
        }

        private static GameObject FindSceneRoot(Scene scene, string rootName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (string.Equals(roots[index].name, rootName, StringComparison.Ordinal))
                {
                    return roots[index];
                }
            }

            return null;
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (string.Equals(root.name, objectName, StringComparison.Ordinal))
                {
                    return root;
                }

                Transform[] children = root.GetComponentsInChildren<Transform>(true);
                for (int childIndex = 0; childIndex < children.Length; childIndex++)
                {
                    Transform child = children[childIndex];
                    if (child != null && string.Equals(child.name, objectName, StringComparison.Ordinal))
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        private static Material GetOrCreateCenterFlowMaterial()
        {
            EnsureFolderExists(GeneratedMaterialsFolder);
            EnsureFolderExists(GeneratedChapterWaterFolder);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(GeneratedFlowMaterialPath);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(FlowShaderPath);
            Texture2D waterTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FlowTexturePath);
            if (shader == null || waterTexture == null)
            {
                return null;
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, GeneratedFlowMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.name = "Chapter01CenterFlow";
            material.SetTexture("_MainTex", waterTexture);
            material.SetColor("_Tint", new Color(0.68f, 0.9f, 0.95f, 0.7f));
            material.SetFloat("_Alpha", 0.72f);
            material.SetVector("_Tiling", new Vector4(3.5f, 2.8f, 0f, 0f));
            material.SetVector("_FlowDirection", new Vector4(1f, 0.2f, 0f, 0f));
            material.SetFloat("_FlowSpeed", 0.22f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static string GetNextCenterFlowSegmentName(Transform flowRoot)
        {
            int nextIndex = 1;
            if (flowRoot != null)
            {
                for (int index = 0; index < flowRoot.childCount; index++)
                {
                    Transform child = flowRoot.GetChild(index);
                    if (child == null || !child.name.StartsWith(CenterFlowSegmentNamePrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string suffix = child.name.Substring(CenterFlowSegmentNamePrefix.Length);
                    if (!int.TryParse(suffix, out int parsedIndex))
                    {
                        continue;
                    }

                    nextIndex = Math.Max(nextIndex, parsedIndex + 1);
                }
            }

            return CenterFlowSegmentNamePrefix + nextIndex.ToString("00");
        }

        private static void EnsureFolderExists(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string[] segments = assetPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }

        private static void BindCenterFlowObject(Scene scene, GameObject flowObject)
        {
            if (!scene.IsValid() || flowObject == null)
            {
                return;
            }

            Chapter01EnvironmentController[] controllers = FindComponentsInScene<Chapter01EnvironmentController>(scene);
            for (int index = 0; index < controllers.Length; index++)
            {
                Chapter01EnvironmentController controller = controllers[index];
                if (controller == null)
                {
                    continue;
                }

                List<GameObject> boundObjects = new List<GameObject>();
                if (controller.flowingObjects != null)
                {
                    for (int objectIndex = 0; objectIndex < controller.flowingObjects.Length; objectIndex++)
                    {
                        GameObject candidate = controller.flowingObjects[objectIndex];
                        if (candidate == null || candidate == flowObject || IsLegacyWaterObject(candidate))
                        {
                            continue;
                        }

                        boundObjects.Add(candidate);
                    }
                }

                boundObjects.Add(flowObject);
                controller.flowingObjects = boundObjects.ToArray();
                EditorUtility.SetDirty(controller);
            }
        }

        private static bool CleanupEnvironmentBindings(Scene scene, HashSet<GameObject> removedTargets)
        {
            bool anyChanged = false;
            Chapter01EnvironmentController[] controllers = FindComponentsInScene<Chapter01EnvironmentController>(scene);
            for (int index = 0; index < controllers.Length; index++)
            {
                Chapter01EnvironmentController controller = controllers[index];
                if (controller == null || controller.flowingObjects == null || controller.flowingObjects.Length == 0)
                {
                    continue;
                }

                bool changed = false;
                List<GameObject> filteredObjects = new List<GameObject>(controller.flowingObjects.Length);
                for (int objectIndex = 0; objectIndex < controller.flowingObjects.Length; objectIndex++)
                {
                    GameObject candidate = controller.flowingObjects[objectIndex];
                    if (candidate == null)
                    {
                        changed = true;
                        continue;
                    }

                    if (IsManualCenterFlowHelper(candidate))
                    {
                        filteredObjects.Add(candidate);
                        continue;
                    }

                    if ((removedTargets != null && removedTargets.Contains(candidate)) || IsLegacyWaterObject(candidate))
                    {
                        changed = true;
                        continue;
                    }

                    filteredObjects.Add(candidate);
                }

                if (!changed)
                {
                    continue;
                }

                controller.flowingObjects = filteredObjects.ToArray();
                EditorUtility.SetDirty(controller);
                anyChanged = true;
            }

            return anyChanged;
        }

        private static bool ShouldInspectScene(Scene scene)
        {
            string scenePath = (scene.path ?? string.Empty).Replace('\\', '/');
            if (scenePath.EndsWith("/Garden_Main.unity", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return FindComponentsInScene<Chapter01EnvironmentController>(scene).Length > 0;
        }

        private static T[] FindComponentsInScene<T>(Scene scene) where T : Component
        {
            List<T> results = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                results.AddRange(roots[index].GetComponentsInChildren<T>(true));
            }

            return results.ToArray();
        }

        private static bool IsLegacyWaterObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            if (string.Equals(gameObject.name, LegacyOverlayName, StringComparison.Ordinal))
            {
                return true;
            }

            return ContainsLegacyWaterRenderer(gameObject);
        }

        private static bool IsManualCenterFlowHelper(GameObject gameObject)
        {
            return IsWithinFlowCenterVisuals(gameObject)
                && !string.Equals(gameObject.name, FlowRootName, StringComparison.Ordinal)
                && !IsGeneratedCenterFlowSurface(gameObject);
        }

        private static bool IsWithinFlowCenterVisuals(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            Transform current = gameObject.transform;
            while (current != null)
            {
                if (string.Equals(current.name, FlowRootName, StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsGeneratedCenterFlowSurface(GameObject gameObject)
        {
            if (gameObject == null || !string.Equals(gameObject.name, FlowSurfaceName, StringComparison.Ordinal))
            {
                return false;
            }

            Transform parent = gameObject.transform.parent;
            return parent != null && string.Equals(parent.name, FlowRootName, StringComparison.Ordinal);
        }

        private static bool ShouldBlockLargeSingleMeshSelection(GameObject selected, Bounds selectionBounds)
        {
            if (selected == null)
            {
                return false;
            }

            float horizontalArea = selectionBounds.size.x * selectionBounds.size.z;
            if (selectionBounds.size.x < LargeSelectionWidthThreshold
                && selectionBounds.size.z < LargeSelectionDepthThreshold
                && horizontalArea < LargeSelectionAreaThreshold)
            {
                return false;
            }

            Renderer[] ownRenderers = selected.GetComponents<Renderer>();
            Collider[] ownColliders = selected.GetComponents<Collider>();
            Renderer[] descendantRenderers = selected.GetComponentsInChildren<Renderer>(true);
            Collider[] descendantColliders = selected.GetComponentsInChildren<Collider>(true);

            bool singlePieceSelection = ownRenderers.Length <= 1
                && ownColliders.Length <= 1
                && CountDescendantComponents(selected.transform, descendantRenderers) <= 1
                && CountDescendantComponents(selected.transform, descendantColliders) <= 1;

            return singlePieceSelection;
        }

        private static bool ContainsLegacyWaterRenderer(GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                if (IsLegacyWaterRenderer(renderers[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLegacyWaterRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                if (IsLegacyWaterMaterial(materials[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLegacyWaterMaterial(Material material)
        {
            if (material == null)
            {
                return false;
            }

            Shader shader = material.shader;
            if (shader != null && string.Equals(shader.name, "FX/Water", StringComparison.Ordinal))
            {
                return true;
            }

            string materialName = material.name ?? string.Empty;
            if (materialName.IndexOf("WaterPro", StringComparison.OrdinalIgnoreCase) >= 0
                || materialName.IndexOf("WaterPlaneMaterial", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string assetPath = AssetDatabase.GetAssetPath(material).Replace('\\', '/');
            return assetPath.StartsWith(LegacyMaterialFolder, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWaterPoolLightMaterial(Material material)
        {
            if (material == null)
            {
                return false;
            }

            Material exactWaterPoolLightMaterial = GetExactWaterPoolLightMaterial();
            if (exactWaterPoolLightMaterial != null && material == exactWaterPoolLightMaterial)
            {
                return true;
            }

            string assetPath = AssetDatabase.GetAssetPath(material).Replace('\\', '/');
            if (!string.Equals(assetPath, WaterPoolLightFbxPath, StringComparison.Ordinal))
            {
                return false;
            }

            return string.Equals(material.name ?? string.Empty, WaterPoolLightMaterialName, StringComparison.Ordinal);
        }

        private static Material GetExactWaterPoolLightMaterial()
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(WaterPoolLightFbxPath);
            for (int index = 0; index < subAssets.Length; index++)
            {
                Material candidate = subAssets[index] as Material;
                if (candidate != null && string.Equals(candidate.name, WaterPoolLightMaterialName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
#endif
