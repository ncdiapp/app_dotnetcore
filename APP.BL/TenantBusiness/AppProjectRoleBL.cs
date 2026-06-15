using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;

using APP.Framework;
namespace App.BL
{
     public static class AppProjectRoleBL
    {
        public static readonly string App_ProjectRoleEntity_Save_OK = "App_ProjectRoleEntity_Save_OK";
        public static readonly string App_ProjectRoleEntity_Save_Failed = "App_ProjectRoleEntity_Save_Failed";
        public static readonly string App_ProjectRoleEntityUILayout_Save_OK = "App_ProjectRoleEntityUILayout_Save_OK";
        public static readonly string App_ProjectRoleEntityUILayout_Save_Failed = "App_ProjectRoleEntityUILayout_Save_Failed";
        public static readonly string App_ProjectRoleEntity_Delete_Ok = "App_ProjectRoleEntity_Delete_Ok";
        public static readonly string App_ProjectRoleEntity_Delete_Failed = "App_ProjectRoleEntity_Delete_Failed";

        public static ObservableSet<AppProjectRoleDto> RetrieveAllAppProjectRoleDto()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppProjectRoleEntity> list = new EntityCollection<AppProjectRoleEntity>();

                SortExpression expression = new SortExpression(AppProjectRoleFields.RoleName | SortOperator.Ascending);

				IPrefetchPath2 rootPath = new PrefetchPath2(APP.LBL.EntityType.AppProjectRoleEntity);
				var subpath =rootPath.Add(AppProjectRoleEntity.PrefetchPathAppProjectRolePrivilege);
				subpath.SubPath.Add(AppProjectRolePrivilegeEntity.PrefetchPathAppProjectPrivilegeLibrary);



				adapter.FetchEntityCollection(list, null, 0, expression, rootPath);

                var aDtoList = new ObservableSet<AppProjectRoleDto>();

                foreach (var roleEntity in list)
				{
					var roleDto = AppProjectRoleConverter.ConvertEntityToDto(roleEntity);
					AddRolePrevileDisplay(roleEntity, roleDto);

					aDtoList.Add(roleDto);
				}

