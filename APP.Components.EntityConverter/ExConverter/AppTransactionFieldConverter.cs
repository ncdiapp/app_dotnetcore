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
    /// Convert Properties between  AppTransactionFieldEntity and  AppTransactionFieldDto
    /// </summary>
    public static partial class AppTransactionFieldConverter
    {      
        static partial void OnCopyEntityToDtoDone(AppTransactionFieldEntity aAppTransactionFieldEntity, AppTransactionFieldDto aAppTransactionFieldDto)
        {
         
          
        }

        static partial void OnCopyDtoToEntityDone(AppTransactionFieldEntity aAppTransactionFieldEntity, AppTransactionFieldDto aAppTransactionFieldDto)
        {
            if (aAppTransactionFieldDto.ControlType == (int)EmAppControlType.DateTimeDetail)
            {
                aAppTransactionFieldEntity.DataType = (int)EmAppDataType.DateTime;
            }
            else if (aAppTransactionFieldDto.ControlType == (int)EmAppControlType.Date)
            {
                aAppTransactionFieldEntity.DataType = (int)EmAppDataType.Date;
            }
            else if (aAppTransactionFieldDto.ControlType == (int)EmAppControlType.Time)
            {
                aAppTransactionFieldEntity.DataType = (int)EmAppDataType.Time;
            }
            else if (aAppTransactionFieldDto.ControlType == (int)EmAppControlType.Numeric)
            {
                if (aAppTransactionFieldDto.Nbdecimal.HasValue && aAppTransactionFieldDto.Nbdecimal > 0)
                {
                    aAppTransactionFieldEntity.DataType = (int)EmAppDataType.Decimal;
                }                
            }

        }
    }
}