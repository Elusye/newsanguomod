> 本文件是 `AGENTS.md` 的分册（原编号沿用，`§4.9` 之类引用仍然有效）。根文件：`../AGENTS.md`。
> 改完代码请把新查证的事实回填本文件，**不要**回填根文件——根文件必须保持短小，否则会被工作区指令预算截断。
## 5. 本地化

- 目录：`newsanguo/localization/{zhs,eng}/`，各 10 个文件：`ancients.json`（先古对话，~21KB，最大）、`cards.json`（~30KB，266 键）、`events.json`、`powers.json`、`relics.json`、`characters.json`、`monsters.json`、`potions.json`、`card_keywords.json`、`static_hover_tips.json`。
- 键规则：`NEWSANGUO_<类型>_<类名 UPPER_SNAKE>`（驼峰拆词，`CaoArtOfWar` → `CAO_ART_OF_WAR`；改名后键都按新类名派生，见 facts.md §4.9）。玩法文案里常见 SmartFormat：`{Damage:diff()}`、`{HeavensForcePower:inverseDiff()}`、`[gold]…[/gold]`。
- **怪物图鉴的招式名是另一套键**：`<怪物 id>.moves.<招式状态名去掉结尾 _MOVE>.title`（引擎 `MonsterModel.GetBestiaryMoveName`，`.decompile/sts2full/sts2.decompiled.cs:78482-78485`；调用它的 `GenerateBestiaryMoveList` 在 `:78308-78351`：先剥 `_MOVE` 再查键，查不到会 `Log.Warn("No loc for move …")` 并回落到状态名）。**2026-10-09 修正**：`newsanguo/localization/{zhs,eng}/monsters.json` 里两个怪物此前写成 `.moves.HEADBUTT_MOVE.title` / `.moves.DIZZY_MOVE.title` / `.moves.NOTHING_MOVE.title`，是永远命不中的死键（状态名带 `_MOVE`、键名不带），已改为 `.moves.HEADBUTT.title` / `.moves.DIZZY.title` / `.moves.NOTHING.title`，并补上盛碗虫（巨石）新增的 `.moves.BUFF.title`。
- 中英两份必须同步改；改完 `ConvertFrom-Json` 校验 + 键数对比 + 检查无 BOM、无残留 `\\n`。
- 本地化在 pck 内 ⇒ 改完**必须由用户重导 pck**。

---
