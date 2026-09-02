using System;
using NLog;

namespace App.BL.AppDataIntegrationAgent
{
    public static class AppDataIntegrationMcpMetricsBL
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static int _runSelectCount;
        private static long _runSelectMsTotal;
        private static long _runSelectBytesTotal;

        public static void LogRunSelect(string sessionId, long elapsedMs, int responseBytes, int rowCount)
        {
            System.Threading.Interlocked.Increment(ref _runSelectCount);
            System.Threading.Interlocked.Add(ref _runSelectMsTotal, elapsedMs);
            System.Threading.Interlocked.Add(ref _runSelectBytesTotal, responseBytes);
            Log.Info(
                "MCP run_select session={0} elapsedMs={1} responseBytes={2} rowCount={3} sessionTotal={4}",
                sessionId ?? "",
                elapsedMs,
                responseBytes,
                rowCount,
                _runSelectCount);
        }

        public static void LogGetTableSchema(string sessionId, long elapsedMs, int responseBytes, string tableName)
        {
            Log.Info(
                "MCP get_table_schema session={0} elapsedMs={1} responseBytes={2} table={3}",
                sessionId ?? "",
                elapsedMs,
                responseBytes,
                tableName ?? "");
        }
    }
}
