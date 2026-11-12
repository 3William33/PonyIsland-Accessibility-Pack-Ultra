# Pony Island 无障碍补丁包（Accessibility Pack）

适用于 Steam 版《Pony Island》（32 位 Unity 游戏，Unity 2017.4.40f1）。
本补丁只**新增文件**，不修改、不替换任何原始游戏文件（`PonyIsland.exe`、`UnityPlayer.dll`、`Assembly-CSharp.dll`、场景文件都保持原样）。

- 补丁版本：1.0.0
- 插件 GUID：`local.ponyisland.accessibility`
- 插件 DLL SHA-256：`919c8ac2efb3ecd7d79499396d7191d1ab7ccc41d93951af521557a8694def5e`
- 加载器：BepInEx 5.4.23.3 (x86) + HarmonyX，通过 Unity Doorstop（`winhttp.dll`）注入
- 不需要管理员权限，不联网，不收集任何数据

---

## 1. 包含的功能

### 功能 A：可访问性模式（热键 F8，默认关闭）

仅在**耶稣奖券关**（场景 `game_PART8`，界面 `Level4`）生效：

| 效果 | 说明 |
|---|---|
| 护栏/门碰撞不致死 | 撞到不再重置小马 |
| 蝴蝶碰撞不致死 | 被碰到不再死亡 |
| 耶稣的星星不致死 | 星星仍会正常消失，但不会杀死玩家 |
| 玩家激光无法伤害耶稣 | 伤害判定被拦截，耶稣血量完全不动 |
| 其他一切不变 | 移动、跳跃、射击、动画、场景切换、关卡进度、奖券条件全部走原版逻辑 |

**其他任何关卡都不受影响**（补丁只在 `game_PART8` 且当前界面为 `Level4` 时介入）。

### 功能 B：绝望之魂终战辅助（热键 F9，默认关闭）

仅在最终 Boss（场景 `game_FINALBOSS`）的 Phase1/2/3 生效：

- 小马被击中时，Boss 不再回血/重置（原版会“重置并 +4 血”）；
- 自动把激光瞄准 Boss，并保持按住射击键；
- Boss 的 40 点生命仍然**由游戏自己的激光碰撞逻辑**扣减，补丁不写血量、不写存档、不跳过任何阶段。

### 补丁不做的事（重要）

- 不写入奖券数量、不写入 Steam 成就、不修改 Steam 统计/账号/云存档；
- 不修改存档（存档在注册表 `HKCU\Software\Daniel Mullins Games\Pony Island`）；
- 不跳过任何关卡、不伪造通关状态；
- 关闭开关（F8 / F9 或 `patch-off.bat`）后即恢复原版行为。

---

## 2. 快速开始（3 步）

> 先**完全退出游戏**（包括 Steam 里显示“正在运行”的状态），再操作文件。

1. 把整个 `PonyIsland-Accessibility-Pack` 文件夹解压/放到任意位置（不要放进游戏目录里面）。
2. 双击 **`install.bat`**。
   - 脚本会**自动定位**游戏目录（见下表），找到就直接用，这就是"默认路径"便利选项；
   - 自动定位失败或你想装到别的目录时：把**游戏文件夹（或 `PonyIsland.exe`）拖到 `install.bat` 图标上**，
     或双击后按提示输入完整路径，或在运行前设置环境变量 `PONYPATH`。
   - 看到 `Done.` 即安装成功。
3. 从 Steam 正常启动游戏。进入关卡后按 **F8** 开启可访问性模式（左上角会显示状态）。

首次安装**默认是关闭的**，不会在你没按 F8 的时候改变游戏行为。

### 游戏目录是怎么找到的

`install.bat` / `uninstall.bat` / `patch-on.bat` / `patch-off.bat` 共用同一个查找顺序，第一个命中且**确实存在 `PonyIsland.exe`** 的路径才会被采用：

| 顺序 | 来源 | 说明 |
|---|---|---|
| 1 | 命令行参数 | 拖放到脚本图标上、或在 cmd 里 `install.bat "X:\你的路径"` |
| 2 | `game-path.txt` | 上一次安装成功后自动记住的目录（位于本包根目录，属于你本机的状态文件，不在校验清单里） |
| 3 | 环境变量 `PONYPATH` | 需要脚本化的话用它 |
| 4 | Steam 注册表 + 常见安装位置 | `HKCU`/`HKLM` 里的 Steam 目录、各盘符下的 `steam` / `SteamLibrary` / `Games\Steam` / `Program Files (x86)\Steam`，再读 `steamapps\libraryfolders.vdf` 里的附加库文件夹 |
| 5 | 手动输入 | 以上都没命中时提示输入；输入的路径同样要真的包含 `PonyIsland.exe`，否则脚本明确报错退出（不会把一个无效目录当成找到了） |

