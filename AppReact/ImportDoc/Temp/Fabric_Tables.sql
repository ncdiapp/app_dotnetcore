-- =============================================================================
-- FABRIC 02 - physical tables (from source/fabricTables.sql)
-- EXECUTION ORDER:
--   1. Fabric_Tables.sql          (this file)
--   2. Fabric_FieldMapping.sql
-- USER SETTINGS (single batch - do not split with GO):
--   @TablePrefix     table prefix, include trailing underscore (default Plm_)
--   @RootTableSuffix root table name after prefix (default ReferenceBasicInfo)
-- Creates: {prefix}ReferenceBasicInfo, {prefix}Fabric_*, {prefix}ProductColor (shared grid)
-- =============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

DECLARE @TablePrefix     NVARCHAR(32)  = N'Plm_';               -- <<< USER SETTING
DECLARE @RootTableSuffix NVARCHAR(128) = N'ReferenceBasicInfo'; -- <<< USER SETTING
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

-- Fabric_Header
SET @TableName = @TablePrefix + N'Fabric_Header';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Composition] [int] NULL, [Division_8] [int] NULL, [Article] [nvarchar](255) NULL, [Division_186] [int] NULL, [Security_Group] [int] NULL, [Raw_Material_Status] [int] NULL, [Fabric_Type] [int] NULL, [Fabric_Mill] [nvarchar](255) NULL, [Item_Type] [int] NULL, [Supplier_Article_Number] [nvarchar](255) NULL, [Weight] [decimal](18, 2) NULL, [Composition1] [decimal](18, 1) NULL, [Composition2] [decimal](18, 1) NULL, [Composition3] [decimal](18, 1) NULL, [Comp1] [int] NULL, [Active_Inactive] [int] NULL, [Comments] [nvarchar](255) NULL, [Composition4] [decimal](18, 1) NULL, [Composition5] [decimal](18, 1) NULL, [Fabric_Name] [int] NULL, [Classification] [int] NULL, [Designer] [int] NULL, [State_3129] [int] NULL, [Sub_Type] [int] NULL, [Weight_Unit] [int] NULL, [Compositionfiber1] [int] NULL, [Compositionfiber2] [int] NULL, [Compositionfiber3] [int] NULL, [Comp2] [int] NULL, [Compositionfiber4] [int] NULL, [Compositionfiber5] [int] NULL, [Name] [nvarchar](255) NULL, [Fabric_Name_txt] [nvarchar](255) NULL, [Comp3] [int] NULL, [state_5078] [nvarchar](255) NULL, [state_5079] [nvarchar](255) NULL, [state_5080] [nvarchar](255) NULL, [state_5101] [nvarchar](255) NULL, [state_5104] [nvarchar](255) NULL, [Per] [int] NULL, [Designer_1] [int] NULL, [Subcategory] [int] NULL, [French_Name] [nvarchar](255) NULL, [ProductTypeGroup] [int] NULL, [Comp4] [int] NULL, [French] [nvarchar](255) NULL, [Designer_2] [int] NULL, [Description] [nvarchar](255) NULL, [Comp5] [int] NULL, [ProductTypeGroup_txt] [nvarchar](255) NULL, [Product_Type] [int] NULL, [Long_Description] [nvarchar](255) NULL, [Comp6] [int] NULL, [Fiber1CB] [int] NULL, [Product_Type_txt] [nvarchar](255) NULL, [Product_Manager] [int] NULL, [Fiber2CB] [int] NULL, [Fiber3CB] [int] NULL, [Fiber4CB] [int] NULL, [Fiber5CB] [int] NULL, [Fiber6CB] [int] NULL, [Fiber1IB] [nvarchar](255) NULL, [Fiber2IB] [nvarchar](255) NULL, [Fiber3IB] [nvarchar](255) NULL, [Fiber4IB] [nvarchar](255) NULL, [Fiber5IB] [nvarchar](255) NULL, [Fiber6IB] [nvarchar](255) NULL, [Comp_ok] [bit] NULL, [Total_Composition] [nvarchar](255) NULL, [CompositionDDL] [int] NULL, [Valid_Selection] [bit] NULL, [Percent_Chk] [bit] NULL, [CompositionTXT] [nvarchar](255) NULL, [comp1ok] [bit] NULL, [comp2ok] [bit] NULL, [comp3ok] [bit] NULL, [comp4ok] [bit] NULL, [comp5ok] [bit] NULL, [comp6ok] [bit] NULL, [Comp1_Tx] [nvarchar](255) NULL, [Comp2_Tx] [nvarchar](255) NULL, [Comp3_Tx] [nvarchar](255) NULL, [Comp4_Tx] [nvarchar](255) NULL, [Comp5_Tx] [nvarchar](255) NULL, [Comp6_Tx] [nvarchar](255) NULL, CONSTRAINT [PK_Fabric_Header] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
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

