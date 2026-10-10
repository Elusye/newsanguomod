> 本文件是 `AGENTS.md` 的分册（原编号沿用，`§4.9` 之类引用仍然有效）。根文件：`../AGENTS.md`。
> 改完代码请把新查证的事实回填本文件，**不要**回填根文件——根文件必须保持短小，否则会被工作区指令预算截断。
## 7. 数据分析（PostHog）

- 环境：项目 `635605`、dashboard `2153145`、insight `12370626`（`2Ry2k6SI`，选取率排行）。
- 工具：`tools/posthog-call.ps1`（`insight-create` / `insight-update` / `insight-query`）、`tools/posthog-sql.ps1`（`execute-sql`）、`tools/posthog-report.ps1`。
- 每次调用前需要：`$env:POSTHOG_HOME = "$env:TEMP\newsanguo-posthog"`，并把仓库 `.posthog-home/credentials.json` 复制进去。
- SQL 分析脚本与产出都在 `analysis/posthog/`（`01…26_*.sql`、`out/*.tsv`、`insights/*.json`、`report.md`、`out/pick_rate_chart.html`）。
- **已知限制（别再试）**：insight 校验器拒绝 `transform(...)`+数组字面量、`CASE WHEN`、`multiIf`（报 `expected identifier in column-alias list, got String`）、外层再包一层 SELECT（`expected ), got Keyword(From)`）、`WITH names AS (...) LEFT JOIN names`（执行期 unknown error）；`substring` 必须 3 参。**中文标签做不出来**，x 轴只能用去前缀短名（`substring(JSONExtractString(choice,'card','id'), 21, 200) AS card_label`），中文牌名只放 `report.md` 与本地 HTML。

---
