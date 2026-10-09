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
| 2 | **引擎补丁与战斗系统** | `Scripts/Patches`、`Scripts/Combat`、`Scripts/Monsters`、`Scripts/Api`、`Scripts/DevConsole`、`Scripts/Telemetry` | `.decompile/sts2full/sts2.decompiled.cs`（按行号 grep）、`.decompile/live111/`、§4 | Harmony 补丁、引擎语义查证、联机同步 |
| 3 | **角色 / 遗物 / 事件 / 设置（meta 框架）** | `Scripts/Characters`、`Relics`、`Potions`、`Events`、`Rewards`、`Settings`、`localization/*/{characters,relics,potions,events,ancients}.json` | `.decompile/STS2RitsuLib.Scaffolding.Characters*`、`.decompile/STS2RitsuLib.Settings`、§4.2/4.5/4.6 | 卡池归属、可见性/解锁、设置页、先古对话 |
| 4 | **美术与音频素材** | `materials/`、`newsanguo/images`、`newsanguo/audios` | §6 命名约定、`AssetProfile` 路径写法 | 卡图/图标/音效补齐与替换 |
| 5 | **PostHog 数据分析** | `tools/posthog-*.ps1`、`analysis/posthog/**` | `analysis/posthog/report.md`、§7 | 跑 SQL、做/改 insight、写报告 |
| 6 | **改名与规范重构**（一次性） | 跨 `Scripts/Cards` + `localization` + `images` | §8「改名取舍」 | 只有用户拍板后才动，代价见 §2.4 |

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
3. **pck 一律由用户重导**（用户明确约定）。改 `.cs` 不需要；改 `newsanguo/localization/**`、`newsanguo/images/**`、`newsanguo/audios/**`、`scenes/**` **必须**在汇报里提醒用户重导 pck（命令见 `README.md` §2）。设置页文案用 `ModSettingsText.Literal` 代码字面量，不走本地化 ⇒ 不需要重导。**打包配置不可回退**：`tools/.gdignore` 与 `export_presets.cfg` 的 `exclude_filter` 是 0.3.1 选牌事故的修复，详见 §4.8。
4. **类名就是 id，不要擅自改名**。证据：`debug.log` 里 `Registered card: AlwaysMine (id=NEWSANGUO_CARD_ALWAYS_MINE)`；仓内没有任何 `override string Id`。改名 = 换 id ⇒ 老存档牌组里的这些牌失效，且要同步本地化键（zhs+eng）、卡图 PNG 与 `.godot` 的 `.import`、两个池列表、别处 `HoverTipFactory.FromCard<T>()` 引用。必须先让用户在 §8「改名取舍」里拍板。派生规则已查证：id = `NEWSANGUO_CARD_` + `ToUpperSnakeCase(类名)`（`.decompile/loadout/Loadout.decompiled.cs:64134-64143`），**命名空间与显示名都不参与**——详见 §4.9。
5. **汇报用中文，精确到 `文件:行`**，附构建/部署证据；不要只写"已完成"。
6. **不要整读 `.decompile/sts2full/sts2.decompiled.cs`（508,815 行）**：先 grep 关键词定位，只读那几十行；查到的结论回填 §4，避免下一条线程重复查证。
7. **本地化 JSON 约定**：无 BOM、LF 结尾（其余资源文件继承仓库 `gitattributes`）。追加/替换优先用整串写入：
   ```powershell
   [IO.File]::WriteAllText($path, $json, (New-Object Text.UTF8Encoding($false)))
   ```
   改完用 `ConvertFrom-Json` 校验并比对键数（当前基线：`cards.json` 各 **266 键**、`powers.json` 各 **119 键**）。注意 `cards.json` 是**扁平键**（键名自带 `.title`/`.description` 后缀，例如 `"NEWSANGUO_CARD_STRIKE_CAOWEI.title"`），`ConvertFrom-Json` 后要用 `$j."<键>.title"` 取值，不能写 `$j.<键>.title`。**字符串里的换行必须写成 JSON 转义 `\n`**——曾出现写成 `\\n` 的 bug（游戏里会原样显示 `\n`）。
8. **不新增第三方依赖**。动手写 Harmony 补丁前，先查 `.decompile/STS2RitsuLib*` 是否已有现成机制（角色可见性、卡池过滤、免费打出、设置页、存档数据等都有现成的）。
9. **不跑 `git-push.bat`、不提交 git**，除非用户明确要求。
10. 用 `pwsh` 读写含中文的文件时注意编码：读取用 `-Encoding UTF8`，写入用上面的 `UTF8Encoding($false)`，否则会出现乱码。**PowerShell 的比较/匹配默认大小写不敏感**（`-eq`/`-ne`/`-like`/`Select-String`）⇒ 批量改类名、"残留旧名"扫描一律用 `-ceq`/`-cne`/`-CaseSensitive` 或 `grep` 工具（ripgrep 默认敏感），否则「整文件只差大小写」的文件会被判为未变化而漏改，新名包含旧名子串（`RatPoisonScheme` ⊃ `RatPoison`）会报假阳性。**Windows 文件系统同样大小写不敏感**：`WhereSWine` 与 `WheresWine` 是**同一个路径**，对这类"只改大小写"的名字执行「写新文件 → 删旧文件」等于删掉刚写的那个（2026-10-09 线程 6 因此丢了 3 个 `.import`，见 §4.13）。

---

## 3. 改动落点清单

### 3.1 新增一张卡（六处同步，缺一就会出现"没图/没文案/没音效"）
1. `Scripts/Cards/<类名>.cs`：`[RegisterCard(typeof(<池>))]`；构造 `base(费用, CardType.X, CardRarity.Y, TargetType.Z)`（技能+全体敌人的先例：`Scripts/Cards/Tremble.cs:48`）；`CanonicalVars`、`OnUpgrade()`、`AdditionalHoverTips`、`AssetProfile`（`PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"`）。带 `CardKeyword.Exhaust` 时**不需要**手写悬停说明（引擎自动，先例 `Scripts/Cards/Soldier.cs:35`）。
   - **命名规则（2026-10-09 用户拍板，m00440）**：卡牌与能力的**类名必须是英文 title 的驼峰形式**——`DarkHornShark` ↔ "Dark Horn Shark"、`BoneMeltingPalm` ↔ "Bone-Melting Palm"、`TigerWindCloudDragon` ↔ "Tiger Wind, Cloud Dragon"（标点/撇号/感叹号在类名里省略，与 `ImGettingDrunk`/`WheresWine` 的既有惯例一致）；**英文 title 建议 ≤24 字符**（当前 126 张平均 14.1；超长的处理见 §4.9 第二批记录）。改类名 = 换 id（§4.9）⇒ 动名前必须先按 §8.1 的取舍清单取得用户同意。
2. 卡池列表：卡池归属由 `[RegisterCard]` 特性决定；**两角色共有**的牌惯例是把特性写成 `typeof(NewsanguoCardPool)` 并在 `Scripts/Characters/ShuHanCardPool.cs` 的 `CardTypes` 里也列一次。见 §4.6。
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

## 4. 已查证事实（带出处，不必重新查证）

### 4.1 能力的移除与类型
- `PowerCmd.Remove(PowerModel? power)`（`.decompile/sts2full/sts2.decompiled.cs:198579-198587`）= `power.RemoveInternal(); await Cmd.CustomScaledWait(0.2f, 0.4f); await power.AfterRemoved(power.Owner);`——除 `AssertMutable()` 外**没有任何"不可移除"守卫**；每次移除自带 0.2~0.4s 演出等待。
- `PowerModel.Type`（抽象）`:79311`；`TypeForCurrentAmount => GetTypeForAmount(Amount)` `:79409`；`RemoveInternal()` `:79744-79749`；`enum PowerType { None, Buff, Debuff }` `:176188-176193`；`Creature.Powers => _powers` `:180960`。
- **不要直接调** `Creature.RemoveAllPowersInternalExcept`（`:181296`；`:181278` 注释明写 "ONLY PowerModel.RemoveInternal should be calling this"）。
- 清敌方正面效果的正例：`Scripts/Cards/BrewHealsAll.cs:71-75`（筛 `TypeForCurrentAmount == PowerType.Debuff` + `PowerCmd.Remove`）；「时光酸雨」（旧名「时空酸雨」，2026-10-09 改名，类名与 id 一起换：`Scripts/Cards/SpaceTimeAcidRain.cs` → `Scripts/Cards/TimeAcidRain.cs`、id `NEWSANGUO_CARD_SPACE_TIME_ACID_RAIN` → `NEWSANGUO_CARD_TIME_ACID_RAIN`）用同法筛 `PowerType.Buff`（`Scripts/Cards/TimeAcidRain.cs`）。
- **清敌方正面效果必须排除「流沙」`SandpitPower`**（2026-10-09 用户反馈：清掉它会让玩家当场死亡）：它是全 dump 里唯一「被移除就强杀玩家」的能力——类定义 `.decompile/sts2full/sts2.decompiled.cs:104503`、`Type => PowerType.Buff` `:104515`（所以会被正面效果筛选命中）、`StackType => Counter` `:104517`、`InstanceType => Instanced` `:104519`，挂在**敌人**（「贪得无厌」The Insatiable：`:120678-120680` 施放时 `sandpitPower.Target = target` 指向玩家）；`AfterRemoved` `:104573-104606` 在「施法者与 `Target` 都没死」时播完收尾演出后 `:104601-104604` 对 `AllAffectedCreatures`（玩家 + 全部宠物）执行 `await CreatureCmd.Kill(allAffectedCreature2, force: true);`。⇒ 实现写法：`Scripts/Cards/TimeAcidRain.cs:91` 的筛选是 `p.TypeForCurrentAmount == PowerType.Buff && p is not SandpitPower`（`using MegaCrit.Sts2.Core.Models.Powers;`，命名空间落在 `:98332`）。旁证：全 dump 15 个 `AfterRemoved` 覆写里只有它一个是 `async` 并调用 `CreatureCmd.Kill`（另一处 `:106916` 的 `TheGambitPower` 是 `PowerType.Debuff` `:106906`，不进正面效果筛选）。

