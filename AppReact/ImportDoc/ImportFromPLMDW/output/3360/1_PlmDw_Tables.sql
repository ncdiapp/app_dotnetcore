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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Composition')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Composition] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Made_in_Country')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Made_in_Country] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Security_Group_3094')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Security_Group_3094] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Designer')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Designer] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Security_Group_3098')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Security_Group_3098] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sourcing')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sourcing] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Security_Group_3950')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Security_Group_3950] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Technical_Manager')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Technical_Manager] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Coordinator')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Coordinator] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'QC_Team')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [QC_Team] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Release_to_Manufacturer')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Release_to_Manufacturer] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Security_Group_5096')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Security_Group_5096] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Security_Group_5097')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Security_Group_5097] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Tech_Pack_Send_to_Vendor')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Tech_Pack_Send_to_Vendor] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Tech_Pack_Sent_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Tech_Pack_Sent_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'RRP')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [RRP] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Target_Cost_FOB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Target_Cost_FOB] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Target_Margin')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Target_Margin] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Target_Landed')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Target_Landed] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Drop')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Drop] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Drop] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Launch_Month')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Launch_Month] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Launch_Month] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Order_Month')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Order_Month] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Order_Month] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Ship_Window_Start')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Ship_Window_Start] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Ship_Window_End')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Ship_Window_End] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Date_Shipping_1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Date_Shipping_1] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ETD')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ETD] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Floorset_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Floorset_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Cancel_By_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Cancel_By_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Embellishment_A_W_Techniques')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Embellishment_A_W_Techniques] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Embellishment_A_W_Techniques] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wash_Thread')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wash_Thread] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Wash_Thread] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Pricing_Comment')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Pricing_Comment] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Pricing_Comment] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Release_Day')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Release_Day] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp1__7079')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp1__7079] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp2__7080')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp2__7080] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp3__7081')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp3__7081] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber1CB_7082')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber1CB_7082] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber2CB_7083')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber2CB_7083] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber3CB_7084')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber3CB_7084] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber1IB_7085')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber1IB_7085] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber1IB_7085] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber2IB_7086')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber2IB_7086] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber2IB_7086] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber3IB_7087')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber3IB_7087] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber3IB_7087] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp_ok_7088')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp_ok_7088] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp_ok_7088] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total_Composition_7089')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total_Composition_7089] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_7089] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CompositionDDL_7090')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CompositionDDL_7090] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_Selection_7091')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_Selection_7091] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Selection_7091] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CompositionTXT_7092')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CompositionTXT_7092] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CompositionTXT_7092] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp1ok_7093')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp1ok_7093] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp1ok_7093] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp2ok_7094')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp2ok_7094] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp2ok_7094] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp3ok_7095')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp3ok_7095] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp3ok_7095] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp1_Tx_7096')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp1_Tx_7096] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp1_Tx_7096] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp2_Tx_7097')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp2_Tx_7097] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp2_Tx_7097] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp3_Tx_7098')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp3_Tx_7098] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp3_Tx_7098] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Percent_Chk_7099')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Percent_Chk_7099] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Percent_Chk_7099] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp4__7100')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp4__7100] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp5__7101')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp5__7101] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber4CB_7102')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber4CB_7102] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber5CB_7103')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber5CB_7103] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber4IB_7104')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber4IB_7104] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber4IB_7104] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber5IB_7105')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber5IB_7105] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber5IB_7105] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp4ok_7106')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp4ok_7106] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp4ok_7106] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp5ok_7107')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp5ok_7107] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp5ok_7107] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp4_Tx_7108')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp4_Tx_7108] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp4_Tx_7108] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp5_Tx_7109')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp5_Tx_7109] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp5_Tx_7109] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric_7156')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric_7156] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_7156] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric_Composition_7159')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric_Composition_7159] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_Composition_7159] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Weight_7169')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Weight_7169] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM_7170')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM_7170] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [UOM_7170] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Designer_2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Designer_2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Designer_1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Designer_1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Calc_7220')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Calc_7220] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Calc_7220] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric_7221')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric_7221] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_7221] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Calc_7222')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Calc_7222] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Calc_7222] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Weight_7223')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Weight_7223] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM_7224')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM_7224] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [UOM_7224] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp1__7225')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp1__7225] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp2__7226')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp2__7226] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp3__7227')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp3__7227] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber1CB_7228')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber1CB_7228] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber1CB_7228] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber2CB_7229')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber2CB_7229] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber2CB_7229] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber3CB_7230')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber3CB_7230] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber3CB_7230] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber1IB_7231')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber1IB_7231] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber1IB_7231] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber2IB_7232')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber2IB_7232] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber2IB_7232] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber3IB_7233')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber3IB_7233] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber3IB_7233] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp_ok_7234')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp_ok_7234] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp_ok_7234] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total_Composition_7235')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total_Composition_7235] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_7235] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CompositionDDL_7236')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CompositionDDL_7236] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CompositionDDL_7236] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Valid_Selection_7237')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Valid_Selection_7237] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Valid_Selection_7237] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CompositionTXT_7238')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CompositionTXT_7238] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CompositionTXT_7238] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp1ok_7239')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp1ok_7239] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp1ok_7239] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp2ok_7240')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp2ok_7240] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp2ok_7240] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp3ok_7241')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp3ok_7241] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp3ok_7241] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp1_Tx_7242')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp1_Tx_7242] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp1_Tx_7242] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp2_Tx_7243')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp2_Tx_7243] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp2_Tx_7243] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp3_Tx_7244')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp3_Tx_7244] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp3_Tx_7244] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Percent_Chk_7245')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Percent_Chk_7245] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Percent_Chk_7245] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp4__7246')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp4__7246] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp5__7247')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp5__7247] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber4CB_7248')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber4CB_7248] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber4CB_7248] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber5CB_7249')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber5CB_7249] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber5CB_7249] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber4IB_7250')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber4IB_7250] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber4IB_7250] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber5IB_7251')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber5IB_7251] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber5IB_7251] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp4ok_7252')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp4ok_7252] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp4ok_7252] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'comp5ok_7253')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [comp5ok_7253] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [comp5ok_7253] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp4_Tx_7254')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp4_Tx_7254] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp4_Tx_7254] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp5_Tx_7255')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp5_Tx_7255] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comp5_Tx_7255] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric_Composition_7256')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric_Composition_7256] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_Composition_7256] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_Main_Fabric')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_Main_Fabric] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Main_Fabric] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_Weight')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_Weight] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Weight] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_UOM] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_UOM] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric_10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric_10] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Weight_10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Weight_10] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_UOM_10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_UOM_10] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_UOM_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric_20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric_20] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Weight_20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Weight_20] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_UOM_20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_UOM_20] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_UOM_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total_Composition_10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total_Composition_10] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total_Composition_20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total_Composition_20] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_Total_Composition')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_Total_Composition] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Total_Composition] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM_ID_10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM_ID_10] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Compositions')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Compositions] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Compositions] [nvarchar](4000) NULL;';
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Type_Group')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Type_Group] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Type_Group] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'is_Outerwear_Swim')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [is_Outerwear_Swim] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [is_Outerwear_Swim] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'O_S_Compositions')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [O_S_Compositions] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [O_S_Compositions] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM_ID_20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM_ID_20] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_UOM_ID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_UOM_ID] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Set_Compositions')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Set_Compositions] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Security_Group')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Security_Group] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Created_by_3154')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Created_by_3154] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Created_by_7111')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Created_by_7111] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Colors')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Colors] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
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
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Front_View')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Front_View] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Back_View')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Back_View] [int] NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Artwork_Approval_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Artwork_Approval_Status] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Artwork_Approval_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Artwork_Approval_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Artwork_Approval_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Construction_File')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Construction_File] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Construction_File] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Construction_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Construction_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Adobe_Illustrator_File')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Adobe_Illustrator_File] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Adobe_Illustrator_File] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Private_Label_File')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Private_Label_File] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Private_Label_File] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Design_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Design_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image01')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image01] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image02')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image02] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image03')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image03] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image04')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image04] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image05')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image05] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image06')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image06] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image07')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image07] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image08')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image08] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image09')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image09] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image10] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image11')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image11] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_Image12')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_Image12] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6751')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6751] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6751] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6752')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6752] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6752] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6753')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6753] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6753] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6754')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6754] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6754] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6755')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6755] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6755] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6756')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6756] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6756] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6757')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6757] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6757] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6758')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6758] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6758] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6759')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6759] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6759] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6760')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6760] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6760] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6761')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6761] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6761] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6762')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6762] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6762] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6763')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6763] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6763] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6764')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6764] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6764] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6765')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6765] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6765] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6766')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6766] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6766] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6767')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6767] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6767] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6768')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6768] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6768] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6769')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6769] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6769] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6770')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6770] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6770] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6771')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6771] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6771] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6772')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6772] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6772] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6773')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6773] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6773] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6774')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6774] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
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

