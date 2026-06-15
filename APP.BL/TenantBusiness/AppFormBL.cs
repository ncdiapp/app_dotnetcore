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

using APP.Framework;
namespace App.BL
{
    public static class AppFormBL
    {
        public static readonly string App_FormEntity_Save_OK = "App_FormEntity_Save_OK";
        public static readonly string App_FormEntity_Save_Failed = "App_FormEntity_Save_Failed";
        public static readonly string App_FormEntityUILayout_Save_OK = "App_FormEntityUILayout_Save_OK";
        public static readonly string App_FormEntityUILayout_Save_Failed = "App_FormEntityUILayout_Save_Failed";
        public static readonly string App_FormEntity_Delete_Ok = "App_FormEntity_Delete_Ok";
        public static readonly string App_FormEntity_Delete_Failed = "App_FormEntity_Delete_Failed";


        public static List<AppFormDto> RetrieveAllAppFormDtoByCreateFromType(EmAppFormCreationFrom? creationFrom, bool includePrintForm = false)
        {
            List<AppFormDto> toReturn = new List<AppFormDto>();

            EntityCollection<AppFormEntity> list = new EntityCollection<AppFormEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket filter = creationFrom.HasValue
                    ? new RelationPredicateBucket(AppFormFields.FormScope == creationFrom.Value)
                    : null;
                adapter.FetchEntityCollection(list, filter);
            }

            foreach (var entity in list)
            {
                var dto = AppFormConverter.ConvertEntityToDto(entity);

                if (dto.IsPrintForm)
                {
                    if (includePrintForm)
                    {
                        toReturn.Add(dto);
                    }
                }
                else
                {
                    toReturn.Add(dto);
                }
            }

            return toReturn;

        }


        public static List<AppFormDto> RetrieveSaasApplicationFormList(int? applicationId)
        {
            List<AppFormDto> toReturn = new List<AppFormDto>();

            if (applicationId.HasValue)
            {
                List<AppFormDto> allFormList = RetrieveAllAppFormDtoByCreateFromType(null);

                List<int> appliationFormIdList = AppTransactionBL.RetrieveSaasApplicationTransactionList(applicationId).Where(o => o.FormId.HasValue && !(o.BusinessScopeId.HasValue && o.BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation)).Select(o => o.FormId.Value).Distinct().ToList();
                List<int> appliationPrintFormIdList = AppTransactionBL.RetrieveSaasApplicationTransactionList(applicationId).Where(o => o.PrintFormId.HasValue && !(o.BusinessScopeId.HasValue && o.BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation)).Select(o => o.PrintFormId.Value).Distinct().ToList();

                toReturn = allFormList.Where(o => (o.SaasApplicationId.HasValue && o.SaasApplicationId.Value == applicationId.Value) || appliationFormIdList.Contains((int)o.Id) || appliationPrintFormIdList.Contains((int)o.Id)).ToList();

            }

            return toReturn;
        }



        //public static AppFormExDto CreateNewTranactionForm(int transactionId, bool? isPrintForm)
        //{
        //    AppFormExDto newAppFormExDto = new AppFormExDto();
        //    newAppFormExDto.Name = "New Form";

        //    OperationCallResult<AppFormExDto> result = AppFormBL.SaveAppFormExDto(newAppFormExDto);

        //    AppFormExDto savedAppFormExDto = result.Object;

        //    int? formId = savedAppFormExDto.Id as int?;



        //    AppTransactionEntity updateAppTransactionEntity = new AppTransactionEntity();

        //    if (isPrintForm.HasValue && isPrintForm.Value)
        //    {
        //        updateAppTransactionEntity.SharedTransactionId = savedAppFormExDto.Id as int?; //PrintFormId
        //    }
        //    else
        //    {
        //        updateAppTransactionEntity.FormId = formId as int?;
        //    }

        //    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //    {

        //        adapter.UpdateEntitiesDirectly(updateAppTransactionEntity, new RelationPredicateBucket(AppTransactionFields.TransactionId == transactionId));


        //    }

        //    result.Object.AssociatedTransactionId = transactionId;
        //    // aAppFormExDto.Id = transactionId;

        //    return result.Object;
        //}

