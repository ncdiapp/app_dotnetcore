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

    public static partial class AppProjectWorkFlowActionConverter
    {

        static partial void OnCopyEntityToDtoDone(AppProjectWorkFlowActionEntity aAppProjectWorkFlowActionEntity, AppProjectWorkFlowActionDto aAppProjectWorkFlowActionDto)
        {
            if (aAppProjectWorkFlowActionEntity.ActionType.HasValue)
            {
                //if (aAppProjectWorkFlowActionEntity.ActionType.Value == (int)EmAppWorkFlowActionType.DataLoad)
                //{
                //    aAppProjectWorkFlowActionDto.LinkToDataLoadId = ControlTypeValueConverter.ConvertValueToInt(aAppProjectWorkFlowActionEntity.FormulaExpression);
                //}

                if (aAppProjectWorkFlowActionEntity.CommandTransactionId.HasValue)
                {
                    if (!string.IsNullOrWhiteSpace(aAppProjectWorkFlowActionEntity.FormulaExpression))
                    {
                        try
                        {
                            aAppProjectWorkFlowActionDto.ActionAttribute = JsonConvert.DeserializeObject<AppActionAttributeDto>(aAppProjectWorkFlowActionEntity.FormulaExpression);
                        }
                        catch
                        {
                            aAppProjectWorkFlowActionDto.ActionAttribute = null;
                        }

                    }
                    else
                    {
                        aAppProjectWorkFlowActionDto.ActionAttribute = null;
                    }
                }
            }
        }

        static partial void OnCopyDtoToEntityDone(AppProjectWorkFlowActionEntity aAppProjectWorkFlowActionEntity, AppProjectWorkFlowActionDto aAppProjectWorkFlowActionDto)
        {
            if (aAppProjectWorkFlowActionDto.NextWorkFlowGuId.HasValue)
            {
                aAppProjectWorkFlowActionEntity.NextWorkFlowGuid = aAppProjectWorkFlowActionDto.NextWorkFlowGuId;
            }

            if (aAppProjectWorkFlowActionDto.ActionType.HasValue)
            {
                if (aAppProjectWorkFlowActionDto.ActionType.Value == (int)EmAppWorkFlowActionType.DataLoad)
                {
                    
                }
                else if (aAppProjectWorkFlowActionDto.CommandTransactionId.HasValue)
                {
                    //try
                    //{
                    //    aAppProjectWorkFlowActionEntity.FormulaExpression = JsonConvert.SerializeObject(aAppProjectWorkFlowActionDto.ActionAttribute);
                    //}
                    //catch
                    //{
                    //    aAppProjectWorkFlowActionEntity.FormulaExpression = string.Empty;
                    //}

                    aAppProjectWorkFlowActionEntity.FormulaExpression = JsonConvert.SerializeObject(aAppProjectWorkFlowActionDto.ActionAttribute);
                }
            }
        }


    }
}

