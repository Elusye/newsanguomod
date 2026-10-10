# AGENTS.md — 新三国（newsanguo）开发手册

> 本文件是本仓库的**工作区指令**。每个新会话开工前先读完它，然后**只做某一个「线程」范围内的改动**（§1）。
> 安装/联机/构建的基础说明见 `README.md`——注意它已部分过时：现在有**曹魏与蜀汉两个角色**，RitsuLib 依赖版本以 `newsanguo.json` 为准。

版本：mod `newsanguo` 0.3.3 / 依赖 `STS2-RitsuLib >= 0.6.2` / 游戏 `>= 0.111.0`（`newsanguo.json`）。

---

## 0. 项目速览

《杀戮尖塔 2》的自定义内容 mod（Godot 4.5.1 Mono + C#），基座是 RitsuLib 框架。

- 两个可玩角色：曹魏 `Scripts/Characters/CaoWeiCharacter.cs`、蜀汉 `Scripts/Characters/ShuHanCharacter.cs`
- 规模：`Scripts/Cards` 132 个 `.cs`、`Scripts/Powers` 44、`Scripts/Patches` 17、`Scripts/Characters` 6、`Scripts/Relics` 10、`Scripts/Events` 5、`Scripts/Combat` 4、`Scripts/Potions` 2、`Scripts/Monsters` 2、`Scripts/Api` 2、`Scripts/Settings` 2、`Scripts/Telemetry` 1、`Scripts/DevConsole` 1；根目录 `Scripts/Entry.cs`（mod 入口）、`Scripts/NewsanguoSfx.cs`（音效）、`Scripts/HearingVolumeController.cs`
- 资源：`newsanguo/images` 530 个文件、`newsanguo/audios` 308、`newsanguo/localization/{zhs,eng}` 各 10 个 JSON
- 核心机制：天意之力（`Scripts/Combat/HeavensForce.cs`）、酒力、战斗内附魔（`Scripts/Helpers/EnchantHelper.cs`）、Scry（`Scripts/Combat/ScryCmd.cs`）
- 跨 mod 互操作契约：`Scripts/Api/NewsanguoPublicApi.cs`（`[ModInterop]` 调用，类名/签名发布后视为契约，改它要单独开线程）
- 遥测：`Scripts/Telemetry/NewsanguoTelemetry.cs`（只申请 `run_history` 权限；`IngestEndpoint` 为空则完全不注册）

---

## 1. 线程地图（按功能切分会话）

一条会话只做一条线程，避免上下文互相污染。每条线程的「常驻参考」是它反复要读的东西；不在本线程的文件不要顺手改。

| # | 线程 | 范围 | 常驻参考 | 典型工作 |
| --- | --- | --- | --- | --- |
| 1 | **卡牌与能力** | `Scripts/Cards`、`Scripts/Powers`、`localization/*/{cards,powers}.json`、`Scripts/NewsanguoSfx.cs` 音效表 | `Scripts/Cards/Tremble.cs`、`Scripts/Cards/HeavenlyDeluge.cs`、`Scripts/Combat/HeavensForce.cs`、§3.1 落点清单 | 新增/调整卡牌与能力、数值与文案 |
| 2 | **引擎补丁与战斗系统** | `Scripts/Patches`、`Scripts/Combat`、`Scripts/Monsters`、`Scripts/Api`、`Scripts/DevConsole`、`Scripts/Telemetry` | `.decompile/sts2full/sts2.decompiled.cs`（按行号 grep）、`.decompile/live111/`、docs/facts.md §4 | Harmony 补丁、引擎语义查证、联机同步 |
| 3 | **角色 / 遗物 / 事件 / 设置（meta 框架）** | `Scripts/Characters`、`Relics`、`Potions`、`Events`、`Rewards`、`Settings`、`localization/*/{characters,relics,potions,events,ancients}.json` | `.decompile/STS2RitsuLib.Scaffolding.Characters*`、`.decompile/STS2RitsuLib.Settings`、docs/facts.md §4.2/4.5/4.6 | 卡池归属、可见性/解锁、设置页、先古对话 |
| 4 | **美术与音频素材** | `materials/`、`newsanguo/images`、`newsanguo/audios` | docs/assets.md §6 命名约定、`AssetProfile` 路径写法 | 卡图/图标/音效补齐与替换 |
| 5 | **PostHog 数据分析** | `tools/posthog-*.ps1`、`analysis/posthog/**` | `analysis/posthog/report.md`、docs/posthog.md §7 | 跑 SQL、做/改 insight、写报告 |
| 6 | **改名与规范重构**（一次性） | 跨 `Scripts/Cards` + `localization` + `images` | docs/todo.md §8「改名取舍」 | 只有用户拍板后才动，代价见 docs/facts.md §4.9 |

