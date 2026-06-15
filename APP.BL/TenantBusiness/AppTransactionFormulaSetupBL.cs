using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;

using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Components.Dto;

using Newtonsoft.Json;

using APP.Framework;
namespace App.BL
{
    // Validation
    public static class AppTransactionFormulaSetupBL
    {

        private static Regex FormulaRegex = new Regex(@"\[.+?\]");
        public static string[] FormulaConstString = { "(", ")", "=", "==", "!=", ">", "<", ">=", "<=", "+", "-", "*", "/", "%", "&&", "||", "&", "|", "true", "false", "!", "::", "," };
        //".TotalMinutes", ".TotalHours", ".TotalDays", ".AddMinutes", ".AddHours", ".AddDays"};
        public static string TransactionFieldFormulaPrefix = "transactionfieldid_";


        public static AppTransactionUnitFormulaSetDto RetrieveAppTransactionUnitFormulaSetDto(int unitId, int transactionId)
        {
            List<AppTransactionUnitFormulaDto> toReturn = new List<AppTransactionUnitFormulaDto>();

            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            AppTransactionUnitExDto aAppTransactionUnitExDto = null;

            if (transactionExDto != null && transactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(unitId.ToString()))
            {
                aAppTransactionUnitExDto = transactionExDto.DictAllTransactionUnitIdExDto[unitId.ToString()];

                //List<AppTransactionFieldExDto> transactionFieldList = aAppTransactionUnitExDto.AppTransactionFieldList.ToList();

                ////AppTransactionUnitExDto aAppTransactionUnitExDto = AppTransactionBL.RetrieveOneAppTransactionUnitExDto(unitId);

                //if (!aAppTransactionUnitExDto.ParentTransactionUnitId.HasValue && !(aAppTransactionUnitExDto.IsMasterSiblingUnit.HasValue && aAppTransactionUnitExDto.IsMasterSiblingUnit.Value))
                //{
                //    transactionFieldList = transactionExDto.DictRootLevelUnitTransactionField.Values.ToList();

                //}

                List<AppTransactionFieldExDto> transactionFieldList = transactionExDto.DictAllTransactionField.Values.ToList();

                InitialTransactionFieldFormularDisplayName(transactionExDto, transactionFieldList);

                EntityCollection<AppTransactionUnitFormulaEntity> entityList = new EntityCollection<AppTransactionUnitFormulaEntity>();
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    adapter.FetchEntityCollection(entityList, new RelationPredicateBucket(AppTransactionUnitFormulaFields.TransactionUnitId == unitId));

                }

                foreach (var formulaEntity in entityList.OrderBy(o => o.CaculationFlowSort))
                {
                    toReturn.Add(AppTransactionUnitFormulaConverter.ConvertEntityToDto(OutFormatFormulaExpressForTransactionUnit(transactionFieldList, formulaEntity)));

                }


                if (aAppTransactionUnitExDto.Children != null && aAppTransactionUnitExDto.Children.Count > 0)
                {
                    foreach (var aChildUnitDto in aAppTransactionUnitExDto.Children)
                    {
                        var childUnitFormular = toReturn.FirstOrDefault(o => o.ChildTransactionUnitId.HasValue && o.ChildTransactionUnitId.Value == (int)aChildUnitDto.Id);
                        if (childUnitFormular == null)
                        {
                            AppTransactionUnitFormulaDto newFormula = new AppTransactionUnitFormulaDto();
                            newFormula.CaculationFlowSort = 1;
                            if (toReturn.Where(o => o.CaculationFlowSort.HasValue).Count() > 0)
                            {
                                newFormula.CaculationFlowSort = toReturn.Where(o => o.CaculationFlowSort.HasValue).Max(o => o.CaculationFlowSort.Value) + 1;
                            }
                            newFormula.TransactionUnitId = (int)aAppTransactionUnitExDto.Id;
                            newFormula.ChildTransactionUnitId = (int)aChildUnitDto.Id;
                            newFormula.FormulaExpression = "";
                            newFormula.AssignToTransFieldId = null;
                            toReturn.Add(newFormula);
                        }

                    }
                }

                AppTransactionUnitFormulaSetDto aAppTransactionUnitFormulaSetDto = new AppTransactionUnitFormulaSetDto();

                aAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula = toReturn;
                aAppTransactionUnitFormulaSetDto.TransactionUnitId = unitId;
                aAppTransactionUnitFormulaSetDto.TransactionId = transactionId;


                foreach (string unitIdStr in transactionExDto.DictAllTransactionUnitIdExDto.Keys)
                {
                    var unitdto = transactionExDto.DictAllTransactionUnitIdExDto[unitIdStr];
                    var aFormulaSetDto = aAppTransactionUnitFormulaSetDto;

                    aFormulaSetDto.ListAppTransactionUnitFormula.ForAll(o =>
                    {
                        if (unitdto.AppTransactionUnitFormula_List != null)
                        {
                            var matchOrgFormula = unitdto.AppTransactionUnitFormula_List.FirstOrDefault(p => o.Id != null && p.Id != null && (int)p.Id == (int)o.Id);
                            if (matchOrgFormula != null)
                            {
                                if (matchOrgFormula.AssignmentLeftSideFieldId > 0)
                                {
                                    o.AssignToTransFieldId = matchOrgFormula.AssignmentLeftSideFieldId;
                                }

                            }
                        }

                    });
                }


                return aAppTransactionUnitFormulaSetDto;
            }

