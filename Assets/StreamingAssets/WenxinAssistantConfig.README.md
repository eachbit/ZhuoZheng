# WenxinAssistantConfig.json README

## 说明

这个 README 只用于说明：

- `Assets/StreamingAssets/WenxinAssistantConfig.json`

它是当前 AI 助手的运行时配置文件。  
项目启动后，`WenxinAssistantController` 会读取这里的字段来决定：

- 使用哪个千帆应用
- 用哪个 API Key
- 助手叫什么
- 用单张头像还是序列帧
- 头像如何展示
- 助手怎么运动
- 语音识别怎么配置


## 当前配置文件路径

- `Assets/StreamingAssets/WenxinAssistantConfig.json`


## 百度智能云申请与配置步骤

这套助手当前接的是百度智能云千帆应用 OpenAPI。  
要跑起来，至少需要准备两样东西：

- `appId`
- `API Key`

建议按下面顺序操作。

### 1. 登录控制台

入口：

- `https://console.bce.baidu.com`

进入后找到：

- 千帆大模型平台
- Agent 开发平台
- AI 原生应用工作台


### 2. 创建或打开你的专属应用

如果还没有应用：

1. 进入 Agent / 应用创建页面
2. 创建一个新的 Agent 或应用
3. 填写名称、描述、角色设定
4. 保存并发布

如果已经有应用：

1. 进入应用详情页
2. 打开 `发布` 或 `API/SDK` 页面


### 3. 获取 `appId`

通常在这些位置能看到：

- `API/SDK`
- `OpenAPI 调用`
- `新建对话`
- `app_id`
- `应用ID`

看到的 `app_id` 就填到配置文件里的：

```json
"appId": "你的_app_id"
```


### 4. 获取 `API Key`

进入控制台左侧或安全认证相关页面：

- `API Key`

然后：

1. 新建 API Key
2. 给这个 Key 开通千帆应用开发相关权限
3. 复制生成后的完整 API Key

把它填到：

```json
"appBuilderApiKey": "你的_api_key"
```

如果语音识别也用同一条 Key，可以先直接填：

```json
"speechApiKey": "你的_api_key"
```


### 5. 回填到配置文件

最少需要填这几项：

```json
{
  "appId": "你的_app_id",
  "appBuilderApiKey": "你的_api_key",
  "speechApiKey": "你的_api_key"
}
```


### 6. 推荐同时修改项目上下文

为了让回答更贴你的工程，建议同时填写：

```json
{
  "assistantProjectName": "你的项目名称",
  "assistantExperimentType": "你的实验类型"
}
```

例如：

```json
{
  "assistantProjectName": "感染性心内膜炎病理学模拟实验",
  "assistantExperimentType": "急性感染性心内膜炎"
}
```


### 7. 配置完成后的测试顺序

建议按这个顺序测试：

1. 先测文本问答
2. 再测截图问答
3. 最后测语音识别


### 8. 常见接入问题

#### 文本问答报未配置 appId 或 API Key

检查：

- `appId` 是否为空
- `appBuilderApiKey` 是否为空
- JSON 是否保存成功


#### 文本能问，语音不能用

检查：

- `speechApiKey` 是否可用
- 麦克风权限是否开启
- `speechSampleRate` 和 `speechFormat` 是否保持默认


#### 不知道从哪复制 `appId`

优先去应用的：

- `发布`
- `API/SDK`
- `OpenAPI 调用`

页面里找 `app_id`


### 9. 官方文档参考

可参考百度官方文档中的这些入口：

- 千帆应用 OpenAPI 快速开始
- 千帆应用/Agent 接口概览
- 百度智能云 API Key 管理页面

如果控制台页面改版，优先认这几个关键词：

- `app_id`
- `API Key`
- `发布`
- `API/SDK`
- `OpenAPI 调用`


## 当前配置示例

```json
{
  "appId": "00ee5309-a9eb-4160-8b60-e6d01c6d86dc",
  "appBuilderApiKey": "你的API Key",
  "speechApiKey": "你的API Key",
  "endUserId": "heart-lab-user",
  "assistantName": "实验AI助手",
  "assistantProjectName": "感染性心内膜炎病理学模拟实验",
  "assistantExperimentType": "急性感染性心内膜炎",
  "assistantWelcomeText": "",
  "questionInputPlaceholder": "",
  "showAssistantName": true,
  "showAssistantProjectName": false,
  "showAssistantExperimentType": true,
  "showStatusBar": false,
  "showImageInfo": false,
  "assistantAvatarImage": "WenxinAssistantAvatar.png",
  "assistantAvatarFramesFolder": "WenxinAssistantFrames",
  "assistantAvatarPresentation": "portrait",
  "assistantMovementMode": "float",
  "assistantMovementEdge": "random",
  "assistantIdleAnchor": "right_edge_random",
  "assistantMoveSpeed": 60.0,
  "assistantAvatarSize": 180.0,
  "assistantAvatarFrameRate": 6.0,
  "speechSampleRate": 16000,
  "speechFormat": "wav",
  "speechDevPid": 1537
}
```


