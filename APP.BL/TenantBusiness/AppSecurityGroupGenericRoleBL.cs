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
    /// AppSecurityUserBL.CurrentUserEntity.AppSecurityGroupMember
    /// </summary>
    /// <remarks></remarks>
    public static partial class AppSecurityGroupBL
    {

        //OrgnizationRole = 1,
        //SupplierRole = 2,
        //CustomerRole = 3,


        // _DictCompnayIdUserMasterDBDataSource
        //  // Key: ParternId, value  AppSecurityGroupEntity

        private static readonly Dictionary<int, AppSecurityGroupEntity> _DictCompnayIdGenericBuiltICompanyAdminRole = new Dictionary<int, AppSecurityGroupEntity>();
        private static readonly Dictionary<int, AppSecurityGroupEntity> _DictCompnayIdGenericBuiltInEmployeeRole = new Dictionary<int, AppSecurityGroupEntity>();
        private static readonly Dictionary<int, AppSecurityGroupEntity> _DictCompnayIdGenericBuiltInCustomerRole = new Dictionary<int, AppSecurityGroupEntity>();
        private static readonly Dictionary<int, AppSecurityGroupEntity> _DictCompnayIdGenericBuiltInSupplierRole = new Dictionary<int, AppSecurityGroupEntity>();
        private static readonly Dictionary<int, AppSecurityGroupEntity> _DictCompnayIdGenericBuiltInClientAgentRole = new Dictionary<int, AppSecurityGroupEntity>();
        private static readonly Dictionary<int, AppSecurityGroupEntity> _DictCompnayIdGenericBuiltInSupplierAgentRole = new Dictionary<int, AppSecurityGroupEntity>();

        private static readonly Dictionary<int, AppSecurityGroupEntity> _DictCompnayIdGenericBuiltInAllUserRole = new Dictionary<int, AppSecurityGroupEntity>();



        // Key: compnayId_OrgId
        private static readonly Dictionary<string, AppSecurityGroupEntity> _DictCompnayIdSpecificOrganizationRole = new Dictionary<string, AppSecurityGroupEntity>();

        //Key CompnayId_BusinessparterId
        private static readonly Dictionary<string, AppSecurityGroupEntity> _DictCompnayIdBusinessPartnerSpecificRole = new Dictionary<string, AppSecurityGroupEntity>();


        internal static AppSecurityGroupEntity GetGenericBuiltInAllUserRole()
        {

            AppSecurityGroupEntity toRetrun = null;
            int currentCompanyId = int.Parse(ServerContext.Instance.CurrentCompanyId.ToString());

            if (_DictCompnayIdGenericBuiltInAllUserRole.ContainsKey(currentCompanyId))
            {
                toRetrun = _DictCompnayIdGenericBuiltInAllUserRole[currentCompanyId];
            }
            else
            {
                toRetrun = RetriveGenericBuiltInAllUserRole();
                _DictCompnayIdGenericBuiltInAllUserRole[currentCompanyId] = toRetrun;
            }




            return toRetrun;


        }

        internal static AppSecurityGroupEntity GetGenericBuiltInRole()
        {

            AppSecurityGroupEntity toRetrun = null;
            int currentCompanyId = int.Parse(ServerContext.Instance.CurrentCompanyId.ToString());
            if (ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.SaasCompanyAdmin)
            {

                if (_DictCompnayIdGenericBuiltICompanyAdminRole.ContainsKey(currentCompanyId))
                {
                    toRetrun = _DictCompnayIdGenericBuiltICompanyAdminRole[currentCompanyId];
                }
                else
                {
                    toRetrun = RetriveGenericBuiltInRole(EmAppUserType.SaasCompanyAdmin);
                    _DictCompnayIdGenericBuiltICompanyAdminRole[currentCompanyId] = toRetrun;
                }

            }
            else if (ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.Employee)
            {

                if (_DictCompnayIdGenericBuiltInEmployeeRole.ContainsKey(currentCompanyId))
                {
                    toRetrun = _DictCompnayIdGenericBuiltInEmployeeRole[currentCompanyId];
                }
                else
                {
                    toRetrun = RetriveGenericBuiltInRole(EmAppUserType.Employee);
                    _DictCompnayIdGenericBuiltInEmployeeRole[currentCompanyId] = toRetrun;
                }

            }
            else if (ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.Customer)
            {
                if (_DictCompnayIdGenericBuiltInCustomerRole.ContainsKey(currentCompanyId))
                {
                    toRetrun = _DictCompnayIdGenericBuiltInCustomerRole[currentCompanyId];
                }
                else
                {
                    toRetrun = RetriveGenericBuiltInRole(EmAppUserType.Customer);
                    _DictCompnayIdGenericBuiltInCustomerRole[currentCompanyId] = toRetrun;
                }

            }


            else if (ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.Supplier)
            {
                if (_DictCompnayIdGenericBuiltInSupplierRole.ContainsKey(currentCompanyId))
                {
                    toRetrun = _DictCompnayIdGenericBuiltInSupplierRole[currentCompanyId];
                }
                else
                {
                    toRetrun = RetriveGenericBuiltInRole(EmAppUserType.Supplier);
                    _DictCompnayIdGenericBuiltInSupplierRole[currentCompanyId] = toRetrun;
                }

            }

            else if (ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.ClientAgent)
            {
                if (_DictCompnayIdGenericBuiltInClientAgentRole.ContainsKey(currentCompanyId))
                {
                    toRetrun = _DictCompnayIdGenericBuiltInClientAgentRole[currentCompanyId];
                }
                else
                {
                    toRetrun = RetriveGenericBuiltInRole(EmAppUserType.ClientAgent);
                    _DictCompnayIdGenericBuiltInClientAgentRole[currentCompanyId] = toRetrun;
                }

            }
            else if (ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.SupplierAgent)
            {
                if (_DictCompnayIdGenericBuiltInSupplierAgentRole.ContainsKey(currentCompanyId))
                {
                    toRetrun = _DictCompnayIdGenericBuiltInSupplierAgentRole[currentCompanyId];
                }
                else
                {
                    toRetrun = RetriveGenericBuiltInRole(EmAppUserType.SupplierAgent);
                    _DictCompnayIdGenericBuiltInSupplierAgentRole[currentCompanyId] = toRetrun;
                }

            }

            return toRetrun;


        }


        internal static AppSecurityGroupEntity GetSpecificBuiltInRole()
        {

            AppSecurityGroupEntity toRetrun = null;
            int currentCompanyId = int.Parse(ServerContext.Instance.CurrentCompanyId.ToString());

            if (ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.Employee)
            {
                if (AppSecurityUserBL.CurrentUserEntity.OrganizationId.HasValue)
                {
                    int orgId = AppSecurityUserBL.CurrentUserEntity.OrganizationId.Value;

                    string compnayId_OrgId = string.Format(@"{0}_{1}", currentCompanyId, orgId);

                    toRetrun = RetriveSpecificOrganizationBuiltInRole();

                }

            }
            else if (
                    ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.Customer ||
                     ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.Supplier ||
                     ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.ClientAgent ||
                     ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.SupplierAgent
                   )
            {

                int parterId = int.Parse(ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId.ToString());
                string compnayId_PartnerId = string.Format(@"{0}_{1}", currentCompanyId, parterId);

                if (_DictCompnayIdBusinessPartnerSpecificRole.ContainsKey(compnayId_PartnerId))
                {
                    toRetrun = _DictCompnayIdBusinessPartnerSpecificRole[compnayId_PartnerId];
                }
                else
                {

                    toRetrun = RetriveSpecificPartnerBuiltInRole(parterId);
                    _DictCompnayIdBusinessPartnerSpecificRole[compnayId_PartnerId] = toRetrun;
                }
            }
            else if (
                    ServerContext.Instance.CurrentLoginUserType == (int)EmAppUserType.CompanyAnonymousUser
                   )
            {
                // Need To Add Built-in Anonymous Role
            }



            return toRetrun;


        }

        internal static AppSecurityGroupEntity RetriveGenericBuiltInRole(EmAppUserType aEmAppRoleUserType)
        {


            RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityGroupFields.RoleUserTypeId == (int)aEmAppRoleUserType & AppSecurityGroupFields.IsBuiltIn == true);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecurityGroupEntity> list = new EntityCollection<AppSecurityGroupEntity>();
                adapter.FetchEntityCollection(list, filter);

                return list.FirstOrDefault();
            }


        }

        private static AppSecurityGroupEntity RetriveGenericBuiltInAllUserRole()
        {


            RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityGroupFields.RoleUserTypeId == (int)EmAppUserType.AllUser & AppSecurityGroupFields.IsBuiltIn == true);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecurityGroupEntity> list = new EntityCollection<AppSecurityGroupEntity>();
                adapter.FetchEntityCollection(list, filter);

                return list.FirstOrDefault();
            }


        }
        internal static AppSecurityGroupEntity RetriveSpecificOrganizationBuiltInRole()
        {

            RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityGroupFields.IsBuiltIn == true & AppSecurityGroupFields.RoleUserTypeId == EmAppUserType.Employee);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecurityGroupEntity> list = new EntityCollection<AppSecurityGroupEntity>();
                adapter.FetchEntityCollection(list, filter);

                return list.FirstOrDefault();
            }


        }


        internal static AppSecurityGroupEntity RetriveSpecificPartnerBuiltInRole(int partnerId)
        {

            RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityGroupFields.BusinessPartnerId == partnerId & AppSecurityGroupFields.IsBuiltIn == true);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecurityGroupEntity> list = new EntityCollection<AppSecurityGroupEntity>();
                adapter.FetchEntityCollection(list, filter);

                return list.FirstOrDefault();
            }


        }
    }
}
