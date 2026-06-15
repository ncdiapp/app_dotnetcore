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
    /// Convert Properties between  AppProjectTaskPredecessorEntity and  AppProjectTaskPredecessorDto
    /// </summary>
    public static partial class AppProjectWorkFlowTaskConverter
    {
        static partial void OnCopyEntityToDtoDone(AppProjectWorkFlowTaskEntity aAppProjectWorkFlowTaskEntity, AppProjectWorkFlowTaskDto aAppProjectWorkFlowTaskDto)
        {
            if (aAppProjectWorkFlowTaskEntity.IsConvertDBUtcToClient)
            {
                if (ClientTimeZoneHelper.IsClientUsingTimeZone)
                {

                    aAppProjectWorkFlowTaskDto.DateActualEnd = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowTaskEntity.DateActualEnd);
                    aAppProjectWorkFlowTaskDto.DateActualStart = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowTaskEntity.DateActualStart);

                    aAppProjectWorkFlowTaskDto.DateModelEnd = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowTaskEntity.DateModelEnd);
                    aAppProjectWorkFlowTaskDto.DateModelStart = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowTaskEntity.DateModelStart);

                    aAppProjectWorkFlowTaskDto.DatePlannedEnd = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowTaskEntity.DatePlannedEnd);
                    aAppProjectWorkFlowTaskDto.DatePlannedStart = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowTaskEntity.DatePlannedStart);

                }
            }
        }

        static partial void OnCopyDtoToEntityDone(AppProjectWorkFlowTaskEntity aAppProjectWorkFlowTaskEntity, AppProjectWorkFlowTaskDto aAppProjectWorkFlowTaskDto)
        {
            aAppProjectWorkFlowTaskEntity.IsExternalChildSumaryTask = aAppProjectWorkFlowTaskDto.IsEnableNotifyPropertyValidationResult;

        }


    }
}

