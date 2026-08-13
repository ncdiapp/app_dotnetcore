-- =============================================================================
-- PLM DW â†’ APP physical tables (generated â€” see ImportFromPLMDW/PROMPT.md)
-- EXECUTION ORDER:
--   1. 1_PlmDw_Tables.sql          (this file)
--   2. 2_PlmDw_FieldMapping.sql
--   3. 3_PlmDw_ImportFromDW.sql
--   3b. 3b_Tchp_ImportFromDW.sql   (when techPack config present â€” StyleSpec/Pom/Fit)
--   3c. 3c_PlmDw_ImportSimpleQc.sql (when SpecQC / Simple QC QX1 present)
--   4. 4_PlmDw_ImportBlueprint.json + Phase D Execute
--   5. 5_PlmDw_ImportBomColorwayGrandchild.sql  (when BOM colorway grids detected)
--   6. 6_PlmDw_CleanupBomColorwayStaging.sql
-- USER SETTINGS (single batch - do not split with GO):
--   @TablePrefix     table prefix, include trailing underscore (default Plm_)
--   @RootTableSuffix root table name after prefix (default ReferenceBasicInfo)
-- Source: plmDW Tab/Grid wide tables for user TabId set
-- TechPack (optional techPack in dwTabImportConfig): SpecFit/SpecGrading â†’ Tchp*;
--   SpecQC â†’ Plm_SimpleQC + Plm_SimpleQCResult (QX1); Size_Run/Base_Size/Measure_Unit â†’ TchpStyleSpec.
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

