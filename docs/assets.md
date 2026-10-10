> 本文件是 `AGENTS.md` 的分册（原编号沿用，`§4.9` 之类引用仍然有效）。根文件：`../AGENTS.md`。
> 改完代码请把新查证的事实回填本文件，**不要**回填根文件——根文件必须保持短小，否则会被工作区指令预算截断。
## 6. 素材

- 卡图：`newsanguo/images/cards/<类名>.png`（来自 `AssetProfile` 的 `PortraitPath`，一律用类名）。
- 角色相关：`newsanguo/images/characters/<角色>/…`（选人图标、锁定图标、战斗形象、死亡形象、商店/休息处形象、地图标记）。
- 能力/遗物图：`res://newsanguo/images/powers|relics/<名字>.png`（大图惯例 `<名字>Big.png`）。
- 音频：`newsanguo/audios/<snake_name>.mp3`。
- 能量图标（2026-10-10）：两角色分别使用 `newsanguo/images/ui/energy/{CaoWei,ShuHan}/energy.png` 与 `energy_small.png`，当前沿用原占位图。卡池的能量标识分别为 `newsanguo_caowei` / `newsanguo_shuhan`，避免 RitsuLib 按 EnergyColorName 缓存时冲突；HUD 补丁按角色读取对应卡池的大图标。共有牌按卡池归属显示；共用遗物/药水池仍保留 `newsanguo` 标识及旧图标。需用户重导 pck。
- 多人指向/石头剪刀布手势（2026-10-10）：两角色分别使用 `newsanguo/images/ui/hands/{CaoWei,ShuHan}/multiplayer_hand_{point,rock,paper,scissors}.png`，由各自角色类的 `CharacterMultiplayerAssetSet` 配置。当前各复制一套现有占位图，可分别替换；新 PNG 的导入缓存由 Godot 生成，需用户重导 pck。
- 加/换素材后要提醒用户重导 pck；`.godot/**/*.import` 由 Godot 侧生成。
- **待补素材清单**（代码已引用、文件还没进包）：只剩 `ChargeToZhugeLiangsCartPower.png`(+`Big`) 两张能力图（`Scripts/Powers/ChargeToZhugeLiangsCartPower.cs:48-50`）。4 张卡图（`HeavenlyDeluge`/`TimeAcidRain`/`ChargeToZhugeLiangsCart`/`FourWheeledCart`）与对应 4 个 mp3 已于 2026-10-09 补齐并完成 Godot 导入。音频响度均衡表 2026-10-09 全量复核（口径见 `Scripts/NewsanguoSfx.cs:52-70`，表内 154 条 / 音频 158 个）。`megalovania_power.mp3` 不在清单内——「狂妄之人」能力不再播放音效（见 todo.md §8.2）。

---
