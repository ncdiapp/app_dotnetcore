-- ============================================================
-- POM_Grading_QC_NewSchema.sql
-- Run against the tenant database via SSMS.
-- Purpose: New tables for POM/Grading/Fit Iteration/QC redesign.
--
-- New tables replacing legacy tbl/V2k tables:
--   TchpSizeRun               Replaces tblSizeRun
--   TchpSizeRunSize           Replaces tblSizeRunRotate (PK: SizeRunSizeId)
--   TchpBodyPart              Replaces PdmV2kBodyPart
--   TchpPomTemplate           Replaces PdmV2kBodyType
--   TchpPomTemplatePart       Replaces PdmV2kBodyTypeDetail
--
-- New domain tables (14 total):
--   TchpGradeRuleSet          Grade rule library header
--   TchpGradeRule             Grade rules per body-part code within a set
--   TchpStyleSpec             Style specification aggregate root
--   TchpStyleSpecDimension    Dimensions active for a spec; tracks selected dimension
--   TchpPomSpecLine           One POM line per spec
--   TchpGradeValue            Per-size delta per POM spec line
--   TchpFitRound              Fit iteration round
--   TchpFitMeasurement        Actual measurements per fit round
--   TchpQcOrder               QC order aggregate root
--   TchpQcOrderSize           Selected sizes for a QC order
--   TchpQcGarment             Individual sampled garment (one SizeRunSizeId)
--   TchpQcResult              QC measurement result per garment/POM (SizeRunSizeId snapshot)
--   TchpSizeRunDimension      Global mapping: size run size → dimension code
--   TchpSizeSystemMapping     Multi-region size equivalence (US/EU/UK/JP)
--
-- Views (read-only / pivot domains):
--   View_TchpStyleActiveSizeRunSizes   Grading pivot column domain (V1)
--   View_TchpSizeRunSize_DefaultDimension
--   View_TchpFitMeasurementByPom       Fit SUMMARY POM×Round pivot (F3)
--   View_TchpQcOrderAvailableSize      QC Order Available Select source
--   View_TchpQcOrderPom                QC Order results host (one row per POM)
--   View_TchpQcOrderPomSizeResult      QC Order results grandchild (POM×Size)
--
-- Inspection add-on (new tables only — never ALTER TchpQcOrder/Garment/Result):
--   Run POM_Grading_QC_InspectionAddon.sql after this script (or rely on App Config Pack DDL).
-- ============================================================
-- Required for TchpQcResult persisted computed columns (run with -I in sqlcmd or SSMS defaults).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ── TchpSizeRun ─────────────────────────────────────────────
-- Named size range (e.g. SCHOOL GIRLS TOPS, WOMEN'S MISSES).
-- Replaces legacy tblSizeRun.
IF OBJECT_ID(N'dbo.TchpSizeRun', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpSizeRun] (
        [SizeRunId]             INT             IDENTITY(1,1)   NOT NULL,
        [SizeRunCode]           NVARCHAR(50)    NOT NULL,
        [SizeRunName]           NVARCHAR(100)   NOT NULL,
        [IsActive]              BIT             NOT NULL CONSTRAINT DF_TchpSizeRun_IsActive DEFAULT (1),
        -- standard audit columns --
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpSizeRun] PRIMARY KEY CLUSTERED ([SizeRunId] ASC),
        CONSTRAINT [UQ_TchpSizeRun_Code] UNIQUE ([SizeRunCode])
    );
    PRINT 'Created TchpSizeRun';
END
ELSE
    PRINT 'TchpSizeRun already exists — skipped';
GO

-- ── TchpSizeRunSize ──────────────────────────────────────────
-- Individual size entry within a size run (e.g. 2T, XS, M, 6X).
-- Replaces legacy tblSizeRunRotate. PK renamed SizeRunSizeId.
IF OBJECT_ID(N'dbo.TchpSizeRunSize', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpSizeRunSize] (
        [SizeRunSizeId]         INT             IDENTITY(1,1)   NOT NULL,
        [SizeRunId]             INT             NOT NULL,
        [SizeLabel]             NVARCHAR(20)    NOT NULL,
        [SizeOrder]             INT             NOT NULL CONSTRAINT DF_TchpSizeRunSize_Order DEFAULT (0),
        [IsActive]              BIT             NOT NULL CONSTRAINT DF_TchpSizeRunSize_IsActive DEFAULT (1),
        -- standard audit columns --
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpSizeRunSize] PRIMARY KEY CLUSTERED ([SizeRunSizeId] ASC),
        CONSTRAINT [FK_TchpSizeRunSize_TchpSizeRun]
            FOREIGN KEY ([SizeRunId]) REFERENCES [dbo].[TchpSizeRun] ([SizeRunId]),
        CONSTRAINT [UQ_TchpSizeRunSize_RunLabel] UNIQUE ([SizeRunId], [SizeLabel])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpSizeRunSize_Run]
        ON [dbo].[TchpSizeRunSize] ([SizeRunId] ASC);
    PRINT 'Created TchpSizeRunSize';
END
ELSE
    PRINT 'TchpSizeRunSize already exists — skipped';
GO

-- ── TchpBodyPart ─────────────────────────────────────────────
-- POM body part library (Chest, Waist, etc.).
-- Replaces legacy PdmV2kBodyPart.
-- GradingMinuValue: "Minu" spelling preserved from existing DTO convention.
IF OBJECT_ID(N'dbo.TchpBodyPart', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpBodyPart] (
        [BodyPartId]            INT             IDENTITY(1,1)   NOT NULL,
        [Code]                  NVARCHAR(50)    NOT NULL,
        [BodyPartName]          NVARCHAR(100)   NOT NULL,
        [Tolerance]             DECIMAL(10,3)   NULL,
        [GradingPlusValue]      DECIMAL(10,3)   NOT NULL CONSTRAINT DF_TchpBodyPart_PlusValue DEFAULT (0),
        [GradingMinuValue]      DECIMAL(10,3)   NOT NULL CONSTRAINT DF_TchpBodyPart_MinuValue DEFAULT (0),
        [IsActive]              BIT             NOT NULL CONSTRAINT DF_TchpBodyPart_IsActive DEFAULT (1),
        -- standard audit columns --
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpBodyPart] PRIMARY KEY CLUSTERED ([BodyPartId] ASC),
        CONSTRAINT [UQ_TchpBodyPart_Code] UNIQUE ([Code])
    );
    PRINT 'Created TchpBodyPart';
END
ELSE
    PRINT 'TchpBodyPart already exists — skipped';
GO

-- ── TchpPomTemplate ──────────────────────────────────────────
-- POM template — a named collection of body parts.
-- Replaces legacy PdmV2kBodyType.
-- DefaultBaseSizeId: suggested base size for new specs (optional, not enforced).
IF OBJECT_ID(N'dbo.TchpPomTemplate', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpPomTemplate] (
        [PomTemplateId]         INT             IDENTITY(1,1)   NOT NULL,
        [TemplateCode]          NVARCHAR(50)    NOT NULL,
        [TemplateName]          NVARCHAR(100)   NOT NULL,
        [DefaultBaseSizeId]     INT             NULL,
        [IsActive]              BIT             NOT NULL CONSTRAINT DF_TchpPomTemplate_IsActive DEFAULT (1),
        -- standard audit columns --
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpPomTemplate] PRIMARY KEY CLUSTERED ([PomTemplateId] ASC),
        CONSTRAINT [UQ_TchpPomTemplate_Code] UNIQUE ([TemplateCode]),
        CONSTRAINT [FK_TchpPomTemplate_TchpSizeRunSize]
            FOREIGN KEY ([DefaultBaseSizeId]) REFERENCES [dbo].[TchpSizeRunSize] ([SizeRunSizeId])
    );
    PRINT 'Created TchpPomTemplate';
