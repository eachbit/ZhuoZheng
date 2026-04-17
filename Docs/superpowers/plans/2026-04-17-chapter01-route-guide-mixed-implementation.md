# 第一章混合式路线引导美化实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不影响第一章现有玩法逻辑的前提下，将当前路线引导升级为“流光主路线 + 关键节点局部地贴 + 终点轻量标记”的混合式场景引导。

**Architecture:** 继续复用 `Chapter01AuthoredRouteGuide` 作为路径解析、贴地采样、显隐时机和裁障碍入口，只升级表现层。主路线仍由现有 ribbon 段生成，新增节点装饰层与终点标记层，并通过 EditMode 测试锁定“表现增强但不改游玩逻辑”的边界。

**Tech Stack:** Unity 2022.3 / URP 14 / C# / 自定义 Shader / UGUI / NUnit EditMode Tests

---

## 文件结构

### 新增文件

- `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\Chapter01GuideDecorationProfile.cs`
  - 承载关键节点地贴、终点标记、颜色和动效参数的轻量配置结构
- `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01RouteGuideVisualTests.cs`
  - 锁定混合式路线表现生成与基础约束的 EditMode 测试

### 修改文件

- `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\Chapter01AuthoredRouteGuide.cs`
  - 继续作为主路线入口，新增节点装饰层、终点标记层、表现参数与运行时显隐控制
- `D:\Unity\New\Assets\Shaders\GuideRibbon.shader`
  - 强化现有路线 shader，让主路线更接近“青绿水光 + 淡金高光”
- `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01UI.EditMode.asmdef`
  - 保证新增路线测试能编译运行
- `D:\Unity\New\Assets\Scenes\Garden_Main.unity`
  - 如有必要，仅挂接新增可调参数，不改章节触发和目标逻辑

---

### Task 1：先补路线表现测试，锁定混合方案边界

**Files:**
- Modify: `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01UI.EditMode.asmdef`
- Create: `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01RouteGuideVisualTests.cs`

- [ ] **Step 1: 为路线测试补齐程序集引用**

把 `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01UI.EditMode.asmdef` 更新为：

```json
{
  "name": "Chapter01UI.EditMode",
  "references": [
    "Unity.TextMeshPro",
    "UnityEngine.UI"
  ],
  "optionalUnityReferences": [
    "TestAssemblies"
  ],
  "includePlatforms": [
    "Editor"
  ]
}
```

- [ ] **Step 2: 写第一个失败测试，锁定路线必须保留主路线与装饰层根节点**

创建 `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01RouteGuideVisualTests.cs`：

```csharp
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter01RouteGuideVisualTests
    {
        [Test]
        public void RebuildGuide_ShouldCreateRibbonAndDecorationRoots()
        {
            Type guideType = Type.GetType("ZhuozhengYuan.Chapter01AuthoredRouteGuide, Assembly-CSharp");
            Assert.IsNotNull(guideType, "Chapter01AuthoredRouteGuide 尚未创建。");

            GameObject root = new GameObject("RouteGuideRoot");
            MonoBehaviour guide = (MonoBehaviour)root.AddComponent(guideType);

            GameObject start = new GameObject("Start");
            GameObject mid = new GameObject("GuidePoint_00");
            GameObject end = new GameObject("End");
            start.transform.position = new Vector3(0f, 0f, 0f);
            mid.transform.position = new Vector3(2f, 0f, 4f);
            end.transform.position = new Vector3(5f, 0f, 8f);

            SetField(guide, "showGuideOnStart", true);
            SetField(guide, "playerStartPose", start.transform);
            SetField(guide, "targetGate", end.transform);
            SetField(guide, "routePoints", new[] { mid.transform });
            SetField(guide, "trimSegmentsAgainstObstacles", false);
            SetField(guide, "groundOffset", 0f);

            Invoke(guide, "RebuildGuide");

            Transform runtimeRoot = root.transform.Find("Chapter01AuthoredGuideRoot");
            Assert.IsNotNull(runtimeRoot, "主路线根节点未创建。");
            Assert.IsNotNull(runtimeRoot.Find("DecorationsRoot"), "局部装饰层根节点未创建。");
            Assert.IsNotNull(runtimeRoot.Find("DestinationMarkerRoot"), "终点标记根节点未创建。");

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(start);
            UnityEngine.Object.DestroyImmediate(mid);
            UnityEngine.Object.DestroyImmediate(end);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"字段 {fieldName} 不存在。");
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"方法 {methodName} 不存在。");
            method.Invoke(target, Array.Empty<object>());
        }
    }
}
```

