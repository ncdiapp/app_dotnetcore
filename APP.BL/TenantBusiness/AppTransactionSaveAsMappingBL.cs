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
    public static class AppTransactionSaveAsMappingBL
    {

        public static ObservableSet<AppTransactionSaveAsMappingExDto> RetrieveAllAppTransactionSaveAsMappingListByTransactionId(object transactionId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionSaveAsMappingEntity> entityList = new EntityCollection<AppTransactionSaveAsMappingEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionSaveAsMappingFields.TransactionId == transactionId);

                adapter.FetchEntityCollection(entityList, filter);


                var aDtoList = new ObservableSet<AppTransactionSaveAsMappingExDto>();
                foreach (var AppTransactionSaveAsMappingEntity in entityList)
                {
                    AppTransactionSaveAsMappingExDto aAppTransactionSaveAsMappingExDto = AppTransactionSaveAsMappingConverter.ConvertEntityToExDto(AppTransactionSaveAsMappingEntity);


                    aDtoList.Add(aAppTransactionSaveAsMappingExDto);

                }
                return aDtoList;
            }
        }

        public static AppTransactionSaveAsMappingEntity RetrieveOneAppTransactionSaveAsMappingEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionSaveAsMappingEntity aAppTransactionSaveAsMappingEntity = new AppTransactionSaveAsMappingEntity(int.Parse(Id.ToString()));
                adapter.FetchEntity(aAppTransactionSaveAsMappingEntity);
                return aAppTransactionSaveAsMappingEntity;
            }
        }

        public static OperationCallResult<AppTransactionSaveAsMappingExDto> SaveAllAppTransactionSaveAsMappingExDto(ObservableSet<AppTransactionSaveAsMappingExDto> aSet, int transactionId)
        {
            OperationCallResult<AppTransactionSaveAsMappingExDto> aOperationCallResult = new OperationCallResult<AppTransactionSaveAsMappingExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;


            aSet.FindNewItems().ForAll(o => validationResult.Merge(ProcessNewDto(o)));
            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(ProcessDeleteDto(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveAllAppTransactionSaveAsMappingListByTransactionId(transactionId);
            }

            return aOperationCallResult;

        }


        private static ValidationResult ProcessNewDto(AppTransactionSaveAsMappingExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTransactionSaveAsMappingEntity aAppTransactionSaveAsMappingEntity = new AppTransactionSaveAsMappingEntity();
            AppTransactionSaveAsMappingConverter.CopyDtoToEntity(aAppTransactionSaveAsMappingEntity, aDto);



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionSaveAsMappingEntity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionSaveAsMappingEntity), "plm_AppTransactionSaveAsMappingEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

              // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionSaveAsMappingEntity), "plm_AppTransactionSaveAsMappingEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

              // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionSaveAsMappingEntity), "plm_AppTransactionSaveAsMappingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppTransactionSaveAsMappingExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTransactionSaveAsMappingEntity aAppTransactionSaveAsMappingEntity = RetrieveOneAppTransactionSaveAsMappingEntity(aDto.Id);

            AppTransactionSaveAsMappingConverter.CopyDtoToEntity(aAppTransactionSaveAsMappingEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionSaveAsMappingEntity, false, true);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionSaveAsMappingEntity), "plm_AppTransactionSaveAsMappingEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }

            

               // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionSaveAsMappingEntity), "plm_AppTransactionSaveAsMappingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
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
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppTransactionSaveAsMappingEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionSaveAsMappingEntity), new RelationPredicateBucket(AppTransactionSaveAsMappingFields.MappingId == Id));
                    adapter.Commit();
                }

              

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionSaveAsMappingEntity), "plm_AppTransactionSaveAsMappingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }   




    }
}