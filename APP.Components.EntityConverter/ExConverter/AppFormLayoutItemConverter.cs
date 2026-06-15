using APP.LBL.EntityClasses;
using APP.Framework;

using APP.Components.EntityDto;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace APP.Components.EntityConverter
{
	/// <summary>
	/// Convert Properties between  AppFormLayoutItemEntity and  AppFormLayoutItemDto
	/// </summary>
	public static partial class AppFormLayoutItemConverter
    {
        
        static partial void OnCopyEntityToDtoDone(AppFormLayoutItemEntity aAppFormLayoutItemEntity, AppFormLayoutItemDto aAppFormLayoutItemDto)
        {		

			if (!string.IsNullOrWhiteSpace(aAppFormLayoutItemEntity.ParameterKeyValue))
            {
                try
                {
                    aAppFormLayoutItemDto.DomAttribute = JsonConvert.DeserializeObject<AppFormDomAttributeDto>(aAppFormLayoutItemEntity.ParameterKeyValue);
                }
                catch
                {
                    aAppFormLayoutItemDto.DomAttribute = null;
                }
                
            }
            else
            {
                aAppFormLayoutItemDto.DomAttribute = null;
            }
        }

        static partial void OnCopyDtoToEntityDone(AppFormLayoutItemEntity aAppFormLayoutItemEntity, AppFormLayoutItemDto aAppFormLayoutItemDto)
        {          

            if (aAppFormLayoutItemDto.DomAttribute != null)
            {
                try
                {
                    aAppFormLayoutItemEntity.ParameterKeyValue = JsonConvert.SerializeObject(aAppFormLayoutItemDto.DomAttribute);
                }
                catch
                {
                    aAppFormLayoutItemEntity.ParameterKeyValue = string.Empty;
                }
                
            }
            else
            {
                aAppFormLayoutItemEntity.ParameterKeyValue = string.Empty;
            }    
        }        
    }
}