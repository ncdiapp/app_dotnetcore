-- update_grid: one call updates every row when rowKey and match are omitted.

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Add/update/delete grid rows. Does not save. ONE call. Update all rows: op=update, omit rowKey and match, fieldsJson={col:value}. Update a subset: match={col:value}. Add: fieldsJson array of every new row. Do not loop per row.',
    CallNotes = N'All rows: omit rowKey and match. Subset: match once. Same fieldsJson must not be sent once per rowKey. Add every new row in one array.',
    ParameterSchemaJson = N'{"unitId":{"type":"string","description":"Grid unitId or name"},"op":{"type":"string","description":"add | update | delete","required":true},"rowKey":{"type":"string","description":"Only for a single row. Omit with match omitted to update/delete every row."},"match":{"type":"string","description":"JSON object selecting rows. Omit with rowKey omitted = all rows."},"parentRowKey":{"type":"string","description":"Parent row for a subgrid"},"fieldsJson":{"type":"string","description":"Update: one JSON object of column to value. Add: JSON array of every new row."}}'
WHERE LibraryKey = N'platform-form-host' AND ToolName = N'update_grid';

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Add/update/delete grid rows. ONE call. All rows: omit rowKey and match. Subset: match={col:value}. Do not loop per row.',
    CallNotes = N'All rows: omit rowKey and match. Never one update_grid per row for the same column change.'
WHERE ToolName = N'update_grid';
GO
