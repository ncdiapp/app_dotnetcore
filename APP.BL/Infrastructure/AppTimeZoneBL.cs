using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL.DatabaseSpecific;

namespace App.BL
{
    public static class AppTimeZoneBL
    {
        public static readonly Dictionary<string, string> DictTimeZoneShortKey = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> DictTimeZoneFullNameKey = new Dictionary<string, string>();

        static AppTimeZoneBL()
        {
            try
            {
                using (var adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
                {
                    const string sql = @"SELECT Abbreviation + '_' + Offset AS ShortKey, TimeZoneName, MicrosoftTimeZoneCode FROM AppTimeZoneAbbreviation";
                    DataTable dt = adapter.ExecuteDataTableRetrievalQuery(sql, new List<System.Data.SqlClient.SqlParameter>());
                    DictTimeZoneShortKey = dt.AsEnumerable()
                        .GroupBy(o => o[0])
                        .ToDictionary(o => o.Key as string, o => o.First()[2] as string);
                    DictTimeZoneFullNameKey = dt.AsEnumerable()
                        .GroupBy(o => o[1])
                        .ToDictionary(o => o.Key as string, o => o.First()[2] as string);
                }
            }
            catch { }
        }

        public static string GetMsTimezoneTokenFromAbbreviationAndTimezoneOffset(string abbreviation, string timeZoneOffset)
        {
            if (string.IsNullOrWhiteSpace(abbreviation) || string.IsNullOrWhiteSpace(timeZoneOffset))
                return string.Empty;

            string shortKey = abbreviation + "_" + PrepareOffsetToken(timeZoneOffset);
            DictTimeZoneShortKey.TryGetValue(shortKey, out var token);
            return token ?? string.Empty;
        }

        public static string PrepareOffsetToken(string timeZoneOffset)
        {
            int offsetMinutes;
            if (!int.TryParse(timeZoneOffset, out offsetMinutes))
                return string.Empty;

            string sign = offsetMinutes > 0 ? "-" : "+";
            int hour = Math.Abs(offsetMinutes) / 60;
            int min  = Math.Abs(offsetMinutes) % 60;

            string token = "UTC " + sign + hour;
            if (min > 0) token += ":" + min;
            return token;
        }
    }
}
