-- 06: keys of one player_stats entry inside a map point
-- map_point_history is [act][point], so flatten before joining.
SELECT
    arrayJoin(JSONExtractKeys(stats)) AS stats_key,
    count() AS n
FROM (
    SELECT arrayJoin(JSONExtractArrayRaw(point, 'player_stats')) AS stats
    FROM (
        SELECT arrayJoin(
            arrayFlatten(
                JSONExtractArrayRaw(
                    JSONExtractRaw(
                        JSONExtractString(ifNull(properties.payload, ''), 'applicant_payload'),
                        'run_history'
                    ),
                    'map_point_history'
                )
            )
        ) AS point
        FROM events
        WHERE event = 'run_history.completed'
    )
)
GROUP BY stats_key
ORDER BY n DESC
LIMIT 40