## 字段总览

### 鉴权与应用

- `appId`
- `appBuilderApiKey`
- `speechApiKey`
- `endUserId`

### 助手显示

- `assistantName`
- `assistantProjectName`
- `assistantExperimentType`
- `assistantWelcomeText`
- `questionInputPlaceholder`
- `showAssistantName`
- `showAssistantProjectName`
- `showAssistantExperimentType`
- `showStatusBar`
- `showImageInfo`
- `assistantAvatarImage`
- `assistantAvatarFramesFolder`
- `assistantAvatarPresentation`
- `assistantAvatarSize`
- `assistantAvatarFrameRate`

### 助手运动

- `assistantMovementMode`
- `assistantMovementEdge`
- `assistantIdleAnchor`
- `assistantMoveSpeed`

### 语音识别

- `speechSampleRate`
- `speechFormat`
- `speechDevPid`


## 字段详解

### `appId`

专属应用的 App ID。  
这是千帆应用接口调用时使用的应用标识。

示例：

```json
"appId": "00ee5309-a9eb-4160-8b60-e6d01c6d86dc"
```


### `appBuilderApiKey`

千帆应用问答接口使用的 API Key。

示例：

```json
"appBuilderApiKey": "bce-v3/ALTAK-xxxx"
```


### `speechApiKey`

语音识别使用的 API Key。  
如果和应用调用用的是同一个 Key，可以直接填相同值。

示例：

```json
"speechApiKey": "bce-v3/ALTAK-xxxx"
```


### `endUserId`

终端用户标识。  
一般可以固定成一个字符串，用于后端会话区分。

示例：

```json
"endUserId": "heart-lab-user"
```


### `assistantName`

界面中显示的助手名称。

示例：

```json
"assistantName": "实验AI助手"
```


### `assistantProjectName`

项目名称或实验名称。  
主要用于 AI 提示词上下文和状态显示。迁移到其他工程时，建议优先修改这个字段。

示例：

```json
"assistantProjectName": "感染性心内膜炎病理学模拟实验"
```


### `assistantExperimentType`

实验类型或当前实验方向。  
这个字段会替代旧工程里写死的项目内状态依赖，便于跨项目复用。

示例：

```json
"assistantExperimentType": "急性感染性心内膜炎"
```


### `assistantWelcomeText`

对话框初始化时显示的欢迎文案。  
默认值为空字符串，也就是默认不显示这段文本。

你可以：

- 填入任意欢迎词
- 留空
- 直接删除字段，让系统按空字符串处理

示例：

```json
"assistantWelcomeText": "你好，可以直接提问、截图提问或语音提问。"
```


### `questionInputPlaceholder`

输入框里的占位提示文案。  
默认值为空字符串，也就是默认不显示灰色提示文字。

你可以：

- 自定义提示语
- 留空
- 直接删除字段，让系统按空字符串处理

示例：

```json
"questionInputPlaceholder": "请输入和当前实验相关的问题"
```


### `showAssistantName`

是否在 UI 中显示 `assistantName`。

影响：

- 顶部标题
- 对话中助手说话人的名字

示例：

```json
"showAssistantName": true
```


### `showAssistantProjectName`

是否在状态栏中显示 `assistantProjectName`。

示例：

```json
"showAssistantProjectName": false
```


### `showAssistantExperimentType`

是否在状态栏中显示 `assistantExperimentType`。

示例：

```json
"showAssistantExperimentType": true
```

### `showStatusBar`

是否显示顶部状态栏。

这个区域通常用于显示：

- 当前场景名
- 项目名
- 实验类型
- 运行中的状态提示

如果你希望界面更干净，建议关闭：

```json
"showStatusBar": false
```

### `showImageInfo`

是否显示截图提示文字。

这个区域通常用于显示：

- 当前未附加实验截图
- 已截取实验主画面
- 截图相关的提示信息

如果你觉得这行提示累赘，可以关闭：

```json
"showImageInfo": false
```

当前推荐做法是直接在 Hierarchy 中调整助手 UI 的布局，而不是通过配置文件切换 UI 变体。


### `assistantAvatarImage`

单张头像文件名或路径。  
默认从 `StreamingAssets` 下读取。

默认素材位置：

- `Assets/StreamingAssets/WenxinAssistantAvatar.png`

示例：

```json
"assistantAvatarImage": "WenxinAssistantAvatar.png"
```


### `assistantAvatarFramesFolder`

序列帧目录名或路径。  
如果使用帧动画，角色帧图放在这里。

默认目录：

- `Assets/StreamingAssets/WenxinAssistantFrames/`

示例：

```json
"assistantAvatarFramesFolder": "WenxinAssistantFrames"
```

建议命名：

- `001.png`
- `002.png`
- `003.png`


### `assistantAvatarPresentation`

头像展示方式。

支持值：

- `portrait`
  圆形头像风格，适合真人头像、半身头像
- `freeform`
  不裁圆，适合透明背景全身角色、虫子、吉祥物

