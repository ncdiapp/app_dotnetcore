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

-- Style_Summary
SET @TableName = @TablePrefix + N'Style_Summary';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Composition] [int] NULL, [Made_in_Country] [int] NULL, [Security_Group_3094] [int] NULL, [Designer] [int] NULL, [Security_Group_3098] [int] NULL, [Sourcing] [int] NULL, [Security_Group_3950] [int] NULL, [Technical_Manager] [int] NULL, [Coordinator] [int] NULL, [QC_Team] [int] NULL, [Release_to_Manufacturer] [int] NULL, [Security_Group_5096] [int] NULL, [Security_Group_5097] [int] NULL, [Comments] [nvarchar](4000) NULL, [Tech_Pack_Send_to_Vendor] [int] NULL, [Tech_Pack_Sent_Date] [datetime] NULL, [RRP] [decimal](18, 2) NULL, [Target_Cost_FOB] [decimal](18, 2) NULL, [Target_Margin] [decimal](18, 2) NULL, [Target_Landed] [decimal](18, 2) NULL, [Drop] [nvarchar](4000) NULL, [Launch_Month] [nvarchar](4000) NULL, [Order_Month] [nvarchar](4000) NULL, [Ship_Window_Start] [datetime] NULL, [Ship_Window_End] [datetime] NULL, [Date_Shipping_1] [datetime] NULL, [ETD] [datetime] NULL, [Floorset_Date] [datetime] NULL, [Cancel_By_Date] [datetime] NULL, [Embellishment_A_W_Techniques] [nvarchar](4000) NULL, [Wash_Thread] [nvarchar](4000) NULL, [Pricing_Comment] [nvarchar](4000) NULL, [Release_Day] [datetime] NULL, [Comp1__7079] [decimal](18, 1) NULL, [Comp2__7080] [decimal](18, 1) NULL, [Comp3__7081] [decimal](18, 1) NULL, [Fiber1CB_7082] [int] NULL, [Fiber2CB_7083] [int] NULL, [Fiber3CB_7084] [int] NULL, [Fiber1IB_7085] [nvarchar](4000) NULL, [Fiber2IB_7086] [nvarchar](4000) NULL, [Fiber3IB_7087] [nvarchar](4000) NULL, [Comp_ok_7088] [nvarchar](4000) NULL, [Total_Composition_7089] [nvarchar](4000) NULL, [CompositionDDL_7090] [int] NULL, [Valid_Selection_7091] [nvarchar](4000) NULL, [CompositionTXT_7092] [nvarchar](4000) NULL, [comp1ok_7093] [nvarchar](4000) NULL, [comp2ok_7094] [nvarchar](4000) NULL, [comp3ok_7095] [nvarchar](4000) NULL, [Comp1_Tx_7096] [nvarchar](4000) NULL, [Comp2_Tx_7097] [nvarchar](4000) NULL, [Comp3_Tx_7098] [nvarchar](4000) NULL, [Percent_Chk_7099] [nvarchar](4000) NULL, [Comp4__7100] [decimal](18, 1) NULL, [Comp5__7101] [decimal](18, 1) NULL, [Fiber4CB_7102] [int] NULL, [Fiber5CB_7103] [int] NULL, [Fiber4IB_7104] [nvarchar](4000) NULL, [Fiber5IB_7105] [nvarchar](4000) NULL, [comp4ok_7106] [nvarchar](4000) NULL, [comp5ok_7107] [nvarchar](4000) NULL, [Comp4_Tx_7108] [nvarchar](4000) NULL, [Comp5_Tx_7109] [nvarchar](4000) NULL, [Main_Fabric_7156] [nvarchar](4000) NULL, [Main_Fabric_Composition_7159] [nvarchar](4000) NULL, [Weight_7169] [decimal](18, 2) NULL, [UOM_7170] [nvarchar](4000) NULL, [Designer_2] [int] NULL, [Designer_1] [int] NULL, [Calc_7220] [nvarchar](4000) NULL, [Main_Fabric_7221] [nvarchar](4000) NULL, [Calc_7222] [nvarchar](4000) NULL, [Weight_7223] [decimal](18, 2) NULL, [UOM_7224] [nvarchar](4000) NULL, [Comp1__7225] [decimal](18, 1) NULL, [Comp2__7226] [decimal](18, 1) NULL, [Comp3__7227] [decimal](18, 1) NULL, [Fiber1CB_7228] [nvarchar](4000) NULL, [Fiber2CB_7229] [nvarchar](4000) NULL, [Fiber3CB_7230] [nvarchar](4000) NULL, [Fiber1IB_7231] [nvarchar](4000) NULL, [Fiber2IB_7232] [nvarchar](4000) NULL, [Fiber3IB_7233] [nvarchar](4000) NULL, [Comp_ok_7234] [nvarchar](4000) NULL, [Total_Composition_7235] [nvarchar](4000) NULL, [CompositionDDL_7236] [nvarchar](4000) NULL, [Valid_Selection_7237] [nvarchar](4000) NULL, [CompositionTXT_7238] [nvarchar](4000) NULL, [comp1ok_7239] [nvarchar](4000) NULL, [comp2ok_7240] [nvarchar](4000) NULL, [comp3ok_7241] [nvarchar](4000) NULL, [Comp1_Tx_7242] [nvarchar](4000) NULL, [Comp2_Tx_7243] [nvarchar](4000) NULL, [Comp3_Tx_7244] [nvarchar](4000) NULL, [Percent_Chk_7245] [nvarchar](4000) NULL, [Comp4__7246] [decimal](18, 1) NULL, [Comp5__7247] [decimal](18, 1) NULL, [Fiber4CB_7248] [nvarchar](4000) NULL, [Fiber5CB_7249] [nvarchar](4000) NULL, [Fiber4IB_7250] [nvarchar](4000) NULL, [Fiber5IB_7251] [nvarchar](4000) NULL, [comp4ok_7252] [nvarchar](4000) NULL, [comp5ok_7253] [nvarchar](4000) NULL, [Comp4_Tx_7254] [nvarchar](4000) NULL, [Comp5_Tx_7255] [nvarchar](4000) NULL, [Main_Fabric_Composition_7256] [nvarchar](4000) NULL, [Final_Main_Fabric] [nvarchar](4000) NULL, [Final_Weight] [nvarchar](4000) NULL, [Final_UOM] [nvarchar](4000) NULL, [Main_Fabric_10] [nvarchar](4000) NULL, [Main_Weight_10] [decimal](18, 2) NULL, [Main_UOM_10] [nvarchar](4000) NULL, [Main_Fabric_20] [nvarchar](4000) NULL, [Main_Weight_20] [decimal](18, 2) NULL, [Main_UOM_20] [nvarchar](4000) NULL, [Total_Composition_10] [nvarchar](4000) NULL, [Total_Composition_20] [nvarchar](4000) NULL, [Final_Total_Composition] [nvarchar](4000) NULL, [UOM_ID_10] [decimal](18, 2) NULL, [Compositions] [nvarchar](4000) NULL, [Comp6] [decimal](18, 1) NULL, [Fiber6CB] [int] NULL, [Fiber6IB] [nvarchar](4000) NULL, [comp6ok] [nvarchar](4000) NULL, [Comp6_Tx] [nvarchar](4000) NULL, [Type_Group] [nvarchar](4000) NULL, [is_Outerwear_Swim] [nvarchar](4000) NULL, [O_S_Compositions] [nvarchar](4000) NULL, [UOM_ID_20] [decimal](18, 2) NULL, [Final_UOM_ID] [decimal](18, 2) NULL, [Set_Compositions] [nvarchar](4000) NULL, CONSTRAINT [PK_Style_Summary] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Drop] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Launch_Month] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Order_Month] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Embellishment_A_W_Techniques] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Wash_Thread] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Pricing_Comment] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber1IB_7085] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber2IB_7086] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber3IB_7087] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp_ok_7088] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_7089] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Selection_7091] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CompositionTXT_7092] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp1ok_7093] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp2ok_7094] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp3ok_7095] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp1_Tx_7096] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp2_Tx_7097] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp3_Tx_7098] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Percent_Chk_7099] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber4IB_7104] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber5IB_7105] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp4ok_7106] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp5ok_7107] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp4_Tx_7108] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp5_Tx_7109] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_7156] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_Composition_7159] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [UOM_7170] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Calc_7220] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_7221] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Calc_7222] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [UOM_7224] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber1CB_7228] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber2CB_7229] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber3CB_7230] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber1IB_7231] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber2IB_7232] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber3IB_7233] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp_ok_7234] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_7235] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CompositionDDL_7236] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Selection_7237] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CompositionTXT_7238] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp1ok_7239] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp2ok_7240] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp3ok_7241] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp1_Tx_7242] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp2_Tx_7243] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp3_Tx_7244] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Percent_Chk_7245] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber4CB_7248] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber5CB_7249] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber4IB_7250] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber5IB_7251] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp4ok_7252] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp5ok_7253] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp4_Tx_7254] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp5_Tx_7255] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_Composition_7256] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Main_Fabric] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Weight] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_UOM] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_UOM_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_UOM_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Total_Composition] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Compositions] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber6IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp6ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp6_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Type_Group] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [is_Outerwear_Swim] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [O_S_Compositions] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Set_Compositions] [nvarchar](4000) NULL;';
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

