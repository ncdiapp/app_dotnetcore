using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.LBL.EntityClasses;

using APP.Framework;
using APP.Framework.Collections;
using APP.Components.EntityDto;
using APP.Components.Dto;
using Newtonsoft.Json;

namespace APP.Components.EntityConverter
{
    /// <summary>
    /// Convert Properties between  AppFormLinkTargetEntity and  AppFormLinkTargetDto
    /// </summary>
    public static partial class AppFormLinkTargetConverter
    {
        static partial void OnCopyEntityToDtoDone(AppFormLinkTargetEntity aAppFormLinkTargetEntity, AppFormLinkTargetDto aAppFormLinkTargetDto)
        {
          
            aAppFormLinkTargetDto.OtherSettingsDto = new AppFormLinkTargetOtherSettingsDto();


            if (!string.IsNullOrWhiteSpace(aAppFormLinkTargetEntity.OtherSettings))
            {
                try
                {
                     var OtherSettingsDto = JsonConvert.DeserializeObject<AppFormLinkTargetOtherSettingsDto>(aAppFormLinkTargetEntity.OtherSettings);

                    if(OtherSettingsDto != null)
                    {
                        aAppFormLinkTargetDto.OtherSettingsDto = OtherSettingsDto;
                    }
                }
                catch
                {
                   
                }                
            }
         

            if (  string.IsNullOrWhiteSpace(aAppFormLinkTargetDto.OtherSettingsDto.UiId))
            {
                aAppFormLinkTargetDto.OtherSettingsDto.UiId = Guid.NewGuid().ToString();
            }
        }

        static partial void OnCopyDtoToEntityDone(AppFormLinkTargetEntity aAppFormLinkTargetEntity, AppFormLinkTargetDto aAppFormLinkTargetDto)
        {
            aAppFormLinkTargetEntity.OtherSettings = string.Empty;

            try
            {
                if(aAppFormLinkTargetDto.OtherSettingsDto !=null)
                {
                    aAppFormLinkTargetEntity.OtherSettings = JsonConvert.SerializeObject(aAppFormLinkTargetDto.OtherSettingsDto);
                }
              
                
            }
            catch
            {
               
            }
        }
    }
}