-- Fabric_Info
SET @TableName = @TablePrefix + N'Fabric_Info';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Season_3] [int] NULL, [Size_Range_10] [int] NULL, [Dimension_11] [int] NULL, [Created_Date] [datetime] NULL, [Security_Group_3098] [int] NULL, [Print_Solid] [int] NULL, [SupplierType] [int] NULL, [Publish_to_ERP] [bit] NULL, [Published_to_ERP] [bit] NULL, [Publish_Failed_to_ERP] [bit] NULL, [Width] [decimal](18, 2) NULL, [Publish_to_ERP_Message] [nvarchar](255) NULL, [Original_Reference] [nvarchar](255) NULL, [New_Carryover] [int] NULL, [Is_this_Greige] [int] NULL, [Greige_Type] [int] NULL, [Finish_1] [int] NULL, [Finish_2] [nvarchar](255) NULL, [Fabric_COO] [int] NULL, [Coordinator] [int] NULL, [Parent_Child] [int] NULL, [RM_Risk_Commnet] [int] NULL, [Security_Group_5097] [int] NULL, [Product_Class_5232] [int] NULL, [Product_Class_5262] [int] NULL, [Dye_Type] [int] NULL, [Fabric_Mill] [int] NULL, [ddl] [int] NULL, [Vendor] [int] NULL, [Composition6] [decimal](18, 1) NULL, [Sketch] [nvarchar](255) NULL, [Size_Detail_Dispaly] [nvarchar](255) NULL, [Created_By] [nvarchar](255) NULL, [Sourcing] [int] NULL, [Notes] [nvarchar](255) NULL, [Width_Unit] [int] NULL, [Product_Code] [nvarchar](255) NULL, [QC_Team] [int] NULL, [Security_Group_5096] [int] NULL, [Collection_5233] [int] NULL, [Class_Group] [int] NULL, [Other_Risks] [nvarchar](255) NULL, [Garment_Factory] [int] NULL, [Free_text] [nvarchar](255) NULL, [Compositionfiber6] [int] NULL, [ERP_Season] [int] NULL, [Collection_4] [int] NULL, [Sample_Size_Detail] [int] NULL, [Last_Revised_Date] [datetime] NULL, [Size_Range_5022] [int] NULL, [sketch_id] [nvarchar](255) NULL, [Per] [int] NULL, [state] [nvarchar](255) NULL, [Last_Revised_By] [nvarchar](255) NULL, [DivisionBlock] [int] NULL, [Collection_txt] [nvarchar](255) NULL, [Group] [int] NULL, [Product_Class_5024] [int] NULL, [Dimension_5025] [int] NULL, [Season_5026] [int] NULL, [Price_Type] [int] NULL, [First_Cost_Currency] [int] NULL, [Valid_Size_Selection] [bit] NULL, [Valid_Product_Code_Selection] [bit] NULL, [Valid_DivisionBlock_Selection] [bit] NULL, [Valid_Product_Class_Selection] [bit] NULL, [Valid_Dimension_Selection] [bit] NULL, [Valid_Season_Selection] [bit] NULL, [Valid_Price_Type_Selection] [bit] NULL, [Valid_First_Cost_Currency_Selection] [bit] NULL, [Color] [int] NULL, [Valid_Color_Selection] [bit] NULL, [Active_Count] [int] NULL, [DimensionColorSizeActiveBooleanSum] [bit] NULL, CONSTRAINT [PK_Fabric_Info] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
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

