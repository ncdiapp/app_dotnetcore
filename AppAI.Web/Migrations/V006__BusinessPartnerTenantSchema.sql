-- ============================================================
-- V006: Move AppBusinessPartner tables to tenant DB
--
-- These three tables are CRM / invitation data scoped to one
-- tenant. They were originally in AppMasterDB as a workaround.
-- After this migration the BL uses GetTenantAdapter() instead
-- of AppMasterDBConnectionString.
-- ============================================================

-- ── AppBusinessPartner ───────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AppBusinessPartner'
)
BEGIN
    CREATE TABLE dbo.AppBusinessPartner (
        AppBusinessPartnerId                     INT            NOT NULL IDENTITY(1,1),
        AppCompanyId                             INT            NULL,
        Code                                     NVARCHAR(50)   NULL,
        ShortName                                NVARCHAR(200)  NULL,
        FullName                                 NVARCHAR(500)  NULL,
        Adress1                                  NVARCHAR(200)  NULL,
        Adress2                                  NVARCHAR(200)  NULL,
        Adress3                                  NVARCHAR(200)  NULL,
        City                                     NVARCHAR(50)   NULL,
        Language                                 NVARCHAR(50)   NULL,
        State                                    NVARCHAR(50)   NULL,
        PostCode                                 NVARCHAR(20)   NULL,
        Country                                  NVARCHAR(20)   NULL,
        Status                                   NVARCHAR(10)   NULL,
        CurrencyCode                             NVARCHAR(10)   NULL,
        ContactPhone                             NVARCHAR(20)   NULL,
        ContactName                              NVARCHAR(20)   NULL,
        ContactFax                               NVARCHAR(20)   NULL,
        PartnerType                              INT            NULL,
        ShipToId                                 INT            NULL,
        BillToId                                 INT            NULL,
        IsBillToSameShipTo                       BIT            NULL,
        AppCreatedById                           INT            NULL,
        AppCreatedDate                           DATETIME       NULL,
        AppModifiedDate                          DATETIME       NULL,
        AppModifiedById                          INT            NULL,
        AppCreatedByCompanyId                    INT            NULL,
        MappingExternalBusinessPartnerAccountId  NVARCHAR(50)   NULL,
        CONSTRAINT PK_AppBusinessPartner PRIMARY KEY (AppBusinessPartnerId)
    );
END
GO

-- ── AppBusinessPartnerInviteUser ─────────────────────────────
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
        CONSTRAINT PK_AppBusinessPartnerInviteUser PRIMARY KEY (ParternerInvitedUserId),
        CONSTRAINT FK_AppBPInviteUser_AppBP FOREIGN KEY (AppBusinessPartnerId)
            REFERENCES dbo.AppBusinessPartner (AppBusinessPartnerId)
    );

    CREATE INDEX IX_AppBPInviteUser_UserId
        ON dbo.AppBusinessPartnerInviteUser (UserId);
    CREATE INDEX IX_AppBPInviteUser_CreatedByCompany
        ON dbo.AppBusinessPartnerInviteUser (AppCreatedByCompanyId);
END
GO

-- ── AppBusinessPartnerInviteUserChildUser ────────────────────
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

-- ── Data migration note ──────────────────────────────────────
-- Existing rows in AppMasterDB must be copied to each tenant DB
-- before dropping from master. Use the companion script:
-- MasterDB_DropBusinessPartner.sql (run AFTER data migration).
-- ============================================================
