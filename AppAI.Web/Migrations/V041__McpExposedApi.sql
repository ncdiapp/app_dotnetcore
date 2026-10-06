-- V041: Which APIs external MCP users (Claude Desktop / ChatGPT Desktop, ...) may use, controlled by security group.
-- An API is reachable through MCP only when it is catalogued here, enabled, AND the calling user belongs to at least
-- one security group granted on it (AppSecurityGroupMember). No row = not exposed (deny by default).
-- OperationId / AppSource are the gateway's Swagger identifiers (ApiSources:Sources[].Name and the operationId).

IF OBJECT_ID(N'dbo.AppMcpExposedApi', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppMcpExposedApi
    (
        ExposedApiId INT IDENTITY(1,1) NOT NULL,
        AppSource    NVARCHAR(100)  NOT NULL,
        OperationId  NVARCHAR(300)  NOT NULL,
        HttpMethod   NVARCHAR(10)   NULL,                 -- snapshot for the admin screen only
        ApiPath      NVARCHAR(500)  NULL,                 -- snapshot for the admin screen only
        Summary      NVARCHAR(500)  NULL,                 -- snapshot for the admin screen only
        IsEnabled    BIT            NOT NULL CONSTRAINT DF_AppMcpExposedApi_IsEnabled DEFAULT 1,
        CreatedUtc   DATETIME2      NOT NULL CONSTRAINT DF_AppMcpExposedApi_CreatedUtc DEFAULT SYSUTCDATETIME(),
        ModifiedUtc  DATETIME2      NOT NULL CONSTRAINT DF_AppMcpExposedApi_ModifiedUtc DEFAULT SYSUTCDATETIME(),
        ModifiedById INT            NULL,                 -- AppMasterDB.dbo.AppSecurityUser.UserId of the admin
        CONSTRAINT PK_AppMcpExposedApi PRIMARY KEY (ExposedApiId)
    );

    CREATE UNIQUE INDEX UX_AppMcpExposedApi_Operation ON dbo.AppMcpExposedApi (AppSource, OperationId);
END
GO

IF OBJECT_ID(N'dbo.AppMcpExposedApiGroup', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppMcpExposedApiGroup
    (
        ExposedApiId INT NOT NULL,
        GroupID      INT NOT NULL,                        -- dbo.AppSecurityGroup.GroupID
        CONSTRAINT PK_AppMcpExposedApiGroup PRIMARY KEY (ExposedApiId, GroupID),
        -- Deleting an API or a security group removes the grant: access can only shrink, never silently widen.
        CONSTRAINT FK_AppMcpExposedApiGroup_Api   FOREIGN KEY (ExposedApiId) REFERENCES dbo.AppMcpExposedApi (ExposedApiId) ON DELETE CASCADE,
        CONSTRAINT FK_AppMcpExposedApiGroup_Group FOREIGN KEY (GroupID)      REFERENCES dbo.AppSecurityGroup (GroupID)      ON DELETE CASCADE
    );

    CREATE INDEX IX_AppMcpExposedApiGroup_Group ON dbo.AppMcpExposedApiGroup (GroupID);
END
GO
