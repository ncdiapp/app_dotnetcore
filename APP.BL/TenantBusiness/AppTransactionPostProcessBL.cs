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
    public static class AppTransactionPostProcessBL
    {

        public static ObservableSet<AppTransactionPostProcessExDto> RetrieveProcessListByTransactionId(object transactionId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionPostProcessEntity> entityList = new EntityCollection<AppTransactionPostProcessEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionPostProcessFields.TransactionId == transactionId);

                adapter.FetchEntityCollection(entityList, filter);


                var aDtoList = new ObservableSet<AppTransactionPostProcessExDto>();
                foreach (var AppTransactionPostProcessEntity in entityList)
                {
                    AppTransactionPostProcessExDto aAppTransactionPostProcessExDto = AppTransactionPostProcessConverter.ConvertEntityToExDto(AppTransactionPostProcessEntity);


                    aDtoList.Add(aAppTransactionPostProcessExDto);

                }
                return aDtoList;
            }
        }


 

        public static AppTransactionPostProcessEntity RetrieveOneAppTransactionPostProcessEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionPostProcessEntity aAppTransactionPostProcessEntity = new AppTransactionPostProcessEntity(int.Parse(Id.ToString()));
                adapter.FetchEntity(aAppTransactionPostProcessEntity);
                return aAppTransactionPostProcessEntity;
            }
        }

        public static OperationCallResult<AppTransactionPostProcessExDto> SaveAllAppTransactionPostProcessExDto(ObservableSet<AppTransactionPostProcessExDto> aSet, int transactionId)
        {
            OperationCallResult<AppTransactionPostProcessExDto> aOperationCallResult = new OperationCallResult<AppTransactionPostProcessExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;


            aSet.FindNewItems().ForAll(o => validationResult.Merge(ProcessNewDto(o)));
            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(ProcessDeleteDto(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveProcessListByTransactionId(transactionId);
            }

            return aOperationCallResult;

        }


        private static ValidationResult ProcessNewDto(AppTransactionPostProcessExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTransactionPostProcessEntity aAppTransactionPostProcessEntity = new AppTransactionPostProcessEntity();
            AppTransactionPostProcessConverter.CopyDtoToEntity(aAppTransactionPostProcessEntity, aDto);



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionPostProcessEntity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionPostProcessEntity), "plm_AppTransactionPostProcessEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

              // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionPostProcessEntity), "plm_AppTransactionPostProcessEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

              // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionPostProcessEntity), "plm_AppTransactionPostProcessEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppTransactionPostProcessExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTransactionPostProcessEntity aAppTransactionPostProcessEntity = RetrieveOneAppTransactionPostProcessEntity(aDto.Id);

            AppTransactionPostProcessConverter.CopyDtoToEntity(aAppTransactionPostProcessEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionPostProcessEntity, false, true);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionPostProcessEntity), "plm_AppTransactionPostProcessEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


               // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionPostProcessEntity), "plm_AppTransactionPostProcessEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
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
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppTransactionPostProcessEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionPostProcessEntity), new RelationPredicateBucket(AppTransactionPostProcessFields.PostProcessId == Id));
                    adapter.Commit();
                }

               
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionPostProcessEntity), "plm_AppTransactionPostProcessEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }   




    }
}