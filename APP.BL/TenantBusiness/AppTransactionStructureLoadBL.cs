using System.Collections.Generic;
using System.Linq;
using DatabaseSchemaMrg.DataSchema;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.LBL.DatabaseSpecific;
using System.Data;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System.Data.SqlClient;
using Newtonsoft.Json;

using APP.Framework;
namespace App.BL
{
    public static class AppTransactionStructureLoadBL
    {

        // need to cache
        public static AppTransactionStructureDto GetAppTransactionStructureDto(int transactionId)
        {
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId); // AppTransactionBL.GetOneTransactionExDtoWithFormId(transactionId);

            AppTransactionStructureDto transactionStructureDto = GetGeneralStrcutureInfo(transactionExDto);
            SetupAllEntityLookUpItem(transactionStructureDto, transactionExDto);




            return transactionStructureDto;
        }


        internal static AppTransactionStructureDto GetGeneralStrcutureInfo(AppTransactionExDto transactionExDto)
        {
            AppTransactionStructureDto transactionStructureDto = new AppTransactionStructureDto();

            transactionStructureDto.HierarchyTransactionExdto = transactionExDto;

            transactionStructureDto.TransactionId = (int)transactionExDto.Id;
            transactionStructureDto.FormID = transactionExDto.FormId;
            transactionStructureDto.SaasApplicationId = transactionExDto.SaasApplicationId;
            transactionStructureDto.EnableFolderSecurity = transactionExDto.NeedToCheckRowVersion;
            transactionStructureDto.BusinessScopeId = transactionExDto.BusinessScopeId;

            transactionExDto.FolderTransactionId = transactionExDto.FolderTransactionId;
            transactionStructureDto.FolderTransactionId = transactionExDto.FolderTransactionId;

            //transactionStructureDto.FolderUsageType = transactionExDto.FolderUsageType;


            if (transactionExDto.OtherOptions != null)
            {
                transactionStructureDto.ErDiagramId = transactionExDto.OtherOptions.ErDiagramId;
                transactionStructureDto.ImportSettingId = transactionExDto.OtherOptions.ImportSettingId;
                transactionStructureDto.IsDraft = transactionExDto.OtherOptions.IsDraft;

                transactionStructureDto.TransactionDataUpdateImportSettingId = transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId;
            }


            //
            //   aAppformDataDto.AppFormDto.ForeignAppTransactionExDto = null;

            AppTransactionDataDeleteBL.SetupDictAppTransactionUnitDeleteListByTransactionId(transactionExDto);
            transactionStructureDto.ChildDeleteValidationUnitIds = transactionExDto.DictUnitValidationDeleteList.Keys.ToList();

            transactionStructureDto.DictAvailableSourceUnitIdAndSubscribeUnitId = new Dictionary<int, int>();
            transactionStructureDto.DictAvailableSubscribeUnitIdAndKeyFieldMapping = new Dictionary<int, Dictionary<string, string>>();

            transactionStructureDto.DictTransactionUnitIdDBFiledNameBarCodeType = new Dictionary<int, Dictionary<string, string>>();
            transactionStructureDto.DictTransactionUnitIdDBFiledNameControlType = new Dictionary<int, Dictionary<string, int>>();
            transactionStructureDto.DictTransactionUnitIdGroupByFields = new Dictionary<int, List<string>>();
            //aAppformDataDto.DictTransactionUnitIdLinkTargetList = new Dictionary<int, List<AppFormLinkTargetExDto>>();
            transactionStructureDto.DictTransactionUnitIdLinkedSearchList = new Dictionary<int, List<AppTransactionUnitLinkedSearchExDto>>();

            //transactionStructureDto.DictTransactionUnitIdDBFieldNameMantory = new Dictionary<int, List<string>>();
            //transactionStructureDto.DictUnitIdFieldDbNameAndValidationDto = new Dictionary<int, Dictionary<string, AppTransactionFieldValidationDto>>();

            transactionStructureDto.DictTransactionUnitIdFiledNameFiledID = new Dictionary<int, Dictionary<string, int>>();
            transactionStructureDto.DictTransactionUnitIdFieldIdFieldDisplayName = new Dictionary<int, Dictionary<int, string>>();
            transactionStructureDto.DictUnitIdStoreProcOrQueryDataSetMasterFiled = new Dictionary<int, List<string>>();


            // mutiple column cascadinding retrieve columns
            transactionStructureDto.IsUsedRetrieveDataMappingDataSourceFiedIds = new List<string>();
            transactionStructureDto.DictRetrieveDataChildDDLFieldIdAndRetrieveButtonFieldIdList = new Dictionary<string, List<string>>();
            transactionStructureDto.DictRetrieveDataOneToManyCascading_TransUnitId_ParentFieldNameAndChildFieldNames = new Dictionary<string, Dictionary<string, List<string>>>();

            transactionStructureDto.DictUnitIdFiledIdMappingCrosssId = new Dictionary<int, Dictionary<string, string>>();

            transactionStructureDto.DictUnitIdPivotGrid = new Dictionary<int, AppPivotDto>();

            transactionStructureDto.DictFolderIdFolderDisplay = new Dictionary<int, string>();

            transactionStructureDto.DictUnitIdAndTreeViewSetting = new Dictionary<int, TreeViewSettingDto>();

            transactionStructureDto.DictUnitIdParentUnitId = new Dictionary<string, string>();
            transactionStructureDto.DictFieldIdUnitId = new Dictionary<string, string>();
            transactionStructureDto.DictGoogleAddressFieldIdAndSubFieldMapping = new Dictionary<string, Dictionary<string, string>>();

            transactionStructureDto.DictCascadedIdParentField = AppCascadingBL.GetCascadedFieldParentFiedIds(transactionExDto);

            if (transactionExDto.RootMasterUnit != null)
            {
                var partnerIdField = transactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CurrentPartnerID);
                if (partnerIdField != null)
                {
                    transactionStructureDto.PartnerIDFieldName = partnerIdField.DataBaseFieldName;
                }

                var userIdField = transactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CurrentUserID);
                if (userIdField != null)
                {
                    transactionStructureDto.UserIDFieldName = userIdField.DataBaseFieldName;
                }
            }

            var dictCommandIdAndDto = transactionExDto.CommandActionList.ToDictionary(o => (int)o.Id,
                o => new CommandUiDto()
                {
                    Id = o.Id,
                    ActionType = o.ActionType,
                    MessageTemplateId = o.MessageTemplateId,
                    ActionAttribute = new
                    { // add more atribute based on UI demand
                        LinkedSearchId = o.ActionAttribute.LinkedSearchId,
                        CallBackCommandID = o.ActionAttribute.CallBackCommandID
                    }
                }

                );


            transactionStructureDto.DictCommandIdAndCommandDto = dictCommandIdAndDto;

            transactionStructureDto.DictFieldIdAndCommandDto = new Dictionary<int, CommandUiDto>();
            transactionExDto.DictRootLevelUnitTransactionField.Values.Where(fieldDto => !string.IsNullOrWhiteSpace(fieldDto.ControlTypeParam1)).ForAll(o =>
            {
                int? commandId = ControlTypeValueConverter.ConvertValueToInt(o.ControlTypeParam1);
                if (commandId.HasValue && dictCommandIdAndDto.ContainsKey(commandId.Value))
                {
                    transactionStructureDto.DictFieldIdAndCommandDto.Add((int)o.Id, dictCommandIdAndDto[commandId.Value]);
                }
            });

            transactionExDto.DictRootLevelUnitTransactionField.Values.Where(o => !string.IsNullOrWhiteSpace(o.ControlTypeParam3)
                && (o.ControlType == (int)EmAppControlType.GoogleAddress || o.ControlType == (int)EmAppControlType.GoogleMap)).ForAll(o =>
            {
                Dictionary<string, string> mappingObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(o.ControlTypeParam3);
                transactionStructureDto.DictGoogleAddressFieldIdAndSubFieldMapping.Add(o.Id.ToString(), mappingObj);
            });


            Dictionary<int, string> dictAllFiledIdDataBaseFileName = new Dictionary<int, string>();

            foreach (var aTransactionUnit in transactionExDto.DictAllTransactionUnitIdExDto.Values)
            {
                if (aTransactionUnit.ParentTransactionUnitId.HasValue)
                {
                    transactionStructureDto.DictUnitIdParentUnitId[aTransactionUnit.Id.ToString()] = aTransactionUnit.ParentTransactionUnitId.ToString();
                }

                foreach (var filed in aTransactionUnit.AppTransactionFieldList)
                {
                    transactionStructureDto.DictFieldIdUnitId[filed.Id.ToString()] = filed.TransactionUnitId.ToString();

                    dictAllFiledIdDataBaseFileName.Add((int)filed.Id, filed.DataBaseFieldName);

                }


                if (aTransactionUnit.EmGridViewDisplayType.HasValue && aTransactionUnit.EmGridViewDisplayType.Value == (int)EmAppTransactionGridDisplayType.TreeGrid)
                {
                    TreeViewSettingDto treeViewSettingDto = new TreeViewSettingDto()
                    {
                        TreeViewKeyField = aTransactionUnit.TreeViewKeyField,
                        TreeViewParentKeyField = aTransactionUnit.TreeViewParentKeyField
                    };

                    transactionStructureDto.DictUnitIdAndTreeViewSetting.Add((int)aTransactionUnit.Id, treeViewSettingDto);
                }
            }


            foreach (var aTransactionUnit in transactionExDto.DictAllTransactionUnitIdExDto.Values)
            {
                PorcessOneUnit(transactionStructureDto, dictAllFiledIdDataBaseFileName, aTransactionUnit);

            }

            //
            transactionStructureDto.DictTransactionUnitPKFied = transactionExDto.DictAllTransactionUnitIdExDto
                .ToDictionary(o => o.Key.ToString(),
                o => o.Value.AppTransactionFieldList
                .Where(f => f.IsPrimaryKey)
                .Select(pkf => pkf.DataBaseFieldName)
                .ToList()
                );

            // will added new columnName to unittable called InternalCode fot ui data binding
            transactionStructureDto.DictTransactionUnitIdInternalCode = transactionExDto.DictAllTransactionUnitIdExDto
                .ToDictionary(o => o.Key.ToString(),
                o => o.Value.DataBaseTableName
                );



            transactionStructureDto.DictUnitIdAuditorFieldNames = transactionExDto.DictAllTransactionUnitIdExDto
               .ToDictionary(o => o.Key.ToString(),
                   o => o.Value.AppTransactionFieldList
                   .Where(f => f.IsNeedLog.HasValue && f.IsNeedLog.Value)
                   .ToDictionary(ao => ao.DataBaseFieldName, ao => (int)ao.Id)
               );

            transactionStructureDto.DictUnitIdLogicalDisplayNames = transactionExDto.DictAllTransactionUnitIdExDto
           .ToDictionary(o => o.Key.ToString(),
               o => o.Value.AppTransactionFieldList
               .Where(f => f.IsLogicalDisplay.HasValue && f.IsLogicalDisplay.Value)
              .ToDictionary(ao => ao.DataBaseFieldName, ao => (int)ao.Id)
           );


            transactionStructureDto.DictUnitIdChangeNotificationDisplayNames = transactionExDto.DictAllTransactionUnitIdExDto
       .ToDictionary(o => o.Key.ToString(),
           o => o.Value.AppTransactionFieldList
           .Where(f => f.IsChangeTrigerNotification.HasValue && f.IsChangeTrigerNotification.Value)
          .ToDictionary(ao => ao.DataBaseFieldName, ao => (int)ao.Id)
       );





            if (transactionStructureDto.FolderTransactionId.HasValue)
            {
                AppSefolderDto[] folderHairarchy = AppSeFolderBL.RetrieveFolderHairarchyDto(transactionStructureDto.FolderTransactionId.Value);
                List<AppSefolderDto> flatFolderList = AppSeFolderBL.ConvertFolderHairarchyToFlatList(folderHairarchy);
                transactionStructureDto.DictFolderIdFolderDisplay = flatFolderList.ToDictionary(o => (int)o.Id, o => o.FolderPath);
            }

            if (transactionExDto.TransactionFileStorageRootFolderId.HasValue)
            {
                transactionStructureDto.FileStorageRootFolderId = transactionExDto.TransactionFileStorageRootFolderId;
            }
            else
            {
                transactionStructureDto.FileStorageRootFolderId = AppTenantSettingBL.GetIntValue(EmTenantSettings.TransactionFileStorageRootFolderId);
            }

            return transactionStructureDto;
        }

        private static void PorcessOneUnit(AppTransactionStructureDto transactionStructureDto, Dictionary<int, string> dictAllFiledIdDataBaseFileName, AppTransactionUnitExDto aTransactionUnit)
        {
            transactionStructureDto.DictTransactionUnitIdDBFiledNameControlType.Add((int)aTransactionUnit.Id, aTransactionUnit.DictTransFieldDBFiledNameControlType);
            var dictTransFieldNameAndBarCodeType = aTransactionUnit.AppTransactionFieldList.Where(o => o.ControlType == (int)EmAppControlType.BarCode).ToDictionary(o => o.DataBaseFieldName, o => o.DefaultValue.ToString());
            transactionStructureDto.DictTransactionUnitIdDBFiledNameBarCodeType.Add((int)aTransactionUnit.Id, dictTransFieldNameAndBarCodeType);

            transactionStructureDto.DictTransactionUnitIdGroupByFields.Add((int)aTransactionUnit.Id, aTransactionUnit.GroupByTransactionFieldList);

            //aAppformDataDto.DictTransactionUnitIdLinkTargetList.Add((int)aTransactionUnit.Id, aTransactionUnit.AppFormLinkTargetList.ToList());

            transactionStructureDto.DictTransactionUnitIdLinkedSearchList.Add((int)aTransactionUnit.Id, aTransactionUnit.AppTransactionUnitLinkedSearchList.ToList());

            if (aTransactionUnit.AvailableSourceUnitId.HasValue)
            {
                transactionStructureDto.DictAvailableSourceUnitIdAndSubscribeUnitId[aTransactionUnit.AvailableSourceUnitId.Value] = (int)aTransactionUnit.Id;
                //DictAvailableSourceSubscribeUnitIdAndKeyFieldMapping

                Dictionary<string, string> dictSubscribeAndSourceFieldMapping = new Dictionary<string, string>();
                foreach (var filed in aTransactionUnit.AppTransactionFieldList)
                {
                    if (filed.MappingToAvailableSourceUnitTransactionFieldId.HasValue)
                    {

                        dictSubscribeAndSourceFieldMapping.Add(filed.DataBaseFieldName.ToString(), dictAllFiledIdDataBaseFileName[filed.MappingToAvailableSourceUnitTransactionFieldId.Value]);

                    }

                }
                if (dictSubscribeAndSourceFieldMapping.Count > 0)
                {
                    transactionStructureDto.DictAvailableSubscribeUnitIdAndKeyFieldMapping.Add((int)aTransactionUnit.Id, dictSubscribeAndSourceFieldMapping);
                }
            }


            // transactionStructureDto.DictTransactionUnitIdDBFieldNameMantory.Add((int)aTransactionUnit.Id,
            //       aTransactionUnit.AppTransactionFieldList
            //     .Where(o => o.IsAllowEmpty.HasValue && (!o.IsAllowEmpty.Value))
            //     .Select(o => o.DataBaseFieldName).ToList()
            //);

            //Dictionary<string, AppTransactionFieldValidationDto> dictTransFieldValidationDto = new Dictionary<string, AppTransactionFieldValidationDto>();
            //transactionStructureDto.DictUnitIdFieldDbNameAndValidationDto.Add((int)aTransactionUnit.Id, dictTransFieldValidationDto);

            //foreach (var transactionField in aTransactionUnit.AppTransactionFieldList.Where(o => o.NeedValidator.HasValue && o.NeedValidator.Value))
            //{
            //    AppTransactionFieldValidationDto transactionFieldValidationDto = new AppTransactionFieldValidationDto();
            //    transactionFieldValidationDto.TransactionFieldId = transactionField.Id;
            //    transactionFieldValidationDto.DataBaseFieldName = transactionField.DataBaseFieldName;
            //    transactionFieldValidationDto.DisplayName = transactionField.DisplayName;
            //    transactionFieldValidationDto.UnitDisplayName = aTransactionUnit.UnitDisplayName;
            //    transactionFieldValidationDto.DataType = transactionField.DataType;                
            //    transactionFieldValidationDto.MaxCharLegnth = transactionField.MaxCharLegnth;
            //    transactionFieldValidationDto.IsAllowEmpty = transactionField.IsAllowEmpty;

            //    if (!dictTransFieldValidationDto.ContainsKey(transactionField.DataBaseFieldName))
            //    {
            //        dictTransFieldValidationDto.Add(transactionField.DataBaseFieldName, transactionFieldValidationDto);
            //    }
            //}


            transactionStructureDto.DictTransactionUnitIdFiledNameFiledID.Add((int)aTransactionUnit.Id, aTransactionUnit.DictDbFileNameFieldId);
            transactionStructureDto.DictTransactionUnitIdFieldIdFieldDisplayName.Add((int)aTransactionUnit.Id,
                aTransactionUnit.AppTransactionFieldList.OrderBy(o => o.SortOrder).Where(o => o.Id != null)
                .ToDictionary(o => (int)o.Id, o => o.DisplayName));




            transactionStructureDto.DictUnitIdStoreProcOrQueryDataSetMasterFiled.Add((int)aTransactionUnit.Id,
                 aTransactionUnit.AppTransactionFieldList
               .Where(o => o.DataRetrieveType.HasValue && (o.DataRetrieveType.Value == (int)EmAppCascadingSourceType.ExternalOneToManyMapping || o.DataRetrieveType.Value == (int)EmAppCascadingSourceType.QueryStatement))
               .Select(o => o.DataBaseFieldName).ToList()
          );



            transactionStructureDto.IsUsedRetrieveDataMappingDataSourceFiedIds.AddRange(
                 aTransactionUnit.AppTransactionFieldList
                 .Where(o => o.ControlType == (int)EmAppControlType.RetrieveData && o.MasterEntityFieldlId.HasValue)
                 .Select(o => o.MasterEntityFieldlId.ToString())
                 .ToList()
                 );


            Dictionary<string, List<string>> dictCurrentUnitParentFieldNameAndChildFieldNames = new Dictionary<string, List<string>>();

            foreach (var aField in aTransactionUnit.AppTransactionFieldList)
            {
                if (aField.ControlType == (int)EmAppControlType.RetrieveData && aField.MasterEntityFieldlId.HasValue)
                {
                    var masterEntityField = aTransactionUnit.AppTransactionFieldList.FirstOrDefault(o => (int)o.Id == aField.MasterEntityFieldlId.Value);

                    if (masterEntityField != null)
                    {
                        if (!transactionStructureDto.DictRetrieveDataChildDDLFieldIdAndRetrieveButtonFieldIdList.ContainsKey(aField.MasterEntityFieldlId.Value.ToString()))
                        {
                            transactionStructureDto.DictRetrieveDataChildDDLFieldIdAndRetrieveButtonFieldIdList.Add(aField.MasterEntityFieldlId.Value.ToString(), new List<string>());
                        }

                        transactionStructureDto.DictRetrieveDataChildDDLFieldIdAndRetrieveButtonFieldIdList[aField.MasterEntityFieldlId.Value.ToString()].Add(aField.Id.ToString());


                        aField.ConvertDataRetrieveMappingStringToDict();

                        if (aField.InputParentTransFiledNameSqlParaMapping.Count > 0)
                        {
                            foreach (string parentFieldName in aField.InputParentTransFiledNameSqlParaMapping.Keys)
                            {
                                if (!dictCurrentUnitParentFieldNameAndChildFieldNames.ContainsKey(parentFieldName))
                                {
                                    dictCurrentUnitParentFieldNameAndChildFieldNames.Add(parentFieldName, new List<string>());
                                }

                                if (!dictCurrentUnitParentFieldNameAndChildFieldNames[parentFieldName].Contains(masterEntityField.DataBaseFieldName))
                                {
                                    dictCurrentUnitParentFieldNameAndChildFieldNames[parentFieldName].Add(masterEntityField.DataBaseFieldName);
                                }
                            }
                        }
                    }
                }
            }

            if (dictCurrentUnitParentFieldNameAndChildFieldNames.Count > 0)
            {
                transactionStructureDto.DictRetrieveDataOneToManyCascading_TransUnitId_ParentFieldNameAndChildFieldNames.Add(aTransactionUnit.Id.ToString(), dictCurrentUnitParentFieldNameAndChildFieldNames);

            }





            Dictionary<string, string> DictFiledIdMappingCrosssId = new Dictionary<string, string>();
            foreach (var filed in aTransactionUnit.AppTransactionFieldList)
            {
                if (filed.ChildUnitSubscribeParentFieldId.HasValue)
                {

                    DictFiledIdMappingCrosssId.Add(filed.DataBaseFieldName.ToString(), dictAllFiledIdDataBaseFileName[filed.ChildUnitSubscribeParentFieldId.Value]);

                }

            }
            if (DictFiledIdMappingCrosssId.Count > 0)
            {
                transactionStructureDto.DictUnitIdFiledIdMappingCrosssId.Add((int)aTransactionUnit.Id, DictFiledIdMappingCrosssId);
            }




            if (aTransactionUnit.EmGridViewDisplayType.HasValue &&
                (aTransactionUnit.EmGridViewDisplayType.Value == (int)EmAppTransactionGridDisplayType.PivotEditGrid
                    || aTransactionUnit.EmGridViewDisplayType.Value == (int)EmAppTransactionGridDisplayType.PivotViewGrid))
            {

                AppPivotDto appPivotDto = new AppPivotDto();
                appPivotDto.IsPivotEdit = (aTransactionUnit.EmGridViewDisplayType.HasValue && aTransactionUnit.EmGridViewDisplayType.Value == (int)EmAppTransactionGridDisplayType.PivotEditGrid);
                appPivotDto.PivotRowFields = aTransactionUnit.AppTransactionFieldList.Where(o => o.IsPivotRow.HasValue && o.IsPivotRow.Value).Select(o => new AppTransactionFieldDto() { Id = o.Id, DataBaseFieldName = o.DataBaseFieldName, DisplayName = o.DisplayName, EntityId = o.EntityId, ControlType = o.ControlType, IsVisible = o.IsVisible }).ToList();
                appPivotDto.PivotColumnFields = aTransactionUnit.AppTransactionFieldList.Where(o => o.IsPivotColumn.HasValue && o.IsPivotColumn.Value).Select(o => new AppTransactionFieldDto() { Id = o.Id, DataBaseFieldName = o.DataBaseFieldName, DisplayName = o.DisplayName, EntityId = o.EntityId, ControlType = o.ControlType, IsVisible = o.IsVisible }).ToList();
                appPivotDto.PivotValueFields = aTransactionUnit.AppTransactionFieldList.Where(o => o.IsPivotValue.HasValue && o.IsPivotValue.Value || o.IsPrimaryKey || o.IsLinkToParentPrimaryKey).Select(o => new AppTransactionFieldDto() { Id = o.Id, DataBaseFieldName = o.DataBaseFieldName, DisplayName = o.DisplayName, EntityId = o.EntityId, ControlType = o.ControlType, IsVisible = o.IsVisible }).ToList();
                appPivotDto.AllFields = aTransactionUnit.AppTransactionFieldList.Select(o => new AppTransactionFieldDto() { Id = o.Id, DataBaseFieldName = o.DataBaseFieldName, DisplayName = o.DisplayName, EntityId = o.EntityId, ControlType = o.ControlType, IsVisible = o.IsVisible }).ToList();

                transactionStructureDto.DictUnitIdPivotGrid.Add((int)aTransactionUnit.Id, appPivotDto);
            }



        }


        internal static void SetupAllEntityLookUpItem(AppTransactionStructureDto transactionStructureDto, AppTransactionExDto AppTransactionExDto)
        {

            transactionStructureDto.DictStandAloneEntityDataSource = new Dictionary<string, List<LookupItemDto>>();

            transactionStructureDto.DictStandAloneFiledIDMappingEntityID = new Dictionary<string, string>();

            Dictionary<object, int> dictFieldIdCurrentUserEtityIds = new Dictionary<object, int>();


            List<int> allDdlEntityIds = new List<int>();

            foreach (AppTransactionUnitExDto unit in AppTransactionExDto.DictAllTransactionUnitIdExDto.Values)
            {
                foreach (var field in unit.AppTransactionFieldList)
                {
                    // dont add 
                    if (field.ControlType == (int)EmAppControlType.DDL || field.ControlType == (int)EmAppControlType.RadioButtons || field.ControlType == (int)EmAppControlType.Progress)
                    {
                        if (field.EntityId.HasValue)
                        {
                            GetEntityDdlLookItemList(transactionStructureDto, dictFieldIdCurrentUserEtityIds, allDdlEntityIds, field);

                        }
                        else
                        {
                            transactionStructureDto.DictStandAloneFiledIDMappingEntityID.Add(field.Id.ToString(), "");

                        }
                    }
                    else if (field.ControlType == (int)EmAppControlType.AutoComplete || field.ControlType == (int)EmAppControlType.SearchAbleDDL)
                    {
                        if (field.EntityId.HasValue)
                        {
                            transactionStructureDto.DictStandAloneFiledIDMappingEntityID.Add(field.Id.ToString(), field.EntityId.Value.ToString());
                            allDdlEntityIds.Add(field.EntityId.Value);

                        }
                        else
                        {
                            transactionStructureDto.DictStandAloneFiledIDMappingEntityID.Add(field.Id.ToString(), "");
                        }
                    }
                }
            }

            List<int> entityIdList = allDdlEntityIds.Distinct().ToList();

            foreach (int entityId in entityIdList)
            {

                transactionStructureDto.DictStandAloneEntityDataSource.Add(entityId.ToString(), AppEntityInfoBL.GetLookupItemList(entityId, string.Empty));
            }



            transactionStructureDto.EntityInfoDtoList = AppEntityInfoBL.RetrieveAllAppEntityInfoDto(entityIdList).ToList();
        }

        private static void GetEntityDdlLookItemList(AppTransactionStructureDto transactionStructureDto,
            Dictionary<object, int> dictFieldIdCurrentUserEtityIds, List<int> allDdlEntityIds, AppTransactionFieldExDto field)
        {
            if (!string.IsNullOrWhiteSpace(field.DdlQueryText))
            {
                // need to parse error

                // it is standlone query, first column is Id, second column is dispaly
                if (string.IsNullOrWhiteSpace(field.WhereClauseExpress))
                {


                    List<LookupItemDto> noEntityIdLookupItemList = new List<LookupItemDto>();
                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        IDataReader reader = adapter.FetchDataReader(new RetrievalQuery(new SqlCommand(field.DdlQueryText)), CommandBehavior.SingleResult);
                        while (reader.Read())
                        {
                            noEntityIdLookupItemList.Add(new LookupItemDto() { Id = reader.GetValue(0), Display = reader.GetValue(1).ToString() });

                        }
                    }
                    // need to add DictFiledDataSource
                    //transactionStructureDto.DictFiledDataSource.Add(field.Id.ToString(), noEntityIdLookupItemList);

                }

            }
            else
            {

                bool isAdmin = AppSecurityUserBL.IsAdminUser();


                //if (!(isAdmin))
                //{
                //    if (AppSecurityRegDomainBL.EntityInfoIds.Contains(field.EntityId.Value))
                //    {
                //        dictFieldIdCurrentUserEtityIds.Add(field.Id, field.EntityId.Value);
                //    }
                //}

                allDdlEntityIds.Add(field.EntityId.Value);

                transactionStructureDto.DictStandAloneFiledIDMappingEntityID.Add(field.Id.ToString(), field.EntityId.ToString());

            }
        }

        internal static void SetChildDefaultValuePlaceholder(Dictionary<string, List<AppChildDataDto>> DictOneToManyFields, AppTransactionUnitExDto aChildTransactionUnitExDto)
        {
            //string childtableName = aChildTransactionUnitExDto.DataBaseTableName;
            //DatabaseTable childdatabaseTable = AppCacheManagerBL.GetDatabaseTable(aChildTransactionUnitExDto); ;

            // only need to add AppChildDataDto in child level !!
            List<AppChildDataDto> childAppformChildDataDto = new List<AppChildDataDto>();
            DictOneToManyFields.Add(aChildTransactionUnitExDto.Id.ToString(), childAppformChildDataDto);

            // one need to one Row as place holder value
            AppChildDataDto aAppformChildDataDto = new AppChildDataDto() { IsNew = true };
            childAppformChildDataDto.Add(aAppformChildDataDto);

            Dictionary<string, string> dictChildDbFieldDefaultValue = aChildTransactionUnitExDto.AppTransactionFieldList.Where(o => !string.IsNullOrWhiteSpace(o.DefaultValue)).ToDictionary(o => o.DataBaseFieldName, o => o.DefaultValue);

            aChildTransactionUnitExDto.AppTransactionFieldList.Where(o => o.ControlType == (int)EmAppControlType.Numeric && !dictChildDbFieldDefaultValue.ContainsKey(o.DataBaseFieldName) && !o.IsPrimaryKey && !o.IsLinkToParentPrimaryKey)
                .ForAll(o => dictChildDbFieldDefaultValue.Add(o.DataBaseFieldName, "0"));


            aAppformChildDataDto.DictOneToOneFields = new Dictionary<string, object>();

            Dictionary<string, object> dictChildTransactionUnitSecurityDDLFieldValue = GetTransactionUnitSecurityDDLFieldValue(aChildTransactionUnitExDto);

            foreach (var column in aChildTransactionUnitExDto.AppTransactionFieldList)
            {
                if (dictChildDbFieldDefaultValue.ContainsKey(column.DataBaseFieldName))
                {
                    aAppformChildDataDto.DictOneToOneFields.Add(column.DataBaseFieldName, dictChildDbFieldDefaultValue[column.DataBaseFieldName]);
                }
                else
                {
                    aAppformChildDataDto.DictOneToOneFields.Add(column.DataBaseFieldName, null);
                }

                if (dictChildTransactionUnitSecurityDDLFieldValue.ContainsKey(column.DataBaseFieldName))
                {
                    aAppformChildDataDto.DictOneToOneFields[column.DataBaseFieldName] = dictChildTransactionUnitSecurityDDLFieldValue[column.DataBaseFieldName];
                }
            }

            aAppformChildDataDto.DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();
            if (aChildTransactionUnitExDto.Children != null && aChildTransactionUnitExDto.Children.Count > 0)
            {



                foreach (AppTransactionUnitExDto aGrandchildAppTransactionUnitExDto in aChildTransactionUnitExDto.Children)
                {

                    //DatabaseTable grandChilddatabaseTable = AppCacheManagerBL.GetDatabaseTable(aGrandchildAppTransactionUnitExDto);

                    List<AppChildDataDto> listGridnChildRow = new System.Collections.Generic.List<AppChildDataDto>();
                    AppChildDataDto aAppChildDataDto = new AppChildDataDto();
                    Dictionary<string, object> dictGrandChildRowKeyValue = new Dictionary<string, object>();
                    aAppChildDataDto.DictOneToOneFields = dictGrandChildRowKeyValue;
                    listGridnChildRow.Add(aAppChildDataDto);

                    Dictionary<string, string> dictGrandChildDbFieldDefaultValue = aGrandchildAppTransactionUnitExDto.AppTransactionFieldList.Where(o => !string.IsNullOrWhiteSpace(o.DefaultValue)).ToDictionary(o => o.DataBaseFieldName, o => o.DefaultValue);
                    aGrandchildAppTransactionUnitExDto.AppTransactionFieldList.Where(o => o.ControlType == (int)EmAppControlType.Numeric && !dictGrandChildDbFieldDefaultValue.ContainsKey(o.DataBaseFieldName) && !o.IsPrimaryKey && !o.IsLinkToParentPrimaryKey)
                        .ForAll(o => dictGrandChildDbFieldDefaultValue.Add(o.DataBaseFieldName, "0"));



                    Dictionary<string, object> dictGrandChildTransactionUnitSecurityDDLFieldValue = GetTransactionUnitSecurityDDLFieldValue(aGrandchildAppTransactionUnitExDto);

                    foreach (var column in aGrandchildAppTransactionUnitExDto.AppTransactionFieldList)
                    {
                        if (dictGrandChildDbFieldDefaultValue.ContainsKey(column.DataBaseFieldName))
                        {
                            dictGrandChildRowKeyValue.Add(column.DataBaseFieldName, dictGrandChildDbFieldDefaultValue[column.DataBaseFieldName]);
                        }
                        else
                        {
                            dictGrandChildRowKeyValue.Add(column.DataBaseFieldName, null);
                        }

                        if (dictGrandChildTransactionUnitSecurityDDLFieldValue.ContainsKey(column.DataBaseFieldName))
                        {
                            dictGrandChildRowKeyValue[column.DataBaseFieldName] = dictGrandChildTransactionUnitSecurityDDLFieldValue[column.DataBaseFieldName];
                        }
                    }

                    aAppformChildDataDto.DictOneToManyFields.Add(aGrandchildAppTransactionUnitExDto.Id.ToString(), listGridnChildRow);
                }
            }
        }

        internal static Dictionary<string, object> GetTransactionUnitSecurityDDLFieldValue(AppTransactionUnitExDto appTransactionUnitExDto)
        {
            Dictionary<string, object> dictUnitSecurityDDLFieldNameAndValue = new Dictionary<string, object>();
            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            foreach (var field in appTransactionUnitExDto.AppTransactionFieldList)
            {
                if ((field.ControlType == (int)EmAppControlType.DDL
                    || field.ControlType == (int)EmAppControlType.AutoComplete
                    || field.ControlType == (int)EmAppControlType.SearchAbleDDL)
                    && field.EntityId.HasValue)
                {
                    //if (!isAdmin)
                    //{
                    //    if (AppSecurityRegDomainBL.EntityInfoIds.Contains(field.EntityId.Value))
                    //    {
                    //        dictUnitSecurityDDLFieldNameAndValue.Add(field.DataBaseFieldName, AppSecurityUserBL.CurrentUserEntity.BusinessUserId);
                    //    }
                    //}
                }
            }

            return dictUnitSecurityDDLFieldNameAndValue;
        }





    }
}