- [ ] **Step 3: 运行测试，确认当前版本先失败**

Run:

```powershell
"<UnityEditorPath>" -batchmode -projectPath "D:\Unity\New" -runTests -testPlatform EditMode -testFilter ZhuozhengYuan.Tests.EditMode.Chapter01RouteGuideVisualTests -logFile -
```

Expected:

- 失败
- 报出 `DecorationsRoot` 或 `DestinationMarkerRoot` 尚未创建

- [ ] **Step 4: 再写第二个失败测试，锁定局部地贴只能做“少量关键节点”**

在同一个测试文件中追加：

```csharp
[Test]
public void RebuildGuide_ShouldCreateLimitedDecorationMarkers()
{
    Type guideType = Type.GetType("ZhuozhengYuan.Chapter01AuthoredRouteGuide, Assembly-CSharp");
    Assert.IsNotNull(guideType, "Chapter01AuthoredRouteGuide 尚未创建。");

    GameObject root = new GameObject("RouteGuideRoot");
    MonoBehaviour guide = (MonoBehaviour)root.AddComponent(guideType);

    Transform[] points = new Transform[4];
    for (int i = 0; i < points.Length; i++)
    {
        GameObject point = new GameObject("GuidePoint_" + i.ToString("00"));
        point.transform.position = new Vector3(i * 3f, 0f, i * 4f);
        points[i] = point.transform;
    }

    SetField(guide, "showGuideOnStart", true);
    SetField(guide, "playerStartPose", points[0]);
    SetField(guide, "targetGate", points[3]);
    SetField(guide, "routePoints", new[] { points[1], points[2] });
    SetField(guide, "trimSegmentsAgainstObstacles", false);
    SetField(guide, "groundOffset", 0f);

    Invoke(guide, "RebuildGuide");

    Transform runtimeRoot = root.transform.Find("Chapter01AuthoredGuideRoot");
    Transform decorationsRoot = runtimeRoot != null ? runtimeRoot.Find("DecorationsRoot") : null;
    Assert.IsNotNull(decorationsRoot, "局部装饰层根节点未创建。");
    Assert.LessOrEqual(decorationsRoot.childCount, 6, "局部地贴数量过多，容易破坏画面。");

    UnityEngine.Object.DestroyImmediate(root);
    for (int i = 0; i < points.Length; i++)
    {
        UnityEngine.Object.DestroyImmediate(points[i].gameObject);
    }
}
```

- [ ] **Step 5: 运行新增测试，确认仍然失败**

Run:

```powershell
"<UnityEditorPath>" -batchmode -projectPath "D:\Unity\New" -runTests -testPlatform EditMode -testFilter ZhuozhengYuan.Tests.EditMode.Chapter01RouteGuideVisualTests -logFile -
```

Expected:

- 失败
- 当前代码尚未生成局部装饰层

- [ ] **Step 6: 提交测试脚手架**

```powershell
git add Assets\Tests\EditMode\UI\Chapter01UI.EditMode.asmdef Assets\Tests\EditMode\UI\Chapter01RouteGuideVisualTests.cs
git commit -m "test: add chapter01 route guide visual constraints"
```

---

### Task 2：给路线引导增加混合式表现配置与运行时层级

