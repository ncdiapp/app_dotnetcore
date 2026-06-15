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
    /// Convert Properties between  AppEntityInfoEntity and  AppEntityInfoDto
    /// </summary>
    public static partial class AppEntityInfoConverter
    {
        static partial void OnCopyEntityToDtoDone(AppEntityInfoEntity aAppEntityInfoEntity, AppEntityInfoDto aAppEntityInfoDto)
        {
            if (!string.IsNullOrWhiteSpace(aAppEntityInfoEntity.OtherSettings))
            {
                try
                {
                    aAppEntityInfoDto.OtherSettingsDto = JsonConvert.DeserializeObject<AppEntityInfoOtherSettingsDto>(aAppEntityInfoEntity.OtherSettings);
                }
                catch
                {
                    aAppEntityInfoDto.OtherSettingsDto = new AppEntityInfoOtherSettingsDto();
                }

            }
            else
            {
                aAppEntityInfoDto.OtherSettingsDto = new AppEntityInfoOtherSettingsDto();
            }
        }

        static partial void OnCopyDtoToEntityDone(AppEntityInfoEntity aAppEntityInfoEntity, AppEntityInfoDto aAppEntityInfoDto)
        {
            try
            {
                aAppEntityInfoEntity.OtherSettings = JsonConvert.SerializeObject(aAppEntityInfoDto.OtherSettingsDto);
            }
            catch
            {
                aAppEntityInfoEntity.OtherSettings = string.Empty;
            }
        }
    }
}