END
ELSE
    PRINT 'TchpPomTemplate already exists — skipped';
GO

-- ── TchpPomTemplatePart ──────────────────────────────────────
-- Junction: POM template ↔ body part with sort order and optional display alias.
-- Replaces legacy PdmV2kBodyTypeDetail.
IF OBJECT_ID(N'dbo.TchpPomTemplatePart', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpPomTemplatePart] (
        [PomTemplatePartId]     INT             IDENTITY(1,1)   NOT NULL,
        [PomTemplateId]         INT             NOT NULL,
        [BodyPartId]            INT             NOT NULL,
        [BodypartAliasName]     NVARCHAR(50)    NULL,
        [Sort]                  INT             NOT NULL CONSTRAINT DF_TchpPomTemplatePart_Sort DEFAULT (0),
        -- standard audit columns --
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpPomTemplatePart] PRIMARY KEY CLUSTERED ([PomTemplatePartId] ASC),
        CONSTRAINT [FK_TchpPomTemplatePart_TchpPomTemplate]
            FOREIGN KEY ([PomTemplateId]) REFERENCES [dbo].[TchpPomTemplate] ([PomTemplateId]),
        CONSTRAINT [FK_TchpPomTemplatePart_TchpBodyPart]
            FOREIGN KEY ([BodyPartId]) REFERENCES [dbo].[TchpBodyPart] ([BodyPartId]),
        CONSTRAINT [UQ_TchpPomTemplatePart_TemplatePart] UNIQUE ([PomTemplateId], [BodyPartId])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpPomTemplatePart_Template]
        ON [dbo].[TchpPomTemplatePart] ([PomTemplateId] ASC);
    PRINT 'Created TchpPomTemplatePart';
END
ELSE
    PRINT 'TchpPomTemplatePart already exists — skipped';
GO

-- ── TchpGradeRuleSet ─────────────────────────────────────────
-- Named, reusable grade rule library (ASTM Women's Misses, etc.)
IF OBJECT_ID(N'dbo.TchpGradeRuleSet', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpGradeRuleSet] (
        [GradeRuleSetId]        INT             IDENTITY(1,1)   NOT NULL,
        [GradeRuleSetName]      NVARCHAR(100)   NOT NULL,
        [Description]           NVARCHAR(800)   NULL,
        -- ASTM | ISO | CUSTOM
        [Standard]              NVARCHAR(20)    NOT NULL CONSTRAINT DF_TchpGradeRuleSet_Standard DEFAULT ('CUSTOM'),
        [IsActive]              BIT             NOT NULL CONSTRAINT DF_TchpGradeRuleSet_IsActive DEFAULT (1),
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpGradeRuleSet] PRIMARY KEY CLUSTERED ([GradeRuleSetId] ASC)
    );
    PRINT 'Created TchpGradeRuleSet';
END
ELSE
    PRINT 'TchpGradeRuleSet already exists — skipped';
GO

-- ── TchpGradeRule ────────────────────────────────────────────
-- One rule per body-part code within a GradeRuleSet.
-- Coupled by Code (NVARCHAR), not FK — template-agnostic.
-- Column names match existing TchpBodyPart convention:
--   GradingPlusValue / GradingMinuValue (note: "Minu" not "Minus").
IF OBJECT_ID(N'dbo.TchpGradeRule', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpGradeRule] (
        [GradeRuleId]           INT             IDENTITY(1,1)   NOT NULL,
        [GradeRuleSetId]        INT             NOT NULL,
        -- Matches TchpBodyPart.Code — loose coupling, no FK
        [BodyPartCode]          NVARCHAR(50)    NOT NULL,
        -- Per-step delta going up in size
        [GradingPlusValue]      DECIMAL(10,3)   NOT NULL CONSTRAINT DF_TchpGradeRule_PlusValue DEFAULT (0),
        -- Per-step delta going down in size ("Minu" matches existing DTO convention)
        [GradingMinuValue]      DECIMAL(10,3)   NOT NULL CONSTRAINT DF_TchpGradeRule_MinuValue DEFAULT (0),
        [IsSymmetric]           BIT             NOT NULL CONSTRAINT DF_TchpGradeRule_IsSymmetric DEFAULT (1),
        [Sort]                  SMALLINT        NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpGradeRule] PRIMARY KEY CLUSTERED ([GradeRuleId] ASC),
        CONSTRAINT [FK_TchpGradeRule_TchpGradeRuleSet]
            FOREIGN KEY ([GradeRuleSetId]) REFERENCES [dbo].[TchpGradeRuleSet] ([GradeRuleSetId])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpGradeRule_SetCode]
        ON [dbo].[TchpGradeRule] ([GradeRuleSetId] ASC, [BodyPartCode] ASC);
    PRINT 'Created TchpGradeRule';
END
ELSE
    PRINT 'TchpGradeRule already exists — skipped';
GO

-- ── TchpStyleSpec ────────────────────────────────────────────
-- Spec aggregate — one per product. StyleSpecId is NOT identity:
-- it equals Root Plm_ReferenceBasicInfo.ReferenceId (sibling PK = parent PK).
-- BaseSizeDetailId: suggested base size; matches TchpPomTemplate.DefaultBaseSizeId convention.
-- Rebuild when old shape exists (IDENTITY StyleSpecId and/or ProductReferenceId column).
IF OBJECT_ID(N'dbo.TchpStyleSpec', N'U') IS NOT NULL
   AND (
        COL_LENGTH(N'dbo.TchpStyleSpec', N'ProductReferenceId') IS NOT NULL
        OR COLUMNPROPERTY(OBJECT_ID(N'dbo.TchpStyleSpec'), N'StyleSpecId', N'IsIdentity') = 1
   )
BEGIN
    PRINT 'Rebuilding TchpStyleSpec (drop ProductReferenceId / IDENTITY StyleSpecId)...';
    -- Leaf → root (FK-safe). Children recreated by later IF NOT EXISTS blocks.
    -- Inspection add-on tables (do not ALTER existing QC tables; drop add-on first).
    IF OBJECT_ID(N'dbo.TchpQcDefectRecord', N'U') IS NOT NULL DROP TABLE [dbo].[TchpQcDefectRecord];
    IF OBJECT_ID(N'dbo.TchpQcGarmentInspection', N'U') IS NOT NULL DROP TABLE [dbo].[TchpQcGarmentInspection];
    IF OBJECT_ID(N'dbo.TchpQcOrderCert', N'U') IS NOT NULL DROP TABLE [dbo].[TchpQcOrderCert];
    IF OBJECT_ID(N'dbo.TchpQcOrderAql', N'U') IS NOT NULL DROP TABLE [dbo].[TchpQcOrderAql];
    IF OBJECT_ID(N'dbo.TchpQcResult', N'U') IS NOT NULL DROP TABLE [dbo].[TchpQcResult];
    IF OBJECT_ID(N'dbo.TchpQcGarment', N'U') IS NOT NULL DROP TABLE [dbo].[TchpQcGarment];
    IF OBJECT_ID(N'dbo.TchpQcOrderSize', N'U') IS NOT NULL DROP TABLE [dbo].[TchpQcOrderSize];
    IF OBJECT_ID(N'dbo.TchpQcOrder', N'U') IS NOT NULL DROP TABLE [dbo].[TchpQcOrder];
    IF OBJECT_ID(N'dbo.TchpGradeValue', N'U') IS NOT NULL DROP TABLE [dbo].[TchpGradeValue];
    IF OBJECT_ID(N'dbo.TchpFitMeasurement', N'U') IS NOT NULL DROP TABLE [dbo].[TchpFitMeasurement];
    IF OBJECT_ID(N'dbo.TchpPomSpecLine', N'U') IS NOT NULL DROP TABLE [dbo].[TchpPomSpecLine];
    IF OBJECT_ID(N'dbo.TchpFitRound', N'U') IS NOT NULL DROP TABLE [dbo].[TchpFitRound];
    IF OBJECT_ID(N'dbo.TchpStyleSpecDimension', N'U') IS NOT NULL DROP TABLE [dbo].[TchpStyleSpecDimension];
    DROP TABLE [dbo].[TchpStyleSpec];
