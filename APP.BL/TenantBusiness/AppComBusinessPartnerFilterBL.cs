using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Components.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System;

using APP.Framework;
namespace App.BL
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks></remarks>
    public static class AppComBusinessPartnerFilterBL
    {
        internal static void SetupBusinessPartnerFilterCondition(AppSearchViewEntity viewEntity, List<string> condtionString)
        {
            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {
                if (ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.Customer ||
                    ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.Supplier ||
                    ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.ClientAgent ||
                    ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.SupplierAgent
                 )
                {

                    int? parternetEntityId = null;

                    if (ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.Customer)
                    {
                        parternetEntityId = AppTenantSettingBL.GetIntValue(EmTenantSettings.CustomerEntity);
                    }
                    else if (ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.Supplier)
                    {
                        parternetEntityId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SupplierEntity);
                    }
                    else if(ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.ClientAgent)
                    {
                        parternetEntityId = AppTenantSettingBL.GetIntValue(EmTenantSettings.CustomerAgentEntity);
                    }
                    else if (ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.SupplierAgent)
                    {
                        parternetEntityId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SupplierAgentEntity);
                    }

                    if (parternetEntityId.HasValue)
                    {
                        var partnerIdColumn = viewEntity.AppSearchViewField.Where(o => o.EntityId.HasValue && o.EntityId.Value == parternetEntityId.Value).FirstOrDefault();

                        if (partnerIdColumn != null)
                        {
                            condtionString.Add(partnerIdColumn.SysTableFiledPath + "=" + ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId);
                        }
                    }

                }
            }
        }

    }
}
