import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

// Enums
export enum EmLLMProvider {
    OpenAI = 1,
    Gemini = 2,
    Anthropic = 3
}

// Interfaces
export interface DbGenieColumnMetadataDto {
    Name: string;
    DataType: string;
    Length?: number;
    Precision?: number;
    Scale?: number;
    IsPrimaryKey: boolean;
    IsNullable: boolean;
    IsAutoIncrement: boolean;
    DefaultValue?: string;
    Description?: string;
}

export interface DbGenieRelationshipMetadataDto {
    Type: string;
    TargetTable: string;
    ForeignKeyColumn: string;
    ReferencedColumn?: string;
    ConstraintName?: string;
}

export interface DbGenieTableMetadataDto {
    Name: string;
    SchemaOwner?: string;
    Description?: string;
    Columns: DbGenieColumnMetadataDto[];
    Relationships: DbGenieRelationshipMetadataDto[];
}

export interface SchemaExtractionResultDto {
    Tables: DbGenieTableMetadataDto[];
    RawLLMResponse?: string;
    IsSuccess: boolean;
    Error?: string;
    GeneratedScript?: string;
}

export interface SQLValidationResultDto {
    IsValid: boolean;
    Warnings: string[];
    BlockedCommands: string[];
    IsDestructive: boolean;
    Command?: string;
    ModifiedSQL?: string;
}

export interface NL2SQLResultDto {
    GeneratedSQL: string;
    IsSuccess: boolean;
    Error?: string;
    Explanation?: string;
    Validation?: SQLValidationResultDto;
}

export interface DbGenieChatMessageDto {
    Role: string;
    Content: string;
    Timestamp: string;
    MessageId: string;
    GeneratedSQL?: string;
    HasSQL: boolean;
}

export interface DbGenieChatResponseDto {
    Message: DbGenieChatMessageDto;
    IsSuccess: boolean;
    Error?: string;
    SessionId: string;
}

export interface ExecuteSQLResultDto {
    IsSuccess: boolean;
    Error?: string;
    RowsAffected?: number;
    Results: Record<string, any>[];
    ColumnNames: string[];
    RequiresConfirmation: boolean;
    ConfirmationMessage?: string;
}

export interface LLMProviderInfoDto {
    Provider: EmLLMProvider;
    Name: string;
    Description: string;
    SupportedModels: string[];
    DefaultModel: string;
}

export interface OperationCallResult<T> {
    Object: T;
    ObjectList?: T[];
    ValidationResult: {
        Items: Array<{
            Type: string;
            PropertyName: string;
            Message: string;
        }>;
        IsValid: boolean;
    };
    IsSuccessful: boolean;
    HasResult: boolean;
}

export interface DbGenieCreateTransactionRequestDto {
    RequirementsText: string;
    DataSourceRegisterId?: number;
    SchemaOwner?: string;
    TransactionName?: string;
    SaasApplicationId?: number;
}

export interface DbGenieCreateTransactionResultDto {
    SchemaExtraction?: SchemaExtractionResultDto;
    CreatedTableScripts?: string;
    IsSuccess: boolean;
    Error?: string;
}

// Request DTOs
export interface ExtractSchemaRequestDto {
    Content?: string;
    FileName?: string;
    FileContent?: string; // Base64 encoded
    LLMProvider: EmLLMProvider;
    ApiKey: string;
    DataSourceRegisterId?: number;
}

export interface GenerateScriptRequestDto {
    Schema: SchemaExtractionResultDto;
    SchemaOwner?: string;
}

export interface ExecuteSQLRequestDto {
    SQL: string;
    DataSourceRegisterId?: number;
    RequireConfirmation: boolean;
    IsConfirmed: boolean;
}

export interface NL2SQLRequestDto {
    Question: string;
    DataSourceRegisterId?: number;
    SchemaContext?: DbGenieTableMetadataDto[];
    LLMProvider: EmLLMProvider;
    ApiKey: string;
}

export interface DataSourceOptionDto {
    DataSourceRegisterId?: number;
    Id?: number;
    DataSourceName: string;
    DataSourceType?: number;
    DatabaseName?: string;
}