-- Colorways
SET @TableName = @TablePrefix + N'Colorways';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Security_Group] [int] NULL, [Created_by_3154] [int] NULL, [Color_Status] [int] NULL, [Comments] [nvarchar](4000) NULL, [Created_by_7111] [int] NULL, [Colors] [nvarchar](4000) NULL, CONSTRAINT [PK_Colorways] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Colors] [nvarchar](4000) NULL;';
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

-- Denim_Details
SET @TableName = @TablePrefix + N'Denim_Details';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Front_View] [int] NULL, [Back_View] [int] NULL, CONSTRAINT [PK_Denim_Details] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
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

-- Artworks
SET @TableName = @TablePrefix + N'Artworks';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Artwork_Approval_Status] [nvarchar](4000) NULL, [Artwork_Approval_Date] [datetime] NULL, [dateisblank_calc] [nvarchar](4000) NULL, [setdate_calc] [nvarchar](4000) NULL, [blankdate_calc] [nvarchar](4000) NULL, CONSTRAINT [PK_Artworks] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Artwork_Approval_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [dateisblank_calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [setdate_calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [blankdate_calc] [nvarchar](4000) NULL;';
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

-- Technical_Details
SET @TableName = @TablePrefix + N'Technical_Details';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Construction_File] [nvarchar](4000) NULL, [Construction_Status] [int] NULL, [Adobe_Illustrator_File] [nvarchar](4000) NULL, [Private_Label_File] [nvarchar](4000) NULL, [Design_Status] [int] NULL, [CAD_Image01] [int] NULL, [CAD_Image02] [int] NULL, [CAD_Image03] [int] NULL, [CAD_Image04] [int] NULL, [CAD_Image05] [int] NULL, [CAD_Image06] [int] NULL, [CAD_Image07] [int] NULL, [CAD_Image08] [int] NULL, [CAD_Image09] [int] NULL, [CAD_Image10] [int] NULL, [CAD_Image11] [int] NULL, [CAD_Image12] [int] NULL, [Description_6751] [nvarchar](4000) NULL, [Comment_6752] [nvarchar](4000) NULL, [Description_6753] [nvarchar](4000) NULL, [Comment_6754] [nvarchar](4000) NULL, [Description_6755] [nvarchar](4000) NULL, [Comment_6756] [nvarchar](4000) NULL, [Description_6757] [nvarchar](4000) NULL, [Comment_6758] [nvarchar](4000) NULL, [Description_6759] [nvarchar](4000) NULL, [Comment_6760] [nvarchar](4000) NULL, [Description_6761] [nvarchar](4000) NULL, [Comment_6762] [nvarchar](4000) NULL, [Description_6763] [nvarchar](4000) NULL, [Comment_6764] [nvarchar](4000) NULL, [Description_6765] [nvarchar](4000) NULL, [Comment_6766] [nvarchar](4000) NULL, [Description_6767] [nvarchar](4000) NULL, [Comment_6768] [nvarchar](4000) NULL, [Description_6769] [nvarchar](4000) NULL, [Comment_6770] [nvarchar](4000) NULL, [Description_6771] [nvarchar](4000) NULL, [Comment_6772] [nvarchar](4000) NULL, [Description_6773] [nvarchar](4000) NULL, [Comment_6774] [nvarchar](4000) NULL, CONSTRAINT [PK_Technical_Details] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Construction_File] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Adobe_Illustrator_File] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Private_Label_File] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6751] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6752] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6753] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6754] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6755] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6756] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6757] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6758] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6759] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6760] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6761] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6762] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6763] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6764] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6765] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6766] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6767] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6768] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6769] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6770] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6771] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6772] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6773] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6774] [nvarchar](4000) NULL;';
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