-- Fabrics___Trims
SET @TableName = @TablePrefix + N'Fabrics___Trims';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Construction_Image] [int] NULL, [Stitch_Details] [int] NULL, [Comments] [nvarchar](4000) NULL, [Trims_Concat] [nvarchar](4000) NULL, [Fabric_Approval_Status] [nvarchar](4000) NULL, [Fabric_Approval_Date] [datetime] NULL, [dateisblank_calc] [nvarchar](4000) NULL, [setdate_calc] [nvarchar](4000) NULL, [blankdate_calc] [nvarchar](4000) NULL, [Main_Fabric] [nvarchar](4000) NULL, [Calc] [nvarchar](4000) NULL, [Weight] [decimal](18, 2) NULL, [UOM] [nvarchar](4000) NULL, [Comp1] [decimal](18, 1) NULL, [Comp2] [decimal](18, 1) NULL, [Comp3] [decimal](18, 1) NULL, [Fiber1CB] [nvarchar](4000) NULL, [Fiber2CB] [nvarchar](4000) NULL, [Fiber3CB] [nvarchar](4000) NULL, [Fiber1IB] [nvarchar](4000) NULL, [Fiber2IB] [nvarchar](4000) NULL, [Fiber3IB] [nvarchar](4000) NULL, [Comp_ok] [nvarchar](4000) NULL, [Total_Composition] [nvarchar](4000) NULL, [CompositionDDL] [nvarchar](4000) NULL, [Valid_Selection] [nvarchar](4000) NULL, [CompositionTXT] [nvarchar](4000) NULL, [comp1ok] [nvarchar](4000) NULL, [comp2ok] [nvarchar](4000) NULL, [comp3ok] [nvarchar](4000) NULL, [Comp1_Tx] [nvarchar](4000) NULL, [Comp2_Tx] [nvarchar](4000) NULL, [Comp3_Tx] [nvarchar](4000) NULL, [Percent_Chk] [nvarchar](4000) NULL, [Comp4] [decimal](18, 1) NULL, [Comp5] [decimal](18, 1) NULL, [Fiber4CB] [nvarchar](4000) NULL, [Fiber5CB] [nvarchar](4000) NULL, [Fiber4IB] [nvarchar](4000) NULL, [Fiber5IB] [nvarchar](4000) NULL, [comp4ok] [nvarchar](4000) NULL, [comp5ok] [nvarchar](4000) NULL, [Comp4_Tx] [nvarchar](4000) NULL, [Comp5_Tx] [nvarchar](4000) NULL, [Main_Fabric_Composition] [nvarchar](4000) NULL, [Final_Main_Fabric] [nvarchar](4000) NULL, [Final_Weight] [nvarchar](4000) NULL, [Final_UOM] [nvarchar](4000) NULL, [Main_Fabric_10] [nvarchar](4000) NULL, [Main_Weight_10] [decimal](18, 2) NULL, [Main_UOM_10] [nvarchar](4000) NULL, [Main_Fabric_20] [nvarchar](4000) NULL, [Main_Weight_20] [decimal](18, 2) NULL, [Main_UOM_20] [nvarchar](4000) NULL, [Total_Composition_10] [nvarchar](4000) NULL, [Total_Composition_20] [nvarchar](4000) NULL, [Final_Total_Composition] [nvarchar](4000) NULL, [UOM_ID_10] [decimal](18, 2) NULL, [Compositions] [nvarchar](4000) NULL, [Test] [nvarchar](4000) NULL, [Type_Group] [nvarchar](4000) NULL, [is_Outerwear_Swim] [nvarchar](4000) NULL, [O_S_Compositions] [nvarchar](4000) NULL, [UOM_ID_20] [decimal](18, 2) NULL, [Final_UOM_ID] [decimal](18, 2) NULL, [Set_Compositions] [nvarchar](4000) NULL, CONSTRAINT [PK_Fabrics___Trims] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Construction_Image')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Construction_Image] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Stitch_Details')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Stitch_Details] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Trims_Concat')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Trims_Concat] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Trims_Concat] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Approval_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Approval_Status] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fabric_Approval_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Approval_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Approval_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Calc')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Calc] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Weight')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Weight] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [UOM] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp1] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp2] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp3] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber1CB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber1CB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber1CB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber2CB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber2CB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber2CB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber3CB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber3CB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber3CB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CompositionDDL')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CompositionDDL] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CompositionDDL] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Percent_Chk')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Percent_Chk] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Percent_Chk] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp4')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp4] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comp5')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comp5] [decimal](18, 1) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber4CB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber4CB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber4CB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fiber5CB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fiber5CB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fiber5CB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric_Composition')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric_Composition] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_Composition] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_Main_Fabric')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_Main_Fabric] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Main_Fabric] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_Weight')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_Weight] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Weight] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_UOM] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_UOM] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric_10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric_10] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Weight_10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Weight_10] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_UOM_10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_UOM_10] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_UOM_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric_20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric_20] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Weight_20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Weight_20] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_UOM_20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_UOM_20] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_UOM_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total_Composition_10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total_Composition_10] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total_Composition_20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total_Composition_20] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Total_Composition_20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_Total_Composition')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_Total_Composition] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Total_Composition] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM_ID_10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM_ID_10] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Compositions')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Compositions] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Compositions] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Test')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Test] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Test] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Type_Group')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Type_Group] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Type_Group] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'is_Outerwear_Swim')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [is_Outerwear_Swim] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [is_Outerwear_Swim] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'O_S_Compositions')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [O_S_Compositions] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [O_S_Compositions] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM_ID_20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM_ID_20] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_UOM_ID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_UOM_ID] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Set_Compositions')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Set_Compositions] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
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

