-- ============================================================
-- V010: Move AppBusinessPartnerInviteUser back to MasterDB
--
-- V006 incorrectly placed AppBusinessPartnerInviteUser in the
-- tenant DB alongside AppBusinessPartner (CRM data). This table
-- is identity/ACL data — it records cross-company user roles and
-- must be readable before any tenant DB context is established.
--
-- Run this script against AppMasterDB.
-- AppBusinessPartner (also created by V006) is correct in
-- tenant DB and is NOT affected by this migration.
-- ============================================================

-- ── Step 1: Create in AppMasterDB ────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AppBusinessPartnerInviteUser'
)
BEGIN
    CREATE TABLE dbo.AppBusinessPartnerInviteUser (
        ParternerInvitedUserId   INT      NOT NULL IDENTITY(1,1),
        AppBusinessPartnerId     INT      NULL,
        UserId                   INT      NULL,
        AppCreatedById           INT      NULL,
        AppCreatedDate           DATETIME NULL,
        AppModifiedDate          DATETIME NULL,
        AppModifiedById          INT      NULL,
        AppCompanyId             INT      NULL,
        AppCreatedByCompanyId    INT      NULL,
        EmInvitedUserType        INT      NULL,
        CONSTRAINT PK_AppBusinessPartnerInviteUser PRIMARY KEY (ParternerInvitedUserId)
        -- No physical FK to AppBusinessPartner — cross-DB FK not enforceable; logical reference only
    );

    CREATE INDEX IX_AppBPInviteUser_UserId
        ON dbo.AppBusinessPartnerInviteUser (UserId);
    CREATE INDEX IX_AppBPInviteUser_CreatedByCompany
        ON dbo.AppBusinessPartnerInviteUser (AppCreatedByCompanyId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AppBusinessPartnerInviteUserChildUser'
)
BEGIN
    CREATE TABLE dbo.AppBusinessPartnerInviteUserChildUser (
        Id                       INT      NOT NULL IDENTITY(1,1),
        ParternerInvitedUserId   INT      NULL,
        ChildUserId              INT      NULL,
        AppCreatedDate           DATETIME NULL,
        CONSTRAINT PK_AppBPInviteUserChildUser PRIMARY KEY (Id),
        CONSTRAINT FK_AppBPInviteChildUser_InviteUser FOREIGN KEY (ParternerInvitedUserId)
            REFERENCES dbo.AppBusinessPartnerInviteUser (ParternerInvitedUserId)
    );
END
GO

-- ── Step 2: Copy rows from each tenant DB ────────────────────
-- Run the INSERT below once per tenant, substituting the catalog name.
-- Verify row counts match before proceeding to Step 3.
--
-- Example for tenant catalog [TenantDB_ACME]:
--
-- INSERT INTO AppMasterDB.dbo.AppBusinessPartnerInviteUser
--     (AppBusinessPartnerId, UserId, AppCreatedById, AppCreatedDate, AppModifiedDate,
--      AppModifiedById, AppCompanyId, AppCreatedByCompanyId, EmInvitedUserType)
-- SELECT AppBusinessPartnerId, UserId, AppCreatedById, AppCreatedDate, AppModifiedDate,
--        AppModifiedById, AppCompanyId, AppCreatedByCompanyId, EmInvitedUserType
-- FROM [TenantDB_ACME].dbo.AppBusinessPartnerInviteUser;
--
-- INSERT INTO AppMasterDB.dbo.AppBusinessPartnerInviteUserChildUser
--     (ParternerInvitedUserId, ChildUserId, AppCreatedDate)
-- SELECT ParternerInvitedUserId, ChildUserId, AppCreatedDate
-- FROM [TenantDB_ACME].dbo.AppBusinessPartnerInviteUserChildUser;
--
-- Run via AppTenantProvisioningBL.RunMigrationsOnAllTenants() or SSMS per tenant.

-- ── Step 3: Drop from tenant DB ──────────────────────────────
-- After verifying data copy is complete, run per tenant:
--
-- DROP TABLE [TenantDB_ACME].dbo.AppBusinessPartnerInviteUserChildUser;
-- DROP TABLE [TenantDB_ACME].dbo.AppBusinessPartnerInviteUser;
