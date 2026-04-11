# Chapter01 Water Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add safe, good-looking Chapter01 water motion by combining a subtle whole-mesh flow material with authored center-channel overlays, while preserving any existing transparent raised helper object and preventing the old tool from turning the entire water mesh into center flow.

**Architecture:** Keep the authored water mesh intact and give it a Chapter01-only global-flow material. Keep puzzle emphasis in `FlowCenterVisuals`, but switch from one auto-generated large surface to several hand-authored center segments and add editor safeguards so large single-mesh selections cannot silently recreate a whole-model overlay. Existing Chapter01 runtime state continues to control only the center-channel visuals.

**Tech Stack:** Unity 2022.3 scene YAML, C# editor tooling, Unity built-in shader pipeline shaders and materials, Chapter01 runtime controller hooks

---

## File Structure

- Modify: `D:/Unity/New/Assets/Scripts/Editor/Chapters/Chapter01/Chapter01ManualWaterEffectTools.cs`
  Responsibility: stop unsafe whole-mesh center-flow generation, preserve non-generated raised transparent helpers, add menu helpers for the safer workflow.

- Modify: `D:/Unity/New/Assets/Scripts/Editor/Chapters/Chapter01/Chapter01FlowSelectionVerifier.cs`
  Responsibility: add editor-side safety checks for the new large-mesh workflow and root binding behavior.

- Create: `D:/Unity/New/Assets/Shaders/Chapters/Chapter01/Chapter01GlobalFlowWater.shader`
  Responsibility: provide subtle whole-water UV flow for the one-piece irregular mesh.

- Create: `D:/Unity/New/Assets/Materials/Chapters/Chapter01/Water/Chapter01GlobalFlow.mat`
  Responsibility: Chapter01-only material instance for the full water mesh.

- Modify: `D:/Unity/New/Assets/Scenes/Garden_Main.unity`
  Responsibility: assign the dedicated global material to the authored water mesh, preserve the current transparent raised helper object, create three center-flow segments under `FlowCenterVisuals`, and bind only the intended overlay root to `Chapter01Environment`.

- Modify only when verification proves a scene-binding issue: `D:/Unity/New/Assets/Scripts/Chapters/Chapter01/Chapter01EnvironmentController.cs`
  Responsibility: keep this file unchanged unless segment authoring exposes a real binding problem that cannot be solved in the editor tooling or scene.

### Task 1: Add Safety Guards For Large Mesh Water Workflow

**Files:**
- Modify: `D:/Unity/New/Assets/Scripts/Editor/Chapters/Chapter01/Chapter01ManualWaterEffectTools.cs`
- Modify: `D:/Unity/New/Assets/Scripts/Editor/Chapters/Chapter01/Chapter01FlowSelectionVerifier.cs`

- [ ] **Step 1: Write the failing verifier for the two hazards**

Add a new verifier method that proves:

1. a large single-mesh water selection should be routed to a manual segment workflow warning instead of creating one giant center-flow quad
2. an existing transparent raised helper under `FlowCenterVisuals` that is not one of the generated tool objects must survive cleanup

```csharp
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace ZhuozhengYuan.EditorTools
{
    public static class Chapter01FlowSelectionVerifier
    {
        public static void RunWaterWorkflowSafeguards()
        {
            var environmentObject = new GameObject("Chapter01Environment");
            environmentObject.AddComponent<Chapter01EnvironmentController>();

            var flowRoot = new GameObject("FlowCenterVisuals");
            var preservedHelper = GameObject.CreatePrimitive(PrimitiveType.Quad);
            preservedHelper.name = "CenterFlowPreviewHelper";
            preservedHelper.transform.SetParent(flowRoot.transform, false);

            var generatedSurface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            generatedSurface.name = "CenterFlowSurface";
            generatedSurface.transform.SetParent(flowRoot.transform, false);

            Chapter01ManualWaterEffectTools.RemoveGeneratedFlowSurfaceForTests(flowRoot);
            AssertCondition(flowRoot.transform.Find("CenterFlowPreviewHelper") != null, "cleanup must preserve manual transparent helper objects");
            AssertCondition(flowRoot.transform.Find("CenterFlowSurface") == null, "cleanup must remove only generated center-flow surface");

            var waterMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            waterMesh.name = "WholeWaterMesh";
            waterMesh.transform.localScale = new Vector3(160f, 0.2f, 140f);

            bool shouldBlockAutoSurface = Chapter01ManualWaterEffectTools.ShouldBlockGeneratedSurfaceForTests(
                waterMesh,
                waterMesh.GetComponent<Renderer>().bounds);

            AssertCondition(shouldBlockAutoSurface, "large one-piece water mesh should require manual segment workflow");
        }

        private static void AssertCondition(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
#endif
```