### 4.2 角色选择界面的可见性（"测试模式"就是靠这套做的）
- `CharacterModel.IsPlayable`（`.decompile/live111/CharacterModel.cs:35`、`.decompile/sts2full/sts2.decompiled.cs:75329`，文档在 `:75327`：`False for test characters (Deprived), meta-characters (RandomCharacter)`）被很多系统用于过滤，但**选人界面不查它**——别指望覆写它能隐藏角色。
- 选人界面：`NCharacterSelectScreen.InitCharacterButtons()`（`:384505-384513`）直接 `foreach (CharacterModel allCharacter in ModelDb.AllCharacters)` 造按钮；`NCharacterSelectButton.Init(...)`（`:383150-383173`）用 `_isLocked = !unlockState.Characters.Contains(_character)` 决定锁定，锁定态显示锁图标 + 解锁提示（`:383185`）；`UpdateRandomCharacterVisibility()`（`:384540-384566`）**要求全员解锁才显示「随机角色」按钮**。
- **正解**：RitsuLib `IModCharacterVanillaSelectionPolicy`（`.decompile/STS2RitsuLib.Scaffolding.Characters/IModCharacterVanillaSelectionPolicy.cs:9-28`）三个属性 `HideFromVanillaCharacterSelect` / `AllowInVanillaRandomCharacterSelect` / `HideInCardLibraryCompendium`；`ModCharacterTemplate.cs:39` 已实现该接口，`:278/:281/:284` 给出虚属性默认值 ⇒ 在角色类里 `override` 即可。生效机制：`CharacterVanillaSelectionPolicyPatches.cs:25-34` 给 `InitCharacterButtons`/`UpdateRandomCharacterVisibility`/`NCharacterSelectButton.Init`/`StartRunLobby.BeginRunLocally` 挂作用域，`CharacterVanillaSelectionPolicyAllCharactersPatch.cs:28,40-44` 在 `ModelDb.AllCharacters` getter 上按作用域过滤（`CharacterVanillaSelectionPolicyScope.cs:49-58,60-71`）。
- **不要**改用「epoch 解锁 / 从 `UnlockState.Characters` 摘掉」来隐藏角色：那只会让角色以"锁定"样子留在选人界面，并**永久藏掉「随机角色」按钮**。详见 `.decompile/STS2RitsuLib.Unlocks/ModUnlockRegistry.cs:197-200,677-691`（未注册 epoch 要求时 `IsUnlocked` 直接返回 true）与 `CharacterUnlockFilterPatch.cs:25-31`。
- 这类属性是**每次构建界面时读取**（不是模型注册期烘焙）⇒ 改开关后重进选人界面即生效，**不必重启**。当前实现：`Scripts/Settings/NewsanguoTestModeSettings.cs` + `Scripts/Characters/ShuHanCharacter.cs:38-51`。

### 4.3 单人/多人卡牌过滤
`CardModel.MultiplayerConstraint => CardMultiplayerConstraint.None`（`:73196`）；`CardPoolModel.GetUnlockedCards`（`:75285-75295`）单人局移除 `MultiplayerOnly`、多人局移除 `SingleplayerOnly`；`enum CardMultiplayerConstraint { None, MultiplayerOnly, SingleplayerOnly }`（`:182196-182201`）。删掉子类里的 `MultiplayerConstraint` override 就回落到 `None`（「你拾它作甚！」就是这么改成单人可遇到的）。

### 4.4 RitsuLib 设置页 / 数据
- `RitsuLibFramework.RegisterModSettings(string modId, Action<ModSettingsPageBuilder> configure, string? pageId = null)`（`.decompile/STS2RitsuLib/RitsuLibFramework.cs:1439`）。
- 自定义 SmartFormat 源（想做"按设置切换卡面文案"）：`RegisterSmartFormatSourceAttribute`（`.decompile/STS2RitsuLib.Interop.AutoRegistration/RegisterSmartFormatSourceAttribute.cs:10`）；LexNinja2 的用法是 `{IfChallengeMode:cond:<挑战文本>|<普通文本>}`。
- 跑局内数据可用 `RitsuLibFramework.BeginModDataRegistration` + `GetRunSavedDataStore(ModId).Register(...)`（类型在 `.decompile/STS2RitsuLib.RunData*`）。

### 4.5 卡池归属（易错）
- 一张牌属于哪个池由**类上的 `[RegisterCard(typeof(...))]` 特性**决定，不是靠 `NewsanguoCardPool.CardTypes` 列表。
- `Scripts/Characters/ShuHanCardPool.cs:60-61` 的列表签名带 `[Obsolete("基类要求保留，请使用新的起始牌注册方式。")]`——基类仍要求它，所以共有牌惯例是"特性写曹魏池 + 蜀汉列表里再列一次"。
- 移出曹魏池的既有先例与注释写法：`Scripts/Cards/Invincible.cs:21-23`（同时把条目从 `NewsanguoCardPool.CardTypes` 删掉）。
- 2026-10-09 用同一手法再移出一张：「自刎归天！」`Scripts/Cards/FallOnOwnSword.cs:18-19` 改成 `[RegisterCard(typeof(ShuHanCardPool))]`，`Scripts/Characters/NewsanguoCardPool.cs:103` 的条目删掉并留日期注释；蜀汉列表 `Scripts/Characters/ShuHanCardPool.cs:103` 本来就有这一条，保留。

### 4.6 天意之力与"免费打出"
`Scripts/Combat/HeavensForce.cs`（`Scripts/Entry.cs:82` 在内容注册前 `HeavensForce.Register()` 注册为次级资源）。`HeavensForceVar` 在免费打出时 `PreviewValue = 0`；卡牌侧判免费的做法见 `Scripts/Cards/HeavenlyDeluge.cs:44,49`（`private int StunThreshold => IsFreeHeavensForceThisTurn ? 0 : DynamicVars["HeavensForcePower"].IntValue;`）。天意体系的牌惯例：`IsHeavensCard => true`、`CanonicalVars => [new HeavensForceVar(n)]`、`AdditionalHoverTips => [HeavensForce.HoverTip(), ...]`。

### 4.7 反编译索引（`.decompile/`，只读参考，不要改）
| 路径 | 内容 |
| --- | --- |
| `.decompile/sts2full/sts2.decompiled.cs` | 游戏本体合并反编译，508,815 行——**只按行号 grep** |
| `.decompile/live111/` | 游戏 0.111 按类型分文件（`CharacterModel.cs`、`CardModel.cs`、`NCardLibrary.cs` …），查单类型最省事 |
| `.decompile/STS2RitsuLib*/` | RitsuLib 各程序集按命名空间分目录（`Scaffolding.Characters*`、`Settings`、`Unlocks`、`RunData`、`Interop.AutoRegistration`、`Scaffolding.Content*` …） |
| `.decompile/loadout-src/`、`ritsulib*.cs`、`ritsulib-cardlib.cs` | 周边 mod / 参考实现 |

### 4.8 PCK 打包与 0.3.1「事件/篝火选牌故障」事故（2026-10-09 定位 + 修复）
- **症状**：玩家反馈「事件与篝火选牌」出故障，日志里是 `NCardGrid` 相关错误；加载顺序是「孙乾宇宙 → 新三国」（新三国最后挂载、最后覆盖）。
- **根因**：`tools/` 下的三个**本体场景副本**被 Godot 当成项目资源导入并导出（pck 里表现为 `tools/*.tscn.remap` 3 条 + `.godot/exported/133200997/export-*-{card_library,card_grid,simple_card_select}.scn` 3 条）。它们是当初用 `tools/pck_find.py` 从本体 `SlayTheSpire2.pck` 提取的参考文件（`tools/card_library.tscn` 的 SHA256 与 `.decompile/card_library.tscn` 一字不差），因此**自带本体 UID**：`uid://dbv1hvti5nub6`(card_library)、`uid://cso1enqajc0tc`(card_grid)、`uid://bw2l1wwri58ah`(simple_card_select)。mod pck 是最后挂载的包，同一 UID 于是出现第二份声明 ⇒ 本体按 UID 取 `card_grid` / `simple_card_select`（事件与篝火选牌用的正是这个屏）时被指到 mod 的副本 ⇒ NCardGrid 故障。时间线吻合：这些副本 10-06 才进仓，0.3.1（10-07 17:55 上传）之后才开始出现故障。
- **修复**（本次改动）：
  1. 新增 `tools/.gdignore`（空文件）⇒ Godot 整个忽略该目录，参考场景仍可人工阅读，但不再导入/导出/登记 UID。
  2. 删除 `.godot/exported/133200997/export-*{card_library,card_grid,simple_card_select}.scn` 陈旧产物。
  3. `export_presets.cfg:11`：`exclude_filter="*tools/*,*analysis/*,*.godot/uid_cache.bin,*.godot/global_script_class_cache.cfg"`（顺带把 `analysis/posthog/insights/*.json` 这类开发数据请出包外）。