            return null;
        }

        public static void InitialTransactionFieldFormularDisplayName(AppTransactionExDto transactionExDto, List<AppTransactionFieldExDto> transactionFieldList, bool isGlobalWorkflowField = false)
        {
            transactionFieldList.ForAll(o =>
            {
                AppTransactionUnitExDto aUnit = null;
                if (transactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(o.TransactionUnitId.ToString()))
                {
                    aUnit = transactionExDto.DictAllTransactionUnitIdExDto[o.TransactionUnitId.ToString()];
                }
                
                if (aUnit != null && aUnit.IsMasterSiblingUnit.HasValue && aUnit.IsMasterSiblingUnit.Value)
                {
                    o.FormulaDisplayName = "[" + aUnit.DataBaseTableName + "." + o.DataBaseFieldName;
                }
                else
                {
                    if (isGlobalWorkflowField)
                    {
                        o.FormulaDisplayName = AppTransactionCommandBL.GlobalTFPrefix + o.DataBaseFieldName;
                    }
                    else
                    {
                        o.FormulaDisplayName = "[" + o.DataBaseFieldName;
                    }                    
                }


                if (o.IsTempVariable.HasValue && o.IsTempVariable.Value)
                {
                    o.FormulaDisplayName += "]";
                }
                else
                {
                    o.FormulaDisplayName += "_" + o.Id + "]";
                }

            });
        }

        public static void InitialSearchViewFieldFormularDisplayName(List<AppSearchViewFieldExDto> fieldList)
        {
            fieldList.ForAll(o =>
            {
                o.FormulaDisplayName = "[" + o.SysTableFiledPath + "_" + o.Id + "]";

            });
        }

        public static OperationCallResult<AppTransactionUnitFormulaSetDto> SaveAppTransactionUnitFormulaSetDtoList(List<AppTransactionUnitFormulaSetDto> appTransactionUnitFormulaSetDtoList)
        {

            OperationCallResult<AppTransactionUnitFormulaSetDto> aOperationCallResult = new OperationCallResult<AppTransactionUnitFormulaSetDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;
            var objectList = new List<AppTransactionUnitFormulaSetDto>();




            int? transactionId = null;
            if (appTransactionUnitFormulaSetDtoList != null)
            {
                if (appTransactionUnitFormulaSetDtoList.Count > 0)
                {
                    if (appTransactionUnitFormulaSetDtoList[0].NeedToUpdateTransactionExDto != null)
                    {
                        AppTransactionExDto transactionExDto = PrepareTransactionCrossRelationFromFormula(appTransactionUnitFormulaSetDtoList);

                        if (transactionExDto.IsModified)
                        {
                            var saveTransactionResult = AppTransactionBL.SaveAppTransactionExDto(appTransactionUnitFormulaSetDtoList[0].NeedToUpdateTransactionExDto);

                            if (saveTransactionResult.ValidationResult.HasErrors)
                            {
                                aValidationResult.Merge(saveTransactionResult.ValidationResult);
                                return aOperationCallResult;
                            }
                        }
                    }

                    foreach (AppTransactionUnitFormulaSetDto aAppTransactionUnitFormulaSetDto in appTransactionUnitFormulaSetDtoList)
                    {
                        if (!transactionId.HasValue)
                        {
                            transactionId = aAppTransactionUnitFormulaSetDto.TransactionId;
                        }

                        var unitOperationCallResult = SaveAppTransactionUnitFormulaSetDto(aAppTransactionUnitFormulaSetDto);

                        if (unitOperationCallResult != null)
                        {
                            aValidationResult.Merge(unitOperationCallResult.ValidationResult);
                            if (unitOperationCallResult.Object != null)
                            {
                                objectList.Add(unitOperationCallResult.Object);
                            }
                        }

                    }
                }
            }

            aOperationCallResult.ObjectList = objectList;

            if (transactionId.HasValue)
            {
                AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId.Value);
            }

