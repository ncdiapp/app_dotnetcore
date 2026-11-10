using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn FileTool for Agent Management chat files.
    /// Paths are relative to FileRepository/Company_{id}/AgentOutput/{ChatSessionKey}/.
    /// ChatSessionKey comes from AgentToolContext (AppGenericAgentSession.SessionKey).
    /// .xlsx / .xls automatically use GemBox tabular read/write (not UTF-8 text).
    /// </summary>
    public class AgentFilePlugin
    {
        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("file_list",
            "List files and folders under the current chat file area (relative to AgentOutput/{sessionKey}/). Omit path or pass empty for the root.")]
        public Task<string> List(
            AgentToolContext context,
            CancellationToken ct,
            [AgentParam("Optional relative folder path. Empty = file area root.")] string path = null)
        {
            try
            {
                RequireSession(context);
                path = GenericAgentExcelFileHelper.NormalizeRelativePath(path);
                var files = GenericAgentFileBL.List(context.ChatSessionKey, path, context.CompanyId, context.SkillKey);
                return Task.FromResult(JsonConvert.SerializeObject(new { Path = path ?? "", Files = files }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = ex.Message }));
            }
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("file_read",
            "Read a file from the chat file area. Text: UTF-8 content. .xlsx/.xls: returns SheetNames and Sheets for all worksheets.")]
        public Task<string> Read(
            AgentToolContext context,
            CancellationToken ct,
            [AgentParam("Relative file path", isRequired: true)] string path)
        {
            try
            {
                RequireSession(context);
                path = GenericAgentExcelFileHelper.NormalizeRelativePath(path);
                if (string.IsNullOrWhiteSpace(path))
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "path is required." }));

                // Previously text-written ".xlsx" cannot be opened by GemBox — report clearly.
                if (GenericAgentExcelFileHelper.IsExcelPath(path))
                {
                    GenericAgentFileBL.EnsureRoot(context.ChatSessionKey, context.CompanyId, context.SkillKey);
                    var full = GenericAgentFileBL.Resolve(context.ChatSessionKey, path, context.CompanyId);
                    if (!File.Exists(full))
                        return Task.FromResult(JsonConvert.SerializeObject(new { Error = "FILE_NOT_FOUND", RelativePath = path }));

                    try
                    {
                        var excel = GenericAgentExcelFileHelper.ReadForAgent(full);
                        return Task.FromResult(JsonConvert.SerializeObject(new
                        {
                            RelativePath = path,
                            Format = "excel",
                            Excel = excel
                        }));
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(JsonConvert.SerializeObject(new
                        {
                            Error = "Not a valid Excel file (often a UTF-8 text file wrongly named .xlsx). Delete it and rewrite with file_write. Detail: " + ex.Message,
                            RelativePath = path
                        }));
                    }
                }

                GenericAgentFileBL.EnsureRoot(context.ChatSessionKey, context.CompanyId, context.SkillKey);
                var content = GenericAgentFileBL.ReadText(context.ChatSessionKey, path, context.CompanyId, context.SkillKey);
                return Task.FromResult(JsonConvert.SerializeObject(content));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = ex.Message }));
            }
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("file_write",
            "Write a file into the chat file area. Text: UTF-8 body. .xlsx/.xls: JSON content with mode append|overwrite_sheet|replace_file.")]
        public Task<string> Write(
            AgentToolContext context,
            CancellationToken ct,
            [AgentParam("Relative file path", isRequired: true)] string path,
            [AgentParam("Text body, or Excel JSON {mode,sheet|sheets,headers,rows}", isRequired: true)] string content)
        {
            try
            {
                RequireSession(context);
                if (string.IsNullOrWhiteSpace(path))
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "path is required." }));

                // Coerce .txt/.csv / missing ext → .xlsx when path or content indicates Excel.
                var originalPath = path;
                path = GenericAgentExcelFileHelper.CoerceExcelWritePath(path, content);

                if (GenericAgentExcelFileHelper.IsExcelPath(path))
                {
                    GenericAgentFileBL.EnsureRoot(context.ChatSessionKey, context.CompanyId, context.SkillKey);
                    var full = GenericAgentFileBL.Resolve(context.ChatSessionKey, path, context.CompanyId);
                    // Replace bogus text-as-xlsx left by older runs
                    if (File.Exists(full) && !IsZipSignature(full)
                        && path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(full);
                    }

                    var result = GenericAgentExcelFileHelper.WriteFromAgentContent(full, content ?? "");
                    return Task.FromResult(JsonConvert.SerializeObject(new
                    {
                        RelativePath = path.Replace('\\', '/'),
                        RequestedPath = GenericAgentExcelFileHelper.NormalizeRelativePath(originalPath),
                        Ok = true,
                        Format = "excel",
                        Excel = result
                    }));
                }

                var written = GenericAgentFileBL.WriteText(context.ChatSessionKey, path, content ?? "", context.CompanyId);
                return Task.FromResult(JsonConvert.SerializeObject(new { RelativePath = written, Ok = true, Format = "text" }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = ex.Message }));
            }
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("file_mkdir", "Create a folder (and parents) under the current chat file area.")]
        public Task<string> Mkdir(
            AgentToolContext context,
            CancellationToken ct,
            [AgentParam("Relative folder path", isRequired: true)] string path)
        {
            try
            {
                RequireSession(context);
                path = GenericAgentExcelFileHelper.NormalizeRelativePath(path);
                if (string.IsNullOrWhiteSpace(path))
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "path is required." }));
                GenericAgentFileBL.Mkdir(context.ChatSessionKey, path, context.CompanyId);
                return Task.FromResult(JsonConvert.SerializeObject(new { RelativePath = path, Ok = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = ex.Message }));
            }
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("execute_agent_sql_file",
            "Execute a .sql file from the current chat AgentOutput area (GO-batch). Connects to APP tenant (or dataSourceId).")]
        public Task<string> ExecuteSqlFile(
            AgentToolContext context,
            CancellationToken ct,
            [AgentParam("Relative .sql path under AgentOutput, e.g. output/3359/3_PlmDw_ImportFromDW.sql", isRequired: true)] string relativePath,
            [AgentParam("Optional target DataSourceRegisterId. Omit or 0 = APP tenant / session default.")] int? dataSourceId = null,
            [AgentParam("Optional comma-separated DataSourceIds to preflight same-server catalogs.")] string requiredDataSourceIds = null)
        {
            try
            {
                RequireSession(context);
                var result = GenericAgentSqlFileBL.Execute(
                    context.ChatSessionKey,
                    context.CompanyId,
                    relativePath,
                    dataSourceId,
                    requiredDataSourceIds);
                return Task.FromResult(JsonConvert.SerializeObject(result));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    Ok = false,
                    Path = relativePath,
                    Error = ex.Message
                }));
            }
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("file_delete", "Delete a file or folder (recursive) under the current chat file area.")]
        public Task<string> Delete(
            AgentToolContext context,
            CancellationToken ct,
            [AgentParam("Relative path to delete", isRequired: true)] string path)
        {
            try
            {
                RequireSession(context);
                path = GenericAgentExcelFileHelper.NormalizeRelativePath(path);
                if (string.IsNullOrWhiteSpace(path))
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "path is required." }));
                GenericAgentFileBL.Delete(context.ChatSessionKey, path, context.CompanyId);
                return Task.FromResult(JsonConvert.SerializeObject(new { RelativePath = path, Deleted = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = ex.Message }));
            }
        }

        private static bool IsZipSignature(string fullPath)
        {
            try
            {
                using var fs = File.OpenRead(fullPath);
                return fs.Length >= 2 && fs.ReadByte() == 'P' && fs.ReadByte() == 'K';
            }
            catch
            {
                return false;
            }
        }

        private static void RequireSession(AgentToolContext context)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                throw new InvalidOperationException("ChatSessionKey is required for agent file tools.");
            if (context.CompanyId <= 0)
                throw new InvalidOperationException("CompanyId is required for agent file tools.");
        }
    }
}
