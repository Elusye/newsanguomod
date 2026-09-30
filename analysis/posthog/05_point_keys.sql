-- 05: keys of one map_point_history entry
SELECT
    arrayJoin(JSONExtractKeys(point)) AS point_key,
    count() AS n
FROM (
    SELECT arrayJoin(
        JSONExtractArrayRaw(
            JSONExtractRaw(
                JSONExtractString(ifNull(properties.payload, ''), 'applicant_payload'),
                'run_history'
            ),
            'map_point_history'
        )
    ) AS point
    FROM events
    WHERE event = 'run_history.completed'
)
GROUP BY point_key
ORDER BY n DESC
LIMIT 40
