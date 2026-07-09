-- =============================================================================
-- Phase A probe: APP tenant DB — transactions, data sources, existing searches.
-- Used for LinkTarget resolution and Phase D integration hints.
-- =============================================================================

SET NOCOUNT ON;

DECLARE @TablePrefix NVARCHAR(32) = N'Plm_';

PRINT '=== Data sources (AppDataSourceRegister) ===';
SELECT
    DataSourceRegisterID,
    Name,
    Description,
    IsDefault
FROM dbo.AppDataSourceRegister
ORDER BY DataSourceRegisterID;

PRINT '=== PLM transactions (IntegrationId like Tab_%) ===';
SELECT
    t.TransactionID,
    t.TransactionName,
    t.IntegrationId,
    t.SaasApplicationID,
    u.DataBaseTableName AS RootTableName
FROM dbo.AppTransaction t
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionID = t.TransactionID AND u.ParentTransactionUnitID IS NULL
WHERE t.IntegrationId LIKE N'Tab[_]%'
   OR u.DataBaseTableName LIKE @TablePrefix + N'%'
ORDER BY t.IntegrationId;

PRINT '=== Existing searches (IntegrationId like Search_%) ===';
SELECT
    s.SearchID,
    s.Name,
    s.IntegrationId,
    s.Type,
    s.DataSetId,
    s.SearchViewId,
    s.SaasApplicationID
FROM dbo.AppSearch s
WHERE s.IntegrationId LIKE N'Search[_]%'
ORDER BY s.IntegrationId;

PRINT '=== Entity info sample (for EntityId resolution check) ===';
SELECT TOP 50
    e.EntityInfoID,
    e.IntegrationId,
    e.Name,
    e.EntityType
FROM dbo.AppEntityInfo e
ORDER BY e.EntityInfoID;

PRINT '=== Search context probe complete ===';