-- Labels___Packaging
SET @TableName = @TablePrefix + N'Labels___Packaging';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([ReferenceId] INT NOT NULL, [Label_Approval_Status] [nvarchar](4000) NULL, [Label_Approval_Date] [datetime] NULL, [dateisblank_calc_5480] [nvarchar](4000) NULL, [setdate_calc_5481] [nvarchar](4000) NULL, [blankdate_calc_5482] [nvarchar](4000) NULL, [Packaging_Approval_Status] [nvarchar](4000) NULL, [Packaging_Approval_Date] [datetime] NULL, [dateisblank_calc_5485] [nvarchar](4000) NULL, [setdate_calc_5486] [nvarchar](4000) NULL, [blankdate_calc_5487] [nvarchar](4000) NULL, [Description_6775] [nvarchar](4000) NULL, [Image01] [int] NULL, [Comment_6777] [nvarchar](4000) NULL, [Description_6778] [nvarchar](4000) NULL, [Image02] [int] NULL, [Comment_6780] [nvarchar](4000) NULL, [Description_6781] [nvarchar](4000) NULL, [Image03] [int] NULL, [Comment_6783] [nvarchar](4000) NULL, [Description_6784] [nvarchar](4000) NULL, [Image04] [int] NULL, [Comment_6786] [nvarchar](4000) NULL, CONSTRAINT [PK_Labels___Packaging] PRIMARY KEY CLUSTERED ([ReferenceId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Label_Approval_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Label_Approval_Status] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Label_Approval_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Label_Approval_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Label_Approval_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'dateisblank_calc_5480')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [dateisblank_calc_5480] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [dateisblank_calc_5480] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'setdate_calc_5481')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [setdate_calc_5481] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [setdate_calc_5481] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'blankdate_calc_5482')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [blankdate_calc_5482] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [blankdate_calc_5482] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Packaging_Approval_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Packaging_Approval_Status] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Packaging_Approval_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Packaging_Approval_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Packaging_Approval_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'dateisblank_calc_5485')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [dateisblank_calc_5485] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [dateisblank_calc_5485] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'setdate_calc_5486')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [setdate_calc_5486] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [setdate_calc_5486] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'blankdate_calc_5487')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [blankdate_calc_5487] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [blankdate_calc_5487] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6775')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6775] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6775] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image01')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image01] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6777')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6777] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6777] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6778')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6778] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6778] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image02')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image02] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6780')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6780] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6780] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6781')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6781] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6781] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image03')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image03] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6783')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6783] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6783] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6784')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6784] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6784] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image04')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image04] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6786')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6786] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Ref')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Ref] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Ref] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_6914')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_6914] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6922')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6922] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6922] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6923')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6923] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6923] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6924')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6924] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6924] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_6925')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_6925] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6926')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6926] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6926] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6927')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6927] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6927] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_6928')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_6928] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6929')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6929] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6929] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6930')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6930] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6930] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_6931')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_6931] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6932')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6932] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6932] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6933')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6933] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6933] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_6934')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_6934] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6935')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6935] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6935] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6936')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6936] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6936] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_6937')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_6937] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6938')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6938] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6938] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6939')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6939] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6939] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_6940')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_6940] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6941')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6941] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment_6941] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_6948')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_6948] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_6948] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_6949')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_6949] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_6950')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_6950] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Artwork__Labelon')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Artwork__Labelon] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Artwork_Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Artwork_Code] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Artwork_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SketchID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SketchID] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Artwork__Labelon__txt')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Artwork__Labelon__txt] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Artwork__Labelon__txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wash_Pot_Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wash_Pot_Code] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wash')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wash] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Wash] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Bleach')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Bleach] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Bleach] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Dry')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Dry] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Dry] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Extra_Dry')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Extra_Dry] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Extra_Dry] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Iron')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Iron] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Iron] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Dry_Clean')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Dry_Clean] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Dry_Clean] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_Care_Label')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_Care_Label] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Care_Label] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Care_Image_num')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Care_Image_num] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Care_Symbols')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Care_Symbols] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Care_Label_Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Care_Label_Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Care_Label_Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Disclaimer_Warning_1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Disclaimer_Warning_1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Disclaimer_Warning_2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Disclaimer_Warning_2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Disclaimer_Warning_3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Disclaimer_Warning_3] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Disclaimer_Warning_4')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Disclaimer_Warning_4] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Disclaimer_Warning_5')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Disclaimer_Warning_5] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wash_1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wash_1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Wash_1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Extra_Wash_1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Extra_Wash_1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Extra_Wash_1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Extra_Wash_2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Extra_Wash_2] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Extra_Wash_2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Spot_Clean')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Spot_Clean] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Spot_Clean] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Soak')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Soak] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Soak] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wring_Out')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wring_Out] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Wring_Out] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Reshape')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Reshape] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Reshape] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Extra_Dry_1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Extra_Dry_1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Extra_Dry_1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Iron_1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Iron_1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Iron_1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Extra_Dry_Clean')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Extra_Dry_Clean] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Extra_Dry_Clean] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Colour_Transfer')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Colour_Transfer] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Colour_Transfer] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Abrasion')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Abrasion] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Abrasion] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Miscellaneous')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Miscellaneous] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Miscellaneous] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Extra_Wash')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Extra_Wash] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Agent_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Agent_WI1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Manufacturer_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Manufacturer_WI1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Factory_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Factory_WI1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Agent_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Agent_WI2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Manufacturer_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Manufacturer_WI2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Factory_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Factory_WI2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Costing_Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Comments_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Comments_WI1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Costing_Comments_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Comments_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Comments_WI2] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Costing_Comments_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Type_5500')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Type_5500] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Price_5501')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Price_5501] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Price_Currency_5502')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Price_Currency_5502] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FOB_5503')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FOB_5503] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_5503] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FCA_5504')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FCA_5504] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_5504] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CPT_5505')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CPT_5505] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_5505] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CIF_5506')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CIF_5506] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_5506] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDP_5507')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDP_5507] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_5507] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDU_5508')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDU_5508] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_5508] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Type_Num_5509')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Type_Num_5509] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Status_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Status_WI1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Type_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Type_WI1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Price_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Price_WI1] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Price_Currency_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Price_Currency_WI1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FOB_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FOB_WI1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FCA_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FCA_WI1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CPT_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CPT_WI1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CIF_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CIF_WI1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDP_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDP_WI1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDU_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDU_WI1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_WI1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Type_Num_WI1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Type_Num_WI1] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Status_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Status_WI2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Type_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Type_WI2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Price_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Price_WI2] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Price_Currency_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Price_Currency_WI2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FOB_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FOB_WI2] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FCA_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FCA_WI2] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CPT_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CPT_WI2] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CIF_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CIF_WI2] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDP_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDP_WI2] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDU_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDU_WI2] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_WI2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Type_Num_WI2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Type_Num_WI2] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Country_From')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Country_From] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Country_To')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Country_To] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Branding_Cost_6493')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Branding_Cost_6493] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Branding_Cost_6493] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Agent_6494')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Agent_6494] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Agent_Commision_6495')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Agent_Commision_6495] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Type_6496')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Type_6496] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Price_6497')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Price_6497] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Price_Currency_6498')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Price_Currency_6498] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FOB_6499')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FOB_6499] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_6499] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CIF_6500')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CIF_6500] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_6500] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDP_6501')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDP_6501] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_6501] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FCA_6502')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FCA_6502] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_6502] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CPT_6503')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CPT_6503] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_6503] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Type_Num_6504')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Type_Num_6504] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Preference_Certificate_6505')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Preference_Certificate_6505] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Yes_Pref_6506')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Yes_Pref_6506] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Yes_Pref_6506] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'No_Pref_6507')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [No_Pref_6507] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [No_Pref_6507] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Yes_No_Num_6508')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Yes_No_Num_6508] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HTS_Code_6509')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HTS_Code_6509] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [HTS_Code_6509] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Rate_PrefCert_6510')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Rate_PrefCert_6510] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Rate_NoPrefCert_6511')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Rate_NoPrefCert_6511] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Rate___6512')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Rate___6512] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Total_Num_6513')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Total_Num_6513] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Type_6514')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Type_6514] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Eff_Date_6515')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Eff_Date_6515] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Rate___6516')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Rate___6516] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Num_6517')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Num_6517] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Commission___6518')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Commission___6518] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Commission_Num_6519')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Commission_Num_6519] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Miscellaneous_Total_6521')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Miscellaneous_Total_6521] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Exchange_Rate_6522')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Exchange_Rate_6522] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Exchange_Eff_Date_6523')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Exchange_Eff_Date_6523] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Exchange_Eff_Date_6523] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_6524')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_6524] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_6525')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_6525] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Commission_6526')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Commission_6526] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Landed_Price_6527')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Landed_Price_6527] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Landed_Price__CAD__6528')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Landed_Price__CAD__6528] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDU_6529')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDU_6529] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_6529] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'US_HTS_Code_6530')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [US_HTS_Code_6530] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [US_HTS_Code_6530] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HighRateDutyYes_6531')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HighRateDutyYes_6531] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HighRateDutyNo_6532')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HighRateDutyNo_6532] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HighRateDutyNum_6533')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HighRateDutyNum_6533] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Landed_Price__CAD__6534')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Landed_Price__CAD__6534] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Miscellaneous_Total_6536')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Miscellaneous_Total_6536] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total_Cost_6537')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total_Cost_6537] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'VAT_Group_6538')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [VAT_Group_6538] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [VAT_Group_6538] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Vat_Rate___6539')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Vat_Rate___6539] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'RRP_6540')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [RRP_6540] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Euro_RRP__€__6541')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Euro_RRP__€__6541] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'RRP___Vat_6542')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [RRP___Vat_6542] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'RRP_Margin___6543')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [RRP_Margin___6543] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Profit__CAD__6544')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Profit__CAD__6544] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wholesale_6545')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wholesale_6545] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Markup_6546')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Markup_6546] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wholesale_Margin___6547')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wholesale_Margin___6547] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wholesale_Profit_6548')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wholesale_Profit_6548] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'JL_List_Price_6549')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [JL_List_Price_6549] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Euro_List_Price_6550')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Euro_List_Price_6550] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SMS_Surcharge')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SMS_Surcharge] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Suppiler_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Suppiler_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total_Cost_6553')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total_Cost_6553] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SMS_Supplier_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SMS_Supplier_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SMS_Total_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SMS_Total_Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Category')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Category] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Quick_Search')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Quick_Search] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Quick_Search] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HTSCode')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HTSCode] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [HTSCode] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Costing_Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Costing_Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Supplier_Costing_Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'_Style___Colour_MOQ')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [_Style___Colour_MOQ] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [_Style___Colour_MOQ] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Landed_Price__CAD__6561')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Landed_Price__CAD__6561] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Miscellaneous_Total_6563')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Miscellaneous_Total_6563] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total_Cost_6564')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total_Cost_6564] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'VAT_Group_6565')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [VAT_Group_6565] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [VAT_Group_6565] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Vat_Rate___6566')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Vat_Rate___6566] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'RRP_6567')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [RRP_6567] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Euro_RRP__€__6568')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Euro_RRP__€__6568] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'RRP___Vat_6569')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [RRP___Vat_6569] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'RRP_Margin___6570')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [RRP_Margin___6570] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Profit__CAD__6571')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Profit__CAD__6571] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wholesale_6572')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wholesale_6572] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Markup_6573')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Markup_6573] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wholesale_Margin___6574')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wholesale_Margin___6574] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wholesale_Profit_6575')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wholesale_Profit_6575] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'JL_List_Price_6576')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [JL_List_Price_6576] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Euro_List_Price_6577')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Euro_List_Price_6577] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Landed_Price__CAD__6578')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Landed_Price__CAD__6578] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Miscellaneous_Total_6580')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Miscellaneous_Total_6580] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total_Cost_6581')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total_Cost_6581] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'VAT_Group_6582')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [VAT_Group_6582] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [VAT_Group_6582] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Vat_Rate___6583')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Vat_Rate___6583] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'RRP_6584')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [RRP_6584] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Euro_RRP__€__6585')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Euro_RRP__€__6585] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'RRP___Vat_6586')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [RRP___Vat_6586] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'RRP_Margin___6587')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [RRP_Margin___6587] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Profit__CAD__6588')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Profit__CAD__6588] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wholesale_6589')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wholesale_6589] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Markup_6590')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Markup_6590] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wholesale_Margin___6591')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wholesale_Margin___6591] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wholesale_Profit_6592')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wholesale_Profit_6592] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'JL_List_Price_6593')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [JL_List_Price_6593] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Euro_List_Price_6594')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Euro_List_Price_6594] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Branding_Cost_6595')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Branding_Cost_6595] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Branding_Cost_6595] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Agent_6596')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Agent_6596] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Agent_Commision_6597')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Agent_Commision_6597] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Type_6598')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Type_6598] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Price_6599')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Price_6599] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Price_Currency_6600')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Price_Currency_6600] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FOB_6601')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FOB_6601] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_6601] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CIF_6602')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CIF_6602] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_6602] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDP_6603')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDP_6603] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_6603] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FCA_6604')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FCA_6604] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_6604] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CPT_6605')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CPT_6605] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_6605] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Type_Num_6606')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Type_Num_6606] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Preference_Certificate_6607')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Preference_Certificate_6607] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Yes_Pref_6608')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Yes_Pref_6608] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Yes_Pref_6608] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'No_Pref_6609')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [No_Pref_6609] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [No_Pref_6609] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Yes_No_Num_6610')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Yes_No_Num_6610] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HTS_Code_6611')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HTS_Code_6611] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [HTS_Code_6611] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Rate_PrefCert_6612')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Rate_PrefCert_6612] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Rate_NoPrefCert_6613')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Rate_NoPrefCert_6613] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Rate___6614')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Rate___6614] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Total_Num_6615')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Total_Num_6615] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Type_6616')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Type_6616] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Eff_Date_6617')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Eff_Date_6617] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Rate___6618')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Rate___6618] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Num_6619')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Num_6619] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Commission___6620')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Commission___6620] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Commission_Num_6621')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Commission_Num_6621] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Miscellaneous_Total_6623')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Miscellaneous_Total_6623] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Exchange_Rate_6624')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Exchange_Rate_6624] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Exchange_Eff_Date_6625')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Exchange_Eff_Date_6625] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Exchange_Eff_Date_6625] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_6626')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_6626] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_6627')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_6627] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Commission_6628')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Commission_6628] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Landed_Price_6629')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Landed_Price_6629] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Landed_Price__CAD__6630')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Landed_Price__CAD__6630] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDU_6631')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDU_6631] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_6631] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'US_HTS_Code_6632')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [US_HTS_Code_6632] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [US_HTS_Code_6632] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HighRateDutyYes_6633')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HighRateDutyYes_6633] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HighRateDutyNo_6634')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HighRateDutyNo_6634] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HighRateDutyNum_6635')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HighRateDutyNum_6635] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Branding_Cost_6636')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Branding_Cost_6636] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Branding_Cost_6636] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Agent_6637')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Agent_6637] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Agent_Commision_6638')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Agent_Commision_6638] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Costing_Type_6639')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Costing_Type_6639] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Price_6640')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Price_6640] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Price_Currency_6641')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Price_Currency_6641] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FOB_6642')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FOB_6642] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FOB_6642] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CIF_6643')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CIF_6643] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CIF_6643] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDP_6644')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDP_6644] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDP_6644] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FCA_6645')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FCA_6645] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [FCA_6645] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CPT_6646')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CPT_6646] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CPT_6646] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Type_Num_6647')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Type_Num_6647] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Preference_Certificate_6648')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Preference_Certificate_6648] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Yes_Pref_6649')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Yes_Pref_6649] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Yes_Pref_6649] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'No_Pref_6650')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [No_Pref_6650] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [No_Pref_6650] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Yes_No_Num_6651')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Yes_No_Num_6651] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HTS_Code_6652')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HTS_Code_6652] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [HTS_Code_6652] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Rate_PrefCert_6653')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Rate_PrefCert_6653] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Rate_NoPrefCert_6654')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Rate_NoPrefCert_6654] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Rate___6655')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Rate___6655] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_Total_Num_6656')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_Total_Num_6656] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Type_6657')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Type_6657] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Eff_Date_6658')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Eff_Date_6658] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Rate___6659')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Rate___6659] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_Num_6660')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_Num_6660] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Commission___6661')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Commission___6661] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Commission_Num_6662')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Commission_Num_6662] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Miscellaneous_Total_6664')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Miscellaneous_Total_6664] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Exchange_Rate_6665')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Exchange_Rate_6665] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Exchange_Eff_Date_6666')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Exchange_Eff_Date_6666] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Exchange_Eff_Date_6666] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Duty_6667')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Duty_6667] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Freight_6668')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Freight_6668] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Commission_6669')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Commission_6669] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Landed_Price_6670')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Landed_Price_6670] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Landed_Price__CAD__6671')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Landed_Price__CAD__6671] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DDU_6672')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DDU_6672] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DDU_6672] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'US_HTS_Code_6673')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [US_HTS_Code_6673] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [US_HTS_Code_6673] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HighRateDutyYes_6674')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HighRateDutyYes_6674] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HighRateDutyNo_6675')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HighRateDutyNo_6675] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'HighRateDutyNum_6676')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [HighRateDutyNum_6676] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Classification')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Classification] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Type_2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Type_2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Season_3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Season_3] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Collection_4')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Collection_4] [int] NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_23')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_23] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_23] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Manager')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Manager] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Long_Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Long_Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Long_Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sample_Size_Detail')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sample_Size_Detail] [int] NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Style_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Style_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'State')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [State] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Print_Solid')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Print_Solid] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Needed_for_Calc')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Needed_for_Calc] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Needed_for_Calc] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Item_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Item_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Publish_to_ERP')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Publish_to_ERP] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Publish_to_ERP] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Published_to_ERP')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Published_to_ERP] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Published_to_ERP] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Publish_Failed_to_ERP')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Publish_Failed_to_ERP] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Publish_Failed_to_ERP] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Code] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Product_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Size_Range_5022')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Size_Range_5022] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DivisionBlock')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DivisionBlock] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Class_5024')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Class_5024] [int] NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Original_Reference')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Original_Reference] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Original_Reference] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'New_Carryover')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [New_Carryover] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Parent__Child')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Parent__Child] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Ref_Style_from_Past_SS')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Ref_Style_from_Past_SS] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Ref_Style_from_Past_SS] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Version_Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Version_Code] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Version_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fit_Type_5239')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fit_Type_5239] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Directional_Fabric')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Directional_Fabric] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Treatment_1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Treatment_1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Treatment_2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Treatment_2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Treatment_Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Treatment_Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Treatment_Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'BW_File')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [BW_File] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [BW_File] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Original_Image')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Original_Image] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Original_Image] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Class_5262')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Class_5262] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Class_Group')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Class_Group] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'French')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [French] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [French] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Inseam')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Inseam] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Manufacturer_COO')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Manufacturer_COO] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Garment_Factory')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Garment_Factory] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Name')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Name] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Name] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Gender_txt_7029')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Gender_txt_7029] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Gender_txt_7029] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Type_txt_7030')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Type_txt_7030] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Product_Type_txt_7030] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fit_Type_txt')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fit_Type_txt] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fit_Type_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CC_7032')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CC_7032] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CC_7032] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Gender_7033')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Gender_7033] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Gender_7033] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Type_7034')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Type_7034] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Product_Type_7034] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Details_7035')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Details_7035] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Details_7035] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fit_Type_7036')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fit_Type_7036] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fit_Type_7036] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'sketch_id')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [sketch_id] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [sketch_id] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ddl')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ddl] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Free_text')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Free_text] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Free_text] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Special_Customer')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Special_Customer] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Inseam_txt')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Inseam_txt] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Inseam_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Length')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Length] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Length] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Collection_7124')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Collection_7124] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Collection_7124] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Collection_txt')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Collection_txt] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Collection_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Selling_Period')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Selling_Period] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Private_Label')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Private_Label] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Vendor')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Vendor] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Code] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fabric_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'_of_Characters')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [_of_Characters] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_7162')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_7162] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_7162] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Char_Count_Check')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Char_Count_Check] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Char_Count_Check] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Gender_7171')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Gender_7171] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CC_7192')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CC_7192] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CC_7192] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Gender_txt_7193')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Gender_txt_7193] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Gender_txt_7193] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Type_txt_7194')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Type_txt_7194] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Product_Type_txt_7194] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Length_txt')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Length_txt] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Length_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Details_7198')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Details_7198] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Details_7198] [nvarchar](4000) NULL;';
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sets_details_fabric')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sets_details_fabric] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Notes')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Notes] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Notes] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Price_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Price_By] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Effective_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Effective_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Publish_to_ERP_Message')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Publish_to_ERP_Message] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Publish_to_ERP_Message] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Price_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Price_By] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Final_Cost_Meter')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Final_Cost_Meter] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Final_Cost_Yard')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Final_Cost_Yard] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
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
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Page_1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Page_1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Page_2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Page_2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Page_3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Page_3] [int] NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Trims_Concat')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Trims_Concat] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine3] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine3] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine4')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine4] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine4] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine5')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine5] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine5] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine6')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine6] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine6] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine7')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine7] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine7] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine8')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine8] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine8] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine9')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine9] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine9] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine10] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine11')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine11] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine11] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine12')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine12] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine12] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine13')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine13] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine13] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine14')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine14] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine14] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine15')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine15] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Userdefine15] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'RGB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [RGB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [RGB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Swatch')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Swatch] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Swatch] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Approv_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Approv_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Approved')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Approved] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Approved] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Name')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Name] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Name] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ReferenceCode')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ReferenceCode] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ReferenceCode] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ReferenceName')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ReferenceName] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ReferenceName] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Standard_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Standard_Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Selling_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Selling_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Retail')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Retail] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color_Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color_Code] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Color_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'First_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [First_Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SupplierColor')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SupplierColor] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [SupplierColor] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Effective_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Effective_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ERPID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ERPID] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ERPID] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ExternalImageLink')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ExternalImageLink] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ExternalImageLink] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Reference')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Reference] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Reference] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAN_Qty_Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAN_Qty_Color] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'USA_Qty_Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [USA_Qty_Color] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FOB_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FOB_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAN_Qty_Total')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAN_Qty_Total] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'USA_Qty_Total')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [USA_Qty_Total] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAN_PO_s')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAN_PO_s] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CAN_PO_s] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAN_ETA')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAN_ETA] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CAN_ETA] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'USA_PO_s')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [USA_PO_s] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [USA_PO_s] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'USA_ETA')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [USA_ETA] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [USA_ETA] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAN_PO_Created_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAN_PO_Created_Date] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [CAN_PO_Created_Date] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'USA_PO_Created_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [USA_PO_Created_Date] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [USA_PO_Created_Date] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color_Combo')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color_Combo] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Color_Combo] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DateBulk1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DateBulk1] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DateBulk2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DateBulk2] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DateBulk3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DateBulk3] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Date_SMS')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Date_SMS] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color_Price_p_m')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color_Price_p_m] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color_Price_p_y')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color_Price_p_y] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Price_p_m')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Price_p_m] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Price_p_y')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Price_p_y] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Active')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Active] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Active] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'USD_WS_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [USD_WS_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'USD_RET_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [USD_RET_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_VIP_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_VIP_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'USD_VIP_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [USD_VIP_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_WS_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_WS_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD_RET_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD_RET_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SketchId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SketchId] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'FirstCostCurrency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FirstCostCurrency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Selling_Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Selling_Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'USD')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [USD] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'CAD')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [CAD] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorReferenceTypeID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorReferenceTypeID] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorFamilyID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorFamilyID] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ProductColorNRF')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ProductColorNRF] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'NRF')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [NRF] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorFolderPath')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorFolderPath] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'GS1_Color_Group')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [GS1_Color_Group] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'GS1_Shade_Group')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [GS1_Shade_Group] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Colour_Risk')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Colour_Risk] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Userdefine2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Userdefine2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SS')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SS] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Colour_Risk_Comment')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Colour_Risk_Comment] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Bulk_1_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Bulk_1_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SMS')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SMS] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [SMS] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Bulk_2_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Bulk_2_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Bulk_3_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Bulk_3_Status] [int] NULL;'; EXEC sp_executesql @sql; END
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
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Print] [nvarchar](4000) NULL, [Description] [nvarchar](4000) NULL, [ProductReferenceID] [decimal](18, 2) NULL, [Comments] [nvarchar](4000) NULL, [Placement] [nvarchar](4000) NULL, [Product_Type] [int] NULL, [Sketch] [int] NULL, [Process_1] [int] NULL, [Process_2] [int] NULL, [Comments_By] [int] NULL, CONSTRAINT [PK_Artwork_BOM_prod] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Print')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Print] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Print] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ProductReferenceID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ProductReferenceID] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Placement')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Placement] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Placement] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Product_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Product_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sketch')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sketch] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Process_1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Process_1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Process_2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Process_2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments_By] [int] NULL;'; EXEC sp_executesql @sql; END
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