-- Fabric_Attributes
SET @TableName = @TablePrefix + N'Fabric_Attributes';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Knit_Type] [int] NULL, [Wales_3350] [int] NULL, [Bf_Wash_Swt_Weight] [decimal](18, 2) NULL, [Aft_Wash_Swt_Weight] [decimal](18, 2) NULL, [Weave_Type] [int] NULL, [Warp_Yarns_p_in] [nvarchar](255) NULL, [Weft_Yarns_p_in] [nvarchar](255) NULL, [Cuttable_Width] [decimal](18, 2) NULL, [Yarn_Size] [int] NULL, [Courses] [int] NULL, [Rib_Knit_Stitches] [int] NULL, [Yarn_Ply] [int] NULL, [WPI_wrap_per_inch] [int] NULL, [Knit_Machine_Type] [int] NULL, [Stretch_Type] [int] NULL, [Thread_Count_Construction] [nvarchar](255) NULL, [Yarn_Type] [int] NULL, [Twist_type] [int] NULL, [Denier_Size] [int] NULL, [Pick_Count] [int] NULL, [Pile_Attribute] [int] NULL, [Non_Conventional_Weave] [int] NULL, [Fill_Power] [int] NULL, [Hand_feel] [int] NULL, [Appearance] [int] NULL, [Drape] [int] NULL, [Comments] [nvarchar](255) NULL, [Coordinator_Comments] [nvarchar](255) NULL, [Wales_5141] [nvarchar](255) NULL, [Fill_Weight] [decimal](18, 2) NULL, [Properties] [int] NULL, [Denim_Dyes] [int] NULL, [Fabric_Type] [int] NULL, [Yarn_Spinning] [int] NULL, [Gauge] [int] NULL, [Knitting_Stitches] [int] NULL, [Purl_Knit_Stitches] [int] NULL, [Knit_Design] [int] NULL, [Wash] [int] NULL, [Denim_Base_Colors] [int] NULL, [Weave_Measurement] [int] NULL, [Woven_Designs] [int] NULL, [Non_Woven_Type] [int] NULL, [Shrinkage_Warp] [nvarchar](255) NULL, [Shrinkage_Weft] [nvarchar](255) NULL, [Growth_Warp] [nvarchar](255) NULL, [Growth_Weft] [nvarchar](255) NULL, [Denim_Fits] [int] NULL, [Composition1] [decimal](18, 1) NULL, [Composition2] [decimal](18, 1) NULL, [Composition3] [decimal](18, 1) NULL, [Composition4] [decimal](18, 1) NULL, [Composition5] [decimal](18, 1) NULL, [Comp1] [int] NULL, [Weight] [decimal](18, 3) NULL, [Width] [decimal](18, 2) NULL, [Composition6] [decimal](18, 1) NULL, [UOM_3356] [int] NULL, [UOM_3358] [int] NULL, [UOM_3382] [int] NULL, [Denim_Category] [int] NULL, [UOM_5139] [int] NULL, [UOM_5140] [int] NULL, [UOM_5142] [int] NULL, [UOM_5144] [int] NULL, [UOM_6855] [int] NULL, [Finish] [int] NULL, [Compositionfiber1] [int] NULL, [Compositionfiber2] [int] NULL, [Compositionfiber3] [int] NULL, [Compositionfiber4] [int] NULL, [Compositionfiber5] [int] NULL, [Comp2] [int] NULL, [Weight_Unit] [int] NULL, [Width_Unit] [int] NULL, [Compositionfiber6] [int] NULL, [state_7066] [nvarchar](255) NULL, [state_7069] [nvarchar](255) NULL, [state_7072] [nvarchar](255) NULL, [state_7075] [nvarchar](255) NULL, [state_7078] [nvarchar](255) NULL, [Comp3] [int] NULL, [Per_7137] [int] NULL, [Per_7138] [int] NULL, [Per_7139] [int] NULL, [Per_7140] [int] NULL, [Per_7143] [int] NULL, [Per_7146] [int] NULL, [state_7316] [nvarchar](255) NULL, [Comp4] [int] NULL, [Comp5] [int] NULL, [Comp6] [int] NULL, [Fiber1CB] [int] NULL, [Fiber2CB] [int] NULL, [Fiber3CB] [int] NULL, [Fiber4CB] [int] NULL, [Fiber5CB] [int] NULL, [Fiber6CB] [int] NULL, [Fiber1IB] [nvarchar](255) NULL, [Fiber2IB] [nvarchar](255) NULL, [Fiber3IB] [nvarchar](255) NULL, [Fiber4IB] [nvarchar](255) NULL, [Fiber5IB] [nvarchar](255) NULL, [Fiber6IB] [nvarchar](255) NULL, [Comp_ok] [bit] NULL, [Total_Composition] [nvarchar](255) NULL, [CompositionDDL] [int] NULL, [Valid_Selection] [bit] NULL, [CompositionTXT] [nvarchar](255) NULL, [Percent_Chk] [bit] NULL, [comp1ok] [bit] NULL, [comp2ok] [bit] NULL, [comp3ok] [bit] NULL, [comp4ok] [bit] NULL, [comp5ok] [bit] NULL, [comp6ok] [bit] NULL, [Comp1_Tx] [nvarchar](255) NULL, [Comp2_Tx] [nvarchar](255) NULL, [Comp3_Tx] [nvarchar](255) NULL, [Comp4_Tx] [nvarchar](255) NULL, [Comp5_Tx] [nvarchar](255) NULL, [Comp6_Tx] [nvarchar](255) NULL, [Main_Fabric_Composition] [nvarchar](255) NULL, CONSTRAINT [PK_Fabric_Attributes] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
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

