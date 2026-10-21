using System.Text.RegularExpressions;

namespace App.BL.AIAgent.GenericAgent.Memory
{
    /// <summary>
    /// Rejects text that looks like a credential before it is stored as memory.
    /// Deliberately conservative: a false positive only means one fact is not remembered.
    /// </summary>
    public static class MemorySecretScanner
    {
        private static readonly Regex[] Patterns =
        {
            new(@"(?i)\bbearer\s+[a-z0-9\-_\.=]{16,}", RegexOptions.Compiled),
            new(@"\bsk-[A-Za-z0-9_\-]{16,}", RegexOptions.Compiled),
            new(@"\bAIza[0-9A-Za-z\-_]{30,}", RegexOptions.Compiled),
            new(@"-----BEGIN [A-Z ]*PRIVATE KEY-----", RegexOptions.Compiled),
            new(@"(?i)\b(password|passwd|pwd|secret|api[_\- ]?key|access[_\- ]?token|token)\b\s*(is|=|:)\s*\S{6,}", RegexOptions.Compiled),
            new(@"(?i)(Server|Data Source)=[^;]+;.*(Password|Pwd)=", RegexOptions.Compiled),
        };

        // Long unbroken run mixing letters and digits (keys, JWTs, hashes).
        private static readonly Regex LongToken = new(@"[A-Za-z0-9+/=_\-\.]{32,}", RegexOptions.Compiled);

        public static bool ContainsSecret(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var p in Patterns)
                if (p.IsMatch(text)) return true;
            foreach (Match m in LongToken.Matches(text))
            {
                var hasDigit = false;
                var hasLetter = false;
                foreach (var c in m.Value)
                {
                    if (char.IsDigit(c)) hasDigit = true;
                    else if (char.IsLetter(c)) hasLetter = true;
                }
                if (hasDigit && hasLetter) return true;
            }
            return false;
        }
    }
}