-- Fabric_BOM_prod
SET @TableName = @TablePrefix + N'Fabric_BOM_prod';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Fabric_Code] [nvarchar](4000) NULL, [Description_7844] [nvarchar](4000) NULL, [Main] [nvarchar](4000) NULL, [Supplier_Article] [nvarchar](4000) NULL, [Placement] [nvarchar](4000) NULL, [Gauges] [decimal](18, 2) NULL, [Status_Check] [nvarchar](4000) NULL, [ProductReferenceID] [decimal](18, 2) NULL, [Final_Total_Composition] [nvarchar](4000) NULL, [Weight] [decimal](18, 2) NULL, [Fabric_Status] [nvarchar](4000) NULL, [Certifications] [nvarchar](4000) NULL, [Description_7884] [nvarchar](4000) NULL, [Cost__YD] [nvarchar](4000) NULL, [Cuttable_Width] [decimal](18, 2) NULL, [Main_Fabric] [nvarchar](4000) NULL, [Main_Fabric_Composition] [nvarchar](4000) NULL, [Main_Weight] [decimal](18, 2) NULL, [Main_UOM] [nvarchar](4000) NULL, [Customer_Placement] [nvarchar](4000) NULL, [Color_Rule] [nvarchar](4000) NULL, [Pattern] [nvarchar](4000) NULL, [UOM_txt] [nvarchar](4000) NULL, [isColorway1] [nvarchar](4000) NULL, [isColorway2] [nvarchar](4000) NULL, [isColorway3] [nvarchar](4000) NULL, [isColorway4] [nvarchar](4000) NULL, [isColorway5] [nvarchar](4000) NULL, [isColorway6] [nvarchar](4000) NULL, [isColorway7] [nvarchar](4000) NULL, [isColorway8] [nvarchar](4000) NULL, [isColorway9] [nvarchar](4000) NULL, [isColorway10] [nvarchar](4000) NULL, [ColorStatus1] [nvarchar](4000) NULL, [ColorStatus2] [nvarchar](4000) NULL, [ColorStatus3] [nvarchar](4000) NULL, [ColorStatus4] [nvarchar](4000) NULL, [ColorStatus5] [nvarchar](4000) NULL, [ColorStatus6] [nvarchar](4000) NULL, [ColorStatus7] [nvarchar](4000) NULL, [ColorStatus8] [nvarchar](4000) NULL, [ColorStatus9] [nvarchar](4000) NULL, [ColorStatus10] [nvarchar](4000) NULL, [isColorway11] [nvarchar](4000) NULL, [isColorway12] [nvarchar](4000) NULL, [isColorway13] [nvarchar](4000) NULL, [isColorway14] [nvarchar](4000) NULL, [isColorway15] [nvarchar](4000) NULL, [isColorway16] [nvarchar](4000) NULL, [isColorway17] [nvarchar](4000) NULL, [isColorway18] [nvarchar](4000) NULL, [isColorway19] [nvarchar](4000) NULL, [isColorway20] [nvarchar](4000) NULL, [ColorStatus11] [nvarchar](4000) NULL, [ColorStatus12] [nvarchar](4000) NULL, [ColorStatus13] [nvarchar](4000) NULL, [ColorStatus14] [nvarchar](4000) NULL, [ColorStatus15] [nvarchar](4000) NULL, [ColorStatus16] [nvarchar](4000) NULL, [ColorStatus17] [nvarchar](4000) NULL, [ColorStatus18] [nvarchar](4000) NULL, [ColorStatus19] [nvarchar](4000) NULL, [ColorStatus20] [nvarchar](4000) NULL, [Consumption] [nvarchar](4000) NULL, [Season__Cost] [decimal](18, 2) NULL, [Fabric_Cost] [decimal](18, 2) NULL, [Comments] [nvarchar](4000) NULL, [UOM_ID] [nvarchar](4000) NULL, [Main_UOM_ID] [nvarchar](4000) NULL, [Sketch] [int] NULL, [UOM] [int] NULL, [Currency] [int] NULL, [Comment_By] [int] NULL, [Fabric_Type] [int] NULL, [Direction] [int] NULL, [Fabric_Name] [int] NULL, [Fabric_Mill] [int] NULL, CONSTRAINT [PK_Fabric_BOM_prod] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Code] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fabric_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_7844')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_7844] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_7844] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Article')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Article] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Supplier_Article] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Placement')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Placement] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Placement] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Gauges')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Gauges] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Status_Check')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Status_Check] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Status_Check] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ProductReferenceID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ProductReferenceID] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Final_Total_Composition')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Final_Total_Composition] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Final_Total_Composition] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Weight')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Weight] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Status] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fabric_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Certifications')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Certifications] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Certifications] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description_7884')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description_7884] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description_7884] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Cost__YD')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Cost__YD] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Cost__YD] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Cuttable_Width')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Cuttable_Width] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Fabric_Composition')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Fabric_Composition] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_Fabric_Composition] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_Weight')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_Weight] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_UOM] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_UOM] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Customer_Placement')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Customer_Placement] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Customer_Placement] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color_Rule')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color_Rule] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Color_Rule] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Pattern')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Pattern] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Pattern] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM_txt')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM_txt] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [UOM_txt] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway2] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway3] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway3] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway4')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway4] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway4] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway5')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway5] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway5] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway6')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway6] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway6] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway7')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway7] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway7] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway8')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway8] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway8] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway9')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway9] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway9] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway10] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus2] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus2] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus3')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus3] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus3] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus4')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus4] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus4] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus5')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus5] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus5] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus6')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus6] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus6] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus7')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus7] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus7] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus8')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus8] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus8] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus9')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus9] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus9] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus10')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus10] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus10] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway11')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway11] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway11] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway12')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway12] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway12] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway13')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway13] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway13] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway14')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway14] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway14] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway15')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway15] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway15] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway16')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway16] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway16] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway17')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway17] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway17] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway18')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway18] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway18] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway19')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway19] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway19] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'isColorway20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [isColorway20] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [isColorway20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus11')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus11] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus11] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus12')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus12] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus12] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus13')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus13] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus13] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus14')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus14] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus14] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus15')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus15] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus15] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus16')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus16] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus16] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus17')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus17] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus17] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus18')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus18] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus18] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus19')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus19] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus19] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorStatus20')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorStatus20] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorStatus20] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Consumption')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Consumption] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Consumption] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Season__Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Season__Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM_ID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM_ID] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [UOM_ID] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main_UOM_ID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main_UOM_ID] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main_UOM_ID] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sketch')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sketch] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_By] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Direction')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Direction] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Name')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Name] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Mill')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Mill] [int] NULL;'; EXEC sp_executesql @sql; END
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

