-- 22: 每次卡牌奖励点里到底记了几条候选、其中有多少个不同卡牌 id
SELECT
    offers_in_point,
    uniq_ids,
    count() AS points
FROM (
    SELECT
        length(card_choices) AS offers_in_point,
        arrayUniq(arrayMap(x -> JSONExtractString(x, 'card', 'id'), card_choices)) AS uniq_ids
    FROM (
        SELECT JSONExtractArrayRaw(stats, 'card_choices') AS card_choices
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
    WHERE length(card_choices) > 0
)
GROUP BY offers_in_point, uniq_ids
ORDER BY points DESC
LIMIT 20