- **验证方法**（改完必做）：导出到临时路径后 `python tools\pck_any.py <pck>`，`tools/` 与 `analysis/` 条目必须为 **0**。基线（2026-10-09 13:11 全量导出，91,905,844 B / **1106 条目**）：`newsanguo/images/*.png.import` 265、`.godot/imported/*.ctex` 266、`newsanguo/audios/*.mp3.import` 154、`localization/` 20、`Scripts/**/*.cs` 233、`scenes/` 2、`materials/` 1，另有 `project.binary`、`newsanguo.json`、`icon.svg`。对照修复前：91,950,304 B / 1117 条目（多出的 11 条＝3 个 tools remap + 3 个 tools 编译场景 + 15 个 analysis JSON − 期间新增的 Scripts）。**`Scripts/**/*.cs` 每条 size=1 是正常占位**（`dotnet/include_scripts_content=false` 生效，不泄露源码），不要去"修"。
- **`exclude_filter` 排不掉的两种文件**：`.godot/uid_cache.bin`(35,844 B) 与 `.godot/global_script_class_cache.cfg`(8 B) 是 Godot 在过滤**之后**强制写进包的（导出日志末段可见 `savepack | 保存文件：res://.godot/uid_cache.bin`）。它们**不是**本次触发源：`E:\games\杀戮尖塔2\pck-backup\` 里 10-05 的正常版本同样带 uid_cache（如 `newsanguo-20261005-125234.pck`：1024 条目、tools 0、analysis 15），而故障只从带 tools 副本的 0.3.1 起出现。当前包内的 uid_cache 只含 `res://newsanguo/**` 与 `res://Scripts/**` 自家路径（已核验：无 `tools/`、无本体 `res://scenes/...`）⇒ 保持现状。若日后要彻底剔除，需要 pck 后处理（`E:\games\杀戮尖塔2\GDRE_tools-v2.6.2-windows\gdre_tools.exe`），暂未采用。
- 导出命令（`README.md` §2 同）：`& 'E:\games\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe' --headless --path 'E:\games\杀戮尖塔2\newsanguo' --export-pack 'Windows Desktop' <输出.pck>`。
- 反查（确认仓里没有别的「本体 UID 副本」）：
  ```powershell
  Get-ChildItem E:\games\杀戮尖塔2\newsanguo -Recurse -File -Include *.tscn,*.tres |
    Where-Object { $_.FullName -notmatch '\\\.decompile\\|\\\.godot\\' } |
    ForEach-Object { "{0} :: {1}" -f $_.FullName, ((Get-Content $_.FullName -TotalCount 1) -replace '.*uid="([^"]+)".*','$1') }
  ```
  当前结果：`scenes/caowei_bg.tscn`、`scenes/shuhan_bg.tscn`、`materials/cards/frames/card_frame_newsanguo_mat.tres` 均**无 uid**；`tools/*` 三个本体 UID 已被 `.gdignore` 屏蔽。自有场景的 ext_resource 同时带 `uid=` 与 `path=` ⇒ 即使包里没有 `uid_cache.bin` 也能按 path 回落加载。

### 4.9 卡牌 id 的派生规则与改名代价（线程 6 审计，2026-10-09）
- **id 规则**：卡牌 `ModelId` = `NEWSANGUO_CARD_` + `ToUpperSnakeCase(类名)`。`ToUpperSnakeCase` 实现（`.decompile/loadout/Loadout.decompiled.cs:64134-64143`）：
  ```csharp
  string input = Regex.Replace(value, "([a-z0-9])([A-Z])", "$1_$2");
  input = Regex.Replace(input, "([A-Z]+)([A-Z][a-z])", "$1_$2");
  return input.ToUpperInvariant();
  ```
  ⇒ `WhereSWine` → `WHERE_S_WINE`（**不是** `WHERE_SWINE`）、`MaClanQuadBlast` → `MA_CLAN_QUAD_BLAST`。能力同理：`NEWSANGUO_POWER_<X>_POWER`（旧类名示例见本节末尾的改名记录）。
- **命名空间不参与 id**：`SOLDIER`（`Scripts/Cards/Soldier.cs`）的 id 与键完全不受命名空间影响（zhs/eng title 均为「士兵」/ "Soldier"）⇒ 统一命名空间是**零 id 风险**的改动；**只有改类名才换 id**。显示名（title）同样不参与 id。
  - **订正（2026-10-09）**：本节旧版写「`Scripts/Cards/Soldier.cs` 完全没有 `namespace`」——**这句是错的**。真相：该文件本来就有 `namespace newsanguo.Scripts;`（在 `:18`），只是 `namespace` 前面贴着一个游离的 U+FEFF（BOM 残留字符），导致 `^namespace` 正则扫描漏掉它。当时用这条错证据反推 id 规则属于侥幸——id 不含命名空间的正证是上一项 `id = NEWSANGUO_CARD_ + ToUpperSnakeCase(类名)` 本身。
- **RitsuLib 没有 model-id 别名 / 迁移设施**：`.decompile` 全量 grep `Alias|DeprecatedId|OldId|RenameModel` = **0 命中**；`Data.Migrations` 下只有 RitsuLib 自己的 settings 版本迁移。唯一可借的钩子是 `ModelDb.GetById<T>(ModelId)` 是静态泛型方法（可前缀补丁）——RitsuLib 自己在 `.decompile/STS2RitsuLib.Lifecycle.Patches/RunHistoryMissingModelDbGetByIdTranspilerPatch.cs:102-127` 就是这么认它的，因此自建「旧 id → 新 id」垫片**理论可行**，但要新写补丁 + 实机验证，不在改名本身的工作量内。
- **每张牌改名的同步清单**：`Scripts/Cards/<类名>.cs`（文件名 + 类名 + `[RegisterCard]`）→ 同名 `Scripts/Cards/<类名>.cs.uid` → `newsanguo/localization/{zhs,eng}/cards.json` 的 `<键>.title` / `<键>.description` → `newsanguo/images/cards/<类名>.png`(+`.png.import`) → 卡池列表 `Scripts/Characters/NewsanguoCardPool.cs:63-140` 与 `Scripts/Characters/ShuHanCardPool.cs:63-130` 的 `CardTypes` → 全仓 `HoverTipFactory.FromCard<T>()`（15 处，见 §4.10）。**若同时改能力类名**，还要加：`Scripts/Powers/<X>Power.cs`(+`.cs.uid`)、`newsanguo/localization/{zhs,eng}/powers.json` 的 `NEWSANGUO_POWER_<X>_POWER` 的 `.title`/`.description`/`.smartDescription` 三键、以及卡面描述里的 `{<X>Power:diff()}` 引用。已存在此耦合的有 WheresWinePower、CaoArtOfWarPower、AssignmentPower、RatPoisonSchemePower。
- **2026-10-09 第二批改名（8 张卡 + 2 个能力，用户 m00440 定的规则：类名 = 英文 title 的驼峰形式、英文尽量短）**：
  | 旧类名 | 新类名 | 新英文 title | zhs 中文 title |
  | --- | --- | --- | --- |
  | `DarkfinShark` | `DarkHornShark` | Dark Horn Shark | 乌角鲨（未动） |
  | `BladeOfVirtue` | `BladesOfVirtue` | Blades of Virtue | 仁之剑，义之剑（未动） |
  | `WindOfTiger` | `TigerWindCloudDragon` | Tiger Wind, Cloud Dragon | 风从虎，云从龙（未动） |
  | `BonelessPalm` | `BoneMeltingPalm` | Bone-Melting Palm | 化骨绵掌（未动） |
  | `CrossForCross` | `CrossTheRiverToo` | Cross the River Too! | 他过江我也过江！（未动） |
  | `CentralBastion` | `CentralPlainsPass` | Central Plains Pass | 中原雄关（未动） |
  | `Unstoppable` | `FireAndWaterProof` | Fire and Water Proof | 水火无敌（未动） |
  | `WineTheOldHero` | `WineIsTheOldHero` | Wine Is the Old Hero | 酒是老英雄（未动） |
  | `WindOfTigerPower` | `TigerWindCloudDragonPower` | 同名 | — |
  | `CentralBastionPower` | `CentralPlainsPassPower` | 同名 | — |
  - 同步落点：`Scripts/Entry.cs:92`（`RegisterArchaicToothTranscendenceMapping<BladesOfVirtue, TheTruestMask>(ModId);`）、卡池列表 `Scripts/Characters/NewsanguoCardPool.cs:71/76/77/87/114/116/117` 与 `Scripts/Characters/ShuHanCardPool.cs:83/86/95/96/98`、能力耦合 `Scripts/Cards/TigerWindCloudDragon.cs:41,53,54` ↔ `Scripts/Powers/TigerWindCloudDragonPower.cs`、`Scripts/Cards/CentralPlainsPass.cs:79` ↔ `Scripts/Powers/CentralPlainsPassPower.cs`；`Scripts/NewsanguoSfx.cs` 音效键 11 个（`blades_of_virtue_1/2` 是**两段语音**，`dark_horn_shark_copy` 是复制品的键）+ 素材 23 个（8 卡图 + 4 能力图含 Big + 11 音频）。**zhs 中文 title 全部未动**，只换 id/键与英文 title。
  - **代价**：这 10 个类换 id ⇒ **老存档牌组里这 8 张牌失效**（用户已接受，不做旧 id 垫片）。
  - **用户决定不改的超长英文**：`ChargeToZhugeLiangsCart`（"Charge Straight at Zhuge Liang's Cart!" 38 字符）、`TenThousandTransparentHoles`（31）、`FatherCanClaimTheThrone`（27）——保持原样。
  - **「黄金起义」`Scripts/Cards/GoldenRebellion.cs` 是用户刻意的起名，永久不要改**（m00440）。



