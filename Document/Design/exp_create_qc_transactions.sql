-- =============================================================================
-- EXPERIMENTAL: Spec QC Order List + Spec Qc Order transactions
-- Target: TenantDB_PLM27 (AppAI metadata only — does NOT alter Tchp* tables)
-- Safe to discard by restoring APP DB or deleting IntegrationId TX_QcOrder*
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @AppId INT = 5602;
DECLARE @DsId INT = 1073;
DECLARE @Schema NVARCHAR(20) = N'dbo';

IF EXISTS (SELECT 1 FROM dbo.AppTransaction WHERE IntegrationId IN (N'TX_QcOrder', N'TX_QcOrderList'))
BEGIN
    RAISERROR(N'QC experimental transactions already exist (TX_QcOrder / TX_QcOrderList). Delete them first or restore DB.', 16, 1);
    RETURN;
END

BEGIN TRAN;

DECLARE @FormOrderId INT, @FormListId INT;
DECLARE @TxOrderId INT, @TxListId INT;

INSERT INTO dbo.AppForm (Name, Description, SaasApplicationID, AppCreatedDate)
VALUES (N'Spec Qc Order (exp)', N'Experimental QC Order form', @AppId, GETDATE());
SET @FormOrderId = SCOPE_IDENTITY();

INSERT INTO dbo.AppForm (Name, Description, SaasApplicationID, AppCreatedDate)
VALUES (N'Spec QC Order List (exp)', N'Experimental QC Order List form', @AppId, GETDATE());
SET @FormListId = SCOPE_IDENTITY();

------------------------------------------------------------
-- 1) Spec Qc Order  (root = TchpQcOrder)
------------------------------------------------------------
INSERT INTO dbo.AppTransaction (
    TransactionName, Description, TransactionOrganizedType, FormID,
    DataSourceFrom, IsPhysicalModelTableCreated, IsShowSaveButton,
    SaasApplicationID, IntegrationId, AppCreatedDate)
VALUES (
    N'Spec Qc Order (exp)', N'Experimental — Order header + OrderSize + Garment/Result',
    1, @FormOrderId, @DsId, 1, 1, @AppId, N'TX_QcOrder', GETDATE());
SET @TxOrderId = SCOPE_IDENTITY();

DECLARE @U_OrderRoot INT, @U_OrderSize INT, @U_Garment INT, @U_Result INT;

INSERT INTO dbo.AppTransactionUnit (
    TransactionID, UnitDisplayName, DataBaseTableName, ParentTransactionUnitID,
    IsMasterSiblingUnit, IsSynchToDatabaseTable, SchemaOwner, AppCreatedDate)
VALUES (@TxOrderId, N'Qc Order', N'TchpQcOrder', NULL, NULL, NULL, @Schema, GETDATE());
SET @U_OrderRoot = SCOPE_IDENTITY();

INSERT INTO dbo.AppTransactionUnit (
    TransactionID, UnitDisplayName, DataBaseTableName, ParentTransactionUnitID,
    IsMasterSiblingUnit, IsSynchToDatabaseTable, SchemaOwner, AppCreatedDate)
VALUES (@TxOrderId, N'Selected Sizes', N'TchpQcOrderSize', @U_OrderRoot, 0, NULL, @Schema, GETDATE());
SET @U_OrderSize = SCOPE_IDENTITY();

INSERT INTO dbo.AppTransactionUnit (
    TransactionID, UnitDisplayName, DataBaseTableName, ParentTransactionUnitID,
    IsMasterSiblingUnit, IsSynchToDatabaseTable, SchemaOwner, AppCreatedDate)
VALUES (@TxOrderId, N'Sampled Garments', N'TchpQcGarment', @U_OrderRoot, 0, NULL, @Schema, GETDATE());
SET @U_Garment = SCOPE_IDENTITY();

INSERT INTO dbo.AppTransactionUnit (
    TransactionID, UnitDisplayName, DataBaseTableName, ParentTransactionUnitID,
    IsMasterSiblingUnit, IsSynchToDatabaseTable, SchemaOwner, AppCreatedDate, EmGridViewDisplayType)
