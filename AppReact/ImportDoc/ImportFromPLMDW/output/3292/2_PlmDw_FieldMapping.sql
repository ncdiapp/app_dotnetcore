-- =============================================================================
-- PLM DW â†’ APP field mapping (generated â€” see ImportFromPLMDW/PROMPT.md)
-- EXECUTION ORDER:
--   1. 1_PlmDw_Tables.sql
--   2. 2_PlmDw_FieldMapping.sql    (this file)
-- USER SETTING: @TablePrefix (must match PlmDw_Tables.sql). Default: Plm_
-- Table: {prefix}FieldMapping
-- =============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

DECLARE @TablePrefix NVARCHAR(32) = N'Plm_';   -- <<< USER SETTING
DECLARE @MappingTable NVARCHAR(128) = @TablePrefix + N'FieldMapping';
DECLARE @sql NVARCHAR(MAX);

IF OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable), N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable))
          AND name = N'DwTableName'
   )
BEGIN
    SET @sql = N'DROP TABLE dbo.' + QUOTENAME(@MappingTable) + N';';
    EXEC sp_executesql @sql;
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@MappingTable) + N' (
        [AppTableName]      NVARCHAR(128) NOT NULL,
        [AppColumnName]     NVARCHAR(128) NOT NULL,
        [DwTableName]       NVARCHAR(256) NOT NULL,
        [DwColumnName]      NVARCHAR(256) NOT NULL,
        [PlmTabId]          INT NULL,
        [PlmSubItemId]      INT NULL,
        [PlmGridSubItemId]  INT NULL,
        [PlmGridId]         INT NULL,
        [PlmMetaColumnId]   INT NULL,
        [PlmBlockId]        INT NULL,
        [DwFkTarget]        NVARCHAR(256) NULL,
        [FieldKind]         NVARCHAR(32)  NOT NULL,
        [PlmControlType]    INT NULL,
        [PlmEntityId]       INT NULL,
        [DwDataType]        NVARCHAR(32)  NULL,
        CONSTRAINT [PK_FieldMapping] PRIMARY KEY CLUSTERED ([AppTableName], [AppColumnName])
    );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable)) AND name = N'PlmControlType')
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@MappingTable) + N' ADD [PlmControlType] INT NULL;';
        EXEC sp_executesql @sql;
    END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable)) AND name = N'PlmEntityId')
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@MappingTable) + N' ADD [PlmEntityId] INT NULL;';
        EXEC sp_executesql @sql;
    END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable)) AND name = N'DwDataType')
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@MappingTable) + N' ADD [DwDataType] NVARCHAR(32) NULL;';
        EXEC sp_executesql @sql;
    END
    IF EXISTS (
        SELECT 1 FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable))
          AND c.name = N'FieldKind'
          AND c.max_length < 64
    )
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@MappingTable) + N' ALTER COLUMN [FieldKind] NVARCHAR(32) NOT NULL;';
        EXEC sp_executesql @sql;
    END
END

SET @sql = N'DELETE FROM dbo.' + QUOTENAME(@MappingTable)
    + N' WHERE [AppTableName] IN (N''@P@ReferenceBasicInfo'', N''@P@Style_Header_V2K_ERP'', N''@P@QC'', N''@P@Testing'', N''@P@SimpleQC'', N''@P@SimpleQCResult'', N''@P@Product_Test_Grid_reg'');';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;

