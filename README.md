# WallCue 0.1.8

**适用于 Beat Saber PC 版 1.40.8 · 群内测试版**

本分支从 1.44.1 版移植，已使用本地 1.40.8 游戏及依赖完成编译、逻辑测试和 DLL 集成检查。已用 `-vrmode oculus` + `fpfc` 启动并进入普通谱，日志确认设置页注册、计数器 / 图标初始化及实际头部撞墙检测均正常，未发现 WallCue 相关报错；VR 中的视觉效果仍待实测。

WallCue 是一个撞墙提示 Mod，适合 FitBeat 等需要频繁下蹲、侧闪的谱面。它提供墙框预警、撞墙计数和固定位置的碰撞图标，帮助你确认是否躲开了墙，尤其是很薄、经过很快的墙。

这是 PC 游戏插件，Quest 通过串流游玩 PC 版时可以使用；不适用于 Quest 独立版。

## 功能介绍

### 墙框提示

- **黄色**：按你当前的头部位置继续保持下去，可能撞上前方墙，或离墙边缘的安全余量不足。
- **红色与透明闪烁**：游戏已判定头部正在撞墙。
- **恢复原样**：已经处于安全位置，或已离开墙。

黄色提示会随你下蹲、侧闪而变化。它不预测你接下来的动作，也不等于已经发生碰撞。

### Wall Hits：撞墙计数器

HUD 上分两行显示 `Wall Hits` 和本局撞墙数。每面墙首次发生实际碰撞时加 1，数字变红 **0.5 秒** 后恢复白色。

同一面墙持续碰撞，或离开后再次碰到，都只计一次；碰到不同墙分别计数。进入新谱面或重开时归零。

### Wall Hit Icon：碰撞图标

每次新增撞墙计数时，在固定位置显示一个白色图标：**淡入 0.1 秒 → 保持 0.3 秒 → 淡出 0.1 秒**。

即使墙很薄、已经经过，图标也会完成这段显示。连续碰到不同墙会延长显示，不会叠出多个图标。

计数与图标均以游戏的实际头部碰撞为依据，光剑碰墙不算。WallCue 不修改游戏的碰撞判定、血量或得分规则。

## 安装与更新

需要先安装适配 **Beat Saber 1.40.8** 的以下依赖：

| 依赖 | 本版要求 |
| --- | --- |
| BSIPA | 4.3.6 起，4.x |
| SiraUtil | 3.2.1 起，3.x；本版使用 3.2.1+bs.1.40.0 验证 |
| BeatSaberMarkupLanguage（BSML） | 1.12.5 起，1.x；本版使用 1.12.5+bs.1.40.0 验证 |
| Counters+ | 2.3.11 起，2.x；本版使用 2.3.11 验证 |

版本号满足要求的同时，依赖本身也必须适配游戏版本。安装包不包含这些依赖。

1. 退出游戏。
2. 将压缩包中的 `Plugins/WallCue.dll` 放入游戏目录的 `Plugins` 文件夹。
3. 更新时覆盖旧文件，不要同时保留改名的旧版 DLL。
4. 启动游戏，按下方说明调整设置。

使用 BSManager 时，请放进 **1.40.8 对应实例** 的目录。图标已包含在 DLL 内，不需要另放图片，也不需要安装 HitScoreVisualizer 或 Enhancements。

更新可保留原配置。卸载时退出游戏并移走 `Plugins/WallCue.dll` 即可。

## 调整预警距离与灵敏度

入口：**选歌界面左侧 → Mods → WallCue**。

| 选项 | 作用 | 默认值 |
| --- | --- | --- |
| Limit warning distance | 开启时限制前方墙的预警距离；关闭时不限距离 | 关闭 |
| Warning distance | 预警距离，5–25 米，步长 0.1 米 | 10.0 米 |
| Sensitivity | 黄色提示的额外安全余量，0–10 厘米，步长 1 厘米 | 5 厘米 |

关闭距离限制时，距离滑条会置灰，但保留上次的数值。开启后可拖动滑条，或用两侧按钮微调。

**灵敏度越大，黄色提示要求留出的安全余量越大。** 例如从 5 厘米调到 10 厘米，需要蹲得更低或侧闪得更远，黄色才会消失。0 厘米表示不加额外余量，直接会撞到的墙仍会预警。灵敏度不会改变实际撞墙判定。

距离设置只限制前方墙的黄色预警，不限制实际撞墙时的红色闪烁、计数或图标。不限距离也只对游戏已经生成的墙生效，不会提前显示整首歌的所有墙。

设置自动保存，改好后直接进入谱面即可，无需重启。不同歌曲共用这套设置，不会自动切换预设。如果找不到 WallCue 标签，可用 Mods 栏的左右箭头翻页，或通过眼睛按钮检查标签是否被隐藏。