END
GO

IF OBJECT_ID(N'dbo.TchpStyleSpec', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpStyleSpec] (
        -- Same value as product ReferenceId (sibling link to Root; no IDENTITY)
        [StyleSpecId]           INT             NOT NULL,
        [SizeRunId]             INT             NOT NULL,
        -- FK → TchpSizeRunSize (BaseSizeDetailId matches BodyType convention)
        [BaseSizeDetailId]      INT             NOT NULL,
        -- CM | INCH
        [UnitOfMeasure]         NVARCHAR(10)    NOT NULL CONSTRAINT DF_TchpStyleSpec_UOM DEFAULT ('CM'),
        -- DRAFT | APPROVED | LOCKED
        [SpecStatus]            NVARCHAR(20)    NOT NULL CONSTRAINT DF_TchpStyleSpec_Status DEFAULT ('DRAFT'),
        [Version]               INT             NOT NULL CONSTRAINT DF_TchpStyleSpec_Version DEFAULT (1),
        -- DELTA | PERCENT — user preference for difference display
        [DiffDisplayMode]       NVARCHAR(10)    NOT NULL CONSTRAINT DF_TchpStyleSpec_DiffMode DEFAULT ('DELTA'),
        -- Pipe-delimited SizeRunSizeId whitelist for grading pivot columns (MultiSelectDDL).
        -- NULL / empty = no extra filter (all Dimension-visible sizes show).
        [VisibleSizes]          NVARCHAR(4000)   NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpStyleSpec] PRIMARY KEY CLUSTERED ([StyleSpecId] ASC),
        CONSTRAINT [FK_TchpStyleSpec_TchpSizeRun]
            FOREIGN KEY ([SizeRunId]) REFERENCES [dbo].[TchpSizeRun] ([SizeRunId]),
        CONSTRAINT [FK_TchpStyleSpec_TchpSizeRunSize]
            FOREIGN KEY ([BaseSizeDetailId]) REFERENCES [dbo].[TchpSizeRunSize] ([SizeRunSizeId])
    );
    PRINT 'Created TchpStyleSpec';
END
ELSE
    PRINT 'TchpStyleSpec already exists — skipped';
GO

-- VisibleSizes: pipe-delimited SizeRunSizeId list (MultiSelectDDL); add on existing tables.
IF OBJECT_ID(N'dbo.TchpStyleSpec', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TchpStyleSpec', N'VisibleSizes') IS NULL
BEGIN
    ALTER TABLE [dbo].[TchpStyleSpec] ADD [VisibleSizes] NVARCHAR(4000) NULL;
    PRINT 'Added TchpStyleSpec.VisibleSizes';
END
GO

-- QcSelectedSizes: pipe-delimited SizeRunSizeId whitelist for Simple QC pivot (separate from Grading VisibleSizes).
IF OBJECT_ID(N'dbo.TchpStyleSpec', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TchpStyleSpec', N'QcSelectedSizes') IS NULL
BEGIN
    ALTER TABLE [dbo].[TchpStyleSpec] ADD [QcSelectedSizes] NVARCHAR(4000) NULL;
    PRINT 'Added TchpStyleSpec.QcSelectedSizes';
END
GO

-- ── TchpStyleSpecDimension ────────────────────────────────────
-- Dimensions configured for a StyleSpec (e.g. MA, UA, XA).
-- A spec can have multiple dimensions; IsActive = 1 marks which one
-- is currently selected for the grading pivot.
-- DimensionCode couples loosely to TchpSizeRunDimension by code — no FK
-- so a spec can reference a dimension before size-run mapping is complete.
IF OBJECT_ID(N'dbo.TchpStyleSpecDimension', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpStyleSpecDimension] (
        [StyleSpecDimensionId]  INT             IDENTITY(1,1)   NOT NULL,
        [StyleSpecId]           INT             NOT NULL,
        -- e.g. MA | UA | XA — matches TchpSizeRunDimension.DimensionCode
        [DimensionCode]         NVARCHAR(20)    NOT NULL,
        -- 1 = this dimension is currently open in the grading pivot
        [IsActive]              BIT             NOT NULL CONSTRAINT DF_TchpStyleSpecDimension_IsActive DEFAULT (0),
        [SortOrder]             INT             NOT NULL CONSTRAINT DF_TchpStyleSpecDimension_Sort DEFAULT (0),
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpStyleSpecDimension] PRIMARY KEY CLUSTERED ([StyleSpecDimensionId] ASC),
        CONSTRAINT [FK_TchpStyleSpecDimension_TchpStyleSpec]
            FOREIGN KEY ([StyleSpecId]) REFERENCES [dbo].[TchpStyleSpec] ([StyleSpecId]),
        CONSTRAINT [UQ_TchpStyleSpecDimension_SpecCode]
            UNIQUE ([StyleSpecId], [DimensionCode])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpStyleSpecDimension_Spec]
        ON [dbo].[TchpStyleSpecDimension] ([StyleSpecId] ASC);
    PRINT 'Created TchpStyleSpecDimension';
END
ELSE
    PRINT 'TchpStyleSpecDimension already exists — skipped';
GO

-- ── TchpPomSpecLine ──────────────────────────────────────────
-- One row per POM body part per StyleSpec.
-- IsFixed = 1 means no grading (replaces IsNeedToApplyGradingRule = 0 logic).
-- BodypartAliasName matches TchpPomTemplatePart convention.
IF OBJECT_ID(N'dbo.TchpPomSpecLine', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpPomSpecLine] (
        [PomSpecLineId]         INT             IDENTITY(1,1)   NOT NULL,
        [StyleSpecId]           INT             NOT NULL,
        [BodyPartId]            INT             NOT NULL,
        -- NULL = use body part defaults; set to apply a named rule set
        [GradeRuleSetId]        INT             NULL,
        -- Base size measurement (stored in CM — convert at API boundary only)
        [BaseValue]             DECIMAL(10,3)   NULL,
        [Tolerance]             DECIMAL(10,3)   NULL,
        -- 1 = fixed POM (no grading); matches IsNeedToApplyGradingRule = false
        [IsFixed]               BIT             NOT NULL CONSTRAINT DF_TchpPomSpecLine_IsFixed DEFAULT (0),
        [Sort]                  INT             NULL,
        -- Optional display alias (matches TchpPomTemplatePart.BodypartAliasName)
        [BodypartAliasName]     NVARCHAR(50)    NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpPomSpecLine] PRIMARY KEY CLUSTERED ([PomSpecLineId] ASC),
        CONSTRAINT [FK_TchpPomSpecLine_TchpStyleSpec]
            FOREIGN KEY ([StyleSpecId]) REFERENCES [dbo].[TchpStyleSpec] ([StyleSpecId]),
        CONSTRAINT [FK_TchpPomSpecLine_TchpBodyPart]
            FOREIGN KEY ([BodyPartId]) REFERENCES [dbo].[TchpBodyPart] ([BodyPartId]),
        CONSTRAINT [FK_TchpPomSpecLine_TchpGradeRuleSet]
            FOREIGN KEY ([GradeRuleSetId]) REFERENCES [dbo].[TchpGradeRuleSet] ([GradeRuleSetId])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpPomSpecLine_StyleSpec]
        ON [dbo].[TchpPomSpecLine] ([StyleSpecId] ASC);
    PRINT 'Created TchpPomSpecLine';
