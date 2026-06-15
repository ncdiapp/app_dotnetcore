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
using System.Text.RegularExpressions;
using System;
using DatabaseSchemaMrg.DataSchema;
using System.Data.SqlClient;
using DatabaseSchemaMrg;
//using APP.Persistence.Common;

using APP.Framework;
namespace App.BL
{
    public static class AppTransactionSaveAsBL
    {

        public static OperationCallResult<AppTransactionExDto> SaveAsAppTransaction(object orgTranscactionId, bool isCreatingEshopCardViewItemTransactionFromBase = false)
        {


            AppTransactionExDto oldHierarchyAppTransactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(orgTranscactionId);


            AppTransactionExDto newHierarchyAppTransactionExDto = oldHierarchyAppTransactionExDto.DeepCopy();
            newHierarchyAppTransactionExDto.Id = null;

            if (isCreatingEshopCardViewItemTransactionFromBase)
            {
                newHierarchyAppTransactionExDto.TransactionName = "Eshop Item Detail - " + oldHierarchyAppTransactionExDto.TransactionName;
            }
            else
            {
                newHierarchyAppTransactionExDto.TransactionName = "copy of" + oldHierarchyAppTransactionExDto.TransactionName;
            }

            newHierarchyAppTransactionExDto.FormId = null;


            OperationCallResult<AppTransactionExDto> saveSaobject = AppTransactionBL.SaveAppTransactionExDto(newHierarchyAppTransactionExDto);
            newHierarchyAppTransactionExDto = saveSaobject.Object;




            Dictionary<object, string> DictOldID_UnitFiledName = new Dictionary<object, string>();
            Dictionary<string, object> DictOldUnitFiledName_ID = new Dictionary<string, object>();
            Dictionary<string, object> DictNewUnitFiledNameID = new Dictionary<string, object>();
            Dictionary<object, object> DictOldTransactionIdId_NewID = new Dictionary<object, object>();

            foreach (var unit in oldHierarchyAppTransactionExDto.DictAllTransactionUnitIdExDto.Values)
            {
                foreach (AppTransactionFieldExDto filed in unit.AppTransactionFieldList)
                {
                    string unitFieName = unit.DataBaseTableName + filed.DataBaseFieldName + filed.RowIdentityGuid;
                    DictOldID_UnitFiledName.Add(filed.Id, unitFieName);
                    DictOldUnitFiledName_ID.Add(unitFieName, filed.Id);
                }
            }


            foreach (var unit in newHierarchyAppTransactionExDto.DictAllTransactionUnitIdExDto.Values)
            {
                foreach (var filed in unit.AppTransactionFieldList)
                {
                    string unitFieName = unit.DataBaseTableName + filed.DataBaseFieldName + filed.RowIdentityGuid;
                    DictNewUnitFiledNameID.Add(unitFieName, filed.Id);
                    object oldID = DictOldUnitFiledName_ID[unitFieName];
                    DictOldTransactionIdId_NewID.Add(oldID, filed.Id);
                }
            }

            //List<AppTransactionFieldAggFunctionDto> oldAppTransactionFieldAggFunctionDtoList, newAppTransactionFieldAggFunctionDtoList;
            //ProcessAggFunctionAndFormula(orgTranscactionId, oldHierarchyAppTransactionExDto, newHierarchyAppTransactionExDto, DictOldID_UnitFiledName, DictOldUnitFiledName_ID, DictNewUnitFiledNameID, DictOldTransactionIdId_NewID, out oldAppTransactionFieldAggFunctionDtoList, out newAppTransactionFieldAggFunctionDtoList);

            ProcessSaveAsForm(oldHierarchyAppTransactionExDto, newHierarchyAppTransactionExDto, DictOldTransactionIdId_NewID);

            OperationCallResult<AppTransactionExDto> toReturn = ProcessPostSave(newHierarchyAppTransactionExDto, DictOldID_UnitFiledName, DictNewUnitFiledNameID
                //, oldAppTransactionFieldAggFunctionDtoList, newAppTransactionFieldAggFunctionDtoList
                );


            OperationCallResult<bool> commandSaveAsResult = ProcessSaveAsCommands(oldHierarchyAppTransactionExDto, newHierarchyAppTransactionExDto);

            if (!commandSaveAsResult.IsSuccessful)
            {
                toReturn.ValidationResult.Merge(commandSaveAsResult.ValidationResult);
            }


            return toReturn;

        }

