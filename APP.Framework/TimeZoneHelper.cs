using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APP.Framework
{
    public static class TimeZoneHelper
    {
		//ranges from -23 through 23.
		 public static readonly Dictionary<int, string> DictHoursTimeZoneId  = GetHourTimzoe();
		public static Dictionary<string, string> GetTimeZoneList()
        {
            Dictionary<string, string> toReturnIdList = new Dictionary<string, string>();
            foreach (TimeZoneInfo zone in TimeZoneInfo.GetSystemTimeZones())
            {
                toReturnIdList.Add(zone.Id, zone.DisplayName);
            }

            return toReturnIdList;
        }


		private static Dictionary<int, string> GetHourTimzoe()
		{
			Dictionary<int, string> toReturnIdList = new Dictionary<int, string>();
			foreach (TimeZoneInfo zone in TimeZoneInfo.GetSystemTimeZones())
			{
				int hours = zone.BaseUtcOffset.Hours;
				if (!toReturnIdList.ContainsKey(hours) )
				{
					toReturnIdList.Add(hours, zone.Id);
				}
				
			}

			return toReturnIdList;
		}

        /// <summary>
        /// FindSystemTimeZoneById throws TimeZoneNotFoundException for null/empty/unknown IDs.
        /// Callers already treat a missing zone as "leave the datetime unchanged".
        /// </summary>
        private static TimeZoneInfo TryFindTimeZone(string clientTimeZonekey)
        {
            if (string.IsNullOrWhiteSpace(clientTimeZonekey))
                return null;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(clientTimeZonekey);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
            catch (InvalidTimeZoneException)
            {
                return null;
            }
        }

		public static DateTime? ConvertClientToUTCDateTime(DateTime? clientDateTime, string clientTimeZonekey)
        {
            if (!clientDateTime.HasValue)
            {
                return null;
            }

            TimeZoneInfo zone = TryFindTimeZone(clientTimeZonekey);

            if (zone == null || zone == TimeZoneInfo.Utc)
            {
                return clientDateTime;
            }

            clientDateTime = DateTime.SpecifyKind(clientDateTime.Value, DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTime(clientDateTime.Value, zone, TimeZoneInfo.Utc);
        }

        public static DateTime? ConvertUTCToClientDateTime(DateTime? utcDateTime, string clientTimeZonekey)
        {
            if (!utcDateTime.HasValue)
                return null;

            TimeZoneInfo zone = TryFindTimeZone(clientTimeZonekey);

            if (zone == null || zone == TimeZoneInfo.Utc)
            {
                return utcDateTime;
            }

            return TimeZoneInfo.ConvertTime(utcDateTime.Value, TimeZoneInfo.Utc, zone);
        }

        public static DateTime ConvertUTCToClientDateTime(DateTime utcDateTime, string clientTimeZonekey)
        {
            if (utcDateTime == default(DateTime))
            {
                return default(DateTime);
            }

            TimeZoneInfo zone = TryFindTimeZone(clientTimeZonekey);

            // Cannot find zone or input date is Utc
            if (zone == null || zone == TimeZoneInfo.Utc  )
            {
                return utcDateTime;
            }
            else
            {                
                return TimeZoneInfo.ConvertTime(utcDateTime, TimeZoneInfo.Utc, zone);
            }
        }

        public static DateTime ConvertClientToUTCDateTime(DateTime clientDateTime, string clientTimeZonekey)
        {
            if (clientDateTime == default(DateTime))
            {
                return default(DateTime);
            }

            clientDateTime = DateTime.SpecifyKind(clientDateTime, DateTimeKind.Unspecified);

            TimeZoneInfo zone = TryFindTimeZone(clientTimeZonekey);

            // Cannot find zone or input date is Utc
            if (zone == null || zone == TimeZoneInfo.Utc)
            {
                return clientDateTime;
            }

            return TimeZoneInfo.ConvertTime(clientDateTime, zone, TimeZoneInfo.Utc);
        }

    }
}