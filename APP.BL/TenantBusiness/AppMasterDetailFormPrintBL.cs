using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace App.BL
{
    public static class AppMasterDetailFormPrintBL
    {
        public static AppTransactionExDto PrepareFormMasterDetailPrintData(int? transactionId, string rootPrimaryKeyValue, bool isUsedForNotifyBody, int? opennedFormAutoExecuteCommandId = null, bool isConfigTestRun = false)
        {
            if (transactionId.HasValue)
            {
                AppTransactionExDto appTransactionExDto = AppTransactionBL.GetCurrentUserOneHierarchyTransactionWithSecurityAndLangLable(transactionId, rootPrimaryKeyValue);


                if (appTransactionExDto == null)
                {
                    return null;
                }

                AppFormExDto AppFormExDto = AppFormBL.RetrieveTransactionAppFormExDto(appTransactionExDto, true);

                //// Used as Notification Message Body
                //int? PrintFormId = appTransactionExDto.FormId;
                //if (isUsedForNotifyBody && appTransactionExDto.PrintFormId.HasValue)
                //{
                //    PrintFormId = appTransactionExDto.PrintFormId;
                //}                               

                //AppFormExDto AppFormExDto = AppFormBL.RetrieveOneAppFormExDto(PrintFormId);

                if (AppFormExDto == null)
                {
                    return null;
                }

                AppTransactionStructureDto aAppTransactionStructureDto = AppTransactionStructureLoadBL.GetAppTransactionStructureDto(transactionId.Value);
                Dictionary<int, AppTransactionFieldExDto> dictTransactionFieldIdAndExDto = new Dictionary<int, AppTransactionFieldExDto>();

                AppMasterDetailDto aAppformDataDto = null;




                if (!string.IsNullOrWhiteSpace(rootPrimaryKeyValue))
                {
                    aAppformDataDto = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId.Value, rootPrimaryKeyValue, opennedFormAutoExecuteCommandId);
                }
                else
                {
                    aAppformDataDto = AppMasterDetailFormDataLoadBL.GetNewFormData(transactionId.Value, isConfigTestRun);
                }

                if (aAppformDataDto == null || aAppTransactionStructureDto == null)
                {
                    return null;
                }


                appTransactionExDto.RootPrimaryKeyValue = rootPrimaryKeyValue;
                AppTransactionUnitExDto aRootTransactionUnitExDto = appTransactionExDto.RootMasterUnit;

                aRootTransactionUnitExDto.UnitDisplayName = AppLocalizeSystemLableBL.GetTransactionUnitLabel(aRootTransactionUnitExDto.Id, aRootTransactionUnitExDto.UnitDisplayName);




                // Root Unit
                foreach (AppTransactionFieldExDto transField in aRootTransactionUnitExDto.AppTransactionFieldList)
                {
                    if (aAppformDataDto.DictOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                    {
                        object transFieldOrgValue = aAppformDataDto.DictOneToOneFields[transField.DataBaseFieldName];
                        object transFieldPrintValue = GetTransFieldPrintValue(transField, transFieldOrgValue, aAppTransactionStructureDto);
                        aAppformDataDto.DictOneToOneFields[transField.DataBaseFieldName] = transFieldPrintValue;
                    }

                    if (!dictTransactionFieldIdAndExDto.ContainsKey((int)transField.Id))
                    {
                        dictTransactionFieldIdAndExDto.Add((int)transField.Id, transField);
                    }
                }

                // Sib Unit
                if (appTransactionExDto.SibLineTransactionUnitIdExDtoList != null)
                {
                    foreach (AppTransactionUnitExDto sibUnit in appTransactionExDto.SibLineTransactionUnitIdExDtoList)
                    {
                        if (aAppformDataDto.DictSiblingOneToOneFields.ContainsKey(sibUnit.Id.ToString()))
                        {
                            var sibOneToOneFields = aAppformDataDto.DictSiblingOneToOneFields[sibUnit.Id.ToString()];
                            if (sibOneToOneFields != null)
                            {
                                foreach (AppTransactionFieldExDto transField in sibUnit.AppTransactionFieldList)
                                {
                                    if (sibOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                                    {
                                        object transFieldOrgValue = sibOneToOneFields[transField.DataBaseFieldName];
                                        object transFieldPrintValue = GetTransFieldPrintValue(transField, transFieldOrgValue, aAppTransactionStructureDto);
                                        sibOneToOneFields[transField.DataBaseFieldName] = transFieldPrintValue;
                                    }

                                    if (!dictTransactionFieldIdAndExDto.ContainsKey((int)transField.Id))
                                    {
                                        dictTransactionFieldIdAndExDto.Add((int)transField.Id, transField);
                                    }
                                }
                            }
                        }
                    }
                }

                // Child Grid Unit
                if (aRootTransactionUnitExDto.Children != null && aRootTransactionUnitExDto.Children.Count > 0)
                {
                    foreach (AppTransactionUnitExDto childUnit in aRootTransactionUnitExDto.Children)
                    {
                        if (aAppformDataDto.DictOneToManyFields.ContainsKey(childUnit.Id.ToString()))
                        {
                            List<AppChildDataDto> childDataDtoList = aAppformDataDto.DictOneToManyFields[childUnit.Id.ToString()];

                            foreach (AppChildDataDto childDataDto in childDataDtoList)
                            {
                                foreach (AppTransactionFieldExDto transField in childUnit.AppTransactionFieldList)
                                {
                                    if (childDataDto.DictOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                                    {
                                        object transFieldOrgValue = childDataDto.DictOneToOneFields[transField.DataBaseFieldName];
                                        object transFieldPrintValue = GetTransFieldPrintValue(transField, transFieldOrgValue, aAppTransactionStructureDto);
                                        childDataDto.DictOneToOneFields[transField.DataBaseFieldName] = transFieldPrintValue;
                                    }

                                    if (!dictTransactionFieldIdAndExDto.ContainsKey((int)transField.Id))
                                    {
                                        dictTransactionFieldIdAndExDto.Add((int)transField.Id, transField);
                                    }
                                }

                                // Grandchild Grid Unit
                                if (childUnit.Children != null && childUnit.Children.Count > 0)
                                {
                                    foreach (AppTransactionUnitExDto grandchildUnit in childUnit.Children)
                                    {
                                        if (childDataDto.DictOneToManyFields.ContainsKey(grandchildUnit.Id.ToString()))
                                        {
                                            List<AppChildDataDto> grandchildRowDataList = childDataDto.DictOneToManyFields[grandchildUnit.Id.ToString()];


                                            foreach (AppChildDataDto aAppChildDataDto in grandchildRowDataList)
                                            {
                                                Dictionary<string, object> dcitGrandchildRowData = aAppChildDataDto.DictOneToOneFields;
                                                foreach (AppTransactionFieldExDto transField in grandchildUnit.AppTransactionFieldList)
                                                {
                                                    if (dcitGrandchildRowData.ContainsKey(transField.DataBaseFieldName))
                                                    {
                                                        object transFieldOrgValue = dcitGrandchildRowData[transField.DataBaseFieldName];
                                                        object transFieldPrintValue = GetTransFieldPrintValue(transField, transFieldOrgValue, aAppTransactionStructureDto);
                                                        dcitGrandchildRowData[transField.DataBaseFieldName] = transFieldPrintValue;
                                                    }

                                                    if (!dictTransactionFieldIdAndExDto.ContainsKey((int)transField.Id))
                                                    {
                                                        dictTransactionFieldIdAndExDto.Add((int)transField.Id, transField);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                        }

                    }
                }

                int RowCount = 0;
                int ColumnCount = 0;

                if (AppFormExDto.LayoutType.HasValue && AppFormExDto.LayoutType.Value == (int)EmAppFormLayoutType.Flex)
                {
                    if (AppFormExDto.AppFormLayoutItemList.Where(o => o.FlowOrGridLayoutSortOrder.HasValue).Count() > 0)
                    {
                        RowCount = AppFormExDto.AppFormLayoutItemList.Where(o => o.FlowOrGridLayoutSortOrder.HasValue).Max(o => o.FlowOrGridLayoutSortOrder.Value);
                    }

                    ColumnCount = 1;
                }
                else
                {
                    if (AppFormExDto.AppFormLayoutItemList.Where(o => o.RowIndex.HasValue).Count() > 0)
                    {
                        RowCount = AppFormExDto.AppFormLayoutItemList.Where(o => o.RowIndex.HasValue).Max(o => o.RowIndex.Value);
                    }

                    if (AppFormExDto.AppFormLayoutItemList.Where(o => o.ColumnIndex.HasValue).Count() > 0)
                    {
                        ColumnCount = AppFormExDto.AppFormLayoutItemList.Where(o => o.ColumnIndex.HasValue).Max(o => o.ColumnIndex.Value);
                    }
                }

                appTransactionExDto.PrintRowCount = RowCount;
                appTransactionExDto.PrintColumnCount = ColumnCount;
                appTransactionExDto.PrintAppMasterDetailDto = aAppformDataDto;



                appTransactionExDto.PrintDictTransactionFieldIdAndExDto = dictTransactionFieldIdAndExDto;
                appTransactionExDto.IsLoadingPrintForm = true;
                appTransactionExDto.PrintAppFormExDto = AppFormExDto;

                return appTransactionExDto;
            }
            return null;
        }




        public static AppTransactionFieldExDto GetRegularFieldLayoutItemLabelAndPrintValue(AppFormLayoutItemExDto layoutItem)
        {
            if (layoutItem != null)
            {
                if (layoutItem.GridUnitRowDataDto != null)
                {
                    return GetChildUnitFieldLayoutItemLabelAndPrintValue(layoutItem);
                }

                AppTransactionExDto appTransactionExDto = layoutItem.PrintAppTransactionExDto;
                var formDataDto = appTransactionExDto.PrintAppMasterDetailDto;
                AppFormExDto formExDto = appTransactionExDto.PrintAppFormExDto;
                Dictionary<int, AppTransactionFieldExDto> dictTransactionFieldIdAndExDto = appTransactionExDto.PrintDictTransactionFieldIdAndExDto;

                if (appTransactionExDto != null && formDataDto != null && formExDto != null && dictTransactionFieldIdAndExDto != null)
                {


                    object fieldValue = null;

                    AppTransactionFieldExDto aAppTransactionFieldExDto = dictTransactionFieldIdAndExDto[layoutItem.TransactionFieldId.Value];

                    if (formDataDto.DictSiblingOneToOneFields.ContainsKey(aAppTransactionFieldExDto.TransactionUnitId.ToString()))
                    {
                        var dictSibOneToOne = formDataDto.DictSiblingOneToOneFields[aAppTransactionFieldExDto.TransactionUnitId.ToString()];

                        if (dictSibOneToOne.ContainsKey(aAppTransactionFieldExDto.DataBaseFieldName))
                        {
                            fieldValue = dictSibOneToOne[aAppTransactionFieldExDto.DataBaseFieldName];
                        }

                    }
                    else if (formDataDto.DictOneToOneFields.ContainsKey(aAppTransactionFieldExDto.DataBaseFieldName))
                    {
                        fieldValue = formDataDto.DictOneToOneFields[aAppTransactionFieldExDto.DataBaseFieldName];
                    }

                    AppTransactionFieldExDto toReturn = new AppTransactionFieldExDto();
                    toReturn.DisplayName = AppLocalizeSystemLableBL.GetDictTransactionFieldLabel(aAppTransactionFieldExDto.Id, aAppTransactionFieldExDto.DisplayName);
                    toReturn.PrintValue = fieldValue;
                    toReturn.ControlType = aAppTransactionFieldExDto.ControlType;
                    toReturn.DefaultValue = aAppTransactionFieldExDto.DefaultValue;
                    return toReturn;
                }
            }

            return null;
        }

        private static AppTransactionFieldExDto GetChildUnitFieldLayoutItemLabelAndPrintValue(AppFormLayoutItemExDto layoutItem)
        {
            if (layoutItem != null)
            {
                AppTransactionExDto appTransactionExDto = layoutItem.PrintAppTransactionExDto;
                var rowDataDto = layoutItem.GridUnitRowDataDto;
                //AppFormExDto formExDto = appTransactionExDto.PrintAppFormExDto;
                Dictionary<int, AppTransactionFieldExDto> dictTransactionFieldIdAndExDto = appTransactionExDto.PrintDictTransactionFieldIdAndExDto;

                if (appTransactionExDto != null && rowDataDto != null && dictTransactionFieldIdAndExDto != null)
                {
                    object fieldValue = null;

                    AppTransactionFieldExDto aAppTransactionFieldExDto = dictTransactionFieldIdAndExDto[layoutItem.TransactionFieldId.Value];
                                       
                    if (rowDataDto.DictOneToOneFields.ContainsKey(aAppTransactionFieldExDto.DataBaseFieldName))
                    {
                        fieldValue = rowDataDto.DictOneToOneFields[aAppTransactionFieldExDto.DataBaseFieldName];
                    }

                    AppTransactionFieldExDto toReturn = new AppTransactionFieldExDto();
                    toReturn.DisplayName = AppLocalizeSystemLableBL.GetDictTransactionFieldLabel(aAppTransactionFieldExDto.Id, aAppTransactionFieldExDto.DisplayName);
                    toReturn.PrintValue = fieldValue;
                    toReturn.ControlType = aAppTransactionFieldExDto.ControlType;
                    toReturn.DefaultValue = aAppTransactionFieldExDto.DefaultValue;
                    return toReturn;
                }
            }

            return null;
        }

        public static object GetRootLevelTransactionFieldValue(AppMasterDetailDto formData, AppTransactionExDto transactionExDto, int transFieldId)
        {
            object value = null;

            if (transactionExDto.DictAllTransactionField.ContainsKey(transFieldId))
            {
                var transField = transactionExDto.DictAllTransactionField[transFieldId];

                if (transField != null)
                {
                    string unitIdStr = transField.TransactionUnitId.ToString();

                    if (transactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(unitIdStr))
                    {
                        var transactionUnitExDto = transactionExDto.DictAllTransactionUnitIdExDto[unitIdStr];

                        if (transactionUnitExDto == transactionExDto.RootMasterUnit)
                        {
                            value = formData.DictOneToOneFields[transField.DataBaseFieldName];
                        }
                        else if (transactionUnitExDto.IsMasterSiblingUnit.HasValue && transactionUnitExDto.IsMasterSiblingUnit.Value)
                        {
                            value = formData.DictSiblingOneToOneFields[unitIdStr][transField.DataBaseFieldName];
                        }

                    }

                }
            }

            return value;
        }

        public static object GetTransFieldPrintValue(AppTransactionFieldExDto transField, object orgValue, AppTransactionStructureDto aAppTransactionStructureDto)
        {
            object printValue = null;

            if (transField != null && orgValue != null)
            {
                if (transField.ControlType == (int)EmAppControlType.DDL
                    || transField.ControlType == (int)EmAppControlType.AutoComplete)
                {
                    if (transField.EntityId.HasValue)
                    {
                        List<LookupItemDto> aLookupItemDtoList = aAppTransactionStructureDto.DictStandAloneEntityDataSource[transField.EntityId.Value.ToString()];
                        printValue = AppEntityInfoBL.GetLookupItemDisplayByLookupItemId(aLookupItemDtoList, orgValue);
                    }
                }
                else if (transField.ControlType == (int)EmAppControlType.Numeric)
                {
                    int nbDecimal = transField.Nbdecimal.HasValue ? transField.Nbdecimal.Value : 0;
                    if (nbDecimal > 0)
                    {
                        string nbDecimalFormat = "N" + nbDecimal;

                        decimal? orgValueDecimal = ControlTypeValueConverter.ConvertValueToDecimal(orgValue);
                        orgValueDecimal = orgValueDecimal.HasValue ? orgValueDecimal.Value : 0;
                        printValue = orgValueDecimal.Value.ToString(nbDecimalFormat);
                    }
                    else
                    {
                        printValue = ControlTypeValueConverter.ConvertValueToInt(orgValue);
                    }
                }
                else if (transField.ControlType == (int)EmAppControlType.Date)
                {
                    DateTime dateObj = Convert.ToDateTime(orgValue);
                    printValue = dateObj.ToShortDateString();
                }
                else if (transField.ControlType == (int)EmAppControlType.DateTimeDetail)
                {
                    DateTime dateObj = Convert.ToDateTime(orgValue);
                    printValue = dateObj.ToShortDateString() + " " + dateObj.ToShortTimeString();
                }
                else if (transField.ControlType == (int)EmAppControlType.Time)
                {
                    TimeSpan? timeObj = ControlTypeValueConverter.ConvertValueToTimeSpan(orgValue);
                    if (timeObj.HasValue)
                    {
                        printValue = timeObj.Value.ToString();
                    }


                }
                else if (transField.ControlType == (int)EmAppControlType.TextBox
                    || transField.ControlType == (int)EmAppControlType.Memo
                    || transField.ControlType == (int)EmAppControlType.Label)
                {
                    printValue = orgValue.ToString();
                }
                else if (transField.ControlType == (int)EmAppControlType.Image
                    || transField.ControlType == (int)EmAppControlType.File
                    || transField.ControlType == (int)EmAppControlType.Video
                    || transField.ControlType == (int)EmAppControlType.Audio)
                {
                    printValue = orgValue;
                }
                //else if (transField.ControlType == (int)EmAppControlType.CheckBox)
                //{
                //    printValue = orgValue;
                //}
                else
                {
                    printValue = orgValue;
                }


            }

            return printValue;
        }



        public static List<int> GetTransactionFormFileIDList(int? transactionId, string rootPrimaryKeyValue)
        {


            if (transactionId.HasValue && !string.IsNullOrEmpty(rootPrimaryKeyValue))
            {
                AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

                if (appTransactionExDto == null)
                {
                    return null;
                }

                List<int> fileIdList = new List<int>();


                AppTransactionStructureDto aAppTransactionStructureDto = AppTransactionStructureLoadBL.GetAppTransactionStructureDto(transactionId.Value);
                AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId.Value, rootPrimaryKeyValue);

                AppTransactionUnitExDto aRootTransactionUnitExDto = appTransactionExDto.RootMasterUnit;
                Dictionary<int, AppTransactionFieldExDto> dictTransactionFieldIdAndExDto = new Dictionary<int, AppTransactionFieldExDto>();

                // Root Unit
                foreach (AppTransactionFieldExDto transField in aRootTransactionUnitExDto.AppTransactionFieldList)
                {
                    if (!dictTransactionFieldIdAndExDto.ContainsKey((int)transField.Id))
                    {
                        if (aAppformDataDto.DictOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                        {
                            object transFieldOrgValue = aAppformDataDto.DictOneToOneFields[transField.DataBaseFieldName];
                            ExtractTransactionField_FileId(fileIdList, transField, transFieldOrgValue);
                        }
                    }
                }

                // Sib Unit
                if (appTransactionExDto.SibLineTransactionUnitIdExDtoList != null)
                {
                    foreach (AppTransactionUnitExDto sibUnit in appTransactionExDto.SibLineTransactionUnitIdExDtoList)
                    {
                        if (aAppformDataDto.DictSiblingOneToOneFields.ContainsKey(sibUnit.Id.ToString()))
                        {
                            var sibOneToOneFields = aAppformDataDto.DictSiblingOneToOneFields[sibUnit.Id.ToString()];
                            if (sibOneToOneFields != null)
                            {
                                foreach (AppTransactionFieldExDto transField in sibUnit.AppTransactionFieldList)
                                {
                                    if (sibOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                                    {
                                        object transFieldOrgValue = sibOneToOneFields[transField.DataBaseFieldName];
                                        ExtractTransactionField_FileId(fileIdList, transField, transFieldOrgValue);
                                    }
                                }
                            }
                        }
                    }
                }

                // Child Grid Unit
                if (aRootTransactionUnitExDto.Children != null && aRootTransactionUnitExDto.Children.Count > 0)
                {
                    foreach (AppTransactionUnitExDto childUnit in aRootTransactionUnitExDto.Children)
                    {
                        if (aAppformDataDto.DictOneToManyFields.ContainsKey(childUnit.Id.ToString()))
                        {
                            List<AppChildDataDto> childDataDtoList = aAppformDataDto.DictOneToManyFields[childUnit.Id.ToString()];

                            foreach (AppChildDataDto childDataDto in childDataDtoList)
                            {
                                foreach (AppTransactionFieldExDto transField in childUnit.AppTransactionFieldList)
                                {
                                    if (childDataDto.DictOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                                    {
                                        object transFieldOrgValue = childDataDto.DictOneToOneFields[transField.DataBaseFieldName];
                                        ExtractTransactionField_FileId(fileIdList, transField, transFieldOrgValue);
                                    }
                                }

                                // Grandchild Grid Unit
                                if (childUnit.Children != null && childUnit.Children.Count > 0)
                                {
                                    foreach (AppTransactionUnitExDto grandchildUnit in childUnit.Children)
                                    {
                                        if (childDataDto.DictOneToManyFields.ContainsKey(grandchildUnit.Id.ToString()))
                                        {
                                            List<Dictionary<string, object>> grandchildRowDataList =
                                                childDataDto.DictOneToManyFields[grandchildUnit.Id.ToString()]
                                                .Select(o => o.DictOneToOneFields).ToList();

                                            foreach (Dictionary<string, object> dcitGrandchildRowData in grandchildRowDataList)
                                            {
                                                foreach (AppTransactionFieldExDto transField in grandchildUnit.AppTransactionFieldList)
                                                {
                                                    if (dcitGrandchildRowData.ContainsKey(transField.DataBaseFieldName))
                                                    {
                                                        object transFieldOrgValue = dcitGrandchildRowData[transField.DataBaseFieldName];
                                                        ExtractTransactionField_FileId(fileIdList, transField, transFieldOrgValue);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                        }

                    }
                }

                return fileIdList;

            }

            return null;
        }

        private static void ExtractTransactionField_FileId(List<int> fileIdList, AppTransactionFieldExDto transField, object transFieldOrgValue)
        {
            if (transField.ControlType == (int)EmAppControlType.Image
                                                       || transField.ControlType == (int)EmAppControlType.File
                                                       || transField.ControlType == (int)EmAppControlType.Video
                                                       || transField.ControlType == (int)EmAppControlType.Audio)
            {

                int? fileId = ControlTypeValueConverter.ConvertValueToInt(transFieldOrgValue);

                if (fileId.HasValue && !fileIdList.Contains(fileId.Value))
                {
                    fileIdList.Add(fileId.Value);
                }
            }
        }
    }
}