跨线程的公共改动（`Entry.cs`、`Scripts/Api`、`ModCharacterTemplate` 之类基类、`NewsanguoSfx.cs` 框架部分）单独判断，别在两条线程里同时改同一个公共文件。

---

## 2. 铁律

1. **构建与部署必须校验**
   ```powershell
   dotnet build newsanguo.csproj -c Debug -v m   # cwd = 仓库根
   ```
   PostBuild 会把 DLL 复制到 `E:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\newsanguo\newsanguo.dll`。
   汇报前**必须**比对构建产物 `.godot/mono/temp/bin/Debug/newsanguo.dll` 与 mods 目录那份的**大小 / 修改时间 / SHA256**一致（`Get-FileHash -Algorithm SHA256`）。
   基线：改动前通常是 **0 错误 / 15 警告**（均为既有的 CS86xx 可空性警告，不要顺手"修"）。
2. **游戏运行中构建会因 DLL 锁定导致 PostBuild 复制失败**，此时让用户先关游戏，再手动复制并校验哈希。
3. **pck 一律由用户重导**（用户明确约定）。改 `.cs` 不需要；改 `newsanguo/localization/**`、`newsanguo/images/**`、`newsanguo/audios/**`、`scenes/**` **必须**在汇报里提醒用户重导 pck（命令见 `README.md` §2）。设置页文案用 `ModSettingsText.Literal` 代码字面量，不走本地化 ⇒ 不需要重导。**打包配置不可回退**：`tools/.gdignore` 与 `export_presets.cfg` 的 `exclude_filter` 是 0.3.1 选牌事故的修复，详见 docs/facts.md §4.8。
4. **类名就是 id，不要擅自改名**。证据：`debug.log` 里 `Registered card: AlwaysMine (id=NEWSANGUO_CARD_ALWAYS_MINE)`；仓内没有任何 `override string Id`。改名 = 换 id ⇒ 老存档牌组里的这些牌失效，且要同步本地化键（zhs+eng）、卡图 PNG 与 `.godot` 的 `.import`、两个池列表、别处 `HoverTipFactory.FromCard<T>()` 引用。必须先让用户在 docs/todo.md §8「改名取舍」里拍板。派生规则已查证：id = `NEWSANGUO_CARD_` + `ToUpperSnakeCase(类名)`（`.decompile/loadout/Loadout.decompiled.cs:64134-64143`），**命名空间与显示名都不参与**——详见 docs/facts.md §4.9。
5. **汇报用中文，精确到 `文件:行`**，附构建/部署证据；不要只写"已完成"。
6. **不要整读 `.decompile/sts2full/sts2.decompiled.cs`（508,815 行）**：先 grep 关键词定位，只读那几十行；查到的结论回填 docs/facts.md §4，避免下一条线程重复查证。
7. **本地化 JSON 约定**：无 BOM、LF 结尾（其余资源文件继承仓库 `gitattributes`）。追加/替换优先用整串写入：
   ```powershell
   [IO.File]::WriteAllText($path, $json, (New-Object Text.UTF8Encoding($false)))
   ```
   改完用 `ConvertFrom-Json` 校验并比对键数（当前基线：`cards.json` 各 **266 键**、`powers.json` 各 **119 键**）。注意 `cards.json` 是**扁平键**（键名自带 `.title`/`.description` 后缀，例如 `"NEWSANGUO_CARD_STRIKE_CAOWEI.title"`），`ConvertFrom-Json` 后要用 `$j."<键>.title"` 取值，不能写 `$j.<键>.title`。**字符串里的换行必须写成 JSON 转义 `\n`**——曾出现写成 `\\n` 的 bug（游戏里会原样显示 `\n`）。
