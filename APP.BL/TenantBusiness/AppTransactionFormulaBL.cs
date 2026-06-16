
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.Components.Dto;
using APP.Components.EntityDto;
using System.Text.RegularExpressions;
using APP.Framework.Communication;
using APP.Framework.Validation;
using System.Text;
using APP.Framework.Globalization;
using DynamicExpresso;
using ExpressionEval;
#if NETFRAMEWORK
using System.Web;
#endif
using System.Net;
using System.Data.Common;
using DatabaseSchemaMrg;
using Newtonsoft.Json.Linq;
using Google.Protobuf.WellKnownTypes;

using APP.Framework;
namespace App.BL
{
    // Validation
    public static class AppTransactionFormulaBL
    {
        public static readonly string FormaulPrefix = "transactionfieldid_";
        public static readonly string FormulaLineEnd = ";";
        public static readonly Interpreter TargetInterpreter = new Interpreter()
            .SetFunction("IsNumericHasValue",   (Func<object, bool>)EvaluatorHelpers.IsNumericHasValue)
            .SetFunction("IsDateHasValue",      (Func<object, bool>)EvaluatorHelpers.IsDateHasValue)
            .SetFunction("IsDDLHasValue",       (Func<object, bool>)EvaluatorHelpers.IsDDLHasValue)
            .SetFunction("IsChecBoxkHasValue",  (Func<object, bool>)EvaluatorHelpers.IsChecBoxkHasValue);

        public static readonly string GetJsonNodeValueByPathFunction = "GetJsonNodeValueByPath";
        public static readonly string FindOneItemFromJsonArrayFunction = "FindOneItemFromJsonArray";
        public static readonly string GetOneItemFromJsonArrayByIndexFunction = "GetOneItemFromJsonArrayByIndex";

        public readonly static string FormulaPasswordSaltKey = "BC365A4E-68A0-4A75-9E46-8AB18C64E796";