END
ELSE
    PRINT 'TchpPomSpecLine already exists — skipped';
GO

-- ── TchpGradeValue ───────────────────────────────────────────
-- Adjacent-step delta per size for each POM spec line.
-- GradingDelta at base size is always 0 (enforced by GradingEngine).
-- Adjacent-step delta toward smaller size; base position is always 0
IF OBJECT_ID(N'dbo.TchpGradeValue', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpGradeValue] (
        [GradeValueId]          INT             IDENTITY(1,1)   NOT NULL,
        [PomSpecLineId]         INT             NOT NULL,
        [SizeRunSizeId]         INT             NOT NULL,
        -- Adjacent-step delta toward smaller size; base position is always 0
        [GradingDelta]          DECIMAL(10,3)   NOT NULL CONSTRAINT DF_TchpGradeValue_Delta DEFAULT (0),
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        CONSTRAINT [PK_TchpGradeValue] PRIMARY KEY CLUSTERED ([GradeValueId] ASC),
        CONSTRAINT [FK_TchpGradeValue_TchpPomSpecLine]
            FOREIGN KEY ([PomSpecLineId]) REFERENCES [dbo].[TchpPomSpecLine] ([PomSpecLineId]),
        CONSTRAINT [FK_TchpGradeValue_TchpSizeRunSize]
            FOREIGN KEY ([SizeRunSizeId]) REFERENCES [dbo].[TchpSizeRunSize] ([SizeRunSizeId]),
        CONSTRAINT [UQ_TchpGradeValue_LineSize]
            UNIQUE ([PomSpecLineId], [SizeRunSizeId])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpGradeValue_SpecLine]
        ON [dbo].[TchpGradeValue] ([PomSpecLineId] ASC);
    PRINT 'Created TchpGradeValue';
END
ELSE
    PRINT 'TchpGradeValue already exists — skipped';
GO

-- ── TchpFitRound ─────────────────────────────────────────────
-- One fit iteration round per StyleSpec.
-- State machine: PENDING → SUBMITTED → APPROVED | REJECTED.
-- TOP sample approval triggers TchpStyleSpec.SpecStatus → LOCKED.
IF OBJECT_ID(N'dbo.TchpFitRound', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpFitRound] (
        [FitRoundId]            INT             IDENTITY(1,1)   NOT NULL,
        [StyleSpecId]           INT             NOT NULL,
        [RoundNumber]           SMALLINT        NOT NULL,
        -- PP1 | PP2 | PP3 | TOP | INTERNAL
        [RoundType]             NVARCHAR(20)    NOT NULL,
        -- PENDING | SUBMITTED | APPROVED | REJECTED
        [RoundStatus]           NVARCHAR(20)    NOT NULL CONSTRAINT DF_TchpFitRound_Status DEFAULT ('PENDING'),
        [Comment]               NVARCHAR(MAX)   NULL,
        [SubmittedAt]           DATETIME        NULL,
        [SubmittedById]         INT             NULL,
        [ApprovedAt]            DATETIME        NULL,
        [ApprovedById]          INT             NULL,
        [RejectionReason]       NVARCHAR(500)   NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpFitRound] PRIMARY KEY CLUSTERED ([FitRoundId] ASC),
        CONSTRAINT [FK_TchpFitRound_TchpStyleSpec]
            FOREIGN KEY ([StyleSpecId]) REFERENCES [dbo].[TchpStyleSpec] ([StyleSpecId]),
        CONSTRAINT [UQ_TchpFitRound_SpecRound]
            UNIQUE ([StyleSpecId], [RoundNumber])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpFitRound_StyleSpec]
        ON [dbo].[TchpFitRound] ([StyleSpecId] ASC);
    PRINT 'Created TchpFitRound';
END
ELSE
    PRINT 'TchpFitRound already exists — skipped';
GO

-- ── TchpFitMeasurement ───────────────────────────────────────
-- Actual measurement per POM per fit round.
-- FinalSpec derivation: last APPROVED round ActualValue, fallback to BaseValue.
IF OBJECT_ID(N'dbo.TchpFitMeasurement', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpFitMeasurement] (
        [FitMeasurementId]      INT             IDENTITY(1,1)   NOT NULL,
        [FitRoundId]            INT             NOT NULL,
        [PomSpecLineId]         INT             NOT NULL,
        -- Stored in CM; null means not yet measured for this round
        [ActualValue]           DECIMAL(10,3)   NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        CONSTRAINT [PK_TchpFitMeasurement] PRIMARY KEY CLUSTERED ([FitMeasurementId] ASC),
        CONSTRAINT [FK_TchpFitMeasurement_TchpFitRound]
            FOREIGN KEY ([FitRoundId]) REFERENCES [dbo].[TchpFitRound] ([FitRoundId]),
        CONSTRAINT [FK_TchpFitMeasurement_TchpPomSpecLine]
            FOREIGN KEY ([PomSpecLineId]) REFERENCES [dbo].[TchpPomSpecLine] ([PomSpecLineId]),
        CONSTRAINT [UQ_TchpFitMeasurement_RoundLine]
            UNIQUE ([FitRoundId], [PomSpecLineId])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpFitMeasurement_Round]
        ON [dbo].[TchpFitMeasurement] ([FitRoundId] ASC);
    PRINT 'Created TchpFitMeasurement';
END
ELSE
    PRINT 'TchpFitMeasurement already exists — skipped';
GO

-- ── TchpQcOrder ──────────────────────────────────────────────
-- QC order aggregate root — linked to a LOCKED StyleSpec version.
-- Product scope is StyleSpecId only (StyleSpecId == Root.ReferenceId); no ProductReferenceId column.
-- SampleSize is computed by AqlSamplingService (BL) and stored here.
IF OBJECT_ID(N'dbo.TchpQcOrder', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpQcOrder] (
        [QcOrderId]             INT             IDENTITY(1,1)   NOT NULL,
        [StyleSpecId]           INT             NOT NULL,
        [LotNumber]             NVARCHAR(50)    NOT NULL,
        -- FK to vendor/factory (FactoryId → existing vendor table)
        [FactoryId]             INT             NULL,
        -- CRITICAL_1 | MAJOR_2_5 | MINOR_4_0
        [AqlLevel]              NVARCHAR(20)    NOT NULL CONSTRAINT DF_TchpQcOrder_AqlLevel DEFAULT ('MAJOR_2_5'),
        [LotQuantity]           INT             NOT NULL CONSTRAINT DF_TchpQcOrder_LotQty DEFAULT (0),
        -- Computed by AqlSamplingService and stored
        [SampleSize]            INT             NOT NULL CONSTRAINT DF_TchpQcOrder_SampleSize DEFAULT (0),
        -- OPEN | IN_PROGRESS | PASSED | FAILED
        [OrderStatus]           NVARCHAR(20)    NOT NULL CONSTRAINT DF_TchpQcOrder_Status DEFAULT ('OPEN'),
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpQcOrder] PRIMARY KEY CLUSTERED ([QcOrderId] ASC),
        CONSTRAINT [FK_TchpQcOrder_TchpStyleSpec]
            FOREIGN KEY ([StyleSpecId]) REFERENCES [dbo].[TchpStyleSpec] ([StyleSpecId])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpQcOrder_StyleSpec]
        ON [dbo].[TchpQcOrder] ([StyleSpecId] ASC);
    PRINT 'Created TchpQcOrder';
