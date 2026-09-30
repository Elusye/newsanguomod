# 新三国 mod 遥测报表（PostHog · US cloud）

- 项目：PostHog US cloud（`POSTHOG_CLI_HOST` = us），事件名 **`run_history.completed`**（RitsuLib 遥测的「大盘层」）
- 数据窗口：**2026-09-29 13:05:56Z → 2026-09-30 02:42:30Z**
- 样本：**1127 局**，来自 **524 个匿名安装 id**（`anonymous_install_id`）
- 数据路径：`events.properties.payload` → JSON 字符串 → `applicant_payload.run_history`
  → `map_point_history[act][point]` → `player_stats[]` → `card_choices[] / event_choices[] / relic_choices[] / potion_choices[] / ancient_choice[]`

> 原始表在 `analysis/posthog/out/*.tsv`；每条查询是一个 `.sql` 文件，可用
> `& .\tools\posthog-sql.ps1 -SqlFile analysis\posthog\13_card_pick_rates.sql` 重跑。

## 1. 大盘（`14_overview.sql`）

| runs | installs | wins | win_rate | avg_floor | median_floor | max_floor | avg_min | avg_players | avg_asc | game_ver | ritsulib_ver | langs |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1124 | 523 | 627 | 55.78% | 34.3 | 48 | 182 | 31.7 | 1.24 | 5.3 | 2 | 2 | 2 |

- 联机占比：`avg_players = 1.24` → 约 **19%** 的局是 2 人联机（按人数加权的粗略估计）。
- 胜率 55.8% 偏高：注意这是**已结束跑局**（含进阶分布 A0–A20，均值仅 5.3），且玩家多为 mod 爱好者，暂不能当作平衡性结论。

## 可视化图表（PostHog 网页端）

已把选取率做成两张可交互图表，并归入同一个 dashboard（US cloud，项目 635605）：

| 图表 | 链接 | 形式 |
|---|---|---|
| ① 卡牌选取率排行（候选≥300） | https://us.posthog.com/project/635605/insights/2Ry2k6SI | 柱状图，y = 选取率（百分比格式），Top 25 |
| ② 卡牌选取率最低（候选≥300） | https://us.posthog.com/project/635605/insights/mq6aUf8C | 柱状图，按选取率**升序**（最冷门在最左） |
| ③ 候选次数 Top 20（被选 vs 跳过） | https://us.posthog.com/project/635605/insights/oHq0zm1p | 堆叠柱状图，y = 被选 / 跳过 次数 |
| Dashboard：新三国 · 卡牌平衡（选取率） | https://us.posthog.com/project/635605/dashboard/2153145 | 上面三张图归到一处 |

三张图都只统计 mod 自己的卡（`card_id LIKE 'CARD.NEWSANGUO_CARD_%'`；否则 x 轴的"去前缀"会把别的 mod 的 id 截成碎片）。

**选取率最低的 mod 卡**（候选≥300，快照 2026-09-30，`26_bottom_pick_rate_mod_only.sql` / insight ②）：

| 卡牌 | 选取率 | 被选/候选 |
|---|---|---|
| 以狼搏狗 | 2.7% | 64/2361 |
| 春秋胡言乱语！ | 2.8% | 65/2334 |
| 秦晋之好 | 2.9% | 63/2173 |
| 盖饭 | 2.9% | 67/2285 |
| 扎聋我自己的耳朵！ | 4.7% | 44/941 |
| 替身打击 | 4.7% | 46/971 |
| 原本就是我的！ | 4.9% | 113/2325 |
| 密谋 | 5.0% | 117/2351 |
| 毒鼠计 | 5.1% | 60/1178 |
| 无情剑法 | 5.4% | 128/2384 |
| 大都督到！ | 5.9% | 131/2212 |
| 天降雄兵 | 6.0% | 50/829 |
| 悍将三刀 | 6.2% | 61/987 |
| 万万没有此事啊！ | 7.2% | 165/2283 |
| 我砍你的头！ | 7.3% | 78/1062 |

（完整 25 条见本地 HTML 与 insight ②；注意高频出现（约 2,300 次）的几张牌正好都在低位，说明牌池权重与玩家偏好不匹配。）

- 定义文件：`analysis/posthog/insights/*.json`（`insight-create` / `insight-update` 的载荷，含完整 HogQL）
- 本地中文标注版：[pick_rate_chart.html](out/pick_rate_chart.html)（双击用浏览器打开）
- x 轴标签：已用 `substring(JSONExtractString(choice,'card','id'), 21, 200)` 去掉 `CARD.NEWSANGUO_CARD_` 前缀，所以图上是 `WINE_CUT` / `INTOXICATED` 这种短名。
- 为什么图上不是中文名：PostHog 的 insight 校验器只接受「函数调用」形式的表达式列，实测被拒的有
  `transform(...)`+数组字面量、`CASE WHEN ... END`、`multiIf(...)`、外层再包一层 SELECT，
  以及 `WITH names AS (...) ... LEFT JOIN names`（HogQL 不支持这一路子查询）；
  另外它的 `substring` 必须是 3 参（`substring(s, offset, length)`）。
  中文名因此只放在本文件与本地 HTML 里。