**Files:**
- Create: `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\Chapter01GuideDecorationProfile.cs`
- Modify: `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\Chapter01AuthoredRouteGuide.cs`

- [ ] **Step 1: 新增路线装饰配置结构**

创建 `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\Chapter01GuideDecorationProfile.cs`：

```csharp
using UnityEngine;

namespace ZhuozhengYuan
{
    [System.Serializable]
    public struct Chapter01GuideDecorationProfile
    {
        public Color ribbonBaseColor;
        public Color ribbonHighlightColor;
        public Color decorationPrimaryColor;
        public Color decorationSecondaryColor;
        public Color destinationMarkerColor;
        public float decorationSpacing;
        public float decorationSize;
        public float destinationMarkerHeight;
        public float destinationMarkerScale;
        public float animationSpeed;

        public static Chapter01GuideDecorationProfile CreateDefault()
        {
            return new Chapter01GuideDecorationProfile
            {
                ribbonBaseColor = new Color(0.34f, 0.68f, 0.62f, 0.78f),
                ribbonHighlightColor = new Color(0.95f, 0.88f, 0.66f, 0.92f),
                decorationPrimaryColor = new Color(0.66f, 0.86f, 0.82f, 0.42f),
                decorationSecondaryColor = new Color(0.92f, 0.84f, 0.58f, 0.28f),
                destinationMarkerColor = new Color(0.98f, 0.92f, 0.74f, 0.88f),
                decorationSpacing = 4f,
                decorationSize = 1.05f,
                destinationMarkerHeight = 1.6f,
                destinationMarkerScale = 0.72f,
                animationSpeed = 1.1f
            };
        }
    }
}
```

- [ ] **Step 2: 在路线脚本中补齐混合引导字段**

在 `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\Chapter01AuthoredRouteGuide.cs` 的字段区加入：

```csharp
[Header("Decoration")]
public bool useHybridDecorations = true;
public bool useDestinationMarker = true;
public Chapter01GuideDecorationProfile decorationProfile = default;
public int maxDecorationMarkers = 6;

private Transform _decorationsRoot;
private Transform _destinationMarkerRoot;
private Material _decorationMaterial;
private Material _destinationMarkerMaterial;
```

并在 `Awake` 或 `Initialize` 路径中补一段默认值保障：

```csharp
if (decorationProfile.decorationSpacing <= 0.01f)
{
    decorationProfile = Chapter01GuideDecorationProfile.CreateDefault();
}
```

- [ ] **Step 3: 为运行时根节点创建主路线、装饰层、终点标记三层结构**

把 `RebuildGuide()` 中创建 `_runtimeRoot` 的部分改成：

```csharp
_runtimeRoot = new GameObject("Chapter01AuthoredGuideRoot").transform;
_runtimeRoot.SetParent(transform, false);

_decorationsRoot = new GameObject("DecorationsRoot").transform;
_decorationsRoot.SetParent(_runtimeRoot, false);

_destinationMarkerRoot = new GameObject("DestinationMarkerRoot").transform;
_destinationMarkerRoot.SetParent(_runtimeRoot, false);
```

- [ ] **Step 4: 跑测试，确认层级根节点通过**

Run:

```powershell
"<UnityEditorPath>" -batchmode -projectPath "D:\Unity\New" -runTests -testPlatform EditMode -testFilter ZhuozhengYuan.Tests.EditMode.Chapter01RouteGuideVisualTests.RebuildGuide_ShouldCreateRibbonAndDecorationRoots -logFile -
```

Expected:

- PASS

- [ ] **Step 5: 提交运行时层级与配置结构**

```powershell
git add Assets\Scripts\Chapters\Chapter01\Chapter01GuideDecorationProfile.cs Assets\Scripts\Chapters\Chapter01\Chapter01AuthoredRouteGuide.cs
git commit -m "feat: add chapter01 hybrid guide runtime scaffolding"
```

---

