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
    + N' WHERE [AppTableName] IN (N''@P@ReferenceBasicInfo'', N''@P@Color_Patette_Header'', N''@P@ProductDesignColorGrid'');';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;

-- FieldMapping INSERT batch 1 (99 row(s))
SET @sql = N'
INSERT INTO dbo.' + QUOTENAME(@MappingTable) + N' (
    [AppTableName],[AppColumnName],[DwTableName],[DwColumnName],
    [PlmTabId],[PlmSubItemId],[PlmGridSubItemId],[PlmGridId],[PlmMetaColumnId],
    [PlmBlockId],[DwFkTarget],[FieldKind],[PlmControlType],[PlmEntityId],[DwDataType]
)
VALUES
        (N''@P@Color_Patette_Header'', N''Season'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Season_3_FK_tblSellingPeriod'', 4254, 3, NULL, NULL, NULL, NULL, N''tblSellingPeriod'', N''TabField'', 1, 8, N''int''),
        (N''@P@Color_Patette_Header'', N''Collection'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Collection_4_FK_tblCollection'', 4254, 4, NULL, NULL, NULL, NULL, N''tblCollection'', N''TabField'', 1, 6, N''int''),
        (N''@P@Color_Patette_Header'', N''Group'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Group_5_FK_tblGroup'', 4254, 5, NULL, NULL, NULL, NULL, N''tblGroup'', N''TabField'', 1, 7, N''int''),
        (N''@P@Color_Patette_Header'', N''Article'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Article__22'', 4254, 22, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Color_Patette_Header'', N''Description'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Description_23'', 4254, 23, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Color_Patette_Header'', N''Long_Description'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Long_Description_121'', 4254, 121, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Color_Patette_Header'', N''Brand'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Brand_141_FK_tblLabel'', 4254, 141, NULL, NULL, NULL, NULL, N''tblLabel'', N''TabField'', 1, 93, N''int''),
        (N''@P@Color_Patette_Header'', N''Created_By'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Created_By_189'', 4254, 189, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Color_Patette_Header'', N''Last_Revised_By'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Last_Revised_By_190'', 4254, 190, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Color_Patette_Header'', N''Product_Class'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Product_Class_6739_FK_tblProductClass'', 4254, 6739, NULL, NULL, NULL, NULL, N''tblProductClass'', N''TabField'', 1, 1, N''int''),
        (N''@P@Color_Patette_Header'', N''Tech_Pack_Type'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Tech_Pack_Type_6740_FK_pdmTechPackType'', 4254, 6740, NULL, NULL, NULL, NULL, N''pdmTechPackType'', N''TabField'', 1, 123, N''int''),
        (N''@P@Color_Patette_Header'', N''French'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''French_6745'', 4254, 6745, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Color_Patette_Header'', N''Name'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Name_7028'', 4254, 7028, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Color_Patette_Header'', N''Collection_txt'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Collection_txt_7125'', 4254, 7125, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Color_Patette_Header'', N''ERP_Season'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''ERP_Season_7362_FK_tblSellingPeriod'', 4254, 7362, NULL, NULL, NULL, NULL, N''tblSellingPeriod'', N''TabField'', 1, 8, N''int''),
        (N''@P@Color_Patette_Header'', N''French_Name'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''French_Name_7366'', 4254, 7366, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Active'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Active_7976'', NULL, 7976, 35, 7, 7976, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''SketchId'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SketchId_468_FK_tblSketch'', NULL, 468, 35, 7, 468, NULL, N''tblSketch'', N''GridColumn'', 5, 11, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ExternalImageLink'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ExternalImageLink_772'', NULL, 772, 35, 7, 772, NULL, NULL, N''GridColumn'', 38, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''SS'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SS_6633_FK_PLM_DW_UD_In_Season_3662'', NULL, 6633, 35, 7, 6633, NULL, N''PLM_DW_UD_In_Season_3662'', N''GridColumn'', 1, 3662, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Swatch'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Swatch_325'', NULL, 325, 35, 7, 325, NULL, NULL, N''GridColumn'', 24, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Name'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Name_372'', NULL, 372, 35, 7, 372, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ColorFamilyID'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ColorFamilyID_472_FK_pdmRGBColorFamily'', NULL, 472, 35, 7, 472, NULL, N''pdmRGBColorFamily'', N''GridColumn'', 1, 122, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Color'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_44_FK_pdmRGBColor'', NULL, 44, 35, 7, 44, NULL, N''pdmRGBColor'', N''GridColumn'', 1, 79, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Color_Code'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Code_635'', NULL, 635, 35, 7, 635, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Color_Combo'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Combo_7755'', NULL, 7755, 35, 7, 7755, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''SMS'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SMS_7717_FK_PLM_DW_UD_Rallye_Status_3667'', NULL, 7717, 35, 7, 7717, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 13, 3667, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Reference'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Reference_7583'', NULL, 7583, 35, 7, 7583, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Comments'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Comments_7584'', NULL, 7584, 35, 7, 7584, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Description'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Description_45'', NULL, 45, 35, 7, 45, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''RGB'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''RGB_324'', NULL, 324, 35, 7, 324, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ColorReferenceTypeID'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ColorReferenceTypeID_469_FK_pdmRGBColorReferenceType'', NULL, 469, 35, 7, 469, NULL, N''pdmRGBColorReferenceType'', N''GridColumn'', 1, 121, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ColorFolderPath'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ColorFolderPath_769_FK_pdmSEFolder'', NULL, 769, 35, 7, 769, NULL, N''pdmSEFolder'', N''GridColumn'', 1, 187, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ReferenceCode'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ReferenceCode_470'', NULL, 470, 35, 7, 470, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ReferenceName'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ReferenceName_471'', NULL, 471, 35, 7, 471, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''NRF'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''NRF_371_FK_tblColorNRF'', NULL, 371, 35, 7, 371, NULL, N''tblColorNRF'', N''GridColumn'', 1, 175, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ProductColorNRF'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ProductColorNRF_653_FK_tblColorNRF'', NULL, 653, 35, 7, 653, NULL, N''tblColorNRF'', N''GridColumn'', 1, 175, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Approved'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Approved_275'', NULL, 275, 35, 7, 275, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Approv_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Approv_Date_71'', NULL, 71, 35, 7, 71, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Image'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Image_274_FK_tblSketch'', NULL, 274, 35, 7, 274, NULL, N''tblSketch'', N''GridColumn'', 5, 11, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine1'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine1_305_FK_PLM_DW_UD_Color_Type_3525'', NULL, 305, 35, 7, 305, NULL, N''PLM_DW_UD_Color_Type_3525'', N''GridColumn'', 1, 3525, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine2'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine2_306_FK_PLM_DW_UD_Colourway_no_3526'', NULL, 306, 35, 7, 306, NULL, N''PLM_DW_UD_Colourway_no_3526'', N''GridColumn'', 1, 3526, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine3'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine3_307'', NULL, 307, 35, 7, 307, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine4'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine4_308'', NULL, 308, 35, 7, 308, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine5'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine5_309'', NULL, 309, 35, 7, 309, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine6'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine6_310'', NULL, 310, 35, 7, 310, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine7'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine7_311'', NULL, 311, 35, 7, 311, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine8'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine8_312'', NULL, 312, 35, 7, 312, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine9'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine9_313'', NULL, 313, 35, 7, 313, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine10'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine10_314'', NULL, 314, 35, 7, 314, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine11'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine11_315'', NULL, 315, 35, 7, 315, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine12'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine12_316'', NULL, 316, 35, 7, 316, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine13'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine13_317'', NULL, 317, 35, 7, 317, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine14'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine14_318'', NULL, 318, 35, 7, 318, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine15'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine15_319'', NULL, 319, 35, 7, 319, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''First_Cost'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''First_Cost_473'', NULL, 473, 35, 7, 473, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''FirstCostCurrency'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''FirstCostCurrency_474_FK_tblCurrency'', NULL, 474, 35, 7, 474, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Standard_Cost'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Standard_Cost_475'', NULL, 475, 35, 7, 475, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Selling_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Selling_Price_476'', NULL, 476, 35, 7, 476, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Selling_Currency'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Selling_Currency_636_FK_tblCurrency'', NULL, 636, 35, 7, 636, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Retail'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Retail_477'', NULL, 477, 35, 7, 477, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Effective_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Effective_Date_641'', NULL, 641, 35, 7, 641, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''SupplierColor'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SupplierColor_640'', NULL, 640, 35, 7, 640, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ERPID'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ERPID_655'', NULL, 655, 35, 7, 655, NULL, NULL, N''GridColumn'', 15, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Colour_Risk'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Colour_Risk_6634_FK_PLM_DW_UD_Yes_No_3494'', NULL, 6634, 35, 7, 6634, NULL, N''PLM_DW_UD_Yes_No_3494'', N''GridColumn'', 1, 3494, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Colour_Risk_Comment'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Colour_Risk_Comment_6635_FK_PLM_DW_UD_Colour_Risk_Comment_3663'', NULL, 6635, 35, 7, 6635, NULL, N''PLM_DW_UD_Colour_Risk_Comment_3663'', N''GridColumn'', 1, 3663, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Date_SMS'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Date_SMS_7761'', NULL, 7761, 35, 7, 7761, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Bulk_1_Status'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Bulk_1_Status_6636_FK_PLM_DW_UD_Rallye_Status_3667'', NULL, 6636, 35, 7, 6636, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 1, 3667, N''int''),
        (N''@P@ProductDesignColorGrid'', N''DateBulk1'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''DateBulk1_7756'', NULL, 7756, 35, 7, 7756, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Bulk_2_Status'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Bulk_2_Status_7759_FK_PLM_DW_UD_Rallye_Status_3667'', NULL, 7759, 35, 7, 7759, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 1, 3667, N''int''),
        (N''@P@ProductDesignColorGrid'', N''DateBulk2'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''DateBulk2_7757'', NULL, 7757, 35, 7, 7757, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Bulk_3_Status'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Bulk_3_Status_7760_FK_PLM_DW_UD_Rallye_Status_3667'', NULL, 7760, 35, 7, 7760, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 1, 3667, N''int''),
        (N''@P@ProductDesignColorGrid'', N''DateBulk3'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''DateBulk3_7758'', NULL, 7758, 35, 7, 7758, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''CAN_Qty_Color'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_Qty_Color_7631'', NULL, 7631, 35, 7, 7631, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAN_Qty_Total'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_Qty_Total_7640'', NULL, 7640, 35, 7, 7640, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USA_Qty_Color'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_Qty_Color_7632'', NULL, 7632, 35, 7, 7632, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USA_Qty_Total'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_Qty_Total_7641'', NULL, 7641, 35, 7, 7641, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''FOB_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''FOB_Price_7633'', NULL, 7633, 35, 7, 7633, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAD'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_8042_FK_tblCurrency'', NULL, 8042, 35, 7, 8042, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''CAD_WS_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_WS_Price_8043'', NULL, 8043, 35, 7, 8043, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAD_RET_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_RET_Price_8044'', NULL, 8044, 35, 7, 8044, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAD_VIP_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_VIP_Price_8049'', NULL, 8049, 35, 7, 8049, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USD'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_8045_FK_tblCurrency'', NULL, 8045, 35, 7, 8045, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''USD_WS_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_WS_Price_8046'', NULL, 8046, 35, 7, 8046, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USD_RET_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_RET_Price_8047'', NULL, 8047, 35, 7, 8047, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USD_VIP_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_VIP_Price_8050'', NULL, 8050, 35, 7, 8050, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAN_PO_s'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_PO_s_7642'', NULL, 7642, 35, 7, 7642, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''CAN_PO_Created_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_PO_Created_Date_7646'', NULL, 7646, 35, 7, 7646, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''CAN_ETA'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_ETA_7643'', NULL, 7643, 35, 7, 7643, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''USA_PO_s'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_PO_s_7644'', NULL, 7644, 35, 7, 7644, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''USA_PO_Created_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_PO_Created_Date_7647'', NULL, 7647, 35, 7, 7647, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''USA_ETA'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_ETA_7645'', NULL, 7645, 35, 7, 7645, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Fabric_Price_p_m'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Fabric_Price_p_m_7969'', NULL, 7969, 35, 7, 7969, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Fabric_Price_p_y'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Fabric_Price_p_y_7970'', NULL, 7970, 35, 7, 7970, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Color_Price_p_m'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Price_p_m_7966'', NULL, 7966, 35, 7, 7966, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Color_Price_p_y'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Price_p_y_7968'', NULL, 7968, 35, 7, 7968, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''GS1_Color_Group'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''GS1_Color_Group_770_FK_pdmGS1Color'', NULL, 770, 35, 7, 770, NULL, N''pdmGS1Color'', N''GridColumn'', 1, 190, N''int''),
        (N''@P@ProductDesignColorGrid'', N''GS1_Shade_Group'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''GS1_Shade_Group_771_FK_pdmGS1ColorShade'', NULL, 771, 35, 7, 771, NULL, N''pdmGS1ColorShade'', N''GridColumn'', 1, 192, N''int''),
        (N''@P@ReferenceBasicInfo'', N''ReferenceCode'', N''PLM_DW_Tab_Color_Patette_Header_4254'', N''Article__22'', 4254, 22, NULL, NULL, NULL, NULL, NULL, N''ReferenceField'', 2, NULL, N''nvarchar'')
';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;
GO

