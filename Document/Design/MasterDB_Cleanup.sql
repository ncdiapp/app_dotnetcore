-- ============================================================
-- AppMasterDB Cleanup Script
-- Drops all tables NOT required in the master DB.
-- Master DB retains only: identity, auth, company config,
-- platform-wide lookup, and audit tables.
-- Run in SSMS or sqlcmd against AppMasterDB.
-- ============================================================

USE AppMasterDB;
GO

-- ============================================================
-- TABLES THAT STAY IN MASTER DB
-- ============================================================
-- AppCompany
-- AppSecurityUser
-- AppSecurityUserSession
-- AppBusinessPartner
-- AppBusinessPartnerInviteUser
-- AppBusinessPartnerInviteUserChildUser
-- AppDataSourceRegister
-- AppSecurityRegDomain
-- AppSecurityRegDomainORG
-- AppSecurityLoginAuditor
-- AppSecurityAuthticationInfo
-- AppSecurityUserContact
-- AppSecurityUserInvitation
-- AppLanguage
-- AppLanguageKey
-- AppTimeZoneAbbreviation
-- AppAISkill
-- AppAISkillRef
-- AppBuilderAgentSession
-- AppBackupLog
-- AppBatchLog
-- AppLogTrack
-- ============================================================

DECLARE @keepTables TABLE (TableName NVARCHAR(200));
INSERT INTO @keepTables VALUES
    ('AppCompany'),
    ('AppSecurityUser'),
    ('AppSecurityUserSession'),
    ('AppBusinessPartner'),
    ('AppBusinessPartnerInviteUser'),
    ('AppBusinessPartnerInviteUserChildUser'),
    ('AppDataSourceRegister'),
    ('AppSecurityRegDomain'),
    ('AppSecurityRegDomainORG'),
    ('AppSecurityLoginAuditor'),
    ('AppSecurityAuthticationInfo'),
    ('AppSecurityUserContact'),
    ('AppSecurityUserInvitation'),
    ('AppLanguage'),
    ('AppLanguageKey'),
    ('AppTimeZoneAbbreviation'),
    ('AppAISkill'),
    ('AppAISkillRef'),
    ('AppBuilderAgentSession'),
    ('AppBackupLog'),
    ('AppBatchLog'),
    ('AppLogTrack');

PRINT '=== Step 1: Dropping all FK constraints involving non-keep tables ===';

-- Drop all FK constraints where the child table OR the parent table is being removed.
-- This handles both intra-drop FKs and cross-boundary FKs (e.g. keep-table FK → drop-table).
DECLARE @fkName   NVARCHAR(200);
DECLARE @fkTable  NVARCHAR(200);
DECLARE fk_cursor CURSOR FAST_FORWARD FOR
    SELECT fk.name, OBJECT_NAME(fk.parent_object_id)
    FROM   sys.foreign_keys fk
    WHERE  OBJECT_NAME(fk.parent_object_id)   NOT IN (SELECT TableName FROM @keepTables)
        OR OBJECT_NAME(fk.referenced_object_id) NOT IN (SELECT TableName FROM @keepTables);

OPEN fk_cursor;
FETCH NEXT FROM fk_cursor INTO @fkName, @fkTable;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @dropFk NVARCHAR(500) = N'ALTER TABLE [dbo].[' + @fkTable + N'] DROP CONSTRAINT [' + @fkName + N'];';
    PRINT @dropFk;
    EXEC sp_executesql @dropFk;
    FETCH NEXT FROM fk_cursor INTO @fkName, @fkTable;
END
CLOSE fk_cursor;
DEALLOCATE fk_cursor;

PRINT '=== Step 2: Dropping all non-keep tables ===';

DECLARE @tableName NVARCHAR(200);
DECLARE tbl_cursor CURSOR FAST_FORWARD FOR
    SELECT TABLE_NAME
    FROM   INFORMATION_SCHEMA.TABLES
    WHERE  TABLE_TYPE = 'BASE TABLE'
      AND  TABLE_NAME NOT IN (SELECT TableName FROM @keepTables)
    ORDER  BY TABLE_NAME;

OPEN tbl_cursor;
FETCH NEXT FROM tbl_cursor INTO @tableName;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @dropTbl NVARCHAR(500) = N'IF OBJECT_ID(N''[dbo].[' + @tableName + N']'', N''U'') IS NOT NULL DROP TABLE [dbo].[' + @tableName + N'];';
    PRINT @dropTbl;
    EXEC sp_executesql @dropTbl;
    FETCH NEXT FROM tbl_cursor INTO @tableName;
END
CLOSE tbl_cursor;
DEALLOCATE tbl_cursor;

PRINT '=== Cleanup complete ===';
PRINT '';

-- Verify: list remaining tables
PRINT '=== Tables remaining in AppMasterDB ===';
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;
GO
