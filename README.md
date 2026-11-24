# Roguelike Slash

Unity 2D 抖音小游戏。编辑器：Unity 2022.3.7f1c1；发布：WebGL + TTSDK 6.9.6。

## 打开

- 正式游戏：`Assets/Game/Scenes/GameScene.unity`。
- 卡牌和动画实验室：`Assets/Game/Scenes/CardLabScene.unity`。
- 菜单 `Tools → Arena` 可直接打开这两个场景。
- WASD或左侧摇杆移动；空格控制闪，J控制斩，K控制无双；按钮同样支持鼠标/触摸。

## 配置与素材

- 主角待机、移动：`Assets/Game/Resources/Animations/Player`。
- 技能动作：`Assets/Game/Art/Animations/Player`。
- 剑气和命中特效：`Assets/Game/Art/Effects`。
- 弹丸：`Assets/Game/Art/Projectiles`；敌人：`Assets/Game/Art/Enemies`；环境：`Assets/Game/Art/Environment`。
- 技能动画配置：`Assets/Game/Data/Animation/DefaultSkillAnimations.asset`。
- 独特原画参考：`ArtSource/References`；生成提示词：`ArtSource/Prompts.json`。

素材以Assets中的文件为唯一运行版本。相同图像在动画配置里复用引用，不复制帧文件。没有保留源帧副本、预览拼图、GIF、旧规划归档或一次性测试脚本。

[实验室与调参](Docs/卡牌实验室使用方法.md) · [技能按钮布局](Docs/技能按钮布局调整.md) · [技能动画装配](Docs/技能动画装配.md)

## 构建

现有抖音导出目录：`Builds/DouyinM5/tt-minigame`，供开发者工具继续使用。正式批处理入口为 `DouyinBuild.Build`；不要传 `-quit`，异步构建完成后会退出。

中文Windows的WebGL构建进程设置 `PYTHONUTF8=1`、`PYTHONIOENCODING=utf-8`。SDK的 `com.bytedance.ttsdk.ref.asmdef` 保留 `Unity.ugui` 引用。

Library、Temp、Logs、Builds、UserSettings属于本机生成目录，不提交Git。本次清理没有重新导出抖音包。

## 脚本位置与调试入口

游戏脚本集中在 `Assets/Game/Scripts`：`Core` 为场景主循环与桥接；`Combat/Attack`、`Combat/Skills`、`Combat/Elements` 分别为普攻、主动技能和元素；`Cards` 为卡牌及构筑规则；`Progression` 为成长、属性和奖励；`UI` 为界面；`Presentation/Animation` 为动画与技能表现；`Debug` 为 CardLab、动画预览和运行调试；`Configuration`、`World`、`Weapons` 分别为场景配置、环境和武器。编辑器工具位于 `Assets/Editor`。移动脚本保留原 GUID，场景与素材引用继续使用原配置。

CardLab Inspector 按「环境、木桩、卡牌装配、射程与视野、面板与存档、角色状态」折叠。Game 视图调试栏保留「环境、组合、卡牌」页签；卡牌页可按普攻、斩与剑气、闪与机动、元素、生存筛选，搜索、说明展开和 +/-1 装配继续可用。