END
ELSE IF COL_LENGTH(N'dbo.TchpQcOrder', N'ProductReferenceId') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TchpQcOrder') AND name = N'IX_TchpQcOrder_ProductRef')
        DROP INDEX [IX_TchpQcOrder_ProductRef] ON [dbo].[TchpQcOrder];
    ALTER TABLE [dbo].[TchpQcOrder] DROP COLUMN [ProductReferenceId];
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TchpQcOrder') AND name = N'IX_TchpQcOrder_StyleSpec')
        CREATE NONCLUSTERED INDEX [IX_TchpQcOrder_StyleSpec]
            ON [dbo].[TchpQcOrder] ([StyleSpecId] ASC);
    PRINT 'TchpQcOrder: dropped ProductReferenceId; indexed StyleSpecId';
END
ELSE
    PRINT 'TchpQcOrder already exists — skipped';
GO

-- ── TchpQcOrderSize ──────────────────────────────────────────
-- Junction: sizes selected for QC inspection in an order.
IF OBJECT_ID(N'dbo.TchpQcOrderSize', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpQcOrderSize] (
        [QcOrderSizeId]         INT             IDENTITY(1,1)   NOT NULL,
        [QcOrderId]             INT             NOT NULL,
        [SizeRunSizeId]         INT             NOT NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        CONSTRAINT [PK_TchpQcOrderSize] PRIMARY KEY CLUSTERED ([QcOrderSizeId] ASC),
        CONSTRAINT [FK_TchpQcOrderSize_TchpQcOrder]
            FOREIGN KEY ([QcOrderId]) REFERENCES [dbo].[TchpQcOrder] ([QcOrderId]),
        CONSTRAINT [FK_TchpQcOrderSize_TchpSizeRunSize]
            FOREIGN KEY ([SizeRunSizeId]) REFERENCES [dbo].[TchpSizeRunSize] ([SizeRunSizeId]),
        CONSTRAINT [UQ_TchpQcOrderSize_OrderSize]
            UNIQUE ([QcOrderId], [SizeRunSizeId])
    );
    PRINT 'Created TchpQcOrderSize';
END
ELSE
    PRINT 'TchpQcOrderSize already exists — skipped';
GO

-- ── TchpQcGarment ────────────────────────────────────────────
-- One row per sampled garment within a QC order.
-- Each garment has exactly one SizeRunSizeId (the physical size of that piece).
-- TchpQcResult.SizeRunSizeId is a redundant snapshot copied from this column.
-- GarmentPassStatus: NULL = not yet evaluated, 1 = pass, 0 = fail.
IF OBJECT_ID(N'dbo.TchpQcGarment', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpQcGarment] (
        [QcGarmentId]           INT             IDENTITY(1,1)   NOT NULL,
        [QcOrderId]             INT             NOT NULL,
        [GarmentSerial]         NVARCHAR(50)    NOT NULL,
        [SizeRunSizeId]         INT             NOT NULL,
        -- Set by QcAggregateService after all measurements are entered
        [GarmentPassStatus]     BIT             NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpQcGarment] PRIMARY KEY CLUSTERED ([QcGarmentId] ASC),
        CONSTRAINT [FK_TchpQcGarment_TchpQcOrder]
            FOREIGN KEY ([QcOrderId]) REFERENCES [dbo].[TchpQcOrder] ([QcOrderId]),
        CONSTRAINT [FK_TchpQcGarment_TchpSizeRunSize]
            FOREIGN KEY ([SizeRunSizeId]) REFERENCES [dbo].[TchpSizeRunSize] ([SizeRunSizeId])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpQcGarment_Order]
        ON [dbo].[TchpQcGarment] ([QcOrderId] ASC);
    CREATE NONCLUSTERED INDEX [IX_TchpQcGarment_Size]
        ON [dbo].[TchpQcGarment] ([SizeRunSizeId] ASC);
    PRINT 'Created TchpQcGarment';
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.TchpQcGarment', N'SizeRunSizeId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[TchpQcGarment] ADD [SizeRunSizeId] INT NULL;
        IF OBJECT_ID(N'dbo.TchpQcResult', N'U') IS NOT NULL
        BEGIN
            UPDATE g
            SET g.SizeRunSizeId = r.SizeRunSizeId
            FROM [dbo].[TchpQcGarment] g
            CROSS APPLY (
                SELECT TOP 1 r0.SizeRunSizeId
                FROM [dbo].[TchpQcResult] r0
                WHERE r0.QcGarmentId = g.QcGarmentId
                  AND r0.SizeRunSizeId IS NOT NULL
                ORDER BY r0.QcResultId
            ) r;
        END
        PRINT 'TchpQcGarment: added SizeRunSizeId (nullable until backfilled)';
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = N'FK_TchpQcGarment_TchpSizeRunSize'
          AND parent_object_id = OBJECT_ID(N'dbo.TchpQcGarment'))
       AND COL_LENGTH(N'dbo.TchpQcGarment', N'SizeRunSizeId') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[TchpQcGarment] WITH NOCHECK
            ADD CONSTRAINT [FK_TchpQcGarment_TchpSizeRunSize]
            FOREIGN KEY ([SizeRunSizeId]) REFERENCES [dbo].[TchpSizeRunSize] ([SizeRunSizeId]);
        PRINT 'TchpQcGarment: added FK to TchpSizeRunSize';
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.TchpQcGarment')
          AND name = N'IX_TchpQcGarment_Size')
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_TchpQcGarment_Size]
            ON [dbo].[TchpQcGarment] ([SizeRunSizeId] ASC);
    END
END
GO