- [ ] **Step 2: Run the verifier step conceptually to confirm it fails against the current editor tool**

In the current codebase this should fail because:

- `RemoveGeneratedFlowSurface` is private and cannot be exercised directly
- the large-mesh path still falls back to auto-authoring one generated surface after creating a fallback anchor

Manual verification before code changes:

- Open `D:/Unity/New/Assets/Scripts/Editor/Chapters/Chapter01/Chapter01ManualWaterEffectTools.cs`
- Confirm there is no test hook for generated-surface cleanup
- Confirm `CreateCenterFlowEffectFromSelectedArea()` still proceeds after `ShouldUseFallbackCenterAnchor(...)`

Expected result:

- The verifier cannot be satisfied by the current implementation

- [ ] **Step 3: Implement the minimum editor-side guard rails**

Refactor the tool so large single-mesh water selections no longer auto-create a whole-route generated overlay. Expose narrow test hooks guarded for editor-only verification.

```csharp
internal static bool ShouldBlockGeneratedSurfaceForTests(GameObject selected, Bounds bounds)
{
    return ShouldUseManualSegmentWorkflow(selected, bounds);
}

internal static void RemoveGeneratedFlowSurfaceForTests(GameObject flowRoot)
{
    RemoveGeneratedFlowSurface(flowRoot);
}

private static bool ShouldUseManualSegmentWorkflow(GameObject selected, Bounds selectionBounds)
{
    if (selected == null)
    {
        return false;
    }

    float horizontalArea = selectionBounds.size.x * selectionBounds.size.z;
    return selectionBounds.size.x >= LargeSelectionWidthThreshold
        || selectionBounds.size.z >= LargeSelectionDepthThreshold
        || horizontalArea >= LargeSelectionAreaThreshold;
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
```

Update the unsafe path in `CreateCenterFlowEffectFromSelectedArea()`:

```csharp
if (ShouldUseManualSegmentWorkflow(selected, selectionBounds))
{
    EditorUtility.DisplayDialog(
        "Use manual center segments",
        "The selected water is a single large mesh. To avoid covering the whole model with a flow overlay, keep the base mesh for global flow and author center flow segments by hand under FlowCenterVisuals.",
        "OK");
    Selection.activeGameObject = FindOrCreateSceneRoot(selected.scene, FlowRootName);
    return;
}
```

- [ ] **Step 4: Re-read the modified tool and verifier to ensure the hazards are both covered**

Run:

```powershell
Select-String -Path 'D:\Unity\New\Assets\Scripts\Editor\Chapters\Chapter01\Chapter01ManualWaterEffectTools.cs' -Pattern 'ShouldUseManualSegmentWorkflow|RemoveGeneratedFlowSurfaceForTests|Use manual center segments'
```

Expected:

- three matches, proving the tool now blocks the old whole-model generation path and exposes the cleanup hook used by the verifier

- [ ] **Step 5: Commit the safety-guard slice**

Run:

```powershell
git -C 'D:\Unity\New' add -- 'Assets/Scripts/Editor/Chapters/Chapter01/Chapter01ManualWaterEffectTools.cs' 'Assets/Scripts/Editor/Chapters/Chapter01/Chapter01FlowSelectionVerifier.cs'
git -C 'D:\Unity\New' commit -m "fix: guard chapter01 water flow authoring"
```

Expected:

- a commit containing only the editor safety changes

### Task 2: Add The Global Whole-Water Flow Shader And Material

**Files:**
- Create: `D:/Unity/New/Assets/Shaders/Chapters/Chapter01/Chapter01GlobalFlowWater.shader`
- Create: `D:/Unity/New/Assets/Materials/Chapters/Chapter01/Water/Chapter01GlobalFlow.mat`

- [ ] **Step 1: Write the shader asset**

Create a shader that keeps the current water readable but adds low-strength motion suitable for one irregular mesh.