-- Trim_BOM_prod
SET @TableName = @TablePrefix + N'Trim_BOM_prod';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Placement] [nvarchar](4000) NULL, [Qty] [decimal](18, 2) NULL, [Size] [nvarchar](4000) NULL, [Remarks_8091] [nvarchar](4000) NULL, [Remarks_8092] [nvarchar](4000) NULL, [Remarks_8093] [nvarchar](4000) NULL, [Remarks_8094] [nvarchar](4000) NULL, [Remarks_8095] [nvarchar](4000) NULL, [Remarks_8096] [nvarchar](4000) NULL, [Remarks_8097] [nvarchar](4000) NULL, [Remarks_8098] [nvarchar](4000) NULL, [Remarks_8099] [nvarchar](4000) NULL, [Remarks_8100] [nvarchar](4000) NULL, [Remarks_8101] [nvarchar](4000) NULL, [Remarks_8102] [nvarchar](4000) NULL, [Remarks_8103] [nvarchar](4000) NULL, [Remarks_8104] [nvarchar](4000) NULL, [Remarks_8105] [nvarchar](4000) NULL, [Remarks_8106] [nvarchar](4000) NULL, [Remarks_8107] [nvarchar](4000) NULL, [Remarks_8108] [nvarchar](4000) NULL, [Remarks_8109] [nvarchar](4000) NULL, [Remarks_8110] [nvarchar](4000) NULL, [ProductReferenceID] [decimal](18, 2) NULL, [Trim_Code] [nvarchar](4000) NULL, [Description] [nvarchar](4000) NULL, [Comments] [nvarchar](4000) NULL, [Trim_Type] [int] NULL, [Sketch] [int] NULL, [Trim_Supplier] [int] NULL, [Comment_By] [int] NULL, [Material] [int] NULL, CONSTRAINT [PK_Trim_BOM_prod] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Placement')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Placement] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Placement] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Qty')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Qty] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Size')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Size] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Size] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8091')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8091] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8091] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8092')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8092] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8092] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8093')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8093] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8093] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8094')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8094] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8094] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8095')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8095] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8095] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8096')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8096] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8096] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8097')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8097] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8097] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8098')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8098] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8098] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8099')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8099] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8099] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8100')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8100] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8100] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8101')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8101] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8101] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8102')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8102] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8102] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8103')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8103] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8103] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8104')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8104] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8104] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8105')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8105] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8105] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8106')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8106] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8106] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8107')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8107] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8107] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8108')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8108] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8108] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8109')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8109] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8109] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Remarks_8110')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Remarks_8110] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Remarks_8110] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ProductReferenceID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ProductReferenceID] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Trim_Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Trim_Code] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Trim_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Trim_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Trim_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sketch')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sketch] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Trim_Supplier')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Trim_Supplier] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_By] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Material')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Material] [int] NULL;'; EXEC sp_executesql @sql; END
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

