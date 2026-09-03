-- V012: Fix BuiltIn tool TypeNames — V008 seeded 'APP.BL.' (all-caps) but the
--       actual C# namespace is 'App.BL.' (mixed-case). Assembly.GetType() is
--       case-sensitive, so every BuiltIn tool call failed with "type not found".

UPDATE dbo.AppAgentToolRegister
SET ToolConfig = REPLACE(ToolConfig, '"TypeName":"APP.BL.AppBuilderAgent.', '"TypeName":"App.BL.AppBuilderAgent.')
WHERE ToolConfig LIKE '%"TypeName":"APP.BL.AppBuilderAgent.%';
GO
