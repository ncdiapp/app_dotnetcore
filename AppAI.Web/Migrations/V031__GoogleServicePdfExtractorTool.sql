-- V031: Google Service / PdfExtractor BuiltIn library tool.
-- Adds the shared library and subscribes all existing agent skill sets.
-- New agents can subscribe to google-service from Agent Management.

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'google-service')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES
    (N'google-service', N'platform', N'Google Service',
     N'Platform Built-in tools backed by Google Cloud services.', N'BuiltIn', 1);
GO

IF NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibraryTool
    WHERE LibraryKey = N'google-service' AND ToolName = N'PdfExtractor')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES
    (N'google-service', N'PdfExtractor',
     N'Extract an uploaded PDF tech pack into pure structured data and image artifacts using Google Document AI. Upload the PDF to the current agent session first, then pass its relative path such as source/Bugaboo Coat.pdf. The tool writes pure data to output/pdf-extraction/{jobId}/pure-data.json and returns image artifact paths.',
     N'{"type":"object","properties":{"path":{"type":"string","description":"Relative path of the uploaded PDF in the current GenericAgent session, for example source/Bugaboo Coat.pdf"}},"required":["path"]}',
     N'BuiltIn',
     N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.PdfExtractorPlugin","MethodName":"Extract"}',
     1, 10);
GO

-- Make the library available to all currently registered agents.
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
SELECT s.SkillKey, N'google-service'
FROM dbo.AppAgentSkillSet s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription x
    WHERE x.SkillKey = s.SkillKey AND x.LibraryKey = N'google-service');
GO
