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
    + N' WHERE [AppTableName] IN (N''@P@ReferenceBasicInfo'', N''@P@Grading'', N''@P@Proto_Summary'', N''@P@Proto_1'', N''@P@Proto_2'', N''@P@Proto_3'', N''@P@Sale_Sample'', N''@P@Fit_1'', N''@P@Fit_2'', N''@P@SpecGradingGrid'', N''@P@SpecFitGrid'');';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;

-- FieldMapping INSERT batch 1 (281 row(s))
SET @sql = N'
INSERT INTO dbo.' + QUOTENAME(@MappingTable) + N' (
    [AppTableName],[AppColumnName],[DwTableName],[DwColumnName],
    [PlmTabId],[PlmSubItemId],[PlmGridSubItemId],[PlmGridId],[PlmMetaColumnId],
    [PlmBlockId],[DwFkTarget],[FieldKind],[PlmControlType],[PlmEntityId],[DwDataType]
)
VALUES
        (N''@P@Grading'', N''Size_Run'', N''PLM_DW_Tab_Grading_4006'', N''Size_Run_43_FK_tblSizeRun'', 4006, 43, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Grading'', N''Base_Size'', N''PLM_DW_Tab_Grading_4006'', N''Base_Size_44_FK_tblSizeRunRotate'', 4006, 44, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Grading'', N''Spec_Selected_Size'', N''PLM_DW_Tab_Grading_4006'', N''Spec_Selected_Size_45'', 4006, 45, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Grading'', N''Measure_Unit'', N''PLM_DW_Tab_Grading_4006'', N''Measure_Unit_58_FK_'', 4006, 58, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 160, N''int''),
        (N''@P@Grading'', N''POM_Template'', N''PLM_DW_Tab_Grading_4006'', N''POM_Template_196_FK_pdmv2kBodyType'', 4006, 196, NULL, NULL, NULL, NULL, N''pdmv2kBodyType'', N''TabField'', 1, 36, N''int''),
        (N''@P@Grading'', N''Security_Group'', N''PLM_DW_Tab_Grading_4006'', N''Security_Group_3167_FK_pdmSecurityUserGroup'', 4006, 3167, NULL, NULL, NULL, NULL, N''pdmSecurityUserGroup'', N''TabField'', 1, 96, N''int''),
        (N''@P@Grading'', N''Grading_Technician'', N''PLM_DW_Tab_Grading_4006'', N''Grading_Technician_3168_FK_pdmsecuritywebuser'', 4006, 3168, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField'', 1, 80, N''int''),
        (N''@P@Grading'', N''Grading_File'', N''PLM_DW_Tab_Grading_4006'', N''Grading_File_3169_FK_tblSketch'', 4006, 3169, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Grading'', N''Grading_Status'', N''PLM_DW_Tab_Grading_4006'', N''Grading_Status_3170_FK_PLM_DW_UD_Sample_Status_3459'', 4006, 3170, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Grading'', N''Factory_Comments'', N''PLM_DW_Tab_Grading_4006'', N''Factory_Comments_5350'', 4006, 5350, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_Summary'', N''Size_Run'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Size_Run_30_FK_tblSizeRun'', 4238, 30, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Proto_Summary'', N''Base_Size'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Base_Size_31_FK_tblSizeRunRotate'', 4238, 31, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Proto_Summary'', N''Spec_Selected_Size'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Spec_Selected_Size_52'', 4238, 52, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_Summary'', N''Measure_Unit'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Measure_Unit_57_FK_'', 4238, 57, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 160, N''int''),
        (N''@P@Proto_Summary'', N''SpecSampleSize'', N''PLM_DW_Tab_Proto_Summary_4238'', N''SpecSampleSize_118_FK_tblSizeRunRotate'', 4238, 118, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Proto_Summary'', N''SpecSampleColor'', N''PLM_DW_Tab_Proto_Summary_4238'', N''SpecSampleColor_119_FK_PdmRGBColor'', 4238, 119, NULL, NULL, NULL, NULL, N''PdmRGBColor'', N''TabField'', 1, 172, N''int''),
        (N''@P@Proto_Summary'', N''SpecFitStatus'', N''PLM_DW_Tab_Proto_Summary_4238'', N''SpecFitStatus_138_FK_'', 4238, 138, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 90, N''int''),
        (N''@P@Proto_Summary'', N''Sample_Status_5378'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Sample_Status_5378_FK_PLM_DW_UD_Sample_Status_3459'', 4238, 5378, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Proto_Summary'', N''State_5379'', N''PLM_DW_Tab_Proto_Summary_4238'', N''State_5379'', 4238, 5379, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Proto_Summary'', N''Sample_Status_5380'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Sample_Status_5380_FK_PLM_DW_UD_Sample_Status_3459'', 4238, 5380, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Proto_Summary'', N''State_5381'', N''PLM_DW_Tab_Proto_Summary_4238'', N''State_5381'', 4238, 5381, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Proto_Summary'', N''Sample_Status_5382'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Sample_Status_5382_FK_PLM_DW_UD_Sample_Status_3459'', 4238, 5382, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Proto_Summary'', N''State_5383'', N''PLM_DW_Tab_Proto_Summary_4238'', N''State_5383'', 4238, 5383, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Proto_Summary'', N''Request_Date_5384'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Request_Date_5384'', 4238, 5384, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Required_By_5385'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Required_By_5385'', 4238, 5385, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Expected_Date_5386'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Expected_Date_5386'', 4238, 5386, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Receive_Date_5387'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Receive_Date_5387'', 4238, 5387, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Approve_Date_5388'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Approve_Date_5388'', 4238, 5388, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Request_Date_5391'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Request_Date_5391'', 4238, 5391, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Required_By_5392'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Required_By_5392'', 4238, 5392, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Expected_Date_5393'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Expected_Date_5393'', 4238, 5393, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Receive_Date_5394'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Receive_Date_5394'', 4238, 5394, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Approve_Date_5395'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Approve_Date_5395'', 4238, 5395, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Request_Date_5396'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Request_Date_5396'', 4238, 5396, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Required_By_5397'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Required_By_5397'', 4238, 5397, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Expected_Date_5398'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Expected_Date_5398'', 4238, 5398, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Receive_Date_5399'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Receive_Date_5399'', 4238, 5399, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_Summary'', N''Approve_Date_5400'', N''PLM_DW_Tab_Proto_Summary_4238'', N''Approve_Date_5400'', 4238, 5400, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_1'', N''Size_Run'', N''PLM_DW_Tab_Proto_1_4239'', N''Size_Run_30_FK_tblSizeRun'', 4239, 30, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Proto_1'', N''Base_Size'', N''PLM_DW_Tab_Proto_1_4239'', N''Base_Size_31_FK_tblSizeRunRotate'', 4239, 31, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Proto_1'', N''Spec_Selected_Size'', N''PLM_DW_Tab_Proto_1_4239'', N''Spec_Selected_Size_52'', 4239, 52, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_1'', N''Measure_Unit'', N''PLM_DW_Tab_Proto_1_4239'', N''Measure_Unit_57_FK_'', 4239, 57, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 160, N''int''),
        (N''@P@Proto_1'', N''SpecSampleSize'', N''PLM_DW_Tab_Proto_1_4239'', N''SpecSampleSize_118_FK_tblSizeRunRotate'', 4239, 118, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Proto_1'', N''SpecSampleColor'', N''PLM_DW_Tab_Proto_1_4239'', N''SpecSampleColor_119_FK_PdmRGBColor'', 4239, 119, NULL, NULL, NULL, NULL, N''PdmRGBColor'', N''TabField'', 1, 172, N''int''),
        (N''@P@Proto_1'', N''SpecFitStatus'', N''PLM_DW_Tab_Proto_1_4239'', N''SpecFitStatus_138_FK_'', 4239, 138, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 90, N''int''),
        (N''@P@Proto_1'', N''Sample_Status'', N''PLM_DW_Tab_Proto_1_4239'', N''Sample_Status_5378_FK_PLM_DW_UD_Sample_Status_3459'', 4239, 5378, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Proto_1'', N''State'', N''PLM_DW_Tab_Proto_1_4239'', N''State_5379'', 4239, 5379, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Proto_1'', N''Request_Date'', N''PLM_DW_Tab_Proto_1_4239'', N''Request_Date_5384'', 4239, 5384, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_1'', N''Required_By'', N''PLM_DW_Tab_Proto_1_4239'', N''Required_By_5385'', 4239, 5385, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_1'', N''Expected_Date'', N''PLM_DW_Tab_Proto_1_4239'', N''Expected_Date_5386'', 4239, 5386, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_1'', N''Receive_Date'', N''PLM_DW_Tab_Proto_1_4239'', N''Receive_Date_5387'', 4239, 5387, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_1'', N''Approve_Date'', N''PLM_DW_Tab_Proto_1_4239'', N''Approve_Date_5388'', 4239, 5388, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_1'', N''Proto_Tech_Pack_Sent'', N''PLM_DW_Tab_Proto_1_4239'', N''Proto_Tech_Pack_Sent_5401'', 4239, 5401, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_1'', N''Comments_Sent'', N''PLM_DW_Tab_Proto_1_4239'', N''Comments_Sent_5402'', 4239, 5402, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_1'', N''Sample_Sent_Date'', N''PLM_DW_Tab_Proto_1_4239'', N''Sample_Sent_Date_5403'', 4239, 5403, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_1'', N''AWB_Courier_Information'', N''PLM_DW_Tab_Proto_1_4239'', N''AWB_Courier_Information_5404'', 4239, 5404, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_1'', N''Rejection_Reason'', N''PLM_DW_Tab_Proto_1_4239'', N''Rejection_Reason_5405'', 4239, 5405, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_1'', N''Supplier_Measurer'', N''PLM_DW_Tab_Proto_1_4239'', N''Supplier_Measurer_5406'', 4239, 5406, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_1'', N''Supplier_Meas_Date'', N''PLM_DW_Tab_Proto_1_4239'', N''Supplier_Meas_Date_5407'', 4239, 5407, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_1'', N''Fit_Date'', N''PLM_DW_Tab_Proto_1_4239'', N''Fit_Date_5408'', 4239, 5408, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_1'', N''Factory_Comments'', N''PLM_DW_Tab_Proto_1_4239'', N''Factory_Comments_5409'', 4239, 5409, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_1'', N''Design_Comments'', N''PLM_DW_Tab_Proto_1_4239'', N''Design_Comments_5410'', 4239, 5410, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_1'', N''Buying_Comments'', N''PLM_DW_Tab_Proto_1_4239'', N''Buying_Comments_5411'', 4239, 5411, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_1'', N''Tech_Comments'', N''PLM_DW_Tab_Proto_1_4239'', N''Tech_Comments_5412'', 4239, 5412, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_2'', N''Size_Run'', N''PLM_DW_Tab_Proto_2_4241'', N''Size_Run_30_FK_tblSizeRun'', 4241, 30, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Proto_2'', N''Base_Size'', N''PLM_DW_Tab_Proto_2_4241'', N''Base_Size_31_FK_tblSizeRunRotate'', 4241, 31, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Proto_2'', N''Spec_Selected_Size'', N''PLM_DW_Tab_Proto_2_4241'', N''Spec_Selected_Size_52'', 4241, 52, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_2'', N''Measure_Unit'', N''PLM_DW_Tab_Proto_2_4241'', N''Measure_Unit_57_FK_'', 4241, 57, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 160, N''int''),
        (N''@P@Proto_2'', N''SpecSampleSize'', N''PLM_DW_Tab_Proto_2_4241'', N''SpecSampleSize_118_FK_tblSizeRunRotate'', 4241, 118, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Proto_2'', N''SpecSampleColor'', N''PLM_DW_Tab_Proto_2_4241'', N''SpecSampleColor_119_FK_PdmRGBColor'', 4241, 119, NULL, NULL, NULL, NULL, N''PdmRGBColor'', N''TabField'', 1, 172, N''int''),
        (N''@P@Proto_2'', N''SpecFitStatus'', N''PLM_DW_Tab_Proto_2_4241'', N''SpecFitStatus_138_FK_'', 4241, 138, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 90, N''int''),
        (N''@P@Proto_2'', N''Sample_Status'', N''PLM_DW_Tab_Proto_2_4241'', N''Sample_Status_5380_FK_PLM_DW_UD_Sample_Status_3459'', 4241, 5380, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Proto_2'', N''State'', N''PLM_DW_Tab_Proto_2_4241'', N''State_5381'', 4241, 5381, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Proto_2'', N''Request_Date'', N''PLM_DW_Tab_Proto_2_4241'', N''Request_Date_5391'', 4241, 5391, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_2'', N''Required_By'', N''PLM_DW_Tab_Proto_2_4241'', N''Required_By_5392'', 4241, 5392, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_2'', N''Expected_Date'', N''PLM_DW_Tab_Proto_2_4241'', N''Expected_Date_5393'', 4241, 5393, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_2'', N''Receive_Date'', N''PLM_DW_Tab_Proto_2_4241'', N''Receive_Date_5394'', 4241, 5394, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_2'', N''Approve_Date'', N''PLM_DW_Tab_Proto_2_4241'', N''Approve_Date_5395'', 4241, 5395, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_2'', N''Comments_Sent'', N''PLM_DW_Tab_Proto_2_4241'', N''Comments_Sent_5413'', 4241, 5413, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_2'', N''Sample_Sent_Date'', N''PLM_DW_Tab_Proto_2_4241'', N''Sample_Sent_Date_5414'', 4241, 5414, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_2'', N''AWB_Courier_Information'', N''PLM_DW_Tab_Proto_2_4241'', N''AWB_Courier_Information_5415'', 4241, 5415, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_2'', N''Rejection_Reason'', N''PLM_DW_Tab_Proto_2_4241'', N''Rejection_Reason_5416'', 4241, 5416, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_2'', N''Supplier_Measurer'', N''PLM_DW_Tab_Proto_2_4241'', N''Supplier_Measurer_5417'', 4241, 5417, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_2'', N''Supplier_Meas_Date'', N''PLM_DW_Tab_Proto_2_4241'', N''Supplier_Meas_Date_5418'', 4241, 5418, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_2'', N''Fit_Date'', N''PLM_DW_Tab_Proto_2_4241'', N''Fit_Date_5419'', 4241, 5419, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_2'', N''Buying_Comments'', N''PLM_DW_Tab_Proto_2_4241'', N''Buying_Comments_5420'', 4241, 5420, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_2'', N''Design_Comments'', N''PLM_DW_Tab_Proto_2_4241'', N''Design_Comments_5421'', 4241, 5421, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_2'', N''Factory_Comments'', N''PLM_DW_Tab_Proto_2_4241'', N''Factory_Comments_5422'', 4241, 5422, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_2'', N''Tech_Comments'', N''PLM_DW_Tab_Proto_2_4241'', N''Tech_Comments_5423'', 4241, 5423, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_3'', N''Size_Run'', N''PLM_DW_Tab_Proto_3_4242'', N''Size_Run_30_FK_tblSizeRun'', 4242, 30, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Proto_3'', N''Base_Size'', N''PLM_DW_Tab_Proto_3_4242'', N''Base_Size_31_FK_tblSizeRunRotate'', 4242, 31, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Proto_3'', N''Spec_Selected_Size'', N''PLM_DW_Tab_Proto_3_4242'', N''Spec_Selected_Size_52'', 4242, 52, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_3'', N''Measure_Unit'', N''PLM_DW_Tab_Proto_3_4242'', N''Measure_Unit_57_FK_'', 4242, 57, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 160, N''int''),
        (N''@P@Proto_3'', N''SpecSampleSize'', N''PLM_DW_Tab_Proto_3_4242'', N''SpecSampleSize_118_FK_tblSizeRunRotate'', 4242, 118, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Proto_3'', N''SpecSampleColor'', N''PLM_DW_Tab_Proto_3_4242'', N''SpecSampleColor_119_FK_PdmRGBColor'', 4242, 119, NULL, NULL, NULL, NULL, N''PdmRGBColor'', N''TabField'', 1, 172, N''int''),
        (N''@P@Proto_3'', N''SpecFitStatus'', N''PLM_DW_Tab_Proto_3_4242'', N''SpecFitStatus_138_FK_'', 4242, 138, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 90, N''int''),
        (N''@P@Proto_3'', N''Sample_Status'', N''PLM_DW_Tab_Proto_3_4242'', N''Sample_Status_5382_FK_PLM_DW_UD_Sample_Status_3459'', 4242, 5382, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Proto_3'', N''State'', N''PLM_DW_Tab_Proto_3_4242'', N''State_5383'', 4242, 5383, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Proto_3'', N''Request_Date'', N''PLM_DW_Tab_Proto_3_4242'', N''Request_Date_5396'', 4242, 5396, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_3'', N''Required_By'', N''PLM_DW_Tab_Proto_3_4242'', N''Required_By_5397'', 4242, 5397, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_3'', N''Expected_Date'', N''PLM_DW_Tab_Proto_3_4242'', N''Expected_Date_5398'', 4242, 5398, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_3'', N''Receive_Date'', N''PLM_DW_Tab_Proto_3_4242'', N''Receive_Date_5399'', 4242, 5399, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_3'', N''Approve_Date'', N''PLM_DW_Tab_Proto_3_4242'', N''Approve_Date_5400'', 4242, 5400, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_3'', N''Buying_Comments'', N''PLM_DW_Tab_Proto_3_4242'', N''Buying_Comments_5424'', 4242, 5424, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_3'', N''Comments_Sent'', N''PLM_DW_Tab_Proto_3_4242'', N''Comments_Sent_5425'', 4242, 5425, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_3'', N''Sample_Sent_Date'', N''PLM_DW_Tab_Proto_3_4242'', N''Sample_Sent_Date_5426'', 4242, 5426, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_3'', N''AWB_Courier_Information'', N''PLM_DW_Tab_Proto_3_4242'', N''AWB_Courier_Information_5427'', 4242, 5427, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_3'', N''Rejection_Reason'', N''PLM_DW_Tab_Proto_3_4242'', N''Rejection_Reason_5428'', 4242, 5428, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_3'', N''Design_Comments'', N''PLM_DW_Tab_Proto_3_4242'', N''Design_Comments_5429'', 4242, 5429, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_3'', N''Factory_Comments'', N''PLM_DW_Tab_Proto_3_4242'', N''Factory_Comments_5430'', 4242, 5430, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Proto_3'', N''Fit_Date'', N''PLM_DW_Tab_Proto_3_4242'', N''Fit_Date_5431'', 4242, 5431, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_3'', N''Supplier_Measurer'', N''PLM_DW_Tab_Proto_3_4242'', N''Supplier_Measurer_5432'', 4242, 5432, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Proto_3'', N''Supplier_Meas_Date'', N''PLM_DW_Tab_Proto_3_4242'', N''Supplier_Meas_Date_5433'', 4242, 5433, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Proto_3'', N''Tech_Comments'', N''PLM_DW_Tab_Proto_3_4242'', N''Tech_Comments_5434'', 4242, 5434, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Sale_Sample'', N''Size_Run'', N''PLM_DW_Tab_Sale_Sample_4243'', N''Size_Run_30_FK_tblSizeRun'', 4243, 30, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Sale_Sample'', N''Base_Size'', N''PLM_DW_Tab_Sale_Sample_4243'', N''Base_Size_31_FK_tblSizeRunRotate'', 4243, 31, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Sale_Sample'', N''Spec_Selected_Size'', N''PLM_DW_Tab_Sale_Sample_4243'', N''Spec_Selected_Size_52'', 4243, 52, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Sale_Sample'', N''Measure_Unit'', N''PLM_DW_Tab_Sale_Sample_4243'', N''Measure_Unit_57_FK_'', 4243, 57, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 160, N''int''),
        (N''@P@Sale_Sample'', N''SpecSampleSize'', N''PLM_DW_Tab_Sale_Sample_4243'', N''SpecSampleSize_118_FK_tblSizeRunRotate'', 4243, 118, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Sale_Sample'', N''SpecSampleColor'', N''PLM_DW_Tab_Sale_Sample_4243'', N''SpecSampleColor_119_FK_PdmRGBColor'', 4243, 119, NULL, NULL, NULL, NULL, N''PdmRGBColor'', N''TabField'', 1, 172, N''int''),
        (N''@P@Sale_Sample'', N''SpecFitStatus'', N''PLM_DW_Tab_Sale_Sample_4243'', N''SpecFitStatus_138_FK_'', 4243, 138, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 90, N''int''),
        (N''@P@Sale_Sample'', N''Buying_Photography_Comments'', N''PLM_DW_Tab_Sale_Sample_4243'', N''Buying_Photography_Comments_5435'', 4243, 5435, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Sale_Sample'', N''Design_Comments'', N''PLM_DW_Tab_Sale_Sample_4243'', N''Design_Comments_5446'', 4243, 5446, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Sale_Sample'', N''Factory_Comments'', N''PLM_DW_Tab_Sale_Sample_4243'', N''Factory_Comments_5449'', 4243, 5449, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Sale_Sample'', N''Supplier_Measurer'', N''PLM_DW_Tab_Sale_Sample_4243'', N''Supplier_Measurer_5454'', 4243, 5454, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Sale_Sample'', N''Supplier_Meas_Date'', N''PLM_DW_Tab_Sale_Sample_4243'', N''Supplier_Meas_Date_5455'', 4243, 5455, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Sale_Sample'', N''Tech_Comments'', N''PLM_DW_Tab_Sale_Sample_4243'', N''Tech_Comments_5460'', 4243, 5460, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Sale_Sample'', N''Wholesale_Comments'', N''PLM_DW_Tab_Sale_Sample_4243'', N''Wholesale_Comments_5463'', 4243, 5463, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Fit_1'', N''Size_Run'', N''PLM_DW_Tab_Fit_1_4244'', N''Size_Run_30_FK_tblSizeRun'', 4244, 30, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Fit_1'', N''Base_Size'', N''PLM_DW_Tab_Fit_1_4244'', N''Base_Size_31_FK_tblSizeRunRotate'', 4244, 31, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Fit_1'', N''Spec_Selected_Size'', N''PLM_DW_Tab_Fit_1_4244'', N''Spec_Selected_Size_52'', 4244, 52, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Fit_1'', N''Measure_Unit'', N''PLM_DW_Tab_Fit_1_4244'', N''Measure_Unit_57_FK_'', 4244, 57, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 160, N''int''),
        (N''@P@Fit_1'', N''SpecSampleSize'', N''PLM_DW_Tab_Fit_1_4244'', N''SpecSampleSize_118_FK_tblSizeRunRotate'', 4244, 118, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Fit_1'', N''SpecSampleColor'', N''PLM_DW_Tab_Fit_1_4244'', N''SpecSampleColor_119_FK_PdmRGBColor'', 4244, 119, NULL, NULL, NULL, NULL, N''PdmRGBColor'', N''TabField'', 1, 172, N''int''),
        (N''@P@Fit_1'', N''SpecFitStatus'', N''PLM_DW_Tab_Fit_1_4244'', N''SpecFitStatus_138_FK_'', 4244, 138, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 90, N''int''),
        (N''@P@Fit_1'', N''Buying_Comments'', N''PLM_DW_Tab_Fit_1_4244'', N''Buying_Comments_5436'', 4244, 5436, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Fit_1'', N''Comments_Sent'', N''PLM_DW_Tab_Fit_1_4244'', N''Comments_Sent_5438'', 4244, 5438, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_1'', N''Sample_Sent_Date'', N''PLM_DW_Tab_Fit_1_4244'', N''Sample_Sent_Date_5439'', 4244, 5439, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_1'', N''AWB_Courier_Information'', N''PLM_DW_Tab_Fit_1_4244'', N''AWB_Courier_Information_5440'', 4244, 5440, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Fit_1'', N''Rejection_Reason'', N''PLM_DW_Tab_Fit_1_4244'', N''Rejection_Reason_5441'', 4244, 5441, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Fit_1'', N''Design_Comments'', N''PLM_DW_Tab_Fit_1_4244'', N''Design_Comments_5447'', 4244, 5447, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Fit_1'', N''Factory_Comments'', N''PLM_DW_Tab_Fit_1_4244'', N''Factory_Comments_5450'', 4244, 5450, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Fit_1'', N''Fit_Date'', N''PLM_DW_Tab_Fit_1_4244'', N''Fit_Date_5452'', 4244, 5452, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_1'', N''Supplier_Measurer'', N''PLM_DW_Tab_Fit_1_4244'', N''Supplier_Measurer_5456'', 4244, 5456, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Fit_1'', N''Supplier_Meas_Date'', N''PLM_DW_Tab_Fit_1_4244'', N''Supplier_Meas_Date_5457'', 4244, 5457, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_1'', N''Tech_Comments'', N''PLM_DW_Tab_Fit_1_4244'', N''Tech_Comments_5461'', 4244, 5461, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Fit_1'', N''Request_Date'', N''PLM_DW_Tab_Fit_1_4244'', N''Request_Date_5464'', 4244, 5464, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_1'', N''Required_By'', N''PLM_DW_Tab_Fit_1_4244'', N''Required_By_5465'', 4244, 5465, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_1'', N''Expected_Date'', N''PLM_DW_Tab_Fit_1_4244'', N''Expected_Date_5466'', 4244, 5466, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_1'', N''Receive_Date'', N''PLM_DW_Tab_Fit_1_4244'', N''Receive_Date_5467'', 4244, 5467, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_1'', N''Approve_Date'', N''PLM_DW_Tab_Fit_1_4244'', N''Approve_Date_5468'', 4244, 5468, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_1'', N''Sample_Status'', N''PLM_DW_Tab_Fit_1_4244'', N''Sample_Status_5474_FK_PLM_DW_UD_Sample_Status_3459'', 4244, 5474, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Fit_1'', N''State'', N''PLM_DW_Tab_Fit_1_4244'', N''State_5475'', 4244, 5475, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Fit_2'', N''Size_Run'', N''PLM_DW_Tab_Fit_2_4245'', N''Size_Run_30_FK_tblSizeRun'', 4245, 30, NULL, NULL, NULL, NULL, N''tblSizeRun'', N''TabField'', 1, 10, N''int''),
        (N''@P@Fit_2'', N''Base_Size'', N''PLM_DW_Tab_Fit_2_4245'', N''Base_Size_31_FK_tblSizeRunRotate'', 4245, 31, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Fit_2'', N''Spec_Selected_Size'', N''PLM_DW_Tab_Fit_2_4245'', N''Spec_Selected_Size_52'', 4245, 52, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Fit_2'', N''Measure_Unit'', N''PLM_DW_Tab_Fit_2_4245'', N''Measure_Unit_57_FK_'', 4245, 57, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 160, N''int''),
        (N''@P@Fit_2'', N''SpecSampleSize'', N''PLM_DW_Tab_Fit_2_4245'', N''SpecSampleSize_118_FK_tblSizeRunRotate'', 4245, 118, NULL, NULL, NULL, NULL, N''tblSizeRunRotate'', N''TabField'', 1, 63, N''int''),
        (N''@P@Fit_2'', N''SpecSampleColor'', N''PLM_DW_Tab_Fit_2_4245'', N''SpecSampleColor_119_FK_PdmRGBColor'', 4245, 119, NULL, NULL, NULL, NULL, N''PdmRGBColor'', N''TabField'', 1, 172, N''int''),
        (N''@P@Fit_2'', N''SpecFitStatus'', N''PLM_DW_Tab_Fit_2_4245'', N''SpecFitStatus_138_FK_'', 4245, 138, NULL, NULL, NULL, NULL, NULL, N''TabField'', 1, 90, N''int''),
        (N''@P@Fit_2'', N''Buying_Comments'', N''PLM_DW_Tab_Fit_2_4245'', N''Buying_Comments_5437'', 4245, 5437, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Fit_2'', N''Comments_Sent'', N''PLM_DW_Tab_Fit_2_4245'', N''Comments_Sent_5442'', 4245, 5442, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_2'', N''Sample_Sent_Date'', N''PLM_DW_Tab_Fit_2_4245'', N''Sample_Sent_Date_5443'', 4245, 5443, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_2'', N''AWB_Courier_Information'', N''PLM_DW_Tab_Fit_2_4245'', N''AWB_Courier_Information_5444'', 4245, 5444, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Fit_2'', N''Rejection_Reason'', N''PLM_DW_Tab_Fit_2_4245'', N''Rejection_Reason_5445'', 4245, 5445, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Fit_2'', N''Design_Comments'', N''PLM_DW_Tab_Fit_2_4245'', N''Design_Comments_5448'', 4245, 5448, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Fit_2'', N''Factory_Comments'', N''PLM_DW_Tab_Fit_2_4245'', N''Factory_Comments_5451'', 4245, 5451, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Fit_2'', N''Fit_Date'', N''PLM_DW_Tab_Fit_2_4245'', N''Fit_Date_5453'', 4245, 5453, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_2'', N''Supplier_Measurer'', N''PLM_DW_Tab_Fit_2_4245'', N''Supplier_Measurer_5458'', 4245, 5458, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Fit_2'', N''Supplier_Meas_Date'', N''PLM_DW_Tab_Fit_2_4245'', N''Supplier_Meas_Date_5459'', 4245, 5459, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_2'', N''Tech_Comments'', N''PLM_DW_Tab_Fit_2_4245'', N''Tech_Comments_5462'', 4245, 5462, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Fit_2'', N''Request_Date'', N''PLM_DW_Tab_Fit_2_4245'', N''Request_Date_5469'', 4245, 5469, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_2'', N''Required_By'', N''PLM_DW_Tab_Fit_2_4245'', N''Required_By_5470'', 4245, 5470, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_2'', N''Expected_Date'', N''PLM_DW_Tab_Fit_2_4245'', N''Expected_Date_5471'', 4245, 5471, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_2'', N''Receive_Date'', N''PLM_DW_Tab_Fit_2_4245'', N''Receive_Date_5472'', 4245, 5472, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_2'', N''Approve_Date'', N''PLM_DW_Tab_Fit_2_4245'', N''Approve_Date_5473'', 4245, 5473, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Fit_2'', N''Sample_Status'', N''PLM_DW_Tab_Fit_2_4245'', N''Sample_Status_5476_FK_PLM_DW_UD_Sample_Status_3459'', 4245, 5476, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Sample_Status_3459'', N''TabField'', 1, 3459, N''int''),
        (N''@P@Fit_2'', N''State'', N''PLM_DW_Tab_Fit_2_4245'', N''State_5477'', 4245, 5477, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@SpecGradingGrid'', N''Critical_Point'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''Critical_Point_552'', 4006, 552, 46, 10, 552, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''BodyPartDetailIDWDimDetailID'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart'', 4006, 167, 46, 10, 167, NULL, N''PdmV2kBodyPart'', N''GridColumn'', 1, 35, N''int''),
        (N''@P@SpecGradingGrid'', N''Code'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''Code_168'', 4006, 168, 46, 10, 168, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''BodyPartName'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''BodyPartName_169'', 4006, 169, 46, 10, 169, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''BodyPartDesc'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''BodyPartDesc_170'', 4006, 170, 46, 10, 170, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''HowToMeasure'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''HowToMeasure_171'', 4006, 171, 46, 10, 171, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''IsoPomItem'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''IsoPomItem_748_FK_pdmV2kBodyPart'', 4006, 748, 46, 10, 748, NULL, N''pdmV2kBodyPart'', N''GridColumn'', 1, 186, N''int''),
        (N''@P@SpecGradingGrid'', N''Add_Desc'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''Add_Desc_247'', 4006, 247, 46, 10, 247, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''Dimension'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''Dimension_648_FK_tblDimension'', 4006, 648, 46, 10, 648, NULL, N''tblDimension'', N''GridColumn'', 1, 9, N''int''),
        (N''@P@SpecGradingGrid'', N''DimensionDetail'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''DimensionDetail_645_FK_tblDimensionDetail'', 4006, 645, 46, 10, 645, NULL, N''tblDimensionDetail'', N''GridColumn'', 1, 73, N''int''),
        (N''@P@SpecGradingGrid'', N''NeedToApplyGradingRule'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''NeedToApplyGradingRule_651'', 4006, 651, 46, 10, 651, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''Tolerance'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''Tolerance_172'', 4006, 172, 46, 10, 172, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingBaseSize'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingBaseSize_173'', 4006, 173, 46, 10, 173, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize1'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize1_174'', 4006, 174, 46, 10, 174, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize2'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize2_177'', 4006, 177, 46, 10, 177, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize3'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize3_180'', 4006, 180, 46, 10, 180, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize4'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize4_183'', 4006, 183, 46, 10, 183, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize5'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize5_186'', 4006, 186, 46, 10, 186, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize6'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize6_189'', 4006, 189, 46, 10, 189, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize7'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize7_192'', 4006, 192, 46, 10, 192, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize8'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize8_195'', 4006, 195, 46, 10, 195, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize9'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize9_198'', 4006, 198, 46, 10, 198, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize10'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize10_201'', 4006, 201, 46, 10, 201, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize11'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize11_204'', 4006, 204, 46, 10, 204, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize12'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize12_207'', 4006, 207, 46, 10, 207, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize13'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize13_210'', 4006, 210, 46, 10, 210, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize14'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize14_213'', 4006, 213, 46, 10, 213, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize15'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize15_216'', 4006, 216, 46, 10, 216, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize16'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize16_219'', 4006, 219, 46, 10, 219, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize17'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize17_222'', 4006, 222, 46, 10, 222, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize18'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize18_225'', 4006, 225, 46, 10, 225, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize19'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize19_228'', 4006, 228, 46, 10, 228, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''GradingSize20'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''GradingSize20_231'', 4006, 231, 46, 10, 231, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecGradingGrid'', N''Comments'', N''PLM_DW_Grid_SpecGradingGrid_10'', N''Comments_234'', 4006, 234, 46, 10, 234, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''BodyPartDetailIDWDimDetailID'', N''PLM_DW_Grid_SpecFitGrid_5'', N''BodyPartDetailIDWDimDetailID_28'', 4238, 28, 32, 5, 28, NULL, NULL, N''GridColumn'', 1, NULL, N''int''),
        (N''@P@SpecFitGrid'', N''Critical_Point'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Critical_Point_404'', 4238, 404, 32, 5, 404, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Code'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Code_65'', 4238, 65, 32, 5, 65, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''BodyPartName'', N''PLM_DW_Grid_SpecFitGrid_5'', N''BodyPartName_66'', 4238, 66, 32, 5, 66, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''BodyPartDesc'', N''PLM_DW_Grid_SpecFitGrid_5'', N''BodyPartDesc_67'', 4238, 67, 32, 5, 67, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''HowToMeasure'', N''PLM_DW_Grid_SpecFitGrid_5'', N''HowToMeasure_39'', 4238, 39, 32, 5, 39, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''NeedToApplyGradingRule'', N''PLM_DW_Grid_SpecFitGrid_5'', N''NeedToApplyGradingRule_650'', 4238, 650, 32, 5, 650, NULL, NULL, N''GridColumn'', 13, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Add_Desc'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Add_Desc_246'', 4238, 246, 32, 5, 246, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Dimension'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Dimension_647_FK_tblDimension'', 4238, 647, 32, 5, 647, NULL, N''tblDimension'', N''GridColumn'', 1, 9, N''int''),
        (N''@P@SpecFitGrid'', N''DimensionDetail'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DimensionDetail_644_FK_tblDimensionDetail'', 4238, 644, 32, 5, 644, NULL, N''tblDimensionDetail'', N''GridColumn'', 1, 73, N''int''),
        (N''@P@SpecFitGrid'', N''Tolerance'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Tolerance_68'', 4238, 68, 32, 5, 68, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''InitiaSpec'', N''PLM_DW_Grid_SpecFitGrid_5'', N''InitiaSpec_29'', 4238, 29, 32, 5, 29, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample_Initia_Spec'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample_Initia_Spec_395'', 4238, 395, 32, 5, 395, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample11'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample11_377'', 4238, 377, 32, 5, 377, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference11'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference11_396'', 4238, 396, 32, 5, 396, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample1'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample1_30'', 4238, 30, 32, 5, 30, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference1'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference1_163'', 4238, 163, 32, 5, 163, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''AfterWashIron1'', N''PLM_DW_Grid_SpecFitGrid_5'', N''AfterWashIron1_656'', 4238, 656, 32, 5, 656, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffAfterIronAndSpec1'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffAfterIronAndSpec1_657'', 4238, 657, 32, 5, 657, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffSample1'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffSample1_378'', 4238, 378, 32, 5, 378, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Revise1'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Revise1_31'', 4238, 31, 32, 5, 31, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Comment1'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Comment1_440'', 4238, 440, 32, 5, 440, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample22'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample22_379'', 4238, 379, 32, 5, 379, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference22'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference22_397'', 4238, 397, 32, 5, 397, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample2'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample2_32'', 4238, 32, 32, 5, 32, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference2'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference2_164'', 4238, 164, 32, 5, 164, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''AfterWashIron2'', N''PLM_DW_Grid_SpecFitGrid_5'', N''AfterWashIron2_658'', 4238, 658, 32, 5, 658, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffAfterIronAndSpec2'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffAfterIronAndSpec2_659'', 4238, 659, 32, 5, 659, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffSample2'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffSample2_380'', 4238, 380, 32, 5, 380, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Revise2'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Revise2_33'', 4238, 33, 32, 5, 33, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Comment2'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Comment2_441'', 4238, 441, 32, 5, 441, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample33'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample33_381'', 4238, 381, 32, 5, 381, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference33'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference33_398'', 4238, 398, 32, 5, 398, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample3'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample3_34'', 4238, 34, 32, 5, 34, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference3'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference3_165'', 4238, 165, 32, 5, 165, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''AfterWashIron3'', N''PLM_DW_Grid_SpecFitGrid_5'', N''AfterWashIron3_660'', 4238, 660, 32, 5, 660, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffAfterIronAndSpec3'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffAfterIronAndSpec3_661'', 4238, 661, 32, 5, 661, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffSample3'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffSample3_382'', 4238, 382, 32, 5, 382, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Revise3'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Revise3_35'', 4238, 35, 32, 5, 35, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Comment3'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Comment3_442'', 4238, 442, 32, 5, 442, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample44'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample44_383'', 4238, 383, 32, 5, 383, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference44'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference44_399'', 4238, 399, 32, 5, 399, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample4'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample4_36'', 4238, 36, 32, 5, 36, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference4'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference4_166'', 4238, 166, 32, 5, 166, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''AfterWashIron4'', N''PLM_DW_Grid_SpecFitGrid_5'', N''AfterWashIron4_662'', 4238, 662, 32, 5, 662, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffAfterIronAndSpec4'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffAfterIronAndSpec4_663'', 4238, 663, 32, 5, 663, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffSample4'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffSample4_384'', 4238, 384, 32, 5, 384, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Revise4'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Revise4_37'', 4238, 37, 32, 5, 37, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Comment4'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Comment4_443'', 4238, 443, 32, 5, 443, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample55'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample55_385'', 4238, 385, 32, 5, 385, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference55'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference55_400'', 4238, 400, 32, 5, 400, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample5'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample5_326'', 4238, 326, 32, 5, 326, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference5'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference5_327'', 4238, 327, 32, 5, 327, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''AfterWashIron5'', N''PLM_DW_Grid_SpecFitGrid_5'', N''AfterWashIron5_664'', 4238, 664, 32, 5, 664, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffAfterIronAndSpec5'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffAfterIronAndSpec5_665'', 4238, 665, 32, 5, 665, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffSample5'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffSample5_386'', 4238, 386, 32, 5, 386, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Revise5'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Revise5_328'', 4238, 328, 32, 5, 328, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Comment5'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Comment5_444'', 4238, 444, 32, 5, 444, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample66'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample66_387'', 4238, 387, 32, 5, 387, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference66'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference66_401'', 4238, 401, 32, 5, 401, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Sample6'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Sample6_329'', 4238, 329, 32, 5, 329, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Difference6'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Difference6_330'', 4238, 330, 32, 5, 330, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''AfterWashIron6'', N''PLM_DW_Grid_SpecFitGrid_5'', N''AfterWashIron6_666'', 4238, 666, 32, 5, 666, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffAfterIronAndSpec6'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffAfterIronAndSpec6_667'', 4238, 667, 32, 5, 667, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''DiffSample6'', N''PLM_DW_Grid_SpecFitGrid_5'', N''DiffSample6_388'', 4238, 388, 32, 5, 388, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Revise6'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Revise6_331'', 4238, 331, 32, 5, 331, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''Comment6'', N''PLM_DW_Grid_SpecFitGrid_5'', N''Comment6_445'', 4238, 445, 32, 5, 445, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@SpecFitGrid'', N''FinalSpec'', N''PLM_DW_Grid_SpecFitGrid_5'', N''FinalSpec_38'', 4238, 38, 32, 5, 38, NULL, NULL, N''GridColumn'', 2, NULL, N''nvarchar''),
        (N''@P@ReferenceBasicInfo'', N''ReferenceCode'', N''PLM_DW_Tab_Grading_4006'', N''Size_Run_43_FK_tblSizeRun'', 4006, 43, NULL, NULL, NULL, NULL, NULL, N''ReferenceField'', 1, 10, N''nvarchar'')
';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;
GO