-- Style_Header_V2K_ERP
SET @TableName = @TablePrefix + N'Style_Header_V2K_ERP';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Classification] [int] NULL, [Product_Type] [int] NULL, [Season_3] [int] NULL, [Collection] [int] NULL, [Group] [int] NULL, [Sketch] [int] NULL, [Division_8] [int] NULL, [Size_Range_10] [int] NULL, [Dimension_11] [int] NULL, [Article] [nvarchar](4000) NULL, [Description] [nvarchar](4000) NULL, [Composition] [int] NULL, [Made_in_Country] [int] NULL, [Vendor] [int] NULL, [Country_Of_Origin] [int] NULL, [Product_Manager] [int] NULL, [Long_Description] [nvarchar](4000) NULL, [Sample_Size_Detail] [int] NULL, [Date_Shipping_1] [datetime] NULL, [Date_Shipping_2] [datetime] NULL, [CancelByDate] [datetime] NULL, [ProductTypeGroup] [int] NULL, [Size_Detail_Dispaly] [nvarchar](4000) NULL, [Division_186] [int] NULL, [Created_By] [nvarchar](4000) NULL, [Last_Revised_By] [nvarchar](4000) NULL, [ProductReferenceId] [nvarchar](4000) NULL, [Style_Status] [int] NULL, [State_3005] [decimal](18, 2) NULL, [Sample_Status] [nvarchar](4000) NULL, [CB_Fit_1_Status] [int] NULL, [IN_Fit_1_State] [decimal](18, 2) NULL, [CB_Fit_2_Status] [int] NULL, [IN_Fit_2_State] [decimal](18, 2) NULL, [CB_Fit_3_Status] [int] NULL, [IN_Fit_3_State] [decimal](18, 2) NULL, [CB_Fit_4_Status] [int] NULL, [IN_Fit_4_State] [decimal](18, 2) NULL, [CB_PP_1_Status] [int] NULL, [IN_PP_1_State] [decimal](18, 2) NULL, [CB_PP_2_Status] [int] NULL, [IN_PP_2_State] [decimal](18, 2) NULL, [CB_PP_3_Status] [int] NULL, [IN_PP_3_State] [decimal](18, 2) NULL, [CB_TOP_1_Status] [int] NULL, [IN_TOP_1_State] [decimal](18, 2) NULL, [CB_TOP_2_Status] [int] NULL, [IN_TOP_2_State] [decimal](18, 2) NULL, [CHK_Fit_1_Latest] [nvarchar](4000) NULL, [CHK_Fit_2_Latest] [nvarchar](4000) NULL, [CHK_Fit_3_Latest] [nvarchar](4000) NULL, [CHK_Fit_4_Latest] [nvarchar](4000) NULL, [CHK_PP_1_Latest] [nvarchar](4000) NULL, [CHK_PP_2_Latest] [nvarchar](4000) NULL, [CHK_PP_3_Latest] [nvarchar](4000) NULL, [CHK_TOP_1_Latest] [nvarchar](4000) NULL, [CHK_TOP_2_Latest] [nvarchar](4000) NULL, [fit1status_IB] [nvarchar](4000) NULL, [fit2status_IB] [nvarchar](4000) NULL, [fit3status_IB] [nvarchar](4000) NULL, [fit4status_IB] [nvarchar](4000) NULL, [pp1status_IB] [nvarchar](4000) NULL, [pp2status_IB] [nvarchar](4000) NULL, [pp3status_IB] [nvarchar](4000) NULL, [top1status_IB] [nvarchar](4000) NULL, [top2status_IB] [nvarchar](4000) NULL, [CB_Fit_1_Type] [int] NULL, [fit1type_IB] [nvarchar](4000) NULL, [CB_Fit_2_Type] [int] NULL, [fit2type_IB] [nvarchar](4000) NULL, [CB_Fit_3_Type] [int] NULL, [fit3type_IB] [nvarchar](4000) NULL, [CB_Fit_4_Type] [int] NULL, [fit4type_IB] [nvarchar](4000) NULL, [CB_PP_1_Type] [int] NULL, [pp1type_IB] [nvarchar](4000) NULL, [CB_PP_2_Type] [int] NULL, [pp2type_IB] [nvarchar](4000) NULL, [CB_PP_3_Type] [int] NULL, [pp3type_IB] [nvarchar](4000) NULL, [CB_TOP_1_Type] [int] NULL, [top1type_IB] [nvarchar](4000) NULL, [CB_TOP_2_Type] [int] NULL, [top2type_IB] [nvarchar](4000) NULL, [Calc_Fit_Status] [nvarchar](4000) NULL, [Item_Type] [int] NULL, [Supplier_Number] [nvarchar](4000) NULL, [Publish_to_ERP] [nvarchar](4000) NULL, [Published_to_ERP] [nvarchar](4000) NULL, [Supplier_Article_Number] [nvarchar](4000) NULL, [Global_ID] [nvarchar](4000) NULL, [Publish_Failed_to_ERP] [nvarchar](4000) NULL, [Composition_txt] [nvarchar](4000) NULL, [Sizerun] [int] NULL, [NumberSize] [decimal](18, 2) NULL, [Composition1] [decimal](18, 1) NULL, [Compositionfiber1] [int] NULL, [Composition2] [decimal](18, 1) NULL, [Compositionfiber2] [int] NULL, [Composition3] [decimal](18, 1) NULL, [Compositionfiber3] [int] NULL, [Comp1] [decimal](18, 1) NULL, [Comp2] [decimal](18, 1) NULL, [Comp3] [decimal](18, 1) NULL, [Fiber1CB] [int] NULL, [Fiber2CB] [int] NULL, [Fiber3CB] [int] NULL, [Fiber1IB] [nvarchar](4000) NULL, [Fiber2IB] [nvarchar](4000) NULL, [Fiber3IB] [nvarchar](4000) NULL, [Comp_ok] [nvarchar](4000) NULL, [Total_Composition] [nvarchar](4000) NULL, [Image1] [int] NULL, [Image2] [int] NULL, [Image1Link] [nvarchar](4000) NULL, [Image2Link] [nvarchar](4000) NULL, [style_size] [int] NULL, [sizeorder] [decimal](18, 2) NULL, [CompositionDDL] [int] NULL, [Valid_Selection] [nvarchar](4000) NULL, [CompositionTXT] [nvarchar](4000) NULL, [comp1ok] [nvarchar](4000) NULL, [comp2ok] [nvarchar](4000) NULL, [comp3ok] [nvarchar](4000) NULL, [Product_Code] [nvarchar](4000) NULL, [Size_Range_5022] [int] NULL, [DivisionBlock] [int] NULL, [Product_Class] [int] NULL, [Dimension_5025] [int] NULL, [Season_5026] [int] NULL, [Price_Type] [int] NULL, [First_Cost_Currency] [int] NULL, [Valid_Size_Selection] [nvarchar](4000) NULL, [Valid_Product_Code_Selection] [nvarchar](4000) NULL, [Valid_DivisionBlock_Selection] [nvarchar](4000) NULL, [Valid_Product_Class_Selection] [nvarchar](4000) NULL, [Valid_Dimension_Selection] [nvarchar](4000) NULL, [Valid_Season_Selection] [nvarchar](4000) NULL, [Valid_Price_Type_Selection] [nvarchar](4000) NULL, [Valid_First_Cost_Currency_Selection] [nvarchar](4000) NULL, [Color] [decimal](18, 2) NULL, [Valid_Color_Selection] [nvarchar](4000) NULL, [Active_Count] [decimal](18, 2) NULL, [DimensionColorSizeActiveBooleanSum] [nvarchar](4000) NULL, [Style] [int] NULL, [Article_gen] [nvarchar](4000) NULL, [Styleautogencalc] [int] NULL, [EmptyPC] [nvarchar](4000) NULL, [test] [nvarchar](4000) NULL, [Comp1_Tx] [nvarchar](4000) NULL, [Comp2_Tx] [nvarchar](4000) NULL, [Comp3_Tx] [nvarchar](4000) NULL, [state_5078] [nvarchar](4000) NULL, [state_5079] [nvarchar](4000) NULL, [state_5080] [nvarchar](4000) NULL, [Percent_Chk] [nvarchar](4000) NULL, [Comp4] [decimal](18, 1) NULL, [Comp5] [decimal](18, 1) NULL, [Fiber4CB] [int] NULL, [Fiber5CB] [int] NULL, [Fiber4IB] [nvarchar](4000) NULL, [Fiber5IB] [nvarchar](4000) NULL, [comp4ok] [nvarchar](4000) NULL, [comp5ok] [nvarchar](4000) NULL, [Comp4_Tx] [nvarchar](4000) NULL, [Comp5_Tx] [nvarchar](4000) NULL, [French] [nvarchar](4000) NULL, [Name] [nvarchar](4000) NULL, [Product_Type_txt] [nvarchar](4000) NULL, [sketch_id] [nvarchar](4000) NULL, [ddl] [int] NULL, [Collection_txt] [nvarchar](4000) NULL, [Comp6] [decimal](18, 1) NULL, [Fiber6CB] [int] NULL, [Fiber6IB] [nvarchar](4000) NULL, [comp6ok] [nvarchar](4000) NULL, [Comp6_Tx] [nvarchar](4000) NULL, [ProductTypeGroup_txt] [nvarchar](4000) NULL, [Subcategory] [int] NULL, [ERP_Season] [int] NULL, [French_Name] [nvarchar](4000) NULL, CONSTRAINT [PK_Style_Header_V2K_ERP] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Classification')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Classification] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Season_3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Season_3] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Collection')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Collection] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Group')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Group] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sketch')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sketch] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Division_8')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Division_8] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Size_Range_10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Size_Range_10] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Dimension_11')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Dimension_11] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Article')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Article] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Article] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Composition')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Composition] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Made_in_Country')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Made_in_Country] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Vendor')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Vendor] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Country_Of_Origin')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Country_Of_Origin] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Manager')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Manager] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Long_Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Long_Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Long_Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sample_Size_Detail')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sample_Size_Detail] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Date_Shipping_1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Date_Shipping_1] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Date_Shipping_2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Date_Shipping_2] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CancelByDate')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CancelByDate] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ProductTypeGroup')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ProductTypeGroup] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Size_Detail_Dispaly')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Size_Detail_Dispaly] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Size_Detail_Dispaly] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Division_186')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Division_186] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Created_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Created_By] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Created_By] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Last_Revised_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Last_Revised_By] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Last_Revised_By] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ProductReferenceId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ProductReferenceId] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ProductReferenceId] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Style_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Style_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'State_3005')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [State_3005] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sample_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sample_Status] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Sample_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_Fit_1_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_Fit_1_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'IN_Fit_1_State')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [IN_Fit_1_State] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_Fit_2_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_Fit_2_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'IN_Fit_2_State')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [IN_Fit_2_State] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_Fit_3_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_Fit_3_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'IN_Fit_3_State')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [IN_Fit_3_State] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_Fit_4_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_Fit_4_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'IN_Fit_4_State')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [IN_Fit_4_State] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_PP_1_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_PP_1_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'IN_PP_1_State')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [IN_PP_1_State] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_PP_2_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_PP_2_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'IN_PP_2_State')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [IN_PP_2_State] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_PP_3_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_PP_3_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'IN_PP_3_State')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [IN_PP_3_State] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_TOP_1_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_TOP_1_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'IN_TOP_1_State')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [IN_TOP_1_State] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_TOP_2_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_TOP_2_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'IN_TOP_2_State')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [IN_TOP_2_State] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CHK_Fit_1_Latest')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CHK_Fit_1_Latest] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_Fit_1_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CHK_Fit_2_Latest')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CHK_Fit_2_Latest] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_Fit_2_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CHK_Fit_3_Latest')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CHK_Fit_3_Latest] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_Fit_3_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CHK_Fit_4_Latest')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CHK_Fit_4_Latest] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_Fit_4_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CHK_PP_1_Latest')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CHK_PP_1_Latest] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_PP_1_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CHK_PP_2_Latest')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CHK_PP_2_Latest] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_PP_2_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CHK_PP_3_Latest')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CHK_PP_3_Latest] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_PP_3_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CHK_TOP_1_Latest')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CHK_TOP_1_Latest] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_TOP_1_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CHK_TOP_2_Latest')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CHK_TOP_2_Latest] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_TOP_2_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'fit1status_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [fit1status_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit1status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'fit2status_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [fit2status_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit2status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'fit3status_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [fit3status_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit3status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'fit4status_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [fit4status_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit4status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'pp1status_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [pp1status_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp1status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'pp2status_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [pp2status_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp2status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'pp3status_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [pp3status_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp3status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'top1status_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [top1status_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [top1status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'top2status_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [top2status_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [top2status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_Fit_1_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_Fit_1_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'fit1type_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [fit1type_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit1type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_Fit_2_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_Fit_2_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'fit2type_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [fit2type_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit2type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_Fit_3_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_Fit_3_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'fit3type_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [fit3type_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit3type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_Fit_4_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_Fit_4_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'fit4type_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [fit4type_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit4type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_PP_1_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_PP_1_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'pp1type_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [pp1type_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp1type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_PP_2_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_PP_2_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'pp2type_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [pp2type_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp2type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_PP_3_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_PP_3_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'pp3type_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [pp3type_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp3type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_TOP_1_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_TOP_1_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'top1type_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [top1type_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [top1type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CB_TOP_2_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CB_TOP_2_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'top2type_IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [top2type_IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [top2type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Calc_Fit_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Calc_Fit_Status] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Calc_Fit_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Item_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Item_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Number')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Number] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Supplier_Number] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Publish_to_ERP')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Publish_to_ERP] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Publish_to_ERP] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Published_to_ERP')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Published_to_ERP] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Published_to_ERP] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Article_Number')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Article_Number] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Supplier_Article_Number] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Global_ID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Global_ID] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Global_ID] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Publish_Failed_to_ERP')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Publish_Failed_to_ERP] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Publish_Failed_to_ERP] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Composition_txt')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Composition_txt] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Composition_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sizerun')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sizerun] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'NumberSize')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [NumberSize] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Composition1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Composition1] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Compositionfiber1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Compositionfiber1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Composition2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Composition2] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Compositionfiber2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Compositionfiber2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Composition3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Composition3] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Compositionfiber3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Compositionfiber3] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp1] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp2] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp3] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber1CB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber1CB] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber2CB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber2CB] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber3CB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber3CB] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber1IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber1IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber1IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber2IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber2IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber2IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber3IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber3IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber3IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp_ok')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp_ok] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp_ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total_Composition')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total_Composition] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image1Link')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image1Link] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Image1Link] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image2Link')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image2Link] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Image2Link] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'style_size')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [style_size] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'sizeorder')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [sizeorder] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CompositionDDL')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CompositionDDL] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_Selection')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_Selection] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CompositionTXT')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CompositionTXT] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CompositionTXT] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp1ok')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp1ok] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp1ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp2ok')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp2ok] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp2ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp3ok')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp3ok] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp3ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Code] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Product_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Size_Range_5022')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Size_Range_5022] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DivisionBlock')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DivisionBlock] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Class')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Class] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Dimension_5025')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Dimension_5025] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Season_5026')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Season_5026] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Price_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Price_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'First_Cost_Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [First_Cost_Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_Size_Selection')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_Size_Selection] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Size_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_Product_Code_Selection')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_Product_Code_Selection] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Product_Code_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_DivisionBlock_Selection')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_DivisionBlock_Selection] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_DivisionBlock_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_Product_Class_Selection')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_Product_Class_Selection] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Product_Class_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_Dimension_Selection')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_Dimension_Selection] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Dimension_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_Season_Selection')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_Season_Selection] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Season_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_Price_Type_Selection')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_Price_Type_Selection] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Price_Type_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_First_Cost_Currency_Selection')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_First_Cost_Currency_Selection] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_First_Cost_Currency_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_Color_Selection')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_Color_Selection] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Color_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Active_Count')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Active_Count] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DimensionColorSizeActiveBooleanSum')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DimensionColorSizeActiveBooleanSum] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DimensionColorSizeActiveBooleanSum] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Style')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Style] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Article_gen')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Article_gen] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Article_gen] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Styleautogencalc')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Styleautogencalc] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'EmptyPC')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [EmptyPC] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [EmptyPC] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'test')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [test] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [test] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp1_Tx')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp1_Tx] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp1_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp2_Tx')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp2_Tx] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp2_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp3_Tx')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp3_Tx] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp3_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'state_5078')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [state_5078] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [state_5078] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'state_5079')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [state_5079] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [state_5079] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'state_5080')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [state_5080] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [state_5080] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Percent_Chk')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Percent_Chk] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Percent_Chk] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp4')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp4] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp5')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp5] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber4CB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber4CB] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber5CB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber5CB] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber4IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber4IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber4IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber5IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber5IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber5IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp4ok')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp4ok] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp4ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp5ok')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp5ok] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp5ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp4_Tx')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp4_Tx] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp4_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp5_Tx')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp5_Tx] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp5_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'French')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [French] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [French] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Name')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Name] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Name] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Type_txt')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Type_txt] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Product_Type_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'sketch_id')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [sketch_id] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [sketch_id] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ddl')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ddl] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Collection_txt')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Collection_txt] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Collection_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp6')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp6] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber6CB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber6CB] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber6IB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber6IB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber6IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp6ok')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp6ok] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp6ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp6_Tx')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp6_Tx] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp6_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ProductTypeGroup_txt')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ProductTypeGroup_txt] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ProductTypeGroup_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Subcategory')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Subcategory] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ERP_Season')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ERP_Season] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'French_Name')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [French_Name] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
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

