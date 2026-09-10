using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Runs user-provided scripts under AgentOutput/{sessionKey}/ with sandbox constraints.
    /// No business script-name whitelist — only path/extension/timeout sandbox.
    /// </summary>
    public static class GenericAgentProcessBL
    {
        public const int DefaultTimeoutSeconds = 1200;
        public const int MaxStdoutChars = 32_000;
        public const int MaxStderrChars = 16_000;

        public class ScriptPolicy
        {
            public List<string> AllowedPathPrefixes { get; set; } = new List<string> { "source/" };
            public List<string> AllowedExtensions { get; set; } = new List<string> { ".ps1" };
            public int MaxTimeoutSeconds { get; set; } = DefaultTimeoutSeconds;
            /// <summary>Relative path to JSON that may contain plmDataSourceId / dwDataSourceId.</summary>
            public string InjectDataSourcesFromConfig { get; set; } = "source/dwTabImportConfig.json";
        }

        public class RunResult
        {
            public bool Ok { get; set; }
            public int ExitCode { get; set; }
            public string RelativePath { get; set; }
            public string StdoutTail { get; set; }
            public string StderrTail { get; set; }
            public int DurationMs { get; set; }
            public string Error { get; set; }
            public List<object> OutputFiles { get; set; }
        }

        public static ScriptPolicy ParsePolicy(string toolConfigJson)
        {
            var policy = new ScriptPolicy();
            if (string.IsNullOrWhiteSpace(toolConfigJson)) return policy;
            try
            {
                var jo = JObject.Parse(toolConfigJson);
                if (jo["AllowedPathPrefixes"] is JArray prefixes && prefixes.Count > 0)
                {
                    policy.AllowedPathPrefixes = prefixes
                        .Select(t => NormalizePrefix(t?.ToString()))
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                }
                if (jo["AllowedExtensions"] is JArray exts && exts.Count > 0)
                {
                    policy.AllowedExtensions = exts
                        .Select(t => (t?.ToString() ?? "").Trim())
                        .Where(s => s.StartsWith(".", StringComparison.Ordinal))
                        .ToList();
                }
                if (jo["MaxTimeoutSeconds"] != null
                    && int.TryParse(jo["MaxTimeoutSeconds"].ToString(), out var sec)
                    && sec > 0)
                {
                    policy.MaxTimeoutSeconds = Math.Min(sec, 3600);
                }
                if (jo["InjectDataSourcesFromConfig"] != null)
                    policy.InjectDataSourcesFromConfig = jo["InjectDataSourcesFromConfig"].ToString()?.Trim();
            }
            catch { /* keep defaults */ }
            return policy;
        }

        public static async Task<RunResult> RunScriptAsync(
            string sessionKey,
            int companyId,
            string relativePath,
            string toolConfigJson,
            CancellationToken ct)
        {
            var result = new RunResult { RelativePath = relativePath };
            try
            {
                if (string.IsNullOrWhiteSpace(sessionKey))
                    throw new InvalidOperationException("ChatSessionKey is required.");
                if (companyId <= 0)
                    throw new InvalidOperationException("CompanyId is required.");
                if (string.IsNullOrWhiteSpace(relativePath))
                    throw new InvalidOperationException("relativePath is required.");

                var policy = ParsePolicy(toolConfigJson);
                var norm = NormalizeRel(relativePath);
                ValidatePathPolicy(norm, policy);

                GenericAgentFileBL.EnsureRoot(sessionKey, companyId);
                var sessionRoot = GenericAgentFileBL.Resolve(sessionKey, null, companyId);
                var scriptFull = GenericAgentFileBL.Resolve(sessionKey, norm, companyId);
                if (!File.Exists(scriptFull))
                    throw new FileNotFoundException("Script not found under agent file area.", norm);

                var ext = Path.GetExtension(scriptFull);
                if (!policy.AllowedExtensions.Any(e =>
                        string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Extension not allowed: " + ext);

                string configBackup = null;
                string configPath = null;
                string injectedUser = null;
                string injectedPassword = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(policy.InjectDataSourcesFromConfig))
                    {
                        configPath = GenericAgentFileBL.Resolve(
                            sessionKey, NormalizeRel(policy.InjectDataSourcesFromConfig), companyId);
                        if (File.Exists(configPath))
                        {
                            configBackup = InjectSqlEndpointsIntoConfig(
                                configPath, out injectedUser, out injectedPassword);
                        }
                    }

                    var run = await StartPowerShellFileAsync(
                        sessionRoot,
                        scriptFull,
                        policy.MaxTimeoutSeconds,
                        injectedUser,
                        injectedPassword,
                        ct).ConfigureAwait(false);
                    result.ExitCode = run.ExitCode;
                    result.StdoutTail = Truncate(run.Stdout, MaxStdoutChars);
                    result.StderrTail = Truncate(run.Stderr, MaxStderrChars);
                    result.DurationMs = run.DurationMs;
                    result.Ok = run.ExitCode == 0;
                    if (!result.Ok && string.IsNullOrWhiteSpace(result.Error))
                        result.Error = "Script exited with code " + run.ExitCode;

                    result.OutputFiles = ListOutputSummary(sessionKey, companyId);
                }
                finally
                {
                    if (configBackup != null && configPath != null)
                    {
                        try { File.WriteAllText(configPath, configBackup, Encoding.UTF8); }
                        catch { /* best effort restore */ }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.Error = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// Patches sqlServer / dwDatabase / plmDatabase from DataSourceIds.
        /// Returns original file text for restore. Does not write passwords into the JSON.
        /// SQL auth credentials are returned via out params for child-process env only.
        /// </summary>
        private static string InjectSqlEndpointsIntoConfig(
            string configPath,
            out string sqlUser,
            out string sqlPassword)
        {
            sqlUser = null;
            sqlPassword = null;
            var original = File.ReadAllText(configPath);
            JObject jo;
            try { jo = JObject.Parse(original); }
            catch { return null; }

            var dwId = jo.Value<int?>("dwDataSourceId") ?? jo.Value<int?>("DwDataSourceId");
            var plmId = jo.Value<int?>("plmDataSourceId") ?? jo.Value<int?>("PlmDataSourceId");
            if ((dwId == null || dwId <= 0) && (plmId == null || plmId <= 0))
                return null;

            SqlConnectionStringBuilder dwBuilder = null;
            SqlConnectionStringBuilder plmBuilder = null;
            if (dwId is > 0)
                dwBuilder = TryGetBuilder(dwId.Value);
            if (plmId is > 0)
                plmBuilder = TryGetBuilder(plmId.Value);

            var primary = dwBuilder ?? plmBuilder;
            if (primary == null) return null;

            if (!string.IsNullOrWhiteSpace(primary.DataSource))
                jo["sqlServer"] = primary.DataSource;
            if (dwBuilder != null && !string.IsNullOrWhiteSpace(dwBuilder.InitialCatalog))
                jo["dwDatabase"] = dwBuilder.InitialCatalog;
            if (plmBuilder != null && !string.IsNullOrWhiteSpace(plmBuilder.InitialCatalog))
                jo["plmDatabase"] = plmBuilder.InitialCatalog;

            var auth = dwBuilder ?? plmBuilder;
            if (auth != null && !auth.IntegratedSecurity
                && !string.IsNullOrWhiteSpace(auth.UserID))
            {
                sqlUser = auth.UserID;
                sqlPassword = auth.Password ?? "";
            }

            jo.Remove("sqlPassword");
            jo.Remove("sqlUser");

            File.WriteAllText(configPath, jo.ToString(Formatting.Indented), Encoding.UTF8);
            return original;
        }

        private static SqlConnectionStringBuilder TryGetBuilder(int dataSourceId)
        {
            try
            {
                var fixture = App.BL.AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null || string.IsNullOrWhiteSpace(fixture.ConnectionString))
                    return null;
                return new SqlConnectionStringBuilder(fixture.ConnectionString);
            }
            catch
            {
                return null;
            }
        }

        private static async Task<(int ExitCode, string Stdout, string Stderr, int DurationMs)> StartPowerShellFileAsync(
            string workingDirectory,
            string scriptFullPath,
            int timeoutSeconds,
            string sqlUser,
            string sqlPassword,
            CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = FindPowerShell(),
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-ExecutionPolicy");
            psi.ArgumentList.Add("Bypass");
            psi.ArgumentList.Add("-File");
            psi.ArgumentList.Add(scriptFullPath);

            // Child-process only — do not mutate App pool environment.
            if (!string.IsNullOrEmpty(sqlUser))
            {
                psi.Environment["PLM_DW_SQL_USER"] = sqlUser;
                psi.Environment["PLM_DW_SQL_PASSWORD"] = sqlPassword ?? "";
            }

            var sw = Stopwatch.StartNew();
            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

            if (!proc.Start())
                throw new InvalidOperationException("Failed to start PowerShell.");

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(30, timeoutSeconds)));
            try
            {
                await proc.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
                sw.Stop();
                throw new TimeoutException(
                    "Script timed out after " + timeoutSeconds + "s (or was cancelled).");
            }

            sw.Stop();
            return (proc.ExitCode, stdout.ToString(), stderr.ToString(), (int)sw.ElapsedMilliseconds);
        }

        private static string FindPowerShell()
        {
            var pwsh = Environment.GetEnvironmentVariable("APP_AI_PWSH");
            if (!string.IsNullOrWhiteSpace(pwsh) && File.Exists(pwsh)) return pwsh;

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var candidate = Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe");
            if (File.Exists(candidate)) return candidate;

            return "powershell.exe";
        }

        private static List<object> ListOutputSummary(string sessionKey, int companyId)
        {
            try
            {
                var files = GenericAgentFileBL.List(sessionKey, "output", companyId)
                    ?? new List<APP.Components.EntityDto.GenericAgentFileDto>();
                var list = new List<object>();
                foreach (var f in files)
                {
                    if (f.IsDirectory)
                    {
                        var nested = GenericAgentFileBL.List(sessionKey, f.RelativePath, companyId);
                        foreach (var n in nested.Where(x => !x.IsDirectory))
                            list.Add(new { n.RelativePath, n.SizeBytes });
                    }
                    else
                        list.Add(new { f.RelativePath, f.SizeBytes });
                }
                return list;
            }
            catch
            {
                return new List<object>();
            }
        }

        private static void ValidatePathPolicy(string normalizedRel, ScriptPolicy policy)
        {
            if (normalizedRel.Contains("..", StringComparison.Ordinal)
                || Path.IsPathRooted(normalizedRel)
                || normalizedRel.IndexOf(':') >= 0)
                throw new UnauthorizedAccessException("Invalid script path.");

            var prefixes = policy.AllowedPathPrefixes ?? new List<string> { "source/" };
            if (prefixes.Count == 0)
                prefixes = new List<string> { "source/" };

            var ok = prefixes.Any(p =>
            {
                var pref = NormalizePrefix(p);
                return normalizedRel.StartsWith(pref, StringComparison.OrdinalIgnoreCase)
                       || string.Equals(normalizedRel, pref.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
            });
            if (!ok)
                throw new UnauthorizedAccessException(
                    "Script path must be under: " + string.Join(", ", prefixes));
        }

        private static string NormalizeRel(string path)
        {
            return (path ?? "").Replace('\\', '/').Trim().TrimStart('/');
        }

        private static string NormalizePrefix(string prefix)
        {
            var p = NormalizeRel(prefix);
            if (string.IsNullOrWhiteSpace(p)) return "source/";
            if (!p.EndsWith("/", StringComparison.Ordinal)) p += "/";
            if (p == "/") return "source/";
            return p;
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(s.Length - max);
        }
    }
}
