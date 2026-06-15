using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using App.BL.DbGenie;
using App.BL.AppMgr.AiSkill;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

/// <summary>
/// WebAPI Controller for DBA-Genie AI Agent
/// </summary>
[Route("webapi/[controller]/[action]")]
public class DbGenieController : SecureBaseController
{
    /// <summary>
    /// Extracts database schema from uploaded document or text
    /// </summary>
    [HttpPost]
    public async Task<OperationCallResult<SchemaExtractionResultDto>> ExtractSchemaFromDocument(ExtractSchemaRequestDto request)
    {
        var result = new OperationCallResult<SchemaExtractionResultDto>();

        try
        {
            string textContent;

            // Extract text from file or use provided content
            if (request.FileContent != null && request.FileContent.Length > 0)
            {
                var extension = System.IO.Path.GetExtension(request.FileName ?? "");
                textContent = AppDbGenieBL.ExtractTextFromDocument(request.FileContent, extension);
            }
            else if (!string.IsNullOrWhiteSpace(request.Content))
            {
                textContent = request.Content;
            }
            else
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(DbGenieController),
                    "DbGenie_NoContent",
                    ValidationItemType.Error,
                    "No content provided. Please provide either file content or text content."));
                return result;
            }

            var extractionResult = await AppDbGenieBL.ExtractSchemaFromTextAsync(
                textContent,
                LLMProviderHelper.GetConfiguredProvider(),
                LLMProviderHelper.GetConfiguredApiKey()).ConfigureAwait(false);

            result.Object = extractionResult;

            if (!extractionResult.IsSuccess)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(DbGenieController),
                    "DbGenie_ExtractionError",
                    ValidationItemType.Error,
                    extractionResult.Error));
            }
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(DbGenieController),
                "DbGenie_Error",
                ValidationItemType.Error,
                $"Error extracting schema: {ex.Message}"));
        }

        return result;
    }

    /// <summary>
    /// Generates CREATE TABLE scripts from extracted schema
    /// </summary>
    [HttpPost]
    public OperationCallResult<string> GenerateCreateScript(GenerateScriptRequestDto request)
    {
        var result = new OperationCallResult<string>();

        try
        {
            if (request.Schema == null || request.Schema.Tables == null || request.Schema.Tables.Count == 0)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(DbGenieController),
                    "DbGenie_NoSchema",
                    ValidationItemType.Error,
                    "No schema provided for script generation."));
                return result;
            }

            var script = AppDbGenieBL.GenerateCreateTableScripts(
                request.Schema.Tables,
                request.SchemaOwner ?? "dbo");

            result.Object = script;
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(DbGenieController),
                "DbGenie_ScriptError",
                ValidationItemType.Error,
                $"Error generating script: {ex.Message}"));
        }

        return result;
    }

    /// <summary>
    /// Executes SQL query with safety validation
    /// </summary>
    [HttpPost]
    public OperationCallResult<ExecuteSQLResultDto> ExecuteSQL(ExecuteSQLRequestDto request)
    {
        var result = new OperationCallResult<ExecuteSQLResultDto>();

        try
        {
            var executionResult = AppDbGenieBL.ExecuteSQL(request);
            result.Object = executionResult;

            if (!executionResult.IsSuccess && !executionResult.RequiresConfirmation)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(DbGenieController),
                    "DbGenie_ExecutionError",
                    ValidationItemType.Error,
                    executionResult.Error));
            }
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(DbGenieController),
                "DbGenie_ExecutionError",
                ValidationItemType.Error,
                $"Error executing SQL: {ex.Message}"));
        }

        return result;
    }

    /// <summary>
    /// Gets schema context (tables and columns) for a data source
    /// </summary>
    [HttpGet]
    public async Task<OperationCallResult<List<DbGenieTableMetadataDto>>> GetSchemaContext(int? dataSourceRegisterId)
    {
        var result = new OperationCallResult<List<DbGenieTableMetadataDto>>();

        try
        {
            if (!dataSourceRegisterId.HasValue)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(DbGenieController),
                    "DbGenie_NoDataSource",
                    ValidationItemType.Error,
                    "Data source ID is required."));
                return result;
            }

            var tables = await AppDbGenieBL.GetSchemaContextAsync(dataSourceRegisterId.Value).ConfigureAwait(false);
            result.Object = tables;
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(DbGenieController),
                "DbGenie_SchemaError",
                ValidationItemType.Error,
                $"Error getting schema context: {ex.Message}"));
        }

        return result;
    }

    /// <summary>
    /// Converts natural language question to SQL query
    /// </summary>
    [HttpPost]
    public async Task<OperationCallResult<NL2SQLResultDto>> NaturalLanguageToSQL(NL2SQLRequestDto request)
    {
        var result = new OperationCallResult<NL2SQLResultDto>();

        try
        {
            request.LLMProvider = LLMProviderHelper.GetConfiguredProvider();
            request.ApiKey = LLMProviderHelper.GetConfiguredApiKey();
            var nl2sqlResult = await AppDbGenieBL.ConvertNaturalLanguageToSQLAsync(request).ConfigureAwait(false);
            result.Object = nl2sqlResult;

            if (!nl2sqlResult.IsSuccess)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(DbGenieController),
                    "DbGenie_NL2SQLError",
                    ValidationItemType.Error,
                    nl2sqlResult.Error));
            }
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(DbGenieController),
                "DbGenie_NL2SQLError",
                ValidationItemType.Error,
                $"Error converting to SQL: {ex.Message}"));
        }

        return result;
    }

    /// <summary>
    /// Handles chat interaction with DBA-Genie agent
    /// </summary>
    [HttpPost]
    public async Task<OperationCallResult<DbGenieChatResponseDto>> ChatWithAgent(DbGenieChatRequestDto request)
    {
        var result = new OperationCallResult<DbGenieChatResponseDto>();

        try
        {
            request.LLMProvider = LLMProviderHelper.GetConfiguredProvider();
            request.ApiKey = LLMProviderHelper.GetConfiguredApiKey();
            var chatResponse = await AppDbGenieBL.ChatWithAgentAsync(request).ConfigureAwait(false);
            result.Object = chatResponse;

            if (!chatResponse.IsSuccess)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(DbGenieController),
                    "DbGenie_ChatError",
                    ValidationItemType.Error,
                    chatResponse.Error));
            }
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(DbGenieController),
                "DbGenie_ChatError",
                ValidationItemType.Error,
                $"Error processing chat: {ex.Message}"));
        }

        return result;
    }

    /// <summary>
    /// Gets list of supported LLM providers
    /// </summary>
    [HttpGet]
    public OperationCallResult<List<LLMProviderInfoDto>> GetLLMProviders()
    {
        var result = new OperationCallResult<List<LLMProviderInfoDto>>
        {
            Object = LLMProviderHelper.GetLLMProviders()
        };

        return result;
    }

    /// <summary>
    /// Validates an LLM API key
    /// </summary>
    [HttpPost]
    public async Task<OperationCallResult<bool>> ValidateLLMApiKey(ValidateApiKeyRequestDto request)
    {
        var result = new OperationCallResult<bool>();

        try
        {
            var isValid = await LLMProviderHelper.ValidateApiKeyAsync(
                LLMProviderHelper.GetConfiguredProvider(),
                LLMProviderHelper.GetConfiguredApiKey()).ConfigureAwait(false);

            result.Object = isValid;

            if (!isValid)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(DbGenieController),
                    "DbGenie_InvalidApiKey",
                    ValidationItemType.Warning,
                    "API key validation failed. Please check your API key."));
            }
        }
        catch (Exception ex)
        {
            result.Object = false;
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(DbGenieController),
                "DbGenie_ApiKeyError",
                ValidationItemType.Error,
                $"Error validating API key: {ex.Message}"));
        }

        return result;
    }

    /// <summary>
    /// Reads natural language requirements, extracts schema via LLM, creates physical tables
    /// on the target data source, then builds the AppTransaction hierarchy automatically.
    /// </summary>
    [HttpPost]
    public async Task<OperationCallResult<DbGenieCreateTransactionResultDto>> CreateHierarchyTransactionFromRequirements(
        DbGenieCreateTransactionRequestDto request)
    {
        var result = new OperationCallResult<DbGenieCreateTransactionResultDto>();

        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RequirementsText))
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(DbGenieController),
                    "DbGenie_NoRequirements",
                    ValidationItemType.Error,
                    "Requirements text is required."));
                return result;
            }

            if (!request.DataSourceRegisterId.HasValue)
                request.DataSourceRegisterId = AppAISkillBL.GetDefaultDataSourceId();

            var createResult = await AppDbGenieBL.CreateHierarchyTransactionFromRequirementsAsync(request)
                .ConfigureAwait(false);

            result.Object = createResult;

            if (!createResult.IsSuccess)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(DbGenieController),
                    "DbGenie_CreateTransactionError",
                    ValidationItemType.Error,
                    createResult.Error));
            }
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(DbGenieController),
                "DbGenie_Error",
                ValidationItemType.Error,
                $"Error creating hierarchy transaction: {ex.Message}"));
        }

        return result;
    }

    /// <summary>
    /// Validates SQL for safety without executing
    /// </summary>
    [HttpPost]
    public OperationCallResult<SQLValidationResultDto> ValidateSQL(string sql)
    {
        var result = new OperationCallResult<SQLValidationResultDto>();

        try
        {
            var validationResult = AppDbGenieBL.ValidateSQL(sql);
            result.Object = validationResult;
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(DbGenieController),
                "DbGenie_ValidationError",
                ValidationItemType.Error,
                $"Error validating SQL: {ex.Message}"));
        }

        return result;
    }
}
