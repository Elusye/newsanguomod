# 新三国（newsanguo）

一个被天意侵蚀的世界。

《杀戮尖塔 2》（Slay the Spire 2）的自定义角色/内容 mod，基于 [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295) 框架开发。

## 当前版本与下载

- **版本：0.3.12-beta.1**（本仓库 `main` 为 Beta 开发分支）
- **游戏：0.114.0**
- **前置：RitsuLib 0.6.16 或更高版本**

[下载可直接安装的 ZIP](https://github.com/Elusye/newsanguomod/releases/download/v0.3.12-beta.1/newsanguo-0.3.12-beta.1-sts2-0.114.0.zip) · [全部发布版本](https://github.com/Elusye/newsanguomod/releases) · [RitsuLib 下载](https://github.com/BAKAOLC/STS2-RitsuLib/releases/tag/v0.6.16)

工坊更新审核期间，可从 GitHub Releases 下载已打包的版本，无需自行编译。请下载发布页的模组 ZIP，自动生成的 `Source code` 压缩包只有源码。

## 安装与更新

1. 关闭游戏。在 Steam 中选择游戏的“管理 → 浏览本地文件”，打开安装目录。
2. 安装或更新 **RitsuLib ≥0.6.16**，使用与游戏版本匹配的安装包；已安装且满足版本要求时可跳过。
3. 下载上方新三国 ZIP，将其中的 `newsanguo` 文件夹解压到游戏的 `mods` 文件夹，更新时覆盖同名文件。
4. 启动游戏，在模组列表启用新三国及 RitsuLib。

正确的文件结构：

```text
Slay the Spire 2/
  mods/
    newsanguo/
      newsanguo.dll
      newsanguo.json
      newsanguo.pck
```

不要额外套一层同名文件夹。如果同时订阅了工坊版，请只启用一份新三国，避免重复加载。联机玩家应使用相同的模组文件；仅版本号相同不能保证文件一致。

Release 分支针对其他游戏版本维护，不随 Beta 自动同步；旧游戏版本请使用对应的兼容版本。

## 内容总览

- **曹魏、蜀汉**两个可玩角色，均可直接选择，无需开启测试模式。
- 两个角色专属卡池，以及双方共有的中立牌。
- 自定义遗物、药水、事件、怪物与能力。
- 旧版卡牌衍生池，包含旧神话形态及旧版塔罗牌，可通过“看看首播版”等效果获得。
- 中英文双语本地化与先古对话。

### 核心机制

| 机制 | 说明 |
| --- | --- |
| 天意之力 | 可获得与失去的资源，关联额外回合及天意侵蚀 |
| 酒力 | 攻击加成资源，可与多种卡牌及能力互动 |
| 募集 | 召唤优先承受攻击的青州兵，每位玩家最多15名 |
| 地域牌 | 持有于手牌时生效，可右键消耗 |
| 附魔 | 为卡牌赋予额外效果 |

### 游戏内设置

在 **设置 → Mod设置 → 新三国设置** 中可调整：

- 卡牌、能力和事件音效的开关与音量倍率。
- 三国杀基础出牌音效开关。
- **启用新三国mod事件**：默认开启，新跑局生效，联机以房主设置为准。

角色选择界面也有“新三国设置”入口。音量还可使用控制台命令 `newsanguo_sfx_volume` 调整。

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

- `newsanguo.json` 声明依赖 `STS2-RitsuLib >= 0.6.16`，游戏版本 `>= 0.114.0`

### 本地双开联机测试（无需 Steam）

将 RitsuLib 工作坊内容完整复制到 `mods\STS2-RitsuLib\`（本地目录 mod 加载需要），然后：

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
  Cards/       卡牌（含旧版衍生牌）
  Characters/  角色与卡池/遗物池/药水池定义（曹魏、蜀汉）
  Combat/      战斗辅助（天意之力、Scry、从右到左自动打出）
  DevConsole/  控制台命令
  Events/      事件（新三国道、陈留大食堂、野生中立伏兵、三顾茅庐、实践主义者、东尼先生的假期）
  Helpers/     工具（EnchantHelper 附魔）
  Monsters/    怪物（盛碗虫（巨石）、青州兵）
  Patches/     Harmony 补丁
  Potions/     药水（杏花村、天意爷的小纸条）
  Powers/      自定义能力
  Relics/      遗物（沛国佳酿、百年佳酿、魔法禁术目录、传送门、R键、
               天子经济学、卫星城小沛、种地将军于禁、巨石）
  Rewards/     奖励（创造模式）
  Settings/    mod 设置（音效音量、事件开关）
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
- **卡池归属**：单角色牌通过 `[RegisterCard(typeof(...))]` 注册到曹魏或蜀汉池；中立牌注册到 `NeutralCardPool` 并继承 `NeutralCardTemplate`，在 `NeutralCardPool.RegisterSharedCards()` 中注入双方奖励池。不要同时重复注册，也不要恢复已弃用的 `CardTypes` 列表。迁移中立牌时同步移除额外注入。
- **卡牌类与能力类不可同名**：能力一律加 `_power` 后缀（如 `heavens_decay` 卡牌 / `heavens_decay_power` 能力），避免 `FromPower<T>` 命名空间解析冲突
- **卡牌描述动态数值**：使用 `PowerVar<T>` / `DamageVar` / `BlockVar` / `IntVar` 等 `CanonicalVars`，描述中配合 `{Name:diff()}` 实时显示
- **升级/降级关键字增减**：在 `OnUpgrade()` / `AfterDowngraded()` 中用 `AddKeyword` / `RemoveKeyword`，不要直接改 `CanonicalKeywords`
- **诅咒牌**：`CardType.Curse` + `CardRarity.Curse` + `TargetType.None` + 费用 -1，`Eternal`/`Unplayable` 关键字由引擎按序自动追加
- **新增卡牌的 4 处配套改动**：卡牌类、`localization/zhs/cards.json` + `localization/eng/cards.json`、卡图 PNG（可选音效：加进 `Scripts/NewsanguoSfx.cs` 的表）
- **资源改动需重新导出 PCK**：使用 `build.cmd` 或 `export-pck.ps1`，不要直接调用 Godot 导出正式包
- **纯代码改动无需重新导出 PCK**：直接运行 `dotnet build` 即可
- **构建部署前关闭游戏**：游戏运行中可能锁定 DLL 或 PCK，导致部署失败
