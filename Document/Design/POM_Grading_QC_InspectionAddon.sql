-- ============================================================
-- POM_Grading_QC_InspectionAddon.sql
-- Additive QC inspection model. Does NOT ALTER existing TchpQc* tables.
-- Run after POM_Grading_QC_NewSchema.sql (or let App Config Pack create tables).
--
-- 1:1 siblings (PK = parent PK, no identity):
--   TchpQcOrderAql            ↔ TchpQcOrder.QcOrderId
--   TchpQcGarmentInspection   ↔ TchpQcGarment.QcGarmentId
-- Children:
--   TchpQcOrderCert           certificates / lab reports at order level
--   TchpQcDefectRecord        floor safety + visual defects per garment
-- Library:
--   TchpQcDefectCode          applicable codes by market + age
-- ============================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ── TchpQcDefectCode ─────────────────────────────────────────
IF OBJECT_ID(N'dbo.TchpQcDefectCode', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpQcDefectCode] (
        [DefectCode]            NVARCHAR(30)    NOT NULL,
        [DisplayName]           NVARCHAR(200)   NOT NULL,
        [DefectClass]           NVARCHAR(10)    NOT NULL,  -- CRITICAL | MAJOR | MINOR
        [InspectionTrack]       NVARCHAR(20)    NOT NULL,  -- SAFETY | VISUAL | LAB | CERT
        [AppliesMarket]         NVARCHAR(20)    NOT NULL CONSTRAINT DF_TchpQcDefectCode_Mkt DEFAULT (N'ALL'), -- ALL | US | EU
        [AppliesAge]            NVARCHAR(20)    NOT NULL CONSTRAINT DF_TchpQcDefectCode_Age DEFAULT (N'ALL'), -- ALL | ADULT | INFANT | CHILD | SLEEPWEAR
        [Regulation]            NVARCHAR(100)   NULL,
        [IsActive]              BIT             NOT NULL CONSTRAINT DF_TchpQcDefectCode_Active DEFAULT (1),
        CONSTRAINT [PK_TchpQcDefectCode] PRIMARY KEY CLUSTERED ([DefectCode] ASC)
    );
    PRINT 'Created TchpQcDefectCode';
END
ELSE
    PRINT 'TchpQcDefectCode already exists — skipped';
GO

-- ── TchpQcOrderAql (sibling of TchpQcOrder) ──────────────────
IF OBJECT_ID(N'dbo.TchpQcOrderAql', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpQcOrderAql] (
        [QcOrderId]             INT             NOT NULL,  -- PK = TchpQcOrder.QcOrderId
        [TargetMarket]          NVARCHAR(20)    NULL,      -- DDL QcTargetMarket stores InternalKey; Code = US|EU|US_EU|OTHER
        [AgeCategory]           NVARCHAR(20)    NULL,      -- DDL QcAgeCategory stores InternalKey; Code = ADULT|INFANT|CHILD|SLEEPWEAR
        [AcCritical]            INT             NOT NULL CONSTRAINT DF_TchpQcOrderAql_AcC DEFAULT (0),
        [AcMajor]               INT             NOT NULL CONSTRAINT DF_TchpQcOrderAql_AcM DEFAULT (0),
        [AcMinor]               INT             NOT NULL CONSTRAINT DF_TchpQcOrderAql_AcN DEFAULT (0),
        [FailCriticalCount]     INT             NOT NULL CONSTRAINT DF_TchpQcOrderAql_Fc DEFAULT (0),
        [FailMajorCount]        INT             NOT NULL CONSTRAINT DF_TchpQcOrderAql_Fm DEFAULT (0),
        [FailMinorCount]        INT             NOT NULL CONSTRAINT DF_TchpQcOrderAql_Fn DEFAULT (0),
        [SampledCount]          INT             NOT NULL CONSTRAINT DF_TchpQcOrderAql_Sc DEFAULT (0),
        [CompletedCount]        INT             NOT NULL CONSTRAINT DF_TchpQcOrderAql_Cc DEFAULT (0),
        [CertFail]              BIT             NULL,
        [AqlPassStatus]         NVARCHAR(20)    NULL,      -- PENDING | PASSED | FAILED
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        CONSTRAINT [PK_TchpQcOrderAql] PRIMARY KEY CLUSTERED ([QcOrderId] ASC),
        CONSTRAINT [FK_TchpQcOrderAql_TchpQcOrder]
            FOREIGN KEY ([QcOrderId]) REFERENCES [dbo].[TchpQcOrder] ([QcOrderId])
    );
    PRINT 'Created TchpQcOrderAql';
