-- 27: data window probe -- per-day run_history volume, to see how much new telemetry accumulated
SELECT
    toDate(timestamp) AS day,
    count() AS runs,
    uniqExact(properties.anonymous_install_id) AS installs,
    round(avg(toFloatOrZero(properties.run_floor_reached)), 1) AS avg_floor,
    round(100.0 * countIf(toString(properties.is_victory) = 'true') / count(), 1) AS win_rate_pct
FROM events
WHERE event = 'run_history.completed'
GROUP BY day
ORDER BY day DESC
LIMIT 60
