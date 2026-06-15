using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Collections;
    
using System.Runtime;

using APP.Framework;
namespace App.BL
{
    public static class AppFormFlexLayoutBL
    {

        private static readonly int _imageItemHeight = 230;
        private static readonly int _videoItemHeight = 300;
        private static readonly int _memoItemHeight = 96;
        private static readonly int _regularItemHeight = 30;




        public static AppFormExDto RetrieveOneAppFormFlexLayoutExDto(object formId)
        {
            AppFormEntity aAppFormEntity = RetrieveOneAppFormFlexLayoutEntity(formId);
            // AppFormEntity aAppFormEntity = AppFormBL.RetrieveOneAppFormEntity(formId);
            AppFormExDto aAppFormExDto = AppFormConverter.ConvertEntityToExDto(aAppFormEntity);

            if (aAppFormExDto.IsPrintForm)
            {
                aAppFormExDto.AssociatedTransactionId = AppFormBL.GetTranscationIdFromPrintFormId(formId);
            }
            else
            {
                aAppFormExDto.AssociatedTransactionId = AppFormBL.GetTranscationIdFromFormId(formId);
            }


            AppTransactionExDto transactionExDto = null;

            if (aAppFormExDto.AssociatedTransactionId.HasValue)
            {
                transactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(aAppFormExDto.AssociatedTransactionId.Value);

                if (transactionExDto != null)
                {
                    aAppFormExDto.IsPhysicalModelTableCreated = transactionExDto.IsPhysicalModelTableCreated;
                    aAppFormExDto.IsApiIntegrationTransaction = transactionExDto.OtherOptions != null && transactionExDto.OtherOptions.IsApiIntegrationTransaction;

                    aAppFormExDto.AssociatedTransactionExDto = transactionExDto;

                    




                    //transactionExDto.DictTransFieldIdAndCrossRelationSettingDto = dictTransFieldIdAndCrossRelationSettingDto;
                }

            }

            PrepareFlexFormLayoutItemExDtoTree(aAppFormEntity, aAppFormExDto, transactionExDto);

            return aAppFormExDto;
        }

        public static AppFormExDto BuildAppFormDefaultLayout(int formId)
        {
            AppFormExDto aAppFormExDto = RetrieveOneAppFormFlexLayoutExDto(formId);
            aAppFormExDto.AppFormLayoutItemList = new ObservableSet<AppFormLayoutItemExDto>();

            AppTransactionExDto transactionExDto = null;

            if (aAppFormExDto.AssociatedTransactionId.HasValue)
            {
                transactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(aAppFormExDto.AssociatedTransactionId.Value);
                int columns = BuildAppFormDefaultLayout_SetMainLayout(aAppFormExDto, transactionExDto);

                Dictionary<string, AppFormLayoutItemExDto> dictUiIdAndLayoutItem = new Dictionary<string, AppFormLayoutItemExDto>();

                AppFormLayoutItemExDto root_row = AppendNewLayoutRow(dictUiIdAndLayoutItem, aAppFormExDto, null);
                AppFormLayoutItemExDto root_stack = AppendNewStackContainer(dictUiIdAndLayoutItem, aAppFormExDto, root_row, 24, columns);

                AppFormLayoutItemExDto fields_OuterContainerRow = AppendNewLayoutRow(dictUiIdAndLayoutItem, aAppFormExDto, root_stack);

                AppFormLayoutItemExDto fields_MainStack = AppendNewStackContainer(dictUiIdAndLayoutItem, aAppFormExDto, fields_OuterContainerRow, 24, columns);
                fields_MainStack.DomAttribute.IsCollapsible = true;
                fields_MainStack.DomAttribute.DisplayName = transactionExDto.RootMasterUnit.UnitDisplayName;

                AppFormLayoutItemExDto fields_MainRow = AppendNewLayoutRow(dictUiIdAndLayoutItem, aAppFormExDto, fields_MainStack);

                AppFormLayoutItemExDto grids_MainRow = AppendNewLayoutRow(dictUiIdAndLayoutItem, aAppFormExDto, root_stack);

                Dictionary<int, List<AppTransactionFieldExDto>> dictColAndFieldList = PrepareColumnAndFieldsDictionary(transactionExDto, columns);

                for (int i = 1; i <= columns; i++)
                {
                    int colSpan = 24 / columns;
                    AppFormLayoutItemExDto fields_OneColumnStack = AppendNewStackContainer(dictUiIdAndLayoutItem, aAppFormExDto, fields_MainRow, colSpan, 1);

                    List<AppTransactionFieldExDto> colFieldList = dictColAndFieldList[i];

                    foreach (AppTransactionFieldExDto fieldDto in colFieldList)
                    {
                        AppFormLayoutItemExDto oneFiled_Row = AppendNewLayoutRow(dictUiIdAndLayoutItem, aAppFormExDto, fields_OneColumnStack);
                        AppFormLayoutItemExDto oneFiled_Item = AppendNewTransFieldItem(dictUiIdAndLayoutItem, aAppFormExDto, oneFiled_Row, fieldDto, 24);
                    }
                }

                AppFormLayoutItemExDto grids_MainStack = AppendNewStackContainer(dictUiIdAndLayoutItem, aAppFormExDto, grids_MainRow, 24, 1);

                if (transactionExDto.RootMasterUnit.Children.Count >= 2)
                {
                    AppFormLayoutItemExDto grids_MainStackRootRow = AppendNewLayoutRow(dictUiIdAndLayoutItem, aAppFormExDto, grids_MainStack);
                    AppFormLayoutItemExDto grids_TabContainer = AppendNewTabContainer(dictUiIdAndLayoutItem, aAppFormExDto, grids_MainStackRootRow, 24, 1);

                    foreach (var gridUnitDto in transactionExDto.RootMasterUnit.Children)
                    {
                        AppFormLayoutItemExDto oneGrid_Tab = AppendNewTab(dictUiIdAndLayoutItem, aAppFormExDto, grids_TabContainer, 24, 1);
                        oneGrid_Tab.DomAttribute.DisplayName = gridUnitDto.UnitDisplayName;

                        AppFormLayoutItemExDto oneTab_MainRow = AppendNewLayoutRow(dictUiIdAndLayoutItem, aAppFormExDto, oneGrid_Tab);
                        AppFormLayoutItemExDto oneTab_MainStack = AppendNewStackContainer(dictUiIdAndLayoutItem, aAppFormExDto, oneTab_MainRow, 24, 1);

                        AppFormLayoutItemExDto oneGrid_Row = AppendNewLayoutRow(dictUiIdAndLayoutItem, aAppFormExDto, oneTab_MainStack);
                        AppFormLayoutItemExDto oneGrid_Item = AppendNewTransChildUnitGridItem(dictUiIdAndLayoutItem, aAppFormExDto, oneGrid_Row, gridUnitDto, 24);
                    }

                }
                else
                {

                    foreach (var gridUnitDto in transactionExDto.RootMasterUnit.Children)
                    {
                        AppFormLayoutItemExDto oneGrid_Row = AppendNewLayoutRow(dictUiIdAndLayoutItem, aAppFormExDto, grids_MainStack);
                        AppFormLayoutItemExDto oneGrid_Item = AppendNewTransChildUnitGridItem(dictUiIdAndLayoutItem, aAppFormExDto, oneGrid_Row, gridUnitDto, 24);
                    }
                }



            }

            return aAppFormExDto;
        }



        public static AppFormLayoutItemExDto RetrieveOneAppFormLayoutItemExDto(int? layoutItemId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppFormLayoutItemEntity entity = new AppFormLayoutItemEntity(layoutItemId.Value);
                adpater.FetchEntity(entity);
                AppFormLayoutItemExDto toReturn = AppFormLayoutItemConverter.ConvertEntityToExDto(entity);

                return toReturn;
            }
        }

