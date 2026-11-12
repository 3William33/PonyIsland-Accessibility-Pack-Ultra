# Pony Island 可访问性补丁 — 测试报告

> **读这份报告前先知道的三件事**
>
> 1. **文中路径分三类**，不要混：
>    - **实测环境**：报告中的游戏安装目录已泛化处理，不包含测试机的本地绝对路径；
>    - **历史工作目录**：`dist\...`、`backup\...` 是当时原始工作目录里的位置，**发布包里已经没有这两个目录**（脚本现在就在包根目录，备份文件不随包分发）；
>    - **示例路径**：README 里出现的 `X:\SteamLibrary\steamapps\common\Pony Island` 之类只是格式演示。
> 2. **本报告的实机结论只覆盖插件 DLL**（SHA-256 `919c8ac2…`，至今未重新编译）。
> 3. 报告末尾另有一段 **2026-11-10 脚本/构建改动**的说明，那部分**没有做过实机复测**，只在沙箱里验证过。

日期：2026-11-07（实机测试）
环境：Windows 10.0.26200 / Steam 版 Pony Island / Unity 2017.4.40f1 (x86) / BepInEx 5.4.23.3 (x86) / HarmonyX
插件：`PonyIslandAccessibility.dll` v1.0.0
SHA-256：`919c8ac2efb3ecd7d79499396d7191d1ab7ccc41d93951af521557a8694def5e`
安装位置（测试机）：`<SteamLibrary>\steamapps\common\Pony Island\BepInEx\plugins\PonyIslandAccessibility\`
配置：`BepInEx\config\local.ponyisland.accessibility.cfg`（`Enabled = true`，`AssistHopelessSoul = true`）

## 结论

- 耶稣关（game_PART8 / Level4Screen）的碰撞免疫与“玩家无法伤害耶稣”均实际生效，并用真实流程拿到第 7 张奖券。
- 24 张奖券全部到账，游戏正常进入 game_FINALBOSS 并完成三阶段绝望之魂。
- 补丁未直接写入奖券数量、成就数据或存档；未修改任何原始游戏文件。
- Steam 成就已由用户在本机 Steam 客户端确认到账（用户反馈，2026-11-07）。

## 验收项

| # | 验收项 | 结果 | 证据 |
|---|---|---|---|
| 1 | 蝴蝶碰撞不致死 | 通过 | 日志 `Prevented butterfly/enemy damage` |
| 2 | 护栏/门碰撞不致死 | 通过 | 日志 `Prevented gate/rail damage` |
| 3 | 耶稣的星星不致死 | 通过 | 日志 `Prevented star/projectile damage`，星星被正常销毁 |
| 4 | 移动 / 跳跃 / 射击正常 | 通过 | 实际完成 Level4 与 Boss 三阶段 |
| 5 | 耶稣不被伤害 | 通过 | 日志 `Blocked player laser damage to Jesus (health untouched)`；关卡正常完成 |
| 6 | 奖券逻辑正常 | 通过 | Level4 触发 `CheckFindTicket(7)`；存档 `ticketsnumberof = 0x18 (24)` |
| 7 | 重启后状态保持 | 通过 | 重启游戏后 BepInEx 配置仍为 `Enabled = true` / `AssistHopelessSoul = true`，状态栏恢复显示 |
| 8 | 关闭后恢复原版 | 代码路径通过，未复测 | 所有 Prefix 在开关为 OFF 时直接 `return true`；本轮未在 Level4 重新做 OFF 对照 |
| 9 | 原始存档未被手动修改 | 通过 | 修改前已备份注册表；全程未写注册表/存档，变化仅来自正常游玩 |

## 绝望之魂（game_FINALBOSS）

- Phase1：Boss 血量 40 → 0；随后进入 Phase2（存档 `screen=Phase2`）。
- Phase2：Boss 血量 40 → 0；随后进入 Phase3。
- Phase3：Boss 血量 40 → 0，触发 `OnPhaseComplete → EndPhase3`，Boss 血条与 UI 消失。
- 阶段结束后日志中的 `4.0 / 40` 是 `HopelessSoulBoss.ResetHealth()` 的 `+4`，不是复活；Boss 已被 `Deactivate()`。
- Boss 的 40 点生命全部由游戏自身的激光碰撞逻辑扣除，补丁没有写入生命值。
- Boss 辅助仅在 `game_FINALBOSS` 生效：拦截 `OnPonyCollision` 的“重置+回血”，按住射击键，Phase1 自动瞄准；对其他关卡无影响。

结局台词已在 level17 场景数据中核对：

```
THE GAME IS OVER.
I WON'T EVEN SHUT IT DOWN THIS TIME
THAT'S UP TO YOU
IF YOU THINK SOMETHING CRAZY IS ABOUT TO HAPPEN
WELL, IT JUST ISN'T.
GAME OVER.
THANKS FOR PLAYING.
```

结局最后一步由玩家自行关闭游戏，不存在出口触发器。

## 安全与回滚

以下 `backup\...` 文件在**当时的原始工作目录**里，不随发布包分发；发布包里没有 `backup\` 和 `dist\` 目录。

- 原始游戏文件：安装前已生成 176 个文件的 SHA-256 清单（`backup/game-original-sha256.txt`），游戏 exe 与场景文件时间戳未变化。
- 存档备份（注册表导出，测试机本地保存）：
  - `backup/PonyIsland-playerprefs-before.reg`（`c524dbf3…569`）
  - `backup/PonyIsland-playerprefs-before-boss.reg`（`d12bb8a4…891`）
- 未使用管理员权限，未修改 Steam 成就数据库、账号数据或云存档。
- 卸载：运行包根目录的 `uninstall.bat`，删除插件与 BepInEx 目录及 `winhttp.dll` / `doorstop_config.ini`；存档不受影响。
- 一键启停：`patch-on.bat` / `patch-off.bat`；游戏内 F8 切换可访问性模式，F9 切换 Boss 辅助。

## 未验证 / 限制

- 验收项 8（OFF 后原版对照）本轮未复测。
- 其他关卡只做了代码范围隔离检查，未逐关实测。
- Steam 成就到账由用户在本机 Steam 客户端确认；补丁侧没有查询或写入 Steam 统计。

## 2026-11-10 补充：脚本与构建改动（未做实机复测）

这一轮只动**工具层**，`payload\` 里的 DLL、BepInEx 运行库和配置文件一个字节都没改，插件源码也未改，所以上面所有游戏内结论仍然对应现在发布的这个 DLL。

改动内容：

- `install.bat` / `uninstall.bat` / `patch-on.bat` / `patch-off.bat`：使用共用 `scripts\find-game.bat` 的 5 级自动定位（参数 → `game-path.txt` → `PONYPATH` → 注册表/VDF/常见安装位置 → 手动输入），并加上"游戏进程在运行就拒绝执行"的前置检查、`payload\` 完整性检查、卸载前的确认与标记文件校验、确定的退出码。
- `src\build.rsp`：移除原来写死的个人绝对路径，改为不含任何本机路径、也不含注释（C# 编译器不接受 rsp 里的 `//`）的可移植参数。
- `src\build.bat`（新增）：自动定位 `csc.exe`、游戏目录、`PonyIsland_Data\Managed` 的 9 个引用程序集与 `payload\BepInEx\core` 的 2 个加载器程序集；缺依赖就先报错、不启动编译；只读游戏目录，加 `/copy` 才写进 `payload\`。
- `verify.bat`（新增）：逐项校验 `SHA256SUMS.txt`。
- 文档：区分示例路径 / 自动定位（默认）/ 实测环境三类写法；本段即为此补充。
- 隐私扫描：包内文本不含个人用户名、个人目录、凭据或密钥；测试机路径已泛化处理。

验证方式与边界（如实）：

| 层次 | 做了什么 | 算不算通过 |
|---|---|---|
| 静态 | 30/30 清单哈希一致；zip 与目录内容一致；已安装的游戏内 DLL 与包内 DLL 同哈希；DLL 无内嵌 PDB / 无用户名 | 通过 |
| 脚本逻辑 | 定位逻辑 14 个用例；假游戏目录（沙箱）里安装 → 停用 → 启用 → 卸载生命周期 13 项；无效路径、带引号粘贴、给 exe 路径、游戏运行中、无标记文件等负例。所有 `.bat` 之后统一改为 CRLF（批处理的标准行尾），改完后**上述用例全部重跑一遍**：生命周期 13 项、定位 9 项、提示输入 5 项仍为预期结果，`game-path.txt`（CRLF）读回不会残留回车符 | 通过 |
| 构建 | 在真实游戏目录**只读**执行 `src\build.bat` 编译成功；用 Mono.Cecil 做 token 归一化 IL 对比，差异仅 `InstallPatch` / `ReadBossHealth` 两处 null 检查编码 + 分支偏移，字符串与补丁目标一致 | 通过（并因此确认"重编不可能字节复现"，`payload\` 保留实测 DLL） |
| 实机 | 本轮**没有**在真实 Steam 安装上重跑新脚本，也没有再进游戏复测 | **未验证** |

结论：新脚本的风险主要在路径定位（已用假目录覆盖），不影响游戏内行为；但如果你要求"发布包每一项都在真实环境验证过"，那这一轮的工具层改动目前只有沙箱证据。
