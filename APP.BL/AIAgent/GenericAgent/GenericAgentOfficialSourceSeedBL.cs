using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using App.BL.CursorCloudAgent;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Copies official ImportDoc probe/template files into Agent Management chat source/
    /// and AgentStarter. Users must not upload these scripts.
    /// </summary>
    public static class GenericAgentOfficialSourceSeedBL
    {
        public const string OrchestratorSkillKey = "plm-integration-orchestrator";
        public const string ImportDwSkillKey = "plm-integration-import-dw";
        public const string SearchSkillKey = "plm-integration-search";
        public const string MassUpdateSkillKey = "plm-integration-massupdate";

        private static readonly string[] SearchFileNames =
        {
            "_plm_probe_search.sql",
            "_app_probe_fieldmapping.sql",
            "_app_probe_search_context.sql",
            "7_PlmSearch_ImportBlueprint.example.json",
            "8_PlmSearch_SiblingView.example.json",
            "plmSearchImportConfig.example.json"
        };

        private static readonly string[] MassUpdateFileNames =
        {
            "_plm_probe_massupdate.sql",
            "_app_probe_fieldmapping.sql",
            "_app_probe_search_context.sql",
            "9_PlmSearch_MassUpdateView.example.json",
            "9b_PlmSearch_MassUpdateView_ListEdit.example.json",
            "plmSearchImportConfig.example.json"
        };

        private static readonly string[] ImportDwFileNames =
        {
            "_gen_plmdw_import_sql.ps1",
            "_gen_plmdw_bom_colorway.ps1",
            "_gen_tchp_import_sql.ps1",
            "_gen_simple_qc.ps1",
            "PlmDw_ImportFromDW.sql",
            "PlmDw_ImportBomColorwayGrandchild.sql",
            "dwTabImportConfig.example.json",
            "bomColorwayImportConfig.example.json",
            "_plm_probe_template.sql",
            "_dw_probe_by_tabids.sql"
        };

        public static bool IsPlmIntegrationSkill(string skillKey)
        {
            var key = Normalize(skillKey);
            return key.StartsWith("plm-integration-", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Seed missing official files into chat source/ and matching AgentStarter folders.
        /// Never overwrites an existing file.
        /// </summary>
        public static int SeedIfMissing(string sessionKey, int companyId, string skillKey)
        {
            if (string.IsNullOrWhiteSpace(sessionKey) || companyId <= 0)
                return 0;

            try
            {
                var key = Normalize(skillKey);
                if (!IsPlmIntegrationSkill(key))
                    return 0;

                var packs = ResolvePacks(key);
                if (packs.Count == 0)
                    return 0;

                var copied = 0;
                foreach (var pack in packs)
                {
                    var srcDir = FindPackDirectory(pack.Kind);
                    if (string.IsNullOrWhiteSpace(srcDir))
                        continue;

                    var chatSource = Path.Combine(GenericAgentFileBL.Resolve(sessionKey, null, companyId), "source");
                    copied += CopyNamedFiles(srcDir, chatSource, pack.FileNames, overwrite: false);

                    foreach (var starterKey in pack.StarterSkillKeys)
                        copied += SeedStarterPack(starterKey, companyId, srcDir, pack.FileNames);
                }
                return copied;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex,
                    "Official PLM source seed skipped for session {0} skill {1}", sessionKey, skillKey);
                return 0;
            }
        }

        public static int SeedStarterIfMissing(string skillKey, int companyId)
        {
            if (string.IsNullOrWhiteSpace(skillKey) || companyId <= 0)
                return 0;

            try
            {
                var packs = ResolvePacks(Normalize(skillKey));
                var copied = 0;
                foreach (var pack in packs)
                {
                    var srcDir = FindPackDirectory(pack.Kind);
                    if (string.IsNullOrWhiteSpace(srcDir))
                        continue;
                    copied += SeedStarterPack(skillKey, companyId, srcDir, pack.FileNames);
                }
                return copied;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex,
                    "Official PLM starter seed skipped for skill {0}", skillKey);
                return 0;
            }
        }

        private static int SeedStarterPack(string skillKey, int companyId, string srcDir, string[] fileNames)
        {
            if (string.IsNullOrWhiteSpace(skillKey))
                return 0;
            var starterRoot = GenericAgentFileBL.ResolveStarter(skillKey, null, companyId);
            Directory.CreateDirectory(starterRoot);
            return CopyNamedFiles(srcDir, starterRoot, fileNames, overwrite: false);
        }

        private enum PackKind
        {
            Search,
            MassUpdate,
            ImportDw
        }

        private sealed class PackSpec
        {
            public PackKind Kind { get; init; }
            public string[] FileNames { get; init; }
            public string[] StarterSkillKeys { get; init; }
        }

        private static List<PackSpec> ResolvePacks(string skillKey)
        {
            var packs = new List<PackSpec>();
            var seedAll = skillKey.Equals(OrchestratorSkillKey, StringComparison.OrdinalIgnoreCase);

            if (seedAll || skillKey.Equals(SearchSkillKey, StringComparison.OrdinalIgnoreCase))
            {
                packs.Add(new PackSpec
                {
                    Kind = PackKind.Search,
                    FileNames = SearchFileNames,
                    StarterSkillKeys = seedAll
                        ? new[] { OrchestratorSkillKey, SearchSkillKey }
                        : new[] { SearchSkillKey }
                });
            }

            if (seedAll || skillKey.Equals(MassUpdateSkillKey, StringComparison.OrdinalIgnoreCase))
            {
                packs.Add(new PackSpec
                {
                    Kind = PackKind.MassUpdate,
                    FileNames = MassUpdateFileNames,
                    StarterSkillKeys = seedAll
                        ? new[] { OrchestratorSkillKey, MassUpdateSkillKey }
                        : new[] { MassUpdateSkillKey }
                });
            }

            if (seedAll || skillKey.Equals(ImportDwSkillKey, StringComparison.OrdinalIgnoreCase))
            {
                packs.Add(new PackSpec
                {
                    Kind = PackKind.ImportDw,
                    FileNames = ImportDwFileNames,
                    StarterSkillKeys = seedAll
                        ? new[] { OrchestratorSkillKey, ImportDwSkillKey }
                        : new[] { ImportDwSkillKey }
                });
            }

            return packs;
        }

        private static string FindPackDirectory(PackKind kind)
        {
            foreach (var root in EnumerateCandidateRoots())
            {
                IEnumerable<string> candidates = kind switch
                {
                    PackKind.Search or PackKind.MassUpdate => new[]
                    {
                        Path.Combine(root, "ImportPLMSearchView", "MultiAgent", "source"),
                        Path.Combine(root, "ImportPLMSearchView", "source"),
                        Path.Combine(root, "AgentOfficialSource", "ImportPLMSearchView"),
                        Path.Combine(root, "ImportPLMSearchView")
                    },
                    PackKind.ImportDw => new[]
                    {
                        Path.Combine(root, "ImportFromPLMDW", "source"),
                        Path.Combine(root, "AgentOfficialSource", "ImportFromPLMDW"),
                        Path.Combine(root, "ImportFromPLMDW")
                    },
                    _ => Array.Empty<string>()
                };

                var marker = kind switch
                {
                    PackKind.Search => "_plm_probe_search.sql",
                    PackKind.MassUpdate => "_plm_probe_massupdate.sql",
                    _ => "_gen_plmdw_import_sql.ps1"
                };

                foreach (var dir in candidates)
                {
                    if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, marker)))
                        return dir;
                }
            }
            return null;
        }

        private static IEnumerable<string> EnumerateCandidateRoots()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roots = new List<string>();

            void Offer(string path)
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                try
                {
                    var full = Path.GetFullPath(path);
                    if (seen.Add(full) && Directory.Exists(full))
                        roots.Add(full);
                }
                catch
                {
                    // ignore invalid path
                }
            }

            Offer(AppDataIntegrationAgentSkillCatalogBL.FindImportDocRoot());

            var starts = new[]
            {
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory()
            };
            foreach (var start in starts.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                Offer(start);
                Offer(Path.Combine(start, "AgentOfficialSource"));
                Offer(Path.Combine(start, "AppReact", "ImportDoc"));
                Offer(Path.Combine(start, "ImportDoc"));

                string walk = start;
                for (var i = 0; i < 6; i++)
                {
                    try { walk = Path.GetFullPath(Path.Combine(walk, "..")); }
                    catch { break; }
                    Offer(walk);
                    Offer(Path.Combine(walk, "AppReact", "ImportDoc"));
                    Offer(Path.Combine(walk, "ImportDoc"));
                    Offer(Path.Combine(walk, "AgentOfficialSource"));
                }
            }

            return roots;
        }

        private static int CopyNamedFiles(string srcDir, string destDir, string[] fileNames, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(srcDir) || !Directory.Exists(srcDir))
                return 0;

            Directory.CreateDirectory(destDir);
            var srcRoot = Path.GetFullPath(srcDir);
            var copied = 0;
            foreach (var name in fileNames ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    continue;
                if (name.Contains("..", StringComparison.Ordinal) || name.Contains('/') || name.Contains('\\'))
                    continue;

                var src = Path.GetFullPath(Path.Combine(srcDir, name));
                if (!src.StartsWith(srcRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(src))
                    continue;

                var dest = Path.Combine(destDir, name);
                if (!overwrite && File.Exists(dest))
                    continue;

                Directory.CreateDirectory(Path.GetDirectoryName(dest) ?? destDir);
                File.Copy(src, dest, overwrite);
                copied++;
            }
            return copied;
        }

        private static string Normalize(string skillKey)
        {
            return (skillKey ?? "").Trim();
        }
    }
}