8. **不新增第三方依赖**。动手写 Harmony 补丁前，先查 `.decompile/STS2RitsuLib*` 是否已有现成机制（角色可见性、卡池过滤、免费打出、设置页、存档数据等都有现成的）。
9. **不跑 `git-push.bat`、不提交 git**，除非用户明确要求。
10. 用 `pwsh` 读写含中文的文件时注意编码：读取用 `-Encoding UTF8`，写入用上面的 `UTF8Encoding($false)`，否则会出现乱码。**PowerShell 的比较/匹配默认大小写不敏感**（`-eq`/`-ne`/`-like`/`Select-String`）⇒ 批量改类名、"残留旧名"扫描一律用 `-ceq`/`-cne`/`-CaseSensitive` 或 `grep` 工具（ripgrep 默认敏感），否则「整文件只差大小写」的文件会被判为未变化而漏改，新名包含旧名子串（`RatPoisonScheme` ⊃ `RatPoison`）会报假阳性。**Windows 文件系统同样大小写不敏感**：`WhereSWine` 与 `WheresWine` 是**同一个路径**，对这类"只改大小写"的名字执行「写新文件 → 删旧文件」等于删掉刚写的那个（2026-10-09 线程 6 因此丢了 3 个 `.import`，见 docs/facts.md §4.13）。

---

## 3. 改动落点清单

### 3.1 新增一张卡（六处同步，缺一就会出现"没图/没文案/没音效"）
1. `Scripts/Cards/<类名>.cs`：`[RegisterCard(typeof(<池>))]`；构造 `base(费用, CardType.X, CardRarity.Y, TargetType.Z)`（技能+全体敌人的先例：`Scripts/Cards/Tremble.cs:48`）；`CanonicalVars`、`OnUpgrade()`、`AdditionalHoverTips`、`AssetProfile`（`PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"`）。带 `CardKeyword.Exhaust` 时**不需要**手写悬停说明（引擎自动，先例 `Scripts/Cards/Soldier.cs:35`）。
   - **命名规则（2026-10-09 用户拍板，m00440）**：卡牌与能力的**类名必须是英文 title 的驼峰形式**——`DarkHornShark` ↔ "Dark Horn Shark"、`BoneMeltingPalm` ↔ "Bone-Melting Palm"、`TigerWindCloudDragon` ↔ "Tiger Wind, Cloud Dragon"（标点/撇号/感叹号在类名里省略，与 `ImGettingDrunk`/`WheresWine` 的既有惯例一致）；**英文 title 建议 ≤24 字符**（当前 126 张平均 14.1；超长的处理见 docs/facts.md §4.9 第二批记录）。改类名 = 换 id（docs/facts.md §4.9）⇒ 动名前必须先按 docs/todo.md §8.1 的取舍清单取得用户同意。
2. 卡池列表：卡池归属由 `[RegisterCard]` 特性决定；**两角色共有**的牌惯例是把特性写成 `typeof(NewsanguoCardPool)` 并在 `Scripts/Characters/ShuHanCardPool.cs` 的 `CardTypes` 里也列一次。见 docs/facts.md §4.6。
3. `newsanguo/localization/zhs/cards.json` 与 `.../eng/cards.json`：键 `NEWSANGUO_CARD_<类名转 UPPER_SNAKE>.title` / `.description`（映射例证：`MaClanQuadBlast` → `NEWSANGUO_CARD_MA_CLAN_QUAD_BLAST`，驼峰拆词）。
4. 音效：`Scripts/NewsanguoSfx.cs` 头部注释 + 表内按字母序插 `["<snake_name>"] = 0f,`，音频文件 `newsanguo/audios/<snake_name>.mp3`（缺失不会崩，但会静音）。
5. 卡图 PNG：`newsanguo/images/cards/<类名>.png`（缺失时用占位，见"待补素材"）。
6. 提醒用户重导 pck。

### 3.2 改数值 / 文案
数值写在 `CanonicalVars`，升级增量用 `UpgradeValueBy(...)` / `UpgradeValueBy(-1)`；描述里用 `{Damage:diff()}`、`{HeavensForcePower:inverseDiff()}` 等 SmartFormat，**改数值不需要动 JSON**（卡面是公式）。全仓改数值前后都 grep 一遍，确认没有第二处硬编码。

