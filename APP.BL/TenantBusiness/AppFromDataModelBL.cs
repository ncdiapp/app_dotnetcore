using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.Conversion;
using DatabaseSchemaMrg.DataSchema;
using DatabaseSchemaMrg.SqlGen;
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
    public static class AppFromDataModelBL
    {



        static AppFromDataModelBL()
        {

        }


        public static OperationCallResult<AppFormExDto> ExtractTranscationModelFromAppForm(object formId, bool isNeedToCreatePhysicalModelTables, int? saasApplicationId, FormPublishSettingDto formPublishSettingDto)
        {
            OperationCallResult<AppFormExDto> aOperationCallResult = new OperationCallResult<AppFormExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            var appFormExDto = AppFormBL.RetrieveOneAppFormExDto(formId);

            //if (isNeedToCreatePhysicalModelTables)
            //{
            //    OperationCallResult<AppFormExDto> publishValidationResult = AppFormFlexLayoutBL.FlexFormPublishValidation(appFormExDto);

            //    if (publishValidationResult.ValidationResult.HasErrors)
            //    {
            //        return publishValidationResult;
            //    }
            //}

            if (appFormExDto.AssociatedTransactionId.HasValue)
            {
                AppTransactionBL.DeleteOneAppTransaction(appFormExDto.AssociatedTransactionId.Value, false);
            }


            AppTransactionExDto aNewAppTransactionExDto = new AppTransactionExDto();
            aNewAppTransactionExDto.TransactionName = appFormExDto.Name;
            aNewAppTransactionExDto.DataSourceFrom = ServerContext.Instance.CurrnetClientIdentity.DataSourceId;
            aNewAppTransactionExDto.Description = " From Form:" + appFormExDto.Name + " :" + appFormExDto.Description;
            aNewAppTransactionExDto.TransactionOrganizedType = (int)EmTransactionOrganizedType.MasterDetail;
            aNewAppTransactionExDto.FormId = appFormExDto.Id as int?;
            aNewAppTransactionExDto.EmAppTransBusinessType = (int)EmAppTransBusinessType.FormData;
            aNewAppTransactionExDto.EmGrandChildEditMode = (int)EmAppGrandChildEditMode.SubGrid;
            aNewAppTransactionExDto.IsPhysicalModelTableCreated = isNeedToCreatePhysicalModelTables;
            aNewAppTransactionExDto.IsShowSaveButton = true;
            aNewAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap = new Dictionary<Guid, Guid>();
            aNewAppTransactionExDto.SaasApplicationId = saasApplicationId;


            // hairarchy  Transaction  unit list
            aNewAppTransactionExDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();


            string tableMiddleName = FilterSQLDBInvalidChar(appFormExDto.Name);
            string rootTablename = tableMiddleName;

            string unitdisplayName = appFormExDto.Name;

            AppTransactionUnitExDto rootUnit = CreateOneUnit(rootTablename, unitdisplayName);
            aNewAppTransactionExDto.AppTransactionUnitList.Add(rootUnit);



            rootUnit.AppTransactionFieldList = new ObservableSet<AppTransactionFieldExDto>();

            var rootDataElements = appFormExDto.AppFormLayoutItemList.Where
            (
                o => (o.DomAttribute.TranscationUnitLevel.HasValue && o.DomAttribute.TranscationUnitLevel.Value == (int)EmAppTransactionUnitLevel.Root)
                 && o.DomAttribute.IsBindingToDataField
             ).ToList();


            // ADD PK Guid
            Guid rootPkGuId = AddOnePKTranscationFiledToUnit(rootUnit);
            AddDataBindFieldListToOneUnit(rootUnit, rootDataElements);

            if (isNeedToCreatePhysicalModelTables)
            {
                if (formPublishSettingDto != null && formPublishSettingDto.IsCreateFolderNavigation)
                {
                    if (rootUnit.AppTransactionFieldList.FirstOrDefault(o => o.DataBaseFieldName.ToLower() == "folderid") == null)
                    {
                        AddFolderIdFieldToRootUnit(rootUnit);
                    }                    
                }
            }

            


            // Process child lement 


            List<AppFormLayoutItemExDto> childGridHostElements = appFormExDto.AppFormLayoutItemList.Where
                   (
                       o => o.DomAttribute.TranscationUnitLevel.HasValue && o.DomAttribute.TranscationUnitLevel.Value == (int)EmAppTransactionUnitLevel.Child

                    ).ToList();






            Dictionary<int, Guid> dictHostElmentIdUnitGuid = new Dictionary<int, Guid>();
            if (!childGridHostElements.IsEmpty())
            {
                rootUnit.Children = new List<AppTransactionUnitExDto>();

                foreach (AppFormLayoutItemExDto hostElmentItem in childGridHostElements)
                {
                    AppTransactionUnitExDto childUnit = ExtractUnitFromGridHostElement(hostElmentItem, 2);


                    dictHostElmentIdUnitGuid[(int)hostElmentItem.Id] = childUnit.TransactionUnitIentityGuid.Value;


                    Guid childPkGuId = AddOnePKTranscationFiledToUnit(childUnit);
                    Guid linkToParentPrimaryKeyFieldIdChildGuid = AddLinkToParentPrimaryKeyFieldIdToUnit(childUnit, rootPkGuId);
                    aNewAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap[linkToParentPrimaryKeyFieldIdChildGuid] = rootPkGuId;

                    rootUnit.Children.Add(childUnit);

                    // 



                    List<AppFormLayoutItemExDto> childElements = appFormExDto.AppFormLayoutItemList
                    .Where(o => o.UigridLayoutParentId == hostElmentItem.Id as int?).ToList();

                    // create child unit with Data Fields
                    AddDataBindFieldListToOneUnit(childUnit, childElements);

                    // create grandvchild unit with Data Fields
                    List<AppFormLayoutItemExDto> grandChildDataBidingElements = childElements.Where(o => o.DomAttribute.TranscationUnitLevel.HasValue
                    && o.DomAttribute.TranscationUnitLevel.Value == (int)EmAppTransactionUnitLevel.Grandchild)
                    .ToList();

                    if (!grandChildDataBidingElements.IsEmpty())
                    {
                        foreach (AppFormLayoutItemExDto grandChildHostElmentItem in grandChildDataBidingElements)
                        {
                            AppTransactionUnitExDto grandChildUnit = ExtractUnitFromGridHostElement(grandChildHostElmentItem, 3);

                            Guid grandChildPkGuId = AddOnePKTranscationFiledToUnit(grandChildUnit);
                            Guid linkToParentPrimaryKeyFieldIdGrandChildGuid = AddLinkToParentPrimaryKeyFieldIdToUnit(grandChildUnit, childPkGuId);
                            aNewAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap[linkToParentPrimaryKeyFieldIdGrandChildGuid] = childPkGuId;


                            childUnit.Children.Add(grandChildUnit);


                            List<AppFormLayoutItemExDto> grandChildElements = appFormExDto.AppFormLayoutItemList
                            .Where(o => o.UigridLayoutParentId == grandChildHostElmentItem.Id as int?).ToList();

                            // create child unit with Data Fields
                            AddDataBindFieldListToOneUnit(grandChildUnit, grandChildElements);


                        }

                    }


                }

            }

            if (isNeedToCreatePhysicalModelTables)
            {
                if (formPublishSettingDto != null)
                {
                    if (formPublishSettingDto.IsEnableComunication)
                    {
                        aNewAppTransactionExDto.IsNeedToSetComunication = true;
                    }

                   
                }
            }


            bool isIgnoreTransactionSaveValidation = !isNeedToCreatePhysicalModelTables;
            OperationCallResult<AppTransactionExDto> result = AppTransactionBL.SaveAppTransactionExDto(aNewAppTransactionExDto, isIgnoreTransactionSaveValidation);


            if (result.IsSuccessfulWithResult)
            {
                AppTransactionExDto transactionDto = result.Object;
                int? transactionId = ControlTypeValueConverter.ConvertValueToInt(transactionDto.Id);

                if (transactionId.HasValue)
                {
                  

                    var flexFormExDto = AppFormFlexLayoutBL.RetrieveOneAppFormFlexLayoutExDto(formId);
                    flexFormExDto.SaasApplicationId = saasApplicationId;
                    aOperationCallResult.Object = flexFormExDto;

                    if (isNeedToCreatePhysicalModelTables)
                    {
                        
                        bool extractTableResult = ExtractTableFromTransaction(result.Object.Id);

                        if (extractTableResult)
                        {
                            //aValidationResult.Items.Add(new ValidationItem(typeof(AppFormExDto), "App_Form_Publish_OK", ValidationItemType.Message, "Publish Successful"));
                            //CreateTransationSimpleSearch(aValidationResult, transactionDto);

                            if (formPublishSettingDto.IsCreateApplicationMenu)
                            {
                                // OperationCallResult<bool> createMenuResult = FormPublishPostProcess_CreateApplicationMenu(formExDto, newSearchId, isFolderNavigationCreated, newListEditTransactionIdList);

                                var createMenuResult = AppDatabaseViewBL.QuickGenerateTransactionDefaultSeachNavigation(transactionId.Value);

                                if (!createMenuResult.IsSuccessful)
                                {
                                    aValidationResult.Merge(createMenuResult.ValidationResult);

                                    var savedTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId.Value);

                                    foreach (var unitDto in savedTransactionExDto.DictAllTransactionUnitIdExDto.Values)
                                    {
                                        if (!string.IsNullOrWhiteSpace(unitDto.DataBaseTableName))
                                        {
                                            var dbTableDto = AppMetaDataBL.GetOneDatabaseTableSchema(unitDto.DataBaseTableName, savedTransactionExDto.DataSourceFrom, "");
                                            string errorMsg = AppMetaDataBL.DropDatabaseTable(dbTableDto);
                                        }
                                    }

                                    AppTransactionBL.DeleteOneAppTransaction(transactionId);
                                }
                            }

                            if (!aValidationResult.HasErrors)
                            { 
                                UpdateFormLayoutFormTrascationField(transactionId, dictHostElmentIdUnitGuid);
                            }
                        }
                        else
                        {
                            AppTransactionBL.DeleteOneAppTransaction(transactionId);
                        }
                    }
                    else
                    {
                        //aValidationResult.Items.Add(new ValidationItem(typeof(AppFormExDto), "App_Form_Save_OK", ValidationItemType.Message, "Save Successful"));

                        UpdateFormLayoutFormTrascationField(transactionId, dictHostElmentIdUnitGuid);
                    }
                }
            }
            else
            {
                aValidationResult.Merge(result.ValidationResult);
            }



            return aOperationCallResult;
        }

        private static void CreateTransationSimpleSearch(ValidationResult aValidationResult, AppTransactionExDto transactionDto)
        {
            OperationCallResult<DatabaseViewUpdateDto> searchCreationResult = AppDatabaseViewBL.CreateSimpleSearchViewFromMasterDetailTransactoin((int)transactionDto.Id);

            if (searchCreationResult.IsSuccessfulWithResult)
            {
                AppTreeListMenuBL.AddSearchToMainMenu(searchCreationResult.Object.SearchId, transactionDto.TransactionName, false);
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppFormExDto), "App_Form_CreateSearch_error", ValidationItemType.Error, "Create Search Failed"));
            }
        }

        private static void UpdateFormLayoutFormTrascationField(object transcaioId, Dictionary<int, Guid> dictHostElmentIdUnitGuid)
        {
            AppTransactionEntity aAppTransactionEntity = AppTransactionBL.RetrieveOneAppTransactionEntity(transcaioId);

            Dictionary<Guid, int> dictChildUnitGuid_PKId = aAppTransactionEntity.AppTransactionUnit
            .Where(o => (o.TransactionUnitIentityGuid.HasValue))
            .ToDictionary(o => o.TransactionUnitIentityGuid.Value, o => o.TransactionUnitId);

            List<Tuple<int, int>> layOutIdFieldId = new List<Tuple<int, int>>();


            foreach (var unit in aAppTransactionEntity.AppTransactionUnit)
            {
                foreach (var field in unit.AppTransactionField)
                {
                    if (field.HostFormLayoutItemId.HasValue)
                    {
                        layOutIdFieldId.Add(new Tuple<int, int>(field.HostFormLayoutItemId.Value, field.TransactionFieldId));
                    }
                }
            }


            using (DataAccessAdapter adaptere = AppTenantAdapterBL.GetTenantAdapter())
            {
                foreach (var lauoutIdTranscationFieldId in layOutIdFieldId)
                {
                    AppFormLayoutItemEntity aItem = new AppFormLayoutItemEntity();
                    aItem.TransactionFieldId = lauoutIdTranscationFieldId.Item2;

                    adaptere.UpdateEntitiesDirectly(aItem, new RelationPredicateBucket(AppFormLayoutItemFields.FormLayoutItemId == lauoutIdTranscationFieldId.Item1));

                }


                foreach (int lauoutId in dictHostElmentIdUnitGuid.Keys)
                {

                    Guid unitGuid = dictHostElmentIdUnitGuid[lauoutId];

                    if (dictChildUnitGuid_PKId.ContainsKey(unitGuid))
                    {
                        AppFormLayoutItemEntity aItem = new AppFormLayoutItemEntity();

                        aItem.GridTransactionUnitId = dictChildUnitGuid_PKId[unitGuid];

                        adaptere.UpdateEntitiesDirectly(aItem, new RelationPredicateBucket(AppFormLayoutItemFields.FormLayoutItemId == lauoutId));

                    }


                }


            }
        }

        private static void AddAutoIdentityPkfield(DatabaseTable databaseTableDto, AppTransactionFieldEntity pkField)
        {
            DatabaseColumn primaryColumn = new DatabaseColumn();

            primaryColumn.Name = pkField.DataBaseFieldName;

            // Need DB DataType
            primaryColumn.DbDataType = "int";
            primaryColumn.IsAutoNumber = true;
            primaryColumn.IsPrimaryKey = true;
            primaryColumn.Nullable = false;
            primaryColumn.Tag = ((EmAppDataType)pkField.DataType.Value).ToString();

            databaseTableDto.Columns.Add(primaryColumn);
        }

        public static bool ExtractTableFromTransaction(object transactionId)
        {

            // all database table from scrac hcreation will save to user master DB
            var appTransactionEntity = AppTransactionBL.RetrieveOneAppTransactionEntity(transactionId);

            foreach (var unitEntity in appTransactionEntity.AppTransactionUnit)
            {

                DatabaseTable databaseTableDto = new DatabaseTable();

                unitEntity.DataBaseTableName = unitEntity.DataBaseTableName + "_" + ExtensionMethodhelper.RandomId();

                // make sure no deplciated table name
                databaseTableDto.Name = unitEntity.DataBaseTableName;
                databaseTableDto.DataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId;

                var pkField = unitEntity.AppTransactionField.Where(o => o.IsPrimaryKey).FirstOrDefault();
                AddAutoIdentityPkfield(databaseTableDto, pkField);


                foreach (var transField in unitEntity.AppTransactionField.Where(o => !o.IsPrimaryKey))
                {
                    DatabaseColumn aDatabaseColumn = new DatabaseColumn();
                    //databaseTableDto.Columns.Add(aDatabaseColumn);
                    aDatabaseColumn.Name = transField.DataBaseFieldName;
                    aDatabaseColumn.Tag = ((EmAppDataType)transField.DataType.Value).ToString();
                    databaseTableDto.Columns.Add(aDatabaseColumn);
                }

                AddSystemCreatedAndModifiedByColumns(databaseTableDto);


                //CreateDataBasePhsicalTable(databaseTableDto);
                string createTableResultMsg = "";
                AppMetaDataBL.CreateNewTable(databaseTableDto, databaseTableDto.DataSourceRegisterId, appTransactionEntity.SaasApplicationId, out createTableResultMsg);

            }

            AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(ServerContext.Instance.CurrnetClientIdentity.DataSourceId);


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(appTransactionEntity);
                    adapter.Commit();

                    AppCacheManagerBL.RefreshOnetHierarchyTranscation(appTransactionEntity.TransactionId);

                  
                    return true;
                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                }
            }

            return false;         

        }

        internal static void AddSystemCreatedAndModifiedByColumns(DatabaseTable databaseTableDto)
        {
            if (databaseTableDto.Columns.FirstOrDefault(o => o.Name.ToLower() == "AppCreatedByID".ToLower()) == null)
            {
                DatabaseColumn aDatabaseColumn = new DatabaseColumn();
                aDatabaseColumn.Name = "AppCreatedByID";
                aDatabaseColumn.Tag = (EmAppDataType.Integer).ToString();
                databaseTableDto.Columns.Add(aDatabaseColumn);
            }

            if (databaseTableDto.Columns.FirstOrDefault(o => o.Name.ToLower() == "AppCreatedDate".ToLower()) == null)
            {
                DatabaseColumn aDatabaseColumn = new DatabaseColumn();
                aDatabaseColumn.Name = "AppCreatedDate";
                aDatabaseColumn.Tag = (EmAppDataType.DateTime).ToString();
                databaseTableDto.Columns.Add(aDatabaseColumn);
            }

            if (databaseTableDto.Columns.FirstOrDefault(o => o.Name.ToLower() == "AppModifiedByID".ToLower()) == null)
            {
                DatabaseColumn aDatabaseColumn = new DatabaseColumn();
                aDatabaseColumn.Name = "AppModifiedByID";
                aDatabaseColumn.Tag = (EmAppDataType.Integer).ToString();
                databaseTableDto.Columns.Add(aDatabaseColumn);
            }

            if (databaseTableDto.Columns.FirstOrDefault(o => o.Name.ToLower() == "AppModifiedDate".ToLower()) == null)
            {
                DatabaseColumn aDatabaseColumn = new DatabaseColumn();
                aDatabaseColumn.Name = "AppModifiedDate";
                aDatabaseColumn.Tag = (EmAppDataType.DateTime).ToString();
                databaseTableDto.Columns.Add(aDatabaseColumn);
            }

            if (databaseTableDto.Columns.FirstOrDefault(o => o.Name.ToLower() == "AppCreatedByCompanyID".ToLower()) == null)
            {
                DatabaseColumn aDatabaseColumn = new DatabaseColumn();
                aDatabaseColumn.Name = "AppCreatedByCompanyID";
                aDatabaseColumn.Tag = (EmAppDataType.Integer).ToString();
                databaseTableDto.Columns.Add(aDatabaseColumn);
            }
        }

        private static Guid AddOnePKTranscationFiledToUnit(AppTransactionUnitExDto oneUnit)
        {
            AppTransactionFieldExDto pkTransactionfield = new AppTransactionFieldExDto();
            pkTransactionfield.IsPrimaryKey = true;
            pkTransactionfield.DataType = (int)EmAppDataType.Integer;
            pkTransactionfield.RowIdentityGuid = System.Guid.NewGuid();
            pkTransactionfield.DataBaseFieldName = "PK" + oneUnit.DataBaseTableName + "ID";
            pkTransactionfield.DisplayName = pkTransactionfield.DataBaseFieldName;
            pkTransactionfield.ControlType = (int)EmAppControlType.Numeric;
            pkTransactionfield.Nbdecimal = 0;
            pkTransactionfield.IsReadonly = true;
            pkTransactionfield.IsAllowEmpty = true;
            pkTransactionfield.IsVisible = false;

            oneUnit.AppTransactionFieldList.Add(pkTransactionfield);
            return pkTransactionfield.RowIdentityGuid.Value;
        }

        private static Guid AddLinkToParentPrimaryKeyFieldIdToUnit(AppTransactionUnitExDto oneUnit, Guid? parentPKFieldGuid)
        {
            AppTransactionFieldExDto linkToParentPrimaryKeyFieldId = new AppTransactionFieldExDto();

            linkToParentPrimaryKeyFieldId.DataType = (int)EmAppDataType.Integer;
            linkToParentPrimaryKeyFieldId.RowIdentityGuid = System.Guid.NewGuid();
            linkToParentPrimaryKeyFieldId.DataBaseFieldName = "LinkToParentPrimaryKeyField" + oneUnit.DataBaseTableName + "ID";
            linkToParentPrimaryKeyFieldId.IsLinkToParentPrimaryKey = true;
            linkToParentPrimaryKeyFieldId.ParentPKFieldGuid = parentPKFieldGuid;
            linkToParentPrimaryKeyFieldId.DisplayName = linkToParentPrimaryKeyFieldId.DataBaseFieldName;
            linkToParentPrimaryKeyFieldId.ControlType = (int)EmAppControlType.Numeric;
            linkToParentPrimaryKeyFieldId.Nbdecimal = 0;
            linkToParentPrimaryKeyFieldId.IsReadonly = true;
            linkToParentPrimaryKeyFieldId.IsAllowEmpty = true;
            linkToParentPrimaryKeyFieldId.IsVisible = false;

            oneUnit.AppTransactionFieldList.Add(linkToParentPrimaryKeyFieldId);
            return linkToParentPrimaryKeyFieldId.RowIdentityGuid.Value;
        }

        private static void AddDataBindFieldListToOneUnit(AppTransactionUnitExDto unitDto, List<AppFormLayoutItemExDto> childElements)
        {
            List<AppFormLayoutItemExDto> childDataBidingElements = childElements.Where(o => o.DomAttribute.IsBindingToDataField
                    && (!o.DomAttribute.TranscationUnitLevel.HasValue || o.DomAttribute.TranscationUnitLevel.Value == (int)EmAppTransactionUnitLevel.Root)).ToList();

            foreach (AppFormLayoutItemExDto layoutItemDto in childDataBidingElements)
            {
                AppFormDomAttributeDto childElementAtt = layoutItemDto.DomAttribute;
                AddDataBingFiledToUnit(unitDto, childElementAtt, layoutItemDto.Id as int?);
            }
        }
        //UDF_Root_
        private static AppTransactionUnitExDto ExtractUnitFromGridHostElement(AppFormLayoutItemExDto hostElmentItem, int level)
        {
            AppFormDomAttributeDto elementAtt = hostElmentItem.DomAttribute;
            string childTableMiddleName = FilterSQLDBInvalidChar(elementAtt.DisplayName);
            string childTablename = childTableMiddleName;
            string childUnitdisplayName = elementAtt.DisplayName;
            AppTransactionUnitExDto childUnit = CreateOneUnit(childTablename, childUnitdisplayName);
            return childUnit;
        }

        private static void AddDataBingFiledToUnit(AppTransactionUnitExDto rootUnit, AppFormDomAttributeDto elementAtt, int? layoutId)
        {
            AppTransactionFieldExDto transactioField = new AppTransactionFieldExDto();
            rootUnit.AppTransactionFieldList.Add(transactioField);
            transactioField.DataBaseFieldName = FilterSQLDBInvalidChar(elementAtt.DisplayName);
            transactioField.DataType = (int)elementAtt.DataType;
            transactioField.ControlType = elementAtt.WidgetDisplayType.Value;
            transactioField.Nbdecimal = 0;
            transactioField.EntityId = elementAtt.EntityId;

            if (elementAtt.WidgetDisplayType.Value == (int)EmAppFormLayoutItemType.Integer)
            {
                transactioField.ControlType = (int)EmAppControlType.Numeric;
            }

            if (elementAtt.WidgetDisplayType.Value == (int)EmAppFormLayoutItemType.Numeric)
            {
                transactioField.Nbdecimal = 2;
            }

            transactioField.DisplayName = elementAtt.DisplayName;

            transactioField.DefaultValue = elementAtt.DefaultValue;
            transactioField.RowIdentityGuid = System.Guid.NewGuid();
            transactioField.HostFormLayoutItemId = layoutId;
            // using SiblingUnitLogicalKeyFieldID as temp keep 
            // SiblingUnitLogicalKeyFieldID
            transactioField.IsReadonly = false;
            transactioField.IsAllowEmpty = true;
            transactioField.IsVisible = true;

            rootUnit.AppTransactionFieldList.Add(transactioField);
        }

        private static void AddFolderIdFieldToRootUnit(AppTransactionUnitExDto rootUnit)
        {
            AppTransactionFieldExDto transactioField = new AppTransactionFieldExDto();
            rootUnit.AppTransactionFieldList.Add(transactioField);
            transactioField.DataBaseFieldName = "FolderID";
            transactioField.DataType = (int)EmAppDataType.Integer;
            transactioField.ControlType = (int)EmAppControlType.Numeric;
            transactioField.DisplayName = "FolderID";

            transactioField.DefaultValue = null;
            transactioField.RowIdentityGuid = System.Guid.NewGuid();
            transactioField.HostFormLayoutItemId = null;     
            transactioField.IsReadonly = false;
            transactioField.IsAllowEmpty = true;
            transactioField.IsVisible = true;

            rootUnit.AppTransactionFieldList.Add(transactioField);
        }   

        private static AppTransactionUnitExDto CreateOneUnit(string unitTableName, string unitDisplayName)
        {
            AppTransactionUnitExDto oneUnit = new AppTransactionUnitExDto();
            string schmeOner = AppMetaDataBL.GetCurrentDbConnectionDefaultSchmeOner(ServerContext.Instance.CurrnetClientIdentity.DataSourceId);
            oneUnit.UnitDisplayName = unitDisplayName;
            oneUnit.DataBaseTableName = unitTableName;
            oneUnit.IsPrimaryKeyIdentityInsert = true;
            oneUnit.SchemaOwner = schmeOner;
            oneUnit.Level = 1;
            oneUnit.IsPrimaryKeyIdentityInsert = true;

            // temp to stor
            oneUnit.TransactionUnitIentityGuid = System.Guid.NewGuid();

            return oneUnit;
        }

        public static string FilterSQLDBInvalidChar(string tabGridColumnName)
        {
            return tabGridColumnName.
                Replace(' ', '_')
                .Replace('(', '_')
                .Replace(')', '_')
                .Replace('-', '_')
                .Replace('&', '_')
                .Replace("'", "")
                .Replace("#", "")
                .Replace("/", "_")
                .Replace('\\', '_')
                .Replace('.', '_')
                .Replace('$', '_')
                .Replace('*', '_')
                .Replace('%', '_')
                .Replace(',', '_')
                .Replace('+', '_')
                .Replace('[', '_')
                .Replace(']', '_')
                .Replace('\uFF08', '_')
                .Replace('\uFF09', '_')
                .Replace("delete", "DLT_")
                .Replace("FROM", "FRM")
                .Replace("SELECT", "SLCT"); ;
        }












    }


}