三种路径在文档里的写法约定（**避免混淆**）：

- **示例路径**：`X:\SteamLibrary\steamapps\common\Pony Island` —— 只用来演示格式，任何机器上都不必是这个名字；
- **默认路径**：没有硬编码默认目录，默认行为就是上表的**自动定位**；
- **实测环境**：2026-11-07 的实机测试用 `D:\steam\steamapps\common\Pony Island`（测试机自己的 Steam 库位置，见 `docs\TEST-REPORT.md`）。

### 热键

| 热键 | 作用 | 是否记忆 |
|---|---|---|
| `F8` | 可访问性模式 开/关 | 是（写入配置文件，重启后保持） |
| `F9` | 绝望之魂辅助 开/关 | 是（写入配置文件，重启后保持） |

状态栏会显示 `Accessibility Mode: ON/OFF` 与 `Boss assist: ON/OFF`。

---

## 3. 配置文件

路径：`<游戏目录>\BepInEx\config\local.ponyisland.accessibility.cfg`

| 键 | 默认值 | 含义 |
|---|---|---|
| `Enabled` | `false` | 可访问性模式总开关 |
| `ToggleKey` | `F8` | 开关热键 |
| `ShowOverlay` | `true` | 是否显示状态栏 |
| `AssistHopelessSoul` | `false` | 绝望之魂辅助开关 |
| `AssistToggleKey` | `F9` | 辅助开关热键 |

