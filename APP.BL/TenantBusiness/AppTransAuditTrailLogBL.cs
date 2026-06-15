using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;

using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System;

using APP.Framework;
namespace App.BL
{

    public static class AppTransAuditTrailLogBL
    {       
        public static List<AppTransAuditTrailLogExDto> RetrieveTransactionFormChangeLog(int? transactionId, string transactionRId)
        {
            if (transactionId.HasValue && !string.IsNullOrWhiteSpace(transactionRId))
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppTransAuditTrailLogEntity> entityList = new EntityCollection<AppTransAuditTrailLogEntity>();

                    IRelationPredicateBucket filter = new RelationPredicateBucket(AppTransAuditTrailLogFields.TransactionId == transactionId & AppTransAuditTrailLogFields.RootValueId == transactionRId);

                    adapter.FetchEntityCollection(entityList, filter);


                    var aDtoList = new List<AppTransAuditTrailLogExDto>();
                    foreach (var AppTransAuditTrailLogEntity in entityList)
                    {
                        AppTransAuditTrailLogExDto aAppTransAuditTrailLogExDto = AppTransAuditTrailLogConverter.ConvertEntityToExDto(AppTransAuditTrailLogEntity);


                        aDtoList.Add(aAppTransAuditTrailLogExDto);

                    }
                    return aDtoList;
                }

            }

            return null;
            
        }   
    }
}