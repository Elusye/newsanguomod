-- 04: keys inside payload.applicant_payload.run_history
SELECT
    arrayJoin(
        JSONExtractKeys(
            JSONExtractRaw(
                JSONExtractString(ifNull(properties.payload, ''), 'applicant_payload'),
                'run_history'
            )
        )
    ) AS run_history_key,
    count() AS n
FROM events
WHERE event = 'run_history.completed'
GROUP BY run_history_key
ORDER BY n DESC
LIMIT 40
