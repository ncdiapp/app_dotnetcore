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

namespace APP.Components.EntityConverter
{
    /// <summary>
    /// Convert Properties between  AppSearchEntity and  AppSearchDto
    /// </summary>
    public static partial class AppSearchConverter
    {    
        static partial void OnCopyEntityToDtoDone(AppSearchEntity aAppSearchEntity, AppSearchDto aAppSearchDto)
        {
            
        }

        static partial void OnCopyDtoToEntityDone(AppSearchEntity aAppSearchEntity, AppSearchDto aAppSearchDto)
        {
            
            
        }
    }
}