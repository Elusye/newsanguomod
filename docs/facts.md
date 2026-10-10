> 本文件是 `AGENTS.md` 的分册（原编号沿用，`§4.9` 之类引用仍然有效）。根文件：`../AGENTS.md`。
> 改完代码请把新查证的事实回填本文件，**不要**回填根文件——根文件必须保持短小，否则会被工作区指令预算截断。
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
- **每张牌改名的同步清单**：`Scripts/Cards/<类名>.cs`（文件名 + 类名 + `[RegisterCard]`）→ 同名 `Scripts/Cards/<类名>.cs.uid` → `newsanguo/localization/{zhs,eng}/cards.json` 的 `<键>.title` / `<键>.description` → `newsanguo/images/cards/<类名>.png`(+`.png.import`) → 卡池列表 `Scripts/Characters/NewsanguoCardPool.cs:63-140` 与 `Scripts/Characters/ShuHanCardPool.cs:63-130` 的 `CardTypes` → 全仓 `HoverTipFactory.FromCard<T>()`（15 处，见 facts.md §4.10）。**若同时改能力类名**，还要加：`Scripts/Powers/<X>Power.cs`(+`.cs.uid`)、`newsanguo/localization/{zhs,eng}/powers.json` 的 `NEWSANGUO_POWER_<X>_POWER` 的 `.title`/`.description`/`.smartDescription` 三键、以及卡面描述里的 `{<X>Power:diff()}` 引用。已存在此耦合的有 WheresWinePower、CaoArtOfWarPower、AssignmentPower、RatPoisonSchemePower。
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
- 订正：旧版把 `Scripts/Cards/OldDirge.cs` 那处写成 `(Soldier)`，实际是 `(Soul)`；行号因 facts.md §4.11 的 `using` 增删整体上移 1 行。

### 4.11 `Scripts/Cards` 命名空间（2026-10-09 已统一为 `newsanguo.Scripts.Cards`）
132 个 `.cs` **全部**是 `namespace newsanguo.Scripts.Cards;`（改动前：125 个 `newsanguo.Scripts` + 6 个 `newsanguo.Scripts.Cards`（`HeavensForceVar.cs`、`NewsanguoCardTemplate.cs`、`NewsanguoCurseTemplate.cs`、`ProxyStrikeIncreaseVar.cs`、`ScryKeyword.cs`、`TaxGoldVar.cs`）+ 1 个 `Soldier.cs`（见 facts.md §4.9 订正））。其余目录一律跟目录名（`Powers`→`newsanguo.Scripts.Powers` 44、`Characters`→`newsanguo.Scripts.Characters` 6、`Patches`→`newsanguo.Scripts.Patches` 17、`Relics` 10、`Events` 5 …）。配套改动：
- 126 个被改命名空间的卡文件里原有的 `using newsanguo.Scripts.Cards;`（它们要用那 6 个辅助类）变成自引用冗余，已删除。
- 17 个 `newsanguo.Scripts.*` 外部文件补了 `using newsanguo.Scripts.Cards;`：`Scripts/Characters/NewsanguoCardPool.cs:12`、`Scripts/Characters/ShuHanCardPool.cs:12`、`Scripts/Entry.cs:8`、`Scripts/Events/ChenliuGrandMessHall.cs:8`、`Scripts/Patches/AlwaysMineDiscardGlowPatch.cs:8`、`Scripts/Powers/ChargeToZhugeLiangsCartPower.cs:8`、`Scripts/Powers/DrunkenMightPower.cs:8`、`Scripts/Powers/HeavenlyTroopsPlusPower.cs:17`、`Scripts/Powers/HeavenlyTroopsPower.cs:17`、`Scripts/Powers/HeavensForcePower.cs:15`、`Scripts/Powers/NearAndFarDexterityPower.cs:15`、`Scripts/Powers/NearAndFarStrengthPower.cs:15`、`Scripts/Powers/RatPoisonSchemePower.cs:12`、`Scripts/Powers/StarryNightDexterityPower.cs:12`、`Scripts/Powers/WindOfTigerPower.cs:11`、`Scripts/Relics/SonOfHeavenEconomics.cs:14`、`Scripts/Rewards/CreativeModeReward.cs:16`。
- `Scripts/Powers/SovereignFormPower.cs:16` 的 XML doc `<see cref="newsanguo.Scripts.SovereignForm"/>` 已改成 `newsanguo.Scripts.Cards.SovereignForm`（全仓**唯一**一处命名空间字面量引用；另 4 处 `<see cref>` 指向 `newsanguo.Scripts.Powers` / `newsanguo.Scripts.Combat`，不受影响）。
- **机制**：卡文件里 `NewsanguoSfx` 等根命名空间类型靠**外层命名空间查找**仍然可见 ⇒ 卡文件**不需要**补 `using newsanguo.Scripts;`；漏 using 的编译错误只会出现在同级命名空间（上述 17 个文件）。命名空间改动**不动任何 id**（facts.md §4.9）。

