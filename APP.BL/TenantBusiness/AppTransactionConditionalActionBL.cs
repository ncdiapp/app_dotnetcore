using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;

using APP.Framework;
namespace App.BL
{
    public static class AppTransactionConditionalActionBL
    {

        public static ObservableSet<AppConditionalActionExDto> RetrieveAllAppConditionalActionListByTransactionId(object transactionId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppConditionalActionEntity> entityList = new EntityCollection<AppConditionalActionEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppConditionalActionFields.TransactionId == transactionId);
                filter.PredicateExpression.AddWithAnd(AppConditionalActionFields.UitriggerTransactionFieldId == DBNull.Value);
                adapter.FetchEntityCollection(entityList, filter);

                var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
                var transFieldList = transactionExDto.DictRootLevelUnitTransactionField.Values.ToList();
                AppTransactionFormulaSetupBL.InitialTransactionFieldFormularDisplayName(transactionExDto, transFieldList);
                var aDtoList = new ObservableSet<AppConditionalActionExDto>();
                foreach (var AppConditionalActionEntity in entityList)
                {

                    AppConditionalActionExDto aAppConditionalActionExDto = AppConditionalActionConverter.ConvertEntityToExDto(AppConditionalActionEntity);
                    OutFormatConditionFormula(transFieldList, aAppConditionalActionExDto);



                    aDtoList.Add(aAppConditionalActionExDto);

                }
                return aDtoList;
            }
        }

        public static ObservableSet<AppConditionalActionExDto> RetrieveTransactionFieldUiTriggerConditionalActionList(object transactionId, int uiTriggerFieldId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppConditionalActionEntity> entityList = new EntityCollection<AppConditionalActionEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppConditionalActionFields.TransactionId == transactionId);


                filter.PredicateExpression.AddWithAnd(AppConditionalActionFields.UitriggerTransactionFieldId == uiTriggerFieldId);


                adapter.FetchEntityCollection(entityList, filter);

                var aDtoList = new ObservableSet<AppConditionalActionExDto>();

                //AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

                //if (transactionExDto != null && transactionExDto.DictAllTransactionField.ContainsKey(uiTriggerFieldId))
                //{
                //    var triggerFiled = transactionExDto.DictAllTransactionField[uiTriggerFieldId];
                //    var aAppTransactionUnitExDto = transactionExDto.DictAllTransactionUnitIdExDto[triggerFiled.TransactionUnitId.ToString()];

                //    List<AppTransactionFieldExDto> transactionFieldList = aAppTransactionUnitExDto.AppTransactionFieldList.ToList();

                //    if (!aAppTransactionUnitExDto.ParentTransactionUnitId.HasValue && !(aAppTransactionUnitExDto.IsMasterSiblingUnit.HasValue && aAppTransactionUnitExDto.IsMasterSiblingUnit.Value))
                //    {
                //        transactionFieldList = transactionExDto.DictRootLevelUnitTransactionField.Values.ToList();
                //    }

                //    AppTransactionFormulaBL.InitialTransactionFieldFormularDisplayName(transactionExDto, transactionFieldList);
                //}

                var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
                var transFieldList = transactionExDto.DictRootLevelUnitTransactionField.Values.ToList();
                AppTransactionFormulaSetupBL.InitialTransactionFieldFormularDisplayName(transactionExDto, transFieldList);

                foreach (var AppConditionalActionEntity in entityList.OrderBy(o => o.Name))
                {
                    //if (!string.IsNullOrWhiteSpace(AppConditionalActionEntity.BooleanConditionFormula))
                    //{
                    //    AppTransactionFormulaBL.OutFormatFormulaExpressForTransactionUnit(transactionFieldList, formulaEntity)));
                    //}

                    AppConditionalActionExDto aAppConditionalActionExDto = AppConditionalActionConverter.ConvertEntityToExDto(AppConditionalActionEntity);
                   
                    OutFormatConditionFormula(transFieldList, aAppConditionalActionExDto);

                    aDtoList.Add(aAppConditionalActionExDto);

                }

                return aDtoList;
            }
        }



        public static AppConditionalActionEntity RetrieveOneAppConditionalActionEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppConditionalActionEntity aAppConditionalActionEntity = new AppConditionalActionEntity(int.Parse(Id.ToString()));
                adapter.FetchEntity(aAppConditionalActionEntity);
                return aAppConditionalActionEntity;
            }
        }

        public static OperationCallResult<AppConditionalActionExDto> SaveAllAppConditionalActionExDto(ObservableSet<AppConditionalActionExDto> aSet, int transactionId, int? uiTriggerFieldId = null)
        {
            OperationCallResult<AppConditionalActionExDto> aOperationCallResult = new OperationCallResult<AppConditionalActionExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            var transFieldList = transactionExDto.DictRootLevelUnitTransactionField.Values.ToList();
            AppTransactionFormulaSetupBL.InitialTransactionFieldFormularDisplayName(transactionExDto, transFieldList);

            foreach (var dto in aSet)
            {
                InFormatConditionFormula(transFieldList, dto);
            }

            aSet.FindNewItems().ForAll(o => validationResult.Merge(ProcessNewDto(o)));
            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(ProcessDeleteDto(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                if (!uiTriggerFieldId.HasValue)
                {
                    aOperationCallResult.ObjectList = RetrieveAllAppConditionalActionListByTransactionId(transactionId);
                }
                else
                {
                    aOperationCallResult.ObjectList = RetrieveTransactionFieldUiTriggerConditionalActionList(transactionId, uiTriggerFieldId.Value);
                }

            }

            return aOperationCallResult;

        }


        private static ValidationResult ProcessNewDto(AppConditionalActionExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppConditionalActionEntity aAppConditionalActionEntity = new AppConditionalActionEntity();
            AppConditionalActionConverter.CopyDtoToEntity(aAppConditionalActionEntity, aDto);



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppConditionalActionEntity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppConditionalActionEntity), "plm_AppConditionalActionEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppConditionalActionEntity), "plm_AppConditionalActionEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppConditionalActionEntity), "plm_AppConditionalActionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppConditionalActionExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppConditionalActionEntity aAppConditionalActionEntity = RetrieveOneAppConditionalActionEntity(aDto.Id);

            AppConditionalActionConverter.CopyDtoToEntity(aAppConditionalActionEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppConditionalActionEntity, false, true);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppConditionalActionEntity), "plm_AppConditionalActionEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppConditionalActionEntity), "plm_AppConditionalActionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDeleteDto(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppConditionalActionEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppConditionalActionEntity), new RelationPredicateBucket(AppConditionalActionFields.ActionId == Id));
                    adapter.Commit();
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppConditionalActionEntity), "plm_AppConditionalActionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }


        private static void InFormatConditionFormula(List<AppTransactionFieldExDto> transactionFieldList, AppConditionalActionExDto dto)
        {
            if (transactionFieldList != null && dto != null)
            {
                string expression = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dto.BooleanConditionFormulaDisplay);
                expression = AppPorjectWorkFlowBL.InFormatExpressionString(transactionFieldList, expression);
                dto.BooleanConditionFormula = expression;
            }
        }

        private static void OutFormatConditionFormula(List<AppTransactionFieldExDto> transactionFieldList, AppConditionalActionExDto dto)
        {
            if (dto != null && dto.BooleanConditionFormula != null)
            {
                string expression = dto.BooleanConditionFormula;
                expression = AppPorjectWorkFlowBL.OutFormatExpressionString(transactionFieldList, expression);
                dto.BooleanConditionFormulaDisplay = expression;
            }

        }


    }

    public class AppTransConditional
    {
        public int? LockAllTranscationBooleanFiledId
        {
            get;
            set;
        }



        // key root Filed key
        public Dictionary<int, List<int>> DictLockRootUnitField
        {
            get;
            set;
        }

        // key1: Parent unitId, key2:  parent  Unit Field, List<int> ChuldUnit
        public Dictionary<int, Dictionary<int, List<int>>> DictChildUnitField
        {
            get;
            set;
        }


        // key1: Parent unitId, key2:parent  Unit Field, key3:childUnit, value List<int> ChuldUnit Filedid
        public Dictionary<int, Dictionary<int, Dictionary<int, List<int>>>> DictChildUnitLockField
        {
            get;
            set;
        }


    }
}