### Task 3：升级主路线丝带质感并加入关键节点局部地贴

**Files:**
- Modify: `D:\Unity\New\Assets\Shaders\GuideRibbon.shader`
- Modify: `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\Chapter01AuthoredRouteGuide.cs`

- [ ] **Step 1: 扩展 shader，让主路线更接近“水光 + 淡金高光”**

把 `D:\Unity\New\Assets\Shaders\GuideRibbon.shader` 的属性扩展为：

```shader
_BaseColor ("Base Color", Color) = (0.34, 0.68, 0.62, 0.78)
_AccentColor ("Accent Color", Color) = (0.95, 0.88, 0.66, 0.92)
_SecondaryColor ("Secondary Color", Color) = (0.70, 0.92, 0.88, 0.55)
_FlowSpeed ("Flow Speed", Float) = 2.0
_PulseSpeed ("Pulse Speed", Float) = 1.1
_EdgeSoftness ("Edge Softness", Range(0.1, 3.0)) = 1.6
_AlphaScale ("Alpha Scale", Range(0.0, 2.0)) = 1.0
_GlowBoost ("Glow Boost", Range(0.0, 6.0)) = 2.6
_RippleStrength ("Ripple Strength", Range(0.0, 2.0)) = 0.45
```

并将颜色计算替换为：

```hlsl
float ripple = 0.5 + 0.5 * sin(input.uv.y * 11.0 + _Time.y * (_FlowSpeed * 1.2));
float stream = 0.5 + 0.5 * sin(input.uv.y * 18.0 - _Time.y * (_FlowSpeed * 4.0));
float crest = smoothstep(0.74, 0.98, stream) * edgeMask;
float aqua = saturate(edgeMask * 0.55 + ripple * _RippleStrength);

float3 color = lerp(_BaseColor.rgb, _SecondaryColor.rgb, aqua);
color += _AccentColor.rgb * crest * _GlowBoost;

float alpha = saturate((_BaseColor.a * (0.26 + edgeMask * 0.74) + crest * 0.32) * _AlphaScale);
return half4(color, alpha * pulse);
```

- [ ] **Step 2: 在路线脚本里给主路线材质注入混合方案颜色**

把 `EnsureMaterials()` 里的颜色设置更新为：

```csharp
Color mainColor = decorationProfile.ribbonBaseColor;
mainColor.a = Mathf.Clamp01(idleAlpha);
ApplyMaterialColors(_mainGuideMaterial, mainColor, decorationProfile.ribbonHighlightColor, 1f, 2.8f);
ApplyMaterialColors(_mistGuideMaterial, decorationProfile.decorationPrimaryColor, decorationProfile.decorationSecondaryColor, 0.72f, 1.8f);
ApplyMaterialColors(_crestGuideMaterial, decorationProfile.ribbonHighlightColor, Color.white, 1.0f, 3.4f);
```

- [ ] **Step 3: 新增关键节点装饰层生成方法**

在 `Chapter01AuthoredRouteGuide.cs` 中加入：

```csharp
private void BuildDecorations(List<Vector3> displayPoints)
{
    if (!useHybridDecorations || _decorationsRoot == null || displayPoints == null || displayPoints.Count < 2)
    {
        return;
    }

    int created = 0;
    for (int i = 1; i < displayPoints.Count - 1 && created < Mathf.Max(1, maxDecorationMarkers); i += 2)
    {
        Vector3 point = displayPoints[i];
        Transform marker = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        marker.name = "DecorationMarker_" + created.ToString("00");
        marker.SetParent(_decorationsRoot, false);
        marker.position = point + Vector3.up * 0.06f;
        marker.rotation = Quaternion.Euler(90f, 0f, 0f);
        marker.localScale = Vector3.one * Mathf.Max(0.4f, decorationProfile.decorationSize);

        Collider collider = marker.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = _decorationMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        created++;
    }
}
```

- [ ] **Step 4: 在 `RebuildGuide()` 末尾接入装饰层构建**