## 2. 卡牌：候选 / 被选 / 选取率（`13_card_pick_rates.sql`，249 张有数据的牌）

按「候选次数」降序（前 25 行，完整表见 `out/13_card_pick_rates.tsv`）：

| card_id | offered | picked | pick_rate | skipped | skip_rate |
|---|---|---|---|---|---|
| CARD.NEWSANGUO_CARD_WOLF_VS_DOG | 1193 | 36 | 3.0% | 1157 | 97.0% |
| CARD.NEWSANGUO_CARD_NONSENSE | 1193 | 34 | 2.8% | 1159 | 97.2% |
| CARD.NEWSANGUO_CARD_GET_OUT | 1174 | 90 | 7.7% | 1084 | 92.3% |
| CARD.NEWSANGUO_CARD_DRAGON_OMEN | 1172 | 163 | 13.9% | 1009 | 86.1% |
| CARD.NEWSANGUO_CARD_ALWAYS_MINE | 1170 | 59 | 5.0% | 1111 | 95.0% |
| CARD.NEWSANGUO_CARD_RUTHLESS_BLADE | 1163 | 58 | 5.0% | 1105 | 95.0% |
| CARD.NEWSANGUO_CARD_LIGHTNING_STRIKE | 1162 | 176 | 15.1% | 986 | 84.9% |
| CARD.NEWSANGUO_CARD_PLOT | 1153 | 63 | 5.5% | 1090 | 94.5% |
| CARD.NEWSANGUO_CARD_WINE_CUT | 1147 | 189 | 16.5% | 958 | 83.5% |
| CARD.NEWSANGUO_CARD_INTOXICATED | 1143 | 355 | 31.1% | 788 | 68.9% |
| CARD.NEWSANGUO_CARD_SLAM_THE_BOWL | 1135 | 38 | 3.3% | 1097 | 96.7% |
| CARD.NEWSANGUO_CARD_NEVER_HAPPENED | 1126 | 83 | 7.4% | 1043 | 92.6% |
| CARD.NEWSANGUO_CARD_DIVINATION | 1111 | 305 | 27.5% | 806 | 72.5% |
| CARD.NEWSANGUO_CARD_SKYWARD_BLADE | 1087 | 84 | 7.7% | 1003 | 92.3% |
| CARD.NEWSANGUO_CARD_GREAT_EVIL | 1086 | 115 | 10.6% | 971 | 89.4% |
| CARD.NEWSANGUO_CARD_STARRY_NIGHT | 1081 | 83 | 7.7% | 998 | 92.3% |
| CARD.NEWSANGUO_CARD_QIN_JIN_ALLIANCE | 1078 | 25 | 2.3% | 1053 | 97.7% |
| CARD.NEWSANGUO_CARD_CHECK_THE_PREMIERE | 1072 | 138 | 12.9% | 934 | 87.1% |
| CARD.NEWSANGUO_CARD_COMMANDER_ARRIVES | 1068 | 66 | 6.2% | 1002 | 93.8% |
| CARD.NEWSANGUO_CARD_TWEAK | 1055 | 114 | 10.8% | 941 | 89.2% |
| CARD.NEWSANGUO_CARD_RELEASE | 598 | 181 | 30.3% | 417 | 69.7% |
| CARD.NEWSANGUO_CARD_RAT_POISON | 593 | 32 | 5.4% | 561 | 94.6% |
| CARD.NEWSANGUO_CARD_HEAVEN_AND_EARTH | 585 | 45 | 7.7% | 540 | 92.3% |
| CARD.NEWSANGUO_CARD_TO_A_BIGGER_GOBLET | 573 | 186 | 32.5% | 387 | 67.5% |
| CARD.NEWSANGUO_CARD_WHERE_S_WINE | 565 | 198 | 35.0% | 367 | 65.0% |

**高选取率**（候选 ≥ 400）：哪里饮酒？ 35.0%、换大盏 32.5%、陶醉 31.1%、释怀 30.3%、酒是老英雄 29.4%、胆寒 28.8%、占卜 27.5%、风从虎 27.0%。
**低选取率**：秦晋之好 2.3%、胡言乱语 2.8%、狼与狗 3.0%、砸碗 3.3%、开溜 3.8%、代理打击 3.8%、扎聋我自己 3.9%、三刀 4.4%、原本就是我的！ 5.0%、无情之刃 5.0%。

## 3. 卡牌奖励的形态与「跳过率」（`22` / `23` / `24`）

**「候选次数」的定义**：某一局里出现过的「三选一」奖励屏，每屏 3 条 `card_choices`；把 1127 局的奖励屏全部摊平后，某张牌每出现一次就 +1。所以 Top 20 表 = **出现次数最多的 20 张牌**，柱子里拆成「被选（该屏拿了它）/ 跳过（该屏拿了别的或没拿）」。它回答两个问题：① 这张牌的选取率有多少样本支撑；② 奖励池里哪些牌最容易冒出来（池子构成/权重）。

