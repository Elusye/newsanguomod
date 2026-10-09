# 新三国（newsanguo）

一个被天意侵蚀的世界。

《杀戮尖塔 2》（Slay the Spire 2）的自定义角色/内容 mod，基于 [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295) 框架开发。

## 内容总览

- 新角色「新三国」（含专属卡池、遗物池、药水池、初始遗物与先古对话）
- 约 100 张卡牌（含初始牌与若干"旧版"牌）
- 3 件遗物、3 个事件、30 余个能力
- 完整中文本地化（含 9 位先古的对话）

### 核心机制

| 机制 | 说明 |
| --- | --- |
| 天意之力 | 资源型能力，卡牌获得/消耗，负值时部分卡牌有额外效果 |
| 酒力 | 攻击加成资源，来自酒相关卡牌与初始遗物 |
| 附魔系统 | 战斗内为手牌附加随机附魔（`EnchantHelper`） |

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

`local.props` 已被 Git 忽略，不会提交本机路径。构建需要 .NET 9 SDK、Godot 4.5.1 Mono 和 Python 3。

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

### 5. 运行依赖

- `newsanguo.json` 声明依赖 `STS2-RitsuLib >= 0.5.1`，游戏版本 `>= 0.111.0`

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
  Cards/        卡牌（约 100 张）
  Characters/   角色、卡池/遗物池/药水池定义
  Combat/       战斗辅助（Scry 等）
  Events/       事件（陈留大食堂、新三国道、野生中立伏兵）
  Helpers/      工具（EnchantHelper 附魔）
  Patches/      Harmony 补丁（死亡音效、能量图标等）
  Powers/       能力（约 30 个）
  Relics/       遗物（沛国佳酿、百年佳酿、传送门）
  Entry.cs      mod 入口与补丁注册
newsanguo/
  localization/zhs/   中文本地化（cards/powers/relics/events/ancients）
  images/             卡图、角色图、遗物图
  audios/             音效与音乐（Fmod bank）
```

## 开发约定

- **卡牌类与能力类不可同名**：能力一律加 `_power` 后缀（如 `heavens_decay` 卡牌 / `heavens_decay_power` 能力），避免 `FromPower<T>` 命名空间解析冲突
- **卡牌描述动态数值**：使用 `PowerVar<T>` / `DamageVar` / `BlockVar` / `IntVar` 等 `CanonicalVars`，描述中配合 `{Name:diff()}` 实时显示
- **升级/降级关键字增减**：在 `OnUpgrade()` / `AfterDowngraded()` 中用 `AddKeyword` / `RemoveKeyword`，不要直接改 `CanonicalKeywords`
- **诅咒牌**：`CardType.Curse` + `CardRarity.Curse` + `TargetType.None` + 费用 -1，`Eternal`/`Unplayable` 关键字由引擎按序自动追加
- **资源改动需重新导出 PCK**：使用 `build.cmd` 或 `export-pck.ps1`，不要直接调用 Godot 导出正式包
- **纯代码改动无需重新导出 PCK**：直接运行 `dotnet build` 即可
- **构建部署前关闭游戏**：游戏运行中可能锁定 DLL 或 PCK，导致部署失败
