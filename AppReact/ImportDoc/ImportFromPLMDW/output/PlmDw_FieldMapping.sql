-- =============================================================================
-- PLM DW â†’ APP field mapping (generated â€” see ImportFromPLMDW/PROMPT.md)
-- EXECUTION ORDER:
--   1. PlmDw_Tables.sql
--   2. PlmDw_FieldMapping.sql    (this file)
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
        [FieldKind]         NVARCHAR(16)  NOT NULL,
        CONSTRAINT [PK_FieldMapping] PRIMARY KEY CLUSTERED ([AppTableName], [AppColumnName])
    );';
    EXEC sp_executesql @sql;
END

SET @sql = N'DELETE FROM dbo.' + QUOTENAME(@MappingTable)
    + N' WHERE [AppTableName] IN (N''@P@ReferenceBasicInfo'', N''@P@Packaging_Header'', N''@P@Packaging_Info'');';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;

SET @sql = N'
INSERT INTO dbo.' + QUOTENAME(@MappingTable) + N' (
    [AppTableName],[AppColumnName],[DwTableName],[DwColumnName],
    [PlmTabId],[PlmSubItemId],[PlmGridSubItemId],[PlmGridId],[PlmMetaColumnId],
    [PlmBlockId],[DwFkTarget],[FieldKind]
)
VALUES
        (N''@P@Packaging_Header'', N''Classification'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Classification_1_FK_tblProductClass'', 4249, 1, NULL, NULL, NULL, NULL, N''tblProductClass'', N''TabField''),
        (N''@P@Packaging_Header'', N''Product_Type'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Product_Type_2_FK_tblProductType'', 4249, 2, NULL, NULL, NULL, NULL, N''tblProductType'', N''TabField''),
        (N''@P@Packaging_Header'', N''Season'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Season_3_FK_tblSellingPeriod'', 4249, 3, NULL, NULL, NULL, NULL, N''tblSellingPeriod'', N''TabField''),
        (N''@P@Packaging_Header'', N''Collection'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Collection_4_FK_tblCollection'', 4249, 4, NULL, NULL, NULL, NULL, N''tblCollection'', N''TabField''),
        (N''@P@Packaging_Header'', N''Group'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Group_5_FK_tblGroup'', 4249, 5, NULL, NULL, NULL, NULL, N''tblGroup'', N''TabField''),
        (N''@P@Packaging_Header'', N''Sketch'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Sketch_6_FK_tblSketch'', 4249, 6, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Header'', N''Division_8'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Division_8_FK_tblCompanyDivision'', 4249, 8, NULL, NULL, NULL, NULL, N''tblCompanyDivision'', N''TabField''),
        (N''@P@Packaging_Header'', N''Size_Range'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Size_Range_10_FK_tblSizeRun'', 4249, 10, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField''),
        (N''@P@Packaging_Header'', N''Dimension'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Dimension_11_FK_tblDimension'', 4249, 11, NULL, NULL, NULL, NULL, N''tblDimension'', N''TabField''),
        (N''@P@Packaging_Header'', N''Article'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Article__22'', 4249, 22, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Description'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Description_23'', 4249, 23, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Country_Of_Origin'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Country_Of_Origin_103_FK_tblCountry'', 4249, 103, NULL, NULL, NULL, NULL, N''tblCountry'', N''TabField''),
        (N''@P@Packaging_Header'', N''Product_Manager'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Product_Manager_109_FK_pdmsecuritywebuser'', 4249, 109, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField''),
        (N''@P@Packaging_Header'', N''Long_Description'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Long_Description_121'', 4249, 121, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Sample_Size_Detail'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Sample_Size_Detail_139_FK_tblSizeRunRotate'', 4249, 139, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField''),
        (N''@P@Packaging_Header'', N''Brand'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Brand_141_FK_tblLabel'', 4249, 141, NULL, NULL, NULL, NULL, N''tblLabel'', N''TabField''),
        (N''@P@Packaging_Header'', N''ProductTypeGroup'', N''PLM_DW_Tab_Packaging_Header_4249'', N''ProductTypeGroup_149_FK_tblProductClassGroup'', 4249, 149, NULL, NULL, NULL, NULL, N''tblProductClassGroup'', N''TabField''),
        (N''@P@Packaging_Header'', N''Size_Detail_Dispaly'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Size_Detail_Dispaly_150_FK_tblSizeRunRotate'', 4249, 150, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField''),
        (N''@P@Packaging_Header'', N''Division_186'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Division_186_FK_tblCompanyDivision'', 4249, 186, NULL, NULL, NULL, NULL, N''tblCompanyDivision'', N''TabField''),
        (N''@P@Packaging_Header'', N''Created_By'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Created_By_189'', 4249, 189, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Last_Revised_By'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Last_Revised_By_190'', 4249, 190, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''ProductReferenceId'', N''PLM_DW_Tab_Packaging_Header_4249'', N''ProductReferenceId_197'', 4249, 197, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Raw_Material_Status'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Raw_Material_Status_3128_FK_PLM_DW_UD_Raw_Material_Status_3461'', 4249, 3128, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Status_3461'', N''TabField''),
        (N''@P@Packaging_Header'', N''State'', N''PLM_DW_Tab_Packaging_Header_4249'', N''State_3129'', 4249, 3129, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Item_Type'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Item_Type_3710_FK_pdmTechPackType'', 4249, 3710, NULL, NULL, NULL, NULL, N''pdmTechPackType'', N''TabField''),
        (N''@P@Packaging_Header'', N''Material'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Material_3811_FK_PLM_DW_UD_Packaging_Material_3522'', 4249, 3811, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Packaging_Material_3522'', N''TabField''),
        (N''@P@Packaging_Header'', N''Supplier_Article_Number'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Supplier_Article_Number_3916'', 4249, 3916, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Release_to_Manufacturer'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Release_to_Manufacturer_5095_FK_PLM_DW_UD_Yes_No_3494'', 4249, 5095, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Yes_No_3494'', N''TabField''),
        (N''@P@Packaging_Header'', N''Old_Code'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Old_Code_6684'', 4249, 6684, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Label_Reference'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Label_Reference_6685'', 4249, 6685, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Cost_1'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Cost_1_6688'', 4249, 6688, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''French'', N''PLM_DW_Tab_Packaging_Header_4249'', N''French_6745'', 4249, 6745, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Process_Type_1'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Process_Type_1_6800_FK_PLM_DW_UD_Packaging_print_type_4727'', 4249, 6800, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Packaging_print_type_4727'', N''TabField''),
        (N''@P@Packaging_Header'', N''Process_Type_2'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Process_Type_2_6806_FK_PLM_DW_UD_Packaging_print_type_4727'', 4249, 6806, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Packaging_print_type_4727'', N''TabField''),
        (N''@P@Packaging_Header'', N''Process_Type_3'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Process_Type_3_6807_FK_PLM_DW_UD_Packaging_print_type_4727'', 4249, 6807, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Packaging_print_type_4727'', N''TabField''),
        (N''@P@Packaging_Header'', N''Supplier_1'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Supplier_1_6885_FK_PLM_DW_UD_Trims_Supplier_4757'', 4249, 6885, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Trims_Supplier_4757'', N''TabField''),
        (N''@P@Packaging_Header'', N''Release_Day'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Release_Day_7024'', 4249, 7024, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Name'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Name_7028'', 4249, 7028, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Product_Type_txt'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Product_Type_txt_7030'', 4249, 7030, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''sketch_id'', N''PLM_DW_Tab_Packaging_Header_4249'', N''sketch_id_7043'', 4249, 7043, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''ddl'', N''PLM_DW_Tab_Packaging_Header_4249'', N''ddl_7045_FK_tblSketch'', 4249, 7045, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Header'', N''Collection_txt'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Collection_txt_7125'', 4249, 7125, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Supplier_2'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Supplier_2_7333_FK_PLM_DW_UD_Trims_Supplier_4757'', 4249, 7333, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Trims_Supplier_4757'', N''TabField''),
        (N''@P@Packaging_Header'', N''USD_for'', N''PLM_DW_Tab_Packaging_Header_4249'', N''USD_for_7339'', 4249, 7339, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''ProductTypeGroup_txt'', N''PLM_DW_Tab_Packaging_Header_4249'', N''ProductTypeGroup_txt_7352'', 4249, 7352, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Header'', N''Subcategory'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Subcategory_7361_FK_PLM_DW_UD_Product_Class_Subcategories_4798'', 4249, 7361, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Product_Class_Subcategories_4798'', N''TabField''),
        (N''@P@Packaging_Header'', N''ERP_Season'', N''PLM_DW_Tab_Packaging_Header_4249'', N''ERP_Season_7362_FK_tblSellingPeriod'', 4249, 7362, NULL, NULL, NULL, NULL, N''tblSellingPeriod'', N''TabField''),
        (N''@P@Packaging_Header'', N''French_Name'', N''PLM_DW_Tab_Packaging_Header_4249'', N''French_Name_7366'', 4249, 7366, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Info'', N''Construction'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Construction_3161_FK_PLM_DW_UD_Label_Construction_4724'', 4250, 3161, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Label_Construction_4724'', N''TabField''),
        (N''@P@Packaging_Info'', N''Width'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Width_4133'', 4250, 4133, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Info'', N''Width_Unit'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Width_Unit_4134_FK_tblUnitOfMeasure'', 4250, 4134, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField''),
        (N''@P@Packaging_Info'', N''Weight'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Weight_4135'', 4250, 4135, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Info'', N''Weight_Unit'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Weight_Unit_4136_FK_tblUnitOfMeasure'', 4250, 4136, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField''),
        (N''@P@Packaging_Info'', N''Length'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Length_4177'', 4250, 4177, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Info'', N''Length_Unit'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Length_Unit_4178_FK_tblUnitOfMeasure'', 4250, 4178, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField''),
        (N''@P@Packaging_Info'', N''Height'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Height_4179'', 4250, 4179, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Info'', N''Height_Unit'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Height_Unit_4180_FK_tblUnitOfMeasure'', 4250, 4180, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField''),
        (N''@P@Packaging_Info'', N''Lead_Time'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Lead_Time_6689'', 4250, 6689, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Info'', N''Lead_Time_UOM'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Lead_Time_UOM_6690_FK_PLM_DW_UD_LeadTime_3678'', 4250, 6690, NULL, NULL, NULL, NULL, N''PLM_DW_UD_LeadTime_3678'', N''TabField''),
        (N''@P@Packaging_Info'', N''Bulk_MOQ'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Bulk_MOQ_6691'', 4250, 6691, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Info'', N''Sample_MOQ'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Sample_MOQ_6692'', 4250, 6692, NULL, NULL, NULL, NULL, NULL, N''TabField''),
        (N''@P@Packaging_Info'', N''MOQ_UOM'', N''PLM_DW_Tab_Packaging_Info_4250'', N''MOQ_UOM_6693_FK_tblUnitOfMeasure'', 4250, 6693, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField''),
        (N''@P@Packaging_Info'', N''File_Attachment_1'', N''PLM_DW_Tab_Packaging_Info_4250'', N''File_Attachment_1_6694_FK_tblSketch'', 4250, 6694, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''File_Attachment_2'', N''PLM_DW_Tab_Packaging_Info_4250'', N''File_Attachment_2_6695_FK_tblSketch'', 4250, 6695, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''File_Attachment_3'', N''PLM_DW_Tab_Packaging_Info_4250'', N''File_Attachment_3_6696_FK_tblSketch'', 4250, 6696, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''File_Attachment_4'', N''PLM_DW_Tab_Packaging_Info_4250'', N''File_Attachment_4_6697_FK_tblSketch'', 4250, 6697, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''Final_View'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Final_View_6698_FK_tblSketch'', 4250, 6698, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''Measurements'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Measurements_6699_FK_tblSketch'', 4250, 6699, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''Position'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Position_6700_FK_tblSketch'', 4250, 6700, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''Quality_Reference'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Quality_Reference_6701_FK_tblSketch'', 4250, 6701, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''Security_Group_6702'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Security_Group_6702_FK_pdmSecurityUserGroup'', 4250, 6702, NULL, NULL, NULL, NULL, N''pdmSecurityUserGroup'', N''TabField''),
        (N''@P@Packaging_Info'', N''Testing_Technician'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Testing_Technician_6703_FK_pdmsecuritywebuser'', 4250, 6703, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField''),
        (N''@P@Packaging_Info'', N''Security_Group_6704'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Security_Group_6704_FK_pdmSecurityUserGroup'', 4250, 6704, NULL, NULL, NULL, NULL, N''pdmSecurityUserGroup'', N''TabField''),
        (N''@P@Packaging_Info'', N''Buyer'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Buyer_6705_FK_pdmsecuritywebuser'', 4250, 6705, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField''),
        (N''@P@Packaging_Info'', N''Image_Content_6734'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Image_Content_6734_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4250, 6734, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField''),
        (N''@P@Packaging_Info'', N''Image_Content_6735'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Image_Content_6735_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4250, 6735, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField''),
        (N''@P@Packaging_Info'', N''Image_Content_6736'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Image_Content_6736_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4250, 6736, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField''),
        (N''@P@Packaging_Info'', N''Image_Content_6737'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Image_Content_6737_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4250, 6737, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField''),
        (N''@P@Packaging_Info'', N''Image_6951'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Image_6951_FK_tblSketch'', 4250, 6951, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''Description_6952'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Description_6952_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4250, 6952, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField''),
        (N''@P@Packaging_Info'', N''Description_6953'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Description_6953_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4250, 6953, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField''),
        (N''@P@Packaging_Info'', N''Image_6954'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Image_6954_FK_tblSketch'', 4250, 6954, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''Description_6955'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Description_6955_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4250, 6955, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField''),
        (N''@P@Packaging_Info'', N''Image_6956'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Image_6956_FK_tblSketch'', 4250, 6956, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''Description_6957'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Description_6957_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4250, 6957, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField''),
        (N''@P@Packaging_Info'', N''Image_6958'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Image_6958_FK_tblSketch'', 4250, 6958, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField''),
        (N''@P@Packaging_Info'', N''Created_by'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Created_by_7111_FK_PLM_DW_UD_CreatedByUsers_4778'', 4250, 7111, NULL, NULL, NULL, NULL, N''PLM_DW_UD_CreatedByUsers_4778'', N''TabField''),
        (N''@P@Packaging_Info'', N''Per_7135'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Per_7135_FK_tblUnitOfMeasure'', 4250, 7135, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField''),
        (N''@P@Packaging_Info'', N''Per_7136'', N''PLM_DW_Tab_Packaging_Info_4250'', N''Per_7136_FK_tblUnitOfMeasure'', 4250, 7136, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField''),
        (N''@P@ReferenceBasicInfo'', N''ReferenceCode'', N''PLM_DW_Tab_Packaging_Header_4249'', N''Article__22'', 4249, 22, NULL, NULL, NULL, NULL, NULL, N''ReferenceField'')
';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;
GO