### 4.10 `HoverTipFactory.FromCard<T>()` 全量引用（改名/删牌必须同步）
全 15 处（2026-10-09 改名后实测）：`Scripts/Cards/ChargeToZhugeLiangsCart.cs:33`(FourWheeledCart)、`Scripts/Cards/CommanderArrives.cs:40`(MilitaryCudgel)、`Scripts/Cards/HeavenlyTroops.cs:34`(Soldier)、`Scripts/Cards/HumanTransmutationSpell.cs:43`(Soldier)、`Scripts/Cards/ImGettingDrunk.cs:35`(Lightweight)、`Scripts/Cards/MyThreeGenerals.cs:35,36,37`(HanXin,BaiQi,ZhouYafu)、`Scripts/Cards/OldCompact.cs:47`(OldFuel)、`Scripts/Cards/OldDirge.cs:44`(**Soul**)、`Scripts/Cards/RatPoisonScheme.cs:28`(VenomRat)、`Scripts/Cards/SovereignForm.cs:48`(HailKingOfHanzhong)、`Scripts/Cards/WhatToEat.cs:37`(ChowDown)、`Scripts/Cards/WindOfTiger.cs:37,38`(SmilingTiger,DragonOmen)。
- 订正：旧版把 `Scripts/Cards/OldDirge.cs` 那处写成 `(Soldier)`，实际是 `(Soul)`；行号因 §4.11 的 `using` 增删整体上移 1 行。

### 4.11 `Scripts/Cards` 命名空间（2026-10-09 已统一为 `newsanguo.Scripts.Cards`）
132 个 `.cs` **全部**是 `namespace newsanguo.Scripts.Cards;`（改动前：125 个 `newsanguo.Scripts` + 6 个 `newsanguo.Scripts.Cards`（`HeavensForceVar.cs`、`NewsanguoCardTemplate.cs`、`NewsanguoCurseTemplate.cs`、`ProxyStrikeIncreaseVar.cs`、`ScryKeyword.cs`、`TaxGoldVar.cs`）+ 1 个 `Soldier.cs`（见 §4.9 订正））。其余目录一律跟目录名（`Powers`→`newsanguo.Scripts.Powers` 44、`Characters`→`newsanguo.Scripts.Characters` 6、`Patches`→`newsanguo.Scripts.Patches` 17、`Relics` 10、`Events` 5 …）。配套改动：
- 126 个被改命名空间的卡文件里原有的 `using newsanguo.Scripts.Cards;`（它们要用那 6 个辅助类）变成自引用冗余，已删除。
- 17 个 `newsanguo.Scripts.*` 外部文件补了 `using newsanguo.Scripts.Cards;`：`Scripts/Characters/NewsanguoCardPool.cs:12`、`Scripts/Characters/ShuHanCardPool.cs:12`、`Scripts/Entry.cs:8`、`Scripts/Events/ChenliuGrandMessHall.cs:8`、`Scripts/Patches/AlwaysMineDiscardGlowPatch.cs:8`、`Scripts/Powers/ChargeToZhugeLiangsCartPower.cs:8`、`Scripts/Powers/DrunkenMightPower.cs:8`、`Scripts/Powers/HeavenlyTroopsPlusPower.cs:17`、`Scripts/Powers/HeavenlyTroopsPower.cs:17`、`Scripts/Powers/HeavensForcePower.cs:15`、`Scripts/Powers/NearAndFarDexterityPower.cs:15`、`Scripts/Powers/NearAndFarStrengthPower.cs:15`、`Scripts/Powers/RatPoisonSchemePower.cs:12`、`Scripts/Powers/StarryNightDexterityPower.cs:12`、`Scripts/Powers/WindOfTigerPower.cs:11`、`Scripts/Relics/SonOfHeavenEconomics.cs:14`、`Scripts/Rewards/CreativeModeReward.cs:16`。
- `Scripts/Powers/SovereignFormPower.cs:16` 的 XML doc `<see cref="newsanguo.Scripts.SovereignForm"/>` 已改成 `newsanguo.Scripts.Cards.SovereignForm`（全仓**唯一**一处命名空间字面量引用；另 4 处 `<see cref>` 指向 `newsanguo.Scripts.Powers` / `newsanguo.Scripts.Combat`，不受影响）。
- **机制**：卡文件里 `NewsanguoSfx` 等根命名空间类型靠**外层命名空间查找**仍然可见 ⇒ 卡文件**不需要**补 `using newsanguo.Scripts;`；漏 using 的编译错误只会出现在同级命名空间（上述 17 个文件）。命名空间改动**不动任何 id**（§4.9）。

### 4.12 卡牌数量的口径
`Scripts/Cards/*.cs` = **132**（含 129 个普通卡 + 模板/变量/关键字等辅助类），带 `[RegisterCard]` 的 = **127**（其中 `Scripts/Cards/NewsanguoCurseTemplate.cs` 是模板基类，其 `:9` 注释本身也含 `[RegisterCard(typeof(CurseCardPool))]` 字样故被 grep 命中）。真正有本地化 `title` 的卡 = **126**；`Scripts/Cards/*.cs.uid` = 134；`newsanguo/images/cards` = 120 png + 120 png.import；`newsanguo/audios` = 154 mp3。`cards.json` 266 键 = `title` 126 + `description` 126 + 无后缀 9 + `selectionScreenPrompt` 5；126 张卡的 title 键在 zhs/eng **零缺失、零孤儿**，驼峰→UPPER_SNAKE 规则与现有键 100% 一致。
- 4 张基础牌是唯一的「卡图不走类名」例外：`Scripts/Cards/StrikeCaowei.cs:27` 与 `Scripts/Cards/StrikeShuhan.cs:25` 都写死 `"res://newsanguo/images/cards/StrikeNewsanguo.png"`；`Scripts/Cards/DefendCaowei.cs:31` 与 `Scripts/Cards/DefendShuhan.cs:31` 都写死 `"res://newsanguo/images/cards/DefendNewsanguo.png"`（**两个文件都在用，不是孤儿 PNG**）。其余全部是 `$"res://newsanguo/images/cards/{GetType().Name}.png"`。
- 4 张基础牌的音效键也是刻意共享的：`strike_newsanguo`（`Scripts/NewsanguoSfx.cs:205` 表项、`Scripts/Cards/StrikeCaowei.cs:48` 与 `Scripts/Cards/StrikeShuhan.cs:46` 播放）、`defend_newsanguo`（`:120` 表项、`Scripts/Cards/DefendCaowei.cs:50` 与 `Scripts/Cards/DefendShuhan.cs:50` 播放）、`:349` 静音白名单 `["strike_newsanguo", "defend_newsanguo", "soldier", "soul_shackles"]`。（2026-10-09 第二批改名后 `LoudnessGainDb` 表按 ordinal 整体重排：150 项、文件 853 行 ⇒ 三个行号分别前移 4/2/1。）
- `Old*` 六张（`OldBorrowedTime`/`OldCompact`/`OldDirge`/`OldExpectAFight`/`OldForgottenRitual`/`OldFuel`，全在 `TokenCardPool`）**不是备份牌**，是刻意保留的旧版衍生牌（保留原版角色卡框，理由见 `Scripts/Cards/OldBorrowedTime.cs:21,26`），不属命名事故。

### 4.13 素材 / Godot 导入缓存改名规程（线程 6 实操，2026-10-09）
- **导入缓存文件名 = `<源文件名>-<md5("res://<资源完整 res 路径>") 的小写 hex>`**（与文件内容无关；4 例实测全部吻合，例：`res://newsanguo/images/cards/Empower.png` → `15694fa0c049143c521ffb9912e5d88a`、`res://newsanguo/images/cards/WhereSWine.png` → `b1977f0b3e5382221bd1bd3a4c014ad7`）。PNG 在 `.godot/imported/` 下是 `<...>.ctex` + `<...>.md5` 两个文件，MP3 是 `<...>.mp3str` + `<...>.md5`。
- 因此**改素材文件名可以完全离线完成、不必开 Godot 重导**：① `Move-Item` 改源文件名；② 按公式算旧/新 token，`Move-Item` 重命名 `.godot/imported/` 里那两个文件；③ 改同名 `.import` 里的 `path=` / `dest_files=`（缓存 token）与 `[deps] source_file=`（`res://` 路径），**`uid=` 保持不变**（uid 是资产身份，改它才会出问题）。
- `.godot/imported/` 里还有大量**陈旧条目**（小写名、`<X>_big.png`、已删素材如 `RatPoisonPlusPower*`、早期管线的 `*.wav-*.sample`）——**不要动**，Godot 会自己清理；判断"陈旧"的办法是看有没有 `.import` 引用它。
- **`.godot/uid_cache.bin` 必须同步改**（`export_presets.cfg:11` 的 `exclude_filter` 排不掉它，Godot 在过滤**之后**强制写进 pck，见 §4.8）：格式 = `int32 count` + 每项（`int64 uid` + `int32 len` + `len` 字节 UTF-8 的 `res://` 路径），末尾无 padding。改路径时 **uid 全部保持不变**——线程 6 改了 52 条路径（11 卡 `.cs` + 11 卡图 + 8 能力图 + 4 能力 `.cs` + 16 mp3 + 2 补丁 `.cs`），文件 35,844 B → 35,984 B，`count` 仍 639、`consumed == bytes`。`global_script_class_cache.cfg`（8 B）不含路径，无需处理。**注意**：手工改只是为了在用户重导前保持仓库自洽；素材改名后**必须重导 pck**，让 Godot 自己刷新这些缓存。
- **Windows 大小写不敏感**（§2.10）：新旧名只差大小写时（`WhereSWine` → `WheresWine`），**不要**写「写新文件 → 删旧文件」——那两条路径指向同一个文件，等于删掉刚写的（线程 6 因此丢了 3 个 `.import`，只能从备份重做）。`.cs` / `.cs.uid` 走 `Move-Item` 就没有这个问题。
- **改名后的完整性检查清单**：每个改名资源的 `.import` 里 `dest_files` 指向的 `.godot/imported/*` 必须存在、`source_file` 指向的源文件必须存在、`uid=` 与改名前**逐字节一致**。线程 6 全量核验结果：`newsanguo/` 下 419 个 `.import`，problems = 0；35 个改名资源 uid 差异 0 条。
- 二次实例（2026-10-09，卡牌「时空酸雨」→「时光酸雨」+ 类名 `SpaceTimeAcidRain` → `TimeAcidRain`）：只改了 `.cs`/`.cs.uid`/本地化键/音效键（这张牌的卡图与音频本来就在「待补素材」里，没有 `.import` 要动），`uid_cache.bin` 按同法把 `res://Scripts/Cards/SpaceTimeAcidRain.cs` 改成 `res://Scripts/Cards/TimeAcidRain.cs`：36,327 B → 36,322 B（路径短 5 字节），`count` 仍 646、`consumed == size`、uid `2323964352520271695` 不变、旧名残留 0 条。

