using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.EntityClasses;
using AppAI.Web.Controllers.Base;
using AppWeb.Models;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class AppTransactionController : SecureBaseController
{
    public static readonly string SqlProvideName = "System.Data.SqlClient";


    [HttpGet]
    public AppTransactionUnitExDto RetrieveOneAppTransactionUnitExDto(int? unitId)
    {
        if (unitId.HasValue)
        {
            return AppTransactionBL.RetrieveOneAppTransactionUnitExDto(unitId.Value);
        }
        else
        {
            return null;
        }
    }


    [HttpGet]
    public List<AppTransactionDto> RetrieveAllAppTransactions(bool? isSystemBuitIn, int? transactionOrganizedType, bool? isIncludeWorkflow)
    {
        if (!isIncludeWorkflow.HasValue)
        {
            isIncludeWorkflow = false;
        }
        List<AppTransactionDto> toReturn = AppTransactionBL.RetrieveAllAppTransactionDto(isSystemBuitIn, transactionOrganizedType, isIncludeWorkflow.Value);

        return toReturn;
    }

    [HttpGet]
    public List<AppTransactionDto> GetCurrentUserAvailableTransactions(bool? isSystemBuitIn, int? transactionType, bool includeReadonlyTransactions)
    {
        List<AppTransactionDto> toReturn = AppTransactionBL.GetCurrentUserAvailableTransactions(isSystemBuitIn, transactionType, includeReadonlyTransactions);

        return toReturn;
    }


    [HttpGet]
    public AppTransactionExDto RetrieveOneAppTransaction(string Id)
    {
        AppTransactionExDto aAppTransactionExDto = null;
        if (Id == "NewTransaction")
        {
            aAppTransactionExDto = new AppTransactionExDto();
            aAppTransactionExDto.TransactionName = "New Transaction";
            aAppTransactionExDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();
            aAppTransactionExDto.EmAppTransBusinessType = (int)EmAppTransBusinessType.FormData;
        }
        else
        {
            aAppTransactionExDto = AppTransactionBL.RetrieveOneAppTransactionExDto(Id);
        }

        return aAppTransactionExDto;
    }

    [HttpGet]
    public AppTransactionExDto GetOneHierarchyTransaction(string Id, bool isNewTransaction = false, int? newTransactionType = null, int? newFolderTransactionUsageType = null, int? newTransactionDataSourceFrom = null, bool? isESitePageDesign = null, int? rootWorkflowTransactionId = null)
    {
        AppTransactionExDto aAppTransactionExDto = null;
        if (isNewTransaction)
        {
            aAppTransactionExDto = new AppTransactionExDto();
            aAppTransactionExDto.TransactionName = "New Transaction";
            aAppTransactionExDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();

            aAppTransactionExDto.TransactionOrganizedType = newTransactionType;
            aAppTransactionExDto.IsEnableFolderSecurity = true;
            aAppTransactionExDto.EmAppTransBusinessType = (int)EmAppTransBusinessType.FormData;

            if (newTransactionDataSourceFrom.HasValue)
            {
                aAppTransactionExDto.DataSourceFrom = newTransactionDataSourceFrom;
            }
            else
            {
                aAppTransactionExDto.DataSourceFrom = ServerContext.Instance.CurrnetClientIdentity.DataSourceId;
            }
        }
        else
        {
            aAppTransactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(int.Parse(Id), rootWorkflowTransactionId);

            if (isESitePageDesign.HasValue && isESitePageDesign.Value)
            {
                AppTransactionBL.PrepareTransactionWebPageDesignBindingExpressions(aAppTransactionExDto);
            }

            if (aAppTransactionExDto.BusinessScopeId.HasValue && aAppTransactionExDto.BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation)
            {
                AppTransactionCommandBL.SyncronizeWorkflowCommandNodeTreeFromActionList(aAppTransactionExDto);
            }
        }

        return aAppTransactionExDto;
    }


    [HttpGet]
    public AppTransactionExDto PrepareNewWorkflowAutomation(int? dataSourceFrom = null)
    {
        AppTransactionExDto aAppTransactionExDto = new AppTransactionExDto();
        aAppTransactionExDto.TransactionName = "New Workflow";
        aAppTransactionExDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();

        aAppTransactionExDto.TransactionOrganizedType = (int)EmTransactionOrganizedType.MasterDetail;
        aAppTransactionExDto.IsEnableFolderSecurity = true;
        aAppTransactionExDto.EmAppTransBusinessType = (int)EmAppTransBusinessType.FormData;
        aAppTransactionExDto.BusinessScopeId = (int)EmAppTransactionScopeUsage.WorkflowAutomation;

        if (dataSourceFrom.HasValue)
        {
            aAppTransactionExDto.DataSourceFrom = dataSourceFrom;
        }
        else
        {
            aAppTransactionExDto.DataSourceFrom = ServerContext.Instance.CurrnetClientIdentity.DataSourceId;
        }

        TableToUnitConverterDto converterDto = new TableToUnitConverterDto();
        converterDto.TableName = "AppWorkflowAutomation";
        converterDto.SchemaOwner = "dbo";
        converterDto.DataSourceRegisterId = aAppTransactionExDto.DataSourceFrom;

        AppTransactionUnitExDto rootUnit = ConvertDbSchemaOwnerTableNameToTransactionUnitExDto(converterDto);
        rootUnit.IsReadOnly = false;
        rootUnit.IsMasterSiblingUnit = false;
        rootUnit.IsMatrixUnit = false;
        rootUnit.IsMatrixPivotUnit = false;
        rootUnit.IsSynchToDatabaseTable = false;
        rootUnit.IsPrimaryKeyIdentityInsert = true;
        rootUnit.IsExclusiveForOwner = false;
        rootUnit.IsDisableAddButton = false;
        rootUnit.IsDisableDeleteButton = false;

        foreach (var fieldDto in rootUnit.AppTransactionFieldList)
        {
            fieldDto.IsLogicalDisplay = false;
            fieldDto.IsTempVariable = false;

            if (fieldDto.DataBaseFieldName == "WorkflowTransactionId")
            {
                fieldDto.IsReadonly = true;
                fieldDto.MappingEmSystemTokenField = (int)EmBLFiledMappingSystemTokenField.TransactionID;

                fieldDto.ControlType = (int)EmAppControlType.DDL;
                var EntityInfo = AppEntityInfoBL.RetrieveOneAppEntityInfoEntityWithCode("AppTransaction");
                fieldDto.EntityId = EntityInfo.EntityInfoId;
                fieldDto.DisplayName = "Workflow Setting Name";
            }
            else if (fieldDto.DataBaseFieldName == "WorkflowAutomationId")
            {
                fieldDto.IsReadonly = true;
            }
            else if (fieldDto.DataBaseFieldName == "Name")
            {
                fieldDto.IsLogicalDisplay = true;
            }
            else if (fieldDto.DataBaseFieldName == "Description")
            {
            }
            else if (fieldDto.DataBaseFieldName == "Notes")
            {
                fieldDto.IsReadonly = true;
            }
            else if (fieldDto.DataBaseFieldName == "WorkflowProgressStatus")
            {
                fieldDto.DisplayName = "Progress Status";
                fieldDto.IsReadonly = true;
            }
            else if (fieldDto.DataBaseFieldName == "OtherSetting")
            {
                fieldDto.IsVisible = false;
                fieldDto.IsReadonly = true;
            }
            else if (fieldDto.DataBaseFieldName == "StartTime")
            {
                fieldDto.ControlType = (int)EmAppControlType.DateTimeDetail;
                fieldDto.IsReadonly = true;
            }
            else if (fieldDto.DataBaseFieldName == "EndTime")
            {
                fieldDto.ControlType = (int)EmAppControlType.DateTimeDetail;
                fieldDto.IsReadonly = true;
            }
        }

        aAppTransactionExDto.AppTransactionUnitList.Add(rootUnit);

        return aAppTransactionExDto;
    }


    [HttpPost]
    public OperationCallResult<AppTransactionExDto> SaveAppTransaction(dynamic jsonObject)
    {
        AppTransactionExDto aAppTransactionExDto = JsonConvert.DeserializeObject<AppTransactionExDto>(jsonObject.ToString());

        if (aAppTransactionExDto != null)
        {
            if (aAppTransactionExDto.DictDeletedItemsIds.ContainsKey("AppTransactionUnitList"))
            {
                aAppTransactionExDto.AppTransactionUnitList.DeletedItemIds = aAppTransactionExDto.DictDeletedItemsIds["AppTransactionUnitList"];
            }


            foreach (var aUnit in aAppTransactionExDto.AppTransactionUnitList)
            {
                if (aUnit.Id != null)
                {
                    if (aAppTransactionExDto.DictDeletedItemsIds.ContainsKey("AppTransactionFieldList_" + aUnit.Id.ToString()))
                    {
                        aUnit.AppTransactionFieldList.DeletedItemIds = aAppTransactionExDto.DictDeletedItemsIds["AppTransactionFieldList_" + aUnit.Id.ToString()];
                    }

                    foreach (var aChildUnit in aUnit.Children)
                    {
                        if (aChildUnit.Id != null)
                        {
                            if (aAppTransactionExDto.DictDeletedItemsIds.ContainsKey("AppTransactionFieldList_" + aChildUnit.Id.ToString()))
                            {
                                aChildUnit.AppTransactionFieldList.DeletedItemIds = aAppTransactionExDto.DictDeletedItemsIds["AppTransactionFieldList_" + aChildUnit.Id.ToString()];
                            }

                            foreach (var aGrandchildUnit in aChildUnit.Children)
                            {
                                if (aGrandchildUnit.Id != null)
                                {
                                    if (aAppTransactionExDto.DictDeletedItemsIds.ContainsKey("AppTransactionFieldList_" + aGrandchildUnit.Id.ToString()))
                                    {
                                        aGrandchildUnit.AppTransactionFieldList.DeletedItemIds = aAppTransactionExDto.DictDeletedItemsIds["AppTransactionFieldList_" + aGrandchildUnit.Id.ToString()];
                                    }
                                }
                            }
                        }
                    }
                }
            }


            OperationCallResult<AppTransactionExDto> aOperationCallResult = AppTransactionBL.SaveAppTransactionExDto(aAppTransactionExDto);
            return aOperationCallResult;
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppTransactionExDto> GenerateUnitsFromSelectedApiNodes(AppTransactionExDto appTransactionExDto)
    {
        if (appTransactionExDto != null)
        {
            return AppTransactionBL.GenerateUnitsFromSelectedApiNodes(appTransactionExDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppTransactionExDto> SynchronizeTransactionUnitFieldsWithApiNodes(AppTransactionExDto appTransactionExDto)
    {
        if (appTransactionExDto != null)
        {
            return AppTransactionBL.SynchronizeTransactionUnitFieldsWithApiNodes(appTransactionExDto);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<AppTransactionExDto> SaveAsAppTransaction(int? orgTranscactionId)
    {
        return AppTransactionSaveAsBL.SaveAsAppTransaction(orgTranscactionId);
    }


    [HttpGet]
    public OperationCallResult<object> DeleteOneAppTransaction(int Id)
    {
        return AppTransactionBL.DeleteOneAppTransaction(Id);
    }


    [HttpGet]
    public List<AppFormLinkTargetDto> RetrieveOneTransactionUnitLinkTargetList(string transactionUnitId)
    {
        return LinkTragetBL.RetrieveOneAppFormLinkTargetList((int)EmAppLinkTargetSourceType.TransactionUnit, transactionUnitId).ToList();
    }

    [HttpPost]
    public OperationCallResult<AppFormLinkTargetDto> SaveOneTransactionUnitLinkTargetList(AppFormLinkTargetSetDto appFormLinkTargetSetDto)
    {
        if (appFormLinkTargetSetDto != null && appFormLinkTargetSetDto.TransactionUnitId.HasValue)
        {
            appFormLinkTargetSetDto.AppFormLinkTargetDtoSet.DeletedItemIds = appFormLinkTargetSetDto.DeletedItemIds;

            OperationCallResult<AppFormLinkTargetDto> aOperationCallResult = LinkTragetBL.SaveOneAppFormLinkTargetList(
                (int)EmAppLinkTargetSourceType.TransactionUnit, appFormLinkTargetSetDto.TransactionUnitId.Value, appFormLinkTargetSetDto.AppFormLinkTargetDtoSet);

            return aOperationCallResult;
        }

        return null;
    }

    [HttpGet]
    public List<EntityMappingToListEditTransactionDto> GetTransactionDataSourceList(int transactionId)
    {
        return LinkTragetBL.GetTransactionDataSourceList(transactionId);
    }


    [HttpGet]
    public List<AppTransactionUnitLinkedSearchExDto> RetrieveOneAppTransactionUnitLinkedSearchList(string transactionUnitId)
    {
        return AppTransactionUnitLinkedSearchBL.RetrieveOneAppTransactionUnitLinkedSearchList(transactionUnitId).ToList();
    }

    [HttpGet]
    public AppTransactionUnitLinkedSearchExDto RetrieveOneAppTransactionUnitLinkedSearchExDto(string transactionUnitLinkedSearchId)
    {
        return AppTransactionUnitLinkedSearchBL.RetrieveOneAppTransactionUnitLinkedSearchExDto(transactionUnitLinkedSearchId);
    }


    [HttpPost]
    public OperationCallResult<AppTransactionUnitLinkedSearchExDto> SaveAppTransactionUnitLinkedSearchExDto(AppTransactionUnitLinkedSearchExDto aAppTransactionUnitLinkedSearchExDto)
    {
        return AppTransactionUnitLinkedSearchBL.SaveAppTransactionUnitLinkedSearchExDto(aAppTransactionUnitLinkedSearchExDto);
    }

    [HttpGet]
    public OperationCallResult<object> DeleteAppTransactionUnitLinkedSearch(string transactionUnitLinkedSearchId)
    {
        return AppTransactionUnitLinkedSearchBL.DeleteAppTransactionUnitLinkedSearch(transactionUnitLinkedSearchId);
    }


    [HttpGet]
    public AppTransactionFieldAggFunctionSetDto RetrieveAppTransactionFieldAggFunctionSetDto(int fieldId)
    {
        return AppTransactionFormulaSetupBL.RetrieveAppTransactionFieldAggFunctionSetDto(fieldId);
    }


    [HttpPost]
    public AppTransactionFieldAggFunctionSetDto SaveAppTransactionFieldAggFunctionSetDto(AppTransactionFieldAggFunctionSetDto aAppTransactionFieldAggFunctionSetDto)
    {
        return AppTransactionFormulaSetupBL.SaveAppTransactionFieldAggFunctionSetDto(aAppTransactionFieldAggFunctionSetDto);
    }


    [HttpGet]
    public AppTransactionUnitFormulaSetDto RetrieveAppTransactionUnitFormulaSetDto(int unitId, int transactionId)
    {
        return AppTransactionFormulaSetupBL.RetrieveAppTransactionUnitFormulaSetDto(unitId, transactionId);
    }


    [HttpGet]
    public List<AppTransactionUnitFormulaSetDto> RetrieveAppTransactionUnitFormulaSetDtoList(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppTransactionFormulaSetupBL.RetrieveAppTransactionUnitFormulaSetDtoList(transactionId);
        }

        return null;
    }


    [HttpGet]
    public AppTransactionUnitFormulaSetDto RetrieveAppSearchViewFormulaSetDto(int? searchViewId)
    {
        if (searchViewId.HasValue)
        {
            return AppTransactionFormulaSetupBL.RetrieveAppSearchViewFormulaSetDto(searchViewId.Value);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppTransactionUnitFormulaSetDto> SaveAppTransactionUnitFormulaSetDto(AppTransactionUnitFormulaSetDto aAppTransactionUnitFormulaSetDto)
    {
        return AppTransactionFormulaSetupBL.SaveAppTransactionUnitFormulaSetDto(aAppTransactionUnitFormulaSetDto);
    }


    [HttpPost]
    public OperationCallResult<AppTransactionUnitFormulaSetDto> SaveAppTransactionFormulas(List<AppTransactionUnitFormulaSetDto> appTransactionUnitFormulaSetDtoList)
    {
        return AppTransactionFormulaSetupBL.SaveAppTransactionUnitFormulaSetDtoList(appTransactionUnitFormulaSetDtoList);
    }


    [HttpPost]
    public OperationCallResult<AppTransactionUnitFormulaSetDto> SaveAppSearchViewFormulaSetDto(AppTransactionUnitFormulaSetDto aAppTransactionUnitFormulaSetDto)
    {
        return AppTransactionFormulaSetupBL.SaveAppSearchViewFormulaSetDto(aAppTransactionUnitFormulaSetDto);
    }


    [HttpPost]
    public ValidationResult ValidateOneFormulaDto(AppTransactionUnitFormulaDto formulaDto)
    {
        if (formulaDto != null)
        {
            return formulaDto.ValidateDto("");
        }

        return null;
    }


    [HttpGet]
    public AppFormExDto RetrieveOneAppForm(int? Id)
    {
        if (Id.HasValue)
        {
            return AppFormBL.RetrieveOneAppFormExDto(Id);
        }
        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> DeleteOneAppFormExDto(int? formId)
    {
        if (formId.HasValue)
        {
            return AppFormBL.DeleteOneAppFormExDto(formId.Value);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> ResetFormLayout(int? formId, int? resetToLayoutType, bool needToGenerateDefaultLayout)
    {
        if (formId.HasValue)
        {
            return AppFormBL.ResetFormLayout(formId.Value, resetToLayoutType, needToGenerateDefaultLayout);
        }

        return null;
    }


    [HttpGet]
    public AppFormExDto CreateFormFromTransaction(int transactionId, int? formLayoutType, int? saasApplicationId, bool isPrintForm)
    {
        return AppFormBL.CreateNewTranactionForm(transactionId, formLayoutType, saasApplicationId, isPrintForm);
    }


    [HttpGet]
    public AppFormExDto CreateTransactionFormWithDefaultLayout(int? transactionId, int? nbColumns)
    {
        if (transactionId.HasValue && nbColumns.HasValue)
        {
            return AppFormBL.CreateTransactionFormWithDefaultLayout(transactionId.Value, nbColumns.Value);
        }
        return null;
    }


    [HttpPost]
    public AppFormExDto SaveAppForm(object jsonObject)
    {
        var aAppFormExDto = JsonConvert.DeserializeObject<AppFormExDto>(jsonObject.ToString());

        if (aAppFormExDto != null)
        {
            if (aAppFormExDto.LayoutType.HasValue && aAppFormExDto.LayoutType.Value == (int)EmAppFormLayoutType.Grid)//grid
            {
            }
            else
            {
                if (aAppFormExDto.DictDeletedItemsIds.ContainsKey("AppFormLayoutItemList"))
                {
                    aAppFormExDto.AppFormLayoutItemList.DeletedItemIds = aAppFormExDto.DictDeletedItemsIds["AppFormLayoutItemList"];
                }
            }

            OperationCallResult<AppFormExDto> aOperationCallResult = AppFormBL.SaveAppFormExDto(aAppFormExDto);
            if (aOperationCallResult != null && aOperationCallResult.Object != null)
            {
                return aOperationCallResult.Object;
            }
        }

        return null;
    }

    [HttpGet]
    public List<AppFormDto> RetrieveAllAppFormDtoByCreateFromType(EmAppFormCreationFrom? creationFrom)
    {
        return AppFormBL.RetrieveAllAppFormDtoByCreateFromType(creationFrom);
    }


    [HttpGet]
    public AppFormExDto GetNewFlexFormExDto()
    {
        AppFormExDto formDto = new AppFormExDto();
        formDto.LayoutType = (int)EmAppFormLayoutType.Flex;
        formDto.FormScope = (int)EmAppFormCreationFrom.FromScratch;
        formDto.Name = "New Flex Form";

        formDto.AppFormLayoutItemList = new ObservableSet<AppFormLayoutItemExDto>();
        return formDto;
    }

    [HttpGet]
    public AppFormExDto RetrieveOneAppFormFlexLayoutExDto(int? formId)
    {
        if (formId.HasValue)
        {
            var formTemplate = AppFormFlexLayoutBL.RetrieveOneAppFormFlexLayoutExDto(formId.Value);
            return formTemplate;
        }

        return null;
    }

    [HttpGet]
    public AppFormExDto BuildAppFormDefaultLayout(int? formId)
    {
        if (formId.HasValue)
        {
            var formTemplate = AppFormFlexLayoutBL.BuildAppFormDefaultLayout(formId.Value);
            return formTemplate;
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppFormExDto> SaveAppFormFlexLayoutExDto(AppFormExDto aAppFormExDto)
    {
        if (aAppFormExDto != null)
        {
            aAppFormExDto.FormPublishSettingDto = null;
            OperationCallResult<AppFormExDto> aOperationCallResult = AppFormFlexLayoutBL.SaveAppFormFlexLayoutExDto(aAppFormExDto);
            return aOperationCallResult;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppFormExDto> PublishAppFormToTransactionAndCreateTables(AppFormExDto appFormExDto)
    {
        if (appFormExDto != null)
        {
            return AppFormFlexLayoutBL.PublishAppFormToTransactionAndCreateTables(appFormExDto);
        }

        return null;
    }


    [HttpPost]
    public AppFormExDto CreateForm(AppFormExDto aAppFormExDto)
    {
        OperationCallResult<AppFormExDto> aOperationCallResult = AppFormBL.SaveAppFormExDto(aAppFormExDto);

        var appFormExDto = aOperationCallResult.Object;
        if (appFormExDto != null)
        {
            return appFormExDto;
        }

        return null;
    }


    [HttpGet]
    public AppTransactionStructureDto GetFormStructure(int? transactionId, int? transGroupId)
    {
        return GetFormStructureMethod(transactionId);
    }


    [HttpPost]
    //rootPrimaryKeyValue:
    //  if get data from external api, use prefix api__, and use | to split pk parameters. Example:api__product_id:1|version:2.
    //  if get data from database, only set the single pk value. Example: 1
    public AppMasterDetailDto GetFormData(GetFormDataDto data)
    {
        // data = { transactionId, rootPrimaryKeyValue, transGroupId, autoExecuteCommandId, selectDataRow };

        return GetFormDataMethod(data.transactionId, data.rootPrimaryKeyValue, data.autoExecuteCommandId, data.selectDataRow);
    }


    [HttpGet]
    public Dictionary<string, object> GetModelData(string entityName, string Id)
    {
        return null;
    }

    [HttpGet]
    public Dictionary<string, object> GetDataModelData(int? transactionId, string rootPrimaryKeyValue)
    {
        if (transactionId.HasValue)
        {
            AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId.Value, rootPrimaryKeyValue);

            DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(aAppformDataDto);

            Dictionary<string, object> returnDict = AppMasterDetailFormDataLoadBL.GetTransUnitNameDataModelData(aAppformDataDto);

            return returnDict;
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<Dictionary<string, object>> SaveDataModelData(Dictionary<string, object> dictDataModule)
    {
        Dictionary<string, object> convertdictDataModule = dictDataModule;

        AppMasterDetailFormDataLoadBL.SaveDataModelData(dictDataModule);

        OperationCallResult<Dictionary<string, object>> toReturn = new OperationCallResult<Dictionary<string, object>>();

        return toReturn;
    }

    [HttpPost]
    public OperationCallResult<AppMasterDetailDto> SaveTransactionData(AppMasterDetailDto appformDataDto)
    {
        if (appformDataDto != null)
        {
            DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(appformDataDto);

            var calculationResult = AppTransactionFormulaBL.ValidateAndCalculateMasterDetailTransactionData(appformDataDto);

            if (!calculationResult.IsSuccessfulWithResult)
            {
                return calculationResult;
            }
            else
            {
                if (appformDataDto != calculationResult.Object)
                {
                    appformDataDto = calculationResult.Object;
                }

                appformDataDto = DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(appformDataDto);

                var saveResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(appformDataDto);

                if (saveResult.Object != null)
                {
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(saveResult.Object);
                }

                if (calculationResult.ValidationResult.Items.FirstOrDefault(o => o.ItemType == ValidationItemType.Warning) != null)
                {
                    saveResult.ValidationResult.Merge(calculationResult.ValidationResult);

                    if (saveResult.Object != null && calculationResult.Object != null)
                    {
                        saveResult.Object.DictTransFieldIdAndWarningHighlightStyleId = calculationResult.Object.DictTransFieldIdAndWarningHighlightStyleId;
                    }
                }

                return saveResult;
            }
        }

        return null;
    }


    [HttpGet]
    public AppListDataDto GetListEditFormData(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            AppListDataDto aAppformDataDto = AppListEditFormDataLoadBL.GetListEditFormData(transactionId.Value);

            DataModelDateTimeConverterBL.ConvertListEditFromUtcToClient(aAppformDataDto);

            return aAppformDataDto;
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppListDataDto> SaveListEditFormData(AppListDataDto aformData)
    {
        if (aformData != null)
        {
            DataModelDateTimeConverterBL.ConvertListEditPostedUtcToClientForCalculation(aformData);

            OperationCallResult<AppListDataDto> validationResult = AppTransactionFormulaBL.ValidateListEditTransactionData(aformData);

            if (!validationResult.IsSuccessfulWithResult)
            {
                return validationResult;
            }
            else
            {
                OperationCallResult<AppListDataDto> saveResult = AppListEditFormDataLoadBL.SaveListEditFormData(aformData);

                if (saveResult.Object != null)
                {
                    DataModelDateTimeConverterBL.ConvertListEditFromUtcToClient(saveResult.Object);
                }

                return saveResult;
            }
        }

        return null;
    }


    [HttpGet]
    public AppMasterDetailDto GetNewFormData(int? transactionId, bool isConfigTestRun = false)
    {
        if (transactionId.HasValue)
        {
            AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetNewFormData(transactionId.Value, isConfigTestRun); // should be client time
            // To Do, Need to verify if need time convert
            return aAppformDataDto;
        }

        return null;
    }


    [HttpGet]
    public AppMasterDetailDto GetNewFormDataFromDataTransfer(int? dataTransferSettingId, string srcTransactionRid)
    {
        if (dataTransferSettingId.HasValue && !string.IsNullOrWhiteSpace(srcTransactionRid))
        {
            AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetNewFormDataFromDataTransfer(dataTransferSettingId.Value, srcTransactionRid);

            return aAppformDataDto;
        }

        return null;
    }

    [HttpPost]
    public AppMasterDetailDto PrepareDataTransferFormData_FromMasterDetailToMasterDetail(AppMasterDetailDto srcFormData)
    {
        if (srcFormData != null && srcFormData.DataTransferSettingId.HasValue)
        {
            DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(srcFormData);

            AppMasterDetailDto aAppformDataDto = AppTransactionDataTransferBL.PrepareDataTransferFormData_FromMasterDetailToMasterDetail(srcFormData, srcFormData.DataTransferSettingId);

            return aAppformDataDto;
        }

        return null;
    }


    [HttpPost]
    public List<AppMasterDetailDto> PrepareDataTransferFormData_FromListEditToMasterDetail(AppListDataDto srcListData)
    {
        if (srcListData != null && srcListData.DataTransferSettingId.HasValue)
        {
            DataModelDateTimeConverterBL.ConvertListEditFromUtcToClient(srcListData);

            List<AppMasterDetailDto> formDataDtoList = AppTransactionDataTransferBL.PrepareDataTransferFormData_FromListEditToMasterDetail(srcListData, srcListData.DataTransferSettingId);

            return formDataDtoList;
        }

        return null;
    }


    [HttpGet]
    public AppMasterDetailDto GetNewFormDataFromDefaultDataTransfer(int? transactionId, int? transactionRid)
    {
        if (transactionId.HasValue && transactionRid.HasValue)
        {
            AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetNewFormDataFromDefaultDataTransfer(transactionId.Value, transactionRid);

            return aAppformDataDto;
        }

        return null;
    }


    [HttpPost]
    public AppMasterDetailDto GetNewFormDataFromBagList(AppEshopBagDto appEshopBagDto)
    {
        List<AppEshopBagItemDto> bagItemList = appEshopBagDto.EshopBagItemList;

        if (!bagItemList.IsEmpty())
        {
            int? transactionId = bagItemList.First().TransactionId;

            if (transactionId.HasValue)
            {
                AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetNewFormData(transactionId.Value);

                Dictionary<string, List<AppEshopBagItemDto>> dictUnitBagitem = bagItemList.GroupBy(o => o.ProductDetaiViewMapUnitId.ToString()).ToDictionary(o => o.Key, o => o.ToList());

                foreach (string unitId in dictUnitBagitem.Keys)
                {
                    List<AppEshopBagItemDto> bagItemDtoList = dictUnitBagitem[unitId];
                    List<AppChildDataDto> AppChildDataDtoList = new List<AppChildDataDto>();

                    AppChildDataDto appChildDataDto = aAppformDataDto.EditCloneDictOneToManyFields[unitId].FirstOrDefault();
                    if (appChildDataDto != null)
                    {
                        foreach (AppEshopBagItemDto BagItemDto in bagItemDtoList)
                        {
                            AppChildDataDto aNewChildDataDto = appChildDataDto.DeepCopy();
                            foreach (string filedName in aNewChildDataDto.DictOneToOneFields.Keys.ToList())
                            {
                                if (BagItemDto.DictOneToOneFields.ContainsKey(filedName))
                                {
                                    aNewChildDataDto.DictOneToOneFields[filedName] = BagItemDto.DictOneToOneFields[filedName];
                                }
                            }

                            AppChildDataDtoList.Add(aNewChildDataDto);
                        }
                    }

                    if (AppChildDataDtoList.Count > 0)
                    {
                        aAppformDataDto.DictOneToManyFields.Remove(unitId);
                        aAppformDataDto.DictOneToManyFields.Add(unitId, AppChildDataDtoList);
                    }
                }

                return aAppformDataDto;
            }
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppMasterDetailDto> ValidateAndCalculateTransactionData(object rootAppformDataDto)
    {
        var aformData = JsonConvert.DeserializeObject<AppMasterDetailDto>(rootAppformDataDto.ToString());

        if (aformData != null)
        {
            DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(aformData);
            return AppTransactionFormulaBL.ValidateAndCalculateMasterDetailTransactionData(aformData);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppMasterDetailDto> CallLinkTargetExternalMethod(object rootAppformDataDto)
    {
        var aformData = JsonConvert.DeserializeObject<AppMasterDetailDto>(rootAppformDataDto.ToString());

        if (aformData != null)
        {
            DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(aformData);

            return AppExternalMethodRegisterBL.CallExternalMethodMasterDetail(aformData.ExternalMethodRegId, new object[] { aformData });
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppMasterDetailDto> CallFieldTargetExternalMethod(object rootAppformDataDto)
    {
        AppMasterDetailDto aformData = JsonConvert.DeserializeObject<AppMasterDetailDto>(rootAppformDataDto.ToString());

        if (aformData != null)
        {
            List<object> paramters = new List<object>();

            DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(aformData);
            return AppExternalMethodRegisterBL.GetFieldTargetExternalMethod(aformData.ExternalMethodRegId, new object[] { aformData });
        }

        return null;
    }


    [HttpPost]
    public AppMasterDetailDto GenerateMatrix(object rootAppformDataDto)
    {
        var aformData = JsonConvert.DeserializeObject<AppMasterDetailDto>(rootAppformDataDto.ToString());

        if (aformData != null)
        {
            DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(aformData);
            return AppTransactionFormulaBL.GenerateMatrix(aformData);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<AppMasterDetailDto> CreateDefaultMasterDetailTransactionData(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetNewFormData(transactionId.Value); // should be client time

            if (aAppformDataDto != null)
            {
                DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(aAppformDataDto);

                var result = AppMasterDetailFormDataSaveBL.SaveTransactionData(aAppformDataDto);

                if (result.Object != null)
                {
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(result.Object);
                }

                return result;
            }
        }
        return null;
    }


    [HttpPost]
    public OperationCallResult<bool> SaveOneTransactionCommandActionList(AppTransactionExDto aAppTransactionExDto)
    {
        if (aAppTransactionExDto != null)
        {
            return AppTransactionCommandBL.SaveOneTransactionCommandActionList(aAppTransactionExDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectWorkFlowActionExDto> CreateOneTransactionCommand(AppProjectWorkFlowActionExDto commandExDto)
    {
        if (commandExDto != null)
        {
            return AppTransactionCommandBL.CreateOneTransactionCommand(commandExDto);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<TransactionCommandActionResultDto> ExcuteTransactionCommonad(AppMasterDetailDto aformData)
    {
        if (aformData != null)
        {
            if (aformData != null && aformData.TransactionCommandId.HasValue)
            {
                // Input: Client DateTime
                // Output: Client DateTime

                DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(aformData);
                OperationCallResult<TransactionCommandActionResultDto> actionResult = AppTransactionCommandBL.ExecuteTransactionRootCommand(aformData);

                return actionResult;
            }
        }
        return null;
    }


    [HttpPost]
    public OperationCallResult<TransactionCommandActionResultDto> ExecuteWorkflowRootCommonad(AppMasterDetailDto aformData)
    {
        if (aformData != null)
        {
            if (aformData != null && aformData.TransactionCommandId.HasValue)
            {
                // Input: Client DateTime
                // Output: Client DateTime

                DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(aformData);
                OperationCallResult<TransactionCommandActionResultDto> actionResult = AppTransactionCommandBL.ExecuteWorkflowRootCommand(aformData);

                return actionResult;
            }
        }
        return null;
    }


    [HttpPost]
    public OperationCallResult<TransactionCommandActionResultDto> ExcuteListEditTransactionCommonad(AppListDataDto listEditFormData)
    {
        if (listEditFormData != null)
        {
            if (listEditFormData != null && listEditFormData.TransactionCommandId.HasValue)
            {
                // Input: Client DateTime
                // Output: Client DateTime

                DataModelDateTimeConverterBL.ConvertListEditPostedUtcToClientForCalculation(listEditFormData);
                OperationCallResult<TransactionCommandActionResultDto> actionResult = AppTransactionCommandBL.ExcuteListEditTransactionCommonad(listEditFormData);

                return actionResult;
            }
        }
        return null;
    }


    [HttpPost]
    public OperationCallResult<TransactionCommandActionResultDto> ExcuteSearchViewCommonad()
    {
        AppMasterDetailDto aformData = new AppMasterDetailDto();

        if (aformData != null)
        {
            if (aformData != null && aformData.TransactionCommandId.HasValue)
            {
                // Input: Client DateTime
                // Output: Client DateTime

                DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(aformData);

                var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aformData.TransactionId);

                if (transactionExDto.BusinessScopeId.HasValue && transactionExDto.BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation)
                {
                    OperationCallResult<TransactionCommandActionResultDto> actionResult = AppTransactionCommandBL.ExecuteWorkflowRootCommand(aformData);
                    return actionResult;
                }
                else
                {
                    OperationCallResult<TransactionCommandActionResultDto> actionResult = AppTransactionCommandBL.ExecuteTransactionRootCommand(aformData);
                    return actionResult;
                }
            }
        }
        return null;
    }

    [HttpGet]
    public OperationCallResult<TransactionCommandActionResultDto> ExecuteOneTransactionCommonadById(int? commandId, int? transactionId, string rootPrimaryKeyValue, int? chlldUnitId = null, string childRowPkCombString = null)
    {
        if (commandId.HasValue && transactionId.HasValue)
        {
            return AppTransactionCommandBL.ExecuteOneRootCommonadById(commandId.Value, transactionId.Value, rootPrimaryKeyValue, chlldUnitId, childRowPkCombString);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<TransactionCommandActionResultDto> ExecuteWorkflowById(int? workflowDataModelId, string rootPrimaryKeyValue = "")
    {
        if (workflowDataModelId.HasValue)
        {
            return AppTransactionCommandBL.ExecuteWorkflowById(workflowDataModelId.Value, rootPrimaryKeyValue);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<TransactionCommandActionResultDto> CreateWorkflowInstanceByIdAndExecute(int? workflowTransactionId)
    {
        if (workflowTransactionId.HasValue)
        {
            return AppTransactionCommandBL.CreateWorkflowInstanceByIdAndExecute(workflowTransactionId.Value);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<TransactionCommandActionResultDto> DeubgWorkflowOneRootChildCommand(int? workflowTransactionId, int? debugWorkflowRootChildCommandId, string debugKey)
    {
        if (workflowTransactionId.HasValue && debugWorkflowRootChildCommandId.HasValue && !string.IsNullOrWhiteSpace(debugKey))
        {
            return AppTransactionCommandBL.DeubgWorkflowOneRootChildCommand(workflowTransactionId.Value, debugWorkflowRootChildCommandId.Value, debugKey);
        }
        return null;
    }


    [HttpPost]
    public OperationCallResult<bool> ExecuteCommandOnSelectedSearchResultRows(SearchResultDto searchResultDto)
    {
        return AppTransactionCommandBL.ExecuteCommandOnSelectedSearchResultRows(searchResultDto);
    }


    [HttpGet]
    // (commandId, transRId, transFieldId1_Value1|transFieldId2_Value2|transFieldId3_Value3)
    public OperationCallResult<TransactionCommandActionResultDto> ExecuteCommandFromTransFieldIdValueParameters(int? commandId, int? transactionRId, string transFieldIdValueParameterStr)
    {
        if (commandId.HasValue && transactionRId.HasValue)
        {
            List<string> transFieldIdValueParameterStrList = new List<string>();

            if (!string.IsNullOrWhiteSpace(transFieldIdValueParameterStr))
            {
                transFieldIdValueParameterStrList = transFieldIdValueParameterStr.Split('|').ToList();
            }
            return AppTransactionCommandBL.ExecuteCommandFromTransFieldIdValueParameters(commandId.Value, transactionRId.Value, transFieldIdValueParameterStrList);
        }

        return null;
    }


    [HttpGet]
    public string Test()
    {
        return "Hello Test";
    }


    // !!Need To Drop
    [HttpPost]
    public AppRetrieveMutipleColumnDataSourceDto RetrieveMutipleColumDataSource(AppRetrieveMutipleColumnDataSourceDto appRetrieveMutipleColumnDataSourceDto)
    {
        return AppCascadingMutipleColumnBL.AppRetrieveMutipleColumnDataSourceDto(appRetrieveMutipleColumnDataSourceDto);
    }


    [HttpPost]
    public object GetCascadingDateTime(object data)
    {
        return AppCascadingBL.GetDateTimeUnitFieldCascading(data);
    }


    [HttpPost]
    public AppMasterDetailDto GetRootUnitFieldTriggerCascadingDataSource(object rootAppformDataDto)
    {
        var aformData = JsonConvert.DeserializeObject<AppMasterDetailDto>(rootAppformDataDto.ToString());

        if (aformData != null)
        {
            return AppCascadingBL.GetRootUnitFieldTriggerCascadingDataSource(aformData);
        }

        return null;
    }


    // For both child and Grandchild unit cascading
    [HttpPost]
    public AppChildDataDto GetChildOrGrandChildUnitFieldTriggerCascadingDataSource(dynamic formChildDataAndMasterDetailDto)
    {
        AppChildDataDto triggerChildRowDataDto = JsonConvert.DeserializeObject<AppChildDataDto>(formChildDataAndMasterDetailDto.AppChildDataDto.ToString());

        AppMasterDetailDto masterDataDto = JsonConvert.DeserializeObject<AppMasterDetailDto>(formChildDataAndMasterDetailDto.MasterDetailDataDto.ToString());

        if (triggerChildRowDataDto != null)
        {
            triggerChildRowDataDto.DictOneToManyFields = null;

            return AppCascadingBL.GetChildOrGrandChildUnitFieldTriggerCascadingDataSource(triggerChildRowDataDto, masterDataDto);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<object> DeleteOneAppForm(int? formId, int? transactionId)
    {
        if (formId.HasValue)
        {
            var validationResult = AppFormBL.DeleteOneAppForm(formId.Value, transactionId);

            return validationResult;
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppTransactionSaveAsMappingExDto> SaveAllAppTransactionSaveAsMappingExDto(TransactionSaveAsMappingSetDto aTransactionSaveAsMappingSetDto)
    {
        if (aTransactionSaveAsMappingSetDto != null && aTransactionSaveAsMappingSetDto.TransactionId.HasValue)
        {
            aTransactionSaveAsMappingSetDto.TransactionSaveAsMappingSet.DeletedItemIds = aTransactionSaveAsMappingSetDto.DeletedItemIds;

            OperationCallResult<AppTransactionSaveAsMappingExDto> aOperationCallResult = AppTransactionSaveAsMappingBL.SaveAllAppTransactionSaveAsMappingExDto(aTransactionSaveAsMappingSetDto.TransactionSaveAsMappingSet, aTransactionSaveAsMappingSetDto.TransactionId.Value);

            return aOperationCallResult;
        }

        return null;
    }


    [HttpGet]
    public ObservableSet<AppConditionalActionExDto> RetrieveAllAppConditionalActionListByTransactionId(int? transactionId)
    {
        return AppTransactionConditionalActionBL.RetrieveAllAppConditionalActionListByTransactionId(transactionId);
    }

    [HttpGet]
    public ObservableSet<AppConditionalActionExDto> RetrieveTransactionFieldUiTriggerConditionalActionList(int? transactionId, int uiTriggerFieldId)
    {
        return AppTransactionConditionalActionBL.RetrieveTransactionFieldUiTriggerConditionalActionList(transactionId, uiTriggerFieldId);
    }


    [HttpPost]
    public OperationCallResult<AppConditionalActionExDto> SaveAllAppConditionalActionExDto(ConditionalActionSetDto aConditionalActionSetDto)
    {
        if (aConditionalActionSetDto != null && aConditionalActionSetDto.TransactionId.HasValue)
        {
            aConditionalActionSetDto.ConditionalActionSet.DeletedItemIds = aConditionalActionSetDto.DeletedItemIds;

            OperationCallResult<AppConditionalActionExDto> aOperationCallResult = AppTransactionConditionalActionBL.SaveAllAppConditionalActionExDto(aConditionalActionSetDto.ConditionalActionSet, aConditionalActionSetDto.TransactionId.Value, aConditionalActionSetDto.UiTriggerFieldId);

            return aOperationCallResult;
        }

        return null;
    }

    [HttpGet]
    public ObservableSet<AppTransactionPostProcessExDto> RetrieveAllAppTransactionPostProcessListByTransactionId(int? transactionId)
    {
        return AppTransactionPostProcessBL.RetrieveProcessListByTransactionId(transactionId);
    }

    [HttpPost]
    public OperationCallResult<AppTransactionPostProcessExDto> SaveAllAppTransactionPostProcessExDto(TransactionPostProcessSetDto aTransactionPostProcessSetDto)
    {
        if (aTransactionPostProcessSetDto != null && aTransactionPostProcessSetDto.TransactionId.HasValue)
        {
            aTransactionPostProcessSetDto.TransactionPostProcessSet.DeletedItemIds = aTransactionPostProcessSetDto.DeletedItemIds;

            OperationCallResult<AppTransactionPostProcessExDto> aOperationCallResult = AppTransactionPostProcessBL.SaveAllAppTransactionPostProcessExDto(aTransactionPostProcessSetDto.TransactionPostProcessSet, aTransactionPostProcessSetDto.TransactionId.Value);

            return aOperationCallResult;
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppMasterDetailDto> SaveAsMasterDetailTransactionData(AppMasterDetailDto aAppMasterDetailDto)
    {
        if (aAppMasterDetailDto != null)
        {
            var result = AppMasterDetailFormDataSaveBL.SaveAsMasterDetailTransactionData(aAppMasterDetailDto.TransactionId, aAppMasterDetailDto.RootPrimaryKeyValue);

            if (result.Object != null)
            {
                DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(result.Object);
            }

            return result;
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> DeleteTransactionData(int transactionId, string rootPrimaryKeyValue)
    {
        return AppTransactionDataDeleteBL.DeleteTransactionData(transactionId, rootPrimaryKeyValue);
    }


    [HttpGet]
    public OperationCallResult<object> ValidatDeleteChildUnit(string unitId, int? rootPrimaryKeyValue, int? dataSourceFromId)
    {
        if (dataSourceFromId.HasValue)
        {
            return AppTransactionDataDeleteBL.ValidatDeleteChildUnit(unitId, rootPrimaryKeyValue, dataSourceFromId.Value);
        }

        return null;
    }


    [HttpGet]
    public ObservableSet<AppTransactionUnitDeleteFlowExDto> RetrieveDeleteFlowListByTransactionUnitId(int? transactionUnitId)
    {
        return AppTransactionUnitDeleteFlowBL.RetrieveDeleteFlowListByTransactionUnitId(transactionUnitId);
    }


    [HttpPost]
    public OperationCallResult<AppTransactionUnitDeleteFlowExDto> SaveAllAppTransactionUnitDeleteFlowExDto(TransactionUnitDeleteFlowSetDto aTransactionUnitDeleteFlowSetDto)
    {
        if (aTransactionUnitDeleteFlowSetDto != null && aTransactionUnitDeleteFlowSetDto.TransactionUnitId.HasValue)
        {
            aTransactionUnitDeleteFlowSetDto.TransactionUnitDeleteFlowSet.DeletedItemIds = aTransactionUnitDeleteFlowSetDto.DeletedItemIds;

            OperationCallResult<AppTransactionUnitDeleteFlowExDto> aOperationCallResult = AppTransactionUnitDeleteFlowBL.SaveAllAppTransactionUnitDeleteFlowExDto(aTransactionUnitDeleteFlowSetDto.TransactionUnitDeleteFlowSet, aTransactionUnitDeleteFlowSetDto.TransactionUnitId.Value);

            return aOperationCallResult;
        }

        return null;
    }


    [HttpGet]
    public AppTransactionFieldExDto RetrieveOneAppTransactionFieldExDto(int? transFieldId, int? layoutItemId)
    {
        return AppTransactionBL.RetrieveOneAppTransactionFieldExDto(transFieldId, layoutItemId);
    }


    [HttpPost]
    public OperationCallResult<AppTransactionExDto> SaveAppTransactionFieldExDto(AppTransactionFieldExDto transFieldExDto)
    {
        return AppTransactionBL.SaveAppTransactionFieldExDto(transFieldExDto);
    }

    [HttpPost]
    public AppTransactionFieldExDto ConvertTransactionFieldDataRetrieveMappingStringToDict(AppTransactionFieldExDto aAppTransactionFieldExDto)
    {
        aAppTransactionFieldExDto.ConvertDataRetrieveMappingStringToDict();
        return aAppTransactionFieldExDto;
    }

    [HttpPost]
    public AppTransactionFieldExDto ConvertBackTransactionFieldDataRetrieveMappingDictToString(AppTransactionFieldExDto aAppTransactionFieldExDto)
    {
        aAppTransactionFieldExDto.ConvertBackDataRetrieveMappingDictToString();
        return aAppTransactionFieldExDto;
    }


    [HttpGet]
    public OperationCallResult<DatabaseViewUpdateDto> CreateSimpleSearchViewFromMasterDetailTransactoin(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            OperationCallResult<DatabaseViewUpdateDto> aOperationCallResult = AppDatabaseViewBL.CreateSimpleSearchViewFromMasterDetailTransactoin(transactionId.Value);
            return aOperationCallResult;
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<DatabaseViewUpdateDto> BuildAdvancedDBViewDtoFromTransaction(int? transactionId, bool isOnlyUseRootTable)
    {
        if (transactionId.HasValue)
        {
            OperationCallResult<DatabaseViewUpdateDto> aOperationCallResult = AppDatabaseViewBL.BuildAdvancedDBViewDtoFromTransaction(transactionId.Value, isOnlyUseRootTable);
            return aOperationCallResult;
        }

        return null;
    }


    [HttpGet]
    public Dictionary<int, string> RetrieveListEditTransactionsBySchemaOwnerTableName(string tableName, string schemaOwner, int? dataSourceFrom)
    {
        if (!string.IsNullOrEmpty(tableName))
        {
            Dictionary<int, string> toReturn = AppTransactionBL.RetrieveListEditTransactionsBySchemaOwnerTableName(tableName, schemaOwner, dataSourceFrom);

            return toReturn;
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<AppTransactionExDto> CreateDefaultListTransactionFromTableName(string tableName, int? dataSourceRegisterId, string schemaOwner, int? saasApplicationId)
    {
        if (!string.IsNullOrEmpty(tableName))
        {
            OperationCallResult<AppTransactionExDto> toReturn = AppTransactionBL.CreateDefaultListTransactionFromTableName(tableName, dataSourceRegisterId, schemaOwner, saasApplicationId);

            return toReturn;
        }

        return null;
    }

    /// <summary>
    /// Creates a master / child / grandchild hierarchy transaction from related database table names.
    /// The platform reads each table's schema, auto-detects FK relationships, and saves the
    /// configured AppTransaction with its AppTransactionUnit hierarchy.
    /// </summary>
    [HttpPost]
    public OperationCallResult<AppTransactionExDto> CreateHierarchyTransactionFromTables(HierarchyTableSetupDto setupDto)
    {
        if (setupDto == null || string.IsNullOrWhiteSpace(setupDto.MasterTableName))
            return null;

        return AppTransactionBL.CreateHierarchyTransactionFromTables(setupDto);
    }


    [HttpGet]
    public List<AppProjectOrWorkFlowExDto> GetFormWorkflowList(int? transactionId, int? rootPkValue)
    {
        if (transactionId.HasValue && rootPkValue.HasValue)
        {
            return AppProjectWorkFlowProcessBL.GetTransactionRunningProjectWorkflowList(transactionId.Value, rootPkValue.Value);
        }

        return null;
    }


    [HttpGet]
    public AppTransactionDataLoadExDto RetrieveOneAppTransactionDataLoadExDto(int? dataLoadId)
    {
        if (dataLoadId.HasValue)
        {
            AppTransactionDataLoadExDto toReturn = AppTransactionTemplateDataLoadSetupBL.RetrieveOneAppTransactionDataLoadExDto(dataLoadId.Value);

            return toReturn;
        }

        return null;
    }


    [HttpGet]
    public ObservableSet<AppTransactionDataLoadDto> RetrieveAllAppTransactionDataLoadDto()
    {
        ObservableSet<AppTransactionDataLoadDto> toReturn = AppTransactionTemplateDataLoadSetupBL.RetrieveAllAppTransactionDataLoadDto();
        return toReturn;
    }


    [HttpGet]
    public ObservableSet<AppTransactionDataLoadDto> RetrievOneAppTransactionDataLoadDto(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            ObservableSet<AppTransactionDataLoadDto> toReturn = AppTransactionTemplateDataLoadSetupBL.RetrievOneAppTransactionDataLoadDto(transactionId.Value);
            return toReturn;
        }

        return null;
    }

    [HttpGet]
    public ObservableSet<AppTransactionDataLoadDto> RetrievOneAppTransactionUnitDataLoadDto(int? transactionUnitId)
    {
        if (transactionUnitId.HasValue)
        {
            ObservableSet<AppTransactionDataLoadDto> toReturn = AppTransactionTemplateDataLoadSetupBL.RetrievOneAppTransactionUnitDataLoadDto(transactionUnitId.Value);
            return toReturn;
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<object> SaveAllAppTransactionDataLoadDto(ObservableSet<AppTransactionDataLoadDto> appTransactionDataLoadDtoSet)
    {
        if (appTransactionDataLoadDtoSet != null)
        {
            return AppTransactionTemplateDataLoadSetupBL.SaveAllAppTransactionDataLoadDto(appTransactionDataLoadDtoSet);
        }

        return null;
    }

    [HttpGet]
    public ObservableSet<AppTranscationReportExDto> RetrieveAllAppTranscationReportListByTransactionId(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            ObservableSet<AppTranscationReportExDto> toReturn = AppTransactionReportBLBL.RetrieveAllAppTranscationReportListByTransactionId(transactionId.Value);
            return toReturn;
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppTranscationReportExDto> SaveAllAppTranscationReportExDto(TransactionReportSetDto aTransactionReportSetDto)
    {
        if (aTransactionReportSetDto != null && aTransactionReportSetDto.TransactionReportSet != null && aTransactionReportSetDto.TransactionId.HasValue)
        {
            aTransactionReportSetDto.TransactionReportSet.DeletedItemIds = aTransactionReportSetDto.DeletedItemIds;
            return AppTransactionReportBLBL.SaveAllAppTranscationReportExDto(aTransactionReportSetDto.TransactionReportSet, aTransactionReportSetDto.TransactionId.Value);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppTransactionDataLoadExDto> SaveAppTransactionDataLoadExDto(AppTransactionDataLoadExDto aAppTransactionDataLoadExDto)
    {
        if (aAppTransactionDataLoadExDto != null)
        {
            aAppTransactionDataLoadExDto.IsModified = true;

            foreach (var mapping in aAppTransactionDataLoadExDto.AppTranscationDataLoadFieldMappingList)
            {
                mapping.IsModified = true;
            }

            if (aAppTransactionDataLoadExDto.DictDeletedItemsIds == null)
            {
                aAppTransactionDataLoadExDto.DictDeletedItemsIds = new Dictionary<string, List<object>>();
            }

            if (aAppTransactionDataLoadExDto.DictDeletedItemsIds.ContainsKey("AppTranscationDataLoadFieldMappingList"))
            {
                aAppTransactionDataLoadExDto.AppTranscationDataLoadFieldMappingList.DeletedItemIds = aAppTransactionDataLoadExDto.DictDeletedItemsIds["AppTranscationDataLoadFieldMappingList"];
            }

            return AppTransactionTemplateDataLoadSetupBL.SaveAppTransactionDataLoadExDto(aAppTransactionDataLoadExDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> DeleteAppTransactionDataLoad(int? dataLoadId)
    {
        if (dataLoadId.HasValue)
        {
            OperationCallResult<object> toReturn = AppTransactionTemplateDataLoadSetupBL.DeleteAppTransactionDataLoad(dataLoadId.Value);

            return toReturn;
        }

        return null;
    }


    [HttpGet]
    public List<LookupItemDto> RetrieveBLQueryColumnList(int? dataLoadId)
    {
        if (dataLoadId.HasValue)
        {
            List<LookupItemDto> toReturn = AppTransactionTemplateDataLoadSetupBL.RetrieveBLQueryColumnList(dataLoadId.Value);

            return toReturn;
        }

        return null;
    }


    [HttpPost]
    public AppMasterDetailDto LoadTransactionTemplateData(AppMasterDetailDto aAppMasterDetailDto)
    {
        // To Do, Need to verify if need time convert
        DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(aAppMasterDetailDto);
        AppMasterDetailDto aAppformDataDto = AppTransactionTemplateDataLoadBL.LoadTransactionTemplateData(aAppMasterDetailDto);

        return aAppformDataDto;
    }


    [HttpGet]
    public List<AppSearchDto> RetrieveAllAppTransactionGroupDto(int? applicationId)
    {
        List<AppSearchDto> toReturn = AppSearchConfigBL.RetrieveSaasApplicationSearchList(applicationId, (int)EmAppSearchUsageType.DataModelTemplate);

        return toReturn;
    }


    [HttpGet]
    public AppSearchExDto RetrieveOneAppTransactionGroupExDto(int? groupId)
    {
        if (groupId.HasValue)
        {
            AppSearchExDto toReturn = AppSearchConfigBL.RetrieveOneAppSearchExDto(groupId.Value);

            return toReturn;
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppTransactionGroupExDto> SaveAppTransactionGroupExDto(AppTransactionGroupExDto aAppTransactionGroupExDto)
    {
        if (aAppTransactionGroupExDto != null)
        {
            if (aAppTransactionGroupExDto.DeletedItemsIds != null)
            {
                aAppTransactionGroupExDto.AppTransactionGroupItemList.DeletedItemIds = aAppTransactionGroupExDto.DeletedItemsIds;
            }

            var toReturn = AppTransactionGroupBL.SaveAppTransactionGroupExDto(aAppTransactionGroupExDto);

            return toReturn;
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> DeleteOneAppTransactionGroup(int? groupId)
    {
        if (groupId.HasValue)
        {
            OperationCallResult<bool> toReturn = AppTransactionGroupBL.DeleteOneAppTransactionGroup(groupId.Value);
            return toReturn;
        }

        return null;
    }


    [HttpGet]
    public AppTransactionGroupSessionExDto RetrieveOneAppTransactionGroupSession(int? groupSessionId)
    {
        if (groupSessionId.HasValue)
        {
            AppTransactionGroupSessionExDto toReturn = AppTransactionGroupSessionBL.RetrieveOneAppTransactionGroupSession(groupSessionId.Value);
            return toReturn;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppTransactionGroupSessionExDto> CreateOneAppTransactionGroupSession(AppTransactionGroupSessionExDto groupSessionExDto)
    {
        OperationCallResult<AppTransactionGroupSessionExDto> toReturn = AppTransactionGroupSessionBL.CreateOneAppTransactionGroupSession(groupSessionExDto);
        return toReturn;
    }

    [HttpGet]
    public AppTransactionGroupSessionExDto LoadOneAppTransactionGroupPreviewSession(int? transactionGroupId)
    {
        if (transactionGroupId.HasValue)
        {
            AppTransactionGroupSessionExDto toReturn = AppTransactionGroupSessionBL.LoadOneAppTransactionGroupPreviewSession(transactionGroupId.Value);
            return toReturn;
        }

        return null;
    }


    [HttpGet]
    public RoleAndUserListDto RetrieveSecurityObjectAvailableOrganizationAllRoleAndUser(int? objId, int? objType, int? actionCode)
    {
        if (objId.HasValue && objType.HasValue)
        {
            RoleAndUserListDto toReturn = AppSecuritySysObjGroupUserBL.RetrieveSecurityObjectAvailableOrganizationAllRoleAndUser(objId, objType, actionCode);
            return toReturn;
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<AppTransactionExDto> CreateDefaultMasterDetailTransaction(int? listEditOrFolderTransactionId, bool isNeedCreateSearch)
    {
        if (listEditOrFolderTransactionId.HasValue)
        {
            return AppTransactionBL.CreateDefaultMasterDetailTransaction(listEditOrFolderTransactionId.Value, isNeedCreateSearch);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<bool> UpdateAppFileFolder(AppFileUpdateDto appFileUpdateDto)
    {
        if (appFileUpdateDto != null && appFileUpdateDto.FileIdList != null)
        {
            return AppListEditFormDataLoadBL.UpdateAppFileFolder(appFileUpdateDto.FileIdList, appFileUpdateDto.TargetFolderId);
        }

        return null;
    }


    [HttpGet]
    public AppMessageDto ConvertMasterDetaiFormDataToNotificationDto(int? transactionId, int? transactionRid)
    {
        if (transactionId.HasValue && transactionRid.HasValue)
        {
            return AppTransactionDataTransferBL.ConvertMasterDetaiFormDataToNotificationDto(transactionId, transactionRid);
        }

        return null;
    }

    [HttpGet]
    public ObservableSet<AppTransactionDataTransferSettingExDto> RetrieveAllAppTransactionDataTransferSettingExDto(int? transactionId, int? destinationTransactionId)
    {
        return AppTransactionDataTransferSettingBL.RetrieveAllAppTransactionDataTransferSettingExDto(transactionId, destinationTransactionId);
    }


    [HttpGet]
    public AppTransactionDataTransferSettingExDto RetrieveOneAppTransactionDataTransferSettingExDto(int? settingId)
    {
        if (settingId.HasValue)
        {
            return AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(settingId);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppTransactionDataTransferSettingExDto> SaveOneAppTransactionDataTransferSettingExDto(AppTransactionDataTransferSettingExDto aSettingExDto)
    {
        if (aSettingExDto != null)
        {
            return AppTransactionDataTransferSettingBL.SaveOneAppTransactionDataTransferSettingExDto(aSettingExDto);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<bool> DeleteOneAppTransactionDataTransferSettingExDto(int? settingId)
    {
        if (settingId.HasValue)
        {
            OperationCallResult<bool> toReturn = AppTransactionDataTransferSettingBL.DeleteOneAppTransactionDataTransferSettingExDto(settingId.Value);
            return toReturn;
        }

        return null;
    }


    [HttpPost]
    public AppTransactionUnitExDto ConvertDbSchemaOwnerTableNameToTransactionUnitExDto(TableToUnitConverterDto converterDto)
    {
        if (converterDto != null && !string.IsNullOrWhiteSpace(converterDto.TableName))
        {
            DatabaseTable dbTable = AppMetaDataBL.GetOneDatabaseTableSchema(converterDto.TableName, converterDto.DataSourceRegisterId, converterDto.SchemaOwner);
            DatabaseTable parentUnitDbTable = null;

            if (converterDto.ParentUnit != null && !string.IsNullOrWhiteSpace(converterDto.ParentUnit.DataBaseTableName))
            {
                parentUnitDbTable = AppMetaDataBL.GetOneDatabaseTableSchema(converterDto.ParentUnit.DataBaseTableName, converterDto.DataSourceRegisterId, converterDto.ParentUnit.SchemaOwner);
            }

            if (dbTable != null)
            {
                AppTransactionUnitExDto parentUnit = converterDto.ParentUnit;

                AppTransactionUnitExDto newUnit = AppTransactionBL.CreateTransactionUnitFromDatabaseTable(dbTable);

                newUnit.IsSynchToDatabaseTable = false;
                newUnit.IsReadOnly = false;

                newUnit.AppTransactionFieldList = new ObservableSet<AppTransactionFieldExDto>();
                newUnit.Children = new List<AppTransactionUnitExDto>();

                int fieldSort = 0;
                foreach (var aColumn in dbTable.Columns)
                {
                    if (!(aColumn.Name == "AppCreatedByID"
                        || aColumn.Name == "AppCreatedDate"
                        || aColumn.Name == "AppModifiedDate"
                        || aColumn.Name == "AppCreatedByID"
                        || aColumn.Name == "AppModifiedByID"
                        || aColumn.Name == "AppCompanyID"
                        || aColumn.Name == "AppCreatedByCompanyID"))
                    {
                        fieldSort += 10;
                        var newTransactionField = ConvertTableColumnToTransactionFieldExDto(aColumn);
                        newTransactionField.SortOrder = fieldSort;
                        newUnit.AppTransactionFieldList.Add(newTransactionField);
                        SetNewTransactionFieldForeignkey(dbTable, parentUnitDbTable, parentUnit, aColumn, newTransactionField);
                    }
                }

                return newUnit;
            }
        }

        return null;
    }


    [HttpPost]
    public List<AppTransactionFieldExDto> ConvertTableColumnsToTransactionFieldExDtoList(TableToUnitConverterDto converterDto)
    {
        if (converterDto != null && !string.IsNullOrWhiteSpace(converterDto.TableName) && converterDto.NeedToAddDbColumns != null && converterDto.NeedToAddDbColumns.Count > 0)
        {
            DatabaseTable dbTable = AppMetaDataBL.GetOneDatabaseTableSchema(converterDto.TableName, converterDto.DataSourceRegisterId, converterDto.SchemaOwner);
            DatabaseTable parentUnitDbTable = null;

            if (converterDto.ParentUnit != null && !string.IsNullOrWhiteSpace(converterDto.ParentUnit.DataBaseTableName))
            {
                parentUnitDbTable = AppMetaDataBL.GetOneDatabaseTableSchema(converterDto.ParentUnit.DataBaseTableName, converterDto.DataSourceRegisterId, converterDto.ParentUnit.SchemaOwner);
            }

            if (dbTable != null)
            {
                AppTransactionUnitExDto parentUnit = converterDto.ParentUnit;

                List<AppTransactionFieldExDto> toReturn = new List<AppTransactionFieldExDto>();

                foreach (DatabaseColumn aColumn in converterDto.NeedToAddDbColumns)
                {
                    AppTransactionFieldExDto newTransactionField = ConvertTableColumnToTransactionFieldExDto(aColumn);

                    if (newTransactionField != null)
                    {
                        toReturn.Add(newTransactionField);
                        SetNewTransactionFieldForeignkey(dbTable, parentUnitDbTable, parentUnit, aColumn, newTransactionField);
                    }
                }

                return toReturn;
            }
        }

        return null;
    }


    [HttpPost]
    public List<LookupItemDto> RetrieveAutoCompleteDDLEntityItemSource(object rootAppformDataDto)
    {
        var aformData = JsonConvert.DeserializeObject<AppMasterDetailDto>(rootAppformDataDto.ToString());

        if (aformData != null)
        {
            return AppEntityInfoBL.RetrieveAutoCompleteDDLEntityItemSource(aformData);
        }

        return null;
    }


    [HttpGet]
    public ObservableSet<AppTransactionNavigationExDto> RetrieveFolderViewBytransactionId(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppTransactionNavigationBL.RetrieveFolderViewListBytransactionId(new List<int>() { transactionId.Value });
        }

        return null;
    }

    [HttpGet]
    public ObservableSet<AppTransactionNavigationExDto> RetrieveQuickSearchBytransactionId(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppTransactionNavigationBL.RetrieveSearchNavigationExDtoListBytransactionId(new List<int>() { transactionId.Value });
        }

        return null;
    }

    [HttpGet]
    public AppTransactionNavigationExDto RetrieveTransactionDefaultNavigationDto(int? transactionId, bool isFolderNavigation)
    {
        if (transactionId.HasValue)
        {
            return AppTransactionNavigationBL.RetrieveOneTransactionDefaultNavigationDto(transactionId, isFolderNavigation);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<bool> DeleteOneTransactionAllNavigations(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppTransactionNavigationBL.DeleteOneTransactionAllNavigations(transactionId.Value);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppTransactionNavigationExDto> SaveQuickSearchNavigationListExDto(TransactionNavigationSetDto aSet)
    {
        if (aSet != null && aSet.TransactionNavigationExDtoSet != null && aSet.TransactionId.HasValue)
        {
            aSet.TransactionNavigationExDtoSet.DeletedItemIds = aSet.DeletedItemIds;
            return AppTransactionNavigationBL.SaveQuickSearchNavigationListExDto(aSet.TransactionNavigationExDtoSet, aSet.TransactionId.Value);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppTransactionNavigationExDto> SaveFolderViewNavigationListExDto(TransactionNavigationSetDto aSet)
    {
        if (aSet != null && aSet.TransactionNavigationExDtoSet != null && aSet.TransactionId.HasValue)
        {
            aSet.TransactionNavigationExDtoSet.DeletedItemIds = aSet.DeletedItemIds;
            return AppTransactionNavigationBL.SaveFolderViewNavigationListExDto(aSet.TransactionNavigationExDtoSet, aSet.TransactionId.Value, aSet.MgtRootFolderId, aSet.IsEnableFolderSecurity);
        }

        return null;
    }


    [HttpGet]
    public List<AppSefolderDto> RetrieveAllRootFolderDtoList(int? folderType)
    {
        return AppSeFolderBL.RetrieveAllRootFolderDtoList(folderType);
    }


    [HttpGet]
    public TemplateFolderNavigationConfigResultDto RetrieveTemplateFolderNavigationConfig(int? templateSearchId)
    {
        return AppTransactionNavigationBL.RetrieveTemplateFolderNavigationConfig(templateSearchId);
    }


    [HttpPost]
    public OperationCallResult<TemplateFolderNavigationConfigResultDto> SaveTemplateFolderNavigationConfig(TemplateFolderNavigationConfigDto configDto)
    {
        return AppTransactionNavigationBL.SaveTemplateFolderNavigationConfig(configDto);
    }


    [HttpGet]
    public FolderNavigationRuntimeContextDto ResolveFolderNavigationRuntimeContext(int? transactionId)
    {
        return AppTransactionNavigationBL.ResolveFolderNavigationRuntimeContext(transactionId);
    }


    [HttpGet]
    public ObservableSet<AppTransactionSaveAsMappingExDto> RetrieveAllAppTransactionSaveAsMappingListByTransactionId(int? transactionId)
    {
        return AppTransactionSaveAsMappingBL.RetrieveAllAppTransactionSaveAsMappingListByTransactionId(transactionId);
    }

    [HttpGet]
    public AppBusienssAssormentNavigationExDto RetrieveOneAppBusienssNavigationExDto(int? navigationId)
    {
        return AppBusinessNavigationBL.RetrieveOneAppBusienssNavigationExDto(navigationId);
    }


    [HttpPost]
    public OperationCallResult<AppBusienssAssormentNavigationExDto> SaveAppBusienssAssormentNavigationExDto(AppBusienssAssormentNavigationExDto aAppBusienssAssormentNavigationExDto)
    {
        if (aAppBusienssAssormentNavigationExDto != null)
        {
            return AppBusinessNavigationBL.SaveAppBusienssAssormentNavigationExDto(aAppBusienssAssormentNavigationExDto);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<bool> DeleteAppBusienssAssormentNavigation(int? navigationId)
    {
        return AppBusinessNavigationBL.DeleteAppBusienssAssormentNavigation(navigationId);
    }


    [HttpGet]
    public AssortmentNavigationDto RetrieveFormAssortmentNavigation(int? navigationId, string transactionRId)
    {
        return AppBusinessNavigationBL.RetrieveFormAssortmentNavigation(navigationId, transactionRId);
    }

    [HttpGet]
    public List<AppTransAuditTrailLogExDto> RetrieveTransactionFormChangeLog(int? transactionId, string transactionRId)
    {
        List<AppTransAuditTrailLogExDto> logList = AppTransAuditTrailLogBL.RetrieveTransactionFormChangeLog(transactionId, transactionRId);

        return logList;
    }


    [HttpPost]
    public OperationCallResult<AppMasterDetailDto> CallTransactionFormExternalService(dynamic paramObj)
    {
        if (paramObj != null)
        {
            if (paramObj.FormData != null)
            {
                AppMasterDetailDto formData = JsonConvert.DeserializeObject<AppMasterDetailDto>(paramObj.FormData.ToString());
                if (formData != null)
                {
                    formData = DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(formData);
                    paramObj.FormData = JObject.Parse(JsonConvert.SerializeObject(formData));
                }
            }

            if (paramObj.LinkedSearchSelectedItems != null)
            {
                // need to convert search result row datetimedetail column to client time zone
            }

            // for CallTransactionFormExternalService, formData need to be UserAgent/client time zone.
            return AppPluginClient.CallTransactionFormExternalService(paramObj, paramObj.RestResourceUri.ToString());
        }
        return null;
    }


    [HttpPost]
    public object CallDynamicExternalDynamicService(dynamic paramObj)
    {
        if (paramObj != null)
        {
            return AppPluginClient.CallDynamicExternalDynamicService(paramObj, paramObj.RestResourceUri.ToString());
        }
        return null;
    }

    [HttpGet]
    public OperationCallResult<AppEntityInfoExDto> GenerateNewEntity(string entityCode, int? saasApplicationId)
    {
        if (!string.IsNullOrEmpty(entityCode))
        {
            return AppEntityInfoBL.GenerateNewEntity(entityCode, saasApplicationId);
        }
        return null;
    }

    [HttpGet]
    public List<AppTransactionDto> RetrieveSaasApplicationWorkflowAutomationList(int? applicationId)
    {
        return AppTransactionBL.RetrieveSaasApplicationWorkflowAutomationList(applicationId, true);
    }


    [HttpGet]
    public List<AppTransactionDto> RetrieveSaasApplicationTransactionList(int? applicationId)
    {
        return AppTransactionBL.RetrieveSaasApplicationTransactionList(applicationId, true);
    }


    [HttpGet]
    public List<AppFormDto> RetrieveSaasApplicationFormList(int? applicationId)
    {
        return AppFormBL.RetrieveSaasApplicationFormList(applicationId);
    }

    [HttpGet]
    public List<AppProjectOrWorkFlowDto> RetrieveSaasApplicationWorkFlowList(int? applicationId)
    {
        return AppProjectWorkFlowStructureBL.RetrieveSaasApplicationWorkFlowList(applicationId);
    }

    [HttpGet]
    public List<AppSearchDto> RetrieveSaasApplicationSearchList(int? applicationId)
    {
        return AppSearchConfigBL.RetrieveSaasApplicationSearchList(applicationId);
    }

    [HttpGet]
    public List<AppDataSetExDto> RetrieveSaasApplicationDataSetList(int? applicationId)
    {
        return AppDataSetBL.RetrieveSaasApplicationDataSetList(applicationId);
    }

    [HttpPost]
    public OperationCallResult<bool> ImportSaasApplicationTransactions(SaasApplicationSectionItemImportDto importDto)
    {
        return AppTransactionBL.ImportSaasApplicationTransactions(importDto);
    }


    [HttpGet]
    public List<TransactionApiSettingDto> RetrieveTransactionApiSettings(int? transactionId)
    {
        return AppTransactionBL.RetrieveTransactionApiSettings(transactionId);
    }

    [HttpGet]
    public List<AppDataSetExDto> RetrieveSaasApplicationErDiagramList(int? applicationId)
    {
        return AppDatabaseErDiagramBL.RetrieveSaasApplicationErDiagramList(applicationId);
    }

    [HttpGet]
    public ObservableSet<AppDataSetExDto> RetrieveAllErDiagramDto()
    {
        return AppDatabaseErDiagramBL.RetrieveAllErDiagramDto();
    }

    [HttpGet]
    public List<AppDataSetExDto> RetrieveSaasApplicationExcelTableImportSettingList(int? applicationId, bool isFlatSingleTableImport)
    {
        return AppDatabaseTableImportBL.RetrieveSaasApplicationExcelTableImportSettingList(applicationId, isFlatSingleTableImport);
    }

    [HttpGet]
    public ObservableSet<AppDataSetExDto> RetrieveAllExcelTableImportSettingDto(bool isFlatSingleTableImport)
    {
        return AppDatabaseTableImportBL.RetrieveAllExcelTableImportSettingDto(isFlatSingleTableImport);
    }

    [HttpGet]
    public List<AppDataSetExDto> RetrieveSaasApplicationDbToDbTableImportSettingList(int? applicationId)
    {
        return AppDatabaseTableImportBL.RetrieveSaasApplicationDbToDbTableImportSettingList(applicationId);
    }

    [HttpGet]
    public ObservableSet<AppDataSetExDto> RetrieveAllDbToDbTableImportSettingDto()
    {
        return AppDatabaseTableImportBL.RetrieveAllDbToDbTableImportSettingDto();
    }


    [HttpGet]
    public List<AppDataSetExDto> RetrieveSaasApplicationEntityImportSettingList(int? applicationId)
    {
        return AppDatabaseTableImportBL.RetrieveSaasApplicationEntityImportSettingList(applicationId);
    }

    [HttpGet]
    public ObservableSet<AppDataSetExDto> RetrieveAllEntityImportSettingDto()
    {
        return AppDatabaseTableImportBL.RetrieveAllEntityImportSettingDto();
    }


    [HttpGet]
    public AppDataSetExDto RetrieveOneErDiagramExDto(int? dataSetId)
    {
        return AppDatabaseErDiagramBL.RetrieveOneErDiagramExDto(dataSetId);
    }

    [HttpPost]
    public OperationCallResult<AppDataSetExDto> SaveOneErDiagramExDto(AppDataSetExDto aAppDataSetExDto)
    {
        return AppDatabaseErDiagramBL.SaveOneErDiagramExDto(aAppDataSetExDto);
    }


    [HttpGet]
    public OperationCallResult<object> DeleteOneErDiagram(int? dataSetId)
    {
        return AppDatabaseErDiagramBL.DeleteOneErDiagram(dataSetId);
    }


    [HttpGet]
    public OperationCallResult<bool> ImportApplicationFromHostDB(int? hostDbApplicatoinId)
    {
        if (hostDbApplicatoinId.HasValue)
        {
            return AppApplicationImportBL.ImportApplicationFromHostDBToCurrentUserDB(hostDbApplicatoinId.Value);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<bool> ImportApplicationFromSourceDBToTargetDB(string srcDbName, string targetDbName, int? srcApplicatoinId)
    {
        if (!string.IsNullOrWhiteSpace(srcDbName) && !string.IsNullOrWhiteSpace(targetDbName) && srcApplicatoinId.HasValue)
        {
            return AppApplicationImportBL.ImportApplicationFromSourceDBToTargetDB(srcDbName, targetDbName, srcApplicatoinId.Value);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<bool> UnistallApplicationFromCurrentUserDB(int? targetApplicatoinId)
    {
        if (targetApplicatoinId.HasValue)
        {
            return AppApplicationImportBL.UnistallApplicationFromCurrentUserDB(targetApplicatoinId.Value);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<bool> UnistallApplicationFromTargetDB(string targetDbName, int? targetApplicatoinId)
    {
        if (!string.IsNullOrWhiteSpace(targetDbName) && targetApplicatoinId.HasValue)
        {
            return AppApplicationImportBL.UnistallApplicationFromTargetDB(targetDbName, targetApplicatoinId.Value);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> SetTransactionForPublicAccess(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppTransactionBL.SetTransactionForPublicAccess(transactionId.Value);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<bool> GenerateConsumeApiDataModelSaveSetting(int? getDataTransactionId, int? saveDataOperationId)
    {
        if (getDataTransactionId.HasValue && saveDataOperationId.HasValue)
        {
            return AppTransactionBL.GenerateConsumeApiDataModelSaveSetting(getDataTransactionId, saveDataOperationId);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> GenerateConsumeApiDataModelDeleteSetting(int? getDataTransactionId, int? deleteDataOperationId)
    {
        if (getDataTransactionId.HasValue && deleteDataOperationId.HasValue)
        {
            return AppTransactionBL.GenerateConsumeApiDataModelDeleteSetting(getDataTransactionId, deleteDataOperationId);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppMasterDetailDto> PublishConsumApiTransactionDataByDataTransfer(AppMasterDetailDto appformDataDto)
    {
        if (appformDataDto != null)
        {
            DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(appformDataDto);

            var calculationResult = AppTransactionFormulaBL.ValidateAndCalculateMasterDetailTransactionData(appformDataDto);

            if (!calculationResult.IsSuccessfulWithResult)
            {
                return calculationResult;
            }
            else
            {
                if (appformDataDto != calculationResult.Object)
                {
                    appformDataDto = calculationResult.Object;
                }

                appformDataDto = DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(appformDataDto);

                var saveResult = AppMasterDetailApiFormDataSaveBL.PublishConsumApiTransactionDataByDataTransfer(appformDataDto);

                if (saveResult.Object != null)
                {
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(saveResult.Object);
                }

                if (calculationResult.ValidationResult.Items.FirstOrDefault(o => o.ItemType == ValidationItemType.Warning) != null)
                {
                    saveResult.ValidationResult.Merge(calculationResult.ValidationResult);

                    if (saveResult.Object != null && calculationResult.Object != null)
                    {
                        saveResult.Object.DictTransFieldIdAndWarningHighlightStyleId = calculationResult.Object.DictTransFieldIdAndWarningHighlightStyleId;
                    }
                }

                return saveResult;
            }
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectWorkFlowActionExDto> GenerateCommandCallApiOperationSetting(AppProjectWorkFlowActionExDto commandDto)
    {
        if (commandDto != null && commandDto.CommandTransactionId.HasValue && commandDto.ApiOperationId.HasValue)
        {
            int transactionId = commandDto.CommandTransactionId.Value;
            int apiOperationId = commandDto.ApiOperationId.Value;

            return AppTransactionBL.GenerateCommandCallApiOperationSetting(commandDto, transactionId, apiOperationId);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppTransactionExDto> SaveOneWorkflowAutomation(AppTransactionExDto aAppTransactionExDto)
    {
        if (aAppTransactionExDto != null)
        {
            if (aAppTransactionExDto.DictDeletedItemsIds.ContainsKey("AppTransactionUnitList"))
            {
                aAppTransactionExDto.AppTransactionUnitList.DeletedItemIds = aAppTransactionExDto.DictDeletedItemsIds["AppTransactionUnitList"];
            }

            foreach (var aUnit in aAppTransactionExDto.AppTransactionUnitList)
            {
                if (aUnit.Id != null)
                {
                    if (aAppTransactionExDto.DictDeletedItemsIds.ContainsKey("AppTransactionFieldList_" + aUnit.Id.ToString()))
                    {
                        aUnit.AppTransactionFieldList.DeletedItemIds = aAppTransactionExDto.DictDeletedItemsIds["AppTransactionFieldList_" + aUnit.Id.ToString()];
                    }

                    foreach (var aChildUnit in aUnit.Children)
                    {
                        if (aChildUnit.Id != null)
                        {
                            if (aAppTransactionExDto.DictDeletedItemsIds.ContainsKey("AppTransactionFieldList_" + aChildUnit.Id.ToString()))
                            {
                                aChildUnit.AppTransactionFieldList.DeletedItemIds = aAppTransactionExDto.DictDeletedItemsIds["AppTransactionFieldList_" + aChildUnit.Id.ToString()];
                            }

                            foreach (var aGrandchildUnit in aChildUnit.Children)
                            {
                                if (aGrandchildUnit.Id != null)
                                {
                                    if (aAppTransactionExDto.DictDeletedItemsIds.ContainsKey("AppTransactionFieldList_" + aGrandchildUnit.Id.ToString()))
                                    {
                                        aGrandchildUnit.AppTransactionFieldList.DeletedItemIds = aAppTransactionExDto.DictDeletedItemsIds["AppTransactionFieldList_" + aGrandchildUnit.Id.ToString()];
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return AppTransactionCommandBL.SaveOneWorkflowAutomation(aAppTransactionExDto);
        }

        return null;
    }


    [HttpPost]
    public string PutMasterDetailFormDataFromToCacheByGuid(AppMasterDetailDto formData)
    {
        DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(formData);

        return AppCacheManagerBL.PutMasterDetailFormDataFromToCacheByGuid(formData);
    }


    [HttpGet]
    public AppMasterDetailDto GetMasterDetailFormDataFromCacheByGuid(string guid)
    {
        AppMasterDetailDto formData = AppCacheManagerBL.GetMasterDetailFormDataFromCacheByGuid(guid);

        DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(formData);

        return formData;
    }


    [HttpGet]
    public bool RemoveOneKeyFromDictGuidAndAppMasterDetailDto(string guid)
    {
        return AppCacheManagerBL.RemoveOneKeyFromDictGuidAndAppMasterDetailDto(guid);
    }

    [HttpPost]
    public List<AppProjectWorkFlowActionDto> SyncronizeWorkflowCommandNodeTreeFromActionList(AppTransactionExDto aAppTransactionExDto)
    {
        return AppTransactionCommandBL.SyncronizeWorkflowCommandNodeTreeFromActionList(aAppTransactionExDto);
    }


    [HttpGet]
    public string GetTransactionRootDatesetQuery(int? transactionId)
    {
        return AppDataSetBL.GetTransactionRootDatesetQuery(transactionId);
    }

    [HttpGet]
    public WorkflowAutomationRuntimeDto GetWorkflowAutomationRuntimeProgressData(int? transactionId, string rootPrimaryKeyValue)
    {
        if (transactionId.HasValue)
        {
            return AppTransactionCommandBL.GetWorkflowAutomationRuntimeProgressData(transactionId.Value, rootPrimaryKeyValue);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<bool> ForceStopWorkflowByBatchNumber(string batchNumber, int? workflowAutomationId)
    {
        if (!string.IsNullOrWhiteSpace(batchNumber))
        {
            return AppTransactionCommandBL.ForceStopWorkflowByBatchNumber(batchNumber, workflowAutomationId);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<AppTransactionExDto> SaveAsOneWorkflowAutomation(int? workflowTransactionId, string newWorkflowSurffix, string newWorkflowName, int? applicationId = null)
    {
        if (workflowTransactionId.HasValue)
        {
            return AppApplicationImportBL.SaveAsOneWorkflowAutomation(workflowTransactionId.Value, newWorkflowSurffix, newWorkflowName, applicationId);
        }

        return null;
    }

    public OperationCallResult<bool> CreateWorkflowWindowsSchedulerTask(AppWinSchedulerTaskDto appWinSchedulerTaskDto)
    {
        if (appWinSchedulerTaskDto != null)
        {
            return AppTransactionCommandBL.CreateWorkflowWindowsSchedulerTask(appWinSchedulerTaskDto);
        }

        return null;
    }

    private AppTransactionFieldExDto ConvertTableColumnToTransactionFieldExDto(DatabaseColumn aTableColumn)
    {
        if (aTableColumn != null)
        {
            AppTransactionFieldExDto aTransactionField = new AppTransactionFieldExDto();

            aTransactionField.DataBaseFieldName = aTableColumn.Name;
            aTransactionField.DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(aTableColumn.Name);

            if (aTableColumn.Tag != null)
            {
                try
                {
                    aTransactionField.DataType = (int)Enum.Parse(typeof(EmAppDataType), aTableColumn.Tag.ToString());
                }
                catch (Exception ex)
                {
                }
            }

            aTransactionField.DataRetrieveType = (int)EmAppCascadingSourceType.RelationalTable;
            aTransactionField.DisplayWidth = "100";
            aTransactionField.Nbdecimal = 0;
            aTransactionField.IsPrimaryKey = aTableColumn.IsPrimaryKey;
            aTransactionField.IsVisible = true;
            aTransactionField.IsReadonly = false;
            aTransactionField.IsGroupBy = false;
            aTransactionField.IsGridUseAvailableEntitySource = false;
            aTransactionField.IsNeedLog = false;
            aTransactionField.IsAllowEmpty = true;
            aTransactionField.IsConvertToUpperCase = false;
            aTransactionField.IsLinkToParentPrimaryKey = false;
            aTransactionField.RowIdentityGuid = Guid.NewGuid();
            aTransactionField.ControlType = (int)EmAppControlType.TextBox;

            aTransactionField.ControlType = (int)ControlTypeValueConverter.ConvertDataTypeToDefaultControlType(aTransactionField.DataType);

            if (aTableColumn.Length.HasValue && aTableColumn.Length.Value > 0)
            {
                aTransactionField.MaxCharLegnth = aTableColumn.Length;
                aTransactionField.NeedValidator = true;
            }

            if ((aTableColumn.IsPrimaryKey || !aTableColumn.Nullable) && !aTableColumn.IsAutoNumber)
            {
                aTransactionField.IsAllowEmpty = false;
                aTransactionField.NeedValidator = true;
            }

            if (aTableColumn.IsUniqueKey && !aTableColumn.IsAutoNumber)
            {
                aTransactionField.IsUnique = true;
                aTransactionField.NeedValidator = true;
            }

            if (aTableColumn.Scale.HasValue)
            {
                aTransactionField.Nbdecimal = aTableColumn.Scale.Value;
            }

            return aTransactionField;
        }

        return null;
    }

    private static void SetNewTransactionFieldForeignkey(DatabaseTable dbTable, DatabaseTable parentUnitDbTable, AppTransactionUnitExDto parentUnit, DatabaseColumn aColumn, AppTransactionFieldExDto newTransactionField)
    {
        if (parentUnitDbTable != null && parentUnit != null && aColumn.IsForeignKey && !string.IsNullOrWhiteSpace(aColumn.ForeignKeyTableName))
        {
            if (parentUnitDbTable.Name.ToLower() == aColumn.ForeignKeyTableName.ToLower())
            {
                newTransactionField.IsLinkToParentPrimaryKey = true;

                if (parentUnitDbTable != null && parentUnitDbTable.PrimaryKey != null && parentUnitDbTable.PrimaryKey.Columns != null && parentUnitDbTable.PrimaryKey.Columns.Count > 0)
                {
                    if (dbTable.ForeignKeys != null)
                    {
                        foreach (var fkobj in dbTable.ForeignKeys)
                        {
                            if (fkobj.RefersToTable.ToLower() == parentUnitDbTable.Name.ToLower())
                            {
                                var fkColumnIndex = fkobj.Columns.IndexOf(aColumn.Name);

                                if (fkColumnIndex >= 0 && parentUnitDbTable.PrimaryKey.Columns.Count > fkColumnIndex)
                                {
                                    var linkToParentTbPKColumnName = parentUnitDbTable.PrimaryKey.Columns[fkColumnIndex];

                                    var parentPkField = parentUnit.AppTransactionFieldList.FirstOrDefault(o => o.DataBaseFieldName.ToLower() == linkToParentTbPKColumnName.ToLower());
                                    if (parentPkField != null)
                                    {
                                        newTransactionField.ParentPKFieldGuid = parentPkField.RowIdentityGuid;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    public static AppTransactionStructureDto GetFormStructureMethod(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            var toReturn = AppTransactionStructureLoadBL.GetAppTransactionStructureDto(transactionId.Value);

            return toReturn;
        }

        return null;
    }

    public static AppMasterDetailDto GetFormDataMethod(int? transactionId, string rootPrimaryKeyValue, int? autoExecuteCommandId = null, StaticSearchResultRowJsonDto selectDataRow = null)
    {
        if (transactionId.HasValue)
        {
            AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId.Value, rootPrimaryKeyValue, autoExecuteCommandId, selectDataRow);

            DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(aAppformDataDto);

            return aAppformDataDto;
        }

        return null;
    }
}
