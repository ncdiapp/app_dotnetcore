using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;

using APP.Framework;
namespace App.BL
{
    public static class AppTransactionRecycleBL
	{

		public static AppTrascationRecycleBinEntity CheckIfTranscationRootValueInRecycleBin(int transctionId, int rootValueId)
		{


			EntityCollection<AppTrascationRecycleBinEntity> listEntity = new EntityCollection<AppTrascationRecycleBinEntity>();
			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{
				RelationPredicateBucket filter = new RelationPredicateBucket(AppTrascationRecycleBinFields.TranscationId == transctionId);
				filter.PredicateExpression.AddWithAnd(AppTrascationRecycleBinFields.RootKeyValueId == rootValueId);
				adapter.FetchEntityCollection(listEntity, filter);
			}

			if(listEntity.Count>0)
			{
				return listEntity[0];
			}
			else
			{
				return null;
			}

		}


		//Key TrnascationId, value FieldIds
		public static bool MoveFileToRecycleBin(KeyValuePair<int, List<int>> trannactionIdFileIds)
		{

			List<AppTrascationRecycleBinDto> listDto = new List<AppTrascationRecycleBinDto>();

			List<AppTrascationRecycleBinEntity> listEntity = new List<AppTrascationRecycleBinEntity>();

			int trnasctionId = trannactionIdFileIds.Key;

			foreach ( int fileId in trannactionIdFileIds.Value )
			{
				AppTrascationRecycleBinDto aAppTrascationRecycleBinDto = new AppTrascationRecycleBinDto();
				aAppTrascationRecycleBinDto.TranscationId = trnasctionId;
				aAppTrascationRecycleBinDto.RootKeyValueId = fileId;

				AppTrascationRecycleBinEntity aAppTrascationRecycleBinEntity = new AppTrascationRecycleBinEntity();
				AppTrascationRecycleBinConverter.CopyDtoToEntity(aAppTrascationRecycleBinEntity, aAppTrascationRecycleBinDto);

				listEntity.Add(aAppTrascationRecycleBinEntity);

			}
			



			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{
				try
				{
					adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

					foreach ( var aAppTrascationRecycleBinEntity in listEntity)
					{
						adapter.SaveEntity(aAppTrascationRecycleBinEntity, false, true);

					}
					

					adapter.Commit();

					return true;
				}
						
				// Database FK Exception .......
				catch (ORMQueryExecutionException ex)
				{
					adapter.Rollback();

					return false;
				}
			}

			
		}

		public static bool RestoreFileFromRecycleBin(KeyValuePair<int, List<int>> trannactionIdFileIds)
		{

			List<AppTrascationRecycleBinDto> listDto = new List<AppTrascationRecycleBinDto>();

			List<AppTrascationRecycleBinEntity> listEntity = new List<AppTrascationRecycleBinEntity>();

			int trnasctionId = trannactionIdFileIds.Key;
			List<int> needToRestoreFiledids = trannactionIdFileIds.Value;

			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{
				try
				{
					adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

					RelationPredicateBucket filter = new RelationPredicateBucket(AppTrascationRecycleBinFields.TranscationId == trnasctionId);
					filter.PredicateExpression.AddWithAnd(AppTrascationRecycleBinFields.RootKeyValueId == needToRestoreFiledids);


					adapter.DeleteEntitiesDirectly(typeof(AppTrascationRecycleBinEntity), filter);



					adapter.Commit();

					return true;
				}

				// Database FK Exception .......
				catch (ORMQueryExecutionException ex)
				{
					adapter.Rollback();

					return false;
				}
			}


		}