        public static OperationCallResult<AppMasterDetailDto> ValidateAndCalculateMasterDetailTransactionData(AppMasterDetailDto appformDataDto)
        {
            OperationCallResult<AppMasterDetailDto> aOperationCallResult = new OperationCallResult<AppMasterDetailDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;
            aOperationCallResult.Object = appformDataDto;
            appformDataDto.DictFormulaTypeAndWarningMessage = new Dictionary<int, List<string>>();
            appformDataDto.DictFormulaTypeAndWarningMessage.Add((int)EmAppFormularType.BooleanExpressionError, new List<string>());
            appformDataDto.DictFormulaTypeAndWarningMessage.Add((int)EmAppFormularType.BooleanExpressionWarning, new List<string>());
            appformDataDto.DictTransFieldIdAndWarningHighlightStyleId = new Dictionary<int, int>();


            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

         

                CaculateOneTranscation(appformDataDto, appTransactionExDto);

                List<string> boolExpWarningList = appformDataDto.DictFormulaTypeAndWarningMessage[(int)EmAppFormularType.BooleanExpressionWarning];
                List<string> boolExpErrorList = appformDataDto.DictFormulaTypeAndWarningMessage[(int)EmAppFormularType.BooleanExpressionError];

                if (boolExpWarningList.Count > 0 || boolExpErrorList.Count > 0)
                {
                    aOperationCallResult.ValidationResult = aValidationResult = new ValidationResult();

                    foreach (string aWarningMsg in boolExpWarningList)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), null, ValidationItemType.Warning, aWarningMsg));
                    }

                    foreach (string anErrorMsg in boolExpErrorList)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), null, ValidationItemType.Error, anErrorMsg));
                    }
                }

                if (!string.IsNullOrWhiteSpace(appTransactionExDto.PreSaveValidationMethod))
                {

                    var exCallResult = AppPluginClient.CallTransactionFormExternalService(appformDataDto, appTransactionExDto.PreSaveValidationMethod);
                    aValidationResult.Merge(exCallResult.ValidationResult);
                    if (exCallResult.IsSuccessfulWithResult)
                    {
                        AppMasterDetailFormDataLoadBL.SetupFormConditionLockingDictValue(appTransactionExDto, exCallResult.Object);
                        aOperationCallResult.Object = exCallResult.Object;
                    }

                }



                Dictionary<string, List<AppChildDataDto>> dictChildUnitvalueList = aOperationCallResult.Object.DictOneToManyFields;
                if (!dictChildUnitvalueList.IsEmpty())
                {
                    foreach (var keyvalue in dictChildUnitvalueList)
                    {
                        var childUnitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[keyvalue.Key];
                        AppMasterDetailFormDataLoadBL.SetupStandAloneEntityDepedentFiled(keyvalue.Value, childUnitDto);
                    }
                }


                AppMasterDetailFormDataLoadBL.SetupFormConditionLockingDictValue(appTransactionExDto, aOperationCallResult.Object);

                return aOperationCallResult;
          
        }


        public static OperationCallResult<AppListDataDto> ValidateListEditTransactionData(AppListDataDto appListDataDto)
        {
            OperationCallResult<AppListDataDto> aOperationCallResult = new OperationCallResult<AppListDataDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;
            aOperationCallResult.Object = appListDataDto;

            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appListDataDto.TransactionId);


            ClearListEditTransactionValidationData(appListDataDto);


            AppListDataValidationDto validationResultDto = new AppListDataValidationDto();

            validationResultDto.ListData = new Dictionary<int, AppChildDatValidationResultDto>();

            bool isValid = true;

            if (appTransactionExDto != null && appTransactionExDto.AppTransactionUnitList != null && appListDataDto != null && appListDataDto.ListData != null)
            {
                var rootUnit = appTransactionExDto.AppTransactionUnitList.ElementAt(0);
                if (rootUnit != null)
                {

                    int childRowIndex = 0;

                    foreach (AppChildDataDto aChildRowData in appListDataDto.ListData)
                    {
                        string childGridRowInfo = StringLocalizer.Localize("App_Validation_Row", "Row") + " "
                            + (childRowIndex + 1).ToString() + ": ";

                        foreach (var transField in rootUnit.AppTransactionFieldList)
                        {
                            if (transField.NeedValidator.HasValue && transField.NeedValidator.Value || transField.MaxCharLegnth.HasValue || !(transField.IsAllowEmpty.HasValue && transField.IsAllowEmpty.Value))
                            {
                                object fieldValue = null;

                                if (aChildRowData.DictOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                                {
                                    fieldValue = aChildRowData.DictOneToOneFields[transField.DataBaseFieldName];

                                    bool isFieldDataValid = FirstLevelValidation_ValidateOneTransactionField(false, aValidationResult, transField, fieldValue, null, childGridRowInfo);

                                    if (!isFieldDataValid)
                                    {
                                        AppChildDatValidationResultDto aChildRowVation = null;

                                        if (!validationResultDto.ListData.ContainsKey(childRowIndex))
                                        {
                                            aChildRowVation = new AppChildDatValidationResultDto();
                                            validationResultDto.ListData.Add(childRowIndex, aChildRowVation);
                                        }
                                        else
                                        {
                                            aChildRowVation = validationResultDto.ListData[childRowIndex];
                                        }

                                        if (aChildRowVation.DictOneToOneFields == null)
                                        {
                                            aChildRowVation.DictOneToOneFields = new Dictionary<string, bool>();
                                        }

                                        if (!aChildRowVation.DictOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                                        {
                                            aChildRowVation.DictOneToOneFields.Add(transField.DataBaseFieldName, true);
                                        }

                                        isValid = false;
                                    }
                                }

                            }
                        }

                        // Validate GrandChild Rows
                        bool isAllGrandchildUnitsValid = FirstLevelValidation_ValidateGrandChildUnit(aValidationResult, rootUnit,
                            validationResultDto.ListData, childRowIndex, aChildRowData, childGridRowInfo, null);

                        isValid = isValid && isAllGrandchildUnitsValid;

                        childRowIndex++;
                    }
                }
            }

            if (aValidationResult.HasErrors)
            {
                appListDataDto.ValidationResultDto = validationResultDto;
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), null, ValidationItemType.Message, "Validation Successful"));
            }


            return aOperationCallResult;
        }





        private static void CaculateOneTranscation(AppMasterDetailDto appformDataDto, AppTransactionExDto appTransactionExDto)
        {
            // GenerateMatrix(appformDataDto);

            var dictOneRowFiedIdValue = MergeSiblingUnitValueToRootUnit(appformDataDto, appTransactionExDto);

            LanuchCaculateFromRootSblingValue(appTransactionExDto, appformDataDto, dictOneRowFiedIdValue);

            UpdateRootAndSiblingDbFieldNameValueFromFiedIdValue(appformDataDto, appTransactionExDto, dictOneRowFiedIdValue);

            appformDataDto.DictRootAndSiblingFieldValue = dictOneRowFiedIdValue;
        }

        public static void CaculateOneSearchResult(SearchResultDto searchResultDto, int searchViewId)
        {
            AppTransactionUnitFormulaSetDto formulaSetDto = AppTransactionFormulaSetupBL.RetrieveAppSearchViewFormulaSetDto(searchViewId);

            if (formulaSetDto.OrgFormulaExDtoList.Count > 0)
            {
                AppSearchViewExDto searchViewExDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(searchViewId);

                LanuchCaculateFromSearchViewResult(searchResultDto, searchViewExDto, formulaSetDto.OrgFormulaExDtoList);

                //UpdateRootAndSiblingDbFieldNameValueFromFiedIdValue(appformDataDto, appTransactionExDto, dictOneRowFiedIdValue);

                //appformDataDto.DictRootAndSiblingFieldValue = dictOneRowFiedIdValue;
            }
        }

        public static Dictionary<int, object> MergeSiblingUnitValueToRootUnit(AppMasterDetailDto appformDataDto, AppTransactionExDto appTransactionExDto)
        {

            AppTransactionUnitExDto rootMasterUnit = appTransactionExDto.AppTransactionUnitList.ElementAt(0);


            Dictionary<string, object> dictRootDbFieNameValue = appformDataDto.DictOneToOneFields;

            //Root
            Dictionary<int, object> dictOneRowFiedIdValue = ConvertUnitOneRowDbFileNameToFileId(rootMasterUnit, dictRootDbFieNameValue);

            //Sibling
            if (!appformDataDto.DictSiblingOneToOneFields.IsEmpty())
            {
                Dictionary<int, object> dictSiblingKey = ConvertSiblingUnitOneRowDbFileNameToFileId(appTransactionExDto, appformDataDto);
                dictOneRowFiedIdValue = dictOneRowFiedIdValue.Concat(dictSiblingKey).ToDictionary(o => o.Key, o => o.Value);
            }




            return dictOneRowFiedIdValue;
        }

        public static void UpdateRootAndSiblingDbFieldNameValueFromFiedIdValue(AppMasterDetailDto appformDataDto, AppTransactionExDto appTransactionExDto, Dictionary<int, object> dictOneRowFiedIdValue, AppMasterDetailDto rootWorkflowFrmData = null)
        {
            if (appTransactionExDto == null)
            {
                appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);
            }

            UpdateOneUnitDbFieldNameValueFromFiedIdValue(appTransactionExDto.RootMasterUnit, dictOneRowFiedIdValue, appformDataDto.DictOneToOneFields);

            if (rootWorkflowFrmData != null)
            {
                var rootWorkflowTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootWorkflowFrmData.TransactionId);
                UpdateOneUnitDbFieldNameValueFromFiedIdValue(rootWorkflowTransactionExDto.RootMasterUnit, dictOneRowFiedIdValue, rootWorkflowFrmData.DictOneToOneFields);
            }


            Dictionary<string, Dictionary<string, object>> dictSiblingValue = appformDataDto.DictSiblingOneToOneFields;

            foreach (string siblingUnitId in dictSiblingValue.Keys)
            {
                AppTransactionUnitExDto siblingTransactionUnitExDto = appTransactionExDto.DictAllTransactionUnitIdExDto[siblingUnitId];

                Dictionary<string, object> dictsiblingDbFieNameValue = dictSiblingValue[siblingUnitId];

                UpdateOneUnitDbFieldNameValueFromFiedIdValue(siblingTransactionUnitExDto, dictOneRowFiedIdValue, dictsiblingDbFieNameValue);

            }
        }

        private static Dictionary<int, object> ConvertSiblingUnitOneRowDbFileNameToFileId(AppTransactionExDto appTransactionExDto, AppMasterDetailDto appformDataDto)
        {

            Dictionary<int, object> toReturndictDbFileNameID = new Dictionary<int, object>();


            Dictionary<string, Dictionary<string, object>> dictSiblingValue = appformDataDto.DictSiblingOneToOneFields;

            foreach (string siblingUnitId in dictSiblingValue.Keys)
            {
                AppTransactionUnitExDto siblingTransactionUnitExDto = appTransactionExDto.DictAllTransactionUnitIdExDto[siblingUnitId];

                Dictionary<string, object> dictsiblingDbFieNameValue = dictSiblingValue[siblingUnitId];

                Dictionary<int, object> dictSiblingOneRowFiedIdValue = ConvertUnitOneRowDbFileNameToFileId(siblingTransactionUnitExDto, dictsiblingDbFieNameValue);

                toReturndictDbFileNameID = toReturndictDbFileNameID.Concat(dictSiblingOneRowFiedIdValue).ToDictionary(o => o.Key, o => o.Value);



            }

            return toReturndictDbFileNameID;

        }
        public static AppMasterDetailDto GenerateMatrix(AppMasterDetailDto appformDataDto, int? unitId = null)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

            List<AppTransactionUnitExDto> childMatrixUnit = appTransactionExDto.ChildMatrixUnit;

            // var editCloneDictOneToManyFields = appformDataDto.EditCloneDictOneToManyFields;


            // //appTransactionExDto.dic

            // // need to add column called Matrxi Generate flow  TransactionFlow
            //// childMatrixUnit = childMatrixUnit.Sort (o=>o.

            //foreach (AppTransactionUnitExDto matrixUnit in childMatrixUnit)
            //{
            ProcessMatrixUnit(appformDataDto, appTransactionExDto, unitId);



            //}


            //// foreach (AppTransactionUnitExDto matrixUnit in appTransactionExDto.ChildMatrixPivotUnit )
            //// {
            ////     ProcessOneMatrixUnit(appformDataDto, appTransactionExDto,  matrixUnit);




            //// }

            return appformDataDto;

        }
        //TODO List
        private static AppMasterDetailDto ProcessMatrixUnit(AppMasterDetailDto appformDataDto, AppTransactionExDto appTransactionExDto, int? matrixUnitId = null)
        {
            var editCloneDictOneToManyFields = appformDataDto.EditCloneDictOneToManyFields;

            if (matrixUnitId.HasValue)
            {
                if (appTransactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(matrixUnitId.ToString()))
                {
                    var matraxUnitExdto = appTransactionExDto.DictAllTransactionUnitIdExDto[matrixUnitId.ToString()];
                    //  var matrixUnitSetting = appTransactionExDto.DictMatrixUnitSetting[matraxUnitExdto];


                    PorcessOneMatrixUnit(appformDataDto, appTransactionExDto, editCloneDictOneToManyFields, matraxUnitExdto);
                }


            }
            else
            {
                foreach (var matrixUnitExdto in appTransactionExDto.DictMatrixUnitSetting.Keys)
                {
                    string unitId = matrixUnitExdto.Id.ToString();
                    PorcessOneMatrixUnit(appformDataDto, appTransactionExDto, editCloneDictOneToManyFields, matrixUnitExdto);

                }
            }


            return appformDataDto;

        }

        private static void PorcessOneMatrixUnit(AppMasterDetailDto appformDataDto, AppTransactionExDto appTransactionExDto, Dictionary<string, List<AppChildDataDto>> editCloneDictOneToManyFields, AppTransactionUnitExDto matrixUnit)
        {
            string unitId = matrixUnit.Id.ToString();
            List<AppChildDataDto> oldMatrixUnitDataList = appformDataDto.DictOneToManyFields[unitId];

            List<AppChildDataDto> newMatrixUnitDataList = new List<AppChildDataDto>();
            appformDataDto.DictOneToManyFields[unitId] = newMatrixUnitDataList;

            AppChildDataDto cloneAppChildDataDto = editCloneDictOneToManyFields[unitId].FirstOrDefault();

            ////Key: Matrix UnitID Value, Value Tuple : matrixKeyFieldExdto, matrixForeignKeyFieldExdto, matrixForeignKeyUnit
            List<Tuple<AppTransactionFieldExDto, AppTransactionFieldExDto, AppTransactionUnitExDto>> settingList = appTransactionExDto.DictMatrixUnitSetting[matrixUnit];

            var matrixKeyDbFiledNameList = settingList.Select(o => o.Item1.DataBaseFieldName).ToList();

            Dictionary<string, AppChildDataDto> dictOldKeyValue = new Dictionary<string, AppChildDataDto>();

            foreach (AppChildDataDto aAppChildDataDto in oldMatrixUnitDataList)
            {


                var dictoeToOneFields = aAppChildDataDto.DictOneToOneFields;
                //string oldrowCombineKey = matrixKeyDbFiledNameList.Aggregate((c, n) => dictoeToOneFields[c].ToString() + "_" + dictoeToOneFields[n].ToString());

                string oldrowCombineKey = "";

                foreach (string key in matrixKeyDbFiledNameList)
                {
                    oldrowCombineKey = oldrowCombineKey + dictoeToOneFields[key] + "_";
                }
                if (oldrowCombineKey != "")
                    oldrowCombineKey = oldrowCombineKey.Substring(0, oldrowCombineKey.Length - 1);

                dictOldKeyValue.Add(oldrowCombineKey, aAppChildDataDto);

            }

            //

            List<List<object>> listKeyList = new List<List<object>>();
            Dictionary<string, int> dictKeyDbFiledNameLevel = new Dictionary<string, int>();


            int level = 0;
            foreach (var setting in settingList)
            {

                AppTransactionFieldExDto matrixFiedDto = setting.Item1;

                AppTransactionFieldExDto fkFiledDto = setting.Item2;

                string fkUnitId = fkFiledDto.TransactionUnitId.ToString();
                List<AppChildDataDto> fkUnitDataList = appformDataDto.DictOneToManyFields[fkUnitId];

                List<object> fkIdList = new List<object>();
                fkUnitDataList.ForAll(o => fkIdList.Add(o.DictOneToOneFields[fkFiledDto.DataBaseFieldName]));

                listKeyList.Add(fkIdList);
                dictKeyDbFiledNameLevel.Add(matrixFiedDto.DataBaseFieldName, level);

                level++;

            }


            //CartesianProduct
            var cartesianProductList = listKeyList.CartesianProduct().ToList();


            foreach (var r in cartesianProductList)
            {
                var keyIdList = r.ToList();
                string rowCombineKey = keyIdList.Aggregate((c, n) => c.ToString() + "_" + n.ToString()).ToString();



                // 
                if (dictOldKeyValue.ContainsKey(rowCombineKey))
                {

                    newMatrixUnitDataList.Add(dictOldKeyValue[rowCombineKey]);

                }
                else // it is new conbine key
                {
                    AppChildDataDto newAppChildDataDto = cloneAppChildDataDto.DeepCopy();


                    newMatrixUnitDataList.Add(newAppChildDataDto);
                    // Find the old Row
                    foreach (string fileKey in dictKeyDbFiledNameLevel.Keys)
                    {

                        int levelindex = dictKeyDbFiledNameLevel[fileKey];
                        newAppChildDataDto.DictOneToOneFields[fileKey] = keyIdList[levelindex];

                    }

                }

                // update Noe-key field value

            }
        }

        public static AppMasterDetailDto GenerateMatrixPivot(AppMasterDetailDto appformDataDto)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

            List<AppTransactionUnitExDto> childMatrixPivotUnit = appTransactionExDto.ChildMatrixPivotUnit;

            var editCloneDictOneToManyFields = appformDataDto.EditCloneDictOneToManyFields;


            //appTransactionExDto.dic

            // need to add column called Matrxi Generate flow  TransactionFlow
            // childMatrixUnit = childMatrixUnit.Sort (o=>o.

            foreach (AppTransactionUnitExDto matrixUnit in childMatrixPivotUnit)
            {
                ConvertCartesianProductWithForeighKeyDataSource(appformDataDto, appTransactionExDto, editCloneDictOneToManyFields, matrixUnit);

            }

            return appformDataDto;

        }
        //a Cartesian product is a mathematical operation that returns a set (or product set or simply product) from multiple sets.
        private static void ConvertCartesianProductWithForeighKeyDataSource(AppMasterDetailDto appformDataDto, AppTransactionExDto appTransactionExDto, Dictionary<string, List<AppChildDataDto>> editCloneDictOneToManyFields, AppTransactionUnitExDto matrixUnit)
        {
            string unitId = matrixUnit.Id.ToString();
            List<AppChildDataDto> oldMatrixUnitDataList = appformDataDto.DictOneToManyFields[unitId];



            List<AppChildDataDto> newMatrixUnitDataList = new List<AppChildDataDto>();
            appformDataDto.DictOneToManyFields[unitId] = newMatrixUnitDataList;


            AppChildDataDto cloneAppChildDataDto = editCloneDictOneToManyFields[unitId].FirstOrDefault();


            var matrixFiledList = matrixUnit.AppTransactionFieldList.Where(o => o.MatrixForeignKeyFieldId.HasValue).ToList();


            List<List<object>> listKeyList = new List<List<object>>();
            Dictionary<string, int> dictKeyDbFiledNameLevel = new Dictionary<string, int>();

            int level = 0;
            foreach (var matrixFiedDto in matrixFiledList)
            {

                int foreighKeyFiedId = matrixFiedDto.MatrixForeignKeyFieldId.Value;
                AppTransactionFieldExDto fkFiledDto = appTransactionExDto.DictAllTransactionField[foreighKeyFiedId];
                string fkUnitId = fkFiledDto.TransactionUnitId.ToString();
                List<AppChildDataDto> fkUnitDataList = appformDataDto.DictOneToManyFields[fkUnitId];

                List<object> fkIdList = new List<object>();
                fkUnitDataList.ForAll(o => fkIdList.Add(o.DictOneToOneFields[fkFiledDto.DataBaseFieldName]));

                listKeyList.Add(fkIdList);
                dictKeyDbFiledNameLevel.Add(matrixFiedDto.DataBaseFieldName, level);

                level++;

            }


            Dictionary<string, AppChildDataDto> dictOldKeyValue = new Dictionary<string, AppChildDataDto>();

            foreach (AppChildDataDto aAppChildDataDto in oldMatrixUnitDataList)
            {


                var dictoeToOneFields = aAppChildDataDto.DictOneToOneFields;
                string oldrowCombineKey = dictKeyDbFiledNameLevel.Keys.Aggregate((c, n) => dictoeToOneFields[c].ToString() + "_" + dictoeToOneFields[n].ToString());
                dictOldKeyValue.Add(oldrowCombineKey, aAppChildDataDto);

            }
            //CartesianProduct
            var cartesianProductList = listKeyList.CartesianProduct().ToList();

            foreach (var r in cartesianProductList)
            {
                var keyIdList = r.ToList();
                string rowCombineKey = keyIdList.Aggregate((c, n) => c.ToString() + "_" + n.ToString()).ToString();



                // 
                if (dictOldKeyValue.ContainsKey(rowCombineKey))
                {

                    newMatrixUnitDataList.Add(dictOldKeyValue[rowCombineKey]);

                }
                else // it is new conbine key
                {
                    AppChildDataDto newAppChildDataDto = cloneAppChildDataDto.DeepCopy();


                    newMatrixUnitDataList.Add(newAppChildDataDto);
                    // Find the old Row
                    foreach (string fileKey in dictKeyDbFiledNameLevel.Keys)
                    {

                        int levelindex = dictKeyDbFiledNameLevel[fileKey];
                        newAppChildDataDto.DictOneToOneFields[fileKey] = keyIdList[levelindex];

                    }

                }

                // update Noe-key field value

            }
        }

        private static void LanuchCaculateFromRootSblingValue(AppTransactionExDto appTransactionExDto, AppMasterDetailDto appformDataDto, Dictionary<int, object> dictRootFiedIdValue)
        {
            AppTransactionUnitExDto rootMasterUnit = appTransactionExDto.RootMasterUnit;

            List<int> uncalculateChildUnitIds = new List<int>();
            if (rootMasterUnit.Children != null && rootMasterUnit.Children.Count > 0)
            {
                uncalculateChildUnitIds = rootMasterUnit.Children.Select(o => (int)o.Id).ToList();
            }

            foreach (AppTransactionUnitFormulaExDto formula in rootMasterUnit.AppTransactionUnitFormula_List.OrderBy(o => o.CaculationFlowSort))
            {
                // it is grid formual
                if (formula.ChildTransactionUnitId.HasValue)
                {
                    CaculateChildUnit(appTransactionExDto, appformDataDto, dictRootFiedIdValue, rootMasterUnit, formula);

                    if (uncalculateChildUnitIds.Contains(formula.ChildTransactionUnitId.Value))
                    {
                        uncalculateChildUnitIds.Remove(formula.ChildTransactionUnitId.Value);
                    }
                }
                else // it is regaulr  root level 
                {
                    CaculateOneUnitOneFormula(dictRootFiedIdValue, formula, appTransactionExDto, appformDataDto.DictFormulaTypeAndWarningMessage, appformDataDto.DictTransFieldIdAndWarningHighlightStyleId);
                }
            }

            foreach (int uncalculatedChildUnitId in uncalculateChildUnitIds)
            {
                AppTransactionUnitFormulaExDto formula = new AppTransactionUnitFormulaExDto();
                formula.ChildTransactionUnitId = uncalculatedChildUnitId;
                CaculateChildUnit(appTransactionExDto, appformDataDto, dictRootFiedIdValue, rootMasterUnit, formula);
            }
        }

        private static void LanuchCaculateFromSearchViewResult(SearchResultDto searchResultDto, AppSearchViewExDto searchViewExDto, List<AppTransactionUnitFormulaExDto> formulaList)
        {
            Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField = new Dictionary<int, AppTransactionFieldExDto>();

            searchViewExDto.AppSearchViewFieldList.ForAll(o =>
            {
                AppTransactionFieldExDto transFieldDto = new AppTransactionFieldExDto();
                transFieldDto.FormulaDisplayName = o.FormulaDisplayName;
                transFieldDto.Id = o.Id;
                transFieldDto.DataBaseFieldName = o.SysTableFiledPath;
                transFieldDto.DisplayName = o.DisplayText;
                transFieldDto.ControlType = o.ControlType.HasValue ? o.ControlType.Value : (int)EmAppControlType.TextBox;
                transFieldDto.DataType = o.DataType;
                transFieldDto.EntityId = o.EntityId;
                dictAllTransactionField.Add((int)o.Id, transFieldDto);
            });

            //foreach (StaticSearchResultRowJsonDto resultRow in searchResultDto.SearchResultRowList)
            //{
            //    foreach (var formula in formulaList.OrderBy(o => o.CaculationFlowSort))
            //    {
            //        CaculateSearchViewOneFormula(resultRow, formula, dictAllTransactionField);
            //    }
            //}

            var resultRowList = searchResultDto.SearchResultRowList.ToList();

            for (int searchResultRowIndex = 0; searchResultRowIndex < resultRowList.Count; searchResultRowIndex++)
            {
                StaticSearchResultRowJsonDto resultRow = resultRowList[searchResultRowIndex];

                foreach (var formula in formulaList.OrderBy(o => o.CaculationFlowSort))
                {
                    if (formula.OperationType.HasValue && formula.OperationType.Value == (int)EmAppFormularType.LeadAssignment)
                    {
                        CalculateSearchViewLeadFunction(resultRowList, searchResultRowIndex, resultRow, formula);
                    }
                    else
                    {
                        CaculateSearchViewOneFormula(resultRow, formula, dictAllTransactionField);
                    }
                }
            }


        }

        private static void CalculateSearchViewLeadFunction(List<StaticSearchResultRowJsonDto> resultRowList, int searchResultRowIndex, StaticSearchResultRowJsonDto resultRow, AppTransactionUnitFormulaExDto formula)
        {
            if (formula.LeadFunctionSettingDto != null)
            {
                int? leadFunctionType = formula.LeadFunctionSettingDto.LeadFunctionType;
                int? leadRows = formula.LeadFunctionSettingDto.LeadRows;
                int? leadFieldId = formula.LeadFunctionSettingDto.LeadFieldId;
                int? assignToFieldId = formula.LeadFunctionSettingDto.AssignToFieldId;

                if (leadFunctionType.HasValue && leadRows.HasValue && leadFieldId.HasValue && assignToFieldId.HasValue)
                {
                    object leadValue = null;

                    if (leadFunctionType.Value == (int)EmAppLeadFunctionType.Sum)
                    {
                        decimal sumValue = 0;

                        if (searchResultRowIndex >= leadRows.Value)
                        {
                            for (int j = searchResultRowIndex - leadRows.Value; j < searchResultRowIndex; j++)
                            {
                                var leadRowDto = resultRowList[j];

                                if (leadRowDto.DictViewColumnIDKeyValue.ContainsKey(leadFieldId.Value))
                                {
                                    decimal? leadRowValue = ControlTypeValueConverter.ConvertValueToDecimal(leadRowDto.DictViewColumnIDKeyValue[leadFieldId.Value]);
                                    if (leadRowValue.HasValue)
                                    {
                                        sumValue += leadRowValue.Value;
                                    }
                                }
                            }
                        }

                        leadValue = sumValue;
                    }
                    else if (leadFunctionType.Value == (int)EmAppLeadFunctionType.Average)
                    {
                        decimal sumValue = 0;
                        int countRow = 0;

                        if (searchResultRowIndex >= leadRows.Value)
                        {
                            for (int j = searchResultRowIndex - leadRows.Value; j < searchResultRowIndex; j++)
                            {
                                var leadRowDto = resultRowList[j];

                                if (leadRowDto.DictViewColumnIDKeyValue.ContainsKey(leadFieldId.Value))
                                {
                                    decimal? leadRowValue = ControlTypeValueConverter.ConvertValueToDecimal(leadRowDto.DictViewColumnIDKeyValue[leadFieldId.Value]);
                                    if (leadRowValue.HasValue)
                                    {
                                        sumValue += leadRowValue.Value;
                                        countRow++;
                                    }
                                }
                            }

                            decimal? currentRowValue = ControlTypeValueConverter.ConvertValueToDecimal(resultRow.DictViewColumnIDKeyValue[leadFieldId.Value]);
                            if (currentRowValue.HasValue)
                            {
                                sumValue += currentRowValue.Value;
                                countRow++;
                            }
                        }


                        leadValue = sumValue / countRow;
                    }
                    else if (leadFunctionType.Value == (int)EmAppLeadFunctionType.Min)
                    {
                        decimal? minValue = null;

                        if (searchResultRowIndex >= leadRows.Value)
                        {
                            decimal? currentRowValue = ControlTypeValueConverter.ConvertValueToDecimal(resultRow.DictViewColumnIDKeyValue[leadFieldId.Value]);
                            if (currentRowValue.HasValue)
                            {
                                minValue = currentRowValue.Value;
                            }

                            for (int j = searchResultRowIndex - leadRows.Value; j < searchResultRowIndex; j++)
                            {
                                var leadRowDto = resultRowList[j];

                                if (leadRowDto.DictViewColumnIDKeyValue.ContainsKey(leadFieldId.Value))
                                {
                                    decimal? leadRowValue = ControlTypeValueConverter.ConvertValueToDecimal(leadRowDto.DictViewColumnIDKeyValue[leadFieldId.Value]);
                                    if (leadRowValue.HasValue)
                                    {
                                        if (!minValue.HasValue)
                                        {
                                            minValue = leadRowValue.Value;
                                        }
                                        else if (leadRowValue.Value < minValue.Value)
                                        {
                                            minValue = leadRowValue.Value;
                                        }
                                    }
                                }
                            }
                        }


                        leadValue = minValue;
                    }
                    else if (leadFunctionType.Value == (int)EmAppLeadFunctionType.Max)
                    {
                        decimal? maxValue = null;

                        if (searchResultRowIndex >= leadRows.Value)
                        {
                            decimal? currentRowValue = ControlTypeValueConverter.ConvertValueToDecimal(resultRow.DictViewColumnIDKeyValue[leadFieldId.Value]);
                            if (currentRowValue.HasValue)
                            {
                                maxValue = currentRowValue.Value;
                            }

                            for (int j = searchResultRowIndex - leadRows.Value; j < searchResultRowIndex; j++)
                            {
                                var leadRowDto = resultRowList[j];

                                if (leadRowDto.DictViewColumnIDKeyValue.ContainsKey(leadFieldId.Value))
                                {
                                    decimal? leadRowValue = ControlTypeValueConverter.ConvertValueToDecimal(leadRowDto.DictViewColumnIDKeyValue[leadFieldId.Value]);
                                    if (leadRowValue.HasValue)
                                    {
                                        if (!maxValue.HasValue)
                                        {
                                            maxValue = leadRowValue.Value;
                                        }
                                        else if (leadRowValue.Value > maxValue.Value)
                                        {
                                            maxValue = leadRowValue.Value;
                                        }
                                    }
                                }
                            }
                        }


                        leadValue = maxValue;
                    }

                    resultRow.DictViewColumnIDKeyValue[assignToFieldId.Value] = leadValue;

                }

            }
        }

        private static void CaculateSearchViewOneFormula(StaticSearchResultRowJsonDto resultRow, AppTransactionUnitFormulaExDto formula, Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField)
        {
            Dictionary<int, object> dictOneRowFiedIdValue = resultRow.DictViewColumnIDKeyValue;

            //ConditionFieldId from Root, need to check Filed Leval
            if (formula.IsConditionalAssignMent && formula.ConditionFieldId.HasValue)
            {
                // Condition Dcu in current Tab
                bool? dcuControlValue = null;
                if (dictOneRowFiedIdValue.ContainsKey(formula.ConditionFieldId.Value))
                {
                    dcuControlValue = ControlTypeValueConverter.ConvertValueToBoolean(dictOneRowFiedIdValue[formula.ConditionFieldId.Value]);
                }


                if (dcuControlValue.HasValue && formula.SwitchTrueFalseType.HasValue)
                {
                    // true condition assignment
                    if (formula.SwitchTrueFalseType == true)
                    {
                        if (dcuControlValue.Value)
                        {
                            //  DoSimpleDcuFormulaAssignment(blockEntity, formula, dictRootFiedIdValue, dictBlockSubitenControlType);
                        }
                    }
                    else // false condition assignment
                    {
                        if (dcuControlValue.Value == false)
                        {
                            // DoSimpleDcuFormulaAssignment(blockEntity, formula, dictRootFiedIdValue, dictBlockSubitenControlType);
                        }
                    }
                }
            }// end condition DCU
            else //
            {
                //DoSearchViewOneFormulaCalculation(appTransactionExDto, formula, dictOneRowFiedIdValue);
                DoOneFormulaCalculation(dictAllTransactionField, formula, dictOneRowFiedIdValue, null, null, null);
            }
        }

        //internal static bool DoSearchViewOneFormulaCalculation(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, AppTransactionUnitFormulaExDto transactionUnitFormula, Dictionary<int, object> dictFieldValue)
        //{
        //    Dictionary<int, int> dictBlockSubitenControlType = dictAllTransactionField.Values.ToDictionary(o => (int)o.Id, o => o.ControlType);

        //    if (!string.IsNullOrWhiteSpace(transactionUnitFormula.FormulaExpression))
        //    {
        //        transactionUnitFormula.FormulaExpression = transactionUnitFormula.FormulaExpression.RegexReplace("\n", " ");

        //        //bool isAssignment = !(transactionUnitFormula.OperationType.HasValue && transactionUnitFormula.OperationType.Value == (int)EmAppFormularType.BooleanExpression);
        //        if (transactionUnitFormula.OperationType.HasValue)
        //        {
        //            if (transactionUnitFormula.OperationType.Value == (int)EmAppFormularType.Assignment)
        //            {
        //                if (transactionUnitFormula.FormulaExpression.Contains(AppTransactionFormulaBL.FormulaLineEnd))
        //                {
        //                    string[] formulaExpressionList = transactionUnitFormula.FormulaExpression.Split(AppTransactionFormulaBL.FormulaLineEnd.ToArray());
        //                    foreach (string aFormulaExpression in formulaExpressionList)
        //                    {
        //                        if (!string.IsNullOrWhiteSpace(aFormulaExpression))
        //                        {
        //                            AppTransactionUnitFormulaExDto aFormulaDto = new AppTransactionUnitFormulaExDto() { OperationType = (int)EmAppFormularType.Assignment, FormulaExpression = aFormulaExpression };
        //                            DoOneFormulaCalculation_ProcessOneLineAssignment(dictAllTransactionField, aFormulaDto, dictFieldValue);
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    DoOneFormulaCalculation_ProcessOneLineAssignment(dictAllTransactionField, transactionUnitFormula, dictFieldValue);
        //                }
        //            }
        //            else if (transactionUnitFormula.OperationType.Value == (int)EmAppFormularType.BooleanExpressionWarning)
        //            {
        //                string warningMessage = DoOneFormulaCalculation_ProcessBooleanExpressionMessage(dictAllTransactionField, transactionUnitFormula, dictFieldValue);
        //                if (!string.IsNullOrWhiteSpace(warningMessage) && dictFormulaTypeAndWarningMessage != null)
        //                {
        //                    if (dictFormulaTypeAndWarningMessage.ContainsKey((int)EmAppFormularType.BooleanExpressionWarning))
        //                    {
        //                        dictFormulaTypeAndWarningMessage[(int)EmAppFormularType.BooleanExpressionWarning].Add(warningMessage);
        //                    }

        //                    if (dictTransFieldIdAndWarningHighlightStyleId != null)
        //                    {
        //                        if (transactionUnitFormula.WarningHighlightTransFieldId.HasValue && transactionUnitFormula.WarningHighlightStyleId.HasValue)
        //                        {
        //                            if (!dictTransFieldIdAndWarningHighlightStyleId.ContainsKey(transactionUnitFormula.WarningHighlightTransFieldId.Value))
        //                            {
        //                                dictTransFieldIdAndWarningHighlightStyleId.Add(transactionUnitFormula.WarningHighlightTransFieldId.Value, transactionUnitFormula.WarningHighlightStyleId.Value);
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //            else if (transactionUnitFormula.OperationType.Value == (int)EmAppFormularType.BooleanExpressionError)
        //            {
        //                string warningMessage = DoOneFormulaCalculation_ProcessBooleanExpressionMessage(dictAllTransactionField, transactionUnitFormula, dictFieldValue);
        //                if (!string.IsNullOrWhiteSpace(warningMessage) && dictFormulaTypeAndWarningMessage != null)
        //                {
        //                    if (dictFormulaTypeAndWarningMessage.ContainsKey((int)EmAppFormularType.BooleanExpressionError))
        //                    {
        //                        dictFormulaTypeAndWarningMessage[(int)EmAppFormularType.BooleanExpressionError].Add(warningMessage);
        //                    }

        //                    if (dictTransFieldIdAndWarningHighlightStyleId != null)
        //                    {
        //                        if (transactionUnitFormula.WarningHighlightTransFieldId.HasValue && transactionUnitFormula.WarningHighlightStyleId.HasValue)
        //                        {
        //                            if (!dictTransFieldIdAndWarningHighlightStyleId.ContainsKey(transactionUnitFormula.WarningHighlightTransFieldId.Value))
        //                            {
        //                                dictTransFieldIdAndWarningHighlightStyleId.Add(transactionUnitFormula.WarningHighlightTransFieldId.Value, transactionUnitFormula.WarningHighlightStyleId.Value);
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //            else if (transactionUnitFormula.OperationType.Value == (int)EmAppFormularType.BooleanExpressionDeleteRow)
        //            {
        //                bool isRemoveRow = DoOneFormulaCalculation_ProcessBooleanExpressionResult(dictAllTransactionField, transactionUnitFormula, dictFieldValue);
        //                return isRemoveRow;
        //            }
        //        }
        //    }

        //    return false;
        //}


        private static void CaculateChildUnit(AppTransactionExDto appTransactionExDto, AppMasterDetailDto appformDataDto, Dictionary<int, object> dictRootFiedIdValue, AppTransactionUnitExDto rootMasterUnit, AppTransactionUnitFormulaExDto formula)
        {
            var childTransactionUnit = rootMasterUnit.Children.FirstOrDefault(o => o.Id.ToString() == formula.ChildTransactionUnitId.ToString());
            if (appformDataDto.DictOneToManyFields.ContainsKey(childTransactionUnit.Id.ToString()))
            {
                List<int> uncalculateGrandChildUnitIds = new List<int>();
                if (childTransactionUnit.Children != null && childTransactionUnit.Children.Count > 0)
                {
                    uncalculateGrandChildUnitIds = childTransactionUnit.Children.Select(o => (int)o.Id).ToList();
                }

                List<AppChildDataDto> childDataList = appformDataDto.DictOneToManyFields[childTransactionUnit.Id.ToString()];

                // child need to get subsribe value from parent
                PushParentValueToTheChild(dictRootFiedIdValue, childTransactionUnit, childDataList);

                List<AppChildDataDto> deleteRowList = new List<AppChildDataDto>();

                // do Child Caculation !!
                foreach (AppTransactionUnitFormulaExDto childformula in childTransactionUnit.AppTransactionUnitFormula_List.OrderBy(o => o.CaculationFlowSort))
                {
                    // it is grand child unit formual
                    if (childformula.ChildTransactionUnitId.HasValue)
                    {
                        CalculateGrandChildUnit(appTransactionExDto, appformDataDto, childTransactionUnit, childDataList, childformula, dictRootFiedIdValue);

                        if (uncalculateGrandChildUnitIds.Contains(childformula.ChildTransactionUnitId.Value))
                        {
                            uncalculateGrandChildUnitIds.Remove(childformula.ChildTransactionUnitId.Value);
                        }

                    }
                    else // it is  pure child formula
                    {

                        CaculateChildFormula(childTransactionUnit, childDataList, childformula, appTransactionExDto, appformDataDto.DictFormulaTypeAndWarningMessage, deleteRowList, dictRootFiedIdValue);
                    }
                }

                foreach (int uncalculatedGrandChildUnitId in uncalculateGrandChildUnitIds)
                {
                    AppTransactionUnitFormulaExDto childformula = new AppTransactionUnitFormulaExDto();
                    childformula.ChildTransactionUnitId = uncalculatedGrandChildUnitId;
                    CalculateGrandChildUnit(appTransactionExDto, appformDataDto, childTransactionUnit, childDataList, childformula, dictRootFiedIdValue);
                }

                if (deleteRowList.Count > 0)
                {
                    foreach (var row in deleteRowList)
                    {
                        childDataList.Remove(row);
                    }
                }


                // need to pass Child Aggegation result to the parent 

                // need to publish aggegation to out side

                CaculateChildAggToParent(rootMasterUnit, dictRootFiedIdValue, childTransactionUnit, childDataList);


            }
        }

        private static void CalculateGrandChildUnit(AppTransactionExDto appTransactionExDto, AppMasterDetailDto appformDataDto, AppTransactionUnitExDto childTransactionUnit, List<AppChildDataDto> childDataList, AppTransactionUnitFormulaExDto childformula, Dictionary<int, object> dictRootFiedIdValue)
        {
            var grandChildTransactionUnit = childTransactionUnit.Children.FirstOrDefault(o => o.Id.ToString() == childformula.ChildTransactionUnitId.ToString());
            string grandChildTransactionUnitId = grandChildTransactionUnit.Id.ToString();
            foreach (AppChildDataDto appChildDataDto in childDataList)
            {
                Dictionary<string, object> dictChildDbFieNameValue = appChildDataDto.DictOneToOneFields;
                Dictionary<int, object> dictChildFiedIdValue = ConvertUnitOneRowDbFileNameToFileId(childTransactionUnit, dictChildDbFieNameValue, dictRootFiedIdValue);
                if (appChildDataDto.DictOneToManyFields.ContainsKey(grandChildTransactionUnitId))
                {
                    List<Dictionary<string, object>> grandChildDateList = appChildDataDto.DictOneToManyFields[grandChildTransactionUnitId]
                        .Select(o => o.DictOneToOneFields).ToList();
                    PushChildValueToGrandChild(dictChildFiedIdValue, grandChildTransactionUnit, grandChildDateList);

                    List<Dictionary<string, object>> needToDeleteRowList = new List<Dictionary<string, object>>();

                    foreach (AppTransactionUnitFormulaExDto grandchildformula in grandChildTransactionUnit.AppTransactionUnitFormula_List.OrderBy(o => o.CaculationFlowSort))
                    {
                        CaculateGrandChildFormula(grandChildTransactionUnit, grandChildDateList, grandchildformula, appTransactionExDto, appformDataDto.DictFormulaTypeAndWarningMessage, needToDeleteRowList, dictChildFiedIdValue);
                    }

                    if (needToDeleteRowList.Count > 0)
                    {
                        foreach (var needToDeleteRow in needToDeleteRowList)
                        {
                            var deleteRow = appChildDataDto.DictOneToManyFields[grandChildTransactionUnitId].FirstOrDefault(o => o.DictOneToOneFields == needToDeleteRow);
                            if (deleteRow != null)
                            {
                                appChildDataDto.DictOneToManyFields[grandChildTransactionUnitId].Remove(deleteRow);
                            }
                        }

                        grandChildDateList = appChildDataDto.DictOneToManyFields[grandChildTransactionUnitId]
                            .Select(o => o.DictOneToOneFields).ToList();
                    }

                    CaculateGrandChildAggToParent(childTransactionUnit, dictChildFiedIdValue, grandChildTransactionUnit, grandChildDateList);

                }
                UpdateOneUnitDbFieldNameValueFromFiedIdValue(childTransactionUnit, dictChildFiedIdValue, dictChildDbFieNameValue);

            }
        }

        private static void CaculateGrandChildAggToParent(AppTransactionUnitExDto childTransactionUnit, Dictionary<int, object> dictChildFiedIdValue, AppTransactionUnitExDto grandChildTransactionUnit, List<Dictionary<string, object>> grandChildDateList)
        {
            //var allList = ConvertAppChildDataToList(childTransactionUnit, childDataList);

            List<Dictionary<int, object>> allList = new List<Dictionary<int, object>>();

            foreach (Dictionary<string, object> dictGrandChildDbFieNameValue in grandChildDateList)
            {
                Dictionary<int, object> dictGrandChildFiedIdValue = ConvertUnitOneRowDbFileNameToFileId(grandChildTransactionUnit, dictGrandChildDbFieNameValue);
                allList.Add(dictGrandChildFiedIdValue);

            }

            foreach (var grandChildfield in grandChildTransactionUnit.AppTransactionFieldList)
            {
                foreach (var aggfuntion in grandChildfield.AppTransactionFieldAggFunction_List)
                {
                    if (aggfuntion.AggregationFunctionType == (int)EmAppAggregationFunctionType.SUM)
                    {


                        var result = allList.Sum(o => ControlTypeValueConverter.ConvertValueToDecimal(o[(int)grandChildfield.Id]));

                        //  var  aggfuntion.id
                        var parentSubfiled = childTransactionUnit.AppTransactionFieldList.FirstOrDefault(o => o.ParentUnitSubscribeChildAggFunctionId.HasValue && o.ParentUnitSubscribeChildAggFunctionId.Value == (int)aggfuntion.Id);
                        if (parentSubfiled != null)
                        {
                            dictChildFiedIdValue[(int)parentSubfiled.Id] = result;

                        }


                    }

                    else if (aggfuntion.AggregationFunctionType == (int)EmAppAggregationFunctionType.ConcatenateString)
                    {
                        string result = GetOneUnitFiledConcatenateString(allList, grandChildfield);

                        //  var  aggfuntion.id
                        var parentSubfiled = childTransactionUnit.AppTransactionFieldList.FirstOrDefault(o => o.ParentUnitSubscribeChildAggFunctionId.HasValue && o.ParentUnitSubscribeChildAggFunctionId.Value == (int)aggfuntion.Id);
                        if (parentSubfiled != null)
                        {
                            dictChildFiedIdValue[(int)parentSubfiled.Id] = result;

                        }


                    }
                    else if (aggfuntion.AggregationFunctionType == (int)EmAppAggregationFunctionType.RowCount)
                    {


                        var result = allList.Count();

                        //  var  aggfuntion.id
                        var parentSubfiled = childTransactionUnit.AppTransactionFieldList.FirstOrDefault(o => o.ParentUnitSubscribeChildAggFunctionId.HasValue && o.ParentUnitSubscribeChildAggFunctionId.Value == (int)aggfuntion.Id);
                        if (parentSubfiled != null)
                        {
                            dictChildFiedIdValue[(int)parentSubfiled.Id] = result;

                        }


                    }

                    else if (aggfuntion.AggregationFunctionType == (int)EmAppAggregationFunctionType.BooleanSum)
                    {

                        bool? result = null;

                        allList.ForAll(o =>
                        {
                            bool? itemValue = ControlTypeValueConverter.ConvertValueToBoolean(o[(int)grandChildfield.Id]);
                            if (!itemValue.HasValue)
                            {
                                itemValue = false;
                            }

                            if (!result.HasValue)
                            {
                                result = itemValue.Value;
                            }
                            else
                            {
                                result = result.Value && itemValue.Value;
                            }

                        });

                        var parentSubfiled = childTransactionUnit.AppTransactionFieldList.FirstOrDefault(o => o.ParentUnitSubscribeChildAggFunctionId.HasValue && o.ParentUnitSubscribeChildAggFunctionId.Value == (int)aggfuntion.Id);
                        if (parentSubfiled != null)
                        {
                            dictChildFiedIdValue[(int)parentSubfiled.Id] = result;

                        }

                    }

                }

            }
        }



        private static void CaculateGrandChildFormula(AppTransactionUnitExDto grandChildTransactionUnit, List<Dictionary<string, object>> grandChildDateList, AppTransactionUnitFormulaExDto grandchildformula,
            AppTransactionExDto appTransactionExDto, Dictionary<int, List<string>> dictFormulaTypeAndWarningMessage, List<Dictionary<string, object>> needToDeleteRowList, Dictionary<int, object> dictChildFiedIdValue)
        {
            foreach (Dictionary<string, object> dictGrandChildDbFieNameValue in grandChildDateList)
            {
                Dictionary<int, object> dictGrandChildFiedIdValue = ConvertUnitOneRowDbFileNameToFileId(grandChildTransactionUnit, dictGrandChildDbFieNameValue, dictChildFiedIdValue);
                bool isRemoveRow = CaculateOneUnitOneFormula(dictGrandChildFiedIdValue, grandchildformula, appTransactionExDto, dictFormulaTypeAndWarningMessage, null);
                UpdateOneUnitDbFieldNameValueFromFiedIdValue(grandChildTransactionUnit, dictGrandChildFiedIdValue, dictGrandChildDbFieNameValue);

                if (isRemoveRow)
                {
                    needToDeleteRowList.Add(dictGrandChildDbFieNameValue);
                }

            }


        }

        private static void PushChildValueToGrandChild(Dictionary<int, object> dictChildFiedIdValue, AppTransactionUnitExDto grandChildTransactionUnit, List<Dictionary<string, object>> grandChildDateList)
        {

            foreach (Dictionary<string, object> dictGrandChildDbFieNameValue in grandChildDateList)
            {
                //  var dictChildDbFieNameValue = aAppformChildDataDto.DictOneToOneFields;

                Dictionary<int, object> dictGrandChildFiedIdValue = ConvertUnitOneRowDbFileNameToFileId(grandChildTransactionUnit, dictGrandChildDbFieNameValue);

                // child need to get subsribe value from parent

                foreach (int childFiedId in grandChildTransactionUnit.DictChildUnitSubscribeFieldIdParentFieldId.Keys)
                {
                    int parentFieldId = grandChildTransactionUnit.DictChildUnitSubscribeFieldIdParentFieldId[childFiedId];

                    dictGrandChildFiedIdValue[childFiedId] = dictChildFiedIdValue[parentFieldId];

                }


                UpdateOneUnitDbFieldNameValueFromFiedIdValue(grandChildTransactionUnit, dictGrandChildFiedIdValue, dictGrandChildDbFieNameValue);



            }
        }

        // ////// child need to get subsribe value from parent
        private static void PushParentValueToTheChild(Dictionary<int, object> dictRootFiedIdValue, AppTransactionUnitExDto childTransactionUnit, List<AppChildDataDto> childDataList)
        {



            foreach (AppChildDataDto aAppformChildDataDto in childDataList)
            {
                var dictChildDbFieNameValue = aAppformChildDataDto.DictOneToOneFields;

                Dictionary<int, object> dictChildFiedIdValue = ConvertUnitOneRowDbFileNameToFileId(childTransactionUnit, dictChildDbFieNameValue);

                // child need to get subsribe value from parent

                foreach (int childFiedId in childTransactionUnit.DictChildUnitSubscribeFieldIdParentFieldId.Keys)
                {
                    int parentFieldId = childTransactionUnit.DictChildUnitSubscribeFieldIdParentFieldId[childFiedId];

                    dictChildFiedIdValue[childFiedId] = dictRootFiedIdValue[parentFieldId];

                }


                UpdateOneUnitDbFieldNameValueFromFiedIdValue(childTransactionUnit, dictChildFiedIdValue, dictChildDbFieNameValue);



            }
        }

        private static void CaculateChildFormula(AppTransactionUnitExDto childTransactionUnit, List<AppChildDataDto> childDataList, AppTransactionUnitFormulaExDto childformula, AppTransactionExDto appTransactionExDto, Dictionary<int, List<string>> dictFormulaTypeAndWarningMessage, List<AppChildDataDto> deleteRowList, Dictionary<int, object> dictRootFiedIdValue)
        {


            foreach (AppChildDataDto aAppformChildDataDto in childDataList)
            {
                Dictionary<string, object> dictChildDbFieNameValue = aAppformChildDataDto.DictOneToOneFields;

                Dictionary<int, object> dictChildFiedIdValue = ConvertUnitOneRowDbFileNameToFileId(childTransactionUnit, dictChildDbFieNameValue, dictRootFiedIdValue);

                foreach (var rootLevelFieldKv in dictRootFiedIdValue)
                {
                    if (!dictChildFiedIdValue.ContainsKey(rootLevelFieldKv.Key))
                    {
                        dictChildFiedIdValue.Add(rootLevelFieldKv.Key, rootLevelFieldKv.Value);
                    }
                }


                bool isRemoveRow = CaculateOneUnitOneFormula(dictChildFiedIdValue, childformula, appTransactionExDto, dictFormulaTypeAndWarningMessage, null);

                UpdateOneUnitDbFieldNameValueFromFiedIdValue(childTransactionUnit, dictChildFiedIdValue, dictChildDbFieNameValue);


                if (isRemoveRow)
                {
                    deleteRowList.Add(aAppformChildDataDto);
                }



            }


        }
        private static List<Dictionary<int, object>> ConvertAppChildDataToList(AppTransactionUnitExDto childTransactionUnit, List<AppChildDataDto> childDataList)
        {
            List<Dictionary<int, object>> allList = new List<Dictionary<int, object>>();
            foreach (AppChildDataDto aAppformChildDataDto in childDataList)
            {
                var dictChildDbFieNameValue = aAppformChildDataDto.DictOneToOneFields;
                Dictionary<int, object> dictChildFiedIdValue = ConvertUnitOneRowDbFileNameToFileId(childTransactionUnit, dictChildDbFieNameValue);

                allList.Add(dictChildFiedIdValue);

            }

            return allList;

        }


        private static void CaculateChildAggToParent(AppTransactionUnitExDto rootMasterUnit, Dictionary<int, object> dictRootFiedIdValue, AppTransactionUnitExDto childTransactionUnit, List<AppChildDataDto> childDataList)
        {


            var allList = ConvertAppChildDataToList(childTransactionUnit, childDataList);

            // List<Dictionary<int, object>> allList

            foreach (var childfield in childTransactionUnit.AppTransactionFieldList)
            {
                foreach (var aggfuntion in childfield.AppTransactionFieldAggFunction_List)
                {
                    if (aggfuntion.AggregationFunctionType == (int)EmAppAggregationFunctionType.SUM)
                    {


                        var result = allList.Sum(o => ControlTypeValueConverter.ConvertValueToDecimal(o[(int)childfield.Id]));

                        //  var  aggfuntion.id
                        //var parentSubfiled = rootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.ParentUnitSubscribeChildAggFunctionId.HasValue && o.ParentUnitSubscribeChildAggFunctionId.Value == (int)aggfuntion.Id);
                        //if (parentSubfiled != null)
                        //{
                        //    dictRootFiedIdValue[(int)parentSubfiled.Id] = result;

                        //}

                        PassChildAggResultToRootAndSiblinUnit(rootMasterUnit, dictRootFiedIdValue, aggfuntion, result);

                    }
                    else if (aggfuntion.AggregationFunctionType == (int)EmAppAggregationFunctionType.RowCount)
                    {


                        var result = allList.Count();

                        //  var  aggfuntion.id

                        PassChildAggResultToRootAndSiblinUnit(rootMasterUnit, dictRootFiedIdValue, aggfuntion, result);

                    }

                    else if (aggfuntion.AggregationFunctionType == (int)EmAppAggregationFunctionType.ConcatenateString)
                    {
                        string result = GetOneUnitFiledConcatenateString(allList, childfield);

                        PassChildAggResultToRootAndSiblinUnit(rootMasterUnit, dictRootFiedIdValue, aggfuntion, result);



                    }

                    else if (aggfuntion.AggregationFunctionType == (int)EmAppAggregationFunctionType.BooleanSum)
                    {

                        bool? result = null;
                        allList.ForAll(o =>
                        {
                            bool? itemValue = ControlTypeValueConverter.ConvertValueToBoolean(o[(int)childfield.Id]);
                            if (!itemValue.HasValue)
                            {
                                itemValue = false;
                            }

                            if (!result.HasValue)
                            {
                                result = itemValue.Value;
                            }
                            else
                            {
                                result = result.Value && itemValue.Value;
                            }

                        });

                        PassChildAggResultToRootAndSiblinUnit(rootMasterUnit, dictRootFiedIdValue, aggfuntion, result);



                    }
                }

            }
        }

        private static string GetOneUnitFiledConcatenateString(List<Dictionary<int, object>> allList, AppTransactionFieldExDto childfield)
        {
            string result = "";
            int grandChildfieldId = (int)childfield.Id;

            List<string> listString;

            if (childfield.EntityId.HasValue && childfield.ControlType == (int)EmAppControlType.DDL)
            {
                int entityId = childfield.EntityId.Value;

                List<object> valuIds = allList
                    .Where(o => o[grandChildfieldId] != null)
                    .Select(o => o[grandChildfieldId]).ToList();
                listString = AppEntityInfoBL.GetEntityFirstDisplayFiedList(entityId, valuIds);

            }
            else
            {
                listString = allList.Where(o => o[grandChildfieldId] != null).Select(o => o[grandChildfieldId].ToString()).ToList();



            }
            if (!listString.IsEmpty())
            {
                result = listString.Aggregate
               (
                 (o, p) => o + ", " + p
                );
            }

            return result;
        }

        private static void PassChildAggResultToRootAndSiblinUnit(AppTransactionUnitExDto rootMasterUnit,
            Dictionary<int, object> dictRootFiedIdValue,
            AppTransactionFieldAggFunctionExDto aggfuntion,
            object result)
        {
            AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootMasterUnit.TransactionId);

            var parentSubfiled = rootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.ParentUnitSubscribeChildAggFunctionId.HasValue && o.ParentUnitSubscribeChildAggFunctionId.Value == (int)aggfuntion.Id);
            if (parentSubfiled != null)
            {
                dictRootFiedIdValue[(int)parentSubfiled.Id] = result;

            }

            if (!aAppTransactionExDto.SibLineTransactionUnitIdExDtoList.IsEmpty())
            {
                foreach (var sibUnit in aAppTransactionExDto.SibLineTransactionUnitIdExDtoList)
                {
                    var subLingSubfiled = sibUnit.AppTransactionFieldList.FirstOrDefault(o => o.ParentUnitSubscribeChildAggFunctionId.HasValue && o.ParentUnitSubscribeChildAggFunctionId.Value == (int)aggfuntion.Id);
                    if (subLingSubfiled != null)
                    {
                        dictRootFiedIdValue[(int)subLingSubfiled.Id] = result;

                    }
                }
            }
        }

        private static bool CaculateOneUnitOneFormula(Dictionary<int, object> dictOneRowFiedIdValue, AppTransactionUnitFormulaExDto formula, AppTransactionExDto appTransactionExDto, Dictionary<int, List<string>> dictFormulaTypeAndWarningMessage, Dictionary<int, int> dictTransFieldIdAndWarningHighlightStyleId)
        {
            bool isRemoveRow = false;

            //ConditionFieldId from Root, need to check Filed Leval
            if (formula.IsConditionalAssignMent && formula.ConditionFieldId.HasValue)
            {
                // Condition Dcu in current Tab
                bool? dcuControlValue = null;
                if (dictOneRowFiedIdValue.ContainsKey(formula.ConditionFieldId.Value))
                {
                    dcuControlValue = ControlTypeValueConverter.ConvertValueToBoolean(dictOneRowFiedIdValue[formula.ConditionFieldId.Value]);
                }


                if (dcuControlValue.HasValue && formula.SwitchTrueFalseType.HasValue)
                {
                    // true condition assignment
                    if (formula.SwitchTrueFalseType == true)
                    {
                        if (dcuControlValue.Value)
                        {
                            //  DoSimpleDcuFormulaAssignment(blockEntity, formula, dictRootFiedIdValue, dictBlockSubitenControlType);
                        }
                    }
                    else // false condition assignment
                    {
                        if (dcuControlValue.Value == false)
                        {
                            // DoSimpleDcuFormulaAssignment(blockEntity, formula, dictRootFiedIdValue, dictBlockSubitenControlType);
                        }
                    }
                }
            }// end condition DCU
            else //
            {
                isRemoveRow = DoTransactionOneFormulaCalculation(appTransactionExDto, formula, dictOneRowFiedIdValue, dictFormulaTypeAndWarningMessage, dictTransFieldIdAndWarningHighlightStyleId);
                // need to pass cross relationship some where




            }

            return isRemoveRow;
        }

        public static Dictionary<int, object> ConvertUnitOneRowDbFileNameToFileId(AppTransactionUnitExDto rootMasterUnit, Dictionary<string, object> dictOneRowFieldNameValue, Dictionary<int, object> dictNeedToMergeParentLevelFieldIdAndValue = null)
        {

            Dictionary<string, int> dictDbFileNameID = rootMasterUnit.DictDbFileNameFieldId;



            Dictionary<int, object> dictOneRowFiedIdValue = new Dictionary<int, object>();


            foreach (string dbField in dictOneRowFieldNameValue.Keys)
            {
                if (dictDbFileNameID.ContainsKey(dbField))
                {
                    int fieldId = dictDbFileNameID[dbField];
                    dictOneRowFiedIdValue.Add(fieldId, dictOneRowFieldNameValue[dbField]);

                }
            }

            if (dictNeedToMergeParentLevelFieldIdAndValue != null)
            {
                foreach (var parentLevelFieldKv in dictNeedToMergeParentLevelFieldIdAndValue)
                {
                    if (!dictOneRowFiedIdValue.ContainsKey(parentLevelFieldKv.Key))
                    {
                        dictOneRowFiedIdValue.Add(parentLevelFieldKv.Key, parentLevelFieldKv.Value);
                    }
                }
            }


            return dictOneRowFiedIdValue;
        }

        internal static Dictionary<string, object> UpdateOneUnitDbFieldNameValueFromFiedIdValue(AppTransactionUnitExDto oneUnit, Dictionary<int, object> dictOneRowFiedIdValue, Dictionary<string, object> dictOneRowFieldNameValue)
        {

            Dictionary<int, string> dictDbFileNameID = oneUnit.DictFieldIdDbFileName;
            //  Dictionary<string, object> dictOneRowFieldNameValue = new Dictionary<string,object> ();

            foreach (int fieldId in dictOneRowFiedIdValue.Keys)
            {
                if (dictDbFileNameID.ContainsKey(fieldId))
                {
                    string dbfieldName = dictDbFileNameID[fieldId];
                    dictOneRowFieldNameValue[dbfieldName] = dictOneRowFiedIdValue[fieldId];

                }
            }
            return dictOneRowFieldNameValue;
        }

        //AppTransactionUnitExDto rootMasterUnit

        internal static bool DoTransactionOneFormulaCalculation(AppTransactionExDto appTransactionExDto,
            AppTransactionUnitFormulaExDto transactionUnitFormula,
            Dictionary<int, object> dictFieldIdValue, Dictionary<int, List<string>> dictFormulaTypeAndWarningMessage, Dictionary<int, int> dictTransFieldIdAndWarningHighlightStyleId,
            AppTransactionExDto rootWorkflowTransaction = null)
        {
            var dictAllTransactionField = appTransactionExDto.DictAllTransactionField; ;

            if (rootWorkflowTransaction != null && rootWorkflowTransaction.DictAllTransactionField != null)
            {
                foreach (var kvPair in rootWorkflowTransaction.DictAllTransactionField)
                {
                    if (!dictAllTransactionField.ContainsKey(kvPair.Key))
                    {
                        dictAllTransactionField.Add(kvPair.Key, kvPair.Value);
                    }
                }
            }

            return DoOneFormulaCalculation(dictAllTransactionField, transactionUnitFormula, dictFieldIdValue, dictFormulaTypeAndWarningMessage, dictTransFieldIdAndWarningHighlightStyleId, appTransactionExDto);
        }


        internal static bool DoOneFormulaCalculation(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, AppTransactionUnitFormulaExDto transactionUnitFormula, Dictionary<int, object> dictFieldValue,
            Dictionary<int, List<string>> dictFormulaTypeAndWarningMessage, Dictionary<int, int> dictTransFieldIdAndWarningHighlightStyleId, AppTransactionExDto appTransactionExDto)
        {
            //var dictAllTransactionField = appTransactionExDto.DictAllTransactionField;
            //[transactionFieldId];
            Dictionary<int, int> dictBlockSubitenControlType = dictAllTransactionField.Values.ToDictionary(o => (int)o.Id, o => o.ControlType);

            if (!string.IsNullOrWhiteSpace(transactionUnitFormula.FormulaExpression))
            {
                transactionUnitFormula.FormulaExpression = Regex.Replace(transactionUnitFormula.FormulaExpression, "\n", " ");

                //bool isAssignment = !(transactionUnitFormula.OperationType.HasValue && transactionUnitFormula.OperationType.Value == (int)EmAppFormularType.BooleanExpression);
                if (transactionUnitFormula.OperationType.HasValue)
                {
                    if (transactionUnitFormula.OperationType.Value == (int)EmAppFormularType.Assignment)
                    {
                        if (transactionUnitFormula.FormulaExpression.Contains(AppTransactionFormulaBL.FormulaLineEnd))
                        {
                            string[] formulaExpressionList = transactionUnitFormula.FormulaExpression.Split(AppTransactionFormulaBL.FormulaLineEnd.ToArray());
                            foreach (string aFormulaExpression in formulaExpressionList)
                            {
                                if (!string.IsNullOrWhiteSpace(aFormulaExpression))
                                {
                                    AppTransactionUnitFormulaExDto aFormulaDto = new AppTransactionUnitFormulaExDto() { OperationType = (int)EmAppFormularType.Assignment, FormulaExpression = aFormulaExpression };
                                    DoOneFormulaCalculation_ProcessOneLineAssignment(dictAllTransactionField, aFormulaDto, dictFieldValue);
                                }
                            }
                        }
                        else
                        {
                            DoOneFormulaCalculation_ProcessOneLineAssignment(dictAllTransactionField, transactionUnitFormula, dictFieldValue);
                        }
                    }
                    else if (transactionUnitFormula.OperationType.Value == (int)EmAppFormularType.SqlScarlarAssignment)
                    {
                        if (transactionUnitFormula.FormulaExpression.Contains(AppTransactionFormulaBL.FormulaLineEnd))
                        {
                            string[] formulaExpressionList = transactionUnitFormula.FormulaExpression.Split(AppTransactionFormulaBL.FormulaLineEnd.ToArray());
                            foreach (string aFormulaExpression in formulaExpressionList)
                            {
                                if (!string.IsNullOrWhiteSpace(aFormulaExpression))
                                {
                                    AppTransactionUnitFormulaExDto aFormulaDto = new AppTransactionUnitFormulaExDto() { OperationType = (int)EmAppFormularType.SqlScarlarAssignment, FormulaExpression = aFormulaExpression };
                                    DoOneFormulaCalculation_ProcessOneLineSQLAssignment(dictAllTransactionField, aFormulaDto, dictFieldValue, appTransactionExDto);
                                }
                            }
                        }
                        else
                        {
                            DoOneFormulaCalculation_ProcessOneLineSQLAssignment(dictAllTransactionField, transactionUnitFormula, dictFieldValue, appTransactionExDto);
                        }
                    }
                    else if (transactionUnitFormula.OperationType.Value == (int)EmAppFormularType.BooleanExpressionWarning)
                    {
                        string warningMessage = DoOneFormulaCalculation_ProcessBooleanExpressionMessage(dictAllTransactionField, transactionUnitFormula, dictFieldValue);
                        if (!string.IsNullOrWhiteSpace(warningMessage) && dictFormulaTypeAndWarningMessage != null)
                        {
                            if (dictFormulaTypeAndWarningMessage.ContainsKey((int)EmAppFormularType.BooleanExpressionWarning))
                            {
                                dictFormulaTypeAndWarningMessage[(int)EmAppFormularType.BooleanExpressionWarning].Add(warningMessage);
                            }

                            if (dictTransFieldIdAndWarningHighlightStyleId != null)
                            {
                                if (transactionUnitFormula.WarningHighlightTransFieldId.HasValue && transactionUnitFormula.WarningHighlightStyleId.HasValue)
                                {
                                    if (!dictTransFieldIdAndWarningHighlightStyleId.ContainsKey(transactionUnitFormula.WarningHighlightTransFieldId.Value))
                                    {
                                        dictTransFieldIdAndWarningHighlightStyleId.Add(transactionUnitFormula.WarningHighlightTransFieldId.Value, transactionUnitFormula.WarningHighlightStyleId.Value);
                                    }
                                }
                            }
                        }
                    }
                    else if (transactionUnitFormula.OperationType.Value == (int)EmAppFormularType.BooleanExpressionError)
                    {
                        string warningMessage = DoOneFormulaCalculation_ProcessBooleanExpressionMessage(dictAllTransactionField, transactionUnitFormula, dictFieldValue);
                        if (!string.IsNullOrWhiteSpace(warningMessage) && dictFormulaTypeAndWarningMessage != null)
                        {
                            if (dictFormulaTypeAndWarningMessage.ContainsKey((int)EmAppFormularType.BooleanExpressionError))
                            {
                                dictFormulaTypeAndWarningMessage[(int)EmAppFormularType.BooleanExpressionError].Add(warningMessage);
                            }

                            if (dictTransFieldIdAndWarningHighlightStyleId != null)
                            {
                                if (transactionUnitFormula.WarningHighlightTransFieldId.HasValue && transactionUnitFormula.WarningHighlightStyleId.HasValue)
                                {
                                    if (!dictTransFieldIdAndWarningHighlightStyleId.ContainsKey(transactionUnitFormula.WarningHighlightTransFieldId.Value))
                                    {
                                        dictTransFieldIdAndWarningHighlightStyleId.Add(transactionUnitFormula.WarningHighlightTransFieldId.Value, transactionUnitFormula.WarningHighlightStyleId.Value);
                                    }
                                }
                            }
                        }
                    }
                    else if (transactionUnitFormula.OperationType.Value == (int)EmAppFormularType.BooleanExpressionDeleteRow)
                    {
                        bool isRemoveRow = DoOneFormulaCalculation_ProcessBooleanExpressionResult(dictAllTransactionField, transactionUnitFormula, dictFieldValue);
                        return isRemoveRow;
                    }
                }
            }

            return false;
        }

        private static void DoOneFormulaCalculation_ProcessOneLineAssignment(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, AppTransactionUnitFormulaExDto transactionUnitFormula, Dictionary<int, object> dictFiedIdValue)
        {
            AppTransactionFieldExDto leftSideFiedExdto = dictAllTransactionField[transactionUnitFormula.AssignmentLeftSideFieldId]; // transactionUnitExDto.DicFieldIdFieldExdto[transactionUnitFormula.AssignmentLeftSideFieldId];


            if (leftSideFiedExdto != null)
            {
                int leftSideKey = (int)leftSideFiedExdto.Id;

                // aBlockFormula.IsDateBeTween || aBlockFormula.IsDateBeTween

                if (transactionUnitFormula.FunctionType.HasValue && transactionUnitFormula.FunctionType.Value == (int)EmAppFormularFunctionType.DateDiff)
                {

                    DoTwoDateDiffCaculation(dictAllTransactionField, transactionUnitFormula, dictFiedIdValue, leftSideKey);

                }
                if (transactionUnitFormula.FunctionType.HasValue && transactionUnitFormula.FunctionType.Value == (int)EmAppFormularFunctionType.Encrypt)
                {

                    DoStrigEncryptCaculation(dictAllTransactionField, transactionUnitFormula, dictFiedIdValue, leftSideKey);

                }

                else if (transactionUnitFormula.IsContainDateConst
                      || leftSideFiedExdto.ControlType == (int)APP.Components.Dto.EmAppControlType.Date
                      || leftSideFiedExdto.ControlType == (int)APP.Components.Dto.EmAppControlType.DateTimeDetail
                      || leftSideFiedExdto.ControlType == (int)APP.Components.Dto.EmAppControlType.Time
                    )
                {
                    #region----------------  Datetime Formual


                    string rightSideDateimEXpress = RightSideDatetimeAssignmentEXpressWithRealValue(dictAllTransactionField, transactionUnitFormula.RightSideExpression, dictFiedIdValue);

                    try
                    {
                        object exResult = ParseAndEvaluteExpress(rightSideDateimEXpress);

                        dictFiedIdValue[leftSideKey] = exResult;

                    }
                    catch (Exception ex)
                    {

                    }


                    #endregion------------ End Datatime formula
                }               
                else
                {
                    #region -------------- Regular Math formual

                    // ExpressionEval aExpressionEval = new ExpressionEval();
                    string rightSideEXpress = RightSideAssignmentEXpressWithRealValue(dictAllTransactionField, transactionUnitFormula.RightSideExpression, dictFiedIdValue);

                    if (rightSideEXpress.IndexOf(EmBLFiledMappingSystemTokenField.CurrentUserID.ToString(), StringComparison.InvariantCultureIgnoreCase) != -1)
                    {

                        string currentUserToken = string.Format("[{0}]", EmBLFiledMappingSystemTokenField.CurrentUserID.ToString());

                        string userIdStr = "";
                        int? userId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentUid);

                        if (userId.HasValue)
                        {
                            userIdStr = userId.Value.ToString();
                        }

                        rightSideEXpress = rightSideEXpress.Replace(currentUserToken, userIdStr);
                    }

                    if (rightSideEXpress.IndexOf(EmBLFiledMappingSystemTokenField.CurrentPartnerID.ToString(), StringComparison.InvariantCultureIgnoreCase) != -1)
                    {

                        string token = string.Format("[{0}]", EmBLFiledMappingSystemTokenField.CurrentPartnerID.ToString());
                        string partnerIdStr = "";

                        int? partnerId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId;

                        if (partnerId.HasValue)
                        {
                            partnerIdStr = partnerId.Value.ToString();
                        }


                        rightSideEXpress = rightSideEXpress.Replace(token, partnerIdStr);
                    }


                    if (rightSideEXpress.IndexOf(EmBLFiledMappingSystemTokenField.CurrentUserIPAddress.ToString(), StringComparison.InvariantCultureIgnoreCase) != -1)
                    {

                        string token = string.Format("[{0}]", EmBLFiledMappingSystemTokenField.CurrentUserIPAddress.ToString());

                        string userIPAddress = GetClientIpAddress();


                        rightSideEXpress = rightSideEXpress.Replace(token, "\"" + userIPAddress + "\"");
                    }


                    try
                    {
                        //  aExpressionEval.Expression = rightSideEXpress;
                        //  object exResult = aExpressionEval.Evaluate();

                        //   object exResult = Evaluator.EvaluateToObject(rightSideEXpress);

                        if (leftSideFiedExdto.ControlType == (int)APP.Components.Dto.EmAppControlType.JsonObject)
                        {
                            dictFiedIdValue[leftSideKey] = rightSideEXpress;
                        }
                        else
                        {
                            object exResult = ParseAndEvaluteExpress(rightSideEXpress);


                            if (transactionUnitFormula.FunctionType.HasValue)
                            {
                                int functionaType = transactionUnitFormula.FunctionType.Value;

                                if (functionaType == (int)EmAppFormularFunctionType.Ceiling)
                                {
                                    exResult = System.Math.Ceiling(double.Parse(exResult.ToString()));
                                }
                                else if (functionaType == (int)EmAppFormularFunctionType.Floor)
                                {
                                    exResult = System.Math.Floor(double.Parse(exResult.ToString()));
                                }
                                else if (functionaType == (int)EmAppFormularFunctionType.Round)
                                {
                                    //MinDecimalPlaces
                                    if (leftSideFiedExdto.Nbdecimal.HasValue)
                                    {
                                        exResult = System.Math.Round(double.Parse(exResult.ToString()), leftSideFiedExdto.Nbdecimal.Value);
                                    }
                                    else
                                    {
                                        exResult = System.Math.Round(double.Parse(exResult.ToString()));
                                    }
                                }
                                else if (functionaType == (int)EmAppFormularFunctionType.Abs)
                                {
                                    exResult = System.Math.Abs(double.Parse(exResult.ToString()));
                                }
                                else if (functionaType == (int)EmAppFormularFunctionType.ToLower)
                                {
                                    exResult = exResult.ToString().ToLower();
                                }
                                else if (functionaType == (int)EmAppFormularFunctionType.ToUpper)
                                {
                                    exResult = exResult.ToString().ToUpper();
                                }
                            }

                            dictFiedIdValue[leftSideKey] = exResult;
                        }

                        

                        
                    }
                    catch (Exception ex)
                    {
                        string eexms = ex.ToString();
                    }

                    #endregion
                }

            }
        }

        private static void DoOneFormulaCalculation_ProcessOneLineSQLAssignment(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, AppTransactionUnitFormulaExDto transactionUnitFormula,
            Dictionary<int, object> dictFiedIdValue, AppTransactionExDto appTransactionExDto)
        {
            AppTransactionFieldExDto leftSideFiedExdto = dictAllTransactionField[transactionUnitFormula.AssignmentLeftSideFieldId]; // transactionUnitExDto.DicFieldIdFieldExdto[transactionUnitFormula.AssignmentLeftSideFieldId];


            if (leftSideFiedExdto != null && appTransactionExDto != null)
            {
                int leftSideKey = (int)leftSideFiedExdto.Id;




                #region -------------- SQL Query Result Assignment


                List<DbParameter> sqlParamterList = new List<DbParameter>();

                string sqlStateMent = AppProjectWorkFlowDataFormSynchBL.ReplaceSQlParamterWithActualValue(dictFiedIdValue, transactionUnitFormula.RightSideExpression, sqlParamterList);


                try
                {
                    DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(appTransactionExDto.DataSourceFrom, null);

                    //string errorMsg = query;

                    List<object> resultSet = new List<object>();
                    string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, sqlStateMent, false, sqlParamterList, resultSet);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        //toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, errorMsg));

                    }
                    else
                    {
                        if (resultSet.Count == 1)
                        {
                            dictFiedIdValue[leftSideKey] = resultSet[0];

                        }
                    }

                }
                catch (Exception ex)
                {
                    string eexms = ex.ToString();
                }


                #endregion


            }
        }

        internal static string GetClientIpAddress()
        {
            string userip = "";

#if NETFRAMEWORK
            // TODO-PHASE4: Replace with IHttpContextAccessor
            try
            {
                string userHostAddress = HttpContext.Current.Request.UserHostAddress;
                if (userHostAddress != null)
                {

                    Int64 macinfo = new Int64();
                    string macSrc = macinfo.ToString("X");
                    if (macSrc == "0")
                    {
                        if (userHostAddress == "127.0.0.1" || userHostAddress == "::1")
                        {
                            // userip = GetServerIpAddress();
                        }
                        else
                        {
                            userip = userHostAddress;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
#endif

            return userip;
        }


        internal static string GetServerIpAddress()
        {
            try
            {

                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);

                foreach (IPAddress address in addresses)
                {
                    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // IPv4
                    {
                        return address.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception, if necessary
            }

            return "";
        }


        internal static object ParseAndEvaluteExpress(string rightSideEXpress)
        {
            object exResult;
            //if (
            //         (rightSideEXpress.IndexOf("string.IsNullOrEmpty") != -1) ||
            //         (rightSideEXpress.IndexOf("IsNumericHasValue") != -1) ||
            //         (rightSideEXpress.IndexOf("IsDateHasValue") != -1) ||
            //         (rightSideEXpress.IndexOf("IsDDLHasValue") != -1) ||
            //         (rightSideEXpress.IndexOf("IsChecBoxkHasValue") != -1) ||
            //         (rightSideEXpress.IndexOf("?") != -1 && rightSideEXpress.IndexOf(":") != -1)
            //    )
            //{
            //    rightSideEXpress = Regex.Replace(rightSideEXpress, "IsNumericHasValue", "PlmExService.IsNumericHasValue");
            //    rightSideEXpress = Regex.Replace(rightSideEXpress, "IsDateHasValue", "PlmExService.IsDateHasValue");
            //    rightSideEXpress = Regex.Replace(rightSideEXpress, "IsDDLHasValue", "PlmExService.IsDDLHasValue");
            //    rightSideEXpress = Regex.Replace(rightSideEXpress, "IsChecBoxkHasValue", "PlmExService.IsChecBoxkHasValue");

            //    exResult = TargetInterpreter.Eval(rightSideEXpress);
            //}

            ////else
            ////{
            ////	ExpressionEval aExpressionEval = new ExpressionEval();
            ////	aExpressionEval.Expression = rightSideEXpress;

            ////	exResult = aExpressionEval.Evaluate();
            ////}
            exResult = TargetInterpreter.Eval(rightSideEXpress);

            return exResult;
        }
        private static string DoOneFormulaCalculation_ProcessBooleanExpressionMessage(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, AppTransactionUnitFormulaExDto transactionUnitFormula, Dictionary<int, object> dictChangeBlockSubitemValue)
        {
            string rightSideEXpress = RightSideAssignmentEXpressWithRealValue(dictAllTransactionField, transactionUnitFormula.FormulaExpression, dictChangeBlockSubitemValue);

            if (rightSideEXpress.IndexOf(EmBLFiledMappingSystemTokenField.CurrentUserID.ToString(), StringComparison.InvariantCultureIgnoreCase) != -1)
            {

                string currentUserToken = string.Format("[{0}]", EmBLFiledMappingSystemTokenField.CurrentUserID.ToString());

                rightSideEXpress = rightSideEXpress.Replace(currentUserToken, ServerContext.Instance.CurrentUid.ToString());
            }
            try
            {
                //  aExpressionEval.Expression = rightSideEXpress;
                //  object exResult = aExpressionEval.Evaluate();

                object exResult = ParseAndEvaluteExpress(rightSideEXpress);

                bool? booleanResult = ControlTypeValueConverter.ConvertValueToBoolean(exResult);

                if (!booleanResult.HasValue || !booleanResult.Value)
                {
                    string warningMessage = StringLocalizer.Localize(transactionUnitFormula.WarningMessage, transactionUnitFormula.WarningMessage);
                    return warningMessage;
                }

            }
            catch (Exception ex)
            {
                string eexms = ex.ToString();
            }

            return string.Empty;
        }

        private static bool DoOneFormulaCalculation_ProcessBooleanExpressionResult(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, AppTransactionUnitFormulaExDto transactionUnitFormula, Dictionary<int, object> dictChangeBlockSubitemValue)
        {
            string rightSideEXpress = RightSideAssignmentEXpressWithRealValue(dictAllTransactionField, transactionUnitFormula.FormulaExpression, dictChangeBlockSubitemValue);

            if (rightSideEXpress.IndexOf(EmBLFiledMappingSystemTokenField.CurrentUserID.ToString(), StringComparison.InvariantCultureIgnoreCase) != -1)
            {

                string currentUserToken = string.Format("[{0}]", EmBLFiledMappingSystemTokenField.CurrentUserID.ToString());

                rightSideEXpress = rightSideEXpress.Replace(currentUserToken, ServerContext.Instance.CurrentUid.ToString());
            }

            try
            {
                //  aExpressionEval.Expression = rightSideEXpress;
                //  object exResult = aExpressionEval.Evaluate();

                object exResult = ParseAndEvaluteExpress(rightSideEXpress);

                bool? booleanResult = ControlTypeValueConverter.ConvertValueToBoolean(exResult);

                if (booleanResult.HasValue && booleanResult.Value)
                {
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }

            return false;
        }


        //internal static Dictionary<int, object> DoCrossTransactionFormulaAssignment(List<AppTransactionExDto> transactionExDtoList, AppTransactionUnitFormulaExDto formulaExDto, Dictionary<int, Dictionary<int, object>> dict_TransId_TransFieldId_Value)
        //{
        //    Dictionary<string, AppTransactionUnitExDto> mergedDictAllTransactionField = new Dictionary<string, AppTransactionUnitExDto>();
        //    Dictionary<int, object> mergedDictTransFieldIdValue = new Dictionary<int, object>();

        //    AppTransactionExDto mergedTransactionExDto = new AppTransactionExDto();
        //    mergedTransactionExDto.DictAllTransactionUnitIdExDto = new Dictionary<string, AppTransactionUnitExDto>();


        //    foreach (AppTransactionExDto aTransactionExDto in transactionExDtoList)
        //    {
        //        foreach (string unitIdKey in aTransactionExDto.DictAllTransactionUnitIdExDto.Keys)
        //        {
        //            if (!mergedTransactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(unitIdKey))
        //            {
        //                mergedTransactionExDto.DictAllTransactionUnitIdExDto.Add(unitIdKey, aTransactionExDto.DictAllTransactionUnitIdExDto[unitIdKey]);
        //            }
        //        }
        //    }

        //    foreach (Dictionary<int, object> dictTransFieldIdValue in dict_TransId_TransFieldId_Value.Values)
        //    {
        //        foreach (int transFieldId in dictTransFieldIdValue.Keys)
        //        {
        //            if (!mergedDictTransFieldIdValue.ContainsKey(transFieldId))
        //            {
        //                mergedDictTransFieldIdValue.Add(transFieldId, dictTransFieldIdValue[transFieldId]);
        //            }
        //        }
        //    }

        //    AppTransactionFormulaBL.DoOneFormulaAssignment(mergedTransactionExDto, formulaExDto, mergedDictTransFieldIdValue);

        //    return mergedDictTransFieldIdValue;
        //}

        private static string RightSideDatetimeAssignmentEXpressWithRealValue(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, string rightSideExpression, Dictionary<int, object> dictFiedIdValue)
        {
            foreach (int transactionFieldId in dictFiedIdValue.Keys)
            {
                AppTransactionFieldExDto transactionField = dictAllTransactionField[transactionFieldId];

                string transactionFieldToken = FormaulPrefix + transactionField.Id.ToString();

                object controlValue = dictFiedIdValue[(int)transactionField.Id];
                int controlType = transactionField.ControlType;
                rightSideExpression = SetupDateTimeControlUnitRightSideExpress(rightSideExpression, transactionFieldToken, controlValue, controlType);

            }

            if (rightSideExpression.IndexOf(EmAppDateTimeTokenType.Today.ToString(), StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                DateTime clientNow = ClientTimeZoneHelper.ConvertUTCToClientDateTime(DateTime.UtcNow);
                DateTime clientToday = clientNow.Date;
                DateTime clientTodayUtcTime = ClientTimeZoneHelper.ConvertClientToUTCDateTime(clientToday);

                string today = string.Format("new DateTime({0} ,{1} ,{2} ,{3} ,{4} ,{5} )  ", clientTodayUtcTime.Year, clientTodayUtcTime.Month, clientTodayUtcTime.Day, clientTodayUtcTime.Hour, clientTodayUtcTime.Minute, clientTodayUtcTime.Second);

                //   string today = " new DateTime(" + todayTicksString + ")";   //string.Format("new DateTime({0} ,{1} ,{2} ,{3} ,{4} ,{5} )", dtValue.Year, dtValue.Month, dtValue.Day, dtValue.Hour, dtValue.Minute, dtValue.Second);

                string todayToken = string.Format("[{0}]", EmAppDateTimeTokenType.Today.ToString());

                rightSideExpression = rightSideExpression.Replace(todayToken, today);
            }

            if (rightSideExpression.IndexOf(EmAppDateTimeTokenType.Now.ToString(), StringComparison.InvariantCultureIgnoreCase) != -1)
            {

                DateTime clientNow = ClientTimeZoneHelper.ConvertUTCToClientDateTime(DateTime.UtcNow);
                string today = string.Format("new DateTime({0} ,{1} ,{2} ,{3} ,{4} ,{5} )  ", clientNow.Year, clientNow.Month, clientNow.Day, clientNow.Hour, clientNow.Minute, clientNow.Second);


                string token = string.Format("[{0}]", EmAppDateTimeTokenType.Now.ToString());

                rightSideExpression = rightSideExpression.Replace(token, today);
            }

            return rightSideExpression;
        }

        private static string SetupDateTimeControlUnitRightSideExpress(string rightSideExpression, string transactionFieldToken, object controlValue, int controlType)
        {
            if (rightSideExpression.ToLower().Contains(transactionFieldToken))
            {
                bool isDataControlUnitEmpty = (controlValue == null || string.IsNullOrEmpty(controlValue.ToString()));

                // cannot  convert null to string
                string aRepalceString = string.Empty;
                if (controlValue == null)
                {
                    aRepalceString = "null";

                }
                else  // not null
                {

                    aRepalceString = controlValue.ToString();

                    if (
                          controlType == (int)APP.Components.Dto.EmAppControlType.Date
                         || controlType == (int)APP.Components.Dto.EmAppControlType.DateTimeDetail)
                    {
                        DateTime? dateTimeValue = ControlTypeValueConverter.ConvertValueToDate(aRepalceString);
                        if (dateTimeValue.HasValue)
                        {
                            var dtValue = dateTimeValue.Value;
                            //   var dttime =  new DateTime(dtValue.Year , dtValue.Month ,dtValue.Day ,dtValue.Hour ,dtValue.Minute ,dtValue.Second );
                            aRepalceString = string.Format("new DateTime({0} ,{1} ,{2} ,{3} ,{4} ,{5} )  ", dtValue.Year, dtValue.Month, dtValue.Day, dtValue.Hour, dtValue.Minute, dtValue.Second);
                        }

                        //new DateTime (
                    }

                    else if (controlType == (int)APP.Components.Dto.EmAppControlType.Time)
                    {
                        if (isDataControlUnitEmpty)
                        {
                            aRepalceString = "0";
                        }
                        else // need to conver time as minutus 
                        {

                            string[] hoursMinues = aRepalceString.Split(':');
                            if (hoursMinues.Length == 2)
                            {
                                try
                                {
                                    int totalMinuts = (int.Parse(hoursMinues[0]) * 60) + int.Parse(hoursMinues[1]);
                                    aRepalceString = totalMinuts.ToString();
                                }
                                catch
                                {
                                }

                            }
                        }

                    }
                }


                rightSideExpression = rightSideExpression.Replace(transactionFieldToken, aRepalceString);
            }

            return rightSideExpression;
        }


        internal static string RightSideAssignmentEXpressWithRealValue(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, string rightSideExpression, Dictionary<int, object> dictFiedIdValue)
        {
            if (rightSideExpression.Contains(GetJsonNodeValueByPathFunction))
            {
                rightSideExpression = RightSideAssignmentEXpress_GetJsonNodeValueByPathFunctionResult(dictAllTransactionField, rightSideExpression, dictFiedIdValue);
            }

            if (rightSideExpression.Contains(FindOneItemFromJsonArrayFunction))
            {
                rightSideExpression = RightSideAssignmentEXpress_FindOneItemFromJsonArrayFunctionResult(dictAllTransactionField, rightSideExpression, dictFiedIdValue);
            }

            if (rightSideExpression.Contains(GetOneItemFromJsonArrayByIndexFunction))
            {
                rightSideExpression = RightSideAssignmentEXpress_GetOneItemFromJsonArrayByIndexFunctionResult(dictAllTransactionField, rightSideExpression, dictFiedIdValue);
            }

            foreach (int transactionFieldId in dictFiedIdValue.Keys)
            {

                var transactionField = dictAllTransactionField[transactionFieldId];

                string controlToken = FormaulPrefix + transactionField.Id.ToString();
                if (

                    transactionField.ControlType != (int)EmAppControlType.Label &&
                    transactionField.ControlType != (int)EmAppControlType.RGBColorDisplay)
                {
                    object controlValue = dictFiedIdValue[(int)transactionField.Id];
                    int controlType = transactionField.ControlType;
                    rightSideExpression = SetupControlUnitRightSideExpress(rightSideExpression, controlToken, controlValue, controlType);
                }
            }

            return rightSideExpression;
        }





        private static string RightSideAssignmentEXpress_GetJsonNodeValueByPathFunctionResult(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, string rightSideExpression, Dictionary<int, object> dictFiedIdValue)
        {
            string pattern = GetJsonNodeValueByPathFunction + @"\((.*)\)";

            MatchCollection matches = Regex.Matches(rightSideExpression, pattern);

            List<string> jsonFunctionSectionList = new List<string>();

            foreach (Match match in matches)
            {
                string content = match.Groups[1].Value;
                jsonFunctionSectionList.Add(content);
            }


            if (jsonFunctionSectionList.Count > 0)
            {
                foreach (string jsonFunctionSection in jsonFunctionSectionList)
                {
                    string funcReturnValue = "\"\"";

                    List<string> jsonFuncParams = jsonFunctionSection.Split(',').ToList();

                    if (jsonFuncParams.Count == 2)
                    {
                        string jsonString = jsonFuncParams[0].Trim();
                        jsonString = RightSideAssignmentEXpressWithRealValue(dictAllTransactionField, jsonString, dictFiedIdValue).Trim();

                        string nodePath = jsonFuncParams[1].Trim();
                        nodePath = RightSideAssignmentEXpressWithRealValue(dictAllTransactionField, nodePath, dictFiedIdValue).Replace("\"", "").Trim();

                        if (!string.IsNullOrWhiteSpace(jsonString) && !string.IsNullOrWhiteSpace(nodePath))
                        {
                            try
                            {
                                string value = GetJsonStringNodeValueByPath(jsonString, nodePath, dictAllTransactionField, dictFiedIdValue);

                                funcReturnValue = "\"" + value + "\"";
                            }
                            catch (Exception ex)
                            {

                            }

                        }
                    }

                    var _rp1Old = GetJsonNodeValueByPathFunction + "(" + jsonFunctionSection + ")";
                    var _rp1Idx = rightSideExpression.IndexOf(_rp1Old, StringComparison.Ordinal);
                    rightSideExpression = _rp1Idx >= 0 ? rightSideExpression.Substring(0, _rp1Idx) + funcReturnValue + rightSideExpression.Substring(_rp1Idx + _rp1Old.Length) : rightSideExpression;
                }
            }


            return rightSideExpression;
        }

        private static string RightSideAssignmentEXpress_FindOneItemFromJsonArrayFunctionResult(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, string rightSideExpression, Dictionary<int, object> dictFiedIdValue)
        {
            string pattern = FindOneItemFromJsonArrayFunction + @"\((.*)\)";

            MatchCollection matches = Regex.Matches(rightSideExpression, pattern);

            List<string> jsonFunctionSectionList = new List<string>();

            foreach (Match match in matches)
            {
                string content = match.Groups[1].Value;
                jsonFunctionSectionList.Add(content);
            }


            if (jsonFunctionSectionList.Count > 0)
            {
                foreach (string jsonFunctionSection in jsonFunctionSectionList)
                {
                    string funcReturnValue = "\"\"";

                    List<string> jsonFuncParams = jsonFunctionSection.Split(',').ToList();

                    if (jsonFuncParams.Count == 3)
                    {
                        string jsonString = jsonFuncParams[0].Trim();
                        jsonString = RightSideAssignmentEXpressWithRealValue(dictAllTransactionField, jsonString, dictFiedIdValue).Trim();

                        string propPath = jsonFuncParams[1].Trim();
                        propPath = RightSideAssignmentEXpressWithRealValue(dictAllTransactionField, propPath, dictFiedIdValue).Replace("\"", "").Trim();

                        string needToFindPropValue = jsonFuncParams[2].Trim();
                        needToFindPropValue = RightSideAssignmentEXpressWithRealValue(dictAllTransactionField, needToFindPropValue, dictFiedIdValue).Replace("\"", "").Trim();

                        if (!string.IsNullOrWhiteSpace(jsonString) && !string.IsNullOrWhiteSpace(propPath))
                        {
                            try
                            {
                                JToken arrayNode = JToken.Parse(jsonString.Trim('"'));

                                if (arrayNode != null && arrayNode.Type == JTokenType.Array)
                                {
                                    foreach (JToken jToken_arrayItem in (JArray)arrayNode)
                                    {
                                        string keyvalue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(jToken_arrayItem.SelectToken(propPath));

                                        if (keyvalue == needToFindPropValue)
                                        {
                                            string value = jToken_arrayItem.ToString();
                                            funcReturnValue = funcReturnValue = "\"" + value + "\""; ;
                                        }
                                    }

                                }
                            }
                            catch (Exception ex)
                            {

                            }

                        }
                    }

                    var _rp2Old = FindOneItemFromJsonArrayFunction + "(" + jsonFunctionSection + ")";
                    var _rp2Idx = rightSideExpression.IndexOf(_rp2Old, StringComparison.Ordinal);
                    rightSideExpression = _rp2Idx >= 0 ? rightSideExpression.Substring(0, _rp2Idx) + funcReturnValue + rightSideExpression.Substring(_rp2Idx + _rp2Old.Length) : rightSideExpression;
                }
            }


            return rightSideExpression;
        }

        private static string RightSideAssignmentEXpress_GetOneItemFromJsonArrayByIndexFunctionResult(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, string rightSideExpression, Dictionary<int, object> dictFiedIdValue)
        {
            string pattern = GetOneItemFromJsonArrayByIndexFunction + @"\((.*)\)";

            MatchCollection matches = Regex.Matches(rightSideExpression, pattern);

            List<string> jsonFunctionSectionList = new List<string>();

            foreach (Match match in matches)
            {
                string content = match.Groups[1].Value;
                jsonFunctionSectionList.Add(content);
            }


            if (jsonFunctionSectionList.Count > 0)
            {
                foreach (string jsonFunctionSection in jsonFunctionSectionList)
                {
                    string funcReturnValue = "\"\"";

                    List<string> jsonFuncParams = jsonFunctionSection.Split(',').ToList();

                    if (jsonFuncParams.Count == 2)
                    {
                        string jsonString = jsonFuncParams[0].Trim();
                        jsonString = RightSideAssignmentEXpressWithRealValue(dictAllTransactionField, jsonString, dictFiedIdValue).Trim();

                        string indexString = jsonFuncParams[1].Trim();
                        indexString = RightSideAssignmentEXpressWithRealValue(dictAllTransactionField, indexString, dictFiedIdValue).Replace("\"", "").Trim();

                        int? index = ControlTypeValueConverter.ConvertValueToInt(indexString);

                        if (!string.IsNullOrWhiteSpace(jsonString) && index.HasValue)
                        {
                            try
                            {
                                JToken arrayNode = JToken.Parse(jsonString.Trim('"'));

                                if (arrayNode != null && arrayNode.Type == JTokenType.Array)
                                {
                                    JArray jArray = (JArray)arrayNode;

                                    if (jArray.Count >= index.Value + 1)
                                    {
                                        string value = jArray[index.Value].ToString();
                                        funcReturnValue = funcReturnValue = "\"" + value + "\""; ;
                                    }

                                }
                            }
                            catch (Exception ex)
                            {

                            }

                        }
                    }

                    var _rp3Old = GetOneItemFromJsonArrayByIndexFunction + "(" + jsonFunctionSection + ")";
                    var _rp3Idx = rightSideExpression.IndexOf(_rp3Old, StringComparison.Ordinal);
                    rightSideExpression = _rp3Idx >= 0 ? rightSideExpression.Substring(0, _rp3Idx) + funcReturnValue + rightSideExpression.Substring(_rp3Idx + _rp3Old.Length) : rightSideExpression;
                }
            }


            return rightSideExpression;
        }

        private static string GetJsonStringNodeValueByPath(string jsonString, string nodePath, Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, Dictionary<int, object> dictFiedIdValue)
        {
            try
            {
               
                var jObj = JObject.Parse(jsonString.Trim('"'));

                if (nodePath.StartsWith("."))
                {
                    nodePath = nodePath.Substring(1);
                }

                string value = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(jObj.SelectToken(nodePath));

                return value;

            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private static string SetupControlUnitRightSideExpress(string rightSideExpression, string controlToken, object controlValue, int controlType)
        {


            string matchSubItem = string.Format(@"\b{0}\b", controlToken);

            if (Regex.IsMatch(rightSideExpression, matchSubItem)) //; conditionWhereClause.Contains(matchSubItem))
            {

                rightSideExpression = DoMatchReplace(rightSideExpression, controlToken, controlValue, controlType);


            }


            //if (rightSideExpression.ToLower().Contains(controlToken))
            //{
            //	rightSideExpression = DoMatchReplace(rightSideExpression, controlToken, controlValue, controlType);
            //}
            return rightSideExpression;
        }

        private static string DoMatchReplace(string rightSideExpression, string controlToken, object controlValue, int controlType)
        {
            bool isValueEmpty = (controlValue == null || string.IsNullOrEmpty(controlValue.ToString()));

            // cannot  convert null to string
            string aRepalceString = string.Empty;
            if (controlValue != null)
                aRepalceString = controlValue.ToString();

            if (
                controlType == (int)EmAppControlType.TextBox ||
                controlType == (int)EmAppControlType.Memo ||

                controlType == (int)EmAppControlType.AutoGeneration ||
                controlType == (int)EmAppControlType.Date ||
                controlType == (int)EmAppControlType.DateTimeDetail ||
                controlType == (int)EmAppControlType.Time
                //|| controlType == (int)EmAppControlType.JsonObject

                )
            {
                if (isValueEmpty)
                {
                    aRepalceString = "\"\"";

                    //if (rightSideExpression.Contains("||") || rightSideExpression.Contains("&&") || rightSideExpression.Contains("!="))
                    //{

                    //    aRepalceString = " string.Empty";
                    //}


                }
                else
                {
                    aRepalceString = aRepalceString.Replace("\"", "\\\"");

                    aRepalceString = "\"" + aRepalceString + "\"";



                }
            }
            else if (
                  controlType == (int)EmAppControlType.Numeric



                )
            {
                if (isValueEmpty)
                {
                    aRepalceString = "0";
                }
            }

            else if (
                         controlType == (int)EmAppControlType.DDL
                         || controlType == (int)EmAppControlType.RadioButtons
                         || controlType == (int)EmAppControlType.Progress

                 )
            {
                if (isValueEmpty)
                {
                    aRepalceString = "null";
                }
            }

            else if (

                controlType == (int)EmAppControlType.Image



              )
            {
                if (isValueEmpty || aRepalceString == "-1")
                {
                    //aRepalceString = "\"\"";
                    aRepalceString = "null";
                }
            }

            else if (

              controlType == (int)EmAppControlType.CheckBox



            )
            {
                if (isValueEmpty)
                {
                    aRepalceString = "\"\"";
                }
                else
                {
                    aRepalceString = aRepalceString.ToLowerInvariant();

                }
            }


            rightSideExpression = rightSideExpression.Replace(controlToken, aRepalceString);
            return rightSideExpression;
        }




        // FirstLevelValidation
        public static OperationCallResult<AppMasterDetailDto> MasterDetailDataFirstLevelValidation(AppMasterDetailDto appformDataDto )
        {

            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

            OperationCallResult<AppMasterDetailDto> aOperationCallResult = new OperationCallResult<AppMasterDetailDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;
            aOperationCallResult.Object = appformDataDto;

           // ValidationResult aValidationResult = new ValidationResult();


            ClearMasterDetailValidationData(appformDataDto);

            AppMasterDetailValidationDto validationResultDto = new AppMasterDetailValidationDto();

            validationResultDto.DictOneToOneFields = new Dictionary<string, bool>();
            validationResultDto.DictSiblingOneToOneFields = new Dictionary<string, Dictionary<string, bool>>();
            validationResultDto.DictOneToManyFields = new Dictionary<string, Dictionary<int, AppChildDatValidationResultDto>>();

            validationResultDto.DictOneToOneFieldNameAndErrorMessage = new Dictionary<string, string>();
            validationResultDto.DictSiblingOneToOneFieldNameAndErrorMessage = new Dictionary<string, Dictionary<string, string>>();
            validationResultDto.DictChildGridUnitIdAndErrorMessageList = new Dictionary<string, List<string>>();

            bool isValid = true;

            if (appTransactionExDto != null && appTransactionExDto.AppTransactionUnitList != null && appformDataDto != null)
            {
                foreach (var rootLevelUnit in appTransactionExDto.AppTransactionUnitList)
                {
                    if (rootLevelUnit.IsMasterSiblingUnit.HasValue && rootLevelUnit.IsMasterSiblingUnit.Value)
                    {
                        bool isSibUnitDataValid = FirstLevelValidation_ValidateSiblingUnit(appformDataDto, aValidationResult, validationResultDto, rootLevelUnit);

                        isValid = isValid && isSibUnitDataValid;
                    }
                    else
                    {
                        bool isRootDataValid = FirstLevelValidation_ValidateRootUnit(appformDataDto, aValidationResult, validationResultDto, rootLevelUnit);

                        bool isChildUnitsValid = FirstLevelValidation_ValidateChildUnit(appformDataDto, aValidationResult, validationResultDto, rootLevelUnit);

                        isValid = isValid && isRootDataValid && isChildUnitsValid;
                    }
                }
            }

            if (aValidationResult.HasErrors)
            {
                appformDataDto.ValidationResultDto = validationResultDto;
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), null, ValidationItemType.Message, "Validation Successful"));
            }


            return aOperationCallResult;
        }

        private static void ClearMasterDetailValidationData(AppMasterDetailDto appformDataDto)
        {
            appformDataDto.ValidationResultDto = null;

            foreach (List<AppChildDataDto> aChildRowList in appformDataDto.DictOneToManyFields.Values)
            {
                foreach (var childRow in aChildRowList)
                {
                    if (childRow.DictOneToManyFields != null)
                    {
                        foreach (List<AppChildDataDto> aGrandchildRowList in childRow.DictOneToManyFields.Values)
                        {
                            foreach (AppChildDataDto grandChildRow in aGrandchildRowList)
                            {

                                Dictionary<string, object> dictGrandChildKeyValue = grandChildRow.DictOneToOneFields;
                                if (dictGrandChildKeyValue.ContainsKey("dictFieldNameAndIsInvalid"))
                                {
                                    dictGrandChildKeyValue.Remove("dictFieldNameAndIsInvalid");
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool FirstLevelValidation_ValidateRootUnit(AppMasterDetailDto appformDataDto, ValidationResult aValidationResult, AppMasterDetailValidationDto validationResultDto, AppTransactionUnitExDto rootLevelUnit)
        {
            bool isRootDataValid = true;

            if (appformDataDto.DictOneToOneFields != null)
            {
                var validationTransFieldList = rootLevelUnit.AppTransactionFieldList.ToList();
                if (appformDataDto.ValidationLimittedTransFieldIdList != null)
                {
                    validationTransFieldList = validationTransFieldList.Where(o => appformDataDto.ValidationLimittedTransFieldIdList.Contains((int)o.Id)).ToList();
                }

                foreach (var transField in validationTransFieldList)
                {
                    if (transField.NeedValidator.HasValue && transField.NeedValidator.Value || transField.MaxCharLegnth.HasValue || !(transField.IsAllowEmpty.HasValue && transField.IsAllowEmpty.Value))
                    {
                        object fieldValue = null;

                        if (appformDataDto.DictOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                        {
                            fieldValue = appformDataDto.DictOneToOneFields[transField.DataBaseFieldName];
                        }

                        bool isFieldDataValid = FirstLevelValidation_ValidateOneTransactionField(true, aValidationResult, transField, fieldValue, validationResultDto.DictOneToOneFieldNameAndErrorMessage);

                        if (!isFieldDataValid)
                        {
                            if (!validationResultDto.DictOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                            {
                                validationResultDto.DictOneToOneFields.Add(transField.DataBaseFieldName, true);
                            }

                            isRootDataValid = false;
                        }
                    }
                }
            }

            return isRootDataValid;
        }

        private static bool FirstLevelValidation_ValidateSiblingUnit(AppMasterDetailDto appformDataDto, ValidationResult aValidationResult, AppMasterDetailValidationDto validationResultDto, AppTransactionUnitExDto rootLevelUnit)
        {
            bool isSiblingUnitValid = true;
            Dictionary<string, bool> dictSibUnitValidation = new Dictionary<string, bool>();
            validationResultDto.DictSiblingOneToOneFields.Add(rootLevelUnit.Id.ToString(), dictSibUnitValidation);

            Dictionary<string, string> dictSibUnitFieldNameAndErrorMessage = new Dictionary<string, string>();
            validationResultDto.DictSiblingOneToOneFieldNameAndErrorMessage.Add(rootLevelUnit.Id.ToString(), dictSibUnitFieldNameAndErrorMessage);

            if (appformDataDto.DictSiblingOneToOneFields != null)
            {
                var validationTransFieldList = rootLevelUnit.AppTransactionFieldList.ToList();
                if (appformDataDto.ValidationLimittedTransFieldIdList != null)
                {
                    validationTransFieldList = validationTransFieldList.Where(o => appformDataDto.ValidationLimittedTransFieldIdList.Contains((int)o.Id)).ToList();
                }

                foreach (var transField in validationTransFieldList)
                {
                    if (transField.NeedValidator.HasValue && transField.NeedValidator.Value || transField.MaxCharLegnth.HasValue || !(transField.IsAllowEmpty.HasValue && transField.IsAllowEmpty.Value))
                    {
                        object fieldValue = null;

                        if (appformDataDto.DictSiblingOneToOneFields.ContainsKey(rootLevelUnit.Id.ToString()) && appformDataDto.DictSiblingOneToOneFields[rootLevelUnit.Id.ToString()].ContainsKey(transField.DataBaseFieldName))
                        {
                            fieldValue = appformDataDto.DictSiblingOneToOneFields[rootLevelUnit.Id.ToString()][transField.DataBaseFieldName];
                        }

                        bool isFieldDataValid = FirstLevelValidation_ValidateOneTransactionField(true, aValidationResult, transField, fieldValue, dictSibUnitFieldNameAndErrorMessage);

                        if (!isFieldDataValid)
                        {
                            if (!dictSibUnitValidation.ContainsKey(transField.DataBaseFieldName))
                            {
                                dictSibUnitValidation.Add(transField.DataBaseFieldName, true);
                            }

                            isSiblingUnitValid = false;
                        }
                    }
                }
            }

            return isSiblingUnitValid;
        }

        private static bool FirstLevelValidation_ValidateChildUnit(AppMasterDetailDto appformDataDto,
            ValidationResult aValidationResult, AppMasterDetailValidationDto validationResultDto, AppTransactionUnitExDto rootLevelUnit)
        {
            bool isAllChildUnitsValid = true;

            if (rootLevelUnit.Children != null && rootLevelUnit.Children.Count > 0)
            {
                foreach (var childUnit in rootLevelUnit.Children)
                {
                    List<string> childUnitErrorMessageList = new List<string>();
                    validationResultDto.DictChildGridUnitIdAndErrorMessageList[childUnit.Id.ToString()] = childUnitErrorMessageList;

                    if (childUnit.IsReadOnly.HasValue && childUnit.IsReadOnly.Value)
                    {
                        continue;
                    }

                    bool isChildUnitValid = true;
                    Dictionary<int, AppChildDatValidationResultDto> dictChildUnitValidation = new Dictionary<int, AppChildDatValidationResultDto>();



                    if (appformDataDto.DictOneToManyFields != null)
                    {
                        List<AppChildDataDto> childRowDataList = appformDataDto.DictOneToManyFields[childUnit.Id.ToString()];

                        int childRowIndex = 0;

                        foreach (AppChildDataDto aChildRowData in childRowDataList)
                        {
                            string childGridRowInfo = StringLocalizer.Localize("App_Validation_Grid", "Grid") + " "
                                + childUnit.UnitDisplayName
                                + " " + StringLocalizer.Localize("App_Validation_Row", "Row") + " "
                                + (childRowIndex + 1).ToString() + ": ";


                            var validationTransFieldList = childUnit.AppTransactionFieldList.ToList();
                            if (appformDataDto.ValidationLimittedTransFieldIdList != null)
                            {
                                validationTransFieldList = validationTransFieldList.Where(o => appformDataDto.ValidationLimittedTransFieldIdList.Contains((int)o.Id)).ToList();
                            }

                            foreach (var transField in validationTransFieldList)
                            {
                                if (transField.NeedValidator.HasValue && transField.NeedValidator.Value || transField.MaxCharLegnth.HasValue || !(transField.IsAllowEmpty.HasValue && transField.IsAllowEmpty.Value))
                                {
                                    object fieldValue = null;

                                    if (appformDataDto.DictOneToManyFields.ContainsKey(childUnit.Id.ToString()))
                                    {
                                        if (aChildRowData.DictOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                                        {
                                            fieldValue = aChildRowData.DictOneToOneFields[transField.DataBaseFieldName];

                                            bool isFieldDataValid = FirstLevelValidation_ValidateOneTransactionField(false, aValidationResult, transField, fieldValue, null, childGridRowInfo, childUnitErrorMessageList);

                                            if (!isFieldDataValid)
                                            {
                                                AppChildDatValidationResultDto aChildRowVation = null;

                                                if (!dictChildUnitValidation.ContainsKey(childRowIndex))
                                                {
                                                    aChildRowVation = new AppChildDatValidationResultDto();
                                                    dictChildUnitValidation.Add(childRowIndex, aChildRowVation);
                                                }
                                                else
                                                {
                                                    aChildRowVation = dictChildUnitValidation[childRowIndex];
                                                }

                                                if (aChildRowVation.DictOneToOneFields == null)
                                                {
                                                    aChildRowVation.DictOneToOneFields = new Dictionary<string, bool>();
                                                }

                                                if (!aChildRowVation.DictOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                                                {
                                                    aChildRowVation.DictOneToOneFields.Add(transField.DataBaseFieldName, true);
                                                }

                                                isChildUnitValid = false;
                                            }
                                        }
                                    }
                                }
                            }

                            // Validate GrandChild Rows
                            bool isAllGrandchildUnitsValid = FirstLevelValidation_ValidateGrandChildUnit(aValidationResult, childUnit,
                                dictChildUnitValidation, childRowIndex, aChildRowData, childGridRowInfo, appformDataDto);

                            isChildUnitValid = isChildUnitValid && isAllGrandchildUnitsValid;

                            childRowIndex++;
                        }
                    }

                    if (!isChildUnitValid)
                    {
                        isAllChildUnitsValid = false;
                        validationResultDto.DictOneToManyFields.Add(childUnit.Id.ToString(), dictChildUnitValidation);
                    }
                }
            }

            return isAllChildUnitsValid;
        }

        private static bool FirstLevelValidation_ValidateGrandChildUnit(ValidationResult aValidationResult, AppTransactionUnitExDto childUnit,
            Dictionary<int, AppChildDatValidationResultDto> dictChildUnitValidation, int childRowIndex, AppChildDataDto aChildRowData, string childGridRowInfo, AppMasterDetailDto appformDataDto)
        {
            bool isAllGrandchildUnitsValid = true;

            if (childUnit.Children != null && childUnit.Children.Count > 0)
            {
                // <GrandChildUnitId, <RowIndex, <FiledName, IsInvalid>>>
                Dictionary<int, Dictionary<string, bool>> dictGrandChildUnitValidation = new Dictionary<int, Dictionary<string, bool>>();

                foreach (var grandChildUnit in childUnit.Children)
                {
                    if (aChildRowData.DictOneToManyFields != null)
                    {
                        List<Dictionary<string, object>> grandchildRowDataList = aChildRowData.DictOneToManyFields[grandChildUnit.Id.ToString()]
                              .Select(o => o.DictOneToOneFields).ToList();

                        int grandchildRowIndex = 0;

                        foreach (Dictionary<string, object> aGrandchildRowData in grandchildRowDataList)
                        {
                            string grandchildGridRowInfo = childGridRowInfo
                                + StringLocalizer.Localize("App_Validation_SubGrid", "Sub Grid")
                                + grandChildUnit.UnitDisplayName
                                + " " + StringLocalizer.Localize("App_Validation_Row", "Row") + " "
                                + (grandchildRowIndex + 1).ToString() + ": ";

                            var validationTransFieldList = grandChildUnit.AppTransactionFieldList.ToList();
                            if (appformDataDto != null && appformDataDto.ValidationLimittedTransFieldIdList != null)
                            {
                                validationTransFieldList = validationTransFieldList.Where(o => appformDataDto.ValidationLimittedTransFieldIdList.Contains((int)o.Id)).ToList();
                            }


                            foreach (var transField in validationTransFieldList)
                            {
                                if (transField.NeedValidator.HasValue && transField.NeedValidator.Value || transField.MaxCharLegnth.HasValue || !(transField.IsAllowEmpty.HasValue && transField.IsAllowEmpty.Value))
                                {
                                    object fieldValue = null;

                                    if (aGrandchildRowData.ContainsKey(transField.DataBaseFieldName))
                                    {
                                        fieldValue = aGrandchildRowData[transField.DataBaseFieldName];

                                        bool isFieldDataValid = FirstLevelValidation_ValidateOneTransactionField(false, aValidationResult, transField, fieldValue, null, grandchildGridRowInfo);

                                        if (!isFieldDataValid)
                                        {
                                            AppChildDatValidationResultDto aChildRowVation = null;

                                            if (!dictChildUnitValidation.ContainsKey(childRowIndex))
                                            {
                                                aChildRowVation = new AppChildDatValidationResultDto();
                                                dictChildUnitValidation.Add(childRowIndex, aChildRowVation);
                                            }
                                            else
                                            {
                                                aChildRowVation = dictChildUnitValidation[childRowIndex];
                                            }

                                            if (aChildRowVation.DictOneToManyFields == null)
                                            {
                                                aChildRowVation.DictOneToManyFields = new Dictionary<string, Dictionary<int, Dictionary<string, bool>>>();
                                            }

                                            if (!aChildRowVation.DictOneToManyFields.ContainsKey(grandChildUnit.Id.ToString()))
                                            {
                                                aChildRowVation.DictOneToManyFields.Add(grandChildUnit.Id.ToString(), new Dictionary<int, Dictionary<string, bool>>());
                                            }

                                            Dictionary<int, Dictionary<string, bool>> dictGrandChildRowindexValidation = aChildRowVation.DictOneToManyFields[grandChildUnit.Id.ToString()];

                                            if (!dictGrandChildRowindexValidation.ContainsKey(grandchildRowIndex))
                                            {
                                                dictGrandChildRowindexValidation.Add(grandchildRowIndex, new Dictionary<string, bool>());
                                            }

                                            Dictionary<string, bool> dicGrandchildFieldValidation = dictGrandChildRowindexValidation[grandchildRowIndex];

                                            if (!dicGrandchildFieldValidation.ContainsKey(transField.DataBaseFieldName))
                                            {
                                                dicGrandchildFieldValidation.Add(transField.DataBaseFieldName, true);
                                            }

                                            isAllGrandchildUnitsValid = false;
                                        }
                                    }
                                }
                            }

                            grandchildRowIndex++;
                        }
                    }
                }
            }

            return isAllGrandchildUnitsValid;
        }

        private static bool FirstLevelValidation_ValidateOneTransactionField(bool isRootLevelField, ValidationResult aValidationResult, AppTransactionFieldExDto transField, object fieldValue, Dictionary<string, string> dictRootFieldNameAndErrorMessage, string gridRowInfo = "", List<string> childUnitErrorMessageList = null)
        {
            if (!(transField.NeedValidator.HasValue && transField.NeedValidator.HasValue))
            {
                return true;
            }
            // for all system ampping token, no need do validation
            if (transField.MappingEmSystemTokenField.HasValue)
            {
                return true;
            }


            bool isValid = true;

            if (isRootLevelField)
            {
                if (dictRootFieldNameAndErrorMessage != null)
                {
                    dictRootFieldNameAndErrorMessage[transField.DataBaseFieldName] = string.Empty;
                }
            }

            if (!transField.IsLinkToParentPrimaryKey && !(transField.IsAllowEmpty.HasValue && transField.IsAllowEmpty.Value))
            {





                if (fieldValue == null || string.IsNullOrWhiteSpace(fieldValue.ToString()))
                {
                    isValid = false;

                    string isEmptyMsg = StringLocalizer.Localize("App_Validation_IsEmpty", "is empty");

                    string errorMessage = gridRowInfo + transField.DisplayName + " " + isEmptyMsg + ". ";

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), null, ValidationItemType.Error, errorMessage));

                    if (isRootLevelField)
                    {
                        if (dictRootFieldNameAndErrorMessage != null)
                        {
                            dictRootFieldNameAndErrorMessage[transField.DataBaseFieldName] += errorMessage;
                        }
                    }
                    else
                    {
                        if (childUnitErrorMessageList != null)
                        {
                            childUnitErrorMessageList.Add(errorMessage);
                        }
                    }
                }
            }

            if (transField.MaxCharLegnth.HasValue)
            {
                if (fieldValue != null && fieldValue.ToString().Length > transField.MaxCharLegnth.Value)
                {
                    isValid = false;

                    string isTooLongStingFormat = StringLocalizer.Localize("App_Validation_IsTooLongWarningFormat", "is too long (max {0} characters). ");
                    string isTooLongMsg = string.Format(isTooLongStingFormat, transField.MaxCharLegnth);

                    string errorMessage = gridRowInfo + transField.DisplayName + " " + isTooLongMsg;
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), null, ValidationItemType.Error, errorMessage));

                    if (isRootLevelField)
                    {
                        if (dictRootFieldNameAndErrorMessage != null)
                        {
                            dictRootFieldNameAndErrorMessage[transField.DataBaseFieldName] += errorMessage;
                        }
                    }
                    else
                    {
                        if (childUnitErrorMessageList != null)
                        {
                            childUnitErrorMessageList.Add(errorMessage);
                        }
                    }
                }
            }

            return isValid;
        }





        private static void ClearListEditTransactionValidationData(AppListDataDto appListDataDto)
        {
            appListDataDto.ValidationResultDto = null;

            foreach (var childRow in appListDataDto.ListData)
            {
                if (childRow.DictOneToManyFields != null)
                {
                    foreach (List<AppChildDataDto> aGrandchildRowList in childRow.DictOneToManyFields.Values)
                    {
                        foreach (AppChildDataDto grandChildRow in aGrandchildRowList)
                        {

                            Dictionary<string, object> dictGrandChildKeyValue = grandChildRow.DictOneToOneFields;
                            if (dictGrandChildKeyValue.ContainsKey("dictFieldNameAndIsInvalid"))
                            {
                                dictGrandChildKeyValue.Remove("dictFieldNameAndIsInvalid");
                            }
                        }
                    }
                }
            }
        }



        private static void DoStrigEncryptCaculation(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, AppTransactionUnitFormulaExDto transactionUnitFormula, Dictionary<int, object> dictFiedIdValue, int leftSideKey)
        {

            foreach (int transactionFieldId in dictFiedIdValue.Keys)
            {

                var transactionField = dictAllTransactionField[transactionFieldId];

                string controlToken = FormaulPrefix + transactionField.Id.ToString();
                if (

                    transactionField.ControlType != (int)EmAppControlType.Label &&
                    transactionField.ControlType != (int)EmAppControlType.RGBColorDisplay)
                {
                    object controlValue = dictFiedIdValue[(int)transactionField.Id];
                    if (controlValue != null)
                    {
                        dictFiedIdValue[leftSideKey] = EnDeCrypt.Encrypt(controlValue.ToString(), FormulaPasswordSaltKey);
                    }

                }
            }



        }

        private static void DoTwoDateDiffCaculation(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, AppTransactionUnitFormulaExDto transactionUnitFormula, Dictionary<int, object> dictFiedIdValue, int leftSideKey)
        {
            DateTime aBaseDatetime = GetFirstFieldBaseDate(dictAllTransactionField, transactionUnitFormula, dictFiedIdValue);

            dictFiedIdValue[leftSideKey] = null; ;

            if (transactionUnitFormula.FormulaExpression.IndexOf('-') != -1)
            {

                string[] expresss = transactionUnitFormula.FormulaExpression.Split(new char[] { '-' });

                string secondTransFieldWithPrefix = expresss[1];

                DateTime? secondDateTime = null;

                foreach (int transactionFieldId in dictFiedIdValue.Keys)
                {
                    AppTransactionFieldExDto transactionField = dictAllTransactionField[transactionFieldId];
                    string transactionFieldToken = FormaulPrefix + transactionField.Id.ToString();

                    if (secondTransFieldWithPrefix.Contains(transactionFieldToken))
                    {
                        secondDateTime = ControlTypeValueConverter.ConvertValueToDate(dictFiedIdValue[transactionFieldId]);
                        break;
                    }
                }

                if (aBaseDatetime != DateTime.MinValue && secondDateTime.HasValue)
                {

                    double numbetrDays = (aBaseDatetime - secondDateTime.Value).TotalDays;

                    dictFiedIdValue[leftSideKey] = numbetrDays;

                }

            }
        }

        private static DateTime GetFirstFieldBaseDate(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, AppTransactionUnitFormulaExDto transactionUnitFormula, Dictionary<int, object> dictFiedIdValue)
        {
            DateTime aBaseDatetime = DateTime.MinValue;

            if (!String.IsNullOrEmpty(transactionUnitFormula.BaseDateTime))
            {
                if (transactionUnitFormula.BaseDateTime == EmAppDateTimeTokenType.Today.ToString())
                {
                    aBaseDatetime = DateTime.Today.Date;
                }
                else if (transactionUnitFormula.BaseDateTime == EmAppDateTimeTokenType.Now.ToString())
                {
                    aBaseDatetime = DateTime.Now;
                }
                else if (transactionUnitFormula.BaseDateTime == EmAppDateTimeTokenType.Null.ToString())
                {
                    aBaseDatetime = DateTime.MinValue;
                }
                else
                {
                    if (transactionUnitFormula.BaseDateTime.Contains(FormaulPrefix))
                    {
                        foreach (int transactionFieldId in dictFiedIdValue.Keys)
                        {
                            AppTransactionFieldExDto transactionField = dictAllTransactionField[transactionFieldId];
                            string transactionFieldToken = FormaulPrefix + transactionField.Id.ToString();

                            if (transactionUnitFormula.BaseDateTime.Contains(transactionFieldToken))
                            {
                                DateTime? fieldValue = ControlTypeValueConverter.ConvertValueToDate(dictFiedIdValue[transactionFieldId]);

                                if (fieldValue.HasValue)
                                {
                                    aBaseDatetime = fieldValue.Value;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        int? transFieldId = ControlTypeValueConverter.ConvertValueToInt(transactionUnitFormula.BaseDateTime);
                        if (transFieldId.HasValue && dictFiedIdValue.ContainsKey(transFieldId.Value))
                        {
                            DateTime? fieldValue = ControlTypeValueConverter.ConvertValueToDate(dictFiedIdValue[transFieldId.Value]);

                            if (fieldValue.HasValue)
                            {
                                aBaseDatetime = fieldValue.Value;
                            }
                        }
                    }

                }
            }

            return aBaseDatetime;
        }
    }
}