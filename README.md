# 新三国（newsanguo）

一个被天意侵蚀的世界。

《杀戮尖塔 2》（Slay the Spire 2）的自定义角色/内容 mod，基于 [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295) 框架开发。

## 内容总览

- 两个角色：
  - **曹魏**：默认可用
  - **蜀汉**：施工中，默认不出现在角色选择界面，需在 mod 设置里打开「测试模式」（见下）
- 126 张卡牌（曹魏卡池 / 蜀汉卡池，含 6 张「旧版」备份牌）
- 10 件遗物、2 种药水、5 个事件、2 种怪物、40 余个能力
- 中/英双语本地化（含 9 位先古的对话）

### 核心机制

| 机制 | 说明 |
| --- | --- |
| 天意之力 | 资源型能力，卡牌获得/消耗，负值时部分卡牌有额外效果 |
| 酒力 | 攻击加成资源，来自酒相关卡牌与初始遗物 |
| 附魔系统 | 战斗内为手牌附加随机附魔（`EnchantHelper`） |

### 测试模式

「蜀汉」仍在施工中，默认**不会**出现在角色选择界面，也不会被「随机角色」选中。打开方式：

**设置 → Mod 设置 → 新三国设置 → 测试模式**

- 默认关闭，开关状态落盘在游戏用户数据目录的 `newsanguo_test_mode.json`
- 切换后**重新进入角色选择界面**即可生效，不需要重启游戏
- 图鉴（卡牌总览）里的蜀汉卡池筛选项始终可见，关闭测试模式时也允许查阅卡牌

同一设置页另有「音效音量」分区（卡牌/能力音效开关与倍率），并配有控制台命令 `newsanguo_sfx_volume`。

## 安装

1. 订阅安装 [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295)（创意工坊物品 3747602295），或将其内容复制到 `mods\RitsuLib\` 使用
2. 将本项目构建产物部署到 `mods\newsanguo\`（见下节）
3. 启动游戏（主菜单勾选 mod）

## 构建与部署

### 1. 配置本机路径

首次构建前，将示例配置复制为本机配置：

```powershell
Copy-Item .\local.props.example .\local.props
```

然后编辑 `local.props`：

- `Sts2Dir`：杀戮尖塔 2 的安装根目录
- `GodotExe`：Godot 4.5.1 Mono 控制台版路径；留空时导出脚本会依次查找 `PATH` 和默认安装位置

`local.props` 已被 Git 忽略，不会提交本机路径。构建需要 .NET 9 SDK、Godot 4.5.1 Mono 和 Python 3。**缺少 `local.props` 时构建会直接报错**（`newsanguo.csproj` 里的 `Validate Local Paths` 目标会挡住）。

### 2. 完整构建并部署

默认构建 Debug 版本：

```powershell
.\build.cmd
```

构建 Release 版本：

```powershell
.\build.cmd Release
```

`build.cmd` 会依次编译 C#、部署 DLL/JSON、导出并清理 PCK，最后将成品部署到 `mods\newsanguo\`。编译失败时不会继续导出 PCK。建议执行前关闭游戏，避免已加载的 DLL 或 PCK 被占用。

### 3. 仅编译 C#

只修改 `.cs` 代码时，可以跳过 PCK 导出：

```powershell
dotnet build .\newsanguo.csproj -c Debug
```

PostBuild 会自动将 DLL 和 JSON 复制到游戏的 `mods\newsanguo\` 目录。

### 4. 仅导出 PCK

修改了 `localization/`、`images/`、`audios/` 等资源时，可以单独运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\export-pck.ps1
```

脚本会先导出到临时文件，移除不应随模组分发的 Godot 类缓存和 UID 缓存，成功后再部署到 `mods\newsanguo\newsanguo.pck`。不要直接调用 Godot 导出正式 PCK，否则会绕过清理步骤。

如需临时覆盖本机配置，可向脚本传入 `-Sts2Dir`、`-GodotExe` 或 `-OutputPath`。

#### PCK 打包注意（0.3.1 选牌界面事故）

模组 PCK 以最高优先级挂载，一旦把**带本体 UID 的场景副本**打进包里，就会覆盖本体资源。0.3.1 曾把 `tools/` 下三份取自本体的参考场景（`card_library.tscn`、`card_grid.tscn`、`simple_card_select.tscn`）打进 PCK，导致**事件与篝火选牌界面报 `NCardGrid` 错误**。现行约定：

- `tools/.gdignore` 让 Godot 整体忽略 `tools/`（参考场景仍可人工阅读，但不参与导入/导出）
- `export_presets.cfg` 的 `exclude_filter` 排除 `tools/*`、`analysis/*` 与两个 Godot 缓存文件
- **导出必须走 `export-pck.ps1`**：它还会用 `tools\sanitize_pck.py` 把 Godot 强制写入的 `.godot/uid_cache.bin` 与 `.godot/global_script_class_cache.cfg` 从包里真正删掉（这两个 `exclude_filter` 拦不住）