-- Fabrics___Trims_10
SET @TableName = @TablePrefix + N'Fabrics___Trims_10';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Construction_Image] [int] NULL, [Fabric_Approval_Status] [nvarchar](4000) NULL, [Fabric_Approval_Date] [datetime] NULL, [dateisblank_calc] [nvarchar](4000) NULL, [setdate_calc] [nvarchar](4000) NULL, [blankdate_calc] [nvarchar](4000) NULL, [Stitch_Details] [int] NULL, [Comments] [nvarchar](4000) NULL, [Comp1] [decimal](18, 1) NULL, [Comp2] [decimal](18, 1) NULL, [Comp3] [decimal](18, 1) NULL, [Fiber1CB] [int] NULL, [Fiber2CB] [int] NULL, [Fiber3CB] [int] NULL, [Fiber1IB] [nvarchar](4000) NULL, [Fiber2IB] [nvarchar](4000) NULL, [Fiber3IB] [nvarchar](4000) NULL, [Comp_ok] [nvarchar](4000) NULL, [Total_Composition] [nvarchar](4000) NULL, [CompositionDDL] [int] NULL, [Valid_Selection] [nvarchar](4000) NULL, [CompositionTXT] [nvarchar](4000) NULL, [comp1ok] [nvarchar](4000) NULL, [comp2ok] [nvarchar](4000) NULL, [comp3ok] [nvarchar](4000) NULL, [Comp1_Tx] [nvarchar](4000) NULL, [Comp2_Tx] [nvarchar](4000) NULL, [Comp3_Tx] [nvarchar](4000) NULL, [Percent_Chk] [nvarchar](4000) NULL, [Comp4] [decimal](18, 1) NULL, [Comp5] [decimal](18, 1) NULL, [Fiber4CB] [int] NULL, [Fiber5CB] [int] NULL, [Fiber4IB] [nvarchar](4000) NULL, [Fiber5IB] [nvarchar](4000) NULL, [comp4ok] [nvarchar](4000) NULL, [comp5ok] [nvarchar](4000) NULL, [Comp4_Tx] [nvarchar](4000) NULL, [Comp5_Tx] [nvarchar](4000) NULL, [Trims_Concat] [nvarchar](4000) NULL, [Main_Fabric] [nvarchar](4000) NULL, [Main_Fabric_Composition] [nvarchar](4000) NULL, [Weight] [decimal](18, 2) NULL, [UOM] [nvarchar](4000) NULL, [Calc] [nvarchar](4000) NULL, [Final_Main_Fabric] [nvarchar](4000) NULL, [Final_Weight] [nvarchar](4000) NULL, [Final_UOM] [nvarchar](4000) NULL, [Main_Fabric_10] [nvarchar](4000) NULL, [Main_Weight_10] [decimal](18, 2) NULL, [Main_UOM_10] [nvarchar](4000) NULL, [Main_Fabric_20] [nvarchar](4000) NULL, [Main_Weight_20] [decimal](18, 2) NULL, [Main_UOM_20] [nvarchar](4000) NULL, [Total_Composition_10] [nvarchar](4000) NULL, [Total_Composition_20] [nvarchar](4000) NULL, [Final_Total_Composition] [nvarchar](4000) NULL, [UOM_ID_10] [decimal](18, 2) NULL, [Compositions] [nvarchar](4000) NULL, [Test] [nvarchar](4000) NULL, [Comp6] [decimal](18, 1) NULL, [Fiber6CB] [int] NULL, [Fiber6IB] [nvarchar](4000) NULL, [comp6ok] [nvarchar](4000) NULL, [Comp6_Tx] [nvarchar](4000) NULL, [Type_Group] [nvarchar](4000) NULL, [is_Outerwear_Swim] [nvarchar](4000) NULL, [O_S_Compositions] [nvarchar](4000) NULL, [UOM_ID_20] [decimal](18, 2) NULL, [Final_UOM_ID] [decimal](18, 2) NULL, [Set_Compositions] [nvarchar](4000) NULL, CONSTRAINT [PK_Fabrics___Trims_10] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fabric_Approval_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [dateisblank_calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [setdate_calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [blankdate_calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber1IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber2IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber3IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp_ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CompositionTXT] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp1ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp2ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp3ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp1_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp2_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp3_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Percent_Chk] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber4IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber5IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp4ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp5ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp4_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp5_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Trims_Concat] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_Composition] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [UOM] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Main_Fabric] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Weight] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_UOM] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_UOM_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_UOM_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Total_Composition] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Compositions] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Test] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber6IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp6ok] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp6_Tx] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Type_Group] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [is_Outerwear_Swim] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [O_S_Compositions] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Set_Compositions] [nvarchar](4000) NULL;';
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

-- Labels___Packaging_10
SET @TableName = @TablePrefix + N'Labels___Packaging_10';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Label_Approval_Status] [nvarchar](4000) NULL, [Label_Approval_Date] [datetime] NULL, [dateisblank_calc_5480] [nvarchar](4000) NULL, [setdate_calc_5481] [nvarchar](4000) NULL, [blankdate_calc_5482] [nvarchar](4000) NULL, [Packaging_Approval_Status] [nvarchar](4000) NULL, [Packaging_Approval_Date] [datetime] NULL, [dateisblank_calc_5485] [nvarchar](4000) NULL, [setdate_calc_5486] [nvarchar](4000) NULL, [blankdate_calc_5487] [nvarchar](4000) NULL, [Description_6775] [nvarchar](4000) NULL, [Image01] [int] NULL, [Comment_6777] [nvarchar](4000) NULL, [Description_6778] [nvarchar](4000) NULL, [Image02] [int] NULL, [Comment_6780] [nvarchar](4000) NULL, [Description_6781] [nvarchar](4000) NULL, [Image03] [int] NULL, [Comment_6783] [nvarchar](4000) NULL, [Description_6784] [nvarchar](4000) NULL, [Image04] [int] NULL, [Comment_6786] [nvarchar](4000) NULL, CONSTRAINT [PK_Labels___Packaging_10] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Label_Approval_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [dateisblank_calc_5480] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [setdate_calc_5481] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [blankdate_calc_5482] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Packaging_Approval_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [dateisblank_calc_5485] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [setdate_calc_5486] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [blankdate_calc_5487] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6775] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6777] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6778] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6780] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6781] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6783] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6784] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6786] [nvarchar](4000) NULL;';
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

