using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using APP.Components.EntityDto;
using APP.Framework;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Agent Management chat files under FileRepository/Company_{id}/AgentOutput/{sessionKey}/.
    /// sessionKey = AppGenericAgentSession.SessionKey (fixed SkillKey:UserId or GUID), sanitized for FS.
    /// Independent of CursorCloudAgentWorkspaceBL (Cursor DI path).
    /// </summary>
    public static class GenericAgentFileBL
    {
        public const string FolderName = "AgentOutput";
        public const long MaxFileBytes = 20L * 1024 * 1024;

        public static string EnsureRoot(string sessionKey, int companyId)
        {
            var root = Resolve(sessionKey, null, companyId);
            Directory.CreateDirectory(Path.Combine(root, "source"));
            Directory.CreateDirectory(Path.Combine(root, "output"));
            return root;
        }

        public static List<GenericAgentFileDto> List(string sessionKey, string relativePath, int companyId)
        {
            var root = EnsureRoot(sessionKey, companyId);
            var dir = string.IsNullOrWhiteSpace(relativePath)
                ? root
                : Resolve(sessionKey, relativePath, companyId);

            if (!Directory.Exists(dir))
                return new List<GenericAgentFileDto>();

            var list = new List<GenericAgentFileDto>();
            foreach (var path in Directory.GetFileSystemEntries(dir))
            {
                var isDir = Directory.Exists(path);
                var info = new FileInfo(path);
                list.Add(new GenericAgentFileDto
                {
                    RelativePath = ToRelative(root, path).Replace('\\', '/'),
                    SizeBytes = isDir ? 0 : info.Exists ? info.Length : 0,
                    UpdatedAt = isDir ? Directory.GetLastWriteTimeUtc(path) : info.LastWriteTimeUtc,
                    IsDirectory = isDir
                });
            }
            return list.OrderByDescending(f => f.IsDirectory).ThenBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static GenericAgentFileContentDto ReadText(string sessionKey, string relativePath, int companyId)
        {
            var bytes = ReadBytes(sessionKey, relativePath, companyId);
            var truncated = bytes.Length > MaxFileBytes;
            var take = truncated ? (int)Math.Min(MaxFileBytes, bytes.Length) : bytes.Length;
            return new GenericAgentFileContentDto
            {
                RelativePath = relativePath,
                Content = Encoding.UTF8.GetString(bytes, 0, take),
                Truncated = truncated
            };
        }

        public static byte[] ReadBytes(string sessionKey, string relativePath, int companyId)
        {
            var full = Resolve(sessionKey, relativePath, companyId);
            if (!File.Exists(full))
                throw new FileNotFoundException("Agent file not found.", relativePath);
            return File.ReadAllBytes(full);
        }

        public static string WriteText(string sessionKey, string relativePath, string content, int companyId)
        {
            return WriteBytes(sessionKey, relativePath, Encoding.UTF8.GetBytes(content ?? ""), companyId);
        }

        public static string WriteBytes(string sessionKey, string relativePath, byte[] bytes, int companyId)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("relativePath is required.");
            EnsureRoot(sessionKey, companyId);
            var full = Resolve(sessionKey, relativePath, companyId);
            Directory.CreateDirectory(Path.GetDirectoryName(full) ?? full);
            if (bytes != null && bytes.Length > MaxFileBytes)
                throw new InvalidOperationException("File exceeds size limit (20 MB).");
            File.WriteAllBytes(full, bytes ?? Array.Empty<byte>());
            return ToRelative(Resolve(sessionKey, null, companyId), full).Replace('\\', '/');
        }

        public static void Mkdir(string sessionKey, string relativePath, int companyId)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("relativePath is required.");
            EnsureRoot(sessionKey, companyId);
            Directory.CreateDirectory(Resolve(sessionKey, relativePath, companyId));
        }

        public static void Rename(string sessionKey, string relativePath, string newPath, int companyId)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || string.IsNullOrWhiteSpace(newPath))
                throw new ArgumentException("relativePath and newPath are required.");
            var src = Resolve(sessionKey, relativePath, companyId);
            var dst = Resolve(sessionKey, newPath, companyId);
            Directory.CreateDirectory(Path.GetDirectoryName(dst) ?? dst);
            if (File.Exists(src)) File.Move(src, dst);
            else if (Directory.Exists(src)) Directory.Move(src, dst);
            else throw new FileNotFoundException("Path not found.", relativePath);
        }

        public static void Delete(string sessionKey, string relativePath, int companyId)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("relativePath is required.");
            var full = Resolve(sessionKey, relativePath, companyId);
            if (File.Exists(full)) File.Delete(full);
            else if (Directory.Exists(full)) Directory.Delete(full, true);
        }

        public static string Resolve(string sessionKey, string fileRelativePath, int companyId)
        {
            var sessionRoot = Path.GetFullPath(Path.Combine(CompanyRoot(companyId), SanitizeFolder(sessionKey)));
            var allowed = Path.GetFullPath(CompanyRoot(companyId));
            if (!sessionRoot.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Path is outside AgentOutput.");

            if (string.IsNullOrWhiteSpace(fileRelativePath))
                return sessionRoot;

            var combined = Path.GetFullPath(Path.Combine(sessionRoot, fileRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!combined.StartsWith(sessionRoot, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("File path is outside the session AgentOutput folder.");
            return combined;
        }

        private static string CompanyRoot(int companyId)
        {
            if (companyId <= 0)
            {
                if (ServerContext.Instance.CurrentCompanyId != null)
                    companyId = Convert.ToInt32(ServerContext.Instance.CurrentCompanyId);
            }
            if (companyId <= 0)
                throw new InvalidOperationException("Company id is required for AgentOutput files.");

            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileRepository", "Company_" + companyId, FolderName);
            Directory.CreateDirectory(root);
            return root;
        }

        public static string SanitizeFolder(string sessionKey)
        {
            if (string.IsNullOrWhiteSpace(sessionKey))
                throw new ArgumentException("sessionKey is required.");
            var s = sessionKey.Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            s = s.Replace('/', '_').Replace('\\', '_').Trim('.', ' ');
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentException("sessionKey is invalid.");
            return s;
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
