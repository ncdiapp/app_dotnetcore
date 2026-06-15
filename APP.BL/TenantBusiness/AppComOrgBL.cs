using System.Collections.Generic;
using System.Linq;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Components.EntityConverter;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL;
using System;
using APP.Framework.Collections;
using System.Data;
////using APP.Persistence.Common;
using APP.Framework.Globalization;

using APP.Framework;
namespace App.BL
{
    public static class AppComOrgBL
    {



        public static List<AppComOrganizationDto> GetCompanyOrgHairarchy(int companyId)
        {
            List<AppComOrganizationDto> dtoList = new List<AppComOrganizationDto>();
            EntityCollection<AppComOrganizationEntity> entityList = new EntityCollection<AppComOrganizationEntity>();

            IRelationPredicateBucket filter = new RelationPredicateBucket(AppComOrganizationFields.AppCompanyId == companyId);

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                adpater.FetchEntityCollection(entityList, filter);
            }

            foreach (var entity in entityList)
            {
                dtoList.Add(AppComOrganizationConverter.ConvertEntityToExDto(entity));
            }

            Dictionary<int, string> dictLevelIdAndShortName = RetrieveCompanyOrgLevelDtoList(companyId).Where(o => o.ClassificationLevel.HasValue).ToDictionary(o => o.ClassificationLevel.Value, o => o.LevelName);

            dictLevelIdAndShortName[0] = "";
            List<AppComOrganizationDto> toReturn = SetupComOrganization(dtoList, dictLevelIdAndShortName);


            return toReturn;
        }

        public static List<AppComOrganizationDto> GetCompanyOrganizationDtoFlatList(int companyId)
        {
            List<AppComOrganizationDto> hairachyList = GetCompanyOrgHairarchy(companyId);
            List<AppComOrganizationDto> flatList = new List<AppComOrganizationDto>();

            foreach (AppComOrganizationDto dto in hairachyList)
            {
                dto.PathName = "Company";
                flatList.Add(dto);
                ProcessChildFlatList(dto, flatList);
            }

            return flatList;
        }

        private static void ProcessChildFlatList(AppComOrganizationDto dto, List<AppComOrganizationDto> flatList)
        {
            if (!dto.Children.IsEmpty())
            {
                foreach (AppComOrganizationDto childDto in dto.Children)
                {
                    flatList.Add(childDto);
                    ProcessChildFlatList(childDto, flatList);
                }
            }

            dto.Children = null;
        }

        public static List<AppComOrgLevelDto> RetrieveCompanyOrgLevelDtoList(int? companyId)
        {
            var folderEntities = new EntityCollection<AppComOrgLevelEntity>();

            using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
            {
                IRelationPredicateBucket filter = null;
                if (companyId.HasValue)
                {
                    filter = new RelationPredicateBucket(AppComOrgLevelFields.AppCompanyId == companyId);
                }


                adapater.FetchEntityCollection(folderEntities, filter);
            }

            var aDtoList = new List<AppComOrgLevelDto>();
            foreach (var folderEntity in folderEntities)
            {
                aDtoList.Add(AppComOrgLevelConverter.ConvertEntityToDto(folderEntity));
            }

            return aDtoList;
        }


        private static AppComOrganizationExDto RetrieveOneAppComOrganizationExDto(object organizationId)
        {
            AppComOrganizationEntity folderEntity = RetrieveOneAppComOrganizationEntity(organizationId);
            AppComOrganizationExDto aAppComOrganizationExDto = AppComOrganizationConverter.ConvertEntityToExDto(folderEntity);

            return aAppComOrganizationExDto;
        }

        //TODO, need to add companID
        //internal static List<AppComOrganizationDto> GetAppComOrganizationDtoFlatList(int? companyId = null)
        //{
        //    List<AppComOrganizationDto> dtoList = new List<AppComOrganizationDto>();
        //    EntityCollection<AppComOrganizationEntity> entityList = new EntityCollection<AppComOrganizationEntity>();

        //    using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
        //    {
        //        IRelationPredicateBucket filter = null;
        //        if (companyId.HasValue)
        //        {
        //            filter = new RelationPredicateBucket(AppComOrganizationFields.AppCompanyId == companyId);
        //        }

        //        adpater.FetchEntityCollection(entityList, filter);
        //    }

        //    foreach (var entity in entityList)
        //    {
        //        dtoList.Add(AppComOrganizationConverter.ConvertEntityToExDto(entity));
        //    }

        //    //Dictionary<int, string> dictLevelIdAndShortName = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppComOrgLevel.ToString(), string.Empty).ToDictionary(o => (int)o.Id, o => o.Display);