-- Fabric_Cost
SET @TableName = @TablePrefix + N'Fabric_Cost';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Price_By] [int] NULL, [Final_Fabric_Cost_Meter] [int] NULL, [Rmb_Cost_Per_Yard] [int] NULL, [Final_Fabric_Cost_Yard] [int] NULL, [Rmb_Cost_Per_Meter] [int] NULL, [Fabric_Price_By] [int] NULL, [Yarn_Price_Lbs] [int] NULL, [Rmb_Payment_Terms] [int] NULL, [Fabric_Final_Cost_Meter] [decimal](18, 2) NULL, [Yarn_Price_Kg] [int] NULL, [Remarks_7061] [nvarchar](255) NULL, [Fabric_Final_Cost_Yard] [decimal](18, 2) NULL, [Dye_Surcharge] [nvarchar](255) NULL, [Currency] [int] NULL, [Payment_Term] [int] NULL, [MOQ] [nvarchar](255) NULL, [MCQ] [nvarchar](255) NULL, [Dev_Fees] [nvarchar](255) NULL, [Lead_Time] [nvarchar](255) NULL, [Remarks_6848] [nvarchar](255) NULL, CONSTRAINT [PK_Fabric_Cost] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
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

-- Fabric_Policy
SET @TableName = @TablePrefix + N'Fabric_Policy';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Policy_File] [nvarchar](255) NULL, CONSTRAINT [PK_Fabric_Policy] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
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

-- Fabric_Testing
SET @TableName = @TablePrefix + N'Fabric_Testing';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Bulk_Fabric_Status] [int] NULL, [Quality_Standards] [nvarchar](255) NULL, [Standard_Bodies] [int] NULL, [Bulk_Fabric_Approved_Date] [datetime] NULL, [QA_Status] [int] NULL, [Standard_Claim] [int] NULL, [Production_Date] [datetime] NULL, [Approved_by] [int] NULL, [Certification_Year] [nvarchar](255) NULL, [Lot_5200] [nvarchar](255) NULL, [Comments] [nvarchar](255) NULL, [Release_to_Manufacturer_5201] [nvarchar](255) NULL, [Lot_5202] [nvarchar](255) NULL, [Release_to_Manufacturer_5203] [nvarchar](255) NULL, CONSTRAINT [PK_Fabric_Testing] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
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

-- Fabric_DenimTracker
SET @TableName = @TablePrefix + N'Fabric_DenimTracker';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Designer] [int] NULL, [SS] [int] NULL, [Fabric_Type] [int] NULL, [Fabric_Mill] [int] NULL, [Fabric] [nvarchar](255) NULL, [Mill_Article] [nvarchar](255) NULL, [Final_Fabric_Content] [nvarchar](255) NULL, [Final_Weight] [int] NULL, [Color_Combo] [nvarchar](255) NULL, [Original_Ref] [nvarchar](255) NULL, [Denim_Base_Color] [int] NULL, [Wash_Code] [nvarchar](255) NULL, [Color] [int] NULL, [Sample_Type] [int] NULL, [Req_Date] [datetime] NULL, [Mill_Send_Date] [datetime] NULL, [Rcvd_Date] [datetime] NULL, [Comments_Date] [datetime] NULL, [Comments_7786] [nvarchar](255) NULL, [Status] [int] NULL, [SMS_Qty_YD_or_M] [nvarchar](255) NULL, [Delivery_Date] [datetime] NULL, [Delivery_Status] [int] NULL, [Send_to] [int] NULL, [Vendor] [int] NULL, [Wash_Leg_Send_Date] [datetime] NULL, [Wash_Leg_Status] [int] NULL, [Raw_Material_Status] [int] NULL, [Weight_Unit] [int] NULL, [Description] [nvarchar](255) NULL, [Comments_7798] [nvarchar](255) NULL, CONSTRAINT [PK_Fabric_DenimTracker] PRIMARY KEY CLUSTERED ([RowId]) );';
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

