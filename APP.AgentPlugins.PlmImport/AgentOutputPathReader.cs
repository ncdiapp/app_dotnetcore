using System;
using App.BL.AIAgent.GenericAgent;
using APP.Framework.Plugin;

namespace APP.AgentPlugins.PlmImport;

/// <summary>Read a file from the current chat AgentOutput folder for path-based PLM tools.</summary>
internal static class AgentOutputPathReader
{
    public static string ReadText(AgentToolContext context, string? relativePath)
    {
        if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
            throw new InvalidOperationException("ChatSessionKey is required to read AgentOutput files.");
        if (context.CompanyId <= 0)
            throw new InvalidOperationException("CompanyId is required to read AgentOutput files.");
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("relativePath is required (e.g. output/3359/4_PlmDw_ImportBlueprint.json).");

        var path = relativePath.Trim().Replace('\\', '/').TrimStart('/');
        if (path.IndexOf("..", StringComparison.Ordinal) >= 0)
            throw new UnauthorizedAccessException("Path must stay under the chat AgentOutput folder.");

        var file = GenericAgentFileBL.ReadText(context.ChatSessionKey, path, context.CompanyId);
        if (file == null || string.IsNullOrWhiteSpace(file.Content))
            throw new InvalidOperationException("File is empty or missing: " + path);
        if (file.Truncated)
            throw new InvalidOperationException("File exceeds the 20 MB AgentOutput limit: " + path);
        return file.Content;
    }
}
