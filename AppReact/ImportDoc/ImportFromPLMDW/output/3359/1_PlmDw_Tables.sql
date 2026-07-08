-- =============================================================================
-- PLM DW â†’ APP physical tables (generated â€” see ImportFromPLMDW/PROMPT.md)
-- EXECUTION ORDER:
--   1. 1_PlmDw_Tables.sql          (this file)
--   2. 2_PlmDw_FieldMapping.sql
--   3. 3_PlmDw_ImportFromDW.sql
--   4. 4_PlmDw_ImportBlueprint.json + Phase D Execute
--   5. 5_PlmDw_ImportBomColorwayGrandchild.sql  (when BOM colorway grids detected)
--   6. 6_PlmDw_CleanupBomColorwayStaging.sql
-- USER SETTINGS (single batch - do not split with GO):
--   @TablePrefix     table prefix, include trailing underscore (default Plm_)
--   @RootTableSuffix root table name after prefix (default ReferenceBasicInfo)
-- Source: plmDW Tab/Grid wide tables for user TabId set
-- =============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

DECLARE @TablePrefix     NVARCHAR(32)  = N'Plm_';  -- <<< USER SETTING
DECLARE @RootTableSuffix NVARCHAR(128) = N'ReferenceBasicInfo';     -- <<< USER SETTING
DECLARE @TableName       NVARCHAR(128);
DECLARE @RootTable       NVARCHAR(128);
DECLARE @FkName          NVARCHAR(128);
DECLARE @HostTable       NVARCHAR(128);
DECLARE @ParentFkName    NVARCHAR(128);
DECLARE @OldRefFkName    NVARCHAR(128);
DECLARE @sql             NVARCHAR(MAX);

-- ReferenceBasicInfo (root)
SET @RootTable = @TablePrefix + @RootTableSuffix;

IF OBJECT_ID(N'dbo.' + QUOTENAME(@RootTable), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@RootTable) + N' (
        [ReferenceId] INT IDENTITY(1,1) NOT NULL,
        [ReferenceCode] NVARCHAR(255) NULL,
        [MasterReferenceId] INT NULL,
        [FolderId] INT NULL,
        [AppCreatedByID] INT NULL,
        [AppCreatedDate] DATETIME NULL,
        [AppModifiedByID] INT NULL,
        [AppModifiedDate] DATETIME NULL,
        CONSTRAINT [PK_' + @RootTableSuffix + N'] PRIMARY KEY CLUSTERED ([ReferenceId])
    );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @RootTable) AND name = N'ReferenceCode')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@RootTable) + N' ADD [ReferenceCode] NVARCHAR(255) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @RootTable) AND name = N'MasterReferenceId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@RootTable) + N' ADD [MasterReferenceId] INT NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @RootTable) AND name = N'FolderId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@RootTable) + N' ADD [FolderId] INT NULL;'; EXEC sp_executesql @sql; END
END

-- Graphic_Requests
SET @TableName = @TablePrefix + N'Graphic_Requests';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Created_By] [nvarchar](4000) NULL, [Last_Revised_By] [nvarchar](4000) NULL, [Other_Image] [int] NULL, [Request_Type] [int] NULL, [Request_Date] [datetime] NULL, [Request_By] [int] NULL, [Deliver_By] [datetime] NULL, [Additional_Info] [nvarchar](4000) NULL, [Requestor_Role] [int] NULL, [Graphic_Req] [nvarchar](4000) NULL, [Handled_By] [int] NULL, [Performed_By] [int] NULL, [Can_Complete_By] [datetime] NULL, [Extra_Time_Needed] [decimal](18, 2) NULL, [Status] [int] NULL, [Completed_Date] [datetime] NULL, [Days_Needed] [decimal](18, 2) NULL, [Revised_Complete_By] [datetime] NULL, [Attachment_6841] [nvarchar](4000) NULL, [Item_Type] [int] NULL, [Path] [nvarchar](4000) NULL, [Attachment_6859] [nvarchar](4000) NULL, [Attachment_6860] [nvarchar](4000) NULL, [References] [nvarchar](4000) NULL, [Attachment_6862] [nvarchar](4000) NULL, [Attachment_6863] [nvarchar](4000) NULL, [Attachment_6864] [nvarchar](4000) NULL, [dateisblank_calc] [nvarchar](4000) NULL, [setdate_calc] [nvarchar](4000) NULL, [blankdate_calc] [nvarchar](4000) NULL, [isCompleted] [nvarchar](4000) NULL, [Request_Name] [nvarchar](4000) NULL, [isArchived] [nvarchar](4000) NULL, [Notify_GA] [int] NULL, [Active_7364] [int] NULL, [Active_7365] [int] NULL, CONSTRAINT [PK_Graphic_Requests] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Created_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Created_By] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Created_By] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Last_Revised_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Last_Revised_By] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Last_Revised_By] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Other_Image')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Other_Image] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Request_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Request_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Request_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Request_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Request_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Request_By] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Deliver_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Deliver_By] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Additional_Info')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Additional_Info] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Additional_Info] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Requestor_Role')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Requestor_Role] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Graphic_Req')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Graphic_Req] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Graphic_Req] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Handled_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Handled_By] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Performed_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Performed_By] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Can_Complete_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Can_Complete_By] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Extra_Time_Needed')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Extra_Time_Needed] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Completed_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Completed_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Days_Needed')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Days_Needed] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Revised_Complete_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Revised_Complete_By] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Attachment_6841')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Attachment_6841] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Attachment_6841] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Item_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Item_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Path')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Path] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Path] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Attachment_6859')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Attachment_6859] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Attachment_6859] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Attachment_6860')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Attachment_6860] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Attachment_6860] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'References')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [References] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [References] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Attachment_6862')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Attachment_6862] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Attachment_6862] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Attachment_6863')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Attachment_6863] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Attachment_6863] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Attachment_6864')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Attachment_6864] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Attachment_6864] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'dateisblank_calc')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [dateisblank_calc] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [dateisblank_calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'setdate_calc')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [setdate_calc] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [setdate_calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'blankdate_calc')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [blankdate_calc] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [blankdate_calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isCompleted')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isCompleted] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isCompleted] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Request_Name')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Request_Name] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Request_Name] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isArchived')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isArchived] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isArchived] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Notify_GA')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Notify_GA] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Active_7364')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Active_7364] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Active_7365')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Active_7365] [int] NULL;'; EXEC sp_executesql @sql; END
END


IF OBJECT_ID(N'dbo.' + QUOTENAME(@RootTable), N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @FkName)
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName)
        + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@FkName)
        + N' FOREIGN KEY ([ReferenceId]) REFERENCES dbo.' + QUOTENAME(@RootTable) + N' ([ReferenceId]);';
    EXEC sp_executesql @sql;
END
ELSE IF OBJECT_ID(N'dbo.' + QUOTENAME(@RootTable), N'U') IS NULL
BEGIN
    PRINT N'Skipped FK ' + @FkName + N': root table dbo.' + @RootTable + N' does not exist.';
END

GO