        public static AppFormExDto CreateNewTranactionForm(int transactionId, int? formLayoutType = null, int? saasApplicationId = null, bool isPrintForm = false)
        {
            if (!formLayoutType.HasValue)
            {
                formLayoutType = (int)EmAppFormLayoutType.Flex;
            }

            AppTransactionEntity transactionEntity = AppTransactionBL.RetrieveOneAppTransactionSimpleEntity(transactionId);


            AppFormExDto newAppFormExDto = new AppFormExDto();
            newAppFormExDto.Name = "New Form";
            newAppFormExDto.LayoutType = formLayoutType;
            newAppFormExDto.SaasApplicationId = saasApplicationId;
            newAppFormExDto.RouteParamter3 = isPrintForm.ToString();

            if (transactionEntity != null)
            {
                newAppFormExDto.Name = transactionEntity.TransactionName;
            }

            OperationCallResult<AppFormExDto> result = AppFormBL.SaveAppFormExDto(newAppFormExDto);

            AppFormExDto savedAppFormExDto = result.Object;

            int? formId = savedAppFormExDto.Id as int?;



            AppTransactionEntity updateAppTransactionEntity = new AppTransactionEntity();






            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                var exdto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

                if (isPrintForm)
                {
                    updateAppTransactionEntity.PrintFormId = formId as int?;
                    adapter.UpdateEntitiesDirectly(updateAppTransactionEntity, new RelationPredicateBucket(AppTransactionFields.TransactionId == transactionId));
                    exdto.PrintFormId = formId;
                }
                else
                {
                    updateAppTransactionEntity.FormId = formId as int?;
                    adapter.UpdateEntitiesDirectly(updateAppTransactionEntity, new RelationPredicateBucket(AppTransactionFields.TransactionId == transactionId));
                    exdto.FormId = formId;
                }



                //if (saasApplicationId.HasValue)
                //{
                //    AppApplicationAssetsItemExDto formAssetsItemDto = new AppApplicationAssetsItemExDto();
                //    formAssetsItemDto.ApplicationId = saasApplicationId;
                //    formAssetsItemDto.FormId = formId;
                //    AppSaasUserApplicationPackageBL.SaveNewApplicationAssetsItemExDto(formAssetsItemDto);
                //}
            }

            result.Object.AssociatedTransactionId = transactionId;
            // aAppFormExDto.Id = transactionId;

