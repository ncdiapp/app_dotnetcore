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
    public static class AppProjectSettingBL
    {

        // Need security Control !!
        public static List<AppProjectOrWorkFlowDto> RetrieveAppProjectList(bool? isPredefined = null)
        {
            List<AppProjectOrWorkFlowDto> allprojectList = new List<AppProjectOrWorkFlowDto>();
            EntityCollection<AppProjectOrWorkFlowEntity> entities = new EntityCollection<AppProjectOrWorkFlowEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                //RelationPredicateBucket filter = new RelationPredicateBucket(AppProjectOrWorkFlowFields.IsPredefined == false & AppProjectOrWorkFlowFields.ProjectWorkflowType == projectWorkflowType);
                RelationPredicateBucket filter = new RelationPredicateBucket(AppProjectOrWorkFlowFields.ProjectWorkflowType == (int)EmAppProjectWorkflowType.Project);

                if (isPredefined.HasValue)
                {
                    filter.PredicateExpression.AddWithAnd(AppProjectOrWorkFlowFields.IsPredefined == isPredefined.Value);
                }

                adapter.FetchEntityCollection(entities, filter);
            }

            foreach (AppProjectOrWorkFlowEntity entity in entities)
            {
                var projectDto = AppProjectOrWorkFlowConverter.ConvertEntityToDto(entity);
                InitializedProjectStage(projectDto);
                allprojectList.Add(projectDto);
            }

            return allprojectList;
        }

        public static List<AppProjectOrWorkFlowDto> RetrieveAppProjectAndWorkflowListByIds(List<int> projectIds)
        {
            List<AppProjectOrWorkFlowDto> allprojectList = new List<AppProjectOrWorkFlowDto>();

            if (projectIds != null && projectIds.Count > 0)
            {
                EntityCollection<AppProjectOrWorkFlowEntity> entities = new EntityCollection<AppProjectOrWorkFlowEntity>();

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    RelationPredicateBucket filter = new RelationPredicateBucket(AppProjectOrWorkFlowFields.ProjectId == projectIds.ToArray());
                    adapter.FetchEntityCollection(entities, filter);
                }

                foreach (AppProjectOrWorkFlowEntity entity in entities)
                {
                    var projectDto = AppProjectOrWorkFlowConverter.ConvertEntityToDto(entity);
                    if (projectDto.ProjectWorkflowType.HasValue && projectDto.ProjectWorkflowType.Value == (int)EmAppProjectWorkflowType.Project)
                    {
                        InitializedProjectStage(projectDto);
                    }

                    allprojectList.Add(projectDto);
                }
            }

            return allprojectList;
        }

        public static AppProjectOrWorkFlowExDto RetrieveProjectSettingExDto(int? projectId)
        {
            AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity = RetrieveOneAppProjectOrWorkFlowEntity(projectId);

            AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto = AppProjectOrWorkFlowConverter.ConvertEntityToExDto(aAppProjectOrWorkFlowEntity);

            foreach (AppProjectTeamMemberEntity appProjectTeamMemberEntity in aAppProjectOrWorkFlowEntity.AppProjectTeamMember)
            {
                AppProjectTeamMemberExDto aAppProjectTeamMemberExDto = AppProjectTeamMemberConverter.ConvertEntityToExDto(appProjectTeamMemberEntity);
                aAppProjectOrWorkFlowExDto.AppProjectTeamMemberList.Add(aAppProjectTeamMemberExDto);

                foreach (var aAppProjectTeamMemberRoleEntity in appProjectTeamMemberEntity.AppProjectTeamMemberRole)
                {
                    AppProjectTeamMemberRoleExDto appProjectTeamMemberRoleExDto = AppProjectTeamMemberRoleConverter.ConvertEntityToExDto(aAppProjectTeamMemberRoleEntity);
                    aAppProjectTeamMemberExDto.AppProjectTeamMemberRoleList.Add(appProjectTeamMemberRoleExDto);

                }
            }

            SetupDomainOrOrgId(aAppProjectOrWorkFlowExDto);

            aAppProjectOrWorkFlowExDto.IsModified = false;
            InitializedProjectStage(aAppProjectOrWorkFlowExDto);

            return aAppProjectOrWorkFlowExDto;
        }

        public static void InitializedProjectStage(AppProjectOrWorkFlowDto aProjectDto)
        {

            if (aProjectDto.DateActualEnd.HasValue)
            {
                aProjectDto.EmAppProjectStage = EmAppProjectStage.Completed;
                aProjectDto.ProjectStageDisplay = "Complete";
            }
            else if (aProjectDto.DatePlannedStart.HasValue && aProjectDto.DateActualStart.HasValue)
            {
                aProjectDto.EmAppProjectStage = EmAppProjectStage.Processing;
                aProjectDto.ProjectStageDisplay = "Execution";
            }
            else
            {
                aProjectDto.EmAppProjectStage = EmAppProjectStage.Planning;
                aProjectDto.ProjectStageDisplay = "Planning";
            }

        }

        public static OperationCallResult<AppProjectOrWorkFlowExDto> SaveProjectSettingExDto(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            OperationCallResult<AppProjectOrWorkFlowExDto> aOperationCallResult = new OperationCallResult<AppProjectOrWorkFlowExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity;


            aAppProjectOrWorkFlowExDto.AppProjectTeamMemberList = new ObservableSet<AppProjectTeamMemberExDto>();

            if (aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto == null)
            {
                aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto = new Dictionary<int, List<AppProjectTeamMemberExDto>>();
            }

            foreach (var listDto in aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto.Values)
            {
                foreach (var dto in listDto)
                {
                    var existingTeamMember = aAppProjectOrWorkFlowExDto.AppProjectTeamMemberList.FirstOrDefault(o => o.UserId.HasValue && dto.UserId.HasValue && o.UserId.Value == dto.UserId.Value);
                    if (existingTeamMember == null)
                    {
                        aAppProjectOrWorkFlowExDto.AppProjectTeamMemberList.Add(dto);
                    }                    
                }


            }




            // prepare Data
            if (aAppProjectOrWorkFlowExDto.IsNew)
            {
                aAppProjectOrWorkFlowEntity = new AppProjectOrWorkFlowEntity();
                AppProjectOrWorkFlowConverter.CopyDtoToEntity(aAppProjectOrWorkFlowEntity, aAppProjectOrWorkFlowExDto);
                foreach (var aAppProjectTeamMemberExDto in aAppProjectOrWorkFlowExDto.AppProjectTeamMemberList)
                {
                    ProcessNewTeammemberExDto(aAppProjectOrWorkFlowEntity, aAppProjectTeamMemberExDto);
                }

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {

                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppProjectOrWorkFlowEntity);
                        adapter.Commit();


                        aAppProjectOrWorkFlowExDto.Id = aAppProjectOrWorkFlowEntity.ProjectId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                    }


                    catch (ORMQueryExecutionException ex)
                    {

                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppProjectOrWorkFlowExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppProjectOrWorkFlowExDto(aAppProjectOrWorkFlowExDto));
            }


            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveProjectSettingExDto(aAppProjectOrWorkFlowExDto.Id as int?);
            }

            return aOperationCallResult;


        }


        public static AppProjectOrWorkFlowExDto ImportProjectTeamFromLibary(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, int? projectTeamId)
        {
            if (aAppProjectOrWorkFlowExDto != null && projectTeamId.HasValue)
            {
                if (aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto == null)
                {
                    aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto = new Dictionary<int, List<AppProjectTeamMemberExDto>>();
                }

                AppProjectTeamExDto teamTemplate = AppProjectTeamLibBL.RetrieveProjectTeamExDto(projectTeamId.Value);

                if (teamTemplate != null && teamTemplate.DictDomainOrOrgIdTeamMemberExDto != null && teamTemplate.DictDomainOrOrgIdTeamMemberExDto.Count > 0)
                {
                    var dictTeamLibary = teamTemplate.DictDomainOrOrgIdTeamMemberExDto;
                    var dictProjectTeam = aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto;

                    foreach (int domainOrOrgId in dictTeamLibary.Keys)
                    {
                        if (!dictProjectTeam.Keys.Contains(domainOrOrgId))
                        {
                            dictProjectTeam.Add(domainOrOrgId, new List<AppProjectTeamMemberExDto>());
                        }

                        if (dictProjectTeam[domainOrOrgId] == null)
                        {
                            dictProjectTeam[domainOrOrgId] = new List<AppProjectTeamMemberExDto>();
                        }

                        var libTeamMembers = dictTeamLibary[domainOrOrgId];
                        var projectTeamMembers = dictProjectTeam[domainOrOrgId];

                        foreach (AppProjectTeamMemberExDto aLibTeamMember in libTeamMembers)
                        {
                            if (aLibTeamMember.UserId.HasValue)
                            {
                                var projectTeamMember = projectTeamMembers.FirstOrDefault(o => o.UserId.HasValue && o.UserId.Value == aLibTeamMember.UserId.Value);

                                if (projectTeamMember == null)
                                {
                                    projectTeamMember = new AppProjectTeamMemberExDto();
                                    projectTeamMember.UserId = aLibTeamMember.UserId;
                                    //projectTeamMember.ProjectId = (int)aAppProjectOrWorkFlowExDto.Id;
                                    projectTeamMembers.Add(projectTeamMember);
                                }

                                projectTeamMember.EmCostType = aLibTeamMember.EmCostType;
                                projectTeamMember.PersonalRate = aLibTeamMember.PersonalRate;
                                projectTeamMember.CurrencyId = aLibTeamMember.CurrencyId;

                                if (projectTeamMember.AppProjectTeamMemberRoleList == null)
                                {
                                    projectTeamMember.AppProjectTeamMemberRoleList = new ObservableSet<AppProjectTeamMemberRoleExDto>();
                                }

                                foreach (var libMemberRole in aLibTeamMember.AppProjectTeamMemberRoleList)
                                {
                                    if (libMemberRole.ProjectRoleId.HasValue)
                                    {
                                        var projectTeamMemeberRole = projectTeamMember.AppProjectTeamMemberRoleList.FirstOrDefault(o => o.ProjectRoleId.HasValue && o.ProjectRoleId.Value == libMemberRole.ProjectRoleId.Value);

                                        if (projectTeamMemeberRole == null)
                                        {
                                            projectTeamMemeberRole = new AppProjectTeamMemberRoleExDto();
                                            projectTeamMemeberRole.ProjectRoleId = libMemberRole.ProjectRoleId;
                                            projectTeamMember.AppProjectTeamMemberRoleList.Add(projectTeamMemeberRole);
                                        }

                                        projectTeamMemeberRole.RoleRate = libMemberRole.RoleRate;
                                    }
                                }
                            }
                        }
                    }
                }

                aAppProjectOrWorkFlowExDto.ParticipateDomainIdList = aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto.Keys.ToList();

            }

            return aAppProjectOrWorkFlowExDto;
        }


        private static void SetupDomainOrOrgId(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            if (aAppProjectOrWorkFlowExDto.EmPrivacy.HasValue && (aAppProjectOrWorkFlowExDto.EmPrivacy.Value == (int)EmAppProjectPrivacy.CrossDomain

                            || aAppProjectOrWorkFlowExDto.EmPrivacy.Value == (int)EmAppProjectPrivacy.Organization))
            {
                var userIds = aAppProjectOrWorkFlowExDto.AppProjectTeamMemberList.Select(o => o.UserId.Value).ToList();

                var userList = AppSecurityUserBL.RetrieveSimpleAppSecurityUserEntityList(userIds);

                Dictionary<int, int> dictUserIdDomainId = userList.ToDictionary(o => o.UserId, o => o.DomainId);
                //Dictionary<int, int> dictUserIdOrgId = userList.ToDictionary(o => o.UserId, o => o.OrganizationId.Value);

                foreach (var appProjectTeamMemberExDto in aAppProjectOrWorkFlowExDto.AppProjectTeamMemberList)
                {
                    appProjectTeamMemberExDto.DomainId = dictUserIdDomainId[appProjectTeamMemberExDto.UserId.Value];
                    //appProjectTeamMemberExDto.OrganizationId = dictUserIdOrgId[appProjectTeamMemberExDto.UserId.Value];

                }

                if (aAppProjectOrWorkFlowExDto.EmPrivacy.Value == (int)EmAppProjectPrivacy.CrossDomain)
                {
                    aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto =
                        aAppProjectOrWorkFlowExDto.AppProjectTeamMemberList.GroupBy(o => o.DomainId).ToDictionary(o => o.Key, g => g.ToList());



                }

                else if (aAppProjectOrWorkFlowExDto.EmPrivacy.Value == (int)EmAppProjectPrivacy.Organization)
                {
                    aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto =
                      aAppProjectOrWorkFlowExDto.AppProjectTeamMemberList.GroupBy(o => o.OrganizationId).ToDictionary(o => o.Key, g => g.ToList());



                }


                // Merge Employee and Admin List. and Clear admin list
                if (!aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto.ContainsKey((int)EmAppUserType.Employee))
                {
                    aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto.Add((int)EmAppUserType.Employee, new List<AppProjectTeamMemberExDto>());
                }

                var employeeList = aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto[(int)EmAppUserType.Employee];

                if (aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto.ContainsKey((int)EmAppUserType.SysAdmin))
                {
                    employeeList.AddRange(aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto[(int)EmAppUserType.SysAdmin]);
                    aAppProjectOrWorkFlowExDto.DictDomainOrOrgIdTeamMemberExDto[(int)EmAppUserType.SysAdmin].Clear();
                }

                aAppProjectOrWorkFlowExDto.AppProjectTeamMemberList = null;

            }
        }

        private static ValidationResult ProcessDirtyAppProjectOrWorkFlowExDto(AppProjectOrWorkFlowExDto appProjectOrWorkFlowExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            AppProjectOrWorkFlowEntity appProjectOrWorkFlowEntity = RetrieveOneAppProjectOrWorkFlowEntity(appProjectOrWorkFlowExDto.Id);
            AppProjectOrWorkFlowConverter.CopyDtoToEntity(appProjectOrWorkFlowEntity, appProjectOrWorkFlowExDto);


            Dictionary<int, AppProjectTeamMemberEntity> dictDbAppProjectTeamMember = appProjectOrWorkFlowEntity.AppProjectTeamMember.ToDictionary(o => o.TeamMemberId, o => o);
            Dictionary<int, AppProjectTeamMemberExDto> dictDbAppProjectTeamMemberExdto = appProjectOrWorkFlowExDto.AppProjectTeamMemberList.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

            List<int> groupIdDbms = dictDbAppProjectTeamMember.Keys.ToList();

            List<int> groupIdUi = dictDbAppProjectTeamMemberExdto.Keys.ToList();


            //Delete Id
            List<int> deletAppProjectTeamMemberIDs = groupIdDbms.Except(groupIdUi).ToList();
            List<int> deletAppProjectTeamMemberRoleIDs = new List<int>();


            //new Entity
            appProjectOrWorkFlowExDto.AppProjectTeamMemberList.Where(o => o.IsNew)
                .ForAll(o => ProcessNewTeammemberExDto(appProjectOrWorkFlowEntity, o));


            //dirty
            List<int> dirtyAppProjectTeamMemberIds = groupIdDbms.Intersect(groupIdUi).ToList();


            // 
            foreach (int teammemberId in dirtyAppProjectTeamMemberIds)
            {
                AppProjectTeamMemberEntity appProjectTeamMemberEntity = dictDbAppProjectTeamMember[teammemberId];
                AppProjectTeamMemberExDto appProjectTeamMemberExdto = dictDbAppProjectTeamMemberExdto[teammemberId];
                AppProjectTeamMemberConverter.CopyDtoToEntity(appProjectTeamMemberEntity, appProjectTeamMemberExdto);
                ProcessDirtyAppProjectTeammember(deletAppProjectTeamMemberRoleIDs, appProjectTeamMemberEntity, appProjectTeamMemberExdto);

            }


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(appProjectOrWorkFlowEntity);


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

        internal static void ProcessDirtyAppProjectTeammember(List<int> deletAppProjectTeamMemberRoleIDs, AppProjectTeamMemberEntity appProjectTeamMemberEntity, AppProjectTeamMemberExDto appProjectTeamMemberExdto)
        {
            // new Children
            PorcessTeamMemberRole(appProjectTeamMemberExdto, appProjectTeamMemberEntity);


            Dictionary<int, AppProjectTeamMemberRoleEntity> dictDbMemberRoleItem = appProjectTeamMemberEntity.AppProjectTeamMemberRole.Where(o => !o.IsNew).ToDictionary(o => o.TeamMemberRoleId, o => o);
            Dictionary<int, AppProjectTeamMemberRoleExDto> dictMeberRoleExdto = appProjectTeamMemberExdto.AppProjectTeamMemberRoleList.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

            List<int> groupItemIdDbms = dictDbMemberRoleItem.Keys.ToList();
            List<int> groupItemIdUi = dictMeberRoleExdto.Keys.ToList();


            //dirty
            List<int> dirtyAppTransactionGroupItemssIds = groupItemIdDbms.Intersect(groupItemIdUi).ToList();

            foreach (int updateGroupItemId in dirtyAppTransactionGroupItemssIds)
            {
                AppProjectTeamMemberRoleEntity appProjectTeamMemberRoleEntity = dictDbMemberRoleItem[updateGroupItemId];
                AppProjectTeamMemberRoleExDto appProjectTeamMemberRoleExDto = dictMeberRoleExdto[updateGroupItemId];
                AppProjectTeamMemberRoleConverter.CopyDtoToEntity(appProjectTeamMemberRoleEntity, appProjectTeamMemberRoleExDto);


            }

            //merger all deleteIds
            deletAppProjectTeamMemberRoleIDs.AddRange(groupItemIdDbms.Except(groupItemIdUi));
        }

        private static void ProcessNewTeammemberExDto(AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity, AppProjectTeamMemberExDto AppProjectTeamMemberExDto)
        {
            AppProjectTeamMemberEntity aAppProjectTeamMemberEntity = new AppProjectTeamMemberEntity();
            AppProjectTeamMemberConverter.CopyDtoToEntity(aAppProjectTeamMemberEntity, AppProjectTeamMemberExDto);
            aAppProjectOrWorkFlowEntity.AppProjectTeamMember.Add(aAppProjectTeamMemberEntity);

            PorcessTeamMemberRole(AppProjectTeamMemberExDto, aAppProjectTeamMemberEntity);

        }

        internal static void PorcessTeamMemberRole(AppProjectTeamMemberExDto appProjectTeamMemberExDto, AppProjectTeamMemberEntity aAppTransactionGroupEntity)
        {
            foreach (var AppProjectTeamMemberDto in appProjectTeamMemberExDto.AppProjectTeamMemberRoleList.Where(o => o.IsNew))
            {

                AppProjectTeamMemberRoleEntity aAppProjectTeamMemberEntity = new AppProjectTeamMemberRoleEntity();
                AppProjectTeamMemberRoleConverter.CopyDtoToEntity(aAppProjectTeamMemberEntity, AppProjectTeamMemberDto);
                aAppTransactionGroupEntity.AppProjectTeamMemberRole.Add(aAppProjectTeamMemberEntity);

            }
        }

        private static AppProjectOrWorkFlowEntity RetrieveOneAppProjectOrWorkFlowEntity(object proejctId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppProjectOrWorkFlowEntity appProjectOrWorkFlowEntity = new AppProjectOrWorkFlowEntity(int.Parse(proejctId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectOrWorkFlowEntity);

                IPrefetchPathElement2 teamGroup = rootPath.Add(AppProjectOrWorkFlowEntity.PrefetchPathAppProjectTeamMember);
                teamGroup.SubPath.Add(AppProjectTeamMemberEntity.PrefetchPathAppProjectTeamMemberRole);




                adpater.FetchEntity(appProjectOrWorkFlowEntity, rootPath);
                return appProjectOrWorkFlowEntity;


            }

        }
    }


}


