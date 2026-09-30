-- 07: keys of one card_choices entry (the card-reward offer/pick records)
SELECT
    arrayJoin(JSONExtractKeys(choice)) AS choice_key,
    count() AS n
FROM (
    SELECT arrayJoin(JSONExtractArrayRaw(stats, 'card_choices')) AS choice
    FROM (
        SELECT arrayJoin(JSONExtractArrayRaw(point, 'player_stats')) AS stats
        FROM (
            SELECT arrayJoin(
                arrayFlatten(
                    JSONExtractArrayRaw(
                        JSONExtractRaw(
                            JSONExtractString(ifNull(properties.payload, ''), 'applicant_payload'),
                            'run_history'
                        ),
                        'map_point_history'
                    )
                )
            ) AS point
            FROM events
            WHERE event = 'run_history.completed'
        )
    )
)
GROUP BY choice_key
ORDER BY n DESC
LIMIT 40
