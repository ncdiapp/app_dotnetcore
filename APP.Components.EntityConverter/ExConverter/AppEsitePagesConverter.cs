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

    public static partial class AppEsitePagesConverter
    {

        static partial void OnCopyEntityToDtoDone(AppEsitePagesEntity aAppEsitePagesEntity, AppEsitePagesDto aAppEsitePagesDto)
        {
            if (!string.IsNullOrWhiteSpace(aAppEsitePagesEntity.DesignLayout))
            {
                try
                {
                    aAppEsitePagesDto.PageAttribute = JsonConvert.DeserializeObject<AppEsitePageAttributeDto>(aAppEsitePagesEntity.DesignLayout);
                }
                catch
                {
                    aAppEsitePagesDto.PageAttribute = null;
                }

            }
            else
            {
                aAppEsitePagesDto.PageAttribute = null;
            }
        }

        static partial void OnCopyDtoToEntityDone(AppEsitePagesEntity aAppEsitePagesEntity, AppEsitePagesDto aAppEsitePagesDto)
        {
            try
            {
                aAppEsitePagesEntity.DesignLayout = JsonConvert.SerializeObject(aAppEsitePagesDto.PageAttribute);
            }
            catch
            {
                aAppEsitePagesEntity.DesignLayout = string.Empty;
            }
        }
    }
}

