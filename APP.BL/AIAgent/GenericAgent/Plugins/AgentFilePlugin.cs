using System;
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
    /// </summary>
    public class AgentFilePlugin
    {
        public Task<string> List(AgentToolContext context, CancellationToken ct, string path = null)
        {
            try
            {
                RequireSession(context);
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
                if (string.IsNullOrWhiteSpace(path))
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "path is required." }));
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
                var written = GenericAgentFileBL.WriteText(context.ChatSessionKey, path, content ?? "", context.CompanyId);
                return Task.FromResult(JsonConvert.SerializeObject(new { RelativePath = written, Ok = true }));
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

        private static void RequireSession(AgentToolContext context)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                throw new InvalidOperationException("ChatSessionKey is required for agent file tools.");
            if (context.CompanyId <= 0)
                throw new InvalidOperationException("CompanyId is required for agent file tools.");
        }
    }
}