### 4.14 能力钩子 `ShouldReceiveCombatHooks` 的真实语义（2026-10-09 线程 1 查证）
- `PowerModel` **自身**就写了 `public override bool ShouldReceiveCombatHooks => true;`（`.decompile/sts2full/sts2.decompiled.cs:79591`，`PowerModel` 类体从 `:79190` 起）⇒ 能力子类里再写一次是**冗余**；`AbstractModel.ShouldReceiveCombatHooks` 是抽象属性（`.decompile/live111/AbstractModel.cs:68`、`sts2.decompiled.cs:69320`）。
- 游戏本体里**没有任何消费点**会读它：能力钩子派发走 `CombatState.IterateHookListeners()`（`:202343-202426`）/`Hook.IterateCombatHookListeners`（`:164480-164490`），只判模型是否还在场、不筛该属性。全 dump 里唯一真消费者是 RitsuLib 的 `.decompile/STS2RitsuLib.Combat.SecondaryResources/SecondaryResourceHook.cs:192,205`（筛次级资源钩子对象）。
- 因此**只有被动修正类能力**（如只覆写 `ModifyMaxEnergy`）可以不写这个属性；带回合钩子（`AfterPlayerTurnStart` 等）的能力仓内惯例仍显式写 `=> true`（`Scripts/Powers` 44 个文件里 37 个写 true、`Scripts/Powers/DragonOmenPower.cs:20` 写 false、5 个不写）。
- `ModifyMaxEnergy(Player, decimal)` 是 `AbstractModel` 虚方法（`:71023`），派发点 `Hook.ModifyMaxEnergy`（`:166206-166214`）= 逐个 listener 串行调用、**不筛** `ShouldReceiveCombatHooks`；消费端 `PlayerCombatState.MaxEnergy => (int)Hook.ModifyMaxEnergy(...)`（`:177379`）。实例：`Scripts/Powers/MegalovaniaPower.cs:46-53`、`Scripts/Powers/FatherCanClaimTheThronePower.cs`。
- 「能力每回合闪烁 + 播声」的写法与移除：`Flash()` 是 `PowerModel` 的方法（`:80256`，批量版 `:80261`）；要静音/静默就**删掉整个回合开始钩子**（`Scripts/Powers/MegalovaniaPower.cs` 2026-10-09 即如此，旧 `AfterPlayerTurnStart` 里是 `Flash(); NewsanguoSfx.Play("event:/newsanguo/sfx/megalovania_power");`），并顺手删掉不再使用的 `using`（同款写法仍在 `Scripts/Powers/FatherCanClaimTheThronePower.cs:58-67`，未改）。

---

## 5. 本地化

- 目录：`newsanguo/localization/{zhs,eng}/`，各 10 个文件：`ancients.json`（先古对话，~21KB，最大）、`cards.json`（~30KB，266 键）、`events.json`、`powers.json`、`relics.json`、`characters.json`、`monsters.json`、`potions.json`、`card_keywords.json`、`static_hover_tips.json`。
- 键规则：`NEWSANGUO_<类型>_<类名 UPPER_SNAKE>`（驼峰拆词，`CaoArtOfWar` → `CAO_ART_OF_WAR`；改名后键都按新类名派生，见 §4.9）。玩法文案里常见 SmartFormat：`{Damage:diff()}`、`{HeavensForcePower:inverseDiff()}`、`[gold]…[/gold]`。
- **怪物图鉴的招式名是另一套键**：`<怪物 id>.moves.<招式状态名去掉结尾 _MOVE>.title`（引擎 `MonsterModel.GetBestiaryMoveName`，`.decompile/sts2full/sts2.decompiled.cs:78482-78485`；调用它的 `GenerateBestiaryMoveList` 在 `:78308-78351`：先剥 `_MOVE` 再查键，查不到会 `Log.Warn("No loc for move …")` 并回落到状态名）。**2026-10-09 修正**：`newsanguo/localization/{zhs,eng}/monsters.json` 里两个怪物此前写成 `.moves.HEADBUTT_MOVE.title` / `.moves.DIZZY_MOVE.title` / `.moves.NOTHING_MOVE.title`，是永远命不中的死键（状态名带 `_MOVE`、键名不带），已改为 `.moves.HEADBUTT.title` / `.moves.DIZZY.title` / `.moves.NOTHING.title`，并补上盛碗虫（巨石）新增的 `.moves.BUFF.title`。
- 中英两份必须同步改；改完 `ConvertFrom-Json` 校验 + 键数对比 + 检查无 BOM、无残留 `\\n`。
- 本地化在 pck 内 ⇒ 改完**必须由用户重导 pck**。

---

## 6. 素材

- 卡图：`newsanguo/images/cards/<类名>.png`（来自 `AssetProfile` 的 `PortraitPath`，一律用类名）。
- 角色相关：`newsanguo/images/characters/<角色>/…`（选人图标、锁定图标、战斗形象、死亡形象、商店/休息处形象、地图标记）。
- 能力/遗物图：`res://newsanguo/images/powers|relics/<名字>.png`（大图惯例 `<名字>Big.png`）。
- 音频：`newsanguo/audios/<snake_name>.mp3`。
- 加/换素材后要提醒用户重导 pck；`.godot/**/*.import` 由 Godot 侧生成。
- **待补素材清单**（代码已引用、文件还没进包）：只剩 `ChargeToZhugeLiangsCartPower.png`(+`Big`) 两张能力图（`Scripts/Powers/ChargeToZhugeLiangsCartPower.cs:48-50`）。4 张卡图（`HeavenlyDeluge`/`TimeAcidRain`/`ChargeToZhugeLiangsCart`/`FourWheeledCart`）与对应 4 个 mp3 已于 2026-10-09 补齐并完成 Godot 导入。音频响度均衡表 2026-10-09 全量复核（口径见 `Scripts/NewsanguoSfx.cs:52-70`，表内 154 条 / 音频 158 个）。`megalovania_power.mp3` 不在清单内——「狂妄之人」能力不再播放音效（见 §8.2）。

---

## 7. 数据分析（PostHog）

- 环境：项目 `635605`、dashboard `2153145`、insight `12370626`（`2Ry2k6SI`，选取率排行）。
- 工具：`tools/posthog-call.ps1`（`insight-create` / `insight-update` / `insight-query`）、`tools/posthog-sql.ps1`（`execute-sql`）、`tools/posthog-report.ps1`。
- 每次调用前需要：`$env:POSTHOG_HOME = "$env:TEMP\newsanguo-posthog"`，并把仓库 `.posthog-home/credentials.json` 复制进去。
- SQL 分析脚本与产出都在 `analysis/posthog/`（`01…26_*.sql`、`out/*.tsv`、`insights/*.json`、`report.md`、`out/pick_rate_chart.html`）。
- **已知限制（别再试）**：insight 校验器拒绝 `transform(...)`+数组字面量、`CASE WHEN`、`multiIf`（报 `expected identifier in column-alias list, got String`）、外层再包一层 SELECT（`expected ), got Keyword(From)`）、`WITH names AS (...) LEFT JOIN names`（执行期 unknown error）；`substring` 必须 3 参。**中文标签做不出来**，x 轴只能用去前缀短名（`substring(JSONExtractString(choice,'card','id'), 21, 200) AS card_label`），中文牌名只放 `report.md` 与本地 HTML。

---

## 8. 当前待办