VALUES (@TxOrderId, N'QC Results', N'TchpQcResult', @U_Garment, NULL, NULL, @Schema, GETDATE(), NULL);
SET @U_Result = SCOPE_IDENTITY();

------------------------------------------------------------
-- 2) Spec QC Order List (root = Plm_ReferenceBasicInfo)
------------------------------------------------------------
INSERT INTO dbo.AppTransaction (
    TransactionName, Description, TransactionOrganizedType, FormID,
    DataSourceFrom, IsPhysicalModelTableCreated, IsShowSaveButton,
    SaasApplicationID, IntegrationId, AppCreatedDate)
VALUES (
    N'Spec QC Order List (exp)', N'Experimental — Product + StyleSpec + QcOrder list',
    1, @FormListId, @DsId, 1, 1, @AppId, N'TX_QcOrderList', GETDATE());
SET @TxListId = SCOPE_IDENTITY();

DECLARE @U_ListRoot INT, @U_ListStyleSpec INT, @U_ListQcOrder INT;

INSERT INTO dbo.AppTransactionUnit (
    TransactionID, UnitDisplayName, DataBaseTableName, ParentTransactionUnitID,
    IsMasterSiblingUnit, IsSynchToDatabaseTable, SchemaOwner, AppCreatedDate)
VALUES (@TxListId, N'Reference Basic Info', N'Plm_ReferenceBasicInfo', NULL, NULL, NULL, @Schema, GETDATE());
SET @U_ListRoot = SCOPE_IDENTITY();

INSERT INTO dbo.AppTransactionUnit (
    TransactionID, UnitDisplayName, DataBaseTableName, ParentTransactionUnitID,
    IsMasterSiblingUnit, IsSynchToDatabaseTable, SchemaOwner, AppCreatedDate)
VALUES (@TxListId, N'Style Spec', N'TchpStyleSpec', NULL, 1, NULL, @Schema, GETDATE());
SET @U_ListStyleSpec = SCOPE_IDENTITY();

INSERT INTO dbo.AppTransactionUnit (
    TransactionID, UnitDisplayName, DataBaseTableName, ParentTransactionUnitID,
    IsMasterSiblingUnit, IsSynchToDatabaseTable, SchemaOwner, AppCreatedDate)
VALUES (@TxListId, N'QC Orders', N'TchpQcOrder', @U_ListRoot, 0, NULL, @Schema, GETDATE());
SET @U_ListQcOrder = SCOPE_IDENTITY();

------------------------------------------------------------
-- Field generation for a unit from live table columns
------------------------------------------------------------
DECLARE @Units TABLE (UnitId INT, TableName SYSNAME, IsRoot BIT);
INSERT INTO @Units VALUES
 (@U_OrderRoot, N'TchpQcOrder', 1),
 (@U_OrderSize, N'TchpQcOrderSize', 0),
 (@U_Garment, N'TchpQcGarment', 0),
 (@U_Result, N'TchpQcResult', 0),
 (@U_ListRoot, N'Plm_ReferenceBasicInfo', 1),
 (@U_ListStyleSpec, N'TchpStyleSpec', 0),
 (@U_ListQcOrder, N'TchpQcOrder', 0);

DECLARE @UnitId INT, @TableName SYSNAME, @IsRoot BIT;
DECLARE @Col SYSNAME, @DataTypeName NVARCHAR(128), @MaxLen INT, @Scale INT, @IsIdentity BIT, @Ord INT;
DECLARE @Sort INT, @Ctrl INT, @Dt INT, @Visible BIT, @IsPk BIT, @AllowEmpty BIT, @Disp NVARCHAR(200);
DECLARE @IsComputed BIT;

