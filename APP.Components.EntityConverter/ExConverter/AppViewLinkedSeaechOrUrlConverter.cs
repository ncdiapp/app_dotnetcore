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
    /// Convert Properties between  AppViewLinkedSeaechOrUrlEntity and  AppViewLinkedSeaechOrUrlDto
    /// </summary>
    public static partial class AppViewLinkedSeaechOrUrlConverter
    {
        static partial void OnCopyEntityToDtoDone(AppViewLinkedSeaechOrUrlEntity aAppViewLinkedSeaechOrUrlEntity, AppViewLinkedSeaechOrUrlDto aAppViewLinkedSeaechOrUrlDto)
        {
            if (!string.IsNullOrWhiteSpace(aAppViewLinkedSeaechOrUrlEntity.OtherSettings))
            {
                try
                {
                    aAppViewLinkedSeaechOrUrlDto.OtherSettingsDto = JsonConvert.DeserializeObject<AppViewLinkedSearchOrUrlOtherSettingsDto>(aAppViewLinkedSeaechOrUrlEntity.OtherSettings);
                }
                catch
                {
                    aAppViewLinkedSeaechOrUrlDto.OtherSettingsDto = new AppViewLinkedSearchOrUrlOtherSettingsDto();
                }

            }
            else
            {
                aAppViewLinkedSeaechOrUrlDto.OtherSettingsDto = new AppViewLinkedSearchOrUrlOtherSettingsDto();
            }
        }

        static partial void OnCopyDtoToEntityDone(AppViewLinkedSeaechOrUrlEntity aAppViewLinkedSeaechOrUrlEntity, AppViewLinkedSeaechOrUrlDto aAppViewLinkedSeaechOrUrlDto)
        {
            try
            {
                aAppViewLinkedSeaechOrUrlEntity.OtherSettings = JsonConvert.SerializeObject(aAppViewLinkedSeaechOrUrlDto.OtherSettingsDto);
            }
            catch
            {
                aAppViewLinkedSeaechOrUrlEntity.OtherSettings = string.Empty;
            }
        }
    }
}