### 8.1 待用户拍板
1. **卡牌类名改名 —— 已执行（2026-10-09，线程 6）**：用户选定 **(c) 全面统一命名** + 命名空间方案 **(ii)**，音效键/音频文件一起改，**不做**旧 id 垫片。17 个类的新名映射、命名空间统一与素材/缓存同步规程见 §4.9 / §4.11 / §4.13；**老存档牌组里这 17 项会失效**（用户已知并接受）。下面 a–d 是当时的取舍清单与代价分析，**保留仅作历史记录**：
   - **(a) 只改显示名**：零 id 风险，只动 `cards.json` 的 `.title`。但审计结论是**中文 title 本身没问题**（每张牌的中文名都是正经的设计文案），所以此项实际只对"想改英文 title"有意义。
   - **(b) 只修拼写/撇号事故**（**会换 id**）：`WhereSWine`（→ 建议 `WheresWine`；现 id `NEWSANGUO_CARD_WHERE_S_WINE`，eng title "Where's Wine"）、`CaosArtOfWar`（→ `CaoArtOfWar`；现 id `NEWSANGUO_CARD_CAOS_ART_OF_WAR`，zhs「曹氏兵法」/ eng "Cao's Art of War"）。两项各带一个能力类（`WhereSWinePower`、`CaosArtOfWarPower`），若连能力一起改则同步成本翻倍（§4.9）。
   - **(b+) 再加"类名 ↔ 英文标题不符"2 项**：`HeavensForceAccept`（eng "Follow Heaven's Will"）、`HeavensForceDecline`（eng "You Dare Refuse?!"）——这两个类名与英文标题完全对不上。
   - **(c) 全面统一**：在 (b+) 基础上再处理近名对 `PoisonRat`（毒鼠，`TokenCardPool`）/ `RatPoison`（毒鼠计，`NewsanguoCardPool`）与语义偏离项 `SkywardBlade`（恨天剑法）、`Empower`（赋值）、`QuadBlast`（马氏四连）、`SelfFall`（自刎归天！）、`GetOut`（叉出去！）。
   - **不属事故、不要动**：`AGrandToast`、`ImGettingDrunk`、`ICantLeave`、`Lightweight`、`WhyPickThatUp`、`MarshalsTerraceFeast`、`SlamTheBowl`、`Onset` 等（中英对照后均可接受）；六张 `Old*` 是刻意保留的旧版衍生牌（§4.12）。4 张基础牌 `StrikeCaowei`/`StrikeShuhan`/`DefendCaowei`/`DefendShuhan` 与「英文词+角色名」之外的惯例不一致，但改它们要连带 `StrikeNewsanguo.png`/`DefendNewsanguo.png` 与共享音效键 `strike_newsanguo`/`defend_newsanguo`（§4.12），收益低。
   - **老存档影响**：RitsuLib **无 id 别名设施**（§4.9）⇒ 选定 (b) 及以上后，老存档牌组里这些牌会失效。若在意，需先单独做「旧 id → 新 id」垫片（`ModelDb.GetById<T>` 前缀补丁 + 实机验证），那是另一件事、要另开线程。
   - **(d) 完全不改名**：只做零 id 风险的规范重构（§4.11 命名空间统一）＋文档订正。
   - **第二批改名（同日，用户 m00440 定的"类名 = 英文 title、英文尽量短"规则）已执行**：8 张卡 + 2 个能力（映射与代价见 §4.9 末表）⇒ **老存档牌组里这 8 张牌会失效**（用户已接受，不做旧 id 垫片）；「黄金起义」`GoldenRebellion` 按用户要求永久不动。
2. LexNinja2「挑战模式」是否移植到新三国（若要，需先定：改哪些牌/系统、持久化方式、是否做脏数据标记、是否要 `{IfChallengeMode:...}` 卡面文案切换）。
3. 两个角色的先古对话是否按口吻拆分改写。
4. 建筑师三段攻击的攻击方是否改成 `Player` + `Both`。
5. 「恭喜爹可以称帝了」能力文案是否改图标。
6. 卡牌总览（图鉴）里的蜀汉卡池筛选项：测试模式关闭时是否也隐藏（当前选择**保持可见**，方便查卡）。
7. 「直奔诸葛亮四轮车！」同一回合重复打出时，能力 Amount 会叠成 2、3…，当前实现取「每次击杀获得 `kills * Amount` 张四轮车」（`Scripts/Powers/ChargeToZhugeLiangsCartPower.cs:114`）＝重复打出会翻倍。是否改成「每击杀固定 1 张、不随叠加翻倍」？

