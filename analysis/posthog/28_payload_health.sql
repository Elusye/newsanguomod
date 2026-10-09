-- 28: payload health -- do the deep run_history paths (and the version fields) still match the 2026-09-30 assumptions?
SELECT
    properties.game_version AS game_version,
    properties.ritsulib_version AS ritsulib_version,
    properties.mod_version AS mod_version,
    count() AS runs,
    countIf(JSONExtractString(ifNull(properties.payload, ''), 'applicant_payload') != '') AS has_payload,
    countIf(JSONExtractArrayRaw(JSONExtractRaw(JSONExtractString(ifNull(properties.payload, ''), 'applicant_payload'), 'run_history'), 'map_point_history') != []) AS has_map_points,
    uniqExact(properties.anonymous_install_id) AS installs,
    min(timestamp) AS first_seen,
    max(timestamp) AS last_seen
FROM events
WHERE event = 'run_history.completed'
GROUP BY game_version, ritsulib_version, mod_version
ORDER BY runs DESC
LIMIT 40