### 3.3 新增能力
`Scripts/Powers/<名字>Power.cs`。卡牌与能力在**本地化键/音效键**上不可重名（能力键加 `_power` 后缀，`README.md`「开发约定」）。升级/降级关键字增减用 `OnUpgrade()` / `AfterDowngraded()` 里的 `AddKeyword` / `RemoveKeyword`，不要直接改 `CanonicalKeywords`。

### 3.4 新增设置项
照 `Scripts/Settings/NewsanguoSfxVolumeSettings.cs:26-73`：`RitsuLibFramework.RegisterModSettings(ModId, page => page.WithSortOrder(0).WithModDisplayName(...).WithTitle(...).AddSection(...).AddToggle/AddSlider(...))`，值用 `ModSettingsBindings.Callback<T>(ModId, dataKey, read, write, save)` 绑定到自己的静态属性，属性 setter 里即时写盘（`user://newsanguo_*.json`）。一个 mod 一个页面 + 多个分区。纯代码 ⇒ 不重导 pck。

### 3.5 音效
`Scripts/NewsanguoSfx.cs` 表 + `newsanguo/audios/*.mp3`；引擎级 FMOD 事件路径（选人/死亡）由 `Scripts/Patches/EngineSfxRedirectPatch` 截获后转交 `NewsanguoSfx` 播放同名资源。**`newsanguo.bank` 已删除**，不要再往 FMOD 方向加东西。

---

## 4–8. 分册索引（原 §4–§8 已拆到 `docs/`，编号沿用）

> 根文件只留「每次开工都要看」的内容（§0–§3、§9）。下面这些分册**按需读**，改完代码把新查证的事实**回填分册**而不是根文件——根文件必须保持短小，否则会被工作区指令预算截断（Codex 默认 32 KiB、DSH 65,536 B；本文件目前约 13 KB，都安全）。
> 分册与本文件冲突时，以分册为准（分册更新更频繁）。

| 要查什么 | 分册 | 原编号 |
| --- | --- | --- |
| 引擎 API 与已查证事实：能力移除/类型、角色选择可见性（测试模式）、单人多人卡牌过滤、RitsuLib 设置与跑局数据、卡池归属、天意之力与免费打出、反编译索引（`.decompile/`）、PCK 打包事故与验证基线、卡牌 id 派生与改名代价（含两批改名映射表）、`HoverTipFactory.FromCard<T>` 引用清单、命名空间统一、卡牌数量口径、素材/Godot 导入缓存改名规程、能力钩子 `ShouldReceiveCombatHooks` 语义 | `docs/facts.md` | §4.1–§4.14 |
| 本地化：目录与键规则、SmartFormat、怪物图鉴招式名（`<怪物id>.moves.<状态名去掉 _MOVE>.title`）、JSON 写法与校验、改完必须提醒重导 pck | `docs/localization.md` | §5 |
| 素材：卡图/能力图/音频路径约定、`AssetProfile` 写法、待补素材清单 | `docs/assets.md` | §6 |
| PostHog 数据分析：项目与 insight id、工具脚本、已知 SQL 限制 | `docs/posthog.md` | §7 |
| 当前待办：待用户拍板、待实机验证、待用户执行 | `docs/todo.md` | §8.1–§8.3 |

动手改某个子系统前先读它对应的分册；根文件里 `docs/facts.md §4.9` 这类引用直接点到哪里读。

---
## 9. 与用户协作的约定

- 用户在 Windows 上、用中文交流；仓库注释、汇报一律中文。
- 用户非常在意**精确**：汇报要给 `文件:行`、给构建结果、给部署产物的哈希；不要含糊说"应该没问题"。
- 用户会自己实机验收并回来反馈，**不要假称已验证游戏内表现**；没实测就写"待实机确认"。
- 用户自己负责重导 pck 与 git 推送；agent 只改代码/资源并提醒。
- 改动尽量小而聚焦，顺手发现的问题要么当场说明并单独提示，要么写进 docs/todo.md §8，不要悄悄大改。
- 需要用户在"几种做法"里选择时，用清晰选项（像 docs/todo.md §8.1 那样列出 a/b/c 与各自代价）问，而不是替用户决定。