-- ProductColor
SET @TableName = @TablePrefix + N'ProductColor';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Active] [bit] NULL, [SketchId] [nvarchar](255) NULL, [SS] [int] NULL, [Swatch] [nvarchar](255) NULL, [Name] [nvarchar](255) NULL, [ColorFamilyID] [int] NULL, [Color] [int] NULL, [Color_Code] [nvarchar](255) NULL, [Color_Combo] [nvarchar](255) NULL, [SMS] [bit] NULL, [Reference] [nvarchar](255) NULL, [Comments] [nvarchar](255) NULL, [Description] [nvarchar](255) NULL, [RGB] [nvarchar](255) NULL, [ColorReferenceTypeID] [int] NULL, [ColorFolderPath] [int] NULL, [ReferenceCode] [nvarchar](255) NULL, [ReferenceName] [nvarchar](255) NULL, [NRF] [int] NULL, [ProductColorNRF] [int] NULL, [Approved] [bit] NULL, [Approv_Date] [datetime] NULL, [Image] [nvarchar](255) NULL, [Userdefine1] [int] NULL, [Userdefine2] [int] NULL, [Userdefine3] [nvarchar](255) NULL, [Userdefine4] [nvarchar](255) NULL, [Userdefine5] [nvarchar](255) NULL, [Userdefine6] [nvarchar](255) NULL, [Userdefine7] [nvarchar](255) NULL, [Userdefine8] [nvarchar](255) NULL, [Userdefine9] [nvarchar](255) NULL, [Userdefine10] [nvarchar](255) NULL, [Userdefine11] [nvarchar](255) NULL, [Userdefine12] [nvarchar](255) NULL, [Userdefine13] [nvarchar](255) NULL, [Userdefine14] [nvarchar](255) NULL, [Userdefine15] [nvarchar](255) NULL, [First_Cost] [decimal](18, 2) NULL, [FirstCostCurrency] [int] NULL, [Standard_Cost] [decimal](18, 2) NULL, [Selling_Price] [decimal](18, 2) NULL, [Selling_Currency] [int] NULL, [Retail] [decimal](18, 2) NULL, [Effective_Date] [datetime] NULL, [SupplierColor] [nvarchar](255) NULL, [ERPID] [nvarchar](255) NULL, [Colour_Risk] [int] NULL, [Colour_Risk_Comment] [int] NULL, [Date_SMS] [datetime] NULL, [Bulk_1_Status] [int] NULL, [DateBulk1] [datetime] NULL, [Bulk_2_Status] [int] NULL, [DateBulk2] [datetime] NULL, [Bulk_3_Status] [int] NULL, [DateBulk3] [datetime] NULL, [CAN_Qty_Color] [int] NULL, [CAN_Qty_Total] [int] NULL, [USA_Qty_Color] [int] NULL, [USA_Qty_Total] [int] NULL, [FOB_Price] [decimal](18, 2) NULL, [CAD] [int] NULL, [CAD_WS_Price] [decimal](18, 2) NULL, [CAD_RET_Price] [decimal](18, 2) NULL, [CAD_VIP_Price] [int] NULL, [USD] [int] NULL, [USD_WS_Price] [decimal](18, 2) NULL, [USD_RET_Price] [decimal](18, 2) NULL, [USD_VIP_Price] [int] NULL, [CAN_PO_s] [nvarchar](255) NULL, [CAN_PO_Created_Date] [nvarchar](255) NULL, [CAN_ETA] [nvarchar](255) NULL, [USA_PO_s] [nvarchar](255) NULL, [USA_PO_Created_Date] [nvarchar](255) NULL, [USA_ETA] [nvarchar](255) NULL, [Fabric_Price_p_m] [int] NULL, [Fabric_Price_p_y] [int] NULL, [Color_Price_p_m] [decimal](18, 2) NULL, [Color_Price_p_y] [decimal](18, 2) NULL, CONSTRAINT [PK_ProductColor] PRIMARY KEY CLUSTERED ([RowId]) );';
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