-- Style_Reference
SET @TableName = @TablePrefix + N'Style_Reference';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Ref] [nvarchar](4000) NULL, [Image_6914] [int] NULL, [Comments] [nvarchar](4000) NULL, [Description_6922] [nvarchar](4000) NULL, [Comment_6923] [nvarchar](4000) NULL, [Description_6924] [nvarchar](4000) NULL, [Image_6925] [int] NULL, [Comment_6926] [nvarchar](4000) NULL, [Description_6927] [nvarchar](4000) NULL, [Image_6928] [int] NULL, [Comment_6929] [nvarchar](4000) NULL, [Description_6930] [nvarchar](4000) NULL, [Image_6931] [int] NULL, [Comment_6932] [nvarchar](4000) NULL, [Description_6933] [nvarchar](4000) NULL, [Image_6934] [int] NULL, [Comment_6935] [nvarchar](4000) NULL, [Description_6936] [nvarchar](4000) NULL, [Image_6937] [int] NULL, [Comment_6938] [nvarchar](4000) NULL, [Description_6939] [nvarchar](4000) NULL, [Image_6940] [int] NULL, [Comment_6941] [nvarchar](4000) NULL, [Description_6948] [nvarchar](4000) NULL, [Image_6949] [int] NULL, [Comment_6950] [nvarchar](4000) NULL, CONSTRAINT [PK_Style_Reference] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Ref] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6922] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6923] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6924] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6926] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6927] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6929] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6930] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6932] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6933] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6935] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6936] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6938] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6939] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6941] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6948] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6950] [nvarchar](4000) NULL;';
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

-- Care_Label
SET @TableName = @TablePrefix + N'Care_Label';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Artwork__Labelon] [int] NULL, [Artwork_Code] [nvarchar](4000) NULL, [Description] [nvarchar](4000) NULL, [SketchID] [decimal](18, 2) NULL, [Image] [int] NULL, [Artwork__Labelon__txt] [nvarchar](4000) NULL, [Wash_Pot_Code] [int] NULL, [Wash] [nvarchar](4000) NULL, [Bleach] [nvarchar](4000) NULL, [Dry] [nvarchar](4000) NULL, [Extra_Dry] [nvarchar](4000) NULL, [Iron] [nvarchar](4000) NULL, [Dry_Clean] [nvarchar](4000) NULL, [Final_Care_Label] [nvarchar](4000) NULL, [Care_Image_num] [decimal](18, 2) NULL, [Care_Symbols] [int] NULL, [Care_Label_Comments] [nvarchar](4000) NULL, [Disclaimer_Warning_1] [int] NULL, [Disclaimer_Warning_2] [int] NULL, [Disclaimer_Warning_3] [int] NULL, [Disclaimer_Warning_4] [int] NULL, [Disclaimer_Warning_5] [int] NULL, [Wash_1] [nvarchar](4000) NULL, [Extra_Wash_1] [nvarchar](4000) NULL, [Extra_Wash_2] [nvarchar](4000) NULL, [Spot_Clean] [nvarchar](4000) NULL, [Soak] [nvarchar](4000) NULL, [Wring_Out] [nvarchar](4000) NULL, [Reshape] [nvarchar](4000) NULL, [Extra_Dry_1] [nvarchar](4000) NULL, [Iron_1] [nvarchar](4000) NULL, [Extra_Dry_Clean] [nvarchar](4000) NULL, [Colour_Transfer] [nvarchar](4000) NULL, [Abrasion] [nvarchar](4000) NULL, [Miscellaneous] [nvarchar](4000) NULL, [Extra_Wash] [nvarchar](4000) NULL, CONSTRAINT [PK_Care_Label] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Artwork_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Artwork__Labelon__txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Wash] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Bleach] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Dry] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Extra_Dry] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Iron] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Dry_Clean] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Care_Label] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Care_Label_Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Wash_1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Extra_Wash_1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Extra_Wash_2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Spot_Clean] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Soak] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Wring_Out] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Reshape] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Extra_Dry_1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Iron_1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Extra_Dry_Clean] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Colour_Transfer] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Abrasion] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Miscellaneous] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Extra_Wash] [nvarchar](4000) NULL;';
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

