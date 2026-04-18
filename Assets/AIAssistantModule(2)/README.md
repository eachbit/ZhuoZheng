# AIAssistantModule

## 作用

这是从当前项目中抽离出来的一套可复用 AI 助手模块，目标是尽量独立地迁移到其他 Unity 工程。

它包含：

- 运行时自动注入的 AI 助手
- 可固定在 Hierarchy 中手动调整布局的编辑模式
- 文本问答
- 语音录音与识别
- 截取当前实验画面并做视觉问答
- 单张头像 / 序列帧头像
- 多种运动模式与停靠位置配置


## 目录结构

```text
AIAssistantModule/
├── README.md
├── Scripts/
│   ├── Editor/
│   │   └── WenxinAssistantEditorMenu.cs
│   ├── WavUtility.cs
│   ├── WenxinAssistantBootstrap.cs
│   ├── WenxinAssistantConfig.cs
│   └── WenxinAssistantController.cs
└── StreamingAssets/
    ├── WenxinAssistantAvatar.png
    ├── WenxinAssistantConfig.json
    ├── WenxinAssistantConfig.README.md
    └── WenxinAssistantFrames/
```


## 如何导入到其他 Unity 工程

最方便的方式不是手工挑文件，而是直接把这个模块目录里的内容整体带过去。

推荐做法：

1. 在目标工程中创建：
   - `Assets/AIAssistantModule/Scripts/`
   - `Assets/AIAssistantModule/Scripts/Editor/`
   - `Assets/StreamingAssets/`
2. 直接复制本模块下的：
   - `Scripts/`
   - `StreamingAssets/`
3. 只改 `WenxinAssistantConfig.json`
4. 进入 Unity 让它自动重新编译

### 1. 复制脚本

把 `Scripts/` 整个目录复制到目标工程，例如：

- `Assets/AIAssistantModule/Scripts/`

主要文件如下：

- `WavUtility.cs`
- `WenxinAssistantBootstrap.cs`
- `WenxinAssistantConfig.cs`
- `WenxinAssistantController.cs`
- `Editor/WenxinAssistantEditorMenu.cs`

说明：

- 前 4 个是运行时核心脚本
- `Editor/WenxinAssistantEditorMenu.cs` 用于在目标工程里一键创建可编辑的助手层级


### 2. 复制 StreamingAssets 内容

把 `StreamingAssets/` 下内容复制到目标工程的：

- `Assets/StreamingAssets/`

至少包括：

- `WenxinAssistantConfig.json`
- `WenxinAssistantAvatar.png`
- `WenxinAssistantFrames/`


### 3. 修改配置

编辑：

- `Assets/StreamingAssets/WenxinAssistantConfig.json`

字段说明见：

- `Assets/StreamingAssets/WenxinAssistantConfig.README.md`


### 4. 运行测试

进入 Unity 后重新编译，然后直接运行任意场景即可。  
该模块使用运行时自动注入，不需要手动在场景里挂 prefab。

如果你想手动调整按钮和输入框布局，可以在目标工程里使用菜单：

- `GameObject > UI > Wenxin Assistant > Editable Assistant`

这样生成出来的助手会固定出现在 Hierarchy 中，你可以直接拖动和改尺寸。


## 目标工程需要满足的条件

- Unity 支持 `UnityEngine.UI`
- 允许 `UnityWebRequest`
- 允许 `Microphone`
- 工程中存在 `StreamingAssets`


## 当前已处理的独立性

这版模块已经去掉了对当前项目 `StaticClass` 的硬依赖。  
项目名称和实验类型通过配置文件读取：

- `assistantProjectName`
- `assistantExperimentType`

因此迁移到其他工程时，不需要再复制当前项目的业务状态类。


## 如果要继续做成更标准的通用模块

后续还可以继续整理成：

- UnityPackage
- UPM 包
- 独立插件目录

当前这版已经适合“直接复制到另一个工程里使用”。
