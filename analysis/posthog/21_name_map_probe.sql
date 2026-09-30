WITH names AS (
    SELECT 'CARD.NEWSANGUO_CARD_NEW_GAME_PLUS' AS id, '二周目玩家' AS label
    UNION ALL SELECT 'CARD.NEWSANGUO_CARD_FEEL_NO_ACID', '咱家不怕酸！'
    UNION ALL SELECT 'CARD.NEWSANGUO_CARD_BREW_LIMIT_BREAK', '突破酒限'
    UNION ALL SELECT 'CARD.NEWSANGUO_CARD_GOLDEN_REBELLION', '黄金起义'
    UNION ALL SELECT 'CARD.NEWSANGUO_CARD_VICTORY_BY_HEAVENS_WILL', '天意致胜'
    UNION ALL SELECT 'CARD.NEWSANGUO_CARD_MY_THREE_GENERALS', '是我的韩信，白起，周亚夫'
    UNION ALL SELECT 'CARD.NEWSANGUO_CARD_CROSS_FOR_CROSS', '他过江我也过江！'
    UNION ALL SELECT 'CARD.NEWSANGUO_CARD_TEN_THOUSAND_TRANSPARENT_HOLES', '一万个透明窟窿！'
    UNION ALL SELECT 'CARD.NEWSANGUO_CARD_WINE_THE_OLD_HERO', '酒是老英雄'
    UNION ALL SELECT 'CARD.NEWSANGUO_CARD_INVOKE_HEAVEN', '召唤天意'
    UNION ALL SELECT 'CARD.NEWSANGUO_CARD_WHERE_S_WINE', '哪里饮酒？'
    UNION ALL SELECT 'CARD.NEWSANGUO_CARD_RETIRE', '告老还乡'
)
SELECT
    ifNull(names.label, substring(JSONExtractString(choice, 'card', 'id'), 21, 200)) AS card_label,
    count() AS offered,
    countIf(JSONExtractBool(choice, 'was_picked')) AS picked,
    round(countIf(JSONExtractBool(choice, 'was_picked')) / count(), 4) AS pick_rate
FROM (
    SELECT arrayJoin(JSONExtractArrayRaw(stats, 'card_choices')) AS choice
    FROM (
        SELECT arrayJoin(JSONExtractArrayRaw(point, 'player_stats')) AS stats
        FROM (
            SELECT arrayJoin(JSONExtractArrayRaw(act_points)) AS point
            FROM (
                SELECT arrayJoin(
                    JSONExtractArrayRaw(
                        JSONExtractRaw(
                            JSONExtractString(ifNull(properties.payload, ''), 'applicant_payload'),
                            'run_history'
                        ),
                        'map_point_history'
                    )
                ) AS act_points
                FROM events
                WHERE event = 'run_history.completed'
            )
        )
    )
)
LEFT JOIN names ON JSONExtractString(choice, 'card', 'id') = names.id
WHERE card_label != ''
GROUP BY card_label
HAVING offered >= 300
ORDER BY pick_rate DESC, offered DESC
LIMIT 12
