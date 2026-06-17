-- ============================================================
-- V003: System and Tenant Settings tables
--
-- AppSystemSetting  -> lives in AppMasterDB (run once, global)
-- AppTenantSetting  -> lives in each AppTenantDB_{companyId}
--
-- SetupCode in both tables uses the enum member NAME (e.g.
-- "SystemEmailFromAddress"), matching the existing AppSetup scheme.
--
-- Data is migrated from AppSetup where present.
-- AppSetup is NOT dropped here — remove it in a later migration
-- once all callers have been updated to use the new BL classes.
-- ============================================================

-- ============================================================
-- 1. AppSystemSetting  (run this script against AppMasterDB)
-- ============================================================
IF OBJECT_ID('dbo.AppSystemSetting', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppSystemSetting
    (
        SetupId       INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppSystemSetting PRIMARY KEY,
        SetupCode     VARCHAR(100)   NOT NULL,
        SetupValue    NVARCHAR(MAX)  NULL,
        Description   NVARCHAR(500)  NULL,
        CONSTRAINT UQ_AppSystemSetting_SetupCode UNIQUE (SetupCode)
    );
    PRINT 'V003: AppSystemSetting created.';
END
ELSE
    PRINT 'V003: AppSystemSetting already exists — skipped create.';
GO

-- Migrate system-scope rows from AppSetup if that table exists
IF OBJECT_ID('dbo.AppSetup', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.AppSystemSetting (SetupCode, SetupValue, Description)
    SELECT s.SetupCode, s.SetupValue, ISNULL(s.Description, s.SetupCode)
    FROM   dbo.AppSetup s
    WHERE  s.SetupCode IN (
               'Timeout',
               'TimeoutWarningGracePeriod',
               'ApplicationTutorialUrl',
               'ApplicationURL',
               'BaseUserDbBackupFilePath',
               'UserDbFileDirectoryPath',
               'AppVersion',
               'InternalApiRestEndPoint',
               'ApplicationPoolName',
               'IISWebSiteName'
           )
      AND NOT EXISTS (
          SELECT 1 FROM dbo.AppSystemSetting t WHERE t.SetupCode = s.SetupCode
      );
    PRINT 'V003: AppSystemSetting data migrated from AppSetup.';
END
GO

-- Seed defaults for any system settings not already present.
-- Update these values to match your installation before running.
INSERT INTO dbo.AppSystemSetting (SetupCode, SetupValue, Description)
SELECT val.SetupCode, val.SetupValue, val.Description
FROM (VALUES
    ('Timeout',                   '30',                                       'Session timeout in minutes'),
    ('TimeoutWarningGracePeriod', '5',                                        'Warning grace period before timeout'),
    ('ApplicationURL',            'http://localhost/appai',                   'Application base URL'),
    ('InternalApiRestEndPoint',   'http://localhost/appai/webapi/PluginApi',  'Internal REST API endpoint'),
    ('ApplicationPoolName',       'AppAI',                                    'IIS application pool name'),
    ('IISWebSiteName',            'Default Web Site',                         'IIS website name')
) AS val(SetupCode, SetupValue, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.AppSystemSetting WHERE SetupCode = val.SetupCode
);
PRINT 'V003: AppSystemSetting defaults seeded.';
GO

-- ============================================================
-- 2. AppTenantSetting  (run this script against each AppTenantDB)
-- ============================================================
IF OBJECT_ID('dbo.AppTenantSetting', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppTenantSetting
    (
        SetupId       INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppTenantSetting PRIMARY KEY,
        SetupCode     VARCHAR(100)   NOT NULL,
        SetupValue    NVARCHAR(MAX)  NULL,
        Description   NVARCHAR(500)  NULL,
        EntityId      INT            NULL,
        UsageType     INT            NULL,
        CONSTRAINT UQ_AppTenantSetting_SetupCode UNIQUE (SetupCode)
    );
    PRINT 'V003: AppTenantSetting created.';
END
ELSE
    PRINT 'V003: AppTenantSetting already exists — skipped create.';
GO

-- Migrate tenant-scope rows from AppSetup if present in this tenant DB.
-- Excludes system-scope codes that belong in AppSystemSetting.
IF OBJECT_ID('dbo.AppSetup', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, EntityId, UsageType)
    SELECT s.SetupCode, s.SetupValue, ISNULL(s.Description, s.SetupCode),
           TRY_CAST(s.EntityId  AS INT),
           TRY_CAST(s.UsageType AS INT)
    FROM   dbo.AppSetup s
    WHERE  s.SetupCode NOT IN (
               'Timeout', 'TimeoutWarningGracePeriod', 'ApplicationTutorialUrl',
               'ApplicationURL', 'BaseUserDbBackupFilePath', 'UserDbFileDirectoryPath',
               'AppVersion', 'InternalApiRestEndPoint', 'ApplicationPoolName', 'IISWebSiteName'
           )
      AND NOT EXISTS (
          SELECT 1 FROM dbo.AppTenantSetting t WHERE t.SetupCode = s.SetupCode
      );
    PRINT 'V003: AppTenantSetting data migrated from AppSetup.';
END
GO
