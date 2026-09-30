-- V038: Remove ask_user from platform-multi-agent library (and stray private registers).
-- ask_user is a system built-in auto-injected by GenericAgentEngine for Interactive agents —
-- it must not appear as a library tool or private BuiltIn registration.
-- Idempotent DELETE.

DELETE FROM dbo.AppAgentLibraryTool
WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'ask_user';
GO

DELETE FROM dbo.AppAgentToolRegister
WHERE ToolName = N'ask_user';
GO
