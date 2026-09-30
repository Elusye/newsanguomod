-- 25: 选取率最低的牌（候选 >= 300，按选取率升序）
SELECT
    JSONExtractString(choice, 'card', 'id') AS card_id,
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
WHERE card_id != ''
GROUP BY card_id
HAVING offered >= 300
ORDER BY pick_rate_pct ASC, offered DESC
LIMIT 25
