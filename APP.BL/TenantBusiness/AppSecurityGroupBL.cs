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
    public static partial class AppSecurityGroupBL
    {
        private static string MasterConnStr => AppCompanyBL.AppMasterDBConnectionString;

        public static AppSecurityGroupEntity RetrieveOneAppSecurityGroupEntity(object groupId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSecurityGroupEntity userGroupEntity = new AppSecurityGroupEntity(int.Parse(groupId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityGroupEntity);
                rootPath.Add(AppSecurityGroupEntity.PrefetchPathAppSecurityGroupMember);
                // AppSecurityUser is master-DB only — do not prefetch AppSecurityUser via tenant adapter.

                adpater.FetchEntity(userGroupEntity, rootPath);
                return userGroupEntity;


            }

        }

        private static Dictionary<int, AppSecurityUserEntity> RetrieveMasterUserEntitiesByIds(List<int> userIds)
        {
            var dict = new Dictionary<int, AppSecurityUserEntity>();
            if (userIds == null || userIds.Count == 0)
            {
                return dict;
            }

            EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();
            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserFields.UserId == userIds.ToArray());
                adapter.FetchEntityCollection(users, filter);
            }

            foreach (AppSecurityUserEntity user in users)
            {
                dict[user.UserId] = user;
            }

            return dict;
        }

        public static ObservableSet<AppSecurityGroupDto> RetrieveAllAppSecurityGroupDto(int? companyId = null)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecurityGroupEntity> list = new EntityCollection<AppSecurityGroupEntity>();
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityGroupEntity);
                rootPath.Add(AppSecurityGroupEntity.PrefetchPathAppSecurityGroupMember);

                // need to add Permission entity code the the entity management
                //Dictionary<int, AppComOrganizationDto> dictOrganization = null;
                Dictionary<int, AppBusinessPartnerDto> dictPartner = null;

                int? effectiveCompanyId = companyId ?? ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
                if (effectiveCompanyId.HasValue)
                {
                    dictPartner = AppBusinessPartnerBL.RetrieveCompanyPartnerDtoList(effectiveCompanyId.Value, null).ToDictionary(o => (int)o.Id, o => o);
                }

                adapter.FetchEntityCollection(list, null, rootPath);

                // List<LookupItemDto>  = AppEntityInfoBL.GetEntityDisplayInfoListByentityCode(EmEntityCode.PDMUser);

                List<LookupItemDto> users = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");


                var aDtoList = new ObservableSet<AppSecurityGroupDto>();
                foreach (var o in list)
                {
                    AppSecurityGroupDto group = AppSecurityGroupConverter.ConvertEntityToDto(o);
                    aDtoList.Add(group);

                    List<int> uids = o.AppSecurityGroupMember.Select(gm => gm.UserId).ToList();
                    //???
                    group.Users = users.Where(d => uids.Contains((int)d.Id)).ToList();
                    group.GroupUserString = EntityHelper.ConvertLookupListToString(group.Users);

                    //if (group.InternalCode == EmAppSecurityGroupInernalCode.CustomerAdmin.ToString()
                    //   || group.InternalCode == EmAppSecurityGroupInernalCode.SupplierAdmin.ToString()
                    //   || group.InternalCode == EmAppSecurityGroupInernalCode.ClientAgentAdmin.ToString())
                    //{
                    //    group.IsDisableEdit = true;
                    //}



                    if (group.RoleUserTypeId.HasValue)
                    {
                        string userTypeName = ((EmAppUserType)(group.RoleUserTypeId.Value)).ToString();

                        group.OrganizationPath = userTypeName;

                        if (group.RoleUserTypeId.Value == (int)EmAppUserType.Employee)
                        {
                            //if (dictOrganization != null && group.OrganizationId.HasValue && dictOrganization.ContainsKey(group.OrganizationId.Value))
                            //{
                            //    group.OrganizationPath = userTypeName + "/ " + dictOrganization[group.OrganizationId.Value].PathName;
                            //}                         
                        }
                        else if (group.RoleUserTypeId.Value == (int)EmAppUserType.Customer
                                || group.RoleUserTypeId.Value == (int)EmAppUserType.Supplier
                                 || group.RoleUserTypeId.Value == (int)EmAppUserType.ClientAgent
                                 || group.RoleUserTypeId.Value == (int)EmAppUserType.SupplierAgent)
                        {
                            if (dictPartner != null && group.BusinessPartnerId.HasValue && dictPartner.ContainsKey(group.BusinessPartnerId.Value))
                            {
                                group.OrganizationPath = userTypeName + "/ " + dictPartner[group.BusinessPartnerId.Value].Code;
                            }
                        }
                    }

                }


                return aDtoList;


            }

        }


        //private static void PrepareGroupPathString(Dictionary<int, AppComOrganizationDto> dictOrganization, AppSecurityGroupDto groupDto)
        //{
        //    if (groupDto.RoleUserTypeId.HasValue)
        //    {
        //        if (groupDto.RoleUserTypeId.Value == (int)EmAppUserType.Employee)
        //        {
        //            if (dictOrganization != null && groupDto.OrganizationId.HasValue && dictOrganization.ContainsKey(groupDto.OrganizationId.Value))
        //            {
        //                groupDto.OrganizationPath = dictOrganization[groupDto.OrganizationId.Value].PathName + "/ ";
        //            }
        //        }
        //        else if (groupDto.RoleUserTypeId.Value == (int)EmAppUserType.Customer)
        //        {
        //            groupDto.OrganizationPath = "Customer/ ";


        //            if (groupDto.OrganizationPath.EndsWith(", "))
        //            {
        //                groupDto.OrganizationPath = groupDto.OrganizationPath.Substring(0, groupDto.OrganizationPath.Length - 2);
        //            }

        //        }
        //        else if (groupDto.RoleUserTypeId.Value == (int)EmAppUserType.Supplier)
        //        {

        //        }
        //    }
        //}

        public static AppSecurityGroupExDto RetrieveOneAppSecurityGroupExDto(object groupId)
        {
            AppSecurityGroupEntity aAppSecurityGroupEntity = RetrieveOneAppSecurityGroupEntity(groupId);

            AppSecurityGroupExDto aAppSecurityGroupExDto = AppSecurityGroupConverter.ConvertEntityToExDto(aAppSecurityGroupEntity);

            List<int> memberUserIds = aAppSecurityGroupEntity.AppSecurityGroupMember
                .Select(m => m.UserId)
                .Distinct()
                .ToList();
            Dictionary<int, AppSecurityUserEntity> dictMasterUsers = RetrieveMasterUserEntitiesByIds(memberUserIds);

            foreach (AppSecurityGroupMemberEntity o in aAppSecurityGroupEntity.AppSecurityGroupMember)
            {
                AppSecurityGroupMemberExDto aAppSecurityGroupMemberExDto = AppSecurityGroupMemberConverter.ConvertEntityToExDto(o);
                if (dictMasterUsers.TryGetValue(o.UserId, out AppSecurityUserEntity masterUser))
                {
                    aAppSecurityGroupMemberExDto.ForeignAppSecurityUserExDto = AppSecurityUserConverter.ConvertEntityToExDto(masterUser);
                }
                aAppSecurityGroupExDto.AppSecurityGroupMemberList.Add(aAppSecurityGroupMemberExDto);
            }

            if (aAppSecurityGroupExDto.RoleUserTypeId.HasValue && aAppSecurityGroupExDto.AppCreatedByCompanyId.HasValue)
            {
                string userTypeName = ((EmAppUserType)(aAppSecurityGroupExDto.RoleUserTypeId.Value)).ToString();
                aAppSecurityGroupExDto.OrganizationPath = userTypeName;

                if (aAppSecurityGroupExDto.RoleUserTypeId.Value == (int)EmAppUserType.Employee)
                {
                    //if (aAppSecurityGroupExDto.OrganizationId.HasValue)
                    //{
                    //    var organizationDto = AppComOrgBL.GetAppComOrganizationDtoFlatList(aAppSecurityGroupExDto.AppCreatedByCompanyId.Value).FirstOrDefault(o => (int)o.Id == aAppSecurityGroupExDto.OrganizationId.Value);
                    //    if (organizationDto != null)
                    //    {
                    //        aAppSecurityGroupExDto.OrganizationPath = userTypeName + "/ " + organizationDto.PathName;
                    //    }
                    //}
                }
                else if (aAppSecurityGroupExDto.RoleUserTypeId.Value == (int)EmAppUserType.Customer
                    || aAppSecurityGroupExDto.RoleUserTypeId.Value == (int)EmAppUserType.Supplier
                    || aAppSecurityGroupExDto.RoleUserTypeId.Value == (int)EmAppUserType.ClientAgent
                    || aAppSecurityGroupExDto.RoleUserTypeId.Value == (int)EmAppUserType.SupplierAgent)
                {

                    if (aAppSecurityGroupExDto.BusinessPartnerId.HasValue)
                    {
                        var partnerEntity = AppBusinessPartnerBL.RetrieveOneAppBusinessPartnerEntity(aAppSecurityGroupExDto.BusinessPartnerId.Value);
                        aAppSecurityGroupExDto.OrganizationPath = userTypeName + "/ " + partnerEntity.Code;
                    }
                }
            }


            //if (aAppSecurityGroupExDto.InternalCode == EmAppSecurityGroupInernalCode.CustomerAdmin.ToString()
            //   || aAppSecurityGroupExDto.InternalCode == EmAppSecurityGroupInernalCode.SupplierAdmin.ToString()
            //   || aAppSecurityGroupExDto.InternalCode == EmAppSecurityGroupInernalCode.ClientAgentAdmin.ToString())
            //{
            //    aAppSecurityGroupExDto.IsDisableEdit = true;
            //}

            return aAppSecurityGroupExDto;


        }

        public static bool IsRoleNameUnique(string roleName, int? roleId)
        {
            PredicateExpression predicateExpression = new PredicateExpression(AppSecurityGroupFields.GroupName == roleName);

            if (roleId.HasValue)
            {
                predicateExpression.Add(AppSecurityGroupFields.GroupId != roleId);
            }

            EntityCollection<AppSecurityGroupEntity> roles = new EntityCollection<AppSecurityGroupEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(roles, new RelationPredicateBucket(predicateExpression));
            }


            return roles.Count == 0;
        }

        public static OperationCallResult<AppSecurityGroupExDto> SaveAppSecurityGroupExDto(AppSecurityGroupExDto aAppSecurityGroupExDto)
        {
            OperationCallResult<AppSecurityGroupExDto> aOperationCallResult = new OperationCallResult<AppSecurityGroupExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSecurityGroupEntity aAppSecurityGroupEntity;

            bool isRoleNameUnique = IsRoleNameUnique(aAppSecurityGroupExDto.GroupName, (int?)aAppSecurityGroupExDto.Id);

            if (isRoleNameUnique)
            {
                // prepare Data
                if (aAppSecurityGroupExDto.IsNew)
                {
                    aAppSecurityGroupEntity = new AppSecurityGroupEntity();
                    AppSecurityGroupConverter.CopyDtoToEntity(aAppSecurityGroupEntity, aAppSecurityGroupExDto);
                    foreach (var securityUserDto in aAppSecurityGroupExDto.AppSecurityGroupMemberList)
                    {

                        AppSecurityGroupMemberEntity aAppSecurityGroupMemberEntity = new AppSecurityGroupMemberEntity();
                        AppSecurityGroupMemberConverter.CopyDtoToEntity(aAppSecurityGroupMemberEntity, securityUserDto);
                        aAppSecurityGroupEntity.AppSecurityGroupMember.Add(aAppSecurityGroupMemberEntity);
                    }


                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {

                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.SaveEntity(aAppSecurityGroupEntity);


                            adapter.Commit();


                            aAppSecurityGroupExDto.Id = aAppSecurityGroupEntity.GroupId;
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityGroupExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                        }


                        catch (ORMQueryExecutionException ex)
                        {

                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityGroupExDto), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }
                }

                else if (aAppSecurityGroupExDto.IsRelatedEntitiesModified())
                {
                    aValidationResult.Merge(ProcessDirtyAppSecurityGroupExDto(aAppSecurityGroupExDto));
                }
            }
            else
            {
                aValidationResult.AddItem(null, "plm_PdmSecurityWebUserGroupEntity_RoleNameAlreadyExist", ValidationItemType.Error, "Role Name Already Exit");
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppSecurityGroupExDto(aAppSecurityGroupExDto.Id);
            }

            return aOperationCallResult;


        }

        private static ValidationResult ProcessDirtyAppSecurityGroupExDto(AppSecurityGroupExDto aAppSecurityGroupExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            int[] dirtyGroupMemberIds = aAppSecurityGroupExDto.AppSecurityGroupMemberList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();


            AppSecurityGroupEntity aAppSecurityGroupEntity = RetrieveOneAppSecurityGroupEntity(aAppSecurityGroupExDto.Id);

            Dictionary<int, AppSecurityGroupMemberEntity> dictAppSecurityGroupMemberFromDbms = aAppSecurityGroupEntity.AppSecurityGroupMember.ToDictionary(o => o.RoleMemberId, o => o);



            AppSecurityGroupConverter.CopyDtoToEntity(aAppSecurityGroupEntity, aAppSecurityGroupExDto);


            //------- check  Group  member

            // new Items
            foreach (AppSecurityGroupMemberExDto aChildDto in aAppSecurityGroupExDto.AppSecurityGroupMemberList.FindNewItems())
            {

                AppSecurityGroupMemberEntity aNewChildEntity = new AppSecurityGroupMemberEntity();
                AppSecurityGroupMemberConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppSecurityGroupEntity.AppSecurityGroupMember.Add(aNewChildEntity);


            }


            // Dirty items, only the update item remove from dbms, no need to update that itmes
            foreach (var modifyitem in aAppSecurityGroupExDto.AppSecurityGroupMemberList.FindModifiedItems())
            {
                if (!modifyitem.IsNew)
                {

                    int dtoKey = int.Parse(modifyitem.Id.ToString());
                    if (dictAppSecurityGroupMemberFromDbms.ContainsKey(dtoKey))
                    {

                        AppSecurityGroupMemberConverter.CopyDtoToEntity(dictAppSecurityGroupMemberFromDbms[dtoKey], modifyitem);


                    }

                }
            }



            // deletedIDs      
            int[] deletSecurityGroupMemberIDs = aAppSecurityGroupExDto.AppSecurityGroupMemberList.FindDeletedItemIds().Cast<int>().ToArray();




            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSecurityGroupEntity);

                    //1: need to delete deletdmSecurityPermissionUserGroupMemberIDs

                    if (deletSecurityGroupMemberIDs.Count() > 0)
                    {

                        adapter.DeleteEntitiesDirectly(typeof(AppSecurityGroupMemberEntity), new RelationPredicateBucket(AppSecurityGroupMemberFields.RoleMemberId == deletSecurityGroupMemberIDs));
                    }


                    if (deletSecurityGroupMemberIDs.Count() > 0)
                    {

                        adapter.DeleteEntitiesDirectly(typeof(AppSecurityGroupMemberEntity), new RelationPredicateBucket(AppSecurityGroupMemberFields.RoleMemberId == deletSecurityGroupMemberIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityGroupExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityGroupExDto), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }


            }




            return aValidationResult;
        }

        //public static ObservableSet<AppSecurityGroupDto> RetrieveAppSecurityGroupDtoByOrnizationId(int orgnizationId)
        //{
        //    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //    {
        //        EntityCollection<AppSecurityGroupEntity> list = new EntityCollection<AppSecurityGroupEntity>();
        //        IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityGroupEntity);
        //        rootPath.Add(AppSecurityGroupEntity.PrefetchPathAppSecurityGroupMember);


        //        RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityGroupFields.OrganizationId == orgnizationId);
        //        adapter.FetchEntityCollection(list, filter, rootPath);

        //        List<LookupItemDto> users = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");

        //        var aDtoList = new ObservableSet<AppSecurityGroupDto>();
        //        foreach (var o in list)
        //        {
        //            AppSecurityGroupDto group = AppSecurityGroupConverter.ConvertEntityToDto(o);
        //            aDtoList.Add(group);

        //            List<int> uids = o.AppSecurityGroupMember.Select(gm => gm.UserId).ToList();
        //            group.Users = users.Where(d => uids.Contains((int)d.Id)).ToList();
        //        }


        //        return aDtoList;
        //    }
        //}


        public static List<AppSecurityGroupDto> RetrieveAppSecurityGroupDtoByUsageType(int groupUsage, bool? isExcludeBuiltInReadOnlyRoles = null, int? userType = null)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecurityGroupEntity> list = new EntityCollection<AppSecurityGroupEntity>();
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityGroupEntity);
                rootPath.Add(AppSecurityGroupEntity.PrefetchPathAppSecurityGroupMember);

                RelationPredicateBucket filter = null;

                if (groupUsage == (int)EmAppGroupUsage.MenuGroup)
                {

                    filter = new RelationPredicateBucket(AppSecurityGroupFields.GroupUsage == (int)EmAppGroupUsage.MenuGroup | 
                        (AppSecurityGroupFields.GroupUsage == (int)EmAppGroupUsage.SecurityGroup & AppSecurityGroupFields.IsBuiltIn == true & AppSecurityGroupFields.BusinessPartnerId == System.DBNull.Value));
                }
                else
                {
                    filter = new RelationPredicateBucket(AppSecurityGroupFields.GroupUsage == groupUsage);
                }

                if (isExcludeBuiltInReadOnlyRoles.HasValue && isExcludeBuiltInReadOnlyRoles.Value)
                {
                    filter.PredicateExpression.AddWithAnd(AppSecurityGroupFields.IsBuiltIn == System.DBNull.Value | AppSecurityGroupFields.IsBuiltIn == false
                       | AppSecurityGroupFields.IsSharedbyMutipleCompany == true

                        //| AppSecurityGroupFields.InternalCode == EmAppSecurityGroupInernalCode.CustomerAdmin.ToString()
                        //| AppSecurityGroupFields.InternalCode == EmAppSecurityGroupInernalCode.SupplierAdmin.ToString()
                        //| AppSecurityGroupFields.InternalCode == EmAppSecurityGroupInernalCode.ClientAgentAdmin.ToString()
                        );
                }

                //Dictionary<int, AppComOrganizationDto> dictOrganization = AppComOrgBL.GetAppComOrganizationDtoFlatList().ToDictionary(o => (int)o.Id, o => o);
                Dictionary<int, AppBusinessPartnerDto> dictPartner = AppBusinessPartnerBL.RetrieveCompanyPartnerDtoList(null, null).ToDictionary(o => (int)o.Id, o => o);

                adapter.FetchEntityCollection(list, filter, rootPath);

                List<LookupItemDto> users = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");


                var aDtoList = new List<AppSecurityGroupDto>();
                foreach (var o in list)
                {
                    if (userType.HasValue)
                    {
                        if (o.RoleUserTypeId.HasValue && o.RoleUserTypeId.Value != userType.Value)
                        {
                            if (userType.Value == (int)EmAppUserType.Customer
                                || userType.Value == (int)EmAppUserType.Supplier
                                || userType.Value == (int)EmAppUserType.ClientAgent
                                || userType.Value == (int)EmAppUserType.SupplierAgent
                                || userType.Value == (int)EmAppUserType.Employee)
                            {
                                continue;
                            }
                            else if (userType.Value == (int)EmAppUserType.SaasCompanyAdmin || userType.Value == (int)EmAppUserType.SysAdmin)
                            {
                                if (o.RoleUserTypeId.Value == (int)EmAppUserType.Customer
                                   || o.RoleUserTypeId.Value == (int)EmAppUserType.Supplier
                                   || o.RoleUserTypeId.Value == (int)EmAppUserType.ClientAgent
                                   || o.RoleUserTypeId.Value == (int)EmAppUserType.SupplierAgent)
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    AppSecurityGroupDto group = AppSecurityGroupConverter.ConvertEntityToDto(o);


                    group.Display = group.GroupName;

                    if (group.IsBuiltIn.HasValue && group.IsBuiltIn.Value)
                    {
                        group.Display = group.GroupName + " - Built-In";
                    }

                    aDtoList.Add(group);

                    List<int> uids = o.AppSecurityGroupMember.Select(gm => gm.UserId).ToList();
                    group.Users = users.Where(d => uids.Contains((int)d.Id)).ToList();
                    group.GroupUserString = EntityHelper.ConvertLookupListToString(group.Users);

                    //if (group.InternalCode == EmAppSecurityGroupInernalCode.CustomerAdmin.ToString()
                    // || group.InternalCode == EmAppSecurityGroupInernalCode.SupplierAdmin.ToString()
                    // || group.InternalCode == EmAppSecurityGroupInernalCode.ClientAgentAdmin.ToString())
                    //{
                    //    group.IsDisableEdit = true;
                    //}


                    if (group.RoleUserTypeId.HasValue)
                    {
                        string userTypeName = ((EmAppUserType)(group.RoleUserTypeId.Value)).ToString();

                        group.OrganizationPath = userTypeName;

                        if (group.RoleUserTypeId.Value == (int)EmAppUserType.Employee)
                        {
                            //if (dictOrganization != null && group.OrganizationId.HasValue && dictOrganization.ContainsKey(group.OrganizationId.Value))
                            //{
                            //    group.OrganizationPath = userTypeName + "/ " + dictOrganization[group.OrganizationId.Value].PathName;
                            //}

                            group.OrganizationPath = userTypeName;
                        }
                        else if (group.RoleUserTypeId.Value == (int)EmAppUserType.Customer 
                            || group.RoleUserTypeId.Value == (int)EmAppUserType.Supplier 
                            || group.RoleUserTypeId.Value == (int)EmAppUserType.ClientAgent
                            || group.RoleUserTypeId.Value == (int)EmAppUserType.SupplierAgent)
                        {
                            if (dictPartner != null && group.BusinessPartnerId.HasValue && dictPartner.ContainsKey(group.BusinessPartnerId.Value))
                            {
                                group.OrganizationPath = userTypeName + "/ " + dictPartner[group.BusinessPartnerId.Value].Code;
                            }
                        }
                    }
                }


                return aDtoList;


            }
        }

        //Delete a Role
        public static OperationCallResult<object> DeleteAppSecurityGroup(object securityGroupId)
        {
            OperationCallResult<object> aValidationResult = new OperationCallResult<object>();

            string referMsg = string.Empty;


            var groupEntity = RetrieveOneAppSecurityGroupEntity(securityGroupId);

            if (groupEntity != null)
            {
                if (groupEntity.IsBuiltIn.HasValue && groupEntity.IsBuiltIn.Value)
                {
                    aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityGroupEntity), "App_SecurityGroupEntity_SystemBuiltInGroupCannotBeDeleted", ValidationItemType.Error, "System built-in group cannot be deleted."));
                }
            }
            else
            {
                aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityGroupEntity), "App_SecurityGroupEntity_SystemCannotFindThisGroup", ValidationItemType.Error, "System cannot find this gorup."));
            }

            if (!aValidationResult.ValidationResult.HasErrors)
            {

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {

                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");



                        adapter.DeleteEntitiesDirectly(typeof(AppSecurityTransactionActionResourceEntity), new RelationPredicateBucket(AppSecurityTransactionActionResourceFields.GroupId == securityGroupId));

                        adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.GroupId == securityGroupId));

                        adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserRolePrevilegeEntity), new RelationPredicateBucket(AppSecurityUserRolePrevilegeFields.RoleId == securityGroupId));

                        adapter.DeleteEntitiesDirectly(typeof(AppSecurityGroupMemberEntity), new RelationPredicateBucket(AppSecurityGroupMemberFields.GroupId == securityGroupId));

                        adapter.DeleteEntitiesDirectly(typeof(AppSecurityGroupEntity), new RelationPredicateBucket(AppSecurityGroupFields.GroupId == securityGroupId));
                        adapter.Commit();
                    }

                    catch (ORMQueryExecutionException ex)
                    {

                        adapter.Rollback();
                        aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityGroupEntity), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                    }

                    // if no any errors
                    if (!aValidationResult.ValidationResult.HasErrors)
                    {
                        aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityGroupEntity), "App_SecurityGroupEntity_Delete_Ok", ValidationItemType.Message, "Deleted successfully"));

                        aValidationResult.Object = securityGroupId;
                    }
                }

            }

            return aValidationResult;


        }


        public static List<int> GetUserIdsByGroupIds(List<int> groupIds)
        {
            List<int> userIds = new List<int>();

            if (groupIds != null && groupIds.Count > 0)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppSecurityGroupEntity> list = new EntityCollection<AppSecurityGroupEntity>();
                    IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityGroupEntity);
                    rootPath.Add(AppSecurityGroupEntity.PrefetchPathAppSecurityGroupMember);

                    RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityGroupFields.GroupId == groupIds.ToArray());
                    adapter.FetchEntityCollection(list, filter, rootPath);

                    foreach (var o in list)
                    {
                        List<int> uids = o.AppSecurityGroupMember.Select(gm => gm.UserId).ToList();
                        userIds.AddRange(uids);
                    }

                    userIds = userIds.Distinct().ToList();
                }

                var allUserDtos = AppSecurityUserBL.DictAllUserDto;

                foreach (int builtInGroupId in groupIds.Where(o => o < 10))
                {
                    if (builtInGroupId == 1) // All Users
                    {
                        userIds.AddRange(allUserDtos.Keys.ToList());
                    }
                    else if (builtInGroupId == (int)EmAppUserType.Employee
                        || builtInGroupId == (int)EmAppUserType.Customer
                        || builtInGroupId == (int)EmAppUserType.Supplier
                        || builtInGroupId == (int)EmAppUserType.ClientAgent
                        || builtInGroupId == (int)EmAppUserType.SupplierAgent
                        )
                    {
                        List<int> groupUserIds = allUserDtos.Values.Where(o => o.DomainId == builtInGroupId).Select(o => (int)o.Id).ToList();
                        userIds.AddRange(groupUserIds);
                    }
                }
            }

            return userIds.Distinct().ToList();
        }

    }
}
