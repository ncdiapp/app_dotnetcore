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
	

	public static class AppViewLinkedSeaechOrUrlBL
    {
		
		public static List<AppViewLinkedSeaechOrUrlExDto> RetrieveOneAppViewLinkedSeaechOrUrlExDto(int viewId)
		{
			EntityCollection<AppViewLinkedSeaechOrUrlEntity> folderEntities = RetrieveOneAppViewLinkedSeaechOrUrlEntityList(viewId);

			var aDtoList = new List<AppViewLinkedSeaechOrUrlExDto>();
			foreach (var folderEntity in folderEntities)
			{
				aDtoList.Add(AppViewLinkedSeaechOrUrlConverter.ConvertEntityToExDto(folderEntity));
			}

			return aDtoList;
		}

		

		internal static EntityCollection<AppViewLinkedSeaechOrUrlEntity> RetrieveOneAppViewLinkedSeaechOrUrlEntityList(int viewId)
		{
			var timeSheetEntities = new EntityCollection<AppViewLinkedSeaechOrUrlEntity>();

			using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
			{

				IRelationPredicateBucket filter = new RelationPredicateBucket(AppViewLinkedSeaechOrUrlFields.SearchViewId == viewId);


				adapater.FetchEntityCollection(timeSheetEntities, filter);
			}

			return timeSheetEntities;
		}

		public static OperationCallResult<AppViewLinkedSeaechOrUrlExDto> SaveAllAppViewLinkedSeaechOrUrlEntityDto(ObservableSet<AppViewLinkedSeaechOrUrlExDto> aSet, int viewId)
		{
			OperationCallResult<AppViewLinkedSeaechOrUrlExDto> aOperationCallResult = new OperationCallResult<AppViewLinkedSeaechOrUrlExDto>();
			ValidationResult validationResult = new ValidationResult();
			aOperationCallResult.ValidationResult = validationResult;


			var allRoleEntity = RetrieveOneAppViewLinkedSeaechOrUrlEntityList(viewId);
			Dictionary<int, AppViewLinkedSeaechOrUrlEntity> dictDbAppViewLinkedSeaechOrUrl = allRoleEntity.ToDictionary(o => o.SearchViewLinkSearchId , o => o);
			Dictionary<int, AppViewLinkedSeaechOrUrlExDto> dictDbAppViewLinkedSeaechOrUrlExdto = aSet.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

			List<int> groupIdDbms = dictDbAppViewLinkedSeaechOrUrl.Keys.ToList();

			List<int> groupIdUi = dictDbAppViewLinkedSeaechOrUrlExdto.Keys.ToList();


			//Delete Id
			List<int> deletAppViewLinkedSeaechOrUrlIDs = groupIdDbms.Except(groupIdUi).ToList();


			//new Entity
			foreach (var dto in aSet)
			{
				if (dto.IsNew)
				{

					AppViewLinkedSeaechOrUrlEntity aParentAppViewLinkedSeaechOrUrlEntity = new AppViewLinkedSeaechOrUrlEntity();

					AppViewLinkedSeaechOrUrlConverter.CopyDtoToEntity(aParentAppViewLinkedSeaechOrUrlEntity, dto);

					allRoleEntity.Add(aParentAppViewLinkedSeaechOrUrlEntity);

				}
				else // update 
				{
					if (dictDbAppViewLinkedSeaechOrUrl.ContainsKey((int)dto.Id))
					{
						var entity = dictDbAppViewLinkedSeaechOrUrl[(int)dto.Id];

						AppViewLinkedSeaechOrUrlConverter.CopyDtoToEntity(entity, dto);

					}

				}


			}


			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{

				try
				{
					adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
					adapter.SaveEntityCollection(allRoleEntity);


					adapter.DeleteEntitiesDirectly(typeof(AppViewLinkedSeaechOrUrlEntity), new RelationPredicateBucket(AppViewLinkedSeaechOrUrlFields.SearchViewLinkSearchId == deletAppViewLinkedSeaechOrUrlIDs));
					//}

					adapter.Commit();
					validationResult.Items.Add(new ValidationItem(typeof(AppViewLinkedSeaechOrUrlExDto), "App_ProjectTeamMember_Save_OK", ValidationItemType.Message, "Saved Successfully"));

				}


				catch (ORMQueryExecutionException ex)
				{

					adapter.Rollback();
					validationResult.Items.Add(new ValidationItem(typeof(AppViewLinkedSeaechOrUrlExDto), "App_ProjectTeamMember_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


				}


			}



			// if no any errors, refresh all entity from DBMS server
			if (!validationResult.HasErrors)
			{
				validationResult.Items.Clear();
				validationResult.Items.Add(new ValidationItem(typeof(AppViewLinkedSeaechOrUrlExDto), "App_AppViewLinkedSeaechOrUrlEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

				aOperationCallResult.ObjectList = RetrieveOneAppViewLinkedSeaechOrUrlExDto(viewId);
			}

			return aOperationCallResult;
		}




	}


}
