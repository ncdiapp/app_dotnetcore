using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Components.EntityConverter;
using System.Data;
using APP.Framework.Communication;

using APP.Framework;
namespace App.BL
{
	

	public static class AppProjectTaskCheckListBL
	{
		
		public static List<AppProjectTaskCheckListExDto> RetrieveOneTaskCheckListDtoList(int projectWorkFlowTaskID)
		{
			EntityCollection<AppProjectTaskCheckListEntity> folderEntities = RetrieveOneTaskCheckListEntityList(projectWorkFlowTaskID);

			var aDtoList = new List<AppProjectTaskCheckListExDto>();
			foreach (var folderEntity in folderEntities)
			{
				aDtoList.Add(AppProjectTaskCheckListConverter.ConvertEntityToExDto(folderEntity));
			}

			return aDtoList;
		}

		

		internal static EntityCollection<AppProjectTaskCheckListEntity> RetrieveOneTaskCheckListEntityList(int projectWorkFlowTaskID)
		{
			var timeSheetEntities = new EntityCollection<AppProjectTaskCheckListEntity>();

			using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
			{

				IRelationPredicateBucket filter = new RelationPredicateBucket(AppProjectTaskCheckListFields.ProjectTaskId == projectWorkFlowTaskID);


				adapater.FetchEntityCollection(timeSheetEntities, filter);
			}

			return timeSheetEntities;
		}

		public static OperationCallResult<AppProjectTaskCheckListExDto> SaveAllAppProjectTaskCheckListEntityDto(ObservableSet<AppProjectTaskCheckListExDto> aSet, int projectWorkFlowTaskID)
		{
			OperationCallResult<AppProjectTaskCheckListExDto> aOperationCallResult = new OperationCallResult<AppProjectTaskCheckListExDto>();
			ValidationResult validationResult = new ValidationResult();
			aOperationCallResult.ValidationResult = validationResult;


			var allRoleEntity = RetrieveOneTaskCheckListEntityList(projectWorkFlowTaskID);
			Dictionary<int, AppProjectTaskCheckListEntity> dictDbAppProjectTaskCheckList = allRoleEntity.ToDictionary(o => o.TaskCheckListId, o => o);
			Dictionary<int, AppProjectTaskCheckListExDto> dictDbAppProjectTaskCheckListExdto = aSet.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

			List<int> groupIdDbms = dictDbAppProjectTaskCheckList.Keys.ToList();

			List<int> groupIdUi = dictDbAppProjectTaskCheckListExdto.Keys.ToList();


			//Delete Id
			List<int> deletAppProjectTaskCheckListIDs = groupIdDbms.Except(groupIdUi).ToList();


			//new Entity
			foreach (var dto in aSet)
			{
				if (dto.IsNew)
				{

					AppProjectTaskCheckListEntity aParentAppProjectTaskCheckListEntity = new AppProjectTaskCheckListEntity();

					AppProjectTaskCheckListConverter.CopyDtoToEntity(aParentAppProjectTaskCheckListEntity, dto);

					allRoleEntity.Add(aParentAppProjectTaskCheckListEntity);

				}
				else // update 
				{
					if (dictDbAppProjectTaskCheckList.ContainsKey((int)dto.Id))
					{
						var entity = dictDbAppProjectTaskCheckList[(int)dto.Id];

						AppProjectTaskCheckListConverter.CopyDtoToEntity(entity, dto);

					}

				}


			}


			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{

				try
				{
					adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
					adapter.SaveEntityCollection(allRoleEntity);


					adapter.DeleteEntitiesDirectly(typeof(AppProjectTaskCheckListEntity), new RelationPredicateBucket(AppProjectTaskCheckListFields.TaskCheckListId == deletAppProjectTaskCheckListIDs));
					//}

					adapter.Commit();
					validationResult.Items.Add(new ValidationItem(typeof(AppProjectTaskCheckListExDto), "App_ProjectTeamMember_Save_OK", ValidationItemType.Message, "Saved Successfully"));

				}


				catch (ORMQueryExecutionException ex)
				{

					adapter.Rollback();
					validationResult.Items.Add(new ValidationItem(typeof(AppProjectTaskCheckListExDto), "App_ProjectTeamMember_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


				}


			}



			// if no any errors, refresh all entity from DBMS server
			if (!validationResult.HasErrors)
			{
				validationResult.Items.Clear();
				validationResult.Items.Add(new ValidationItem(typeof(AppProjectTaskCheckListExDto), "App_AppProjectTaskCheckListEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

				aOperationCallResult.ObjectList = RetrieveOneTaskCheckListDtoList(projectWorkFlowTaskID);
			}

			return aOperationCallResult;
		}




	}


}