        private static OperationCallResult<bool> ProcessSaveAsCommands(AppTransactionExDto oldHierarchyAppTransactionExDto, AppTransactionExDto newHierarchyAppTransactionExDto)
        {
            List<AppProjectWorkFlowActionExDto> orgCommandList = AppTransactionCommandBL.RetrieveOneTransactionCommandActionList(oldHierarchyAppTransactionExDto);

            Dictionary<int, string> orgCommandIdAndId = orgCommandList.Where(o => o.ActionGuid.HasValue).ToDictionary(o => (int)o.Id, o => o.ActionGuid.Value.ToString());

            foreach (var commandDto in orgCommandList)
            {
                commandDto.Id = null;
                commandDto.TransactionId = null;
            }

            newHierarchyAppTransactionExDto.CommandActionList = orgCommandList;

            var preSaveResult = AppTransactionCommandBL.SaveOneTransactionCommandActionList(newHierarchyAppTransactionExDto);

            if (preSaveResult.IsSuccessful)
            {
                List<AppProjectWorkFlowActionExDto> newCommandList = AppTransactionCommandBL.RetrieveOneTransactionCommandActionList(newHierarchyAppTransactionExDto);
                Dictionary<string, int> newCommandGuidAndId = newCommandList.Where(o => o.ActionGuid.HasValue).ToDictionary(o => o.ActionGuid.Value.ToString(), o => (int)o.Id);

                foreach (var newCommnadDto in newCommandList)
                {
                    if (newCommnadDto.ActionAttribute != null && newCommnadDto.ActionAttribute.ChildActionList != null && newCommnadDto.ActionAttribute.ChildActionList.Count > 0)
                    {
                        foreach (var childMappingDto in newCommnadDto.ActionAttribute.ChildActionList)
                        {
                            if (childMappingDto.CommandId.HasValue && orgCommandIdAndId.ContainsKey(childMappingDto.CommandId.Value))
                            {
                                string commandGuid = orgCommandIdAndId[childMappingDto.CommandId.Value];

                                if (newCommandGuidAndId.ContainsKey(commandGuid))
                                {
                                    int newCommnandId = newCommandGuidAndId[commandGuid];
                                    childMappingDto.CommandId = newCommnandId;
                                }
                            }
                        }
                    }
                }

                newHierarchyAppTransactionExDto.CommandActionList = newCommandList;

                var postSaveResult = AppTransactionCommandBL.SaveOneTransactionCommandActionList(newHierarchyAppTransactionExDto);

                return postSaveResult;
            }

            else
            {
                return preSaveResult;
            }
        }