				return aDtoList;
            }
        }

		private static void AddRolePrevileDisplay(AppProjectRoleEntity roleEntity, AppProjectRoleDto roleDto)
		{
			var displayList = roleEntity.AppProjectRolePrivilege.Select(o => o.AppProjectPrivilegeLibrary.Description).ToList();
			if (!displayList.IsEmpty())
			{
				roleDto.RoleDisplay = displayList.Aggregate((i, j) => i + "," + j);
			}
		}

		public static ObservableSet<AppProjectPrivilegeLibraryDto> RetrieveAllAppProjectPrivilegeLibraryDto()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppProjectPrivilegeLibraryEntity> list = new EntityCollection<AppProjectPrivilegeLibraryEntity>();

                SortExpression expression = new SortExpression(AppProjectPrivilegeLibraryFields.Description | SortOperator.Ascending);
                adapter.FetchEntityCollection(list, null, 0, expression);

                var aDtoList = new ObservableSet<AppProjectPrivilegeLibraryDto>();

                foreach (var o in list)
                {
                    aDtoList.Add(AppProjectPrivilegeLibraryConverter.ConvertEntityToDto(o));
                }

                return aDtoList;
            }
        }
        

        public static AppProjectRoleExDto RetrieveOneAppProjectRoleExDto(object ProjectRoleId)
        {
            AppProjectRoleEntity aAppProjectRoleEntity = RetrieveOneAppProjectRoleEntity(ProjectRoleId);
            AppProjectRoleExDto aProjectRoleDto = AppProjectRoleConverter.ConvertEntityToExDto(aAppProjectRoleEntity);

			AddRolePrevileDisplay(aAppProjectRoleEntity, aProjectRoleDto);

			foreach (var o in aAppProjectRoleEntity.AppProjectRolePrivilege)
            {
                AppProjectRolePrivilegeExDto aAppProjectRoleKeyExDto = AppProjectRolePrivilegeConverter.ConvertEntityToExDto(o);
			


				aProjectRoleDto.AppProjectRolePrivilegeList.Add(aAppProjectRoleKeyExDto);
            }


            return aProjectRoleDto;
        }


        public static AppProjectRoleEntity RetrieveOneAppProjectRoleEntity(object ProjectRoleId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppProjectRoleEntity ProjectRoleEntity = new AppProjectRoleEntity(int.Parse(ProjectRoleId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(APP.LBL.EntityType.AppProjectRoleEntity);
			
			

				rootPath.Add(AppProjectRoleEntity.PrefetchPathAppProjectRolePrivilege)
				.SubPath.Add(AppProjectRolePrivilegeEntity.PrefetchPathAppProjectPrivilegeLibrary); ;

                adpater.FetchEntity(ProjectRoleEntity, rootPath);
                return ProjectRoleEntity;
            }
        }

        public static OperationCallResult<AppProjectRoleExDto> SaveAppProjectRoleExDto(AppProjectRoleExDto aAppProjectRoleExDto)
        {
            OperationCallResult<AppProjectRoleExDto> aOperationCallResult = new OperationCallResult<AppProjectRoleExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppProjectRoleEntity aAppProjectRoleEntity;

            // prepare Data
            if (aAppProjectRoleExDto.IsNew)
            {
                aAppProjectRoleEntity = new AppProjectRoleEntity();
                AppProjectRoleConverter.CopyDtoToEntity(aAppProjectRoleEntity, aAppProjectRoleExDto);



                foreach (var templatefieldDto in aAppProjectRoleExDto.AppProjectRolePrivilegeList)
                {
                    AppProjectRolePrivilegeEntity aAppProjectRolePrivilegeEntity = new AppProjectRolePrivilegeEntity();
                    AppProjectRolePrivilegeConverter.CopyDtoToEntity(aAppProjectRolePrivilegeEntity, templatefieldDto);
                    aAppProjectRoleEntity.AppProjectRolePrivilege.Add(aAppProjectRolePrivilegeEntity);
                }
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
					{//ProjectRoleId
						adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppProjectRoleEntity);
                        adapter.Commit();
						//ProjectRoleId
						aAppProjectRoleExDto.Id = aAppProjectRoleEntity.ProjectRoleId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectRoleEntity), "App_ProjectRoleEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

               
                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectRoleEntity), "App_ProjectRoleEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppProjectRoleExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppProjectRoleExDto(aAppProjectRoleExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppProjectRoleExDto(aAppProjectRoleExDto.Id);
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<object> DeleteOneAppProjectRole(object ProjectRoleId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppProjectRolePrivilegeEntity), new RelationPredicateBucket(AppProjectRolePrivilegeFields.ProjectRoleId == ProjectRoleId));
                    adapter.DeleteEntitiesDirectly(typeof(AppProjectRoleEntity), new RelationPredicateBucket(AppProjectRoleFields.ProjectRoleId == ProjectRoleId));
                    string message = StringLocalizer.Localize(App_ProjectRoleEntity_Delete_Ok, "ProjectRole Delete Successful");
                    aValidationResult.Items.Add(new ValidationItem(null, App_ProjectRoleEntity_Delete_Ok, ValidationItemType.Message, message));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize(App_ProjectRoleEntity_Delete_Failed, "ProjectRole Delete Failed" + ex.ToString());
                    aValidationResult.Items.Add(new ValidationItem(null, App_ProjectRoleEntity_Delete_Failed, ValidationItemType.Error, message));
                }
            }

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = ProjectRoleId;
            }

            return aOperationCallResult;
        }

        private static ValidationResult ProcessDirtyAppProjectRoleExDto(AppProjectRoleExDto aAppProjectRoleExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

       

            AppProjectRoleEntity aAppProjectRoleEntity = RetrieveOneAppProjectRoleEntity(aAppProjectRoleExDto.Id);

			//ProjectRoleActionId  ProjectRoleActionId
			Dictionary<int, AppProjectRolePrivilegeEntity> dictAppProjectRolePrivilegeFromDbms = aAppProjectRoleEntity.AppProjectRolePrivilege.ToDictionary(o => o.ProjectRoleActionId, o => o);

            AppProjectRoleConverter.CopyDtoToEntity(aAppProjectRoleEntity, aAppProjectRoleExDto);


			aAppProjectRoleEntity.AppProjectRolePrivilege.Clear();

			foreach (var aChildDto in aAppProjectRoleExDto.AppProjectRolePrivilegeList)
            {
                AppProjectRolePrivilegeEntity aNewChildEntity = new AppProjectRolePrivilegeEntity();
                AppProjectRolePrivilegeConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppProjectRoleEntity.AppProjectRolePrivilege.Add(aNewChildEntity);
            }

          

       

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");


					adapter.DeleteEntitiesDirectly(typeof(AppProjectRolePrivilegeEntity), new RelationPredicateBucket(AppProjectRolePrivilegeFields.ProjectRoleId == aAppProjectRoleExDto.Id));
				
					adapter.SaveEntity(aAppProjectRoleEntity);

                

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectRoleEntity), "App_ProjectRoleEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                     
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectRoleEntity), "App_ProjectRoleEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }


    }
}