        //    Dictionary<int, string> dictLevelIdAndShortName = RetrieveCompanyOrgLevelDtoList(companyId).Where(o => o.ClassificationLevel.HasValue).ToDictionary(o => o.ClassificationLevel.Value, o => o.LevelName);


        //    dictLevelIdAndShortName[0] = "";
        //    List<AppComOrganizationDto> toReturn = SetupComOrganization(dtoList, dictLevelIdAndShortName);

        //    //  toReturn.AddRange(GetNoneEmployeeUserType(dtoList, dictLevelIdAndShortName));

        //    return dtoList;
        //}

        private static List<AppComOrganizationDto> SetupComOrganization(List<AppComOrganizationDto> dtoList, Dictionary<int, string> dictLevelIdAndShortName)
        {
            List<AppComOrganizationDto> toReturn = new List<AppComOrganizationDto>();

            var rootCom = dtoList.Where(o => o.UserTypeEm == (int)EmAppUserType.Employee && o.ClassificationLevel == 1).FirstOrDefault();
            if (rootCom != null)
            {
                string currentNodePathDescription = rootCom.FullName;

                if (rootCom.ClassificationLevel.HasValue && dictLevelIdAndShortName.ContainsKey(rootCom.ClassificationLevel.Value))
                {
                    currentNodePathDescription = dictLevelIdAndShortName[rootCom.ClassificationLevel.Value] + ": " + currentNodePathDescription;
                }


                rootCom.PathName = string.Empty; // rootCom.Code;
                rootCom.PathDescription = currentNodePathDescription;

                ProcessChilds(dtoList, rootCom, dictLevelIdAndShortName);

                if (rootCom.PathName.StartsWith("/ "))
                {
                    rootCom.PathName = rootCom.PathName.Substring(2);
                }
                // only return Root Node (UI need to expend two level)


                toReturn.Add(rootCom);
            }
            return toReturn;
        }


        //private static List<AppComOrganizationDto> GetNoneEmployeeUserType(List<AppComOrganizationDto> dtoList, Dictionary<int, string> dictLevelIdAndShortName)
        //{
        //    var rootExternallList = dtoList.Where(o => o.UserTypeEm != (int)EmAppUserType.Employee && (!o.BelongToId.HasValue)).ToList();

        //    foreach (var rootCom in rootExternallList)
        //    {
        //        rootCom.PathName = string.Empty; // rootCom.Code;

        //        ProcessChilds(dtoList, rootCom, dictLevelIdAndShortName);

        //        if (rootCom.PathName.StartsWith("/ ")) {
        //            rootCom.PathName = rootCom.PathName.Substring(2);
        //        }
        //    }



        //    List<AppComOrganizationDto> toReturn = new List<AppComOrganizationDto>();

        //    toReturn.AddRange(rootExternallList);
        //    return toReturn;
        //}

        private static List<AppComOrganizationDto> GetChildLevelOrgHairarchy(int parentId)
        {
            List<AppComOrganizationDto> dtoList = new List<AppComOrganizationDto>();
            EntityCollection<AppComOrganizationEntity> entityList = GetChildLevelEntityList(parentId);

            foreach (var entity in entityList)
            {
                dtoList.Add(AppComOrganizationConverter.ConvertEntityToDto(entity));
            }



            return dtoList;
        }

        private static EntityCollection<AppComOrganizationEntity> GetChildLevelEntityList(int parentId)
        {
            EntityCollection<AppComOrganizationEntity> entityList = new EntityCollection<AppComOrganizationEntity>();

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                adpater.FetchEntityCollection(entityList, new RelationPredicateBucket(AppComOrganizationFields.BelongToId == parentId));
            }