### 8.2 待实机验证
- 「测试模式」：关闭时选人界面无蜀汉且「随机角色」按钮正常；开启后蜀汉可选；切换后重进选人界面即生效。
- 「时光酸雨」（2026-10-09 由「时空酸雨」改名，**类名与 id 一起换**：`Scripts/Cards/SpaceTimeAcidRain.cs` → `Scripts/Cards/TimeAcidRain.cs`、id `NEWSANGUO_CARD_SPACE_TIME_ACID_RAIN` → `NEWSANGUO_CARD_TIME_ACID_RAIN`；2 费罕见技能（`Scripts/Cards/TimeAcidRain.cs:53`，本节旧版误写「1 费」）/ 失去 3/2 天意之力 / **去除敌方全部格挡** + 清敌方全部正面效果 / 消耗）：去格挡照原版 Expose 的写法 `await CreatureCmd.LoseBlock(choiceContext, enemy, enemy.Block, base.Owner.Creature)`（`.decompile/sts2full/sts2.decompiled.cs:139499`），只对存活敌人执行；`LoseBlock`（`:197298-197310`）内部已判 `amount<=0`/目标已死/战斗收尾，破防时自动放 `event:/sfx/block_break` 并触发 `Hook.AfterBlockBroken`。待实机确认：卡面与图鉴显示「时光酸雨」；**id 已换** ⇒ 老存档牌组里旧 id 的这张牌会失效（与线程 6 的 17 项同类代价），新开局的奖励/商店/图鉴能正常出现。清正面效果时**排除「流沙」`SandpitPower`**（2026-10-09 用户反馈：清掉它玩家会当场死亡，机制与全部出处见 §4.1）：筛选条件在 `Scripts/Cards/TimeAcidRain.cs:91`（`p.TypeForCurrentAmount == PowerType.Buff && p is not SandpitPower`）。待实机确认补充：对「贪得无厌」打出这张牌不再自杀，其他正面效果（力量/灵巧等）仍被清除。
- 「直奔诸葛亮四轮车！」+ 衍生牌「四轮车」（`Scripts/Cards/ChargeToZhugeLiangsCart.cs`、`Scripts/Cards/FourWheeledCart.cs`、`Scripts/Powers/ChargeToZhugeLiangsCartPower.cs`）：抽牌堆+弃牌堆攻击牌进手（含中途抽到的攻击牌 0 费）、攻击牌击杀给四轮车、回合末能力自动消失、升级 3→2、**升级前后都带[消耗]**（`Scripts/Cards/ChargeToZhugeLiangsCart.cs:32`）。·连带：手牌溢出上限取舍、AoE 多杀多给、击杀效果与战斗收尾的时序。
- 「四轮车」升级 = 每次格挡 4→5（次数仍 4 次，`Scripts/Cards/FourWheeledCart.cs:63` 的 `DynamicVars.Block.UpgradeValueBy(1m)`；注意衍生牌通常只在战斗内存在，要靠战斗内升级效果才能看到，卡面 `{Block:diff()}` 会自动显示 5 并高亮）。衍生牌自身**升级前后都带[消耗]**（2026-10-09 追加，`Scripts/Cards/FourWheeledCart.cs:31` 的 `CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords]`）⇒ 生成的每张四轮车打出后进消耗堆，不会再被抽回。
- 「一日好过一日」升级 = 下回合抽 X+2 / 得 X+2 能量、且**保留[消耗]**（`Scripts/Cards/BetterEachDay.cs:52` 的 `int bonus = IsUpgraded ? 2 : 1;`，关键词常驻不再被 OnUpgrade 移除，文案 `{IfUpgraded:show:X+2|X+1}`）。
- 卡池归属改动（2026-10-09）：曹魏（新三国）卡池的奖励/商店/图鉴里不再出现「自刎归天！」，蜀汉卡池仍可选出；老存档牌组里的这张牌不受影响（只改池归属、没改 id）。
- 「狂妄之人」易伤时序修复；「托管」/复制品提示；卡面第二行「参见汉中王！」触发时机。
- 「狂妄之人」能力不再有每回合表现（2026-10-09 用户需求 m00555）：`Scripts/Powers/MegalovaniaPower.cs` 删除了 `AfterPlayerTurnStart` 钩子（原 `Flash()` + `NewsanguoSfx.Play("event:/newsanguo/sfx/megalovania_power")`），并删掉随之冗余的 `ShouldReceiveCombatHooks`（`PowerModel` 默认即 `true`，见 `.decompile/sts2full/sts2.decompiled.cs:79591`）；能量加成完全靠 `ModifyMaxEnergy`（`:71023` 虚方法，经 `Hook.ModifyMaxEnergy` `:166206-166214` 的 `IterateCombatHookListeners` 派发，**不过滤** `ShouldReceiveCombatHooks`）。音效键 `megalovania_power` 已从 `Scripts/NewsanguoSfx.cs` 表内删除（该音频从未补齐，现也永不播放）。待实机确认：打出「狂妄之人」后每回合能量上限仍按层数 +1，且回合开始**不再**出现图标闪烁与音效。
- 青州兵承伤优先级（2026-10-09 线程 2 改，`Scripts/Patches/CaoArtOfWarSoldierDamageCascadePatch.cs`）：敌人「强化攻击」打玩家时的顺序改为 **青州兵（按召唤顺序依次穿透）→ 玩家格挡 → 奥斯提 → 玩家**（原版是「玩家格挡 → 奥斯提 → 玩家」，且多只召唤物同时在场时一次攻击只有 1 只接刀、溢出直接落回玩家）。实现：链头只结算一次 `Hook.ModifyDamage` + `Hook.BeforeDamageReceived`（`DistributeDamage`，`:110-170`），① 手工按顺序打青州兵（带 `Unblockable`，格挡属于玩家、排在青州兵之后），②③④ 把余量伤害交回原版 `CreatureCmd.Damage`（目标仍是玩家、**不带** Unblockable）⇒ 格挡扣减 / 奥斯提改道 / 奥斯提溢出回到玩家 / 死亡结算全部复用引擎。嵌套调用靠 `SoldierDamageCascade.SkipNestedHooks`（`:16-31`）跳过 `Hook.ModifyDamage` 与 `Hook.BeforeDamageReceived` 两个钩子（`:179-207`，否则易伤/虚弱会逐只重复计算、原版「荆棘」会反击两次）。待实机确认：多只青州兵依次阵亡与溢出转交；**格挡充足时青州兵也会先挨打阵亡**（这是需求指定的顺序，若想改成“先扣格挡再青州兵”需回报）；青州兵全灭后奥斯提仍能按原版接刀；「荆棘」只反击一次；玩家被这一击打死 / 战斗收尾时无异常。
- 青州兵战斗表现：站位 + 悬浮名牌（2026-10-09 用户 m01230「召唤的位置很诡异，鼠标悬浮上去也无法看到它的名字」，线程 2；新补丁 `Scripts/Patches/CaoArtOfWarSoldierFormationPatch.cs`，接线 `Scripts/Entry.cs:60-63`）。根因 = 引擎只给**原版奥斯提**留了专用分支：`NCombatRoom.AddCreature`（`.decompile/sts2full/sts2.decompiled.cs:406994-407040`）里 `if (creature.Monster is Osty && LocalContext.IsMe(player))`（`:407023-407027`）把奥斯提摆到 `NCreature.GetOstyOffsetFromPlayer`（`:477307-477311` = 主人判定框半宽 + `Osty.MinOffset/MaxOffset` 抬高 75 像素）并插到主人索引处（画在主人后面）；**其余宠物**走通用行（`:407029-407035`）`x = 主人x − 20 + i × (主人判定框宽/(只数−1)) + 自身判定框宽/2`、`y = 主人y + 10`，并逐只 `ToggleIsInteractable(false)`（`:477065-477071` ⇒ `Hitbox.MouseFilter = Ignore` + `_stateDisplay.Visible = false` ⇒ `NCreature.OnFocus`（`:476860-476885`）永不触发 ⇒ 既不 `ShowNameplate()`（名牌）也不显示 `Creature.HoverTips`，血条容器一起被隐藏；原版非死灵缚者的宠物本来就 `IsHealthBarVisible => false`（`:111503`/`:117306`），所以这一手对它们无害，青州兵 `Scripts/Monsters/QingzhouSoldier.cs:57` 想要血条就一起被阉）。站位公式按**主人**宽度分摊间距，而青州兵借用 `osty.tscn` 的判定框 232×204、曹操/刘备立绘只有 265×209 ⇒ 第 1 只正好压在主人右半身、第 3 只起互相重叠成一坨。修法：postfix `AddCreature` + postfix `PositionPlayersAndPets`（`:406830-406947`，只在战斗建立/读档时跑一次、调用点 `:406744`，不补这一处则读档后会被打回原形）⇒ ①逐只 `ToggleIsInteractable(true)`；②位置 = `主人节点.Position + NCreature.GetOstyOffsetFromPlayer(青州兵)`（**照抄原版奥斯提锚点**，只有一只时与「1 生命值奥斯提」完全同位；原版 `OstyScaleToSize` 的按血量缩放对 1 生命值是空操作 `Lerp(1,2,1/150)≈1.007`，故不改缩放），多只时照抄创意工坊 mod「召唤独立 / SummonOstys」（workshop id 3768978847，反编译 `OstyPositioning.Reposition`）的**斜向阶梯**：每只再偏 `(30, −23)`（向右上），第 15 只起循环复用槽位，越早召唤的越贴近主人——靠往上叠腾位置，既不会挤进敌人区，也不会像引擎通用行那样在数量多时把间距压缩到重叠。待实机确认：悬浮青州兵能看到「青州兵（迫真）」名牌 + 血条 + 「替死」提示；1 只时站位与原版奥斯提一致、多只沿斜向阶梯向右上排开、不压主人（顺带消除「悬浮主人右半身显示青州兵名牌」的冲突）；阵亡后幸存者**不回填空位**（只在召唤/读档时重排，暂不跟随死亡重排）；`OffsetX = 30f`、`OffsetY = −23f`、`MaxUniquePositions = 15` 三个常量若观感不对可直接调（值照抄那个 mod）；**注意**该 mod 为此把整个 `NCreature.OstyScaleToSize` 重写了——原版 `:477297-477300` 会在本地玩家身上把 `position` 补间回 `GetOstyOffsetFromPlayer`，将来给青州兵加缩放必须避开这一句。**纯代码改动 ⇒ 无需重导 pck**。
- 青州兵关键词「募集」（2026-10-09 用户需求 m00151，同日 m00512 修订、m00717 合并：「募集X」是**悬停提示的标题**且 X 显示卡面数值；说明里含“在你的回合开始时，失去所有募集的青州兵。”——m00512 曾把它拆成「临时募集」变体，m00717 用户要求合并回单一「募集」）。机制不变：每打出一张牌以 1 生命值生成 {层数} 名 1 生命值青州兵、每次都是独立个体。
  - **不再用 RitsuLib 模组关键词**（第一版曾用 `[RegisterOwnedCardKeyword("recruit")]`，已废弃）。原因已查证：`ModKeywordDefinition`（`.decompile/STS2RitsuLib.Keywords/ModKeywordDefinition.cs`）只有固定 title/description 键、没有 DynamicVar 参数化设施，且 `HoverTipFactoryFromKeywordPatch.cs:42-51` 创建提示时拿不到卡牌上下文 ⇒ 标题里的数量永远是静态文案，做不到「募集1」。
  - 现实现：`Scripts/Cards/RecruitKeyword.cs` 是静态工厂（`RecruitKeyword.Recruit(DynamicVar|decimal)`），照抄原版 `HoverTipFactory.Static` 的路子（`.decompile/sts2full/sts2.decompiled.cs:164298-164309`）：`new LocString("static_hover_tips", <键>)` → `LocString.Add(amount)`（`:161125-161128`）→ `new HoverTip(title, description)`。`HoverTip` 的构造函数**当场求值**（`:164059-164070`）⇒ 变量必须在构造前塞入；`DynamicVar.ToString()` 返回 `IntValue`（`:163284-163287`）⇒ 文案里写 `{Summon}` 即渲染成整数；`{Summon}` 这个变量名沿用原版 `SummonVar`（`:163786-163807`）。
  - 文案表是原版已有的 `static_hover_tips`（模组的 localization 只能合并进原版已有的表，见 `Scripts/Combat/HeavensForce.cs:37-39`）：`newsanguo/localization/{zhs,eng}/static_hover_tips.json` 新增 2 键 `NEWSANGUO_KEYWORD_RECRUIT.title`＝「募集{Summon}」/「Recruit {Summon}」、`.description`＝“生成{Summon}名1生命值的青州兵。在玩家受到攻击时，青州兵受伤会优先于玩家扣除格挡。在你的回合开始时，失去所有募集的青州兵。”/“Generate {Summon} Qingzhou Soldiers with 1 HP each. When you are attacked, Qingzhou Soldiers take damage before your [gold]Block[/gold] is consumed. At the start of your turn, lose all recruited Qingzhou Soldiers.”。原 `card_keywords.json` 的 2 个 RECRUIT 键**已删除**（该表只剩 Scry）⇒ 键数 4→2，`static_hover_tips.json` 3→5；曾有一版的 `NEWSANGUO_KEYWORD_TEMPORARY_RECRUIT.title` / `.extraDescription` 两键（键数到过 7），m00717 合并后已删除。
  - 接线：`Scripts/Cards/NewsanguoCardTemplate.cs` 删掉 `RecruitKeywordId` 常量、`IsRecruitCard` 虚属性与 `CanonicalKeywords` 里的 yield（Scry 机制原样保留）；`Scripts/Cards/CaoArtOfWar.cs:39-49` 的 `AdditionalHoverTips` = `[RecruitKeyword.Recruit(base.DynamicVars.Summon), HoverTipFactory.FromPower<StrengthPower>()]`（本卡 `Summon = 1` ⇒ 标题显示“募集1”）；`Scripts/Powers/CaoArtOfWarPower.cs:73-77` 加 `AdditionalHoverTips => [RecruitKeyword.Recruit(Amount)]`（数量取能力当前层数）。卡面/能力文案里的金色词为「募集」/「Recruit」：`newsanguo/localization/{zhs,eng}/cards.json:226`、`powers.json:89-90`。同日（用户 m00798）删去卡面与能力文案末句“在你的回合开始时，移除所有青州兵。”——该信息只由「募集」提示提供（能力类注释里仍保留行为说明）。
  - 待实机确认：曹氏兵法卡面与「曹氏兵法」能力悬停时标题为「募集1」（能力按层数）、说明含“在你的回合开始时，失去所有募集的青州兵。”、不再出现“召唤奥斯提”；升级/叠层后数值仍跟随；`RecruitKeyword.Recruit(decimal)`（直接给数量的重载）目前暂无调用方。**文案在 pck 里 ⇒ 需重导 pck 才可见**。

  - 文案简化（2026-10-09 用户 m01088）：`newsanguo/localization/{zhs,eng}/cards.json:226` 与 `powers.json:89-90` 里的「以1生命值[gold]募集[/gold]…」/「… with 1 HP each.」删掉了生命值那半句（现在读作「你每打出一张牌，就[gold]募集[/gold]{Summon:diff()}名青州兵。」/「…[gold]Recruit[/gold] {Summon:diff()} Qingzhou Soldiers.」）；`static_hover_tips.json:6` 的 `NEWSANGUO_KEYWORD_RECRUIT.description` 在「在你的回合开始时」/「At the start of your turn」前补了一个 JSON 转义的 `\n`（zhs/eng 同步）。键数不变（cards 266 / powers 119 / static_hover_tips 5）。改的是 pck 里的文案 ⇒ 需重导 pck。

  - 文案再简化（2026-10-09 用户 m01193）：卡面首句最终定成「你每打出一张牌，[gold]募集[/gold]1。」/「Whenever you play a card, [gold]Recruit[/gold] 1.」——**去掉了 `{Summon:diff()}` 与「名青州兵」**，数字写死 1（`Scripts/Cards/CaoArtOfWar.cs` 的 `Summon = 1` 不变；若日后募集数量改动，卡面不会跟着变，需手改这一处）。`\n` 与第二句（死亡给力量 `{StrengthPower:diff()}`）保留。**能力文案 `powers.json:89-90` 未动**（仍是 `{Amount}`，因为能力会叠层）。`newsanguo/localization/{zhs,eng}/cards.json:226` 同步；键数不变（cards 266）。改的是 pck 里的文案 ⇒ 需重导 pck。
