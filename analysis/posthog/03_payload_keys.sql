-- 03: top-level keys of the telemetry payload
SELECT
    arrayJoin(JSONExtractKeys(ifNull(properties.payload, ''))) AS payload_key,
    count() AS n
FROM events
WHERE event = 'run_history.completed'
GROUP BY payload_key
ORDER BY n DESC
LIMIT 20
