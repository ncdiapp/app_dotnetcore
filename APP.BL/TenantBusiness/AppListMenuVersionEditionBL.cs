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
	

	public static class AppVersionEditionModuleBL
	{


		public static List<AppVersionEditionModuleExDto> RetrieveModuleDtoList(int emversionEdition)
		{
			EntityCollection<AppVersionEditionModuleEntity> folderEntities = RetrieveModuleEntityList(emversionEdition);

			var aDtoList = new List<AppVersionEditionModuleExDto>();
			foreach (var folderEntity in folderEntities)
			{
				aDtoList.Add(AppVersionEditionModuleConverter.ConvertEntityToExDto(folderEntity));
			}

			return aDtoList;
		}

		private static EntityCollection<AppVersionEditionModuleEntity> RetrieveModuleEntityList(int emversionEdition)
		{
			var folderEntities = new EntityCollection<AppVersionEditionModuleEntity>();

			using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
			{

				IRelationPredicateBucket filter = new RelationPredicateBucket(AppVersionEditionModuleFields.EmApplicationVersionEdition == emversionEdition);


				adapater.FetchEntityCollection(folderEntities, filter);
			}

			return folderEntities;
		}

		public static OperationCallResult<AppVersionEditionModuleExDto> SaveAllAppVersionEditionModuleEntityDto(ObservableSet<AppVersionEditionModuleExDto> aSet, int emversionEdition)
		{
			OperationCallResult<AppVersionEditionModuleExDto> aOperationCallResult = new OperationCallResult<AppVersionEditionModuleExDto>();
			ValidationResult validationResult = new ValidationResult();
			aOperationCallResult.ValidationResult = validationResult;


			var allRoleEntity = RetrieveModuleEntityList(emversionEdition);
			Dictionary<int, AppVersionEditionModuleEntity> dictDbAppVersionEditionModule = allRoleEntity.ToDictionary(o => o.EditionModuleItemId, o => o);
			Dictionary<int, AppVersionEditionModuleExDto> dictDbAppVersionEditionModuleExdto = aSet.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

			List<int> groupIdDbms = dictDbAppVersionEditionModule.Keys.ToList();

			List<int> groupIdUi = dictDbAppVersionEditionModuleExdto.Keys.ToList();


			//Delete Id
			List<int> deletAppVersionEditionModuleIDs = groupIdDbms.Except(groupIdUi).ToList();


			//new Entity
			foreach (var dto in aSet)
			{
				if (dto.IsNew)
				{

					AppVersionEditionModuleEntity aParentAppVersionEditionModuleEntity = new AppVersionEditionModuleEntity();

					AppVersionEditionModuleConverter.CopyDtoToEntity(aParentAppVersionEditionModuleEntity, dto);

					allRoleEntity.Add(aParentAppVersionEditionModuleEntity);

				}
				else // update 
				{
					if (dictDbAppVersionEditionModule.ContainsKey((int)dto.Id))
					{
						var entity = dictDbAppVersionEditionModule[(int)dto.Id];

						AppVersionEditionModuleConverter.CopyDtoToEntity(entity, dto);

					}

				}


			}


			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{

				try
				{
					adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
					adapter.SaveEntityCollection(allRoleEntity);


					adapter.DeleteEntitiesDirectly(typeof(AppVersionEditionModuleEntity), new RelationPredicateBucket(AppVersionEditionModuleFields.EditionModuleItemId == deletAppVersionEditionModuleIDs));
					//}

					adapter.Commit();
					validationResult.Items.Add(new ValidationItem(typeof(AppVersionEditionModuleExDto), "App_ProjectTeamMember_Save_OK", ValidationItemType.Message, "Saved Successfully"));

				}


				catch (ORMQueryExecutionException ex)
				{

					adapter.Rollback();
					validationResult.Items.Add(new ValidationItem(typeof(AppVersionEditionModuleExDto), "App_ProjectTeamMember_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


				}


			}



			// if no any errors, refresh all entity from DBMS server
			if (!validationResult.HasErrors)
			{
				validationResult.Items.Clear();
				validationResult.Items.Add(new ValidationItem(typeof(AppVersionEditionModuleExDto), "App_AppVersionEditionModuleEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

				aOperationCallResult.ObjectList = RetrieveModuleDtoList(emversionEdition);
			}

			return aOperationCallResult;
		}




	}


}
