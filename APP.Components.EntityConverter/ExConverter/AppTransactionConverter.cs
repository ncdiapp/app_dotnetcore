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

    public static partial class AppTransactionConverter
    {

        static partial void OnCopyEntityToDtoDone(AppTransactionEntity aAppTransactionEntity, AppTransactionDto aAppTransactionDto)
        {
            if (!string.IsNullOrWhiteSpace(aAppTransactionEntity.PostProcessStoreProcedure))
            {
                try
                {
                    aAppTransactionDto.OtherOptions = JsonConvert.DeserializeObject<TransactionOptionDto>(aAppTransactionEntity.PostProcessStoreProcedure);
                }
                catch
                {
                    aAppTransactionDto.OtherOptions = new TransactionOptionDto();
                }

                //aAppTransactionDto.ApiInputParameterList = new List<string>();

                
                //if (aAppTransactionDto.OtherOptions != null && !string.IsNullOrWhiteSpace(aAppTransactionDto.OtherOptions.ApiLogicKeyParameterName))
                //{
                //    aAppTransactionDto.ApiInputParameterList = aAppTransactionDto.OtherOptions.ApiLogicKeyParameterName.Split('|').ToList();
                //}

            }
            else
            {
                aAppTransactionDto.PostProcessStoreProcedure = null;
            }
        }

        static partial void OnCopyDtoToEntityDone(AppTransactionEntity aAppTransactionEntity, AppTransactionDto aAppTransactionDto)
        {
            try
            {
                //if (aAppTransactionDto.OtherOptions != null && aAppTransactionDto.ApiInputParameterList != null)
                //{
                //    aAppTransactionDto.OtherOptions.ApiLogicKeyParameterName = string.Join("|", aAppTransactionDto.ApiInputParameterList);
                //}

                aAppTransactionEntity.PostProcessStoreProcedure = JsonConvert.SerializeObject(aAppTransactionDto.OtherOptions);
            }
            catch
            {
                //aAppTransactionEntity.PostProcessStoreProcedure = string.Empty;
            }
        }
    }
}

