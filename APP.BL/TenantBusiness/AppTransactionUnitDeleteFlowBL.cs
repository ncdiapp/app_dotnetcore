using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
    public static class AppTransactionUnitDeleteFlowBL
    {

      


        //CRUD!!
        public static ObservableSet<AppTransactionUnitDeleteFlowExDto> RetrieveDeleteFlowListByTransactionUnitId(object transactionUnitId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionUnitDeleteFlowEntity> entityList = new EntityCollection<AppTransactionUnitDeleteFlowEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionUnitDeleteFlowFields.TransactionUnitId == transactionUnitId);

                adapter.FetchEntityCollection(entityList, filter);


                var aDtoList = new ObservableSet<AppTransactionUnitDeleteFlowExDto>();
                foreach (var AppTransactionUnitDeleteFlowEntity in entityList)
                {
                    AppTransactionUnitDeleteFlowExDto aAppTransactionUnitDeleteFlowExDto = AppTransactionUnitDeleteFlowConverter.ConvertEntityToExDto(AppTransactionUnitDeleteFlowEntity);


                    aDtoList.Add(aAppTransactionUnitDeleteFlowExDto);

                }
                return aDtoList;
            }
        }


  

   

        public static OperationCallResult<AppTransactionUnitDeleteFlowExDto> SaveAllAppTransactionUnitDeleteFlowExDto(ObservableSet<AppTransactionUnitDeleteFlowExDto> aSet, int transactionId)
        {
            OperationCallResult<AppTransactionUnitDeleteFlowExDto> aOperationCallResult = new OperationCallResult<AppTransactionUnitDeleteFlowExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;
            
            foreach (AppTransactionUnitDeleteFlowExDto aAppTransactionUnitDeleteFlowExDto in aSet) {
                validationResult.Merge(aAppTransactionUnitDeleteFlowExDto.ValidateDto());                
            }

            //if (aSet.Count > 1) 
            //{
            //    var storedProcedureFlow = aSet.FirstOrDefault(o => !string.IsNullOrEmpty(o.StoredProcedureName));
            //    if (storedProcedureFlow != null)
            //    {
            //        validationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitDeleteFlowEntity), "App_TransactionUnitDeleteFlow_OnlyOneFlowAllowedIfStoredProcedureIsUsed", ValidationItemType.Error, "Only One Delete Flow Allowed If Stored Procedure Is Used"));
            //    }
            //}

            if (validationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = validationResult;
                return aOperationCallResult;
            }

            aSet.FindNewItems().ForAll(o => validationResult.Merge(ProcessNewDto(o)));
            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(ProcessDeleteDto(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveDeleteFlowListByTransactionUnitId(transactionId);
            }

            return aOperationCallResult;

        }


        private static AppTransactionUnitDeleteFlowEntity RetrieveOneAppTransactionUnitDeleteFlowEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionUnitDeleteFlowEntity aAppTransactionUnitDeleteFlowEntity = new AppTransactionUnitDeleteFlowEntity(int.Parse(Id.ToString()));
                adapter.FetchEntity(aAppTransactionUnitDeleteFlowEntity);
                return aAppTransactionUnitDeleteFlowEntity;
            }
        }
        private static ValidationResult ProcessNewDto(AppTransactionUnitDeleteFlowExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTransactionUnitDeleteFlowEntity aAppTransactionUnitDeleteFlowEntity = new AppTransactionUnitDeleteFlowEntity();
            AppTransactionUnitDeleteFlowConverter.CopyDtoToEntity(aAppTransactionUnitDeleteFlowEntity, aDto);



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionUnitDeleteFlowEntity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitDeleteFlowEntity), "plm_AppTransactionUnitDeleteFlowEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

              // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitDeleteFlowEntity), "plm_AppTransactionUnitDeleteFlowEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

              // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitDeleteFlowEntity), "plm_AppTransactionUnitDeleteFlowEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppTransactionUnitDeleteFlowExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTransactionUnitDeleteFlowEntity aAppTransactionUnitDeleteFlowEntity = RetrieveOneAppTransactionUnitDeleteFlowEntity(aDto.Id);

            AppTransactionUnitDeleteFlowConverter.CopyDtoToEntity(aAppTransactionUnitDeleteFlowEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionUnitDeleteFlowEntity, false, true);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitDeleteFlowEntity), "plm_AppTransactionUnitDeleteFlowEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


               // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitDeleteFlowEntity), "plm_AppTransactionUnitDeleteFlowEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
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
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppTransactionUnitDeleteFlowEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitDeleteFlowEntity), new RelationPredicateBucket(AppTransactionUnitDeleteFlowFields.DeleteFlowId == Id));
                    adapter.Commit();
                }

               
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitDeleteFlowEntity), "plm_AppTransactionUnitDeleteFlowEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }   




    }
}