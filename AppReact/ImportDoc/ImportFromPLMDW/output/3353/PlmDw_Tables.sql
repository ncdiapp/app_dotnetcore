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
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @RootTable) AND name = N'ReferenceCode')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@RootTable) + N' ADD [ReferenceCode] NVARCHAR(255) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @RootTable) AND name = N'MasterReferenceId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@RootTable) + N' ADD [MasterReferenceId] INT NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @RootTable) AND name = N'FolderId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@RootTable) + N' ADD [FolderId] INT NULL;'; EXEC sp_executesql @sql; END
END

-- Artwork_Details
SET @TableName = @TablePrefix + N'Artwork_Details';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Classification] [int] NULL, [Product_Type] [int] NULL, [Season] [int] NULL, [Collection] [int] NULL, [Group] [int] NULL, [Sketch] [int] NULL, [Division_8] [int] NULL, [Article] [nvarchar](4000) NULL, [Description_23] [nvarchar](4000) NULL, [Made_in_Country] [int] NULL, [Product_Manager] [int] NULL, [Long_Description] [nvarchar](4000) NULL, [Brand] [int] NULL, [ProductTypeGroup] [int] NULL, [Division_186] [int] NULL, [Created_By_189] [nvarchar](4000) NULL, [Last_Revised_By] [nvarchar](4000) NULL, [Fabric_Type] [int] NULL, [Security_Group_3153] [int] NULL, [Created_by_3154] [int] NULL, [Sub_Type] [int] NULL, [Product_Class] [int] NULL, [Class_Group] [int] NULL, [Artwork_sent_to_Vendor] [int] NULL, [Artwork_sent_Date] [datetime] NULL, [Artwork_Status] [int] NULL, [Print_Direction] [int] NULL, [Comments] [nvarchar](4000) NULL, [Print_Theme] [int] NULL, [Artwork_File] [nvarchar](4000) NULL, [Artwork_Due_Date] [datetime] NULL, [Artwork_Approval_Date] [datetime] NULL, [Recolour] [int] NULL, [Recolour_From] [nvarchar](4000) NULL, [Height] [decimal](18, 2) NULL, [UOM_5328] [int] NULL, [Width] [decimal](18, 2) NULL, [UOM_5330] [int] NULL, [Artwork_Placement] [int] NULL, [Exclusive] [int] NULL, [Item_Type] [int] NULL, [File_Attachment_1] [nvarchar](4000) NULL, [File_Attachment_2] [nvarchar](4000) NULL, [File_Attachment_3] [nvarchar](4000) NULL, [File_Attachment_4] [nvarchar](4000) NULL, [Final_View] [int] NULL, [Measurements] [int] NULL, [Position] [int] NULL, [Quality_Reference] [int] NULL, [Image_Content_6734] [int] NULL, [Image_Content_6735] [int] NULL, [Image_Content_6736] [int] NULL, [Image_Content_6737] [int] NULL, [French] [nvarchar](4000) NULL, [Process_Type_1] [int] NULL, [Process_Type_2] [int] NULL, [Process_Type_3] [int] NULL, [Image_6951] [int] NULL, [Description_6952] [int] NULL, [Description_6953] [int] NULL, [Image_6954] [int] NULL, [Description_6955] [int] NULL, [Image_6956] [int] NULL, [Description_6957] [int] NULL, [Image_6958] [int] NULL, [Security_Group_7022] [int] NULL, [Approved_by] [int] NULL, [Name] [nvarchar](4000) NULL, [Product_Type_txt] [nvarchar](4000) NULL, [sketch_id] [nvarchar](4000) NULL, [ddl] [int] NULL, [Collection_txt] [nvarchar](4000) NULL, [ProductTypeGroup_txt] [nvarchar](4000) NULL, [Subcategory] [int] NULL, [ERP_Season] [int] NULL, [French_Name] [nvarchar](4000) NULL, CONSTRAINT [PK_Artwork_Details] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Article] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_23] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Long_Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Created_By_189] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Last_Revised_By] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Artwork_File] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Recolour_From] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [File_Attachment_1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [File_Attachment_2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [File_Attachment_3] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [File_Attachment_4] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [French] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Name] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Product_Type_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [sketch_id] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Collection_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ProductTypeGroup_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [French_Name] [nvarchar](4000) NULL;';
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

-- ProductDesignColorGrid
SET @TableName = @TablePrefix + N'ProductDesignColorGrid';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Userdefine3] [nvarchar](4000) NULL, [Userdefine4] [nvarchar](4000) NULL, [Userdefine5] [nvarchar](4000) NULL, [Userdefine6] [nvarchar](4000) NULL, [Userdefine7] [nvarchar](4000) NULL, [Userdefine8] [nvarchar](4000) NULL, [Userdefine9] [nvarchar](4000) NULL, [Userdefine10] [nvarchar](4000) NULL, [Userdefine11] [nvarchar](4000) NULL, [Userdefine12] [nvarchar](4000) NULL, [Userdefine13] [nvarchar](4000) NULL, [Userdefine14] [nvarchar](4000) NULL, [Userdefine15] [nvarchar](4000) NULL, [RGB] [nvarchar](4000) NULL, [Swatch] [nvarchar](4000) NULL, [Description] [nvarchar](4000) NULL, [Approv_Date] [datetime] NULL, [Approved] [nvarchar](4000) NULL, [Name] [nvarchar](4000) NULL, [ReferenceCode] [nvarchar](4000) NULL, [ReferenceName] [nvarchar](4000) NULL, [Standard_Cost] [decimal](18, 2) NULL, [Selling_Price] [decimal](18, 2) NULL, [Retail] [decimal](18, 2) NULL, [Color_Code] [nvarchar](4000) NULL, [First_Cost] [decimal](18, 2) NULL, [SupplierColor] [nvarchar](4000) NULL, [Effective_Date] [datetime] NULL, [ERPID] [nvarchar](4000) NULL, [ExternalImageLink] [nvarchar](4000) NULL, [Reference] [nvarchar](4000) NULL, [Comments] [nvarchar](4000) NULL, [CAN_Qty_Color] [decimal](18, 2) NULL, [USA_Qty_Color] [decimal](18, 2) NULL, [FOB_Price] [decimal](18, 2) NULL, [CAN_Qty_Total] [decimal](18, 2) NULL, [USA_Qty_Total] [decimal](18, 2) NULL, [CAN_PO_s] [nvarchar](4000) NULL, [CAN_ETA] [nvarchar](4000) NULL, [USA_PO_s] [nvarchar](4000) NULL, [USA_ETA] [nvarchar](4000) NULL, [CAN_PO_Created_Date] [nvarchar](4000) NULL, [USA_PO_Created_Date] [nvarchar](4000) NULL, [Color_Combo] [nvarchar](4000) NULL, [DateBulk1] [datetime] NULL, [DateBulk2] [datetime] NULL, [DateBulk3] [datetime] NULL, [Date_SMS] [datetime] NULL, [Color_Price_p_m] [decimal](18, 2) NULL, [Color_Price_p_y] [decimal](18, 2) NULL, [Fabric_Price_p_m] [decimal](18, 2) NULL, [Fabric_Price_p_y] [decimal](18, 2) NULL, [Active] [nvarchar](4000) NULL, [USD_WS_Price] [decimal](18, 2) NULL, [USD_RET_Price] [decimal](18, 2) NULL, [CAD_VIP_Price] [decimal](18, 2) NULL, [USD_VIP_Price] [decimal](18, 2) NULL, [CAD_WS_Price] [decimal](18, 2) NULL, [CAD_RET_Price] [decimal](18, 2) NULL, [SketchId] [int] NULL, [Image] [int] NULL, [FirstCostCurrency] [int] NULL, [Selling_Currency] [int] NULL, [USD] [int] NULL, [CAD] [int] NULL, [Color] [int] NULL, [ColorReferenceTypeID] [int] NULL, [ColorFamilyID] [int] NULL, [ProductColorNRF] [int] NULL, [NRF] [int] NULL, [ColorFolderPath] [int] NULL, [GS1_Color_Group] [int] NULL, [GS1_Shade_Group] [int] NULL, [Colour_Risk] [int] NULL, [Userdefine1] [int] NULL, [Userdefine2] [int] NULL, [SS] [int] NULL, [Colour_Risk_Comment] [int] NULL, [Bulk_1_Status] [int] NULL, [SMS] [nvarchar](4000) NULL, [Bulk_2_Status] [int] NULL, [Bulk_3_Status] [int] NULL, CONSTRAINT [PK_ProductDesignColorGrid] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine3] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine4] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine5] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine6] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine7] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine8] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine9] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine11] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine12] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine13] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine14] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine15] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [RGB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Swatch] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Approved] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Name] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ReferenceCode] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ReferenceName] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Color_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [SupplierColor] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ERPID] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ExternalImageLink] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Reference] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CAN_PO_s] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CAN_ETA] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [USA_PO_s] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [USA_ETA] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CAN_PO_Created_Date] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [USA_PO_Created_Date] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Color_Combo] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Active] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [SMS] [nvarchar](4000) NULL;';
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

