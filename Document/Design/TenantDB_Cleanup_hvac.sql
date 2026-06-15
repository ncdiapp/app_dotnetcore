-- ============================================================
-- hvac Tenant DB Cleanup Script
-- Removes master-only tables and junk/test tables from hvac.
-- Leaves only tenant app-definition and business data tables.
-- ============================================================

USE hvac;
GO

DECLARE @dropTables TABLE (TableName NVARCHAR(200));

-- ── Master-only tables (live in AppMasterDB, not tenant DB) ──
INSERT INTO @dropTables VALUES
    ('AppAISkill'),
    ('AppAISkillRef'),
    ('AppBackupLog'),
    ('AppBatchLog'),
    ('AppBuilderAgentSession'),
    ('AppBusinessPartner'),
    ('AppBusinessPartnerInviteUser'),
    ('AppBusinessPartnerInviteUserChildUser'),
    ('AppCompany'),
    ('AppCompanyOrderModule'),
    ('AppCompanyUserTypeRegister'),
    ('AppCountry'),
    ('AppCountryRegion'),
    ('AppCurrency'),
    ('AppDataSourceRegister'),
    ('AppLanguage'),
    ('AppLanguageKey'),
    ('AppLogTrack'),
    ('AppModuleLibRegister'),
    ('AppSecurityAuthticationInfo'),
    ('AppSecurityLoginAuditor'),
    ('AppSecurityRegDomain'),
    ('AppSecurityRegDomainORG'),
    ('AppSecurityUser'),
    ('AppSecurityUserContact'),
    ('AppSecurityUserInvitation'),
    ('AppSecurityUserSession'),
    ('AppSetup'),
    ('AppTimeZoneAbbreviation');

-- ── Junk / test / demo tables ──
INSERT INTO @dropTables VALUES
    ('a01Test1'),
    ('AAATEST'),
    ('AAATest2'),
    ('aaatest3'),
    ('aNewUnitName'),
    ('AppDatabaseDiagram'),
    ('AppDatabaseDiagramItem'),
    ('c1_MP6'),
    ('CustomerOr_Customers'),
    ('CustomerOr_OrderItems'),
    ('CustomerOr_Orders'),
    ('HelpdeskTi_TicketCategories'),
    ('HelpdeskTi_TicketComments'),
    ('HelpdeskTi_Tickets'),
    ('HelpdeskTi_Users'),
    ('HVAC_OrderItemOption'),
    ('HVAC_UserInfoExtra'),
    ('HvacBlog'),
    ('HvacBrand'),
    ('HvacCaseStudy'),
    ('HvacCategory1'),
    ('HvacCategory2'),
    ('HvacCategory3'),
    ('HvacClientMessage'),
    ('HvacClientUser'),
    ('HvacInvoice'),
    ('HvacInvoiceItem'),
    ('HvacOrder'),
    ('HvacOrderDetail'),
    ('HvacProduct'),
    ('HvacService'),
    ('HvacShoppingCart'),
    ('HvacTopCatalogGroup'),
    ('HvacTopCatalogItem'),
    ('ImportExcelStaging_EWZR68_c1_xls'),
    ('ImportExcelStaging_FVDW97_c3_csv'),
    ('ImportExcelStaging_IEGT81_c1_xls'),
    ('ImportExcelStaging_IPLC15_c1_xls'),
    ('ImportExcelStaging_IPLC15_c1_xlsACRE99'),
    ('ImportExcelStaging_IPLC15_c1_xlsOZES78'),
    ('ImportExcelStaging_YMNT86_c1_xls'),
    ('ImportExcelStaging_YOFL75_c1_xls'),
    ('ImportExcelStaging_YSFX07_c1_xls'),
    ('importtest004c3'),
    ('InventoryM_Categories'),
    ('InventoryM_InventoryM_Categories'),
    ('InventoryM_InventoryM_Products'),
    ('InventoryM_InventoryM_StockLevels'),
    ('InventoryM_InventoryM_Suppliers'),
    ('InventoryM_Products'),
    ('InventoryM_StockLevels'),
    ('InventoryM_Suppliers'),
    ('JsonImport___location7'),
    ('Order8'),
    ('OrderDetails8'),
    ('sysdiagrams'),
    ('Test1'),
    ('TestExcelImport1'),
    ('TestHu103O_Customers'),
    ('TestHu103O_OrderItems'),
    ('TestHu103O_Orders'),
    ('testImport002c1'),
    ('testimport003c1'),
    ('weather___current'),
    ('weather___current2'),
    ('weather___current4'),
    ('weather___current5'),
    ('weather___current6'),
    ('weather___current7'),
    ('weather___location'),
    ('weather___location1'),
    ('weather___location2'),
    ('weather___location4'),
    ('weather___location5'),
    ('weather___location6'),
    ('weather___location7');

-- ── Step 1: Drop all FK constraints where child OR parent is being removed ──
PRINT '=== Step 1: Dropping FK constraints ===';

DECLARE @fkName  NVARCHAR(200);
DECLARE @fkTable NVARCHAR(200);
DECLARE fk_cursor CURSOR FAST_FORWARD FOR
    SELECT fk.name, OBJECT_NAME(fk.parent_object_id)
    FROM   sys.foreign_keys fk
    WHERE  OBJECT_NAME(fk.parent_object_id)    IN (SELECT TableName FROM @dropTables)
        OR OBJECT_NAME(fk.referenced_object_id) IN (SELECT TableName FROM @dropTables);

OPEN fk_cursor;
FETCH NEXT FROM fk_cursor INTO @fkName, @fkTable;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @dropFk NVARCHAR(500) =
        N'ALTER TABLE [dbo].[' + @fkTable + N'] DROP CONSTRAINT [' + @fkName + N'];';
    PRINT @dropFk;
    EXEC sp_executesql @dropFk;
    FETCH NEXT FROM fk_cursor INTO @fkName, @fkTable;
END
CLOSE fk_cursor;
DEALLOCATE fk_cursor;

-- ── Step 2: Drop the tables ──
PRINT '=== Step 2: Dropping tables ===';

DECLARE @tbl NVARCHAR(200);
DECLARE tbl_cursor CURSOR FAST_FORWARD FOR
    SELECT TableName FROM @dropTables
    WHERE  OBJECT_ID(N'[dbo].[' + TableName + N']', N'U') IS NOT NULL
    ORDER  BY TableName;

OPEN tbl_cursor;
FETCH NEXT FROM tbl_cursor INTO @tbl;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @dropTbl NVARCHAR(500) =
        N'DROP TABLE [dbo].[' + @tbl + N'];';
    PRINT @dropTbl;
    EXEC sp_executesql @dropTbl;
    FETCH NEXT FROM tbl_cursor INTO @tbl;
END
CLOSE tbl_cursor;
DEALLOCATE tbl_cursor;

PRINT '=== Cleanup complete ===';

-- ── Verify: list remaining tables ──
SELECT TABLE_NAME AS RemainingTables
FROM   INFORMATION_SCHEMA.TABLES
WHERE  TABLE_TYPE = 'BASE TABLE'
ORDER  BY TABLE_NAME;
GO