		// 
		public static bool DeleteFileFromRecycleBin(KeyValuePair<int, List<int>> trannactionIdInitialFileIds)
		{

			List<AppTrascationRecycleBinDto> listDto = new List<AppTrascationRecycleBinDto>();

			List<AppTrascationRecycleBinEntity> listEntity = new List<AppTrascationRecycleBinEntity>();

			int trnasctionId = trannactionIdInitialFileIds.Key;
			List<int> needToDeleteInitialFiledids = trannactionIdInitialFileIds.Value;

			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{
				try
				{
					adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

					RelationPredicateBucket filter = new RelationPredicateBucket(AppTrascationRecycleBinFields.TranscationId == trnasctionId);
					filter.PredicateExpression.AddWithAnd(AppTrascationRecycleBinFields.RootKeyValueId == needToDeleteInitialFiledids);
					adapter.DeleteEntitiesDirectly(typeof(AppTrascationRecycleBinEntity), filter);

					// need to delete file and where used , in the future we need to setup a job to clear all file dependenct transcation ( set null)

					// delete history;
					RelationPredicateBucket childFileFilter = new RelationPredicateBucket(AppFileFields.InitialFileId == needToDeleteInitialFiledids);
					adapter.DeleteEntitiesDirectly(typeof(AppFileEntity), childFileFilter);


                    // delete AppFileOrFolderShareToOther
                    RelationPredicateBucket shareToOtherFilter = new RelationPredicateBucket(AppFileOrFolderShareToOtherFields.FileId == needToDeleteInitialFiledids);
                    adapter.DeleteEntitiesDirectly(typeof(AppFileOrFolderShareToOtherEntity), shareToOtherFilter);
                    

                    // delete  AppCurrentUserFavouriteFolderOrFile
                    RelationPredicateBucket favoriteFilter = new RelationPredicateBucket(AppCurrentUserFavouriteFolderOrFileFields.FiledId == needToDeleteInitialFiledids);
                    adapter.DeleteEntitiesDirectly(typeof(AppCurrentUserFavouriteFolderOrFileEntity), favoriteFilter);

                    // delete initial file itself

                    RelationPredicateBucket initfileFilter = new RelationPredicateBucket(AppFileFields.FileId == needToDeleteInitialFiledids);
					adapter.DeleteEntitiesDirectly(typeof(AppFileEntity), initfileFilter);


					adapter.Commit();

					return true;
				}

				// Database FK Exception .......
				catch (ORMQueryExecutionException ex)
				{
					adapter.Rollback();

					return false;
				}
			}


		}

		internal static int[] GetCurrentUserRecycleBinRootValueIds(int? transactionId)
		{

			if (transactionId.HasValue)
			{
				EntityCollection<AppTrascationRecycleBinEntity> list = new EntityCollection<AppTrascationRecycleBinEntity>();

				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{
					RelationPredicateBucket filter = new RelationPredicateBucket(AppTrascationRecycleBinFields.AppCreatedById == AppSecurityUserBL.CurrentUserId);
					filter.PredicateExpression.AddWithAnd(AppTrascationRecycleBinFields.TranscationId == transactionId);

					IncludeFieldsList includeFied = new IncludeFieldsList();
					includeFied.Add(AppTrascationRecycleBinFields.RootKeyValueId);


					adapter.FetchEntityCollection(list, includeFied, filter);



				}

				return list.Where(o => o.RootKeyValueId.HasValue).Select(o => o.RootKeyValueId.Value).ToArray();
			}
			else
			{
				return null;
			}

		}

		internal static int[] GetAllTransactionRecycleBinRootValueIds(int? transactionId)
		{

			if (transactionId.HasValue)
			{
				EntityCollection<AppTrascationRecycleBinEntity> list = new EntityCollection<AppTrascationRecycleBinEntity>();

				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{

					RelationPredicateBucket filter = new RelationPredicateBucket(AppTrascationRecycleBinFields.TranscationId == transactionId);


					IncludeFieldsList includeFied = new IncludeFieldsList();
					includeFied.Add(AppTrascationRecycleBinFields.RootKeyValueId);


					adapter.FetchEntityCollection(list, includeFied, filter);



				}

				return list.Where(o => o.RootKeyValueId.HasValue).Select(o => o.RootKeyValueId.Value).ToArray();
			}
			else
			{
				return null;
			}

		}



	}


}