-- QC
SET @TableName = @TablePrefix + N'QC';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [QC_Color] [int] NULL, [Comment_Date] [datetime] NULL, [Colors___Sizes_Comments] [nvarchar](4000) NULL, [Measurement_Comments] [nvarchar](4000) NULL, [Make_up_Fit_Comments] [nvarchar](4000) NULL, [Conclusion] [nvarchar](4000) NULL, CONSTRAINT [PK_QC] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'QC_Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [QC_Color] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Colors___Sizes_Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Colors___Sizes_Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Colors___Sizes_Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Measurement_Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Measurement_Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Measurement_Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Make_up_Fit_Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Make_up_Fit_Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Make_up_Fit_Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Conclusion')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Conclusion] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Conclusion] [nvarchar](4000) NULL;';
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

-- Testing
SET @TableName = @TablePrefix + N'Testing';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Security_Group] [int] NULL, [Created_by] [int] NULL, [Testing_Status] [int] NULL, [State] [decimal](18, 2) NULL, [Test_Date_Needed] [datetime] NULL, CONSTRAINT [PK_Testing] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Security_Group')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Security_Group] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Created_by')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Created_by] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Testing_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Testing_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'State')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [State] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Test_Date_Needed')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Test_Date_Needed] [datetime] NULL;'; EXEC sp_executesql @sql; END
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

