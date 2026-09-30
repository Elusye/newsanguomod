-- 11: shape of map_point_history (array? object? string?)
SELECT
    length(JSONExtractRaw(base, 'map_point_history')) AS raw_len,
    substring(JSONExtractRaw(base, 'map_point_history'), 1, 420) AS head
FROM (
    SELECT JSONExtractRaw(
        JSONExtractString(ifNull(properties.payload, ''), 'applicant_payload'),
        'run_history'
    ) AS base
    FROM events
    WHERE event = 'run_history.completed'
    LIMIT 1
)