-- Label_BOM_prod
SET @TableName = @TablePrefix + N'Label_BOM_prod';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Label_Code] [nvarchar](4000) NULL, [Description] [nvarchar](4000) NULL, [Qty] [decimal](18, 2) NULL, [Placement] [nvarchar](4000) NULL, [ProductReferenceID] [decimal](18, 2) NULL, [Width] [decimal](18, 2) NULL, [Height] [decimal](18, 2) NULL, [Old_Label] [nvarchar](4000) NULL, [Image_4_Description] [nvarchar](4000) NULL, [Cost] [decimal](18, 2) NULL, [Total] [decimal](18, 2) NULL, [Comments] [nvarchar](4000) NULL, [Image_1_Description] [nvarchar](4000) NULL, [Image_2_Description] [nvarchar](4000) NULL, [Image_3_Description] [nvarchar](4000) NULL, [Sketch] [int] NULL, [Width_UOM] [int] NULL, [Height_UOM] [int] NULL, [UOM] [int] NULL, [Currency] [int] NULL, [Comment_By] [int] NULL, [Season_Cost] [int] NULL, [Label_Status] [int] NULL, [Label_Type] [int] NULL, [Sub_Type] [int] NULL, [Material] [int] NULL, [Supplier] [int] NULL, CONSTRAINT [PK_Label_BOM_prod] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Label_Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Label_Code] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Label_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Qty')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Qty] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Placement')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Placement] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Placement] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ProductReferenceID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ProductReferenceID] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Width')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Width] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Height')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Height] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Old_Label')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Old_Label] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Old_Label] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_4_Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_4_Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Image_4_Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_1_Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_1_Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Image_1_Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_2_Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_2_Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Image_2_Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image_3_Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image_3_Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Image_3_Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sketch')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sketch] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Width_UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Width_UOM] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Height_UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Height_UOM] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_By] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Season_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Season_Cost] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Label_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Label_Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Label_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Label_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sub_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sub_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Material')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Material] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier] [int] NULL;'; EXEC sp_executesql @sql; END
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

-- Packaging_BOM_prod
SET @TableName = @TablePrefix + N'Packaging_BOM_prod';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Packaging_Code] [nvarchar](4000) NULL, [Description] [nvarchar](4000) NULL, [Qty] [decimal](18, 2) NULL, [Placement] [nvarchar](4000) NULL, [ProductReferenceID] [decimal](18, 2) NULL, [Width] [decimal](18, 2) NULL, [Height] [decimal](18, 2) NULL, [Old_Packaging] [nvarchar](4000) NULL, [Total] [decimal](18, 2) NULL, [Comments] [nvarchar](4000) NULL, [Cost] [decimal](18, 2) NULL, [Depth] [decimal](18, 2) NULL, [Packaging_Status] [nvarchar](4000) NULL, [Sketch] [int] NULL, [Width_UOM] [int] NULL, [Height_UOM] [int] NULL, [Depth_UOM] [int] NULL, [UOM] [int] NULL, [Currency] [int] NULL, [Comment_By] [int] NULL, [Season_Cost] [int] NULL, [Packaging_Type] [int] NULL, [Sub_Type] [int] NULL, [Material] [int] NULL, [Finish] [int] NULL, [Print] [int] NULL, [Supplier] [int] NULL, CONSTRAINT [PK_Packaging_BOM_prod] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Packaging_Code')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Packaging_Code] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Packaging_Code] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Qty')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Qty] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Placement')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Placement] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Placement] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ProductReferenceID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ProductReferenceID] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Width')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Width] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Height')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Height] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Old_Packaging')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Old_Packaging] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Old_Packaging] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Total')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Total] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Depth')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Depth] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Packaging_Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Packaging_Status] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Packaging_Status] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sketch')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sketch] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Width_UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Width_UOM] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Height_UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Height_UOM] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Depth_UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Depth_UOM] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'UOM')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [UOM] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_By')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_By] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Season_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Season_Cost] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Packaging_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Packaging_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sub_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sub_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Material')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Material] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Finish')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Finish] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Print')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Print] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier] [int] NULL;'; EXEC sp_executesql @sql; END
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

