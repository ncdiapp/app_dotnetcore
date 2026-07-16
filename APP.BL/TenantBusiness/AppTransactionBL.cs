using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.Text.RegularExpressions;
using System;
using DatabaseSchemaMrg.DataSchema;
using System.Data.SqlClient;
using DatabaseSchemaMrg;

using NJsonSchema;

//



using APP.Framework;
namespace App.BL
{
    public static class AppTransactionBL
    {
        public static readonly string App_TransactionEntity_Save_OK = "App_TransactionEntity_Save_OK";
        public static readonly string App_TransactionEntity_Save_Failed = "App_TransactionEntity_Save_Failed";
        public static readonly string App_TransactionEntityUILayout_Save_OK = "App_TransactionEntityUILayout_Save_OK";
        public static readonly string App_TransactionEntityUILayout_Save_Failed = "App_TransactionEntityUILayout_Save_Failed";
        public static readonly string App_TransactionEntity_Delete_Ok = "App_TransactionEntity_Delete_Ok";
        public static readonly string App_TransactionEntity_Delete_Failed = "App_TransactionEntity_Delete_Failed";

        public static readonly string AppUserDefineTablePrefix = "UDF";
        public static readonly string JsonSchemaMappingSplit = "___";


        public static List<AppTransactionDto> RetrieveAllAppTransactionDto(bool? isSystemBuitIn, int? transactionOrganizedType = null, bool isIncludeWorkflow = false)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionEntity> list = new EntityCollection<AppTransactionEntity>();

                SortExpression expression = new SortExpression(AppTransactionFields.TransactionName | SortOperator.Ascending);
                PrefetchPath2 path = new PrefetchPath2(EntityType.AppTransactionEntity);
                path.Add(AppTransactionEntity.PrefetchPathAppTransactionNavigation);


                RelationPredicateBucket filter = new RelationPredicateBucket();
                if (isSystemBuitIn.HasValue)
                {
                    if (isSystemBuitIn.Value)
                    {
                        filter.PredicateExpression.Add(AppTransactionFields.IsSystemBuitIn == isSystemBuitIn.Value);

                    }
                    else
                    {
                        filter.PredicateExpression.Add(AppTransactionFields.IsSystemBuitIn == isSystemBuitIn.Value | AppTransactionFields.IsSystemBuitIn == DBNull.Value);
                    }
                }

                if (transactionOrganizedType.HasValue)
                {
                    filter.PredicateExpression.AddWithAnd(AppTransactionFields.TransactionOrganizedType == transactionOrganizedType.Value);
                }
                
                if (!isIncludeWorkflow)
                {
                    filter.PredicateExpression.AddWithAnd(AppTransactionFields.BusinessScopeId == DBNull.Value | AppTransactionFields.BusinessScopeId != (int)EmAppTransactionScopeUsage.WorkflowAutomation);
                }

                //if (folderUsageType.HasValue)
                //{
                //    filter.PredicateExpression.AddWithAnd(AppTransactionFields.FolderUsageType == folderUsageType.Value);
                //}





                adapter.FetchEntityCollection(list, filter, 0, expression, path);

                var aDtoList = new List<AppTransactionDto>();

                foreach (var o in list)
                {
                    var transDto = AppTransactionConverter.ConvertEntityToDto(o);
                    aDtoList.Add(transDto);

                    if (o.AppTransactionNavigation != null && o.AppTransactionNavigation.Count > 0)
                    {
                        foreach (var navigationEntity in o.AppTransactionNavigation)
                        {
                            if (navigationEntity.QuickSearchId.HasValue)
                            {
                                transDto.DefaultNavigationSearchId = navigationEntity.QuickSearchId.Value;
                            }
                            else if (navigationEntity.FolderViewId.HasValue)
                            {
                                transDto.DefaultNavigationFolderViewId = navigationEntity.FolderViewId.Value;
                            }
                        }
                    }

                    transDto.CreatedFromDisplay = EmTransactionCreatedFrom.Database.ToString();

                    if (transDto.OtherOptions != null)
                    {
                        if (transDto.OtherOptions.IsApiIntegrationTransaction)
                        {
                            transDto.CreatedFromDisplay = EmTransactionCreatedFrom.Json.ToString();
                        }
                        else if (transDto.OtherOptions.ImportSettingId.HasValue)
                        {
                            transDto.CreatedFromDisplay = EmTransactionCreatedFrom.Excel.ToString();
                        }
                    }
                }

