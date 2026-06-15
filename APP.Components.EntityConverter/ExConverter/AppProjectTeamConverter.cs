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
    /// Convert Properties between  AppProjectTeamEntity and  AppProjectTeamDto
    /// </summary>
    public static partial class AppProjectTeamConverter
	{

        static partial void OnCopyEntityToDtoDone(AppProjectTeamEntity aAppProjectTeamEntity, AppProjectTeamDto aAppProjectTeamDto)
        {
          

			if(! string.IsNullOrWhiteSpace (aAppProjectTeamEntity.ParticipatedDmainId ))
			{
				string [] ids = aAppProjectTeamEntity.ParticipatedDmainId.Split("|".ToCharArray());
				aAppProjectTeamDto.ParticipateDomainIdList = new List<int>();

				ids.ForAll(o => aAppProjectTeamDto.ParticipateDomainIdList.Add (int.Parse(o)));

			}
        }

        static partial void OnCopyDtoToEntityDone(AppProjectTeamEntity aAppProjectTeamEntity, AppProjectTeamDto aAppProjectTeamDto)
        {

			if(! aAppProjectTeamDto.ParticipateDomainIdList.IsEmpty ())
			{
				List<string> ids = new List<string>();

				aAppProjectTeamDto.ParticipateDomainIdList.ForAll(o => ids.Add(o.ToString()));
				aAppProjectTeamEntity.ParticipatedDmainId = ids.Aggregate((current, next) => current + "|" + next);
			}

            //if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            //{
            //    aAppProjectTeamEntity.CreatedDate = ClientTimeZoneHelper.ConvertClientToUTCDateTime(aAppProjectTeamDto.CreatedDate);
            //    //aAppProjectTeamEntity.ModifiedDate = ClientTimeZoneHelper.ConvertClientToUTCDateTime(aAppProjectTeamDto.ModifiedDate);
            //}
        }
   
       
    }
}

 