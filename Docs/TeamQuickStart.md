# 团队开发快速上手

这份说明是给第一次接手项目的同学看的。

## 1. 先做什么

1. 安装 GitHub Desktop 或 Git。
2. 从 GitHub 拉取仓库到本地。
3. 打开 Unity Hub。
4. 用 Unity `2022.3.62f3c1` 打开这个项目。

## 2. 第一次打开项目

1. 等 Unity 自动导入资源。
2. 打开场景：
   `Assets/Scenes/Garden_Main.unity`
3. 先点一次运行，确认项目能正常进入游戏。
4. 确认玩家可以正常第一人称和第三人称跑图。

如果打不开，先不要乱改代码，先把报错截图发到群里。

## 3. 每个人开发前要做什么

1. 先拉取最新代码。
2. 新建自己的分支。
3. 只在自己的分支上开发。
4. 不要直接改 `main`。

分支名字建议：

- `feature/chapter01`
- `feature/chapter02`
- `feature/chapter03`
- `feature/ui`
- `feature/art`

## 4. 这个项目现在怎么分

### 公共功能

这些是大家都能用的，不要随便乱搬位置：

- `Assets/Scripts/Core`
- `Assets/Scripts/Player`
- `Assets/Scripts/UI`
- `Assets/Scripts/Interaction`
- `Assets/Scripts/Environment`

其中最重要的是：

- `Assets/Scripts/Player`

这里放的是已经能用的第一人称、第三人称和跑图功能。  
后面别的章节直接复用这一套，不要自己再复制一份。

### 第一章节

第一章节现在放在这里：

- `Assets/Scripts/Chapters/Chapter01`
- `Assets/Scripts/Editor/Chapters/Chapter01`
- `Assets/Figure/Chapters/Chapter01`
- `Assets/Materials/Chapters/Chapter01`
- `Assets/Shaders/Chapters/Chapter01`

如果你负责第一章，就主要改这些地方。

## 5. 场景里怎么找东西

`Garden_Main` 现在按下面分组：

- `_00_Core`
- `_10_World`
- `_20_Story`
- `_30_Chapters`

第一章在这里：

- `_31_Chapter01_Gameplay`
- `_32_Chapter01_Visuals`

简单理解：

- `Gameplay` 里放玩法物体
- `Visuals` 里放表现物体

## 6. 每个人尽量只改自己的部分

建议这样分：

- 关卡开发：改自己章节的 `Gameplay`
- UI：改 UI 相关脚本和预制体
- 美化：改 `Visuals`、材质、特效、模型

尽量不要几个人同时改同一个文件。

## 7. 提交代码前要注意

1. 先保存场景。
2. 先确认 Unity Console 没有新的红色报错。
3. 只提交自己这次改的内容。
4. 写清楚提交说明。

提交说明例子：

- `完成 Chapter02 初始机关交互`
- `补充 UI 提示框`
- `调整 Chapter01 水闸反馈表现`

## 8. 哪些内容不要提交

这些一般不用传：

- `Artifacts`
- 临时截图
- 测试视频
- 本机生成的缓存文件

## 9. 如果改乱了怎么办

1. 先不要继续乱点保存。
2. 看自己改了哪些文件。
3. 不确定就先问，不要直接覆盖别人的内容。

## 10. 最简单的团队规则

记住这 5 条就够了：

1. 不直接改 `main`
2. 每个人一个分支
3. 只改自己负责的部分
4. 公共跑图功能不要乱动
5. 有报错先发出来再处理
