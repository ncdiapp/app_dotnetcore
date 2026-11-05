-- Catalog-only HostContextJson; update_grid match; get_form_state slice. Apply is Calculation-style on posted FormData.
UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Read live values from the open form. Optional unitId (or unit name), match JSON object of column to value, take, parentRowKey for a subgrid. Does not return the full catalog.',
    ParameterSchemaJson = N'{"unitId":{"type":"string","description":"Grid or unit id or display name"},"rowKey":{"type":"string","description":"Optional row key"},"match":{"type":"string","description":"JSON object of db or label to value, e.g. {\"FabricCode\":\"Woven ABC123\"}"},"take":{"type":"string","description":"Max rows, default 20"},"parentRowKey":{"type":"string","description":"Parent grid row for a subgrid"}}'
WHERE LibraryKey = N'platform-form-host' AND ToolName = N'get_form_state';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Set header or sibling 1-1 fields on the open form. Does not save. fieldsJson is db or label to value. Dropdown display names are resolved on the server.',
    ParameterSchemaJson = N'{"fieldsJson":{"type":"string","description":"JSON object of field to value","required":true},"rowKey":{"type":"string","description":"List Edit row key"}}'
WHERE LibraryKey = N'platform-form-host' AND ToolName = N'set_fields';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Add/update/delete rows on a form grid. Does not save. Address the grid by unitId or name. For update, pass match to change every matching row in one call (example: match {\"FabricCode\":\"Woven ABC123\"} and fieldsJson {\"Status\":\"Approved\"}). For add, pass ALL new rows in one fieldsJson JSON array. Do not call once per row.',
    ParameterSchemaJson = N'{"unitId":{"type":"string","description":"Grid unitId or display name from HostContextJson.units"},"op":{"type":"string","description":"add | update | delete","required":true},"rowKey":{"type":"string","description":"Optional single row key"},"match":{"type":"string","description":"JSON object to select rows, e.g. {\"FabricCode\":\"Woven ABC123\"}"},"parentRowKey":{"type":"string","description":"Parent row for a subgrid"},"fieldsJson":{"type":"string","description":"For add: JSON array of row objects. For update: one JSON object of column to value."}}'
WHERE LibraryKey = N'platform-form-host' AND ToolName = N'update_grid';
GO
