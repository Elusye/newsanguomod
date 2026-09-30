-- 13: card reward offers vs picks, per card (the main "card pick rate" table)
SELECT
    JSONExtractString(choice, 'card', 'id') AS card_id,
    count() AS offered,
    countIf(JSONExtractBool(choice, 'was_picked')) AS picked,
    round(100.0 * countIf(JSONExtractBool(choice, 'was_picked')) / count(), 1) AS pick_rate_pct,
    countIf(NOT JSONExtractBool(choice, 'was_picked')) AS skipped,
    round(100.0 * countIf(NOT JSONExtractBool(choice, 'was_picked')) / count(), 1) AS skip_rate_pct
FROM (
    SELECT arrayJoin(JSONExtractArrayRaw(stats, 'card_choices')) AS choice
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
)
WHERE card_id != ''
GROUP BY card_id
ORDER BY offered DESC
LIMIT 300
