-- V018: Add AgentUi to AppAgentSkillSet.
-- Selects which agent chat shell is used for Run preview (and later published instances).
-- EmAppAgentUi: 0=Unspecified(→GenericChat), 1=GenericChat, 2=ConfigurationAndIntegration,
--               3=DbManagement, 4=ImageAndFileProcess

IF COL_LENGTH('dbo.AppAgentSkillSet', 'AgentUi') IS NULL
BEGIN
    ALTER TABLE dbo.AppAgentSkillSet
        ADD AgentUi INT NOT NULL CONSTRAINT DF_AppAgentSkillSet_AgentUi DEFAULT (1);
END
GO

-- data-integration uses Configuration and Integration UI; others stay Generic Chat (1)
UPDATE dbo.AppAgentSkillSet
SET AgentUi = 2
WHERE SkillKey = N'data-integration';
GO