            return aOperationCallResult;
        }



        public static OperationCallResult<AppTransactionUnitFormulaSetDto> SaveAppTransactionUnitFormulaSetDto(AppTransactionUnitFormulaSetDto aAppTransactionUnitFormulaSetDto)
        {
            OperationCallResult<AppTransactionUnitFormulaSetDto> aOperationCallResult = new OperationCallResult<AppTransactionUnitFormulaSetDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            aAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula = aAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula.Where(o =>
                !(o.OperationType.HasValue && (o.OperationType.Value == (int)EmAppFormularType.SubscribeFromGridColumnAggregation
                || o.OperationType.Value == (int)EmAppFormularType.SubscribeFromParentLevelField))).ToList();

            foreach (AppTransactionUnitFormulaDto formulaDto in aAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula)
            {

                aValidationResult.Merge(formulaDto.ValidateDto(aAppTransactionUnitFormulaSetDto.TransactionUnitName));
            }

            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }




            //AppTransactionUnitExDto aAppTransactionUnitExDto = AppTransactionBL.RetrieveOneAppTransactionUnitExDto(aAppTransactionUnitFormulaSetDto.TransactionUnitId);

            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppTransactionUnitFormulaSetDto.TransactionId);
            AppTransactionUnitExDto aAppTransactionUnitExDto = null;

            if (transactionExDto != null && transactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(aAppTransactionUnitFormulaSetDto.TransactionUnitId.ToString()))
            {
                aAppTransactionUnitExDto = transactionExDto.DictAllTransactionUnitIdExDto[aAppTransactionUnitFormulaSetDto.TransactionUnitId.ToString()];

                //List<AppTransactionFieldExDto> transactionFieldList = aAppTransactionUnitExDto.AppTransactionFieldList.ToList();

                //if (!aAppTransactionUnitExDto.ParentTransactionUnitId.HasValue && !(aAppTransactionUnitExDto.IsMasterSiblingUnit.HasValue && aAppTransactionUnitExDto.IsMasterSiblingUnit.Value))
                //{
                //    transactionFieldList = transactionExDto.DictRootLevelUnitTransactionField.Values.ToList();
                //}

                List<AppTransactionFieldExDto> transactionFieldList = transactionExDto.DictAllTransactionField.Values.ToList();
                InitialTransactionFieldFormularDisplayName(transactionExDto, transactionFieldList);

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitFormulaEntity), new RelationPredicateBucket(AppTransactionUnitFormulaFields.TransactionUnitId == aAppTransactionUnitFormulaSetDto.TransactionUnitId));

                        if (aAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula.Count > 0)
                        {
                            foreach (var dto in aAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula)
                            {
                                if (string.IsNullOrEmpty(dto.FormulaExpression))
                                {
                                    dto.FormulaExpression = string.Empty;
                                }

                                AppTransactionUnitFormulaEntity aAppTransactionUnitFormulaEntity = new AppTransactionUnitFormulaEntity();
                                AppTransactionUnitFormulaConverter.CopyDtoToEntity(aAppTransactionUnitFormulaEntity, InFormatFormulaExepressForTransactionUnit(transactionFieldList, dto));
                                aAppTransactionUnitFormulaEntity.TransactionUnitId = aAppTransactionUnitFormulaSetDto.TransactionUnitId;

                                adapter.SaveEntity(aAppTransactionUnitFormulaEntity);
                            }
                        }

                        adapter.Commit();

                        ValidationItem item = aValidationResult.AddItem(typeof(AppTransactionUnitFormulaEntity), "App_TransactionUnitFormula_Save_OK", ValidationItemType.Message, "Transaction Unit [{0}]: Formula Saved Successfully", aAppTransactionUnitFormulaSetDto.TransactionUnitName);
                        item.MessageParameters.Add(aAppTransactionUnitFormulaSetDto.TransactionUnitName);


                    }
                    catch (Exception ex)
                    {
                        adapter.Rollback();
                        ValidationItem item = aValidationResult.AddItem(typeof(AppTransactionUnitFormulaEntity), "App_TransactionUnitFormula_SaveError", ValidationItemType.Error, "Transaction Unit [{0}]: Formula Save Error: \n    {1}", aAppTransactionUnitFormulaSetDto.TransactionUnitName);
                        item.MessageParameters.Add(aAppTransactionUnitFormulaSetDto.TransactionUnitName);
                        item.MessageParameters.Add(ex.ToString());
                    }
                }


            }
            else
            {
                ValidationItem item = aValidationResult.AddItem(typeof(AppTransactionUnitFormulaEntity), "App_TransactionUnitFormula_SaveError", ValidationItemType.Error, "Transaction Unit [{0}]: Formula Save Error: \n    {1}", aAppTransactionUnitFormulaSetDto.TransactionUnitName);
            }



            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveAppTransactionUnitFormulaSetDto(aAppTransactionUnitFormulaSetDto.TransactionUnitId, aAppTransactionUnitFormulaSetDto.TransactionId);

                AppCacheManagerBL.RefreshOnetHierarchyTranscation(aAppTransactionUnitFormulaSetDto.TransactionId);
            }

            return aOperationCallResult;
        }



        public static OperationCallResult<AppTransactionUnitFormulaSetDto> SaveAppSearchViewFormulaSetDto(AppTransactionUnitFormulaSetDto aAppTransactionUnitFormulaSetDto)
        {
            OperationCallResult<AppTransactionUnitFormulaSetDto> aOperationCallResult = new OperationCallResult<AppTransactionUnitFormulaSetDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            foreach (AppTransactionUnitFormulaDto formulaDto in aAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula)
            {
                aValidationResult.Merge(formulaDto.ValidateDto(aAppTransactionUnitFormulaSetDto.TransactionUnitName));
            }

            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            AppSearchViewExDto searchViewExDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(aAppTransactionUnitFormulaSetDto.SearchViewId);

            if (searchViewExDto != null)
            {
                List<AppSearchViewFieldExDto> searchViewFieldList = searchViewExDto.AppSearchViewFieldList.ToList();

                InitialSearchViewFieldFormularDisplayName(searchViewFieldList);

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitFormulaEntity), new RelationPredicateBucket(AppTransactionUnitFormulaFields.SearchViewId == aAppTransactionUnitFormulaSetDto.SearchViewId.Value));

                        if (aAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula.Count > 0)
                        {
                            foreach (var dto in aAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula)
                            {
                                if (string.IsNullOrEmpty(dto.FormulaExpression))
                                {
                                    dto.FormulaExpression = string.Empty;
                                }

                                AppTransactionUnitFormulaEntity aAppTransactionUnitFormulaEntity = new AppTransactionUnitFormulaEntity();
                                AppTransactionUnitFormulaConverter.CopyDtoToEntity(aAppTransactionUnitFormulaEntity, InFormatFormulaExepressForSearchView(searchViewFieldList, dto));
                                aAppTransactionUnitFormulaEntity.SearchViewId = aAppTransactionUnitFormulaSetDto.SearchViewId;

                                adapter.SaveEntity(aAppTransactionUnitFormulaEntity);
                            }
                        }

                        adapter.Commit();

                        ValidationItem item = aValidationResult.AddItem(typeof(AppTransactionUnitFormulaEntity), "App_TransactionUnitFormula_Save_OK", ValidationItemType.Message, "Search View Formula Saved Successfully", aAppTransactionUnitFormulaSetDto.TransactionUnitName);
                        item.MessageParameters.Add(aAppTransactionUnitFormulaSetDto.TransactionUnitName);


                    }
                    catch (Exception ex)
                    {
                        adapter.Rollback();
                        ValidationItem item = aValidationResult.AddItem(typeof(AppTransactionUnitFormulaEntity), "App_TransactionUnitFormula_SaveError", ValidationItemType.Error, "Search View Formula Save Error: \n    {0}");
                        item.MessageParameters.Add(aAppTransactionUnitFormulaSetDto.TransactionUnitName);
                        item.MessageParameters.Add(ex.ToString());
                    }
                }


            }
            else
            {
                ValidationItem item = aValidationResult.AddItem(typeof(AppTransactionUnitFormulaEntity), "App_TransactionUnitFormula_SaveError", ValidationItemType.Error, "Search View Formula Save Error: \n    {0}");
            }



            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveAppSearchViewFormulaSetDto(aAppTransactionUnitFormulaSetDto.SearchViewId.Value);

            }

            return aOperationCallResult;
        }



        // Formular Help Methods:
        public static AppTransactionUnitFormulaDto InFormatFormulaExepressForTransactionUnit(List<AppTransactionFieldExDto> transactionFieldList, AppTransactionUnitFormulaDto aAppTransactionUnitFormulaDto)
        {
            string expression = aAppTransactionUnitFormulaDto.FormulaExpression;
            MatchCollection matchList = FormulaRegex.Matches(aAppTransactionUnitFormulaDto.FormulaExpression);
            for (int i = 0; i < matchList.Count; i++)
            {
                string fieldFormularDisplayName = matchList[i].Value.ToString().Trim();

                AppTransactionFieldExDto aAppTransactionFieldDto = transactionFieldList.FirstOrDefault(o => o.FormulaDisplayName.ToLower().Trim() == fieldFormularDisplayName.ToLower().Trim());
                if (aAppTransactionFieldDto != null)
                {
                    expression = expression.Replace(fieldFormularDisplayName, TransactionFieldFormulaPrefix + aAppTransactionFieldDto.Id.ToString());
                }
            }
            aAppTransactionUnitFormulaDto.FormulaExpression = expression;
            return aAppTransactionUnitFormulaDto;
        }

        public static AppTransactionUnitFormulaDto InFormatFormulaExepressForSearchView(List<AppSearchViewFieldExDto> searchFieldList, AppTransactionUnitFormulaDto aAppTransactionUnitFormulaDto)
        {
            string expression = aAppTransactionUnitFormulaDto.FormulaExpression;
            MatchCollection matchList = FormulaRegex.Matches(aAppTransactionUnitFormulaDto.FormulaExpression);
            for (int i = 0; i < matchList.Count; i++)
            {
                string fieldFormularDisplayName = matchList[i].Value.ToString().Trim();

                AppSearchViewFieldExDto searchViewFieldDto = searchFieldList.FirstOrDefault(o => o.FormulaDisplayName.ToLower().Trim() == fieldFormularDisplayName.ToLower().Trim());
                if (searchViewFieldDto != null)
                {
                    expression = expression.Replace(fieldFormularDisplayName, TransactionFieldFormulaPrefix + searchViewFieldDto.Id.ToString());
                }
            }
            aAppTransactionUnitFormulaDto.FormulaExpression = expression;
            return aAppTransactionUnitFormulaDto;
        }

        public static List<AppTransactionUnitFormulaSetDto> RetrieveAppTransactionUnitFormulaSetDtoList(int? transactionId)
        {
            if (!transactionId.HasValue)
            {
                return null;
            }

            List<AppTransactionUnitFormulaSetDto> toReturnList = new List<AppTransactionUnitFormulaSetDto>();

            var transactionExDto = AppTransactionBL.RetrieveOneAppTransactionExDto(transactionId);

            if (transactionExDto != null)
            {


                List<int> unitIdList = transactionExDto.AppTransactionUnitList.Select(o => (int)o.Id).ToList();
                foreach (int unitId in unitIdList)
                {
                    toReturnList.Add(RetrieveAppTransactionUnitFormulaSetDto(unitId, transactionId.Value));
                }

            }

            return toReturnList;
        }

        public static AppTransactionUnitFormulaSetDto RetrieveAppSearchViewFormulaSetDto(int searchViewId)
        {
            AppTransactionUnitFormulaSetDto aAppTransactionUnitFormulaSetDto = new AppTransactionUnitFormulaSetDto();
            aAppTransactionUnitFormulaSetDto.OrgFormulaExDtoList = new List<AppTransactionUnitFormulaExDto>();
            List<AppTransactionUnitFormulaDto> toReturn = new List<AppTransactionUnitFormulaDto>();

            AppSearchViewExDto searchViewExDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(searchViewId);

            if (searchViewExDto != null)
            {
                List<AppSearchViewFieldExDto> searchViewFieldList = searchViewExDto.AppSearchViewFieldList.ToList();


                InitialSearchViewFieldFormularDisplayName(searchViewFieldList);

                EntityCollection<AppTransactionUnitFormulaEntity> entityList = new EntityCollection<AppTransactionUnitFormulaEntity>();
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    adapter.FetchEntityCollection(entityList, new RelationPredicateBucket(AppTransactionUnitFormulaFields.SearchViewId == searchViewId));

                }

                foreach (var formulaEntity in entityList.OrderBy(o => o.CaculationFlowSort))
                {
                    var orgFormulaExDto = AppTransactionUnitFormulaConverter.ConvertEntityToExDto(formulaEntity);
                    aAppTransactionUnitFormulaSetDto.OrgFormulaExDtoList.Add(orgFormulaExDto);
                    orgFormulaExDto.LeadFunctionSettingDto = new AppFormulaLeadSettingDto();

                    if (orgFormulaExDto.OperationType.HasValue && orgFormulaExDto.OperationType.Value == (int)EmAppFormularType.LeadAssignment)
                    {
                        orgFormulaExDto.LeadFunctionSettingDto = JsonConvert.DeserializeObject<AppFormulaLeadSettingDto>(formulaEntity.FormulaExpression);
                    }


                    string orgFormulaExpression = formulaEntity.FormulaExpression;
                    var dto = AppTransactionUnitFormulaConverter.ConvertEntityToDto(OutFormatFormulaExpressForSearchView(searchViewFieldList, formulaEntity));
                    dto.LeadFunctionSettingDto = orgFormulaExDto.LeadFunctionSettingDto;
                    dto.OrgFormulaExpression = orgFormulaExpression;
                    toReturn.Add(dto);

                }



                aAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula = toReturn;
                aAppTransactionUnitFormulaSetDto.SearchViewId = searchViewId;
                aAppTransactionUnitFormulaSetDto.TransactionUnitName = searchViewExDto.Name;

                aAppTransactionUnitFormulaSetDto.ListAppTransactionUnitFormula.ForAll(o =>
                {
                    if ((o.OperationType.HasValue && o.OperationType.Value == (int)EmAppFormularType.LeadAssignment))
                    {
                        //o.LeadFunctionSettingDto = JsonConvert.DeserializeObject<AppFormulaLeadSettingDto>(o.OrgFormulaExpression);

                        if (o.LeadFunctionSettingDto != null)
                        {
                            o.AssignToTransFieldId = o.LeadFunctionSettingDto.AssignToFieldId;
                        }
                    }
                    else
                    {
                        int assignToField = AppTransactionUnitFormulaExDto.GetAssignmentLeftSideFieldId(o.OrgFormulaExpression);

                        if (assignToField > 0)
                        {
                            o.AssignToTransFieldId = assignToField;
                        }
                    }


                });

                return aAppTransactionUnitFormulaSetDto;
            }

            return null;
        }


        public static AppTransactionUnitFormulaEntity OutFormatFormulaExpressForTransactionUnit(List<AppTransactionFieldExDto> transactionFieldList, AppTransactionUnitFormulaEntity aAppTransactionUnitFormulaEntity)
        {
            string expression = aAppTransactionUnitFormulaEntity.FormulaExpression;
            var members = aAppTransactionUnitFormulaEntity.FormulaExpression.Split(FormulaConstString, StringSplitOptions.RemoveEmptyEntries);
            foreach (string info in members)
            {
                if (info.Trim().StartsWith(TransactionFieldFormulaPrefix))
                {
                    string id = info.Replace(TransactionFieldFormulaPrefix, "").Trim();

                    AppTransactionFieldExDto aAppTransactionFieldDto = transactionFieldList.FirstOrDefault(o => object.Equals(o.Id.ToString().Trim(), id.Trim()));
                    if (aAppTransactionFieldDto != null)
                    {
                        expression = expression.Replace(info, aAppTransactionFieldDto.FormulaDisplayName);
                    }
                }
            }
            aAppTransactionUnitFormulaEntity.FormulaExpression = expression;
            return aAppTransactionUnitFormulaEntity;
        }

        public static AppTransactionUnitFormulaEntity OutFormatFormulaExpressForSearchView(List<AppSearchViewFieldExDto> searchViewFieldList, AppTransactionUnitFormulaEntity aAppTransactionUnitFormulaEntity)
        {
            string expression = aAppTransactionUnitFormulaEntity.FormulaExpression;
            var members = aAppTransactionUnitFormulaEntity.FormulaExpression.Split(FormulaConstString, StringSplitOptions.RemoveEmptyEntries);
            foreach (string info in members)
            {
                if (info.Trim().StartsWith(TransactionFieldFormulaPrefix))
                {
                    string id = info.Replace(TransactionFieldFormulaPrefix, "").Trim();

                    AppSearchViewFieldExDto searchViewFieldDto = searchViewFieldList.FirstOrDefault(o => object.Equals(o.Id.ToString().Trim(), id.Trim()));
                    if (searchViewFieldDto != null)
                    {
                        expression = expression.Replace(info, searchViewFieldDto.FormulaDisplayName);
                    }
                }
            }
            aAppTransactionUnitFormulaEntity.FormulaExpression = expression;
            return aAppTransactionUnitFormulaEntity;
        }


        public static AppTransactionFieldAggFunctionSetDto RetrieveAppTransactionFieldAggFunctionSetDto(int fieldId)
        {
            List<AppTransactionFieldAggFunctionDto> toReturn = new List<AppTransactionFieldAggFunctionDto>();

            EntityCollection<AppTransactionFieldAggFunctionEntity> entityList = new EntityCollection<AppTransactionFieldAggFunctionEntity>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(entityList, new RelationPredicateBucket(AppTransactionFieldAggFunctionFields.TransactionFieldId == fieldId));

            }

            foreach (var formulaEntity in entityList)
            {
                toReturn.Add(AppTransactionFieldAggFunctionConverter.ConvertEntityToDto(formulaEntity));

            }

            AppTransactionFieldAggFunctionSetDto aAppTransactionFieldAggFunctionSetDto = new AppTransactionFieldAggFunctionSetDto();

            aAppTransactionFieldAggFunctionSetDto.ListAppTransactionFieldAggFunction = toReturn;
            aAppTransactionFieldAggFunctionSetDto.TransactionFieldId = fieldId;

            return aAppTransactionFieldAggFunctionSetDto;
        }

        public static AppTransactionFieldAggFunctionSetDto SaveAppTransactionFieldAggFunctionSetDto(AppTransactionFieldAggFunctionSetDto aAppTransactionFieldAggFunctionSetDto)
        {
            EntityCollection<AppTransactionFieldAggFunctionEntity> orgAggFuncEntityListFromDB = new EntityCollection<AppTransactionFieldAggFunctionEntity>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(orgAggFuncEntityListFromDB, new RelationPredicateBucket(AppTransactionFieldAggFunctionFields.TransactionFieldId == aAppTransactionFieldAggFunctionSetDto.TransactionFieldId));

                var aggFuncTypeIdsFromDB = orgAggFuncEntityListFromDB.Where(o => o.AggregationFunctionType.HasValue).Select(o => o.AggregationFunctionType.Value).ToList();
                var needToSaveAggFuncTypeIds = aAppTransactionFieldAggFunctionSetDto.ListAppTransactionFieldAggFunction.Where(o => o.AggregationFunctionType.HasValue).Select(o => o.AggregationFunctionType.Value).ToList();

                //Delete Id
                List<int> deleteAggFuncTypeIds = aggFuncTypeIdsFromDB.Except(needToSaveAggFuncTypeIds).ToList();
                List<int> newAggFuncTypeIds = needToSaveAggFuncTypeIds.Except(aggFuncTypeIdsFromDB).ToList();

                aAppTransactionFieldAggFunctionSetDto.ListAppTransactionFieldAggFunction.Where(o => o.AggregationFunctionType.HasValue && newAggFuncTypeIds.Contains(o.AggregationFunctionType.Value)).ToList();

                EntityCollection<AppTransactionFieldAggFunctionEntity> entityList = new EntityCollection<AppTransactionFieldAggFunctionEntity>();

                adapter.DeleteEntitiesDirectly(typeof(AppTransactionFieldAggFunctionEntity), new RelationPredicateBucket(
                    AppTransactionFieldAggFunctionFields.TransactionFieldId == aAppTransactionFieldAggFunctionSetDto.TransactionFieldId
                    & AppTransactionFieldAggFunctionFields.AggregationFunctionType == deleteAggFuncTypeIds.ToArray()));

                foreach (var dto in aAppTransactionFieldAggFunctionSetDto.ListAppTransactionFieldAggFunction)
                {
                    if (dto.AggregationFunctionType.HasValue && newAggFuncTypeIds.Contains(dto.AggregationFunctionType.Value))
                    {

                        AppTransactionFieldAggFunctionEntity aAppTransactionFieldAggFunctionEntity = new AppTransactionFieldAggFunctionEntity();
                        aAppTransactionFieldAggFunctionEntity.TransactionFieldId = aAppTransactionFieldAggFunctionSetDto.TransactionFieldId;
                        AppTransactionFieldAggFunctionConverter.CopyDtoToEntity(aAppTransactionFieldAggFunctionEntity, dto);
                        aAppTransactionFieldAggFunctionEntity.TransactionFieldId = aAppTransactionFieldAggFunctionSetDto.TransactionFieldId;
                        adapter.SaveEntity(aAppTransactionFieldAggFunctionEntity);
                    }
                }

            }

            if (aAppTransactionFieldAggFunctionSetDto.TransactionId.HasValue)
            {
                AppCacheManagerBL.RefreshOnetHierarchyTranscation(aAppTransactionFieldAggFunctionSetDto.TransactionId.Value);
            }




            return RetrieveAppTransactionFieldAggFunctionSetDto(aAppTransactionFieldAggFunctionSetDto.TransactionFieldId);
        }

        private static AppTransactionExDto PrepareTransactionCrossRelationFromFormula(List<AppTransactionUnitFormulaSetDto> appTransactionUnitFormulaSetDtoList)
        {
            var transactionExDto = appTransactionUnitFormulaSetDtoList[0].NeedToUpdateTransactionExDto;
            List<int> formulaUnitIdList = appTransactionUnitFormulaSetDtoList.Select(o => o.TransactionUnitId).ToList();


            Dictionary<int, AppTransactionFieldExDto> dictTransFieldIdAndDto = new Dictionary<int, AppTransactionFieldExDto>();

            foreach (var unitDto in transactionExDto.AppTransactionUnitList)
            {
                unitDto.AppTransactionFieldList.ForAll(o => dictTransFieldIdAndDto.Add((int)o.Id, o));

                foreach (var childUnitDto in unitDto.Children)
                {
                    childUnitDto.AppTransactionFieldList.ForAll(o => dictTransFieldIdAndDto.Add((int)o.Id, o));

                    foreach (var grandchildUnitDto in childUnitDto.Children)
                    {
                        grandchildUnitDto.AppTransactionFieldList.ForAll(o => dictTransFieldIdAndDto.Add((int)o.Id, o));
                    }
                }
            }


            List<int> transFieldIds_SubscribeFromGridColumnAggregation = new List<int>();
            List<int> transFieldIds_SubscribeFromParentLevelField = new List<int>();

            foreach (var formularSet in appTransactionUnitFormulaSetDtoList)
            {
                transFieldIds_SubscribeFromGridColumnAggregation.AddRange(formularSet.ListAppTransactionUnitFormula.Where(o => o.OperationType.HasValue
                    && o.OperationType.Value == (int)EmAppFormularType.SubscribeFromGridColumnAggregation
                    && o.AssignToTransFieldId.HasValue).Select(o => o.AssignToTransFieldId.Value).ToList());

                transFieldIds_SubscribeFromParentLevelField.AddRange(formularSet.ListAppTransactionUnitFormula.Where(o => o.OperationType.HasValue
                    && o.OperationType.Value == (int)EmAppFormularType.SubscribeFromParentLevelField
                    && o.AssignToTransFieldId.HasValue).Select(o => o.AssignToTransFieldId.Value).ToList());
            }

            foreach (int transFiledId in transactionExDto.DictTransFieldIdAndCrossRelationSettingDto.Keys)
            {
                AppTransactionFieldCrossRelationSettingDto crossSettingDto = transactionExDto.DictTransFieldIdAndCrossRelationSettingDto[transFiledId];
                if (crossSettingDto.ParentUnitSubscribeChildAggFunctionId.HasValue)
                {
                    if (!transFieldIds_SubscribeFromGridColumnAggregation.Contains(transFiledId))
                    {
                        if (dictTransFieldIdAndDto.ContainsKey(transFiledId))
                        {
                            var fieldDto = dictTransFieldIdAndDto[transFiledId];
                            if (formulaUnitIdList.Contains(fieldDto.TransactionUnitId))
                            {
                                fieldDto.ParentUnitSubscribeChildAggFunctionId = null;
                                transactionExDto.IsModified = true;
                            }

                        }

                    }
                }
                else if (crossSettingDto.SubscribeToTransFieldId.HasValue)
                {
                    if (!transFieldIds_SubscribeFromParentLevelField.Contains(transFiledId))
                    {
                        if (dictTransFieldIdAndDto.ContainsKey(transFiledId))
                        {
                            var fieldDto = dictTransFieldIdAndDto[transFiledId];
                            if (formulaUnitIdList.Contains(fieldDto.TransactionUnitId))
                            {
                                fieldDto.ChildUnitSubscribeParentFieldId = null;
                                transactionExDto.IsModified = true;
                            }


                        }
                    }
                }
            }

            return transactionExDto;
        }


    }
}