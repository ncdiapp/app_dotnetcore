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
    /// Convert Properties between  AppDesktopEntity and  AppDesktopDto
    /// </summary>
    public static partial class AppDesktopConverter
    {
        static partial void OnCopyEntityToDtoDone(AppDesktopEntity aAppDesktopEntity, AppDesktopDto aAppDesktopDto)
        {
            if (!string.IsNullOrWhiteSpace(aAppDesktopEntity.OtherSettings))
            {
                try
                {
                    aAppDesktopDto.OtherSettingsDto = JsonConvert.DeserializeObject<AppDesktopOtherSettingsDto>(aAppDesktopEntity.OtherSettings);
                }
                catch
                {
                    aAppDesktopDto.OtherSettingsDto = new AppDesktopOtherSettingsDto();
                }

            }
            else
            {
                aAppDesktopDto.OtherSettingsDto = new AppDesktopOtherSettingsDto();
            }


            if (!string.IsNullOrWhiteSpace(aAppDesktopEntity.UserFavoriteList))
            {           
              
                try
                {
                    aAppDesktopDto.UserBookmarkList = JsonConvert.DeserializeObject<List<AppListMenuDto>>(aAppDesktopEntity.UserFavoriteList);
                    aAppDesktopDto.UserBookmarkList = aAppDesktopDto.UserBookmarkList.OrderBy(o => o.Sort).ToList();
                }
                catch
                {
                    aAppDesktopDto.UserBookmarkList = new List<AppListMenuDto>();
                }
            }
            else
            {
                aAppDesktopDto.UserBookmarkList = new List<AppListMenuDto>();
            }
        }

        static partial void OnCopyDtoToEntityDone(AppDesktopEntity aAppDesktopEntity, AppDesktopDto aAppDesktopDto)
        {
            try
            {
                aAppDesktopEntity.OtherSettings = JsonConvert.SerializeObject(aAppDesktopDto.OtherSettingsDto);
            }
            catch (Exception ex)
            {
                aAppDesktopEntity.OtherSettings = string.Empty;
            }


            try
            {
                aAppDesktopEntity.UserFavoriteList = JsonConvert.SerializeObject(aAppDesktopDto.UserBookmarkList);
            }
            catch (Exception ex)
            {
                aAppDesktopEntity.UserFavoriteList = string.Empty;
            }
        }


    }
}

 