            return entityList;
        }



        private static string MasterConnStr => AppCompanyBL.AppMasterDBConnectionString;

        public static List<AppSecurityUserDto> GetOrganizationUserDtoList(bool isIncludeBusinessParternerUsers)
        {
            var aDtoList = new List<AppSecurityUserDto>();

            EntityCollection<AppSecurityUserEntity> list = GetOrgainizationUserEntitysWithGroupInfo(isIncludeBusinessParternerUsers);
            ConvertUserEntityListToDtoList(aDtoList, list);

            return aDtoList;
        }

        /// <summary>
        /// Users for the session working company: master DB (AppCreatedByCompanyId) plus tenant security group membership.
        /// </summary>
        public static List<AppSecurityUserDto> GetCurrentTenantUserDtoList(bool isIncludeBusinessParternerUsers)
        {
            int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
            if (!companyId.HasValue)
            {
                return new List<AppSecurityUserDto>();
            }

            EntityCollection<AppSecurityUserEntity> masterUsers =
                GetMasterOrganizationUserEntitiesForCompany(companyId.Value, isIncludeBusinessParternerUsers);
            if (masterUsers.Count == 0)
            {
                return new List<AppSecurityUserDto>();
            }

            var aDtoList = new List<AppSecurityUserDto>();
            foreach (AppSecurityUserEntity masterEntity in masterUsers)
            {
                AppendMasterOrganizationUserDto(aDtoList, masterEntity);
            }

            List<int> userIds = masterUsers.Select(u => u.UserId).Distinct().ToList();
            Dictionary<int, List<int>> userIdToGroupIds = GetTenantGroupIdsByUserIds(userIds);
            ApplyTenantGroupMembershipToUserDtos(aDtoList, userIdToGroupIds);

            return aDtoList;
        }

        public static List<AppSecurityUserDto> GetPartnerTypeUserDtoList(int partnerType)
        {
            int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
            if (!companyId.HasValue)
            {
                return new List<AppSecurityUserDto>();
            }

            List<AppBusinessPartnerDto> partners =
                AppBusinessPartnerBL.RetrieveCompanyPartnerDtoList(companyId.Value, partnerType);
            List<int> partnerIds = partners.Select(p => (int)p.Id).ToList();
            List<int> userIds = GetInviteUserIdsByBusinessPartnerIds(partnerIds);
            return BuildUserDtoListFromMasterUserIds(userIds);
        }

        internal static List<int> GetInviteUserIdsByBusinessPartnerIds(List<int> businessPartnerIds)
        {
            var userIdSet = new HashSet<int>();
            if (businessPartnerIds == null || businessPartnerIds.Count == 0)
            {
                return new List<int>();
            }

            RelationPredicateBucket filter = new RelationPredicateBucket(
                AppBusinessPartnerInviteUserFields.AppBusinessPartnerId == businessPartnerIds.ToArray());

            EntityCollection<AppBusinessPartnerInviteUserEntity> masterEntities =
                new EntityCollection<AppBusinessPartnerInviteUserEntity>();
            using (DataAccessAdapter masterAdapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                masterAdapter.FetchEntityCollection(masterEntities, filter);
            }
            AddInviteUserIdsFromEntities(userIdSet, masterEntities);

            return userIdSet.ToList();
        }

        private static void AddInviteUserIdsFromEntities(
            HashSet<int> userIdSet, EntityCollection<AppBusinessPartnerInviteUserEntity> entities)
        {
            foreach (AppBusinessPartnerInviteUserEntity entity in entities)
            {
                if (entity.UserId.HasValue && entity.UserId.Value > 0)
                {
                    userIdSet.Add(entity.UserId.Value);
                }
            }
        }

        internal static List<AppSecurityUserDto> BuildUserDtoListFromMasterUserIds(List<int> userIds)
        {
            var aDtoList = new List<AppSecurityUserDto>();
            if (userIds == null || userIds.Count == 0)
            {
                return aDtoList;
            }

            List<AppSecurityUserDto> masterDtos = AppSecurityUserBL.RetrieveAllMasterDBUsersByUserId(userIds);
            if (masterDtos == null || masterDtos.Count == 0)
            {
                return aDtoList;
            }

            List<LookupItemDto> domainLookupItems = AppEntityInfoBL.GetLookupItemListByCode(
                EmAppEntityLookupInfoCode.AppSecurityRegDomain.ToString(), string.Empty);

            foreach (AppSecurityUserDto aUserDto in masterDtos)
            {
                var domainItem = domainLookupItems.FirstOrDefault(p => (int)p.Id == aUserDto.DomainId);
                if (domainItem != null)
                {
                    aUserDto.DomainDispaly = domainItem.Display;
                }

                aDtoList.Add(aUserDto);
            }

            ApplyTenantGroupMembershipToUserDtos(aDtoList, GetTenantGroupIdsByUserIds(userIds));
            return aDtoList;
        }



        internal static void ConvertUserEntityListToDtoList(List<AppSecurityUserDto> aDtoList, EntityCollection<AppSecurityUserEntity> userEntityList)
        {
            if (userEntityList.Count > 0)
            {
                Dictionary<int, AppSecurityUserDto> dictUserIdAndMasterDBUserDto =
                    AppSecurityUserBL.RetrieveAllMasterDBUsersByUserId(userEntityList.Select(o => o.UserId).ToList()).ToDictionary(o => (int)o.Id, o => o);


                //List<LookupItemDto> groupLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityGroup.ToString(), "");
                List<LookupItemDto> DomainLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityRegDomain.ToString(), "");

                List<LookupItemDto> groupLookupItems = new List<LookupItemDto>();
                AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType((int)EmAppGroupUsage.SecurityGroup).ForAll(aGroupDto =>
                {
                    LookupItemDto lookupDto = new LookupItemDto();
                    lookupDto.Id = aGroupDto.Id;
                    lookupDto.Display = aGroupDto.GroupName;
                    groupLookupItems.Add(lookupDto);
                });

                List<LookupItemDto> projectTeamLookupItems = new List<LookupItemDto>();
                AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType((int)EmAppGroupUsage.ProjectTeam).ForAll(aGroupDto =>
                {
                    LookupItemDto lookupDto = new LookupItemDto();
                    lookupDto.Id = aGroupDto.Id;
                    lookupDto.Display = aGroupDto.GroupName;
                    projectTeamLookupItems.Add(lookupDto);
                });

                foreach (var o in userEntityList)
                {
                    if (dictUserIdAndMasterDBUserDto.ContainsKey(o.UserId))
                    {
                        AppSecurityUserDto aUserDto = dictUserIdAndMasterDBUserDto[o.UserId];
                        aDtoList.Add(aUserDto);

                        var aDomainLookUpItem = DomainLookupItems.FirstOrDefault(p => (int)p.Id == aUserDto.DomainId);
                        if (aDomainLookUpItem != null)
                        {
                            aUserDto.DomainDispaly = aDomainLookUpItem.Display;
                        }

                        List<int> groupids = o.AppSecurityGroupMember.Select(g => g.GroupId).ToList();
                        aUserDto.UserGroups = groupLookupItems.Where(dg => groupids.Contains((int)dg.Id)).ToList();
                        aUserDto.UserGroupString = EntityHelper.ConvertLookupListToString(aUserDto.UserGroups);

                        aUserDto.UserProjectTeams = projectTeamLookupItems.Where(dg => groupids.Contains((int)dg.Id)).ToList();
                        aUserDto.UserProjectTeamString = EntityHelper.ConvertLookupListToString(aUserDto.UserProjectTeams);
                    }
                }
            }
        }

        private static EntityCollection<AppSecurityUserEntity> GetOrgainizationUserEntityList(int organizationId)
        {
            EntityCollection<AppSecurityUserEntity> entityList = new EntityCollection<AppSecurityUserEntity>();

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                adpater.FetchEntityCollection(entityList, new RelationPredicateBucket(AppSecurityUserFields.OrganizationId == organizationId));
            }

            return entityList;
        }

        private static EntityCollection<AppSecurityUserEntity> GetOrgainizationUserEntitysWithGroupInfo(bool isIncludeBusinessParternerUsers)
        {
            EntityCollection<AppSecurityUserEntity> list = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityUserEntity);


                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserFields.LoginName != AppSecurityUserBL.AppSuperuser 
                    & (AppSecurityUserFields.DomainId == (int)EmAppUserType.SysAdmin | AppSecurityUserFields.DomainId == (int)EmAppUserType.Employee | AppSecurityUserFields.DomainId == (int)EmAppUserType.SaasCompanyAdmin));

                if (isIncludeBusinessParternerUsers)
                {
                    filter = new RelationPredicateBucket(AppSecurityUserFields.LoginName != AppSecurityUserBL.AppSuperuser);
                }
                

                rootPath.Add(AppSecurityUserEntity.PrefetchPathAppSecurityGroupMember);
                adapter.FetchEntityCollection(list, filter, rootPath);
            }

            return list;
        }

        private static EntityCollection<AppSecurityUserEntity> GetMasterOrganizationUserEntitiesForCompany(
            int companyId, bool isIncludeBusinessParternerUsers)
        {
            EntityCollection<AppSecurityUserEntity> list = new EntityCollection<AppSecurityUserEntity>();
            RelationPredicateBucket filter = new RelationPredicateBucket(
                AppSecurityUserFields.AppCreatedByCompanyId == companyId
                & AppSecurityUserFields.LoginName != AppSecurityUserBL.AppSuperuser);

            if (!isIncludeBusinessParternerUsers)
            {
                filter.PredicateExpression.AddWithAnd(
                    AppSecurityUserFields.DomainId == (int)EmAppUserType.SysAdmin
                    | AppSecurityUserFields.DomainId == (int)EmAppUserType.Employee
                    | AppSecurityUserFields.DomainId == (int)EmAppUserType.SaasCompanyAdmin);
            }

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                adapter.FetchEntityCollection(list, filter);
            }

            return list;
        }

        private static Dictionary<int, List<int>> GetTenantGroupIdsByUserIds(List<int> userIds)
        {
            var userIdToGroupIds = new Dictionary<int, List<int>>();
            if (userIds == null || userIds.Count == 0)
            {
                return userIdToGroupIds;
            }

            EntityCollection<AppSecurityGroupMemberEntity> members = new EntityCollection<AppSecurityGroupMemberEntity>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(
                    AppSecurityGroupMemberFields.UserId == userIds.ToArray());
                adapter.FetchEntityCollection(members, filter);
            }

            foreach (AppSecurityGroupMemberEntity member in members)
            {
                if (!userIdToGroupIds.ContainsKey(member.UserId))
                {
                    userIdToGroupIds[member.UserId] = new List<int>();
                }

                userIdToGroupIds[member.UserId].Add(member.GroupId);
            }

            return userIdToGroupIds;
        }

        private static void ApplyTenantGroupMembershipToUserDtos(
            List<AppSecurityUserDto> aDtoList, Dictionary<int, List<int>> userIdToGroupIds)
        {
            if (aDtoList == null || aDtoList.Count == 0 || userIdToGroupIds == null || userIdToGroupIds.Count == 0)
            {
                return;
            }

            List<LookupItemDto> groupLookupItems = new List<LookupItemDto>();
            AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType((int)EmAppGroupUsage.SecurityGroup).ForAll(aGroupDto =>
            {
                LookupItemDto lookupDto = new LookupItemDto();
                lookupDto.Id = aGroupDto.Id;
                lookupDto.Display = aGroupDto.GroupName;
                groupLookupItems.Add(lookupDto);
            });

            List<LookupItemDto> projectTeamLookupItems = new List<LookupItemDto>();
            AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType((int)EmAppGroupUsage.ProjectTeam).ForAll(aGroupDto =>
            {
                LookupItemDto lookupDto = new LookupItemDto();
                lookupDto.Id = aGroupDto.Id;
                lookupDto.Display = aGroupDto.GroupName;
                projectTeamLookupItems.Add(lookupDto);
            });

            foreach (AppSecurityUserDto aUserDto in aDtoList)
            {
                if (!userIdToGroupIds.TryGetValue((int)aUserDto.Id, out List<int> groupIds) || groupIds.Count == 0)
                {
                    continue;
                }

                aUserDto.UserGroups = groupLookupItems.Where(dg => groupIds.Contains((int)dg.Id)).ToList();
                aUserDto.UserGroupString = EntityHelper.ConvertLookupListToString(aUserDto.UserGroups);
                aUserDto.UserProjectTeams = projectTeamLookupItems.Where(dg => groupIds.Contains((int)dg.Id)).ToList();
                aUserDto.UserProjectTeamString = EntityHelper.ConvertLookupListToString(aUserDto.UserProjectTeams);
            }
        }

        private static void AppendMasterOrganizationUserDto(List<AppSecurityUserDto> aDtoList, AppSecurityUserEntity masterEntity)
        {
            AppSecurityUserDto aUserDto = AppSecurityUserConverter.ConvertEntityToDto(masterEntity);
            aDtoList.Add(aUserDto);

            List<LookupItemDto> domainLookupItems = AppEntityInfoBL.GetLookupItemListByCode(
                EmAppEntityLookupInfoCode.AppSecurityRegDomain.ToString(), string.Empty);
            var domainItem = domainLookupItems.FirstOrDefault(p => (int)p.Id == aUserDto.DomainId);
            if (domainItem != null)
            {
                aUserDto.DomainDispaly = domainItem.Display;
            }
        }

        public static List<AppSecurityGroupDto> GetOrganizationGroupList()
        {
            List<AppSecurityGroupDto> allGroupDtos = AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType(1).ToList();

            List<AppSecurityGroupDto> orgGroups = new List<AppSecurityGroupDto>();

            orgGroups = allGroupDtos.Where(o => !o.BusinessPartnerId.HasValue).ToList();

            return orgGroups;
        }

        public static List<AppSecurityGroupDto> GetPartnerTypeGroupList(int partnerType)
        {
            List<AppSecurityGroupDto> allGroupDtos = AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType(1).ToList();

            List<AppSecurityGroupDto> orgGroups = new List<AppSecurityGroupDto>();

            orgGroups = allGroupDtos.Where(o => o.RoleUserTypeId.HasValue && o.RoleUserTypeId.Value == partnerType).ToList();

            return orgGroups;
        }

        //private static List<int> GetHirachyOrganizationIdList(List<int> hirachyOrgIdList, Dictionary<int, AppComOrganizationDto> dictOrganization, int orgId)
        //{
        //    if (hirachyOrgIdList == null)
        //    {
        //        hirachyOrgIdList = new List<int>();
        //    }

        //    if (dictOrganization == null)
        //    {
        //        dictOrganization = AppComOrgBL.GetAppComOrganizationDtoFlatList().ToDictionary(o => (int)o.Id, o => o);
        //    }

        //    if (dictOrganization.ContainsKey(orgId) && dictOrganization[orgId] != null)
        //    {
        //        var orgDto = dictOrganization[orgId];

        //        if (orgDto.BelongToId.HasValue)
        //        {
        //            GetHirachyOrganizationIdList(hirachyOrgIdList, dictOrganization, orgDto.BelongToId.Value);
        //        }

        //        hirachyOrgIdList.Add(orgId);
        //    }

        //    return hirachyOrgIdList;
        //}


        private static EntityCollection<AppSecurityGroupEntity> GetOrganizationUserGroupEntity(int organizationId)
        {
            EntityCollection<AppSecurityGroupEntity> entityList = new EntityCollection<AppSecurityGroupEntity>();

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                adpater.FetchEntityCollection(entityList, new RelationPredicateBucket(AppSecurityGroupFields.OrganizationId == organizationId));
            }

            return entityList;
        }

        public static AppComOrganizationEntity RetrieveOneAppComOrganizationEntity(object organizationId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppComOrganizationEntity folderEntity = new AppComOrganizationEntity(int.Parse(organizationId.ToString()));
                adpater.FetchEntity(folderEntity);
                return folderEntity;
            }
        }

        private static void ProcessChilds(IEnumerable<AppComOrganizationDto> allfoldersTreeItems, AppComOrganizationDto parentDto, Dictionary<int, string> dictLevelIdAndShortName)
        {
            AppComOrganizationDto[] children = GetChilds(allfoldersTreeItems, parentDto).OrderBy(f => f.ShortName).ToArray();

            if (!children.IsEmpty())
            {
                parentDto.Children = children;
                parentDto.Children.ForAll(c =>
                {
                    string currentNodePathDescriptoin = c.FullName;

                    if (c.ClassificationLevel.HasValue && dictLevelIdAndShortName.ContainsKey(c.ClassificationLevel.Value))
                    {
                        currentNodePathDescriptoin = dictLevelIdAndShortName[c.ClassificationLevel.Value] + ": " + currentNodePathDescriptoin;
                    }

                    c.PathName = parentDto.PathName + "/ " + c.Code;


                    if (c.PathName.StartsWith("/ "))
                    {
                        c.PathName = c.PathName.Substring(2);
                    }

                    c.PathDescription = parentDto.PathDescription + " / " + currentNodePathDescriptoin;

                    ProcessChilds(allfoldersTreeItems, c, dictLevelIdAndShortName);

                });

            }
        }

        private static AppComOrganizationDto[] GetChilds(IEnumerable<AppComOrganizationDto> allfoldersTreeItems, AppComOrganizationDto folderTreeItemDto)
        {
            return allfoldersTreeItems.Where(f => f.BelongToId == (int)folderTreeItemDto.Id).ToArray();
        }





        public static OperationCallResult<AppComOrganizationExDto> SaveAppComOrganization(AppComOrganizationExDto aAppComOrganizationExDto)
        {
            OperationCallResult<AppComOrganizationExDto> aOperationCallResult = new OperationCallResult<AppComOrganizationExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            // Save New
            if (aAppComOrganizationExDto.IsNew)
            {
                aValidationResult.Merge(SaveAppComOrganizationExDto_ProcessNewDto(aAppComOrganizationExDto));
            }

            // Save Dirty
            else if (aAppComOrganizationExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(SaveAppComOrganizationExDto_ProcessDirtyAppComOrganizationExDto(aAppComOrganizationExDto));
            }

            // if no any errors, refresh from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppComOrganizationExDto(aAppComOrganizationExDto.Id);
                aOperationCallResult.Object.PathName = aAppComOrganizationExDto.PathName;
                aOperationCallResult.Object.PathDescription = aAppComOrganizationExDto.PathDescription;
                aOperationCallResult.Object.Children = GetChildLevelOrgHairarchy((int)aAppComOrganizationExDto.Id).ToArray();

            }

            return aOperationCallResult;
        }

        private static ValidationResult SaveAppComOrganizationExDto_ProcessNewDto(AppComOrganizationExDto aAppComOrganizationExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();
            AppComOrganizationEntity aAppComOrganizationEntity = new AppComOrganizationEntity();
            AppComOrganizationConverter.CopyDtoToEntity(aAppComOrganizationEntity, aAppComOrganizationExDto);

            List<AppComOrganizationEntity> appComOrganizationEntityList = new List<AppComOrganizationEntity>();

            if (!aAppComOrganizationExDto.Children.IsEmpty())
            {
                foreach (var childDto in aAppComOrganizationExDto.Children)
                {
                    AppComOrganizationEntity achildComOrganizationEntity = new AppComOrganizationEntity();
                    AppComOrganizationConverter.CopyDtoToEntity(achildComOrganizationEntity, aAppComOrganizationExDto);
                    appComOrganizationEntityList.Add(achildComOrganizationEntity);
                }



            }


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppComOrganizationEntity);

                    int organizationId = aAppComOrganizationEntity.OrganizationId;

                    foreach (var childEntity in appComOrganizationEntityList)
                    {
                        childEntity.BelongToId = organizationId;

                        adapter.SaveEntity(childEntity);
                    }



                    adapter.Commit();

                    aAppComOrganizationExDto.Id = aAppComOrganizationEntity.OrganizationId;
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppComOrganizationEntity), "App_SefolderEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                // Database FK Exeption ........
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppComOrganizationEntity), "App_SefolderEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult SaveAppComOrganizationExDto_ProcessDirtyAppComOrganizationExDto(AppComOrganizationExDto aAppComOrganizationExDto)
        {
            int organizationId = int.Parse(aAppComOrganizationExDto.Id.ToString());

            ValidationResult aValidationResult = new ValidationResult();

            AppComOrganizationEntity aAppComOrganizationEntity = RetrieveOneAppComOrganizationEntity(organizationId);
            AppComOrganizationConverter.CopyDtoToEntity(aAppComOrganizationEntity, aAppComOrganizationExDto);



            List<AppComOrganizationEntity> appOrganizationNewEntityList = new List<AppComOrganizationEntity>();

            List<AppComOrganizationEntity> appOrganizationUdpateEntityList = new List<AppComOrganizationEntity>();



            if (!aAppComOrganizationExDto.Children.IsEmpty())
            {
                foreach (var childDto in aAppComOrganizationExDto.Children)
                {
                    AppComOrganizationEntity achildComOrganizationEntity = new AppComOrganizationEntity();

                    AppComOrganizationConverter.CopyDtoToEntity(achildComOrganizationEntity, childDto);

                    if (childDto.IsNew)
                    {
                        achildComOrganizationEntity.AppSecurityGroup.Add(new AppSecurityGroupEntity()
                        {
                            GroupName = achildComOrganizationEntity.Code,
                            Description = achildComOrganizationEntity.ShortName,
                            IsBuiltIn = true,
                            IsSharedbyMutipleCompany = false,
                            GroupUsage = (int)EmAppGroupUsage.SecurityGroup,
                            RoleUserTypeId = (int)EmAppUserType.Employee,
                        });

                        appOrganizationNewEntityList.Add(achildComOrganizationEntity);
                    }
                    else
                    {
                        achildComOrganizationEntity.OrganizationId = (int)childDto.Id;
                        appOrganizationUdpateEntityList.Add(achildComOrganizationEntity);

                    }

                }



            }



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppComOrganizationEntity);

                    foreach (var childEntity in appOrganizationNewEntityList)
                    {
                        childEntity.BelongToId = organizationId;

                        adapter.SaveEntity(childEntity);
                    }

                    foreach (var childEntity in appOrganizationUdpateEntityList)
                    {

                        adapter.UpdateEntitiesDirectly(childEntity,
                            new RelationPredicateBucket(AppComOrganizationFields.OrganizationId == childEntity.OrganizationId));

                    }



                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppComOrganizationEntity), "App_SefolderEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppComOrganizationEntity), "App_SefolderEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Concurrency validation ("Concurrency violation, data not updated")
                catch (ORMConcurrencyException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppComOrganizationEntity), "App_SefolderEntity_Concurrency_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppComOrganizationEntity), "App_SefolderEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }


        public static OperationCallResult<object> DeleteAppComOrganization(object organizationId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            string validationmseeage = "";
            var ChildNodeList = GetChildLevelEntityList(int.Parse(organizationId.ToString()));
            if (ChildNodeList.Count > 0)
            {
                validationmseeage = validationmseeage + " has sub level organization  ";
            }

            var userGroupNodeList = GetOrganizationUserGroupEntity(int.Parse(organizationId.ToString()));
            var userNodeList = GetOrgainizationUserEntityList(int.Parse(organizationId.ToString()));

            if (userGroupNodeList.Where(o => !(o.IsBuiltIn.HasValue && o.IsBuiltIn.Value)).ToList().Count > 0)
            {
                validationmseeage = validationmseeage + " has user group list.";
            }
            else if (userNodeList.Count > 0)
            {
                validationmseeage = validationmseeage + " has user  list.";
            }


            if (!string.IsNullOrWhiteSpace(validationmseeage))
            {

                validationmseeage = "Delete failed " + System.Environment.NewLine + validationmseeage;
                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppComOrganizationEntity), "App_orgValidation_Error", ValidationItemType.Error, validationmseeage.ToString()));

                return aOperationCallResult;


            }




            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {

                    EntityCollection<AppSecurityGroupEntity> builtInGroupEntityList = new EntityCollection<AppSecurityGroupEntity>();
                    IRelationPredicateBucket groupFilter = new RelationPredicateBucket(AppSecurityGroupFields.OrganizationId == organizationId & AppSecurityGroupFields.IsBuiltIn == true);
                    adapter.FetchEntityCollection(builtInGroupEntityList, groupFilter);

                    List<int> builtInSecurityGroupId = builtInGroupEntityList.Select(o => o.GroupId).ToList();

                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityTransactionActionResourceEntity), new RelationPredicateBucket(AppSecurityTransactionActionResourceFields.GroupId == builtInSecurityGroupId));

                    adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.GroupId == builtInSecurityGroupId));

                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserRolePrevilegeEntity), new RelationPredicateBucket(AppSecurityUserRolePrevilegeFields.RoleId == builtInSecurityGroupId));

                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityGroupMemberEntity), new RelationPredicateBucket(AppSecurityGroupMemberFields.GroupId == builtInSecurityGroupId));

                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityGroupEntity), new RelationPredicateBucket(AppSecurityGroupFields.GroupId == builtInSecurityGroupId));

                    adapter.DeleteEntitiesDirectly(
                        typeof(AppComOrganizationEntity),
                        new RelationPredicateBucket(AppComOrganizationFields.OrganizationId == organizationId));

                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppComOrganizationEntity), "App_SefolderEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }

                // if no any errors
                if (!aOperationCallResult.ValidationResult.HasErrors)
                {
                    aOperationCallResult.Object = organizationId;
                }
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<bool> SaveAppComOrganizationSet(ObservableSet<AppComOrganizationExDto> aSet)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;



            aSet.FindNewItems().ForAll(o => validationResult.Merge(SaveAppComOrganizationExDto_ProcessNewDto(o)));

            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(SaveAppComOrganizationExDto_ProcessDirtyAppComOrganizationExDto(o)));

            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(DeleteAppComOrganization(Id).ValidationResult));



            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;


        }

        public static OperationCallResult<bool> TransferOrganizationUsers(int? targetOrganizationId, int? targetDomainId, List<int> needToTransferUserIdList)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (needToTransferUserIdList != null && needToTransferUserIdList.Count > 0 && targetOrganizationId.HasValue && targetDomainId.HasValue)
            {
                AppSecurityUserEntity userEntity = new AppSecurityUserEntity();
                userEntity.OrganizationId = targetOrganizationId.Value;
                userEntity.DomainId = targetDomainId.Value;

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                        RelationPredicateBucket filter = new RelationPredicateBucket();
                        filter.PredicateExpression.Add(AppSecurityUserFields.UserId == needToTransferUserIdList.ToArray());


                        adapter.UpdateEntitiesDirectly(userEntity, filter);

                        string message = StringLocalizer.Localize("User_Transfer_OK", "Users Have Been Transfered Successfully ");
                        aOperationCallResult.ValidationResult.AddItem(null, "User_Transfer_OK", ValidationItemType.Message, message);
                        adapter.Commit();
                    }
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        string message = StringLocalizer.Localize("User_Transfer_Failed", " User Transfer Failed" + ex.ToString());
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(null, "User_Transfer_Failed", ValidationItemType.Error, message));
                    }
                }
                if (!aOperationCallResult.ValidationResult.HasErrors)
                {
                    aOperationCallResult.Object = true;
                }
            }

            return aOperationCallResult;

        }
    }
}