> 包内 `payload\BepInEx\config\` 里的配置只包含前三个键（这是被实测过的版本）。
> 第一次运行游戏后，BepInEx 会自动补上 `AssistHopelessSoul` / `AssistToggleKey` 两项，默认都是关闭。
> 想改默认状态，直接编辑这个文件（游戏关闭时改），或进游戏按 F8/F9。

---

## 4. 临时停用 / 彻底卸载

| 操作 | 用哪个脚本 | 结果 |
|---|---|---|
| 临时让游戏以**纯原版**启动 | `patch-off.bat` | 把 `winhttp.dll` 改名为 `winhttp.dll.disabled`，补丁完全不加载；游戏文件、存档都不变 |
| 重新启用补丁 | `patch-on.bat` | 把名字改回来 |
| 彻底卸载 | `uninstall.bat` | 删除 `winhttp.dll`、`doorstop_config.ini`、`BepInEx\` 整个目录和安装标记 `.pony_accessibility_patch` |

说明：

- 四个脚本都用同一套自动定位（见 §2 的表格），也接受同样的自定义路径方式：参数 / 拖放 / `PONYPATH` / `game-path.txt` / 手动输入。
- 四个脚本都要在**游戏关闭时**运行（`winhttp.dll` 被游戏占用时无法改名/删除）。脚本会先检查 `PonyIsland.exe` 是否在运行，在运行就拒绝执行并提示你先退出游戏。
- `uninstall.bat` 只删除本补丁新增的文件（安装时会写一个标记文件 `.pony_accessibility_patch`，游戏目录里没有这个标记就拒绝执行），**不会碰存档**，删除前还会列出清单让你确认。
- 卸载后游戏就是原版，不需要重装游戏，也不需要 Steam 校验文件。
- 如果你之后在 Steam 里做了“验证游戏文件完整性”，Steam 可能会删掉 `winhttp.dll` 等新增文件，补丁失效，重新运行 `install.bat` 即可。

---

## 5. 常见问题

**Q：脚本提示找不到游戏目录 / 自动定位找到的目录不是我想要的？**
A：不用改脚本。用下面任意一种方式明确指定，优先级高于自动定位：

```
install.bat "X:\SteamLibrary\steamapps\common\Pony Island"
install.bat "X:\...\Pony Island\PonyIsland.exe"        rem 给 exe 路径也可以
set "PONYPATH=X:\SteamLibrary\steamapps\common\Pony Island" && install.bat
```

也可以把游戏文件夹直接拖到 `install.bat` 图标上，或在提示里输入路径。
装成功后脚本会把这次的路径记进 `game-path.txt`，之后四个脚本都优先用它；换目录重装时删掉这个文件即可。

**Q：双击 `install.bat` 提示 `BepInEx already exists` / `winhttp.dll already exists`？**
A：说明这个游戏目录已经装过补丁（或别的 BepInEx 模组）。脚本为了安全不会覆盖。已装过就直接用；想重装先运行 `uninstall.bat`。

**Q：提示 `Pony Island is running`？**
A：游戏进程还在（包括 Steam 里显示"正在运行"的状态）。完全退出游戏后重新运行脚本。

**Q：启动游戏没反应、没有状态栏？**
A：检查 `<游戏目录>\BepInEx\LogOutput.log`：
- 出现 `Accessibility Mode loaded` 和 `Patched ...` 说明补丁已加载；
- 没有这个文件说明 `winhttp.dll` 没被加载（被杀软隔离、或 Steam 校验删掉了），重新安装并把游戏目录加入杀软白名单。
- 只支持 **32 位**游戏客户端；本补丁不适用于其他游戏版本。

**Q：杀毒软件报 `winhttp.dll` 可疑？**
A：这是 Unity Doorstop 的标准注入方式（BepInEx 通用），属于误报。不放心可以只用 `patch-off.bat` 保持原版，或不要安装。

**Q：会影响 Steam 成就吗？**
A：不会。成就仍由游戏原生逻辑判断并提交给 Steam；补丁没有读写任何成就数据。实测中耶稣关通关、24/24 奖券与终战成就均为游戏正常结算。

**Q：会改我的存档吗？**
A：不会。补丁不读也不写存档；存档变化只来自你正常游玩。

**Q：F8 按了没反应？**
A：确认游戏窗口在前台（输入法/其他程序可能抢占按键）；确认状态栏存在；再查看日志里有没有 `toggled ON/OFF`。

---

## 6. 文件清单

```
PonyIsland-Accessibility-Pack\
├─ README.md                     ← 本说明
├─ install.bat                   ← 安装（自动定位游戏目录，可自定义路径）
├─ uninstall.bat                 ← 彻底卸载（只删本补丁新增的文件）
├─ patch-on.bat                  ← 重新启用注入
├─ patch-off.bat                 ← 临时关闭注入（纯原版启动）
├─ verify.bat                    ← 按 SHA256SUMS.txt 逐个校验包内文件
├─ SHA256SUMS.txt                ← 所有发布文件的 SHA-256 校验值
├─ THIRD-PARTY-NOTICES.md        ← BepInEx / Doorstop / HarmonyX 等第三方二进制的版本与许可说明
├─ game-path.txt                 ← 本机状态：上次安装用的游戏目录（install.bat 自动写，不在校验清单里）
├─ scripts\
│  └─ find-game.bat              ← 四个脚本共用的游戏目录定位逻辑
├─ payload\                      ← 实际复制进游戏目录的内容
│  ├─ winhttp.dll                （Unity Doorstop 注入器）
│  ├─ doorstop_config.ini
│  └─ BepInEx\
│     ├─ core\...                （BepInEx 5.4.23.3 x86 + HarmonyX 运行库）
│     ├─ plugins\PonyIslandAccessibility\PonyIslandAccessibility.dll
│     └─ config\local.ponyisland.accessibility.cfg
├─ docs\
│  └─ TEST-REPORT.md             ← 实测记录（9 项验收、Boss 三阶段、限制说明）
└─ src\
   ├─ AccessibilityPlugin.cs     ← 插件完整源码（可审计）
   ├─ build.rsp                  ← 编译参数（不含任何本机绝对路径，可移植）
   └─ build.bat                  ← 重新编译脚本（自动找 csc、游戏目录和依赖 DLL）