```shader
Shader "ZhuozhengYuan/Chapter01GlobalFlowWater"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.68, 0.9, 0.95, 0.72)
        _Alpha ("Alpha", Range(0, 1)) = 0.72
        _Tiling ("Tiling", Vector) = (3.5, 2.8, 0, 0)
        _PrimaryFlowDirection ("Primary Flow Direction", Vector) = (1, 0.12, 0, 0)
        _SecondaryFlowDirection ("Secondary Flow Direction", Vector) = (-0.35, 1, 0, 0)
        _PrimaryFlowSpeed ("Primary Flow Speed", Range(0, 1)) = 0.06
        _SecondaryFlowSpeed ("Secondary Flow Speed", Range(0, 1)) = 0.03
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.14
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
            float _Alpha;
            float4 _Tiling;
            float4 _PrimaryFlowDirection;
            float4 _SecondaryFlowDirection;
            float _PrimaryFlowSpeed;
            float _SecondaryFlowSpeed;
            float _FresnelStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = _WorldSpaceCameraPos.xyz - worldPos.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 tiling = max(_Tiling.xy, float2(0.01, 0.01));
                float2 primaryDir = normalize(max(abs(_PrimaryFlowDirection.xy), 0.0001) * sign(_PrimaryFlowDirection.xy + 0.0001));
                float2 secondaryDir = normalize(max(abs(_SecondaryFlowDirection.xy), 0.0001) * sign(_SecondaryFlowDirection.xy + 0.0001));
                float2 uv = i.uv * tiling;
                float t = _Time.y;

                fixed4 sampleA = tex2D(_MainTex, uv + primaryDir * (t * _PrimaryFlowSpeed));
                fixed4 sampleB = tex2D(_MainTex, uv + secondaryDir * (t * _SecondaryFlowSpeed) + float2(0.11, 0.07));
                fixed3 color = lerp(sampleA.rgb, sampleB.rgb, 0.35) * _Tint.rgb;

                float3 worldNormal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                float fresnel = pow(1.0 - saturate(dot(worldNormal, viewDir)), 3.0) * _FresnelStrength;
                color += fresnel;

                return fixed4(color, saturate(_Alpha * _Tint.a));
            }
            ENDCG
        }
    }
}
```

- [ ] **Step 2: Create the Chapter01-only global material**

Author the material with the same source texture family as the current water look so the global layer stays visually close to what is already in the scene.

Use these values:

```text
Shader: ZhuozhengYuan/Chapter01GlobalFlowWater
MainTex: Water_Pool_Light.jpg
Tint: (0.68, 0.90, 0.95, 0.72)
Alpha: 0.72
Tiling: (3.5, 2.8, 0, 0)
PrimaryFlowDirection: (1, 0.12, 0, 0)
SecondaryFlowDirection: (-0.35, 1, 0, 0)
PrimaryFlowSpeed: 0.06
SecondaryFlowSpeed: 0.03
FresnelStrength: 0.14
```

- [ ] **Step 3: Verify the new assets exist and are isolated from the shared imported material**

Run:

```powershell
Get-ChildItem 'D:\Unity\New\Assets\Shaders\Chapters\Chapter01' -Filter 'Chapter01GlobalFlowWater.shader'
Get-ChildItem 'D:\Unity\New\Assets\Materials\Chapters\Chapter01\Water' -Filter 'Chapter01GlobalFlow.mat'
```

Expected:

- one shader file and one material file found at the Chapter01-specific paths

- [ ] **Step 4: Commit the global flow asset slice**

Run:

```powershell
git -C 'D:\Unity\New' add -- 'Assets/Shaders/Chapters/Chapter01/Chapter01GlobalFlowWater.shader' 'Assets/Materials/Chapters/Chapter01/Water/Chapter01GlobalFlow.mat'
git -C 'D:\Unity\New' commit -m "feat: add chapter01 global water flow assets"
```

Expected:

- a commit with only the global flow shader and material

### Task 3: Update The Editor Workflow For Safe Water Authoring

**Files:**
- Modify: `D:/Unity/New/Assets/Scripts/Editor/Chapters/Chapter01/Chapter01ManualWaterEffectTools.cs`

- [ ] **Step 1: Add a menu command to apply the Chapter01 global-flow material to the selected water renderer**

Add a menu command that operates on the selected renderer only. Do not mutate the imported shared material asset in place.