        private static OperationCallResult<AppTransactionExDto> ProcessPostSave(AppTransactionExDto newHierarchyAppTransactionExDto, Dictionary<object, string> DictOldID_UnitFiledName, Dictionary<string, object> DictNewUnitFiledNameID
            //, List<AppTransactionFieldAggFunctionDto> oldAppTransactionFieldAggFunctionDtoList, List<AppTransactionFieldAggFunctionDto> newAppTransactionFieldAggFunctionDtoList
            )
        {
            foreach (var unit in newHierarchyAppTransactionExDto.DictAllTransactionUnitIdExDto.Values)
            {

                foreach (var filed in unit.AppTransactionFieldList)
                {



                    if (filed.DdlparentLevelId.HasValue)
                    {
                        string ddlName = DictOldID_UnitFiledName[filed.DdlparentLevelId];

                        filed.DdlparentLevelId = DictNewUnitFiledNameID[ddlName] as int?;

                        filed.IsModified = true;
                    }

                    if (filed.MasterEntityFieldlId.HasValue)
                    {
                        string ddlName = DictOldID_UnitFiledName[filed.MasterEntityFieldlId];

                        filed.MasterEntityFieldlId = DictNewUnitFiledNameID[ddlName] as int?;

                        filed.IsModified = true;
                    }

                    if (filed.ChildUnitSubscribeParentFieldId.HasValue)
                    {
                        string ddlName = DictOldID_UnitFiledName[filed.ChildUnitSubscribeParentFieldId];

                        filed.ChildUnitSubscribeParentFieldId = DictNewUnitFiledNameID[ddlName] as int?;

                        filed.IsModified = true;
                    }




                    //if (filed.ParentUnitSubscribeChildAggFunctionId.HasValue)
                    //{

                    //    var oldOne = oldAppTransactionFieldAggFunctionDtoList.FirstOrDefault(o => o.Id as int? == filed.ParentUnitSubscribeChildAggFunctionId);
                    //    int? aggType = oldOne.AggregationFunctionType;

                    //    int oldFiledId = oldOne.TransactionFieldId.Value;
                    //    string oldName = DictOldID_UnitFiledName[oldFiledId];

                    //    object newFiledId = DictNewUnitFiledNameID[oldName];




                    //    var newOne = newAppTransactionFieldAggFunctionDtoList.FirstOrDefault(o => o.TransactionFieldId == newFiledId as int? && o.AggregationFunctionType == aggType);

                    //    if (newOne != null)
                    //    {

                    //        filed.ParentUnitSubscribeChildAggFunctionId = newOne.Id as int?;
                    //        filed.IsModified = true;

                    //    }
                    //}

                }
            }

            newHierarchyAppTransactionExDto.IsModified = true;




            return AppTransactionBL.SaveAppTransactionExDto(newHierarchyAppTransactionExDto);
        }

        //private static void ProcessAggFunctionAndFormula(object orgTranscactionId, AppTransactionExDto oldHierarchyAppTransactionExDto, AppTransactionExDto newHierarchyAppTransactionExDto, 
        //    Dictionary<object, string> DictOldID_UnitFiledName, Dictionary<string, object> DictOldUnitFiledName_ID, Dictionary<string, object> DictNewUnitFiledNameID,
        //    Dictionary<object, object> DictOldTransactionIdId_NewID,
        //    out List<AppTransactionFieldAggFunctionDto> oldAppTransactionFieldAggFunctionDtoList, out List<AppTransactionFieldAggFunctionDto> newAppTransactionFieldAggFunctionDtoList)
        //{
        //    Dictionary<string, AppTransactionFieldAggFunctionSetDto> DicOldUnitFiledName_dAggFunctionSetDto = new Dictionary<string, AppTransactionFieldAggFunctionSetDto>();
        //    Dictionary<string, AppTransactionUnitFormulaSetDto> DicOldUnitFiledName_AppTransactionUnitFormulaSetDto = new Dictionary<string, AppTransactionUnitFormulaSetDto>();
        //    Dictionary<string, ObservableSet<AppTransactionUnitDeleteFlowExDto>> DicOldUnitDeleteFlowExDto = new Dictionary<string, ObservableSet<AppTransactionUnitDeleteFlowExDto>>();
        //    Dictionary<string, ObservableSet<AppFormLinkTargetDto>> DicOldUnitFormLinkExDto = new Dictionary<string, ObservableSet<AppFormLinkTargetDto>>();

        //    Dictionary<string, ObservableSet<AppTransactionUnitLinkedSearchExDto>> DicOldLinkedSearchExDto = new Dictionary<string, ObservableSet<AppTransactionUnitLinkedSearchExDto>>();



        //    //AppTransactionFieldAggFunctionDto
        //    oldAppTransactionFieldAggFunctionDtoList = new List<AppTransactionFieldAggFunctionDto>();
        //    foreach (var unit in oldHierarchyAppTransactionExDto.DictAllTransactionUnitIdExDto.Values)
        //    {

        //        foreach (AppTransactionFieldExDto filed in unit.AppTransactionFieldList)
        //        {

        //            string unitFieName = unit.DataBaseTableName + filed.DataBaseFieldName + filed.RowIdentityGuid;

        //            DictOldID_UnitFiledName.Add(filed.Id, unitFieName);

