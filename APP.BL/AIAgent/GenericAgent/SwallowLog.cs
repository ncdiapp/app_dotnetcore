using System;
using System.Runtime.CompilerServices;

namespace App.BL.AIAgent.GenericAgent
{
    // Every catch that swallows an exception must leave a trace (handbook rule). Use where the failure is
    // genuinely non-fatal, e.g. best-effort cleanup or optional prompt enrichment.
    public static class SwallowLog
    {
        public static void Write(Exception ex, string why = null, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            var where = System.IO.Path.GetFileNameWithoutExtension(file) + "." + member;
            NLog.LogManager.GetLogger(where).Warn(ex, string.IsNullOrEmpty(why) ? "Non-fatal exception swallowed" : "Non-fatal exception swallowed: " + why);
        }
    }
}