        public static OperationCallResult<AppFormLayoutItemExDto> UpdateOneAppFormLayoutItemExDto(AppFormLayoutItemExDto layoutItemExDto)
        {
            OperationCallResult<AppFormLayoutItemExDto> aOperationCallResult = new OperationCallResult<AppFormLayoutItemExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppFormLayoutItemEntity entity = new AppFormLayoutItemEntity();
            AppFormLayoutItemConverter.CopyDtoToEntity(entity, layoutItemExDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    
                    adapter.UpdateEntitiesDirectly(entity, new RelationPredicateBucket(AppFormLayoutItemFields.FormLayoutItemId == (int)layoutItemExDto.Id));
                  
                    adapter.Commit();                    
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormExDto), "App_Form_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                    aOperationCallResult.Object = RetrieveOneAppFormLayoutItemExDto((int)layoutItemExDto.Id);
                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormExDto), "App_Form_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }


            }

            return aOperationCallResult;
        }



        //private static Dictionary<int, List<AppTransactionFieldExDto>> PrepareColumnAndFieldsDictionary(AppTransactionExDto transactionExDto, int columns)
        //{
        //    Dictionary<int, List<AppTransactionFieldExDto>> dictColAndFieldList = new Dictionary<int, List<AppTransactionFieldExDto>>();

        //    List<AppTransactionFieldExDto> rootLevelFields = transactionExDto.DictRootLevelUnitTransactionField.Values.Where(o => o.IsVisible.HasValue && o.IsVisible.Value).ToList();

        //    int nbRootField = rootLevelFields.Count;

        //    int nbFieldPerCol = (int)(Math.Ceiling((decimal)nbRootField / columns));



        //    for (int col = 1; col <= columns; col++)
        //    {
        //        List<AppTransactionFieldExDto> colFiels = new List<AppTransactionFieldExDto>();

        //        for (int i = 0; i < nbFieldPerCol; i++)
        //        {
        //            int fieldIndex = i + nbFieldPerCol * (col - 1);
        //            if (fieldIndex < nbRootField)
        //            {
        //                colFiels.Add(rootLevelFields[fieldIndex]);
        //            }
        //        }

        //        dictColAndFieldList.Add(col, colFiels);
        //    }

        //    return dictColAndFieldList;
        //}





        public static OperationCallResult<AppFormExDto> SaveAppFormFlexLayoutExDto(AppFormExDto aAppFormExDto)
        {
            OperationCallResult<AppFormExDto> aOperationCallResult = new OperationCallResult<AppFormExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            ObservableSet<AppFormLayoutItemExDto> layoutItemFlatList = new ObservableSet<AppFormLayoutItemExDto>();
            ConvertFormUiHiarachyLayoutItemsToFlatList(aAppFormExDto.AppFormLayoutItemList, layoutItemFlatList);
            RemoveUiPlaceHolderRowsAndButtons(layoutItemFlatList);

            //Dictionary<>

            //foreach (var layoutItemExDto in layoutItemFlatList)
            //{
            //    try {

            //    }
            //}

            // prepare Data
            if (aAppFormExDto.IsNew)
            {
                AppFormEntity aAppFormEntity = new AppFormEntity();

                ConvertAppFormExDtoFlexUiLayoutToEntity(aAppFormExDto, layoutItemFlatList, aAppFormEntity);

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppFormEntity);

                        adapter.Commit();

                        aAppFormExDto.Id = aAppFormEntity.FormId;

                        //
                        UpdateLayoutParentKey(aAppFormEntity.FormId);
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_FormEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                        //if (aAppFormExDto.SaasApplicationId.HasValue)
                        //{
                        //    AppApplicationAssetsItemExDto formAssetsItemDto = new AppApplicationAssetsItemExDto();
                        //    formAssetsItemDto.ApplicationId = aAppFormExDto.SaasApplicationId;
                        //    formAssetsItemDto.FormId = aAppFormEntity.FormId;
                        //    AppSaasUserApplicationPackageBL.SaveNewApplicationAssetsItemExDto(formAssetsItemDto);
                        //}
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_FormEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));



                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_FormEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else
            {
                aValidationResult.Merge(SaveAppFormFlexLayoutExDto_ProcessDirtyAppFormExDto(aAppFormExDto, layoutItemFlatList));
            }

            if (!aValidationResult.HasErrors)
            {

                //if (aAppFormExDto.IsPhysicalModelTableCreated.HasValue && aAppFormExDto.IsPhysicalModelTableCreated.Value)
                //{
                //    // if already published, don't recreate transaction from form. update form and transaction.
                //    aOperationCallResult.Object = RetrieveOneAppFormFlexLayoutExDto(aAppFormExDto.Id);
                //}
                //else
                //{
                //    // if not published, recreate transaction from form layout.   
                //    OperationCallResult<AppFormExDto> extractionResult = AppFromDataModelBL.ExtractTranscationModelFromAppForm(aAppFormExDto.Id, false, aAppFormExDto.SaasApplicationId, aAppFormExDto.FormPublishSettingDto);

                //    if (extractionResult.IsSuccessfulWithResult)
                //    {
                //        aOperationCallResult.Object = extractionResult.Object;
                //    }
                //    else
                //    {
                //        aOperationCallResult.ValidationResult.Merge(extractionResult.ValidationResult);
                //    }

                //}

                if (aAppFormExDto.AssociatedTransactionExDto != null && aAppFormExDto.AssociatedTransactionExDto.IsModified)
                {
                    if (aAppFormExDto.AssociatedTransactionExDto.DictUnitldIdAndFormulaSetDto != null)
                    {
                        List<AppTransactionUnitFormulaSetDto> needToSaveFormularSetDtoList = new List<AppTransactionUnitFormulaSetDto>();

                        foreach (var formularSetDto in aAppFormExDto.AssociatedTransactionExDto.DictUnitldIdAndFormulaSetDto.Values)
                        {
                            if (formularSetDto.IsModified)
                            {
                                needToSaveFormularSetDtoList.Add(formularSetDto);
                            }
                        }

                        OperationCallResult<AppTransactionUnitFormulaSetDto> formularSaveResult = AppTransactionFormulaSetupBL.SaveAppTransactionUnitFormulaSetDtoList(needToSaveFormularSetDtoList);

                        if (!formularSaveResult.IsSuccessful)
                        {
                            aValidationResult.Merge(formularSaveResult.ValidationResult);
                        }
                    }


                    if (aAppFormExDto.AssociatedTransactionExDto.DictTransFieldIdAndCrossRelationSettingDto != null)
                    {
                        foreach (var transFieldId in aAppFormExDto.AssociatedTransactionExDto.DictTransFieldIdAndCrossRelationSettingDto.Keys)
                        {
                            var crossSettingDto = aAppFormExDto.AssociatedTransactionExDto.DictTransFieldIdAndCrossRelationSettingDto[transFieldId];

                            if (crossSettingDto.IsModified)
                            {
                                if (aAppFormExDto.AssociatedTransactionExDto.DictAllTransactionField != null
                                        && aAppFormExDto.AssociatedTransactionExDto.DictAllTransactionField.ContainsKey(transFieldId))
                                {
                                    var transFieldDto = aAppFormExDto.AssociatedTransactionExDto.DictAllTransactionField[transFieldId];
                                    transFieldDto.ChildUnitSubscribeParentFieldId = crossSettingDto.ChildUnitSubscribeParentFieldId;
                                    transFieldDto.ParentUnitSubscribeChildAggFunctionId = crossSettingDto.ParentUnitSubscribeChildAggFunctionId;
                                }
                            }
                        }
                    }

                    var saveTransactionResult = AppTransactionBL.SaveAppTransactionExDto(aAppFormExDto.AssociatedTransactionExDto);

                    if (!saveTransactionResult.IsSuccessfulWithResult)
                    {
                        aValidationResult.Merge(saveTransactionResult.ValidationResult);
                    }
                }

                aOperationCallResult.Object = RetrieveOneAppFormFlexLayoutExDto(aAppFormExDto.Id);
            }

            return aOperationCallResult;
        }





        public static OperationCallResult<AppFormExDto> PublishAppFormToTransactionAndCreateTables(AppFormExDto appFormExDto)
        {
            OperationCallResult<AppFormExDto> publishValidationResult = AppFormFlexLayoutBL.FlexFormPublishValidation(appFormExDto);

            try
            {
                if (publishValidationResult.ValidationResult.HasErrors)
                {
                    return publishValidationResult;
                }
                else
                {
                    OperationCallResult<AppFormExDto> saveResult = SaveAppFormFlexLayoutExDto(appFormExDto);

                    if (saveResult.IsSuccessfulWithResult)
                    {
                        OperationCallResult<AppFormExDto> extractResult = AppFromDataModelBL.ExtractTranscationModelFromAppForm(saveResult.Object.Id, true, appFormExDto.SaasApplicationId, appFormExDto.FormPublishSettingDto);
                        return extractResult;                      
                    }
                    else
                    {
                        return saveResult;
                    }
                }
            }
            catch (Exception e)
            {
                publishValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_PublishAppFormToTransactionAndCreateTables_PublishSystemError_Error", ValidationItemType.Error, e.ToString()));
                return publishValidationResult;
            }
        }

        private static ValidationResult ExecuteFormPublishPostProcess(AppFormExDto formExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();
            if (formExDto.FormPublishSettingDto != null)
            {
                if (formExDto.AssociatedTransactionId.HasValue)
                {
                    AppTransactionExDto transactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(formExDto.AssociatedTransactionId.Value);

                    if (transactionExDto != null)
                    {
                        //transactionExDto.SaasApplicationId = formExDto.SaasApplicationId;
                        int? newSearchId = null;
                        bool isFolderNavigationCreated = false;
                        List<int> newListEditTransactionIdList = new List<int>();

                        if (formExDto.FormPublishSettingDto.IsCreateApplicationMenu)
                        {
                           // OperationCallResult<bool> createMenuResult = FormPublishPostProcess_CreateApplicationMenu(formExDto, newSearchId, isFolderNavigationCreated, newListEditTransactionIdList);

                            var createMenuResult = AppDatabaseViewBL.QuickGenerateTransactionDefaultSeachNavigation((int)transactionExDto.Id);

                            if (!createMenuResult.IsSuccessful)
                            {
                                aValidationResult.Merge(createMenuResult.ValidationResult);
                            }                          
                        }
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CannotFindFormTransaction_Error", ValidationItemType.Error, "Cannot Find Form Transaction."));
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CannotFindFormAssociatedTransactionId_Error", ValidationItemType.Error, "Cannot Find Form Associated Transaction Id."));
                }
            }


            return aValidationResult;
        }



        private static OperationCallResult<bool> FormPublishPostProcess_CreateApplicationMenu(AppFormExDto formExDto, int? newSearchId, bool isFolderNavigationCreated, List<int> newListEditTransactionIdList)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            toReturn.ValidationResult = new ValidationResult();

            if (newSearchId.HasValue || isFolderNavigationCreated || newListEditTransactionIdList.Count > 0)
            {
                if (formExDto.SaasApplicationId.HasValue)
                {
                    ObservableSet<AppListMenuExDto> menuTreeList = AppTreeListMenuBL.RetrieveListMenuHairarchyDto(false, formExDto.SaasApplicationId.Value);

                    if (menuTreeList != null && menuTreeList.Count == 1)
                    {
                        menuTreeList = menuTreeList.First().AppListMenu_List;
                        AppListMenuExDto mainMenuGroup = menuTreeList.FirstOrDefault(o => o.Name.ToLower() == "main");
                        AppListMenuExDto othersMenuGroup = menuTreeList.FirstOrDefault(o => o.Name.ToLower() == "others");

                        if (mainMenuGroup == null)
                        {
                            mainMenuGroup = new AppListMenuExDto();
                            mainMenuGroup.ParentId = formExDto.SaasApplicationId.Value;
                            mainMenuGroup.Sort = 10;
                            if (menuTreeList.Where(o => o.Sort.HasValue).Count() > 0)
                            {
                                mainMenuGroup.Sort = menuTreeList.Where(o => o.Sort.HasValue).Max(o => o.Sort.Value) + 10;
                            }
                            mainMenuGroup.Name = "Main";
                            mainMenuGroup.Description = "Applicaton Main Pages";
                            mainMenuGroup.IconName = "helm.png";
                            mainMenuGroup.EmDeviceMenuShowMode = 3;
                            mainMenuGroup.LinkType = (int)EmAppListMenuLinkType.SystemPage;
                            menuTreeList.Add(mainMenuGroup);
                        }

                        if (othersMenuGroup == null)
                        {
                            othersMenuGroup = new AppListMenuExDto();
                            othersMenuGroup.ParentId = formExDto.SaasApplicationId.Value;
                            othersMenuGroup.Sort = 10;
                            if (menuTreeList.Where(o => o.Sort.HasValue).Count() > 0)
                            {
                                othersMenuGroup.Sort = menuTreeList.Where(o => o.Sort.HasValue).Max(o => o.Sort.Value) + 10;
                            }
                            othersMenuGroup.Sort = menuTreeList.Where(o => o.Sort.HasValue).Max(o => o.Sort.Value) + 10;
                            othersMenuGroup.Name = "Others";
                            othersMenuGroup.Description = "Applicaton Other Pages";
                            othersMenuGroup.IconName = "Share-64.png";
                            othersMenuGroup.EmDeviceMenuShowMode = 3;
                            othersMenuGroup.LinkType = (int)EmAppListMenuLinkType.SystemPage;
                            menuTreeList.Add(othersMenuGroup);
                        }

                        if (newSearchId.HasValue)
                        {
                            AppListMenuExDto menuExDto = new AppListMenuExDto();
                            menuExDto.Sort = 10;
                            if (mainMenuGroup.AppListMenu_List.Where(o => o.Sort.HasValue).Count() > 0)
                            {
                                menuExDto.Sort = mainMenuGroup.AppListMenu_List.Where(o => o.Sort.HasValue).Max(o => o.Sort.Value) + 10;
                            }

                            menuExDto.Name = formExDto.Name;
                            menuExDto.Description = formExDto.Name;
                            menuExDto.IconName = "Fine Print-64.png";
                            menuExDto.EmDeviceMenuShowMode = 3;
                            menuExDto.LinkType = (int)EmAppListMenuLinkType.SystemPage;
                            menuExDto.RouteCode = "MasterDataManagement";
                            menuExDto.Link = newSearchId.Value.ToString();
                            mainMenuGroup.AppListMenu_List.Add(menuExDto);
                        }

                        if (isFolderNavigationCreated)
                        {
                            AppListMenuExDto menuExDto = new AppListMenuExDto();
                            menuExDto.Sort = 10;
                            if (mainMenuGroup.AppListMenu_List.Where(o => o.Sort.HasValue).Count() > 0)
                            {
                                menuExDto.Sort = mainMenuGroup.AppListMenu_List.Where(o => o.Sort.HasValue).Max(o => o.Sort.Value) + 10;
                            }
                            menuExDto.Name = formExDto.Name + " Management";
                            menuExDto.Description = formExDto.Name + " Management";
                            menuExDto.IconName = "Open Folder-64.png";
                            menuExDto.EmDeviceMenuShowMode = 3;
                            menuExDto.LinkType = (int)EmAppListMenuLinkType.SystemPage;
                            menuExDto.RouteCode = "FolderNavigation";
                            menuExDto.Link = formExDto.AssociatedTransactionId.Value.ToString();
                            mainMenuGroup.AppListMenu_List.Add(menuExDto);
                        }

                        var saveMeuResult = AppListMenuBL.SaveAllAppListMenuEntityDto(menuTreeList);

                        if (saveMeuResult.IsSuccessful)
                        {
                            toReturn.Object = true;
                        }
                        else
                        {
                            toReturn.ValidationResult = saveMeuResult.ValidationResult;
                        }
                    }
                    else
                    {
                        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CannotFindApplicationRootMenu_Error", ValidationItemType.Error, "Create Menu Failed. Cannot Find Application Root Menu."));
                    }
                }
                else
                {
                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CannotFindApplicationRootMenu_Error", ValidationItemType.Error, "Create Menu Failed. Cannot Find Application Root Menu."));
                }
            }
            else
            {
                toReturn.Object = true;
            }


            return toReturn;



        }

        private static OperationCallResult<bool> FormPublishPostProcess_CreateFolderNavigation(AppFormExDto formExDto, AppTransactionExDto transactionExDto)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            toReturn.ValidationResult = new ValidationResult();


            OperationCallResult<DatabaseViewUpdateDto> dBViewResult = AppDatabaseViewBL.BuildAdvancedDBViewDtoFromTransaction(formExDto.AssociatedTransactionId.Value, true);

            if (!dBViewResult.IsSuccessfulWithResult && dBViewResult.Object.OrgViewDto != null
                && dBViewResult.Object.OrgViewDto.SelectedColumnsList != null)
            {
                if (dBViewResult.ValidationResult.HasErrors)
                {
                    toReturn.ValidationResult = dBViewResult.ValidationResult;
                }
                else
                {
                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CreateFolderNavigation_Error", ValidationItemType.Error, "Create Folder navigation failed."));
                }

                return toReturn;
            }
            else
            {
                DatabaseViewUpdateDto dbViewUpdateDto = dBViewResult.Object;

                if (dbViewUpdateDto.DataSetDto != null)
                {
                    dbViewUpdateDto.DataSetDto.SaasApplicationId = transactionExDto.SaasApplicationId;
                }

                int colCount = 0;

                foreach (var aColumn in dbViewUpdateDto.OrgViewDto.SelectedColumnsList)
                {
                    colCount++;

                    aColumn.IsUsedAsViewField = false;

                    if (colCount <= 8)
                    {
                        aColumn.IsUsedAsViewField = true;
                    }

                    if (aColumn.ColumnName.StartsWith("PKUDF"))
                    {
                        aColumn.IsUsedAsViewField = true;
                        aColumn.SearchViewDisplayName = "ID";
                    }

                    if (aColumn.ColumnName.ToLower() == "folderid")
                    {
                        aColumn.IsUsedAsViewField = true;
                        aColumn.IsFolderIdKeyField = true;
                    }
                }

                AppSefolderExDto rootFolder = new AppSefolderExDto();
                rootFolder.Name = formExDto.Name + " Management";
                rootFolder.FolderType = (int)EmAppTransBusinessType.FormData;
                var rootFolderSaveResult = AppSeFolderBL.SaveAppSefolder(rootFolder);

                if (rootFolderSaveResult.IsSuccessfulWithResult)
                {
                    dbViewUpdateDto.RootFolderId = (int)rootFolderSaveResult.Object.Id;
                    if (transactionExDto.RootMasterUnit != null)
                    {
                        if (transactionExDto.RootMasterUnit.PrimaryKeyDbfieldList != null && transactionExDto.RootMasterUnit.PrimaryKeyDbfieldList.Count == 1)
                        {
                            dbViewUpdateDto.TransactionRootPrimaryKey = transactionExDto.RootMasterUnit.PrimaryKeyDbfieldList[0];
                        }
                    }


                    var folderNavigationSaveResult = AppDatabaseViewBL.SaveDataSetAndCreateFolderViewNavigation(dbViewUpdateDto);

                    if (folderNavigationSaveResult.IsSuccessful)
                    {
                        toReturn.Object = true;
                    }
                    else
                    {
                        if (folderNavigationSaveResult.ValidationResult.HasErrors)
                        {
                            toReturn.ValidationResult = folderNavigationSaveResult.ValidationResult;
                        }
                        else
                        {
                            toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CreateFolderNavigation_Error", ValidationItemType.Error, "Create Folder navigation failed."));
                        }
                    }

                    return toReturn;
                }
                else
                {
                    toReturn.ValidationResult.Merge(rootFolderSaveResult.ValidationResult);
                    return toReturn;
                }
            }

        }

        private static OperationCallResult<int?> FormPublishPostProcess_CreateSearchNavigation(AppTransactionExDto transactionExDto)
        {
            OperationCallResult<int?> toReturn = new OperationCallResult<int?>();
            toReturn.ValidationResult = new ValidationResult();

            OperationCallResult<DatabaseViewUpdateDto> dBViewResult = AppDatabaseViewBL.BuildAdvancedDBViewDtoFromTransaction((int)transactionExDto.Id, true);
            if (!dBViewResult.IsSuccessfulWithResult && dBViewResult.Object.OrgViewDto != null
                && dBViewResult.Object.OrgViewDto.SelectedColumnsList != null)
            {
                if (dBViewResult.ValidationResult.HasErrors)
                {
                    toReturn.ValidationResult = dBViewResult.ValidationResult;
                }
                else
                {
                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CreateSearch_Error", ValidationItemType.Error, "Create Search navigation failed."));
                }

                return toReturn;
            }
            else
            {
                DatabaseViewUpdateDto dbViewUpdateDto = dBViewResult.Object;

                if (dbViewUpdateDto.DataSetDto != null)
                {
                    dbViewUpdateDto.DataSetDto.SaasApplicationId = transactionExDto.SaasApplicationId;
                }

                int colCount = 0;

                foreach (var aColumn in dbViewUpdateDto.OrgViewDto.SelectedColumnsList)
                {
                    colCount++;

                    aColumn.IsUsedAsSearchField = false;
                    aColumn.IsUsedAsViewField = false;

                    if (colCount <= 3)
                    {
                        aColumn.IsUsedAsSearchField = true;
                    }

                    if (colCount <= 12)
                    {
                        aColumn.IsUsedAsViewField = true;
                    }

                    if (aColumn.ColumnName.StartsWith("PKUDF"))
                    {
                        aColumn.IsUsedAsViewField = true;
                        aColumn.SearchViewDisplayName = "ID";
                    }
                }

                var createSearchResult = AppDatabaseViewBL.SaveDataSetAndCreateSearchView(dbViewUpdateDto);

                if (createSearchResult.IsSuccessfulWithResult)
                {
                    toReturn.Object = createSearchResult.Object.SearchId;
                }
                else
                {
                    if (createSearchResult.ValidationResult.HasErrors)
                    {
                        toReturn.ValidationResult = createSearchResult.ValidationResult;
                    }
                    else
                    {
                        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CreateSearch_Error", ValidationItemType.Error, "Create Search navigation failed."));
                    }
                }

                return toReturn;

            }
        }

        public static OperationCallResult<AppFormExDto> FlexFormPublishValidation(AppFormExDto appFormExDto)
        {
            OperationCallResult<AppFormExDto> aOperationCallResult = new OperationCallResult<AppFormExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            Dictionary<string, string> dictFormPropertyNameAndErrorMessage = new Dictionary<string, string>();
            Dictionary<string, Dictionary<string, string>> dictLayoutItemHostIdAndPropertyErrorMessage = new Dictionary<string, Dictionary<string, string>>();

            ObservableSet<AppFormLayoutItemExDto> layoutItemFlatList = new ObservableSet<AppFormLayoutItemExDto>();
            ConvertFormUiHiarachyLayoutItemsToFlatList(appFormExDto.AppFormLayoutItemList, layoutItemFlatList);
            RemoveUiPlaceHolderRowsAndButtons(layoutItemFlatList);
            List<DatabaseTableDto> dbTableDtoList = AppMetaDataBL.GetDataSourceTableAndViewList(ServerContext.Instance.CurrnetClientIdentity.DataSourceId).Where(o => !o.IsDbView).ToList();


            if (string.IsNullOrWhiteSpace(appFormExDto.Name))
            {
                dictFormPropertyNameAndErrorMessage[AppFormDto.NameProperty] = "Form name cannot be empty.";
            }
            else if (appFormExDto.Name.ToLower().Contains("new flex form"))
            {
                dictFormPropertyNameAndErrorMessage[AppFormDto.NameProperty] = "Please change the form name.";
            }
            else
            {
                //string tableMiddleName = AppFromDataModelBL.FilterSQLDBInvalidChar(appFormExDto.Name);
                //string rootTablename = "UDF_Root_" + tableMiddleName + "_" + ServerContext.Instance.CurrentUid;
                //rootTablename = AppFromDataModelBL.FilterSQLDBInvalidChar(rootTablename);


                //var duplicateDbTable = dbTableDtoList.FirstOrDefault(o => o.Name.ToLower() == rootTablename.Trim().ToLower());
                //if (duplicateDbTable != null)
                //{
                //    dictFormPropertyNameAndErrorMessage[AppFormDto.NameProperty] = "Please change the form name. It conflicts with an existing database table name.";
                //}

            }

            foreach (var layoutItemExDto in appFormExDto.AppFormLayoutItemList)
            {
                FlexFormPublishValidation_ValidateOneLayoutItem(layoutItemFlatList, layoutItemExDto, dictLayoutItemHostIdAndPropertyErrorMessage, dbTableDtoList);
            }

            appFormExDto.DictFormPropertyNameAndErrorMessage = dictFormPropertyNameAndErrorMessage;
            appFormExDto.DictLayoutItemHostIdAndPropertyErrorMessage = dictLayoutItemHostIdAndPropertyErrorMessage;

            aOperationCallResult.Object = appFormExDto;

            if (appFormExDto.DictFormPropertyNameAndErrorMessage.Keys.Count > 0 || appFormExDto.DictLayoutItemHostIdAndPropertyErrorMessage.Count > 0)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppmessageNotificationSettingExDto), "App_FlexFormPublishValidation_Error", ValidationItemType.Error, "Validation failed"));
            }
            else
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppmessageNotificationSettingExDto), "App_FlexFormPublishValidation_OK", ValidationItemType.Message, "Validation successful"));
            }

            return aOperationCallResult;
        }

        //public static string ConvertFormFlexLayoutToHtmlView(object formId)
        //{
        //    string viewContent = string.Empty;

        //    AppFormExDto formDto = RetrieveOneAppFormFlexLayoutExDto(formId);

        //    if (formDto != null && formDto.LayoutType.HasValue && formDto.LayoutType.Value == (int)EmAppFormLayoutType.Flex)
        //    {
        //        int? formWidth = ControlTypeValueConverter.ConvertValueToInt(formDto.DefaultWidth);

        //        if (!formWidth.HasValue)
        //        {
        //            formWidth = 800;
        //        }

        //        viewContent += "<div class=\"FlexForm\" style=\"width:" + formWidth.Value + "px;\">";

        //        foreach (var layoutRow in formDto.AppFormLayoutItemList.OrderBy(o => o.FlowOrGridLayoutSortOrder))
        //        {
        //            viewContent += "<div class=\"LayoutRow\" style=\"width:" + formWidth.Value + "px;\">";

        //            viewContent += "Row" + layoutRow.FlowOrGridLayoutSortOrder.Value;

        //            viewContent += "</div>";
        //        }

        //        viewContent += "</div>";
        //    }


        //    return viewContent;
        //}


        private static AppFormEntity RetrieveOneAppFormFlexLayoutEntity(object FormId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppFormEntity formEntity = new AppFormEntity(int.Parse(FormId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppFormEntity);

                rootPath.Add(AppFormEntity.PrefetchPathAppFormLayoutItem);

                adpater.FetchEntity(formEntity, rootPath);
                return formEntity;
            }
        }



        private static ValidationResult SaveAppFormFlexLayoutExDto_ProcessDirtyAppFormExDto(AppFormExDto aAppFormExDto, ObservableSet<AppFormLayoutItemExDto> layoutItemFlatList)
        {

            ValidationResult aValidationResult = new ValidationResult();

            AppFormEntity appFormEntity = RetrieveOneAppFormFlexLayoutEntity(aAppFormExDto.Id);
            //AppFormEntity appFormEntity = AppFormBL.RetrieveOneAppFormEntity(aAppFormExDto.Id);
            AppFormConverter.CopyDtoToEntity(appFormEntity, aAppFormExDto);


            Dictionary<int, AppFormLayoutItemEntity> dictDbAppFormLayoutEntity = appFormEntity.AppFormLayoutItem.ToDictionary(o => o.FormLayoutItemId, o => o);
            Dictionary<int, AppFormLayoutItemExDto> dictAppFormLayoutExdto = layoutItemFlatList.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

            List<int> groupIdDbms = dictDbAppFormLayoutEntity.Keys.ToList();
            List<int> groupIdUi = dictAppFormLayoutExdto.Keys.ToList();

            //Delete Id
            List<int> deletAppFormLayoutIDs = groupIdDbms.Except(groupIdUi).ToList();



            //new Entity
            layoutItemFlatList.Where(o => o.IsNew)
                .ForAll(o =>
                {
                    AppFormLayoutItemEntity aAppFormLayoutItemEntity = new AppFormLayoutItemEntity();
                    AppFormLayoutItemConverter.CopyDtoToEntity(aAppFormLayoutItemEntity, o);
                    appFormEntity.AppFormLayoutItem.Add(aAppFormLayoutItemEntity);
                });


            //dirty
            List<int> dirtyAppFormLayoutIds = groupIdDbms.Intersect(groupIdUi).ToList();


            // 
            foreach (int layoutItemId in dirtyAppFormLayoutIds)
            {
                AppFormLayoutItemEntity appFormLayoutItemEntity = dictDbAppFormLayoutEntity[layoutItemId];
                AppFormLayoutItemExDto appFormLayoutItemExDto = dictAppFormLayoutExdto[layoutItemId];
                AppFormLayoutItemConverter.CopyDtoToEntity(appFormLayoutItemEntity, appFormLayoutItemExDto);

            }


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(appFormEntity);

                    adapter.UpdateEntitiesDirectly(new AppFormLayoutItemEntity() { UigridLayoutParentId = null }, new RelationPredicateBucket(AppFormLayoutItemFields.FormLayoutItemId == deletAppFormLayoutIDs));

                    adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.FormLayoutItemId == deletAppFormLayoutIDs));
                    //}

                    adapter.Commit();
                    UpdateLayoutParentKey((int)aAppFormExDto.Id);
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormExDto), "App_Form_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormExDto), "App_Form_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }


            }

            return aValidationResult;
        }


        private static void ConvertAppFormExDtoFlexUiLayoutToEntity(AppFormExDto aAppFormExDto, ObservableSet<AppFormLayoutItemExDto> layoutItemFlatList, AppFormEntity aAppFormEntity)
        {
            AppFormConverter.CopyDtoToEntity(aAppFormEntity, aAppFormExDto);

            foreach (AppFormLayoutItemExDto appFormLayoutItemExDto in layoutItemFlatList)
            {
                if (appFormLayoutItemExDto.DomAttribute != null && appFormLayoutItemExDto.DomAttribute.WidgetDisplayType.HasValue)
                {
                    //int displayType = appFormLayoutItemExDto.DomAttribute.WidgetDisplayType.Value;

                    //if (displayType == (int)EmAppFormLayoutItemType.NewItemAddButton)
                    //{
                    //    continue;
                    //}

                    //if (displayType == (int)EmAppFormLayoutItemType.LayoutRow)
                    //{
                    //    if (appFormLayoutItemExDto.AppFormLayoutItem_List.IsEmpty())
                    //    {
                    //        continue;
                    //    }
                    //    else if (appFormLayoutItemExDto.AppFormLayoutItem_List.Count == 1)
                    //    {
                    //        var rowItem = appFormLayoutItemExDto.AppFormLayoutItem_List.First();
                    //        if (rowItem.DomAttribute != null && rowItem.DomAttribute.WidgetDisplayType.HasValue
                    //            && rowItem.DomAttribute.WidgetDisplayType.Value == (int)EmAppFormLayoutItemType.NewItemAddButton)
                    //        {
                    //            continue;
                    //        }
                    //    }
                    //}



                    AppFormLayoutItemEntity aAppFormLayoutItemEntity = new AppFormLayoutItemEntity();
                    AppFormLayoutItemConverter.CopyDtoToEntity(aAppFormLayoutItemEntity, appFormLayoutItemExDto);

                    //aAppFormLayoutItemEntity.DomElementTag = appFormLayoutItemExDto.CurrentHostId;
                    //aAppFormLayoutItemEntity.DisplayTitle = appFormLayoutItemExDto.ParentHostId;

                    aAppFormEntity.AppFormLayoutItem.Add(aAppFormLayoutItemEntity);
                }
            }
        }

        private static void UpdateLayoutParentKey(int formId)
        {
            AppFormEntity savedFormEntity = AppFormBL.RetrieveOneAppFormEntity(formId);
            if (savedFormEntity != null)
            {
                Dictionary<string, int> dictLayoutItemHostIdAndId = savedFormEntity.AppFormLayoutItem.ToDictionary(o => o.CurrentHostId, o => o.FormLayoutItemId);
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {

                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");


                        EntityCollection<AppFormLayoutItemEntity> needToSaveEntitys = new EntityCollection<AppFormLayoutItemEntity>();
                        foreach (var layoutItemEntity in savedFormEntity.AppFormLayoutItem)
                        {
                            if (!string.IsNullOrWhiteSpace(layoutItemEntity.ParentHostId))
                            {
                                if (dictLayoutItemHostIdAndId.ContainsKey(layoutItemEntity.ParentHostId))
                                {
                                    layoutItemEntity.UigridLayoutParentId = dictLayoutItemHostIdAndId[layoutItemEntity.ParentHostId];

                                    adapter.SaveEntity(layoutItemEntity);
                                }
                            }
                        }




                        adapter.Commit();


                        //aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_FormEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        // aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_FormEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        // aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_FormEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }

            }




        }


        internal static void ConvertFormUiHiarachyLayoutItemsToFlatList(ObservableSet<AppFormLayoutItemExDto> hiarachyList, ObservableSet<AppFormLayoutItemExDto> faltList)
        {
            if (hiarachyList != null && faltList != null)
            {
                foreach (var layoutItemExDto in hiarachyList)
                {
                    faltList.Add(layoutItemExDto);
                    ConvertFormUiHiarachyLayoutItemsToFlatList(layoutItemExDto.AppFormLayoutItem_List, faltList);
                }

            }
        }

        private static void RemoveUiPlaceHolderRowsAndButtons(ObservableSet<AppFormLayoutItemExDto> faltList)
        {
            List<AppFormLayoutItemExDto> needToRemoveLayoutItems = new List<AppFormLayoutItemExDto>();

            foreach (var appFormLayoutItemExDto in faltList)
            {
                if (appFormLayoutItemExDto.DomAttribute != null && appFormLayoutItemExDto.DomAttribute.WidgetDisplayType.HasValue)
                {
                    int displayType = appFormLayoutItemExDto.DomAttribute.WidgetDisplayType.Value;

                    if (displayType == (int)EmAppFormLayoutItemType.NewItemAddButton)
                    {
                        needToRemoveLayoutItems.Add(appFormLayoutItemExDto);
                        continue;
                    }

                    if (displayType == (int)EmAppFormLayoutItemType.LayoutRow)
                    {
                        if (appFormLayoutItemExDto.AppFormLayoutItem_List.IsEmpty())
                        {
                            needToRemoveLayoutItems.Add(appFormLayoutItemExDto);
                            continue;
                        }
                        else if (appFormLayoutItemExDto.AppFormLayoutItem_List.Count == 1)
                        {
                            var rowItem = appFormLayoutItemExDto.AppFormLayoutItem_List.First();
                            if (rowItem.DomAttribute != null && rowItem.DomAttribute.WidgetDisplayType.HasValue
                                && rowItem.DomAttribute.WidgetDisplayType.Value == (int)EmAppFormLayoutItemType.NewItemAddButton)
                            {
                                needToRemoveLayoutItems.Add(appFormLayoutItemExDto);
                                continue;
                            }
                        }
                    }
                }
            }

            needToRemoveLayoutItems.ForAll(o => faltList.Remove(o));
        }

        //private static AppFormLayoutItemExDto ConvertFormFlxLayoutItemEntityToExDto(AppFormLayoutItemEntity layoutItemEntity)
        //{
        //    AppFormLayoutItemExDto appFormLayoutItemExDto = AppFormLayoutItemConverter.ConvertEntityToExDto(layoutItemEntity);

        //    foreach (var childLayoutItemEntity in layoutItemEntity.AppFormLayoutItem_)
        //    {
        //        AppFormLayoutItemExDto aChildLayoutItemExDto = ConvertFormFlxLayoutItemEntityToExDto(childLayoutItemEntity);
        //        appFormLayoutItemExDto.AppFormLayoutItem_List.Add(aChildLayoutItemExDto);
        //    }

        //    return appFormLayoutItemExDto;
        //}

        internal static void PrepareFlexFormLayoutItemExDtoTree(AppFormEntity aAppFormEntity, AppFormExDto aAppFormExDto, AppTransactionExDto transactionExDto)
        {
            foreach (var formLayoutEntity in aAppFormEntity.AppFormLayoutItem)
            {
                AppFormLayoutItemExDto appFormLayoutItemExDto = AppFormLayoutItemConverter.ConvertEntityToExDto(formLayoutEntity);
                aAppFormExDto.AppFormLayoutItemList.Add(appFormLayoutItemExDto);

                if (transactionExDto != null && transactionExDto.DictAllTransactionField != null && transactionExDto.DictAllTransactionUnitIdExDto != null)
                {
                    if (appFormLayoutItemExDto.TransactionFieldId.HasValue)
                    {
                        if (transactionExDto.DictAllTransactionField.ContainsKey(appFormLayoutItemExDto.TransactionFieldId.Value))
                        {
                            appFormLayoutItemExDto.ForeignAppTransactionFieldExDto = transactionExDto.DictAllTransactionField[appFormLayoutItemExDto.TransactionFieldId.Value];
                        }
                    }

                    if (appFormLayoutItemExDto.GridTransactionUnitId.HasValue)
                    {
                        if (transactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(appFormLayoutItemExDto.GridTransactionUnitId.Value.ToString()))
                        {
                            appFormLayoutItemExDto.ForeignAppTransactionUnitExDto = transactionExDto.DictAllTransactionUnitIdExDto[appFormLayoutItemExDto.GridTransactionUnitId.Value.ToString()];
                        }
                    }

                    else if (appFormLayoutItemExDto.DomAttribute != null && appFormLayoutItemExDto.DomAttribute.WidgetDisplayType.HasValue)
                    {
                        if (appFormLayoutItemExDto.DomAttribute.WidgetDisplayType.Value == (int)EmAppFormLayoutItemType.CommandActionButton)
                        {
                            if (transactionExDto.CommandActionList != null && appFormLayoutItemExDto.DomAttribute.CommandActionId.HasValue)
                            {
                                appFormLayoutItemExDto.BindToCommandAction = transactionExDto.CommandActionList.FirstOrDefault(o => (int)o.Id == appFormLayoutItemExDto.DomAttribute.CommandActionId.Value);

                                if (appFormLayoutItemExDto.BindToCommandAction != null)
                                {
                                    // appFormLayoutItemExDto.BindToCommandAction.ForeignAppTransactionExDto = transactionExDto;

                                    int? conditionFieldId = appFormLayoutItemExDto.BindToCommandAction.CommandConditionTransactionFieldId;
                                    if (conditionFieldId.HasValue)
                                    {
                                        if (transactionExDto.DictRootLevelUnitTransactionField != null && transactionExDto.DictRootLevelUnitTransactionField.ContainsKey(conditionFieldId.Value))
                                        {
                                            var conditionField = transactionExDto.DictRootLevelUnitTransactionField[conditionFieldId.Value];
                                            appFormLayoutItemExDto.BindToCommandAction.CommandConditionFieldDbFieldName = conditionField.DataBaseFieldName;
                                        }
                                    }
                                }
                            }
                        }
                        else if (appFormLayoutItemExDto.DomAttribute.WidgetDisplayType.Value == (int)EmAppFormLayoutItemType.LinkedSearch)
                        {
                            Dictionary<int, AppTransactionUnitLinkedSearchDto> dictRootLevelUnitLinkedSearchIdAndDto = new Dictionary<int, AppTransactionUnitLinkedSearchDto>();

                            transactionExDto.AppTransactionUnitList.Where(o => o.AppTransactionUnitLinkedSearchList != null)
                                .ForAll(o => o.AppTransactionUnitLinkedSearchList
                                .ForAll(p => dictRootLevelUnitLinkedSearchIdAndDto[(int)p.Id] = p));

                            int? linkedSearchId = appFormLayoutItemExDto.DomAttribute.LinkedSearchId;

                            if (linkedSearchId.HasValue && dictRootLevelUnitLinkedSearchIdAndDto.ContainsKey(linkedSearchId.Value))
                            {
                                appFormLayoutItemExDto.BindToLinkedSearch = dictRootLevelUnitLinkedSearchIdAndDto[linkedSearchId.Value];
                            }
                        }
                    }
                }
            }


            aAppFormExDto.AppFormLayoutItemList = ConvertFormLayoutItemsFlatListToHiarachyTree(aAppFormExDto.AppFormLayoutItemList);
        }

        private static ObservableSet<AppFormLayoutItemExDto> ConvertFormLayoutItemsFlatListToHiarachyTree(ObservableSet<AppFormLayoutItemExDto> appFormLayoutItemList)
        {
            List<AppFormLayoutItemExDto> rootItems = appFormLayoutItemList.Where(o => !o.UigridLayoutParentId.HasValue).ToList();

            foreach (var rootItem in rootItems)
            {
                ProcessChilds(appFormLayoutItemList, rootItem);
            }

            ObservableSet<AppFormLayoutItemExDto> toReturn = new ObservableSet<AppFormLayoutItemExDto>();
            rootItems.ForAll(o => toReturn.Add(o));

            return toReturn;
        }

        private static void ProcessChilds(IEnumerable<AppFormLayoutItemExDto> appFormLayoutItemList, AppFormLayoutItemExDto layoutItemDto)
        {
            ObservableSet<AppFormLayoutItemExDto> childItems = new ObservableSet<AppFormLayoutItemExDto>();
            appFormLayoutItemList.Where(o => o.UigridLayoutParentId.HasValue && o.UigridLayoutParentId.Value == (int)layoutItemDto.Id).ForAll(o => childItems.Add(o));

            if (!childItems.IsEmpty())
            {
                layoutItemDto.AppFormLayoutItem_List = childItems;
                layoutItemDto.AppFormLayoutItem_List.ForAll(c =>
                {
                    ProcessChilds(appFormLayoutItemList, c);
                });

            }
        }



        private static bool FlexFormPublishValidation_ValidateOneLayoutItem(ObservableSet<AppFormLayoutItemExDto> layoutItemFlatList, AppFormLayoutItemExDto layoutItemExDto,
            Dictionary<string, Dictionary<string, string>> dictLayoutItemHostIdAndPropertyErrorMessage, List<DatabaseTableDto> dbTableDtoList)
        {
            layoutItemExDto.IsInvalid = false;

            if (layoutItemExDto.DomAttribute != null && layoutItemExDto.DomAttribute.IsBindingToDataField)
            {

                bool isRootLevelField = layoutItemExDto.DomAttribute.TranscationUnitLevel.HasValue && layoutItemExDto.DomAttribute.TranscationUnitLevel == 1;
                bool isGrid = layoutItemExDto.DomAttribute.TranscationUnitLevel.HasValue && layoutItemExDto.DomAttribute.TranscationUnitLevel > 1;
                bool isGridColumn = false;
                AppFormLayoutItemExDto columnParentGridItem = null;

                if (!string.IsNullOrWhiteSpace(layoutItemExDto.ParentHostId))
                {
                    var parentItem = layoutItemFlatList.FirstOrDefault(o => o.CurrentHostId == layoutItemExDto.ParentHostId);
                    if (parentItem != null && parentItem.DomAttribute.TranscationUnitLevel.HasValue && parentItem.DomAttribute.TranscationUnitLevel.Value > 1)
                    {
                        columnParentGridItem = parentItem;
                        isGridColumn = true;
                    }
                }

                string validateProperty = "DisplayName";

                if (isRootLevelField)
                {
                    if (string.IsNullOrWhiteSpace(layoutItemExDto.DomAttribute.DisplayName))
                    {
                        layoutItemExDto.IsInvalid = true;
                        Dictionary<string, string> dictPropertyNameAndErrorMessage = new Dictionary<string, string>();
                        dictPropertyNameAndErrorMessage[validateProperty] = "Label text cannot be empty.";
                        dictLayoutItemHostIdAndPropertyErrorMessage.Add(layoutItemExDto.CurrentHostId, dictPropertyNameAndErrorMessage);
                    }
                    else if (layoutItemExDto.DomAttribute.DisplayName.ToLower().Contains("new field"))
                    {
                        layoutItemExDto.IsInvalid = true;
                        Dictionary<string, string> dictPropertyNameAndErrorMessage = new Dictionary<string, string>();
                        dictPropertyNameAndErrorMessage[validateProperty] = "Please change the label text from " + layoutItemExDto.DomAttribute.DisplayName;
                        dictLayoutItemHostIdAndPropertyErrorMessage.Add(layoutItemExDto.CurrentHostId, dictPropertyNameAndErrorMessage);
                    }
                    else
                    {
                        var duplicateItem = layoutItemFlatList.FirstOrDefault(o => o.DomAttribute.TranscationUnitLevel.HasValue && o.DomAttribute.TranscationUnitLevel.Value == 1
                                && o.CurrentHostId != layoutItemExDto.CurrentHostId
                                && AppFromDataModelBL.FilterSQLDBInvalidChar(o.DomAttribute.DisplayName.Trim().ToLower())
                                == AppFromDataModelBL.FilterSQLDBInvalidChar(layoutItemExDto.DomAttribute.DisplayName.Trim().ToLower()));

                        if (duplicateItem != null)
                        {
                            layoutItemExDto.IsInvalid = true;
                            Dictionary<string, string> dictPropertyNameAndErrorMessage = new Dictionary<string, string>();
                            dictPropertyNameAndErrorMessage[validateProperty] = "Duplicate label text found. Please change the label text from " + layoutItemExDto.DomAttribute.DisplayName;
                            dictLayoutItemHostIdAndPropertyErrorMessage.Add(layoutItemExDto.CurrentHostId, dictPropertyNameAndErrorMessage);
                        }
                    }
                }
                else if (isGrid)
                {
                    if (string.IsNullOrWhiteSpace(layoutItemExDto.DomAttribute.DisplayName))
                    {
                        layoutItemExDto.IsInvalid = true;
                        Dictionary<string, string> dictPropertyNameAndErrorMessage = new Dictionary<string, string>();
                        dictPropertyNameAndErrorMessage[validateProperty] = "Grid label text cannot be empty.";
                        dictLayoutItemHostIdAndPropertyErrorMessage.Add(layoutItemExDto.CurrentHostId, dictPropertyNameAndErrorMessage);
                    }
                    else if (layoutItemExDto.DomAttribute.DisplayName.ToLower().Contains("new grid") || layoutItemExDto.DomAttribute.DisplayName.ToLower().Contains("new subgrid"))
                    {
                        layoutItemExDto.IsInvalid = true;
                        Dictionary<string, string> dictPropertyNameAndErrorMessage = new Dictionary<string, string>();
                        dictPropertyNameAndErrorMessage[validateProperty] = "Please change the grid label text from " + layoutItemExDto.DomAttribute.DisplayName;
                        dictLayoutItemHostIdAndPropertyErrorMessage.Add(layoutItemExDto.CurrentHostId, dictPropertyNameAndErrorMessage);
                    }
                    else
                    {
                        //string childTableMiddleName = AppFromDataModelBL.FilterSQLDBInvalidChar(layoutItemExDto.DomAttribute.DisplayName);
                        //string childTablename = "UDF_Detail_" + childTableMiddleName + "_" + ServerContext.Instance.CurrentUid;

                        //var duplicateDbTable = dbTableDtoList.FirstOrDefault(o => AppFromDataModelBL.FilterSQLDBInvalidChar(o.Name.ToLower())
                        //== AppFromDataModelBL.FilterSQLDBInvalidChar(childTablename.Trim().ToLower()));

                        //if (duplicateDbTable != null)
                        //{
                        //    layoutItemExDto.IsInvalid = true;
                        //    Dictionary<string, string> dictPropertyNameAndErrorMessage = new Dictionary<string, string>();
                        //    dictPropertyNameAndErrorMessage[validateProperty] = "Please change the grid label text from " + layoutItemExDto.DomAttribute.DisplayName + ". It conflicts with an existing database table name. ";
                        //    dictLayoutItemHostIdAndPropertyErrorMessage.Add(layoutItemExDto.CurrentHostId, dictPropertyNameAndErrorMessage);
                        //}
                        //else
                        //{
                        //    var duplicateItem = layoutItemFlatList.FirstOrDefault(o => o.DomAttribute.TranscationUnitLevel.HasValue && o.DomAttribute.TranscationUnitLevel.Value > 1
                        //            && o.CurrentHostId != layoutItemExDto.CurrentHostId
                        //            && AppFromDataModelBL.FilterSQLDBInvalidChar(o.DomAttribute.DisplayName.Trim().ToLower())
                        //            == AppFromDataModelBL.FilterSQLDBInvalidChar(layoutItemExDto.DomAttribute.DisplayName.Trim().ToLower()));

                        //    if (duplicateItem != null)
                        //    {
                        //        layoutItemExDto.IsInvalid = true;
                        //        Dictionary<string, string> dictPropertyNameAndErrorMessage = new Dictionary<string, string>();
                        //        dictPropertyNameAndErrorMessage[validateProperty] = "Duplicate grid name found. Please change the grid label text from " + layoutItemExDto.DomAttribute.DisplayName;
                        //        dictLayoutItemHostIdAndPropertyErrorMessage.Add(layoutItemExDto.CurrentHostId, dictPropertyNameAndErrorMessage);
                        //    }
                        //}
                    }

                }
                else if (isGridColumn)
                {
                    if (string.IsNullOrWhiteSpace(layoutItemExDto.DomAttribute.DisplayName))
                    {
                        layoutItemExDto.IsInvalid = true;
                        Dictionary<string, string> dictPropertyNameAndErrorMessage = new Dictionary<string, string>();
                        dictPropertyNameAndErrorMessage[validateProperty] = "Column name cannot be empty.";
                        dictLayoutItemHostIdAndPropertyErrorMessage.Add(layoutItemExDto.CurrentHostId, dictPropertyNameAndErrorMessage);
                    }
                    else
                    {
                        Match match = Regex.Match(layoutItemExDto.DomAttribute.DisplayName.Trim().Replace(" ", ""), @"Column([0-9\-]+)", RegexOptions.IgnoreCase);

                        if (match.Success)
                        {
                            layoutItemExDto.IsInvalid = true;
                            Dictionary<string, string> dictPropertyNameAndErrorMessage = new Dictionary<string, string>();
                            dictPropertyNameAndErrorMessage[validateProperty] = "Please rename the column " + layoutItemExDto.DomAttribute.DisplayName;
                            dictLayoutItemHostIdAndPropertyErrorMessage.Add(layoutItemExDto.CurrentHostId, dictPropertyNameAndErrorMessage);
                        }
                        else
                        {
                            var duplicateItem = columnParentGridItem.AppFormLayoutItem_List.FirstOrDefault(o => o.CurrentHostId != layoutItemExDto.CurrentHostId
                                && AppFromDataModelBL.FilterSQLDBInvalidChar(o.DomAttribute.DisplayName.Trim().ToLower())
                                == AppFromDataModelBL.FilterSQLDBInvalidChar(layoutItemExDto.DomAttribute.DisplayName.Trim().ToLower()));



                            if (duplicateItem != null)
                            {
                                layoutItemExDto.IsInvalid = true;
                                Dictionary<string, string> dictPropertyNameAndErrorMessage = new Dictionary<string, string>();
                                dictPropertyNameAndErrorMessage[validateProperty] = "Duplicate column name found. Please rename the column " + layoutItemExDto.DomAttribute.DisplayName;
                                dictLayoutItemHostIdAndPropertyErrorMessage.Add(layoutItemExDto.CurrentHostId, dictPropertyNameAndErrorMessage);
                            }
                        }
                    }

                }
            }

            if (layoutItemExDto.AppFormLayoutItem_List != null)
            {
                foreach (var childItemExDto in layoutItemExDto.AppFormLayoutItem_List)
                {
                    bool isChildItemInvalid = FlexFormPublishValidation_ValidateOneLayoutItem(layoutItemFlatList, childItemExDto, dictLayoutItemHostIdAndPropertyErrorMessage, dbTableDtoList);
                    layoutItemExDto.IsInvalid = (layoutItemExDto.IsInvalid || isChildItemInvalid);
                }
            }

            return layoutItemExDto.IsInvalid;
        }


        private static Dictionary<int, List<AppTransactionFieldExDto>> PrepareColumnAndFieldsDictionary(AppTransactionExDto transactionExDto, int columns)
        {
            Dictionary<int, List<AppTransactionFieldExDto>> dictColAndFieldList = new Dictionary<int, List<AppTransactionFieldExDto>>();

            Dictionary<int, int> dictColAndTotalHeight = new Dictionary<int, int>();

            List<AppTransactionFieldExDto> rootLevelFields = transactionExDto.DictRootLevelUnitTransactionField.Values.Where(o => o.IsVisible.HasValue && o.IsVisible.Value).ToList();

            int nbRootField = rootLevelFields.Count;


            var imageFieldList = rootLevelFields.Where(o => o.ControlType == (int)EmAppControlType.Image).ToList();
            var videoOrMapFieldList = rootLevelFields.Where(o => o.ControlType == (int)EmAppControlType.Video || o.ControlType == (int)EmAppControlType.GoogleMap).ToList();
            var memoFieldList = rootLevelFields.Where(o => o.ControlType == (int)EmAppControlType.Memo).ToList();
            var regularFieldList = rootLevelFields.Where(o => o.ControlType != (int)EmAppControlType.Image
                                            && o.ControlType != (int)EmAppControlType.Video
                                            && o.ControlType != (int)EmAppControlType.GoogleMap
                                            && o.ControlType != (int)EmAppControlType.Memo).ToList();

            var regularAndMemoFieldList = rootLevelFields.Where(o => o.ControlType != (int)EmAppControlType.Image
                                            && o.ControlType != (int)EmAppControlType.Video
                                            && o.ControlType != (int)EmAppControlType.GoogleMap).ToList();



            int totalImgFieldHeight = imageFieldList.Count * (_imageItemHeight + 8);
            int totalVideoOrMapFieldHeight = videoOrMapFieldList.Count * (_videoItemHeight + 8);
            int totalMemoFieldHeight = memoFieldList.Count * (_memoItemHeight + 8);
            int totalRegularFieldHeight = regularFieldList.Count * (_regularItemHeight + 4);

            int totalHeight = totalImgFieldHeight + totalVideoOrMapFieldHeight + totalMemoFieldHeight + totalRegularFieldHeight;

            int averageHeightPerCol = totalHeight / columns;



            int totalLargeFieldHeight = totalImgFieldHeight + totalVideoOrMapFieldHeight;

            int nbColWithLargeField = (int)Math.Ceiling(totalLargeFieldHeight * 1.0 / averageHeightPerCol);

            for (int col = 1; col <= columns; col++)
            {
                List<AppTransactionFieldExDto> colFiels = new List<AppTransactionFieldExDto>();
                dictColAndFieldList.Add(col, colFiels);
            }


            List<AppTransactionFieldExDto> largeFieldList = new List<AppTransactionFieldExDto>();
            largeFieldList.AddRange(videoOrMapFieldList);
            largeFieldList.AddRange(imageFieldList);

            int remainLargeFieldCount = largeFieldList.Count;

            while (remainLargeFieldCount > 0)
            {
                var fieldDto = largeFieldList[0];
                largeFieldList.Remove(fieldDto);

                int largeFieldColumnIndex = columns - (remainLargeFieldCount % nbColWithLargeField);
                var colFieldList = dictColAndFieldList[largeFieldColumnIndex];
                colFieldList.Add(fieldDto);
                remainLargeFieldCount = largeFieldList.Count;
            }

            List<AppTransactionFieldExDto> smallFieldList = new List<AppTransactionFieldExDto>();
            smallFieldList.AddRange(regularAndMemoFieldList);

            int remainSmallFieldCount = smallFieldList.Count;
            int colIndex = 1;

            while (remainSmallFieldCount > 0 && colIndex <= columns)
            {
                var colFieldList = dictColAndFieldList[colIndex];
                int totalColHeight = CalculateFieldListTotalHeight(colFieldList);

                if (totalColHeight < averageHeightPerCol)
                {
                    var fieldDto = smallFieldList[0];
                    smallFieldList.Remove(fieldDto);
                    colFieldList.Add(fieldDto);
                    remainSmallFieldCount = smallFieldList.Count;
                }
                else
                {
                    colIndex++;
                }
            }

            if (remainSmallFieldCount > 0)
            {
                while (remainSmallFieldCount > 0)
                {

                    int? colIndexFound = FindSmallestHeightColumnIndex(dictColAndFieldList);
                    if (colIndexFound.HasValue)
                    {
                        colIndexFound = 1;
                    }

                    var colFieldList = dictColAndFieldList[colIndexFound.Value];

                    var fieldDto = smallFieldList[0];
                    smallFieldList.Remove(fieldDto);
                    colFieldList.Add(fieldDto);
                    remainSmallFieldCount = smallFieldList.Count;
                }
            }

            return dictColAndFieldList;
        }


        private static int? FindSmallestHeightColumnIndex(Dictionary<int, List<AppTransactionFieldExDto>> dictColAndFieldList)
        {
            int? smallestHeightColIndex = null;

            if (dictColAndFieldList != null && dictColAndFieldList.Keys.Count > 0)
            {
                int? smallestHeight = null;

                for (int i = 1; i <= dictColAndFieldList.Keys.Count; i++)
                {
                    if (dictColAndFieldList.ContainsKey(i))
                    {
                        var fieldList = dictColAndFieldList[i];

                        if (fieldList != null)
                        {
                            int colHeight = CalculateFieldListTotalHeight(fieldList);

                            if (!smallestHeight.HasValue || smallestHeight.Value > colHeight)
                            {
                                smallestHeight = colHeight;
                                smallestHeightColIndex = i;
                            }
                        }
                    }
                }
            }

            return smallestHeightColIndex;
        }

        private static int CalculateFieldListTotalHeight(List<AppTransactionFieldExDto> fieldList)
        {
            var imageFieldList = fieldList.Where(o => o.ControlType == (int)EmAppControlType.Image).ToList();
            var videoOrMapFieldList = fieldList.Where(o => o.ControlType == (int)EmAppControlType.Video || o.ControlType == (int)EmAppControlType.GoogleMap).ToList();
            var memoFieldList = fieldList.Where(o => o.ControlType == (int)EmAppControlType.Memo).ToList();
            var regularFieldList = fieldList.Where(o => o.ControlType != (int)EmAppControlType.Image
                                            && o.ControlType != (int)EmAppControlType.Video
                                            && o.ControlType != (int)EmAppControlType.GoogleMap
                                            && o.ControlType != (int)EmAppControlType.Memo).ToList();


            int totalImgFieldHeight = imageFieldList.Count * (_imageItemHeight + 8);
            int totalVideoOrMapFieldHeight = videoOrMapFieldList.Count * (_videoItemHeight + 8);
            int totalMemoFieldHeight = memoFieldList.Count * (_memoItemHeight + 8);
            int totalRegularFieldHeight = regularFieldList.Count * (_regularItemHeight + 4);

            int totalHeight = totalImgFieldHeight + totalVideoOrMapFieldHeight + totalMemoFieldHeight + totalRegularFieldHeight;

            return totalHeight;
        }



        private static AppFormLayoutItemExDto AppendNewLayoutRow(Dictionary<string, AppFormLayoutItemExDto> dictUiIdAndLayoutItem, AppFormExDto aAppFormExDto, AppFormLayoutItemExDto parentSection)
        {
            AppFormLayoutItemExDto layoutRow = AppdenDefaultLayoutItem(dictUiIdAndLayoutItem, aAppFormExDto, parentSection);
            layoutRow.DomAttribute.WidgetDisplayType = (int)EmAppFormLayoutItemType.LayoutRow;
            return layoutRow;
        }

        private static AppFormLayoutItemExDto AppendNewStackContainer(Dictionary<string, AppFormLayoutItemExDto> dictUiIdAndLayoutItem, AppFormExDto aAppFormExDto, AppFormLayoutItemExDto parentSection, int colSpan = 24, int innerCols = 1)
        {
            AppFormLayoutItemExDto layoutItem = AppdenDefaultLayoutItem(dictUiIdAndLayoutItem, aAppFormExDto, parentSection);

            layoutItem.DomAttribute.ColSpanValue = colSpan;
            layoutItem.DomAttribute.DisplayName = "Stack Container";
            layoutItem.DomAttribute.WidgetDisplayType = (int)EmAppFormLayoutItemType.Section;
            layoutItem.DomAttribute.InlineStyle = "";
            layoutItem.DomAttribute.BackgroundColor = "#ffffff";
            layoutItem.DomAttribute.TextColor = "#000000";
            layoutItem.DomAttribute.IsBindingToDataField = false;
            layoutItem.DomAttribute.DefaultNbColumns = innerCols;

            dictUiIdAndLayoutItem.Add(layoutItem.CurrentHostId, layoutItem);

            return layoutItem;
        }

        private static AppFormLayoutItemExDto AppendNewTabContainer(Dictionary<string, AppFormLayoutItemExDto> dictUiIdAndLayoutItem, AppFormExDto aAppFormExDto, AppFormLayoutItemExDto parentSection, int colSpan = 24, int innerCols = 1)
        {
            AppFormLayoutItemExDto layoutItem = AppdenDefaultLayoutItem(dictUiIdAndLayoutItem, aAppFormExDto, parentSection);

            layoutItem.DomAttribute.ColSpanValue = colSpan;
            layoutItem.DomAttribute.DisplayName = "Tab Container";
            layoutItem.DomAttribute.WidgetDisplayType = (int)EmAppFormLayoutItemType.TabContainer;
            layoutItem.DomAttribute.InlineStyle = "";
            layoutItem.DomAttribute.BackgroundColor = "#ffffff";
            layoutItem.DomAttribute.TextColor = "#000000";
            layoutItem.DomAttribute.IsBindingToDataField = false;
            layoutItem.DomAttribute.DefaultNbColumns = innerCols;

            dictUiIdAndLayoutItem.Add(layoutItem.CurrentHostId, layoutItem);

            return layoutItem;
        }

        private static AppFormLayoutItemExDto AppendNewTab(Dictionary<string, AppFormLayoutItemExDto> dictUiIdAndLayoutItem, AppFormExDto aAppFormExDto, AppFormLayoutItemExDto parentSection, int colSpan = 24, int innerCols = 1)
        {
            AppFormLayoutItemExDto layoutItem = AppdenDefaultLayoutItem(dictUiIdAndLayoutItem, aAppFormExDto, parentSection);

            layoutItem.DomAttribute.DisplayName = "Tab";
            layoutItem.DomAttribute.WidgetDisplayType = (int)EmAppFormLayoutItemType.Section;
            layoutItem.DomAttribute.InlineStyle = "";
            layoutItem.DomAttribute.BackgroundColor = "#ffffff";
            layoutItem.DomAttribute.TextColor = "#000000";
            layoutItem.DomAttribute.IsBindingToDataField = false;
            layoutItem.DomAttribute.DefaultNbColumns = innerCols;

            dictUiIdAndLayoutItem.Add(layoutItem.CurrentHostId, layoutItem);

            return layoutItem;
        }

        private static AppFormLayoutItemExDto AppendNewTransChildUnitGridItem(Dictionary<string, AppFormLayoutItemExDto> dictUiIdAndLayoutItem, AppFormExDto aAppFormExDto, AppFormLayoutItemExDto parentSection, AppTransactionUnitExDto gridUnitDto, int colSpan = 24)
        {
            AppFormLayoutItemExDto layoutItem = AppdenDefaultLayoutItem(dictUiIdAndLayoutItem, aAppFormExDto, parentSection);

            layoutItem.GridTransactionUnitId = (int)gridUnitDto.Id;
            layoutItem.DomAttribute.ColSpanValue = colSpan;
            layoutItem.DomAttribute.DisplayName = gridUnitDto.UnitDisplayName;
            layoutItem.DomAttribute.WidgetDisplayType = (int)EmAppFormLayoutItemType.Grid;
            layoutItem.DomAttribute.InlineStyle = "";
            layoutItem.DomAttribute.BackgroundColor = "#ffffff";
            layoutItem.DomAttribute.TextColor = "#000000";
            layoutItem.DomAttribute.IsBindingToDataField = true;
            layoutItem.DomAttribute.HeightValue = 400; //258
            layoutItem.DomAttribute.TranscationUnitLevel = 2;

            dictUiIdAndLayoutItem.Add(layoutItem.CurrentHostId, layoutItem);

            return layoutItem;
        }

        private static AppFormLayoutItemExDto AppendNewTransFieldItem(Dictionary<string, AppFormLayoutItemExDto> dictUiIdAndLayoutItem, AppFormExDto aAppFormExDto, AppFormLayoutItemExDto parentSection, AppTransactionFieldExDto fieldDto, int colSpan = 24)
        {
            AppFormLayoutItemExDto layoutItem = AppdenDefaultLayoutItem(dictUiIdAndLayoutItem, aAppFormExDto, parentSection);

            layoutItem.TransactionFieldId = (int)fieldDto.Id;
            layoutItem.DomAttribute.ColSpanValue = colSpan;
            layoutItem.DomAttribute.DisplayName = fieldDto.DisplayName;
            layoutItem.DomAttribute.WidgetDisplayType = fieldDto.ControlType;
            layoutItem.DomAttribute.InlineStyle = "";
            layoutItem.DomAttribute.BackgroundColor = "#ffffff";
            layoutItem.DomAttribute.TextColor = "#000000";
            layoutItem.DomAttribute.IsBindingToDataField = true;

            if (fieldDto.ControlType == (int)EmAppFormLayoutItemType.Memo)
            {
                layoutItem.DomAttribute.HeightValue = _memoItemHeight; //158
            }
            else if (fieldDto.ControlType == (int)EmAppFormLayoutItemType.GoogleMap)
            {
                layoutItem.DomAttribute.HeightValue = _videoItemHeight; //408
            }
            else if (fieldDto.ControlType == (int)EmAppFormLayoutItemType.Video || fieldDto.ControlType == (int)EmAppFormLayoutItemType.YoutubeVideo)
            {
                layoutItem.DomAttribute.HeightValue = _videoItemHeight; // 408
            }
            else if (fieldDto.ControlType == (int)EmAppFormLayoutItemType.Image || fieldDto.ControlType == (int)EmAppFormLayoutItemType.ExternalImageUrl)
            {
                layoutItem.DomAttribute.HeightValue = _imageItemHeight; //258
            }
            else // 34
            {

            }

            dictUiIdAndLayoutItem.Add(layoutItem.CurrentHostId, layoutItem);

            return layoutItem;
        }


        private static AppFormLayoutItemExDto AppdenDefaultLayoutItem(Dictionary<string, AppFormLayoutItemExDto> dictUiIdAndLayoutItem, AppFormExDto aAppFormExDto, AppFormLayoutItemExDto parentSection)
        {
            ObservableSet<AppFormLayoutItemExDto> parentlayoutItemList = GetParentLayoutItemList(aAppFormExDto, parentSection);
            int maxSort = GetLayoutItemMaxSort(parentlayoutItemList);
            AppFormLayoutItemExDto layoutItem = new AppFormLayoutItemExDto();
            layoutItem.FlowOrGridLayoutSortOrder = maxSort + 1;

            layoutItem.DomAttribute = new AppFormDomAttributeDto();
            layoutItem.CurrentHostId = ExtensionMethodhelper.RandomId();
            layoutItem.ParentHostId = parentSection != null ? parentSection.CurrentHostId : null;

            layoutItem.AppFormLayoutItem_List = new ObservableSet<AppFormLayoutItemExDto>();

            parentlayoutItemList.Add(layoutItem);
            return layoutItem;
        }

        private static ObservableSet<AppFormLayoutItemExDto> GetParentLayoutItemList(AppFormExDto aAppFormExDto, AppFormLayoutItemExDto parentSection)
        {
            ObservableSet<AppFormLayoutItemExDto> layoutRowList = null;

            if (parentSection != null)
            {

                if (parentSection.AppFormLayoutItem_List == null)
                {
                    parentSection.AppFormLayoutItem_List = new ObservableSet<AppFormLayoutItemExDto>();
                }

                layoutRowList = parentSection.AppFormLayoutItem_List;
            }
            else
            {
                if (aAppFormExDto.AppFormLayoutItemList == null)
                {
                    aAppFormExDto.AppFormLayoutItemList = new ObservableSet<AppFormLayoutItemExDto>();
                }


                layoutRowList = aAppFormExDto.AppFormLayoutItemList;
            }

            return layoutRowList;
        }

        private static int GetLayoutItemMaxSort(ObservableSet<AppFormLayoutItemExDto> layoutRowList)
        {
            int maxSort = 0;

            var layoutSortedRows = layoutRowList.Where(o => o.FlowOrGridLayoutSortOrder.HasValue).ToList();

            if (layoutSortedRows.Count > 0)
            {
                maxSort = layoutSortedRows.Max(o => o.FlowOrGridLayoutSortOrder.Value);
            }

            return maxSort;
        }

        private static int BuildAppFormDefaultLayout_SetMainLayout(AppFormExDto aAppFormExDto, AppTransactionExDto transactionExDto)
        {
            var rootFieldList = transactionExDto.DictRootLevelUnitTransactionField.Values.Where(o => o.IsVisible.HasValue && o.IsVisible.Value).ToList();
            int nbRootField = rootFieldList.Count;
            int nbChildGrid = transactionExDto.AppTransactionUnitList.FirstOrDefault().Children.Count; ;

            int columns = 2;

            int nbFiledPerCol = 6;

            if (nbRootField <= nbFiledPerCol)
            {
                columns = 1;
            }
            else if (nbRootField <= nbFiledPerCol * 2)
            {
                columns = 2;
            }
            else if (nbRootField <= nbFiledPerCol * 3)
            {
                columns = 3;
            }
            else
            {
                columns = 4;
            }


            int formWidth = 1000;

            if (columns == 1)
            {
                formWidth = 600;
            }
            else if (columns == 2)
            {
                formWidth = 1000;
            }
            else if (columns == 3)
            {
                formWidth = 1200;
            }
            else if (columns == 4)
            {
                formWidth = 1600;
            }
            else
            {
                formWidth = 1900;
            }

            aAppFormExDto.DefaultNbColumns = columns;
            aAppFormExDto.DefaultWidth = formWidth.ToString();
            return columns;
        }

    }
}