        //            DictOldUnitFiledName_ID.Add(unitFieName, filed.Id);

        //            AppTransactionFieldAggFunctionSetDto aAppTransactionFieldAggFunctionSetDto = AppTransactionFormulaSetupBL.RetrieveAppTransactionFieldAggFunctionSetDto((int)filed.Id);
        //            if (aAppTransactionFieldAggFunctionSetDto.ListAppTransactionFieldAggFunction.Count > 0)
        //            {
        //                DicOldUnitFiledName_dAggFunctionSetDto.Add(unitFieName, aAppTransactionFieldAggFunctionSetDto);

        //                foreach (AppTransactionFieldAggFunctionDto oldTransactionFieldAggFunctionDto in aAppTransactionFieldAggFunctionSetDto.ListAppTransactionFieldAggFunction)
        //                {

        //                    oldAppTransactionFieldAggFunctionDtoList.Add(oldTransactionFieldAggFunctionDto.DeepCopy());

        //                }

        //            }


        //        }

        //        // need to retrive old formula
        //        AppTransactionUnitFormulaSetDto aAppTransactionUnitFormulaSetDto = AppTransactionFormulaSetupBL.RetrieveAppTransactionUnitFormulaSetDto((int)unit.Id, (int)orgTranscactionId);
        //        DicOldUnitFiledName_AppTransactionUnitFormulaSetDto.Add(unit.DataBaseTableName, aAppTransactionUnitFormulaSetDto);

        //    }







        //    // save  new AppTransactionFieldAggFunctionDt
        //    newAppTransactionFieldAggFunctionDtoList = new List<AppTransactionFieldAggFunctionDto>();
        //    foreach (var unit in newHierarchyAppTransactionExDto.DictAllTransactionUnitIdExDto.Values)
        //    {

        //        foreach (var filed in unit.AppTransactionFieldList)
        //        {
        //            string unitFieName = unit.DataBaseTableName + filed.DataBaseFieldName + filed.RowIdentityGuid;
        //            if (DicOldUnitFiledName_dAggFunctionSetDto.ContainsKey(unitFieName))
        //            {
        //                AppTransactionFieldAggFunctionSetDto oldFieldAggFunctionSetDto = DicOldUnitFiledName_dAggFunctionSetDto[unitFieName];
        //                int oldTransactionFieldId = oldFieldAggFunctionSetDto.TransactionFieldId;

        //                object newFiledId = DictNewUnitFiledNameID[unitFieName];
        //                oldFieldAggFunctionSetDto.TransactionFieldId = (int)newFiledId;


        //                AppTransactionFieldAggFunctionSetDto newFieldAggFunctionSetDto = AppTransactionFormulaSetupBL.SaveAppTransactionFieldAggFunctionSetDto(oldFieldAggFunctionSetDto);

        //                foreach (AppTransactionFieldAggFunctionDto aAppTransactionFieldAggFunctionDto in newFieldAggFunctionSetDto.ListAppTransactionFieldAggFunction)
        //                {

        //                    newAppTransactionFieldAggFunctionDtoList.Add(aAppTransactionFieldAggFunctionDto);

        //                }



        //            }


        //        }
        //    }

        //    // Save new AppTransactionUnitFormulaSetDto 

        //    foreach (var unit in newHierarchyAppTransactionExDto.DictAllTransactionUnitIdExDto.Values)
        //    {

        //        AppTransactionUnitFormulaSetDto newAppTransactionUnitFormulaSetDto = DicOldUnitFiledName_AppTransactionUnitFormulaSetDto[unit.DataBaseTableName];

        //        newAppTransactionUnitFormulaSetDto.TransactionUnitId = (int)unit.Id;

        //        foreach (AppTransactionUnitFormulaDto formulaDto in newAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula)
        //        {

        //            string fx = formulaDto.FormulaExpression;

        //            //transactionfieldid_2082=transactionfieldid_2194+transactionfieldid_2195+transactionfieldid_2196
        //            foreach (object oldId in DictOldTransactionIdId_NewID.Keys)
        //            {
        //                fx = fx.Replace("transactionfieldid_" + oldId, "transactionfieldid_" + DictOldTransactionIdId_NewID[oldId]);


