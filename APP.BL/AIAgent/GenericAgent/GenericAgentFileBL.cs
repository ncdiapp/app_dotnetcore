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
    /// Default Source Files (agent editor): FileRepository/Company_{id}/AgentStarter/{skillKey}/.
    /// New Chat copies starter files into that chat source/ (not shared writable).
    /// Independent of CursorCloudAgentWorkspaceBL (Cursor DI path).
    /// </summary>
    public static class GenericAgentFileBL
    {
        public const string FolderName = "AgentOutput";
        public const string StarterFolderName = "AgentStarter";
        public const long MaxFileBytes = 20L * 1024 * 1024;
        public const string WorkingConfigFileName = "dwTabImportConfig.json";

        public static string EnsureRoot(string sessionKey, int companyId, string skillKey = null)
        {
            var root = Resolve(sessionKey, null, companyId);
            var created = !Directory.Exists(root);
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(Path.Combine(root, "source"));
            Directory.CreateDirectory(Path.Combine(root, "output"));
            if (created && !string.IsNullOrWhiteSpace(skillKey))
                CopyDefaultSourceToChat(skillKey, sessionKey, companyId, overwrite: false);
            return root;
        }

        public static string EnsureStarterRoot(string skillKey, int companyId)
        {
            var root = ResolveStarter(skillKey, null, companyId);
            Directory.CreateDirectory(root);
            return root;
        }

        /// <summary>
        /// Copy AgentStarter/{skillKey}/* into AgentOutput/{session}/source/.
        /// Skips working dwTabImportConfig.json and a top-level output folder.
        /// </summary>
        public static int CopyDefaultSourceToChat(string skillKey, string sessionKey, int companyId, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(skillKey) || string.IsNullOrWhiteSpace(sessionKey))
                return 0;

            var starterRoot = EnsureStarterRoot(skillKey, companyId);
            if (!Directory.Exists(starterRoot))
                return 0;

            var chatRoot = Resolve(sessionKey, null, companyId);
            var destSource = Path.Combine(chatRoot, "source");
            Directory.CreateDirectory(destSource);
            return CopyTree(starterRoot, destSource, overwrite);
        }

        public static List<GenericAgentFileDto> ListDefaultSource(string skillKey, string relativePath, int companyId)
        {
            var root = EnsureStarterRoot(skillKey, companyId);
            var dir = string.IsNullOrWhiteSpace(relativePath)
                ? root
                : ResolveStarter(skillKey, relativePath, companyId);
            return ListDir(root, dir);
        }

        public static GenericAgentFileContentDto ReadDefaultSourceText(string skillKey, string relativePath, int companyId)
        {
            var bytes = ReadDefaultSourceBytes(skillKey, relativePath, companyId);
            var truncated = bytes.Length > MaxFileBytes;
            var take = truncated ? (int)Math.Min(MaxFileBytes, bytes.Length) : bytes.Length;
            return new GenericAgentFileContentDto
            {
                RelativePath = relativePath,
                Content = DecodeText(bytes, take),
                Truncated = truncated
            };
        }

        public static byte[] ReadDefaultSourceBytes(string skillKey, string relativePath, int companyId)
        {
            var full = ResolveStarter(skillKey, relativePath, companyId);
            if (!File.Exists(full))
                throw new FileNotFoundException("Default Source File not found.", relativePath);
            return File.ReadAllBytes(full);
        }

        public static string WriteDefaultSourceBytes(string skillKey, string relativePath, byte[] bytes, int companyId)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("relativePath is required.");
            EnsureStarterRoot(skillKey, companyId);
            var full = ResolveStarter(skillKey, relativePath, companyId);
            Directory.CreateDirectory(Path.GetDirectoryName(full) ?? full);
            if (bytes != null && bytes.Length > MaxFileBytes)
                throw new InvalidOperationException("File exceeds size limit (20 MB).");
            File.WriteAllBytes(full, bytes ?? Array.Empty<byte>());
            return ToRelative(ResolveStarter(skillKey, null, companyId), full).Replace('\\', '/');
        }

        public static string WriteDefaultSourceText(string skillKey, string relativePath, string content, int companyId)
        {
            return WriteDefaultSourceBytes(skillKey, relativePath, Encoding.UTF8.GetBytes(content ?? ""), companyId);
        }

        public static void MkdirDefaultSource(string skillKey, string relativePath, int companyId)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("relativePath is required.");
            EnsureStarterRoot(skillKey, companyId);
            Directory.CreateDirectory(ResolveStarter(skillKey, relativePath, companyId));
        }

        public static void RenameDefaultSource(string skillKey, string relativePath, string newPath, int companyId)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || string.IsNullOrWhiteSpace(newPath))
                throw new ArgumentException("relativePath and newPath are required.");
            var src = ResolveStarter(skillKey, relativePath, companyId);
            var dst = ResolveStarter(skillKey, newPath, companyId);
            Directory.CreateDirectory(Path.GetDirectoryName(dst) ?? dst);
            if (File.Exists(src)) File.Move(src, dst);
            else if (Directory.Exists(src)) Directory.Move(src, dst);
            else throw new FileNotFoundException("Path not found.", relativePath);
        }

        public static void DeleteDefaultSource(string skillKey, string relativePath, int companyId)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("relativePath is required.");
            var full = ResolveStarter(skillKey, relativePath, companyId);
            if (File.Exists(full)) File.Delete(full);
            else if (Directory.Exists(full)) Directory.Delete(full, true);
        }

        public static List<GenericAgentFileDto> List(string sessionKey, string relativePath, int companyId, string skillKey = null)
        {
            var root = EnsureRoot(sessionKey, companyId, skillKey);
            var dir = string.IsNullOrWhiteSpace(relativePath)
                ? root
                : Resolve(sessionKey, relativePath, companyId);
            return ListDir(root, dir);
        }

        public static GenericAgentFileContentDto ReadText(string sessionKey, string relativePath, int companyId)
        {
            var bytes = ReadBytes(sessionKey, relativePath, companyId);
            var truncated = bytes.Length > MaxFileBytes;
            var take = truncated ? (int)Math.Min(MaxFileBytes, bytes.Length) : bytes.Length;
            return new GenericAgentFileContentDto
            {
                RelativePath = relativePath,
                Content = DecodeText(bytes, take),
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
            return ResolveUnder(CompanyStoreRoot(companyId, FolderName), sessionKey, fileRelativePath, "AgentOutput");
        }

        public static string ResolveStarter(string skillKey, string fileRelativePath, int companyId)
        {
            return ResolveUnder(CompanyStoreRoot(companyId, StarterFolderName), skillKey, fileRelativePath, "AgentStarter");
        }

        private static string ResolveUnder(string storeRoot, string key, string fileRelativePath, string label)
        {
            var allowed = Path.GetFullPath(storeRoot);
            var itemRoot = Path.GetFullPath(Path.Combine(allowed, SanitizeFolder(key)));
            if (!itemRoot.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Path is outside " + label + ".");

            if (string.IsNullOrWhiteSpace(fileRelativePath))
                return itemRoot;

            var combined = Path.GetFullPath(Path.Combine(itemRoot, fileRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!combined.StartsWith(itemRoot, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("File path is outside the " + label + " folder.");
            return combined;
        }

        private static string CompanyStoreRoot(int companyId, string storeFolder)
        {
            if (companyId <= 0)
            {
                if (ServerContext.Instance.CurrentCompanyId != null)
                    companyId = Convert.ToInt32(ServerContext.Instance.CurrentCompanyId);
            }
            if (companyId <= 0)
                throw new InvalidOperationException("Company id is required for agent files.");

            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileRepository", "Company_" + companyId, storeFolder);
            Directory.CreateDirectory(root);
            return root;
        }

        private static List<GenericAgentFileDto> ListDir(string root, string dir)
        {
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

        private static int CopyTree(string sourceDir, string destDir, bool overwrite)
        {
            var copied = 0;
            foreach (var path in Directory.GetFileSystemEntries(sourceDir))
            {
                var name = Path.GetFileName(path);
                if (string.Equals(name, "output", StringComparison.OrdinalIgnoreCase) && Directory.Exists(path)
                    && string.Equals(Path.GetFullPath(sourceDir), Path.GetFullPath(Path.GetDirectoryName(path) ?? sourceDir), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (ShouldSkipStarterFile(name))
                    continue;

                var dest = Path.Combine(destDir, name);
                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(dest);
                    copied += CopyTree(path, dest, overwrite);
                }
                else if (File.Exists(path))
                {
                    if (!overwrite && File.Exists(dest))
                        continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(dest) ?? destDir);
                    File.Copy(path, dest, overwrite);
                    copied++;
                }
            }
            return copied;
        }

        private static bool ShouldSkipStarterFile(string fileName)
        {
            return string.Equals(fileName, WorkingConfigFileName, StringComparison.OrdinalIgnoreCase);
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

        /// <summary>
        /// Agent SQL/JSON is often written by PowerShell (UTF-8 BOM or UTF-16 LE).
        /// Blind UTF-8 decode turns those prefixes into '?' / U+FFFD and breaks SQL/JSON parse.
        /// </summary>
        internal static string DecodeText(byte[] bytes, int count)
        {
            if (bytes == null || count <= 0)
                return string.Empty;

            int offset = 0;
            Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            if (count >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                offset = 3;
            }
            else if (count >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                encoding = Encoding.Unicode;
                offset = 2;
            }
            else if (count >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                encoding = Encoding.BigEndianUnicode;
                offset = 2;
            }
            else if (LooksLikeUtf16LeWithoutBom(bytes, count))
            {
                encoding = Encoding.Unicode;
            }

            var text = encoding.GetString(bytes, offset, count - offset);
            return text.TrimStart('\uFEFF', '\uFFFE');
        }

        private static bool LooksLikeUtf16LeWithoutBom(byte[] bytes, int count)
        {
            if (count < 4)
                return false;
            int pairs = Math.Min(32, count / 2);
            int zeros = 0;
            for (int i = 0; i < pairs; i++)
            {
                if (bytes[i * 2 + 1] == 0)
                    zeros++;
            }
            return zeros >= (pairs * 3) / 4;
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