```csharp
private const string ApplyGlobalFlowMenuPath = "Tools/Zhuozhengyuan/Apply Chapter01 Global Water Flow To Selected Renderer";
private const string GeneratedGlobalFlowMaterialPath = "Assets/Materials/Chapters/Chapter01/Water/Chapter01GlobalFlow.mat";

[MenuItem(ApplyGlobalFlowMenuPath)]
public static void ApplyGlobalFlowToSelectedRenderer()
{
    var selected = Selection.activeGameObject;
    if (selected == null)
    {
        EditorUtility.DisplayDialog("No selection", "Select the authored water mesh renderer first.", "OK");
        return;
    }

    var renderer = selected.GetComponent<Renderer>();
    var flowMaterial = AssetDatabase.LoadAssetAtPath<Material>(GeneratedGlobalFlowMaterialPath);
    if (renderer == null || flowMaterial == null)
    {
        EditorUtility.DisplayDialog("Global flow unavailable", "Missing renderer or Chapter01GlobalFlow material.", "OK");
        return;
    }

    var materials = renderer.sharedMaterials;
    for (int i = 0; i < materials.Length; i++)
    {
        if (materials[i] != null && string.Equals(materials[i].name, "Water_Pool_Light", StringComparison.Ordinal))
        {
            materials[i] = flowMaterial;
        }
    }

    renderer.sharedMaterials = materials;
    EditorUtility.SetDirty(renderer);
}
```

- [ ] **Step 2: Add a manual center-segment creation helper instead of reusing the old whole-surface generator**

Add a separate helper that creates one narrow segment under `FlowCenterVisuals` at the current Scene view pivot or selected transform so the artist can place three segments by hand.

```csharp
private const string CreateCenterSegmentMenuPath = "Tools/Zhuozhengyuan/Create Center Flow Segment";

[MenuItem(CreateCenterSegmentMenuPath)]
public static void CreateCenterFlowSegment()
{
    Scene scene = SceneManager.GetActiveScene();
    GameObject flowRoot = FindOrCreateSceneRoot(scene, FlowRootName);
    Material flowMaterial = GetOrCreateCenterFlowMaterial();

    GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Quad);
    segment.name = GetNextSegmentName(flowRoot.transform);
    SceneManager.MoveGameObjectToScene(segment, scene);
    segment.transform.SetParent(flowRoot.transform, false);
    segment.transform.localScale = new Vector3(8f, 2.5f, 1f);

    Collider collider = segment.GetComponent<Collider>();
    if (collider != null)
    {
        Object.DestroyImmediate(collider);
    }

    MeshRenderer renderer = segment.GetComponent<MeshRenderer>();
    renderer.sharedMaterial = flowMaterial;
    renderer.shadowCastingMode = ShadowCastingMode.Off;
    renderer.receiveShadows = false;

    BindCenterFlowObject(scene, flowRoot);
    Selection.activeGameObject = segment;
    EditorGUIUtility.PingObject(segment);
}
```

- [ ] **Step 3: Preserve existing manual transparent raised helpers during tool cleanup**

Restrict cleanup to generated objects only.

```csharp
private static bool IsGeneratedCenterFlowObject(GameObject candidate)
{
    if (candidate == null)
    {
        return false;
    }

    return string.Equals(candidate.name, FlowSurfaceName, StringComparison.Ordinal)
        || string.Equals(candidate.name, LegacyOverlayName, StringComparison.Ordinal)
        || string.Equals(candidate.name, LegacyEffectName, StringComparison.Ordinal);
}
```

Use this in cleanup and binding filters instead of broad removal of unknown flow-related children.

- [ ] **Step 4: Verify the new menu paths and safe-object filter exist**

Run:

```powershell
Select-String -Path 'D:\Unity\New\Assets\Scripts\Editor\Chapters\Chapter01\Chapter01ManualWaterEffectTools.cs' -Pattern 'Apply Chapter01 Global Water Flow To Selected Renderer|Create Center Flow Segment|IsGeneratedCenterFlowObject'
```

Expected:

- three matches for the new safer workflow entry points

- [ ] **Step 5: Commit the editor workflow slice**

Run:

```powershell
git -C 'D:\Unity\New' add -- 'Assets/Scripts/Editor/Chapters/Chapter01/Chapter01ManualWaterEffectTools.cs'
git -C 'D:\Unity\New' commit -m "feat: add chapter01 safe water authoring tools"
```

Expected:

- a commit that changes only the editor workflow

### Task 4: Author The Scene Water Layers In Garden_Main

**Files:**
- Modify: `D:/Unity/New/Assets/Scenes/Garden_Main.unity`

- [ ] **Step 1: Apply the dedicated global-flow material to the authored water mesh**

In Unity Editor:

1. Open `D:/Unity/New/Assets/Scenes/Garden_Main.unity`
2. Select the authored water object currently shown as `GardenModel/.../Mesh3825`
3. In its renderer materials, replace only the water slot that currently uses `Water_Pool_Light` with `Chapter01GlobalFlow`
4. Leave the rock and bank materials untouched

