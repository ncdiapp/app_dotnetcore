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
        public Task<string> List(AgentToolContext context, CancellationToken ct, string path = null)
        {
            try
            {
                RequireSession(context);
                path = GenericAgentExcelFileHelper.NormalizeRelativePath(path);
                var files = GenericAgentFileBL.List(context.ChatSessionKey, path, context.CompanyId);
                return Task.FromResult(JsonConvert.SerializeObject(new { Path = path ?? "", Files = files }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = ex.Message }));
            }
        }

        public Task<string> Read(AgentToolContext context, CancellationToken ct, string path)
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

                var content = GenericAgentFileBL.ReadText(context.ChatSessionKey, path, context.CompanyId);
                return Task.FromResult(JsonConvert.SerializeObject(content));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = ex.Message }));
            }
        }

        public Task<string> Write(AgentToolContext context, CancellationToken ct, string path, string content)
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
                    GenericAgentFileBL.EnsureRoot(context.ChatSessionKey, context.CompanyId);
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

        public Task<string> Mkdir(AgentToolContext context, CancellationToken ct, string path)
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

        public Task<string> Delete(AgentToolContext context, CancellationToken ct, string path)
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
