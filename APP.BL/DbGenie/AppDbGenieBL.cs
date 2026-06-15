using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using App.BL;
using App.BL.AppMgr.AiSkill;

using DatabaseSchemaMrg.DataSchema;
using System.Data.SqlClient;
using GemBox.Document;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.DbGenie
{
    /// <summary>
    /// Business logic for DBA-Genie AI agent
    /// </summary>
    public static class AppDbGenieBL
    {
        #region System Prompts

        /// <summary>
        /// Well-known skill name used to look up the SQL skill from the AppAISkill table.
        /// Users can edit this skill via the AI Skill Management UI.
        /// </summary>
        public const string SQL_SKILL_NAME = "DbGenie.SqlSkill";

        private static readonly Lazy<string> _sqlSkillEmbedded = new Lazy<string>(() =>
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "APP.BL.DbGenie.sqlskill.md";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        });

        /// <summary>
        /// Returns the SQL skill prompt: DB-stored version (editable via AI Skill Mgt) takes priority;
        /// falls back to the embedded sqlskill.md if no active DB record is found.
        /// </summary>
        private static string GetSqlSkillPrompt()
        {
            try
            {
                var dsId = AppAISkillBL.GetDefaultDataSourceId();
                if (dsId.HasValue)
                {
                    var skill = AppAISkillBL.GetSkillByName(dsId.Value, SQL_SKILL_NAME);
                    if (skill != null)
                    {
                        var dbPrompt = AppAISkillBL.GetComposedSkillPrompt(dsId.Value, skill.SkillId);
                        if (!string.IsNullOrWhiteSpace(dbPrompt))
                            return dbPrompt;
                    }
                }
            }
            catch { }

            return _sqlSkillEmbedded.Value;
        }

        private static readonly string DBA_SYSTEM_PROMPT = @"You are an expert Database Administrator AI assistant. Your role is to:
1. Analyze requirements and extract database schema entities
2. Generate valid SQL DDL scripts (CREATE TABLE, ALTER, etc.)
3. Convert natural language questions to SQL queries
4. Provide database design recommendations

Always output valid SQL for SQL Server database.
When extracting schema, return JSON format.
Be concise and accurate.";

        private static readonly string SCHEMA_EXTRACTION_PROMPT_TEMPLATE = @"Analyze the following requirements and extract database schema information.

Requirements:
{0}

Extract all tables, columns, data types, and relationships.

RELATIONSHIP TYPES — CRITICAL, read carefully:

  ONE_TO_MANY  — COMPOSITION. This table is the PARENT. The FK column lives in the TARGET (child) table.
                 Use ONLY when child rows cannot exist without this parent row.
                 Example: Order owns OrderItems → Order has ONE_TO_MANY to OrderItem.
                          OrderItem.OrderId (FK) points back to Order.Id.
                          foreignKeyColumn=""OrderId"" lives in OrderItem (the target), NOT in Order.

  MANY_TO_ONE  — ASSOCIATION / LOOKUP REFERENCE. This table has a FK column pointing to an independent entity.
                 The FK column lives IN THIS TABLE (the source), not in the target.
                 Use when this table references a standalone lookup/reference entity.
                 Example: Product references Category → Product has MANY_TO_ONE to Category.
                          Product.CategoryId (FK) points to Category.Id.
                          foreignKeyColumn=""CategoryId"" lives in Product (the source / this table).

FK DIRECTION RULE (non-negotiable):
  • If a FK column (e.g. CategoryId, StatusId, SupplierId) is a column IN THIS TABLE → use MANY_TO_ONE.
  • NEVER generate ONE_TO_MANY when the foreignKeyColumn is a column of the source (this) table.
  • For ONE_TO_MANY the foreignKeyColumn MUST be a column of the targetTable, not this table.

Return JSON in this exact format:
{{
  ""tables"": [
    {{
      ""name"": ""TableName"",
      ""description"": ""Table description"",
      ""description2"": ""Table longer description with more details"",
      ""columns"": [
        {{
          ""name"": ""ColumnName"",
          ""dataType"": ""INT"",
          ""length"": null,
          ""precision"": null,
          ""scale"": null,
          ""isPrimaryKey"": true,
          ""isNullable"": false,
          ""isAutoIncrement"": true,
          ""defaultValue"": null,
          ""description"": ""Column description""
        }}
      ],
      ""relationships"": [
        {{
          ""type"": ""ONE_TO_MANY"",
          ""targetTable"": ""OrderItem"",
          ""foreignKeyColumn"": ""OrderId"",
          ""referencedColumn"": ""Id""
        }},
        {{
          ""type"": ""MANY_TO_ONE"",
          ""targetTable"": ""Category"",
          ""foreignKeyColumn"": ""CategoryId"",
          ""referencedColumn"": ""Id""
        }}
      ]
    }}
  ]
}}

Use appropriate SQL Server data types: INT, BIGINT, VARCHAR(n), NVARCHAR(n), DATETIME, DECIMAL(p,s), BIT, etc.
Include primary keys, foreign keys, and appropriate constraints.
Return ONLY the JSON, no additional text.";

        private static readonly string NL2SQL_PROMPT_TEMPLATE = @"Given the following database schema:
{0}

Convert this question to a SQL Server SELECT statement:
{1}

Rules:
- Always include TOP 100 to limit results
- Use proper table aliases
- Use appropriate JOINs when needed
- Return only the SQL statement, no explanation
- The SQL must be valid for SQL Server";

        private static readonly string CHAT_SYSTEM_PROMPT = @"You are DBA-Genie, an expert Database Administrator AI assistant.

## Schema Discovery (CRITICAL — follow every time)
When a user asks about a subject (e.g. 'orders', 'tickets', 'products'):
1. Search the DATABASE SCHEMA section below for tables whose names contain keywords related to that subject.
2. **Always list the matching tables to the user first** — e.g. 'I found these related tables: OrderHeader, OrderDetail, OrderStatus'.
3. If multiple tables are clearly related (header/detail pattern, FK relationship), include ALL of them in the query.
4. Build the complete SQL query directly. Do NOT ask the user to run sp_help or any diagnostic commands.

## Column Discovery
If you need exact column names for a specific table, generate a SELECT query using INFORMATION_SCHEMA — do NOT use sp_help:
```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'YourTableName'
ORDER BY ORDINAL_POSITION
```
The system will execute this and feed the results back to you automatically. Use them to refine your query.

## SQL Rules
- Use SQL Server syntax
- Include TOP 100 for SELECT queries unless the user specifies a limit
- Use proper table aliases
- When joining tables, use meaningful aliases (oh for OrderHeader, od for OrderDetail, etc.)

## Response Format
When a user asks for a query:
1. **Tables found:** list the relevant tables you identified from the schema
2. **Query:** the complete SQL in a ```sql code block
3. Brief explanation of what the query does

Be concise and build the query directly without unnecessary back-and-forth.";

        #endregion

        #region Blocked Commands

        private static readonly string[] BlockedCommands = new[]
        {
            "SHUTDOWN",
            "GRANT",
            "REVOKE",
            "CREATE LOGIN",
            "ALTER LOGIN",
            "DROP LOGIN",
            "DROP DATABASE",
            "xp_cmdshell",
            "OPENROWSET",
            "BULK INSERT",
            "OPENDATASOURCE",
            "sp_configure",
            "sp_addextendedproc",
            "RECONFIGURE",
            "CREATE USER",
            "DROP USER",
            "ALTER USER"
        };

        private static readonly string[] DestructiveCommands = new[]
        {
            "DROP",
            "DELETE",
            "TRUNCATE",
            "ALTER"
        };

        #endregion

        #region Document Text Extraction

        /// <summary>
        /// Extracts text content from uploaded document (PDF, DOCX, TXT)
        /// </summary>
        public static string ExtractTextFromDocument(byte[] fileBytes, string fileExtension)
        {
            if (fileBytes == null || fileBytes.Length == 0)
            {
                throw new ArgumentException("File content is empty");
            }

            fileExtension = (fileExtension ?? "").ToLower().TrimStart('.');

            switch (fileExtension)
            {
                case "pdf":
                    return ExtractTextFromPdf(fileBytes);

                case "docx":
                case "doc":
                    return ExtractTextFromWord(fileBytes);

                case "txt":
                    return Encoding.UTF8.GetString(fileBytes);

                default:
                    throw new NotSupportedException($"File type '{fileExtension}' is not supported. Supported types: PDF, DOCX, DOC, TXT");
            }
        }

        /// <summary>
        /// Extracts text from PDF using GemBox.Document (same as Word; avoids GemBox.Pdf API differences across versions).
        /// </summary>
        private static string ExtractTextFromPdf(byte[] fileBytes)
        {
            using (var stream = new MemoryStream(fileBytes))
            {
                var document = DocumentModel.Load(stream, LoadOptions.PdfDefault);
                return document.Content.ToString();
            }
        }

        /// <summary>
        /// Extracts text from Word document using GemBox.Document
        /// </summary>
        private static string ExtractTextFromWord(byte[] fileBytes)
        {
            using (var stream = new MemoryStream(fileBytes))
            {
                var document = DocumentModel.Load(stream, LoadOptions.DocxDefault);
                return document.Content.ToString();
            }
        }

        #endregion

        #region Schema Extraction

        /// <summary>
        /// Extracts database schema from text using LLM
        /// </summary>
        public static async Task<SchemaExtractionResultDto> ExtractSchemaFromTextAsync(
            string inputText,
            EmLLMProvider provider,
            string apiKey)
        {
            var result = new SchemaExtractionResultDto();

            if (string.IsNullOrWhiteSpace(inputText))
            {
                result.IsSuccess = false;
                result.Error = "Input text is empty";
                return result;
            }

            try
            {
                var prompt = string.Format(SCHEMA_EXTRACTION_PROMPT_TEMPLATE, inputText);

                var request = new LLMRequestDto
                {
                    Provider = provider,
                    ApiKey = apiKey,
                    SystemPrompt = DBA_SYSTEM_PROMPT,
                    Prompt = prompt,
                    Temperature = 0.3,
                    MaxTokens = 16000
                };

                var llmResponse = await LLMProviderHelper.CallLLMAsync(request).ConfigureAwait(false);

                if (!llmResponse.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.Error = llmResponse.Error;
                    return result;
                }

                result.RawLLMResponse = llmResponse.Content;

                // Parse JSON response
                var tables = ParseSchemaJson(llmResponse.Content);
                result.Tables = tables;
                result.IsSuccess = true;

                // Generate CREATE scripts
                result.GeneratedScript = GenerateCreateTableScripts(tables);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Error = $"Error extracting schema: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Parses the LLM JSON response into table metadata
        /// </summary>
        private static List<DbGenieTableMetadataDto> ParseSchemaJson(string jsonResponse)
        {
            var tables = new List<DbGenieTableMetadataDto>();

            // Try to extract JSON from the response
            var jsonContent = ExtractJsonFromResponse(jsonResponse);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                throw new Exception("No valid JSON found in LLM response");
            }

            var json = JObject.Parse(jsonContent);
            var tablesArray = json["tables"] as JArray;

            if (tablesArray == null)
            {
                throw new Exception("No 'tables' array found in JSON response");
            }

            foreach (var tableToken in tablesArray)
            {
                var table = new DbGenieTableMetadataDto
                {
                    Name = tableToken["name"]?.ToString(),
                    Description = tableToken["description"]?.ToString(),
                    SchemaOwner = "dbo",
                    Columns = new List<DbGenieColumnMetadataDto>(),
                    Relationships = new List<DbGenieRelationshipMetadataDto>()
                };

                // Parse columns
                var columnsArray = tableToken["columns"] as JArray;
                if (columnsArray != null)
                {
                    foreach (var colToken in columnsArray)
                    {
                        var column = new DbGenieColumnMetadataDto
                        {
                            Name = colToken["name"]?.ToString(),
                            DataType = colToken["dataType"]?.ToString() ?? "NVARCHAR",
                            Length = colToken["length"]?.Value<int?>(),
                            Precision = colToken["precision"]?.Value<int?>(),
                            Scale = colToken["scale"]?.Value<int?>(),
                            IsPrimaryKey = colToken["isPrimaryKey"]?.Value<bool>() ?? false,
                            IsNullable = colToken["isNullable"]?.Value<bool>() ?? true,
                            IsAutoIncrement = colToken["isAutoIncrement"]?.Value<bool>() ?? false,
                            DefaultValue = colToken["defaultValue"]?.ToString(),
                            Description = colToken["description"]?.ToString()
                        };
                        table.Columns.Add(column);
                    }
                }

                // Parse relationships
                var relationshipsArray = tableToken["relationships"] as JArray;
                if (relationshipsArray != null)
                {
                    foreach (var relToken in relationshipsArray)
                    {
                        var relationship = new DbGenieRelationshipMetadataDto
                        {
                            Type = relToken["type"]?.ToString(),
                            TargetTable = relToken["targetTable"]?.ToString(),
                            ForeignKeyColumn = relToken["foreignKeyColumn"]?.ToString(),
                            ReferencedColumn = relToken["referencedColumn"]?.ToString()
                        };
                        table.Relationships.Add(relationship);
                    }
                }

                tables.Add(table);
            }

            return tables;
        }

        /// <summary>
        /// Extracts JSON content from LLM response (handles markdown code blocks)
        /// </summary>
        private static string ExtractJsonFromResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return null;

            string candidate = null;

            // Try to find JSON in code blocks first (greedy inner match to get the full object)
            var codeBlockPattern = @"```(?:json)?\s*(\{[\s\S]*\})\s*```";
            var match = Regex.Match(response, codeBlockPattern);
            if (match.Success)
                candidate = match.Groups[1].Value;

            // Fall back to raw JSON — find the first '{' and take everything from there
            if (candidate == null)
            {
                var start = response.IndexOf('{');
                if (start >= 0)
                    candidate = response.Substring(start);
            }

            if (candidate == null)
                return null;

            // Try parsing as-is
            try { JObject.Parse(candidate); return candidate; } catch { }

            // Response may be truncated — attempt to close unclosed brackets/braces
            candidate = RepairTruncatedJson(candidate);
            try { JObject.Parse(candidate); return candidate; } catch { }

            return null;
        }

        /// <summary>
        /// Attempts to close unclosed JSON arrays and objects caused by LLM token-limit truncation.
        /// </summary>
        private static string RepairTruncatedJson(string json)
        {
            // Remove any trailing incomplete token (partial string, number, identifier)
            json = Regex.Replace(json.TrimEnd(), @"[,\s]*$", "");

            // Count unclosed brackets and braces
            var stack = new System.Collections.Generic.Stack<char>();
            bool inString = false;
            bool escape = false;

            foreach (char c in json)
            {
                if (escape) { escape = false; continue; }
                if (c == '\\' && inString) { escape = true; continue; }
                if (c == '"') { inString = !inString; continue; }
                if (inString) continue;

                if (c == '{') stack.Push('}');
                else if (c == '[') stack.Push(']');
                else if (c == '}' || c == ']')
                {
                    if (stack.Count > 0 && stack.Peek() == c)
                        stack.Pop();
                }
            }

            var sb = new StringBuilder(json);
            while (stack.Count > 0)
                sb.Append(stack.Pop());

            return sb.ToString();
        }

        #endregion

        #region SQL Generation

        /// <summary>
        /// Generates CREATE TABLE scripts from extracted schema
        /// </summary>
        public static string GenerateCreateTableScripts(List<DbGenieTableMetadataDto> tables, string schemaOwner = "dbo")
        {
            if (tables == null || tables.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine("-- DBA-Genie Generated SQL Script");
            sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();

            foreach (var table in tables)
            {
                sb.AppendLine($"-- Table: {table.Name}");
                if (!string.IsNullOrWhiteSpace(table.Description))
                    sb.AppendLine($"-- Description: {table.Description}");

                var schema = string.IsNullOrWhiteSpace(table.SchemaOwner) ? schemaOwner : table.SchemaOwner;

                // Wrap in IF NOT EXISTS so re-running is safe when tables already exist
                sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '{table.Name}' AND schema_id = SCHEMA_ID('{schema}'))");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"CREATE TABLE [{schema}].[{table.Name}] (");

                var columnDefs = new List<string>();
                var primaryKeyColumns = new List<string>();

                foreach (var column in table.Columns)
                {
                    columnDefs.Add($"    {GenerateColumnDefinition(column)}");
                    if (column.IsPrimaryKey)
                        primaryKeyColumns.Add($"[{column.Name}]");
                }

                if (primaryKeyColumns.Count > 0)
                    columnDefs.Add($"    CONSTRAINT [PK_{table.Name}] PRIMARY KEY ({string.Join(", ", primaryKeyColumns)})");

                sb.AppendLine(string.Join(",\n", columnDefs));
                sb.AppendLine(");");
                sb.AppendLine("END");
                sb.AppendLine();
            }

            // Generate foreign key constraints (guarded so re-runs don't fail).
            // The table that gets ALTER must be the one that actually has the FK column (child).
            // The LLM may attach the relationship to either parent or child; only consider those two
            // tables when resolving (never pick a third table that merely has a same-named column).
            var emittedFks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in tables)
            {
                var schema = string.IsNullOrWhiteSpace(table.SchemaOwner) ? schemaOwner : table.SchemaOwner;

                if (table.Relationships == null) continue;
                foreach (var rel in table.Relationships)
                {
                    if (string.IsNullOrWhiteSpace(rel.ForeignKeyColumn) || string.IsNullOrWhiteSpace(rel.TargetTable))
                        continue;

                    var targetTableObj = tables.FirstOrDefault(t =>
                        string.Equals(t.Name, rel.TargetTable, StringComparison.OrdinalIgnoreCase));
                    bool currentTableHasFkCol = table.Columns != null &&
                        table.Columns.Any(c => string.Equals(c.Name, rel.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase));
                    bool targetTableHasFkCol = targetTableObj?.Columns != null &&
                        targetTableObj.Columns.Any(c => string.Equals(c.Name, rel.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase));

                    DbGenieTableMetadataDto childTable;
                    string parentTableName;

                    // Use relationship type as the primary direction signal.
                    // Column-presence check alone is unreliable: the FK column name can match
                    // the source PK (e.g. Orders.OrderId is PK; OrderItems.OrderId is FK) which
                    // causes the source to be incorrectly identified as the child table.
                    //
                    //  ONE_TO_MANY : current table is PARENT, target table is CHILD (FK lives there)
                    //  MANY_TO_ONE : current table is CHILD  (FK lives here),  target is PARENT
                    if (string.Equals(rel.Type, "ONE_TO_MANY", StringComparison.OrdinalIgnoreCase))
                    {
                        if (targetTableObj == null) continue; // target not in schema — skip
                        childTable     = targetTableObj;
                        parentTableName = table.Name;
                    }
                    else if (string.Equals(rel.Type, "MANY_TO_ONE", StringComparison.OrdinalIgnoreCase))
                    {
                        childTable     = table;
                        parentTableName = rel.TargetTable;
                    }
                    else
                    {
                        // Unknown / unset type — fall back to column presence
                        if (currentTableHasFkCol)
                        {
                            childTable     = table;
                            parentTableName = rel.TargetTable;
                        }
                        else if (targetTableHasFkCol && targetTableObj != null)
                        {
                            childTable     = targetTableObj;
                            parentTableName = table.Name;
                        }
                        else
                        {
                            childTable     = table;
                            parentTableName = rel.TargetTable;
                        }
                    }

                    var constraintName = $"FK_{childTable.Name}_{parentTableName}";
                    if (emittedFks.Contains(constraintName))
                        continue;
                    emittedFks.Add(constraintName);

                    // Resolve the referenced column: prefer explicit value, then PK of parent
                    var parentTableObj = tables.FirstOrDefault(t =>
                        string.Equals(t.Name, parentTableName, StringComparison.OrdinalIgnoreCase));
                    string refColumn = rel.ReferencedColumn;
                    if (string.IsNullOrWhiteSpace(refColumn))
                    {
                        var pkCol = parentTableObj?.Columns?.FirstOrDefault(c => c.IsPrimaryKey);
                        refColumn = pkCol != null ? pkCol.Name : "Id";
                    }
                    var parentSchema = parentTableObj != null && !string.IsNullOrWhiteSpace(parentTableObj.SchemaOwner)
                        ? parentTableObj.SchemaOwner
                        : schemaOwner;
                    var childSchema = string.IsNullOrWhiteSpace(childTable.SchemaOwner) ? schemaOwner : childTable.SchemaOwner;

                    sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{constraintName}')");
                    sb.AppendLine("BEGIN");
                    sb.AppendLine($"ALTER TABLE [{childSchema}].[{childTable.Name}]");
                    sb.AppendLine($"ADD CONSTRAINT [{constraintName}]");
                    sb.AppendLine($"FOREIGN KEY ([{rel.ForeignKeyColumn}]) REFERENCES [{parentSchema}].[{parentTableName}]([{refColumn}]);");
                    sb.AppendLine("END");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates column definition for CREATE TABLE
        /// </summary>
        private static string GenerateColumnDefinition(DbGenieColumnMetadataDto column)
        {
            var sb = new StringBuilder();
            sb.Append($"[{column.Name}] ");

            // Data type
            var dataType = (column.DataType ?? "NVARCHAR").ToUpper();

            if (dataType == "VARCHAR" || dataType == "NVARCHAR" || dataType == "CHAR" || dataType == "NCHAR")
            {
                var length = column.Length ?? 255;
                sb.Append($"{dataType}({(length == -1 ? "MAX" : length.ToString())})");
            }
            else if (dataType == "DECIMAL" || dataType == "NUMERIC")
            {
                var precision = column.Precision ?? 18;
                var scale = column.Scale ?? 2;
                sb.Append($"{dataType}({precision},{scale})");
            }
            else
            {
                sb.Append(dataType);
            }

            // Identity
            if (column.IsAutoIncrement)
            {
                sb.Append(" IDENTITY(1,1)");
            }

            // Nullability
            sb.Append(column.IsNullable ? " NULL" : " NOT NULL");

            // Default value
            if (!string.IsNullOrWhiteSpace(column.DefaultValue) && !column.IsAutoIncrement)
            {
                sb.Append($" DEFAULT {column.DefaultValue}");
            }

            return sb.ToString();
        }

        #endregion

        #region Natural Language to SQL

        /// <summary>
        /// Converts natural language question to SQL query
        /// </summary>
        public static async Task<NL2SQLResultDto> ConvertNaturalLanguageToSQLAsync(
            NL2SQLRequestDto request)
        {
            var result = new NL2SQLResultDto();

            if (string.IsNullOrWhiteSpace(request.Question))
            {
                result.IsSuccess = false;
                result.Error = "Question is required";
                return result;
            }

            try
            {
                // Get schema context if dataSourceRegisterId is provided
                var schemaContext = request.SchemaContext;
                if ((schemaContext == null || schemaContext.Count == 0) && request.DataSourceRegisterId.HasValue)
                {
                    schemaContext = await GetSchemaContextAsync(request.DataSourceRegisterId.Value).ConfigureAwait(false);
                }

                var schemaDescription = FormatSchemaContext(schemaContext);
                var prompt = string.Format(NL2SQL_PROMPT_TEMPLATE, schemaDescription, request.Question);

                var llmRequest = new LLMRequestDto
                {
                    Provider = request.LLMProvider,
                    ApiKey = request.ApiKey,
                    SystemPrompt = DBA_SYSTEM_PROMPT,
                    Prompt = prompt,
                    Temperature = 0.2,
                    MaxTokens = 2048
                };

                var llmResponse = await LLMProviderHelper.CallLLMAsync(llmRequest).ConfigureAwait(false);

                if (!llmResponse.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.Error = llmResponse.Error;
                    return result;
                }

                // Extract SQL from response
                var sql = ExtractSqlFromResponse(llmResponse.Content);

                // Validate and apply query limits
                result.Validation = ValidateSQL(sql);
                result.GeneratedSQL = ApplyQueryLimits(sql);
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Error = $"Error converting to SQL: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Formats schema context for the prompt
        /// </summary>
        private static string FormatSchemaContext(List<DbGenieTableMetadataDto> tables)
        {
            if (tables == null || tables.Count == 0)
                return "No schema context available.";

            var sb = new StringBuilder();

            foreach (var table in tables)
            {
                var owner = !string.IsNullOrWhiteSpace(table.SchemaOwner) ? $"{table.SchemaOwner}." : "";

                if (table.Columns != null && table.Columns.Count > 0)
                {
                    // Compact single-line format: dbo.OrderHeader(Id int PK, CustomerId int, OrderDate datetime, ...)
                    var cols = string.Join(", ", table.Columns.Select(c =>
                    {
                        var pk = c.IsPrimaryKey ? " PK" : "";
                        var nullable = c.IsNullable ? "?" : "";
                        return $"{c.Name} {c.DataType}{nullable}{pk}";
                    }));
                    sb.AppendLine($"{owner}{table.Name}({cols})");
                }
                else
                {
                    // No column info — just the table name so LLM can still search by name
                    sb.AppendLine($"{owner}{table.Name}");
                }

                // Foreign keys
                if (table.Relationships != null && table.Relationships.Count > 0)
                {
                    foreach (var rel in table.Relationships)
                        sb.AppendLine($"  FK: {rel.ForeignKeyColumn} -> {rel.TargetTable}.{rel.ReferencedColumn}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Extracts SQL statement from LLM response
        /// </summary>
        private static string ExtractSqlFromResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return string.Empty;

            // Try to find SQL in code blocks
            var codeBlockPattern = @"```(?:sql)?\s*([\s\S]*?)\s*```";
            var match = Regex.Match(response, codeBlockPattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Return the whole response if no code block
            return response.Trim();
        }

        #endregion

        #region SQL Validation

        /// <summary>
        /// Validates SQL for safety
        /// </summary>
        public static SQLValidationResultDto ValidateSQL(string sql)
        {
            var result = new SQLValidationResultDto { IsValid = true };

            if (string.IsNullOrWhiteSpace(sql))
            {
                result.IsValid = false;
                result.Warnings.Add("SQL is empty");
                return result;
            }

            var upperSql = sql.ToUpper();

            // Check for blocked commands
            foreach (var blocked in BlockedCommands)
            {
                if (upperSql.Contains(blocked.ToUpper()))
                {
                    result.IsValid = false;
                    result.BlockedCommands.Add(blocked);
                }
            }

            if (result.BlockedCommands.Count > 0)
            {
                result.Warnings.Add($"SQL contains blocked commands: {string.Join(", ", result.BlockedCommands)}");
            }

            // Check for destructive commands
            foreach (var destructive in DestructiveCommands)
            {
                if (Regex.IsMatch(upperSql, $@"\b{destructive}\b"))
                {
                    result.IsDestructive = true;
                    result.Command = destructive;
                    result.Warnings.Add($"SQL contains destructive command: {destructive}");
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Applies query limits (TOP 100) to SELECT statements
        /// </summary>
        public static string ApplyQueryLimits(string sql, int limit = 100)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            // Check if it's a SELECT statement without TOP
            var selectPattern = @"\bSELECT\s+(?!TOP\s)";
            if (Regex.IsMatch(sql, selectPattern, RegexOptions.IgnoreCase))
            {
                sql = Regex.Replace(sql, @"\bSELECT\s+", $"SELECT TOP {limit} ", RegexOptions.IgnoreCase);
            }

            return sql;
        }

        #endregion

        #region Chat with Agent

        /// <summary>
        /// Handles chat interaction with DBA-Genie agent
        /// </summary>
        public static async Task<DbGenieChatResponseDto> ChatWithAgentAsync(DbGenieChatRequestDto request)
        {
            var response = new DbGenieChatResponseDto
            {
                SessionId = request.SessionId ?? Guid.NewGuid().ToString()
            };

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                response.IsSuccess = false;
                response.Error = "Message is required";
                return response;
            }

            try
            {
                // ── Tier 1: load persisted session history from disk
                // Use whichever is longer: disk session may be stale if the frontend added
                // execution-result messages that haven't been saved yet.
                var storedHistory = DbGenieMemoryBL.LoadSession(response.SessionId);
                var frontendHistory = request.ConversationHistory ?? new List<DbGenieChatMessageDto>();
                var activeHistory = (storedHistory != null && storedHistory.Count >= frontendHistory.Count)
                    ? storedHistory
                    : frontendHistory;

                // Build conversation history for context
                var conversationContext = BuildConversationContext(activeHistory);

                // ── Tier 2: build system prompt = base + schema + dialect + memory
                var baseSystemPrompt = GetSqlSkillPrompt() ?? CHAT_SYSTEM_PROMPT;

                var systemPromptParts = new StringBuilder(baseSystemPrompt);

                // Inject full schema into system prompt (authoritative — LLM must use this)
                if (request.DataSourceRegisterId.HasValue)
                {
                    var tables = await GetSchemaContextAsync(request.DataSourceRegisterId.Value).ConfigureAwait(false);
                    var schemaText = FormatSchemaContext(tables);
                    systemPromptParts.Append("\n\n## DATABASE SCHEMA\nThe following tables exist in the connected database. Use this to find relevant tables for any query:\n\n");
                    systemPromptParts.Append(schemaText);
                }

                if (!string.IsNullOrWhiteSpace(request.DbDialect))
                    systemPromptParts.Append($"\n\n## DATABASE DIALECT\nGenerate SQL compatible with: {request.DbDialect}");

                var memoryContext = DbGenieMemoryBL.LoadMemoryContext();
                if (memoryContext != null)
                    systemPromptParts.Append("\n\n" + memoryContext);

                var systemPrompt = systemPromptParts.ToString();
                var fullPrompt = $"{conversationContext}\n\nUser: {request.Message}";

                var llmRequest = new LLMRequestDto
                {
                    Provider = request.LLMProvider,
                    ApiKey = request.ApiKey,
                    SystemPrompt = systemPrompt,
                    Prompt = fullPrompt,
                    Temperature = 0.7,
                    MaxTokens = 4096
                };

                var llmResponse = await LLMProviderHelper.CallLLMAsync(llmRequest).ConfigureAwait(false);

                if (!llmResponse.IsSuccess)
                {
                    response.IsSuccess = false;
                    response.Error = llmResponse.Error;
                    return response;
                }

                // Create response message
                var assistantMessage = new DbGenieChatMessageDto
                {
                    Role = "assistant",
                    Content = llmResponse.Content,
                    Timestamp = DateTime.UtcNow
                };

                // Check if response contains SQL
                var sqlMatch = Regex.Match(llmResponse.Content, @"```sql\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
                if (sqlMatch.Success)
                {
                    assistantMessage.HasSQL = true;
                    assistantMessage.GeneratedSQL = sqlMatch.Groups[1].Value.Trim();
                }

                response.Message = assistantMessage;
                response.IsSuccess = true;

                // ── Save exchange to both memory tiers
                DbGenieMemoryBL.SaveExchange(
                    response.SessionId,
                    request.Message,
                    llmResponse.Content,
                    assistantMessage.GeneratedSQL,
                    request.DataSourceRegisterId);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Error = $"Error processing chat: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Builds conversation context from history
        /// </summary>
        private static string BuildConversationContext(List<DbGenieChatMessageDto> history)
        {
            if (history == null || history.Count == 0)
                return "";

            var sb = new StringBuilder();
            sb.AppendLine("Previous conversation:");

            // Take last 10 messages for context
            var recentHistory = history.Skip(Math.Max(0, history.Count - 10)).ToList();

            foreach (var msg in recentHistory)
            {
                var role = msg.Role == "user" ? "User" : "Assistant";
                sb.AppendLine($"{role}: {msg.Content}");
            }

            return sb.ToString();
        }

        #endregion

        #region Schema Context from Database

        /// <summary>
        /// Gets schema context from a data source
        /// </summary>
        public static async Task<List<DbGenieTableMetadataDto>> GetSchemaContextAsync(int dataSourceRegisterId)
        {
            return await Task.Run(() =>
            {
                var tables   = new List<DbGenieTableMetadataDto>();
                var tableDict = new Dictionary<string, DbGenieTableMetadataDto>(StringComparer.OrdinalIgnoreCase);

                try
                {
                    var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

                    // Query ALL tables/views + their columns directly from INFORMATION_SCHEMA.
                    // This is the authoritative source — reflects the real DB, not just
                    // what is registered in the AppAI system (so AppTransaction, etc. are included).
                    const string colQuery = @"
SELECT
    c.TABLE_SCHEMA,
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.IS_NULLABLE,
    ISNULL(COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity'), 0) AS IS_IDENTITY,
    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PK
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN (
    SELECT ku.TABLE_NAME, ku.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
        AND tc.TABLE_NAME     = ku.TABLE_NAME
    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
) pk ON c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION";

                    var colDt = fixture.RetriveDataTable(colQuery, new List<DbParameter>());
                    if (colDt != null)
                    {
                        foreach (DataRow row in colDt.Rows)
                        {
                            var tblName    = row["TABLE_NAME"]?.ToString();
                            var schemaName = row["TABLE_SCHEMA"]?.ToString();
                            if (string.IsNullOrWhiteSpace(tblName)) continue;

                            if (!tableDict.TryGetValue(tblName, out var meta))
                            {
                                meta = new DbGenieTableMetadataDto
                                {
                                    Name          = tblName,
                                    SchemaOwner   = schemaName,
                                    Columns       = new List<DbGenieColumnMetadataDto>(),
                                    Relationships = new List<DbGenieRelationshipMetadataDto>()
                                };
                                tableDict[tblName] = meta;
                                tables.Add(meta);
                            }

                            meta.Columns.Add(new DbGenieColumnMetadataDto
                            {
                                Name            = row["COLUMN_NAME"]?.ToString(),
                                DataType        = row["DATA_TYPE"]?.ToString(),
                                IsNullable      = row["IS_NULLABLE"]?.ToString() == "YES",
                                IsPrimaryKey    = Convert.ToInt32(row["IS_PK"])    == 1,
                                IsAutoIncrement = Convert.ToInt32(row["IS_IDENTITY"]) == 1
                            });
                        }
                    }
                }
                catch (Exception)
                {
                    // Fall back to app-registered tables if INFORMATION_SCHEMA query fails
                    try
                    {
                        var dbTables = AppMetaDataBL.GetSaasDataSourceTableAndViewList(dataSourceRegisterId, null, null);
                        if (dbTables != null)
                        {
                            foreach (var dbTable in dbTables)
                            {
                                var t = new DbGenieTableMetadataDto
                                {
                                    Name = dbTable.Name, SchemaOwner = dbTable.SchemaOwner,
                                    Columns = new List<DbGenieColumnMetadataDto>(),
                                    Relationships = new List<DbGenieRelationshipMetadataDto>()
                                };
                                if (dbTable.Columns != null)
                                    foreach (var col in dbTable.Columns)
                                        t.Columns.Add(new DbGenieColumnMetadataDto { Name = col.Name, DataType = col.DataType });
                                tables.Add(t);
                            }
                        }
                    }
                    catch { }
                }

                return tables;
            }).ConfigureAwait(false);
        }

        #endregion

        #region SQL Execution

        /// <summary>
        /// Executes SQL query safely
        /// </summary>
        public static ExecuteSQLResultDto ExecuteSQL(ExecuteSQLRequestDto request)
        {
            var result = new ExecuteSQLResultDto();

            if (string.IsNullOrWhiteSpace(request.SQL))
            {
                result.IsSuccess = false;
                result.Error = "SQL is required";
                return result;
            }

            // Validate SQL first
            var validation = ValidateSQL(request.SQL);

            if (!validation.IsValid)
            {
                result.IsSuccess = false;
                result.Error = $"SQL validation failed: {string.Join(", ", validation.Warnings)}";
                return result;
            }

            // Check if confirmation is required for destructive operations
            if (validation.IsDestructive && request.RequireConfirmation && !request.IsConfirmed)
            {
                result.IsSuccess = false;
                result.RequiresConfirmation = true;
                result.ConfirmationMessage = $"This operation contains a {validation.Command} command. Please confirm to proceed.";
                return result;
            }

            // Resolve data source — fall back to default if not supplied
            if (!request.DataSourceRegisterId.HasValue)
                request.DataSourceRegisterId = AppAISkillBL.GetDefaultDataSourceId();

            try
            {
                var upperSql = request.SQL.Trim().ToUpper();
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(request.DataSourceRegisterId.Value);

                bool isSelect = upperSql.StartsWith("SELECT");
                bool isExec   = upperSql.StartsWith("EXEC ") || upperSql.StartsWith("EXEC\t")
                             || upperSql.StartsWith("EXECUTE ") || upperSql.StartsWith("EXECUTE\t")
                             || upperSql == "EXEC" || upperSql == "EXECUTE";

                // Handle SELECT queries
                if (isSelect)
                {
                    var limitedSql = ApplyQueryLimits(request.SQL);
                    var dataTable = fixture.RetriveDataTable(limitedSql, new List<DbParameter>());

                    if (dataTable != null)
                    {
                        result.ColumnNames = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

                        foreach (DataRow row in dataTable.Rows)
                        {
                            var rowDict = new Dictionary<string, object>();
                            foreach (DataColumn col in dataTable.Columns)
                            {
                                rowDict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                            }
                            result.Results.Add(rowDict);
                        }

                        result.RowsAffected = dataTable.Rows.Count;
                    }
                }
                // Handle EXEC / stored procedures — may return multiple result sets
                else if (isExec)
                {
                    var ds = new DataSet();
                    using (var conn = new SqlConnection(fixture.ConnectionString))
                    using (var adapter = new SqlDataAdapter(request.SQL, conn))
                    {
                        adapter.SelectCommand.CommandTimeout = 120;
                        adapter.Fill(ds);
                    }

                    int setIndex = 0;
                    foreach (DataTable dt in ds.Tables)
                    {
                        if (dt.Columns.Count == 0) continue;

                        // Separator row between result sets
                        if (setIndex > 0 && result.Results.Count > 0)
                        {
                            var sep = new Dictionary<string, object>();
                            foreach (var col in result.ColumnNames)
                                sep[col] = "---";
                            result.Results.Add(sep);
                        }

                        // Use first result set for column names; remap subsequent sets
                        var cols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                        if (result.ColumnNames.Count == 0)
                            result.ColumnNames = cols;

                        foreach (DataRow row in dt.Rows)
                        {
                            var rowDict = new Dictionary<string, object>();
                            // Map by position into first result-set column names so UI renders consistently
                            for (int i = 0; i < cols.Count; i++)
                            {
                                var key = i < result.ColumnNames.Count ? result.ColumnNames[i] : cols[i];
                                rowDict[key] = row[cols[i]] == DBNull.Value ? null : row[cols[i]];
                            }
                            result.Results.Add(rowDict);
                        }

                        result.RowsAffected = (result.RowsAffected ?? 0) + dt.Rows.Count;
                        setIndex++;
                    }
                }
                // Handle other queries (INSERT, UPDATE, DELETE, CREATE, etc.)
                else
                {
                    fixture.ExecuteNonQueryResult(request.SQL, new List<DbParameter>());
                }

                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Error = $"SQL execution error: {ex.Message}";
            }

            return result;
        }

        #endregion

        #region Create Hierarchy Transaction from Requirements

        /// <summary>
        /// Full pipeline: extracts schema from requirements text via LLM, creates physical tables
        /// on the target data source, then creates the AppTransaction hierarchy (root → children → grandchildren).
        /// </summary>
        public static async Task<DbGenieCreateTransactionResultDto> CreateHierarchyTransactionFromRequirementsAsync(
            DbGenieCreateTransactionRequestDto request)
        {
            var result = new DbGenieCreateTransactionResultDto();

            if (string.IsNullOrWhiteSpace(request?.RequirementsText))
            {
                result.IsSuccess = false;
                result.Error = "Requirements text is required";
                return result;
            }

            if (!request.DataSourceRegisterId.HasValue)
                request.DataSourceRegisterId = AppAISkillBL.GetDefaultDataSourceId();

            // Step 1: Extract schema from requirements text using the configured LLM
            var extraction = await ExtractSchemaFromTextAsync(
                request.RequirementsText,
                LLMProviderHelper.GetConfiguredProvider(),
                LLMProviderHelper.GetConfiguredApiKey()).ConfigureAwait(false);

            result.SchemaExtraction = extraction;

            if (!extraction.IsSuccess || extraction.Tables == null || extraction.Tables.Count == 0)
            {
                result.IsSuccess = false;
                result.Error = extraction.Error ?? "No tables could be extracted from the requirements text";
                return result;
            }

            // Step 1b: Apply application-name prefix to all table names
            //   - Remove spaces/special chars from TransactionName, take first 10 chars
            //   - Prefix every table name and every FK TargetTable reference so they stay consistent
            var rawAppName = !string.IsNullOrWhiteSpace(request.TransactionName)
                ? request.TransactionName
                : "Unknown";
            var appPrefix = Regex.Replace(rawAppName, @"[^A-Za-z0-9]", "");
            if (appPrefix.Length > 10) appPrefix = appPrefix.Substring(0, 10);
            if (string.IsNullOrEmpty(appPrefix)) appPrefix = "Unknown";
            appPrefix = appPrefix + "_";

            // Build a map of old name → new name so FK TargetTable refs can be updated
            var nameMap = extraction.Tables.ToDictionary(
                t => t.Name,
                t => appPrefix + t.Name,
                StringComparer.OrdinalIgnoreCase);

            foreach (var table in extraction.Tables)
            {
                table.Name = nameMap[table.Name];
                foreach (var rel in table.Relationships)
                {
                    if (!string.IsNullOrWhiteSpace(rel.TargetTable) && nameMap.ContainsKey(rel.TargetTable))
                        rel.TargetTable = nameMap[rel.TargetTable];
                }
            }

            // Regenerate scripts now that table names include the prefix
            extraction.GeneratedScript = GenerateCreateTableScripts(extraction.Tables, request.SchemaOwner ?? "dbo");

            // Step 2: Execute CREATE TABLE scripts on the target data source
            result.CreatedTableScripts = extraction.GeneratedScript;

            if (!string.IsNullOrWhiteSpace(extraction.GeneratedScript))
            {
                var execResult = ExecuteSQL(new ExecuteSQLRequestDto
                {
                    SQL = extraction.GeneratedScript,
                    DataSourceRegisterId = request.DataSourceRegisterId,
                    RequireConfirmation = false,
                    IsConfirmed = true
                });

                if (!execResult.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.Error = $"Failed to create tables in target database: {execResult.Error}";
                    return result;
                }

                // Refresh the schema cache so CreateHierarchyTransactionFromTables can find the new tables
                if (request.DataSourceRegisterId.HasValue)
                {
                    try { AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(request.DataSourceRegisterId); }
                    catch { /* non-fatal: proceed even if cache refresh fails */ }
                }
            }

            // Step 3: Analyse ONE_TO_MANY relationships to determine root/child/grandchild hierarchy
            var hierarchySetupDto = BuildHierarchySetupFromLLMSchema(extraction.Tables, request);

            if (hierarchySetupDto == null)
            {
                result.IsSuccess = false;
                result.Error = "Could not determine table hierarchy from the extracted schema";
                return result;
            }

            // Step 4: Create the AppTransaction hierarchy from the physical tables
            var transactionResult = AppTransactionBL.CreateHierarchyTransactionFromTables(hierarchySetupDto);

            if (transactionResult.ValidationResult != null &&
                transactionResult.ValidationResult.Items != null &&
                transactionResult.ValidationResult.Items.Any(i => i.ItemType == ValidationItemType.Error))
            {
                result.IsSuccess = false;
                result.Error = string.Join("; ", transactionResult.ValidationResult.Items
                    .Where(i => i.ItemType == ValidationItemType.Error)
                    .Select(i => i.Message));
                return result;
            }

            result.IsSuccess       = true;
            result.TransactionId   = transactionResult.Object != null ? Convert.ToInt32(transactionResult.Object.Id) : 0;
            result.TransactionName = transactionResult.Object?.TransactionName;
            return result;
        }

        /// <summary>
        /// Execute-phase only: takes an already-extracted (and possibly user-edited) schema,
        /// generates + executes the CREATE TABLE scripts, then creates the AppTransaction hierarchy.
        ///
        /// Called by execute_approved_schema after propose_schema has already obtained user approval.
        /// The schema's table names must already include the application prefix (applied by propose_schema).
        /// </summary>
        public static async Task<DbGenieCreateTransactionResultDto> CreateHierarchyFromApprovedSchemaAsync(
            SchemaExtractionResultDto approvedSchema,
            DbGenieCreateTransactionRequestDto request)
        {
            var result = new DbGenieCreateTransactionResultDto { SchemaExtraction = approvedSchema };

            if (approvedSchema?.Tables == null || approvedSchema.Tables.Count == 0)
            {
                result.IsSuccess = false;
                result.Error = "Approved schema contains no tables.";
                return result;
            }

            if (!request.DataSourceRegisterId.HasValue)
                request.DataSourceRegisterId = AppAISkillBL.GetDefaultDataSourceId();

            // Step 1: Generate CREATE TABLE scripts from the approved schema
            var script = GenerateCreateTableScripts(approvedSchema.Tables, request.SchemaOwner ?? "dbo");
            approvedSchema.GeneratedScript = script;
            result.CreatedTableScripts      = script;

            // Step 2: Execute the DDL
            if (!string.IsNullOrWhiteSpace(script))
            {
                var execResult = ExecuteSQL(new ExecuteSQLRequestDto
                {
                    SQL = script,
                    DataSourceRegisterId = request.DataSourceRegisterId,
                    RequireConfirmation  = false,
                    IsConfirmed          = true
                });

                if (!execResult.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.Error = $"Failed to create tables: {execResult.Error}";
                    return result;
                }

                if (request.DataSourceRegisterId.HasValue)
                {
                    try { AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(request.DataSourceRegisterId); }
                    catch { /* non-fatal */ }
                }
            }

            // Step 3: Build hierarchy + create transactions
            var hierarchySetupDto = BuildHierarchySetupFromLLMSchema(approvedSchema.Tables, request);
            if (hierarchySetupDto == null)
            {
                result.IsSuccess = false;
                result.Error = "Could not determine table hierarchy from the approved schema.";
                return result;
            }

            var transactionResult = AppTransactionBL.CreateHierarchyTransactionFromTables(hierarchySetupDto);

            if (transactionResult.ValidationResult != null &&
                transactionResult.ValidationResult.Items != null &&
                transactionResult.ValidationResult.Items.Any(i => i.ItemType == ValidationItemType.Error))
            {
                result.IsSuccess = false;
                result.Error = string.Join("; ", transactionResult.ValidationResult.Items
                    .Where(i => i.ItemType == ValidationItemType.Error)
                    .Select(i => i.Message));
                return result;
            }

            result.IsSuccess       = true;
            result.TransactionId   = transactionResult.Object != null ? Convert.ToInt32(transactionResult.Object.Id) : 0;
            result.TransactionName = transactionResult.Object?.TransactionName;
            result.LookupTables    = approvedSchema.Tables.Where(t => t.IsLookup).Select(t => t.Name).ToList();
            return result;
        }

        /// <summary>
        /// Analyses LLM-extracted table metadata to identify the root/child/grandchild hierarchy
        /// and builds a HierarchyTableSetupDto.
        ///
        /// Algorithm:
        ///   Root   = table that no other table targets via ONE_TO_MANY (nothing is "parent" of it)
        ///   Children   = root's ONE_TO_MANY TargetTables
        ///   Grandchildren = each child's ONE_TO_MANY TargetTables
        /// </summary>
        private static HierarchyTableSetupDto BuildHierarchySetupFromLLMSchema(
            List<DbGenieTableMetadataDto> tables,
            DbGenieCreateTransactionRequestDto request)
        {
            // Lookup tables are standalone entities (list-edit screens) — exclude them from the
            // master-detail hierarchy entirely. Only non-lookup ONE_TO_MANY relationships count
            // as composition when deciding who is a "child" in the hierarchy.
            var nonLookupTables = tables.Where(t => !t.IsLookup).ToList();

            // Collect composition children: targets of ONE_TO_MANY from NON-lookup source tables only
            var referencedAsManyTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in nonLookupTables)
            {
                if (t.Relationships == null) continue;
                foreach (var rel in t.Relationships)
                {
                    if (string.Equals(rel.Type, "ONE_TO_MANY", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(rel.TargetTable))
                    {
                        referencedAsManyTables.Add(rel.TargetTable);
                    }
                }
            }

            // Root = non-lookup table not referenced by any other non-lookup table via ONE_TO_MANY
            var rootTable = nonLookupTables.FirstOrDefault(t =>
                !referencedAsManyTables.Contains(t.Name) &&
                t.Relationships != null &&
                t.Relationships.Any(r => string.Equals(r.Type, "ONE_TO_MANY", StringComparison.OrdinalIgnoreCase)));

            // Fallback 1: non-lookup table that simply isn't a composition child
            if (rootTable == null)
                rootTable = nonLookupTables.FirstOrDefault(t => !referencedAsManyTables.Contains(t.Name));

            // Fallback 2: any non-lookup table
            if (rootTable == null)
                rootTable = nonLookupTables.FirstOrDefault();

            // Fallback 3: original behaviour when every table is a lookup
            if (rootTable == null)
                rootTable = tables.FirstOrDefault(t => !referencedAsManyTables.Contains(t.Name))
                            ?? tables.FirstOrDefault();

            if (rootTable == null)
                return null;

            var tableDict = tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

            // Build children and grandchildren from ONE_TO_MANY relationships.
            // Track which tables have been placed anywhere in the hierarchy to avoid duplicates
            // (a table in two positions causes "An item with the same key has already been added" in SaveAppTransactionExDto).
            var placedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootTable.Name };

            var childDefs = new List<HierarchyChildTableDto>();
            if (rootTable.Relationships != null)
            {
                foreach (var rel in rootTable.Relationships)
                {
                    if (!string.Equals(rel.Type, "ONE_TO_MANY", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(rel.TargetTable))
                        continue;

                    // Skip if this table has already been placed in the hierarchy
                    if (placedTables.Contains(rel.TargetTable))
                        continue;

                    // Skip lookup (reference) tables — they get standalone list-edit screens,
                    // not master-detail child tabs. This guards against the LLM accidentally
                    // generating ONE_TO_MANY toward a reference/lookup entity.
                    if (tableDict.TryGetValue(rel.TargetTable, out var relTargetMeta) && relTargetMeta.IsLookup)
                        continue;

                    placedTables.Add(rel.TargetTable);

                    var grandChildNames = new List<string>();

                    if (tableDict.TryGetValue(rel.TargetTable, out var childTable) &&
                        childTable.Relationships != null)
                    {
                        foreach (var gcRel in childTable.Relationships)
                        {
                            if (string.Equals(gcRel.Type, "ONE_TO_MANY", StringComparison.OrdinalIgnoreCase) &&
                                !string.IsNullOrWhiteSpace(gcRel.TargetTable) &&
                                !placedTables.Contains(gcRel.TargetTable))
                            {
                                placedTables.Add(gcRel.TargetTable);
                                grandChildNames.Add(gcRel.TargetTable);
                            }
                        }
                    }

                    childDefs.Add(new HierarchyChildTableDto
                    {
                        TableName = rel.TargetTable,
                        GrandChildTableNames = grandChildNames
                    });
                }
            }

            return new HierarchyTableSetupDto
            {
                MasterTableName = rootTable.Name,
                ChildTables = childDefs,
                DataSourceRegisterId = request.DataSourceRegisterId,
                SchemaOwner = request.SchemaOwner,
                TransactionName = request.TransactionName,
                SaasApplicationId = request.SaasApplicationId
            };
        }

        #endregion
    }
}
