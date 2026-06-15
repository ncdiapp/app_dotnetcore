using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

using APP.Framework;
namespace App.BL
{
    public static class AppProjectTeamLibBL
    {
        public static List<AppProjectTeamDto> RetrieveAllAppProjectTeamDto()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppProjectTeamEntity> list = new EntityCollection<AppProjectTeamEntity>();
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectTeamEntity);
                rootPath.Add(AppProjectTeamEntity.PrefetchPathAppProjectTeamMember);

                // need to add Permission entity code the the entity management


                adapter.FetchEntityCollection(list, null, rootPath);

                List<LookupItemDto> users = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");

                var aDtoList = new List<AppProjectTeamDto>();
                foreach (var o in list)
                {
                    AppProjectTeamDto group = AppProjectTeamConverter.ConvertEntityToDto(o);
                    aDtoList.Add(group);

                    //List<int> uids = o.AppProjectTeamMember.Where(o=>o.UserId.HasValue).Select(o => o.UserId.Value).ToList();                  

                    //group.Users = users.Where(d => uids.Contains((int)d.Id)).ToList();
                    //group.GroupUserString = EntityHelper.ConvertLookupListToString(group.Users);                   
                }

                return aDtoList;
            }
        }

        public static AppProjectTeamExDto RetrieveProjectTeamExDto(int? teamId)
        {
            AppProjectTeamEntity aAppProjectTeamEntity = RetrieveOneAppProjectTeamEntity(teamId);

            AppProjectTeamExDto aAppProjectTeamExDto = AppProjectTeamConverter.ConvertEntityToExDto(aAppProjectTeamEntity);

            foreach (AppProjectTeamMemberEntity appProjectTeamMemberEntity in aAppProjectTeamEntity.AppProjectTeamMember)
            {
                AppProjectTeamMemberExDto aAppProjectTeamMemberExDto = AppProjectTeamMemberConverter.ConvertEntityToExDto(appProjectTeamMemberEntity);
                aAppProjectTeamExDto.AppProjectTeamMemberList.Add(aAppProjectTeamMemberExDto);

                foreach (var aAppProjectTeamMemberRoleEntity in appProjectTeamMemberEntity.AppProjectTeamMemberRole)
                {
                    AppProjectTeamMemberRoleExDto appProjectTeamMemberRoleExDto = AppProjectTeamMemberRoleConverter.ConvertEntityToExDto(aAppProjectTeamMemberRoleEntity);
                    aAppProjectTeamMemberExDto.AppProjectTeamMemberRoleList.Add(appProjectTeamMemberRoleExDto);

                }
            }

            SetupDomainOrOrgId(aAppProjectTeamExDto);

            return aAppProjectTeamExDto;
        }

        public static OperationCallResult<AppProjectTeamExDto> SaveAppProjectTeamExDto(AppProjectTeamExDto aAppProjectTeamExDto)
        {
            OperationCallResult<AppProjectTeamExDto> aOperationCallResult = new OperationCallResult<AppProjectTeamExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppProjectTeamEntity aAppProjectTeamEntity;


            aAppProjectTeamExDto.AppProjectTeamMemberList = new ObservableSet<AppProjectTeamMemberExDto>();

            // Convert DictDomainOrOrgIdTeamMemberExDto to the List
            if (!aAppProjectTeamExDto.DictDomainOrOrgIdTeamMemberExDto.IsEmpty())
            {
                foreach (var listDto in aAppProjectTeamExDto.DictDomainOrOrgIdTeamMemberExDto.Values)
                {
                    foreach (var dto in listDto)
                    {
                        var existingTeamMember = aAppProjectTeamExDto.AppProjectTeamMemberList.FirstOrDefault(o => o.UserId.HasValue && dto.UserId.HasValue && o.UserId.Value == dto.UserId.Value);
                        if (existingTeamMember == null)
                        {
                            aAppProjectTeamExDto.AppProjectTeamMemberList.Add(dto);
                        }
                    }


                }
            }






            // prepare Data
            if (aAppProjectTeamExDto.IsNew)
            {
                aAppProjectTeamEntity = new AppProjectTeamEntity();
                AppProjectTeamConverter.CopyDtoToEntity(aAppProjectTeamEntity, aAppProjectTeamExDto);
                foreach (var aAppProjectTeamMemberExDto in aAppProjectTeamExDto.AppProjectTeamMemberList)
                {
                    ProcessNewTeammemberExDto(aAppProjectTeamEntity, aAppProjectTeamMemberExDto);
                }

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {

                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppProjectTeamEntity);
                        adapter.Commit();

                        aAppProjectTeamExDto.Id = aAppProjectTeamEntity.ProejctTeamId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                    }


                    catch (ORMQueryExecutionException ex)
                    {

                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamExDto), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppProjectTeamExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppProjectTeamExDto(aAppProjectTeamExDto));
            }


            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveProjectTeamExDto(aAppProjectTeamExDto.Id as int?);
            }

            return aOperationCallResult;


        }

        private static void SetupDomainOrOrgId(AppProjectTeamExDto aAppProjectTeamExDto)
        {
            if (aAppProjectTeamExDto.EmPrivacy.HasValue && (aAppProjectTeamExDto.EmPrivacy.Value == (int)EmAppProjectPrivacy.CrossDomain

                            || aAppProjectTeamExDto.EmPrivacy.Value == (int)EmAppProjectPrivacy.Organization))
            {
                var userIds = aAppProjectTeamExDto.AppProjectTeamMemberList.Select(o => o.UserId.Value).ToList();

                var userList = AppSecurityUserBL.RetrieveSimpleAppSecurityUserEntityList(userIds);

                Dictionary<int, int> dictUserIdDomainId = userList.ToDictionary(o => o.UserId, o => o.DomainId);
                //Dictionary<int, int> dictUserIdOrgId = userList.ToDictionary(o => o.UserId, o => o.OrganizationId.Value);

                foreach (var appProjectTeamMemberExDto in aAppProjectTeamExDto.AppProjectTeamMemberList)
                {
                    appProjectTeamMemberExDto.DomainId = dictUserIdDomainId[appProjectTeamMemberExDto.UserId.Value];
                    //appProjectTeamMemberExDto.OrganizationId = dictUserIdOrgId[appProjectTeamMemberExDto.UserId.Value];

                }

                if (aAppProjectTeamExDto.EmPrivacy.Value == (int)EmAppProjectPrivacy.CrossDomain)
                {
                    aAppProjectTeamExDto.DictDomainOrOrgIdTeamMemberExDto =
                        aAppProjectTeamExDto.AppProjectTeamMemberList.GroupBy(o => o.DomainId).ToDictionary(o => o.Key, g => g.ToList());                    
                }

                else if (aAppProjectTeamExDto.EmPrivacy.Value == (int)EmAppProjectPrivacy.Organization)
                {
                    aAppProjectTeamExDto.DictDomainOrOrgIdTeamMemberExDto =
                      aAppProjectTeamExDto.AppProjectTeamMemberList.GroupBy(o => o.OrganizationId).ToDictionary(o => o.Key, g => g.ToList());
                }



                // Merge Employee and Admin List. and Clear admin list
                if (!aAppProjectTeamExDto.DictDomainOrOrgIdTeamMemberExDto.ContainsKey((int)EmAppUserType.Employee))
                {
                    aAppProjectTeamExDto.DictDomainOrOrgIdTeamMemberExDto.Add((int)EmAppUserType.Employee, new List<AppProjectTeamMemberExDto>());
                }

                var employeeList = aAppProjectTeamExDto.DictDomainOrOrgIdTeamMemberExDto[(int)EmAppUserType.Employee];

                if (aAppProjectTeamExDto.DictDomainOrOrgIdTeamMemberExDto.ContainsKey((int)EmAppUserType.SysAdmin))
                {
                    employeeList.AddRange(aAppProjectTeamExDto.DictDomainOrOrgIdTeamMemberExDto[(int)EmAppUserType.SysAdmin]);
                    aAppProjectTeamExDto.DictDomainOrOrgIdTeamMemberExDto[(int)EmAppUserType.SysAdmin].Clear();
                }

                

                aAppProjectTeamExDto.AppProjectTeamMemberList = null;

            }
        }

        private static ValidationResult ProcessDirtyAppProjectTeamExDto(AppProjectTeamExDto AppProjectTeamExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            AppProjectTeamEntity AppProjectTeamEntity = RetrieveOneAppProjectTeamEntity(AppProjectTeamExDto.Id);
            AppProjectTeamConverter.CopyDtoToEntity(AppProjectTeamEntity, AppProjectTeamExDto);


            Dictionary<int, AppProjectTeamMemberEntity> dictDbAppProjectTeamMember = AppProjectTeamEntity.AppProjectTeamMember.ToDictionary(o => o.TeamMemberId, o => o);
            Dictionary<int, AppProjectTeamMemberExDto> dictDbAppProjectTeamMemberExdto = AppProjectTeamExDto.AppProjectTeamMemberList.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

            List<int> groupIdDbms = dictDbAppProjectTeamMember.Keys.ToList();

            List<int> groupIdUi = dictDbAppProjectTeamMemberExdto.Keys.ToList();


            //Delete Id
            List<int> deletAppProjectTeamMemberIDs = groupIdDbms.Except(groupIdUi).ToList();
            List<int> deletAppProjectTeamMemberRoleIDs = new List<int>();


            //new Entity
            AppProjectTeamExDto.AppProjectTeamMemberList.Where(o => o.IsNew)
                .ForAll(o => ProcessNewTeammemberExDto(AppProjectTeamEntity, o));


            //dirty
            List<int> dirtyAppProjectTeamMemberIds = groupIdDbms.Intersect(groupIdUi).ToList();


            // 
            foreach (int teammemberId in dirtyAppProjectTeamMemberIds)
            {
                AppProjectTeamMemberEntity appProjectTeamMemberEntity = dictDbAppProjectTeamMember[teammemberId];
                AppProjectTeamMemberExDto appProjectTeamMemberExdto = dictDbAppProjectTeamMemberExdto[teammemberId];
                AppProjectTeamMemberConverter.CopyDtoToEntity(appProjectTeamMemberEntity, appProjectTeamMemberExdto);

                AppProjectSettingBL.ProcessDirtyAppProjectTeammember(deletAppProjectTeamMemberRoleIDs, appProjectTeamMemberEntity, appProjectTeamMemberExdto);


            }


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(AppProjectTeamEntity);

                    adapter.DeleteEntitiesDirectly(typeof(AppProjectTeamMemberRoleEntity), new RelationPredicateBucket(AppProjectTeamMemberRoleFields.TeamMemberRoleId == deletAppProjectTeamMemberRoleIDs));

                    adapter.DeleteEntitiesDirectly(typeof(AppProjectTeamMemberEntity), new RelationPredicateBucket(AppProjectTeamMemberFields.TeamMemberId == deletAppProjectTeamMemberIDs));
                    //}

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ProjectTeamMember_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ProjectTeamMember_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }


            }

            return aValidationResult;
        }

        private static void ProcessNewTeammemberExDto(AppProjectTeamEntity aAppProjectTeamEntity, AppProjectTeamMemberExDto AppProjectTeamMemberExDto)
        {
            AppProjectTeamMemberEntity aAppProjectTeamMemberEntity = new AppProjectTeamMemberEntity();
            AppProjectTeamMemberConverter.CopyDtoToEntity(aAppProjectTeamMemberEntity, AppProjectTeamMemberExDto);
            aAppProjectTeamEntity.AppProjectTeamMember.Add(aAppProjectTeamMemberEntity);

            AppProjectSettingBL.PorcessTeamMemberRole(AppProjectTeamMemberExDto, aAppProjectTeamMemberEntity);

        }

        private static AppProjectTeamEntity RetrieveOneAppProjectTeamEntity(object teamId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppProjectTeamEntity AppProjectTeamEntity = new AppProjectTeamEntity(int.Parse(teamId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectTeamEntity);

                IPrefetchPathElement2 teamGroup = rootPath.Add(AppProjectTeamEntity.PrefetchPathAppProjectTeamMember);
                teamGroup.SubPath.Add(AppProjectTeamMemberEntity.PrefetchPathAppProjectTeamMemberRole);




                adpater.FetchEntity(AppProjectTeamEntity, rootPath);
                return AppProjectTeamEntity;


            }

        }
    }


}


