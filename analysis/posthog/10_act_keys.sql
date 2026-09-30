-- 10: keys of one run_history.acts[] entry
SELECT
    arrayJoin(JSONExtractKeys(act)) AS act_key,
    count() AS n
FROM (
    SELECT arrayJoin(
        JSONExtractArrayRaw(
            JSONExtractRaw(
                JSONExtractString(ifNull(properties.payload, ''), 'applicant_payload'),
                'run_history'
            ),
            'acts'
        )
    ) AS act
    FROM events
    WHERE event = 'run_history.completed'
)
GROUP BY act_key
ORDER BY n DESC
LIMIT 40