### 4.12 卡牌数量的口径
`Scripts/Cards/*.cs` = **132**（含 129 个普通卡 + 模板/变量/关键字等辅助类），带 `[RegisterCard]` 的 = **127**（其中 `Scripts/Cards/NewsanguoCurseTemplate.cs` 是模板基类，其 `:9` 注释本身也含 `[RegisterCard(typeof(CurseCardPool))]` 字样故被 grep 命中）。真正有本地化 `title` 的卡 = **126**；`Scripts/Cards/*.cs.uid` = 134；`newsanguo/images/cards` = 120 png + 120 png.import；`newsanguo/audios` = 154 mp3。`cards.json` 266 键 = `title` 126 + `description` 126 + 无后缀 9 + `selectionScreenPrompt` 5；126 张卡的 title 键在 zhs/eng **零缺失、零孤儿**，驼峰→UPPER_SNAKE 规则与现有键 100% 一致。
- 4 张基础牌是唯一的「卡图不走类名」例外：`Scripts/Cards/StrikeCaowei.cs:27` 与 `Scripts/Cards/StrikeShuhan.cs:25` 都写死 `"res://newsanguo/images/cards/StrikeNewsanguo.png"`；`Scripts/Cards/DefendCaowei.cs:31` 与 `Scripts/Cards/DefendShuhan.cs:31` 都写死 `"res://newsanguo/images/cards/DefendNewsanguo.png"`（**两个文件都在用，不是孤儿 PNG**）。其余全部是 `$"res://newsanguo/images/cards/{GetType().Name}.png"`。
- 4 张基础牌的音效键也是刻意共享的：`strike_newsanguo`（`Scripts/NewsanguoSfx.cs:205` 表项、`Scripts/Cards/StrikeCaowei.cs:48` 与 `Scripts/Cards/StrikeShuhan.cs:46` 播放）、`defend_newsanguo`（`:120` 表项、`Scripts/Cards/DefendCaowei.cs:50` 与 `Scripts/Cards/DefendShuhan.cs:50` 播放）、`:349` 静音白名单 `["strike_newsanguo", "defend_newsanguo", "soldier", "soul_shackles"]`。（2026-10-09 第二批改名后 `LoudnessGainDb` 表按 ordinal 整体重排：150 项、文件 853 行 ⇒ 三个行号分别前移 4/2/1。）
- `Old*` 六张（`OldBorrowedTime`/`OldCompact`/`OldDirge`/`OldExpectAFight`/`OldForgottenRitual`/`OldFuel`，全在 `TokenCardPool`）**不是备份牌**，是刻意保留的旧版衍生牌（保留原版角色卡框，理由见 `Scripts/Cards/OldBorrowedTime.cs:21,26`），不属命名事故。

### 4.13 素材 / Godot 导入缓存改名规程（线程 6 实操，2026-10-09）
- **导入缓存文件名 = `<源文件名>-<md5("res://<资源完整 res 路径>") 的小写 hex>`**（与文件内容无关；4 例实测全部吻合，例：`res://newsanguo/images/cards/Empower.png` → `15694fa0c049143c521ffb9912e5d88a`、`res://newsanguo/images/cards/WhereSWine.png` → `b1977f0b3e5382221bd1bd3a4c014ad7`）。PNG 在 `.godot/imported/` 下是 `<...>.ctex` + `<...>.md5` 两个文件，MP3 是 `<...>.mp3str` + `<...>.md5`。
- 因此**改素材文件名可以完全离线完成、不必开 Godot 重导**：① `Move-Item` 改源文件名；② 按公式算旧/新 token，`Move-Item` 重命名 `.godot/imported/` 里那两个文件；③ 改同名 `.import` 里的 `path=` / `dest_files=`（缓存 token）与 `[deps] source_file=`（`res://` 路径），**`uid=` 保持不变**（uid 是资产身份，改它才会出问题）。
- `.godot/imported/` 里还有大量**陈旧条目**（小写名、`<X>_big.png`、已删素材如 `RatPoisonPlusPower*`、早期管线的 `*.wav-*.sample`）——**不要动**，Godot 会自己清理；判断"陈旧"的办法是看有没有 `.import` 引用它。
- **`.godot/uid_cache.bin` 必须同步改**（`export_presets.cfg:11` 的 `exclude_filter` 排不掉它，Godot 在过滤**之后**强制写进 pck，见 facts.md §4.8）：格式 = `int32 count` + 每项（`int64 uid` + `int32 len` + `len` 字节 UTF-8 的 `res://` 路径），末尾无 padding。改路径时 **uid 全部保持不变**——线程 6 改了 52 条路径（11 卡 `.cs` + 11 卡图 + 8 能力图 + 4 能力 `.cs` + 16 mp3 + 2 补丁 `.cs`），文件 35,844 B → 35,984 B，`count` 仍 639、`consumed == bytes`。`global_script_class_cache.cfg`（8 B）不含路径，无需处理。**注意**：手工改只是为了在用户重导前保持仓库自洽；素材改名后**必须重导 pck**，让 Godot 自己刷新这些缓存。
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