DECLARE u CURSOR LOCAL FAST_FORWARD FOR SELECT UnitId, TableName, IsRoot FROM @Units;
OPEN u;
FETCH NEXT FROM u INTO @UnitId, @TableName, @IsRoot;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sort = 0;
    DECLARE col CURSOR LOCAL FAST_FORWARD FOR
    SELECT c.name,
           t.name,
           c.max_length,
           c.scale,
           c.is_identity,
           c.column_id,
           c.is_computed
    FROM sys.columns c
    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(QUOTENAME(@Schema) + N'.' + QUOTENAME(@TableName))
    ORDER BY c.column_id;

    OPEN col;
    FETCH NEXT FROM col INTO @Col, @DataTypeName, @MaxLen, @Scale, @IsIdentity, @Ord, @IsComputed;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Sort = @Sort + 10;
        SET @IsPk = CASE WHEN EXISTS (
            SELECT 1 FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            INNER JOIN sys.columns pkc ON pkc.object_id = ic.object_id AND pkc.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID(QUOTENAME(@Schema) + N'.' + QUOTENAME(@TableName))
              AND i.is_primary_key = 1 AND pkc.name = @Col) THEN 1 ELSE 0 END;

        SET @Visible = 1;
        IF @IsPk = 1 SET @Visible = 0;
        IF @Col IN (N'SystemTimeStamp', N'AppCreatedById', N'AppCreatedDate', N'AppModifiedDate',
                    N'AppModifiedById', N'AppCreatedByCompanyId', N'AppCreatedByID', N'AppModifiedByID',
                    N'AppCreatedByCompanyID') SET @Visible = 0;
        -- Parent FK columns: hide on child grids (wired below)
        IF @IsRoot = 0 AND @Col IN (N'QcOrderId', N'QcGarmentId') SET @Visible = 0;

        SET @Ctrl = 2; -- TextBox
        SET @Dt = NULL;
        IF @DataTypeName IN (N'decimal', N'numeric', N'float', N'real', N'money') BEGIN SET @Ctrl = 20; SET @Dt = 3; END
        IF @DataTypeName IN (N'datetime', N'date', N'datetime2', N'smalldatetime') BEGIN SET @Ctrl = 7; SET @Dt = 4; END
        IF @DataTypeName IN (N'nvarchar', N'varchar', N'ntext', N'text') AND (@MaxLen < 0 OR @MaxLen > 500) SET @Ctrl = 4; -- Memo
        IF @IsComputed = 1 SET @Ctrl = 20; -- computed numeric diffs

        SET @AllowEmpty = CASE WHEN @IsPk = 1 OR @IsIdentity = 1 THEN 0 ELSE 1 END;
        SET @Disp = REPLACE(@Col, N'_', N' ');

        INSERT INTO dbo.AppTransactionField (
            TransactionUnitID, DisplayName, DataBaseFieldName, ControlType, DataType,
            SortOrder, MaxCharLegnth, NBDecimal, IsPrimaryKey, IsLinkToParentPrimaryKey,
            IsVisible, IsAllowEmpty, IsReadonly, DataRetrieveType, RowIdentityGuid, AppCreatedDate)
        VALUES (
            @UnitId, @Disp, @Col, @Ctrl, @Dt,
            @Sort,
            CASE WHEN @DataTypeName LIKE N'%char%' AND @MaxLen > 0 THEN @MaxLen / CASE WHEN @DataTypeName LIKE N'n%' THEN 2 ELSE 1 END ELSE NULL END,
            CASE WHEN @Ctrl = 20 THEN ISNULL(@Scale, 3) ELSE NULL END,
            @IsPk, 0,
            @Visible, @AllowEmpty,
            CASE WHEN @IsComputed = 1 THEN 1 ELSE 0 END,
            1, -- DataRetrieveType = RelationalTable (required for cascading / DDL filter)
            NEWID(), GETDATE());

        FETCH NEXT FROM col INTO @Col, @DataTypeName, @MaxLen, @Scale, @IsIdentity, @Ord, @IsComputed;
    END
    CLOSE col; DEALLOCATE col;

    FETCH NEXT FROM u INTO @UnitId, @TableName, @IsRoot;
END
CLOSE u; DEALLOCATE u;

------------------------------------------------------------
-- Wire parent links (FK → parent PK field)
------------------------------------------------------------
DECLARE @F_Order_QcOrderId INT = (
    SELECT TransactionFieldID FROM dbo.AppTransactionField
    WHERE TransactionUnitID = @U_OrderRoot AND DataBaseFieldName = N'QcOrderId');