### 5. 运行依赖

- `newsanguo.json` 声明依赖 `STS2-RitsuLib >= 0.6.2`，游戏版本 `>= 0.111.0`

### 本地双开联机测试（无需 Steam）

将 RitsuLib 工作坊内容完整复制到 `mods\RitsuLib\`（本地目录 mod 加载需要），然后：

```powershell
# 主机
SlayTheSpire2.exe --fastmp host_standard --force-steam off
# 加入方
SlayTheSpire2.exe --fastmp join --force-steam off
```

## 项目结构

```
Scripts/
  Api/         跨 mod 公开 API（NewsanguoPublicApi）
  Cards/       卡牌（126 张，含「旧版」备份牌）
  Characters/  角色与卡池/遗物池/药水池定义（曹魏、蜀汉）
  Combat/      战斗辅助（天意之力、Scry、从右到左自动打出）
  DevConsole/  控制台命令
  Events/      事件（新三国道、陈留大食堂、野生中立伏兵、三顾茅庐、实践主义者）
  Helpers/     工具（EnchantHelper 附魔）
  Monsters/    怪物（盛碗虫（巨石）、青州兵）
  Patches/     Harmony 补丁（17 个）
  Potions/     药水（杏花村、天意爷的小纸条）
  Powers/      能力（40 余个）
  Relics/      遗物（沛国佳酿、百年佳酿、魔法禁术目录、传送门、空城计、R键、
               天子经济学、卫星城小沛、种地将军于禁、巨石）
  Rewards/     奖励（创造模式）
  Settings/    mod 设置（音效音量、测试模式）
  Telemetry/   遥测
  Entry.cs     mod 入口与补丁注册
  NewsanguoSfx.cs      音效表与播放封装
newsanguo/
  localization/zhs/   中文本地化（cards/powers/relics/events/ancients 等 10 个 JSON）
  localization/eng/   英文本地化（与 zhs 键一一对应）
  images/             卡图、角色图、遗物图
  audios/             音效与音乐
```

## 开发约定

- **卡牌类名同时是卡牌 id、本地化键与卡图文件名**：本地化键为 `NEWSANGUO_CARD_` + 类名的 UPPER_SNAKE 形式（`CaoArtOfWar` → `NEWSANGUO_CARD_CAO_ART_OF_WAR`），卡图路径为 `res://newsanguo/images/cards/{类名}.png`。**类名一经发布不要再改**：改类名等于换 id，老存档牌组里的这些牌会认不出来，卡图、`.import` 与 `localization/{zhs,eng}/cards.json` 的键都要跟着改
- **卡池归属由 `[RegisterCard(typeof(...))]` 特性决定**，不是 `CardTypes` 列表（后者已标记 `[Obsolete]`，仅为基类保留）：只属于曹魏写 `typeof(NewsanguoCardPool)`；只属于蜀汉写 `typeof(ShuHanCardPool)`；两角色共有＝特性写 `typeof(NewsanguoCardPool)` 并在 `ShuHanCardPool.CardTypes` 里再列一次
- **测试中的内容用 `NewsanguoTestModeSettings.TestModeEnabled` 控制可见性**，不要用 epoch 解锁或改 `IsPlayable`（会把「随机角色」整块藏掉）
- **卡牌类与能力类不可同名**：能力一律加 `_power` 后缀（如 `heavens_decay` 卡牌 / `heavens_decay_power` 能力），避免 `FromPower<T>` 命名空间解析冲突
- **卡牌描述动态数值**：使用 `PowerVar<T>` / `DamageVar` / `BlockVar` / `IntVar` 等 `CanonicalVars`，描述中配合 `{Name:diff()}` 实时显示
- **升级/降级关键字增减**：在 `OnUpgrade()` / `AfterDowngraded()` 中用 `AddKeyword` / `RemoveKeyword`，不要直接改 `CanonicalKeywords`
- **诅咒牌**：`CardType.Curse` + `CardRarity.Curse` + `TargetType.None` + 费用 -1，`Eternal`/`Unplayable` 关键字由引擎按序自动追加
- **新增卡牌的 4 处配套改动**：卡牌类、`localization/zhs/cards.json` + `localization/eng/cards.json`、卡图 PNG（可选音效：加进 `Scripts/NewsanguoSfx.cs` 的表）
- **资源改动需重新导出 PCK**：使用 `build.cmd` 或 `export-pck.ps1`，不要直接调用 Godot 导出正式包
- **纯代码改动无需重新导出 PCK**：直接运行 `dotnet build` 即可
- **构建部署前关闭游戏**：游戏运行中可能锁定 DLL 或 PCK，导致部署失败
