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
        private const string RemoveCenterFlowMenuPath = "Tools/Zhuozhengyuan/Remove Current Center Flow Effect";
        private const string BindCenterFlowMenuPath = "Tools/Zhuozhengyuan/Bind Selected Object To Center Flow Visuals";
        private const string LegacyOverlayName = "__Chapter01WaterFlowOverlay";
        private const string LegacyEffectName = "CenterWaterEffect";
        private const string FlowRootName = "FlowCenterVisuals";
        private const string FlowSurfaceName = "CenterFlowSurface";
        private const string LegacyMaterialFolder = "Assets/Standard Assets/Environment/Water/Water/Materials/";
        private const string FlowShaderPath = "Assets/Shaders/Chapters/Chapter01/Chapter01SimpleFlowWater.shader";
        private const string FlowTexturePath = "Assets/Assets/Assets/01 FBX \u62D9\u653F\u56ED-\u65E0\u6811/\u62D9\u653F\u56ED-\u65E0\u6811/Water_Pool_Light.jpg";
        private const string GeneratedMaterialsFolder = "Assets/Materials";
        private const string GeneratedChapterWaterFolder = "Assets/Materials/Chapters/Chapter01/Water";
        private const string GeneratedFlowMaterialPath = "Assets/Materials/Chapters/Chapter01/Water/Chapter01CenterFlow.mat";

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

            if (!TryGetSelectionBounds(selected, out Bounds selectionBounds))
            {
                EditorUtility.DisplayDialog("Selection unsupported", "The selected object has no renderer or collider bounds that can be used to place the flow surface.", "OK");
                return;
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
            flowSurface.transform.rotation = Quaternion.Euler(90f, selected.transform.eulerAngles.y, 0f);
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

        private static bool TryGetSelectionBounds(GameObject selected, out Bounds bounds)
        {
            Renderer[] renderers = selected.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = TryEncapsulateRendererBounds(renderers, out bounds);
            if (hasBounds)
            {
                return true;
            }

            Collider[] colliders = selected.GetComponentsInChildren<Collider>(true);
            if (colliders != null && colliders.Length > 0)
            {
                bounds = colliders[0].bounds;
                for (int index = 1; index < colliders.Length; index++)
                {
                    bounds.Encapsulate(colliders[index].bounds);
                }

                return true;
            }

            bounds = default;
            return false;
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

            GameObject flowRoot = FindSceneRoot(scene, FlowRootName);
            return flowRoot != null && flowRoot.transform.Find(FlowSurfaceName) != null;
        }

        private static int RemoveCurrentCenterFlowEffect(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }

            GameObject flowRoot = FindSceneRoot(scene, FlowRootName);
            if (flowRoot == null)
            {
                return 0;
            }

            Transform generatedSurface = flowRoot.transform.Find(FlowSurfaceName);
            if (generatedSurface == null)
            {
                return 0;
            }

            HashSet<GameObject> removedTargets = new HashSet<GameObject>();
            removedTargets.Add(generatedSurface.gameObject);
            Object.DestroyImmediate(generatedSurface.gameObject);

            if (flowRoot.transform.childCount == 0)
            {
                removedTargets.Add(flowRoot);
                Object.DestroyImmediate(flowRoot);
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
                    if (string.Equals(current.name, LegacyOverlayName, StringComparison.Ordinal))
                    {
                        targets.Add(current);
                    }
                }

                Renderer[] renderers = roots[index].GetComponentsInChildren<Renderer>(true);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
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

        private static GameObject FindOrCreateSceneRoot(Scene scene, string rootName)
        {
            GameObject existingRoot = FindSceneRoot(scene, rootName);
            if (existingRoot != null)
            {
                return existingRoot;
            }

            GameObject createdRoot = new GameObject(rootName);
            SceneManager.MoveGameObjectToScene(createdRoot, scene);
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
    }
}
#endif