DECLARE @F_Garment_QcGarmentId INT = (
    SELECT TransactionFieldID FROM dbo.AppTransactionField
    WHERE TransactionUnitID = @U_Garment AND DataBaseFieldName = N'QcGarmentId');
DECLARE @F_List_ReferenceId INT = (
    SELECT TransactionFieldID FROM dbo.AppTransactionField
    WHERE TransactionUnitID = @U_ListRoot AND DataBaseFieldName = N'ReferenceId');

-- OrderSize.QcOrderId → Order.QcOrderId
UPDATE dbo.AppTransactionField
SET IsLinkToParentPrimaryKey = 1,
    LinkToParentPrimaryKeyFieldID = @F_Order_QcOrderId,
    IsVisible = 0
WHERE TransactionUnitID = @U_OrderSize AND DataBaseFieldName = N'QcOrderId';

-- Garment.QcOrderId → Order.QcOrderId
UPDATE dbo.AppTransactionField
SET IsLinkToParentPrimaryKey = 1,
    LinkToParentPrimaryKeyFieldID = @F_Order_QcOrderId,
    IsVisible = 0
WHERE TransactionUnitID = @U_Garment AND DataBaseFieldName = N'QcOrderId';

-- Result.QcGarmentId → Garment.QcGarmentId
UPDATE dbo.AppTransactionField
SET IsLinkToParentPrimaryKey = 1,
    LinkToParentPrimaryKeyFieldID = @F_Garment_QcGarmentId,
    IsVisible = 0
WHERE TransactionUnitID = @U_Result AND DataBaseFieldName = N'QcGarmentId';

-- List sibling StyleSpec.StyleSpecId → Root.ReferenceId (L2, same as Fit/Grading)
UPDATE dbo.AppTransactionField
SET LinkToParentPrimaryKeyFieldID = @F_List_ReferenceId,
    IsVisible = 0
WHERE TransactionUnitID = @U_ListStyleSpec AND DataBaseFieldName = N'StyleSpecId';

-- List child QcOrder.StyleSpecId → Root.ReferenceId (product scoped; StyleSpecId == ReferenceId).
-- Do NOT use ProductReferenceId (column removed from TchpQcOrder).
UPDATE dbo.AppTransactionField
SET IsLinkToParentPrimaryKey = 1,
    LinkToParentPrimaryKeyFieldID = @F_List_ReferenceId,
    IsVisible = 0
WHERE TransactionUnitID = @U_ListQcOrder AND DataBaseFieldName = N'StyleSpecId';

------------------------------------------------------------
-- Link Target: List QcOrder grid → Spec Qc Order TX (popup)
------------------------------------------------------------
INSERT INTO dbo.AppFormLinkTarget (
    TransactionUnitID, LinkTargetTransactionID,
    SourceColumn1, TargetColumn1,
    NavigationActionName, ActionType,
    LinkTargetUsageType, SourceColumnType,
    IsPopup, PopupWidth, PopupHeight, Sort)
VALUES (
    @U_ListQcOrder, @TxOrderId,
    N'QcOrderId', N'QcOrderId',
    N'Open Spec Qc Order', 1,
    101, 3,
    1, 1200, 700, 10);

COMMIT TRAN;

SELECT N'Created experimental QC transactions' AS Msg,
       @TxListId AS Tx_QcOrderList_Id,
       @TxOrderId AS Tx_QcOrder_Id,
       @FormListId AS FormListId,
       @FormOrderId AS FormOrderId;

SELECT t.TransactionID, t.TransactionName, t.IntegrationId, u.TransactionUnitID, u.DataBaseTableName,
       u.UnitDisplayName, u.ParentTransactionUnitID, u.IsMasterSiblingUnit
FROM dbo.AppTransaction t
JOIN dbo.AppTransactionUnit u ON u.TransactionID = t.TransactionID
WHERE t.IntegrationId IN (N'TX_QcOrder', N'TX_QcOrderList')
ORDER BY t.TransactionID, CASE WHEN u.ParentTransactionUnitID IS NULL THEN 0 ELSE 1 END, u.TransactionUnitID;
GO
