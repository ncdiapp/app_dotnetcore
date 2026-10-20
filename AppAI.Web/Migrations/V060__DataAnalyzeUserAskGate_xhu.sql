-- V060: Gate data_analyze — require user_ask; forbid speculative / show-export-only calls.

DECLARE @desc nvarchar(max) = N'GATED. Call ONLY when the current user message explicitly asks to analyze, aggregate, summarize metrics, compute sum/avg/min/max/count/distinct/distribution, or shape data for a KPI/chart/dashboard they requested.
Do NOT call for search, list, show, card/table render, top-N display (use render limit), or any export.
Required: user_ask = short quote or paraphrase of the user words that justify this call. If you cannot fill user_ask from the user message, do not call this tool.
Prefer dataset_name from a prior tool that returned dataset_cached. Operations: summary|count|sum|avg|min|max|distinct|top|distribution. Optional: column, group_by, filter, filter_column, limit, order_by. Do not re-fetch large tables; do not dump raw rows into FinalResponse.';

DECLARE @schema nvarchar(max) = N'{
  "user_ask":{"type":"string","description":"Required. Quote or paraphrase the user words that request analysis/aggregation/KPI/chart shaping. Empty or show/export-only text is rejected.","required":true},
  "operation":{"type":"string","description":"summary | count | sum | avg | min | max | distinct | top | distribution — must match what user_ask asks for","required":true},
  "dataset_name":{"type":"string","description":"Cached dataset name from a prior tool envelope"},
  "dataJson":{"type":"string","description":"Optional inline JSON array / {Rows|data:[...]} when no cache yet"},
  "column":{"type":"string","description":"Target column for sum/avg/min/max/distinct/top/distribution"},
  "group_by":{"type":"string","description":"Group key for count/sum/avg/min/max"},
  "filter":{"type":"string","description":"Substring filter"},
  "filter_column":{"type":"string","description":"Column to apply filter to"},
  "limit":{"type":"string","description":"Max groups/rows to return (default 20). Not a substitute for render limit on card/table display."},
  "order_by":{"type":"string","description":"asc | desc"}
}';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = @desc,
    ParameterSchemaJson = @schema
WHERE LibraryKey = N'data-analyze'
  AND ToolName = N'data_analyze';

UPDATE dbo.AppAgentToolLibrary
SET Description = N'Gated analysis: data_analyze runs only when the user asks to analyze/aggregate/shape metrics. Not for show/render/export.'
WHERE LibraryKey = N'data-analyze';

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = @desc,
    ParameterSchemaJson = @schema
WHERE ToolName = N'data_analyze'
  AND ToolType = N'BuiltIn';
GO
