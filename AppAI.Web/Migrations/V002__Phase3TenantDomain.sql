-- ============================================================
-- V002: Phase 3 — Add tenant domain columns to AppDataSourceRegister
--
-- NOTE: AppDataSourceRegister lives in AppMasterDB, not tenant DBs.
-- This migration adds the Phase 3 columns to any tenant DB that
-- might carry a copy of AppDataSourceRegister (legacy scenario only).
-- New tenant DBs do not include AppDataSourceRegister at all.
-- ============================================================

IF OBJECT_ID('dbo.AppDataSourceRegister', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('dbo.AppDataSourceRegister')
                     AND name = 'CustomDomain')
        ALTER TABLE dbo.AppDataSourceRegister ADD CustomDomain NVARCHAR(255) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('dbo.AppDataSourceRegister')
                     AND name = 'DomainToken')
        ALTER TABLE dbo.AppDataSourceRegister ADD DomainToken NVARCHAR(100) NULL;

    PRINT 'V002: AppDataSourceRegister domain columns ensured.';
END
ELSE
    PRINT 'V002: AppDataSourceRegister not present in this tenant DB — skipped.';
GO
