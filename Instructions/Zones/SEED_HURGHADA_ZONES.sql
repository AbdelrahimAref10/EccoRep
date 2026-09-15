/*
  Seed: Hurghada zone group + English zone names + empty directed rate matrix
  Source: OpenStreetMap Nominatim (WGS84) — same datum as Google Maps
  Run AFTER the ZoneCycle migration. Idempotent: safe to re-run.

  Verify a point in Google Maps:
    https://www.google.com/maps?q=27.2537339,33.8246227
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @By nvarchar(256) = N'seed-hurghada';
DECLARE @GroupName nvarchar(256) = N'Hurghada';
DECLARE @GroupId int;

SELECT TOP (1) @GroupId = ZoneGroupId
FROM dbo.VO_ZoneGroup
WHERE [Name] IN (N'Hurghada', N'الغردقة')
ORDER BY CASE WHEN [Name] = N'Hurghada' THEN 0 ELSE 1 END;

IF @GroupId IS NULL
BEGIN
    INSERT INTO dbo.VO_ZoneGroup ([Name], IsActive, CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate)
    VALUES (@GroupName, 1, @By, @Now, @By, @Now);

    SELECT @GroupId = ZoneGroupId
    FROM dbo.VO_ZoneGroup
    WHERE [Name] = @GroupName;
END
ELSE
BEGIN
    UPDATE dbo.VO_ZoneGroup
    SET [Name] = @GroupName,
        IsActive = 1,
        LastModifiedBy = @By,
        LastModifiedDate = @Now
    WHERE ZoneGroupId = @GroupId;
END

IF @GroupId IS NULL
BEGIN
    RAISERROR(N'Failed to resolve zone group Hurghada.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- Rename Arabic names from a previous seed so MERGE does not duplicate them
UPDATE dbo.VO_Zone SET [Name] = N'El Gouna',                 LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'الجونة';
UPDATE dbo.VO_Zone SET [Name] = N'El Ahyaa',                 LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'الأحياء';
UPDATE dbo.VO_Zone SET [Name] = N'Al Helal',                 LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'الهلال';
UPDATE dbo.VO_Zone SET [Name] = N'El Dahar',                 LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'الدهار';
UPDATE dbo.VO_Zone SET [Name] = N'El Sakala',                LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'السقالة';
UPDATE dbo.VO_Zone SET [Name] = N'Marina',                   LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'المارينا';
UPDATE dbo.VO_Zone SET [Name] = N'El Hadaba',                LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'الهضبة';
UPDATE dbo.VO_Zone SET [Name] = N'El Kawthar',               LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'الكوثر';
UPDATE dbo.VO_Zone SET [Name] = N'El Mamsha (Sheraton Rd)',  LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'الممشى (شارع شيراتون)';
UPDATE dbo.VO_Zone SET [Name] = N'Touristic Villages',       LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'القرى السياحية';
UPDATE dbo.VO_Zone SET [Name] = N'Airport',                  LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'المطار';
UPDATE dbo.VO_Zone SET [Name] = N'Intercontinental',         LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'الإنتركونتيننتال';
UPDATE dbo.VO_Zone SET [Name] = N'Magawish',                 LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'المغاويش';
UPDATE dbo.VO_Zone SET [Name] = N'Sahl Hasheesh',            LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'سهل حشيش';
UPDATE dbo.VO_Zone SET [Name] = N'Makadi Bay',               LastModifiedBy = @By, LastModifiedDate = @Now WHERE ZoneGroupId = @GroupId AND [Name] = N'خليج مكادي';

DECLARE @Zones TABLE
(
    SortOrder int NOT NULL,
    [Name] nvarchar(256) NOT NULL PRIMARY KEY,
    Latitude float NOT NULL,
    Longitude float NOT NULL
);

INSERT INTO @Zones (SortOrder, [Name], Latitude, Longitude)
VALUES
-- North → South along the Red Sea coast
( 1, N'El Gouna',                27.3989266, 33.6741556),
( 2, N'El Ahyaa',                27.2887760, 33.7616132),
( 3, N'Al Helal',                27.2572310, 33.8069326),
( 4, N'El Dahar',                27.2537339, 33.8246227),
( 5, N'El Sakala',               27.2367258, 33.8405717),
( 6, N'Marina',                  27.2261148, 33.8425408),
( 7, N'El Hadaba',               27.2150867, 33.8310967),
( 8, N'El Kawthar',              27.1965711, 33.8268908),
( 9, N'El Mamsha (Sheraton Rd)', 27.1983750, 33.8491422),
(10, N'Touristic Villages',      27.1774397, 33.8236420),
(11, N'Airport',                 27.1799951, 33.7883992),
(12, N'Intercontinental',        27.1529233, 33.8255366),
(13, N'Magawish',                27.1414845, 33.8183404),
(14, N'Sahl Hasheesh',           27.0484448, 33.8868439),
(15, N'Makadi Bay',              26.9793002, 33.9077007);

MERGE dbo.VO_Zone AS t
USING @Zones AS s
    ON t.ZoneGroupId = @GroupId
   AND t.[Name] = s.[Name]
WHEN MATCHED THEN
    UPDATE SET
        t.Latitude = s.Latitude,
        t.Longitude = s.Longitude,
        t.IsActive = 1,
        t.LastModifiedBy = @By,
        t.LastModifiedDate = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ZoneGroupId, [Name], Latitude, Longitude, IsActive, CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate)
    VALUES (@GroupId, s.[Name], s.Latitude, s.Longitude, 1, @By, @Now, @By, @Now);

INSERT INTO dbo.VO_ZoneDeliveryRate
    (ZoneGroupId, FromZoneId, ToZoneId, Fee, CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate)
SELECT
    @GroupId,
    f.ZoneId,
    t.ZoneId,
    CAST(0 AS decimal(18, 2)),
    @By,
    @Now,
    @By,
    @Now
FROM dbo.VO_Zone AS f
INNER JOIN dbo.VO_Zone AS t
    ON t.ZoneGroupId = f.ZoneGroupId
WHERE f.ZoneGroupId = @GroupId
  AND f.IsActive = 1
  AND t.IsActive = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.VO_ZoneDeliveryRate AS r
      WHERE r.FromZoneId = f.ZoneId
        AND r.ToZoneId = t.ZoneId
  );

COMMIT TRANSACTION;

SELECT
    g.ZoneGroupId,
    g.[Name] AS ZoneGroupName,
    z.ZoneId,
    z.[Name] AS ZoneName,
    z.Latitude,
    z.Longitude
FROM dbo.VO_ZoneGroup AS g
INNER JOIN dbo.VO_Zone AS z ON z.ZoneGroupId = g.ZoneGroupId
WHERE g.ZoneGroupId = @GroupId
ORDER BY z.[Name];
