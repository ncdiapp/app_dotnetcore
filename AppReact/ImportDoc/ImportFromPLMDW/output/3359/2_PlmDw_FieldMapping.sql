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
    + N' WHERE [AppTableName] IN (N''@P@ReferenceBasicInfo'', N''@P@Graphic_Requests'');';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;

-- FieldMapping INSERT batch 1 (37 row(s))
SET @sql = N'
INSERT INTO dbo.' + QUOTENAME(@MappingTable) + N' (
    [AppTableName],[AppColumnName],[DwTableName],[DwColumnName],
    [PlmTabId],[PlmSubItemId],[PlmGridSubItemId],[PlmGridId],[PlmMetaColumnId],
    [PlmBlockId],[DwFkTarget],[FieldKind],[PlmControlType],[PlmEntityId],[DwDataType]
)
VALUES
        (N''@P@Graphic_Requests'', N''Created_By'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Created_By_189'', 4259, 189, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Last_Revised_By'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Last_Revised_By_190'', 4259, 190, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Other_Image'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Other_Image_3104_FK_tblSketch'', 4259, 3104, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 5, 11, N''int''),
        (N''@P@Graphic_Requests'', N''Request_Type'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Request_Type_6817_FK_PLM_DW_UD_GraphicRequestType_4735'', 4259, 6817, NULL, NULL, NULL, NULL, N''PLM_DW_UD_GraphicRequestType_4735'', N''TabField'', 1, 4735, N''int''),
        (N''@P@Graphic_Requests'', N''Request_Date'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Request_Date_6818'', 4259, 6818, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Graphic_Requests'', N''Request_By'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Request_By_6819_FK_pdmsecuritywebuser'', 4259, 6819, NULL, NULL, NULL, NULL, N''pdmsecuritywebuser'', N''TabField'', 1, 80, N''int''),
        (N''@P@Graphic_Requests'', N''Deliver_By'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Deliver_By_6820'', 4259, 6820, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Graphic_Requests'', N''Additional_Info'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Additional_Info_6821'', 4259, 6821, NULL, NULL, NULL, NULL, NULL, N''TabField'', 4, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Requestor_Role'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Requestor_Role_6831_FK_pdmSecurityUserGroup'', 4259, 6831, NULL, NULL, NULL, NULL, N''pdmSecurityUserGroup'', N''TabField'', 1, 96, N''int''),
        (N''@P@Graphic_Requests'', N''Graphic_Req'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Graphic_Req__6832'', 4259, 6832, NULL, NULL, NULL, NULL, NULL, N''TabField'', 23, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Handled_By'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Handled_By_6833_FK_PLM_DW_UD_GraphicRequestHandledBy_4780'', 4259, 6833, NULL, NULL, NULL, NULL, N''PLM_DW_UD_GraphicRequestHandledBy_4780'', N''TabField'', 1, 4780, N''int''),
        (N''@P@Graphic_Requests'', N''Performed_By'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Performed_By_6834_FK_PLM_DW_UD_GraphicRequestHandledBy_4780'', 4259, 6834, NULL, NULL, NULL, NULL, N''PLM_DW_UD_GraphicRequestHandledBy_4780'', N''TabField'', 1, 4780, N''int''),
        (N''@P@Graphic_Requests'', N''Can_Complete_By'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Can_Complete_By_6835'', 4259, 6835, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Graphic_Requests'', N''Extra_Time_Needed'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Extra_Time_Needed_6836'', 4259, 6836, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Graphic_Requests'', N''Status'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Status_6837_FK_PLM_DW_UD_GraphicRequestStatus_4736'', 4259, 6837, NULL, NULL, NULL, NULL, N''PLM_DW_UD_GraphicRequestStatus_4736'', N''TabField'', 1, 4736, N''int''),
        (N''@P@Graphic_Requests'', N''Completed_Date'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Completed_Date_6838'', 4259, 6838, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Graphic_Requests'', N''Days_Needed'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Days_Needed_6839'', 4259, 6839, NULL, NULL, NULL, NULL, NULL, N''TabField'', 20, NULL, N''float''),
        (N''@P@Graphic_Requests'', N''Revised_Complete_By'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Revised_Complete_By_6840'', 4259, 6840, NULL, NULL, NULL, NULL, NULL, N''TabField'', 7, NULL, N''datetime''),
        (N''@P@Graphic_Requests'', N''Attachment_6841'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Attachment_6841_FK_tblSketch'', 4259, 6841, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Item_Type'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Item_Type_6844_FK_pdmTechPackType'', 4259, 6844, NULL, NULL, NULL, NULL, N''pdmTechPackType'', N''TabField'', 1, 123, N''int''),
        (N''@P@Graphic_Requests'', N''Path'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Path_6858'', 4259, 6858, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Attachment_6859'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Attachment_6859_FK_tblSketch'', 4259, 6859, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Attachment_6860'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Attachment_6860_FK_tblSketch'', 4259, 6860, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''References'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''References_6861'', 4259, 6861, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Attachment_6862'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Attachment_6862_FK_tblSketch'', 4259, 6862, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Attachment_6863'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Attachment_6863_FK_tblSketch'', 4259, 6863, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Attachment_6864'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Attachment_6864_FK_tblSketch'', 4259, 6864, NULL, NULL, NULL, NULL, N''tblSketch'', N''TabField'', 9, 11, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''dateisblank_calc'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''dateisblank_calc_6881'', 4259, 6881, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''setdate_calc'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''setdate_calc_6882'', 4259, 6882, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''blankdate_calc'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''blankdate_calc_6883'', 4259, 6883, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''isCompleted'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''isCompleted_6884'', 4259, 6884, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Request_Name'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Request_Name_7154'', 4259, 7154, NULL, NULL, NULL, NULL, NULL, N''TabField'', 2, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''isArchived'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''isArchived_7155'', 4259, 7155, NULL, NULL, NULL, NULL, NULL, N''TabField'', 13, NULL, N''nvarchar''),
        (N''@P@Graphic_Requests'', N''Notify_GA'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Notify_GA_7164_FK_pdmSecurityUserGroup'', 4259, 7164, NULL, NULL, NULL, NULL, N''pdmSecurityUserGroup'', N''TabField'', 1, 96, N''int''),
        (N''@P@Graphic_Requests'', N''Active_7364'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Active_7364_FK_PLM_DW_UD_Active_Inactive_3637'', 4259, 7364, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Active_Inactive_3637'', N''TabField'', 1, 3637, N''int''),
        (N''@P@Graphic_Requests'', N''Active_7365'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Active_7365_FK_PLM_DW_UD_Active_Inactive_3637'', 4259, 7365, NULL, NULL, NULL, NULL, N''PLM_DW_UD_Active_Inactive_3637'', N''TabField'', 1, 3637, N''int''),
        (N''@P@ReferenceBasicInfo'', N''ReferenceCode'', N''PLM_DW_Tab_Graphic_Requests_4259'', N''Request_Name_7154'', 4259, 7154, NULL, NULL, NULL, NULL, NULL, N''ReferenceField'', 2, NULL, N''nvarchar'')
';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;
GO

