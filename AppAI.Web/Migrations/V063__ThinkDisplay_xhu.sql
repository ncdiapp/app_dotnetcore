-- V063: (1) Reasoning loop (Think Mode) toggle on AppAgentSkillSet — default OFF.
--       (2) Bias analysis-like asks toward ask_user(KPI) + data_analyze (tool descriptions).
-- Engine also injects AnalysisPathHardRule when data_analyze is subscribed.

IF COL_LENGTH(N'dbo.AppAgentSkillSet', N'EnableThinkMode') IS NULL
BEGIN
    ALTER TABLE dbo.AppAgentSkillSet
        ADD EnableThinkMode BIT NOT NULL
            CONSTRAINT DF_AppAgentSkillSet_EnableThinkMode DEFAULT (0);
END
GO

DECLARE @analyzeDesc nvarchar(max) = N'Run aggregations on a ChatSession-cached dataset (count/sum/avg/… with optional group_by).
REQUIRED PATH for analysis-like user asks (analyze / group by / ranges / breakdown / multi-part metrics):
1) Call ask_user FIRST (contextKey=confirm_kpi_dashboard, mode=single_choice) — options kpi_dashboard vs fetch_display. Bias toward kpi_dashboard.
2) Call this tool ONLY after the user picks kpi_dashboard. Pass user_ask = quote of the user words that request analysis.
3) Prefer operation=count|distribution with group_by for each dimension the user named. Do NOT use operation=summary on an already GROUP BY''d SQL result (TotalCount/Count columns) — render with yField=measure instead.
4) Never invent analysis tables in FinalResponse when this tool exists.
If user picks fetch_display: do not call this tool — use render_*/export_* instead.
Required: user_ask. Prefer dataset_name from dataset_cached. Operations: summary|count|sum|avg|min|max|distinct|top|distribution. Optional: column, group_by, filter, filter_column, limit, order_by.';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = @analyzeDesc
WHERE LibraryKey = N'data-analyze'
  AND ToolName = N'data_analyze';

UPDATE dbo.AppAgentToolLibrary
SET Description = N'data_analyze after ask_user confirms kpi_dashboard (bias KPI path for analysis-like asks).'
WHERE LibraryKey = N'data-analyze';

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = @analyzeDesc
WHERE ToolName = N'data_analyze'
  AND ToolType = N'BuiltIn';
GO

-- Strengthen agent SystemPrompt blocks that already have the V060 analysis gate.
UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

## Analysis path bias (V063)
For analyze / group-by / ranges / multi-part metrics: ALWAYS ask_user (confirm_kpi_dashboard) before text-only answers. After kpi_dashboard: data_analyze + render_interactive dashboard. Do not skip to FinalResponse tables.'
WHERE IsActive = 1
  AND SystemPrompt LIKE N'%Data analysis intent — ask the user%'
  AND SystemPrompt NOT LIKE N'%Analysis path bias (V063)%'
  AND SystemPrompt NOT LIKE N'%Analysis path bias (V064)%';
GO
