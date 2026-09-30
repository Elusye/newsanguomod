-- 14: run-level overview (the "大盘" table)
SELECT
    count() AS runs,
    uniqExact(properties.anonymous_install_id) AS installs,
    countIf(toString(properties.is_victory) = 'true') AS wins,
    round(100.0 * countIf(toString(properties.is_victory) = 'true') / count(), 2) AS win_rate_pct,
    round(avg(toFloatOrZero(properties.run_floor_reached)), 1) AS avg_floor,
    quantile(0.5)(toFloatOrZero(properties.run_floor_reached)) AS median_floor,
    max(toFloatOrZero(properties.run_floor_reached)) AS max_floor,
    round(avg(toFloatOrZero(properties.run_time_seconds)) / 60.0, 1) AS avg_minutes,
    round(avg(toFloatOrZero(properties.run_player_count)), 2) AS avg_players,
    round(avg(toFloatOrZero(properties.run_ascension)), 1) AS avg_ascension,
    uniqExact(properties.game_version) AS game_versions,
    uniqExact(properties.ritsulib_version) AS ritsulib_versions,
    uniqExact(properties.game_language) AS languages
FROM events
WHERE event = 'run_history.completed'
