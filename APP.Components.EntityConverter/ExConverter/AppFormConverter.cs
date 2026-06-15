using APP.LBL.EntityClasses;
using APP.Framework;

using APP.Components.EntityDto;
using Newtonsoft.Json;
using System.Collections.Generic;
using APP.Components.Dto;

namespace APP.Components.EntityConverter
{
	/// <summary>
	/// Convert Properties between  AppFormEntity and  AppFormDto
	/// </summary>
	public static partial class AppFormConverter
    {
        
        static partial void OnCopyEntityToDtoDone(AppFormEntity aAppFormEntity, AppFormDto aAppFormDto)
        {
            aAppFormDto.DefaultDeviceType = ControlTypeValueConverter.ConvertValueToInt(aAppFormEntity.RouteParamter1);
            aAppFormDto.DefaultNbColumns = ControlTypeValueConverter.ConvertValueToInt(aAppFormEntity.RouteParamter2);
        }

        static partial void OnCopyDtoToEntityDone(AppFormEntity aAppFormEntity, AppFormDto aAppFormDto)
        {          

            if (aAppFormDto.DefaultDeviceType.HasValue)
            {
                aAppFormEntity.RouteParamter1 = aAppFormDto.DefaultDeviceType.Value.ToString();
            }
            else
            {
                aAppFormEntity.RouteParamter1 = string.Empty;
            }

            if (aAppFormDto.DefaultNbColumns.HasValue)
            {
                aAppFormEntity.RouteParamter2 = aAppFormDto.DefaultNbColumns.Value.ToString();
            }
            else
            {
                aAppFormEntity.RouteParamter2 = string.Empty;
            }
        }        
    }
}