-- WashDetailStyle
SET @TableName = @TablePrefix + N'WashDetailStyle';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Wash_Name] [nvarchar](4000) NULL, [Original_Ref] [nvarchar](4000) NULL, [Factory] [nvarchar](4000) NULL, [Style] [nvarchar](4000) NULL, [Requested_Date] [datetime] NULL, [Send_to_Date] [datetime] NULL, [AWB] [nvarchar](4000) NULL, [Received_Date] [datetime] NULL, [Comments] [nvarchar](4000) NULL, [Wash_Ref] [nvarchar](4000) NULL, [Main] [nvarchar](4000) NULL, [Approved_Date] [datetime] NULL, [Placement] [nvarchar](4000) NULL, [Image1] [int] NULL, [Image2] [int] NULL, [Status] [int] NULL, [Denim_Fabric_Type] [int] NULL, [SS] [int] NULL, [Wash] [int] NULL, [Mill_Name] [int] NULL, [Fabric_Type] [int] NULL, [Dye_Type] [int] NULL, [Finish] [int] NULL, [Base_Color] [int] NULL, [Comment_by] [int] NULL, CONSTRAINT [PK_WashDetailStyle] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wash_Name')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wash_Name] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Wash_Name] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Original_Ref')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Original_Ref] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Original_Ref] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Factory')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Factory] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Factory] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Style')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Style] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Style] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Requested_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Requested_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Send_to_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Send_to_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'AWB')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [AWB] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [AWB] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Received_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Received_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comments')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comments] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comments] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wash_Ref')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wash_Ref] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Wash_Ref] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Main')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Main] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Main] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Approved_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Approved_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Placement')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Placement] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Placement] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image1] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Image2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Image2] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Denim_Fabric_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Denim_Fabric_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SS')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SS] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Wash')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Wash] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Mill_Name')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Mill_Name] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fabric_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fabric_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Dye_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Dye_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Finish')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Finish] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Base_Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Base_Color] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment_by')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment_by] [int] NULL;'; EXEC sp_executesql @sql; END
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

-- Miscellaneous_Factors
SET @TableName = @TablePrefix + N'Miscellaneous_Factors';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Charge_Type] [int] NULL, [Cost] [decimal](18, 2) NULL, CONSTRAINT [PK_Miscellaneous_Factors] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Charge_Type')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Charge_Type] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
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

-- SizeRunDetailGrid
SET @TableName = @TablePrefix + N'SizeRunDetailGrid';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [SizeRunDetail] [int] NULL, CONSTRAINT [PK_SizeRunDetailGrid] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SizeRunDetail')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SizeRunDetail] [int] NULL;'; EXEC sp_executesql @sql; END
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

-- DimensionDetailGrid
SET @TableName = @TablePrefix + N'DimensionDetailGrid';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [DimensionDetail] [int] NULL, CONSTRAINT [PK_DimensionDetailGrid] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
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

-- Trims_Approval
SET @TableName = @TablePrefix + N'Trims_Approval';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Season] [int] NULL, [Trim] [nvarchar](4000) NULL, [Description] [nvarchar](4000) NULL, [Status] [int] NULL, [Date] [datetime] NULL, [Comment] [nvarchar](4000) NULL, CONSTRAINT [PK_Trims_Approval] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Season')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Season] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Trim')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Trim] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Trim] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Status')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Status] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Comment')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Comment] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Comment] [nvarchar](4000) NULL;';
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

-- ProductCatagoryGrid
SET @TableName = @TablePrefix + N'ProductCatagoryGrid';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Category] [int] NULL, [SystemTypeName1] [nvarchar](4000) NULL, [SystemTypeName2] [nvarchar](4000) NULL, CONSTRAINT [PK_ProductCatagoryGrid] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Category')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Category] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SystemTypeName1')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SystemTypeName1] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [SystemTypeName1] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SystemTypeName2')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SystemTypeName2] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [SystemTypeName2] [nvarchar](4000) NULL;';
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

-- ProductWarehouseGrid
SET @TableName = @TablePrefix + N'ProductWarehouseGrid';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Warehouse] [int] NULL, [Description] [nvarchar](4000) NULL, CONSTRAINT [PK_ProductWarehouseGrid] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Warehouse')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Warehouse] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Description')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Description] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Description] [nvarchar](4000) NULL;';
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

-- Selected_Category_Colour_Dimension_Size
SET @TableName = @TablePrefix + N'Selected_Category_Colour_Dimension_Size';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Category] [int] NULL, [Color] [int] NULL, [Dimension] [int] NULL, [Size] [int] NULL, [Active] [nvarchar](4000) NULL, [ColorERPID] [nvarchar](4000) NULL, [IPC] [nvarchar](4000) NULL, [SizeOrder] [nvarchar](4000) NULL, [SizeRun] [int] NULL, [DimensionColorSizeActive] [nvarchar](4000) NULL, [Active_Count] [decimal](18, 2) NULL, [Publish_to_ERP] [nvarchar](4000) NULL, [TotalSizes] [nvarchar](4000) NULL, CONSTRAINT [PK_Selected_Category_Colour_Dimension_Size] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Category')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Category] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Dimension')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Dimension] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Size')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Size] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Active')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Active] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Active] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ColorERPID')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ColorERPID] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [ColorERPID] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'IPC')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [IPC] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [IPC] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SizeOrder')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SizeOrder] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [SizeOrder] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SizeRun')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SizeRun] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'DimensionColorSizeActive')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [DimensionColorSizeActive] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [DimensionColorSizeActive] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Active_Count')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Active_Count] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Publish_to_ERP')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Publish_to_ERP] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Publish_to_ERP] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'TotalSizes')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [TotalSizes] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [TotalSizes] [nvarchar](4000) NULL;';
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

-- Price_by_Style
SET @TableName = @TablePrefix + N'Price_by_Style';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Category] [int] NULL, [Color] [int] NULL, [Dimension] [int] NULL, [First_Cost] [decimal](18, 2) NULL, [First_Cost_Currency] [int] NULL, [Standard_Cost] [decimal](18, 2) NULL, [Effective_Date] [datetime] NULL, [Supplier_Color] [nvarchar](4000) NULL, [Sketch] [int] NULL, [Warehouse] [int] NULL, [Selling_Currency] [int] NULL, [Selling_Price] [decimal](18, 2) NULL, [Retail] [decimal](18, 2) NULL, CONSTRAINT [PK_Price_by_Style] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Category')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Category] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Dimension')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Dimension] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'First_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [First_Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'First_Cost_Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [First_Cost_Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Standard_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Standard_Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Effective_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Effective_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Color] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Supplier_Color] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sketch')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sketch] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Warehouse')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Warehouse] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Selling_Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Selling_Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Selling_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Selling_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Retail')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Retail] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
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

-- Selling_and_Retail_by_Style_Currency
SET @TableName = @TablePrefix + N'Selling_and_Retail_by_Style_Currency';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Selling_Currency] [int] NULL, [Selling_Price] [decimal](18, 2) NULL, [Retail] [decimal](18, 2) NULL, [VIP_Price] [decimal](18, 2) NULL, [Fob_Cost] [nvarchar](4000) NULL, CONSTRAINT [PK_Selling_and_Retail_by_Style_Currency] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Selling_Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Selling_Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Selling_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Selling_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Retail')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Retail] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'VIP_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [VIP_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Fob_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Fob_Cost] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Fob_Cost] [nvarchar](4000) NULL;';
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

-- Price_by_Colour_Dimension_Size_Warehouse
SET @TableName = @TablePrefix + N'Price_by_Colour_Dimension_Size_Warehouse';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Color] [int] NULL, [Dimension] [int] NULL, [Size] [int] NULL, [Warehouse] [int] NULL, [First_Cost] [decimal](18, 2) NULL, [Standard_Cost] [decimal](18, 2) NULL, [Selling_Price] [decimal](18, 2) NULL, [Retail] [decimal](18, 2) NULL, [Effective_Date] [datetime] NULL, [Supplier_Color] [nvarchar](4000) NULL, [First_Cost_Currency] [int] NULL, [Sketch] [int] NULL, [Selling_Currency] [int] NULL, CONSTRAINT [PK_Price_by_Colour_Dimension_Size_Warehouse] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Dimension')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Dimension] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Size')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Size] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Warehouse')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Warehouse] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'First_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [First_Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Standard_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Standard_Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Selling_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Selling_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Retail')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Retail] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Effective_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Effective_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Color] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Supplier_Color] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'First_Cost_Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [First_Cost_Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sketch')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sketch] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Selling_Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Selling_Currency] [int] NULL;'; EXEC sp_executesql @sql; END
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

-- Price_by_Color
SET @TableName = @TablePrefix + N'Price_by_Color';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ([RowId] INT IDENTITY(1,1) NOT NULL, [ReferenceId] INT NOT NULL, [Sort] INT NULL, [Category] [int] NULL, [Color] [int] NULL, [Dimension] [int] NULL, [First_Cost_Currency] [int] NULL, [Currency] [int] NULL, [Effective_Date] [datetime] NULL, [Supplier_Color] [nvarchar](4000) NULL, [Sketch] [int] NULL, [Warehouse] [int] NULL, [First_Cost] [nvarchar](4000) NULL, [Standard_Cost] [decimal](18, 2) NULL, [Selling_Price] [decimal](18, 2) NULL, [Retail_Price] [decimal](18, 2) NULL, CONSTRAINT [PK_Price_by_Color] PRIMARY KEY CLUSTERED ([RowId]) );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Category')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Category] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Color] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Dimension')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Dimension] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'First_Cost_Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [First_Cost_Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Currency')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Currency] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Effective_Date')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Effective_Date] [datetime] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Supplier_Color')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Supplier_Color] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [Supplier_Color] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Sketch')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Sketch] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Warehouse')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Warehouse] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'First_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [First_Cost] [nvarchar](4000) NULL;'; EXEC sp_executesql @sql; END
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [First_Cost] [nvarchar](4000) NULL;';
    EXEC sp_executesql @sql;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Standard_Cost')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Standard_Cost] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Selling_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Selling_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'Retail_Price')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Retail_Price] [decimal](18, 2) NULL;'; EXEC sp_executesql @sql; END
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


