-- 16: how often each event/ancient/relic option is OFFERED.
-- NOTE: vanilla run-history `event_choices[]` entries only carry `title` (+ `variables`) -- there is
-- no chosen-flag field, so choose-rates cannot be computed from this array. (Card rewards do carry
-- `was_picked`, which is why 13/17 can compute pick/skip rates.)
SELECT
    JSONExtractString(choice, 'title', 'table') AS loc_table,
    JSONExtractString(choice, 'title', 'key') AS option_key,
    count() AS offered
FROM (
    SELECT arrayJoin(JSONExtractArrayRaw(stats, 'event_choices')) AS choice
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
WHERE option_key != ''
GROUP BY loc_table, option_key
ORDER BY offered DESC
LIMIT 200
