-- 26: 新三国卡牌里选取率最低的（候选 >= 300，按选取率升序；只保留 mod 自己的卡）
SELECT
    substring(JSONExtractString(choice, 'card', 'id'), 21, 200) AS card_label,
    count() AS offered,
    countIf(JSONExtractBool(choice, 'was_picked')) AS picked,
    round(100.0 * countIf(JSONExtractBool(choice, 'was_picked')) / count(), 1) AS pick_rate_pct
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
WHERE card_label != ''
GROUP BY card_label
HAVING offered >= 300
ORDER BY pick_rate_pct ASC, offered DESC
LIMIT 20
