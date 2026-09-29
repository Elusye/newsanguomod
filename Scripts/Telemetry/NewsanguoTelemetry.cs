using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;

namespace newsanguo.Scripts.Telemetry;

/// <summary>
/// 遥测（**大盘层**）：只申请 <c>run_history</c> —— 已结束跑局的原版 run-history JSON。
///
/// 为什么这一层就够“大盘”用：原版已经把「候选卡 + 是否被选」直接写进了跑局历史
/// （CardReward.cs:294-332：选中的记 <c>was_picked: true</c>、其余候选与“跳过/重掷”记 false；
/// 结构见 CardChoiceHistoryEntry{ card, was_picked }，挂在 PlayerMapPointHistoryEntry.CardChoices），
/// 同一条记录里还带着 CardsGained / RelicChoices / PotionChoices / CardsRemoved / CardsEnchanted /
/// CardsTransformed / UpgradedCards / EventChoices / RestSiteChoices / 金币与血量收支等字段 ——
/// 卡牌选取率、跳过率、重掷、遗物/药水/事件选择倾向、拿了某张牌之后的胜率，都能在**后端**直接聚合，
/// 本 mod 不需要逐点埋点。我们自己的卡（NEWSANGUO_CARD_*）与事件选项也走同一套记录，所以同样在内。
///
/// <b>后端 = PostHog</b>（RitsuLib 只提供发送端，PostHog 负责收）：
///  · 一次跑局 = 一个事件 <c>run_history.completed</c>；RitsuLib 在跑局结束后会**立即 flush**，
///    因此每个 HTTP 批次通常只有 1 条事件（不会出现“上千条大 JSON 挤一个请求”的情况）。
///  · <c>distinct_id</c> = RitsuLib 生成的**匿名安装 ID**；跑局 JSON 里实测也没有账号标识
///    （没有 Steam64 id，只有槽位号 net_id:1 与 platform_type:"steam"）。仍属可撤销的 opt-in 授权。
///  · 事件属性（RitsuLib 自动带上）：<c>capture_source</c>=run_ended、<c>is_victory</c>、<c>is_abandoned</c>、
///    <c>run_game_mode</c>、<c>run_is_daily</c>、<c>run_player_count</c>、<c>run_floor_reached</c>、
///    <c>run_ascension</c>、<c>run_time_seconds</c>、<c>run_win_time_seconds</c>、<c>run_reload_count</c>、
///    <c>run_character_ids</c>，以及适配器补的 schema/applicant_id/request_id/category。
///  · 跑局本体在 <c>properties.payload.run_history</c>；PostHog 侧用 HogQL/SQL 展开
///    <c>…map_point_history[].player_stats[].card_choices[]</c>（每项 <c>{ card.id, was_picked }</c>）即可算选取率，
///    或把 payload 经 data warehouse / 批量导出转到仓库里算。
///
/// 已知限制（大盘层固有，与本实现无关）：
///  · 只有**已结束**的跑局才会上传（<c>RunEndedEvent</c>）→ 打开游戏就退出的玩家没有数据；
///    弃局默认也采集（<c>is_abandoned</c> 可用于过滤），若要完全不采集弃局，给 RunHistory 传
///    <c>captureFilter: evt =&gt; !evt.IsAbandoned</c>（一行，但会进一步缩小样本）。
///  · 必须玩家 opt-in，样本偏小且非随机（早期尤其明显）。
///  · 本 mod 自定义的选择界面（「黄金起义」三选一、天意的「顺应天意 / 竟然不许！」等）走的是
///    <c>CardSelectCmd</c>，原版历史**不记录** —— 要统计它们得另加 Custom 事件（本层未做）。
/// </summary>
public static class NewsanguoTelemetry
{
    /// <summary>
    /// PostHog 项目 API Key。以 <c>phc_</c> 开头的是**客户端可写入的公开 ingest key**，
    /// 嵌进 mod/DLL 属于正常用法（它只能写入事件，不能读数据）；留空 = 完全不注册遥测。
    /// </summary>
    private const string PostHogProjectToken = "phc_y4o6gDYy4z3psStkKCkAHXkoa2PHwMrSsfVacbqrtCAj";

    /// <summary>
    /// PostHog 区域主机：美国云 <c>https://us.i.posthog.com</c>、欧洲云 <c>https://eu.i.posthog.com</c>、
    /// 自建则填自己的域名。区域填错的表现是 PostHog 返回 401 / 无效 key，客户端日志里会记发送失败。
    /// </summary>
    private const string PostHogHost = "https://us.i.posthog.com";

    /// <summary>在 mod 初始化时调用一次（见 <see cref="Entry.Init"/>）。</summary>
    public static void Register()
    {
        if (string.IsNullOrWhiteSpace(PostHogProjectToken))
        {
            Entry.Logger.Info("[Telemetry] 未配置 PostHog Project Token，跳过遥测注册（玩家不会看到授权请求）");
            return;
        }

        TelemetryRegistry.RegisterApplicant(new TelemetryApplicant
        {
            ApplicantId = Entry.ModId,
            OwnerModId = Entry.ModId,
            DisplayName = "新三国",
            DisplayNameText = ModSettingsText.Literal("新三国"),
            Adapter = new PostHogTelemetryAdapter(PostHogHost, PostHogProjectToken),
            Requests =
            [
                // 授权说明按 RitsuLib 的要求写清「数据类别 + 用途 + 接收方」，不写空话
                TelemetryRequest.RunHistory(
                    ModSettingsText.Literal(
                        "上传已结束跑局的原版 run-history，用来分析平衡性与卡牌选取率："
                        + "每个地图点的候选卡与最终选择、遗物/药水/事件的选择、卡牌升级与附魔、金币与血量变化。"
                        + "数据发送到 PostHog（第三方分析服务），只带匿名安装 ID；"
                        + "不会上传昵称、账号、Steam ID 或存档路径。可随时在设置里撤回授权。"
                        + "  Uploads the vanilla run-history of finished runs (card choices, relic/potion/event picks, "
                        + "upgrades, enchants, gold and HP changes) to PostHog (third-party analytics) under an "
                        + "anonymous install ID, to analyze balance and card pick rates. "
                        + "No nickname, account, Steam ID or save paths. Consent can be revoked at any time."),
                    []),
            ],
        });

        Entry.Logger.Info($"[Telemetry] 已注册遥测申请方（仅 run_history）→ PostHog {PostHogHost}");
    }
}