示例：

```json
"assistantAvatarPresentation": "portrait"
```


### `assistantMovementMode`

助手运动模式。

支持值：

- `fixed`
  完全不动
- `float`
  原地轻微浮动
- `side_pingpong`
  沿一条边折返
- `edge_cw`
  顺时针绕屏
- `edge_ccw`
  逆时针绕屏
- `screen_random`
  在整个屏幕范围内随机运动，但不会出屏幕外
- `random`
  随机切换几种运动方式

示例：

```json
"assistantMovementMode": "float"
```


### `assistantMovementEdge`

主要在 `side_pingpong` 模式下生效。  
用来指定沿哪一条边折返。

支持值：

- `top`
- `right`
- `bottom`
- `left`
- `random`

示例：

```json
"assistantMovementMode": "side_pingpong",
"assistantMovementEdge": "right"
```

或者：

```json
"assistantMovementMode": "side_pingpong",
"assistantMovementEdge": "random"
```


### `assistantIdleAnchor`

在 `fixed` 和 `float` 模式下生效。  
用来指定助手停在哪个位置。

支持值：

- `top_left`
- `top_right`
- `bottom_left`
- `bottom_right`
- `edge_random`
  从四条边中随机选一条，再取该边上的随机点
- `top_edge_random`
- `right_edge_random`
- `bottom_edge_random`
- `left_edge_random`

示例：

```json
"assistantMovementMode": "fixed",
"assistantIdleAnchor": "top_left"
```

```json
"assistantMovementMode": "float",
"assistantIdleAnchor": "right_edge_random"
```


### `assistantMoveSpeed`

助手移动速度。  
适用于会移动的模式，比如：

- `side_pingpong`
- `edge_cw`
- `edge_ccw`
- `screen_random`
- `random`

示例：

```json
"assistantMoveSpeed": 60.0
```


### `assistantAvatarSize`

悬浮助手头像大小。

示例：

```json
"assistantAvatarSize": 180.0
```


### `assistantAvatarFrameRate`

序列帧播放帧率。

示例：

```json
"assistantAvatarFrameRate": 6.0
```


### `speechSampleRate`

录音采样率。  
默认一般用 `16000`。

示例：

```json
"speechSampleRate": 16000
```


### `speechFormat`

语音格式。  
当前默认是 `wav`。

示例：

```json
"speechFormat": "wav"
```


### `speechDevPid`

百度语音识别的模型参数。

示例：

```json
"speechDevPid": 1537
```


## 素材使用方式

### 单张头像

放在：

- `Assets/StreamingAssets/WenxinAssistantAvatar.png`

适合：

- 真人头像
- 医学助手头像
- 单张透明 PNG


### 序列帧动画

放在：

- `Assets/StreamingAssets/WenxinAssistantFrames/`

适合：

- 虫子爬行
- 小吉祥物运动
- 连续角色动画

命名建议：

- `001.png`
- `002.png`
- `003.png`


## 推荐配置示例

### 1. 固定在右下角

```json
"assistantMovementMode": "fixed",
"assistantIdleAnchor": "bottom_right"
```


### 2. 右边随机位置轻微浮动

```json
"assistantMovementMode": "float",
"assistantIdleAnchor": "right_edge_random"
```


### 3. 沿屏幕右边折返

```json
"assistantMovementMode": "side_pingpong",
"assistantMovementEdge": "right"
```


### 4. 沿任意一条边折返

```json
"assistantMovementMode": "side_pingpong",
"assistantMovementEdge": "random"
```


### 5. 顺时针绕屏

```json
"assistantMovementMode": "edge_cw"
```


### 6. 逆时针绕屏

```json
"assistantMovementMode": "edge_ccw"
```


### 7. 在屏幕内随机游走

```json
"assistantMovementMode": "screen_random"
```


### 8. 自动随机切换多种模式

```json
"assistantMovementMode": "random"
```


## 修改配置后的建议操作

修改完 `WenxinAssistantConfig.json` 后，建议：

1. 保存文件
2. 回 Unity 停止当前 `Play`
3. 重新 `Play`

这样能确保新配置被重新读取。


## 常见问题

### 1. 改了配置但看起来没变化

检查：

- 是否重新 `Play`
- JSON 格式是否写错
- 字段名是否拼写正确


### 2. 序列帧不播放

检查：

- `assistantAvatarFramesFolder` 是否正确
- 帧图是否真的在 `StreamingAssets/WenxinAssistantFrames/`
- 文件名是否有顺序
- `assistantAvatarFrameRate` 是否大于 0


### 3. 透明 PNG 显示不理想

建议：

- 人物头像优先 `portrait`
- 虫子、全身角色优先 `freeform`
- 素材本身尽量使用真正带 alpha 的 PNG


### 4. fixed / float 位置不对

检查：

- `assistantIdleAnchor`


### 5. side_pingpong 没沿预期边运动

检查：

- `assistantMovementMode` 是否真的是 `side_pingpong`
- `assistantMovementEdge` 是否填成了你要的边
