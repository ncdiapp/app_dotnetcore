using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
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
using Google.Protobuf.WellKnownTypes;

using APP.Framework;
namespace App.BL
{
    public static class AppTransactionDataTransferSettingBL
    {
        public static EntityCollection<AppTransactionDataTransferSettingEntity> RetrieveAllAppTransactionDataTransferSettingEntity(int? srcTransactionId = null, int? destinationTransactionId = null)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionDataTransferSettingEntity> list = new EntityCollection<AppTransactionDataTransferSettingEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket();

                if (srcTransactionId.HasValue)
                {
                    filter.PredicateExpression.AddWithAnd(AppTransactionDataTransferSettingFields.TransactionId == srcTransactionId.Value);
                }

                if (destinationTransactionId.HasValue)
                {
                    filter.PredicateExpression.AddWithOr(AppTransactionDataTransferSettingFields.DestinationTransactionId == destinationTransactionId.Value);
                }

                adapter.FetchEntityCollection(list, filter);

                return list;
            }
        }


        public static AppTransactionDataTransferSettingEntity RetrieveOneAppTransactionDataTransferSettingEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionDataTransferSettingEntity aAppTransactionDataTransferSettingEntity = new AppTransactionDataTransferSettingEntity(int.Parse(Id.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppTransactionDataTransferSettingEntity);
                rootPath.Add(AppTransactionDataTransferSettingEntity.PrefetchPathAppTransactionSaveAsMapping);
                //rootPath.Add(AppTransactionDataTransferSettingEntity.PrefetchPathAppTransactionSaveAsMapping);

                adapter.FetchEntity(aAppTransactionDataTransferSettingEntity, rootPath);

                return aAppTransactionDataTransferSettingEntity;
            }
        }

        public static ObservableSet<AppTransactionDataTransferSettingExDto> RetrieveAllAppTransactionDataTransferSettingExDto(int? transactionId, int? destinationTransactionId)
        {
            ObservableSet<AppTransactionDataTransferSettingExDto> aSet = new ObservableSet<AppTransactionDataTransferSettingExDto>();
            EntityCollection<AppTransactionDataTransferSettingEntity> list = RetrieveAllAppTransactionDataTransferSettingEntity(transactionId, destinationTransactionId);

            foreach (var o in list)
            {
                AppTransactionDataTransferSettingExDto aDto = AppTransactionDataTransferSettingConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        public static AppTransactionDataTransferSettingExDto RetrieveOneAppTransactionDataTransferSettingExDto(object Id)
        {
            AppTransactionDataTransferSettingEntity aAppTransactionDataTransferSettingEntity = RetrieveOneAppTransactionDataTransferSettingEntity(Id);
            AppTransactionDataTransferSettingExDto aSettingExDto = AppTransactionDataTransferSettingConverter.ConvertEntityToExDto(aAppTransactionDataTransferSettingEntity);

            aSettingExDto.ApiResponseMappingList = new List<AppTransactionSaveAsMappingExDto>();
            aSettingExDto.DataTransferMappingList = new List<AppTransactionSaveAsMappingExDto>();

            foreach (var o in aAppTransactionDataTransferSettingEntity.AppTransactionSaveAsMapping)
            {
                AppTransactionSaveAsMappingExDto detailExDto = AppTransactionSaveAsMappingConverter.ConvertEntityToExDto(o);
                aSettingExDto.AppTransactionSaveAsMappingList.Add(detailExDto);

                if (detailExDto.IsBlankTargetField.HasValue && detailExDto.IsBlankTargetField.Value)
                {
                    aSettingExDto.ApiResponseMappingList.Add(detailExDto);
                }
                else
                {
                    aSettingExDto.DataTransferMappingList.Add(detailExDto);
                }

            }

            if (aSettingExDto.TransferTypeId.HasValue && aSettingExDto.TransferTypeId.Value == (int)EmAppTransactionDataTransferType.FromDataModelToApi)
            {
                InitializeFromDataModelToApiDataTransferSetting(aSettingExDto);
            }

            return aSettingExDto;
        }


        public static AppTransactionDataTransferSettingExDto RetrieveDataTransferSettingByTransactionIdAndInternalCode(int? transactionId, string internalCode)
        {
            if (transactionId.HasValue && !string.IsNullOrWhiteSpace(internalCode))
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppTransactionDataTransferSettingEntity> list = new EntityCollection<AppTransactionDataTransferSettingEntity>();

                    RelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionDataTransferSettingFields.TransactionId == transactionId.Value & AppTransactionDataTransferSettingFields.InternalCode == internalCode);

                    adapter.FetchEntityCollection(list, filter);

                    if (list.Count > 0)
                    {
                        return RetrieveOneAppTransactionDataTransferSettingExDto(list[0].DataTransferSettingId);
                    }

                }
            }

            return null;
        }


        public static OperationCallResult<AppTransactionDataTransferSettingExDto> SaveOneAppTransactionDataTransferSettingExDto(AppTransactionDataTransferSettingExDto aSettingExDto)
        {
            OperationCallResult<AppTransactionDataTransferSettingExDto> aOperationCallResult = new OperationCallResult<AppTransactionDataTransferSettingExDto>();

            var aValidationResult = aSettingExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;


            if (aSettingExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(aSettingExDto));
            }
            else if (aSettingExDto.IsModified)
            {
                validationResult.Merge(ProcessDirtyDto(aSettingExDto));
            }


            if (!validationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppTransactionDataTransferSettingExDto(aSettingExDto.Id);
            }

            return aOperationCallResult;
        }


        public static AppTransactionDataTransferSettingExDto InitializeFromDataModelToApiDataTransferSetting(AppTransactionDataTransferSettingExDto aSettingExDto)
        {
            if (aSettingExDto.TransferTypeId.HasValue && aSettingExDto.TransferTypeId.Value == (int)EmAppTransactionDataTransferType.FromDataModelToApi)
            {


                if (aSettingExDto.TransactionId.HasValue)
                {
                    var srcTransactionDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aSettingExDto.TransactionId.Value);

                    InitializeFromDataModelToApiDataTransfer_InitApiProperties(aSettingExDto, srcTransactionDto);


                }

            }



            return aSettingExDto;
        }

        private static void InitializeFromDataModelToApiDataTransfer_InitApiProperties(AppTransactionDataTransferSettingExDto aSettingExDto, AppTransactionExDto srcTransactionDto)
        {
            aSettingExDto.TargetApiOperationId = ControlTypeValueConverter.ConvertValueToInt(aSettingExDto.InternalCode);

            if (aSettingExDto.TargetApiOperationId.HasValue)
            {
                aSettingExDto.TargetApiOperationDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(aSettingExDto.TargetApiOperationId.Value);

                var operationDto = aSettingExDto.TargetApiOperationDto;

                aSettingExDto.TargetApiInputParameterList = new List<string>();

                foreach (string targetInputParamName in operationDto.APIConfigParameters.PathParams.Keys)
                {
                    aSettingExDto.TargetApiInputParameterList.Add(AppMasterDetailApiFormDataLoadBL.ApiPathParamPrefix + targetInputParamName);
                }

                foreach (string targetInputParamName in operationDto.APIConfigParameters.QueryParams.Keys)
                {
                    aSettingExDto.TargetApiInputParameterList.Add(AppMasterDetailApiFormDataLoadBL.ApiQueryParamPrefix + targetInputParamName);
                }

                foreach (string variableKey in operationDto.DictUsedEnvironmentVariable.Keys)
                {
                    aSettingExDto.TargetApiInputParameterList.Add(AppMasterDetailApiFormDataLoadBL.ApiEvironmentVariablePrefix + variableKey);
                }


                EmAppApiPayloadDataType payloadDataType = EmAppApiPayloadDataType.JSON;

                if (operationDto.OtherSettingsDto != null && operationDto.OtherSettingsDto.PayloadDataType.HasValue)
                {
                    payloadDataType = operationDto.OtherSettingsDto.PayloadDataType.Value;
                }

                if (payloadDataType == EmAppApiPayloadDataType.ServerFilePath)
                {
                    var filePathNodeDto = new ApiDataStructureNodeDto();
                    filePathNodeDto.Display = "Api Upload Server File Path, Web File Url, Or Ftp File Path";
                    filePathNodeDto.SchemaTypeName = "";
                    filePathNodeDto.Name = EmBLFiledMappingSystemTokenField.ApiUploadServerFilePath.ToString();
                    filePathNodeDto.AbsolutePath = filePathNodeDto.Name;

                    aSettingExDto.TargetApiPayloadDataStructure = new List<ApiDataStructureNodeDto>() { filePathNodeDto };




                    var ftpUserNameNodeDto = new ApiDataStructureNodeDto();
                    ftpUserNameNodeDto.Display = "Ftp Username";
                    ftpUserNameNodeDto.SchemaTypeName = "";
                    ftpUserNameNodeDto.Name = EmBLFiledMappingSystemTokenField.ApiUploadFtpUserName.ToString();
                    ftpUserNameNodeDto.AbsolutePath = ftpUserNameNodeDto.Name;

                    aSettingExDto.TargetApiPayloadDataStructure.Add(ftpUserNameNodeDto);


                    var ftpPasswordNodeDto = new ApiDataStructureNodeDto();
                    ftpPasswordNodeDto.Display = "Ftp Password";
                    ftpPasswordNodeDto.SchemaTypeName = "";
                    ftpPasswordNodeDto.Name = EmBLFiledMappingSystemTokenField.ApiUploadFtpPassword.ToString();
                    ftpPasswordNodeDto.AbsolutePath = ftpPasswordNodeDto.Name;

                    aSettingExDto.TargetApiPayloadDataStructure.Add(ftpPasswordNodeDto);
                }
                else if (payloadDataType == EmAppApiPayloadDataType.FileByteArray)
                {
                    var rootNodeDto = new ApiDataStructureNodeDto();
                    rootNodeDto.Display = "Api Upload File Id";
                    rootNodeDto.SchemaTypeName = "";
                    rootNodeDto.Name = EmBLFiledMappingSystemTokenField.ApiUploadFileId.ToString();
                    rootNodeDto.AbsolutePath = rootNodeDto.Name;

                    aSettingExDto.TargetApiPayloadDataStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(operationDto.JsonSchema))
                    {
                        var rootNodeDto = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(operationDto.JsonSchema);
                        AppIntergrationSettingBL.InitApiNodeAbolutePath(rootNodeDto, null);

                        if (srcTransactionDto.TransactionOrganizedType.HasValue)
                        {
                            if ((operationDto.HttpMethd == "Post"
                                  || operationDto.HttpMethd == "Put")
                                   && !string.IsNullOrWhiteSpace(operationDto.JsonSchema)
                                   )
                            {
                                if (srcTransactionDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                                {
                                    if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsObject)
                                    {
                                        rootNodeDto = rootNodeDto.Children[0];
                                        rootNodeDto.Display = "[ ]";
                                    }

                                    aSettingExDto.TargetApiPayloadDataStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };

                                }
                                else if (srcTransactionDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                                {
                                    if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsArray)
                                    {
                                        rootNodeDto = rootNodeDto.Children[0];
                                        rootNodeDto.Display = "[ ]";
                                    }

                                    aSettingExDto.TargetApiPayloadDataStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };

                                }
                            }
                        }
                    }

                }


                if ((operationDto.HttpMethd == "Post" || operationDto.HttpMethd == "Put")
                    && operationDto.PostResponseDto != null
                    && !string.IsNullOrWhiteSpace(operationDto.PostResponseDto.ResponseJsonSchema))
                {
                    if (operationDto.PostResponseDto != null)
                    {
                        var responseStructureNodeDto = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(operationDto.PostResponseDto.ResponseJsonSchema);

                        AppIntergrationSettingBL.InitApiNodeAbolutePath(responseStructureNodeDto, null);
                        aSettingExDto.TargetApiResponseDataStructure = new List<ApiDataStructureNodeDto>() { responseStructureNodeDto };
                    }
                }
                else if ((operationDto.HttpMethd == "Get" || operationDto.HttpMethd == "Delete")
                    && !string.IsNullOrWhiteSpace(operationDto.JsonSchema))
                {
                    var responseStructureNodeDto = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(operationDto.JsonSchema);

                    AppIntergrationSettingBL.InitApiNodeAbolutePath(responseStructureNodeDto, null);
                    aSettingExDto.TargetApiResponseDataStructure = new List<ApiDataStructureNodeDto>() { responseStructureNodeDto };
                }

            }
        }

        public static OperationCallResult<object> DeleteOneAppTransactionDataTransferSettingEntityDto(object Id)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppTransactionDataTransferSettingEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionDataTransferSettingEntity), new RelationPredicateBucket(AppTransactionDataTransferSettingFields.DataTransferSettingId == Id));
                    adapter.Commit();
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_AppTransactionDataTransferSettingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }

            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }


        private static ValidationResult ProcessNewDto(AppTransactionDataTransferSettingExDto aSettingExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTransactionDataTransferSettingEntity appTransactionDataTransferSettingEntity = new AppTransactionDataTransferSettingEntity();
            AppTransactionDataTransferSettingConverter.CopyDtoToEntity(appTransactionDataTransferSettingEntity, aSettingExDto);

            foreach (var settingDetailExDto in aSettingExDto.AppTransactionSaveAsMappingList)
            {

                AppTransactionSaveAsMappingEntity settingDetailEntity = new AppTransactionSaveAsMappingEntity();
                AppTransactionSaveAsMappingConverter.CopyDtoToEntity(settingDetailEntity, settingDetailExDto);
                appTransactionDataTransferSettingEntity.AppTransactionSaveAsMapping.Add(settingDetailEntity);
            }


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(appTransactionDataTransferSettingEntity);

                    adapter.Commit();
                    aSettingExDto.Id = appTransactionDataTransferSettingEntity.DataTransferSettingId;

                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionDataTransferSettingExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppTransactionDataTransferSettingExDto aSettingExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            var orgSettingEntity = RetrieveOneAppTransactionDataTransferSettingEntity(aSettingExDto.Id);

            int[] dirtySettingDetailIds = aSettingExDto.AppTransactionSaveAsMappingList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();


            AppTransactionDataTransferSettingEntity aAppTransactionDataTransferSettingEntity = RetrieveOneAppTransactionDataTransferSettingEntity(aSettingExDto.Id);

            Dictionary<int, AppTransactionSaveAsMappingEntity> dictSettingDetailFromDbms = aAppTransactionDataTransferSettingEntity.AppTransactionSaveAsMapping.ToDictionary(o => o.MappingId, o => o);

            AppTransactionDataTransferSettingConverter.CopyDtoToEntity(aAppTransactionDataTransferSettingEntity, aSettingExDto);


            // new Items
            foreach (AppTransactionSaveAsMappingExDto aChildDto in aSettingExDto.AppTransactionSaveAsMappingList.FindNewItems())
            {

                AppTransactionSaveAsMappingEntity aNewChildEntity = new AppTransactionSaveAsMappingEntity();
                AppTransactionSaveAsMappingConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppTransactionDataTransferSettingEntity.AppTransactionSaveAsMapping.Add(aNewChildEntity);

            }

            // Dirty items, only the update item remove from dbms, no need to update that itmes
            foreach (var modifyitem in aSettingExDto.AppTransactionSaveAsMappingList.FindModifiedItems())
            {
                if (!modifyitem.IsNew)
                {
                    int dtoKey = int.Parse(modifyitem.Id.ToString());
                    if (dictSettingDetailFromDbms.ContainsKey(dtoKey))
                    {
                        AppTransactionSaveAsMappingConverter.CopyDtoToEntity(dictSettingDetailFromDbms[dtoKey], modifyitem);
                    }
                }
            }

            // deletedIDs      
            List<int> deletDetailIDs = aSettingExDto.AppTransactionSaveAsMappingList.FindDeletedItemIds().Cast<int>().ToList();


            foreach (var mappingEntity in orgSettingEntity.AppTransactionSaveAsMapping)
            {
                if (aSettingExDto.TransferTypeId.HasValue && aSettingExDto.TransferTypeId.Value == (int)EmAppTransactionDataTransferType.FromDataModelToApi)
                {
                    deletDetailIDs.Add(mappingEntity.MappingId);
                }
                else
                {
                    if (mappingEntity.SourceFiledId.HasValue)
                    {
                        var newMapping = aSettingExDto.AppTransactionSaveAsMappingList
                             .FirstOrDefault(o => o.SourceFiledId.HasValue && o.SourceFiledId.Value == mappingEntity.SourceFiledId.Value);

                        if (newMapping == null)
                        {
                            deletDetailIDs.Add(mappingEntity.MappingId);
                        }
                    }

                }
            }



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionDataTransferSettingEntity);

                    //1: need to delete deletdmSecurityPermissionUserGroupMemberIDs

                    if (deletDetailIDs.Count() > 0)
                    {

                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionSaveAsMappingEntity), new RelationPredicateBucket(AppTransactionSaveAsMappingFields.MappingId == deletDetailIDs.ToArray()));
                    }


                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityGroupExDto), "AppTransactionDataTransferSettingEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityGroupExDto), "AppTransactionDataTransferSettingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }


            }




            return aValidationResult;
        }




        public static OperationCallResult<bool> DeleteOneAppTransactionDataTransferSettingExDto(object settingId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adpater.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adpater.DeleteEntitiesDirectly(typeof(AppTransactionSaveAsMappingEntity), new RelationPredicateBucket(AppTransactionSaveAsMappingFields.DataTransferSettingId == settingId));
                    adpater.DeleteEntitiesDirectly(typeof(AppTransactionDataTransferSettingEntity), new RelationPredicateBucket(AppTransactionDataTransferSettingFields.DataTransferSettingId == settingId));

                    adpater.Commit();

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupEntity), "App_AppTransactionDataTransferSetting_Delete_OK", ValidationItemType.Message, "Delete Successful"));
                }


                catch (Exception ex)
                {
                    adpater.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupEntity), "App_AppTransactionDataTransferSetting_Delete_Error", ValidationItemType.Error, ex.ToString()));

                }
            }

            return aOperationCallResult;
        }

    }

}