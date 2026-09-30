-- 01: inventory of events in this PostHog project (verifies the mod telemetry actually landed)
SELECT
    event,
    count() AS events,
    min(timestamp) AS first_seen,
    max(timestamp) AS last_seen,
    uniqExact(distinct_id) AS distinct_ids
FROM events
WHERE timestamp > now() - INTERVAL 30 DAY
GROUP BY event
ORDER BY events DESC
LIMIT 50
