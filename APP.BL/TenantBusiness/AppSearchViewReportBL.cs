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
    public static class AppSearchViewReportBL
	{

        public static ObservableSet<AppSearchViewReportExDto> RetrieveAllAppSearchViewReportListBySearchViewId(object SearchViewId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSearchViewReportEntity> entityList = new EntityCollection<AppSearchViewReportEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppSearchViewReportFields.SearchViewId == SearchViewId);

                adapter.FetchEntityCollection(entityList, filter);


                var aDtoList = new ObservableSet<AppSearchViewReportExDto>();
                foreach (var AppSearchViewReportEntity in entityList)
                {
                    AppSearchViewReportExDto aAppSearchViewReportExDto = AppSearchViewReportConverter.ConvertEntityToExDto(AppSearchViewReportEntity);


                    aDtoList.Add(aAppSearchViewReportExDto);

                }
                return aDtoList;
            }
        }



        public static AppSearchViewReportEntity RetrieveOneAppSearchViewReportEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSearchViewReportEntity aAppSearchViewReportEntity = new AppSearchViewReportEntity(int.Parse(Id.ToString()));
                adapter.FetchEntity(aAppSearchViewReportEntity);
                return aAppSearchViewReportEntity;
            }
        }

        public static OperationCallResult<AppSearchViewReportExDto> SaveAllAppSearchViewReportExDto(ObservableSet<AppSearchViewReportExDto> aSet, int SearchViewId)
        {
            OperationCallResult<AppSearchViewReportExDto> aOperationCallResult = new OperationCallResult<AppSearchViewReportExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;


            aSet.FindNewItems().ForAll(o => validationResult.Merge(ProcessNewDto(o)));
            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(ProcessDeleteDto(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveAllAppSearchViewReportListBySearchViewId(SearchViewId);
            }

            return aOperationCallResult;

        }


        private static ValidationResult ProcessNewDto(AppSearchViewReportExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppSearchViewReportEntity aAppSearchViewReportEntity = new AppSearchViewReportEntity();
            AppSearchViewReportConverter.CopyDtoToEntity(aAppSearchViewReportEntity, aDto);



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSearchViewReportEntity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewReportEntity), "plm_AppSearchViewReportEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

              // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewReportEntity), "plm_AppSearchViewReportEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

              // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewReportEntity), "plm_AppSearchViewReportEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppSearchViewReportExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppSearchViewReportEntity aAppSearchViewReportEntity = RetrieveOneAppSearchViewReportEntity(aDto.Id);

            AppSearchViewReportConverter.CopyDtoToEntity(aAppSearchViewReportEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSearchViewReportEntity, false, true);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewReportEntity), "plm_AppSearchViewReportEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


               // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewReportEntity), "plm_AppSearchViewReportEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
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
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppSearchViewReportEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppSearchViewReportEntity), new RelationPredicateBucket(AppSearchViewReportFields.SearchViewReportId == Id));
                    adapter.Commit();
                }

         

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewReportEntity), "plm_AppSearchViewReportEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }   





    }

   
}