                return aDtoList;
            }
        }



        public static List<AppTransactionDto> RetrieveSaasApplicationWorkflowAutomationList(int? applicationId, bool isIncludeDraftImportSetting = false)
        {
            List<AppTransactionDto> toReturn = new List<AppTransactionDto>();

             List<AppTransactionDto> allTransactionList = RetrieveAllAppTransactionDto(null, (int)EmTransactionOrganizedType.MasterDetail, true)
                    .Where(o=>o.BusinessScopeId.HasValue && o.BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation).ToList();

            if (applicationId.HasValue)
            {

                EntityCollection<AppApplicationAssetsItemEntity> list = AppSaasUserApplicationPackageBL.RetrieveAppApplicationAssetsItemEntityListByType(applicationId.Value, (int)EmAppApplicationAssetsType.Transaction);

                List<int> importedTransactionIdList = list.Where(o => o.TransactionId.HasValue).Select(o => o.TransactionId.Value).Distinct().ToList();

                toReturn = allTransactionList.Where(o => (o.SaasApplicationId.HasValue && o.SaasApplicationId.Value == applicationId.Value) || importedTransactionIdList.Contains((int)o.Id)).ToList();
            }
            else
            {
                toReturn = allTransactionList;
            }

            return toReturn;
        }

        public static List<AppTransactionDto> RetrieveSaasApplicationTransactionList(int? applicationId, bool isIncludeDraftImportSetting = false)
        {
            List<AppTransactionDto> toReturn = new List<AppTransactionDto>();

            if (applicationId.HasValue)
            {
                List<AppTransactionDto> allTransactionList = RetrieveAllAppTransactionDto(null);


                EntityCollection<AppApplicationAssetsItemEntity> list = AppSaasUserApplicationPackageBL.RetrieveAppApplicationAssetsItemEntityListByType(applicationId.Value, (int)EmAppApplicationAssetsType.Transaction);

                List<int> importedTransactionIdList = list.Where(o => o.TransactionId.HasValue).Select(o => o.TransactionId.Value).Distinct().ToList();

                toReturn = allTransactionList.Where(o=>!(o.BusinessScopeId.HasValue && o.BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation))
                    .Where(o => (o.SaasApplicationId.HasValue && o.SaasApplicationId.Value == applicationId.Value) || importedTransactionIdList.Contains((int)o.Id)).ToList();

                Dictionary<int, AppFormDto> dictFormIdAndDto = AppFormBL.RetrieveAllAppFormDtoByCreateFromType(null, true).ToDictionary(o => (int)o.Id, o => o);

                foreach (var o in toReturn)
                {
                    if (o.FormId.HasValue && dictFormIdAndDto.ContainsKey(o.FormId.Value))
                    {
                        o.FormLayoutType = dictFormIdAndDto[o.FormId.Value].LayoutType;
                    }

                    if (o.PrintFormId.HasValue && dictFormIdAndDto.ContainsKey(o.PrintFormId.Value))
                    {
                        o.PrintFormLayoutType = dictFormIdAndDto[o.PrintFormId.Value].LayoutType;
                    }


                }

                if (isIncludeDraftImportSetting)
                {
                    List<AppDataSetExDto> importSettings = AppDatabaseTableImportBL.RetrieveSaasApplicationExcelTableImportSettingList(applicationId.Value, false);

                    foreach (var dataSetDto in importSettings)
                    {
                        if (dataSetDto.OtherSettingsDto != null && dataSetDto.OtherSettingsDto.TableImportSettingDto != null)
                        {
                            var importSettingDto = dataSetDto.OtherSettingsDto.TableImportSettingDto;
                            if (importSettingDto.IsDraft && !importSettingDto.DefaultTransactionId.HasValue && !importSettingDto.NeedToUpdateTransactionId.HasValue
                                && !importSettingDto.IsEntityImport)
                            {
                                AppTransactionDto placeholderTransactionDto = new AppTransactionDto();
                                placeholderTransactionDto.TransactionName = dataSetDto.Name;
                                placeholderTransactionDto.Description = dataSetDto.Description;
                                placeholderTransactionDto.TransactionOrganizedType = (int)EmTransactionOrganizedType.ImportDraft;
                                placeholderTransactionDto.OtherOptions = new TransactionOptionDto();
                                placeholderTransactionDto.OtherOptions.TransactionDataUpdateImportSettingId = (int)dataSetDto.Id;
                                placeholderTransactionDto.OtherOptions.ImportSettingId = (int)dataSetDto.Id;
                                placeholderTransactionDto.OtherOptions.IsDraft = true;
                                placeholderTransactionDto.AppCreatedById = dataSetDto.AppCreatedById;
                                placeholderTransactionDto.AppCreatedDate = dataSetDto.AppCreatedDate;
                                placeholderTransactionDto.AppModifiedById = dataSetDto.AppModifiedById;
                                placeholderTransactionDto.AppModifiedDate = dataSetDto.AppModifiedDate;
                                placeholderTransactionDto.DataSourceFrom = dataSetDto.DataSourceFrom;
                                placeholderTransactionDto.CreatedFromDisplay = EmTransactionCreatedFrom.Excel.ToString();

                                toReturn.Add(placeholderTransactionDto);
                            }
                        }
                    }
                }
            }

            return toReturn;
        }


        public static List<int> RetrieveAllAppTransactionIds()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionEntity> list = new EntityCollection<AppTransactionEntity>();

                IncludeFieldsList includeList = new IncludeFieldsList();
                includeList.Add(AppTransactionFields.TransactionId);

                adapter.FetchEntityCollection(list, includeList, null);

                List<int> idlistt = new List<int>();

                foreach (var o in list)
                {
                    idlistt.Add(o.TransactionId);
                }

                return idlistt;
            }
        }
        public static AppTransactionExDto RetrieveOneAppTransactionExDto(object transcactionId, int? rootWorkflowTransactionId = null)
        {
             int? transcactionIdint = ControlTypeValueConverter.ConvertValueToInt(transcactionId);
             if (! transcactionIdint.HasValue) return null;

            AppTransactionEntity aAppTransactionEntity = RetrieveOneAppTransactionEntity(transcactionId);
            if (aAppTransactionEntity == null) return null;


            AppTransactionExDto aAppTransactionExDto = ConvertTransactionEntityToExdto( aAppTransactionEntity);

            if (aAppTransactionEntity.FormId.HasValue)
            {
                var formEntity = AppFormBL.RetrieveSimpleAppFormEntity(aAppTransactionEntity.FormId.Value);
                aAppTransactionExDto.FormLayoutType = formEntity.LayoutType;
            }

            if (aAppTransactionEntity.PrintFormId.HasValue)
            {
                var formEntity = AppFormBL.RetrieveSimpleAppFormEntity(aAppTransactionEntity.PrintFormId.Value);
                aAppTransactionExDto.PrintFormLayoutType = formEntity.LayoutType;
            }

            SetupMappingToAvailableSourceUnitTransactionFieldExDto(aAppTransactionExDto);
                        
            aAppTransactionExDto.CommandActionList = AppTransactionCommandBL.RetrieveOneTransactionCommandActionList(aAppTransactionExDto, rootWorkflowTransactionId);

            PrepareApiTransactionProperties(aAppTransactionExDto);

            int? transId = ControlTypeValueConverter.ConvertValueToInt(transcactionId);

            var existingNavigationDto = AppTransactionNavigationBL.RetrieveOneTransactionDefaultNavigationDto(transId, false);

            if (existingNavigationDto != null && existingNavigationDto.DefaultMenuDto != null)
            {
                aAppTransactionExDto.DefaultNavigationSearchId = existingNavigationDto.QuickSearchId;
                aAppTransactionExDto.DefaultNavigationMenuId = (int)existingNavigationDto.DefaultMenuDto.Id;
            }


            return aAppTransactionExDto;
        }

        internal static AppTransactionExDto ConvertTransactionEntityToExdto( AppTransactionEntity aAppTransactionEntity)
        {
            if(aAppTransactionEntity == null ) return null;

            Dictionary<int, string> dictFiledIdDatabaseFiledName = new Dictionary<int, string>();
            foreach (AppTransactionUnitEntity unitEntity in aAppTransactionEntity.AppTransactionUnit)
            {

                foreach (var filedEntity in unitEntity.AppTransactionField)
                {
                    dictFiledIdDatabaseFiledName[filedEntity.TransactionFieldId] = filedEntity.DataBaseFieldName;


                }
            }

            AppTransactionExDto aAppTransactionExDto = AppTransactionConverter.ConvertEntityToExDto(aAppTransactionEntity);

            aAppTransactionExDto.FolderTransactionId = aAppTransactionEntity.FolderTransactionId;
            AssignDefaultTransactionSearchId(aAppTransactionEntity.TransactionId, aAppTransactionExDto);

            Dictionary<int, AppTransactionFieldExDto> dictTransFieldIdAndDto = new Dictionary<int, AppTransactionFieldExDto>();
            Dictionary<int, AppTransactionFieldAggFunctionExDto> dictAggFuncIdAndDto = new Dictionary<int, AppTransactionFieldAggFunctionExDto>();

            foreach (AppTransactionUnitEntity unitEntity in aAppTransactionEntity.AppTransactionUnit)
            {
                AppTransactionUnitExDto transactionUnitExdto = AppTransactionUnitConverter.ConvertEntityToExDto(unitEntity);

                aAppTransactionExDto.AppTransactionUnitList.Add(transactionUnitExdto);

                if (!aAppTransactionExDto.DataSourceFrom.HasValue)
                    aAppTransactionExDto.DataSourceFrom = ServerContext.Instance.DataSourceId;


                transactionUnitExdto.DataSourceFrom = aAppTransactionExDto.DataSourceFrom;

                // Available-source pool units are always treated as read-only.
                // Do not overwrite a persisted IsReadOnly=true when IsUsedForLoadingAvailableSource is false/null
                // (that was wiping Unit Editor "Is Read-Only" after save/reload).
                if (transactionUnitExdto.IsUsedForLoadingAvailableSource == true)
                {
                    transactionUnitExdto.IsReadOnly = true;
                }


                //	transactionUnitExdto.PrimaryKeyDbfieldList = new List<string>(); 

                transactionUnitExdto.DictLinkToParentKeyDbfield = new Dictionary<string, string>();

                foreach (var filedEntity in unitEntity.AppTransactionField)
                {
                    //if (!string.IsNullOrEmpty(unitEntity.PrimaryKeyDbfield) && filedEntity.DataBaseFieldName == unitEntity.PrimaryKeyDbfield)
                    //{
                    //    filedEntity.IsReadonly = true;
                    //}

                    if (filedEntity.IsPrimaryKey)
                    {


                        transactionUnitExdto.PrimaryKeyDbfieldList.Add(filedEntity.DataBaseFieldName);
                        if (transactionUnitExdto.IsPrimaryKeyIdentityInsert)
                        {
                            //transactionUnitExdto.PrimaryKeyDbfield = filedEntity.DataBaseFieldName;
                            filedEntity.IsReadonly = true;
                        }

                    }

                    // need 
                    if (filedEntity.LinkToParentPrimaryKeyFieldId.HasValue)
                    {
                        filedEntity.IsLinkToParentPrimaryKey = true;

                        transactionUnitExdto.DictLinkToParentKeyDbfield[filedEntity.DataBaseFieldName]
                            = dictFiledIdDatabaseFiledName[filedEntity.LinkToParentPrimaryKeyFieldId.Value];

                        filedEntity.IsReadonly = true;
                    }


                    AppTransactionFieldExDto aAppTransactionFieldExDto = AppTransactionFieldConverter.ConvertEntityToExDto(filedEntity);
                    transactionUnitExdto.AppTransactionFieldList.Add(aAppTransactionFieldExDto);

                    foreach (var aggfunction in filedEntity.AppTransactionFieldAggFunction_)
                    {

                        AppTransactionFieldAggFunctionExDto aAppTransactionFieldAggFunctionExDto = AppTransactionFieldAggFunctionConverter.ConvertEntityToExDto(aggfunction);
                        aAppTransactionFieldExDto.AppTransactionFieldAggFunction_List.Add(aAppTransactionFieldAggFunctionExDto);
                        dictAggFuncIdAndDto.Add((int)aAppTransactionFieldAggFunctionExDto.Id, aAppTransactionFieldAggFunctionExDto);

                    }




                    foreach (var conditionAction in filedEntity.AppConditionalAction__)
                    {

                        AppConditionalActionExDto appConditionalActionExDto = AppConditionalActionConverter.ConvertEntityToExDto(conditionAction);
                        aAppTransactionFieldExDto.AppConditionalAction__List.Add(appConditionalActionExDto);


                    }

                    dictTransFieldIdAndDto.Add((int)aAppTransactionFieldExDto.Id, aAppTransactionFieldExDto);
                }



                // need to add AppTransactionUnitFormula for caculation
                foreach (var formulaEntity in unitEntity.AppTransactionUnitFormula_)
                {
                    AppTransactionUnitFormulaExDto aAppTransactionUnitFormulaExDto = AppTransactionUnitFormulaConverter.ConvertEntityToExDto(formulaEntity);
                    transactionUnitExdto.AppTransactionUnitFormula_List.Add(aAppTransactionUnitFormulaExDto);



                }

                foreach (AppFormLinkTargetEntity linkTargetEntity in unitEntity.AppFormLinkTarget)
                {
                    AppFormLinkTargetExDto aAppFormLinkTargetExDto = AppFormLinkTargetConverter.ConvertEntityToExDto(linkTargetEntity);
                    transactionUnitExdto.AppFormLinkTargetList.Add(aAppFormLinkTargetExDto);
                }

                foreach (AppTransactionUnitLinkedSearchEntity linkedSearchEntity in unitEntity.AppTransactionUnitLinkedSearch)
                {

                    AppTransactionUnitLinkedSearchExDto aAppTransactionUnitLinkedSearchExDto = AppTransactionUnitLinkedSearchBL.ConvertOneEntityToDto(linkedSearchEntity);
                    transactionUnitExdto.AppTransactionUnitLinkedSearchList.Add(aAppTransactionUnitLinkedSearchExDto);
                }

            }


            foreach (AppTransactionDataLoadEntity dataLoadEntity in aAppTransactionEntity.AppTransactionDataLoad)
            {
                aAppTransactionExDto.AppTransactionDataLoadList.Add(AppTransactionDataLoadConverter.ConvertEntityToExDto(dataLoadEntity));
            }

            foreach (var commandEntity in aAppTransactionEntity.AppProjectWorkFlowAction)
            {
                aAppTransactionExDto.AppProjectWorkFlowActionList.Add(AppProjectWorkFlowActionConverter.ConvertEntityToExDto(commandEntity));
            }

            PrepareTransFieldCrossRelationSettingDictionary(aAppTransactionExDto, dictTransFieldIdAndDto, dictAggFuncIdAndDto);

            if (aAppTransactionExDto.TransactionFileStorageRootFolderId.HasValue)
            {
                var folderEntity = AppSeFolderBL.RetrieveOneAppSefolderEntity(aAppTransactionExDto.TransactionFileStorageRootFolderId.Value);
                if (folderEntity != null)
                {
                    aAppTransactionExDto.TransactionFileStorageRootFolderName = folderEntity.Name;
                }
            }




            return aAppTransactionExDto;
        }



        private static void AssignDefaultTransactionSearchId(object transcactionId, AppTransactionExDto aAppTransactionExDto)
        {
            if (aAppTransactionExDto.TransactionOrganizedType.HasValue)
            {
                int? folderSearchTransactionId = aAppTransactionExDto.FolderTransactionId;

                if (aAppTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.FolderList)
                {
                    folderSearchTransactionId = ControlTypeValueConverter.ConvertValueToInt(transcactionId);
                }

                if (folderSearchTransactionId.HasValue)
                {
                    var defaultFolderTransactionSearch = AppSearchConfigBL.RetrieveSearchEntityListByFolderTransactionId(folderSearchTransactionId.Value).FirstOrDefault();
                    if (defaultFolderTransactionSearch != null)
                    {
                        aAppTransactionExDto.DefaultTransactionSearchId = defaultFolderTransactionSearch.SearchId;
                    }
                }
            }
        }


        public static AppTransactionExDto GetCurrentUserOneHierarchyTransactionWithSecurityAndLangLable(object transcactionId, object rootPkId)
        {
            //  .DeepCopy() use stream sealize 	will not copy	[IgnoreDataMember] 
            ////AppTransactionExDto appTransactionExDto = GetHierarchyTranscationFromDatabase(transcactionId);
            //AppTransactionEntity aAppTransactionEntity = AppCacheManagerBL.GetOnetTranscationEntityFromCache(transcactionId);

            //AppTransactionExDto aAppTransactionExDto = ConvertTransactionEntityToExdto(transcactionId, aAppTransactionEntity);

            //SetupHierarchyUnit(aAppTransactionExDto);


            var aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transcactionId);


            AppSecurityManagementBL.ProcessCurrentUserTransactionAllSecurity(aAppTransactionExDto, rootPkId);


            return aAppTransactionExDto;
        }




        //public static AppTransactionExDto RetrieveOneAppTransactionExDto(object transcactionId)
        //{
        //	AppTransactionEntity aAppTransactionEntity = RetrieveOneAppTransactionEntity(transcactionId);
        //	AppTransactionExDto aAppTransactionExDto = ConvertTransactionEntityToExdto(transcactionId, aAppTransactionEntity);

        //	return aAppTransactionExDto;
        //}


        public static AppTransactionExDto GetHierarchyTranscationFromDatabase(object transcactionId, int? rootWorkflowTransactionId = null)
        {
            AppTransactionExDto appTransactionExDto = RetrieveOneAppTransactionExDto(transcactionId, rootWorkflowTransactionId);
            SetupHierarchyUnit(appTransactionExDto);

            PrepareUnitFormulaDictionary(appTransactionExDto);

            return appTransactionExDto;
        }

        private static void PrepareUnitFormulaDictionary(AppTransactionExDto appTransactionExDto)
        {
            Dictionary<int, AppTransactionUnitFormulaSetDto> dictUnitldIdAndFormulaSetDto = new Dictionary<int, AppTransactionUnitFormulaSetDto>();
            //Dictionary<int, AppTransactionFieldCrossRelationSettingDto> dictTransFieldIdAndCrossRelationSettingDto = new Dictionary<int, AppTransactionFieldCrossRelationSettingDto>();

            foreach (string unitIdStr in appTransactionExDto.DictAllTransactionUnitIdExDto.Keys)
            {
                var unitdto = appTransactionExDto.DictAllTransactionUnitIdExDto[unitIdStr];
                var aFormulaSetDto = AppTransactionFormulaSetupBL.RetrieveAppTransactionUnitFormulaSetDto(int.Parse(unitIdStr), (int)appTransactionExDto.Id);

                if (aFormulaSetDto != null)
                {
                    aFormulaSetDto.ListAppTransactionUnitFormula.ForAll(o =>
                    {
                        if (unitdto.AppTransactionUnitFormula_List != null)
                        {
                            var matchOrgFormula = unitdto.AppTransactionUnitFormula_List.FirstOrDefault(p => o.Id != null && p.Id != null && (int)p.Id == (int)o.Id);
                            if (matchOrgFormula != null)
                            {
                                o.AssignToTransFieldId = matchOrgFormula.AssignmentLeftSideFieldId;
                            }
                        }

                    });
                }

                dictUnitldIdAndFormulaSetDto.Add(int.Parse(unitIdStr), aFormulaSetDto);
            }

            appTransactionExDto.DictUnitldIdAndFormulaSetDto = dictUnitldIdAndFormulaSetDto;
        }

        //public static AppTransactionExDto GetOneHierarchyTransactionForDesign(object transcactionId)
        //{
        //	AppTransactionExDto appTransactionExDto = RetrieveOneAppTransactionExDto(transcactionId);
        //	SetupHierarchyUnit(appTransactionExDto);

        //	return appTransactionExDto;
        //}


        public static void PrepareTransactionWebPageDesignBindingExpressions(AppTransactionExDto aAppTransactionExDto)
        {
            foreach (var unitExDto in aAppTransactionExDto.AppTransactionUnitList)
            {
                // Root or Sibling Unit Fields
                PrepareTransactionWebPageDesignBindingExpressions_RootLevelFields(unitExDto);

                foreach (var childUnitExDto in unitExDto.Children)
                {
                    //Chlid Unit
                    PrepareTransactionWebPageDesignBindingExpressions_ChildAndGrandChildUnit(childUnitExDto);


                    foreach (var grandchildUnitExDto in childUnitExDto.Children)
                    {
                        //Chlid Unit
                        PrepareTransactionWebPageDesignBindingExpressions_ChildAndGrandChildUnit(grandchildUnitExDto);


                    }
                }
            }
        }


        internal static void SetupHierarchyUnit(AppTransactionExDto appTransactionExDto)
        {
            ObservableSet<AppTransactionUnitExDto> allTransactionUnits = appTransactionExDto.AppTransactionUnitList;
            appTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap = new Dictionary<Guid, Guid>();

            MapCurentKeyParentKeyGuid(appTransactionExDto, allTransactionUnits);

            appTransactionExDto.SibLineTransactionUnitIdExDtoList = allTransactionUnits.Where(o => o.IsMasterSiblingUnit.HasValue && o.IsMasterSiblingUnit.Value).ToList();

            appTransactionExDto.DictAllTransactionUnitIdExDto = allTransactionUnits.ToDictionary(o => o.Id.ToString(), o => o);
            appTransactionExDto.DictAllTableNameUnitId = allTransactionUnits.Where(o => !string.IsNullOrEmpty(o.DataBaseTableName)).ToDictionary(o => o.DataBaseTableName, o => o.Id.ToString());


            //ParentTransactionUnitId

            AppTransactionUnitExDto rootUnit = allTransactionUnits.Where(o => (o.ParentTransactionUnitId == null) && (!(o.IsMasterSiblingUnit.HasValue && o.IsMasterSiblingUnit.Value))).FirstOrDefault();

            if (rootUnit != null)
            {
                //	appTransactionExDto.RootTransactionUnitExDto = rootUnit;


                ProcessChildren(allTransactionUnits, rootUnit);

                // need to override the origal AppTransactionUnitList
                appTransactionExDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();
                appTransactionExDto.AppTransactionUnitList.Add(rootUnit);

                // Add sibline to the root level

                foreach (AppTransactionUnitExDto aAppTransactionUnitExDto in appTransactionExDto.SibLineTransactionUnitIdExDtoList)
                {
                    appTransactionExDto.AppTransactionUnitList.Add(aAppTransactionUnitExDto);
                }

                foreach (var sibUnitDto in appTransactionExDto.SibLineTransactionUnitIdExDtoList)
                {
                    foreach (var filedDto in sibUnitDto.AppTransactionFieldList)
                    {

                        filedDto.IsSiblingField = true;

                    }

                }
            }

            if (appTransactionExDto.TransactionOrganizedType.HasValue
                && (appTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.FolderList || appTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                && rootUnit != null && !rootUnit.Children.IsEmpty())
            {
                var firstChildUnit = rootUnit.Children.First();

                var firstEditTypelinkTarget = LinkTragetBL.RetrieveOneAppFormLinkTargetList((int)EmAppLinkTargetSourceType.TransactionUnit, (int)firstChildUnit.Id)
                    .FirstOrDefault(o => o.ActionType.HasValue && (o.ActionType.Value == (int)EmAppLinkTargetActionType.Edit || o.ActionType.Value == (int)EmAppLinkTargetActionType.EditOnPopup)
                    && o.LinkTargetTransactionId.HasValue);

                if (firstEditTypelinkTarget != null)
                {
                    appTransactionExDto.DefaultMasterDetailTransactionId = firstEditTypelinkTarget.LinkTargetTransactionId.Value;
                    appTransactionExDto.DefaultMasterDetailTransactionFormId = AppTransactionBL.RetrieveOneAppTransactionSimpleEntity(appTransactionExDto.DefaultMasterDetailTransactionId.Value).FormId;
                }
            }

            SetupMappingToAvailableSourceUnitTransactionFieldExDto(appTransactionExDto);

        }

        //internal static void PrepareApiTransactionProperties(AppTransactionExDto aAppTransactionExDto)
        //{
        //    if (aAppTransactionExDto.OtherOptions != null && aAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction)
        //    {              

        //        if (aAppTransactionExDto.OtherOptions.ReadApi.HasValue())
        //        {
        //            aAppTransactionExDto.BaseApiConfigDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(aAppTransactionExDto.OtherOptions.ReadApi);
        //        }

        //        if (aAppTransactionExDto.OtherOptions.UpdateApi.HasValue())
        //        {
        //            aAppTransactionExDto.ApiConfigUpdate = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(aAppTransactionExDto.OtherOptions.UpdateApi);
        //        }



        //        if (aAppTransactionExDto.BaseApiConfigDto != null && !string.IsNullOrWhiteSpace(aAppTransactionExDto.BaseApiConfigDto.JsonSchema))
        //        {
        //            var rootNodeDto = PrepareApiDataStrucureFromJsonSchema(aAppTransactionExDto.BaseApiConfigDto.JsonSchema);

        //            if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsObject)
        //            {
        //                rootNodeDto = rootNodeDto.Children[0];
        //            }

        //            aAppTransactionExDto.ApiDataStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };
        //        }
        //        else if (aAppTransactionExDto.ApiConfigUpdate != null && !string.IsNullOrWhiteSpace(aAppTransactionExDto.ApiConfigUpdate.JsonSchema))
        //        {
        //            var rootNodeDto = PrepareApiDataStrucureFromJsonSchema(aAppTransactionExDto.ApiConfigUpdate.JsonSchema);

        //            if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsObject)
        //            {
        //                rootNodeDto = rootNodeDto.Children[0];
        //            }

        //            aAppTransactionExDto.ApiDataStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };
        //        }

        //        if (aAppTransactionExDto.ApiConfigUpdate != null && !string.IsNullOrWhiteSpace(aAppTransactionExDto.ApiConfigUpdate.SchemaFromDataSetMapping))
        //        {
        //            var rootNodeDto = PrepareApiDataStrucureFromJsonSchema(aAppTransactionExDto.ApiConfigUpdate.SchemaFromDataSetMapping);

        //            aAppTransactionExDto.ApiPostResponseDataStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };
        //        }
        //    }
        //}

        internal static void PrepareApiTransactionProperties(AppTransactionExDto transactionDto)
        {
            if (transactionDto.OtherOptions != null)
            {
                if (transactionDto.OtherOptions.IsApiIntegrationTransaction && transactionDto.FolderUsageType.HasValue)
                {
                    int apiId = transactionDto.FolderUsageType.Value;



                    //if (!string.IsNullOrWhiteSpace(transactionDto.OtherOptions.ApiLogicKeyParameterName))
                    //{
                    //    transactionDto.ApiInputParameterList = transactionDto.OtherOptions.ApiLogicKeyParameterName.Split('|').ToList();
                    //}




                    transactionDto.BaseApiConfigDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(apiId);

                    transactionDto.ApiInputParameterList = new List<string>();

                    foreach (string targetInputParamName in transactionDto.BaseApiConfigDto.APIConfigParameters.PathParams.Keys)
                    {
                        transactionDto.ApiInputParameterList.Add(AppMasterDetailApiFormDataLoadBL.ApiPathParamPrefix + targetInputParamName);
                    }

                    foreach (string targetInputParamName in transactionDto.BaseApiConfigDto.APIConfigParameters.QueryParams.Keys)
                    {
                        transactionDto.ApiInputParameterList.Add(AppMasterDetailApiFormDataLoadBL.ApiQueryParamPrefix + targetInputParamName);
                    }

                    foreach (string variableKey in transactionDto.BaseApiConfigDto.DictUsedEnvironmentVariable.Keys)
                    {
                        transactionDto.ApiInputParameterList.Add(AppMasterDetailApiFormDataLoadBL.ApiEvironmentVariablePrefix + variableKey);
                    }

                    if (!string.IsNullOrWhiteSpace(transactionDto.BaseApiConfigDto.JsonSchema))
                    {
                        var rootNodeDto = PrepareApiDataStrucureFromJsonSchema(transactionDto.BaseApiConfigDto.JsonSchema);

                        if (transactionDto.TransactionOrganizedType.HasValue)
                        {
                            if (transactionDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                            {
                                if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsObject)
                                {
                                    rootNodeDto = rootNodeDto.Children[0];
                                    rootNodeDto.Display = "{ }";
                                }

                                transactionDto.ApiDataStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };


                                // If Post API Response Data Not Empty, Prepare Response Structue


                            }
                            else if (transactionDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                            {
                                if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsArray)
                                {
                                    rootNodeDto = rootNodeDto.Children[0];
                                    rootNodeDto.Display = "[ ]";
                                }

                                transactionDto.ApiDataStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };

                            }
                        }
                    }

                    if (transactionDto.BaseApiConfigDto.PostResponseDto != null)
                    {
                        if ((transactionDto.BaseApiConfigDto.HttpMethd == "Post"
                                    || transactionDto.BaseApiConfigDto.HttpMethd == "Put")
                                     && !string.IsNullOrWhiteSpace(transactionDto.BaseApiConfigDto.PostResponseDto.ResponseJsonSchema)
                                     )
                        {
                            var responseStructureNodeDto = PrepareApiDataStrucureFromJsonSchema(transactionDto.BaseApiConfigDto.PostResponseDto.ResponseJsonSchema);

                            AppIntergrationSettingBL.InitApiNodeAbolutePath(responseStructureNodeDto, null);
                            transactionDto.ApiPostResponseDataStructure = new List<ApiDataStructureNodeDto>() { responseStructureNodeDto };
                        }
                    }


                    transactionDto.IsAllowSaveAs = false;

                    transactionDto.IsShowSaveButton = false;


                }


                if (transactionDto.OtherOptions.IsEnableSaveByApiCall && transactionDto.OtherOptions.SaveByApiCallDataTransferId.HasValue)
                {
                    AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveAllAppTransactionDataTransferSettingExDto((int)transactionDto.Id, null)
                        .FirstOrDefault(o => (int)o.Id == transactionDto.OtherOptions.SaveByApiCallDataTransferId.Value);

                    if (dataTransferDto != null)
                    {
                        int? apiId = ControlTypeValueConverter.ConvertValueToInt(dataTransferDto.InternalCode);
                        if (apiId.HasValue)
                        {
                            var apiDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(apiId.Value);

                            TransactionApiSettingDto apiSettingDto = InitTransactionApiSettingDto(apiDto);

                            apiSettingDto.ConsumeOrProvideType = "ConsumeApiDataTransfer";
                            apiSettingDto.CRUDType = "Update";

                            apiSettingDto.DataTransferSettingId = (int)dataTransferDto.Id;

                            apiSettingDto.DataTransferSettingName = dataTransferDto.Description;


                            transactionDto.ConsumApiDataModelSaveSettingDto = apiSettingDto;

                            //transactionDto.IsShowSaveButton = true;
                        }
                    }
                }

                if (transactionDto.OtherOptions.IsEnableDeleteByApi && transactionDto.OtherOptions.DeleteDataTransferId.HasValue)
                {
                    AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveAllAppTransactionDataTransferSettingExDto((int)transactionDto.Id, null)
                        .FirstOrDefault(o => (int)o.Id == transactionDto.OtherOptions.DeleteDataTransferId.Value);

                    if (dataTransferDto != null)
                    {

                        int? apiId = ControlTypeValueConverter.ConvertValueToInt(dataTransferDto.InternalCode);
                        if (apiId.HasValue)
                        {
                            var apiDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(apiId.Value);

                            TransactionApiSettingDto apiSettingDto = InitTransactionApiSettingDto(apiDto);

                            apiSettingDto.ConsumeOrProvideType = "ConsumeApiDataTransfer";
                            apiSettingDto.CRUDType = "Delete";

                            apiSettingDto.DataTransferSettingId = (int)dataTransferDto.Id;

                            apiSettingDto.DataTransferSettingName = dataTransferDto.Description;


                            transactionDto.ConsumApiDataModelDeleteSettingDto = apiSettingDto;

                            transactionDto.IsShowDeleteButton = true;
                        }
                    }
                }
            }
        }

        private static void SynchronizeComsumeApiTransactionId(int transactionId, int? apiId, DataAccessAdapter adapter)
        {
            AppIntergrationSettingParameterEntity clearEntity = new AppIntergrationSettingParameterEntity();
            clearEntity.TranscationId = null;
            adapter.UpdateEntitiesDirectly(clearEntity, new RelationPredicateBucket(AppIntergrationSettingParameterFields.IntergrationSettingId != 1 & AppIntergrationSettingParameterFields.TranscationId == transactionId));

            if (apiId.HasValue)
            {
                AppIntergrationSettingParameterEntity updateEntity = new AppIntergrationSettingParameterEntity();
                updateEntity.TranscationId = transactionId;
                adapter.UpdateEntitiesDirectly(updateEntity, new RelationPredicateBucket(AppIntergrationSettingParameterFields.IntergrationSettingId != 1 & AppIntergrationSettingParameterFields.SettingParameterId == apiId.Value));
            }
        }

        internal static ApiDataStructureNodeDto PrepareApiDataStrucureFromJsonSchema(string jsonSchemaString, bool isIncludeProperties = true, bool isIncludeChildNodeOfArray = true)
        {
            JsonSchema schema = JsonSchema.FromJsonAsync(jsonSchemaString).Result;

            var rootNodeDto = new ApiDataStructureNodeDto();
            rootNodeDto.Display = "{ }";
            rootNodeDto.SchemaTypeName = "";
            rootNodeDto.IsObject = true;
            rootNodeDto.Children = new List<ApiDataStructureNodeDto>();
            ProcessOneSchemaNode(schema, rootNodeDto, isIncludeProperties, isIncludeChildNodeOfArray);



            return rootNodeDto;
        }

        internal static void ProcessOneSchemaNode(JsonSchema schema, ApiDataStructureNodeDto parentNodeDto, bool isIncludeProperties = true, bool isIncludeChildNodeOfArray = true)
        {
            List<ApiDataStructureNodeDto> needToAddNodeList = new List<ApiDataStructureNodeDto>();

            if (schema.IsArray)
            {
                ApiDataStructureNodeDto nodeDto = new ApiDataStructureNodeDto();
                needToAddNodeList.Add(nodeDto);
                nodeDto.Children = new List<ApiDataStructureNodeDto>();
                nodeDto.Name = "";
                nodeDto.IsArray = true;
                nodeDto.SchemaTypeName = parentNodeDto.SchemaTypeName;
                if (schema.Item != null && schema.Item.Reference != null && isIncludeChildNodeOfArray)
                {
                    var chlidSchema = schema.Item.Reference;

                    ProcessOneSchemaNode(chlidSchema, nodeDto, isIncludeProperties, isIncludeChildNodeOfArray);
                }
            }
            else
            {
                foreach (string key in schema.Properties.Keys)
                {
                    var schemaProperty = schema.Properties[key];

                    ApiDataStructureNodeDto nodeDto = new ApiDataStructureNodeDto();
                    needToAddNodeList.Add(nodeDto);
                    nodeDto.Children = new List<ApiDataStructureNodeDto>();
                    nodeDto.Name = schemaProperty.Name;


                    if (schemaProperty.IsArray)
                    {
                        nodeDto.IsArray = true;

                        if (!string.IsNullOrWhiteSpace(parentNodeDto.SchemaTypeName))
                        {
                            nodeDto.SchemaTypeName = parentNodeDto.SchemaTypeName + JsonSchemaMappingSplit + schemaProperty.Name;
                        }
                        else
                        {
                            nodeDto.SchemaTypeName = schemaProperty.Name;
                        }

                        if (schemaProperty.Item != null && schemaProperty.Item.Reference != null && isIncludeChildNodeOfArray)
                        {
                            var chlidSchema = schemaProperty.Item.Reference;

                            if (chlidSchema.Type == JsonObjectType.String)
                            {
                                nodeDto.IsSimpleList = true;
                            }

                            ProcessOneSchemaNode(chlidSchema, nodeDto, isIncludeProperties, isIncludeChildNodeOfArray);
                        }



                    }
                    else if (schemaProperty.Reference != null)
                    {

                        var chlidSchema = schemaProperty.Reference;
                        nodeDto.IsObject = chlidSchema.IsObject;

                        if (!string.IsNullOrWhiteSpace(parentNodeDto.SchemaTypeName))
                        {
                            nodeDto.SchemaTypeName = parentNodeDto.SchemaTypeName + JsonSchemaMappingSplit + schemaProperty.Name;
                        }
                        else
                        {
                            nodeDto.SchemaTypeName = schemaProperty.Name;
                        }

                        ProcessOneSchemaNode(chlidSchema, nodeDto, isIncludeProperties, isIncludeChildNodeOfArray);
                    }

                    if (nodeDto.IsArray)
                    {
                        nodeDto.Display = " " + schemaProperty.Name + " [ ]";
                    }
                    else if (nodeDto.IsObject)
                    {
                        nodeDto.Display = " " + schemaProperty.Name + " { }";
                    }
                    else
                    {
                        nodeDto.Display = " " + schemaProperty.Name + " ";
                    }
                }


            }

            if (needToAddNodeList.Where(o => !o.IsObject && !o.IsArray).Count() > 0)
            {
                parentNodeDto.HasSimpleProperties = true;

                if (isIncludeProperties)
                {
                    foreach (var nodeDto in needToAddNodeList.Where(o => !o.IsObject && !o.IsArray))
                    {
                        parentNodeDto.Children.Add(nodeDto);
                    }
                }
            }





            foreach (var nodeDto in needToAddNodeList.Where(o => o.IsObject))
            {
                parentNodeDto.Children.Add(nodeDto);
            }

            foreach (var nodeDto in needToAddNodeList.Where(o => o.IsArray))
            {
                parentNodeDto.Children.Add(nodeDto);
            }



        }

        public static AppTransactionFieldExDto RetrieveOneAppTransactionFieldExDto(object Id, int? layoutItemId = null)
        {
            AppTransactionFieldEntity aEntity = new AppTransactionFieldEntity(int.Parse(Id.ToString()));

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntity(aEntity);


            }



            AppTransactionFieldExDto aAppTransactionFieldExDto = AppTransactionFieldConverter.ConvertEntityToExDto(aEntity);

            if (aAppTransactionFieldExDto.DataRetrieveType.HasValue
                 && (aAppTransactionFieldExDto.DataRetrieveType.Value == (int)EmAppCascadingSourceType.ExternalOneToManyMapping
                 || aAppTransactionFieldExDto.DataRetrieveType.Value == (int)EmAppCascadingSourceType.ExternalOneToOneMapping

                 )

            )
            {


                aAppTransactionFieldExDto.ConvertDataRetrieveMappingStringToDict();


            }


            if (layoutItemId.HasValue)
            {
                AppFormLayoutItemExDto layoutItemExDto = AppFormFlexLayoutBL.RetrieveOneAppFormLayoutItemExDto(layoutItemId.Value);
                aAppTransactionFieldExDto.DomAttribute = layoutItemExDto.DomAttribute;
                aAppTransactionFieldExDto.StyleLayoutInfo = layoutItemExDto.StyleLayoutInfo;

                aAppTransactionFieldExDto.LayoutItemId = layoutItemId;
            }

            return aAppTransactionFieldExDto;
        }

        public static OperationCallResult<bool> SetTransactionForPublicAccess(int transactionId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionEntity entity = new AppTransactionEntity();
            entity.IsForPublicAcesss = true;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.UpdateEntitiesDirectly(entity, new RelationPredicateBucket(AppTransactionFields.TransactionId == transactionId));


                    adapter.Commit();

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_UpdateTransaction_Ok", ValidationItemType.Message, "Update Transaction Success."));

                    aOperationCallResult.Object = true;


                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aOperationCallResult;
        }

        /// <summary>Factory: creates a blank AppTransactionFieldExDto ready to be populated and passed to the overload below.</summary>
        public static AppTransactionFieldExDto CreateNewAppTransactionFieldExDto() => new AppTransactionFieldExDto();

        public static OperationCallResult<AppTransactionFieldExDto> CreateNewAppTransactionFieldExDto(AppTransactionFieldExDto transFieldExDto)
        {
            var aOperationCallResult = new OperationCallResult<AppTransactionFieldExDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            try
            {
                var entity = new AppTransactionFieldEntity();
                AppTransactionFieldConverter.CopyDtoToEntity(entity, transFieldExDto);

                using (var adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "CreateTransactionField");
                        adapter.SaveEntity(entity, true); // refetchAfterSave = true → populates generated PK
                        adapter.Commit();
                    }
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionFieldEntity),
                            "CreateField_Error", ValidationItemType.Error, ex.ToString()));
                        return aOperationCallResult;
                    }
                }

                transFieldExDto.Id = entity.TransactionFieldId;
                aOperationCallResult.Object = transFieldExDto;

                if (transFieldExDto.TransactionId.HasValue)
                    AppCacheManagerBL.RefreshOnetHierarchyTranscation(transFieldExDto.TransactionId);
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionFieldEntity),
                    "CreateField_Error", ValidationItemType.Error, ex.Message));
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppTransactionExDto> SaveAppTransactionFieldExDto(AppTransactionFieldExDto transFieldExDto)
        {
            OperationCallResult<AppTransactionExDto> aOperationCallResult = new OperationCallResult<AppTransactionExDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (transFieldExDto != null && transFieldExDto.Id != null)
            {
                if (transFieldExDto.ControlType != (int)EmAppControlType.DDL
                    && transFieldExDto.ControlType != (int)EmAppControlType.AutoComplete
                    && transFieldExDto.ControlType != (int)EmAppControlType.SearchAbleDDL
                    && transFieldExDto.ControlType != (int)EmAppControlType.RadioButtons
                    && transFieldExDto.ControlType != (int)EmAppControlType.Progress
                    )
                {
                    transFieldExDto.EntityId = null;
                }

                AppTransactionFieldEntity entity = new AppTransactionFieldEntity();
                AppTransactionFieldConverter.CopyDtoToEntity(entity, transFieldExDto);

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.UpdateEntitiesDirectly(entity, new RelationPredicateBucket(AppTransactionFieldFields.TransactionFieldId == (int)transFieldExDto.Id)); ;


                        adapter.Commit();


                        if (transFieldExDto.LayoutItemId.HasValue)
                        {
                            if (transFieldExDto.DomAttribute != null)
                            {
                                var layoutItemExDto = AppFormFlexLayoutBL.RetrieveOneAppFormLayoutItemExDto(transFieldExDto.LayoutItemId.Value);

                                layoutItemExDto.DomAttribute = transFieldExDto.DomAttribute;

                                var updateLayoutResult = AppFormFlexLayoutBL.UpdateOneAppFormLayoutItemExDto(layoutItemExDto);

                                if (updateLayoutResult.ValidationResult.HasErrors)
                                {
                                    aValidationResult.Merge(updateLayoutResult.ValidationResult);
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(transFieldExDto.StyleLayoutInfo))
                            {
                                var layoutItemExDto = AppFormFlexLayoutBL.RetrieveOneAppFormLayoutItemExDto(transFieldExDto.LayoutItemId.Value);

                                layoutItemExDto.StyleLayoutInfo = transFieldExDto.StyleLayoutInfo;

                                var updateLayoutResult = AppFormFlexLayoutBL.UpdateOneAppFormLayoutItemExDto(layoutItemExDto);

                                if (updateLayoutResult.ValidationResult.HasErrors)
                                {
                                    aValidationResult.Merge(updateLayoutResult.ValidationResult);
                                }

                            }
                        }

                        UpdateTransactionFieldStrucutureAndDataFromChangeSetting(transFieldExDto, aValidationResult);

                        AppCacheManagerBL.RefreshOnetHierarchyTranscation(transFieldExDto.TransactionId);

                        AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transFieldExDto.TransactionId);








                        if (transactionExDto.OtherOptions != null && transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId.HasValue)
                        {
                            int importSettingId = transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId.Value;
                            AppDatabaseTableImportBL.UpdateTransactionDataFromImport_ReloadTransaction(importSettingId);
                        }



                    }
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }


                }



            }

            return aOperationCallResult;
        }

        private static void UpdateTransactionFieldStrucutureAndDataFromChangeSetting(AppTransactionFieldExDto transFieldExDto, ValidationResult aValidationResult)
        {
            if (transFieldExDto.FieldChangeSetting != null)
            {
                string query = "";

                var changeDto = transFieldExDto.FieldChangeSetting;

                var unitEntity = AppTransactionBL.RetrieveOneAppTransactionUnitEntity(transFieldExDto.TransactionUnitId);
                string targetTableName = unitEntity.DataBaseTableName;
                string targetColumnName = transFieldExDto.DataBaseFieldName;
                string tempColumnName = targetColumnName + "__Org";
                string mapToEntityColumn = changeDto.MappingToEntityColumnName;
                AppEntityInfoExDto entityInfoDto = null;



                if (!string.IsNullOrWhiteSpace(unitEntity.DataBaseTableName))
                {
                    if (changeDto.IsChagneFromDDLToOtherType)
                    {
                        if (changeDto.OrgEntityId.HasValue && changeDto.MappingToEntityColumnName.HasValue())
                        {
                            entityInfoDto = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(changeDto.OrgEntityId.Value);
                            string entityTableName = entityInfoDto.TableName;
                            string entityIdColumn = entityInfoDto.IdentityField;

                            DatabaseTable entityTable = AppMetaDataBL.GetOneDatabaseTableSchema(entityInfoDto.TableName, entityInfoDto.DataSourceFrom, entityInfoDto.SchemaOwner);

                            var entityIdColumnDto = entityTable.Columns.FirstOrDefault(o => o.Name == entityIdColumn);
                            var mapToEntityColumnDto = entityTable.Columns.FirstOrDefault(o => o.Name == mapToEntityColumn);

                            string targetColumn_newType = mapToEntityColumnDto.DbDataType;
                            if (mapToEntityColumnDto.Length.HasValue && mapToEntityColumnDto.Length.Value > 0)
                            {
                                targetColumn_newType += "(" + mapToEntityColumnDto.Length.Value + ")";
                            }

                            //ALTER TABLE a0001_Product ADD Category__Org nvarchar(max) null
                            //UPDATE a0001_Product set Category__Org = Category
                            //UPDATE a0001_Product set Category = null

                            //ALTER TABLE a0001_Product ALTER COLUMN Category nvarchar(max) null

                            //UPDATE a0001_Product SET Category = entityTable.Category
                            //FROM a0001_Product inner join a0001_Category as entityTable on(a0001_Product.Category__Org = entityTable.a0001_CategoryId)

                            //ALTER TABLE a0001_Product DROP COLUMN Category__Org

                            //ALTER TABLE [a0088_Product] ALTER COLUMN [Category] nvarchar(max) null;

                            query += string.Format(@"
                                                ALTER TABLE [{0}] ADD [{1}] nvarchar(max) null;",
                                    targetTableName, tempColumnName);

                            query += string.Format(@"
                                                UPDATE [{0}] set [{1}] = [{2}];",
                                    targetTableName, tempColumnName, targetColumnName);

                            query += string.Format(@"
                                                UPDATE [{0}] set [{1}] = NULL;",
                                    targetTableName, targetColumnName);

                            query += string.Format(@"
                                                ALTER TABLE [{0}] ALTER COLUMN [{1}] nvarchar(max) null;",
                                    targetTableName, targetColumnName);

                            query += string.Format(@"
                                                UPDATE [{0}] SET [{1}] = entityTable.[{2}] 
                                                FROM [{3}] as TargetTable inner join [{4}] as entityTable on(TargetTable.[{5}] = entityTable.[{6}]);",
                                    targetTableName, targetColumnName, mapToEntityColumn,
                                    targetTableName, entityTableName, tempColumnName, entityIdColumn
                                    );

                            query += string.Format(@"
                                                ALTER TABLE [{0}] DROP COLUMN [{1}];",
                                    targetTableName, tempColumnName);


                            query += string.Format(@"
                                                ALTER TABLE [{0}] ALTER COLUMN [{1}] {2} null;",
                                   targetTableName, targetColumnName, targetColumn_newType);





                        }
                    }
                    else if (changeDto.IsChangeFromOtherTypeToDDL)
                    {
                        if (changeDto.NewEntityId.HasValue && changeDto.MappingToEntityColumnName.HasValue())
                        {
                            entityInfoDto = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(changeDto.NewEntityId.Value);
                            string entityTableName = entityInfoDto.TableName;
                            string entityIdColumn = entityInfoDto.IdentityField;

                            DatabaseTable entityTable = AppMetaDataBL.GetOneDatabaseTableSchema(entityInfoDto.TableName, entityInfoDto.DataSourceFrom, entityInfoDto.SchemaOwner);

                            var entityIdColumnDto = entityTable.Columns.FirstOrDefault(o => o.Name == entityIdColumn);
                            var mapToEntityColumnDto = entityTable.Columns.FirstOrDefault(o => o.Name == mapToEntityColumn);

                            string targetColumn_newType = entityIdColumnDto.DbDataType;
                            if (entityIdColumnDto.Length.HasValue && entityIdColumnDto.Length.Value > 0)
                            {
                                targetColumn_newType += "(" + entityIdColumnDto.Length.Value + ")";
                            }

                            //ALTER TABLE a0001_Product ADD Category__Org nvarchar(max) null
                            //UPDATE a0001_Product set Category__Org = Category
                            //UPDATE a0001_Product set Category = null

                            //ALTER TABLE a0001_Product ALTER COLUMN Category nvarchar(max) null

                            //UPDATE a0001_Product SET Category = entityTable.a0001_CategoryId
                            //FROM a0001_Product inner join a0001_Category as entityTable on(a0001_Product.Category__Org = entityTable.Category)

                            //ALTER TABLE a0001_Product DROP COLUMN Category__Org

                            //ALTER TABLE [a0088_Product] ALTER COLUMN [Category] int null;

                            query += string.Format(@"
                                                ALTER TABLE [{0}] ADD [{1}] nvarchar(max) null;",
                                   targetTableName, tempColumnName);

                            query += string.Format(@"
                                                UPDATE [{0}] set [{1}] = [{2}];",
                                    targetTableName, tempColumnName, targetColumnName);

                            query += string.Format(@"
                                                UPDATE [{0}] set [{1}] = NULL;",
                                    targetTableName, targetColumnName);

                            query += string.Format(@"
                                                ALTER TABLE [{0}] ALTER COLUMN [{1}] nvarchar(max) null;",
                                    targetTableName, targetColumnName);

                            query += string.Format(@"
                                                UPDATE [{0}] SET [{1}] = entityTable.[{2}] 
                                                FROM [{3}] as TargetTable inner join [{4}] as entityTable on(TargetTable.[{5}] = entityTable.[{6}]);",
                                    targetTableName, targetColumnName, entityIdColumn,
                                    targetTableName, entityTableName, tempColumnName, mapToEntityColumn
                                    );

                            query += string.Format(@"
                                                ALTER TABLE [{0}] DROP COLUMN [{1}];",
                                    targetTableName, tempColumnName);

                            query += string.Format(@"
                                                ALTER TABLE [{0}] ALTER COLUMN [{1}] {2} null;",
                                   targetTableName, targetColumnName, targetColumn_newType);
                        }
                    }
                }


                if (!string.IsNullOrWhiteSpace(query))
                {
                    DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(entityInfoDto.DataSourceFrom, null);

                    string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, query);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error, "Import Failed. " + errorMsg));
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_UpdateImportedTableDataFromTempTable_Ok", ValidationItemType.Message, "Update Transaction Field Success."));

                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Query", ValidationItemType.Message, "\nQuery: \n" + query + "\n"));
                    }

                }

            }
        }

        internal static AppTransactionUnitEntity RetrieveOneAppTransactionUnitEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionUnitEntity aUnitEntity = new AppTransactionUnitEntity(int.Parse(Id.ToString()));
                adapter.FetchEntity(aUnitEntity);
                return aUnitEntity;
            }
        }

        public static AppTransactionUnitExDto RetrieveOneAppTransactionUnitExDto(object Id)
        {

            AppTransactionUnitEntity aUnitEntity = null;
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                aUnitEntity = new AppTransactionUnitEntity(int.Parse(Id.ToString()));

                adapter.FetchEntity(aUnitEntity);


            }

            // need to get Child Unit as well

            if (aUnitEntity != null && aUnitEntity.TransactionId.HasValue)
            {

                AppTransactionExDto AppTransactionExDto = RetrieveOneAppTransactionExDto(aUnitEntity.TransactionId);


                AppTransactionUnitExDto rootUnitDto = AppTransactionExDto.AppTransactionUnitList.FirstOrDefault();

                if (rootUnitDto != null)
                {
                    rootUnitDto.Level = 1;
                    foreach (var childDto in rootUnitDto.Children)
                    {
                        childDto.Level = 2;
                        foreach (var grandChildDto in childDto.Children)
                        {
                            grandChildDto.Level = 3;
                        }
                    }
                }

                var allTransactionUnitList = AppTransactionExDto.AppTransactionUnitList;

                List<string> isUsedRetrieveDataMappingDataSourceFiedIds = new List<string>();

                foreach (var transactionUnit in allTransactionUnitList)
                {
                    isUsedRetrieveDataMappingDataSourceFiedIds.AddRange(transactionUnit.AppTransactionFieldList
                    .Where(o => o.ControlType == (int)EmAppControlType.RetrieveData && o.MasterEntityFieldlId.HasValue)
                    .Select(o => o.MasterEntityFieldlId.ToString())
                    .ToList()
                    );
                }

                foreach (var transactionUnit in allTransactionUnitList)
                {
                    transactionUnit.AppTransactionFieldList
                       .Where(o => isUsedRetrieveDataMappingDataSourceFiedIds.Contains(o.Id.ToString()))
                       .ForAll(o => o.IsUsedRetrieveDataMappingDataSource = true);
                }


                var currentUnit = allTransactionUnitList.Where(o => ((int)o.Id == aUnitEntity.TransactionUnitId)).FirstOrDefault();
                if (currentUnit != null)
                {
                    ProcessChildren(allTransactionUnitList, currentUnit);
                    return currentUnit;
                }



            }

            return null;
        }



        public static OperationCallResult<AppTransactionExDto> CreateDefaultMasterDetailTransaction(int listEditTransactionId, bool isNeedCreateSearch)
        {
            OperationCallResult<AppTransactionExDto> aOperationCallResult = new OperationCallResult<AppTransactionExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            AppTransactionExDto listEditTransactionDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(listEditTransactionId);
            AppTransactionUnitExDto listEditRootUnit = null;

            if (listEditTransactionDto == null || !listEditTransactionDto.TransactionOrganizedType.HasValue)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_InvalidTransaction", ValidationItemType.Error, "Invalid Transaction."));
                return aOperationCallResult;
            }

            if (listEditTransactionDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
            {
                listEditRootUnit = listEditTransactionDto.AppTransactionUnitList.FirstOrDefault();
            }

            if (listEditRootUnit == null || listEditRootUnit.Id == null)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_InvalidTransaction", ValidationItemType.Error, "Invalid Transaction: Root unit is not available."));
                return aOperationCallResult;
            }

            if (listEditRootUnit.AppTransactionFieldList.FirstOrDefault(o => o.IsPrimaryKey) == null)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_MissingRootUnitPrimarykey", ValidationItemType.Error, "Cannot create default master detail editing form. Root unit does not have primary key."));
                return aOperationCallResult;
            }

            if (listEditTransactionDto.DefaultMasterDetailTransactionId.HasValue)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_LinkedTargetFormAlreadyExist", ValidationItemType.Error, "Link target form already exist. Please check unit linked target setting."));
                return aOperationCallResult;
            }


            AppTransactionExDto newMasterDetailTransaction = new AppTransactionExDto();
            newMasterDetailTransaction.DataSourceFrom = listEditTransactionDto.DataSourceFrom;
            newMasterDetailTransaction.TransactionName = listEditTransactionDto.TransactionName + " Edit";
            newMasterDetailTransaction.TransactionOrganizedType = (int)EmTransactionOrganizedType.MasterDetail;
            newMasterDetailTransaction.DictCurrentPKOrFKLinkToParentKeyGuidMap = new Dictionary<Guid, Guid>();


            AppTransactionUnitExDto newUnit = new AppTransactionUnitExDto();
            newUnit.SchemaOwner = listEditRootUnit.SchemaOwner;
            newUnit.UnitDisplayName = listEditRootUnit.UnitDisplayName;
            newUnit.DataBaseTableName = listEditRootUnit.DataBaseTableName;
            newUnit.IsPrimaryKeyIdentityInsert = listEditRootUnit.IsPrimaryKeyIdentityInsert;

            foreach (var orgTransField in listEditRootUnit.AppTransactionFieldList)
            {
                var newTransField = orgTransField.DeepCopy();
                newTransField.Id = null;

                if (orgTransField.IsPrimaryKey)
                {
                    newTransField.IsReadonly = true;
                    newTransField.IsVisible = false;
                }

                if (orgTransField.IsLinkToParentPrimaryKey)
                {
                    newTransField.IsLinkToParentPrimaryKey = false;

                    if (listEditTransactionDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                    {
                        newTransField.IsReadonly = true;
                        newTransField.IsVisible = false;
                    }
                }
                newUnit.AppTransactionFieldList.Add(newTransField);
            }

            newMasterDetailTransaction.AppTransactionUnitList.Add(newUnit);

            OperationCallResult<AppTransactionExDto> saveNewTransactionResult = SaveAppTransactionExDto(newMasterDetailTransaction);

            if (saveNewTransactionResult.IsSuccessfulWithResult)
            {
                var masterDetailTransaction = saveNewTransactionResult.Object;
                AppFormExDto masterDetailForm = AppFormBL.CreateTransactionFormWithDefaultLayout((int)masterDetailTransaction.Id, 1);
                masterDetailTransaction.FormId = ControlTypeValueConverter.ConvertValueToInt(masterDetailForm.Id);

                var masterDetailRootUnit = masterDetailTransaction.AppTransactionUnitList.FirstOrDefault();

                if (masterDetailRootUnit != null)
                {
                    var primaryKeyColumn = masterDetailRootUnit.AppTransactionFieldList.FirstOrDefault(o => o.IsPrimaryKey);


                    if (primaryKeyColumn != null)
                    {
                        List<EmAppLinkTargetActionType> needToCreateLinkTargetActions = new List<EmAppLinkTargetActionType>(){
                                    EmAppLinkTargetActionType.Create,
                                    EmAppLinkTargetActionType.Edit,
                                    EmAppLinkTargetActionType.Delete,
                                    //EmAppLinkTargetActionType.Preview
                                };



                        ObservableSet<AppFormLinkTargetDto> aAppFormLinkTargetDtoSet = new ObservableSet<AppFormLinkTargetDto>();

                        foreach (EmAppLinkTargetActionType action in needToCreateLinkTargetActions)
                        {
                            AppFormLinkTargetDto aAppFormLinkTargetDto = new AppFormLinkTargetDto();
                            aAppFormLinkTargetDto.ActionType = (int)action;
                            aAppFormLinkTargetDto.NavigationActionName = action.ToString();


                            if (aAppFormLinkTargetDto.NavigationActionName == EmAppLinkTargetActionType.Edit.ToString())
                            {
                                aAppFormLinkTargetDto.NavigationActionName = "Open";
                            }

                            aAppFormLinkTargetDto.LinkTargetUsageType = (int)EmAppLinkTargetUsageType.SearchViewLinkToForm;
                            aAppFormLinkTargetDto.TransactionUnitId = (int)listEditRootUnit.Id;
                            aAppFormLinkTargetDto.LinkTargetTransactionId = (int)masterDetailTransaction.Id;
                            aAppFormLinkTargetDto.SourceColumnType = (int)EmAppLinkTargetSourceColumnType.SearchViewField;
                            aAppFormLinkTargetDto.SourceColumn1 = primaryKeyColumn.DataBaseFieldName;
                            aAppFormLinkTargetDto.TargetColumn1 = "Primary Key";
                            aAppFormLinkTargetDtoSet.Add(aAppFormLinkTargetDto);
                        }

                        OperationCallResult<AppFormLinkTargetDto> linkTargetResult = LinkTragetBL.SaveOneAppFormLinkTargetList((int)EmAppLinkTargetSourceType.TransactionUnit, (int)listEditRootUnit.Id, aAppFormLinkTargetDtoSet);


                        if (linkTargetResult.IsSuccessfulWithResult)
                        {
                            if (isNeedCreateSearch)
                            {
                                OperationCallResult<DatabaseViewUpdateDto> createSearchResult = AppDatabaseViewBL.CreateSimpleSearchViewFromMasterDetailTransactoin((int)masterDetailTransaction.Id, newMasterDetailTransaction.FolderTransactionId);

                                if (createSearchResult.IsSuccessfulWithResult)
                                {
                                    OperationCallResult<object> addSearchToMenuResult = AppTreeListMenuBL.AddSearchToMainMenu(createSearchResult.Object.SearchId, listEditTransactionDto.TransactionName + " Search", false);

                                    if (addSearchToMenuResult.IsSuccessfulWithResult)
                                    {
                                        aOperationCallResult.Object = masterDetailTransaction;
                                        validationResult.Items.Add(new ValidationItem(typeof(AppSearchSavedEntity), "App_TransactionEntity_LinkTargetFormCreated_OK", ValidationItemType.Message, "Link Target Form Created Successfully"));
                                    }
                                    else
                                    {
                                        validationResult.Merge(addSearchToMenuResult.ValidationResult);
                                    }
                                }
                                else
                                {
                                    validationResult.Merge(createSearchResult.ValidationResult);
                                }
                            }
                            else
                            {
                                aOperationCallResult.Object = masterDetailTransaction;
                                validationResult.Items.Add(new ValidationItem(typeof(AppSearchSavedEntity), "App_TransactionEntity_LinkTargetFormCreated_OK", ValidationItemType.Message, "Link Target Form Created Successfully"));
                            }
                        }
                        else
                        {
                            validationResult.Merge(linkTargetResult.ValidationResult);
                        }
                    }
                }
            }
            else
            {
                validationResult.Merge(saveNewTransactionResult.ValidationResult);
            }

            AppCacheManagerBL.RefreshOnetHierarchyTranscation(listEditTransactionId);
            return aOperationCallResult;
        }





        private static void ProcessChildren(IEnumerable<AppTransactionUnitExDto> allTransactionUnitExDto, AppTransactionUnitExDto appTransactionUnitExDto)
        {
            List<AppTransactionUnitExDto> children = GetChildrenWithoutSibling(allTransactionUnitExDto, appTransactionUnitExDto);

            if (!children.IsEmpty())
            {
                appTransactionUnitExDto.Children = children;

                appTransactionUnitExDto.Children.ForAll(c => ProcessChildren(allTransactionUnitExDto, c));

            }
        }

        private static List<AppTransactionUnitExDto> GetChildrenWithoutSibling(IEnumerable<AppTransactionUnitExDto> allfoldersTreeItems, AppTransactionUnitExDto folderTreeItemDto)
        {
            return allfoldersTreeItems.Where(f => f.ParentTransactionUnitId == (int?)folderTreeItemDto.Id
            &&
            (!(f.IsMasterSiblingUnit.HasValue && f.IsMasterSiblingUnit.Value)))
            .ToList();
        }


        // get all unit 
        public static AppTransactionEntity RetrieveOneAppTransactionEntity(object transcactionId)
        {

            int? transcactionIdInt = ControlTypeValueConverter.ConvertValueToInt(transcactionId);
            if (!transcactionIdInt.HasValue)
                return null;
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionEntity transactionEntity = new AppTransactionEntity(transcactionIdInt.Value );


                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppTransactionEntity);
                IPrefetchPathElement2 unitFetchElment = rootPath.Add(AppTransactionEntity.PrefetchPathAppTransactionUnit);
                rootPath.Add(AppTransactionEntity.PrefetchPathAppTransactionDataLoad);
                rootPath.Add(AppTransactionEntity.PrefetchPathAppProjectWorkFlowAction);

                unitFetchElment.SubPath.Add(AppTransactionUnitEntity.PrefetchPathAppTransactionUnitFormula_);

                var transcationFiedPath = unitFetchElment.SubPath.Add(AppTransactionUnitEntity.PrefetchPathAppTransactionField);



                transcationFiedPath.SubPath.Add(AppTransactionFieldEntity.PrefetchPathAppTransactionFieldAggFunction_);
                transcationFiedPath.SubPath.Add(AppTransactionFieldEntity.PrefetchPathAppConditionalAction__);


                unitFetchElment.SubPath.Add(AppTransactionUnitEntity.PrefetchPathAppFormLinkTarget);

                IPrefetchPathElement2 linkedSearchElement = unitFetchElment.SubPath.Add(AppTransactionUnitEntity.PrefetchPathAppTransactionUnitLinkedSearch);

                linkedSearchElement.SubPath.Add(AppTransactionUnitLinkedSearchEntity.PrefetchPathAppTransactionUnitSearchFieldMapping)
                    .SubPath.Add(AppTransactionUnitSearchFieldMappingEntity.PrefetchPathAppTransactionField);

                linkedSearchElement.SubPath.Add(AppTransactionUnitLinkedSearchEntity.PrefetchPathAppTransactionUnitSearchViewFieldMapping)
                    .SubPath.Add(AppTransactionUnitSearchViewFieldMappingEntity.PrefetchPathAppTransactionField);

                adpater.FetchEntity(transactionEntity, rootPath);

                //EntityCollection<AppProjectWorkFlowActionEntity> commandActionList = new EntityCollection<AppProjectWorkFlowActionEntity>();
                //adpater.FetchEntityCollection(commandActionList, new RelationPredicateBucket(AppProjectWorkFlowActionFields.CommandTransactionId == transcactionId));

                return transactionEntity;
            }
        }

        //public static List<AppProjectWorkFlowActionExDto> RetrieveOneTransactionCommandActionList(object transcactionId)
        //{
        //    List<AppProjectWorkFlowActionExDto> toReturn = new List<AppProjectWorkFlowActionExDto>();
        //    using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
        //    {
        //        EntityCollection<AppProjectWorkFlowActionEntity> commandActionList = new EntityCollection<AppProjectWorkFlowActionEntity>();
        //        adpater.FetchEntityCollection(commandActionList, new RelationPredicateBucket(AppProjectWorkFlowActionFields.CommandTransactionId == transcactionId));

        //        foreach (var entity in commandActionList)
        //        {
        //            toReturn.Add(AppProjectWorkFlowActionConverter.ConvertEntityToExDto(entity));
        //        }
        //    }

        //    return toReturn;
        //}

        public static AppTransactionEntity RetrieveOneAppTransactionSimpleEntity(object transcactionId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionEntity transactionEntity = new AppTransactionEntity(int.Parse(transcactionId.ToString()));
                adpater.FetchEntity(transactionEntity);
                return transactionEntity;
            }
        }


        public static AppTransactionEntity RetrieveOneAppTransactionEntityByFormId(object formId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionEntity> entityList = new EntityCollection<AppTransactionEntity>();
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    adapter.FetchEntityCollection(entityList, new RelationPredicateBucket(AppTransactionFields.FormId == formId));

                }

                if (entityList.Count() > 0)
                {
                    return entityList.First();
                }
                else
                {
                    return null;
                }
            }
        }


        public static OperationCallResult<AppTransactionExDto> SaveAppTransactionExDto(
            AppTransactionExDto aHierarchyAppTransactionExDto,
            bool isIgnoreValidation = false,
            bool skipPostSaveCacheSync = false)
        {
            OperationCallResult<AppTransactionExDto> aOperationCallResult = new OperationCallResult<AppTransactionExDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (!isIgnoreValidation)
            {
                aValidationResult.Merge(aHierarchyAppTransactionExDto.ValidateDto());

                if (aValidationResult.HasErrors)
                {
                    aOperationCallResult.ValidationResult = aValidationResult;
                    return aOperationCallResult;
                }
            }

            AppTransactionEntity aAppTransactionEntity;

            //aHierarchyAppTransactionExDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();

            Dictionary<string, string> dictChildParentTableNameMapping = new Dictionary<string, string>();


            ObservableSet<AppTransactionUnitExDto> flatTranscatuonUnitList = new ObservableSet<AppTransactionUnitExDto>();


            //all  childUnitid  deleteid will be stored to root aHierarchyAppTransactionExDto.AppTransactionUnitList;
            flatTranscatuonUnitList.DeletedItemIds.AddRange(aHierarchyAppTransactionExDto.AppTransactionUnitList.DeletedItemIds);


            var rootUnitDto = aHierarchyAppTransactionExDto.AppTransactionUnitList.FirstOrDefault();
            if (rootUnitDto != null)
            {
                flatTranscatuonUnitList.Add(rootUnitDto);

                foreach (var childDto in rootUnitDto.Children)
                {
                    if (!dictChildParentTableNameMapping.ContainsKey(childDto.UnitDisplayName))
                        dictChildParentTableNameMapping.Add(childDto.UnitDisplayName, rootUnitDto.UnitDisplayName);

                    flatTranscatuonUnitList.Add(childDto);

                    foreach (var grandChildDto in childDto.Children)
                    {
                        if (!dictChildParentTableNameMapping.ContainsKey(grandChildDto.UnitDisplayName))
                            dictChildParentTableNameMapping.Add(grandChildDto.UnitDisplayName, childDto.UnitDisplayName);
                        flatTranscatuonUnitList.Add(grandChildDto);

                    }

                }



            }


            // needt to add sibling , skip first root elment Test !!!, no need to set ParentTransactionUnitId
            for (int i = 1; i <= aHierarchyAppTransactionExDto.AppTransactionUnitList.Count - 1; i++)
            {

                flatTranscatuonUnitList.Add(aHierarchyAppTransactionExDto.AppTransactionUnitList.ElementAt(i));

            }

            // need to overide aHierarchyAppTransactionExDto
            aHierarchyAppTransactionExDto.AppTransactionUnitList = flatTranscatuonUnitList;





            // prepare Data
            if (aHierarchyAppTransactionExDto.IsNew)
            {
                aAppTransactionEntity = PorcessNewTeanscation(aHierarchyAppTransactionExDto, aValidationResult, dictChildParentTableNameMapping);

                //if (aHierarchyAppTransactionExDto.SaasApplicationId.HasValue
                //    && (!aHierarchyAppTransactionExDto.IsPhysicalModelTableCreated.HasValue || aHierarchyAppTransactionExDto.IsPhysicalModelTableCreated.Value))
                //{
                //    AppApplicationAssetsItemExDto transactionAssetsItemDto = new AppApplicationAssetsItemExDto();
                //    transactionAssetsItemDto.ApplicationId = aHierarchyAppTransactionExDto.SaasApplicationId;
                //    transactionAssetsItemDto.TransactionId = aAppTransactionEntity.TransactionId;
                //    AppSaasUserApplicationPackageBL.SaveNewApplicationAssetsItemExDto(transactionAssetsItemDto);
                //}
            }

            else if (aHierarchyAppTransactionExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppTransactionExDto(aHierarchyAppTransactionExDto, dictChildParentTableNameMapping));
            }






            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {

                // need 
                AppTransactionEntity appTransactionEntity = RetrieveOneAppTransactionEntity(aHierarchyAppTransactionExDto.Id);

                Dictionary<Guid, int> dictGuidfileId = new Dictionary<Guid, int>();
                Dictionary<Guid, AppTransactionFieldEntity> dictIdAppTransactionFieldEntit = new Dictionary<Guid, AppTransactionFieldEntity>();

                foreach (var unitEntity in appTransactionEntity.AppTransactionUnit)
                {
                    foreach (var filedEntity in unitEntity.AppTransactionField)
                    {
                        dictGuidfileId.Add(filedEntity.RowIdentityGuid.Value, filedEntity.TransactionFieldId);
                        dictIdAppTransactionFieldEntit.Add(filedEntity.RowIdentityGuid.Value, filedEntity);
                    }

                }

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                        foreach (var entity in dictIdAppTransactionFieldEntit.Values)
                        {
                            entity.LinkToParentPrimaryKeyFieldId = null;


                        }

                        if (aHierarchyAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap != null)
                        {
                            foreach (Guid currkey in aHierarchyAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap.Keys)
                            {
                                Guid parakeGuid = aHierarchyAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap[currkey];
                                AppTransactionFieldEntity aAppTransactionFieldEntity = dictIdAppTransactionFieldEntit[currkey];
                                aAppTransactionFieldEntity.LinkToParentPrimaryKeyFieldId = dictGuidfileId[parakeGuid];


                            }
                        }

                        foreach (var entity in dictIdAppTransactionFieldEntit.Values)
                        {
                            adapter.UpdateEntitiesDirectly(entity, new RelationPredicateBucket(AppTransactionFieldFields.TransactionFieldId == entity.TransactionFieldId));


                        }

                        adapter.Commit();


                    }
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }







                if (skipPostSaveCacheSync)
                {
                    aOperationCallResult.Object = aHierarchyAppTransactionExDto;
                }
                else
                {
                    var freshaHierarchyAppTransactionExDto = GetHierarchyTranscationFromDatabase(aHierarchyAppTransactionExDto.Id);
                    SynchronizeDatabaseTableAndUpdateCahce(freshaHierarchyAppTransactionExDto);
                    aOperationCallResult.Object = GetHierarchyTranscationFromDatabase(aHierarchyAppTransactionExDto.Id);
                }


                AppTransactionExDto transactionExDto = aOperationCallResult.Object;

                if (transactionExDto.OtherOptions != null && transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId.HasValue)
                {
                    int importSettingId = transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId.Value;
                    AppDatabaseTableImportBL.UpdateTransactionDataFromImport_ReloadTransaction(importSettingId);
                }

                if (transactionExDto.SaasApplicationId.HasValue)
                {
                    if (transactionExDto.DictAllTransactionUnitIdExDto != null && transactionExDto.DataSourceFrom.HasValue)
                    {
                        Dictionary<string, int> dictDbObjNameAndUsageType = new Dictionary<string, int>();

                        var dbFixture = AppCacheManagerBL.GetOneDatabaseFixture(transactionExDto.DataSourceFrom.Value);



                        foreach (var unitExDto in transactionExDto.DictAllTransactionUnitIdExDto.Values.Where(o => !string.IsNullOrWhiteSpace(o.DataBaseTableName)))
                        {
                            string tableName = unitExDto.DataBaseTableName.ToLower().Trim();
                            if (!dictDbObjNameAndUsageType.ContainsKey(tableName))
                            {
                                if (dbFixture.AllViews().FirstOrDefault(o => o.Name.ToLower() == tableName) != null)
                                {
                                    dictDbObjNameAndUsageType.Add(tableName, (int)EmAppDataSetUsageType.DatabaseView);
                                }
                                else
                                {
                                    var existTable = dbFixture.Table(tableName);
                                    if (existTable != null)
                                    {
                                        dictDbObjNameAndUsageType.Add(tableName, (int)EmAppDataSetUsageType.DatabaseTable);
                                    }
                                }
                            }
                        }

                        if (dictDbObjNameAndUsageType.Count > 0)
                        {
                            AppDataSetBL.AddDatabaseObjectsToApplication(dictDbObjNameAndUsageType, transactionExDto.SaasApplicationId.Value);
                        }
                    }
                }

            }

            // Update  FK mapping



            return aOperationCallResult;
        }


        public static OperationCallResult<AppTransactionExDto> GenerateUnitsFromSelectedApiNodes(AppTransactionExDto appTransactionExDto, bool isSelectAllNode = false)
        {
            OperationCallResult<AppTransactionExDto> aOperationCallResult = new OperationCallResult<AppTransactionExDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            appTransactionExDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();

            if (appTransactionExDto.ApiDataStructure != null && appTransactionExDto.ApiDataStructure.Count > 0)
            {
                var rootNode = appTransactionExDto.ApiDataStructure.FirstOrDefault();

                if (rootNode != null)
                {
                    //if (rootNode.Children != null && rootNode.Children.Count == 1 && rootNode.Children[0].IsObject)
                    //{
                    //    rootNode = rootNode.Children[0];
                    //}



                    if (rootNode.IsSelected || isSelectAllNode)
                    {
                        if (appTransactionExDto.TransactionOrganizedType.HasValue)
                        {
                            if (appTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                            {
                                AppTransactionUnitExDto newUnit = ConvertOneApiNodeToUnit(rootNode, null, "", isSelectAllNode);
                                appTransactionExDto.AppTransactionUnitList.Add(newUnit);
                            }
                            else if (appTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                            {
                                if (rootNode.IsArray)
                                {
                                    AppTransactionUnitExDto newUnit = ConvertOneApiNodeToUnit(rootNode, null, "", isSelectAllNode);
                                    appTransactionExDto.AppTransactionUnitList.Add(newUnit);
                                }
                                else if (rootNode.IsObject)
                                {
                                    var rootArrayNode = FindFirstSelectedChildArrrayNode(rootNode, isSelectAllNode);

                                    if (rootArrayNode != null)
                                    {
                                        string unitPath = rootArrayNode.NodePath;
                                        rootArrayNode.NodePath = "";
                                        AppTransactionUnitExDto newUnit = ConvertOneApiNodeToUnit(rootArrayNode, null, unitPath, isSelectAllNode);
                                        appTransactionExDto.AppTransactionUnitList.Add(newUnit);
                                    }
                                }
                            }
                        }

                    }
                }
            }

            PrepareFileUploadApiUnit(appTransactionExDto);

            aOperationCallResult.Object = appTransactionExDto;

            return aOperationCallResult;
        }


        public static OperationCallResult<AppTransactionExDto> SynchronizeTransactionUnitFieldsWithApiNodes(AppTransactionExDto appTransactionExDto)
        {
            OperationCallResult<AppTransactionExDto> aOperationCallResult = new OperationCallResult<AppTransactionExDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (appTransactionExDto.ApiDataStructure != null && appTransactionExDto.ApiDataStructure.Count > 0
                && appTransactionExDto.AppTransactionUnitList != null && appTransactionExDto.AppTransactionUnitList.Count > 0)
            {
                var rootNode = appTransactionExDto.ApiDataStructure.FirstOrDefault();
                AppTransactionUnitExDto orgRootUnit = appTransactionExDto.AppTransactionUnitList.FirstOrDefault();


                if (appTransactionExDto.TransactionOrganizedType.HasValue)
                {
                    if (appTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                    {
                        AppTransactionUnitExDto newRootTempUnit = ConvertOneApiNodeToUnit(rootNode, null, "", true);
                        SynchronizeTransactionUnitFieldsWithApiNodes_ProcessOneUnitAndChildUnits(aValidationResult, orgRootUnit, newRootTempUnit);

                    }
                    else if (appTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                    {
                        if (rootNode.IsArray)
                        {
                            AppTransactionUnitExDto newRootTempUnit = ConvertOneApiNodeToUnit(rootNode, null, "", true);
                            SynchronizeTransactionUnitFieldsWithApiNodes_ProcessOneUnitAndChildUnits(aValidationResult, orgRootUnit, newRootTempUnit);
                        }
                        else if (rootNode.IsObject)
                        {
                            var rootArrayNode = FindFirstSelectedChildArrrayNode(rootNode);

                            if (rootArrayNode != null)
                            {
                                string unitPath = rootArrayNode.NodePath;
                                rootArrayNode.NodePath = "";
                                AppTransactionUnitExDto newRootTempUnit = ConvertOneApiNodeToUnit(rootArrayNode, null, unitPath, true);
                                SynchronizeTransactionUnitFieldsWithApiNodes_ProcessOneUnitAndChildUnits(aValidationResult, orgRootUnit, newRootTempUnit);
                            }
                        }
                    }
                }

            }

            aOperationCallResult.Object = appTransactionExDto;

            return aOperationCallResult;
        }

        private static void SynchronizeTransactionUnitFieldsWithApiNodes_ProcessOneUnitAndChildUnits(ValidationResult aValidationResult, AppTransactionUnitExDto orgRootUnit, AppTransactionUnitExDto newRootTempUnit)
        {
            if (newRootTempUnit.DataBaseTableName == orgRootUnit.DataBaseTableName)
            {
                List<string> newPropertyNameList = new List<string>();

                foreach (var newFieldDto in newRootTempUnit.AppTransactionFieldList)
                {
                    if (!string.IsNullOrWhiteSpace(newFieldDto.DataBaseFieldName))
                    {
                        newPropertyNameList.Add(newFieldDto.DataBaseFieldName);

                        var orgFieldDto = orgRootUnit.AppTransactionFieldList.FirstOrDefault(o => o.DataBaseFieldName == newFieldDto.DataBaseFieldName);

                        if (orgFieldDto != null)
                        {
                            orgFieldDto.IsTempVariable = false;
                        }
                        else
                        {
                            orgRootUnit.AppTransactionFieldList.Add(newFieldDto);
                        }
                    }
                }

                foreach (var fieldDto in orgRootUnit.AppTransactionFieldList)
                {
                    if (!newPropertyNameList.Contains(fieldDto.DataBaseFieldName))
                    {
                        fieldDto.IsTempVariable = true;
                    }
                }

                foreach (var orgChildUnit in orgRootUnit.Children)
                {
                    AppTransactionUnitExDto newTempChildUnit = null;

                    if (newRootTempUnit.Children != null)
                    {
                        newTempChildUnit = newRootTempUnit.Children.FirstOrDefault(o => o.DataBaseTableName == orgChildUnit.DataBaseTableName);
                    }

                    if (newTempChildUnit != null)
                    {
                        SynchronizeTransactionUnitFieldsWithApiNodes_ProcessOneUnitAndChildUnits(aValidationResult, orgChildUnit, newTempChildUnit);
                    }
                }

            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "SynchronizeTransactionUnitFieldsWithApiNodes_Warning", ValidationItemType.Warning,
                    "Cannot find API node for unit \"" + orgRootUnit.UnitDisplayName + "\"."));
            }
        }

        private static void PrepareFileUploadApiUnit(AppTransactionExDto appTransactionExDto)
        {
            if (appTransactionExDto.BaseApiConfigDto != null && appTransactionExDto.BaseApiConfigDto.OtherSettingsDto != null)
            {
                EmAppApiPayloadDataType payloadDataType = EmAppApiPayloadDataType.JSON;

                if (appTransactionExDto.BaseApiConfigDto.OtherSettingsDto.PayloadDataType.HasValue)
                {
                    payloadDataType = appTransactionExDto.BaseApiConfigDto.OtherSettingsDto.PayloadDataType.Value;
                }

                if (payloadDataType == EmAppApiPayloadDataType.ServerFilePath || payloadDataType == EmAppApiPayloadDataType.FileByteArray)
                {
                    if (appTransactionExDto.TransactionOrganizedType.HasValue)
                    {
                        if (appTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                        {
                            AppTransactionUnitExDto unitDto = appTransactionExDto.AppTransactionUnitList.FirstOrDefault();

                            if (unitDto == null)
                            {
                                unitDto = new AppTransactionUnitExDto();
                                unitDto.Children = new List<AppTransactionUnitExDto>();
                                unitDto.AppTransactionFieldList = new ObservableSet<AppTransactionFieldExDto>();

                                unitDto.IsPrimaryKeyIdentityInsert = false;
                                unitDto.IsSynchToDatabaseTable = false;

                                unitDto.UnitDisplayName = "File Info";
                                unitDto.DataBaseTableName = "";
                                appTransactionExDto.AppTransactionUnitList.Add(unitDto);
                            }

                            AppTransactionFieldExDto fieldDto = new AppTransactionFieldExDto();

                            fieldDto.DataType = (int)EmAppDataType.String;
                            fieldDto.ControlType = (int)EmAppControlType.TextBox;
                            fieldDto.IsTempVariable = true;
                            fieldDto.IsAllowEmpty = true;
                            fieldDto.IsVisible = true;
                            fieldDto.Nbdecimal = 0;
                            fieldDto.RowIdentityGuid = Guid.NewGuid();

                            if (payloadDataType == EmAppApiPayloadDataType.ServerFilePath)
                            {
                                fieldDto.ControlType = (int)EmAppControlType.TextBox;
                                fieldDto.DataBaseFieldName = "ServerFilePath";
                                fieldDto.DisplayName = "Server File Path, Web File Url, Or Ftp File Path";
                                fieldDto.MappingEmSystemTokenField = (int)EmBLFiledMappingSystemTokenField.ApiUploadServerFilePath;


                                AppTransactionFieldExDto ftpUserNamefieldDto = new AppTransactionFieldExDto();

                                ftpUserNamefieldDto.DataType = (int)EmAppDataType.String;
                                ftpUserNamefieldDto.ControlType = (int)EmAppControlType.TextBox;
                                ftpUserNamefieldDto.IsTempVariable = true;
                                ftpUserNamefieldDto.IsAllowEmpty = true;
                                ftpUserNamefieldDto.IsVisible = true;
                                ftpUserNamefieldDto.Nbdecimal = 0;
                                ftpUserNamefieldDto.RowIdentityGuid = Guid.NewGuid();
                                fieldDto.DataBaseFieldName = "FtpUsername";
                                fieldDto.DisplayName = "Ftp Username";
                                fieldDto.MappingEmSystemTokenField = (int)EmBLFiledMappingSystemTokenField.ApiUploadFtpUserName;

                                AppTransactionFieldExDto ftpPasswordfieldDto = new AppTransactionFieldExDto();

                                ftpPasswordfieldDto.DataType = (int)EmAppDataType.String;
                                ftpPasswordfieldDto.ControlType = (int)EmAppControlType.TextBox;
                                ftpPasswordfieldDto.IsTempVariable = true;
                                ftpPasswordfieldDto.IsAllowEmpty = true;
                                ftpPasswordfieldDto.IsVisible = true;
                                ftpPasswordfieldDto.Nbdecimal = 0;
                                ftpPasswordfieldDto.RowIdentityGuid = Guid.NewGuid();
                                fieldDto.DataBaseFieldName = "FtpPassword";
                                fieldDto.DisplayName = "Ftp Password";
                                fieldDto.MappingEmSystemTokenField = (int)EmBLFiledMappingSystemTokenField.ApiUploadFtpPassword;

                                unitDto.AppTransactionFieldList.Add(fieldDto);
                                unitDto.AppTransactionFieldList.Add(ftpUserNamefieldDto);
                                unitDto.AppTransactionFieldList.Add(ftpPasswordfieldDto);
                            }
                            else if (payloadDataType == EmAppApiPayloadDataType.FileByteArray)
                            {
                                fieldDto.ControlType = (int)EmAppControlType.Numeric;
                                fieldDto.DataBaseFieldName = "FileId";
                                fieldDto.DisplayName = "File Id";
                                fieldDto.MappingEmSystemTokenField = (int)EmBLFiledMappingSystemTokenField.ApiUploadFileId;
                                unitDto.AppTransactionFieldList.Add(fieldDto);
                            }

                            

                        }
                    }
                }
            }
        }

        private static ApiDataStructureNodeDto FindFirstSelectedChildArrrayNode(ApiDataStructureNodeDto node, bool isSelectAllNode = false)
        {
            if (string.IsNullOrWhiteSpace(node.NodePath))
            {
                node.NodePath = node.Name;
            }

            foreach (var childNode in node.Children.Where(o => o.IsSelected || isSelectAllNode))
            {
                if (childNode.IsArray || childNode.IsObject)
                {
                    childNode.NodePath = childNode.Name;

                    if (!string.IsNullOrWhiteSpace(node.NodePath))
                    {
                        childNode.NodePath = node.NodePath + "___" + childNode.Name;
                    }
                }

                if (childNode.IsArray)
                {
                    return childNode;
                }
                else if (childNode.IsObject)
                {
                    return FindFirstSelectedChildArrrayNode(childNode, isSelectAllNode);
                }
            }

            return null;
        }

        private static AppTransactionEntity PorcessNewTeanscation(AppTransactionExDto aHierarchyAppTransactionExDto, ValidationResult aValidationResult, Dictionary<string, string> dictChildParentTableNameMapping)
        {

            AppTransactionEntity aAppTransactionEntity = new AppTransactionEntity();
            AppTransactionConverter.CopyDtoToEntity(aAppTransactionEntity, aHierarchyAppTransactionExDto);

            //if (aAppTransactionEntity.DataSourceFrom.HasValue && aAppTransactionEntity.DataSourceFrom.Value == int.MaxValue)
            //{
            //    aAppTransactionEntity.DataSourceFrom = null;
            //}

            foreach (var unitDto in aHierarchyAppTransactionExDto.AppTransactionUnitList)
            {
                if (string.IsNullOrWhiteSpace(unitDto.SchemaOwner))
                {
                    unitDto.SchemaOwner = AppMetaDataBL.GetCurrentDbConnectionDefaultSchmeOner(aHierarchyAppTransactionExDto.DataSourceFrom);
                }

                AppTransactionUnitEntity aAppTransactionUnitEntity = new AppTransactionUnitEntity();
                AppTransactionUnitConverter.CopyDtoToEntity(aAppTransactionUnitEntity, unitDto);
                aAppTransactionEntity.AppTransactionUnit.Add(aAppTransactionUnitEntity);


                foreach (var fieldDto in unitDto.AppTransactionFieldList)
                {
                    //if ((fieldDto.IsTempVariable.HasValue && fieldDto.IsTempVariable.Value  || fieldDto.IsStoreToExtendTable.HasValue && fieldDto.IsStoreToExtendTable.Value)
                    //    && !fieldDto.DataBaseFieldName.HasValue()
                    //    && !(aHierarchyAppTransactionExDto.OtherOptions != null && aHierarchyAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction))
                    //{
                    //    fieldDto.DataBaseFieldName = fieldDto.DisplayName.Replace(" ", "");
                    //}

                    AppTransactionFieldEntity aAppTransactionFieldEntity = new AppTransactionFieldEntity();
                    AppTransactionFieldConverter.CopyDtoToEntity(aAppTransactionFieldEntity, fieldDto);
                    aAppTransactionUnitEntity.AppTransactionField.Add(aAppTransactionFieldEntity);


                }


            }



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionEntity);

                    UpdateChildEntityParentUnitID(aAppTransactionEntity, dictChildParentTableNameMapping, adapter);


                    //// for new listTranscation, need to add Foder root on the fly
                    //if (aHierarchyAppTransactionExDto.TransactionOrganizedType.HasValue && aHierarchyAppTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.FolderList)
                    //{
                    //    AppSefolderEntity aAppSefolderEntity = new AppSefolderEntity();
                    //    aAppSefolderEntity.Name = "Root" + aHierarchyAppTransactionExDto.TransactionName;
                    //    aAppSefolderEntity.Description = aHierarchyAppTransactionExDto.Description;

                    //    // only can acess aAppTransactionEntity.TransactionId
                    //    aAppSefolderEntity.TransactionId = aAppTransactionEntity.TransactionId;


                    //    adapter.SaveEntity(aAppSefolderEntity);


                    //}

                    int? apiId = aHierarchyAppTransactionExDto.FolderUsageType;

                    if (apiId.HasValue)
                    {
                        SynchronizeComsumeApiTransactionId(aAppTransactionEntity.TransactionId, apiId, adapter);
                    }

                    adapter.Commit();

                    aHierarchyAppTransactionExDto.Id = aAppTransactionEntity.TransactionId;
                    
                }


                // Database FK Exeption ........
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            if (!aValidationResult.HasErrors && !(aHierarchyAppTransactionExDto.OtherOptions != null && aHierarchyAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction))
            {
                UpdateTempOrExtendFieldDatabaseFieldName(aAppTransactionEntity.TransactionId, aValidationResult);
            }

            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
            }


            return aAppTransactionEntity;
        }

        private static void UpdateTempOrExtendFieldDatabaseFieldName(int transactionId, ValidationResult aValidationResult)
        {
            List<int> needToUpdateTransFieldIds = new List<int>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {                
                    AppTransactionEntity aAppTransactionEntity = RetrieveOneAppTransactionEntity(transactionId);

                    aAppTransactionEntity.AppTransactionUnit.ForAll(o => o.AppTransactionField.ForAll(p =>
                    {
                        if (string.IsNullOrWhiteSpace(p.DataBaseFieldName) &&
                            (p.IsTempVariable.HasValue && p.IsTempVariable.Value || p.IsStoreToExtendTable.HasValue && p.IsStoreToExtendTable.Value))
                        {
                            needToUpdateTransFieldIds.Add(p.TransactionFieldId);
                        }
                    }));


                    EntityCollection<AppTransactionFieldEntity> list = new EntityCollection<AppTransactionFieldEntity>();
                    RelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionFieldFields.TransactionFieldId == needToUpdateTransFieldIds);

                    adapter.FetchEntityCollection(list, filter);

                    foreach (var fieldEntity in list)
                    {

                        fieldEntity.DataBaseFieldName = fieldEntity.DisplayName.Replace(" ", "") + "_" + fieldEntity.TransactionFieldId;
                        adapter.UpdateEntitiesDirectly(fieldEntity, new RelationPredicateBucket(AppTransactionFieldFields.TransactionFieldId == fieldEntity.TransactionFieldId));

                    }
                }                
                catch (Exception ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }            
        }

        public static List<AppTransactionFieldExDto> RetrieveTransactionFieldsByControlTypes(List<int> controlTypes)
        {
            List<AppTransactionFieldExDto> transactionFieldList = new List<AppTransactionFieldExDto>();

            if (controlTypes != null && controlTypes.Count > 0)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppTransactionFieldEntity> list = new EntityCollection<AppTransactionFieldEntity>();

                    RelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionFieldFields.ControlType == controlTypes.ToArray());

                    IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppTransactionFieldEntity);
                    rootPath.Add(AppTransactionFieldEntity.PrefetchPathAppTransactionUnit);
                    rootPath.Add(AppTransactionUnitEntity.PrefetchPathAppTransaction);

                    adapter.FetchEntityCollection(list, filter, rootPath);

                    foreach (var transactionFieldEntity in list)
                    {
                        AppTransactionFieldExDto appTransactionFieldExDto = AppTransactionFieldConverter.ConvertEntityToExDto(transactionFieldEntity);
                        var transUnit = transactionFieldEntity.AppTransactionUnit;
                        if (transUnit != null)
                        {
                            appTransactionFieldExDto.DataBaseTableName = transUnit.DataBaseTableName;
                            appTransactionFieldExDto.UnitDisplayName = transUnit.UnitDisplayName;
                            appTransactionFieldExDto.TransactionId = transUnit.TransactionId;
                            appTransactionFieldExDto.ForeignAppTransactionUnitExDto = AppTransactionUnitConverter.ConvertEntityToExDto(transUnit);

                            //if (transUnit.Transaction != null)
                            //{
                            //    appTransactionFieldExDto.TransactionName = transUnit.AppTransaction.TransactionName;
                            //    appTransactionFieldExDto.TransactionOrganizedType = transUnit.AppTransaction.TransactionOrganizedType;
                            //    appTransactionFieldExDto.ForeignAppTransactionUnitExDto.ForeignAppTransactionExDto = AppTransactionConverter.ConvertEntityToExDto(transUnit.AppTransaction);
                            //}
                        }

                        transactionFieldList.Add(appTransactionFieldExDto);
                    }
                }
            }

            return transactionFieldList;
        }


        private static void UpdateChildEntityParentUnitID(AppTransactionEntity aAppTransactionEntity, Dictionary<string, string> dictChildParentTableNameMapping, DataAccessAdapter adapter)
        {
            EntityCollection<AppTransactionUnitEntity> list = new EntityCollection<AppTransactionUnitEntity>();
            RelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionUnitFields.TransactionId == aAppTransactionEntity.TransactionId);

            adapter.FetchEntityCollection(list, filter);

            var dictdataBasenameAndUnitentity = list.ToDictionary(o => o.UnitDisplayName, o => o);


            foreach (string childTableTableName in dictChildParentTableNameMapping.Keys)
            {

                AppTransactionUnitEntity childEntity = dictdataBasenameAndUnitentity[childTableTableName];

                string parentTablename = dictChildParentTableNameMapping[childTableTableName];

                AppTransactionUnitEntity parentEntity = dictdataBasenameAndUnitentity[dictChildParentTableNameMapping[childTableTableName]];

                childEntity.ParentTransactionUnitId = parentEntity.TransactionUnitId;


                adapter.UpdateEntitiesDirectly(childEntity, new RelationPredicateBucket(AppTransactionUnitFields.TransactionUnitId == childEntity.TransactionUnitId));

            }

        }



        public static OperationCallResult<object> DeleteOneAppTransaction(object transcactionId, bool isDeleteForm = true, bool isDeleteImportSetting = true)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;



            List<int> deleteUnitIds = new List<int>();
            List<int> deleteTransactionFieldIds = new List<int>();

            try
            {
                var transactionExDto = RetrieveOneAppTransactionExDto(transcactionId);

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {

                    EntityCollection<AppTransactionUnitEntity> list = new EntityCollection<AppTransactionUnitEntity>();

                    IncludeFieldsList icludeFieldsList = new IncludeFieldsList();
                    icludeFieldsList.Add(AppTransactionUnitFields.TransactionUnitId);

                    var fitler = new RelationPredicateBucket(AppTransactionUnitFields.TransactionId == transcactionId);

                    adapter.FetchEntityCollection(list, icludeFieldsList, fitler);

                    deleteUnitIds.AddRange(list.Select(o => o.TransactionUnitId));


                    // need to remove
                    EntityCollection<AppTransactionFieldEntity> listTranscationField = new EntityCollection<AppTransactionFieldEntity>();

                    IncludeFieldsList icludeFiledFieldsList = new IncludeFieldsList();
                    icludeFiledFieldsList.Add(AppTransactionFieldFields.TransactionFieldId);

                    var fitlerField = new RelationPredicateBucket(AppTransactionFieldFields.TransactionUnitId == deleteUnitIds);

                    adapter.FetchEntityCollection(listTranscationField, icludeFiledFieldsList, fitlerField);

                    deleteTransactionFieldIds.AddRange(listTranscationField.Select(o => o.TransactionFieldId));

                }



                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                        if (isDeleteForm)
                        {
                            adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.TransactionFieldId == deleteTransactionFieldIds));
                            adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.GridTransactionUnitId == deleteUnitIds));
                        }
                        else // Delete From Form Creation
                        {
                            adapter.UpdateEntitiesDirectly(new AppFormLayoutItemEntity() { TransactionFieldId = null }, new RelationPredicateBucket(AppFormLayoutItemFields.TransactionFieldId == deleteTransactionFieldIds));
                            adapter.UpdateEntitiesDirectly(new AppFormLayoutItemEntity() { GridTransactionUnitId = null }, new RelationPredicateBucket(AppFormLayoutItemFields.GridTransactionUnitId == deleteUnitIds));
                        }


                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionSaveAsMappingEntity), new RelationPredicateBucket(AppTransactionSaveAsMappingFields.TransactionId == transcactionId));
                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionSaveAsMappingEntity), new RelationPredicateBucket(AppTransactionSaveAsMappingFields.SourceFiledId == deleteTransactionFieldIds));
                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionSaveAsMappingEntity), new RelationPredicateBucket(AppTransactionSaveAsMappingFields.TargetFiledId == deleteTransactionFieldIds));
                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionDataTransferSettingEntity), new RelationPredicateBucket(AppTransactionDataTransferSettingFields.TransactionId == transcactionId));
                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionDataTransferSettingEntity), new RelationPredicateBucket(AppTransactionDataTransferSettingFields.DestinationTransactionId == transcactionId));

                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionFieldEntity), new RelationPredicateBucket(AppTransactionFieldFields.TransactionUnitId == deleteUnitIds));
                        //need to remove the AppFormLayoutItem
                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitFormulaEntity), new RelationPredicateBucket(AppTransactionUnitFormulaFields.TransactionUnitId == deleteUnitIds));

                        adapter.DeleteEntitiesDirectly(typeof(AppFormLinkTargetEntity), new RelationPredicateBucket(AppFormLinkTargetFields.LinkTargetTransactionId == transcactionId));
                        adapter.DeleteEntitiesDirectly(typeof(AppFormLinkTargetEntity), new RelationPredicateBucket(AppFormLinkTargetFields.TransactionUnitId == deleteUnitIds.ToArray()));



                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitEntity), new RelationPredicateBucket(AppTransactionUnitFields.TransactionId == transcactionId));
                        adapter.DeleteEntitiesDirectly(typeof(AppmessageNotificationSettingEntity), new RelationPredicateBucket(AppmessageNotificationSettingFields.TranscationId == transcactionId));

                        adapter.DeleteEntitiesDirectly(typeof(AppProjectWorkFlowActionEntity), new RelationPredicateBucket(AppProjectWorkFlowActionFields.CommandTransactionId == transcactionId));
                        adapter.DeleteEntitiesDirectly(typeof(AppProjectWorkFlowActionEntity), new RelationPredicateBucket(AppProjectWorkFlowActionFields.NextTransactionId == transcactionId));
                        adapter.DeleteEntitiesDirectly(typeof(AppProjectWorkFlowActionEntity), new RelationPredicateBucket(AppProjectWorkFlowActionFields.TransactionId == transcactionId));

                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionNavigationEntity), new RelationPredicateBucket(AppTransactionNavigationFields.TransactionId == transcactionId));

                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionEntity), new RelationPredicateBucket(AppTransactionFields.TransactionId == transcactionId));





                        string message = StringLocalizer.Localize(App_TransactionEntity_Delete_Ok, "Transaction delete successful");
                        aValidationResult.Items.Add(new ValidationItem(null, App_TransactionEntity_Delete_Ok, ValidationItemType.Message, message));



                        adapter.Commit();
                    }

                    // FK Exception .......
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        string message = StringLocalizer.Localize(App_TransactionEntity_Delete_Failed, "Transaction Delete Failed" + ex.ToString());
                        aValidationResult.Items.Add(new ValidationItem(null, App_TransactionEntity_Delete_Failed, ValidationItemType.Error, message));
                    }
                }

                // if no any errors
                if (!aOperationCallResult.ValidationResult.HasErrors)
                {
                    aOperationCallResult.Object = transcactionId;

                    try
                    {

                        if (transactionExDto.OtherOptions != null && isDeleteImportSetting)
                        {
                            if (transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId.HasValue)
                            {
                                AppDataSetBL.DeleteOneAppDataSetEntityDto(transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId.Value);
                            }

                            if (transactionExDto.OtherOptions.ImportSettingId.HasValue)
                            {
                                AppDataSetBL.DeleteOneAppDataSetEntityDto(transactionExDto.OtherOptions.ImportSettingId.Value);
                            }

                        }

                    }
                    catch (Exception ex)
                    {

                    }


                }

            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(null, App_TransactionEntity_Delete_Ok, ValidationItemType.Error, "Deleting data model does not exist."));

            }

            return aOperationCallResult;
        }

        private static ValidationResult ProcessDirtyAppTransactionExDto(AppTransactionExDto aAppTransactionExDto, Dictionary<string, string> dictChildParentTableNameMapping)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTransactionEntity aServerAppTransactionEntity = RetrieveOneAppTransactionEntity(aAppTransactionExDto.Id);

            bool isNeedToSynchronizeComsumeApiTransactionId = false;

            if (aAppTransactionExDto.OtherOptions != null && aAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction)
            {
                string orgApiId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aServerAppTransactionEntity.FolderUsageType);
                string newApiId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aAppTransactionExDto.FolderUsageType);

                if (orgApiId != newApiId)
                {
                    isNeedToSynchronizeComsumeApiTransactionId = true;
                }
            }


            Dictionary<int, AppTransactionUnitEntity> dictAppTransactionUnitFromDbms = aServerAppTransactionEntity.AppTransactionUnit.ToDictionary(o => o.TransactionUnitId, o => o);

            List<int> orgUnitIds = dictAppTransactionUnitFromDbms.Keys.ToList();
            List<int> unitIds = aAppTransactionExDto.AppTransactionUnitList.Where(o => o.Id != null).Select(o => (int)o.Id).ToList();

            int[] deleteUnitIds = orgUnitIds.Except(unitIds).ToArray();
            List<int> deleteFieldsIDs = new List<int>();

            foreach (int deleteUnitId in deleteUnitIds)
            {
                deleteFieldsIDs.AddRange(dictAppTransactionUnitFromDbms[deleteUnitId].AppTransactionField.Select(o => o.TransactionFieldId).ToList());
            }
            

            AppTransactionConverter.CopyDtoToEntity(aServerAppTransactionEntity, aAppTransactionExDto);




            //------- check   AppTransactionUnit

            // new Items
            foreach (var unitDto in aAppTransactionExDto.AppTransactionUnitList.FindNewItems())
            {
                if (string.IsNullOrWhiteSpace(unitDto.SchemaOwner))
                {
                    unitDto.SchemaOwner = AppMetaDataBL.GetCurrentDbConnectionDefaultSchmeOner(aAppTransactionExDto.DataSourceFrom);
                }

                AppTransactionUnitEntity aAppTransactionUnitEntity = new AppTransactionUnitEntity();
                AppTransactionUnitConverter.CopyDtoToEntity(aAppTransactionUnitEntity, unitDto);
                aServerAppTransactionEntity.AppTransactionUnit.Add(aAppTransactionUnitEntity);


                foreach (var fieldDto in unitDto.AppTransactionFieldList)
                {
                    //if ((fieldDto.IsTempVariable.HasValue && fieldDto.IsTempVariable.Value || fieldDto.IsStoreToExtendTable.HasValue && fieldDto.IsStoreToExtendTable.Value) 
                    //    && !fieldDto.DataBaseFieldName.HasValue()
                    //    && !(aAppTransactionExDto.OtherOptions != null && aAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction))
                    //{
                    //    fieldDto.DataBaseFieldName = fieldDto.DisplayName.Replace(" ", "");
                    //}

                    AppTransactionFieldEntity aAppTransactionFieldEntity = new AppTransactionFieldEntity();
                    AppTransactionFieldConverter.CopyDtoToEntity(aAppTransactionFieldEntity, fieldDto);
                    aAppTransactionUnitEntity.AppTransactionField.Add(aAppTransactionFieldEntity);



                }
            }



            
            foreach (var unitDto in aAppTransactionExDto.AppTransactionUnitList.FindModifiedItems())
            {
                int dtoKey = int.Parse(unitDto.Id.ToString());
                if (dictAppTransactionUnitFromDbms.ContainsKey(dtoKey))
                {
                    AppTransactionUnitEntity aAppTransactionUnitEntity = dictAppTransactionUnitFromDbms[dtoKey];

                    // need to set ParentTransactionUnitId as null 
                    aAppTransactionUnitEntity.ParentTransactionUnitId = null;

                    var dictAppTransactionFieldFromDbms = aAppTransactionUnitEntity.AppTransactionField.ToDictionary(o => o.TransactionFieldId, o => o);

                    AppTransactionUnitConverter.CopyDtoToEntity(aAppTransactionUnitEntity, unitDto);

                    // new Field Item
                    foreach (var fieldDto in unitDto.AppTransactionFieldList.FindNewItems())
                    {
                        //if ((fieldDto.IsTempVariable.HasValue && fieldDto.IsTempVariable.Value || fieldDto.IsStoreToExtendTable.HasValue && fieldDto.IsStoreToExtendTable.Value)
                        //    && !fieldDto.DataBaseFieldName.HasValue()
                        //    && !(aAppTransactionExDto.OtherOptions != null && aAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction))
                        //{
                        //    fieldDto.DataBaseFieldName = fieldDto.DisplayName.Replace(" ", "");
                        //}

                        AppTransactionFieldEntity aAppTransactionFieldEntity = new AppTransactionFieldEntity();
                        AppTransactionFieldConverter.CopyDtoToEntity(aAppTransactionFieldEntity, fieldDto);
                        aAppTransactionUnitEntity.AppTransactionField.Add(aAppTransactionFieldEntity);
                    }

                    // dirty  FieldItem


                    foreach (var fieldDto in unitDto.AppTransactionFieldList.FindModifiedItems())
                    {
                        int fieldDtoKey = int.Parse(fieldDto.Id.ToString());
                        if (dictAppTransactionFieldFromDbms.ContainsKey(fieldDtoKey))
                        {
                            //if ((fieldDto.IsTempVariable.HasValue && fieldDto.IsTempVariable.Value || fieldDto.IsStoreToExtendTable.HasValue && fieldDto.IsStoreToExtendTable.Value) 
                            //    && !fieldDto.DataBaseFieldName.HasValue()
                            //    && !(aAppTransactionExDto.OtherOptions != null && aAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction))
                            //{
                            //    fieldDto.DataBaseFieldName = fieldDto.DisplayName.Replace(" ", "");
                            //}

                            AppTransactionFieldEntity aAppTransactionFieldEntity = dictAppTransactionFieldFromDbms[fieldDtoKey];
                            AppTransactionFieldConverter.CopyDtoToEntity(aAppTransactionFieldEntity, fieldDto);

                        }
                    }


                    // delete FieldITEm
                    deleteFieldsIDs.AddRange(unitDto.AppTransactionFieldList.FindDeletedItemIds().Cast<int>());

                }




            }

            // deletedIDs
            // int[] deleteUnitIds = aAppTransactionExDto.AppTransactionUnitList.FindDeletedItemIds().Cast<int>().ToArray();




            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aServerAppTransactionEntity);


                    // Need to delete  AppTransactionUnitFields

                    if (deleteFieldsIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitExtendFieldValueEntity), new RelationPredicateBucket(AppTransactionUnitExtendFieldValueFields.UnitExtendFiledId == deleteFieldsIDs));
                        adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.TransactionFieldId == deleteFieldsIDs));
                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionFieldEntity), new RelationPredicateBucket(AppTransactionFieldFields.TransactionFieldId == deleteFieldsIDs));
                    }

                    if (deleteUnitIds.Count() > 0)
                    {

                        //need to remove the AppFormLayoutItem
                        adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.GridTransactionUnitId == deleteUnitIds));

                        //need to remove the AppFormLayoutItem
                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitFormulaEntity), new RelationPredicateBucket(AppTransactionUnitFormulaFields.TransactionUnitId == deleteUnitIds));

                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionFieldEntity), new RelationPredicateBucket(AppTransactionFieldFields.TransactionUnitId == deleteUnitIds));

                        // need to set     aAppTransactionUnitEntity.ParentTransactionUnitId = null to remove FK

                        AppTransactionUnitEntity updateAppTransactionUnitEntity = new AppTransactionUnitEntity();
                        updateAppTransactionUnitEntity.ParentTransactionUnitId = null;

                        adapter.UpdateEntitiesDirectly(updateAppTransactionUnitEntity, new RelationPredicateBucket(AppTransactionUnitFields.TransactionUnitId == deleteUnitIds));

                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitEntity), new RelationPredicateBucket(AppTransactionUnitFields.TransactionUnitId == deleteUnitIds));


                    }

                    UpdateChildEntityParentUnitID(aServerAppTransactionEntity, dictChildParentTableNameMapping, adapter);
                       
                    if (isNeedToSynchronizeComsumeApiTransactionId)
                    {
                        int? apiId = aAppTransactionExDto.FolderUsageType;
                        int transactionId = (int)aAppTransactionExDto.Id;

                        SynchronizeComsumeApiTransactionId(transactionId, apiId, adapter);
                    }


                    adapter.Commit();                    
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            if (!aValidationResult.HasErrors && !(aAppTransactionExDto.OtherOptions != null && aAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction))
            {
                UpdateTempOrExtendFieldDatabaseFieldName(aServerAppTransactionEntity.TransactionId, aValidationResult);
            }

            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
            }

            return aValidationResult;
        }

        public static void SynchronizeDatabaseTableAndUpdateCahce(AppTransactionExDto appTransactionExDto)
        {



            foreach (var oneUnit in appTransactionExDto.DictAllTransactionUnitIdExDto.Values)
            {

                if (oneUnit.IsSynchToDatabaseTable.HasValue && oneUnit.IsSynchToDatabaseTable.Value)
                {

                    string tableName = oneUnit.DataBaseTableName;

                    if (string.IsNullOrWhiteSpace(tableName))
                    {
                        GenerateTranscationOneUnitDatabaseTable(oneUnit, appTransactionExDto.SaasApplicationId);

                    }
                    else  // table all ready exist !!
                    {
                        UpdateTranscationOneUnitDatabaseTable(oneUnit);

                    }

                    // -- need to update Table Cache

                    AppCacheManagerBL.RefreshOneTableCache(oneUnit.DataBaseTableName, oneUnit.DataSourceFrom, oneUnit.SchemaOwner);
                }


            }


            AppCacheManagerBL.RefreshOnetHierarchyTranscation(appTransactionExDto.Id);




        }

        private static void GenerateTranscationOneUnitDatabaseTable(AppTransactionUnitExDto transactionUnitDto, int? applicationId)
        {
            DatabaseTable dbtable = new DatabaseTable();
            dbtable.DataSourceRegisterId = transactionUnitDto.DataSourceFrom;
            string dbTablename = ConvertDisplayStringToDBString(transactionUnitDto.UnitDisplayName);
            dbtable.Name = AppUserDefineTablePrefix + dbTablename;
            dbtable.SchemaOwner = transactionUnitDto.SchemaOwner;


            // check if table is arlead exists !                       
            DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(dbtable.DataSourceRegisterId, transactionUnitDto.SchemaOwner);

            var existTable = databaseFixtureInstance.Table(dbtable.Name);

            if (existTable != null)
            {
                dbtable.Name = dbtable.Name + "_";
            }

            transactionUnitDto.DataBaseTableName = dbtable.Name;





            foreach (var transactionField in transactionUnitDto.AppTransactionFieldList)
            {
                DatabaseColumn column = ConvertTransactionFieldDtoToDatabaseColumn(transactionField);

                dbtable.Columns.Add(column);

                //!!!!
                transactionField.DataBaseFieldName = column.Name;

            }
            string createTableResultMsg = "";
            AppMetaDataBL.CreateNewTable(dbtable, dbtable.DataSourceRegisterId, applicationId, out createTableResultMsg);

            UpdateTransactionUnitDatabaeName(transactionUnitDto);



        }


        private static void UpdateTranscationOneUnitDatabaseTable(AppTransactionUnitExDto transactionUnitDto)
        {
            DatabaseTable dbtable = AppMetaDataBL.GetOneDatabaseTableSchema(transactionUnitDto.DataBaseTableName, transactionUnitDto.DataSourceFrom, transactionUnitDto.SchemaOwner);


            var dictTableColumn = dbtable.Columns.ToDictionary(o => o.Name, o => o);
            var dictTransFiledTableColumn = transactionUnitDto.AppTransactionFieldList.Where(o => !string.IsNullOrWhiteSpace(o.DataBaseFieldName)).ToDictionary(o => o.DataBaseFieldName, o => o);

            SchemaMetaDataDto schemaMetaDataDto = new SchemaMetaDataDto();

            //  schemaMetaDataDto.DatabaseTable = dbtable;


            List<DatabaseColumn> ListNewDatabaseColumn = new List<DatabaseColumn>();
            List<DatabaseColumn> ListDropDatabaseColumn = new List<DatabaseColumn>();
            List<KeyValuePair<DatabaseColumn, DatabaseColumn>> ListPairAlterDatabaseColumn = new List<KeyValuePair<DatabaseColumn, DatabaseColumn>>();

            schemaMetaDataDto.ListNewDatabaseColumn = ListNewDatabaseColumn;
            schemaMetaDataDto.ListDropDatabaseColumn = ListDropDatabaseColumn;
            schemaMetaDataDto.ListPairAlterDatabaseColumn = ListPairAlterDatabaseColumn;

            foreach (var transactionFieldDto in transactionUnitDto.AppTransactionFieldList)
            {

                string dbColumnName = transactionFieldDto.DataBaseFieldName;
                if (string.IsNullOrWhiteSpace(dbColumnName))
                {
                    DatabaseColumn newColumn = ConvertTransactionFieldDtoToDatabaseColumn(transactionFieldDto);
                    ListNewDatabaseColumn.Add(newColumn);

                    transactionFieldDto.DataBaseFieldName = newColumn.Name;


                }
                else // 
                {
                    DatabaseColumn newDatabaseColumn = ConvertTransactionFieldDtoToDatabaseColumn(transactionFieldDto);
                    //!!!!1 need to verride with dbColumnName
                    newDatabaseColumn.Name = dbColumnName;

                    if (dictTableColumn.ContainsKey(dbColumnName))
                    {
                        DatabaseColumn orgDataColumn = dictTableColumn[dbColumnName];
                        if ((newDatabaseColumn.Tag != orgDataColumn.Tag)
                             || (newDatabaseColumn.IsPrimaryKey != orgDataColumn.IsPrimaryKey)
                             || (newDatabaseColumn.Precision != orgDataColumn.Precision)
                          )
                        {

                            KeyValuePair<DatabaseColumn, DatabaseColumn> pair = new KeyValuePair<DatabaseColumn, DatabaseColumn>(newDatabaseColumn, orgDataColumn);
                            ListPairAlterDatabaseColumn.Add(pair);
                        }



                    }
                    else// Database not include  ?? the column was droped by DBA
                    {

                        ListNewDatabaseColumn.Add(newDatabaseColumn);

                    }


                }



            }

            // Remove column

            List<string> removeColumnList = dictTableColumn.Keys.Except(dictTransFiledTableColumn.Keys).ToList();

            foreach (string removeColumn in removeColumnList)
            {
                DatabaseColumn aDatabaseColumn = dictTableColumn[removeColumn];
                ListDropDatabaseColumn.Add(aDatabaseColumn);


            }


            try
            {
                //Save 
                AppMetaDataBL.SaveModifiedTableSchema(schemaMetaDataDto, transactionUnitDto.DataSourceFrom);

                //Set 
                foreach (string removeColumn in removeColumnList)
                {
                    DatabaseColumn aDatabaseColumn = dictTableColumn[removeColumn];
                    SetWhereUsedTransactionFileDbColumnAsEmpty(transactionUnitDto, aDatabaseColumn);

                }

                UpdateTransactionUnitDatabaeName(transactionUnitDto);
            }
            catch (Exception ex)
            {

            }


            // need to update Transcation setting



        }

        public static Dictionary<int, string> RetrieveListEditTransactionsBySchemaOwnerTableName(string tableName, string schemaOwner, int? dataSourceFrom)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                string queryDatasourceTransId =
                    @"  SELECT    distinct AppTransaction.TransactionID AS DataSourceTransactionID, AppTransaction.TransactionName
                        FROM    [AppTransactionUnit] INNER JOIN AppTransaction ON AppTransactionUnit.TransactionID = AppTransaction.TransactionID
							
                        WHERE   (AppTransactionUnit.ParentTransactionUnitID is null) AND (AppTransaction.TransactionOrganizedType = 3)
                                and  AppTransactionUnit.DataBaseTableName = @DataBaseTableName";

                List<SqlParameter> listPars = new List<SqlParameter>();
                listPars.Add(new SqlParameter("@DataBaseTableName", tableName));

                if (!string.IsNullOrWhiteSpace(schemaOwner))
                {
                    queryDatasourceTransId += @" AND AppTransactionUnit.SchemaOwner = @SchemaOwner";
                    listPars.Add(new SqlParameter("@SchemaOwner", schemaOwner));
                }

                if (dataSourceFrom.HasValue)
                {
                    queryDatasourceTransId += @" AND AppTransaction.DataSourceFrom = @DataSourceFrom";
                    listPars.Add(new SqlParameter("@DataSourceFrom", dataSourceFrom.Value));
                }

                return adapter.ExecuteDataTableRetrievalQuery(queryDatasourceTransId, listPars).AsEnumerable().ToDictionary(o => (int)o["DataSourceTransactionID"], o => o["TransactionName"] as string);

            }

        }

        public static OperationCallResult<bool> ImportSaasApplicationTransactions(SaasApplicationSectionItemImportDto importDto)
        {
            OperationCallResult<bool> operationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            if (importDto != null && importDto.ApplicationId.HasValue && importDto.SelectedItemIdList != null)
            {
                ObservableSet<AppApplicationAssetsItemExDto> needToSaveSet = new ObservableSet<AppApplicationAssetsItemExDto>();

                List<AppApplicationAssetsItemExDto> org_list = AppSaasUserApplicationPackageBL.RetrieveAppApplicationAssetsItemDtoListByType(importDto.ApplicationId.Value, (int)EmAppApplicationAssetsType.Transaction);

                foreach (int transactionId in importDto.SelectedItemIdList)
                {
                    var existingAssetItem = org_list.FirstOrDefault(o => o.TransactionId.HasValue && o.TransactionId.Value == transactionId);

                    if (existingAssetItem != null)
                    {
                        needToSaveSet.Add(existingAssetItem);
                    }
                    else
                    {
                        AppApplicationAssetsItemExDto newItem = new AppApplicationAssetsItemExDto();
                        newItem.ApplicationId = importDto.ApplicationId.Value;
                        newItem.TransactionId = transactionId;
                        needToSaveSet.Add(newItem);
                    }
                }

                validationResult.Merge(AppSaasUserApplicationPackageBL.SaveAppApplicationAssetsItemDtoList(needToSaveSet, importDto.ApplicationId.Value, (int)EmAppApplicationAssetsType.Transaction).ValidationResult);

                if (!validationResult.HasErrors)
                {
                    operationCallResult.Object = true;
                }
            }

            return operationCallResult;
        }

        private static void SetWhereUsedTransactionFileDbColumnAsEmpty(AppTransactionUnitExDto transactionUnitDto, DatabaseColumn aDatabaseColumn)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "SetTransactionFiledDBAsEmpty");

                    string updateWhereUsedFiled = @"Update      dbo.AppTransactionField set DataBaseFieldName='' from AppTransactionField INNER JOIN
                    dbo.AppTransactionUnit ON dbo.AppTransactionField.TransactionUnitID = dbo.AppTransactionUnit.TransactionUnitID					 
					 where dbo.AppTransactionUnit.DataBaseTableName=@tableName and AppTransactionField.DataBaseFieldName=@columnName";

                    var listParas = new List<System.Data.SqlClient.SqlParameter>();
                    listParas.Add(new SqlParameter("@tableName", transactionUnitDto.DataBaseTableName));
                    listParas.Add(new SqlParameter("@columnName", aDatabaseColumn.Name));
                    adapter.ExecuteScalarQuery(updateWhereUsedFiled, listParas);
                    adapter.Commit();

                }
                catch
                {
                    adapter.Rollback();

                }
            }
        }

        private static DatabaseColumn ConvertTransactionFieldDtoToDatabaseColumn(AppTransactionFieldExDto transactionField)
        {
            DatabaseColumn databaseColumn = new DatabaseColumn();

            string dbColumnName = ConvertDisplayStringToDBString(transactionField.DisplayName);
            transactionField.DataBaseFieldName = dbColumnName;
            databaseColumn.Name = transactionField.DataBaseFieldName;




            databaseColumn.DefaultValue = transactionField.DefaultValue;

            int? dataType = transactionField.DataType;
            if (dataType.HasValue)
            {
                databaseColumn.Tag = ((EmAppDataType)dataType.Value).ToString();

                if (dataType.Value == (int)EmAppDataType.Decimal)
                {
                    //decimal is same as the numerric 
                    databaseColumn.Precision = 18;
                    databaseColumn.Scale = transactionField.Nbdecimal;
                    // column.p

                }

                if (dataType.Value == (int)EmAppDataType.String)
                {
                    if (transactionField.MaxCharLegnth.HasValue)
                    {
                        databaseColumn.Length = transactionField.MaxCharLegnth.Value;
                    }
                    else
                    {
                        databaseColumn.Length = 4000;
                    }

                }



            }
            if (transactionField.IsPrimaryKey)
            {

                databaseColumn.IsPrimaryKey = transactionField.IsPrimaryKey;
                databaseColumn.Nullable = false;
            }
            else
            {
                databaseColumn.Nullable = true;
            }
            return databaseColumn;
        }

        private static void UpdateTransactionUnitDatabaeName(AppTransactionUnitExDto transactionUnitDto)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "UpdateAppTransactionUnitEntity");

                    AppTransactionUnitEntity aEntity = new AppTransactionUnitEntity();
                    aEntity.DataBaseTableName = transactionUnitDto.DataBaseTableName;
                    adapter.UpdateEntitiesDirectly(aEntity, new RelationPredicateBucket(AppTransactionUnitFields.TransactionUnitId == transactionUnitDto.Id));

                    foreach (var transactionFieldDto in transactionUnitDto.AppTransactionFieldList)
                    {

                        AppTransactionFieldEntity aFieldEntity = new AppTransactionFieldEntity();
                        aFieldEntity.DataBaseFieldName = transactionFieldDto.DataBaseFieldName;
                        adapter.UpdateEntitiesDirectly(aFieldEntity, new RelationPredicateBucket(AppTransactionFieldFields.TransactionFieldId == transactionFieldDto.Id));


                    }

                    adapter.Commit();

                }
                catch
                {
                    adapter.Rollback();

                }
            }
        }


        private static string ConvertDisplayStringToDBString(string dbTablename)
        {
            //a-zA-Z0-9_
            //  string filterString = Regex.Replace(dbTablename, "[^a-zA-Z]", "");

            string filterString = Regex.Replace(dbTablename, "[^a-zA-Z0-9_]", "");


            if (filterString.Length > 50)
            {

                filterString = filterString.Substring(0, 50);

            }
            return filterString;
        }

        public static string FilterSQLDBInvalidChar(string str)
        {
            // return Regex.IsMatch(str, @"^[a-zA-Z]+$");


            return Regex.Replace(str, "[^a-zA-Z]+", "");


        }

        public static OperationCallResult<AppTransactionExDto> CreateDefaultListTransactionFromTableName(string tableName, int? dataSourceRegisterId, string schemaOner, int? saasApplicationId)
        {
            OperationCallResult<AppTransactionExDto> aOperationCallResult = new OperationCallResult<AppTransactionExDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (string.IsNullOrEmpty(tableName))
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_TableNameEmpty_Error", ValidationItemType.Error, "Table Name Is Empty."));
            }

            if (dataSourceRegisterId.HasValue)
            {
                if (string.IsNullOrWhiteSpace(schemaOner))
                {
                    schemaOner = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value).CurrentOwner;
                }
            }


            List<KeyValuePair<string, string>> listOnerTable = new List<KeyValuePair<string, string>>();
            KeyValuePair<string, string> pair = new KeyValuePair<string, string>(schemaOner, tableName);

            listOnerTable.Add(pair);


            Dictionary<string, DatabaseTable> dictDBTables = AppMetaDataBL.GetDatabaseTableSchemaDictionaryBySchemaOwnerTableNames(listOnerTable, dataSourceRegisterId);

            string key = AppMetaDataBL.GetOwnerTableKey(schemaOner, tableName);

            if (dictDBTables.ContainsKey(key))
            {
                var databaseTble = dictDBTables[key];

                AppTransactionExDto aHierarchyAppTransactionExDto = new AppTransactionExDto();
                aHierarchyAppTransactionExDto.TransactionOrganizedType = (int)EmTransactionOrganizedType.List;
                aHierarchyAppTransactionExDto.TransactionName = "EntityInfo_" + key;
                aHierarchyAppTransactionExDto.Description = "EntityInfo Data Edit";
                aHierarchyAppTransactionExDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();
                aHierarchyAppTransactionExDto.DataSourceFrom = dataSourceRegisterId;
                aHierarchyAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap = new Dictionary<Guid, Guid>();
                aHierarchyAppTransactionExDto.SaasApplicationId = saasApplicationId;
                aHierarchyAppTransactionExDto.IsShowSaveButton = true;
                aHierarchyAppTransactionExDto.IsPhysicalModelTableCreated = true;

                AppTransactionUnitExDto rootUnitDto = CreateTransactionUnitFromDatabaseTable(databaseTble);

                foreach (var aTableColumn in databaseTble.Columns)
                {
                    if (aTableColumn.Name.ToLower() != "AppCreatedByID".ToLower()
                        && aTableColumn.Name.ToLower() != "AppCreatedDate".ToLower()
                        && aTableColumn.Name.ToLower() != "AppModifiedDate".ToLower()
                        && aTableColumn.Name.ToLower() != "AppModifiedByID".ToLower()
                        && aTableColumn.Name.ToLower() != "AppCreatedByCompanyID".ToLower())
                    {
                        AppTransactionFieldExDto aTransactionField = new AppTransactionFieldExDto();
                        aTransactionField.SortOrder = (rootUnitDto.AppTransactionFieldList.Count + 1) * 10;
                        aTransactionField.RowIdentityGuid = Guid.NewGuid();

                        aTransactionField.DataBaseFieldName = aTableColumn.Name;
                        aTransactionField.DisplayName = ConvertDbNameToDisplayName(aTableColumn.Name);

                        aTransactionField.ControlType = (int)EmAppControlType.TextBox;

                        int? dataType = ControlTypeValueConverter.ConvertValueToInt(aTableColumn.Tag);
                        if (dataType.HasValue)
                        {
                            aTransactionField.DataType = dataType.Value;
                        }

                        aTransactionField.DisplayWidth = "100";
                        aTransactionField.Nbdecimal = 0;
                        aTransactionField.IsPrimaryKey = aTableColumn.IsPrimaryKey; ;
                        aTransactionField.IsVisible = true;
                        aTransactionField.IsReadonly = false;
                        aTransactionField.IsGroupBy = false;
                        aTransactionField.IsGridUseAvailableEntitySource = false;
                        aTransactionField.IsNeedLog = false;
                        aTransactionField.IsAllowEmpty = true;
                        aTransactionField.IsConvertToUpperCase = false;
                        aTransactionField.IsLinkToParentPrimaryKey = false;
                        aTransactionField.DataRetrieveType = (int)EmAppCascadingSourceType.RelationalTable;

                        rootUnitDto.AppTransactionFieldList.Add(aTransactionField);
                    }
                }

                aHierarchyAppTransactionExDto.AppTransactionUnitList.Add(rootUnitDto);

                return SaveAppTransactionExDto(aHierarchyAppTransactionExDto);
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_CannotFindTable_Error", ValidationItemType.Error, "Cannot Find Table."));
            }

            return aOperationCallResult;
        }

        /// <summary>
        /// Creates a fully configured hierarchy transaction (master / child / grandchild)
        /// from a list of related database table names supplied in <paramref name="setupDto"/>.
        /// FK relationships are detected from the table schema to wire up parent-child links.
        /// </summary>
        /// <summary>
        /// Creates a hierarchy transaction (root → children → grandchildren) from the
        /// tree structure described in <paramref name="setupDto"/>.
        ///
        /// Tree shape:
        ///   RootUnit            (1 master table)
        ///     ChildUnit[]       (multiple child tables, all direct children of root)
        ///       GrandChildUnit[] (multiple grandchild tables per child)
        ///
        /// FK relationships between tables are auto-detected from the database schema.
        /// </summary>
        public static OperationCallResult<AppTransactionExDto> CreateHierarchyTransactionFromTables(HierarchyTableSetupDto setupDto)
        {
            return CreateHierarchyTransactionFromTables(setupDto, isIgnoreValidation: false, skipPostSaveCacheSync: false);
        }

        /// <summary>Same as overload without flags; migration may skip validation and post-save cache reload.</summary>
        public static OperationCallResult<AppTransactionExDto> CreateHierarchyTransactionFromTables(
            HierarchyTableSetupDto setupDto,
            bool isIgnoreValidation,
            bool skipPostSaveCacheSync = false)
        {
            OperationCallResult<AppTransactionExDto> result = new OperationCallResult<AppTransactionExDto>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            if (setupDto == null || string.IsNullOrWhiteSpace(setupDto.MasterTableName))
            {
                validation.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_TableNameEmpty_Error", ValidationItemType.Error, "Master table name is required."));
                return result;
            }

            if (string.IsNullOrWhiteSpace(setupDto.SchemaOwner) && setupDto.DataSourceRegisterId.HasValue)
            {
                setupDto.SchemaOwner = AppCacheManagerBL.GetOneDatabaseFixture(setupDto.DataSourceRegisterId.Value).CurrentOwner;
            }

            List<string> allTableNames = new List<string> { setupDto.MasterTableName };
            if (setupDto.SiblingTableNames != null)
            {
                allTableNames.AddRange(setupDto.SiblingTableNames.Where(n => !string.IsNullOrWhiteSpace(n)));
            }
            if (setupDto.ChildTables != null)
            {
                foreach (var child in setupDto.ChildTables)
                {
                    if (!string.IsNullOrWhiteSpace(child.TableName))
                        allTableNames.Add(child.TableName);

                    if (child.GrandChildTableNames != null)
                        allTableNames.AddRange(child.GrandChildTableNames.Where(n => !string.IsNullOrWhiteSpace(n)));
                }
            }

            List<KeyValuePair<string, string>> ownerTablePairs = allTableNames
                .Select(n => new KeyValuePair<string, string>(setupDto.SchemaOwner, n))
                .ToList();

            Dictionary<string, DatabaseTable> dictDBTables =
                AppMetaDataBL.GetDatabaseTableSchemaDictionaryBySchemaOwnerTableNames(ownerTablePairs, setupDto.DataSourceRegisterId);

            string masterKey = AppMetaDataBL.GetOwnerTableKey(setupDto.SchemaOwner, setupDto.MasterTableName);
            if (!dictDBTables.ContainsKey(masterKey))
            {
                validation.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_CannotFindTable_Error", ValidationItemType.Error, "Cannot find master table: " + setupDto.MasterTableName));
                return result;
            }

            AppTransactionExDto transactionDto = new AppTransactionExDto();
            transactionDto.TransactionOrganizedType = (int)EmTransactionOrganizedType.MasterDetail;
            transactionDto.TransactionName = !string.IsNullOrWhiteSpace(setupDto.TransactionName)
                ? setupDto.TransactionName
                : ConvertDbNameToDisplayName(setupDto.MasterTableName);
            transactionDto.Description = transactionDto.TransactionName + " Data Edit";
            transactionDto.DataSourceFrom = setupDto.DataSourceRegisterId;
            transactionDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();
            transactionDto.DictCurrentPKOrFKLinkToParentKeyGuidMap = new Dictionary<Guid, Guid>();
            transactionDto.SaasApplicationId = setupDto.SaasApplicationId;
            transactionDto.IsShowSaveButton = true;
            transactionDto.IsPhysicalModelTableCreated = true;

            AppTransactionUnitExDto rootUnit = BuildHierarchyUnitWithFields(dictDBTables[masterKey]);
            transactionDto.AppTransactionUnitList.Add(rootUnit);

            Guid? rootPkGuid = rootUnit.AppTransactionFieldList
                .FirstOrDefault(f => f.IsPrimaryKey == true && f.RowIdentityGuid.HasValue)
                ?.RowIdentityGuid;

            if (setupDto.SiblingTableNames != null)
            {
                foreach (string siblingTableName in setupDto.SiblingTableNames
                             .Where(n => !string.IsNullOrWhiteSpace(n))
                             .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    string siblingKey = AppMetaDataBL.GetOwnerTableKey(setupDto.SchemaOwner, siblingTableName);
                    if (!dictDBTables.ContainsKey(siblingKey))
                        continue;

                    AppTransactionUnitExDto siblingUnit = BuildHierarchyUnitWithFields(dictDBTables[siblingKey]);
                    siblingUnit.IsMasterSiblingUnit = true;
                    MarkFkField(siblingUnit, dictDBTables[siblingKey], setupDto.MasterTableName,
                                rootPkGuid, transactionDto.DictCurrentPKOrFKLinkToParentKeyGuidMap);
                    transactionDto.AppTransactionUnitList.Add(siblingUnit);
                }
            }

            if (setupDto.ChildTables != null)
            {
                foreach (var childTableDef in setupDto.ChildTables)
                {
                    if (string.IsNullOrWhiteSpace(childTableDef.TableName))
                        continue;

                    string childKey = AppMetaDataBL.GetOwnerTableKey(setupDto.SchemaOwner, childTableDef.TableName);
                    if (!dictDBTables.ContainsKey(childKey))
                        continue;

                    AppTransactionUnitExDto childUnit = BuildHierarchyUnitWithFields(dictDBTables[childKey]);
                    MarkFkField(childUnit, dictDBTables[childKey], setupDto.MasterTableName,
                                rootPkGuid, transactionDto.DictCurrentPKOrFKLinkToParentKeyGuidMap);
                    rootUnit.Children.Add(childUnit);

                    Guid? childPkGuid = childUnit.AppTransactionFieldList
                        .FirstOrDefault(f => f.IsPrimaryKey == true && f.RowIdentityGuid.HasValue)
                        ?.RowIdentityGuid;

                    if (childTableDef.GrandChildTableNames != null)
                    {
                        foreach (string grandChildTableName in childTableDef.GrandChildTableNames)
                        {
                            if (string.IsNullOrWhiteSpace(grandChildTableName))
                                continue;

                            string grandChildKey = AppMetaDataBL.GetOwnerTableKey(setupDto.SchemaOwner, grandChildTableName);
                            if (!dictDBTables.ContainsKey(grandChildKey))
                                continue;

                            AppTransactionUnitExDto grandChildUnit = BuildHierarchyUnitWithFields(dictDBTables[grandChildKey]);
                            MarkFkField(grandChildUnit, dictDBTables[grandChildKey], childTableDef.TableName,
                                        childPkGuid, transactionDto.DictCurrentPKOrFKLinkToParentKeyGuidMap);
                            childUnit.Children.Add(grandChildUnit);
                        }
                    }
                }
            }

            return SaveAppTransactionExDto(transactionDto, isIgnoreValidation, skipPostSaveCacheSync);
        }

        /// <summary>Builds an AppTransactionUnitExDto with fields populated from the given DatabaseTable.</summary>
        private static AppTransactionUnitExDto BuildHierarchyUnitWithFields(DatabaseTable dbTable)
        {
            AppTransactionUnitExDto unit = CreateTransactionUnitFromDatabaseTable(dbTable);
            unit.AppTransactionFieldList = new ObservableSet<AppTransactionFieldExDto>();

            foreach (var col in dbTable.Columns)
            {
                if (col.Name.Equals("AppCreatedByID", StringComparison.OrdinalIgnoreCase)
                    || col.Name.Equals("AppCreatedDate", StringComparison.OrdinalIgnoreCase)
                    || col.Name.Equals("AppModifiedDate", StringComparison.OrdinalIgnoreCase)
                    || col.Name.Equals("AppModifiedByID", StringComparison.OrdinalIgnoreCase)
                    || col.Name.Equals("AppCreatedByCompanyID", StringComparison.OrdinalIgnoreCase))
                    continue;

                AppTransactionFieldExDto field = new AppTransactionFieldExDto();
                field.SortOrder = (unit.AppTransactionFieldList.Count + 1) * 10;
                field.RowIdentityGuid = Guid.NewGuid();
                field.DataBaseFieldName = col.Name;
                field.DisplayName = ConvertDbNameToDisplayName(col.Name);
                field.ControlType = (int)EmAppControlType.TextBox;

                int? dataType = ControlTypeValueConverter.ConvertValueToInt(col.Tag);
                if (dataType.HasValue) field.DataType = dataType.Value;

                field.DisplayWidth = "100";
                field.Nbdecimal = 0;
                field.IsPrimaryKey = col.IsPrimaryKey;
                field.IsVisible = !col.IsPrimaryKey;
                field.IsReadonly = col.IsPrimaryKey;
                field.IsGroupBy = false;
                field.IsGridUseAvailableEntitySource = false;
                field.IsNeedLog = false;
                field.IsAllowEmpty = true;
                field.IsConvertToUpperCase = false;
                field.IsLinkToParentPrimaryKey = false;
                field.DataRetrieveType = (int)EmAppCascadingSourceType.RelationalTable;

                unit.AppTransactionFieldList.Add(field);
            }

            return unit;
        }

        /// <summary>
        /// Finds the FK column in <paramref name="unit"/> that references <paramref name="parentTableName"/>
        /// and marks it as IsLinkToParentPrimaryKey = true (hidden, readonly).
        /// Also sets ParentPKFieldGuid (required by field validator) and registers the mapping
        /// in <paramref name="dictPkFkMap"/> so SaveAppTransactionExDto can resolve LinkToParentPrimaryKeyFieldId.
        /// </summary>
        private static void MarkFkField(
            AppTransactionUnitExDto         unit,
            DatabaseTable                   dbTable,
            string                          parentTableName,
            Guid?                           parentPkGuid,
            Dictionary<Guid, Guid>          dictPkFkMap)
        {
            if (dbTable.ForeignKeys == null) return;

            foreach (var fk in dbTable.ForeignKeys)
            {
                if (!string.IsNullOrEmpty(fk.RefersToTable) &&
                    fk.RefersToTable.Equals(parentTableName, StringComparison.OrdinalIgnoreCase) &&
                    fk.Columns != null && fk.Columns.Count > 0)
                {
                    var fkField = unit.AppTransactionFieldList
                        .FirstOrDefault(f => f.DataBaseFieldName != null &&
                            f.DataBaseFieldName.Equals(fk.Columns[0], StringComparison.OrdinalIgnoreCase));

                    if (fkField != null)
                    {
                        fkField.IsLinkToParentPrimaryKey = true;
                        fkField.IsReadonly  = true;
                        fkField.IsVisible   = false;

                        // ParentPKFieldGuid is required by the field validator when IsLinkToParentPrimaryKey=true
                        if (parentPkGuid.HasValue)
                        {
                            fkField.ParentPKFieldGuid = parentPkGuid;

                            // Register in the transaction-level map so the save step can convert the GUID
                            // reference into the persisted LinkToParentPrimaryKeyFieldId integer FK
                            if (fkField.RowIdentityGuid.HasValue && dictPkFkMap != null)
                                dictPkFkMap[fkField.RowIdentityGuid.Value] = parentPkGuid.Value;
                        }
                    }
                    break;
                }
            }
        }

        public static AppTransactionUnitExDto CreateTransactionUnitFromDatabaseTable(DatabaseTable tableData)
        {
            AppTransactionUnitExDto rootUnitDto = new AppTransactionUnitExDto();
            rootUnitDto.DataBaseTableName = tableData.Name;
            rootUnitDto.UnitDisplayName = ConvertDbNameToDisplayName(tableData.Name);

            rootUnitDto.AppTransactionFieldList = new ObservableSet<AppTransactionFieldExDto>();
            rootUnitDto.SchemaOwner = tableData.SchemaOwner;
            //foreach ( var column in tableData.PrimaryKey )

            rootUnitDto.IsPrimaryKeyIdentityInsert = tableData.IsPrimaryKeyAutoNumber;//tableData.FirstPrimaryKeyColumn != null && tableData.FirstPrimaryKeyColumn.IsAutoNumber;

            return rootUnitDto;
        }


        public static string ConvertDbNameToDisplayName(string dbName)
        {
            string toReturn = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Regex.Replace(dbName.Replace("_", " "), "([a-z])([A-Z])", "$1 $2").Trim());

            //if (toReturn.Length > 2)
            //{
            //    if (toReturn.ToLower().EndsWith("id"))
            //    {
            //        toReturn = toReturn.Substring(0, toReturn.Length - 2).Trim();
            //    }
            //}

            return toReturn;
        }




        public static List<AppTransactionDto> GetCurrentUserAvailableTransactions(bool? isSystemBuitIn, int? transactionType, bool includeReadonlyTransactions)
        {
            List<AppTransactionDto> allAvailableTransactions = RetrieveAllAppTransactionDto(isSystemBuitIn);

            if (transactionType.HasValue)
            {
                allAvailableTransactions = allAvailableTransactions.Where(o => o.TransactionOrganizedType.HasValue && o.TransactionOrganizedType.Value == transactionType.Value).ToList();
            }

            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            if (isAdmin)
            {
                return allAvailableTransactions;
            }
            else
            {
                List<int> orgAvailableTransactionIds = new List<int>();
                List<int> currentUserInvisibleTransactionIds = new List<int>();
                List<int> currentUserReadonlyTransactionIds = new List<int>();
                List<AppTransactionDto> toReturn = new List<AppTransactionDto>();

                int? currentUserOrganizationId = AppSecurityUserBL.CurrentUserEntity.OrganizationId;

                if (currentUserOrganizationId.HasValue)
                {
                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        EntityCollection<AppSecuritySysObjGroupUserEntity> availableOrgSysObjlist = new EntityCollection<AppSecuritySysObjGroupUserEntity>();
                        RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.TransactionId != System.DBNull.Value);
                        filter.PredicateExpression.AddWithAnd(AppSecuritySysObjGroupUserFields.OrganizationId == currentUserOrganizationId.Value);
                        adapter.FetchEntityCollection(availableOrgSysObjlist, filter, 0, null); ;
                        orgAvailableTransactionIds = availableOrgSysObjlist.Select(o => o.TransactionId.Value).ToList();
                    }

                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        EntityCollection<AppSecuritySysObjGroupUserEntity> restrictUserRoleObjList = new EntityCollection<AppSecuritySysObjGroupUserEntity>();
                        RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.TransactionId != System.DBNull.Value);
                        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserId == AppSecurityUserBL.CurrentUserEntity.UserId
                            | AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds);
                        adapter.FetchEntityCollection(restrictUserRoleObjList, filter, 0, null);

                        if (restrictUserRoleObjList.Count > 0)
                        {
                            currentUserInvisibleTransactionIds = restrictUserRoleObjList.Where(o => o.IsInVisible.HasValue && o.IsInVisible.Value).Select(o => o.TransactionId.Value).ToList();
                            currentUserReadonlyTransactionIds = restrictUserRoleObjList.Where(o => o.IsUnSaveAble.HasValue && o.IsUnSaveAble.Value).Select(o => o.TransactionId.Value).ToList();
                        }
                    }

                    foreach (var aTransaction in allAvailableTransactions)
                    {
                        int transactionId = (int)aTransaction.Id;

                        if (includeReadonlyTransactions)
                        {
                            if (orgAvailableTransactionIds.Contains(transactionId)
                                && !currentUserInvisibleTransactionIds.Contains(transactionId))
                            {
                                toReturn.Add(aTransaction);
                            }
                        }
                        else
                        {
                            if (orgAvailableTransactionIds.Contains(transactionId)
                                && !currentUserInvisibleTransactionIds.Contains(transactionId)
                                && !currentUserReadonlyTransactionIds.Contains(transactionId))
                            {
                                toReturn.Add(aTransaction);
                            }
                        }

                    }
                }

                return toReturn;
            }
        }

        public static OperationCallResult<bool> GenerateConsumeApiDataModelSaveSetting(int? getDataTransactionId, int? saveDataOperationId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionExDto srcTransactionDto = GetHierarchyTranscationFromDatabase(getDataTransactionId);

            var resultSaveTransfer1 = GenerateDataModelToApiDataTransferSetting(saveDataOperationId, srcTransactionDto);

            if (resultSaveTransfer1.IsSuccessfulWithResult)
            {
                AppTransactionDataTransferSettingExDto dataTransferSettingDto = resultSaveTransfer1.Object;

                srcTransactionDto.OtherOptions.SaveByApiCallDataTransferId = (int)dataTransferSettingDto.Id;
                srcTransactionDto.OtherOptions.NeedToRefreshAfterSaveByApiCall = false;
                srcTransactionDto.OtherOptions.NeedToSaveFormAfterPublishByApiCall = false;

                if (!srcTransactionDto.OtherOptions.IsApiIntegrationTransaction)
                {
                    srcTransactionDto.OtherOptions.NeedToSaveFormAfterPublishByApiCall = true;
                }

                var resultSave3 = SaveAppTransactionExDto(srcTransactionDto);

                aValidationResult.Merge(resultSave3.ValidationResult);

                if (resultSave3.IsSuccessfulWithResult)
                {
                    aOperationCallResult.Object = true;
                }


            }
            else
            {
                aValidationResult.Merge(resultSaveTransfer1.ValidationResult);
            }




            return aOperationCallResult;
        }


        public static OperationCallResult<bool> GenerateConsumeApiDataModelDeleteSetting(int? getDataTransactionId, int? deleteDataOperationId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;



            AppTransactionExDto srcTransactionDto = GetHierarchyTranscationFromDatabase(getDataTransactionId);

            var resultSaveTransfer1 = GenerateDataModelToApiDataTransferSetting(deleteDataOperationId.Value, srcTransactionDto);

            if (resultSaveTransfer1.IsSuccessfulWithResult)
            {
                AppTransactionDataTransferSettingExDto dataTransferSettingDto = resultSaveTransfer1.Object;


                srcTransactionDto.OtherOptions.DeleteDataTransferId = (int)dataTransferSettingDto.Id;

                var resultSave3 = SaveAppTransactionExDto(srcTransactionDto);

                aValidationResult.Merge(resultSave3.ValidationResult);

                if (resultSave3.IsSuccessfulWithResult)
                {
                    aOperationCallResult.Object = true;
                }
            }
            else
            {
                aValidationResult.Merge(resultSaveTransfer1.ValidationResult);
            }


            return aOperationCallResult;
        }

        public static OperationCallResult<AppProjectWorkFlowActionExDto> GenerateCommandCallApiOperationSetting(AppProjectWorkFlowActionExDto commandDto, int transactionId, int apiOperationId)
        {
            OperationCallResult<AppProjectWorkFlowActionExDto> aOperationCallResult = new OperationCallResult<AppProjectWorkFlowActionExDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            var apiDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(apiOperationId);
            AppTransactionExDto srcTransactionDto = GetHierarchyTranscationFromDatabase(transactionId);

            var resultSaveTransfer1 = GenerateDataModelToApiDataTransferSetting(apiOperationId, srcTransactionDto);

            if (resultSaveTransfer1.IsSuccessfulWithResult)
            {
                AppTransactionDataTransferSettingExDto dataTransferDto = resultSaveTransfer1.Object;

                commandDto.DataTransferSettingId = (int)dataTransferDto.Id;


                aOperationCallResult.Object = commandDto;
            }
            else
            {
                aValidationResult.Merge(resultSaveTransfer1.ValidationResult);
            }


            return aOperationCallResult;
        }




        private static void AutoMapDataTransfer(AppTransactionDataTransferSettingExDto dataTransferSettingDto, AppTransactionExDto srcTransactionDto, AppTransactionExDto targetTransactionDto)
        {
            if (dataTransferSettingDto.TransferTypeId == (int)EmAppTransactionDataTransferType.FromDataModelToDataModel
               || dataTransferSettingDto.TransferTypeId == (int)EmAppTransactionDataTransferType.FromDataModelToApi)
            {

                Dictionary<int, string> dictSrcTransFieldIdAndDisplay = new Dictionary<int, string>();
                Dictionary<string, int> dicSrcTransFieldDisplayAndId = new Dictionary<string, int>();

                Dictionary<int, string> dictTargetTransFieldIdAndDisplay = new Dictionary<int, string>();
                Dictionary<string, int> dictTargetTransFieldDisplayAndId = new Dictionary<string, int>();

                foreach (var unitDto in srcTransactionDto.DictAllTransactionUnitIdExDto.Values)
                {
                    foreach (var fieldDto in unitDto.AppTransactionFieldList)
                    {
                        dictSrcTransFieldIdAndDisplay.Add((int)fieldDto.Id, (unitDto.UnitDisplayName + " : " + fieldDto.DataBaseFieldName).ToLower());

                        if (!dicSrcTransFieldDisplayAndId.ContainsKey((unitDto.UnitDisplayName + " : " + fieldDto.DataBaseFieldName).ToLower()))
                            dicSrcTransFieldDisplayAndId.Add((unitDto.UnitDisplayName + " : " + fieldDto.DataBaseFieldName).ToLower(), (int)fieldDto.Id);
                    }
                }

                foreach (var unitDto in targetTransactionDto.DictAllTransactionUnitIdExDto.Values)
                {
                    foreach (var fieldDto in unitDto.AppTransactionFieldList)
                    {

                        dictTargetTransFieldIdAndDisplay.Add((int)fieldDto.Id, (unitDto.UnitDisplayName + " : " + fieldDto.DataBaseFieldName).ToLower());

                        if (!dictTargetTransFieldDisplayAndId.ContainsKey((unitDto.UnitDisplayName + " : " + fieldDto.DataBaseFieldName).ToLower()))
                            dictTargetTransFieldDisplayAndId.Add((unitDto.UnitDisplayName + " : " + fieldDto.DataBaseFieldName).ToLower(), (int)fieldDto.Id);
                    }
                }



                foreach (var aMapping in dataTransferSettingDto.AppTransactionSaveAsMappingList)
                {
                    if (aMapping.SourceFiledId.HasValue && !aMapping.TargetFiledId.HasValue)
                    {
                        if (dictSrcTransFieldIdAndDisplay.ContainsKey(aMapping.SourceFiledId.Value))
                        {
                            string srcFieldDisplay = dictSrcTransFieldIdAndDisplay[aMapping.SourceFiledId.Value];

                            if (!string.IsNullOrWhiteSpace(srcFieldDisplay) && dictTargetTransFieldDisplayAndId.ContainsKey(srcFieldDisplay))
                            {
                                aMapping.TargetFiledId = dictTargetTransFieldDisplayAndId[srcFieldDisplay];
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(aMapping.Name))
                    {
                        if (targetTransactionDto.ApiInputParameterList != null)
                        {
                            string matchTargetParmeString = targetTransactionDto.ApiInputParameterList.FirstOrDefault(o => o.ToLower() == aMapping.Name.ToLower());
                            if (!string.IsNullOrWhiteSpace(matchTargetParmeString))
                            {
                                aMapping.JsonPropertyPathName = matchTargetParmeString;
                            }
                        }
                    }
                }
            }
        }

        private static void SetupMappingToAvailableSourceUnitTransactionFieldExDto(AppTransactionExDto appTransactionExDto)
        {
            if (appTransactionExDto.DictAllTransactionUnitIdExDto.IsEmpty())
            {
                appTransactionExDto.DictAllTransactionUnitIdExDto = appTransactionExDto.AppTransactionUnitList.ToDictionary(o => o.Id.ToString(), o => o);
            }

            //MappingToAvailableSourceUnitTransactionFieldExDto
            foreach (var aAppTransactionFieldExDto in appTransactionExDto.DictAllTransactionField.Values)
            {
                if (aAppTransactionFieldExDto.MappingToAvailableSourceUnitTransactionFieldId.HasValue)
                {
                    int srcFieldId = aAppTransactionFieldExDto.MappingToAvailableSourceUnitTransactionFieldId.Value;
                    if (appTransactionExDto.DictAllTransactionField != null && appTransactionExDto.DictAllTransactionField.ContainsKey(srcFieldId))
                    {
                        aAppTransactionFieldExDto.MappingToAvailableSourceUnitTransactionFieldExDto = appTransactionExDto.DictAllTransactionField[srcFieldId];
                    }
                }
            }
        }

        private static void MapCurentKeyParentKeyGuid(AppTransactionExDto appTransactionExDto, ObservableSet<AppTransactionUnitExDto> allTransactionUnits)
        {
            Dictionary<int, Guid> dictIdRowGuid = new Dictionary<int, Guid>();

            foreach (var unitDtoEx in allTransactionUnits)
            {
                foreach (var fieldDto in unitDtoEx.AppTransactionFieldList)
                {
                    dictIdRowGuid[(int)fieldDto.Id] = fieldDto.RowIdentityGuid.Value;
                }

            }

            foreach (var unitDtoEx in allTransactionUnits)
            {
                foreach (var fieldDto in unitDtoEx.AppTransactionFieldList)
                {
                    if (fieldDto.LinkToParentPrimaryKeyFieldId.HasValue)
                    {
                        Guid curentUid = fieldDto.RowIdentityGuid.Value;
                        Guid parentGuid = dictIdRowGuid[fieldDto.LinkToParentPrimaryKeyFieldId.Value];

                        appTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap[curentUid] = parentGuid;
                    }
                }

            }
        }


        private static void PrepareTransFieldCrossRelationSettingDictionary(AppTransactionExDto aAppTransactionExDto,
            Dictionary<int, AppTransactionFieldExDto> dictTransFieldIdAndDto, Dictionary<int, AppTransactionFieldAggFunctionExDto> dictAggFuncIdAndDto)
        {
            Dictionary<int, AppTransactionFieldCrossRelationSettingDto> dictTransFieldIdAndCrossRelationSettingDto = new Dictionary<int, AppTransactionFieldCrossRelationSettingDto>();



            foreach (AppTransactionFieldExDto transFieldDto in dictTransFieldIdAndDto.Values.Where(o => o.ParentUnitSubscribeChildAggFunctionId.HasValue))
            {
                if (dictAggFuncIdAndDto != null && dictAggFuncIdAndDto.ContainsKey(transFieldDto.ParentUnitSubscribeChildAggFunctionId.Value))
                {
                    var aggFuncDto = dictAggFuncIdAndDto[transFieldDto.ParentUnitSubscribeChildAggFunctionId.Value];

                    if (aggFuncDto.TransactionFieldId.HasValue && dictTransFieldIdAndDto.ContainsKey(aggFuncDto.TransactionFieldId.Value))
                    {
                        var subScribeToTrasnFieldDto = dictTransFieldIdAndDto[aggFuncDto.TransactionFieldId.Value];

                        AppTransactionFieldCrossRelationSettingDto crossSettingDto = new AppTransactionFieldCrossRelationSettingDto();
                        crossSettingDto.SubscribeToUnitId = subScribeToTrasnFieldDto.TransactionUnitId;
                        crossSettingDto.SubscribeToTransFieldId = (int)subScribeToTrasnFieldDto.Id;
                        crossSettingDto.ParentUnitSubscribeChildAggFunctionId = transFieldDto.ParentUnitSubscribeChildAggFunctionId.Value;
                        crossSettingDto.AggregationType = aggFuncDto.AggregationFunctionType;
                        crossSettingDto.CurrentUnitId = transFieldDto.TransactionUnitId;

                        if (!dictTransFieldIdAndCrossRelationSettingDto.ContainsKey((int)transFieldDto.Id))
                        {
                            dictTransFieldIdAndCrossRelationSettingDto.Add((int)transFieldDto.Id, crossSettingDto);
                        }
                    }
                }
            }

            foreach (AppTransactionFieldExDto transFieldDto in dictTransFieldIdAndDto.Values.Where(o => o.ChildUnitSubscribeParentFieldId.HasValue))
            {
                if (dictTransFieldIdAndDto.ContainsKey(transFieldDto.ChildUnitSubscribeParentFieldId.Value))
                {
                    var subScribeToTrasnFieldDto = dictTransFieldIdAndDto[transFieldDto.ChildUnitSubscribeParentFieldId.Value];
                    AppTransactionFieldCrossRelationSettingDto crossSettingDto = new AppTransactionFieldCrossRelationSettingDto();
                    crossSettingDto.SubscribeToUnitId = subScribeToTrasnFieldDto.TransactionUnitId;
                    crossSettingDto.SubscribeToTransFieldId = (int)subScribeToTrasnFieldDto.Id;
                    crossSettingDto.ChildUnitSubscribeParentFieldId = transFieldDto.ChildUnitSubscribeParentFieldId.Value;

                    if (!dictTransFieldIdAndCrossRelationSettingDto.ContainsKey((int)transFieldDto.Id))
                    {
                        dictTransFieldIdAndCrossRelationSettingDto.Add((int)transFieldDto.Id, crossSettingDto);
                    }
                }
            }

            aAppTransactionExDto.DictTransFieldIdAndCrossRelationSettingDto = dictTransFieldIdAndCrossRelationSettingDto;
        }

        private static void PrepareTransactionWebPageDesignBindingExpressions_RootLevelFields(AppTransactionUnitExDto unitExDto)
        {
            foreach (var transFieldExDto in unitExDto.AppTransactionFieldList)
            {
                transFieldExDto.Expression = new Dictionary<string, string>();

                string valueBinding = AppTransactionFieldExDto.GetRootorSiblingBindField(transFieldExDto);
                string lockField = AppTransactionFieldExDto.GetLockingBindField(transFieldExDto);
                string hideField = AppTransactionFieldExDto.GetNeedToHideBindField(transFieldExDto);
                string primaryKeyReadonly = AppTransactionFieldExDto.GetPrimaryKeyReadonlyBinding(transFieldExDto);
                string isFieldReadOnly = transFieldExDto.IsFormLayoutReadOnly.ToString().ToLower() + " || " + lockField + " || " + primaryKeyReadonly;
                string isRequiredExpression = AppTransactionFieldExDto.GetTransfieldIsRequiredSymbol(transFieldExDto);

                int? ratingMaxValue = ControlTypeValueConverter.ConvertValueToInt(transFieldExDto.MaxNumber);

                if (!(ratingMaxValue.HasValue && ratingMaxValue.Value > 1))
                {
                    ratingMaxValue = 5;
                }


                string maxlengthExpression = "";
                if (transFieldExDto.MaxCharLegnth.HasValue && transFieldExDto.MaxCharLegnth.Value > 0)
                {
                    maxlengthExpression = @"maxlength=""" + transFieldExDto.MaxCharLegnth.ToString() + @"""";
                }
                string isSibling = transFieldExDto.IsSiblingField ? "true" : "false";

                string fieldItemSource = AppTransactionFieldExDto.GetFieldItemSource(transFieldExDto);

                string toolTip = transFieldExDto.DisplayName;

                if (!string.IsNullOrWhiteSpace(transFieldExDto.ToolTip))
                {
                    toolTip = StringLocalizer.Localize(transFieldExDto.ToolTip.Trim(), transFieldExDto.ToolTip.Trim());
                }
                string filenameDisplayBinding = AppTransactionFieldExDto.GetFileDisplayBinding(transFieldExDto);

                string uiValidationFunction = AppTransactionFieldExDto.GetFieldUiValidationFunction(transFieldExDto);
                string errorMessageBinding = AppTransactionFieldExDto.GetFieldErrorMessageBinding(transFieldExDto);
                string ddlControlBinding = "controllerModel.dictDdlControl['" + transFieldExDto.Id.ToString() + "']";

                transFieldExDto.Expression[EmAppTransFieldExpressionType.FieldId.ToString()] = transFieldExDto.Id.ToString();
                transFieldExDto.Expression[EmAppTransFieldExpressionType.UnitId.ToString()] = transFieldExDto.TransactionUnitId.ToString();
                transFieldExDto.Expression[EmAppTransFieldExpressionType.DisplayName.ToString()] = transFieldExDto.DisplayName;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.DBColumnName.ToString()] = transFieldExDto.DataBaseFieldName;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.Value.ToString()] = valueBinding;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.FilenameDisplay.ToString()] = filenameDisplayBinding;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.GoogleMapLocationFieldDBColumnName.ToString()] = transFieldExDto.ControlTypeParam2;

                transFieldExDto.Expression[EmAppTransFieldExpressionType.RatingMaxValue.ToString()] = ratingMaxValue.Value.ToString();


                transFieldExDto.Expression[EmAppTransFieldExpressionType.Tooltip.ToString()] = toolTip;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.IsReadOnly.ToString()] = isFieldReadOnly;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.MaxLength.ToString()] = maxlengthExpression;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.IsRequired.ToString()] = isRequiredExpression;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.IsSiblingUnit.ToString()] = isSibling;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.ItemSource.ToString()] = fieldItemSource;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.ErrorMessage.ToString()] = errorMessageBinding;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.UiValidationFunction.ToString()] = uiValidationFunction;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.DdlControlBinding.ToString()] = ddlControlBinding;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.FieldLevel.ToString()] = "1";
            }
        }


        private static void PrepareTransactionWebPageDesignBindingExpressions_ChildAndGrandChildUnit(AppTransactionUnitExDto childUnitExDto)
        {
            string srcFieldDbName = "";
            string subscribeUnitId = "";
            string subscribeFieldDbName = "";

            if (childUnitExDto.IsUsedForLoadingAvailableSource.HasValue && childUnitExDto.IsUsedForLoadingAvailableSource.Value)
            {
                if (childUnitExDto.ForeignAppTransactionExDto == null)
                {
                    var transactionExDto = childUnitExDto.ForeignAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(childUnitExDto.TransactionId);

                    var subscribeUnitDto = transactionExDto.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(o => o.AvailableSourceUnitId.HasValue && o.AvailableSourceUnitId.Value == (int)childUnitExDto.Id);
                    if (subscribeUnitDto != null)
                    {

                        var subscribeField = subscribeUnitDto.AppTransactionFieldList.FirstOrDefault(o => o.MappingToAvailableSourceUnitTransactionFieldExDto != null);
                        if (subscribeField != null)
                        {
                            subscribeUnitId = subscribeUnitDto.Id.ToString();
                            subscribeFieldDbName = subscribeField.DataBaseFieldName;
                            srcFieldDbName = subscribeField.MappingToAvailableSourceUnitTransactionFieldExDto.DataBaseFieldName;
                        }
                    }
                }
            }

            string isChildUnitExclusiveForOwner = (childUnitExDto.IsExclusiveForOwner.HasValue && childUnitExDto.IsExclusiveForOwner.Value) ? "true" : "false";

            string itemFormatter = "";
            string childItemsPath = "";
            string gridStyleClass = "";

            if (childUnitExDto.EmGridViewDisplayType.HasValue
            && childUnitExDto.EmGridViewDisplayType.Value == (int)EmAppTransactionGridDisplayType.TreeGrid)
            {
                childItemsPath = "TreeViewChildren";
                itemFormatter = "childTreeGridItemFormatter";
                gridStyleClass = "app-wj-treeview";
            }


            childUnitExDto.Expression = new Dictionary<string, string>();

            childUnitExDto.Expression[EmAppTransUnitExpressionType.UnitId.ToString()] = childUnitExDto.Id.ToString();
            childUnitExDto.Expression[EmAppTransUnitExpressionType.DataBaseTableName.ToString()] = childUnitExDto.DataBaseTableName.ToString();
            childUnitExDto.Expression[EmAppTransUnitExpressionType.DisplayName.ToString()] = childUnitExDto.UnitDisplayName.ToString();
            childUnitExDto.Expression[EmAppTransUnitExpressionType.SubscribeUnitId.ToString()] = subscribeUnitId;
            childUnitExDto.Expression[EmAppTransUnitExpressionType.SrcFieldDBName.ToString()] = subscribeFieldDbName;
            childUnitExDto.Expression[EmAppTransUnitExpressionType.SubscribeFieldDbName.ToString()] = srcFieldDbName;
            childUnitExDto.Expression[EmAppTransUnitExpressionType.IsChildUnitExclusiveForOwner.ToString()] = isChildUnitExclusiveForOwner;
            childUnitExDto.Expression[EmAppTransUnitExpressionType.ItemFormatter.ToString()] = itemFormatter;
            childUnitExDto.Expression[EmAppTransUnitExpressionType.ChildItemsPath.ToString()] = childItemsPath;
            childUnitExDto.Expression[EmAppTransUnitExpressionType.GridStyleClass.ToString()] = gridStyleClass;
            childUnitExDto.Expression[EmAppTransUnitExpressionType.ParentUnitId.ToString()] = childUnitExDto.ParentTransactionUnitId.Value.ToString();



            foreach (var transFieldExDto in childUnitExDto.AppTransactionFieldList)
            {
                transFieldExDto.Expression = new Dictionary<string, string>();

                string valueBinding = "dataModel.currentEditChildGridRow.DictOneToOneFields." + transFieldExDto.DataBaseFieldName;
                string lockField = AppTransactionFieldExDto.GetLockingBindField(transFieldExDto);

                string isFieldReadOnly = transFieldExDto.IsFormLayoutReadOnly.ToString().ToLower() + " || " + lockField;

                string isRequiredExpression = AppTransactionFieldExDto.GetTransfieldIsRequiredSymbol(transFieldExDto);

                int? ratingMaxValue = ControlTypeValueConverter.ConvertValueToInt(transFieldExDto.MaxNumber);

                if (!(ratingMaxValue.HasValue && ratingMaxValue.Value > 1))
                {
                    ratingMaxValue = 5;
                }

                string maxValue = string.Empty;

                if (transFieldExDto.DataType.HasValue && transFieldExDto.DataType.Value == (int)EmAppDataType.Integer)
                {
                    maxValue = int.MaxValue.ToString();
                }


                string maxlengthExpression = "";
                if (transFieldExDto.MaxCharLegnth.HasValue && transFieldExDto.MaxCharLegnth.Value > 0)
                {
                    maxlengthExpression = @"maxlength=""" + transFieldExDto.MaxCharLegnth.ToString() + @"""";
                }
                string isSibling = transFieldExDto.IsSiblingField ? "true" : "false";

                string ddlFiledCVBinding = "dataModel.dictCurrentEditChildRowCascadingFiledIdAndCV";

                string fieldItemSource = ddlFiledCVBinding + "['" + transFieldExDto.Id.ToString() + "'] || dataModel.dictFieldEntityDataMap['" + transFieldExDto.Id.ToString() + "'].collectionView";

                string ddlControlBinding = "controllerModel.dictCurrentEditChildGridRow_FieldIdAndDdlControl['" + transFieldExDto.Id.ToString() + "']";

                string toolTip = transFieldExDto.DisplayName;

                if (!string.IsNullOrWhiteSpace(transFieldExDto.ToolTip))
                {
                    toolTip = StringLocalizer.Localize(transFieldExDto.ToolTip.Trim(), transFieldExDto.ToolTip.Trim());
                }



                string filenameDisplayBinding = "dataModel.currentFormData.DictDocumentIdFileCode[" + valueBinding + "] || " + valueBinding;

                string uiValidationFunction = AppTransactionFieldExDto.GetFieldUiValidationFunction(transFieldExDto);
                string errorMessageBinding = AppTransactionFieldExDto.GetFieldErrorMessageBinding(transFieldExDto);

                transFieldExDto.Expression[EmAppTransFieldExpressionType.FieldId.ToString()] = transFieldExDto.Id.ToString();
                transFieldExDto.Expression[EmAppTransFieldExpressionType.UnitId.ToString()] = transFieldExDto.TransactionUnitId.ToString();
                transFieldExDto.Expression[EmAppTransFieldExpressionType.DisplayName.ToString()] = transFieldExDto.DisplayName;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.DBColumnName.ToString()] = transFieldExDto.DataBaseFieldName;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.Value.ToString()] = valueBinding;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.FilenameDisplay.ToString()] = filenameDisplayBinding;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.GoogleMapLocationFieldDBColumnName.ToString()] = transFieldExDto.ControlTypeParam2;

                transFieldExDto.Expression[EmAppTransFieldExpressionType.RatingMaxValue.ToString()] = ratingMaxValue.Value.ToString();
                transFieldExDto.Expression[EmAppTransFieldExpressionType.MaxValue.ToString()] = maxValue;

                transFieldExDto.Expression[EmAppTransFieldExpressionType.Tooltip.ToString()] = toolTip;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.IsReadOnly.ToString()] = isFieldReadOnly;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.MaxLength.ToString()] = maxlengthExpression;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.IsRequired.ToString()] = isRequiredExpression;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.IsSiblingUnit.ToString()] = isSibling;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.ItemSource.ToString()] = fieldItemSource;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.ErrorMessage.ToString()] = errorMessageBinding;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.UiValidationFunction.ToString()] = uiValidationFunction;
                transFieldExDto.Expression[EmAppTransFieldExpressionType.DdlControlBinding.ToString()] = ddlControlBinding;

                transFieldExDto.Expression[EmAppTransFieldExpressionType.FieldLevel.ToString()] = "2";
                transFieldExDto.Expression[EmAppTransFieldExpressionType.RowItemBinding.ToString()] = "dataModel.currentEditChildGridRow";


                if (transFieldExDto.MappingToAvailableSourceUnitTransactionFieldId.HasValue)
                {
                    childUnitExDto.Expression[EmAppTransUnitExpressionType.SubscribeFieldId.ToString()] = transFieldExDto.Id.ToString();
                    childUnitExDto.Expression[EmAppTransUnitExpressionType.SubscribeFieldDbName.ToString()] = transFieldExDto.DataBaseFieldName;
                    childUnitExDto.Expression[EmAppTransUnitExpressionType.SubscribeFieldDisplayName.ToString()] = transFieldExDto.DisplayName;
                }
            }

            if (childUnitExDto.AvailableSourceUnitId.HasValue)
            {
                var availableSourceUnitExDto = AppTransactionBL.RetrieveOneAppTransactionUnitExDto(childUnitExDto.AvailableSourceUnitId.Value);

                var mappingTransField = childUnitExDto.AppTransactionFieldList.FirstOrDefault(o => o.MappingToAvailableSourceUnitTransactionFieldExDto != null);

                if (mappingTransField != null)
                {
                    int srcUnitId = childUnitExDto.AvailableSourceUnitId.Value;
                    childUnitExDto.Expression[EmAppTransUnitExpressionType.SrcUnitId.ToString()] = srcUnitId.ToString();

                    AppTransactionFieldExDto srcTransField = mappingTransField.MappingToAvailableSourceUnitTransactionFieldExDto;

                    var srcDisplayField = availableSourceUnitExDto.AppTransactionFieldList.OrderBy(o => o.SortOrder).FirstOrDefault(o => o.IsVisible.HasValue && o.IsVisible.Value && o.ControlType == (int)EmAppControlType.TextBox);
                    if (srcDisplayField == null)
                    {
                        srcDisplayField = srcTransField;
                    }
                    childUnitExDto.Expression[EmAppTransUnitExpressionType.SrcDisplayFieldDbName.ToString()] = srcDisplayField.DataBaseFieldName;

                    childUnitExDto.Expression[EmAppTransUnitExpressionType.SrcFieldDBName.ToString()] = srcTransField.DataBaseFieldName;
                }


            }

        }

        private static AppTransactionUnitExDto ConvertOneApiNodeToUnit(ApiDataStructureNodeDto nodeDto, ApiDataStructureNodeDto parentNode, string unitPath = "", bool isSelectAllNode = false)
        {

            AppTransactionUnitExDto newUnit = new AppTransactionUnitExDto();
            newUnit.Children = new List<AppTransactionUnitExDto>();
            newUnit.AppTransactionFieldList = new ObservableSet<AppTransactionFieldExDto>();

            newUnit.IsPrimaryKeyIdentityInsert = false;
            newUnit.IsSynchToDatabaseTable = false;

            if (string.IsNullOrWhiteSpace(unitPath))
            {
                unitPath = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(nodeDto.Name);

                if (parentNode != null)
                {
                    string parentPath = parentNode.NodePath;
                    if (!string.IsNullOrWhiteSpace(parentPath))
                    {
                        unitPath = parentPath + "___" + nodeDto.Name;
                    }
                }
            }


            newUnit.UnitDisplayName = nodeDto.Name;
            newUnit.DataBaseTableName = unitPath;

            if (string.IsNullOrEmpty(newUnit.UnitDisplayName) && parentNode == null)
            {
                newUnit.UnitDisplayName = "Root Unit";
            }

            ProcessChildNodeByType(newUnit, nodeDto, null, isSelectAllNode);

            return newUnit;

        }

        private static void ProcessChildNodeByType(AppTransactionUnitExDto unit, ApiDataStructureNodeDto nodeDto, ApiDataStructureNodeDto parentNodeDto, bool isSelectAllNode = false)
        {
            if (nodeDto.IsArray && nodeDto.IsSimpleList)
            {
                unit.IsExclusiveForOwner = true;
                var childNode = new ApiDataStructureNodeDto();
                childNode.Name = nodeDto.Name;
                ProcessChildNodeByType_Field(unit, childNode, nodeDto);
            }
            else
            {
                foreach (var childNode in nodeDto.Children)
                {
                    if (childNode.IsObject)
                    {
                        if (childNode.IsSelected || isSelectAllNode)
                        {
                            ProcessChildNodeByType_Obj(unit, childNode, nodeDto);
                        }

                    }
                    else if (childNode.IsArray)
                    {
                        if (childNode.IsSelected || isSelectAllNode)
                        {
                            ProcessChildNodeByType_Array(unit, childNode, nodeDto, isSelectAllNode);
                        }
                    }
                    else
                    {
                        ProcessChildNodeByType_Field(unit, childNode, nodeDto);
                    }
                }
            }
        }

        private static void ProcessChildNodeByType_Obj(AppTransactionUnitExDto unit, ApiDataStructureNodeDto nodeDto, ApiDataStructureNodeDto parentNodeDto, bool isSelectAllNode = false)
        {
            if (parentNodeDto != null && parentNodeDto.NodePath.HasValue())
            {
                nodeDto.NodePath = parentNodeDto.NodePath + "___" + nodeDto.Name;
            }
            else
            {
                nodeDto.NodePath = nodeDto.Name;
            }


            ProcessChildNodeByType(unit, nodeDto, parentNodeDto, isSelectAllNode);

        }

        private static void ProcessChildNodeByType_Array(AppTransactionUnitExDto unit, ApiDataStructureNodeDto nodeDto, ApiDataStructureNodeDto parentNodeDto, bool isSelectAllNode = false)
        {
            AppTransactionUnitExDto newUnit = ConvertOneApiNodeToUnit(nodeDto, parentNodeDto, "", isSelectAllNode);
            unit.Children.Add(newUnit);
        }

        private static void ProcessChildNodeByType_Field(AppTransactionUnitExDto unit, ApiDataStructureNodeDto nodeDto, ApiDataStructureNodeDto parentNodeDto)
        {
            AppTransactionFieldExDto fieldDto = new AppTransactionFieldExDto();

            fieldDto.DataType = (int)EmAppDataType.String;
            fieldDto.ControlType = (int)EmAppControlType.TextBox;
            fieldDto.IsTempVariable = false;
            fieldDto.IsAllowEmpty = true;
            fieldDto.IsVisible = true;
            fieldDto.Nbdecimal = 0;
            fieldDto.RowIdentityGuid = Guid.NewGuid();


            if (parentNodeDto != null && parentNodeDto.NodePath.HasValue())
            {
                fieldDto.DataBaseFieldName = parentNodeDto.NodePath + "___" + nodeDto.Name;
            }
            else
            {
                fieldDto.DataBaseFieldName = nodeDto.Name;
            }

            fieldDto.DisplayName = nodeDto.Name;
            unit.AppTransactionFieldList.Add(fieldDto);
        }

        public static List<TransactionApiSettingDto> RetrieveTransactionApiSettings(int? transactionId)
        {
            List<TransactionApiSettingDto> toReturn = new List<TransactionApiSettingDto>();

            if (transactionId.HasValue)
            {


                var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId.Value);

                bool isArrayJsonData = transactionExDto.TransactionOrganizedType.HasValue && transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List;

                if (transactionExDto.OtherOptions != null && transactionExDto.OtherOptions.IsApiIntegrationTransaction)
                {
                    // 1. Consume: 
                    if (transactionExDto.BaseApiConfigDto != null)
                    {
                        TransactionApiSettingDto apiSettingDto = InitTransactionApiSettingDto(transactionExDto.BaseApiConfigDto);
                        apiSettingDto.ConsumeOrProvideType = "ConsumeApiOperation";
                        //apiSettingDto.CRUDType = "Read";

                        apiSettingDto.IsDataModelBaseApiOperation = true;
                        toReturn.Add(apiSettingDto);

                        //var all3rdPartApiProviders = AppIntergrationSettingBL.RetrieveAllAppIntergrationSettingDto(isArrayJsonData);

                        //foreach (var providerExDto in all3rdPartApiProviders)
                        //{
                        //    foreach (var operationExDto in providerExDto.AppIntergrationSettingParameterList)
                        //    {
                        //        if (operationExDto.TranscationId.HasValue && operationExDto.TranscationId.Value == transactionId.Value)
                        //        {
                        //            if ((int)operationExDto.Id != (int)transactionExDto.BaseApiConfigDto.Id)
                        //            {
                        //                TransactionApiSettingDto settingDto = InitTransactionApiSettingDto(operationExDto);
                        //                settingDto.ConsumeOrProvideType = "ConsumeApiOperation";
                        //                toReturn.Add(settingDto);
                        //            }
                        //        }

                        //    }
                        //}

                    }

                }



                // 3. Consume APIs: Publish Data To 3red Part API Throught Data Transfer
                List<AppTransactionDataTransferSettingExDto> dataTransferList = AppTransactionDataTransferSettingBL.RetrieveAllAppTransactionDataTransferSettingExDto(transactionId.Value, null).ToList();

                foreach (AppTransactionDataTransferSettingExDto dataTransferDto in dataTransferList)
                {
                    if (dataTransferDto.DestinationTransactionId.HasValue)
                    {
                        var distinationTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.DestinationTransactionId.Value);

                        if (distinationTransactionExDto.OtherOptions != null && distinationTransactionExDto.OtherOptions.IsApiIntegrationTransaction && distinationTransactionExDto.BaseApiConfigDto != null)
                        {
                            TransactionApiSettingDto apiSettingDto = InitTransactionApiSettingDto(distinationTransactionExDto.BaseApiConfigDto);

                            apiSettingDto.ConsumeOrProvideType = "ConsumeApiDataTransfer";
                            apiSettingDto.CRUDType = "Update";

                            apiSettingDto.DataTransferSettingId = (int)dataTransferDto.Id;

                            apiSettingDto.DataTransferSettingName = dataTransferDto.Description;

                            apiSettingDto.TargetTransactionId = (int)distinationTransactionExDto.Id;
                            apiSettingDto.TargetTransactionName = distinationTransactionExDto.TransactionName;

                            toReturn.Add(apiSettingDto);
                        }
                    }
                }


                // 4. Provide APIs: 

                AppIntergrationSettingExDto publicApiBuilderExDto = AppIntergrationSettingBL.TryRetrieveAppBuiltInProviderExDto();
                if (publicApiBuilderExDto?.AppIntergrationSettingParameterList != null)
                {
                    foreach (var publicApiDto in publicApiBuilderExDto.AppIntergrationSettingParameterList)
                    {
                        if (publicApiDto.TranscationId.HasValue && publicApiDto.TranscationId.Value == transactionId.Value)
                        {
                            TransactionApiSettingDto apiSettingDto = InitTransactionApiSettingDto(publicApiDto);
                            apiSettingDto.ConsumeOrProvideType = "ProvideApiOperation";
                            toReturn.Add(apiSettingDto);
                        }
                    }
                }

            }


            return toReturn;
        }

        //private static TransactionApiSettingDto InitTransactionApiSettingDto(AppTransactionDataTransferSettingExDto dataTransferDto)
        //{
        //    TransactionApiSettingDto apiSettingDto = new TransactionApiSettingDto();
        //    var apiDto = dataTransferDto.TargetApiOperationDto;
        //    apiSettingDto.OperationId = (int)apiDto.Id;
        //    apiSettingDto.ActionCode = apiDto.ActionCode;
        //    apiSettingDto.HttpMethd = apiDto.HttpMethd;

        //    apiSettingDto.CRUDType = apiDto.InternalFiledName;
        //    apiSettingDto.IsSimpleQuery = apiDto.IsSimpleQuery.HasValue && apiDto.IsSimpleQuery.Value;

        //    return apiSettingDto;
        //}

        internal static TransactionApiSettingDto InitTransactionApiSettingDto(AppIntergrationSettingParameterExDto apiDto)
        {
            TransactionApiSettingDto apiSettingDto = new TransactionApiSettingDto();

            apiSettingDto.OperationId = (int)apiDto.Id;
            apiSettingDto.ActionCode = apiDto.ActionCode;
            apiSettingDto.HttpMethd = apiDto.HttpMethd;

            apiSettingDto.CRUDType = apiDto.InternalFiledName;
            apiSettingDto.IsSimpleQuery = apiDto.IsSimpleQuery.HasValue && apiDto.IsSimpleQuery.Value;

            return apiSettingDto;
        }

        private static OperationCallResult<AppTransactionDataTransferSettingExDto> GenerateDataModelToApiDataTransferSetting(int? apiOperationId, AppTransactionExDto srcTransactionDto)
        {
            var ApiActionSetting = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterEntity(apiOperationId);

            AppTransactionDataTransferSettingExDto dataTransferSettingDto = new AppTransactionDataTransferSettingExDto();
            dataTransferSettingDto.Description = srcTransactionDto.TransactionName + "_Mapping_" + ApiActionSetting.ActionCode;
            dataTransferSettingDto.TransactionId = (int)srcTransactionDto.Id;
            dataTransferSettingDto.InternalCode = apiOperationId.Value.ToString();
            dataTransferSettingDto.TransferTypeId = (int)EmAppTransactionDataTransferType.FromDataModelToApi;
            dataTransferSettingDto.AppTransactionSaveAsMappingList = new ObservableSet<AppTransactionSaveAsMappingExDto>();

            if (srcTransactionDto.ApiInputParameterList != null)
            {
                foreach (string srcInputParamString in srcTransactionDto.ApiInputParameterList)
                {
                    AppTransactionSaveAsMappingExDto paramMappingItem = new AppTransactionSaveAsMappingExDto()
                    {
                        IsBlankTargetField = false,
                        TransactionId = (int)srcTransactionDto.Id,
                        Name = srcInputParamString,
                    };

                    dataTransferSettingDto.AppTransactionSaveAsMappingList.Add(paramMappingItem);
                }
            }


            var resultSaveTransfer = AppTransactionDataTransferSettingBL.SaveOneAppTransactionDataTransferSettingExDto(dataTransferSettingDto);

            return resultSaveTransfer;
        }

    }
}