-- ── TchpQcResult ─────────────────────────────────────────────
-- QC measurement per garment × POM. SizeRunSizeId is a snapshot copied from
-- parent TchpQcGarment.SizeRunSizeId (one physical size per garment).
-- SpecValue and Tolerance are snapshots from the locked StyleSpec at QC time.
-- Shrinkage, Recovery, FinalDiff are PERSISTED computed columns (pure arithmetic).
-- Pass and DefectClass are stored (updated by QcAggregateService after each stage).
--
-- Null rule: any value NULL means "not yet measured" — NOT zero difference.
IF OBJECT_ID(N'dbo.TchpQcResult', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpQcResult] (
        [QcResultId]            INT             IDENTITY(1,1)   NOT NULL,
        [QcGarmentId]           INT             NOT NULL,
        [PomSpecLineId]         INT             NOT NULL,
        [SizeRunSizeId]         INT             NOT NULL,
        -- Stage 1 — production before any wash
        [ProductionValue]       DECIMAL(10,3)   NULL,
        -- Stage 2 — before wash
        [BeforeWashValue]       DECIMAL(10,3)   NULL,
        -- Stage 3 — after wash
        [AfterWashValue]        DECIMAL(10,3)   NULL,
        -- Stage 4 — after iron (final QC pass basis)
        [AfterIronValue]        DECIMAL(10,3)   NULL,
        -- Snapshots from locked StyleSpec — never recalculated from live spec
        [SpecValue]             DECIMAL(10,3)   NOT NULL,
        [Tolerance]             DECIMAL(10,3)   NOT NULL,
        -- Computed: BeforeWash − AfterWash (positive = shrinkage)
        [Shrinkage]             AS (CASE WHEN [BeforeWashValue] IS NULL OR [AfterWashValue]  IS NULL THEN NULL
                                         ELSE [BeforeWashValue] - [AfterWashValue] END) PERSISTED,
        -- Computed: AfterIron − AfterWash (positive = recovery)
        [Recovery]              AS (CASE WHEN [AfterIronValue]  IS NULL OR [AfterWashValue]  IS NULL THEN NULL
                                         ELSE [AfterIronValue]  - [AfterWashValue]  END) PERSISTED,
        -- Computed: AfterIron − SpecValue (final QC difference)
        [FinalDiff]             AS (CASE WHEN [AfterIronValue]  IS NULL THEN NULL
                                         ELSE [AfterIronValue]  - [SpecValue]       END) PERSISTED,
        -- Stored BIT: set by QcAggregateService; NULL = not yet measured
        [Pass]                  BIT             NULL,
        -- CRITICAL | MAJOR | MINOR | NULL — set by defect classification logic in BL
        [DefectClass]           NVARCHAR(10)    NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        CONSTRAINT [PK_TchpQcResult] PRIMARY KEY CLUSTERED ([QcResultId] ASC),
        CONSTRAINT [FK_TchpQcResult_TchpQcGarment]
            FOREIGN KEY ([QcGarmentId]) REFERENCES [dbo].[TchpQcGarment] ([QcGarmentId]),
        CONSTRAINT [FK_TchpQcResult_TchpPomSpecLine]
            FOREIGN KEY ([PomSpecLineId]) REFERENCES [dbo].[TchpPomSpecLine] ([PomSpecLineId]),
        CONSTRAINT [FK_TchpQcResult_TchpSizeRunSize]
            FOREIGN KEY ([SizeRunSizeId]) REFERENCES [dbo].[TchpSizeRunSize] ([SizeRunSizeId]),
        CONSTRAINT [UQ_TchpQcResult_GarmentPomSize]
            UNIQUE ([QcGarmentId], [PomSpecLineId], [SizeRunSizeId])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpQcResult_Garment]
        ON [dbo].[TchpQcResult] ([QcGarmentId] ASC);
    PRINT 'Created TchpQcResult';
END
ELSE
    PRINT 'TchpQcResult already exists — skipped';
GO

-- ── TchpSizeRunDimension ──────────────────────────────────────
-- Global mapping: which TchpSizeRunSize rows belong to which dimension
-- within a size run. Defined once per size run; shared across all specs.
-- Example — School Girls Tops: MA→2T,3T,4T | UA→4,5,6,6X | XA→7,8,10,12,14
-- UQ on (SizeRunSizeId) enforces: one size belongs to exactly one dimension.
IF OBJECT_ID(N'dbo.TchpSizeRunDimension', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpSizeRunDimension] (
        [SizeRunDimensionId]    INT             IDENTITY(1,1)   NOT NULL,
        [SizeRunId]             INT             NOT NULL,
        [SizeRunSizeId]         INT             NOT NULL,
        -- Dimension this size belongs to (e.g. MA | UA | XA)
        [DimensionCode]         NVARCHAR(20)    NOT NULL,
        [SortOrder]             INT             NOT NULL CONSTRAINT DF_TchpSizeRunDimension_Sort DEFAULT (0),
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpSizeRunDimension] PRIMARY KEY CLUSTERED ([SizeRunDimensionId] ASC),
        CONSTRAINT [FK_TchpSizeRunDimension_TchpSizeRun]
            FOREIGN KEY ([SizeRunId]) REFERENCES [dbo].[TchpSizeRun] ([SizeRunId]),
        CONSTRAINT [FK_TchpSizeRunDimension_TchpSizeRunSize]
            FOREIGN KEY ([SizeRunSizeId]) REFERENCES [dbo].[TchpSizeRunSize] ([SizeRunSizeId]),
        -- Each size belongs to exactly one dimension within its size run
        CONSTRAINT [UQ_TchpSizeRunDimension_Size]
            UNIQUE ([SizeRunSizeId])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpSizeRunDimension_RunDim]
        ON [dbo].[TchpSizeRunDimension] ([SizeRunId] ASC, [DimensionCode] ASC);
    PRINT 'Created TchpSizeRunDimension';
END
ELSE
    PRINT 'TchpSizeRunDimension already exists — skipped';
GO

-- ── TchpSizeSystemMapping ────────────────────────────────────
-- Multi-region size equivalence: US 6 = EU 36 = JP 9.
-- One row per size per region code for a given TchpSizeRunSize entry.
IF OBJECT_ID(N'dbo.TchpSizeSystemMapping', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpSizeSystemMapping] (
        [SizeSystemMappingId]   INT             IDENTITY(1,1)   NOT NULL,
        [SizeRunSizeId]         INT             NOT NULL,
        -- US | EU | UK | JP | CN | INTL
        [SystemCode]            NVARCHAR(10)    NOT NULL,
        -- Equivalent size label in that system (e.g., "36" for EU)
        [SizeLabel]             NVARCHAR(20)    NOT NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpSizeSystemMapping] PRIMARY KEY CLUSTERED ([SizeSystemMappingId] ASC),
        CONSTRAINT [FK_TchpSizeSystemMapping_TchpSizeRunSize]
            FOREIGN KEY ([SizeRunSizeId]) REFERENCES [dbo].[TchpSizeRunSize] ([SizeRunSizeId]),
        CONSTRAINT [UQ_TchpSizeSystemMapping_SizeSystem]
            UNIQUE ([SizeRunSizeId], [SystemCode])
    );
    PRINT 'Created TchpSizeSystemMapping';
END
ELSE
    PRINT 'TchpSizeSystemMapping already exists — skipped';
GO

-- ── Reference Data: ASTM Grade Rule Sets ────────────────────
-- Seed standard rule sets. Run only once; guard with name check.
IF NOT EXISTS (SELECT 1 FROM [dbo].[TchpGradeRuleSet] WHERE [GradeRuleSetName] = 'ASTM Women''s Misses')
BEGIN
    INSERT INTO [dbo].[TchpGradeRuleSet] ([GradeRuleSetName], [Description], [Standard], [IsActive])
    VALUES
    ('ASTM Women''s Misses', 'ASTM D5585 adult female misses half-step system', 'ASTM', 1),
    ('ASTM Men''s Shirt',    'ASTM D6960 adult male shirt standard increments',  'ASTM', 1);

    DECLARE @WomensId INT = SCOPE_IDENTITY() - 1;
    DECLARE @MensId   INT = SCOPE_IDENTITY();

    -- Women's Misses grade rules (cm per step, half-step system)
    INSERT INTO [dbo].[TchpGradeRule]
        ([GradeRuleSetId], [BodyPartCode], [GradingPlusValue], [GradingMinuValue], [IsSymmetric], [Sort])
    VALUES
    (@WomensId, 'CHEST',     2.0, 2.0, 1, 1),
    (@WomensId, 'BUST',      2.0, 2.0, 1, 2),
    (@WomensId, 'WAIST',     2.0, 2.0, 1, 3),
    (@WomensId, 'HIP',       2.0, 2.0, 1, 4),
    (@WomensId, 'RISE_F',    0.6, 0.6, 1, 5),
    (@WomensId, 'RISE_B',    0.6, 0.6, 1, 6),
    (@WomensId, 'INSEAM',    0.6, 0.6, 1, 7),
    (@WomensId, 'SHOULDER',  0.6, 0.6, 1, 8),
    (@WomensId, 'SLV_LEN',   0.6, 0.6, 1, 9),
    (@WomensId, 'NECK_W',    0.3, 0.3, 1, 10);

    -- Men's Shirt grade rules (cm per step)
    INSERT INTO [dbo].[TchpGradeRule]
        ([GradeRuleSetId], [BodyPartCode], [GradingPlusValue], [GradingMinuValue], [IsSymmetric], [Sort])
    VALUES
    (@MensId, 'CHEST',     2.5, 2.5, 1, 1),
    (@MensId, 'WAIST',     2.5, 2.5, 1, 2),
    (@MensId, 'SEAT',      2.5, 2.5, 1, 3),
    (@MensId, 'BACK_LEN',  1.5, 1.5, 1, 4),
    (@MensId, 'SLV_LEN',   1.0, 1.0, 1, 5);

    PRINT 'Seeded ASTM grade rule sets';
