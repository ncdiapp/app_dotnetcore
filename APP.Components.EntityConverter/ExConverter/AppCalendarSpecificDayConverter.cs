

 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.LBL.EntityClasses;
using APP.Components.EntityDto;



namespace APP.Components.EntityConverter
{
    /// <summary>
    /// Convert Properties between  AppDesktopEntity and  AppDesktopDto
    /// </summary>
    public static partial class AppCalendarSpecificDayConverter
    {
        static partial void OnCopyEntityToDtoDone(AppCalendarSpecificDayEntity aAppCalendarSpecificDayEntity, AppCalendarSpecificDayDto aAppCalendarSpecificDayDto)
        {
            if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCalendarSpecificDayDto.StartDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarSpecificDayEntity.StartDate);
                aAppCalendarSpecificDayDto.EndDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarSpecificDayEntity.EndDate);
            }
        }

        static partial void OnCopyDtoToEntityDone(AppCalendarSpecificDayEntity aAppCalendarSpecificDayEntity, AppCalendarSpecificDayDto aAppCalendarSpecificDayDto)
        {
            //if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            //{
            //    aAppCalendarSpecificDayEntity.StartDate = ClientTimeZoneHelper.ConvertClientToUTCDateTime(aAppCalendarSpecificDayDto.StartDate);
            //    aAppCalendarSpecificDayEntity.EndDate = ClientTimeZoneHelper.ConvertClientToUTCDateTime(aAppCalendarSpecificDayDto.EndDate);
            //}
        }


    }
}