-- Costing
SET @TableName = @TablePrefix + N'Costing';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Agent_WI1] [int] NULL, [Manufacturer_WI1] [int] NULL, [Factory_WI1] [int] NULL, [Agent_WI2] [int] NULL, [Manufacturer_WI2] [int] NULL, [Factory_WI2] [int] NULL, [Costing_Comments] [nvarchar](4000) NULL, [Costing_Comments_WI1] [nvarchar](4000) NULL, [Costing_Comments_WI2] [nvarchar](4000) NULL, [Costing_Status] [int] NULL, [Costing_Type_5500] [int] NULL, [Supplier_Price_5501] [decimal](18, 2) NULL, [Price_Currency_5502] [int] NULL, [FOB_5503] [nvarchar](4000) NULL, [FCA_5504] [nvarchar](4000) NULL, [CPT_5505] [nvarchar](4000) NULL, [CIF_5506] [nvarchar](4000) NULL, [DDP_5507] [nvarchar](4000) NULL, [DDU_5508] [nvarchar](4000) NULL, [Type_Num_5509] [decimal](18, 2) NULL, [Costing_Status_WI1] [int] NULL, [Costing_Type_WI1] [int] NULL, [Supplier_Price_WI1] [decimal](18, 2) NULL, [Price_Currency_WI1] [int] NULL, [FOB_WI1] [nvarchar](4000) NULL, [FCA_WI1] [nvarchar](4000) NULL, [CPT_WI1] [nvarchar](4000) NULL, [CIF_WI1] [nvarchar](4000) NULL, [DDP_WI1] [nvarchar](4000) NULL, [DDU_WI1] [nvarchar](4000) NULL, [Type_Num_WI1] [decimal](18, 2) NULL, [Costing_Status_WI2] [int] NULL, [Costing_Type_WI2] [int] NULL, [Supplier_Price_WI2] [decimal](18, 2) NULL, [Price_Currency_WI2] [int] NULL, [FOB_WI2] [nvarchar](4000) NULL, [FCA_WI2] [nvarchar](4000) NULL, [CPT_WI2] [nvarchar](4000) NULL, [CIF_WI2] [nvarchar](4000) NULL, [DDP_WI2] [nvarchar](4000) NULL, [DDU_WI2] [nvarchar](4000) NULL, [Type_Num_WI2] [decimal](18, 2) NULL, [Country_From] [int] NULL, [Country_To] [int] NULL, [Branding_Cost_6493] [nvarchar](4000) NULL, [Agent_6494] [int] NULL, [Agent_Commision_6495] [decimal](18, 2) NULL, [Costing_Type_6496] [int] NULL, [Supplier_Price_6497] [decimal](18, 2) NULL, [Price_Currency_6498] [int] NULL, [FOB_6499] [nvarchar](4000) NULL, [CIF_6500] [nvarchar](4000) NULL, [DDP_6501] [nvarchar](4000) NULL, [FCA_6502] [nvarchar](4000) NULL, [CPT_6503] [nvarchar](4000) NULL, [Type_Num_6504] [decimal](18, 2) NULL, [Preference_Certificate_6505] [int] NULL, [Yes_Pref_6506] [nvarchar](4000) NULL, [No_Pref_6507] [nvarchar](4000) NULL, [Yes_No_Num_6508] [decimal](18, 2) NULL, [HTS_Code_6509] [nvarchar](4000) NULL, [Duty_Rate_PrefCert_6510] [decimal](18, 2) NULL, [Duty_Rate_NoPrefCert_6511] [decimal](18, 2) NULL, [Duty_Rate___6512] [decimal](18, 2) NULL, [Duty_Total_Num_6513] [decimal](18, 2) NULL, [Freight_Type_6514] [int] NULL, [Freight_Eff_Date_6515] [int] NULL, [Freight_Rate___6516] [decimal](18, 2) NULL, [Freight_Num_6517] [decimal](18, 2) NULL, [Commission___6518] [decimal](18, 2) NULL, [Commission_Num_6519] [decimal](18, 2) NULL, [Miscellaneous_Total_6521] [decimal](18, 2) NULL, [Exchange_Rate_6522] [decimal](18, 2) NULL, [Exchange_Eff_Date_6523] [nvarchar](4000) NULL, [Duty_6524] [decimal](18, 2) NULL, [Freight_6525] [decimal](18, 2) NULL, [Commission_6526] [decimal](18, 2) NULL, [Landed_Price_6527] [decimal](18, 2) NULL, [Landed_Price__CAD__6528] [decimal](18, 2) NULL, [DDU_6529] [nvarchar](4000) NULL, [US_HTS_Code_6530] [nvarchar](4000) NULL, [HighRateDutyYes_6531] [decimal](18, 2) NULL, [HighRateDutyNo_6532] [decimal](18, 2) NULL, [HighRateDutyNum_6533] [decimal](18, 2) NULL, [Landed_Price__CAD__6534] [decimal](18, 2) NULL, [Miscellaneous_Total_6536] [decimal](18, 2) NULL, [Total_Cost_6537] [decimal](18, 2) NULL, [VAT_Group_6538] [nvarchar](4000) NULL, [Vat_Rate___6539] [decimal](18, 2) NULL, [RRP_6540] [decimal](18, 2) NULL, [Euro_RRP__€__6541] [decimal](18, 2) NULL, [RRP___Vat_6542] [decimal](18, 2) NULL, [RRP_Margin___6543] [decimal](18, 2) NULL, [Profit__CAD__6544] [decimal](18, 2) NULL, [Wholesale_6545] [decimal](18, 2) NULL, [Markup_6546] [decimal](18, 2) NULL, [Wholesale_Margin___6547] [decimal](18, 2) NULL, [Wholesale_Profit_6548] [decimal](18, 2) NULL, [JL_List_Price_6549] [decimal](18, 2) NULL, [Euro_List_Price_6550] [decimal](18, 2) NULL, [SMS_Surcharge] [decimal](18, 2) NULL, [Suppiler_Price] [decimal](18, 2) NULL, [Total_Cost_6553] [decimal](18, 2) NULL, [SMS_Supplier_Price] [decimal](18, 2) NULL, [SMS_Total_Cost] [decimal](18, 2) NULL, [Category] [int] NULL, [Quick_Search] [nvarchar](4000) NULL, [HTSCode] [nvarchar](4000) NULL, [Supplier_Costing_Comments] [nvarchar](4000) NULL, [_Style___Colour_MOQ] [nvarchar](4000) NULL, [Landed_Price__CAD__6561] [decimal](18, 2) NULL, [Miscellaneous_Total_6563] [decimal](18, 2) NULL, [Total_Cost_6564] [decimal](18, 2) NULL, [VAT_Group_6565] [nvarchar](4000) NULL, [Vat_Rate___6566] [decimal](18, 2) NULL, [RRP_6567] [decimal](18, 2) NULL, [Euro_RRP__€__6568] [decimal](18, 2) NULL, [RRP___Vat_6569] [decimal](18, 2) NULL, [RRP_Margin___6570] [decimal](18, 2) NULL, [Profit__CAD__6571] [decimal](18, 2) NULL, [Wholesale_6572] [decimal](18, 2) NULL, [Markup_6573] [decimal](18, 2) NULL, [Wholesale_Margin___6574] [decimal](18, 2) NULL, [Wholesale_Profit_6575] [decimal](18, 2) NULL, [JL_List_Price_6576] [decimal](18, 2) NULL, [Euro_List_Price_6577] [decimal](18, 2) NULL, [Landed_Price__CAD__6578] [decimal](18, 2) NULL, [Miscellaneous_Total_6580] [decimal](18, 2) NULL, [Total_Cost_6581] [decimal](18, 2) NULL, [VAT_Group_6582] [nvarchar](4000) NULL, [Vat_Rate___6583] [decimal](18, 2) NULL, [RRP_6584] [decimal](18, 2) NULL, [Euro_RRP__€__6585] [decimal](18, 2) NULL, [RRP___Vat_6586] [decimal](18, 2) NULL, [RRP_Margin___6587] [decimal](18, 2) NULL, [Profit__CAD__6588] [decimal](18, 2) NULL, [Wholesale_6589] [decimal](18, 2) NULL, [Markup_6590] [decimal](18, 2) NULL, [Wholesale_Margin___6591] [decimal](18, 2) NULL, [Wholesale_Profit_6592] [decimal](18, 2) NULL, [JL_List_Price_6593] [decimal](18, 2) NULL, [Euro_List_Price_6594] [decimal](18, 2) NULL, [Branding_Cost_6595] [nvarchar](4000) NULL, [Agent_6596] [int] NULL, [Agent_Commision_6597] [decimal](18, 2) NULL, [Costing_Type_6598] [int] NULL, [Supplier_Price_6599] [decimal](18, 2) NULL, [Price_Currency_6600] [int] NULL, [FOB_6601] [nvarchar](4000) NULL, [CIF_6602] [nvarchar](4000) NULL, [DDP_6603] [nvarchar](4000) NULL, [FCA_6604] [nvarchar](4000) NULL, [CPT_6605] [nvarchar](4000) NULL, [Type_Num_6606] [decimal](18, 2) NULL, [Preference_Certificate_6607] [int] NULL, [Yes_Pref_6608] [nvarchar](4000) NULL, [No_Pref_6609] [nvarchar](4000) NULL, [Yes_No_Num_6610] [decimal](18, 2) NULL, [HTS_Code_6611] [nvarchar](4000) NULL, [Duty_Rate_PrefCert_6612] [decimal](18, 2) NULL, [Duty_Rate_NoPrefCert_6613] [decimal](18, 2) NULL, [Duty_Rate___6614] [decimal](18, 2) NULL, [Duty_Total_Num_6615] [decimal](18, 2) NULL, [Freight_Type_6616] [int] NULL, [Freight_Eff_Date_6617] [int] NULL, [Freight_Rate___6618] [decimal](18, 2) NULL, [Freight_Num_6619] [decimal](18, 2) NULL, [Commission___6620] [decimal](18, 2) NULL, [Commission_Num_6621] [decimal](18, 2) NULL, [Miscellaneous_Total_6623] [decimal](18, 2) NULL, [Exchange_Rate_6624] [decimal](18, 2) NULL, [Exchange_Eff_Date_6625] [nvarchar](4000) NULL, [Duty_6626] [decimal](18, 2) NULL, [Freight_6627] [decimal](18, 2) NULL, [Commission_6628] [decimal](18, 2) NULL, [Landed_Price_6629] [decimal](18, 2) NULL, [Landed_Price__CAD__6630] [decimal](18, 2) NULL, [DDU_6631] [nvarchar](4000) NULL, [US_HTS_Code_6632] [nvarchar](4000) NULL, [HighRateDutyYes_6633] [decimal](18, 2) NULL, [HighRateDutyNo_6634] [decimal](18, 2) NULL, [HighRateDutyNum_6635] [decimal](18, 2) NULL, [Branding_Cost_6636] [nvarchar](4000) NULL, [Agent_6637] [int] NULL, [Agent_Commision_6638] [decimal](18, 2) NULL, [Costing_Type_6639] [int] NULL, [Supplier_Price_6640] [decimal](18, 2) NULL, [Price_Currency_6641] [int] NULL, [FOB_6642] [nvarchar](4000) NULL, [CIF_6643] [nvarchar](4000) NULL, [DDP_6644] [nvarchar](4000) NULL, [FCA_6645] [nvarchar](4000) NULL, [CPT_6646] [nvarchar](4000) NULL, [Type_Num_6647] [decimal](18, 2) NULL, [Preference_Certificate_6648] [int] NULL, [Yes_Pref_6649] [nvarchar](4000) NULL, [No_Pref_6650] [nvarchar](4000) NULL, [Yes_No_Num_6651] [decimal](18, 2) NULL, [HTS_Code_6652] [nvarchar](4000) NULL, [Duty_Rate_PrefCert_6653] [decimal](18, 2) NULL, [Duty_Rate_NoPrefCert_6654] [decimal](18, 2) NULL, [Duty_Rate___6655] [decimal](18, 2) NULL, [Duty_Total_Num_6656] [decimal](18, 2) NULL, [Freight_Type_6657] [int] NULL, [Freight_Eff_Date_6658] [int] NULL, [Freight_Rate___6659] [decimal](18, 2) NULL, [Freight_Num_6660] [decimal](18, 2) NULL, [Commission___6661] [decimal](18, 2) NULL, [Commission_Num_6662] [decimal](18, 2) NULL, [Miscellaneous_Total_6664] [decimal](18, 2) NULL, [Exchange_Rate_6665] [decimal](18, 2) NULL, [Exchange_Eff_Date_6666] [nvarchar](4000) NULL, [Duty_6667] [decimal](18, 2) NULL, [Freight_6668] [decimal](18, 2) NULL, [Commission_6669] [decimal](18, 2) NULL, [Landed_Price_6670] [decimal](18, 2) NULL, [Landed_Price__CAD__6671] [decimal](18, 2) NULL, [DDU_6672] [nvarchar](4000) NULL, [US_HTS_Code_6673] [nvarchar](4000) NULL, [HighRateDutyYes_6674] [decimal](18, 2) NULL, [HighRateDutyNo_6675] [decimal](18, 2) NULL, [HighRateDutyNum_6676] [decimal](18, 2) NULL, CONSTRAINT [PK_Costing] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Costing_Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Costing_Comments_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Costing_Comments_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_5503] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_5504] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_5505] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_5506] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_5507] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_5508] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Branding_Cost_6493] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_6499] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_6500] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_6501] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_6502] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_6503] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Yes_Pref_6506] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [No_Pref_6507] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [HTS_Code_6509] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Exchange_Eff_Date_6523] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_6529] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [US_HTS_Code_6530] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [VAT_Group_6538] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Quick_Search] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [HTSCode] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Supplier_Costing_Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [_Style___Colour_MOQ] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [VAT_Group_6565] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [VAT_Group_6582] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Branding_Cost_6595] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_6601] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_6602] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_6603] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_6604] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_6605] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Yes_Pref_6608] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [No_Pref_6609] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [HTS_Code_6611] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Exchange_Eff_Date_6625] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_6631] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [US_HTS_Code_6632] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Branding_Cost_6636] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_6642] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_6643] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_6644] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_6645] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_6646] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Yes_Pref_6649] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [No_Pref_6650] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [HTS_Code_6652] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Exchange_Eff_Date_6666] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_6672] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [US_HTS_Code_6673] [nvarchar](4000) NULL;';
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

