-- V054: Layer 1 working memory — persist the rolling conversation summary per chat,
-- so older turns are summarised once instead of on every run.
-- SummarizedTurnCount = number of leading text turns folded into SummaryText.
-- SummaryFingerprint  = hash of the first turn, used to discard the summary if the history diverged.

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppGenericAgentSession', N'SummaryText') IS NULL
BEGIN
    ALTER TABLE dbo.AppGenericAgentSession ADD SummaryText NVARCHAR(MAX) NULL;
END
GO

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppGenericAgentSession', N'SummarizedTurnCount') IS NULL
BEGIN
    ALTER TABLE dbo.AppGenericAgentSession ADD SummarizedTurnCount INT NOT NULL
        CONSTRAINT DF_AppGenericAgentSession_SummarizedTurnCount DEFAULT 0;
END
GO

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppGenericAgentSession', N'SummaryFingerprint') IS NULL
BEGIN
    ALTER TABLE dbo.AppGenericAgentSession ADD SummaryFingerprint NVARCHAR(64) NULL;
END
GO