## 调整计数器和图标

入口：**Counters+ 设置 → Counters 列表**。

| 项目 | 用途 | 初次安装默认位置 |
| --- | --- | --- |
| Wall Hits | 撞墙计数器 | Below Combo，Distance 2 |
| Wall Hit Icon | 固定位置碰撞图标 | Over Highway，Distance 0 |

两项默认启用，可以分别设置 **Enabled（开关）、Position（位置）、Distance（位置偏移）和 Canvas（画布）**。需要更细的位置或大小调整时，可使用 Counters+ 的 Canvas 设置。这里的 `Distance` 用于摆放 HUD，与前面的预警距离无关。

图标在 Counters+ 预览里可能显示为 `Wall Hit Icon` 文字占位，进入谱面并撞墙后才会出现实际图标。

**墙框开关也在 Counters+ → Wall Hits 的附加设置中：**

| 选项 | 作用 |
| --- | --- |
| Wall frame cues | 开关黄色预警与红色墙框闪烁；不会关闭计数器和图标 |
| Test: all walls yellow | 墙框显示测试，正常游玩请保持关闭 |

只想保留计数和图标时，关闭 `Wall frame cues` 即可。

## 兼容情况

| 谱面或模式 | 当前情况 |
| --- | --- |
| 普通谱 / FitBeat | 1.40.8 普通谱已通过初始化与实际撞墙日志检查；FitBeat 和 VR 视觉效果待实测，原 1.44.1 版已实测 |
| Mapping Extensions（ME）谱 | 原 1.44.1 版已有谱面实测；1.40.8 待游戏内验证 |
| Noodle Extensions（NE）谱 | **自动关闭墙框效果，保留计数和图标**；原 1.44.1 版已实测，1.40.8 待游戏内验证 |
| BSPlus 多人模式 | 原 1.44.1 版已在测试者环境中实测；1.40.8 待游戏内验证 |
| 其他多人实现、战役、教程、回放、其他游戏版本 | 尚未充分验证 |

NE 谱的装饰墙或不可碰撞墙，不会仅因视觉上穿过头部就计数；只有游戏实际判定的碰撞才触发反馈。NE 谱关闭墙框效果是预期行为，切回普通谱会自动恢复。自动识别依赖谱面对当前难度的 NE 声明。

如果其他 Mod 隐藏了墙框，WallCue 不会强制让它重新显示；计数和图标仍可独立工作。动态墙色、特殊动画和不同 Mod 组合的兼容性仍需要更多玩家测试。

## 常见问题

**看不到计数器或图标？**

检查 Counters+ 总开关、对应项目的 Enabled、位置及 Canvas，以及是否开启了隐藏 HUD 的选项。图标平时就是隐藏的，只有实际撞墙才会出现。

**墙没有变黄？**

确认 `Wall frame cues` 已开启；检查距离限制，以及自己是否已经躲到安全位置。NE 谱不显示墙框提示；其他 Mod 隐藏墙框也可能影响显示。

**所有墙一直是黄色？**

检查并关闭 `Test: all walls yellow`。

**短墙的红色闪烁看不清？**

墙框闪烁仅在实际碰撞期间显示，薄墙经过很快。可使用独立持续 0.5 秒的计数变红和碰撞图标来确认。

**计数器显示 `--`？**

表示当前关卡的碰撞检测不可用，不是零次碰撞。请提供日志反馈。

## Development

WallCue is currently under active development.

Requirements:

- .NET SDK 8+
- Beat Saber PC 1.40.8
- BSIPA, SiraUtil, BSML and Counters+ installed (see the versions above)

This branch targets Beat Saber **1.40.8**. Build and run the checks against a patched installation with the dependencies listed above:

```powershell
python build.py --game "../../games/BeatSaber-1.40.8" --dotnet "../../tools/dotnet-sdk/dotnet.exe" --sdk-root "../../tools/dotnet-sdk/sdk/8.0.424"
python tests/run.py --game "../../games/BeatSaber-1.40.8" --dotnet "../../tools/dotnet-sdk/dotnet.exe" --sdk-root "../../tools/dotnet-sdk/sdk/8.0.424"
```

The DLL is written to `out/WallCue.dll`. Integration checks use the actual game and plugin DLLs to verify private collision/rendering fields, dependency version ranges, Counters+ registration, BSML bindings, and the embedded icon. They do not exercise Unity rendering or VR gameplay.

Bug reports, compatibility reports and pull requests are welcome.

When reporting an issue, please include:
- Beat Saber version
- WallCue version
- Relevant mods
- `_latest.log`
- Steps to reproduce the issue

## Author

SD無 (GitHub: @neon28)
