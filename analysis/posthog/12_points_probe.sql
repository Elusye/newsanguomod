-- 12: verify the two-level array extraction (map_point_history is [act][point])
SELECT
    count() AS points,
    countIf(length(JSONExtractRaw(point, 'player_stats')) > 0) AS has_player_stats,
    max(length(JSONExtractRaw(point, 'player_stats'))) AS max_stats_len
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
