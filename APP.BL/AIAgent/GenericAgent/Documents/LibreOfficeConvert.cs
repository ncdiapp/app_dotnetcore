using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using APP.Framework;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>Headless LibreOffice convert. Each call uses its own user profile.</summary>
    public static class LibreOfficeConvert
    {
        public const int TimeoutMs = 120000;

        public static byte[] Convert(byte[] input, string inputExtension, string outputExtension)
        {
            if (input == null || input.Length == 0)
                throw new InvalidOperationException("Nothing to convert.");
            var soffice = ResolveSoffice();
            var dir = Path.Combine(Path.GetTempPath(), "appai-lo-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var profile = Path.Combine(dir, "profile");
            Directory.CreateDirectory(profile);
            try
            {
                var ext = inputExtension.StartsWith(".") ? inputExtension : "." + inputExtension;
                var target = outputExtension.TrimStart('.').ToLowerInvariant();
                var src = Path.Combine(dir, "in" + ext);
                File.WriteAllBytes(src, input);
                var profileUri = new Uri(profile).AbsoluteUri;
                var psi = new ProcessStartInfo
                {
                    FileName = soffice,
                    Arguments = "--headless --norestore --nolockcheck --nologo -env:UserInstallation=" + profileUri
                        + " --convert-to " + target + " --outdir \"" + dir + "\" \"" + src + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = Process.Start(psi);
                if (process == null)
                    throw new InvalidOperationException("LibreOffice did not start.");
                if (!process.WaitForExit(TimeoutMs))
                {
                    try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
                    throw new InvalidOperationException("LibreOffice timed out.");
                }
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("LibreOffice exited " + process.ExitCode + ".");
                var produced = Directory.GetFiles(dir, "*." + target).FirstOrDefault();
                if (produced == null)
                    throw new InvalidOperationException("LibreOffice wrote no ." + target + " file.");
                return File.ReadAllBytes(produced);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* temp cleanup is best effort */ }
            }
        }

        public static string ResolveSoffice()
        {
            var configured = AppConfig.Get("DocumentExport.SofficePath");
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
                return configured;
            var windows = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "LibreOffice", "program", "soffice.exe");
            if (File.Exists(windows))
                return windows;
            if (File.Exists("/usr/bin/soffice"))
                return "/usr/bin/soffice";
            throw new InvalidOperationException("LibreOffice was not found. Set DocumentExport:SofficePath.");
        }
    }
}
