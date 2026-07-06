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
END

SET @sql = N'DELETE FROM dbo.' + QUOTENAME(@MappingTable)
    + N' WHERE [AppTableName] IN (N''@P@ReferenceBasicInfo'', N''@P@Artwork_Details'', N''@P@ProductDesignColorGrid'');';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;

SET @sql = N'
INSERT INTO dbo.' + QUOTENAME(@MappingTable) + N' (
    [AppTableName],[AppColumnName],[DwTableName],[DwColumnName],
    [PlmTabId],[PlmSubItemId],[PlmGridSubItemId],[PlmGridId],[PlmMetaColumnId],
    [PlmBlockId],[DwFkTarget],[FieldKind],[PlmControlType],[PlmEntityId],[DwDataType]
)
VALUES
        (N''@P@Artwork_Details'', N''Classification'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Classification_1_FK_tblProductClass'', 4228, 1, NULL, NULL, NULL, NULL, N''tblProductClass'', N''TabField'', 1, 1, N''int''),
        (N''@P@Artwork_Details'', N''Product_Type'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Product_Type_2_FK_tblProductType'', 4228, 2, NULL, NULL, NULL, NULL, N''tblProductType'', N''TabField'', 1, 4, N''int''),
        (N''@P@Artwork_Details'', N''Season'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Season_3_FK_tblSellingPeriod'', 4228, 3, NULL, NULL, NULL, NULL, N''tblSellingPeriod'', N''TabField'', 1, 8, N''int''),
        (N''@P@Artwork_Details'', N''Collection'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Collection_4_FK_tblCollection'', 4228, 4, NULL, NULL, NULL, NULL, N''tblCollection'', N''TabField'', 1, 6, N''int''),
        (N''@P@Artwork_Details'', N''Group'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Group_5_FK_tblGroup'', 4228, 5, NULL, NULL, NULL, NULL, N''tblGroup'', N''TabField'', 1, 7, N''int''),
        (N''@P@Artwork_Details'', N''Sketch'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Sketch_6_FK_tblSketch'', 4228, 6, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Artwork_Details'', N''Division_8'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Division_8_FK_tblCompanyDivision'', 4228, 8, NULL, NULL, NULL, NULL, N''tblCompanyDivision'', N''TabField'', 1, 5, N''int''),
        (N''@P@Artwork_Details'', N''Article'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Article__22'', 4228, 22, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Description_23'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Description_23'', 4228, 23, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Made_in_Country'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Made_in_Country_48_FK_tblCountry'', 4228, 48, NULL, NULL, NULL, NULL, N''tblCountry'', N''TabField'', 1, 59, N''int''),
        (N''@P@Artwork_Details'', N''Product_Manager'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Product_Manager_109_FK_pdmsecuritywebuser'', 4228, 109, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField'', 1, 80, N''int''),
        (N''@P@Artwork_Details'', N''Long_Description'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Long_Description_121'', 4228, 121, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Brand'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Brand_141_FK_tblLabel'', 4228, 141, NULL, NULL, NULL, NULL, N''tblLabel'', N''TabField'', 1, 93, N''int''),
        (N''@P@Artwork_Details'', N''ProductTypeGroup'', N''PLM_DW_Tab_Artwork_Details_4228'', N''ProductTypeGroup_149_FK_tblProductClassGroup'', 4228, 149, NULL, NULL, NULL, NULL, N''tblProductClassGroup'', N''TabField'', 1, 95, N''int''),
        (N''@P@Artwork_Details'', N''Division_186'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Division_186_FK_tblCompanyDivision'', 4228, 186, NULL, NULL, NULL, NULL, N''tblCompanyDivision'', N''TabField'', 1, 5, N''int''),
        (N''@P@Artwork_Details'', N''Created_By_189'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Created_By_189'', 4228, 189, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Last_Revised_By'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Last_Revised_By_190'', 4228, 190, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Fabric_Type'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Fabric_Type_3130_FK_PLM_DW_UD_Fabric_Type_3463'', 4228, 3130, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Fabric_Type_3463'', N''TabField'', 1, 3463, N''int''),
        (N''@P@Artwork_Details'', N''Security_Group_3153'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Security_Group_3153_FK_pdmSecurityUserGroup'', 4228, 3153, NULL, NULL, NULL, NULL, N''pdmSecurityUserGroup'', N''TabField'', 1, 96, N''int''),
        (N''@P@Artwork_Details'', N''Created_by_3154'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Created_by_3154_FK_pdmsecuritywebuser'', 4228, 3154, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField'', 1, 80, N''int''),
        (N''@P@Artwork_Details'', N''Sub_Type'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Sub_Type_3790_FK_PLM_DW_UD_Fabric_SubType_3509'', 4228, 3790, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Fabric_SubType_3509'', N''TabField'', 1, 3509, N''int''),
        (N''@P@Artwork_Details'', N''Product_Class'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Product_Class_5262_FK_tblProductClass'', 4228, 5262, NULL, NULL, NULL, NULL, N''tblProductClass'', N''TabField'', 1, 1, N''int''),
        (N''@P@Artwork_Details'', N''Class_Group'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Class_Group_5263_FK_tblProductClassGroup'', 4228, 5263, NULL, NULL, NULL, NULL, N''tblProductClassGroup'', N''TabField'', 1, 95, N''int''),
        (N''@P@Artwork_Details'', N''Artwork_sent_to_Vendor'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Artwork_sent_to_Vendor_5293_FK_PLM_DW_UD_Yes_No_3494'', 4228, 5293, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Yes_No_3494'', N''TabField'', 1, 3494, N''int''),
        (N''@P@Artwork_Details'', N''Artwork_sent_Date'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Artwork_sent_Date_5294'', 4228, 5294, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Artwork_Details'', N''Artwork_Status'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Artwork_Status_5295_FK_PLM_DW_UD_Artwork_Status_3699'', 4228, 5295, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Artwork_Status_3699'', N''TabField'', 1, 3699, N''int''),
        (N''@P@Artwork_Details'', N''Print_Direction'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Print_Direction_5296_FK_PLM_DW_UD_Print_Direction_3669'', 4228, 5296, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Print_Direction_3669'', N''TabField'', 1, 3669, N''int''),
        (N''@P@Artwork_Details'', N''Comments'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Comments_5297'', 4228, 5297, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Print_Theme'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Print_Theme_5314_FK_PLM_DW_UD_Print_Theme_3671'', 4228, 5314, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Print_Theme_3671'', N''TabField'', 1, 3671, N''int''),
        (N''@P@Artwork_Details'', N''Artwork_File'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Artwork_File_5320_FK_tblSketch'', 4228, 5320, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Artwork_Due_Date'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Artwork_Due_Date_5323'', 4228, 5323, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Artwork_Details'', N''Artwork_Approval_Date'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Artwork_Approval_Date_5324'', 4228, 5324, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Artwork_Details'', N''Recolour'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Recolour_5325_FK_PLM_DW_UD_Yes_No_3494'', 4228, 5325, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Yes_No_3494'', N''TabField'', 1, 3494, N''int''),
        (N''@P@Artwork_Details'', N''Recolour_From'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Recolour_From__5326'', 4228, 5326, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Height'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Height_5327'', 4228, 5327, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Artwork_Details'', N''UOM_5328'', N''PLM_DW_Tab_Artwork_Details_4228'', N''UOM_5328_FK_tblUnitOfMeasure'', 4228, 5328, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField'', 1, 67, N''int''),
        (N''@P@Artwork_Details'', N''Width'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Width_5329'', 4228, 5329, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Artwork_Details'', N''UOM_5330'', N''PLM_DW_Tab_Artwork_Details_4228'', N''UOM_5330_FK_tblUnitOfMeasure'', 4228, 5330, NULL, NULL, NULL, NULL, N''tblUnitOfMeasure'', N''TabField'', 1, 67, N''int''),
        (N''@P@Artwork_Details'', N''Artwork_Placement'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Artwork_Placement_5331_FK_PLM_DW_UD_Artwork_Placement_3706'', 4228, 5331, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Artwork_Placement_3706'', N''TabField'', 1, 3706, N''int''),
        (N''@P@Artwork_Details'', N''Exclusive'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Exclusive_5332_FK_PLM_DW_UD_Yes_No_3494'', 4228, 5332, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Yes_No_3494'', N''TabField'', 1, 3494, N''int''),
        (N''@P@Artwork_Details'', N''Item_Type'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Item_Type_6677_FK_pdmTechPackType'', 4228, 6677, NULL, NULL, NULL, NULL, N''pdmTechPackType'', N''TabField'', 1, 123, N''int''),
        (N''@P@Artwork_Details'', N''File_Attachment_1'', N''PLM_DW_Tab_Artwork_Details_4228'', N''File_Attachment_1_6694_FK_tblSketch'', 4228, 6694, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Artwork_Details'', N''File_Attachment_2'', N''PLM_DW_Tab_Artwork_Details_4228'', N''File_Attachment_2_6695_FK_tblSketch'', 4228, 6695, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Artwork_Details'', N''File_Attachment_3'', N''PLM_DW_Tab_Artwork_Details_4228'', N''File_Attachment_3_6696_FK_tblSketch'', 4228, 6696, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Artwork_Details'', N''File_Attachment_4'', N''PLM_DW_Tab_Artwork_Details_4228'', N''File_Attachment_4_6697_FK_tblSketch'', 4228, 6697, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Final_View'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Final_View_6698_FK_tblSketch'', 4228, 6698, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Artwork_Details'', N''Measurements'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Measurements_6699_FK_tblSketch'', 4228, 6699, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Artwork_Details'', N''Position'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Position_6700_FK_tblSketch'', 4228, 6700, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Artwork_Details'', N''Quality_Reference'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Quality_Reference_6701_FK_tblSketch'', 4228, 6701, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Artwork_Details'', N''Image_Content_6734'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Image_Content_6734_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4228, 6734, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Artwork_Details'', N''Image_Content_6735'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Image_Content_6735_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4228, 6735, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Artwork_Details'', N''Image_Content_6736'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Image_Content_6736_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4228, 6736, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Artwork_Details'', N''Image_Content_6737'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Image_Content_6737_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4228, 6737, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Artwork_Details'', N''French'', N''PLM_DW_Tab_Artwork_Details_4228'', N''French_6745'', 4228, 6745, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Process_Type_1'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Process_Type_1_6797_FK_PLM_DW_UD_Trim_print_type_4728'', 4228, 6797, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Trim_print_type_4728'', N''TabField'', 1, 4728, N''int''),
        (N''@P@Artwork_Details'', N''Process_Type_2'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Process_Type_2_6798_FK_PLM_DW_UD_Trim_print_type_4728'', 4228, 6798, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Trim_print_type_4728'', N''TabField'', 1, 4728, N''int''),
        (N''@P@Artwork_Details'', N''Process_Type_3'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Process_Type_3_6799_FK_PLM_DW_UD_Trim_print_type_4728'', 4228, 6799, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Trim_print_type_4728'', N''TabField'', 1, 4728, N''int''),
        (N''@P@Artwork_Details'', N''Image_6951'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Image_6951_FK_tblSketch'', 4228, 6951, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Artwork_Details'', N''Description_6952'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Description_6952_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4228, 6952, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Artwork_Details'', N''Description_6953'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Description_6953_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4228, 6953, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Artwork_Details'', N''Image_6954'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Image_6954_FK_tblSketch'', 4228, 6954, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Artwork_Details'', N''Description_6955'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Description_6955_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4228, 6955, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Artwork_Details'', N''Image_6956'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Image_6956_FK_tblSketch'', 4228, 6956, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Artwork_Details'', N''Description_6957'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Description_6957_FK_PLM_DW_UD_Raw_Material_Image_Content_4717'', 4228, 6957, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Raw_Material_Image_Content_4717'', N''TabField'', 1, 4717, N''int''),
        (N''@P@Artwork_Details'', N''Image_6958'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Image_6958_FK_tblSketch'', 4228, 6958, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Artwork_Details'', N''Security_Group_7022'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Security_Group_7022_FK_pdmSecurityUserGroup'', 4228, 7022, NULL, NULL, NULL, NULL, N''pdmSecurityUserGroup'', N''TabField'', 1, 96, N''int''),
        (N''@P@Artwork_Details'', N''Approved_by'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Approved_by_7023_FK_pdmsecuritywebuser'', 4228, 7023, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField'', 1, 80, N''int''),
        (N''@P@Artwork_Details'', N''Name'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Name_7028'', 4228, 7028, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Product_Type_txt'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Product_Type_txt_7030'', 4228, 7030, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''sketch_id'', N''PLM_DW_Tab_Artwork_Details_4228'', N''sketch_id_7043'', 4228, 7043, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''ddl'', N''PLM_DW_Tab_Artwork_Details_4228'', N''ddl_7045_FK_tblSketch'', 4228, 7045, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 1, 11, N''int''),
        (N''@P@Artwork_Details'', N''Collection_txt'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Collection_txt_7125'', 4228, 7125, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''ProductTypeGroup_txt'', N''PLM_DW_Tab_Artwork_Details_4228'', N''ProductTypeGroup_txt_7352'', 4228, 7352, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Artwork_Details'', N''Subcategory'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Subcategory_7361_FK_PLM_DW_UD_Product_Class_Subcategories_4798'', 4228, 7361, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Product_Class_Subcategories_4798'', N''TabField'', 1, 4798, N''int''),
        (N''@P@Artwork_Details'', N''ERP_Season'', N''PLM_DW_Tab_Artwork_Details_4228'', N''ERP_Season_7362_FK_tblSellingPeriod'', 4228, 7362, NULL, NULL, NULL, NULL, N''tblSellingPeriod'', N''TabField'', 1, 8, N''int''),
        (N''@P@Artwork_Details'', N''French_Name'', N''PLM_DW_Tab_Artwork_Details_4228'', N''French_Name_7366'', 4228, 7366, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Active'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Active_7976'', 4228, 7976, 35, 7, 7976, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''SketchId'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SketchId_468_FK_tblSketch'', 4228, 468, 35, 7, 468, NULL, N''tblSketch'', N''GridColumn'', 5, 11, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ExternalImageLink'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ExternalImageLink_772'', 4228, 772, 35, 7, 772, NULL, NULL, N''GridColumn'', 38, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''SS'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SS_6633_FK_PLM_DW_UD_In_Season_3662'', 4228, 6633, 35, 7, 6633, NULL, N''PLM_DW_UD_In_Season_3662'', N''GridColumn'', 1, 3662, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Swatch'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Swatch_325'', 4228, 325, 35, 7, 325, NULL, NULL, N''GridColumn'', 24, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Name'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Name_372'', 4228, 372, 35, 7, 372, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ColorFamilyID'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ColorFamilyID_472_FK_pdmRGBColorFamily'', 4228, 472, 35, 7, 472, NULL, N''pdmRGBColorFamily'', N''GridColumn'', 1, 122, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Color'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_44_FK_pdmRGBColor'', 4228, 44, 35, 7, 44, NULL, N''pdmRGBColor'', N''GridColumn'', 1, 79, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Color_Code'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Code_635'', 4228, 635, 35, 7, 635, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Color_Combo'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Combo_7755'', 4228, 7755, 35, 7, 7755, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''SMS'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SMS_7717_FK_PLM_DW_UD_Rallye_Status_3667'', 4228, 7717, 35, 7, 7717, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 13, 3667, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Reference'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Reference_7583'', 4228, 7583, 35, 7, 7583, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Comments'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Comments_7584'', 4228, 7584, 35, 7, 7584, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Description'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Description_45'', 4228, 45, 35, 7, 45, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''RGB'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''RGB_324'', 4228, 324, 35, 7, 324, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ColorReferenceTypeID'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ColorReferenceTypeID_469_FK_pdmRGBColorReferenceType'', 4228, 469, 35, 7, 469, NULL, N''pdmRGBColorReferenceType'', N''GridColumn'', 1, 121, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ColorFolderPath'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ColorFolderPath_769_FK_pdmSEFolder'', 4228, 769, 35, 7, 769, NULL, N''pdmSEFolder'', N''GridColumn'', 1, 187, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ReferenceCode'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ReferenceCode_470'', 4228, 470, 35, 7, 470, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ReferenceName'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ReferenceName_471'', 4228, 471, 35, 7, 471, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''NRF'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''NRF_371_FK_tblColorNRF'', 4228, 371, 35, 7, 371, NULL, N''tblColorNRF'', N''GridColumn'', 1, 175, N''int''),
        (N''@P@ProductDesignColorGrid'', N''ProductColorNRF'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ProductColorNRF_653_FK_tblColorNRF'', 4228, 653, 35, 7, 653, NULL, N''tblColorNRF'', N''GridColumn'', 1, 175, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Approved'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Approved_275'', 4228, 275, 35, 7, 275, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Approv_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Approv_Date_71'', 4228, 71, 35, 7, 71, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Image'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Image_274_FK_tblSketch'', 4228, 274, 35, 7, 274, NULL, N''tblSketch'', N''GridColumn'', 5, 11, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine1'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine1_305_FK_PLM_DW_UD_Color_Type_3525'', 4228, 305, 35, 7, 305, NULL, N''PLM_DW_UD_Color_Type_3525'', N''GridColumn'', 1, 3525, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine2'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine2_306_FK_PLM_DW_UD_Colourway_no_3526'', 4228, 306, 35, 7, 306, NULL, N''PLM_DW_UD_Colourway_no_3526'', N''GridColumn'', 1, 3526, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine3'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine3_307'', 4228, 307, 35, 7, 307, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine4'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine4_308'', 4228, 308, 35, 7, 308, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine5'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine5_309'', 4228, 309, 35, 7, 309, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine6'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine6_310'', 4228, 310, 35, 7, 310, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine7'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine7_311'', 4228, 311, 35, 7, 311, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine8'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine8_312'', 4228, 312, 35, 7, 312, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine9'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine9_313'', 4228, 313, 35, 7, 313, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine10'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine10_314'', 4228, 314, 35, 7, 314, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine11'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine11_315'', 4228, 315, 35, 7, 315, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine12'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine12_316'', 4228, 316, 35, 7, 316, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine13'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine13_317'', 4228, 317, 35, 7, 317, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine14'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine14_318'', 4228, 318, 35, 7, 318, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Userdefine15'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Userdefine15_319'', 4228, 319, 35, 7, 319, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''First_Cost'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''First_Cost_473'', 4228, 473, 35, 7, 473, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''FirstCostCurrency'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''FirstCostCurrency_474_FK_tblCurrency'', 4228, 474, 35, 7, 474, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Standard_Cost'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Standard_Cost_475'', 4228, 475, 35, 7, 475, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Selling_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Selling_Price_476'', 4228, 476, 35, 7, 476, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Selling_Currency'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Selling_Currency_636_FK_tblCurrency'', 4228, 636, 35, 7, 636, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Retail'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Retail_477'', 4228, 477, 35, 7, 477, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Effective_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Effective_Date_641'', 4228, 641, 35, 7, 641, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''SupplierColor'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''SupplierColor_640'', 4228, 640, 35, 7, 640, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''ERPID'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''ERPID_655'', 4228, 655, 35, 7, 655, NULL, NULL, N''GridColumn'', 15, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Colour_Risk'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Colour_Risk_6634_FK_PLM_DW_UD_Yes_No_3494'', 4228, 6634, 35, 7, 6634, NULL, N''PLM_DW_UD_Yes_No_3494'', N''GridColumn'', 1, 3494, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Colour_Risk_Comment'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Colour_Risk_Comment_6635_FK_PLM_DW_UD_Colour_Risk_Comment_3663'', 4228, 6635, 35, 7, 6635, NULL, N''PLM_DW_UD_Colour_Risk_Comment_3663'', N''GridColumn'', 1, 3663, N''int''),
        (N''@P@ProductDesignColorGrid'', N''Date_SMS'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Date_SMS_7761'', 4228, 7761, 35, 7, 7761, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Bulk_1_Status'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Bulk_1_Status_6636_FK_PLM_DW_UD_Rallye_Status_3667'', 4228, 6636, 35, 7, 6636, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 1, 3667, N''int''),
        (N''@P@ProductDesignColorGrid'', N''DateBulk1'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''DateBulk1_7756'', 4228, 7756, 35, 7, 7756, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Bulk_2_Status'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Bulk_2_Status_7759_FK_PLM_DW_UD_Rallye_Status_3667'', 4228, 7759, 35, 7, 7759, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 1, 3667, N''int''),
        (N''@P@ProductDesignColorGrid'', N''DateBulk2'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''DateBulk2_7757'', 4228, 7757, 35, 7, 7757, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''Bulk_3_Status'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Bulk_3_Status_7760_FK_PLM_DW_UD_Rallye_Status_3667'', 4228, 7760, 35, 7, 7760, NULL, N''PLM_DW_UD_Rallye_Status_3667'', N''GridColumn'', 1, 3667, N''int''),
        (N''@P@ProductDesignColorGrid'', N''DateBulk3'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''DateBulk3_7758'', 4228, 7758, 35, 7, 7758, NULL, NULL, N''GridColumn'', 7, NULL, N''datetime''),
        (N''@P@ProductDesignColorGrid'', N''CAN_Qty_Color'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_Qty_Color_7631'', 4228, 7631, 35, 7, 7631, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAN_Qty_Total'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_Qty_Total_7640'', 4228, 7640, 35, 7, 7640, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USA_Qty_Color'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_Qty_Color_7632'', 4228, 7632, 35, 7, 7632, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USA_Qty_Total'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_Qty_Total_7641'', 4228, 7641, 35, 7, 7641, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''FOB_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''FOB_Price_7633'', 4228, 7633, 35, 7, 7633, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAD'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_8042_FK_tblCurrency'', 4228, 8042, 35, 7, 8042, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''CAD_WS_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_WS_Price_8043'', 4228, 8043, 35, 7, 8043, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAD_RET_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_RET_Price_8044'', 4228, 8044, 35, 7, 8044, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAD_VIP_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAD_VIP_Price_8049'', 4228, 8049, 35, 7, 8049, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USD'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_8045_FK_tblCurrency'', 4228, 8045, 35, 7, 8045, NULL, N''tblCurrency'', N''GridColumn'', 1, 68, N''int''),
        (N''@P@ProductDesignColorGrid'', N''USD_WS_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_WS_Price_8046'', 4228, 8046, 35, 7, 8046, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USD_RET_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_RET_Price_8047'', 4228, 8047, 35, 7, 8047, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''USD_VIP_Price'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USD_VIP_Price_8050'', 4228, 8050, 35, 7, 8050, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''CAN_PO_s'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_PO_s_7642'', 4228, 7642, 35, 7, 7642, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''CAN_PO_Created_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_PO_Created_Date_7646'', 4228, 7646, 35, 7, 7646, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''CAN_ETA'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''CAN_ETA_7643'', 4228, 7643, 35, 7, 7643, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''USA_PO_s'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_PO_s_7644'', 4228, 7644, 35, 7, 7644, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''USA_PO_Created_Date'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_PO_Created_Date_7647'', 4228, 7647, 35, 7, 7647, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''USA_ETA'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''USA_ETA_7645'', 4228, 7645, 35, 7, 7645, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ProductDesignColorGrid'', N''Fabric_Price_p_m'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Fabric_Price_p_m_7969'', 4228, 7969, 35, 7, 7969, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Fabric_Price_p_y'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Fabric_Price_p_y_7970'', 4228, 7970, 35, 7, 7970, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Color_Price_p_m'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Price_p_m_7966'', 4228, 7966, 35, 7, 7966, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''Color_Price_p_y'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''Color_Price_p_y_7968'', 4228, 7968, 35, 7, 7968, NULL, NULL, N''GridColumn'', 20, NULL, N''float''),
        (N''@P@ProductDesignColorGrid'', N''GS1_Color_Group'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''GS1_Color_Group_770_FK_pdmGS1Color'', 4228, 770, 35, 7, 770, NULL, N''pdmGS1Color'', N''GridColumn'', 1, 190, N''int''),
        (N''@P@ProductDesignColorGrid'', N''GS1_Shade_Group'', N''PLM_DW_Grid_ProductDesignColorGrid_7'', N''GS1_Shade_Group_771_FK_pdmGS1ColorShade'', 4228, 771, 35, 7, 771, NULL, N''pdmGS1ColorShade'', N''GridColumn'', 1, 192, N''int''),
        (N''@P@ReferenceBasicInfo'', N''ReferenceCode'', N''PLM_DW_Tab_Artwork_Details_4228'', N''Article__22'', 4228, 22, NULL, NULL, NULL, NULL, NULL, N''ReferenceField'', 2, NULL, N''nvarchar'')
';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;
GO

