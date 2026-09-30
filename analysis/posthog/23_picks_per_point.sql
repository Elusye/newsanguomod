-- 23: 在「恰好 3 个候选」的奖励点里，有几张被选（taken=1 正常，taken=0 = 整屏没拿）
SELECT
    taken,
    count() AS points
FROM (
    SELECT arrayCount(x -> JSONExtractBool(x, 'was_picked'), choices) AS taken
    FROM (
        SELECT JSONExtractArrayRaw(stats, 'card_choices') AS choices
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
        WHERE length(choices) = 3
    )
)
GROUP BY taken
ORDER BY taken
