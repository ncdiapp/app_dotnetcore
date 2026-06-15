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
    /// Convert Properties between  AppSefolderEntity and  AppSefolderDto
    /// </summary>
    public static partial class AppSefolderConverter
    {
        static partial void OnCopyEntityToDtoDone(AppSefolderEntity aAppSefolderEntity, AppSefolderDto aAppSefolderDto)
        {
            if (!string.IsNullOrWhiteSpace(aAppSefolderEntity.OtherSettings))
            {
                try
                {
                    aAppSefolderDto.DragDropPostProcessSetting = JsonConvert.DeserializeObject<AppFolderOtherSettingsDto>(aAppSefolderEntity.OtherSettings);
                }
                catch
                {
                    aAppSefolderDto.DragDropPostProcessSetting = new AppFolderOtherSettingsDto();
                }

            }
            else
            {
                aAppSefolderDto.DragDropPostProcessSetting = new AppFolderOtherSettingsDto();
            }
        }

        static partial void OnCopyDtoToEntityDone(AppSefolderEntity aAppSefolderEntity, AppSefolderDto aAppSefolderDto)
        {
            try
            {
                aAppSefolderEntity.OtherSettings = JsonConvert.SerializeObject(aAppSefolderDto.DragDropPostProcessSetting);
            }
            catch
            {
                aAppSefolderEntity.OtherSettings = string.Empty;
            }
        }
    }
}