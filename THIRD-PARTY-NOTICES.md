# 第三方组件说明（Third-party notices）

本包**不含**《Pony Island》的任何游戏文件、资源或反编译产物：游戏必须由玩家自己从 Steam 安装，
`payload\` 只提供模组加载器和这一个插件。下面列出的第三方二进制来自 BepInEx 官方 5.4.23 (x86) 发行包，
**逐个文件都是未修改的原版**——`SHA256SUMS.txt` 记录了它们的哈希，`verify.bat` 会逐个复核。

| 组件 | 本包内版本（从二进制里读出来的） | 位置 | 上游 |
|---|---|---|---|
| BepInEx（Preloader / core / BepInEx.Harmony） | 5.4.23.3，commit `cd593c47566adfe971f1d96225fa82d1864ae877` | `payload\BepInEx\core\` | [BepInEx/BepInEx](https://github.com/BepInEx/BepInEx) |
| Unity Doorstop（注入器） | 4.4.0 | `payload\winhttp.dll` | [NeighTools/UnityDoorstop](https://github.com/NeighTools/UnityDoorstop) |
| HarmonyX / 0Harmony（含 `0Harmony20.dll`、`HarmonyXInterop.dll`） | 汇编版本 2.9.0.0 | `payload\BepInEx\core\` | [BepInEx/HarmonyX](https://github.com/BepInEx/HarmonyX)，基于 [pardeike/Harmony](https://github.com/pardeike/Harmony) |
| Mono.Cecil（`Mono.Cecil.dll` 及 Mdb / Pdb / Rocks） | 随 BepInEx 发行 | `payload\BepInEx\core\` | [jbevain/cecil](https://github.com/jbevain/cecil) |
| MonoMod（`MonoMod.RuntimeDetour.dll`、`MonoMod.Utils.dll`） | 随 BepInEx 发行 | `payload\BepInEx\core\` | [MonoMod](https://github.com/MonoMod) |

许可情况（如实说明，不做超出证据的判断）：

- **BepInEx 项目本体为 LGPL-2.1**（上游 README 明确写着 "The BepInEx project is licensed under the LGPL-2.1 license."）。LGPL-2.1 允许再分发未修改的二进制，条件是随附许可说明并提供获取源码的途径——本文件加上上游链接即满足这一条。
- **Unity Doorstop、HarmonyX / Lib.Harmony、Mono.Cecil、MonoMod 上游均采用 MIT 类宽松许可**；具体条款以上游仓库内的 LICENSE 文件为准。
- 本包为了控制体积**没有复制上游的 LICENSE 原文**。如果要把许可文本一起打包分发，请从 BepInEx 官方 5.4.23 发行包（内含各组件许可文件）补齐后重新生成 `SHA256SUMS.txt`。

本包自研部分——`install.bat` / `uninstall.bat` / `patch-on.bat` / `patch-off.bat` / `verify.bat` /
`scripts\find-game.bat` / `src\build.bat` / `src\AccessibilityPlugin.cs`——目前**没有附任何许可证声明**，
因此默认为"保留所有权利"。如果你想让它们变成 MIT 或其他许可，需要你本人决定并添加 `LICENSE` 文件；
插件源码引用的游戏内部类名（`AdventureModeLevel`、`SoulPhase1Screen` 等）来自游戏自带程序集，
不属于本包的授权范围。
