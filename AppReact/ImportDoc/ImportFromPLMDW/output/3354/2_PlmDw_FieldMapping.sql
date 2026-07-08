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
    + N' WHERE [AppTableName] IN (N''@P@ReferenceBasicInfo'', N''@P@Label_Header'', N''@P@Label_Info'', N''@P@SizeRunDetailGrid'', N''@P@DimensionDetailGrid'', N''@P@ProductDesignColorGrid'', N''@P@MaterialCosting_reg'', N''@P@Trims_Tracking'');';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;

-- FieldMapping INSERT batch 1 (202 row(s))
SET @sql = N'
INSERT INTO dbo.' + QUOTENAME(@MappingTable) + N' (
    [AppTableName],[AppColumnName],[DwTableName],[DwColumnName],
    [PlmTabId],[PlmSubItemId],[PlmGridSubItemId],[PlmGridId],[PlmMetaColumnId],
    [PlmBlockId],[DwFkTarget],[FieldKind],[PlmControlType],[PlmEntityId],[DwDataType]
)
VALUES
        (N''@P@Label_Header'', N''Classification'', N''PLM_DW_Tab_Label_Header_4247'', N''Classification_1_FK_tblProductClass'', 4247, 1, NULL, NULL, NULL, NULL, N''tblProductClass'', N''TabField'', 1, 1, N''int''),
        (N''@P@Label_Header'', N''Product_Type'', N''PLM_DW_Tab_Label_Header_4247'', N''Product_Type_2_FK_tblProductType'', 4247, 2, NULL, NULL, NULL, NULL, N''tblProductType'', N''TabField'', 1, 4, N''int''),
        (N''@P@Label_Header'', N''Season'', N''PLM_DW_Tab_Label_Header_4247'', N''Season_3_FK_tblSellingPeriod'', 4247, 3, NULL, NULL, NULL, NULL, N''tblSellingPeriod'', N''TabField'', 1, 8, N''int''),
        (N''@P@Label_Header'', N''Collection'', N''PLM_DW_Tab_Label_Header_4247'', N''Collection_4_FK_tblCollection'', 4247, 4, NULL, NULL, NULL, NULL, N''tblCollection'', N''TabField'', 1, 6, N''int''),
        (N''@P@Label_Header'', N''Group'', N''PLM_DW_Tab_Label_Header_4247'', N''Group_5_FK_tblGroup'', 4247, 5, NULL, NULL, NULL, NULL, N''tblGroup'', N''TabField'', 1, 7, N''int''),
        (N''@P@Label_Header'', N''Sketch'', N''PLM_DW_Tab_Label_Header_4247'', N''Sketch_6_FK_tblSketch'', 4247, 6, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Label_Header'', N''Division_8'', N''PLM_DW_Tab_Label_Header_4247'', N''Division_8_FK_tblCompanyDivision'', 4247, 8, NULL, NULL, NULL, NULL, N''tblCompanyDivision'', N''TabField'', 1, 5, N''int''),
        (N''@P@Label_Header'', N''Size_Range'', N''PLM_DW_Tab_Label_Header_4247'', N''Size_Range_10_FK_tblSizeRun'', 4247, 10, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Label_Header'', N''Dimension'', N''PLM_DW_Tab_Label_Header_4247'', N''Dimension_11_FK_tblDimension'', 4247, 11, NULL, NULL, NULL, NULL, N''tblDimension'', N''TabField'', 1, 9, N''int''),
        (N''@P@Label_Header'', N''Article'', N''PLM_DW_Tab_Label_Header_4247'', N''Article__22'', 4247, 22, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Description'', N''PLM_DW_Tab_Label_Header_4247'', N''Description_23'', 4247, 23, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Country_Of_Origin'', N''PLM_DW_Tab_Label_Header_4247'', N''Country_Of_Origin_103_FK_tblCountry'', 4247, 103, NULL, NULL, NULL, NULL, N''tblCountry'', N''TabField'', 1, 59, N''int''),
        (N''@P@Label_Header'', N''Product_Manager'', N''PLM_DW_Tab_Label_Header_4247'', N''Product_Manager_109_FK_pdmsecuritywebuser'', 4247, 109, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField'', 1, 80, N''int''),
        (N''@P@Label_Header'', N''Long_Description'', N''PLM_DW_Tab_Label_Header_4247'', N''Long_Description_121'', 4247, 121, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Sample_Size_Detail'', N''PLM_DW_Tab_Label_Header_4247'', N''Sample_Size_Detail_139_FK_tblSizeRunRotate'', 4247, 139, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Label_Header'', N''Brand'', N''PLM_DW_Tab_Label_Header_4247'', N''Brand_141_FK_tblLabel'', 4247, 141, NULL, NULL, NULL, NULL, N''tblLabel'', N''TabField'', 1, 93, N''int''),
        (N''@P@Label_Header'', N''ProductTypeGroup'', N''PLM_DW_Tab_Label_Header_4247'', N''ProductTypeGroup_149_FK_tblProductClassGroup'', 4247, 149, NULL, NULL, NULL, NULL, N''tblProductClassGroup'', N''TabField'', 1, 95, N''int''),
        (N''@P@Label_Header'', N''Size_Detail_Dispaly'', N''PLM_DW_Tab_Label_Header_4247'', N''Size_Detail_Dispaly_150_FK_tblSizeRunRotate'', 4247, 150, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 2, 63, N''nvarchar''),
        (N''@P@Label_Header'', N''Division_186'', N''PLM_DW_Tab_Label_Header_4247'', N''Division_186_FK_tblCompanyDivision'', 4247, 186, NULL, NULL, NULL, NULL, N''tblCompanyDivision'', N''TabField'', 1, 5, N''int''),
        (N''@P@Label_Header'', N''Created_By'', N''PLM_DW_Tab_Label_Header_4247'', N''Created_By_189'', 4247, 189, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Last_Revised_By'', N''PLM_DW_Tab_Label_Header_4247'', N''Last_Revised_By_190'', 4247, 190, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''ProductReferenceId'', N''PLM_DW_Tab_Label_Header_4247'', N''ProductReferenceId_197'', 4247, 197, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Raw_Material_Status'', N''PLM_DW_Tab_Label_Header_4247'', N''Raw_Material_Status_3128_FK_PLM_DW_UD_Raw_Material_Status_3461'', 4247, 3128, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Status_3461'', N''TabField'', 1, 3461, N''int''),
        (N''@P@Label_Header'', N''State'', N''PLM_DW_Tab_Label_Header_4247'', N''State_3129'', 4247, 3129, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Label_Header'', N''Item_Type'', N''PLM_DW_Tab_Label_Header_4247'', N''Item_Type_3711_FK_pdmTechPackType'', 4247, 3711, NULL, NULL, NULL, NULL, N''pdmTechPackType'', N''TabField'', 1, 123, N''int''),
        (N''@P@Label_Header'', N''Material'', N''PLM_DW_Tab_Label_Header_4247'', N''Material_3810_FK_PLM_DW_UD_Label_Material_3521'', 4247, 3810, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Label_Material_3521'', N''TabField'', 1, 3521, N''int''),
        (N''@P@Label_Header'', N''Supplier_Article_Number'', N''PLM_DW_Tab_Label_Header_4247'', N''Supplier_Article_Number_3916'', 4247, 3916, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Release_to_Manufacturer'', N''PLM_DW_Tab_Label_Header_4247'', N''Release_to_Manufacturer_5095_FK_PLM_DW_UD_Yes_No_3494'', 4247, 5095, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Yes_No_3494'', N''TabField'', 1, 3494, N''int''),
        (N''@P@Label_Header'', N''Old_Code'', N''PLM_DW_Tab_Label_Header_4247'', N''Old_Code_6684'', 4247, 6684, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Label_Reference'', N''PLM_DW_Tab_Label_Header_4247'', N''Label_Reference_6685'', 4247, 6685, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Process_Type_1'', N''PLM_DW_Tab_Label_Header_4247'', N''Process_Type_1_6686_FK_PLM_DW_UD_label_print_type_4713'', 4247, 6686, NULL, NULL, NULL, NULL, N''PLM_DW_UD_label_print_type_4713'', N''TabField'', 1, 4713, N''int''),
        (N''@P@Label_Header'', N''Cost_1'', N''PLM_DW_Tab_Label_Header_4247'', N''Cost_1_6688'', 4247, 6688, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Label_Header'', N''French'', N''PLM_DW_Tab_Label_Header_4247'', N''French_6745'', 4247, 6745, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Process_Type_2'', N''PLM_DW_Tab_Label_Header_4247'', N''Process_Type_2_6794_FK_PLM_DW_UD_label_print_type_4713'', 4247, 6794, NULL, NULL, NULL, NULL, N''PLM_DW_UD_label_print_type_4713'', N''TabField'', 1, 4713, N''int''),
        (N''@P@Label_Header'', N''Process_Type_3'', N''PLM_DW_Tab_Label_Header_4247'', N''Process_Type_3_6795_FK_PLM_DW_UD_label_print_type_4713'', 4247, 6795, NULL, NULL, NULL, NULL, N''PLM_DW_UD_label_print_type_4713'', N''TabField'', 1, 4713, N''int''),
        (N''@P@Label_Header'', N''Supplier_1'', N''PLM_DW_Tab_Label_Header_4247'', N''Supplier_1_6885_FK_PLM_DW_UD_Trims_Supplier_4757'', 4247, 6885, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Trims_Supplier_4757'', N''TabField'', 1, 4757, N''int''),
        (N''@P@Label_Header'', N''Release_Day'', N''PLM_DW_Tab_Label_Header_4247'', N''Release_Day_7024'', 4247, 7024, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Label_Header'', N''Name'', N''PLM_DW_Tab_Label_Header_4247'', N''Name_7028'', 4247, 7028, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Product_Type_txt'', N''PLM_DW_Tab_Label_Header_4247'', N''Product_Type_txt_7030'', 4247, 7030, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''sketch_id'', N''PLM_DW_Tab_Label_Header_4247'', N''sketch_id_7043'', 4247, 7043, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''ddl'', N''PLM_DW_Tab_Label_Header_4247'', N''ddl_7045_FK_tblSketch'', 4247, 7045, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 1, 11, N''int''),
        (N''@P@Label_Header'', N''Collection_txt'', N''PLM_DW_Tab_Label_Header_4247'', N''Collection_txt_7125'', 4247, 7125, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Supplier_2'', N''PLM_DW_Tab_Label_Header_4247'', N''Supplier_2_7333_FK_PLM_DW_UD_Trims_Supplier_4757'', 4247, 7333, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Trims_Supplier_4757'', N''TabField'', 1, 4757, N''int''),
        (N''@P@Label_Header'', N''Cost_2'', N''PLM_DW_Tab_Label_Header_4247'', N''Cost_2_7335'', 4247, 7335, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Label_Header'', N''Cost_3'', N''PLM_DW_Tab_Label_Header_4247'', N''Cost_3_7337'', 4247, 7337, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Label_Header'', N''USD_for_7339'', N''PLM_DW_Tab_Label_Header_4247'', N''USD_for_7339'', 4247, 7339, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''USD_for_7342'', N''PLM_DW_Tab_Label_Header_4247'', N''USD_for_7342'', 4247, 7342, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''USD_for_7344'', N''PLM_DW_Tab_Label_Header_4247'', N''USD_for_7344'', 4247, 7344, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''ProductTypeGroup_txt'', N''PLM_DW_Tab_Label_Header_4247'', N''ProductTypeGroup_txt_7352'', 4247, 7352, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Header'', N''Subcategory'', N''PLM_DW_Tab_Label_Header_4247'', N''Subcategory_7361_FK_PLM_DW_UD_Product_Class_Subcategories_4798'', 4247, 7361, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Product_Class_Subcategories_4798'', N''TabField'', 1, 4798, N''int''),
        (N''@P@Label_Header'', N''ERP_Season'', N''PLM_DW_Tab_Label_Header_4247'', N''ERP_Season_7362_FK_tblSellingPeriod'', 4247, 7362, NULL, NULL, NULL, NULL, N''tblSellingPeriod'', N''TabField'', 1, 8, N''int''),
        (N''@P@Label_Header'', N''French_Name'', N''PLM_DW_Tab_Label_Header_4247'', N''French_Name_7366'', 4247, 7366, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Info'', N''Construction'', N''PLM_DW_Tab_Label_Info_4248'', N''Construction_3161_FK_PLM_DW_UD_Label_Construction_4724'', 4248, 3161, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Label_Construction_4724'', N''TabField'', 1, 4724, N''int''),
        (N''@P@Label_Info'', N''Width'', N''PLM_DW_Tab_Label_Info_4248'', N''Width_4133'', 4248, 4133, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Label_Info'', N''Width_Unit'', N''PLM_DW_Tab_Label_Info_4248'', N''Width_Unit_4134_FK_tblUnitOfMeasure'', 4248, 4134, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField'', 1, 67, N''int''),
        (N''@P@Label_Info'', N''Height'', N''PLM_DW_Tab_Label_Info_4248'', N''Height_4179'', 4248, 4179, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Label_Info'', N''Height_Unit'', N''PLM_DW_Tab_Label_Info_4248'', N''Height_Unit_4180_FK_tblUnitOfMeasure'', 4248, 4180, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField'', 1, 67, N''int''),
        (N''@P@Label_Info'', N''Lead_Time'', N''PLM_DW_Tab_Label_Info_4248'', N''Lead_Time_6689'', 4248, 6689, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Info'', N''Lead_Time_UOM'', N''PLM_DW_Tab_Label_Info_4248'', N''Lead_Time_UOM_6690_FK_PLM_DW_UD_LeadTime_3678'', 4248, 6690, NULL, NULL, NULL, NULL, N''PLM_DW_UD_LeadTime_3678'', N''TabField'', 1, 3678, N''int''),
        (N''@P@Label_Info'', N''Bulk_MOQ'', N''PLM_DW_Tab_Label_Info_4248'', N''Bulk_MOQ_6691'', 4248, 6691, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Info'', N''Sample_MOQ'', N''PLM_DW_Tab_Label_Info_4248'', N''Sample_MOQ_6692'', 4248, 6692, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Label_Info'', N''MOQ_UOM'', N''PLM_DW_Tab_Label_Info_4248'', N''MOQ_UOM_6693_FK_tblUnitOfMeasure'', 4248, 6693, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField'', 1, 67, N''int''),
        (N''@P@Label_Info'', N''File_Attachment_1'', N''PLM_DW_Tab_Label_Info_4248'', N''File_Attachment_1_6694_FK_tblSketch'', 4248, 6694, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Label_Info'', N''File_Attachment_2'', N''PLM_DW_Tab_Label_Info_4248'', N''File_Attachment_2_6695_FK_tblSketch'', 4248, 6695, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Label_Info'', N''File_Attachment_3'', N''PLM_DW_Tab_Label_Info_4248'', N''File_Attachment_3_6696_FK_tblSketch'', 4248, 6696, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Label_Info'', N''File_Attachment_4'', N''PLM_DW_Tab_Label_Info_4248'', N''File_Attachment_4_6697_FK_tblSketch'', 4248, 6697, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Label_Info'', N''Final_View'', N''PLM_DW_Tab_Label_Info_4248'', N''Final_View_6698_FK_tblSketch'', 4248, 6698, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Label_Info'', N''Measurements'', N''PLM_DW_Tab_Label_Info_4248'', N''Measurements_6699_FK_tblSketch'', 4248, 6699, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Label_Info'', N''Position'', N''PLM_DW_Tab_Label_Info_4248'', N''Position_6700_FK_tblSketch'', 4248, 6700, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Label_Info'', N''Quality_Reference'', N''PLM_DW_Tab_Label_Info_4248'', N''Quality_Reference_6701_FK_tblSketch'', 4248, 6701, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Label_Info'', N''Security_Group'', N''PLM_DW_Tab_Label_Info_4248'', N''Security_Group_6702_FK_pdmSecurityUserGroup'', 4248, 6702, NULL, NULL, NULL, NULL, N''pdmSecurityUserGroup'', N''TabField'', 1, 96, N''int''),
        (N''@P@Label_Info'', N''Testing_Technician'', N''PLM_DW_Tab_Label_Info_4248'', N''Testing_Technician_6703_FK_pdmsecuritywebuser'', 4248, 6703, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField'', 1, 80, N''int''),
        (N''@P@Label_Info'', N''Image_Content_6734'', N''PLM_DW_Tab_Label_Info_4248'', N''Image_Content_6734_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4248, 6734, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Label_Info'', N''Image_Content_6735'', N''PLM_DW_Tab_Label_Info_4248'', N''Image_Content_6735_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4248, 6735, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Label_Info'', N''Image_Content_6736'', N''PLM_DW_Tab_Label_Info_4248'', N''Image_Content_6736_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4248, 6736, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Label_Info'', N''Image_Content_6737'', N''PLM_DW_Tab_Label_Info_4248'', N''Image_Content_6737_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4248, 6737, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Label_Info'', N''Image_6951'', N''PLM_DW_Tab_Label_Info_4248'', N''Image_6951_FK_tblSketch'', 4248, 6951, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Label_Info'', N''Description_6952'', N''PLM_DW_Tab_Label_Info_4248'', N''Description_6952_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4248, 6952, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Label_Info'', N''Description_6953'', N''PLM_DW_Tab_Label_Info_4248'', N''Description_6953_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4248, 6953, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Label_Info'', N''Image_6954'', N''PLM_DW_Tab_Label_Info_4248'', N''Image_6954_FK_tblSketch'', 4248, 6954, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Label_Info'', N''Description_6955'', N''PLM_DW_Tab_Label_Info_4248'', N''Description_6955_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4248, 6955, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Label_Info'', N''Image_6956'', N''PLM_DW_Tab_Label_Info_4248'', N''Image_6956_FK_tblSketch'', 4248, 6956, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Label_Info'', N''Description_6957'', N''PLM_DW_Tab_Label_Info_4248'', N''Description_6957_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4248, 6957, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Label_Info'', N''Image_6958'', N''PLM_DW_Tab_Label_Info_4248'', N''Image_6958_FK_tblSketch'', 4248, 6958, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Label_Info'', N''Created_by'', N''PLM_DW_Tab_Label_Info_4248'', N''Created_by_7111_FK_PLM_DW_UD_CreatedByUsers_4778'', 4248, 7111, NULL, NULL, NULL, NULL, N''PLM_DW_UD_CreatedByUsers_4778'', N''TabField'', 1, 4778, N''int''),
        (N''@P@Label_Info'', N''Per'', N''PLM_DW_Tab_Label_Info_4248'', N''Per_7136_FK_tblUnitOfMeasure'', 4248, 7136, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField'', 1, 67, N''int''),
        (N''@P@SizeRunDetailGrid'', N''SizeRunDetail'', N''PLM_DW_Grid_SizeRunDetailGrid_26'', N''SizeRunDetail_642_FK_tblSizeRunRotate'', 4247, 642, 191, 26, 642, NULL, N''tblSizeRunRotate'', N''GridColumn'', 1, 63, N''int''),
        (N''@P@DimensionDetailGrid'', N''DimensionDetail'', N''PLM_DW_Grid_DimensionDetailGrid_27'', N''DimensionDetail_643_FK_tblDimensionDetail'', 4247, 643, 192, 27, 643, NULL, N''tblDimensionDetail'', N''GridColumn'', 1, 73, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Active'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Active_7976'', 4248, 7976, 35, 7, 7976, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''SketchId'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SketchId_468_FK_tblSketch'', 4248, 468, 35, 7, 468, NULL, N''tblSketch'', N''GridColumn'', 5, 11, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ExternalImageLink'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ExternalImageLink_772'', 4248, 772, 35, 7, 772, NULL, NULL, N''GridColumn'', 38, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''SS'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SS_6633_FK_PLM_DW_UD_In_Season_3662'', 4248, 6633, 35, 7, 6633, NULL, N''PLM_DW_UD_In_Season_3662'', N''GridColumn'', 1, 3662, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Swatch'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Swatch_325'', 4248, 325, 35, 7, 325, NULL, NULL, N''GridColumn'', 24, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Name'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Name_372'', 4248, 372, 35, 7, 372, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ColorFamilyID'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ColorFamilyID_472_FK_pdmRGBColorFamily'', 4248, 472, 35, 7, 472, NULL, N''pdmRGBColorFamily'', N''GridColumn'', 1, 122, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Color'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_44_FK_pdmRGBColor'', 4248, 44, 35, 7, 44, NULL, N''pdmRGBColor'', N''GridColumn'', 1, 79, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Color_Code'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Code_635'', 4248, 635, 35, 7, 635, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Color_Combo'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Combo_7755'', 4248, 7755, 35, 7, 7755, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''SMS'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SMS_7717_FK_PLM_DW_UD_Rallye_Status_3667'', 4248, 7717, 35, 7, 7717, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 13, 3667, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Reference'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Reference_7583'', 4248, 7583, 35, 7, 7583, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Comments'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Comments_7584'', 4248, 7584, 35, 7, 7584, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Description'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Description_45'', 4248, 45, 35, 7, 45, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''RGB'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''RGB_324'', 4248, 324, 35, 7, 324, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ColorReferenceTypeID'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ColorReferenceTypeID_469_FK_pdmRGBColorReferenceType'', 4248, 469, 35, 7, 469, NULL, N''pdmRGBColorReferenceType'', N''GridColumn'', 1, 121, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ColorFolderPath'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ColorFolderPath_769_FK_pdmSEFolder'', 4248, 769, 35, 7, 769, NULL, N''pdmSEFolder'', N''GridColumn'', 1, 187, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ReferenceCode'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ReferenceCode_470'', 4248, 470, 35, 7, 470, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ReferenceName'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ReferenceName_471'', 4248, 471, 35, 7, 471, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''NRF'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''NRF_371_FK_tblColorNRF'', 4248, 371, 35, 7, 371, NULL, N''tblColorNRF'', N''GridColumn'', 1, 175, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ProductColorNRF'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ProductColorNRF_653_FK_tblColorNRF'', 4248, 653, 35, 7, 653, NULL, N''tblColorNRF'', N''GridColumn'', 1, 175, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Approved'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Approved_275'', 4248, 275, 35, 7, 275, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Approv_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Approv_Date_71'', 4248, 71, 35, 7, 71, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Image'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Image_274_FK_tblSketch'', 4248, 274, 35, 7, 274, NULL, N''tblSketch'', N''GridColumn'', 5, 11, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine1'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine1_305_FK_PLM_DW_UD_Color_Type_3525'', 4248, 305, 35, 7, 305, NULL, N''PLM_DW_UD_Color_Type_3525'', N''GridColumn'', 1, 3525, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine2'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine2_306_FK_PLM_DW_UD_Colourway_no_3526'', 4248, 306, 35, 7, 306, NULL, N''PLM_DW_UD_Colourway_no_3526'', N''GridColumn'', 1, 3526, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine3'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine3_307'', 4248, 307, 35, 7, 307, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine4'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine4_308'', 4248, 308, 35, 7, 308, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine5'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine5_309'', 4248, 309, 35, 7, 309, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine6'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine6_310'', 4248, 310, 35, 7, 310, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine7'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine7_311'', 4248, 311, 35, 7, 311, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine8'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine8_312'', 4248, 312, 35, 7, 312, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine9'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine9_313'', 4248, 313, 35, 7, 313, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine10'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine10_314'', 4248, 314, 35, 7, 314, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine11'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine11_315'', 4248, 315, 35, 7, 315, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine12'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine12_316'', 4248, 316, 35, 7, 316, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine13'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine13_317'', 4248, 317, 35, 7, 317, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine14'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine14_318'', 4248, 318, 35, 7, 318, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine15'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine15_319'', 4248, 319, 35, 7, 319, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''First_Cost'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''First_Cost_473'', 4248, 473, 35, 7, 473, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''FirstCostCurrency'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''FirstCostCurrency_474_FK_tblCurrency'', 4248, 474, 35, 7, 474, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Standard_Cost'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Standard_Cost_475'', 4248, 475, 35, 7, 475, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Selling_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Selling_Price_476'', 4248, 476, 35, 7, 476, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Selling_Currency'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Selling_Currency_636_FK_tblCurrency'', 4248, 636, 35, 7, 636, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Retail'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Retail_477'', 4248, 477, 35, 7, 477, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Effective_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Effective_Date_641'', 4248, 641, 35, 7, 641, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''SupplierColor'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SupplierColor_640'', 4248, 640, 35, 7, 640, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ERPID'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ERPID_655'', 4248, 655, 35, 7, 655, NULL, NULL, N''GridColumn'', 15, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Colour_Risk'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Colour_Risk_6634_FK_PLM_DW_UD_Yes_No_3494'', 4248, 6634, 35, 7, 6634, NULL, N''PLM_DW_UD_Yes_No_3494'', N''GridColumn'', 1, 3494, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Colour_Risk_Comment'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Colour_Risk_Comment_6635_FK_PLM_DW_UD_Colour_Risk_Comment_3663'', 4248, 6635, 35, 7, 6635, NULL, N''PLM_DW_UD_Colour_Risk_Comment_3663'', N''GridColumn'', 1, 3663, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Date_SMS'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Date_SMS_7761'', 4248, 7761, 35, 7, 7761, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Bulk_1_Status'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Bulk_1_Status_6636_FK_PLM_DW_UD_Rallye_Status_3667'', 4248, 6636, 35, 7, 6636, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 1, 3667, N''int''),
        (N''@P@ProductDesignColorGrid'', N''DateBulk1'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''DateBulk1_7756'', 4248, 7756, 35, 7, 7756, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Bulk_2_Status'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Bulk_2_Status_7759_FK_PLM_DW_UD_Rallye_Status_3667'', 4248, 7759, 35, 7, 7759, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 1, 3667, N''int''),
        (N''@P@ProductDesignColorGrid'', N''DateBulk2'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''DateBulk2_7757'', 4248, 7757, 35, 7, 7757, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Bulk_3_Status'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Bulk_3_Status_7760_FK_PLM_DW_UD_Rallye_Status_3667'', 4248, 7760, 35, 7, 7760, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 1, 3667, N''int''),
        (N''@P@ProductDesignColorGrid'', N''DateBulk3'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''DateBulk3_7758'', 4248, 7758, 35, 7, 7758, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''CAN_Qty_Color'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_Qty_Color_7631'', 4248, 7631, 35, 7, 7631, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAN_Qty_Total'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_Qty_Total_7640'', 4248, 7640, 35, 7, 7640, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USA_Qty_Color'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_Qty_Color_7632'', 4248, 7632, 35, 7, 7632, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USA_Qty_Total'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_Qty_Total_7641'', 4248, 7641, 35, 7, 7641, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''FOB_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''FOB_Price_7633'', 4248, 7633, 35, 7, 7633, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAD'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_8042_FK_tblCurrency'', 4248, 8042, 35, 7, 8042, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''CAD_WS_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_WS_Price_8043'', 4248, 8043, 35, 7, 8043, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAD_RET_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_RET_Price_8044'', 4248, 8044, 35, 7, 8044, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAD_VIP_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_VIP_Price_8049'', 4248, 8049, 35, 7, 8049, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USD'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_8045_FK_tblCurrency'', 4248, 8045, 35, 7, 8045, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''USD_WS_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_WS_Price_8046'', 4248, 8046, 35, 7, 8046, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USD_RET_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_RET_Price_8047'', 4248, 8047, 35, 7, 8047, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USD_VIP_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_VIP_Price_8050'', 4248, 8050, 35, 7, 8050, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAN_PO_s'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_PO_s_7642'', 4248, 7642, 35, 7, 7642, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''CAN_PO_Created_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_PO_Created_Date_7646'', 4248, 7646, 35, 7, 7646, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''CAN_ETA'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_ETA_7643'', 4248, 7643, 35, 7, 7643, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''USA_PO_s'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_PO_s_7644'', 4248, 7644, 35, 7, 7644, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''USA_PO_Created_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_PO_Created_Date_7647'', 4248, 7647, 35, 7, 7647, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''USA_ETA'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_ETA_7645'', 4248, 7645, 35, 7, 7645, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Fabric_Price_p_m'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Fabric_Price_p_m_7969'', 4248, 7969, 35, 7, 7969, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Fabric_Price_p_y'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Fabric_Price_p_y_7970'', 4248, 7970, 35, 7, 7970, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Color_Price_p_m'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Price_p_m_7966'', 4248, 7966, 35, 7, 7966, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Color_Price_p_y'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Price_p_y_7968'', 4248, 7968, 35, 7, 7968, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''GS1_Color_Group'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''GS1_Color_Group_770_FK_pdmGS1Color'', 4248, 770, 35, 7, 770, NULL, N''pdmGS1Color'', N''GridColumn'', 1, 190, N''int''),
        (N''@P@ProductDesignColorGrid'', N''GS1_Shade_Group'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''GS1_Shade_Group_771_FK_pdmGS1ColorShade'', 4248, 771, 35, 7, 771, NULL, N''pdmGS1ColorShade'', N''GridColumn'', 1, 192, N''int''),
        (N''@P@MaterialCosting_reg'', N''ID'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''ID_7067_FK_PLM_DW_UD_ID_3460'', 4248, 7067, 5193, 3144, 7067, NULL, N''PLM_DW_UD_ID_3460'', N''GridColumn'', 1, 3460, N''int''),
        (N''@P@MaterialCosting_reg'', N''Reference_Image'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Reference_Image_6795_FK_tblSketch'', 4248, 6795, 5193, 3144, 6795, NULL, N''tblSketch'', N''GridColumn'', 5, 11, N''int''),
        (N''@P@MaterialCosting_reg'', N''Season'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Season_6796_FK_tblSellingPeriod'', 4248, 6796, 5193, 3144, 6796, NULL, N''tblSellingPeriod'', N''GridColumn'', 1, 8, N''int''),
        (N''@P@MaterialCosting_reg'', N''Color'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Color_6982_FK_pdmRGBColor'', 4248, 6982, 5193, 3144, 6982, NULL, N''pdmRGBColor'', N''GridColumn'', 1, 79, N''int''),
        (N''@P@MaterialCosting_reg'', N''Supplier'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Supplier_6797_FK_PLM_DW_UD_Trims_Supplier_4757'', 4248, 6797, 5193, 3144, 6797, NULL, N''PLM_DW_UD_Trims_Supplier_4757'', N''GridColumn'', 1, 4757, N''int''),
        (N''@P@MaterialCosting_reg'', N''Cost'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Cost_6798'', 4248, 6798, 5193, 3144, 6798, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@MaterialCosting_reg'', N''Currency'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Currency_6800_FK_tblCurrency'', 4248, 6800, 5193, 3144, 6800, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@MaterialCosting_reg'', N''Per'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Per_8058'', 4248, 8058, 5193, 3144, 8058, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@MaterialCosting_reg'', N''UOM'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''UOM_6799_FK_tblUnitOfMeasure'', 4248, 6799, 5193, 3144, 6799, NULL, N''tblUnitOfMeasure'', N''GridColumn'', 1, 67, N''int''),
        (N''@P@MaterialCosting_reg'', N''Rate'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Rate_6801'', 4248, 6801, 5193, 3144, 6801, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@MaterialCosting_reg'', N''Converted_Cost'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Converted_Cost_6802'', 4248, 6802, 5193, 3144, 6802, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@MaterialCosting_reg'', N''Converted_Currency'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Converted_Currency_6803_FK_tblCurrency'', 4248, 6803, 5193, 3144, 6803, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@MaterialCosting_reg'', N''Cost_Terms'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Cost_Terms_6804_FK_PLM_DW_UD_Terms_3485'', 4248, 6804, 5193, 3144, 6804, NULL, N''PLM_DW_UD_Terms_3485'', N''GridColumn'', 1, 3485, N''int''),
        (N''@P@MaterialCosting_reg'', N''Payment_Method'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Payment_Method_6805_FK_PLM_DW_UD_Payment_Method_3679'', 4248, 6805, 5193, 3144, 6805, NULL, N''PLM_DW_UD_Payment_Method_3679'', N''GridColumn'', 1, 3679, N''int''),
        (N''@P@MaterialCosting_reg'', N''MOQ'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''MOQ_6806'', 4248, 6806, 5193, 3144, 6806, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@MaterialCosting_reg'', N''Comments'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Comments_6807'', 4248, 6807, 5193, 3144, 6807, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@MaterialCosting_reg'', N''Comment_BY'', N''PLM_DW_Grid_MaterialCosting_reg_3144'', N''Comment_BY_6808_FK_PLM_DW_UD_AllUsers_4777'', 4248, 6808, 5193, 3144, 6808, NULL, N''PLM_DW_UD_AllUsers_4777'', N''GridColumn'', 1, 4777, N''int''),
        (N''@P@Trims_Tracking'', N''Trim'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Trim__7695'', NULL, 7695, 7027, 3179, 7695, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@Trims_Tracking'', N''Description'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Description_7696'', NULL, 7696, 7027, 3179, 7696, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@Trims_Tracking'', N''Combo_Color'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Combo_Color_7605_FK_pdmRGBColor'', NULL, 7605, 7027, 3179, 7605, NULL, N''pdmRGBColor'', N''GridColumn'', 1, 79, N''int''),
        (N''@P@Trims_Tracking'', N''Season'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Season_7607_FK_PLM_DW_UD_In_Season_3662'', NULL, 7607, 7027, 3179, 7607, NULL, N''PLM_DW_UD_In_Season_3662'', N''GridColumn'', 1, 3662, N''int''),
        (N''@P@Trims_Tracking'', N''Type'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Type_7724_FK_PLM_DW_UD_Trim_Request_Type_4788'', NULL, 7724, 7027, 3179, 7724, NULL, N''PLM_DW_UD_Trim_Request_Type_4788'', N''GridColumn'', 1, 4788, N''int''),
        (N''@P@Trims_Tracking'', N''Status'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Status_7612_FK_PLM_DW_UD_Trims_Tracking_Status_4771'', NULL, 7612, 7027, 3179, 7612, NULL, N''PLM_DW_UD_Trims_Tracking_Status_4771'', N''GridColumn'', 1, 4771, N''int''),
        (N''@P@Trims_Tracking'', N''Date'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Date_7606'', NULL, 7606, 7027, 3179, 7606, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@Trims_Tracking'', N''Designer'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Designer_7725_FK_PLM_DW_UD_DesignerUsers_4772'', NULL, 7725, 7027, 3179, 7725, NULL, N''PLM_DW_UD_DesignerUsers_4772'', N''GridColumn'', 1, 4772, N''int''),
        (N''@P@Trims_Tracking'', N''Comment'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Comment_7613'', NULL, 7613, 7027, 3179, 7613, NULL, NULL, N''GridColumn'', 4, NULL, N''nvarchar''),
        (N''@P@Trims_Tracking'', N''Creation'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Creation_7617'', NULL, 7617, 7027, 3179, 7617, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@Trims_Tracking'', N''ID'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''ID_7621_FK_PLM_DW_UD_1_to_8_3482'', NULL, 7621, 7027, 3179, 7621, NULL, N''PLM_DW_UD_1_to_8_3482'', N''GridColumn'', 1, 3482, N''int''),
        (N''@P@Trims_Tracking'', N''ColorCode_txt'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''ColorCode_txt_7652'', NULL, 7652, 7027, 3179, 7652, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@Trims_Tracking'', N''Season_txt'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Season_txt_7656'', NULL, 7656, 7027, 3179, 7656, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@Trims_Tracking'', N''Status_txt'', N''PLM_DW_Grid_Trims_Tracking_3179'', N''Status_txt_7657'', NULL, 7657, 7027, 3179, 7657, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ReferenceBasicInfo'', N''ReferenceCode'', N''PLM_DW_Tab_Label_Header_4247'', N''Article__22'', 4247, 22, NULL, NULL, NULL, NULL, NULL, N''ReferenceField'', 2, NULL, N''nvarchar'')
';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;
GO