-- SimpleQC
SET @TableName = @TablePrefix + N'SimpleQC';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [CriticalPoint] [nvarchar](4000) NULL, [BodyPartDetailIDWDimDetailID] [int] NULL, [Code] [nvarchar](4000) NULL, [BodyPartName] [nvarchar](4000) NULL, [BodyPartDesc] [nvarchar](4000) NULL, [HowToMeasure] [nvarchar](4000) NULL, [Tolerance] [nvarchar](4000) NULL, [GradingBaseSize] [nvarchar](4000) NULL, [Commtents] [nvarchar](4000) NULL, [AddDesc] [nvarchar](4000) NULL, [NeedToApplyGradingRule] [nvarchar](4000) NULL, [Dimension] [int] NULL, [DimensionDetail] [int] NULL, CONSTRAINT [PK_SimpleQC] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CriticalPoint')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CriticalPoint] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CriticalPoint] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'BodyPartDetailIDWDimDetailID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [BodyPartDetailIDWDimDetailID] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Code] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'BodyPartName')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [BodyPartName] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [BodyPartName] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'BodyPartDesc')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [BodyPartDesc] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [BodyPartDesc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HowToMeasure')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HowToMeasure] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [HowToMeasure] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Tolerance')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Tolerance] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Tolerance] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'GradingBaseSize')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [GradingBaseSize] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [GradingBaseSize] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Commtents')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Commtents] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Commtents] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'AddDesc')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [AddDesc] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [AddDesc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'NeedToApplyGradingRule')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [NeedToApplyGradingRule] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [NeedToApplyGradingRule] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Dimension')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Dimension] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DimensionDetail')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DimensionDetail] [int] NULL;'; EXEC sp_executesql @sql; END
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

