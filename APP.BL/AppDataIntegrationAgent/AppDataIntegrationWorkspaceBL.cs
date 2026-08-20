using System;
using System.Collections.Generic;
using System.IO;
using APP.Components.EntityDto;
using APP.Framework;

namespace App.BL.AppDataIntegrationAgent
{
    public static class AppDataIntegrationWorkspaceBL
    {
        public const string FolderName = "AgentOutput";

        public static string EnsureSessionDir(string relativePath, int? companyId = null)
        {
            var full = Resolve(relativePath, null, companyId);
            Directory.CreateDirectory(Path.Combine(full, "packs"));
            Directory.CreateDirectory(Path.Combine(full, "scripts"));
            Directory.CreateDirectory(Path.Combine(full, "output"));
            Directory.CreateDirectory(Path.Combine(full, "notes"));
            Directory.CreateDirectory(Path.Combine(full, "artifacts"));
            return full;
        }

        public static string PublicUrl(string workspaceRelativePath, string fileRelativePath, int? companyId = null)
        {
            var company = RequireCompanyId(companyId);
            var session = SessionFolder(workspaceRelativePath);
            var rel = (fileRelativePath ?? "").Replace('\\', '/').TrimStart('/');
            return "/FileRepository/Company_" + company + "/" + FolderName + "/" + session + "/" + rel;
        }

        public static string Resolve(string workspaceRelativePath, string fileRelativePath, int? companyId = null)
        {
            var sessionRoot = Path.GetFullPath(Path.Combine(CompanyRoot(companyId), SessionFolder(workspaceRelativePath)));
            var allowed = Path.GetFullPath(CompanyRoot(companyId));
            if (!sessionRoot.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Workspace path is outside AgentOutput.");

            if (string.IsNullOrWhiteSpace(fileRelativePath))
                return sessionRoot;

            var combined = Path.GetFullPath(Path.Combine(sessionRoot, fileRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!combined.StartsWith(sessionRoot, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("File path is outside the session AgentOutput folder.");
            return combined;
        }

        public static List<AppDataIntegrationAgentWorkspaceFileDto> ListFiles(string workspaceRelativePath, int? companyId = null)
        {
            var dir = EnsureSessionDir(workspaceRelativePath, companyId);
            var list = new List<AppDataIntegrationAgentWorkspaceFileDto>();
            foreach (var path in Directory.GetFileSystemEntries(dir, "*", SearchOption.AllDirectories))
            {
                var info = new FileInfo(path);
                var isDir = Directory.Exists(path);
                var rel = ToRelative(dir, path).Replace('\\', '/');
                list.Add(new AppDataIntegrationAgentWorkspaceFileDto
                {
                    RelativePath = rel,
                    SizeBytes = isDir ? 0 : info.Exists ? info.Length : 0,
                    UpdatedAt = isDir ? Directory.GetLastWriteTimeUtc(path) : info.LastWriteTimeUtc,
                    IsDirectory = isDir,
                    PublicUrl = isDir ? null : PublicUrl(workspaceRelativePath, rel, companyId)
                });
            }
            return list;
        }

        public static AppDataIntegrationAgentFileContentDto ReadFile(string workspaceRelativePath, string relativePath, int? companyId = null)
        {
            var bytes = ReadBytes(workspaceRelativePath, relativePath, companyId);
            var maxBytes = AppDataIntegrationAgentConfig.MaxWorkspaceFileMb * 1024L * 1024L;
            var truncated = bytes.Length > maxBytes;
            var take = truncated ? (int)Math.Min(maxBytes, bytes.Length) : bytes.Length;
            return new AppDataIntegrationAgentFileContentDto
            {
                RelativePath = relativePath,
                Content = System.Text.Encoding.UTF8.GetString(bytes, 0, take),
                Truncated = truncated
            };
        }

        public static byte[] ReadBytes(string workspaceRelativePath, string relativePath, int? companyId = null)
        {
            var full = Resolve(workspaceRelativePath, relativePath, companyId);
            if (!File.Exists(full))
                throw new FileNotFoundException("AgentOutput file not found.", relativePath);
            return File.ReadAllBytes(full);
        }

        public static string WriteFile(string workspaceRelativePath, string relativePath, string content, int? companyId = null)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content ?? "");
            return WriteBytes(workspaceRelativePath, relativePath, bytes, companyId);
        }

        public static string WriteBytes(string workspaceRelativePath, string relativePath, byte[] bytes, int? companyId = null)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("relativePath is required.");
            EnsureSessionDir(workspaceRelativePath, companyId);
            var full = Resolve(workspaceRelativePath, relativePath, companyId);
            Directory.CreateDirectory(Path.GetDirectoryName(full) ?? full);
            var maxBytes = Math.Max(AppDataIntegrationAgentConfig.MaxWorkspaceFileMb, 20) * 1024L * 1024L;
            if (bytes != null && bytes.Length > maxBytes)
                throw new InvalidOperationException("File exceeds AgentOutput size limit.");
            File.WriteAllBytes(full, bytes ?? Array.Empty<byte>());
            return ToRelative(Resolve(workspaceRelativePath, null, companyId), full).Replace('\\', '/');
        }

        public static void DeleteFile(string workspaceRelativePath, string relativePath, int? companyId = null)
        {
            var full = Resolve(workspaceRelativePath, relativePath, companyId);
            if (File.Exists(full)) File.Delete(full);
            else if (Directory.Exists(full)) Directory.Delete(full, true);
        }

        public static string RewriteCloudPaths(string text, string workspaceRelativePath, int? companyId = null)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var prefix = "/FileRepository/Company_" + RequireCompanyId(companyId) + "/" + FolderName + "/"
                + SessionFolder(workspaceRelativePath) + "/artifacts/";
            return text
                .Replace("/opt/cursor/artifacts/", prefix)
                .Replace("\\opt\\cursor\\artifacts\\", prefix);
        }

        private static string CompanyRoot(int? companyId)
        {
            var id = RequireCompanyId(companyId);
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileRepository", "Company_" + id, FolderName);
            Directory.CreateDirectory(root);
            return root;
        }

        private static int RequireCompanyId(int? companyId)
        {
            if (companyId.HasValue && companyId.Value > 0)
                return companyId.Value;
            if (ServerContext.Instance.CurrentCompanyId != null)
                return Convert.ToInt32(ServerContext.Instance.CurrentCompanyId);
            throw new InvalidOperationException("Company id is required to write AgentOutput.");
        }

        private static string SessionFolder(string workspaceRelativePath)
        {
            if (string.IsNullOrWhiteSpace(workspaceRelativePath))
                throw new ArgumentException("session folder is required.");
            var p = workspaceRelativePath.Replace('\\', '/').Trim('/');
            var i = p.LastIndexOf('/');
            return i >= 0 ? p.Substring(i + 1) : p;
        }

        private static string ToRelative(string root, string full)
        {
            var r = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return full.StartsWith(r, StringComparison.OrdinalIgnoreCase)
                ? full.Substring(r.Length)
                : Path.GetFileName(full);
        }
    }
}