-- Style_Header
SET @TableName = @TablePrefix + N'Style_Header';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Classification] [int] NULL, [Product_Type_2] [int] NULL, [Season_3] [int] NULL, [Collection_4] [int] NULL, [Group] [int] NULL, [Sketch] [int] NULL, [Division_8] [int] NULL, [Size_Range_10] [int] NULL, [Dimension_11] [int] NULL, [Article] [nvarchar](4000) NULL, [Description_23] [nvarchar](4000) NULL, [Product_Manager] [int] NULL, [Long_Description] [nvarchar](4000) NULL, [Sample_Size_Detail] [int] NULL, [ProductTypeGroup] [int] NULL, [Size_Detail_Dispaly] [nvarchar](4000) NULL, [Division_186] [int] NULL, [Created_By] [nvarchar](4000) NULL, [Last_Revised_By] [nvarchar](4000) NULL, [Style_Status] [int] NULL, [State] [decimal](18, 2) NULL, [Sample_Status] [nvarchar](4000) NULL, [CB_Fit_1_Status] [int] NULL, [IN_Fit_1_State] [decimal](18, 2) NULL, [CB_Fit_2_Status] [int] NULL, [IN_Fit_2_State] [decimal](18, 2) NULL, [CB_Fit_3_Status] [int] NULL, [IN_Fit_3_State] [decimal](18, 2) NULL, [CB_Fit_4_Status] [int] NULL, [IN_Fit_4_State] [decimal](18, 2) NULL, [CB_PP_1_Status] [int] NULL, [IN_PP_1_State] [decimal](18, 2) NULL, [CB_PP_2_Status] [int] NULL, [IN_PP_2_State] [decimal](18, 2) NULL, [CB_PP_3_Status] [int] NULL, [IN_PP_3_State] [decimal](18, 2) NULL, [CB_TOP_1_Status] [int] NULL, [IN_TOP_1_State] [decimal](18, 2) NULL, [CB_TOP_2_Status] [int] NULL, [IN_TOP_2_State] [decimal](18, 2) NULL, [CHK_Fit_1_Latest] [nvarchar](4000) NULL, [CHK_Fit_2_Latest] [nvarchar](4000) NULL, [CHK_Fit_3_Latest] [nvarchar](4000) NULL, [CHK_Fit_4_Latest] [nvarchar](4000) NULL, [CHK_PP_1_Latest] [nvarchar](4000) NULL, [CHK_PP_2_Latest] [nvarchar](4000) NULL, [CHK_PP_3_Latest] [nvarchar](4000) NULL, [CHK_TOP_1_Latest] [nvarchar](4000) NULL, [CHK_TOP_2_Latest] [nvarchar](4000) NULL, [fit1status_IB] [nvarchar](4000) NULL, [fit2status_IB] [nvarchar](4000) NULL, [fit3status_IB] [nvarchar](4000) NULL, [fit4status_IB] [nvarchar](4000) NULL, [pp1status_IB] [nvarchar](4000) NULL, [pp2status_IB] [nvarchar](4000) NULL, [pp3status_IB] [nvarchar](4000) NULL, [top1status_IB] [nvarchar](4000) NULL, [top2status_IB] [nvarchar](4000) NULL, [CB_Fit_1_Type] [int] NULL, [fit1type_IB] [nvarchar](4000) NULL, [CB_Fit_2_Type] [int] NULL, [fit2type_IB] [nvarchar](4000) NULL, [CB_Fit_3_Type] [int] NULL, [fit3type_IB] [nvarchar](4000) NULL, [CB_Fit_4_Type] [int] NULL, [fit4type_IB] [nvarchar](4000) NULL, [CB_PP_1_Type] [int] NULL, [pp1type_IB] [nvarchar](4000) NULL, [CB_PP_2_Type] [int] NULL, [pp2type_IB] [nvarchar](4000) NULL, [CB_PP_3_Type] [int] NULL, [pp3type_IB] [nvarchar](4000) NULL, [CB_TOP_1_Type] [int] NULL, [top1type_IB] [nvarchar](4000) NULL, [CB_TOP_2_Type] [int] NULL, [top2type_IB] [nvarchar](4000) NULL, [Calc_Fit_Status] [nvarchar](4000) NULL, [Print_Solid] [int] NULL, [Supplier_Cost] [decimal](18, 2) NULL, [Currency] [int] NULL, [Needed_for_Calc] [nvarchar](4000) NULL, [Item_Type] [int] NULL, [Publish_to_ERP] [nvarchar](4000) NULL, [Published_to_ERP] [nvarchar](4000) NULL, [Publish_Failed_to_ERP] [nvarchar](4000) NULL, [Product_Code] [nvarchar](4000) NULL, [Size_Range_5022] [int] NULL, [DivisionBlock] [int] NULL, [Product_Class_5024] [int] NULL, [Dimension_5025] [int] NULL, [Season_5026] [int] NULL, [Price_Type] [int] NULL, [First_Cost_Currency] [int] NULL, [Valid_Size_Selection] [nvarchar](4000) NULL, [Valid_Product_Code_Selection] [nvarchar](4000) NULL, [Valid_DivisionBlock_Selection] [nvarchar](4000) NULL, [Valid_Product_Class_Selection] [nvarchar](4000) NULL, [Valid_Dimension_Selection] [nvarchar](4000) NULL, [Valid_Season_Selection] [nvarchar](4000) NULL, [Valid_Price_Type_Selection] [nvarchar](4000) NULL, [Valid_First_Cost_Currency_Selection] [nvarchar](4000) NULL, [Color] [decimal](18, 2) NULL, [Valid_Color_Selection] [nvarchar](4000) NULL, [Active_Count] [decimal](18, 2) NULL, [DimensionColorSizeActiveBooleanSum] [nvarchar](4000) NULL, [Original_Reference] [nvarchar](4000) NULL, [New_Carryover] [int] NULL, [Parent__Child] [int] NULL, [Ref_Style_from_Past_SS] [nvarchar](4000) NULL, [Version_Code] [nvarchar](4000) NULL, [Fit_Type_5239] [int] NULL, [Directional_Fabric] [int] NULL, [Treatment_1] [int] NULL, [Treatment_2] [int] NULL, [Treatment_Comments] [nvarchar](4000) NULL, [BW_File] [nvarchar](4000) NULL, [Original_Image] [nvarchar](4000) NULL, [Product_Class_5262] [int] NULL, [Class_Group] [int] NULL, [French] [nvarchar](4000) NULL, [Inseam] [int] NULL, [Manufacturer_COO] [int] NULL, [Garment_Factory] [int] NULL, [Name] [nvarchar](4000) NULL, [Gender_txt_7029] [nvarchar](4000) NULL, [Product_Type_txt_7030] [nvarchar](4000) NULL, [Fit_Type_txt] [nvarchar](4000) NULL, [CC_7032] [nvarchar](4000) NULL, [Gender_7033] [nvarchar](4000) NULL, [Product_Type_7034] [nvarchar](4000) NULL, [Details_7035] [nvarchar](4000) NULL, [Fit_Type_7036] [nvarchar](4000) NULL, [sketch_id] [nvarchar](4000) NULL, [ddl] [int] NULL, [Free_text] [nvarchar](4000) NULL, [Special_Customer] [int] NULL, [Inseam_txt] [nvarchar](4000) NULL, [Length] [nvarchar](4000) NULL, [Collection_7124] [nvarchar](4000) NULL, [Collection_txt] [nvarchar](4000) NULL, [Selling_Period] [int] NULL, [Private_Label] [int] NULL, [Vendor] [int] NULL, [Fabric_Code] [nvarchar](4000) NULL, [_of_Characters] [decimal](18, 2) NULL, [Description_7162] [nvarchar](4000) NULL, [Char_Count_Check] [nvarchar](4000) NULL, [Gender_7171] [int] NULL, [CC_7192] [nvarchar](4000) NULL, [Gender_txt_7193] [nvarchar](4000) NULL, [Product_Type_txt_7194] [nvarchar](4000) NULL, [Length_txt] [nvarchar](4000) NULL, [Details_7198] [nvarchar](4000) NULL, [ProductTypeGroup_txt] [nvarchar](4000) NULL, [Subcategory] [int] NULL, [ERP_Season] [int] NULL, [French_Name] [nvarchar](4000) NULL, [Sets_details_fabric] [nvarchar](4000) NULL, CONSTRAINT [PK_Style_Header] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
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
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Size_Detail_Dispaly] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Created_By] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Last_Revised_By] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Sample_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_Fit_1_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_Fit_2_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_Fit_3_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_Fit_4_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_PP_1_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_PP_2_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_PP_3_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_TOP_1_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CHK_TOP_2_Latest] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit1status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit2status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit3status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit4status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp1status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp2status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp3status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [top1status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [top2status_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit1type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit2type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit3type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [fit4type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp1type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp2type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [pp3type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [top1type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [top2type_IB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Calc_Fit_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Needed_for_Calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Publish_to_ERP] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Published_to_ERP] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Publish_Failed_to_ERP] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Product_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Size_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Product_Code_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_DivisionBlock_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Product_Class_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Dimension_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Season_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Price_Type_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_First_Cost_Currency_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Color_Selection] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DimensionColorSizeActiveBooleanSum] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Original_Reference] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Ref_Style_from_Past_SS] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Version_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Treatment_Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [BW_File] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Original_Image] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [French] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Name] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Gender_txt_7029] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Product_Type_txt_7030] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fit_Type_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CC_7032] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Gender_7033] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Product_Type_7034] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Details_7035] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fit_Type_7036] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [sketch_id] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Free_text] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Inseam_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Length] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Collection_7124] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Collection_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fabric_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_7162] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Char_Count_Check] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CC_7192] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Gender_txt_7193] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Product_Type_txt_7194] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Length_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Details_7198] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ProductTypeGroup_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [French_Name] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Sets_details_fabric] [nvarchar](4000) NULL;';
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

