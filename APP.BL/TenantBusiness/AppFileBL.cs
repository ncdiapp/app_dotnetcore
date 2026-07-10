using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using APP.Framework;
namespace App.BL
{
    public static class AppFileBL 
    {
        public const string fileIdPrefix = "FileId_";

        private static readonly List<AppTransactionFieldExDto> _ImageAndFileTransactionFieldList = null;

        public static EntityCollection<AppFileEntity> RetrieveAllAppFileEntity()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppFileEntity> list = new EntityCollection<AppFileEntity>();
                SortClause aSortClause = AppFileFields.FileCode | SortOperator.Ascending;

                adapter.FetchEntityCollection(list, null, 0, new SortExpression(aSortClause), null);

                return list;
            }
        }

        public static EntityCollection<AppFileEntity> RetrieveAllAppFileEntityForFolders(int[] fodlerIds)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppFileEntity> list = new EntityCollection<AppFileEntity>();

                ExcludeFieldsList excludeFieldsList = new ExcludeFieldsList();
                excludeFieldsList.Add(AppFileFields.FileContent);

                IPrefetchPath2 root = new PrefetchPath2(EntityType.AppFileEntity);
                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppFileFields.FolderId == fodlerIds);
                adapter.FetchEntityCollection(list, aFilter, 0, null, root, excludeFieldsList);


                return list;
            }
        }


        public static EntityCollection<AppFileEntity> RetrieveMyOwnAllAppFileEntity()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppFileEntity> list = new EntityCollection<AppFileEntity>();

                ExcludeFieldsList excludeFieldsList = new ExcludeFieldsList();
                excludeFieldsList.Add(AppFileFields.FileContent);

                IPrefetchPath2 root = new PrefetchPath2(EntityType.AppFileEntity);
                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppFileFields.AppCreatedById == AppSecurityUserBL.CurrentUserId);
                adapter.FetchEntityCollection(list, aFilter, 0, null, root, excludeFieldsList);


                return list;
            }
        }

        /// <summary>
        /// Batch-resolve thumbnail static resource URLs for search grid image columns.
        /// Returns map of FileId → /api/resources/Company_{id}/Image/thumbnail/{guid}.
        /// </summary>
        public static Dictionary<int, string> RetrieveThumbnailResourceUrlsByFileIds(IEnumerable<int> fileIds)
        {
            return RetrieveSearchImageResourceUrlsByFileIds(fileIds).ThumbnailUrls;
        }

        /// <summary>
        /// Batch-resolve regular (card) image static resource URLs for search image columns.
        /// Returns map of FileId → /api/resources/Company_{id}/Image/regular/{guid}.
        /// </summary>
        public static Dictionary<int, string> RetrieveImageResourceUrlsByFileIds(IEnumerable<int> fileIds)
        {
            return RetrieveSearchImageResourceUrlsByFileIds(fileIds).ImageUrls;
        }

        /// <summary>
        /// One AppFile batch lookup → thumbnail + regular image resource URLs for search results.
        /// Thumbnail prefers Thumbnail → Regular → Original; Image prefers Regular → Original → Thumbnail.
        /// </summary>
        public static (Dictionary<int, string> ThumbnailUrls, Dictionary<int, string> ImageUrls)
            RetrieveSearchImageResourceUrlsByFileIds(IEnumerable<int> fileIds)
        {
            var thumbnailUrls = new Dictionary<int, string>();
            var imageUrls = new Dictionary<int, string>();
            var idList = fileIds?.Where(id => id > 0).Distinct().ToList();
            if (idList == null || idList.Count == 0)
                return (thumbnailUrls, imageUrls);

            int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
            if (!companyId.HasValue)
                return (thumbnailUrls, imageUrls);

            string companySegment = $"Company_{companyId.Value}";

            EntityCollection<AppFileEntity> entities = RetrieveMultipleFileEntityByIds(idList, true);
            foreach (AppFileEntity entity in entities)
            {
                string? thumbPath = BuildCompanyImageResourcePath(companySegment, entity.ThumbnailFilePath)
                    ?? BuildCompanyImageResourcePath(companySegment, entity.RegularImageFilepath)
                    ?? BuildCompanyImageResourcePath(companySegment, entity.OriginalFilePath);
                if (thumbPath != null)
                    thumbnailUrls[entity.FileId] = $"/api/resources/{thumbPath}";

                string? imagePath = BuildCompanyImageResourcePath(companySegment, entity.RegularImageFilepath)
                    ?? BuildCompanyImageResourcePath(companySegment, entity.OriginalFilePath)
                    ?? BuildCompanyImageResourcePath(companySegment, entity.ThumbnailFilePath);
                if (imagePath != null)
                    imageUrls[entity.FileId] = $"/api/resources/{imagePath}";
            }

            return (thumbnailUrls, imageUrls);
        }

        /// <summary>
        /// Maps AppFile image path (e.g. /thumbnail/guid) to FileRepository-relative resource path.
        /// </summary>
        internal static string? BuildCompanyImageResourcePath(string companySegment, string? imageRelativePath)
        {
            if (string.IsNullOrWhiteSpace(imageRelativePath))
                return null;

            string normalized = imageRelativePath.TrimStart('/', '\\').Replace('\\', '/');
            return $"{companySegment}/Image/{normalized}";
        }

        //	select FileID from AppFile where FolderID in (1232,121)

        public static EntityCollection<AppFileEntity> RetrieveMultipleFileEntityByIds(List<int> fileIdList, bool isRetrieveSimpleData = true)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppFileEntity> list = new EntityCollection<AppFileEntity>();

                ExcludeFieldsList excludeFieldsList = new ExcludeFieldsList();

                if (isRetrieveSimpleData)
                {
                    excludeFieldsList.Add(AppFileFields.FileContent);
                }

                if (fileIdList != null && fileIdList.Count > 0)
                {
                    IPrefetchPath2 root = new PrefetchPath2(EntityType.AppFileEntity);
                    IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppFileFields.FileId == fileIdList.ToArray());
                    adapter.FetchEntityCollection(list, aFilter, 0, null, root, excludeFieldsList);
                }

                return list;
            }
        }



        public static ObservableSet<AppFileExDto> RetrieveAllAppFileEntityDto()
        {
            ObservableSet<AppFileExDto> aSet = new ObservableSet<AppFileExDto>();
            EntityCollection<AppFileEntity> list = RetrieveAllAppFileEntity();
            foreach (var o in list)
            {
                AppFileExDto aDto = AppFileConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        private static AppFileEntity RetrieveOneAppFileEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppFileEntity aAppFileEntity = new AppFileEntity(int.Parse(Id.ToString()));
                // FetchEntity returns false when the row does not exist. Returning the unfetched
                // (out-of-sync) entity would make any property access throw ORMEntityOutOfSyncException
                // (surfacing as a 500). Return null so callers can respond with NotFound instead.
                if (!adapter.FetchEntity(aAppFileEntity))
                {
                    return null;
                }
                return aAppFileEntity;
            }
        }

        public static int? RollBackFileVersion(int Id)
        {
            bool isAllowUploadEditVersion = CheckUploadEditFilePermission(Id);

            if (!isAllowUploadEditVersion)
            {
                return null;
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "versionRooback");

                    AppFileEntity aAppFileEntity = new AppFileEntity(Id);
                    adapter.FetchEntity(aAppFileEntity);

                    var fielDto = AppFileConverter.ConvertEntityToDto(aAppFileEntity);
                    fielDto.Id = null;
                    AppFileEntity aNewAppFileEntity = new AppFileEntity();
                    AppFileConverter.CopyDtoToEntity(aNewAppFileEntity, fielDto);


                    if (aAppFileEntity.InitialFileId.HasValue)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppFileEntity), new RelationPredicateBucket(AppFileFields.FileId == Id));

                    }
                    else //  (aAppFileEntity.InitialFileId is null come from root, dont delete root
                    {
                        aNewAppFileEntity.InitialFileId = Id;
                    }

                    adapter.SaveEntity(aNewAppFileEntity);

                    adapter.Commit();

                    return aNewAppFileEntity.FileId;

                    //return aAppFileEntity;
                }
                catch (Exception ex)
                {
                    adapter.Rollback();
                    return null;
                }
            }
        }

        public static AppFileExDto RetrieveOneOrgAppFileExDto(object Id)
        {
            AppFileEntity aOrgAppFileEntity = RetrieveOneAppFileEntity(Id);
            if (aOrgAppFileEntity == null)
            {
                return null;
            }


            // it has  initalefileID

            int? initialFileId = aOrgAppFileEntity.InitialFileId;

            if (initialFileId.HasValue)
            {
                AppFileEntity aInitAppFileEntity = RetrieveOneAppFileEntity(initialFileId.Value);

                if (aInitAppFileEntity == null || !CheckDownloadFilePermission(aInitAppFileEntity))
                {
                    return null;
                    //throw new Exception("Invalid Request");
                }


            }
            else // it is initial file 
            {


                if (!CheckDownloadFilePermission(aOrgAppFileEntity))
                {
                    return null;
                    //throw new Exception("Invalid Request");
                }

            }




            AppFileExDto aAppFileExDto = AppFileConverter.ConvertEntityToExDto(aOrgAppFileEntity);

            return aAppFileExDto;
        }

        ////  block file url securtiy load 
        //1: need to check if this file is delete or not ( recycle bin
        //2: need to check if this file current onwer ( do)
        //3: need to check folder security
        //4: shared filed security 
        //5: disable public folder
        //6:  create a file--> sahre to other with wriiten permission--> other peopel modify file--> upload to drop box --version history

        public static AppFileExDto RetrieveOneLatestAppFileExDto(object Id)
        {

            // need to check  security first !!


            AppFileEntity aAppFileEntity = RetrieveOneAppFileEntity(Id);
            if (aAppFileEntity == null || !CheckDownloadFilePermission(aAppFileEntity))
            {
                return null;
                //throw new Exception("Invalid Request");
            }
                ;


            // it has  initalefileID

            int? initialFileId = aAppFileEntity.InitialFileId;

            if (!initialFileId.HasValue)
            {
                initialFileId = aAppFileEntity.FileId;
            }

            if (initialFileId.HasValue)
            {
                string query = @" SELECT        MAX(FileID) 
                               FROM            dbo.AppFile 
                               WHERE   InitialFileID  =@InitialFileId";
                List<SqlParameter> paramters = new List<SqlParameter>();
                paramters.Add(new SqlParameter("@InitialFileId", initialFileId.Value));

                int? latesFiedId = null;
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    latesFiedId = ControlTypeValueConverter.ConvertValueToInt(adapter.ExecuteScalarQuery(query, paramters));
                }
                if (latesFiedId.HasValue)
                {
                    var latestEntity = RetrieveOneAppFileEntity(latesFiedId);
                    if (latestEntity != null)
                    {
                        aAppFileEntity = latestEntity;
                    }
                }

            }


            AppFileExDto aAppFileExDto = AppFileConverter.ConvertEntityToExDto(aAppFileEntity);

            return aAppFileExDto;
        }

        private static bool CheckDownloadFilePermission(AppFileEntity aAppFileEntity)
        {
            int fileId = aAppFileEntity.FileId;

            bool isAllowReadFile = false;

            // Check 

            int? fileTransactionId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SystemDefinedFileTransactionId);
            AppTrascationRecycleBinEntity recycleBinEntity = fileTransactionId.HasValue
                ? AppTransactionRecycleBL.CheckIfTranscationRootValueInRecycleBin(fileTransactionId.Value, fileId)
                : null;

            // Not in recycle bin..
            if (recycleBinEntity == null)
            {

                bool isAdmin = AppSecurityUserBL.IsAdminUser();

                if (isAdmin)
                {
                    isAllowReadFile = true;
                }
                else
                {
                    if (aAppFileEntity.AppCreatedById == AppSecurityUserBL.CurrentUserId)
                    {
                        isAllowReadFile = true;
                    }
                    else if (CheckIfFileShareByCurrentUser(fileId))
                    {
                        isAllowReadFile = true;

                    }
                    else if (CheckIfFileFolderShareByCurrentUser(aAppFileEntity))
                    {
                        isAllowReadFile = true;

                    }
                }
            }


            return isAllowReadFile;
        }

        public static bool CheckUploadEditFilePermission(object fileObjId)
        {

            var aExsitngAppFileEntity = GetInitialFileEntityWithoutContent(fileObjId);

            int fileId = aExsitngAppFileEntity.FileId;

            bool isAllowUploadEditVersion = false;

            // Check 

            int? fileTransactionId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SystemDefinedFileTransactionId);
            AppTrascationRecycleBinEntity recycleBinEntity = AppTransactionRecycleBL.CheckIfTranscationRootValueInRecycleBin(fileTransactionId.Value, fileId);

            // Not in Recycel bin
            if (recycleBinEntity == null)
            {
                bool isAdmin = AppSecurityUserBL.IsAdminUser();

                if (isAdmin)
                {
                    isAllowUploadEditVersion = true;
                }
                else
                {
                    if (aExsitngAppFileEntity.AppCreatedById == AppSecurityUserBL.CurrentUserId)
                    {
                        isAllowUploadEditVersion = true;
                    }
                    else if (CheckIfFileShareModifyByCurrentUser(fileId))
                    {
                        isAllowUploadEditVersion = true;

                    }
                    else if (CheckIfUploadFileFolderShareByCurrentUser(aExsitngAppFileEntity))
                    {
                        isAllowUploadEditVersion = true;
                    }
                }

            }


            return isAllowUploadEditVersion;
        }



        private static bool CheckIfUploadFileFolderShareByCurrentUser(AppFileEntity aAppFileEntity)
        {
            int? transactiId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SystemDefinedFileTransactionId);

            bool isEnablFoder = AppSeFolderSecurityBL.CheckIfFolderTransactionEnableSecurity(transactiId);

            if (!isEnablFoder)
            {
                return true;
            }



            int fodlerId = aAppFileEntity.FolderId.Value;

            // 

            EntityCollection<AppSefolderResourceEntity> list = new EntityCollection<AppSefolderResourceEntity>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                RelationPredicateBucket filter = new RelationPredicateBucket(AppSefolderResourceFields.FolderId == fodlerId);
                filter.PredicateExpression.AddWithAnd(
                       AppSefolderResourceFields.UserId == AppSecurityUserBL.CurrentUserId |
                       AppSefolderResourceFields.RoleId == AppSecurityUserBL.CurrentGroupIds
                    );
                adapter.FetchEntityCollection(list, filter);
            }
            if (list.Count > 0)
            {
                var reaonlifList = list.Where(o => o.IsReadOnly.HasValue && o.IsReadOnly.Value).ToList();

                if (reaonlifList.Count > 0)
                {
                    return false;
                }
                else
                {
                    return true;

                }

            }
            else
            {
                return false;
            }




        }


        private static bool CheckIfFileFolderShareByCurrentUser(AppFileEntity aAppFileEntity)
        {
            int? transactiId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SystemDefinedFileTransactionId);

            bool isEnablFoder = AppSeFolderSecurityBL.CheckIfFolderTransactionEnableSecurity(transactiId);

            if (!isEnablFoder)
            {
                return true;
            }



            int fodlerId = aAppFileEntity.FolderId.Value;

            // 

            EntityCollection<AppSefolderResourceEntity> list = new EntityCollection<AppSefolderResourceEntity>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                RelationPredicateBucket filter = new RelationPredicateBucket(AppSefolderResourceFields.FolderId == fodlerId);
                filter.PredicateExpression.AddWithAnd(
                       AppSefolderResourceFields.UserId == AppSecurityUserBL.CurrentUserId |
                       AppSefolderResourceFields.RoleId == AppSecurityUserBL.CurrentGroupIds
                    );
                adapter.FetchEntityCollection(list, filter);
            }
            if (list.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }




        }

        private static bool CheckIfFileShareModifyByCurrentUser(int fileId)
        {
            EntityCollection<AppFileOrFolderShareToOtherEntity> list = GetFileShareByCurrentUser(fileId);

            var listCanwrite = list.Where(o => o.IsCanWrite.HasValue && o.IsCanWrite.Value).ToList();

            if (listCanwrite.Count > 0)
            {
                return true;
            }

            return false;
        }


        private static bool CheckIfFileShareByCurrentUser(int fileId)
        {
            EntityCollection<AppFileOrFolderShareToOtherEntity> list = GetFileShareByCurrentUser(fileId);
            if (list.Count > 0)
            {
                return true;
            }

            return false;
        }



        private static EntityCollection<AppFileOrFolderShareToOtherEntity> GetFileShareByCurrentUser(int fileId)
        {
            EntityCollection<AppFileOrFolderShareToOtherEntity> list = new EntityCollection<AppFileOrFolderShareToOtherEntity>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                RelationPredicateBucket filter = new RelationPredicateBucket(AppFileOrFolderShareToOtherFields.FileId == fileId);
                filter.PredicateExpression.AddWithAnd(AppFileOrFolderShareToOtherFields.ShareToOtherUserId == AppSecurityUserBL.CurrentUserId | AppFileOrFolderShareToOtherFields.ShareToOtherRoleId == AppSecurityUserBL.CurrentGroupIds);
                adapter.FetchEntityCollection(list, filter);
            }

            return list;
        }

        public static AppFileEntity RetrieveOneFileEntityWithoutContenct(object fileId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppFileEntity> list = new EntityCollection<AppFileEntity>();
                AppFileEntity aAppFileEntity = new AppFileEntity(int.Parse(fileId.ToString()));
                ExcludeFieldsList excludeFieldsList = new ExcludeFieldsList();

                excludeFieldsList.Add(AppFileFields.FileContent);

                adapter.FetchEntity(aAppFileEntity);
                return aAppFileEntity;
            }
        }



        internal static AppFileEntity GetInitialFileEntityWithoutContent(object fileId)
        {
            AppFileEntity aAppFileEntity = RetrieveOneFileEntityWithoutContenct(fileId);
            if (aAppFileEntity.InitialFileId.HasValue)
            {
                aAppFileEntity = RetrieveOneFileEntityWithoutContenct(aAppFileEntity.InitialFileId.Value);
            }

            return aAppFileEntity;
        }

        public static List<AppFileExDto> RetrieveMultipleFileExDtoByIds(List<int> fileIdList)
        {
            List<AppFileExDto> dtoList = new List<AppFileExDto>();
            EntityCollection<AppFileEntity> list = RetrieveMultipleFileEntityByIds(fileIdList, false);
            foreach (var o in list)
            {
                AppFileExDto aDto = AppFileConverter.ConvertEntityToExDto(o);
                dtoList.Add(aDto);
            }

            return dtoList;
        }

        public static List<AppFileDto> RetrieveMultipleFileSimpleDtoByIds(List<int> fileIdList)
        {
            List<AppFileDto> dtoList = new List<AppFileDto>();
            EntityCollection<AppFileEntity> list = RetrieveMultipleFileEntityByIds(fileIdList);
            foreach (var o in list)
            {
                AppFileDto aDto = AppFileConverter.ConvertEntityToDto(o);
                dtoList.Add(aDto);
            }

            return dtoList;
        }

        public static AppFileDto RetrieveOneFileByCreatdByAndFileName(string FileCode, int TargetFolderId)
        {
            EntityCollection<AppFileEntity> list = new EntityCollection<AppFileEntity>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {


                ExcludeFieldsList excludeFieldsList = new ExcludeFieldsList();
                excludeFieldsList.Add(AppFileFields.FileContent);

                IPrefetchPath2 root = new PrefetchPath2(EntityType.AppFileEntity);
                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppFileFields.AppCreatedById == AppSecurityUserBL.CurrentUserId);
                aFilter.PredicateExpression.AddWithAnd(AppFileFields.FileCode == FileCode);
                aFilter.PredicateExpression.AddWithAnd(AppFileFields.FolderId == TargetFolderId);


                adapter.FetchEntityCollection(list, aFilter, 0, null, root, excludeFieldsList);



            }

            if (list.Count > 0)
            {
                return AppFileConverter.ConvertEntityToDto(list[0]);
            }
            else
            {
                return null;
            }

        }

        public static List<AppFileDto> GetOneFolderLatestFileList(int? folderId, int? transactionId)
        {
            List<AppFileDto> toRetrun = new List<AppFileDto>();
            AppSearchViewEntity viewEntity = GetTransNavigationSearchViewEntity(transactionId);

            var dataTableResult = AppStaticDataSetSearchBL.RetriveFolderSearchViewDataTable(folderId, viewEntity, transactionId);

            var fieldIdColumn = viewEntity.AppSearchViewField.Where(o => o.IsTransRootId.HasValue && o.IsTransRootId.Value).FirstOrDefault();

            //if(fieldIdColumn ==null)
            //{
            //	return toRetrun;

            //}


            List<int> fileIds = dataTableResult.AsEnumerable().Select(o => (int)o[fieldIdColumn.SysTableFiledPath]).ToList();



            if (fileIds.IsEmpty())
                return toRetrun;

            EntityCollection<AppFileEntity> list = new EntityCollection<AppFileEntity>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {


                ExcludeFieldsList excludeFieldsList = new ExcludeFieldsList();
                excludeFieldsList.Add(AppFileFields.FileContent);

                IPrefetchPath2 root = new PrefetchPath2(EntityType.AppFileEntity);
                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppFileFields.FileId == fileIds);




                adapter.FetchEntityCollection(list, aFilter, 0, null, root, excludeFieldsList);



            }

            foreach (var entity in list)
            {

                toRetrun.Add(AppFileConverter.ConvertEntityToDto(entity));
            }


            return toRetrun;


        }

        //need to Cache
        public static AppSearchViewEntity GetTransNavigationSearchViewEntity(int? transactionId)
        {
            //var trans = AppCacheManagerBL.GetOnetTranscationEntityFromCache(transactionId);
            //var trans = AppCacheManagerBL.
            if (transactionId.HasValue)
            {
                var navigationviewEntity = AppTransactionNavigationBL.GetTransactionNavigationEntityList(new List<int>() { transactionId.Value }).Where(o => o.IsDefaultView.HasValue && o.IsDefaultView.Value).FirstOrDefault();

                AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(navigationviewEntity.FolderViewId);
                return viewEntity;
            }

            return null;
        }

        public static OperationCallResult<AppFileExDto> SaveAllAppFileEntityDto(ObservableSet<AppFileExDto> aSet)
        {
            OperationCallResult<AppFileExDto> aOperationCallResult = new OperationCallResult<AppFileExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            foreach (var newItemDto in aSet.FindNewItems())
            {
                var result = ProcessNewDto(newItemDto);
                validationResult.Merge(result);
            }

            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(DeleteOneAppFileEntity(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveAllAppFileEntityDto();
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppFileExDto> SaveOneAppFileEntityDto(AppFileExDto aAppFileExDto)
        {
            OperationCallResult<AppFileExDto> aOperationCallResult = new OperationCallResult<AppFileExDto>();

            var aValidationResult = aAppFileExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (aAppFileExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(aAppFileExDto));
            }
            else if (aAppFileExDto.IsModified)
            {
                validationResult.Merge(ProcessDirtyDto(aAppFileExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                var entity = AppFileBL.RetrieveOneAppFileEntity(aAppFileExDto.Id);
                aOperationCallResult.Object = AppFileConverter.ConvertEntityToExDto(entity);
            }

            return aOperationCallResult;
        }

        //  RetrieveAllAppFileEntityDto(aAppFileExDto.Id )

        public static OperationCallResult<object> DeleteOneAppFileEntityDto(object Id)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            ValidationResult avalidationResult = DeleteOneAppFileEntity(Id);
            aOperationCallResult.ValidationResult = avalidationResult;
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = Id;
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<bool> DeleteAppFileDtoByIds(List<int> fileIdList)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();

            ValidationResult avalidationResult = DeleteAppFileEntityByIds(fileIdList);
            aOperationCallResult.ValidationResult = avalidationResult;

            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }

        //??????
        public static object RetrieveFileWhereUsedInForm(List<int> fileIdList)
        {
            List<FileWhereUsedInFormDto> whereUsedList = new List<FileWhereUsedInFormDto>();

            if (!fileIdList.IsEmpty())
            {
                var transFiledList = GetImageAndFileTransactionFields();

                if (!transFiledList.IsEmpty())
                {
                    foreach (int fileId in fileIdList)
                    {
                        foreach (var transFiledDto in transFiledList)
                        {
                            if (!string.IsNullOrEmpty(transFiledDto.DataBaseTableName) && !string.IsNullOrEmpty(transFiledDto.DataBaseFieldName))
                            {
                                string pkFieldName = "''";
                                string fkFieldName = "''";

                                if (transFiledDto.ForeignAppEntityInfoExDto != null)
                                {
                                }

                                string queryStringFormat = @" select " + transFiledDto.DataBaseFieldName + ", " + pkFieldName + " as PK, " + fkFieldName + " as FK "
                                    + " From " + transFiledDto.DataBaseTableName
                                    + " where  " + transFiledDto.DataBaseFieldName + "IN ({0})";

                                List<SqlParameter> lsitparamter = new List<SqlParameter>();
                                string[] paramNames = fileIdList.Select((s, i) => "@filedValue" + i.ToString()).ToArray();
                                string inClause = string.Join(", ", paramNames);

                                for (int i = 0; i < paramNames.Length; i++)
                                {
                                    lsitparamter.Add(new SqlParameter(paramNames[i], fileIdList[i]));
                                }

                                string query = string.Format(queryStringFormat, inClause);

                                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                                {
                                    DataTable result = adapter.ExecuteDataTableRetrievalQuery(query, lsitparamter);
                                }
                            }
                        }
                    }
                }
            }

            return whereUsedList;
        }

        public static List<AppTransactionFieldExDto> GetImageAndFileTransactionFields()
        {
            if (_ImageAndFileTransactionFieldList != null)
            {
                return _ImageAndFileTransactionFieldList;
            }
            else
            {
                List<int> controlTypes = new List<int> { (int)EmAppControlType.File, (int)EmAppControlType.Image };
                return AppTransactionBL.RetrieveTransactionFieldsByControlTypes(controlTypes);
            }
        }

        public static KeyValuePair<int?, string> GetFileIdAndNamePairFromFileIdNameStrng(string fileIdAndNameString)
        {
            KeyValuePair<int?, string> fileIdNamePair = new KeyValuePair<int?, string>();

            if (!string.IsNullOrWhiteSpace(fileIdAndNameString))
            {
                fileIdAndNameString = fileIdAndNameString.Trim();

                if (fileIdAndNameString.StartsWith(fileIdPrefix))
                {
                    fileIdAndNameString = fileIdAndNameString.Substring(7);

                    int index = fileIdAndNameString.IndexOf("_");

                    if (index > 0)
                    {
                        int? fileId = ControlTypeValueConverter.ConvertValueToInt(fileIdAndNameString.Substring(0, index));
                        string fileName = fileIdAndNameString.Substring(index + 1);

                        if (fileId.HasValue && !string.IsNullOrWhiteSpace(fileName))
                        {
                            fileIdNamePair = new KeyValuePair<int?, string>(fileId.Value, fileName.Trim());
                        }
                    }
                }
            }

            return fileIdNamePair;
        }

        public static List<AppFileDto> GetFileUpdateHistoryDtoListByFileId(int? fileId)
        {
            List<AppFileDto> historyFileDtoList = new List<AppFileDto>();

            if (fileId.HasValue)
            {
                List<int> hisotryFileIdList = new List<int>();

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        List<SqlParameter> lsitparamter = new List<SqlParameter>();
                        lsitparamter.Add(new SqlParameter("@fileId", fileId.Value));

                        string query = @"  SELECT [FileID]
                                        FROM AppFile
                                        where FileID = (
	                                        SELECT FileID from AppFile WHERE InitialFileID is null AND FileID IN (
		                                        select @fileId
		                                        union
		                                        (select InitialFileID from AppFile where FileID = @fileId)
	                                        )
                                        )
                                        OR
                                        InitialFileID = (
	                                        SELECT FileID from AppFile WHERE InitialFileID is null AND FileID IN (
		                                        select @fileId
		                                        union
		                                        (select InitialFileID from AppFile where FileID = @fileId)
	                                        )
                                        )";

                        DataTable dt = adapter.ExecuteDataTableRetrievalQuery(query, lsitparamter);

                        foreach (DataRow dataRow in dt.Rows)
                        {
                            int? aHistoryFileId = ControlTypeValueConverter.ConvertValueToInt(dataRow["FileID"].ToString());
                            if (aHistoryFileId.HasValue)
                            {
                                hisotryFileIdList.Add(aHistoryFileId.Value);
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                if (hisotryFileIdList.Count > 0)
                {
                    historyFileDtoList = RetrieveMultipleFileSimpleDtoByIds(hisotryFileIdList);
                }
            }

            return historyFileDtoList;
        }

        private static ValidationResult DeleteAppFileEntityByIds(List<int> fileIdList)
        {
            ValidationResult aValidationResult = new ValidationResult();

            if (fileIdList != null && fileIdList.Count > 0)
            {
                EntityCollection<AppFileEntity> fileEntityList = RetrieveMultipleFileEntityByIds(fileIdList);
                try
                {
                    foreach (AppFileEntity fileEntity in fileEntityList)
                    {
                        if (!string.IsNullOrEmpty(fileEntity.OriginalFilePath))
                        {
                            string fileFullPathName = AppDomain.CurrentDomain.BaseDirectory + "FileRepository" + fileEntity.OriginalFilePath;
                            var uri = new Uri(fileFullPathName, UriKind.Absolute);
                            System.IO.File.Delete(uri.LocalPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFileExDto), "App_AppFileEntity_FileDelete_Error", ValidationItemType.Warning, "Remove physical file from server failed."));
                }

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppFileEntity");
                        adapter.DeleteEntitiesDirectly(typeof(AppFileEntity), new RelationPredicateBucket(AppFileFields.FileId == fileIdList.ToArray()));
                        adapter.Commit();
                    }

                    // Database FK Exception .......
                    catch (ORMQueryExecutionException ex)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppFileExDto), "App_AppFileEntity_FileDelete_Error", ValidationItemType.Error, ex.ToString()));

                        adapter.Rollback();
                    }
                }
            }
            return aValidationResult;
        }

        private static ValidationResult ProcessNewDto(AppFileExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();




            AppFileEntity aParentAppFileEntity = new AppFileEntity();

            AppFileConverter.CopyDtoToEntity(aParentAppFileEntity, aDto);

            if (aParentAppFileEntity.FolderId.HasValue && !aParentAppFileEntity.InitialFileId.HasValue)
            {
                var folderEntity = AppSeFolderBL.RetrieveOneAppSefolderEntity(aParentAppFileEntity.FolderId.Value);

                if (folderEntity != null && folderEntity.AppCreatedById.HasValue)
                {
                    aParentAppFileEntity.AppCreatedById = folderEntity.AppCreatedById.Value;
                }

            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppFileEntity);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFileExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aDto.Id = aParentAppFileEntity.FileId;
                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFileExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppFileExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppFileEntity aAppFileEntity = RetrieveOneAppFileEntity(aDto.Id);

            AppFileConverter.CopyDtoToEntity(aAppFileEntity, aDto);

            // bug test
            // aDto.Id = 100;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppFileEntity, false, true);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFileExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFileExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult DeleteOneAppFileEntity(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppFileEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppFileEntity), new RelationPredicateBucket(AppFileFields.FileId == Id));
                    adapter.Commit();
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFileExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }
    }
}