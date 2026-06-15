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
    public static partial class AppCalendarRecurringDayConverter
    {
        static partial void OnCopyEntityToDtoDone(AppCalendarRecurringDayEntity aAppCalendarRecurringDayEntity, AppCalendarRecurringDayDto aAppCalendarRecurringDayDto)
        {
            if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCalendarRecurringDayDto.RecurringStartDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarRecurringDayEntity.RecurringStartDate);
                aAppCalendarRecurringDayDto.RecurringEndDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarRecurringDayEntity.RecurringEndDate);
            }
        }

        static partial void OnCopyDtoToEntityDone(AppCalendarRecurringDayEntity aAppCalendarRecurringDayEntity, AppCalendarRecurringDayDto aAppCalendarRecurringDayDto)
        {
            //if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            //{
            //    aAppCalendarRecurringDayEntity.RecurringStartDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarRecurringDayDto.RecurringStartDate);
            //    aAppCalendarRecurringDayEntity.RecurringEndDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarRecurringDayDto.RecurringEndDate);
            //}
        }


    }
}