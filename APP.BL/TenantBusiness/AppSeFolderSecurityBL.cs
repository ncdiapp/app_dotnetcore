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
//using APP.Persistence.Common;

using APP.Framework;
namespace App.BL
{
    public static class AppSeFolderSecurityBL
	{

		public static EntityCollection<AppSefolderEntity> RetrieveCurrentUserFolderEntityByTransaction(int? transactionId)
		{
			EntityCollection<AppSefolderEntity> toReturnCollection = new EntityCollection<AppSefolderEntity>();

			if (!transactionId.HasValue)
			{
				return toReturnCollection;
			}

			var tranExdto= AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

			bool isFolderEnableSEcurity = CheckIfFolderTransactionEnableSecurity(transactionId);

            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            //  bool isInSysAdminDomain = AppSecurityUserBL.CurrentUserEntity.IsInSysAdminDomain  ;

            if (isAdmin ||  (! isFolderEnableSEcurity) )
			{
				using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
				{
					IRelationPredicateBucket adminFilter;
					if (tranExdto?.EmAppTransBusinessType.HasValue == true)
					{
						adminFilter = new RelationPredicateBucket(AppSefolderFields.FolderType == tranExdto.EmAppTransBusinessType);
					}
					else
					{
						adminFilter = new RelationPredicateBucket();
					}
					adapater.FetchEntityCollection(toReturnCollection, adminFilter);
				}

				Dictionary<int, AppSefolderEntity> dictadminAllEntity = toReturnCollection.ToDictionary(o => o.FolderId, o => o);
				SetupRootFolderFlag(toReturnCollection, dictadminAllEntity);
				return toReturnCollection;
			}
			else
			{
			
				var  avialbeFolders = GetRegularUserAvialbeFoldersByTransaction(transactionId.Value, isFolderEnableSEcurity);

				// Set
				Dictionary<int, AppSefolderEntity> dictAvailableEntity = avialbeFolders.ToDictionary(o => o.FolderId, o => o);
				SetupRootFolderFlag(toReturnCollection, dictAvailableEntity);

				// Process Client Actions

				if (isFolderEnableSEcurity)
				{
					CheckAvailableFolderRestriction(dictAvailableEntity);
				}

				return avialbeFolders;

			}

		}

		private static void CheckAvailableFolderRestriction(Dictionary<int, AppSefolderEntity> dictAvialbeFolderEntity)
		{
			HashSet<int> readOnlyfodlerId;

			string fodlerInclause = DBInteractionBase.GenerateColumnInClauseWithAndCondition(dictAvialbeFolderEntity.Keys, "FolderId", false);
			string roleINClaseu = DBInteractionBase.GenerateColumnInClauseWithAndCondition(AppSecurityUserBL.CurrentGroupIds, "RoleID", false);

			string roleInclasue = string.Empty;
			if (!string.IsNullOrEmpty(roleINClaseu))
			{
				roleInclasue = " or " + roleINClaseu;
			}

			if (!string.IsNullOrWhiteSpace(fodlerInclause))
			{
				fodlerInclause = fodlerInclause + " and ";
			}
			string queryFodlerID = @" select FolderId  from AppSefolderResource
                                          where  " + fodlerInclause + "   ( UserID = " + AppSecurityUserBL.CurrentUserId + roleInclasue + ") and IsReadOnly =1  ";

			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{
				var listId = adapter.ExecuteDataTableRetrievalQuery(queryFodlerID, null).AsEnumerable().Select(r => r.Field<int>("FolderId"));
				readOnlyfodlerId = new HashSet<int>(listId);
			}


			
			
			foreach (var pair in dictAvialbeFolderEntity)
			{
				int forderId = pair.Key;
				var fodler = pair.Value;

				// need to set readonly security
				if (readOnlyfodlerId.Contains(forderId))
				{
					fodler.UserAvailableActions = new List<string>();
				}
				else//// null: no more restriction security control
				{
					fodler.UserAvailableActions = null;
				}
			}
		}

