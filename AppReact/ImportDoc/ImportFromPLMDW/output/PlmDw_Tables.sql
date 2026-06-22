-- =============================================================================
-- PLM DW â†’ APP physical tables (generated â€” see ImportFromPLMDW/PROMPT.md)
-- EXECUTION ORDER:
--   1. PlmDw_Tables.sql          (this file)
--   2. PlmDw_FieldMapping.sql
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

-- Packaging_Header
SET @TableName = @TablePrefix + N'Packaging_Header';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Classification] [int] NULL, [Product_Type] [int] NULL, [Season] [int] NULL, [Collection] [int] NULL, [Group] [int] NULL, [Sketch] [int] NULL, [Division_8] [int] NULL, [Size_Range] [int] NULL, [Dimension] [int] NULL, [Article] [nvarchar](255) NULL, [Description] [nvarchar](255) NULL, [Country_Of_Origin] [int] NULL, [Product_Manager] [int] NULL, [Long_Description] [nvarchar](255) NULL, [Sample_Size_Detail] [int] NULL, [Brand] [int] NULL, [ProductTypeGroup] [int] NULL, [Size_Detail_Dispaly] [nvarchar](255) NULL, [Division_186] [int] NULL, [Created_By] [nvarchar](255) NULL, [Last_Revised_By] [nvarchar](255) NULL, [ProductReferenceId] [nvarchar](255) NULL, [Raw_Material_Status] [int] NULL, [State] [decimal](18, 2) NULL, [Item_Type] [int] NULL, [Material] [int] NULL, [Supplier_Article_Number] [nvarchar](255) NULL, [Release_to_Manufacturer] [int] NULL, [Old_Code] [nvarchar](255) NULL, [Label_Reference] [nvarchar](255) NULL, [Cost_1] [decimal](18, 2) NULL, [French] [nvarchar](255) NULL, [Process_Type_1] [int] NULL, [Process_Type_2] [int] NULL, [Process_Type_3] [int] NULL, [Supplier_1] [int] NULL, [Release_Day] [datetime] NULL, [Name] [nvarchar](255) NULL, [Product_Type_txt] [nvarchar](255) NULL, [sketch_id] [nvarchar](255) NULL, [ddl] [int] NULL, [Collection_txt] [nvarchar](255) NULL, [Supplier_2] [int] NULL, [USD_for] [nvarchar](255) NULL, [ProductTypeGroup_txt] [nvarchar](255) NULL, [Subcategory] [int] NULL, [ERP_Season] [int] NULL, [French_Name] [nvarchar](255) NULL, CONSTRAINT [PK_Packaging_Header] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
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

-- Packaging_Info
SET @TableName = @TablePrefix + N'Packaging_Info';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Construction] [int] NULL, [Width] [decimal](18, 2) NULL, [Width_Unit] [int] NULL, [Weight] [decimal](18, 2) NULL, [Weight_Unit] [int] NULL, [Length] [decimal](18, 2) NULL, [Length_Unit] [int] NULL, [Height] [decimal](18, 2) NULL, [Height_Unit] [int] NULL, [Lead_Time] [nvarchar](255) NULL, [Lead_Time_UOM] [int] NULL, [Bulk_MOQ] [nvarchar](255) NULL, [Sample_MOQ] [nvarchar](255) NULL, [MOQ_UOM] [int] NULL, [File_Attachment_1] [nvarchar](255) NULL, [File_Attachment_2] [nvarchar](255) NULL, [File_Attachment_3] [nvarchar](255) NULL, [File_Attachment_4] [nvarchar](255) NULL, [Final_View] [int] NULL, [Measurements] [int] NULL, [Position] [int] NULL, [Quality_Reference] [int] NULL, [Security_Group_6702] [int] NULL, [Testing_Technician] [int] NULL, [Security_Group_6704] [int] NULL, [Buyer] [int] NULL, [Image_Content_6734] [int] NULL, [Image_Content_6735] [int] NULL, [Image_Content_6736] [int] NULL, [Image_Content_6737] [int] NULL, [Image_6951] [int] NULL, [Description_6952] [int] NULL, [Description_6953] [int] NULL, [Image_6954] [int] NULL, [Description_6955] [int] NULL, [Image_6956] [int] NULL, [Description_6957] [int] NULL, [Image_6958] [int] NULL, [Created_by] [int] NULL, [Per_7135] [int] NULL, [Per_7136] [int] NULL, CONSTRAINT [PK_Packaging_Info] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
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

