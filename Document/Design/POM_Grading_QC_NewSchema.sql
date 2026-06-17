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
--   TchpQcGarment             Individual sampled garment
--   TchpQcResult              QC measurement result per garment/POM/size
--   TchpSizeRunDimension      Global mapping: size run size → dimension code
--   TchpSizeSystemMapping     Multi-region size equivalence (US/EU/UK/JP)
-- ============================================================

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
-- Spec aggregate root — one per style, versioned on approval.
-- ProductReferenceId matches existing PLM FK naming convention.
-- BaseSizeDetailId: suggested base size; matches TchpPomTemplate.DefaultBaseSizeId convention.
IF OBJECT_ID(N'dbo.TchpStyleSpec', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpStyleSpec] (
        [StyleSpecId]           INT             IDENTITY(1,1)   NOT NULL,
        -- FK → product/style (ProductReferenceId convention from PdmProductQcSize)
        [ProductReferenceId]    INT             NOT NULL,
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
    CREATE NONCLUSTERED INDEX [IX_TchpStyleSpec_ProductRef]
        ON [dbo].[TchpStyleSpec] ([ProductReferenceId] ASC);
    PRINT 'Created TchpStyleSpec';
END
ELSE
    PRINT 'TchpStyleSpec already exists — skipped';
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
-- SampleSize is computed by AqlSamplingService (BL) and stored here.
IF OBJECT_ID(N'dbo.TchpQcOrder', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpQcOrder] (
        [QcOrderId]             INT             IDENTITY(1,1)   NOT NULL,
        [ProductReferenceId]    INT             NOT NULL,
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
    CREATE NONCLUSTERED INDEX [IX_TchpQcOrder_ProductRef]
        ON [dbo].[TchpQcOrder] ([ProductReferenceId] ASC);
    PRINT 'Created TchpQcOrder';
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
-- GarmentPassStatus: NULL = not yet evaluated, 1 = pass, 0 = fail.
IF OBJECT_ID(N'dbo.TchpQcGarment', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpQcGarment] (
        [QcGarmentId]           INT             IDENTITY(1,1)   NOT NULL,
        [QcOrderId]             INT             NOT NULL,
        [GarmentSerial]         NVARCHAR(50)    NOT NULL,
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
            FOREIGN KEY ([QcOrderId]) REFERENCES [dbo].[TchpQcOrder] ([QcOrderId])
    );
    CREATE NONCLUSTERED INDEX [IX_TchpQcGarment_Order]
        ON [dbo].[TchpQcGarment] ([QcOrderId] ASC);
    PRINT 'Created TchpQcGarment';
END
ELSE
    PRINT 'TchpQcGarment already exists — skipped';
GO

-- ── TchpQcResult ─────────────────────────────────────────────
-- QC measurement per garment × POM × size, all four wash stages.
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

PRINT '=== POM_Grading_QC_NewSchema.sql completed ===';
GO
