-- 17: how often a whole card-reward screen is skipped (nothing picked)
SELECT
    count() AS reward_points,
    countIf(taken = 0) AS all_skipped_points,
    round(100.0 * countIf(taken = 0) / count(), 1) AS all_skipped_pct,
    round(avg(offers), 2) AS avg_offers_per_point
FROM (
    SELECT
        length(card_choices) AS offers,
        arrayCount(x -> JSONExtractBool(x, 'was_picked'), card_choices) AS taken
    FROM (
        SELECT JSONExtractArrayRaw(stats, 'card_choices') AS card_choices
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
        WHERE length(card_choices) > 0
    )
)