-- Publish_to_ERP
SET @TableName = @TablePrefix + N'Publish_to_ERP';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Notes] [nvarchar](4000) NULL, [Price_By] [int] NULL, [Effective_Date] [datetime] NULL, [Publish_to_ERP_Message] [nvarchar](4000) NULL, [Fabric_Price_By] [int] NULL, [Fabric_Final_Cost_Meter] [decimal](18, 2) NULL, [Fabric_Final_Cost_Yard] [decimal](18, 2) NULL, CONSTRAINT [PK_Publish_to_ERP] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Notes] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Publish_to_ERP_Message] [nvarchar](4000) NULL;';
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

-- Approvals
SET @TableName = @TablePrefix + N'Approvals';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Trims_Concat] [nvarchar](4000) NULL, CONSTRAINT [PK_Approvals] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Trims_Concat] [nvarchar](4000) NULL;';
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

-- AJ
SET @TableName = @TablePrefix + N'AJ';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Page_1] [int] NULL, [Page_2] [int] NULL, [Page_3] [int] NULL, CONSTRAINT [PK_AJ] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
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

-- Artwork_BOM_prod
SET @TableName = @TablePrefix + N'Artwork_BOM_prod';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Print] [nvarchar](4000) NULL, [Description] [nvarchar](4000) NULL, [ProductReferenceID] [decimal](18, 2) NULL, [Comments] [nvarchar](4000) NULL, [Placement] [nvarchar](4000) NULL, [Product_Type] [int] NULL, [Image1] [int] NULL, [Image2] [int] NULL, [Image3] [int] NULL, [Image4] [int] NULL, [Sketch] [int] NULL, [Image5] [int] NULL, [Image6] [int] NULL, [Image7] [int] NULL, [Image8] [int] NULL, [Image9] [int] NULL, [Image10] [int] NULL, [Image11] [int] NULL, [Image12] [int] NULL, [Colorway_7] [int] NULL, [Colorway_8] [int] NULL, [Colorway_9] [int] NULL, [Colorway_10] [int] NULL, [Colorway_11] [int] NULL, [Colorway_12] [int] NULL, [Colorway_5] [int] NULL, [Colorway_6] [int] NULL, [Colorway_1] [int] NULL, [Colorway_2] [int] NULL, [Colorway_3] [int] NULL, [Colorway_4] [int] NULL, [Process_1] [int] NULL, [Process_2] [int] NULL, [Comments_By] [int] NULL, CONSTRAINT [PK_Artwork_BOM_prod] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Print] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Placement] [nvarchar](4000) NULL;';
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