END
ELSE
    PRINT 'ASTM grade rule sets already seeded — skipped';
GO

-- ── View_TchpStyleActiveSizeRunSizes ─────────────────────────
-- Read-only sizes for the StyleSpec's current SizeRun.
-- IsVisible:
--   1) Dimension filter (TchpStyleSpecDimension.IsActive=1 via TchpSizeRunDimension).
--      - No StyleSpecDimension rows → all sizes dimension-visible
--      - No IsActive=1 yet → match any configured DimensionCode (legacy)
--      - Active dimension set → only sizes mapped to that DimensionCode
--   2) AND VisibleSizes whitelist (TchpStyleSpec.VisibleSizes = pipe-delimited SizeRunSizeId).
--      - NULL / empty → no extra filter
--      - Non-empty → SizeRunSizeId must appear in the list
-- Used as ROOT child (StyleSpecId → Root.ReferenceId); pivot column domain.
-- Keep in sync with ImportFromPLMDW 3b_Tchp_ImportFromDW.sql (CREATE OR ALTER).
IF OBJECT_ID(N'dbo.View_TchpStyleActiveSizeRunSizes', N'V') IS NOT NULL
    DROP VIEW [dbo].[View_TchpStyleActiveSizeRunSizes];
GO

CREATE VIEW [dbo].[View_TchpStyleActiveSizeRunSizes]
AS
SELECT
    ss.StyleSpecId,
    ss.SizeRunId,
    srs.SizeRunSizeId,
    srs.SizeLabel,
    srs.SizeOrder,
    srs.IsActive,
    CASE
        WHEN (
            CASE
                WHEN NOT EXISTS (
                    SELECT 1
                    FROM dbo.TchpStyleSpecDimension AS ssd
                    WHERE ssd.StyleSpecId = ss.StyleSpecId
                ) THEN 1
                WHEN EXISTS (
                    SELECT 1
                    FROM dbo.TchpSizeRunDimension AS srd
                    INNER JOIN dbo.TchpStyleSpecDimension AS ssd
                        ON ssd.StyleSpecId = ss.StyleSpecId
                       AND ssd.DimensionCode = srd.DimensionCode
                       AND (
                            ssd.IsActive = 1
                            OR NOT EXISTS (
                                SELECT 1
                                FROM dbo.TchpStyleSpecDimension AS x
                                WHERE x.StyleSpecId = ss.StyleSpecId
                                  AND x.IsActive = 1
                            )
                       )
                    WHERE srd.SizeRunSizeId = srs.SizeRunSizeId
                ) THEN 1
                ELSE 0
            END
        ) = 0 THEN 0
        WHEN NULLIF(LTRIM(RTRIM(ss.VisibleSizes)), N'') IS NULL THEN 1
        WHEN EXISTS (
            SELECT 1
            FROM STRING_SPLIT(REPLACE(ss.VisibleSizes, N'|', N','), N',') AS tok
            WHERE TRY_CONVERT(INT, LTRIM(RTRIM(tok.[value]))) = srs.SizeRunSizeId
        ) THEN 1
        ELSE 0
    END AS IsVisible
FROM dbo.TchpStyleSpec AS ss
INNER JOIN dbo.TchpSizeRunSize AS srs
    ON srs.SizeRunId = ss.SizeRunId
WHERE ISNULL(srs.IsActive, 1) = 1;
GO

PRINT 'Created View_TchpStyleActiveSizeRunSizes';
GO

-- ── View_TchpSimpleQcSelectedSizes ───────────────────────────
-- Simple QC pivot column domain (QX1). IsVisible from QcSelectedSizes only
-- (not Grading VisibleSizes / Dimension). Keep in sync with ImportFromPLMDW 3b.
IF OBJECT_ID(N'dbo.View_TchpSimpleQcSelectedSizes', N'V') IS NOT NULL
    DROP VIEW [dbo].[View_TchpSimpleQcSelectedSizes];
GO

CREATE VIEW [dbo].[View_TchpSimpleQcSelectedSizes]
AS
SELECT
    ss.StyleSpecId,
    ss.SizeRunId,
    srs.SizeRunSizeId,
    srs.SizeLabel,
    srs.SizeOrder,
    srs.IsActive,
    CASE
        WHEN NULLIF(LTRIM(RTRIM(ss.QcSelectedSizes)), N'') IS NULL THEN 1
        WHEN EXISTS (
            SELECT 1
            FROM STRING_SPLIT(REPLACE(ss.QcSelectedSizes, N'|', N','), N',') AS tok
            WHERE TRY_CONVERT(INT, LTRIM(RTRIM(tok.[value]))) = srs.SizeRunSizeId
        ) THEN 1
        ELSE 0
    END AS IsVisible
FROM dbo.TchpStyleSpec AS ss
INNER JOIN dbo.TchpSizeRunSize AS srs
    ON srs.SizeRunId = ss.SizeRunId
WHERE ISNULL(srs.IsActive, 1) = 1;
GO

PRINT 'Created View_TchpSimpleQcSelectedSizes';
GO

-- ── View_TchpSizeRunSize_DefaultDimension ────────────────────
-- Size-run size list with DimensionCode from TchpSizeRunDimension.
-- One row per SizeRunSizeId. If a size maps to multiple DimensionCodes,
-- keep the first by SortOrder, SizeRunDimensionId. Missing map → ''.
IF OBJECT_ID(N'dbo.View_TchpSizeRunSize', N'V') IS NOT NULL
    DROP VIEW [dbo].[View_TchpSizeRunSize]; -- rename: old short name
IF OBJECT_ID(N'dbo.View_TchpSizeRunSize_DefaultDimension', N'V') IS NOT NULL
    DROP VIEW [dbo].[View_TchpSizeRunSize_DefaultDimension];
GO

CREATE VIEW [dbo].[View_TchpSizeRunSize_DefaultDimension]
AS
SELECT
    srs.SizeRunSizeId,
    srs.SizeRunId,
    srs.SizeLabel,
    srs.SizeOrder,
    srs.IsActive,
    ISNULL(dim.DimensionCode, N'') AS DimensionCode
FROM dbo.TchpSizeRunSize AS srs
OUTER APPLY (
    SELECT TOP (1)
        srd.DimensionCode
    FROM dbo.TchpSizeRunDimension AS srd
    WHERE srd.SizeRunSizeId = srs.SizeRunSizeId
    ORDER BY srd.SortOrder ASC, srd.SizeRunDimensionId ASC
) AS dim;
GO

PRINT 'Created View_TchpSizeRunSize_DefaultDimension';
GO