在 `_segments` 构建完成并且 `_guideHidden` 判断之后，加入：

```csharp
BuildDecorations(displayPoints);
BuildDestinationMarker();
```

- [ ] **Step 5: 跑测试，确认局部装饰数量受控**

Run:

```powershell
"<UnityEditorPath>" -batchmode -projectPath "D:\Unity\New" -runTests -testPlatform EditMode -testFilter ZhuozhengYuan.Tests.EditMode.Chapter01RouteGuideVisualTests.RebuildGuide_ShouldCreateLimitedDecorationMarkers -logFile -
```

Expected:

- PASS

- [ ] **Step 6: 提交主路线与节点装饰层**

```powershell
git add Assets\Shaders\GuideRibbon.shader Assets\Scripts\Chapters\Chapter01\Chapter01AuthoredRouteGuide.cs
git commit -m "feat: add hybrid ribbon and node decorations for chapter01 route"
```

---

### Task 4：增加终点轻量标记并补齐淡出行为验证

**Files:**
- Modify: `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\Chapter01AuthoredRouteGuide.cs`
- Modify: `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01RouteGuideVisualTests.cs`

- [ ] **Step 1: 增加终点标记材质与构建逻辑**

在 `Chapter01AuthoredRouteGuide.cs` 中加入：

```csharp
private void BuildDestinationMarker()
{
    if (!useDestinationMarker || _destinationMarkerRoot == null || targetGate == null)
    {
        return;
    }

    Transform ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
    ring.name = "DestinationMarker_Ring";
    ring.SetParent(_destinationMarkerRoot, false);
    ring.position = targetGate.position + Vector3.up * decorationProfile.destinationMarkerHeight;
    ring.localScale = new Vector3(
        decorationProfile.destinationMarkerScale,
        0.03f,
        decorationProfile.destinationMarkerScale);

    Collider collider = ring.GetComponent<Collider>();
    if (collider != null)
    {
        Destroy(collider);
    }

    MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
    if (renderer != null)
    {
        renderer.sharedMaterial = _destinationMarkerMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
```

- [ ] **Step 2: 补齐淡出时对装饰层与终点标记层的统一处理**

把 `DestroyRuntimeGuide()` 保持为：

```csharp
private void DestroyRuntimeGuide()
{
    if (_revealRoutine != null)
    {
        StopCoroutine(_revealRoutine);
        _revealRoutine = null;
    }

    if (_fadeRoutine != null)
    {
        StopCoroutine(_fadeRoutine);
        _fadeRoutine = null;
    }

    if (_runtimeRoot != null)
    {
        Destroy(_runtimeRoot.gameObject);
        _runtimeRoot = null;
    }

    _decorationsRoot = null;
    _destinationMarkerRoot = null;
    _segments.Clear();
}
```

- [ ] **Step 3: 再补一个测试，锁定终点标记必须创建**

在 `Chapter01RouteGuideVisualTests.cs` 中追加：

```csharp
[Test]
public void RebuildGuide_ShouldCreateDestinationMarkerChild()
{
    Type guideType = Type.GetType("ZhuozhengYuan.Chapter01AuthoredRouteGuide, Assembly-CSharp");
    Assert.IsNotNull(guideType, "Chapter01AuthoredRouteGuide 尚未创建。");

    GameObject root = new GameObject("RouteGuideRoot");
    MonoBehaviour guide = (MonoBehaviour)root.AddComponent(guideType);

    GameObject start = new GameObject("Start");
    GameObject end = new GameObject("End");
    start.transform.position = Vector3.zero;
    end.transform.position = new Vector3(5f, 0f, 5f);

    SetField(guide, "showGuideOnStart", true);
    SetField(guide, "playerStartPose", start.transform);
    SetField(guide, "targetGate", end.transform);
    SetField(guide, "routePoints", Array.Empty<Transform>());
    SetField(guide, "trimSegmentsAgainstObstacles", false);
    SetField(guide, "groundOffset", 0f);

    Invoke(guide, "RebuildGuide");

    Transform markerRoot = root.transform.Find("Chapter01AuthoredGuideRoot/DestinationMarkerRoot");
    Assert.IsNotNull(markerRoot, "终点标记根节点未创建。");
    Assert.Greater(markerRoot.childCount, 0, "终点标记内容未创建。");

    UnityEngine.Object.DestroyImmediate(root);
    UnityEngine.Object.DestroyImmediate(start);
    UnityEngine.Object.DestroyImmediate(end);
}
```