Expected:

- the whole one-piece water mesh keeps its original silhouette and color family
- only the water material slot changes

- [ ] **Step 2: Preserve the existing transparent raised helper object**

If there is already a manually added transparent raised helper object in the scene:

1. Move it under `FlowCenterVisuals` if it belongs to the center-channel overlay workflow
2. Rename it to a manual name that is not one of the generated cleanup names, for example `CenterFlowPreviewHelper`
3. Keep its position and height offset as the baseline reference for the overlay stack

Expected:

- the object remains in scene after tool cleanup
- it is visually separable from generated surfaces and named segments

- [ ] **Step 3: Create three center-flow segments under FlowCenterVisuals**

In Unity Editor:

1. Use `Tools/Zhuozhengyuan/Create Center Flow Segment` three times
2. Rename and place them as:
   - `CenterFlowSegment_01`
   - `CenterFlowSegment_02`
   - `CenterFlowSegment_03`
3. Position them along the center route from `FlowSelectorInteractable` toward `Chapter01PagePickup`
4. Keep each segment about `0.02` to `0.05` units above the base water mesh
5. Slightly overlap adjacent segments to avoid visible seams

Expected:

- no one segment covers the entire water network
- each segment follows a local bend of the route

- [ ] **Step 4: Bind only the center-flow root to Chapter01Environment**

Open `Chapter01Environment` in the scene and confirm:

- `flowingObjects` contains `FlowCenterVisuals`
- `flowingObjects` does not contain the base water mesh `Mesh3825`

Expected:

- global flow is always visible through the base material
- only the center overlay root is toggled by solved state

- [ ] **Step 5: Save and verify the scene diff is limited to intended objects**

Run:

```powershell
git -C 'D:\Unity\New' diff -- 'Assets/Scenes/Garden_Main.unity'
```

Expected:

- material swap on the water mesh
- additions or moves for `FlowCenterVisuals` children
- no accidental whole-model generated center-flow surface spanning the entire water network

- [ ] **Step 6: Commit the scene authoring slice**

Run:

```powershell
git -C 'D:\Unity\New' add -- 'Assets/Scenes/Garden_Main.unity'
git -C 'D:\Unity\New' commit -m "feat: author chapter01 layered water visuals"
```

Expected:

- one scene-focused commit for the water visual authoring

### Task 5: Verify Runtime Behavior And Visual Safety

**Files:**
- Verify: `D:/Unity/New/Assets/Scenes/Garden_Main.unity`
- Verify: `D:/Unity/New/Assets/Scripts/Chapters/Chapter01/Chapter01EnvironmentController.cs`
- Verify: `D:/Unity/New/Assets/Scripts/Editor/Chapters/Chapter01/Chapter01FlowSelectionVerifier.cs`

- [ ] **Step 1: Run the editor verifier for workflow safety**

Open the verifier file and invoke the method from an explicit editor menu item or a dedicated one-line editor invocation:

```csharp
Chapter01FlowSelectionVerifier.RunWaterWorkflowSafeguards();
```

Expected:

- no exception
- generated center-flow surface cleanup removes only generated objects
- large single-mesh water selection is classified as manual-segment workflow

- [ ] **Step 2: Verify edit-mode scene state**

In Unity Editor scene view confirm:

- the whole water mesh shows the new global-flow material
- the transparent raised helper still exists if it existed before
- the three center segments are narrow and centered on the main route
- no segment sits obviously above the banks

Expected:

- no visible whole-model overlay

- [ ] **Step 3: Verify runtime state progression in Play Mode**

In Play Mode walk through Chapter01:

1. Before solving the flow puzzle, confirm the whole water mesh already has subtle motion
2. Choose a wrong direction and confirm the existing wrong-choice feedback still plays
3. Solve the flow puzzle and confirm `FlowCenterVisuals` becomes visibly stronger than the base water
4. Reveal the page and confirm no existing page logic regressed

Expected:

- global flow always present
- center route only becomes visually dominant after success
- no regression in the Chapter01 interaction loop

- [ ] **Step 4: Capture the final modified file list**

Run:

```powershell
git -C 'D:\Unity\New' status --short
```

Expected:

- only intended water-flow implementation files remain changed before final branch-finishing work

- [ ] **Step 5: Announce branch completion workflow**

After the above verification passes, stop implementation and switch to the required completion workflow:

```text
I'm using the finishing-a-development-branch skill to complete this work.
```

Then follow the finishing skill before any final merge, push, or PR action.