-- SimpleQCResult (QX1): one size row under SimpleQCResult; SizeRunSizeId = SizeOrder position N
SET @TableName = @TablePrefix + N'SimpleQCResult';
SET @HostTable = @TablePrefix + N'SimpleQC';
SET @ParentFkName = N'FK_' + @TableName + N'_Parent';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' (
        [RowId] INT IDENTITY(1,1) NOT NULL,
        [ParentRowId] INT NOT NULL,
        [SizeRunSizeId] INT NOT NULL,
        [GradingSize] NVARCHAR(4000) NULL,
        [QCSize] NVARCHAR(4000) NULL,
        [Difference] NVARCHAR(4000) NULL,
        [QCSizeBeforeWash] NVARCHAR(4000) NULL,
        [DiffBeforeWashAndGrading] NVARCHAR(4000) NULL,
        [QCAfterWashIron] NVARCHAR(4000) NULL,
        [DiffAfterIronAndGrading] NVARCHAR(4000) NULL,
        [QCAfterIron] NVARCHAR(4000) NULL,
        CONSTRAINT [PK_SimpleQCResult] PRIMARY KEY CLUSTERED ([RowId])
    );';
    EXEC sp_executesql @sql;
    PRINT N'Created ' + @TableName;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ParentRowId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ParentRowId] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SizeRunSizeId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SizeRunSizeId] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'GradingSize')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [GradingSize] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [GradingSize] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'QCSize')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [QCSize] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [QCSize] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Difference')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Difference] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Difference] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'QCSizeBeforeWash')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [QCSizeBeforeWash] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [QCSizeBeforeWash] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DiffBeforeWashAndGrading')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DiffBeforeWashAndGrading] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DiffBeforeWashAndGrading] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'QCAfterWashIron')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [QCAfterWashIron] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [QCAfterWashIron] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DiffAfterIronAndGrading')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DiffAfterIronAndGrading] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DiffAfterIronAndGrading] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'QCAfterIron')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [QCAfterIron] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [QCAfterIron] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql;
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @ParentFkName)
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName)
        + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@ParentFkName)
        + N' FOREIGN KEY ([ParentRowId]) REFERENCES dbo.' + QUOTENAME(@HostTable) + N' ([RowId]);';
    EXEC sp_executesql @sql;
END


-- Product_Test_Grid_reg
SET @TableName = @TablePrefix + N'Product_Test_Grid_reg';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Test] [decimal](18, 2) NULL, [Test_Req] [nvarchar](4000) NULL, [Status] [int] NULL, [Test_File] [nvarchar](4000) NULL, [Needed_By] [datetime] NULL, [Completed] [datetime] NULL, [Test_Comments] [nvarchar](4000) NULL, CONSTRAINT [PK_Product_Test_Grid_reg] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Test')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Test] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Test_Req')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Test_Req] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Test_Req] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Test_File')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Test_File] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Test_File] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Needed_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Needed_By] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Completed')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Completed] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Test_Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Test_Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Test_Comments] [nvarchar](4000) NULL;';
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

