-- ============================================================
-- Phase 3: Custom URL per Tenant - DB Migration
-- Run against AppMasterDB only.
-- AppDataSourceRegister lives in master; tenant DBs do not have it.
-- ============================================================

USE AppMasterDB;
GO

-- ── Step 1: Add new columns to AppDataSourceRegister ──
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AppDataSourceRegister')
      AND name = 'CustomDomain')
BEGIN
    ALTER TABLE dbo.AppDataSourceRegister
        ADD CustomDomain NVARCHAR(255) NULL;
    PRINT 'Added CustomDomain column.';
END
ELSE
    PRINT 'CustomDomain column already exists.';

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AppDataSourceRegister')
      AND name = 'DomainToken')
BEGIN
    ALTER TABLE dbo.AppDataSourceRegister
        ADD DomainToken NVARCHAR(100) NULL;
    PRINT 'Added DomainToken column.';
END
ELSE
    PRINT 'DomainToken column already exists.';
GO

-- ── Step 2: Seed DomainToken from AppCompany.CompanyDomainIdentityToken ──
UPDATE ds
SET    ds.DomainToken = LOWER(LTRIM(RTRIM(c.CompanyDomainIdentityToken)))
FROM   dbo.AppDataSourceRegister ds
INNER JOIN dbo.AppCompany c
       ON c.AppCompanyId = ds.DataSourceOwnerCompanyId
WHERE  c.CompanyDomainIdentityToken IS NOT NULL
  AND  LTRIM(RTRIM(c.CompanyDomainIdentityToken)) <> ''
  AND  ds.DomainToken IS NULL;

PRINT CAST(@@ROWCOUNT AS NVARCHAR) + ' row(s) seeded with DomainToken.';
GO

-- ── Verify ──
SELECT DataSourceId, DataSourceName, DataSourceOwnerCompanyId, DomainToken, CustomDomain
FROM   dbo.AppDataSourceRegister
ORDER  BY DataSourceName;
GO
