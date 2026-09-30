-- 18: literal head of one event_choices entry (to find the chosen-flag field name)
SELECT
    substring(event_choice, 1, 300) AS head
FROM (
    SELECT arrayJoin(JSONExtractArrayRaw(stats, 'event_choices')) AS event_choice
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
LIMIT 3
