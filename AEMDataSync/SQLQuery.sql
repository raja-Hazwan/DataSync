WITH LastWellPerPlatform AS (
    SELECT 
        PlatformId,
        MAX(UpdatedAt) AS MaxUpdatedAt
    FROM 
        Wells 
    GROUP BY 
        PlatformId
)
SELECT 
    p.Name AS PlatformName,
    w.Id,
    w.PlatformId,
    w.Name AS UniqueName,
    w.Latitude,
    w.Longitude,
    w.CreatedAt,
    w.UpdatedAt
FROM 
    Platforms p
INNER JOIN 
    Wells w ON p.Id = w.PlatformId
INNER JOIN 
    LastWellPerPlatform lwp ON w.PlatformId = lwp.PlatformId 
    AND w.UpdatedAt = lwp.MaxUpdatedAt
ORDER BY 
    w.Id;