END
ELSE
    PRINT 'TchpQcOrderAql already exists — skipped';
GO

-- ── TchpQcOrderCert ──────────────────────────────────────────
IF OBJECT_ID(N'dbo.TchpQcOrderCert', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpQcOrderCert] (
        [QcOrderCertId]         INT             IDENTITY(1,1)   NOT NULL,
        [QcOrderId]             INT             NOT NULL,
        [CertType]              NVARCHAR(30)    NOT NULL,  -- DDL QcCertType stores InternalKey; Code = CPC|GPSR_RP|OEKOTEX|AFIRM|FLAM_1610|FLAM_1615
        [Result]                NVARCHAR(20)    NOT NULL CONSTRAINT DF_TchpQcOrderCert_Result DEFAULT (N'PENDING'), -- DDL QcCertResult stores InternalKey; Code = PASS|FAIL|NA|MISSING|PENDING
        [IsRequired]            BIT             NOT NULL CONSTRAINT DF_TchpQcOrderCert_Req DEFAULT (0),
        [DocumentRef]           NVARCHAR(100)   NULL,
        [Notes]                 NVARCHAR(500)   NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        CONSTRAINT [PK_TchpQcOrderCert] PRIMARY KEY CLUSTERED ([QcOrderCertId] ASC),
        CONSTRAINT [FK_TchpQcOrderCert_TchpQcOrder]
            FOREIGN KEY ([QcOrderId]) REFERENCES [dbo].[TchpQcOrder] ([QcOrderId]),
        CONSTRAINT [UQ_TchpQcOrderCert_OrderType]
            UNIQUE ([QcOrderId], [CertType])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpQcOrderCert_Order]
        ON [dbo].[TchpQcOrderCert] ([QcOrderId] ASC);
    PRINT 'Created TchpQcOrderCert';
END
ELSE
    PRINT 'TchpQcOrderCert already exists — skipped';
GO

-- ── TchpQcGarmentInspection (sibling of TchpQcGarment) ───────
IF OBJECT_ID(N'dbo.TchpQcGarmentInspection', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpQcGarmentInspection] (
        [QcGarmentId]           INT             NOT NULL,  -- PK = TchpQcGarment.QcGarmentId
        [InspectionStatus]      NVARCHAR(20)    NOT NULL CONSTRAINT DF_TchpQcGarmentInsp_St DEFAULT (N'OPEN'),
            -- OPEN | STOPPED_CRITICAL | COMPLETE
        [CriticalFail]          BIT             NULL,
        [MajorFail]             BIT             NULL,
        [MinorFail]             BIT             NULL,
        [StopReason]            NVARCHAR(200)   NULL,
        [InspectedAt]           DATETIME        NULL,
        [InspectorId]           INT             NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        CONSTRAINT [PK_TchpQcGarmentInspection] PRIMARY KEY CLUSTERED ([QcGarmentId] ASC),
        CONSTRAINT [FK_TchpQcGarmentInspection_TchpQcGarment]
            FOREIGN KEY ([QcGarmentId]) REFERENCES [dbo].[TchpQcGarment] ([QcGarmentId])
    );
    PRINT 'Created TchpQcGarmentInspection';
END
ELSE
    PRINT 'TchpQcGarmentInspection already exists — skipped';
GO

-- ── TchpQcDefectRecord ───────────────────────────────────────
IF OBJECT_ID(N'dbo.TchpQcDefectRecord', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpQcDefectRecord] (
        [QcDefectRecordId]      INT             IDENTITY(1,1)   NOT NULL,
        [QcGarmentId]           INT             NOT NULL,
        [DefectClass]           NVARCHAR(10)    NULL,      -- filled from catalog on recalc
        [DefectCode]            NVARCHAR(30)    NOT NULL,
        [DefectLocation]        NVARCHAR(50)    NULL,
        [Description]           NVARCHAR(500)   NULL,
        [Quantity]              INT             NOT NULL CONSTRAINT DF_TchpQcDefectRecord_Qty DEFAULT (1),
        [InspectionTrack]       NVARCHAR(20)    NULL,      -- SAFETY | VISUAL
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        CONSTRAINT [PK_TchpQcDefectRecord] PRIMARY KEY CLUSTERED ([QcDefectRecordId] ASC),
        CONSTRAINT [FK_TchpQcDefectRecord_TchpQcGarment]
            FOREIGN KEY ([QcGarmentId]) REFERENCES [dbo].[TchpQcGarment] ([QcGarmentId])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpQcDefectRecord_Garment]
        ON [dbo].[TchpQcDefectRecord] ([QcGarmentId] ASC);
    PRINT 'Created TchpQcDefectRecord';
