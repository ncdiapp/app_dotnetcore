using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// Enum for supported LLM providers
    /// </summary>
    public enum EmLLMProvider
    {
        OpenAI = 1,
        Gemini = 2,
        Anthropic = 3
    }

    /// <summary>
    /// Request DTO for LLM API calls
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class LLMRequestDto
    {
        [DataMember]
        public EmLLMProvider Provider { get; set; }

        [DataMember]
        public string Prompt { get; set; }

        [DataMember]
        public string ApiKey { get; set; }

        [DataMember]
        public string Model { get; set; }

        [DataMember]
        public string SystemPrompt { get; set; }

        [DataMember]
        public double? Temperature { get; set; }

        [DataMember]
        public int? MaxTokens { get; set; }
    }

    /// <summary>
    /// Response DTO from LLM API calls
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class LLMResponseDto
    {
        [DataMember]
        public string Content { get; set; }

        [DataMember]
        public int? TokensUsed { get; set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string Model { get; set; }
    }

    /// <summary>
    /// Column metadata extracted from schema or LLM
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DbGenieColumnMetadataDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string DataType { get; set; }

        [DataMember]
        public int? Length { get; set; }

        [DataMember]
        public int? Precision { get; set; }

        [DataMember]
        public int? Scale { get; set; }

        [DataMember]
        public bool IsPrimaryKey { get; set; }

        [DataMember]
        public bool IsNullable { get; set; }

        [DataMember]
        public bool IsAutoIncrement { get; set; }

        [DataMember]
        public string DefaultValue { get; set; }

        [DataMember]
        public string Description { get; set; }
    }

    /// <summary>
    /// Relationship metadata between tables
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DbGenieRelationshipMetadataDto
    {
        [DataMember]
        public string Type { get; set; } // ONE_TO_MANY, MANY_TO_ONE, ONE_TO_ONE, MANY_TO_MANY

        [DataMember]
        public string TargetTable { get; set; }

        [DataMember]
        public string ForeignKeyColumn { get; set; }

        [DataMember]
        public string ReferencedColumn { get; set; }

        [DataMember]
        public string ConstraintName { get; set; }
    }

    /// <summary>
    /// Table metadata extracted from schema or LLM
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DbGenieTableMetadataDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string SchemaOwner { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public List<DbGenieColumnMetadataDto> Columns { get; set; }

        [DataMember]
        public List<DbGenieRelationshipMetadataDto> Relationships { get; set; }

        /// <summary>
        /// True = this table is a standalone lookup/reference entity (List Edit screen).
        /// Its ONE_TO_MANY relationships are associations, NOT composition — it must NOT appear
        /// as master/child in the master-detail hierarchy.
        /// Set during schema extraction in SchemaDesignerPlugin.
        /// </summary>
        [DataMember]
        public bool IsLookup { get; set; }

        public DbGenieTableMetadataDto()
        {
            Columns = new List<DbGenieColumnMetadataDto>();
            Relationships = new List<DbGenieRelationshipMetadataDto>();
        }
    }

    /// <summary>
    /// Result of schema extraction from text/document
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class SchemaExtractionResultDto
    {
        [DataMember]
        public List<DbGenieTableMetadataDto> Tables { get; set; }

        [DataMember]
        public string RawLLMResponse { get; set; }

        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public string GeneratedScript { get; set; }

        public SchemaExtractionResultDto()
        {
            Tables = new List<DbGenieTableMetadataDto>();
        }
    }

    /// <summary>
    /// Request to extract schema from document or text
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ExtractSchemaRequestDto
    {
        [DataMember]
        public string Content { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public byte[] FileContent { get; set; }

        [DataMember]
        public EmLLMProvider LLMProvider { get; set; }

        [DataMember]
        public string ApiKey { get; set; }

        [DataMember]
        public int? DataSourceRegisterId { get; set; }
    }

    /// <summary>
    /// SQL validation result
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class SQLValidationResultDto
    {
        [DataMember]
        public bool IsValid { get; set; }

        [DataMember]
        public List<string> Warnings { get; set; }

        [DataMember]
        public List<string> BlockedCommands { get; set; }

        [DataMember]
        public bool IsDestructive { get; set; }

        [DataMember]
        public string Command { get; set; }

        [DataMember]
        public string ModifiedSQL { get; set; }

        public SQLValidationResultDto()
        {
            Warnings = new List<string>();
            BlockedCommands = new List<string>();
            IsValid = true;
        }
    }

    /// <summary>
    /// Request to convert natural language to SQL
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class NL2SQLRequestDto
    {
        [DataMember]
        public string Question { get; set; }

        [DataMember]
        public int? DataSourceRegisterId { get; set; }

        [DataMember]
        public List<DbGenieTableMetadataDto> SchemaContext { get; set; }

        [DataMember]
        public EmLLMProvider LLMProvider { get; set; }

        [DataMember]
        public string ApiKey { get; set; }

        public NL2SQLRequestDto()
        {
            SchemaContext = new List<DbGenieTableMetadataDto>();
        }
    }

    /// <summary>
    /// Result of NL2SQL conversion
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class NL2SQLResultDto
    {
        [DataMember]
        public string GeneratedSQL { get; set; }

        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public string Explanation { get; set; }

        [DataMember]
        public SQLValidationResultDto Validation { get; set; }
    }

    /// <summary>
    /// Chat message in a DBA-Genie conversation
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DbGenieChatMessageDto
    {
        [DataMember]
        public string Role { get; set; } // user, assistant, system

        [DataMember]
        public string Content { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public string MessageId { get; set; }

        [DataMember]
        public string GeneratedSQL { get; set; }

        [DataMember]
        public bool HasSQL { get; set; }

        public DbGenieChatMessageDto()
        {
            Timestamp = DateTime.UtcNow;
            MessageId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Request to chat with DBA-Genie agent
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DbGenieChatRequestDto
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string SessionId { get; set; }

        [DataMember]
        public int? DataSourceRegisterId { get; set; }

        [DataMember]
        public EmLLMProvider LLMProvider { get; set; }

        [DataMember]
        public string ApiKey { get; set; }

        [DataMember]
        public List<DbGenieChatMessageDto> ConversationHistory { get; set; }

        [DataMember]
        public string DbDialect { get; set; }

        public DbGenieChatRequestDto()
        {
            ConversationHistory = new List<DbGenieChatMessageDto>();
        }
    }

    /// <summary>
    /// Response from DBA-Genie chat
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DbGenieChatResponseDto
    {
        [DataMember]
        public DbGenieChatMessageDto Message { get; set; }

        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public string SessionId { get; set; }
    }

    /// <summary>
    /// DBA-Genie session state
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DbGenieSessionDto
    {
        [DataMember]
        public string SessionId { get; set; }

        [DataMember]
        public List<DbGenieChatMessageDto> Messages { get; set; }

        [DataMember]
        public SchemaExtractionResultDto CurrentSchema { get; set; }

        [DataMember]
        public int? DataSourceRegisterId { get; set; }

        [DataMember]
        public EmLLMProvider LLMProvider { get; set; }

        [DataMember]
        public DateTime CreatedAt { get; set; }

        [DataMember]
        public DateTime LastActivityAt { get; set; }

        public DbGenieSessionDto()
        {
            SessionId = Guid.NewGuid().ToString();
            Messages = new List<DbGenieChatMessageDto>();
            CreatedAt = DateTime.UtcNow;
            LastActivityAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Request to execute SQL
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ExecuteSQLRequestDto
    {
        [DataMember]
        public string SQL { get; set; }

        [DataMember]
        public int? DataSourceRegisterId { get; set; }

        [DataMember]
        public bool RequireConfirmation { get; set; }

        [DataMember]
        public bool IsConfirmed { get; set; }
    }

    /// <summary>
    /// Result of SQL execution
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ExecuteSQLResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public int? RowsAffected { get; set; }

        [DataMember]
        public List<Dictionary<string, object>> Results { get; set; }

        [DataMember]
        public List<string> ColumnNames { get; set; }

        [DataMember]
        public bool RequiresConfirmation { get; set; }

        [DataMember]
        public string ConfirmationMessage { get; set; }

        public ExecuteSQLResultDto()
        {
            Results = new List<Dictionary<string, object>>();
            ColumnNames = new List<string>();
        }
    }

    /// <summary>
    /// Request to generate CREATE script
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class GenerateScriptRequestDto
    {
        [DataMember]
        public SchemaExtractionResultDto Schema { get; set; }

        [DataMember]
        public string SchemaOwner { get; set; }
    }

    /// <summary>
    /// Request to validate LLM API key
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ValidateApiKeyRequestDto
    {
        [DataMember]
        public EmLLMProvider Provider { get; set; }

        [DataMember]
        public string ApiKey { get; set; }
    }

    /// <summary>
    /// Request to create hierarchy transaction from natural language requirements
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DbGenieCreateTransactionRequestDto
    {
        /// <summary>Natural language requirements text describing the data entities needed.</summary>
        [DataMember]
        public string RequirementsText { get; set; }

        /// <summary>Target data source where tables will be physically created.</summary>
        [DataMember]
        public int? DataSourceRegisterId { get; set; }

        [DataMember]
        public string SchemaOwner { get; set; }

        /// <summary>Optional name for the generated AppTransaction. Defaults to the root table display name.</summary>
        [DataMember]
        public string TransactionName { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }
    }

    /// <summary>
    /// Result of creating a hierarchy transaction from natural language requirements
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DbGenieCreateTransactionResultDto
    {
        /// <summary>Schema extracted from the requirements text by the LLM.</summary>
        [DataMember]
        public SchemaExtractionResultDto SchemaExtraction { get; set; }

        /// <summary>The CREATE TABLE SQL scripts that were executed on the target data source.</summary>
        [DataMember]
        public string CreatedTableScripts { get; set; }

        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string Error { get; set; }

        /// <summary>ID of the root AppTransaction created (use for create_search_view).</summary>
        [DataMember]
        public int TransactionId { get; set; }

        /// <summary>Display name of the root AppTransaction created.</summary>
        [DataMember]
        public string TransactionName { get; set; }

        /// <summary>
        /// Table names identified as lookup/reference tables (IsLookup=true).
        /// These are NOT included in the master-detail hierarchy — the agent must create
        /// a list-edit transaction for each one via create_entity_from_table +
        /// create_transaction_from_table + create_list_edit_form.
        /// </summary>
        [DataMember]
        public List<string> LookupTables { get; set; } = new List<string>();
    }

    /// <summary>
    /// LLM provider info
    /// </summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class LLMProviderInfoDto
    {
        [DataMember]
        public EmLLMProvider Provider { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public List<string> SupportedModels { get; set; }

        [DataMember]
        public string DefaultModel { get; set; }

        public LLMProviderInfoDto()
        {
            SupportedModels = new List<string>();
        }
    }
}