-- FieldMapping INSERT batch 1 (224 row(s))
SET @sql = N'
INSERT INTO dbo.' + QUOTENAME(@MappingTable) + N' (
    [AppTableName],[AppColumnName],[DwTableName],[DwColumnName],
    [PlmTabId],[PlmSubItemId],[PlmGridSubItemId],[PlmGridId],[PlmMetaColumnId],
    [PlmBlockId],[DwFkTarget],[FieldKind],[PlmControlType],[PlmEntityId],[DwDataType]
)
VALUES
        (N''@P@Style_Header_V2K_ERP'', N''Classification'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Classification_1_FK_tblProductClass'', 3991, 1, NULL, NULL, NULL, NULL, N''tblProductClass'', N''TabField'', 1, 1, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Product_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Product_Type_2_FK_tblProductType'', 3991, 2, NULL, NULL, NULL, NULL, N''tblProductType'', N''TabField'', 1, 4, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Season_3'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Season_3_FK_tblSellingPeriod'', 3991, 3, NULL, NULL, NULL, NULL, N''tblSellingPeriod'', N''TabField'', 1, 8, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Collection'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Collection_4_FK_tblCollection'', 3991, 4, NULL, NULL, NULL, NULL, N''tblCollection'', N''TabField'', 1, 6, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Group'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Group_5_FK_tblGroup'', 3991, 5, NULL, NULL, NULL, NULL, N''tblGroup'', N''TabField'', 1, 7, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Sketch'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Sketch_6_FK_tblSketch'', 3991, 6, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Division_8'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Division_8_FK_tblCompanyDivision'', 3991, 8, NULL, NULL, NULL, NULL, N''tblCompanyDivision'', N''TabField'', 1, 5, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Size_Range_10'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Size_Range_10_FK_tblSizeRun'', 3991, 10, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Dimension_11'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Dimension_11_FK_tblDimension'', 3991, 11, NULL, NULL, NULL, NULL, N''tblDimension'', N''TabField'', 1, 9, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Article'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Article__22'', 3991, 22, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Description'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Description_23'', 3991, 23, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Composition'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Composition_47_FK_tblComposition'', 3991, 47, NULL, NULL, NULL, NULL, N''tblComposition'', N''TabField'', 1, 12, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Made_in_Country'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Made_in_Country_48_FK_tblCountry'', 3991, 48, NULL, NULL, NULL, NULL, N''tblCountry'', N''TabField'', 1, 59, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Vendor'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Vendor_56_FK_tblVendor'', 3991, 56, NULL, NULL, NULL, NULL, N''tblVendor'', N''TabField'', 1, 42, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Country_Of_Origin'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Country_Of_Origin_103_FK_tblCountry'', 3991, 103, NULL, NULL, NULL, NULL, N''tblCountry'', N''TabField'', 1, 59, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Product_Manager'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Product_Manager_109_FK_pdmsecuritywebuser'', 3991, 109, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField'', 1, 80, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Long_Description'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Long_Description_121'', 3991, 121, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Sample_Size_Detail'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Sample_Size_Detail_139_FK_tblSizeRunRotate'', 3991, 139, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Date_Shipping_1'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Date_Shipping_1_146'', 3991, 146, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Style_Header_V2K_ERP'', N''Date_Shipping_2'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Date_Shipping_2_147'', 3991, 147, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Style_Header_V2K_ERP'', N''CancelByDate'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CancelByDate_148'', 3991, 148, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Style_Header_V2K_ERP'', N''ProductTypeGroup'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''ProductTypeGroup_149_FK_tblProductClassGroup'', 3991, 149, NULL, NULL, NULL, NULL, N''tblProductClassGroup'', N''TabField'', 1, 95, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Size_Detail_Dispaly'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Size_Detail_Dispaly_150_FK_tblSizeRunRotate'', 3991, 150, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 2, 63, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Division_186'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Division_186_FK_tblCompanyDivision'', 3991, 186, NULL, NULL, NULL, NULL, N''tblCompanyDivision'', N''TabField'', 1, 5, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Created_By'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Created_By_189'', 3991, 189, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Last_Revised_By'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Last_Revised_By_190'', 3991, 190, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''ProductReferenceId'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''ProductReferenceId_197'', 3991, 197, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Style_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Style_Status_3004_FK_PLM_DW_UD_Finished_Good_Status_3501'', 3991, 3004, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Finished_Good_Status_3501'', N''TabField'', 1, 3501, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''State_3005'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''State_3005'', 3991, 3005, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Sample_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Sample_Status_3006'', 3991, 3006, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_Fit_1_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_Fit_1_Status_3007_FK_PLM_DW_UD_Sample_Status_3459'', 3991, 3007, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''IN_Fit_1_State'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''IN_Fit_1_State_3008'', 3991, 3008, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_Fit_2_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_Fit_2_Status_3009_FK_PLM_DW_UD_Sample_Status_3459'', 3991, 3009, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''IN_Fit_2_State'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''IN_Fit_2_State_3010'', 3991, 3010, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_Fit_3_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_Fit_3_Status_3011_FK_PLM_DW_UD_Sample_Status_3459'', 3991, 3011, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''IN_Fit_3_State'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''IN_Fit_3_State_3012'', 3991, 3012, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_Fit_4_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_Fit_4_Status_3013_FK_PLM_DW_UD_Sample_Status_3459'', 3991, 3013, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''IN_Fit_4_State'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''IN_Fit_4_State_3014'', 3991, 3014, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_PP_1_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_PP_1_Status_3015_FK_PLM_DW_UD_Sample_Status_3459'', 3991, 3015, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''IN_PP_1_State'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''IN_PP_1_State_3016'', 3991, 3016, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_PP_2_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_PP_2_Status_3017_FK_PLM_DW_UD_Sample_Status_3459'', 3991, 3017, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''IN_PP_2_State'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''IN_PP_2_State_3018'', 3991, 3018, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_PP_3_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_PP_3_Status_3019_FK_PLM_DW_UD_Sample_Status_3459'', 3991, 3019, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''IN_PP_3_State'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''IN_PP_3_State_3020'', 3991, 3020, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_TOP_1_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_TOP_1_Status_3021_FK_PLM_DW_UD_Sample_Status_3459'', 3991, 3021, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''IN_TOP_1_State'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''IN_TOP_1_State_3022'', 3991, 3022, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_TOP_2_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_TOP_2_Status_3023_FK_PLM_DW_UD_Sample_Status_3459'', 3991, 3023, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''IN_TOP_2_State'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''IN_TOP_2_State_3024'', 3991, 3024, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''CHK_Fit_1_Latest'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CHK_Fit_1_Latest_3025'', 3991, 3025, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CHK_Fit_2_Latest'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CHK_Fit_2_Latest_3026'', 3991, 3026, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CHK_Fit_3_Latest'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CHK_Fit_3_Latest_3027'', 3991, 3027, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CHK_Fit_4_Latest'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CHK_Fit_4_Latest_3028'', 3991, 3028, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CHK_PP_1_Latest'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CHK_PP_1_Latest_3029'', 3991, 3029, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CHK_PP_2_Latest'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CHK_PP_2_Latest_3030'', 3991, 3030, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CHK_PP_3_Latest'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CHK_PP_3_Latest_3031'', 3991, 3031, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CHK_TOP_1_Latest'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CHK_TOP_1_Latest_3032'', 3991, 3032, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CHK_TOP_2_Latest'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CHK_TOP_2_Latest_3033'', 3991, 3033, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''fit1status_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''fit1status_IB_3034'', 3991, 3034, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''fit2status_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''fit2status_IB_3035'', 3991, 3035, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''fit3status_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''fit3status_IB_3036'', 3991, 3036, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''fit4status_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''fit4status_IB_3037'', 3991, 3037, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''pp1status_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''pp1status_IB_3038'', 3991, 3038, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''pp2status_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''pp2status_IB_3039'', 3991, 3039, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''pp3status_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''pp3status_IB_3040'', 3991, 3040, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''top1status_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''top1status_IB_3041'', 3991, 3041, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''top2status_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''top2status_IB_3042'', 3991, 3042, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_Fit_1_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_Fit_1_Type_3043_FK_PLM_DW_UD_Sample_Type_3458'', 3991, 3043, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Type_3458'', N''TabField'', 1, 3458, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''fit1type_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''fit1type_IB_3044'', 3991, 3044, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_Fit_2_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_Fit_2_Type_3045_FK_PLM_DW_UD_Sample_Type_3458'', 3991, 3045, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Type_3458'', N''TabField'', 1, 3458, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''fit2type_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''fit2type_IB_3046'', 3991, 3046, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_Fit_3_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_Fit_3_Type_3047_FK_PLM_DW_UD_Sample_Type_3458'', 3991, 3047, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Type_3458'', N''TabField'', 1, 3458, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''fit3type_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''fit3type_IB_3048'', 3991, 3048, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_Fit_4_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_Fit_4_Type_3049_FK_PLM_DW_UD_Sample_Type_3458'', 3991, 3049, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Type_3458'', N''TabField'', 1, 3458, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''fit4type_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''fit4type_IB_3050'', 3991, 3050, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_PP_1_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_PP_1_Type_3051_FK_PLM_DW_UD_Sample_Type_3458'', 3991, 3051, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Type_3458'', N''TabField'', 1, 3458, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''pp1type_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''pp1type_IB_3052'', 3991, 3052, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_PP_2_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_PP_2_Type_3053_FK_PLM_DW_UD_Sample_Type_3458'', 3991, 3053, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Type_3458'', N''TabField'', 1, 3458, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''pp2type_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''pp2type_IB_3054'', 3991, 3054, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_PP_3_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_PP_3_Type_3055_FK_PLM_DW_UD_Sample_Type_3458'', 3991, 3055, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Type_3458'', N''TabField'', 1, 3458, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''pp3type_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''pp3type_IB_3056'', 3991, 3056, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_TOP_1_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_TOP_1_Type_3057_FK_PLM_DW_UD_Sample_Type_3458'', 3991, 3057, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Type_3458'', N''TabField'', 1, 3458, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''top1type_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''top1type_IB_3058'', 3991, 3058, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CB_TOP_2_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CB_TOP_2_Type_3059_FK_PLM_DW_UD_Sample_Type_3458'', 3991, 3059, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Type_3458'', N''TabField'', 1, 3458, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''top2type_IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''top2type_IB_3060'', 3991, 3060, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Calc_Fit_Status'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Calc_Fit_Status_3061'', 3991, 3061, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Item_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Item_Type_3707_FK_pdmTechPackType'', 3991, 3707, NULL, NULL, NULL, NULL, N''pdmTechPackType'', N''TabField'', 1, 123, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Supplier_Number'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Supplier_Number_3789'', 3991, 3789, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Publish_to_ERP'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Publish_to_ERP_3914'', 3991, 3914, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Published_to_ERP'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Published_to_ERP_3915'', 3991, 3915, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Supplier_Article_Number'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Supplier_Article_Number_3916'', 3991, 3916, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Global_ID'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Global_ID_3919'', 3991, 3919, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Publish_Failed_to_ERP'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Publish_Failed_to_ERP_3920'', 3991, 3920, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Composition_txt'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Composition_txt_4621'', 3991, 4621, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Sizerun'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Sizerun_4631_FK_tblSizeRun'', 3991, 4631, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''NumberSize'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''NumberSize_4632'', 3991, 4632, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Composition1'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Composition1__4922'', 3991, 4922, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Compositionfiber1'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Compositionfiber1_4923_FK_PLM_DW_UD_FiberCompositionBuilder_3626'', 3991, 4923, NULL, NULL, NULL, NULL, N''PLM_DW_UD_FiberCompositionBuilder_3626'', N''TabField'', 1, 3626, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Composition2'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Composition2__4924'', 3991, 4924, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Compositionfiber2'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Compositionfiber2_4925_FK_PLM_DW_UD_FiberCompositionBuilder_3626'', 3991, 4925, NULL, NULL, NULL, NULL, N''PLM_DW_UD_FiberCompositionBuilder_3626'', N''TabField'', 1, 3626, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Composition3'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Composition3__4926'', 3991, 4926, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Compositionfiber3'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Compositionfiber3_4927_FK_PLM_DW_UD_FiberCompositionBuilder_3626'', 3991, 4927, NULL, NULL, NULL, NULL, N''PLM_DW_UD_FiberCompositionBuilder_3626'', N''TabField'', 1, 3626, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp1'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp1__4928'', 3991, 4928, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp2'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp2__4929'', 3991, 4929, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp3'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp3__4930'', 3991, 4930, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber1CB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber1CB_4931_FK_PLM_DW_UD_FiberCompositionBuilder_3626'', 3991, 4931, NULL, NULL, NULL, NULL, N''PLM_DW_UD_FiberCompositionBuilder_3626'', N''TabField'', 1, 3626, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber2CB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber2CB_4932_FK_PLM_DW_UD_FiberCompositionBuilder_3626'', 3991, 4932, NULL, NULL, NULL, NULL, N''PLM_DW_UD_FiberCompositionBuilder_3626'', N''TabField'', 1, 3626, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber3CB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber3CB_4933_FK_PLM_DW_UD_FiberCompositionBuilder_3626'', 3991, 4933, NULL, NULL, NULL, NULL, N''PLM_DW_UD_FiberCompositionBuilder_3626'', N''TabField'', 1, 3626, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber1IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber1IB_4934'', 3991, 4934, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber2IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber2IB_4935'', 3991, 4935, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber3IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber3IB_4936'', 3991, 4936, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp_ok'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp_ok_4937'', 3991, 4937, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Total_Composition'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Total_Composition_4938'', 3991, 4938, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Image1'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Image1_4947_FK_tblSketch'', 3991, 4947, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Image2'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Image2_4948_FK_tblSketch'', 3991, 4948, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Image1Link'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Image1Link_4949'', 3991, 4949, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Image2Link'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Image2Link_4950'', 3991, 4950, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''style_size'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''style_size_4951_FK_tblSizeRunRotate'', 3991, 4951, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''sizeorder'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''sizeorder_4952'', 3991, 4952, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''CompositionDDL'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CompositionDDL_4980_FK_tblComposition'', 3991, 4980, NULL, NULL, NULL, NULL, N''tblComposition'', N''TabField'', 1, 12, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Valid_Selection'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Valid_Selection_4983'', 3991, 4983, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''CompositionTXT'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''CompositionTXT_4998'', 3991, 4998, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''comp1ok'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''comp1ok_4999'', 3991, 4999, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''comp2ok'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''comp2ok_5000'', 3991, 5000, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''comp3ok'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''comp3ok_5001'', 3991, 5001, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Product_Code'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Product_Code_5021'', 3991, 5021, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Size_Range_5022'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Size_Range_5022_FK_tblSizeRun'', 3991, 5022, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''DivisionBlock'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''DivisionBlock_5023_FK_tblCompanyDivision'', 3991, 5023, NULL, NULL, NULL, NULL, N''tblCompanyDivision'', N''TabField'', 1, 5, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Product_Class'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Product_Class_5024_FK_tblProductClass'', 3991, 5024, NULL, NULL, NULL, NULL, N''tblProductClass'', N''TabField'', 1, 1, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Dimension_5025'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Dimension_5025_FK_tblDimension'', 3991, 5025, NULL, NULL, NULL, NULL, N''tblDimension'', N''TabField'', 1, 9, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Season_5026'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Season_5026_FK_tblSellingPeriod'', 3991, 5026, NULL, NULL, NULL, NULL, N''tblSellingPeriod'', N''TabField'', 1, 8, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Price_Type'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Price_Type_5027_FK_empty'', 3991, 5027, NULL, NULL, NULL, NULL, N''empty'', N''TabField'', 1, 84, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''First_Cost_Currency'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''First_Cost_Currency_5028_FK_tblCurrency'', 3991, 5028, NULL, NULL, NULL, NULL, N''tblCurrency'', N''TabField'', 1, 68, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Valid_Size_Selection'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Valid_Size_Selection_5029'', 3991, 5029, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Valid_Product_Code_Selection'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Valid_Product_Code_Selection_5030'', 3991, 5030, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Valid_DivisionBlock_Selection'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Valid_DivisionBlock_Selection_5031'', 3991, 5031, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Valid_Product_Class_Selection'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Valid_Product_Class_Selection_5032'', 3991, 5032, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Valid_Dimension_Selection'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Valid_Dimension_Selection_5033'', 3991, 5033, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Valid_Season_Selection'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Valid_Season_Selection_5034'', 3991, 5034, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Valid_Price_Type_Selection'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Valid_Price_Type_Selection_5035'', 3991, 5035, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Valid_First_Cost_Currency_Selection'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Valid_First_Cost_Currency_Selection_5036'', 3991, 5036, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Color'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Color_5037'', 3991, 5037, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Valid_Color_Selection'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Valid_Color_Selection_5038'', 3991, 5038, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Active_Count'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Active_Count_5041'', 3991, 5041, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''DimensionColorSizeActiveBooleanSum'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''DimensionColorSizeActiveBooleanSum_5042'', 3991, 5042, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Style'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Style_5053_FK_tblProductClass'', 3991, 5053, NULL, NULL, NULL, NULL, N''tblProductClass'', N''TabField'', 1, 1, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Article_gen'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Article_gen_5059'', 3991, 5059, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Styleautogencalc'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Styleautogencalc_5060_FK_tblProductClass'', 3991, 5060, NULL, NULL, NULL, NULL, N''tblProductClass'', N''TabField'', 1, 1, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''EmptyPC'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''EmptyPC_5061'', 3991, 5061, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''test'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''test_5063'', 3991, 5063, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp1_Tx'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp1_Tx_5068'', 3991, 5068, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp2_Tx'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp2_Tx_5069'', 3991, 5069, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp3_Tx'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp3_Tx_5070'', 3991, 5070, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''state_5078'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''state_5078'', 3991, 5078, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''state_5079'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''state_5079'', 3991, 5079, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''state_5080'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''state_5080'', 3991, 5080, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Percent_Chk'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Percent_Chk_5081'', 3991, 5081, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp4'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp4__5105'', 3991, 5105, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp5'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp5__5106'', 3991, 5106, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber4CB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber4CB_5107_FK_PLM_DW_UD_FiberCompositionBuilder_3626'', 3991, 5107, NULL, NULL, NULL, NULL, N''PLM_DW_UD_FiberCompositionBuilder_3626'', N''TabField'', 1, 3626, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber5CB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber5CB_5108_FK_PLM_DW_UD_FiberCompositionBuilder_3626'', 3991, 5108, NULL, NULL, NULL, NULL, N''PLM_DW_UD_FiberCompositionBuilder_3626'', N''TabField'', 1, 3626, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber4IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber4IB_5109'', 3991, 5109, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber5IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber5IB_5110'', 3991, 5110, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''comp4ok'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''comp4ok_5111'', 3991, 5111, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''comp5ok'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''comp5ok_5112'', 3991, 5112, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp4_Tx'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp4_Tx_5113'', 3991, 5113, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp5_Tx'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp5_Tx_5114'', 3991, 5114, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''French'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''French_6745'', 3991, 6745, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Name'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Name_7028'', 3991, 7028, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Product_Type_txt'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Product_Type_txt_7030'', 3991, 7030, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''sketch_id'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''sketch_id_7043'', 3991, 7043, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''ddl'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''ddl_7045_FK_tblSketch'', 3991, 7045, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 1, 11, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Collection_txt'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Collection_txt_7125'', 3991, 7125, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp6'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp6__7320'', 3991, 7320, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber6CB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber6CB_7321_FK_PLM_DW_UD_FiberCompositionBuilder_3626'', 3991, 7321, NULL, NULL, NULL, NULL, N''PLM_DW_UD_FiberCompositionBuilder_3626'', N''TabField'', 1, 3626, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''Fiber6IB'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Fiber6IB_7322'', 3991, 7322, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''comp6ok'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''comp6ok_7323'', 3991, 7323, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Comp6_Tx'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Comp6_Tx_7324'', 3991, 7324, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''ProductTypeGroup_txt'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''ProductTypeGroup_txt_7352'', 3991, 7352, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Style_Header_V2K_ERP'', N''Subcategory'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Subcategory_7361_FK_PLM_DW_UD_Product_Class_Subcategories_4798'', 3991, 7361, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Product_Class_Subcategories_4798'', N''TabField'', 1, 4798, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''ERP_Season'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''ERP_Season_7362_FK_tblSellingPeriod'', 3991, 7362, NULL, NULL, NULL, NULL, N''tblSellingPeriod'', N''TabField'', 1, 8, N''int''),
        (N''@P@Style_Header_V2K_ERP'', N''French_Name'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''French_Name_7366'', 3991, 7366, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@QC'', N''SelectedSizes'', N''PLM_DW_Tab_QC_4029'', N''Selected_Size_174'', 4029, 174, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@QC'', N''QC_Color'', N''PLM_DW_Tab_QC_4029'', N''QC_Color_175_FK_PdmRGBColor'', 4029, 175, NULL, NULL, NULL, NULL, N''PdmRGBColor'', N''TabField'', 1, 172, N''int''),
        (N''@P@QC'', N''Comment_Date'', N''PLM_DW_Tab_QC_4029'', N''Comment_Date_3329'', 4029, 3329, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@QC'', N''Colors___Sizes_Comments'', N''PLM_DW_Tab_QC_4029'', N''Colors___Sizes_Comments_3330'', 4029, 3330, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@QC'', N''Measurement_Comments'', N''PLM_DW_Tab_QC_4029'', N''Measurement_Comments_3331'', 4029, 3331, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@QC'', N''Make_up_Fit_Comments'', N''PLM_DW_Tab_QC_4029'', N''Make_up_Fit_Comments_3332'', 4029, 3332, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@QC'', N''Conclusion'', N''PLM_DW_Tab_QC_4029'', N''Conclusion_3333'', 4029, 3333, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Testing'', N''Security_Group'', N''PLM_DW_Tab_Testing_4030'', N''Security_Group_3153_FK_pdmSecurityUserGroup'', 4030, 3153, NULL, NULL, NULL, NULL, N''pdmSecurityUserGroup'', N''TabField'', 1, 96, N''int''),
        (N''@P@Testing'', N''Created_by'', N''PLM_DW_Tab_Testing_4030'', N''Created_by_3154_FK_pdmsecuritywebuser'', 4030, 3154, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField'', 1, 80, N''int''),
        (N''@P@Testing'', N''Testing_Status'', N''PLM_DW_Tab_Testing_4030'', N''Testing_Status_3157_FK_PLM_DW_UD_Raw_Material_Status_3461'', 4030, 3157, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Status_3461'', N''TabField'', 1, 3461, N''int''),
        (N''@P@Testing'', N''State'', N''PLM_DW_Tab_Testing_4030'', N''State_3158'', 4030, 3158, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Testing'', N''Test_Date_Needed'', N''PLM_DW_Tab_Testing_4030'', N''Test_Date_Needed_3334'', 4030, 3334, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@SimpleQC'', N''CriticalPoint'', N''PLM_DW_Grid_SpecQCGrid_22'', N''CriticalPoint_557'', 4029, 557, 177, 22, 557, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@SimpleQC'', N''BodyPartDetailIDWDimDetailID'', N''PLM_DW_Grid_SpecQCGrid_22'', N''BodyPartDetailIDWDimDetailID_558'', 4029, 558, 177, 22, 558, NULL, NULL, N''GridColumn'', 1, NULL, N''int''),
        (N''@P@SimpleQC'', N''Code'', N''PLM_DW_Grid_SpecQCGrid_22'', N''Code_559'', 4029, 559, 177, 22, 559, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQC'', N''BodyPartName'', N''PLM_DW_Grid_SpecQCGrid_22'', N''BodyPartName_560'', 4029, 560, 177, 22, 560, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQC'', N''BodyPartDesc'', N''PLM_DW_Grid_SpecQCGrid_22'', N''BodyPartDesc_561'', 4029, 561, 177, 22, 561, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQC'', N''HowToMeasure'', N''PLM_DW_Grid_SpecQCGrid_22'', N''HowToMeasure_562'', 4029, 562, 177, 22, 562, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQC'', N''Tolerance'', N''PLM_DW_Grid_SpecQCGrid_22'', N''Tolerance_563'', 4029, 563, 177, 22, 563, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQC'', N''GradingBaseSize'', N''PLM_DW_Grid_SpecQCGrid_22'', N''GradingBaseSize_564'', 4029, 564, 177, 22, 564, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQC'', N''Commtents'', N''PLM_DW_Grid_SpecQCGrid_22'', N''Commtents_625'', 4029, 625, 177, 22, 625, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQC'', N''AddDesc'', N''PLM_DW_Grid_SpecQCGrid_22'', N''Add_Desc_626'', 4029, 626, 177, 22, 626, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQC'', N''NeedToApplyGradingRule'', N''PLM_DW_Grid_SpecQCGrid_22'', N''NeedToApplyGradingRule_652'', 4029, 652, 177, 22, 652, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@SimpleQC'', N''Dimension'', N''PLM_DW_Grid_SpecQCGrid_22'', N''Dimension_649_FK_tblDimension'', 4029, 649, 177, 22, 649, NULL, N''tblDimension'', N''GridColumn'', 1, 9, N''int''),
        (N''@P@SimpleQC'', N''DimensionDetail'', N''PLM_DW_Grid_SpecQCGrid_22'', N''DimensionDetail_646_FK_tblDimensionDetail'', 4029, 646, 177, 22, 646, NULL, N''tblDimensionDetail'', N''GridColumn'', 1, 73, N''int''),
        (N''@P@SimpleQCResult'', N''ParentRowId'', N''PLM_DW_Grid_SpecQCGrid_22'', N'''', 4029, 177, 177, 22, NULL, NULL, N''SimpleQC'', N''GrandchildPivot'', 2, NULL, N''int''),
        (N''@P@SimpleQCResult'', N''SizeRunSizeId'', N''PLM_DW_Grid_SpecQCGrid_22'', N'''', 4029, 177, 177, 22, NULL, NULL, NULL, N''GrandchildPivot'', 2, NULL, N''int''),
        (N''@P@SimpleQCResult'', N''GradingSize'', N''PLM_DW_Grid_SpecQCGrid_22'', N'''', 4029, 177, 177, 22, NULL, NULL, NULL, N''GrandchildPivot'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQCResult'', N''QCSize'', N''PLM_DW_Grid_SpecQCGrid_22'', N'''', 4029, 177, 177, 22, NULL, NULL, NULL, N''GrandchildPivot'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQCResult'', N''Difference'', N''PLM_DW_Grid_SpecQCGrid_22'', N'''', 4029, 177, 177, 22, NULL, NULL, NULL, N''GrandchildPivot'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQCResult'', N''QCSizeBeforeWash'', N''PLM_DW_Grid_SpecQCGrid_22'', N'''', 4029, 177, 177, 22, NULL, NULL, NULL, N''GrandchildPivot'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQCResult'', N''DiffBeforeWashAndGrading'', N''PLM_DW_Grid_SpecQCGrid_22'', N'''', 4029, 177, 177, 22, NULL, NULL, NULL, N''GrandchildPivot'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQCResult'', N''QCAfterWashIron'', N''PLM_DW_Grid_SpecQCGrid_22'', N'''', 4029, 177, 177, 22, NULL, NULL, NULL, N''GrandchildPivot'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQCResult'', N''DiffAfterIronAndGrading'', N''PLM_DW_Grid_SpecQCGrid_22'', N'''', 4029, 177, 177, 22, NULL, NULL, NULL, N''GrandchildPivot'', 2, NULL, N''nvarchar''),
        (N''@P@SimpleQCResult'', N''QCAfterIron'', N''PLM_DW_Grid_SpecQCGrid_22'', N'''', 4029, 177, 177, 22, NULL, NULL, NULL, N''GrandchildPivot'', 2, NULL, N''nvarchar''),
        (N''@P@Product_Test_Grid_reg'', N''Test'', N''PLM_DW_Grid_Product_Test_Grid_reg_3005'', N''Test__4235'', 4030, 4235, 3335, 3005, 4235, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@Product_Test_Grid_reg'', N''Test_Req'', N''PLM_DW_Grid_Product_Test_Grid_reg_3005'', N''Test_Req__4236'', 4030, 4236, 3335, 3005, 4236, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@Product_Test_Grid_reg'', N''Needed_By'', N''PLM_DW_Grid_Product_Test_Grid_reg_3005'', N''Needed_By_4239'', 4030, 4239, 3335, 3005, 4239, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@Product_Test_Grid_reg'', N''Status'', N''PLM_DW_Grid_Product_Test_Grid_reg_3005'', N''Status_4237_FK_PLM_DW_UD_Testing_Status_3507'', 4030, 4237, 3335, 3005, 4237, NULL, N''PLM_DW_UD_Testing_Status_3507'', N''GridColumn'', 1, 3507, N''int''),
        (N''@P@Product_Test_Grid_reg'', N''Completed'', N''PLM_DW_Grid_Product_Test_Grid_reg_3005'', N''Completed_4240'', 4030, 4240, 3335, 3005, 4240, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@Product_Test_Grid_reg'', N''Test_File'', N''PLM_DW_Grid_Product_Test_Grid_reg_3005'', N''Test_File_4238_FK_tblSketch'', 4030, 4238, 3335, 3005, 4238, NULL, N''tblSketch'', N''GridColumn'', 9, 11, N''nvarchar''),
        (N''@P@Product_Test_Grid_reg'', N''Test_Comments'', N''PLM_DW_Grid_Product_Test_Grid_reg_3005'', N''Test_Comments_4241'', 4030, 4241, 3335, 3005, 4241, NULL, NULL, N''GridColumn'', 4, NULL, N''nvarchar''),
        (N''@P@ReferenceBasicInfo'', N''ReferenceCode'', N''PLM_DW_Tab_Style_Header_V2K_ERP_3991'', N''Product_Code_5021'', 3991, 5021, NULL, NULL, NULL, NULL, NULL, N''ReferenceField'', 2, NULL, N''nvarchar'')
';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;
GO