		private static EntityCollection<AppSefolderEntity> GetRegularUserAvialbeFoldersByTransaction(int transactionId, bool isFolderEnableSEcurity)
		{
			EntityCollection<AppSefolderEntity> toReturnCollection = new EntityCollection<AppSefolderEntity>();

			AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

			IRelationPredicateBucket userfilter;
			if (isFolderEnableSEcurity)
			{
				
				userfilter = new RelationPredicateBucket();

				userfilter.PredicateExpression.Add(

					   AppSefolderResourceFields.UserId == AppSecurityUserBL.CurrentUserId |
					   AppSefolderResourceFields.RoleId == AppSecurityUserBL.CurrentGroupIds

					);

				userfilter.Relations.Add(AppSefolderEntity.Relations.AppSefolderResourceEntityUsingFolderId);
			}
			else if (appTransactionExDto?.EmAppTransBusinessType.HasValue == true)
			{
				EmAppTransBusinessType transBusinessType = (EmAppTransBusinessType)appTransactionExDto.EmAppTransBusinessType.Value;
				userfilter = new RelationPredicateBucket(AppSefolderFields.FolderType == transBusinessType);
			}
			else
			{
				userfilter = new RelationPredicateBucket();
			}

			EntityCollection<AppSefolderEntity> aCreaterCollection;
			using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
			{
				adapater.FetchEntityCollection(toReturnCollection, userfilter);
				aCreaterCollection = GetCreatersFoldersByTransaction(transactionId, adapater);
			}

			// process Creater folder
			Dictionary<int, AppSefolderEntity> dictAlEntity = toReturnCollection.ToDictionary(o => o.FolderId, o => o);

			//  Dictionary<int, AppSefolderEntity> dictCreaterFodler = new Dictionary<int, AppSefolderEntity>();

			// Merge Creater folder to all Folders
			foreach (var createrfolder in aCreaterCollection)
			{
				if (!dictAlEntity.ContainsKey(createrfolder.FolderId))
				{
					toReturnCollection.Add(createrfolder);

					// dictCreaterFodler.Add(createrfolder.FolderId, createrfolder);
				}
			}

			return toReturnCollection;
		}


		private static EntityCollection<AppSefolderEntity> GetCreatersFoldersByTransaction(int transactionId, DataAccessAdapter adapater)
		{
			EntityCollection<AppSefolderEntity> aCreaterCollection = new EntityCollection<AppSefolderEntity>();

			AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
			if (appTransactionExDto?.EmAppTransBusinessType.HasValue != true)
			{
				return aCreaterCollection;
			}

			EmAppTransBusinessType transBusinessType = (EmAppTransBusinessType)appTransactionExDto.EmAppTransBusinessType.Value;


			RelationPredicateBucket createfilter = new RelationPredicateBucket(AppSefolderFields.FolderType == (int)transBusinessType);
			createfilter.PredicateExpression.AddWithAnd(AppSefolderFields.AppCreatedById == AppSecurityUserBL.CurrentUserId);
			adapater.FetchEntityCollection(aCreaterCollection, createfilter);
			return aCreaterCollection;
		}

		internal static bool CheckIfFolderTransactionEnableSecurity(int? transactionId)
		{
			bool isFolderEnableSEcurity = false;

			if (transactionId.HasValue)
			{
				//AppTransactionEntity transactionEntity = AppTransactionBL.RetrieveOneAppTransactionEntity(transactionId);
				//if (transactionEntity != null
				//    && transactionEntity.TransactionOrganizedType.HasValue && transactionEntity.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.FolderList
				//    && transactionEntity.NeedToCheckRowVersion.HasValue && transactionEntity.NeedToCheckRowVersion.Value)
				//{
				//    isFolderEnableSEcurity = true;
				//}

				var trans = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
				if (trans?.MgtRootFolderId.HasValue == true && trans.IsEnableFolderSecurity.HasValue && trans.IsEnableFolderSecurity.Value)
				{
					isFolderEnableSEcurity = true;

				}
			}

			return isFolderEnableSEcurity;
		}



		private static void SetupRootFolderFlag(EntityCollection<AppSefolderEntity> toReturnCollection, Dictionary<int, AppSefolderEntity> dictAllEntity)
		{
			foreach (var folder in toReturnCollection)
			{
				if (!folder.ParentId.HasValue)
				{
					folder.IsRootFolder = true;
				}
				else
				{
					if (dictAllEntity.ContainsKey(folder.ParentId.Value))
					{
						folder.IsRootFolder = false;
					}
					else // not include in the current collection
					{
						folder.IsRootFolder = true; //
					}
				}
			}
		}




		private static EntityCollection<AppSefolderEntity> GetCreatersFileFolders(DataAccessAdapter adapater, EmAppTransBusinessType emAppTransBusinessType)
		{
			EntityCollection<AppSefolderEntity> aCreaterCollection = new EntityCollection<AppSefolderEntity>();
			RelationPredicateBucket createfilter = new RelationPredicateBucket(AppSefolderFields.FolderType == (int)emAppTransBusinessType);
			createfilter.PredicateExpression.AddWithAnd(AppSefolderFields.AppCreatedById == AppSecurityUserBL.CurrentUserId);
			adapater.FetchEntityCollection(aCreaterCollection, createfilter);
			return aCreaterCollection;
		}




	}
}