- [ ] **Step 4: 运行新增路线测试，确认全部通过**

Run:

```powershell
"<UnityEditorPath>" -batchmode -projectPath "D:\Unity\New" -runTests -testPlatform EditMode -testFilter ZhuozhengYuan.Tests.EditMode.Chapter01RouteGuideVisualTests -logFile -
```

Expected:

- PASS

- [ ] **Step 5: 做一次脚本级回归编译检查**

Run:

```powershell
Get-Content "$env:LOCALAPPDATA\Unity\Editor\Editor.log" | Select-String -Pattern "Chapter01AuthoredRouteGuide|Chapter01RouteGuideVisualTests|error CS"
```

Expected:

- 没有与 `Chapter01AuthoredRouteGuide.cs` 或 `Chapter01RouteGuideVisualTests.cs` 相关的 `error CS`

- [ ] **Step 6: 提交终点标记与测试闭环**

```powershell
git add Assets\Scripts\Chapters\Chapter01\Chapter01AuthoredRouteGuide.cs Assets\Tests\EditMode\UI\Chapter01RouteGuideVisualTests.cs
git commit -m "feat: add destination marker for chapter01 route guide"
```

---

### Task 5：场景调参与最终人工验证

**Files:**
- Modify: `D:\Unity\New\Assets\Scenes\Garden_Main.unity`（仅当需要暴露或调整参数）

- [ ] **Step 1: 在场景中确认路线脚本参数默认值**

在 `Garden_Main` 中检查或设置：

```text
guideWidth = 2.0 ~ 2.3
groundOffset = 0.06 ~ 0.09
idleAlpha = 0.62 ~ 0.74
useHybridDecorations = true
maxDecorationMarkers = 4 ~ 6
useDestinationMarker = true
```

- [ ] **Step 2: 手动验证第一章开局引导**

验证项：

- 开局能看见主路线
- 转弯处能看到少量节点装饰
- 终点附近有轻量标记
- 画面没有被路线特效“刷白”

- [ ] **Step 3: 手动验证接近目标后的淡出**

验证项：

- 玩家靠近左侧暗闸后路线消失
- 对话、交互提示、暗闸 UI 不受影响

- [ ] **Step 4: 手动验证逻辑回归**

验证项：

- 第一章目标文本逻辑不变
- 左右暗闸与水流选择逻辑不变
- 第二章目标切换门槛不变

- [ ] **Step 5: 提交场景参数收尾**

```powershell
git add Assets\Scenes\Garden_Main.unity
git commit -m "tune: polish chapter01 hybrid route guide in scene"
```

---

## 计划自检

### Spec coverage

- 已覆盖主路线美化：Task 2 / Task 3
- 已覆盖局部地贴辅层：Task 3
- 已覆盖终点轻量标记：Task 4
- 已覆盖“不影响逻辑”的验证：Task 1 / Task 4 / Task 5

### Placeholder scan

- 没有保留 TBD / TODO / “稍后实现”
- 每个 task 都给出明确文件、命令与代码

### Type consistency

- 混合方案统一使用 `Chapter01AuthoredRouteGuide`
- 新增配置统一落在 `Chapter01GuideDecorationProfile`
- 根节点命名统一为 `Chapter01AuthoredGuideRoot / DecorationsRoot / DestinationMarkerRoot`