        //            }

        //            formulaDto.FormulaExpression = fx;


        //            if (formulaDto.ConditionFieldId.HasValue)
        //            {
        //                formulaDto.ConditionFieldId = DictOldTransactionIdId_NewID[formulaDto.ConditionFieldId] as int?;


        //            }

        //            if (formulaDto.ChildTransactionUnitId.HasValue)
        //            {

        //                var oldChilldUnit = oldHierarchyAppTransactionExDto.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(o => o.Id as int? == formulaDto.ChildTransactionUnitId);
        //                if (oldChilldUnit != null)
        //                {
        //                    var newChilldUnit = newHierarchyAppTransactionExDto.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(o => o.DataBaseTableName == oldChilldUnit.DataBaseTableName);

        //                    formulaDto.ChildTransactionUnitId = newChilldUnit.Id as int?;        //DictOldId_NewID[formulaDto.ConditionFieldId] as int?;

        //                }



        //            }


        //        }

        //        AppTransactionFormulaSetupBL.SaveAppTransactionUnitFormulaSetDto(newAppTransactionUnitFormulaSetDto);


        //    }
        //}

        private static void ProcessSaveAsForm(AppTransactionExDto oldHierarchyAppTransactionExDto, AppTransactionExDto newHierarchyAppTransactionExDto, Dictionary<object, object> DictOldTransactionIdId_NewID)
        {
            //save as Transaction From

            if (oldHierarchyAppTransactionExDto.FormId.HasValue)
            {
                AppFormExDto oldAppFormExDto = AppFormFlexLayoutBL.RetrieveOneAppFormFlexLayoutExDto(oldHierarchyAppTransactionExDto.FormId.Value);
                AppFormExDto newAppFormExDto = oldAppFormExDto.DeepCopy();
                newAppFormExDto.Id = null;

                ObservableSet<AppFormLayoutItemExDto> layoutItemFlatList = new ObservableSet<AppFormLayoutItemExDto>();
                AppFormFlexLayoutBL.ConvertFormUiHiarachyLayoutItemsToFlatList(newAppFormExDto.AppFormLayoutItemList, layoutItemFlatList);


                foreach (AppFormLayoutItemExDto appFormLayoutItemExDto in layoutItemFlatList)
                {

                    if (appFormLayoutItemExDto.TransactionFieldId.HasValue)
                    {

                        appFormLayoutItemExDto.TransactionFieldId = DictOldTransactionIdId_NewID[appFormLayoutItemExDto.TransactionFieldId] as int?;

                    }

                    if (appFormLayoutItemExDto.GridTransactionUnitId.HasValue)
                    {


                        var oldChilldUnit = oldHierarchyAppTransactionExDto.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(o => o.Id as int? == appFormLayoutItemExDto.GridTransactionUnitId);
                        if (oldChilldUnit != null)
                        {
                            if (oldHierarchyAppTransactionExDto.IsReadOnly.HasValue && oldHierarchyAppTransactionExDto.IsReadOnly.Value)
                            {
                                var newChilldUnit = newHierarchyAppTransactionExDto.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(o => o.DataSourceQuery == oldChilldUnit.DataSourceQuery);
                                appFormLayoutItemExDto.GridTransactionUnitId = newChilldUnit.Id as int?;
                            }
                            else
                            {
                                var newChilldUnit = newHierarchyAppTransactionExDto.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(o => o.DataBaseTableName == oldChilldUnit.DataBaseTableName);
                                appFormLayoutItemExDto.GridTransactionUnitId = newChilldUnit.Id as int?;
                            }


                        }
                    }
                }

                OperationCallResult<AppFormExDto> saveFormResult = AppFormFlexLayoutBL.SaveAppFormFlexLayoutExDto(newAppFormExDto);

                if (saveFormResult.IsSuccessfulWithResult)
                {
                    newHierarchyAppTransactionExDto.FormId = (int)saveFormResult.Object.Id;
                }






            }

        }
    }
}