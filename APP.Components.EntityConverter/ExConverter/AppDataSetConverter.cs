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
    /// Convert Properties between  AppDataSetEntity and  AppDataSetDto
    /// </summary>
    public static partial class AppDataSetConverter
    {
        static partial void OnCopyEntityToDtoDone(AppDataSetEntity aAppDataSetEntity, AppDataSetDto aAppDataSetDto)
        {
            if (!string.IsNullOrWhiteSpace(aAppDataSetEntity.OtherSettings))
            {
                try
                {
                    aAppDataSetDto.OtherSettingsDto = JsonConvert.DeserializeObject<AppDataSetOtherSettingsDto>(aAppDataSetEntity.OtherSettings);
                }
                catch
                {
                    aAppDataSetDto.OtherSettingsDto = new AppDataSetOtherSettingsDto();
                }

            }
            else
            {
                aAppDataSetDto.OtherSettingsDto = new AppDataSetOtherSettingsDto();
            }
        }

        static partial void OnCopyDtoToEntityDone(AppDataSetEntity aAppDataSetEntity, AppDataSetDto aAppDataSetDto)
        {
            try
            {
                aAppDataSetEntity.OtherSettings = JsonConvert.SerializeObject(aAppDataSetDto.OtherSettingsDto);
            }
            catch
            {
                aAppDataSetEntity.OtherSettings = string.Empty;
            }
        }
    }
}