奖励屏本身（`22_card_choice_point_shape.sql`，只统计 `card_choices` 非空的奖励点）：

| 每屏候选数 | 奖励点数 |
|---|---|
| 3（标准三选一） | 40516 |
| 6 / 7 / 4 / 5 | 4238 / 3998 / 2044 / 886 |
| 8 个以上 | 其余（含重掷/额外奖励） |

标准三选一的取舍（`23_picks_per_point.sql`，共 40591 屏）：

| 该屏拿了 | 屏数 | 占比 |
|---|---|---|
| 1 张 | 25546 | 62.9% |
| 0 张（整屏没拿） | 15013 | 37.0% |
| ≥2 张 | 32 | 0.1% |

⚠️ **重复记录**（`24_offer_duplicates.sql`，按 `request_id` 在同一局内找相同的 3 张候选组合）：

| 同组出现次数 | 其中标记"被选"的副本数 | 组数 |
|---|---|---|
| 1 | 1 | 14193 |
| 1 | 0 | 8332 |
| 2 | 2 | 2350 |
| 2 | 0 | 1150 |
| 2 | 1 | 907 |
| 3 | 3 / 0 / 2 / 1 | 730 / 266 / 244 / 225 |

结论：约 **15% 的奖励屏在同一局里被记录了 2 次以上**（引擎 run history 侧的重复；副本大多状态一致：2 副本里 2350 组两次都有被选，仅 907 组不一致）。因此：

- **「候选次数」是偏高的绝对数**（含重复副本），适合看相对大小/池子构成，别当"独立奖励屏数"用；
- **选取率（被选 ÷ 候选）大体可靠**，但因为有 1150 组"两次都没标记"的副本只加分母、不加分子，实际选取率应比表里**略高几个百分点**；
- 上一版这块写过的「45.5% 整屏没拿 / 平均 3.94 个候选」是早期查询口径不一致的结果，**以本节 37.0%（标准三选一）为准**。

## 4. 各类选择的数据覆盖（`15_choice_types.sql`）

| stats_rows | card_choices | relic_choices | potion_choices | event_choices | ancient_choice |
|---|---|---|---|---|---|
| 49251 | 26131 | 20342 | 15567 | 10475 | 3220 |

## 5. 事件/遗物选项的被提供次数（`16_event_options.sql`）

⚠️ 原版 `event_choices[]` 条目里**只有 `title`（+ 部分 `variables`），没有"是否被选"的字段**，所以这里只能统计"被提供次数"，不能算选择率（卡牌那边有 `was_picked`，所以 2、3 两张表能算）。

| 选项 | offered |
|---|---|
| NEWSANGUO_EVENT_THREE_VISITS_TO_THE_THATCHED_COTTAGE · ARRIVE | 287 |
| 同上 · FIRST_VISIT / VISIT_AGAIN | 254 |
| NEWSANGUO_EVENT_PRAGMATIST · INITIAL / SEARCH | 252 |
| SLIPPERY_BRIDGE · OVERCOME | 248 |
| THREE_VISITS · SECOND_VISIT / VISIT_AGAIN | 240 |
| **NEWSANGUO_EVENT_PRAGMATIST · AMBUSH / FIGHT** | **220** |
| SLIPPERY_BRIDGE · HOLD_ON_0 | 206 |
| NEWSANGUO_EVENT_CHENLIU_GRAND_MESS_HALL · GAIN_MAX_HP | 175 |
| THREE_VISITS · THIRD_VISIT / REMOVE_TWO_CARDS | 173 |
| PRAGMATIST · SEARCH_2 / CONTINUE | 145 |

- 三个 mod 事件（三顾茅庐 / 实践主义者 / 陈留大食堂）都排在最前面 → 出现频率符合预期。
- **值得复查**：`PRAGMATIST · AMBUSH/FIGHT` 被提供 220 次 vs `INITIAL/SEARCH` 252 次（≈87%）。盛碗虫（巨石）伏击我按 25% 概率实现，这个比例对不上，可能是 AMBUSH 页还有别的进入路径（例如 SEARCH_2 之后固定进入），也可能是概率没生效——需要再看一眼事件页流转。

## 6. 复现方式

```powershell
# 单条查询
& .\tools\posthog-sql.ps1 -SqlFile analysis\posthog\13_card_pick_rates.sql

# 批量跑并落盘到 analysis\posthog\out\<name>.tsv
& .\tools\posthog-report.ps1 -SqlFiles analysis\posthog\14_overview.sql,analysis\posthog\17_skip_rate.sql
```

凭据：`posthog-cli login` 生成的 token 复制到项目内 `.posthog-home\credentials.json`（已在 `.gitignore`）。

## 7. 还没做的

- **事件选项选择率**：需要先找到"被选中的那一个"记在哪（可能在 point 的其他字段，而不是 `event_choices[]`）。
- **切片**：按进阶（`run_ascension`）、人数（`run_player_count`）、楼层段、房间类型拆分卡牌选取率。
- **沉淀到 PostHog**：用 `insight-create` / `dashboard-create` / `notebooks-create` 把这些查询存成图表，之后在网页端直接看。
