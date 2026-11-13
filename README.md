# Roguelike Slash

Unity 2D / C#，目标发布平台为抖音小游戏（WebGL + TTSDK）。

## 当前状态

- Unity 2022.3.7f1c1 已创建并成功批处理编译此项目。
- `Assets/Game/Scenes/SmokeTest.unity` 是构建验证场景，不是 M1 游戏实现。
- `SmokeBuild.Prepare` 生成正交相机、测试方块，设置横屏并加入构建场景。
- BGDT 3.0.271 已从抖音官方包导入。
- WebGL 模块已安装；TTSDK 6.9.6 已安装；最小 WebGL 构建成功。
- Unity WebGL 产物：`Builds/WebGLSmoke/`。
- 抖音适配产物：`Builds/DouyinSmoke/tt-minigame/`，TTSDK 构建成功，CLI 预览二维码生成成功，真机运行待扫码验证。
- App ID：`tt3c554ae8a2291d8907`（今天要砍怪）。导出设置和预览包均为横屏。
- 无屏幕构建入口：Unity batchmode 调用 `DouyinBuild.Build`，不要传 `-quit`，异步构建完成后脚本自行退出。仍需设置 Python UTF-8 环境变量。

## 规划

原始规划保存在 `Docs/roguelike_slash_mvp_v0_1.md`。玩法与 M1–M6 验收继续沿用；发布目标补充为抖音小游戏。

## 验证记录

工作区 `../UnitySetupLogs/` 保存工程创建、场景编译、BGDT 导入和 WebGL 安装日志。
创建工程与场景准备日志均以 return code 0 退出。
`webgl-build-utf8.log` 包含 `SMOKE_WEBGL_BUILD_OK`，退出码为 0。

## 环境修复

- 添加 `com.unity.ugui` 1.0.0，并为 TTSDK 的 `com.bytedance.ttsdk.ref.asmdef` 添加 `Unity.ugui` 引用，解决 BaseInput 编译错误。升级 SDK 后需确认此引用保留。
- 中文 Windows 的 WebGL Python 工具遇到 GBK 解码错误；构建进程设置 `PYTHONUTF8=1`、`PYTHONIOENCODING=utf-8` 后成功，不修改系统语言。
- Unity 编辑器曾报告在线包搜索失败；本次实际依赖解析和 WebGL 构建成功。

## 打开

在 Unity Hub 添加此目录，使用 2022.3.7f1c1 打开。打开 `Assets/Game/Scenes/SmokeTest.unity`。
ByteGame → ByteGame Develop Tools 可查看 TTSDK 安装状态。

## M1 基础场景

打开 `Assets/Game/Scenes/GameScene.unity` 后点击 Play。用 WASD 或在横屏左下区域按住并拖动来移动，观察镜头与网格。进入右侧红色测试区并停留，血量降为零后点击 Retry，检查角色位置、血量和计时重置。击杀数在引入敌人前显示 0。

`ArenaPrototype.cs` 负责移动、受伤和 HUD，`ArenaSetup.cs` 生成场景。当前网格覆盖出生点附近，测试区用于验证受伤与重开。

## M2 基础战斗

`GameScene` 的红色测试区已被战斗替代。开局用 WASD 或左下摇杆移动，靠近红色追踪怪时会自动射击。30 秒后刷怪池加入橙色蓄力怪和紫色远程怪。观察远程怪亮白瞄准后发出的橙色子弹，横向移动可避开。死亡后点 Retry，检查敌人、弹丸、血量、计时和击杀数重置。五分钟持续运行和真机触控需实测。

战斗逻辑位于 `Assets/Game/ArenaPrototype.Combat.cs`。抖音导出目录为 `Builds/DouyinM2/tt-minigame/`。

## M3 主动技能

打开 `Assets/Game/Scenes/GameScene.unity`，在 Hierarchy 选中 Arena。Inspector 中可调整移动、技能时序和伤害、自动射击间隔、特殊怪登场时间，以及各对象的材质、贴图和染色。音效槽可拖入 AudioClip，空槽静音。使用透明贴图时请选支持透明和主贴图的材质。

右下角三个技能按钮支持触屏和鼠标。A 点按弹反，长按减伤、松开反击。B 点按冲刺，长按拖动瞄准、松开打出矩形攻击。C 点按环斩，长按持续旋转。技能结束才进入冷却，默认 8 秒。

## M4 双技能融合

用鼠标或手指从一个就绪技能按钮拖到另一个就绪按钮上。融合按钮留在拖动目标的位置，起点显示唯一的「取消」按钮，第三个技能仍可单独使用。取消不消耗冷却。

- A+B 短按为带弹反的防御冲锋。长按时减伤，拖动瞄准，松开释放矩形反击。
- B+C 短按先冲刺，再让双弧斩从冲刺起点向实际终点推进。长按瞄准后松开，发出一道前移的大弧斩。
- 确认攻击结束后两枚参与按钮才同时进入 12 秒冷却。融合冷却和弧斩飞行时长可在 Arena 的 Fusion 配置栏调整。

## M5 闪避与守备反击

A+C 短按闪避，之后在 3 秒内点融合按钮释放免费环形斩。普通闪避有 2 次，避开原本会命中的敌弹或蓄力冲撞时有 4 次，并暂时减慢敌人和敌弹。长按进入减伤守备，最多反击 3 次。松手或 5 秒到期时，剩余斩击依次释放。等最后一次攻击结算后，A、C 才同时开始 12 秒冷却。

### 基础技能调整

A 按下立即举盾，短按阶段免伤并反射敌弹，松手后进入 2.5 秒冷却。按过长按阈值后，护圈变为土金色，持续免伤并挡下敌弹。松手或按满 3 秒会发出缓慢前移的护圈。护圈消失后开始默认 8 秒冷却。

B 长按时可以减速移动，松手打出矩形攻击。B 短按冲刺会留下快速淡出的红色残影。C 短按扫斩期间锁定移动，长按持续显示范围圈。每次斩击用蓝色半径扫出圆弧。

数值和颜色可在 Arena 组件的 Base Skill Tuning 栏调整。拖拽超过阈值后进入融合手势，松手时若未命中有效目标，恢复原按钮。