-- Grandchild table: Artwork_BOM_prodGrandColorway (BOM colorway pivot storage; links to host Artwork_BOM_prod via ParentRowId)
SET @TableName = @TablePrefix + N'Artwork_BOM_prodGrandColorway';
SET @HostTable = @TablePrefix + N'Artwork_BOM_prod';
SET @ParentFkName = N'FK_' + @TableName + N'_Parent';
SET @OldRefFkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' (
        [RowId] INT IDENTITY(1,1) NOT NULL,
        [ParentRowId] INT NOT NULL,
        [Colorway] INT NOT NULL,
        [ArtworkColor] [int] NULL,
        [ArtworkPhoto] [int] NULL,
        CONSTRAINT [PK_Artwork_BOM_prodGrandColorway] PRIMARY KEY CLUSTERED ([RowId])
    );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ParentRowId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ParentRowId] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'Colorway')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Colorway] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ArtworkColor')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ArtworkColor] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ArtworkPhoto')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ArtworkPhoto] [int] NULL;'; EXEC sp_executesql @sql; END
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @OldRefFkName)
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' DROP CONSTRAINT ' + QUOTENAME(@OldRefFkName) + N';'; EXEC sp_executesql @sql; END
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ReferenceId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' DROP COLUMN [ReferenceId];'; EXEC sp_executesql @sql; END
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @ParentFkName)
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName)
        + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@ParentFkName)
        + N' FOREIGN KEY ([ParentRowId]) REFERENCES dbo.' + QUOTENAME(@HostTable) + N' ([RowId]);';
    EXEC sp_executesql @sql;
END


-- Grandchild table: Trim_BOM_prodGrandColorway (BOM colorway pivot storage; links to host Trim_BOM_prod via ParentRowId)
SET @TableName = @TablePrefix + N'Trim_BOM_prodGrandColorway';
SET @HostTable = @TablePrefix + N'Trim_BOM_prod';
SET @ParentFkName = N'FK_' + @TableName + N'_Parent';
SET @OldRefFkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' (
        [RowId] INT IDENTITY(1,1) NOT NULL,
        [ParentRowId] INT NOT NULL,
        [Colorway] INT NOT NULL,
        [TrimColorway] [int] NULL,
        CONSTRAINT [PK_Trim_BOM_prodGrandColorway] PRIMARY KEY CLUSTERED ([RowId])
    );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ParentRowId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ParentRowId] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'Colorway')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Colorway] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'TrimColorway')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [TrimColorway] [int] NULL;'; EXEC sp_executesql @sql; END
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @OldRefFkName)
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' DROP CONSTRAINT ' + QUOTENAME(@OldRefFkName) + N';'; EXEC sp_executesql @sql; END
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ReferenceId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' DROP COLUMN [ReferenceId];'; EXEC sp_executesql @sql; END
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @ParentFkName)
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName)
        + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@ParentFkName)
        + N' FOREIGN KEY ([ParentRowId]) REFERENCES dbo.' + QUOTENAME(@HostTable) + N' ([RowId]);';
    EXEC sp_executesql @sql;
END


-- Grandchild table: Label_BOM_prodGrandColorway (BOM colorway pivot storage; links to host Label_BOM_prod via ParentRowId)
SET @TableName = @TablePrefix + N'Label_BOM_prodGrandColorway';
SET @HostTable = @TablePrefix + N'Label_BOM_prod';
SET @ParentFkName = N'FK_' + @TableName + N'_Parent';
SET @OldRefFkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' (
        [RowId] INT IDENTITY(1,1) NOT NULL,
        [ParentRowId] INT NOT NULL,
        [Colorway] INT NOT NULL,
        [LabelColorway] [int] NULL,
        [LabelPhoto] [int] NULL,
        CONSTRAINT [PK_Label_BOM_prodGrandColorway] PRIMARY KEY CLUSTERED ([RowId])
    );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ParentRowId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ParentRowId] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'Colorway')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Colorway] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'LabelColorway')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [LabelColorway] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'LabelPhoto')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [LabelPhoto] [int] NULL;'; EXEC sp_executesql @sql; END
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @OldRefFkName)
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' DROP CONSTRAINT ' + QUOTENAME(@OldRefFkName) + N';'; EXEC sp_executesql @sql; END
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ReferenceId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' DROP COLUMN [ReferenceId];'; EXEC sp_executesql @sql; END
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @ParentFkName)
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName)
        + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@ParentFkName)
        + N' FOREIGN KEY ([ParentRowId]) REFERENCES dbo.' + QUOTENAME(@HostTable) + N' ([RowId]);';
    EXEC sp_executesql @sql;
END


-- Grandchild table: Trims_ApprovalGrandColorway (BOM colorway pivot storage; links to host Trims_Approval via ParentRowId)
SET @TableName = @TablePrefix + N'Trims_ApprovalGrandColorway';
SET @HostTable = @TablePrefix + N'Trims_Approval';
SET @ParentFkName = N'FK_' + @TableName + N'_Parent';
SET @OldRefFkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' (
        [RowId] INT IDENTITY(1,1) NOT NULL,
        [ParentRowId] INT NOT NULL,
        [Colorway] INT NOT NULL,
        [ApprovalColor] [int] NULL,
        CONSTRAINT [PK_Trims_ApprovalGrandColorway] PRIMARY KEY CLUSTERED ([RowId])
    );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ParentRowId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ParentRowId] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'Colorway')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Colorway] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ApprovalColor')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ApprovalColor] [int] NULL;'; EXEC sp_executesql @sql; END
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @OldRefFkName)
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' DROP CONSTRAINT ' + QUOTENAME(@OldRefFkName) + N';'; EXEC sp_executesql @sql; END
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ReferenceId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' DROP COLUMN [ReferenceId];'; EXEC sp_executesql @sql; END
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @ParentFkName)
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName)
        + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@ParentFkName)
        + N' FOREIGN KEY ([ParentRowId]) REFERENCES dbo.' + QUOTENAME(@HostTable) + N' ([RowId]);';
    EXEC sp_executesql @sql;
END


-- Grandchild table: Fabric_BOM_prodGrandColorway (BOM colorway pivot storage; links to host Fabric_BOM_prod via ParentRowId)
SET @TableName = @TablePrefix + N'Fabric_BOM_prodGrandColorway';
SET @HostTable = @TablePrefix + N'Fabric_BOM_prod';
SET @ParentFkName = N'FK_' + @TableName + N'_Parent';
SET @OldRefFkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' (
        [RowId] INT IDENTITY(1,1) NOT NULL,
        [ParentRowId] INT NOT NULL,
        [Colorway] INT NOT NULL,
        [FabricColorway] [int] NULL,
        [FabricColorwayStatus] [int] NULL,
        CONSTRAINT [PK_Fabric_BOM_prodGrandColorway] PRIMARY KEY CLUSTERED ([RowId])
    );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ParentRowId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ParentRowId] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'Colorway')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [Colorway] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'FabricColorway')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FabricColorway] [int] NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'FabricColorwayStatus')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [FabricColorwayStatus] [int] NULL;'; EXEC sp_executesql @sql; END
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @OldRefFkName)
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' DROP CONSTRAINT ' + QUOTENAME(@OldRefFkName) + N';'; EXEC sp_executesql @sql; END
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ReferenceId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' DROP COLUMN [ReferenceId];'; EXEC sp_executesql @sql; END
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @ParentFkName)
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName)
        + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@ParentFkName)
        + N' FOREIGN KEY ([ParentRowId]) REFERENCES dbo.' + QUOTENAME(@HostTable) + N' ([RowId]);';
    EXEC sp_executesql @sql;
END

GO