            return result.Object;
        }

        //public static AppFormExDto SetFormDefaultLayout(int transactionId, int nbColumns)
        //{

        //    AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
        //    if (!transactionExDto.FormId.HasValue)
        //    {

        //        AppFormExDto newForm = CreateNewTranactionForm(transactionId);
        //        Dictionary<int, AppTransactionFieldExDto> dictRootLevelField = new Dictionary<int, AppTransactionFieldExDto>();
        //        List<AppTransactionUnitExDto> childUnitList = new List<AppTransactionUnitExDto>();

        //        foreach (var rootLevelUnit in transactionExDto.AppTransactionUnitList)
        //        {
        //            foreach (var afield in rootLevelUnit.AppTransactionFieldList.OrderBy(o => o.SortOrder).ToList())
        //            {
        //                if (!(afield.IsVisible.HasValue && !afield.IsVisible.Value))
        //                {
        //                    dictRootLevelField.Add(dictRootLevelField.Count + 1, afield);
        //                }
        //            }

        //            if (!(rootLevelUnit.IsMasterSiblingUnit.HasValue && rootLevelUnit.IsMasterSiblingUnit.Value))
        //            {
        //                childUnitList.AddRange(rootLevelUnit.Children);
        //            }
        //        }


        //        newForm.AppFormLayoutItemList = new ObservableSet<AppFormLayoutItemExDto>();

        //        if (nbColumns >= 1)
        //        {

        //            // Root Unit
        //            int maxNbFieldPerColumn = (int)Math.Ceiling(dictRootLevelField.Count * 1.0 / nbColumns);

        //            for (int i = 1; i <= nbColumns; i++)
        //            {
        //                List<AppTransactionFieldExDto> needToAddFields = dictRootLevelField
        //                    .Where(o => o.Key > (i - 1) * maxNbFieldPerColumn && o.Key <= i * maxNbFieldPerColumn)
        //                    .Select(o => o.Value).ToList();

        //                var aRootFieldCell = new AppFormLayoutItemExDto()
        //                {
        //                    AppFormLayoutItem_List = new ObservableSet<AppFormLayoutItemExDto>(),
        //                    ColumnIndex = i - 1,
        //                    ColumnSpan = 1,
        //                    RowIndex = 0,
        //                    RowSpan = 1,
        //                };


        //                int layoutFieldSort = 0;
        //                foreach (var field in needToAddFields)
        //                {
        //                    AppFormLayoutItemExDto aFormLayoutItemExDto = new AppFormLayoutItemExDto()
        //                    {
        //                        DisplayTitle = field.DisplayName,
        //                        DomElementTag = field.DataBaseFieldName,
        //                        FlowOrGridLayoutSortOrder = layoutFieldSort,
        //                        StyleLayoutInfo = "width:330px;height:30px;",
        //                        TransactionFieldId = (int)field.Id,
        //                        WidgetItemType = 3,
        //                    };

        //                    if (field.ControlType == (int)EmAppControlType.Image
        //                        || field.ControlType == (int)EmAppControlType.Video
        //                        || field.ControlType == (int)EmAppControlType.BarChart
        //                        || field.ControlType == (int)EmAppControlType.DoughnutChart
        //                        || field.ControlType == (int)EmAppControlType.LinearChart
        //                        || field.ControlType == (int)EmAppControlType.PieChart)
        //                    {
        //                        aFormLayoutItemExDto.StyleLayoutInfo = "width:330px;height:200px;";
        //                    }
        //                    else if (field.ControlType == (int)EmAppControlType.Memo
        //                        || field.ControlType == (int)EmAppControlType.Audio)
        //                    {
        //                        aFormLayoutItemExDto.StyleLayoutInfo = "width:330px;height:100px;";
        //                    }

        //                    aRootFieldCell.AppFormLayoutItem_List.Add(aFormLayoutItemExDto);

        //                    layoutFieldSort++;
        //                }

        //                newForm.AppFormLayoutItemList.Add(aRootFieldCell);

        //            }

        //            // Child Unit
        //            var aChildUnitCell = new AppFormLayoutItemExDto()
        //            {
        //                AppFormLayoutItem_List = new ObservableSet<AppFormLayoutItemExDto>(),
        //                ColumnIndex = 0,
        //                ColumnSpan = nbColumns,
        //                RowIndex = 1,
        //                RowSpan = 1,
        //            };

        //            int unitCount = 1;
        //            foreach (var unit in childUnitList)
        //            {
        //                aChildUnitCell.AppFormLayoutItem_List.Add(new AppFormLayoutItemExDto()
        //                {
        //                    DisplayTitle = unit.UnitDisplayName,
        //                    DomElementTag = unit.DataBaseTableName,
        //                    FlowOrGridLayoutSortOrder = unitCount,
        //                    GridTransactionUnitId = (int)unit.Id,
        //                    StyleLayoutInfo = "width:" + (340 * nbColumns - 10) + "px;height:300px;",
        //                    WidgetItemType = 3,

        //                });
        //                unitCount++;
        //            }

        //            newForm.AppFormLayoutItemList.Add(aChildUnitCell);
        //        }





        //        SaveAppFormExDto(newForm);
        //        return newForm;
        //    }

        //    return null;
        //}



        public static AppFormExDto CreateTransactionFormWithDefaultLayout(int transactionId, int nbColumns)
        {

            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            if (!transactionExDto.FormId.HasValue)
            {

                AppFormExDto newForm = CreateNewTranactionForm(transactionId);
                Dictionary<int, AppTransactionFieldExDto> dictRootLevelField = new Dictionary<int, AppTransactionFieldExDto>();
                List<AppTransactionUnitExDto> childUnitList = new List<AppTransactionUnitExDto>();

                foreach (var rootLevelUnit in transactionExDto.AppTransactionUnitList)
                {
                    foreach (var afield in rootLevelUnit.AppTransactionFieldList.OrderBy(o => o.SortOrder).ToList())
                    {
                        if (!(afield.IsVisible.HasValue && !afield.IsVisible.Value))
                        {
                            dictRootLevelField.Add(dictRootLevelField.Count + 1, afield);
                        }
                    }

                    if (!(rootLevelUnit.IsMasterSiblingUnit.HasValue && rootLevelUnit.IsMasterSiblingUnit.Value))
                    {
                        childUnitList.AddRange(rootLevelUnit.Children);
                    }
                }


                newForm.AppFormLayoutItemList = new ObservableSet<AppFormLayoutItemExDto>();

                if (nbColumns >= 1)
                {

                    // Root Unit
                    int maxNbFieldPerColumn = (int)Math.Ceiling(dictRootLevelField.Count * 1.0 / nbColumns);

                    for (int i = 1; i <= nbColumns; i++)
                    {
                        List<AppTransactionFieldExDto> needToAddFields = dictRootLevelField
                            .Where(o => o.Key > (i - 1) * maxNbFieldPerColumn && o.Key <= i * maxNbFieldPerColumn)
                            .Select(o => o.Value).ToList();

                        var aRootFieldCell = new AppFormLayoutItemExDto()
                        {
                            AppFormLayoutItem_List = new ObservableSet<AppFormLayoutItemExDto>(),
                            ColumnIndex = i - 1,
                            ColumnSpan = 1,
                            RowIndex = 0,
                            RowSpan = 1,
                        };


                        int layoutFieldSort = 0;
                        foreach (var field in needToAddFields)
                        {
                            AppFormLayoutItemExDto aFormLayoutItemExDto = new AppFormLayoutItemExDto()
                            {
                                DisplayTitle = field.DisplayName,
                                DomElementTag = field.DataBaseFieldName,
                                FlowOrGridLayoutSortOrder = layoutFieldSort,
                                StyleLayoutInfo = "width:330px;height:30px;",
                                TransactionFieldId = (int)field.Id,
                                WidgetItemType = 3,
                            };

                            if (field.ControlType == (int)EmAppControlType.Image
                                || field.ControlType == (int)EmAppControlType.Video
                                || field.ControlType == (int)EmAppControlType.BarChart
                                || field.ControlType == (int)EmAppControlType.DoughnutChart
                                || field.ControlType == (int)EmAppControlType.LinearChart
                                || field.ControlType == (int)EmAppControlType.PieChart)
                            {
                                aFormLayoutItemExDto.StyleLayoutInfo = "width:330px;height:200px;";
                            }
                            else if (field.ControlType == (int)EmAppControlType.Memo
                                || field.ControlType == (int)EmAppControlType.Audio)
                            {
                                aFormLayoutItemExDto.StyleLayoutInfo = "width:330px;height:100px;";
                            }

                            aRootFieldCell.AppFormLayoutItem_List.Add(aFormLayoutItemExDto);

                            layoutFieldSort++;
                        }

                        newForm.AppFormLayoutItemList.Add(aRootFieldCell);

                    }

                    // Child Unit
                    var aChildUnitCell = new AppFormLayoutItemExDto()
                    {
                        AppFormLayoutItem_List = new ObservableSet<AppFormLayoutItemExDto>(),
                        ColumnIndex = 0,
                        ColumnSpan = nbColumns,
                        RowIndex = 1,
                        RowSpan = 1,
                    };

                    int unitCount = 1;
                    foreach (var unit in childUnitList)
                    {
                        aChildUnitCell.AppFormLayoutItem_List.Add(new AppFormLayoutItemExDto()
                        {
                            DisplayTitle = unit.UnitDisplayName,
                            DomElementTag = unit.DataBaseTableName,
                            FlowOrGridLayoutSortOrder = unitCount,
                            GridTransactionUnitId = (int)unit.Id,
                            StyleLayoutInfo = "width:" + (340 * nbColumns - 10) + "px;height:300px;",
                            WidgetItemType = 3,

                        });
                        unitCount++;
                    }

                    newForm.AppFormLayoutItemList.Add(aChildUnitCell);
                }





                SaveAppFormExDto(newForm);
                return newForm;
            }

            return null;
        }


        // attach Traanscation to Form view
        public static AppFormExDto RetrieveTransactionAppFormExDto(AppTransactionExDto aAppTransactionExDto, bool isPrintForm = false)
        {
            object formId = aAppTransactionExDto.FormId;

            if (isPrintForm && aAppTransactionExDto.PrintFormId.HasValue)
            {
                formId = aAppTransactionExDto.PrintFormId.Value;
            }

            AppFormEntity aAppFormEntity = RetrieveOneAppFormEntity(formId);
            AppFormExDto aAppFormExDto = AppFormConverter.ConvertEntityToExDto(aAppFormEntity);


            aAppFormExDto.AssociatedTransactionId = aAppTransactionExDto.Id as int?;

            if (aAppFormExDto.LayoutType.HasValue && aAppFormExDto.LayoutType.Value == (int)EmAppFormLayoutType.Flex)
            {
                AppFormFlexLayoutBL.PrepareFlexFormLayoutItemExDtoTree(aAppFormEntity, aAppFormExDto, aAppTransactionExDto);
            }
            else
            {
                foreach (var formLayoutEntity in aAppFormEntity.AppFormLayoutItem)
                {
                    AppFormLayoutItemExDto appFormLayoutItemExDto = ConvertOneLayoutItemEntityToLayoutItemDto(formLayoutEntity, aAppTransactionExDto);

                    aAppFormExDto.AppFormLayoutItemList.Add(appFormLayoutItemExDto);

                    foreach (var controlUnitEntity in formLayoutEntity.AppFormLayoutItem_)
                    {
                        AppFormLayoutItemExDto aUnitAppFormKeyExDto = ConvertOneLayoutItemEntityToLayoutItemDto(controlUnitEntity, aAppTransactionExDto);
                        appFormLayoutItemExDto.AppFormLayoutItem_List.Add(aUnitAppFormKeyExDto);


                    }
                }

            }



            //foreach (var aFormLinkTargetEntity in aAppFormEntity.AppFormLinkTarget)
            //{
            //    aAppFormExDto.AppFormLinkTargetList.Add(AppFormLinkTargetConverter.ConvertEntityToExDto(aFormLinkTargetEntity));                
            //}



            return aAppFormExDto;
        }


        public static int? GetTranscationIdFromFormId(object formId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                string query = @"SELECT         AppTransaction.TransactionID FROM AppTransaction where FormID = @FormID";
                List<SqlParameter> listParas = new List<SqlParameter>();
                listParas.Add(new SqlParameter("@FormID", formId));
                return adapter.ExecuteScalarQuery(query, listParas) as int?;

            }


        }

        public static int? GetTranscationIdFromPrintFormId(object formId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                string query = @"SELECT         AppTransaction.TransactionID FROM AppTransaction where PrintFormID = @FormID";
                List<SqlParameter> listParas = new List<SqlParameter>();
                listParas.Add(new SqlParameter("@FormID", formId));
                return adapter.ExecuteScalarQuery(query, listParas) as int?;

            }


        }

        public static AppFormExDto RetrieveOneAppFormExDto(object formId)
        {

            AppFormEntity aAppFormEntity = RetrieveOneAppFormEntity(formId);

            AppFormExDto aAppFormExDto = AppFormConverter.ConvertEntityToExDto(aAppFormEntity);

            if (aAppFormExDto.IsPrintForm)
            {
                aAppFormExDto.AssociatedTransactionId = GetTranscationIdFromPrintFormId(formId);
            }
            else
            {
                aAppFormExDto.AssociatedTransactionId = GetTranscationIdFromFormId(formId);
            }



            foreach (var formLayoutEntity in aAppFormEntity.AppFormLayoutItem)
            {


                AppFormLayoutItemExDto appFormLayoutItemExDto = AppFormLayoutItemConverter.ConvertEntityToExDto(formLayoutEntity);

                aAppFormExDto.AppFormLayoutItemList.Add(appFormLayoutItemExDto);

                foreach (var controlUnitEntity in formLayoutEntity.AppFormLayoutItem_)
                {
                    AppFormLayoutItemExDto aUnitAppFormKeyExDto = AppFormLayoutItemConverter.ConvertEntityToExDto(controlUnitEntity);

                    appFormLayoutItemExDto.AppFormLayoutItem_List.Add(aUnitAppFormKeyExDto);


                }
            }





            return aAppFormExDto;
        }




        public static OperationCallResult<bool> DeleteOneAppFormExDto(int formId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                var formEntity = RetrieveOneAppFormEntity(formId);

                int[] rootFormLayoutids = formEntity.AppFormLayoutItem.Select(o => o.FormLayoutItemId).ToArray();

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    AppTransactionEntity transactionEntity = new AppTransactionEntity();
                    transactionEntity.FormId = null;
                    adapter.UpdateEntitiesDirectly(transactionEntity, new RelationPredicateBucket(AppTransactionFields.FormId == formId));
                    adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.UigridLayoutParentId == rootFormLayoutids));
                    adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.FormId == formId));
                    adapter.DeleteEntitiesDirectly(typeof(AppFormEntity), new RelationPredicateBucket(AppFormFields.FormId == formId));

                    aValidationResult.Items.Add(new ValidationItem(null, "App_Form_Delete_Ok", ValidationItemType.Message, "Form delete successful"));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(null, "App_Form_Delete_Failed", ValidationItemType.Error, "Form Delete Failed: " + ex.ToString()));
                }
            }

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }



        public static OperationCallResult<bool> ResetFormLayout(int formId, int? resetToLayoutType, bool needToGenerateDefaultLayout)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                var formEntity = RetrieveOneAppFormEntity(formId);

                int[] rootFormLayoutids = formEntity.AppFormLayoutItem.Select(o => o.FormLayoutItemId).ToArray();

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.UigridLayoutParentId == rootFormLayoutids));
                    adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.FormId == formId));

                    if (resetToLayoutType.HasValue)
                    {
                        formEntity.LayoutType = resetToLayoutType.Value;
                        adapter.UpdateEntitiesDirectly(formEntity, new RelationPredicateBucket(AppFormFields.FormId == formId));
                    }

                    aValidationResult.Items.Add(new ValidationItem(null, "App_Form_Reset_Ok", ValidationItemType.Message, "Form Reset successful"));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(null, "App_Form_Reset_Failed", ValidationItemType.Error, "Form Reset Failed: " + ex.ToString()));
                }
            }


            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                if (needToGenerateDefaultLayout)
                {
                    if (resetToLayoutType.HasValue)
                    {
                        if (resetToLayoutType.Value == (int)EmAppFormLayoutType.Flex)
                        {
                            var resetedFormExDto = AppFormFlexLayoutBL.BuildAppFormDefaultLayout(formId);

                            var saveResult = AppFormFlexLayoutBL.SaveAppFormFlexLayoutExDto(resetedFormExDto);

                            if (saveResult.ValidationResult.HasErrors)
                            {
                                aValidationResult.Merge(saveResult.ValidationResult);
                            }
                        }
                    }
                    
                }
            }
           

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }








        private static AppFormLayoutItemExDto ConvertOneLayoutItemEntityToLayoutItemDto(AppFormLayoutItemEntity formLayoutItemEntity, AppTransactionExDto aHierarchyTransactionExDto)
        {
            AppFormLayoutItemExDto aAppFormKeyExDto = AppFormLayoutItemConverter.ConvertEntityToExDto(formLayoutItemEntity);
            //var dictRootFieldList
            // var rootUnit = aAppTransactionExDto.AppTransactionUnitList.FirstOrDefault();

            if (aAppFormKeyExDto.GridTransactionUnitId.HasValue)
            {
                aAppFormKeyExDto.ForeignAppTransactionUnitExDto = aHierarchyTransactionExDto.DictAllTransactionUnitIdExDto[aAppFormKeyExDto.GridTransactionUnitId.ToString()];
            }

            if (aAppFormKeyExDto.TransactionFieldId.HasValue)
            {
                aAppFormKeyExDto.ForeignAppTransactionFieldExDto = aHierarchyTransactionExDto.DictAllTransactionField[aAppFormKeyExDto.TransactionFieldId.Value];//AppTransactionBL.RetrieveOneAppTransactionFieldExDto(aAppFormKeyExDto.TransactionFieldId.Value);

            }

            else if (aAppFormKeyExDto.DomAttribute != null && aAppFormKeyExDto.DomAttribute.WidgetDisplayType.HasValue)
            {
                if (aAppFormKeyExDto.DomAttribute.WidgetDisplayType.Value == (int)EmAppFormLayoutItemType.CommandActionButton)
                {
                    if (aHierarchyTransactionExDto.CommandActionList != null && aAppFormKeyExDto.DomAttribute.CommandActionId.HasValue)
                    {
                        aAppFormKeyExDto.BindToCommandAction = aHierarchyTransactionExDto.CommandActionList.FirstOrDefault(o => (int)o.Id == aAppFormKeyExDto.DomAttribute.CommandActionId.Value);
                        //aAppFormKeyExDto.BindToCommandAction.ForeignAppTransactionExDto = aHierarchyTransactionExDto;

                        int? conditionFieldId = aAppFormKeyExDto.BindToCommandAction.CommandConditionTransactionFieldId;
                        if (conditionFieldId.HasValue)
                        {
                            if (aHierarchyTransactionExDto.DictRootLevelUnitTransactionField != null && aHierarchyTransactionExDto.DictRootLevelUnitTransactionField.ContainsKey(conditionFieldId.Value))
                            {
                                var conditionField = aHierarchyTransactionExDto.DictRootLevelUnitTransactionField[conditionFieldId.Value];
                                aAppFormKeyExDto.BindToCommandAction.CommandConditionFieldDbFieldName = conditionField.DataBaseFieldName;
                            }
                        }
                    }
                }
                else if (aAppFormKeyExDto.DomAttribute.WidgetDisplayType.Value == (int)EmAppFormLayoutItemType.LinkedSearch)
                {
                    Dictionary<int, AppTransactionUnitLinkedSearchDto> dictRootLevelUnitLinkedSearchIdAndDto = new Dictionary<int, AppTransactionUnitLinkedSearchDto>();

                    aHierarchyTransactionExDto.AppTransactionUnitList.Where(o => o.AppTransactionUnitLinkedSearchList != null)
                        .ForAll(o => o.AppTransactionUnitLinkedSearchList
                        .ForAll(p => dictRootLevelUnitLinkedSearchIdAndDto[(int)p.Id] = p));

                    int? linkedSearchId = aAppFormKeyExDto.DomAttribute.LinkedSearchId;

                    if (linkedSearchId.HasValue && dictRootLevelUnitLinkedSearchIdAndDto.ContainsKey(linkedSearchId.Value))
                    {
                        aAppFormKeyExDto.BindToLinkedSearch = dictRootLevelUnitLinkedSearchIdAndDto[linkedSearchId.Value];
                    }
                }
            }

            return aAppFormKeyExDto;
        }


        public static AppFormEntity RetrieveSimpleAppFormEntity(object FormId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppFormEntity formEntity = new AppFormEntity(int.Parse(FormId.ToString()));


                adpater.FetchEntity(formEntity);
                return formEntity;
            }
        }


        public static AppFormEntity RetrieveOneAppFormEntity(object FormId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppFormEntity formEntity = new AppFormEntity(int.Parse(FormId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppFormEntity);

                rootPath.Add(AppFormEntity.PrefetchPathAppFormLayoutItem).SubPath.Add(AppFormLayoutItemEntity.PrefetchPathAppFormLayoutItem_);
                //  rootPath.Add(AppFormEntity.PrefetchPathAppFormLinkTarget);

                adpater.FetchEntity(formEntity, rootPath);
                return formEntity;
            }
        }


        public static AppFormLayoutItemExDto RetrieveOneAppFormLayoutItemExDto(object Id)
        {
            AppFormLayoutItemEntity aEntity = new AppFormLayoutItemEntity(int.Parse(Id.ToString()));

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntity(aEntity);
                AppFormLayoutItemExDto appTransactionFieldExDto = AppFormLayoutItemConverter.ConvertEntityToExDto(aEntity);

                if (appTransactionFieldExDto.TransactionFieldId.HasValue)
                {
                    appTransactionFieldExDto.ForeignAppTransactionFieldExDto = AppTransactionBL.RetrieveOneAppTransactionFieldExDto(appTransactionFieldExDto.TransactionFieldId.Value);
                }

                if (appTransactionFieldExDto.GridTransactionUnitId.HasValue)
                {
                    appTransactionFieldExDto.ForeignAppTransactionUnitExDto = AppTransactionBL.RetrieveOneAppTransactionUnitExDto(appTransactionFieldExDto.GridTransactionUnitId.Value);
                }

                return appTransactionFieldExDto;
            }
        }



        public static OperationCallResult<AppFormExDto> SaveAppFormExDto(AppFormExDto aAppFormExDto)
        {
            OperationCallResult<AppFormExDto> aOperationCallResult = new OperationCallResult<AppFormExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppFormEntity aAppFormEntity;

            // prepare Data
            if (aAppFormExDto.IsNew)
            {
                aAppFormEntity = new AppFormEntity();
                AppFormConverter.CopyDtoToEntity(aAppFormEntity, aAppFormExDto);



                foreach (AppFormLayoutItemExDto appFormLayoutItemExDto in aAppFormExDto.AppFormLayoutItemList)
                {
                    AppFormLayoutItemEntity aAppFormLayoutItemEntity = new AppFormLayoutItemEntity();
                    AppFormLayoutItemConverter.CopyDtoToEntity(aAppFormLayoutItemEntity, appFormLayoutItemExDto);
                    aAppFormEntity.AppFormLayoutItem.Add(aAppFormLayoutItemEntity);

                    foreach (AppFormLayoutItemExDto controlUnitLayout in appFormLayoutItemExDto.AppFormLayoutItem_List)
                    {

                        AppFormLayoutItemEntity aAppUnitFormLayoutItemEntity = new AppFormLayoutItemEntity();
                        AppFormLayoutItemConverter.CopyDtoToEntity(aAppUnitFormLayoutItemEntity, controlUnitLayout);


                        aAppFormLayoutItemEntity.AppFormLayoutItem_.Add(aAppUnitFormLayoutItemEntity);

                    }



                }
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppFormEntity);
                        adapter.Commit();

                        aAppFormExDto.Id = aAppFormEntity.FormId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_FormEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
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

                //if (aAppFormExDto.SaasApplicationId.HasValue)
                //{
                //    AppApplicationAssetsItemExDto formAssetsItemDto = new AppApplicationAssetsItemExDto();
                //    formAssetsItemDto.ApplicationId = aAppFormExDto.SaasApplicationId;
                //    formAssetsItemDto.FormId = aAppFormEntity.FormId;
                //    AppSaasUserApplicationPackageBL.SaveNewApplicationAssetsItemExDto(formAssetsItemDto);
                //}
            }

            else if (aAppFormExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppFormExDto(aAppFormExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                var formDto = RetrieveOneAppFormExDto(aAppFormExDto.Id);

                if (formDto != null)
                {
                    if (formDto.AssociatedTransactionId.HasValue)
                    {
                        var freshHierarchyAppTransactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(formDto.AssociatedTransactionId.Value);
                        AppTransactionBL.SynchronizeDatabaseTableAndUpdateCahce(freshHierarchyAppTransactionExDto);
                    }


                    aOperationCallResult.Object = RetrieveOneAppFormExDto(aAppFormExDto.Id);
                }

            }

            return aOperationCallResult;
        }


        public static OperationCallResult<object> DeleteOneAppForm(object formId, int? transactionId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            AppFormEntity aAppFormEntity = RetrieveOneAppFormEntity(formId);

            List<int> controlUnitLayoutItemIds = new List<int>();

            foreach (var formLayoutEntity in aAppFormEntity.AppFormLayoutItem)
            {
                foreach (var controlUnitEntity in formLayoutEntity.AppFormLayoutItem_)
                {
                    controlUnitLayoutItemIds.Add(controlUnitEntity.FormLayoutItemId);
                }
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");


                    AppTransactionEntity updateAppTransactionEntity = new AppTransactionEntity();
                    updateAppTransactionEntity.FormId = null;
                    adapter.UpdateEntitiesDirectly(updateAppTransactionEntity, new RelationPredicateBucket(AppTransactionFields.FormId == formId));

                    adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.FormLayoutItemId == controlUnitLayoutItemIds.ToArray()));
                    adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.FormId == formId));
                    adapter.DeleteEntitiesDirectly(typeof(AppFormEntity), new RelationPredicateBucket(AppFormFields.FormId == formId));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();

                    aValidationResult.Items.Add(new ValidationItem(null, App_FormEntity_Delete_Failed, ValidationItemType.Error, ""));
                }
            }

            aOperationCallResult.Object = formId;

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                string message = StringLocalizer.Localize(App_FormEntity_Delete_Ok, "Form Delete Successful");
                aValidationResult.Items.Add(new ValidationItem(null, App_FormEntity_Delete_Ok, ValidationItemType.Message, message));


                if (transactionId.HasValue) // refresh transaction cache
                {
                    var exdto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId.Value);
                    exdto.FormId = null;
                }
            }

            return aOperationCallResult;
        }

        private static ValidationResult ProcessDirtyAppFormExDto(AppFormExDto aAppFormExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            int[] dirtyFieldIds = aAppFormExDto.AppFormLayoutItemList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppFormEntity aServerAppFormEntity = RetrieveOneAppFormEntity(aAppFormExDto.Id);

            Dictionary<int, AppFormLayoutItemEntity> dictAppFormLayoutItemFromDbms = aServerAppFormEntity.AppFormLayoutItem.ToDictionary(o => o.FormLayoutItemId, o => o);

            AppFormConverter.CopyDtoToEntity(aServerAppFormEntity, aAppFormExDto);
            //  aAppFormEntity.ModifiedDate = System.DateTime.UtcNow;
            //  aAppFormEntity.ModifiedBy = (int)ServerContext.Instance.CurrentUid;

            //------- check  AppFormLayoutItem

            // new Items

            // need to delete all child
            //if (aAppFormExDto.LayoutType == (int)EmAppFormLayoutType.Grid)
            //{
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                // Need to delete AppFormLayoutItemFields

                var formEntity = RetrieveOneAppFormEntity(aAppFormExDto.Id);

                int[] rootFormLayoutids = formEntity.AppFormLayoutItem.Select(o => o.FormLayoutItemId).ToArray();

                // AppFormLayoutItemEntity aAppFormLayoutItemEntity = new AppFormLayoutItemEntity ();
                // aAppFormLayoutItemEntity.UigridLayoutParentId = null;

                //  adapter.UpdateEntitiesDirectly(aAppFormLayoutItemEntity, new RelationPredicateBucket(AppFormLayoutItemFields.UigridLayoutParentId == rootFormLayoutids));
                adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.UigridLayoutParentId == rootFormLayoutids));


                adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.FormId == aAppFormExDto.Id));


            }



            //}


            foreach (var appFormLayoutItemExDto in aAppFormExDto.AppFormLayoutItemList.FindNewItems())
            {
                AppFormLayoutItemEntity aAppFormLayoutItemEntity = new AppFormLayoutItemEntity();
                AppFormLayoutItemConverter.CopyDtoToEntity(aAppFormLayoutItemEntity, appFormLayoutItemExDto);
                aServerAppFormEntity.AppFormLayoutItem.Add(aAppFormLayoutItemEntity);


                foreach (AppFormLayoutItemExDto controlUnitLayout in appFormLayoutItemExDto.AppFormLayoutItem_List)
                {

                    AppFormLayoutItemEntity aAppUnitFormLayoutItemEntity = new AppFormLayoutItemEntity();
                    AppFormLayoutItemConverter.CopyDtoToEntity(aAppUnitFormLayoutItemEntity, controlUnitLayout);


                    aAppFormLayoutItemEntity.AppFormLayoutItem_.Add(aAppUnitFormLayoutItemEntity);

                }
            }

            // Dirty items //????

            List<int> controlUnitDeleteFieldIDs = new List<int>();

            foreach (var modifyCellLayoutItem in aAppFormExDto.AppFormLayoutItemList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyCellLayoutItem.Id.ToString());

                AppFormLayoutItemEntity aAppFormLayoutItemEntity = dictAppFormLayoutItemFromDbms[dtoKey];

                Dictionary<int, AppFormLayoutItemEntity> dictCotrolUnitEntity = aAppFormLayoutItemEntity.AppFormLayoutItem_.ToDictionary(o => o.FormLayoutItemId, o => o);

                if (dictAppFormLayoutItemFromDbms.ContainsKey(dtoKey))
                {
                    AppFormLayoutItemConverter.CopyDtoToEntity(aAppFormLayoutItemEntity, modifyCellLayoutItem);
                }

                foreach (AppFormLayoutItemExDto controlUnitLayout in modifyCellLayoutItem.AppFormLayoutItem_List)
                {

                    if (controlUnitLayout.IsNew)
                    {
                        AppFormLayoutItemEntity aAppUnitFormLayoutItemEntity = new AppFormLayoutItemEntity();
                        AppFormLayoutItemConverter.CopyDtoToEntity(aAppUnitFormLayoutItemEntity, controlUnitLayout);
                        aAppFormLayoutItemEntity.AppFormLayoutItem_.Add(aAppUnitFormLayoutItemEntity);
                    }

                    else  // it is modified
                    {
                        if (dictCotrolUnitEntity.ContainsKey((int)controlUnitLayout.Id))
                        {

                            AppFormLayoutItemConverter.CopyDtoToEntity(dictCotrolUnitEntity[(int)controlUnitLayout.Id], controlUnitLayout);
                            //  AppFormLayoutItemConverter.CopyDtoToEntity(aAppUnitFormLayoutItemEntity, controlUnitLayout);
                        }


                    }




                }


                controlUnitDeleteFieldIDs.AddRange(modifyCellLayoutItem.AppFormLayoutItem_List.FindDeletedItemIds().Cast<int>());



                // controlUnitDeleteFieldIDs.
            }

            // deletedIDs
            int[] cellDeleteFieldIDs = aAppFormExDto.AppFormLayoutItemList.FindDeletedItemIds().Cast<int>().ToArray();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aServerAppFormEntity);


                    // Need to delete AppFormLayoutItemFields
                    if (controlUnitDeleteFieldIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.FormLayoutItemId == controlUnitDeleteFieldIDs));
                    }


                    // Need to delete AppFormLayoutItemFields
                    if (cellDeleteFieldIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppFormLayoutItemEntity), new RelationPredicateBucket(AppFormLayoutItemFields.FormLayoutItemId == cellDeleteFieldIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_FormEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_FormEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }


        // SearchView Laout Form
        public static AppFormExDto CreateNewSearchViewForm(int searchViewId)
        {
            AppFormExDto newAppFormExDto = new AppFormExDto();
            newAppFormExDto.Name = "New SearchView Form";

            OperationCallResult<AppFormExDto> result = AppFormBL.SaveAppFormExDto(newAppFormExDto);

            AppFormExDto savedAppFormExDto = result.Object;

            int? formId = savedAppFormExDto.Id as int?;
            AppSearchViewEntity updateAppSearchViewEntity = new AppSearchViewEntity();

            updateAppSearchViewEntity.ChartType = formId as int?;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.UpdateEntitiesDirectly(updateAppSearchViewEntity, new RelationPredicateBucket(AppSearchViewFields.SearchViewId == searchViewId));
            }

            result.Object.AssociatedSearchViewId = searchViewId;

            return result.Object;
        }

        //public static AppFormExDto RetrieveSearchViewAppFormExDto(AppSearchViewExDto aAppSearchViewExDto)
        //{
        //    object FormId = aAppSearchViewExDto.ChartType;
        //    AppFormEntity aAppFormEntity = RetrieveOneAppFormEntity(FormId);
        //    AppFormExDto aAppFormExDto = AppFormConverter.ConvertEntityToExDto(aAppFormEntity);


        //    aAppFormExDto.AssociatedSearchViewId = aAppSearchViewExDto.Id as int?;





        //    foreach (var formLayoutEntity in aAppFormEntity.AppFormLayoutItem)
        //    {
        //        AppFormLayoutItemExDto appFormLayoutItemExDto = ConvertOneLayoutItemEntityToLayoutItemDto(formLayoutEntity, aAppSearchViewExDto);

        //        aAppFormExDto.AppFormLayoutItemList.Add(appFormLayoutItemExDto);

        //        foreach (var controlUnitEntity in formLayoutEntity.AppFormLayoutItem_)
        //        {
        //            AppFormLayoutItemExDto aUnitAppFormKeyExDto = ConvertOneLayoutItemEntityToLayoutItemDto(controlUnitEntity, aAppSearchViewExDto);
        //            appFormLayoutItemExDto.AppFormLayoutItem_List.Add(aUnitAppFormKeyExDto);


        //        }
        //    }




        //    return aAppFormExDto;
        //}




    }
}