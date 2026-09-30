-- 02: which property keys exist on our telemetry event (shape probe)
SELECT
    arrayJoin(JSONExtractKeys(properties)) AS property_key,
    count() AS events
FROM events
WHERE event = 'run_history.completed'
GROUP BY property_key
ORDER BY events DESC
LIMIT 40