-- ── View_TchpFitMeasurementByPom ─────────────────────────────
-- Read-only Fit measurements keyed by POM for SUMMARY pivot (F3).
-- RoundNumber / RoundType come from TchpFitRound (not on TchpFitMeasurement).
-- ChildUnitPivotColumns: IsPivotColumn=RoundNumber, IsPivotValue=ActualValue.
-- Keep in sync with ImportFromPLMDW 3b_Tchp_ImportFromDW.sql (CREATE OR ALTER)
-- and AppReact/ImportDoc/ImportFromPLMDW/PROMPT.md §TechPack Fit F3.
IF OBJECT_ID(N'dbo.View_TchpFitMeasurementByPom', N'V') IS NOT NULL
    DROP VIEW [dbo].[View_TchpFitMeasurementByPom];
GO

CREATE VIEW [dbo].[View_TchpFitMeasurementByPom]
AS
SELECT
    fm.FitMeasurementId,
    fm.PomSpecLineId,
    pl.StyleSpecId,
    fr.FitRoundId,
    fr.RoundNumber,
    fr.RoundType,
    CONCAT(N'Fit ', fr.RoundNumber) AS RoundLabel,
    fm.ActualValue
FROM dbo.TchpFitMeasurement AS fm
INNER JOIN dbo.TchpFitRound AS fr
    ON fr.FitRoundId = fm.FitRoundId
INNER JOIN dbo.TchpPomSpecLine AS pl
    ON pl.PomSpecLineId = fm.PomSpecLineId;
GO

PRINT 'Created View_TchpFitMeasurementByPom';
GO

-- ── View_TchpPomSpecLine ─────────────────────────────────────
-- DDL entity source for FIT ROUND grid: PomSpecLineId shows BodyPartName;
-- BaseValue / Tolerance are subscribed into temp InitValue / Tol fields.
-- Keep in sync with ImportFromPLMDW 3b + PROMPT.md §F2 Fit Round measurement UX.
IF OBJECT_ID(N'dbo.View_TchpPomSpecLine', N'V') IS NOT NULL
    DROP VIEW [dbo].[View_TchpPomSpecLine];
GO

CREATE VIEW [dbo].[View_TchpPomSpecLine]
AS
SELECT
    pl.PomSpecLineId,
    bp.BodyPartName,
    pl.StyleSpecId,
    pl.GradeRuleSetId,
    pl.BaseValue,
    pl.Tolerance,
    pl.IsFixed,
    pl.Sort,
    pl.BodypartAliasName
FROM dbo.TchpPomSpecLine AS pl
INNER JOIN dbo.TchpBodyPart AS bp
    ON bp.BodyPartId = pl.BodyPartId;
GO

PRINT 'Created View_TchpPomSpecLine';
GO

-- ── View_TchpQcOrderAvailableSize ────────────────────────────
-- Available Select SOURCE for QC Order selected sizes.
-- Parent link: QcOrderId. Mapping key: SizeRunSizeId.
CREATE OR ALTER VIEW [dbo].[View_TchpQcOrderAvailableSize]
AS
SELECT
    CAST(o.QcOrderId AS BIGINT) * 1000000000 + CAST(srs.SizeRunSizeId AS BIGINT) AS QcOrderAvailableSizeId,
    o.QcOrderId,
    o.StyleSpecId,
    srs.SizeRunId,
    srs.SizeRunSizeId,
    srs.SizeLabel,
    srs.SizeOrder,
    srs.IsActive
FROM dbo.TchpQcOrder AS o
INNER JOIN dbo.TchpStyleSpec AS ss
    ON ss.StyleSpecId = o.StyleSpecId
INNER JOIN dbo.TchpSizeRunSize AS srs
    ON srs.SizeRunId = ss.SizeRunId
WHERE ISNULL(srs.IsActive, 1) = 1;
GO

PRINT 'Created View_TchpQcOrderAvailableSize';
GO

-- ── View_TchpQcOrderPom ──────────────────────────────────────
-- QC Order Child4 host: one row per POM on the order's StyleSpec.
-- PK QcOrderPomId is unique per (QcOrderId, PomSpecLineId).
CREATE OR ALTER VIEW [dbo].[View_TchpQcOrderPom]
AS
SELECT
    CAST(o.QcOrderId AS BIGINT) * 1000000000 + CAST(psl.PomSpecLineId AS BIGINT) AS QcOrderPomId,
    o.QcOrderId,
    psl.PomSpecLineId,
    psl.StyleSpecId,
    ISNULL(NULLIF(LTRIM(RTRIM(psl.BodypartAliasName)), N''), bp.BodyPartName) AS PomName,
    bp.BodyPartName,
    psl.BodypartAliasName,
    psl.Sort,
    psl.BaseValue,
    psl.Tolerance
FROM dbo.TchpQcOrder AS o
INNER JOIN dbo.TchpPomSpecLine AS psl
    ON psl.StyleSpecId = o.StyleSpecId
INNER JOIN dbo.TchpBodyPart AS bp
    ON bp.BodyPartId = psl.BodyPartId;
GO

PRINT 'Created View_TchpQcOrderPom';
GO

-- ── View_TchpQcOrderPomSizeResult ────────────────────────────
-- QC Order Child4 grandchild: POM × selected Size aggregates.
-- ChildUnitPivotColumns: SizeRunSizeId = pivot column; FailCount / AvgFinalDiff = values.
-- Rows exist for every selected size even when no garments have been measured yet.
CREATE OR ALTER VIEW [dbo].[View_TchpQcOrderPomSizeResult]
AS
SELECT
    CAST(o.QcOrderId AS BIGINT) * 1000000000000
        + CAST(psl.PomSpecLineId AS BIGINT) * 1000000
        + CAST(os.SizeRunSizeId AS BIGINT) AS QcOrderPomSizeId,
    CAST(o.QcOrderId AS BIGINT) * 1000000000 + CAST(psl.PomSpecLineId AS BIGINT) AS QcOrderPomId,
    o.QcOrderId,
    psl.PomSpecLineId,
    os.SizeRunSizeId,
    srs.SizeLabel,
    srs.SizeOrder,
    COUNT(r.QcResultId) AS SampleCount,
    SUM(CASE WHEN r.[Pass] = 0 THEN 1 ELSE 0 END) AS FailCount,
    SUM(CASE WHEN r.[Pass] = 1 THEN 1 ELSE 0 END) AS PassCount,
    SUM(CASE WHEN r.[Pass] IS NULL AND r.QcResultId IS NOT NULL THEN 1 ELSE 0 END) AS PendingCount,
    AVG(r.FinalDiff) AS AvgFinalDiff
FROM dbo.TchpQcOrder AS o
INNER JOIN dbo.TchpQcOrderSize AS os
    ON os.QcOrderId = o.QcOrderId
INNER JOIN dbo.TchpSizeRunSize AS srs
    ON srs.SizeRunSizeId = os.SizeRunSizeId
INNER JOIN dbo.TchpPomSpecLine AS psl
    ON psl.StyleSpecId = o.StyleSpecId
LEFT JOIN dbo.TchpQcGarment AS g
    ON g.QcOrderId = o.QcOrderId
   AND g.SizeRunSizeId = os.SizeRunSizeId
LEFT JOIN dbo.TchpQcResult AS r
    ON r.QcGarmentId = g.QcGarmentId
   AND r.PomSpecLineId = psl.PomSpecLineId
GROUP BY
    o.QcOrderId,
    psl.PomSpecLineId,
    os.SizeRunSizeId,
    srs.SizeLabel,
    srs.SizeOrder;
GO

PRINT 'Created View_TchpQcOrderPomSizeResult';
GO

PRINT '=== POM_Grading_QC_NewSchema.sql completed ===';
GO
