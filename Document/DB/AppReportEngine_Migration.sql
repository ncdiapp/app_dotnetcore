-- ============================================================
-- Print Engine Migration
-- Run once per tenant DB
-- ============================================================

-- 1. Report template config (HTML template + data source + page setup)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AppReportTemplate')
BEGIN
    CREATE TABLE AppReportTemplate (
        ReportTemplateId    INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
        ReportId            INT             NOT NULL,
        TemplateHtml        NVARCHAR(MAX)   NULL,
        DataSpName          NVARCHAR(200)   NULL,
        DataApiPath         NVARCHAR(500)   NULL,
        PageSize            NVARCHAR(10)    NOT NULL DEFAULT 'A4',
        Orientation         NVARCHAR(11)    NOT NULL DEFAULT 'portrait',
        MarginMm            INT             NOT NULL DEFAULT 15,
        ExtraParamConfig    NVARCHAR(MAX)   NULL,
        CreatedDate         DATETIME        NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate        DATETIME        NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_AppReportTemplate_AppReport FOREIGN KEY (ReportId)
            REFERENCES AppReport (ReportID) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UX_AppReportTemplate_ReportId ON AppReportTemplate (ReportId);
END
GO

-- 2. Print request store (for batch / search-view print)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AppReportRequest')
BEGIN
    CREATE TABLE AppReportRequest (
        RequestId               INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
        ReportId                INT             NOT NULL,
        MainReferenceId         INT             NOT NULL,
        MasterReferenceId       INT             NULL,
        MultipleReferenceIds    NVARCHAR(MAX)   NULL,
        ParameterMapping        NVARCHAR(MAX)   NULL,
        RequestedBy             INT             NOT NULL,
        RequestedAt             DATETIME        NOT NULL DEFAULT GETUTCDATE(),
        Status                  INT             NOT NULL DEFAULT 0,
        CONSTRAINT FK_AppReportRequest_AppReport FOREIGN KEY (ReportId)
            REFERENCES AppReport (ReportID)
    );
END
GO

-- 3. Print audit log
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AppReportLog')
BEGIN
    CREATE TABLE AppReportLog (
        LogId               INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
        ReportId            INT             NOT NULL,
        RequestId           INT             NULL,
        MainReferenceId     INT             NOT NULL,
        RequestedBy         INT             NOT NULL,
        RequestedAt         DATETIME        NOT NULL DEFAULT GETUTCDATE(),
        DurationMs          INT             NULL,
        PdfPageCount        INT             NULL,
        ClientIp            NVARCHAR(50)    NULL
    );
END
GO