- 青州兵形象（2026-10-09）：**用户 m00823 的「自己贴图」需求已按 m01088 回滚为复用原版奥斯提的 Spine 场景**，并把显示名改叫「青州兵（迫真）」。当前状态：`Scripts/Monsters/QingzhouSoldier.cs:42` 的 `AssetProfile`（`res://scenes/creature_visuals/osty.tscn`）就是实际战斗形象；显示名在 `newsanguo/localization/{zhs,eng}/monsters.json:6` 的 `NEWSANGUO_MONSTER_QINGZHOU_SOLDIER.name`＝「青州兵（迫真）」/「Qingzhou Soldier (Allegedly)」（用户指定中文，英文自拟，想改就改这一处），`Scripts/Monsters/QingzhouSoldier.cs:62` 的 `Title` 仍走同一个键。原来的实现（`TryCreateCreatureVisuals` + 卡面过渡图 + 按高度归一缩放）已整块删除，但机制值得留档：RitsuLib 的 `ModMonsterTemplate.TryCreateCreatureVisuals`（`.decompile/STS2RitsuLib.Scaffolding.Content/ModMonsterTemplate.cs:50-53,78-81`）会被 `MonsterModel.CreateVisuals` 的 `[HarmonyPriority(800)] Prefix` 优先采纳（`.decompile/STS2RitsuLib.Scaffolding.Content.Patches/ModModelRuntimeGodotFactoryPatches.cs:62-80`），配 `RitsuGodotNodeFactories.CreateFromResource<NCreatureVisuals>(texture)` 就能把一张 PNG 直接变成战斗形象（贴图底边贴地、Bounds = 尺寸×1.1，见 `.decompile/STS2RitsuLib.Scaffolding.Godot.NodeFactories/RitsuNCreatureVisualsNodeFactory.cs:122-157`）；非 Spine 形象下引擎不会调 `GenerateAnimator`（`.decompile/sts2full/sts2.decompiled.cs:476680-476698`）。以后要再换图可直接照此重做。
  - 死亡音效（用户 2026-10-09 拍板「死亡直接消失」）：`Scripts/Monsters/QingzhouSoldier.cs:54` 的 `HasDeathSfx => false` 是唯一的静音开关（`:50` 的 `DeathSfx` 保留奥斯提 `osty_die` 字符串当注释）。**形象换回 Spine 后引擎的死亡流程是可达的**：`NCreature.StartDeathAnim` 的 `SfxCmd.PlayDeath` 在 `if (_spineAnimator != null)` 内（`.decompile/sts2full/sts2.decompiled.cs:477104-477134`），`_spineAnimator` 只在 `if (HasSpineAnimation)` 里赋值（`:476680-476698`）⇒ 想恢复叫声就把该属性改成 `true`。受击音仍是原版通用护甲撞击音（`TakeDamageSfxType` 默认 `Armor`，`:78170-78172`；`SfxCmd.PlayDamage` 在 `:196959`；`Scripts/Patches/EngineSfxRedirectPatch.cs:15-53` 只截 4 条 `event:/newsanguo/sfx/*` 白名单）。待实机确认：死亡无声，但死亡动画仍是奥斯提那套（`GenerateAnimator` `:75` 又生效了；若连动画也要去掉需另写补丁）。

- 盛碗虫（巨石）的招式循环（2026-10-09 用户需求 m12112 逐字「修改“盛碗虫（巨石）”的意图，首个回合使用强化：获得10点力量，之后每回合攻击基础伤害改为15」）：`Scripts/Monsters/BowlbugBoulder.cs` 新增 `BUFF_MOVE`（`new BuffIntent()`，`BuffMove` 里 `CreatureCmd.TriggerAnim(creature, "Cast", 0.6f)` + `PowerCmd.Apply<StrengthPower>(…)` 给 `StrengthGain = 10`），`MonsterMoveStateMachine` 的**初始状态改成 buff**（`:177,:193`）⇒ 只在开战第一回合出现一次，之后进原版同构的头槌/眩晕循环；头槌基础伤害 `HeadbuttDamage` 25 → **15**（`:72`，意图数字由引擎按力量加成显示 ⇒ 强化后常态显示 25，与旧版等效但会随力量增减变化）。同时把图鉴插入“眩晕”动作的下标由写死 `1` 改成按 `stateId == HEADBUTT_MOVE` 定位（`:264-271`）。待实机确认：第 1 回合意图为「强化」且力量 +10；第 2 回合起意图显示 25；失衡眩晕分支仍正常；图鉴里「强化/头槌/昏头转向」三个招式名都显示（键名修正见 §5）。
- 第二批改名后的 8 张卡（2026-10-09，映射表见 §4.9）：`Scripts/Cards/DarkHornShark.cs`、`BladesOfVirtue.cs`、`TigerWindCloudDragon.cs`、`BoneMeltingPalm.cs`、`CrossTheRiverToo.cs`、`CentralPlainsPass.cs`、`FireAndWaterProof.cs`、`WineIsTheOldHero.cs` + 能力 `Scripts/Powers/TigerWindCloudDragonPower.cs`、`CentralPlainsPassPower.cs`。待实机确认：图鉴/卡面/牌堆显示新英文 title 与新能力名；卡图与音效不再是占位/静音（**素材与本地化都在 pck 里 ⇒ 必须先重导 pck**）；「中原雄关」「风从虎，云从龙」卡面的 `{<X>Power:diff()}` 仍能解析；老存档里这 8 张牌失效属预期。

### 8.3 待用户执行
- **重导 pck（本次必做）**：2026-10-09 两批改名合计动了 `newsanguo/localization/**`（键名 + 英文 title）、`newsanguo/images/{cards,powers}/*.png`（**31 张**改名图 = 线程 6 的 19 张 + 第二批 12 张）、`newsanguo/audios/*.mp3`（**27 个**改名音频 = 16 + 11）⇒ **不重导就会出现旧文案 / 占位图 / 静音**。命令见 README.md §2。其余积压待重导项：先古对话、海玻璃、曹氏兵法文案（含 2026-10-09 的「募集」/「临时募集」关键词文案：`static_hover_tips.json` 新增 2 键（键数 3→5）、`card_keywords.json` 删 2 键、`cards.json:226` 与 `powers.json:89-90` 改金色词，见 §8.2）、青州兵名称、天上大水与时光酸雨文案（含 2026-10-09 的「时空酸雨」→「时光酸雨」改名：类名 `TimeAcidRain`、本地化键 `NEWSANGUO_CARD_TIME_ACID_RAIN.*`、音效键 `time_acid_rain`）、直奔诸葛亮四轮车/四轮车文案与能力文案等、**怪物图鉴招式名**（2026-10-09：`monsters.json` 的键名由 `.moves.XXX_MOVE.title` 修正为 `.moves.XXX.title`，并新增盛碗虫的 `.moves.BUFF.title`「强化」/「Power Up」）。
- **用修好的配置重导 pck 并上传 Workshop**：2026-10-09 的修复只改了打包配置（`tools/.gdignore` + `export_presets.cfg:11`），**线上 Workshop 里那份仍是带 tools 副本的坏包**。用平时的导出流程重导即可（配置已生效，无需额外操作）；验证用的一次性导出包在 `%TEMP%\newsanguo_verify\newsanguo.pck`（91,905,844 B，`tools/`/`analysis/` 均为 0 条），也可以直接拿它去替换 `mods\newsanguo\newsanguo.pck` 做实测。**注**：第二批改名**没有**手工改 `.godot/uid_cache.bin`——检测到 Godot 编辑器正在运行（2026-10-09 17:15 刚写过一次，且头部 `count=660` 与实际 657 条已不一致），留给 Godot 重导时自行刷新（§4.13）。

### 8.4 待换/待补
- 青州兵形象：**已按用户 2026-10-09（m01088）要求回滚为复用原版奥斯提的 Spine 场景**（`Scripts/Monsters/QingzhouSoldier.cs:42`），显示名改为「青州兵（迫真）」/「Qingzhou Soldier (Allegedly)」（`newsanguo/localization/{zhs,eng}/monsters.json:6`，需重导 pck）。原「专用立绘 `newsanguo/images/monsters/QingzhouSoldier.png`」需求作废，代码里的自绘贴图方案已删除（详见 §8.2；要重做可照 §8.2 留档的机制另开一轮）。

此外 §6 的 11 个待补素材（`megalovania_power.mp3` 已于 2026-10-09 移出，见 §8.2）。


---

## 9. 与用户协作的约定

- 用户在 Windows 上、用中文交流；仓库注释、汇报一律中文。
- 用户非常在意**精确**：汇报要给 `文件:行`、给构建结果、给部署产物的哈希；不要含糊说"应该没问题"。
- 用户会自己实机验收并回来反馈，**不要假称已验证游戏内表现**；没实测就写"待实机确认"。
- 用户自己负责重导 pck 与 git 推送；agent 只改代码/资源并提醒。
- 改动尽量小而聚焦，顺手发现的问题要么当场说明并单独提示，要么写进 §8，不要悄悄大改。
- 需要用户在"几种做法"里选择时，用清晰选项（像 §8.1 那样列出 a/b/c 与各自代价）问，而不是替用户决定。
