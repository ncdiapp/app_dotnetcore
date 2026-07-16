-- =============================================================================
-- 3_Tchp_Import_PomTemplate.sql  | RunId: pilot1 | D3 TemplateCode from BodyTypeName
-- Connect to: TenantDB_PLM27
-- Source: plm_live_20260602 BodyType + BodyTypeDetail
-- DefaultBaseSizeId: only if SizeRunRotateID exists in TchpSizeRunSize
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @PlmDb sysname = N'plm_live_20260602';
DECLARE @Now datetime = GETDATE();

BEGIN TRAN;

DECLARE @sql nvarchar(max);

-- Template
SET @sql = N'
SET IDENTITY_INSERT dbo.TchpPomTemplate ON;

;WITH base AS (
    SELECT
        bt.BodyTypeID,
        LTRIM(RTRIM(bt.BodyTypeName)) AS BodyTypeName,
        bt.DefaultBaseSizeDetailID
    FROM ' + QUOTENAME(@PlmDb) + N'.dbo.pdmv2kBodyType AS bt
),
norm AS (
    SELECT
        BodyTypeID,
        BodyTypeName,
        DefaultBaseSizeDetailID,
        -- D3: uppercase, non-alnum -> _, collapse, trim, left 50
        LEFT(
            REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
            UPPER(BodyTypeName)
            , N'' '', N''_'')
            , N''-'', N''_'')
            , N''/'', N''_'')
            , N''\'', N''_'')
            , N''.'', N''_'')
            , N''('', N''_'')
            , N'')'', N''_'')
            , N''__'', N''_'')
        , 50) AS TemplateCodeRaw
    FROM base
),
coded AS (
    SELECT
        BodyTypeID AS PomTemplateId,
        BodyTypeName AS TemplateName,
        -- collision append _BodyTypeID (5 templates unlikely to collide)
        CASE
            WHEN COUNT(*) OVER (PARTITION BY TemplateCodeRaw) > 1
                THEN LEFT(TemplateCodeRaw, 40) + N''_'' + CAST(BodyTypeID AS nvarchar(10))
            ELSE TemplateCodeRaw
        END AS TemplateCode,
        CASE
            WHEN EXISTS (
                SELECT 1 FROM dbo.TchpSizeRunSize s
                WHERE s.SizeRunSizeId = DefaultBaseSizeDetailID
            ) THEN DefaultBaseSizeDetailID
            ELSE NULL
        END AS DefaultBaseSizeId,
        CAST(1 AS bit) AS IsActive
    FROM norm
)
MERGE dbo.TchpPomTemplate AS t
USING coded AS s
ON t.PomTemplateId = s.PomTemplateId
WHEN MATCHED THEN UPDATE SET
    TemplateCode = s.TemplateCode,
    TemplateName = s.TemplateName,
    DefaultBaseSizeId = s.DefaultBaseSizeId,
    IsActive = s.IsActive,
    AppModifiedDate = @Now
WHEN NOT MATCHED BY TARGET THEN INSERT
    (PomTemplateId, TemplateCode, TemplateName, DefaultBaseSizeId, IsActive, AppCreatedDate, AppModifiedDate)
    VALUES (s.PomTemplateId, s.TemplateCode, s.TemplateName, s.DefaultBaseSizeId, s.IsActive, @Now, @Now);

SET IDENTITY_INSERT dbo.TchpPomTemplate OFF;
';
EXEC sp_executesql @sql, N'@Now datetime', @Now = @Now;

DECLARE @n1 int = (SELECT COUNT(*) FROM dbo.TchpPomTemplate);
PRINT 'TchpPomTemplate rows: ' + CAST(@n1 AS nvarchar(20));

-- Parts
SET @sql = N'
SET IDENTITY_INSERT dbo.TchpPomTemplatePart ON;

MERGE dbo.TchpPomTemplatePart AS t
USING (
    SELECT
        d.BodyTypeDetailID AS PomTemplatePartId,
        d.BodyTypeID AS PomTemplateId,
        d.BodyPartID AS BodyPartId,
        LEFT(d.BodypartAliasName, 50) AS BodypartAliasName,
        ISNULL(d.Sort, 0) AS Sort
    FROM ' + QUOTENAME(@PlmDb) + N'.dbo.pdmV2kBodyTypeDetail AS d
    INNER JOIN dbo.TchpPomTemplate tmp ON tmp.PomTemplateId = d.BodyTypeID
    INNER JOIN dbo.TchpBodyPart bp ON bp.BodyPartId = d.BodyPartID
) AS s
ON t.PomTemplatePartId = s.PomTemplatePartId
WHEN MATCHED THEN UPDATE SET
    PomTemplateId = s.PomTemplateId,
    BodyPartId = s.BodyPartId,
    BodypartAliasName = s.BodypartAliasName,
    Sort = s.Sort,
    AppModifiedDate = @Now
WHEN NOT MATCHED BY TARGET THEN INSERT
    (PomTemplatePartId, PomTemplateId, BodyPartId, BodypartAliasName, Sort, AppCreatedDate, AppModifiedDate)
    VALUES (s.PomTemplatePartId, s.PomTemplateId, s.BodyPartId, s.BodypartAliasName, s.Sort, @Now, @Now);

SET IDENTITY_INSERT dbo.TchpPomTemplatePart OFF;
';
EXEC sp_executesql @sql, N'@Now datetime', @Now = @Now;

DECLARE @n2 int = (SELECT COUNT(*) FROM dbo.TchpPomTemplatePart);
PRINT 'TchpPomTemplatePart rows: ' + CAST(@n2 AS nvarchar(20));

COMMIT TRAN;
PRINT '3_Tchp_Import_PomTemplate.sql DONE';
GO
