-- ============================================================
-- V004: Drop cross-database FK constraints on DataSourceFrom
--
-- AppDataSourceRegister lives in AppMasterDB.  The tenant DB
-- has its own copy only to satisfy FK constraints, which causes
-- duplication and sync problems.
--
-- DataSourceFrom is a logical reference resolved at runtime by
-- AppCacheManagerBL.GetOneDatabaseFixture() — no DB-level FK
-- needed.  Drop the constraints so tenant DBs no longer need a
-- local copy of AppDataSourceRegister.
--
-- Run against every TenantDB_{CODE}.
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_AppEntityInfo_AppDataSourceRegister'
)
    ALTER TABLE [dbo].[AppEntityInfo]  DROP CONSTRAINT FK_AppEntityInfo_AppDataSourceRegister;
GO

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_AppTransaction_AppDataSourceRegister'
)
    ALTER TABLE [dbo].[AppTransaction] DROP CONSTRAINT FK_AppTransaction_AppDataSourceRegister;
GO

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_AppDataSet_AppDataSourceRegister'
)
    ALTER TABLE [dbo].[AppDataSet]     DROP CONSTRAINT FK_AppDataSet_AppDataSourceRegister;
GO

PRINT 'V004: Cross-DB DataSourceFrom FK constraints dropped.';
GO