```

校验方法——直接双击 `verify.bat`（逐项显示 `OK`/`BAD`，全部通过返回 0）。手工核对某一个文件：

```powershell
Get-FileHash .\payload\BepInEx\plugins\PonyIslandAccessibility\PonyIslandAccessibility.dll -Algorithm SHA256
```

---

## 7. 已验证 / 未验证（如实说明）

**A. 游戏内实测 —— 2026-11-07，真实 Steam 流程**
测试环境是当时那台机器：Steam 库在 `D:\steam`，游戏目录 `D:\steam\steamapps\common\Pony Island`，`PonyIslandAccessibility.dll` 即本包 `payload\` 里现在这个文件（SHA-256 见开头，之后未重新编译过）。

- 耶稣关：蝴蝶、护栏/门、星星均不致死；玩家激光无法伤害耶稣；正常拿到第 7 张奖券；
- 移动/跳跃/射击/动画/场景切换正常，24/24 奖券正常累计；
- 绝望之魂三阶段全部击破，Boss 40 点血全部由原生碰撞扣减；
- 重启后 F8/F9 状态保持；
- 补丁未修改原始游戏文件、未写入奖券或成就数据。

**B. 脚本与构建改动 —— 2026-11-10，只在沙箱/假游戏目录里验证**
这一轮改的是 `install/uninstall/patch-on/patch-off.bat`、`scripts\find-game.bat`、`src\build.bat`、`src\build.rsp` 和文档，**插件 DLL 一个字节都没动**。

- 已验证：定位逻辑 14 个用例（参数、拖放 exe、`PONYPATH`、`game-path.txt`、注册表、VDF、无效输入重试）、沙箱安装→停用→启用→卸载生命周期 13 项、游戏运行中拒绝执行、无标记文件时拒绝卸载、`build.bat` 能在真实游戏目录只读编译通过；
- **未验证：这一轮的脚本没有在那台真实 Steam 安装上重跑一遍**，也没有在游戏里复测。B 类改动只是脚本层，理论上不影响游戏行为，但如果你要严谨，请把它当作"未经实机验证"来看。

**未验证 / 限制：**

- 关闭开关后的原版对照（OFF 状态）本轮未重新实测，仅通过代码路径确认（OFF 时所有拦截直接放行）；
- 除耶稣关与终战外，其他关卡只做了代码范围隔离检查，未逐关实测；
- 未在别人的机器 / 其他游戏版本上验证；BepInEx 版本更新后未重新适配。

完整细节见 `docs\TEST-REPORT.md`。

---

## 8. 从源码重新编译（可选）

发布包里的 DLL 已经过实机测试；只有你想改源码或自己审计时才需要重编。

需要 .NET Framework 4.x 的 `csc.exe`（Windows 自带，一般无需安装）。**不用先改任何路径**，直接：

```
src\build.bat
src\build.bat "X:\SteamLibrary\steamapps\common\Pony Island"    rem 手动指定游戏目录
src\build.bat /copy                                             rem 把新 DLL 写进 payload\
set "PONYCSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" && src\build.bat
```

`build.bat` 会自动定位：编译器、游戏目录（复用 `scripts\find-game.bat`，因为插件要引用游戏自带的 Unity / mscorlib 程序集）、`PonyIsland_Data\Managed` 里的 9 个引用程序集，以及 `payload\BepInEx\core` 里的 `BepInEx.dll` / `0Harmony.dll`；缺哪个会先报错并停止，不会启动编译。整个过程**只读游戏目录**，只有 `/copy` 会写进本包。

编译产物默认落在 `src\build\`，`payload\` 保持原样（`src\build\` 是本地构建输出，不进校验清单）。

**关于"重新编译得到一模一样的文件"：做不到，也不该期待。** .NET 每次编译都会生成新的 MVID 和 PE 时间戳，所以字节级不可能复现 `919c8ac2…` 这个哈希。做过 IL 级对比（token 归一化后逐条比对）：新编译的 DLL 与原发布 DLL 的唯一实质差异是 `InstallPatch` / `ReadBossHealth` 里两处 null 检查的编码方式，其余方法、字符串、补丁目标完全一致。
因此：**没有实机复测过，就不要用 `/copy` 替换 `payload\` 里的文件**，也不要因为哈希不同而认为包被改坏了——用 `verify.bat` 对照 `SHA256SUMS.txt` 才是判断依据。

---

## 9. 安全声明

- 不下载第三方修改器，不执行来源不明的脚本；
- 不使用管理员权限；
- 不修改 Steam 成就数据库、账号数据或云存档；
- 不修改原始游戏程序与存档；
- 包内的 BepInEx / Doorstop / HarmonyX 等第三方二进制全部是**未修改的官方版**，版本与许可说明见 `THIRD-PARTY-NOTICES.md`；本包不含任何游戏文件或反编译产物；
- 所有改动都可以通过 `patch-off.bat`（临时）或 `uninstall.bat`（彻底）回滚。
