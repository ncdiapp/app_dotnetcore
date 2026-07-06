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
using System.Data.SqlClient;

using APP.Framework;
namespace App.BL
{
    public static class AppSeFolderBL
    {







        // ---- Folder Configuration ---
        #region ---- Folder Configuration ---

        public static AppSefolderEntity RetrieveOneAppSefolderEntity(object folderId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSefolderEntity folderEntity = new AppSefolderEntity(int.Parse(folderId.ToString()));
                adpater.FetchEntity(folderEntity);
                return folderEntity;
            }
        }


        public static AppSefolderExDto RetrieveOneAppSefolderExDto(object folderId)
        {
            AppSefolderEntity folderEntity = RetrieveOneAppSefolderEntity(folderId);
            AppSefolderExDto aAppSefolderExDto = AppSefolderConverter.ConvertEntityToExDto(folderEntity);

            return aAppSefolderExDto;
        }



		public static List<int> DeleteFolder(AppSefolderDto appSefolderDto)
		{
			// move all file to recycle bin bin or permannetly delte
			List<int> toRetrun = new List<int>();

		   AppSefolderDto[] allEnterFoler = RetrieveCurrentUserAllSubFolderHairarchyDto((int)appSefolderDto.Id, appSefolderDto.TransactionId.Value);
			var flatLsit = ConvertFolderHairarchyToFlatList(allEnterFoler);

			for (int i = flatLsit.Count - 1; i >= 0; i--)
			{
				var oneFodler = flatLsit[i];

				// Delete File
				EntityCollection<AppFileEntity> collectioEntity = new EntityCollection<AppFileEntity>();
				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{
					IncludeFieldsList inclue = new IncludeFieldsList();
					inclue.Add(AppFileFields.FileId);

					adapter.FetchEntityCollection(collectioEntity, inclue,new RelationPredicateBucket(AppFileFields.FolderId == oneFodler.Id));

					adapter.DeleteEntitiesDirectly(typeof(AppFileEntity), new RelationPredicateBucket(AppFileFields.FolderId == oneFodler.Id));

					toRetrun.AddRange(collectioEntity.Select(o => o.FileId));

				}

				// Delete Folder

				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{
					adapter.DeleteEntitiesDirectly(typeof(AppSefolderEntity), new RelationPredicateBucket(AppSefolderFields.FolderId == oneFodler.Id));

				}

			}

			return toRetrun;
		}


		public static AppSefolderDto[] RetrieveCurrentUserAllSubFolderHairarchyDto(int folderID, int transactionId)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            EmAppTransBusinessType transBusinessType = (EmAppTransBusinessType)appTransactionExDto.EmAppTransBusinessType.Value;
            AppSefolderDto[] rootFolders = GetAllSubFolderWithBusienssType(folderID, transactionId);

