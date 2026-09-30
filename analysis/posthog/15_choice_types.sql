-- 15: which choice types exist per player_stats row (card / event / relic / potion / ancient)
SELECT
    count() AS stats_rows,
    countIf(length(JSONExtractRaw(stats, 'card_choices')) > 0) AS with_card_choices,
    countIf(length(JSONExtractRaw(stats, 'event_choices')) > 0) AS with_event_choices,
    countIf(length(JSONExtractRaw(stats, 'relic_choices')) > 0) AS with_relic_choices,
    countIf(length(JSONExtractRaw(stats, 'ancient_choice')) > 0) AS with_ancient_choice,
    countIf(length(JSONExtractRaw(stats, 'potion_choices')) > 0) AS with_potion_choices
FROM (
    SELECT arrayJoin(JSONExtractArrayRaw(point, 'player_stats')) AS stats
    FROM (
        SELECT arrayJoin(JSONExtractArrayRaw(act_points)) AS point
        FROM (
            SELECT arrayJoin(
                JSONExtractArrayRaw(
                    JSONExtractRaw(
                        JSONExtractString(ifNull(properties.payload, ''), 'applicant_payload'),
                        'run_history'
                    ),
                    'map_point_history'
                )
            ) AS act_points
            FROM events
            WHERE event = 'run_history.completed'
        )
    )
)
