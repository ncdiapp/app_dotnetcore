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
    /// Convert Properties between  AppProjectOrWorkFlowEntity and  AppProjectOrWorkFlowDto
    /// </summary>
    public static partial class AppProjectOrWorkFlowConverter
    {

        static partial void OnCopyEntityToDtoDone(AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity, AppProjectOrWorkFlowDto aAppProjectOrWorkFlowDto)
        {
            if (aAppProjectOrWorkFlowEntity.IsConvertDBUtcToClient)
            {
                if (ClientTimeZoneHelper.IsClientUsingTimeZone)
                {
                    aAppProjectOrWorkFlowDto.DateAborted = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrWorkFlowEntity.DateAborted);
                    aAppProjectOrWorkFlowDto.DateModelEnd = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrWorkFlowEntity.DateModelEnd);
                    aAppProjectOrWorkFlowDto.DateModelStart = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrWorkFlowEntity.DateModelStart);

                    aAppProjectOrWorkFlowDto.DatePlannedStart = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrWorkFlowEntity.DatePlannedStart);
                    aAppProjectOrWorkFlowDto.DatePlannedEnd = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrWorkFlowEntity.DatePlannedEnd);
                    aAppProjectOrWorkFlowDto.DateActualStart = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrWorkFlowEntity.DateActualStart);
                    aAppProjectOrWorkFlowDto.DateActualEnd = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrWorkFlowEntity.DateActualEnd);



                }
            }

            if (!string.IsNullOrWhiteSpace(aAppProjectOrWorkFlowEntity.ParticipatedDmainId))
            {
                string[] ids = aAppProjectOrWorkFlowEntity.ParticipatedDmainId.Split("|".ToCharArray());
                aAppProjectOrWorkFlowDto.ParticipateDomainIdList = new List<int>();

                ids.ForAll(o => aAppProjectOrWorkFlowDto.ParticipateDomainIdList.Add(int.Parse(o)));

            }
        }

        static partial void OnCopyDtoToEntityDone(AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity, AppProjectOrWorkFlowDto aAppProjectOrWorkFlowDto)
        {

            if (!aAppProjectOrWorkFlowDto.ParticipateDomainIdList.IsEmpty())
            {
                List<string> ids = new List<string>();

                aAppProjectOrWorkFlowDto.ParticipateDomainIdList.ForAll(o => ids.Add(o.ToString()));
                aAppProjectOrWorkFlowEntity.ParticipatedDmainId = ids.Aggregate((current, next) => current + "|" + next);
            }

            //if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            //{
            //    aAppProjectOrWorkFlowEntity.CreatedDate = ClientTimeZoneHelper.ConvertClientToUTCDateTime(aAppProjectOrWorkFlowDto.CreatedDate);
            //    //aAppProjectOrWorkFlowEntity.ModifiedDate = ClientTimeZoneHelper.ConvertClientToUTCDateTime(aAppProjectOrWorkFlowDto.ModifiedDate);
            //}
        }


    }
}

