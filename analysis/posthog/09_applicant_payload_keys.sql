-- 09: keys of payload.applicant_payload, treating it as a JSON string if needed
SELECT
    arrayJoin(
        JSONExtractKeys(
            ifNull(JSONExtractString(ifNull(properties.payload, ''), 'applicant_payload'), '')
        )
    ) AS k,
    count() AS n
FROM events
WHERE event = 'run_history.completed'
GROUP BY k
ORDER BY n DESC
LIMIT 40