export interface DbGenieChatRequestDto {
    Message: string;
    SessionId?: string;
    DataSourceRegisterId?: number;
    DbDialect?: string;
    LLMProvider: EmLLMProvider;
    ApiKey: string;
    ConversationHistory?: DbGenieChatMessageDto[];
}

export interface ValidateApiKeyRequestDto {
    Provider: EmLLMProvider;
    ApiKey: string;
}

class DbGenieService {
    private baseUrl = `${endpoints.BASE_URL}/webapi/DbGenie`;

    /**
     * Extracts database schema from document or text
     */
    async extractSchemaFromDocument(request: ExtractSchemaRequestDto): Promise<OperationCallResult<SchemaExtractionResultDto>> {
        const response = await fetch(`${this.baseUrl}/ExtractSchemaFromDocument`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(request)
        });
        if (!response.ok) throw new Error('Failed to extract schema from document');
        return response.json();
    }

    /**
     * Extracts schema from file (handles file to base64 conversion)
     */
    async extractSchemaFromFile(
        file: File,
        dataSourceRegisterId?: number
    ): Promise<OperationCallResult<SchemaExtractionResultDto>> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = async () => {
                try {
                    const base64 = (reader.result as string).split(',')[1];
                    const request: ExtractSchemaRequestDto = {
                        FileName: file.name,
                        FileContent: base64,
                        LLMProvider: EmLLMProvider.OpenAI,
                        ApiKey: '',
                        DataSourceRegisterId: dataSourceRegisterId
                    };
                    const result = await this.extractSchemaFromDocument(request);
                    resolve(result);
                } catch (error) {
                    reject(error);
                }
            };
            reader.onerror = () => reject(new Error('Failed to read file'));
            reader.readAsDataURL(file);
        });
    }

    /**
     * Extracts schema from text content
     */
    async extractSchemaFromText(
        content: string,
        dataSourceRegisterId?: number
    ): Promise<OperationCallResult<SchemaExtractionResultDto>> {
        const request: ExtractSchemaRequestDto = {
            Content: content,
            LLMProvider: EmLLMProvider.OpenAI,
            ApiKey: '',
            DataSourceRegisterId: dataSourceRegisterId
        };
        return this.extractSchemaFromDocument(request);
    }

    /**
     * Generates CREATE TABLE scripts from extracted schema
     */
    async generateCreateScript(
        schema: SchemaExtractionResultDto,
        schemaOwner?: string
    ): Promise<OperationCallResult<string>> {
        const request: GenerateScriptRequestDto = {
            Schema: schema,
            SchemaOwner: schemaOwner
        };
        const response = await fetch(`${this.baseUrl}/GenerateCreateScript`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(request)
        });
        if (!response.ok) throw new Error('Failed to generate create script');
        return response.json();
    }

    /**
     * Executes SQL query with safety validation
     */
    async executeSQL(
        sql: string,
        dataSourceRegisterId?: number,
        requireConfirmation: boolean = true,
        isConfirmed: boolean = false
    ): Promise<OperationCallResult<ExecuteSQLResultDto>> {
        const request: ExecuteSQLRequestDto = {
            SQL: sql,
            DataSourceRegisterId: dataSourceRegisterId,
            RequireConfirmation: requireConfirmation,
            IsConfirmed: isConfirmed
        };
        const response = await fetch(`${this.baseUrl}/ExecuteSQL`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(request)
        });
        if (!response.ok) throw new Error('Failed to execute SQL');
        return response.json();
    }

    /**
     * Gets schema context (tables and columns) for a data source
     */
    async getSchemaContext(dataSourceRegisterId: number): Promise<OperationCallResult<DbGenieTableMetadataDto[]>> {
        const response = await fetch(`${this.baseUrl}/GetSchemaContext?dataSourceRegisterId=${dataSourceRegisterId}`, {
            headers: getHeaders()
        });
        if (!response.ok) throw new Error('Failed to get schema context');
        return response.json();
    }

    /**
     * Converts natural language question to SQL query
     */
    async naturalLanguageToSQL(request: NL2SQLRequestDto): Promise<OperationCallResult<NL2SQLResultDto>> {
        const response = await fetch(`${this.baseUrl}/NaturalLanguageToSQL`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(request)
        });
        if (!response.ok) throw new Error('Failed to convert natural language to SQL');
        return response.json();
    }

    /**
     * Shorthand for NL2SQL
     */
    async askQuestion(
        question: string,
        dataSourceRegisterId?: number
    ): Promise<OperationCallResult<NL2SQLResultDto>> {
        const request: NL2SQLRequestDto = {
            Question: question,
            LLMProvider: EmLLMProvider.OpenAI,
            ApiKey: '',
            DataSourceRegisterId: dataSourceRegisterId
        };
        return this.naturalLanguageToSQL(request);
    }

    /**
     * Handles chat interaction with DBA-Genie agent
     */
    async chatWithAgent(request: DbGenieChatRequestDto): Promise<OperationCallResult<DbGenieChatResponseDto>> {
        const response = await fetch(`${this.baseUrl}/ChatWithAgent`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(request)
        });
        if (!response.ok) throw new Error('Failed to chat with agent');
        return response.json();
    }

    /**
     * Shorthand for chatting
     */
    async sendMessage(
        message: string,
        sessionId?: string,
        dataSourceRegisterId?: number,
        conversationHistory?: DbGenieChatMessageDto[],
        dbDialect?: string
    ): Promise<OperationCallResult<DbGenieChatResponseDto>> {
        const request: DbGenieChatRequestDto = {
            Message: message,
            SessionId: sessionId,
            DataSourceRegisterId: dataSourceRegisterId,
            DbDialect: dbDialect,
            LLMProvider: EmLLMProvider.OpenAI,
            ApiKey: '',
            ConversationHistory: conversationHistory
        };
        return this.chatWithAgent(request);
    }

    /**
     * Gets list of available data sources
     */
    async getDataSourceList(): Promise<DataSourceOptionDto[]> {
        const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetDataSourceRegisterList?withAppMasterdb=false`, {
            headers: getHeaders()
        });
        if (!response.ok) throw new Error('Failed to get data source list');
        const result = await response.json();
        return result?.ObjectList ?? (Array.isArray(result) ? result : []);
    }

    /**
     * Gets list of supported LLM providers
     */
    async getLLMProviders(): Promise<OperationCallResult<LLMProviderInfoDto[]>> {
        const response = await fetch(`${this.baseUrl}/GetLLMProviders`, {
            headers: getHeaders()
        });
        if (!response.ok) throw new Error('Failed to get LLM providers');
        return response.json();
    }

    /**
     * Validates an LLM API key
     */
    async validateLLMApiKey(provider: EmLLMProvider, apiKey: string): Promise<OperationCallResult<boolean>> {
        const request: ValidateApiKeyRequestDto = {
            Provider: provider,
            ApiKey: apiKey
        };
        const response = await fetch(`${this.baseUrl}/ValidateLLMApiKey`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(request)
        });
        if (!response.ok) throw new Error('Failed to validate API key');
        return response.json();
    }

    /**
     * Full pipeline: LLM extracts schema from requirements text, creates physical tables
     * on the target data source, then builds the AppTransaction hierarchy automatically.
     */
    async createHierarchyTransactionFromRequirements(
        request: DbGenieCreateTransactionRequestDto
    ): Promise<OperationCallResult<DbGenieCreateTransactionResultDto>> {
        const response = await fetch(`${this.baseUrl}/CreateHierarchyTransactionFromRequirements`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(request)
        });
        if (!response.ok) throw new Error('Failed to create hierarchy transaction from requirements');
        return response.json();
    }

    /**
     * Validates SQL for safety without executing
     */
    async validateSQL(sql: string): Promise<OperationCallResult<SQLValidationResultDto>> {
        const response = await fetch(`${this.baseUrl}/ValidateSQL`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(sql)
        });
        if (!response.ok) throw new Error('Failed to validate SQL');
        return response.json();
    }
}

export const dbGenieService = new DbGenieService();
