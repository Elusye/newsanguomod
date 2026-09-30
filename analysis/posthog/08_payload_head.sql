-- 08: literal head of one telemetry payload (to see the real nesting/encoding)
SELECT
    substring(ifNull(properties.payload, ''), 1, 700) AS payload_head
FROM events
WHERE event = 'run_history.completed'
LIMIT 1