END
ELSE
    PRINT 'TchpQcDefectRecord already exists — skipped';
GO

-- Auto-create 1:1 sibling rows was removed: the form save already INSERTs
-- TchpQcOrderAql / TchpQcGarmentInspection. AFTER INSERT triggers raced that
-- insert and caused PK_TchpQcOrderAql / PK_TchpQcGarmentInspection violations.
-- Recalc / Seed commands still INSERT the sibling when missing.
IF OBJECT_ID(N'dbo.trg_TchpQcOrder_EnsureAql', N'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_TchpQcOrder_EnsureAql];
GO
IF OBJECT_ID(N'dbo.trg_TchpQcGarment_EnsureInspection', N'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_TchpQcGarment_EnsureInspection];
GO

-- ── Views ────────────────────────────────────────────────────
CREATE OR ALTER VIEW [dbo].[View_TchpQcOrderDefectSummary]
AS
SELECT
    CAST(g.QcOrderId AS BIGINT) * 1000000000 + CAST(d.QcDefectRecordId AS BIGINT) AS QcOrderDefectId,
    g.QcOrderId,
    d.QcDefectRecordId,
    g.QcGarmentId,
    g.GarmentSerial,
    d.DefectClass,
    d.DefectCode,
    d.DefectLocation,
    d.Quantity,
    d.Description,
    d.InspectionTrack
FROM dbo.TchpQcDefectRecord AS d
INNER JOIN dbo.TchpQcGarment AS g
    ON g.QcGarmentId = d.QcGarmentId;
GO

CREATE OR ALTER VIEW [dbo].[View_TchpProductQcSummary]
AS
SELECT
    ss.StyleSpecId,
    COUNT(o.QcOrderId) AS OrderCount,
    SUM(CASE WHEN o.OrderStatus = N'PASSED' THEN 1 ELSE 0 END) AS PassedOrderCount,
    SUM(CASE WHEN o.OrderStatus = N'FAILED' THEN 1 ELSE 0 END) AS FailedOrderCount,
    SUM(CASE WHEN o.OrderStatus IN (N'OPEN', N'IN_PROGRESS') THEN 1 ELSE 0 END) AS OpenOrderCount,
    SUM(ISNULL(a.FailCriticalCount, 0)) AS FailCriticalCount,
    SUM(ISNULL(a.FailMajorCount, 0)) AS FailMajorCount,
    SUM(ISNULL(a.FailMinorCount, 0)) AS FailMinorCount
FROM dbo.TchpStyleSpec AS ss
LEFT JOIN dbo.TchpQcOrder AS o
    ON o.StyleSpecId = ss.StyleSpecId
LEFT JOIN dbo.TchpQcOrderAql AS a
    ON a.QcOrderId = o.QcOrderId
GROUP BY ss.StyleSpecId;
GO

-- ── Seed defect catalog ──────────────────────────────────────
MERGE dbo.TchpQcDefectCode AS t
USING (VALUES
    (N'CRIT-NEEDLE',   N'Broken needle fragment', N'CRITICAL', N'SAFETY', N'ALL', N'ALL', N'Industry'),
    (N'CRIT-FLAM-01',  N'Fabric Class 3 flammability (lab)', N'CRITICAL', N'LAB', N'US', N'ALL', N'16 CFR 1610'),
    (N'CRIT-FLAM-02',  N'Children sleepwear char length fail (lab)', N'CRITICAL', N'LAB', N'US', N'SLEEPWEAR', N'16 CFR 1615/1616'),
    (N'CRIT-FLAM-03',  N'Sleepwear missing FITS SNUGLY label', N'CRITICAL', N'SAFETY', N'US', N'SLEEPWEAR', N'16 CFR 1615/1616'),
    (N'CRIT-DRAW-01',  N'Drawstring in hood/neck 2T-12', N'CRITICAL', N'SAFETY', N'US', N'CHILD', N'ASTM F1816'),
    (N'CRIT-DRAW-02',  N'Waist/hem drawstring protrusion > 3 in', N'CRITICAL', N'SAFETY', N'US', N'CHILD', N'ASTM F1816'),
    (N'CRIT-DRAW-03',  N'Drawstring toggle/knot/attachment', N'CRITICAL', N'SAFETY', N'US', N'CHILD', N'ASTM F1816'),
    (N'CRIT-DRAW-04',  N'Hood/neck cord ages 0-7', N'CRITICAL', N'SAFETY', N'EU', N'CHILD', N'EN 14682'),
    (N'CRIT-DRAW-05',  N'Hood/neck cord > 75 mm ages 7-14', N'CRITICAL', N'SAFETY', N'EU', N'CHILD', N'EN 14682'),
    (N'CRIT-DRAW-06',  N'Belt/sash protrusion > 140 mm', N'CRITICAL', N'SAFETY', N'EU', N'CHILD', N'EN 14682'),
    (N'CRIT-SMALL-01', N'Detachable decoration is small part', N'CRITICAL', N'SAFETY', N'US', N'INFANT', N'16 CFR 1501'),
    (N'CRIT-SMALL-02', N'Toy component small part', N'CRITICAL', N'SAFETY', N'EU', N'INFANT', N'EN 71-1'),
    (N'CRIT-SHARP-01', N'Accessible sharp point or edge', N'CRITICAL', N'SAFETY', N'ALL', N'CHILD', N'ASTM F963 / EN 71-1'),
    (N'CRIT-SHARP-02', N'Component breaks into sharp/small part', N'CRITICAL', N'SAFETY', N'ALL', N'CHILD', N'ASTM F963 / EN 71-1'),
    (N'MAJ-SEAM-01',   N'Open seam / broken seam', N'MAJOR', N'VISUAL', N'ALL', N'ALL', N'Industry'),
    (N'MAJ-STITCH-01', N'Broken / skipped stitch (structural)', N'MAJOR', N'VISUAL', N'ALL', N'ALL', N'Industry'),
    (N'MAJ-LABEL-01',  N'Wrong size or care label', N'MAJOR', N'VISUAL', N'ALL', N'ALL', N'Industry'),
    (N'MAJ-HW-01',     N'Functional hardware failure (non-safety)', N'MAJOR', N'VISUAL', N'ALL', N'ALL', N'Industry'),
    (N'MIN-THREAD-01', N'Loose thread / untrimmed end', N'MINOR', N'VISUAL', N'ALL', N'ALL', N'Industry'),
    (N'MIN-PILL-01',   N'Pilling', N'MINOR', N'VISUAL', N'ALL', N'ALL', N'Industry'),
    (N'MIN-COLOR-01',  N'Shade variation / color banding', N'MINOR', N'VISUAL', N'ALL', N'ALL', N'Industry'),
    (N'MIN-LABEL-02',  N'Crooked / misaligned label', N'MINOR', N'VISUAL', N'ALL', N'ALL', N'Industry'),
    (N'MIN-PRINT-01',  N'Print misregistration', N'MINOR', N'VISUAL', N'ALL', N'ALL', N'Industry'),
    (N'MIN-STITCH-02', N'Uneven stitching (cosmetic)', N'MINOR', N'VISUAL', N'ALL', N'ALL', N'Industry')
) AS s (DefectCode, DisplayName, DefectClass, InspectionTrack, AppliesMarket, AppliesAge, Regulation)
ON t.DefectCode = s.DefectCode
WHEN MATCHED THEN
    UPDATE SET DisplayName = s.DisplayName, DefectClass = s.DefectClass, InspectionTrack = s.InspectionTrack,
               AppliesMarket = s.AppliesMarket, AppliesAge = s.AppliesAge, Regulation = s.Regulation, IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (DefectCode, DisplayName, DefectClass, InspectionTrack, AppliesMarket, AppliesAge, Regulation, IsActive)
    VALUES (s.DefectCode, s.DisplayName, s.DefectClass, s.InspectionTrack, s.AppliesMarket, s.AppliesAge, s.Regulation, 1);
GO

PRINT '=== POM_Grading_QC_InspectionAddon.sql completed ===';
GO
