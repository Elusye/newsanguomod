-- 24: 同一局(按 request_id)内同一组 3 张候选出现几次，以及其中几次标记了"被选"
SELECT
    copies_all,
    copies_taken,
    count() AS offer_groups
FROM (
    SELECT
        rid,
        sig,
        count() AS copies_all,
        countIf(taken > 0) AS copies_taken
    FROM (
        SELECT
            rid,
            arrayStringConcat(arraySort(arrayMap(x -> JSONExtractString(x, 'card', 'id'), choices)), '|') AS sig,
            arrayCount(x -> JSONExtractBool(x, 'was_picked'), choices) AS taken
        FROM (
            SELECT rid, JSONExtractArrayRaw(stats, 'card_choices') AS choices
            FROM (
                SELECT rid, arrayJoin(JSONExtractArrayRaw(point, 'player_stats')) AS stats
                FROM (
                    SELECT rid, arrayJoin(JSONExtractArrayRaw(act_points)) AS point
                    FROM (
                        SELECT
                            properties.request_id AS rid,
                            arrayJoin(
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
            WHERE length(choices) = 3
        )
    )
    GROUP BY rid, sig
)
GROUP BY copies_all, copies_taken
ORDER BY offer_groups DESC
LIMIT 12
