using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.LBL.EntityClasses;
using APP.Components.EntityDto;
using APP.Components.Dto;
using Newtonsoft.Json;

namespace APP.Components.EntityConverter
{

    public static partial class AppIntergrationSettingConverter
    {

        static partial void OnCopyEntityToDtoDone(AppIntergrationSettingEntity aAppIntergrationSettingEntity, AppIntergrationSettingDto aAppIntergrationSettingDto)
        {
            if (!string.IsNullOrWhiteSpace(aAppIntergrationSettingEntity.ApicredentialConfig))
            {
                try
                {
                    aAppIntergrationSettingDto.OtherSettingsDto = JsonConvert.DeserializeObject<AppIntergrationOtherSettingsDto>(aAppIntergrationSettingEntity.ApicredentialConfig);
                }
                catch
                {
                    aAppIntergrationSettingDto.OtherSettingsDto = new AppIntergrationOtherSettingsDto();


                    // For leagacy API setting data
                    //aAppIntergrationSettingDto.OtherSettingsDto.ApiDefaultAuthorization = aAppIntergrationSettingEntity.ApicredentialConfig;
                }

            }
            else
            {
                aAppIntergrationSettingDto.OtherSettingsDto = new AppIntergrationOtherSettingsDto();
            }
                        

            if (!string.IsNullOrWhiteSpace(aAppIntergrationSettingDto.InternalCode) 
                && string.IsNullOrWhiteSpace(aAppIntergrationSettingDto.OtherSettingsDto.DatabaseTablePrefix))
            {
                // For leagacy API setting data
                aAppIntergrationSettingDto.OtherSettingsDto.DatabaseTablePrefix = aAppIntergrationSettingDto.InternalCode;
            }
        }

        static partial void OnCopyDtoToEntityDone(AppIntergrationSettingEntity aAppIntergrationSettingEntity, AppIntergrationSettingDto aAppIntergrationSettingDto)
        {            
            try
            {
                aAppIntergrationSettingEntity.ApicredentialConfig = JsonConvert.SerializeObject(aAppIntergrationSettingDto.OtherSettingsDto);
            }
            catch
            {
                aAppIntergrationSettingEntity.ApicredentialConfig = string.Empty;
            }
        }
    }
}

