SELECT 
    p.Name AS PlatformName,
    w.Id,
    w.PlatformId,
    w.Name AS WellName,
    w.Code AS WellCode,
    w.CreatedAt AS [Created/At],
    w.UpdatedAt AS [Updated/At]
FROM 
    Wells w
INNER JOIN 
    Platforms p ON w.PlatformId = p.Id
WHERE 
    w.UpdatedAt = (
        SELECT MAX(w2.UpdatedAt)
        FROM Wells w2
        WHERE w2.PlatformId = w.PlatformId
    )
ORDER BY 
    p.Id;