            return rootFolders;

        }


	
		private static AppSefolderDto[] GetAllSubFolderWithBusienssType(int folderID, int transactionId)
        {
            int enterFoderId = folderID;


            var list = AppSeFolderSecurityBL.RetrieveCurrentUserFolderEntityByTransaction(transactionId);

            var allfoldersTreeItems = new ObservableSet<AppSefolderDto>();

            Dictionary<int, int> dictFolderIdAndContentCount = CountFolderContentByTransactionId(transactionId);

            foreach (var o in list)
            {
                AppSefolderDto aAppSefolderDto = AppSefolderConverter.ConvertEntityToDto(o);

                if (o.UserAvailableActions != null && o.UserAvailableActions.Count == 0)
                {
                    aAppSefolderDto.IsFolderReadonly = true;
                }

                if (dictFolderIdAndContentCount != null && dictFolderIdAndContentCount.ContainsKey((int)aAppSefolderDto.Id))
                {
                    aAppSefolderDto.CountContent = dictFolderIdAndContentCount[(int)aAppSefolderDto.Id];
                }

                allfoldersTreeItems.Add(aAppSefolderDto);
            }


           


            AppSefolderDto[] rootFolders = allfoldersTreeItems.Where(f => (int)f.Id == enterFoderId).ToArray();

            foreach (var rootFolder in rootFolders)
            {
                rootFolder.FolderPath = rootFolder.Name + "/";
                ProcessChilds(allfoldersTreeItems, rootFolder);
            }

            CalculateFolderEntityCountSubTotal(allfoldersTreeItems.ToList());

            return rootFolders;
        }



        public static AppSefolderDto[] RetrieveCurrentUserTranscationFolderHairarchyDto(int transactionId)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            EmAppTransBusinessType transBusinessType = (EmAppTransBusinessType)appTransactionExDto.EmAppTransBusinessType.Value;

            int rootFoderId = appTransactionExDto.MgtRootFolderId.Value;



            //var list = AppSeFolderSecurityBL.RetrieveCurrentUserFolderEntities(transBusinessType);

            var list = AppSeFolderSecurityBL.RetrieveCurrentUserFolderEntityByTransaction(transactionId);

            var allfoldersTreeItems = new ObservableSet<AppSefolderDto>();

            Dictionary<int, int> dictFolderIdAndContentCount = CountFolderContentByTransactionId(transactionId);

            foreach (var o in list)
            {

                AppSefolderDto aAppSefolderDto = AppSefolderConverter.ConvertEntityToDto(o);

                if (o.UserAvailableActions != null && o.UserAvailableActions.Count == 0)
                {
                    aAppSefolderDto.IsFolderReadonly = true;
                }

                if (dictFolderIdAndContentCount !=null && dictFolderIdAndContentCount.ContainsKey((int)aAppSefolderDto.Id))
                {
                    aAppSefolderDto.CountContent = dictFolderIdAndContentCount[(int)aAppSefolderDto.Id];
                }

                allfoldersTreeItems.Add(aAppSefolderDto);
            }





            AppSefolderDto[] rootFolders = allfoldersTreeItems.Where(f => (int)f.Id == rootFoderId).ToArray();

            foreach (var rootFolder in rootFolders)
            {
                rootFolder.FolderPath = rootFolder.Name + "/";
                ProcessChilds(allfoldersTreeItems, rootFolder);
            }


            CalculateFolderEntityCountSubTotal(allfoldersTreeItems.ToList());

            return rootFolders;

        }



        public static List<AppSefolderDto> RetrieveAllRootFolderDtoList(int? folderType = null)
        {
            var folderEntities = new EntityCollection<AppSefolderEntity>();

            using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
            {

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppSefolderFields.ParentId == DBNull.Value);

                if (folderType.HasValue)
                {
                    filter.PredicateExpression.AddWithAnd(AppSefolderFields.FolderType == folderType.Value);
                }

                adapater.FetchEntityCollection(folderEntities, filter);
            }

            var aDtoList = new List<AppSefolderDto>();
            foreach (var folderEntity in folderEntities)
            {
                aDtoList.Add(AppSefolderConverter.ConvertEntityToDto(folderEntity));
            }

            return aDtoList;
        }

        internal static void ProcessChilds(IEnumerable<AppSefolderDto> allfoldersTreeItems, AppSefolderDto folderTreeItemDto)
        {

            AppSefolderDto[] children = GetChilds(allfoldersTreeItems, folderTreeItemDto).OrderBy(f => f.Name).ToArray();

            if (!children.IsEmpty())
            {
                folderTreeItemDto.Children = children;
                folderTreeItemDto.Children.ForAll(c =>
                {
                    c.FolderPath = folderTreeItemDto.FolderPath + c.Name + "/";
                    if (!c.DefaultViewId.HasValue)
                    {
                        c.DefaultViewId = folderTreeItemDto.DefaultViewId;
                    }

                    ProcessChilds(allfoldersTreeItems, c);
                });

            }
        }

        private static void AddFolderHairarchyToFlatList(AppSefolderDto[] folderHairarchy, List<AppSefolderDto> flatFolderList)
        {
            flatFolderList.AddRange(folderHairarchy);

            foreach (AppSefolderDto folderDto in folderHairarchy)
            {
                if (folderDto.Children != null && folderDto.Children.Count() > 0)
                {
                    AddFolderHairarchyToFlatList(folderDto.Children, flatFolderList);
                }
            }
        }


        private static AppSefolderDto[] GetChilds(IEnumerable<AppSefolderDto> allfoldersTreeItems, AppSefolderDto folderTreeItemDto)
        {
            return allfoldersTreeItems.Where(f => f.ParentId == (int)folderTreeItemDto.Id).ToArray();
        }





        public static OperationCallResult<AppSefolderDto> SaveAppSefolder(AppSefolderDto aAppSefolderExDto)
        {
            OperationCallResult<AppSefolderDto> aOperationCallResult = new OperationCallResult<AppSefolderDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSefolderEntity aAppSefolderEntity;

            // Save New
            if (aAppSefolderExDto.IsNew)
            {
                aAppSefolderEntity = new AppSefolderEntity();
                AppSefolderConverter.CopyDtoToEntity(aAppSefolderEntity, aAppSefolderExDto);



                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppSefolderEntity);
                        adapter.Commit();

                        aAppSefolderExDto.Id = aAppSefolderEntity.FolderId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }



                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            // Save Dirty
            else if (aAppSefolderExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(SaveAppSefolderExDto_ProcessDirtyAppSefolderExDto(aAppSefolderExDto));
            }

            // if no any errors, refresh from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppSefolderExDto(aAppSefolderExDto.Id);
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<object> PasteAppSefolder(int folderId, int parentFolderId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    if (!isChildFolder(parentFolderId, folderId))
                    {
                        AppSefolderEntity updateEntity = new AppSefolderEntity();
                        updateEntity.ParentId = parentFolderId;
                        adapter.UpdateEntitiesDirectly(updateEntity, new RelationPredicateBucket(AppSefolderFields.FolderId == folderId));
                        adapter.Commit();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                        aOperationCallResult.Object = true;
                    }
                    else
                    {
                        string referMsg = "Cannot move the folder into its' child folder.";
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_QueryExecution_Error", ValidationItemType.Error, referMsg));
                    }

                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aOperationCallResult;
        }

        public static bool isChildFolder(object childFolderId, object parentFolderId)
        {
            AppSefolderEntity childFolder = RetrieveOneAppSefolderEntity(childFolderId);
            while (childFolder.ParentId.HasValue && childFolder.ParentId != childFolder.FolderId)
            {
                if (childFolder.ParentId.Value == int.Parse(parentFolderId.ToString()))
                {
                    return true;
                }
                childFolder = RetrieveOneAppSefolderEntity(childFolder.ParentId.Value);
            }
            return false;
        }

        private static ValidationResult SaveAppSefolderExDto_ProcessDirtyAppSefolderExDto(AppSefolderDto aAppSefolderExDto)
        {
            int folderId = int.Parse(aAppSefolderExDto.Id.ToString());

            ValidationResult aValidationResult = new ValidationResult();

            AppSefolderEntity aAppSefolderEntity = RetrieveOneAppSefolderEntity(folderId);

            AppSefolderConverter.CopyDtoToEntity(aAppSefolderEntity, aAppSefolderExDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSefolderEntity);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }



        public static OperationCallResult<object> DeleteAppSefolder(object folderId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    var folderEntity = RetrieveOneAppSefolderEntity(folderId);
                    if (folderEntity != null && !folderEntity.ParentId.HasValue)
                    {
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_rootfoldercannotbedeleted", ValidationItemType.Error, "Root Folder Cannot Be Deleted."));
                        return aOperationCallResult;
                    }

                    var childFolderCollection = new EntityCollection<AppSefolderEntity>();
                    IRelationPredicateBucket childFolderFilter = new RelationPredicateBucket(AppSefolderFields.ParentId == folderId);
                    adapter.FetchEntityCollection(childFolderCollection, childFolderFilter);

                    if (childFolderCollection.Count > 0)
                    {
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_thisfolderhaschildfolders", ValidationItemType.Error, "This Folder Has Child Folders."));
                        return aOperationCallResult;
                    }

                    bool isFolderContetnEmpty = AppListEditFormDataLoadBL.CheckIsFolderEmpty((int)folderId);
                    if (!isFolderContetnEmpty)
                    {
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_thisfolderisnotempty", ValidationItemType.Error, "This Folder Is Not Empty."));
                        return aOperationCallResult;
                    }


                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");


					AppSecurityUserEntity userEntity = new AppSecurityUserEntity();
					userEntity.DefaultVendorRequestFolderId = null;
					adapter.UpdateEntitiesDirectly(userEntity,new RelationPredicateBucket(AppSecurityUserFields.DefaultVendorRequestFolderId == folderId));

				


					adapter.DeleteEntitiesDirectly(typeof(AppCurrentUserFavouriteFolderOrFileEntity), new RelationPredicateBucket(AppCurrentUserFavouriteFolderOrFileFields.FolderId == folderId));

					adapter.DeleteEntitiesDirectly(typeof(AppFileOrFolderShareToOtherEntity), new RelationPredicateBucket(AppFileOrFolderShareToOtherFields.FolderId == folderId));

					adapter.DeleteEntitiesDirectly(typeof(AppSefolderResourceEntity), new RelationPredicateBucket(AppSefolderResourceFields.FolderId == folderId));
					adapter.DeleteEntitiesDirectly(typeof(AppSefolderEntity), new RelationPredicateBucket(AppSefolderFields.FolderId == folderId));
		


					adapter.Commit();
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_Delete_OK", ValidationItemType.Message, "Delete Successful"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "App_SefolderEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }

                // if no any errors
                if (!aOperationCallResult.ValidationResult.HasErrors)
                {
                    aOperationCallResult.Object = folderId;
                }
            }

            return aOperationCallResult;
        }


        #endregion




        // ---- Folder Security Configuration---
        #region ---- Folder Security Configuration---

        public static List<AppSefolderResourceExDto> RetrieveOneAppSefolderResourceList(object folderId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSefolderResourceEntity> entityList = new EntityCollection<AppSefolderResourceEntity>();
                IRelationPredicateBucket filter = new RelationPredicateBucket(AppSefolderResourceFields.FolderId == folderId);
                adapter.FetchEntityCollection(entityList, filter);

                var aDtoList = new List<AppSefolderResourceExDto>();
                foreach (var appSefolderResourceEntity in entityList)
                {
                    aDtoList.Add(AppSefolderResourceConverter.ConvertEntityToExDto(appSefolderResourceEntity));
                }
                return aDtoList;
            }
        }

        public static AppSefolderResourceEntity RetrieveOneAppSefolderResourceEntity(object folderResourceId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSefolderResourceEntity aAppSefolderResourceEntity = new AppSefolderResourceEntity(int.Parse(folderResourceId.ToString()));
                adpater.FetchEntity(aAppSefolderResourceEntity);
                return aAppSefolderResourceEntity;
            }
        }

        public static ObservableSet<AppSefolderResourceDto> RetrieveMultipleAppSefolderResource(int[] folderIds)
        {
            ObservableSet<AppSefolderResourceDto> appSefolderResourceDtoSet = new ObservableSet<AppSefolderResourceDto>();
            EntityCollection<AppSefolderResourceEntity> entityList = new EntityCollection<AppSefolderResourceEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                IRelationPredicateBucket filter = new RelationPredicateBucket();
                filter.PredicateExpression.Add(AppSefolderResourceFields.FolderId == folderIds);
                adapter.FetchEntityCollection(entityList, filter);
            }

            foreach (var AppSefolderResourceEntity in entityList)
            {
                appSefolderResourceDtoSet.Add(AppSefolderResourceConverter.ConvertEntityToDto(AppSefolderResourceEntity));
            }

            return appSefolderResourceDtoSet;
        }


        public static OperationCallResult<AppSefolderResourceExDto> SaveAppSefolderResource(object folderId, ObservableSet<AppSefolderResourceExDto> aAppSefolderResourceExDtoSet)
        {

            OperationCallResult<AppSefolderResourceExDto> aOperationCallResult = new OperationCallResult<AppSefolderResourceExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            // New
            aAppSefolderResourceExDtoSet.FindNewItems().ForAll(o =>
            {
                o.FolderId = int.Parse(folderId.ToString());
                aValidationResult.Merge(SaveAppSefolderResource_ProcessNewfolderResourceExDto(o));
            });

            // Modified
            aAppSefolderResourceExDtoSet.FindModifiedItems().ForAll(o => aValidationResult.Merge(SaveAppSefolderResource_ProcessDirtyAppSefolderResourceExDto(o)));

            // Deleted
            int[] needToDeleteAppSefolderResourceId = aAppSefolderResourceExDtoSet.FindDeletedItemIds().Cast<int>().ToArray();
            aValidationResult.Merge(SaveAppSefolderResource_ProcessDeleteAppSefolderResourceExDto(needToDeleteAppSefolderResourceId));




            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveOneAppSefolderResourceList(folderId);
            }

            return aOperationCallResult;
        }

        private static ValidationResult SaveAppSefolderResource_ProcessNewfolderResourceExDto(AppSefolderResourceExDto aAppSefolderResourceExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppSefolderResourceEntity aAppSefolderResourceEntity = new AppSefolderResourceEntity();
            AppSefolderResourceConverter.CopyDtoToEntity(aAppSefolderResourceEntity, aAppSefolderResourceExDto);



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSefolderResourceEntity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderResourceEntity), "plm_AppSefolderResourceEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderResourceEntity), "plm_AppSefolderResourceEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderResourceEntity), "plm_AppSefolderResourceEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult SaveAppSefolderResource_ProcessDirtyAppSefolderResourceExDto(AppSefolderResourceExDto aAppSefolderResourceExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppSefolderResourceEntity aAppSefolderResourceEntity = RetrieveOneAppSefolderResourceEntity(aAppSefolderResourceExDto.Id);

            AppSefolderResourceConverter.CopyDtoToEntity(aAppSefolderResourceEntity, aAppSefolderResourceExDto);


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSefolderResourceEntity, false, true);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderResourceEntity), "plm_AppSefolderResourceEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderResourceEntity), "plm_AppSefolderResourceEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult SaveAppSefolderResource_ProcessDeleteAppSefolderResourceExDto(int[] appSefolderResourceId)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppSefolderResourceEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppSefolderResourceEntity), new RelationPredicateBucket(AppSefolderResourceFields.FolderResourceId == appSefolderResourceId));
                    adapter.Commit();
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderResourceEntity), "plm_AppSefolderResourceEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        public static OperationCallResult<bool> ApplySecurityToSubFolders(object folderId, ObservableSet<AppSefolderResourceExDto> aAppSefolderResourceExDtoSet, int transactionId)
        {

            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            aValidationResult.Merge(SetSecurityToSubFolders(folderId, aAppSefolderResourceExDtoSet, transactionId));

            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<bool> RemoveSecurityFromSubFolders(object folderId, int? transactionId)
        {

            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            aValidationResult.Merge(SetSecurityToSubFolders(folderId, new ObservableSet<AppSefolderResourceExDto>(), transactionId.Value));

            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }

        private static ValidationResult SetSecurityToSubFolders(object folderId, ObservableSet<AppSefolderResourceExDto> aAppSefolderResourceExDtoSet, int transactionId)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppSefolderEntity rootFolderEntity = RetrieveOneAppSefolderEntity(folderId);

            if (rootFolderEntity != null)
            {
                EntityCollection<AppSefolderEntity> subFolderEntities = new EntityCollection<AppSefolderEntity>();
                EntityCollection<AppSefolderEntity> allFolderEntity = AppSeFolderSecurityBL.RetrieveCurrentUserFolderEntityByTransaction(transactionId);

                RetrieveChildFolders(rootFolderEntity, allFolderEntity, subFolderEntities);

                List<int> subFolderIds = RetrieveSubFolderIds(folderId, transactionId);

                // Delete Subfolder Security
                List<int> needToDeleteAppSefolderResourceIdList = new List<int>();

                foreach (int subFolderId in subFolderIds)
                {
                    foreach (var securityResource in RetrieveOneAppSefolderResourceList(subFolderId))
                    {
                        needToDeleteAppSefolderResourceIdList.Add((int)securityResource.Id);
                    }
                }
                aValidationResult.Merge(SaveAppSefolderResource_ProcessDeleteAppSefolderResourceExDto(needToDeleteAppSefolderResourceIdList.ToArray()));

                // Copy Current Folder seuciryt to all subfolder

                foreach (int subFolderId in subFolderIds)
                {
                    foreach (AppSefolderResourceExDto rootFolderSecurityResoruce in aAppSefolderResourceExDtoSet)
                    {
                        AppSefolderResourceExDto newAppSefolderResourceExDto = rootFolderSecurityResoruce.DeepCopy();
                        newAppSefolderResourceExDto.FolderId = subFolderId;
                        newAppSefolderResourceExDto.Id = null;
                        aValidationResult.Merge(SaveAppSefolderResource_ProcessNewfolderResourceExDto(newAppSefolderResourceExDto));
                    }
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppSefolderEntity), "plm_AppSefolderEntity_CannotFindFolderByID", ValidationItemType.Error, "Cannot find folder by ID."));
            }
            return aValidationResult;
        }


        #endregion






        // Folder Helper
        #region ------- Folder Helper Method ------

        public static AppSefolderDto[] RetrieveFolderHairarchyDto(int transactionId, int? entryFolderId = null)
        {


            // var list = RetrieveAllFolderEntity(transactionId);

            var list = AppSeFolderSecurityBL.RetrieveCurrentUserFolderEntityByTransaction(transactionId);
            var allfoldersTreeItems = new ObservableSet<AppSefolderDto>();
            Dictionary<int, int> dictFolderIdAndContentCount = CountFolderContentByTransactionId(transactionId);

            foreach (var o in list)
            {
                AppSefolderDto aAppSefolderDto = AppSefolderConverter.ConvertEntityToDto(o);

                if (o.UserAvailableActions != null && o.UserAvailableActions.Count == 0)
                {
                    aAppSefolderDto.IsFolderReadonly = true;
                }

                if (dictFolderIdAndContentCount != null && dictFolderIdAndContentCount.ContainsKey((int)aAppSefolderDto.Id))
                {
                    aAppSefolderDto.CountContent = dictFolderIdAndContentCount[(int)aAppSefolderDto.Id];
                }

                allfoldersTreeItems.Add(aAppSefolderDto);
            }

            //   int rootCount = allfoldersTreeItems.Count(f => f.ParentId == null);

            List<int> allFolderIds = allfoldersTreeItems.Select(o => (int)o.Id).ToList();

            AppSefolderDto[] rootFolders = allfoldersTreeItems.Where(f => f.ParentId == null || !allFolderIds.Contains(f.ParentId.Value)).ToArray();

            if (entryFolderId.HasValue)
            {
                rootFolders = allfoldersTreeItems.Where(o => (int)o.Id == entryFolderId.Value).ToArray();
            }

            foreach (var rootFolder in rootFolders)
            {
                rootFolder.FolderPath = rootFolder.Name + "/";
                ProcessChilds(allfoldersTreeItems, rootFolder);
            }

            CalculateFolderEntityCountSubTotal(allfoldersTreeItems.ToList());

            return rootFolders;

        }





        public static List<AppSefolderDto> ConvertFolderHairarchyToFlatList(AppSefolderDto[] folderHairarchy)
        {
            List<AppSefolderDto> flatFolderList = new List<AppSefolderDto>();
            AddFolderHairarchyToFlatList(folderHairarchy, flatFolderList);
            flatFolderList.ForAll(o => o.Children = null);
            return flatFolderList;
        }



        public static List<int> RetrieveSubFolderIds(object folderId, int transactionId)
        {
            List<int> subFolderIdsList = new List<int>();

            EntityCollection<AppSefolderEntity> allFolders = AppSeFolderSecurityBL.RetrieveCurrentUserFolderEntityByTransaction(transactionId);

            Dictionary<int, int?> allFoldersDict = new Dictionary<int, int?>();
            foreach (AppSefolderEntity aFolderEntity in allFolders)
            {
                allFoldersDict.Add(aFolderEntity.FolderId, aFolderEntity.ParentId);
            }

            subFolderIdsList = FindAllSubFolderIds(allFoldersDict, int.Parse(folderId.ToString()), subFolderIdsList);

            return subFolderIdsList;
        }

        //public static List<int> RetrieveSubFolderIds(object folderId, int transactionId)
        //{
        //	List<int> subFolderIdsList = new List<int>();


        //	EntityCollection<AppSefolderEntity> allFolders = AppSeFolderSecurityBL.RetrieveCurrentUserFolderEntityByTransaction(transactionId);

        //	Dictionary<int, int?> allFoldersDict = new Dictionary<int, int?>();
        //	foreach (AppSefolderEntity aFolderEntity in allFolders)
        //	{
        //		allFoldersDict.Add(aFolderEntity.FolderId, aFolderEntity.ParentId);
        //	}

        //	subFolderIdsList = FindAllSubFolderIds(allFoldersDict, int.Parse(folderId.ToString()), subFolderIdsList);

        //	return subFolderIdsList;
        //}


        private static List<int> FindAllSubFolderIds(Dictionary<int, int?> foldersDict, int folderId, List<int> subFolderIdsList)
        {
            //foldersDict.Remove(folderId);

            foreach (KeyValuePair<int, int?> subFolder in foldersDict)
            {
                if (subFolder.Value.HasValue && subFolder.Value.Value == folderId)
                {
                    subFolderIdsList.Add(subFolder.Key);
                    FindAllSubFolderIds(foldersDict, subFolder.Key, subFolderIdsList);
                }
            }

            return subFolderIdsList;
        }

        private static void RetrieveChildFolders(AppSefolderEntity aAppSefolderEntity, EntityCollection<AppSefolderEntity> list, EntityCollection<AppSefolderEntity> toReturn)
        {
            IEnumerable<AppSefolderEntity> childFolders = list.Where(o => o.ParentId.HasValue && o.ParentId.Value == aAppSefolderEntity.FolderId);

            if (childFolders.Count() > 0)
            {
                toReturn.AddRange(childFolders);

                childFolders.ForAll(o =>
                {
                    RetrieveChildFolders(o, list, toReturn);
                });
            }
        }


        public static Dictionary<int, int> CountFolderContentByTransactionId(int? transactionId)
        {

            Dictionary<int, int> dictFolderIdAndContentCount = new Dictionary<int, int>();

            if (!transactionId.HasValue)
            {
                return dictFolderIdAndContentCount;
            }

            string query = null;
            int? fileTransactionId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SystemDefinedFileTransactionId);

            if (fileTransactionId.HasValue && transactionId.Value == fileTransactionId.Value)
            {
                query = @" select FolderID, count(*) as ContentCount from AppFile "
                        + " where FolderID is not null and InitialFileID is null "
                        + " and FileID not in (select RootKeyValueID from AppTrascationRecycleBin where TranscationID = " + transactionId.Value.ToString() + ")"
                        + " group by FolderID";
            }
            else
            {
                var transactionDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

                if (transactionDto != null && transactionDto.RootMasterUnit != null)
                {
                    string tableName = transactionDto.RootMasterUnit.DataBaseTableName;
                    string rootPkColumnName = transactionDto.RootMasterUnit.PrimaryKeyDbfieldList.FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(tableName) && !string.IsNullOrWhiteSpace(rootPkColumnName))
                    {
                        query = @" select FolderID, count(*) as ContentCount from "
                                + tableName
                                + " where FolderID is not null "
                                + " and " + rootPkColumnName
                                + " not in (select RootKeyValueID from AppTrascationRecycleBin where TranscationID = " + transactionId.Value.ToString() + ")"
                                + " group by FolderID";
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return dictFolderIdAndContentCount;
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    List<SqlParameter> lsitparamter = new List<SqlParameter>();
                    DataTable dt = adapter.ExecuteDataTableRetrievalQuery(query, lsitparamter);

                    foreach (DataRow dataRow in dt.Rows)
                    {
                        int? folderId = ControlTypeValueConverter.ConvertValueToInt(dataRow["FolderID"].ToString());
                        int? contentCount = ControlTypeValueConverter.ConvertValueToInt(dataRow["ContentCount"].ToString());

                        if (folderId.HasValue && contentCount.HasValue)
                        {
                            dictFolderIdAndContentCount.Add(folderId.Value, contentCount.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }

            return dictFolderIdAndContentCount;
        }


        private static void CalculateFolderEntityCountSubTotal(List<AppSefolderDto> folderList)
        {
            if (folderList != null)
            {
                Dictionary<int, AppSefolderDto> dictFolderIdFolderDto = folderList.ToDictionary(o => (int)o.Id, o => o);
                Dictionary<int, int?> dictFolderIdParentId = folderList.ToDictionary(o => (int)o.Id, o => o.ParentId);

                foreach (var folderDto in folderList.Where(o => !o.ParentId.HasValue))
                {
                    CalculateOneFolderEntityCountSubTotal((int)folderDto.Id, dictFolderIdFolderDto, dictFolderIdParentId);
                }

                foreach (var folderDto in folderList)
                {
                    folderDto.CountContent = folderDto.CountContentSubTotal;
                }
            }

        }

        private static void CalculateOneFolderEntityCountSubTotal(int folderId, Dictionary<int, AppSefolderDto> dictFolderIdFolderDto, Dictionary<int, int?> dictFolderIdParentId)
        {
            if (dictFolderIdFolderDto.ContainsKey(folderId))
            {

                AppSefolderDto folderDto = dictFolderIdFolderDto[folderId];

                if (folderDto.CountContentSubTotal > 0)
                {
                    return;
                }
                else
                {
                    folderDto.CountContentSubTotal = folderDto.CountContent;

                    List<int> subFolderIds = dictFolderIdParentId.Where(o => o.Value.HasValue && o.Value.Value == folderId).Select(o => o.Key).ToList();

                    foreach (int aSubFolderId in subFolderIds)
                    {
                        if (dictFolderIdFolderDto.ContainsKey(aSubFolderId))
                        {
                            CalculateOneFolderEntityCountSubTotal(aSubFolderId, dictFolderIdFolderDto, dictFolderIdParentId);
                            folderDto.CountContentSubTotal += dictFolderIdFolderDto[aSubFolderId].CountContentSubTotal;
                        }
                    }
                }
            }

        }

        #endregion






    }
}