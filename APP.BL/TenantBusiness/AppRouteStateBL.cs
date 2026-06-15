using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;

using APP.Framework;
namespace App.BL
{
    public static class AppRouteStateBL
    {
        public static EntityCollection<AppRouteStateEntity> RetrieveAllAppRouteStateEntity()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppRouteStateEntity> list = new EntityCollection<AppRouteStateEntity>();
                SortClause aSortClause = AppRouteStateFields.StateCode | SortOperator.Ascending;

                adapter.FetchEntityCollection(list, null, 0, new SortExpression(aSortClause), null);



                return list;
            }
        }

     

        public static AppRouteStateEntity RetrieveOneAppRouteStateEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppRouteStateEntity aAppRouteStateEntity = new AppRouteStateEntity(int.Parse(Id.ToString()));
                adapter.FetchEntity(aAppRouteStateEntity);
                return aAppRouteStateEntity;
            }
        }

        public static ObservableSet<AppRouteStateExDto> RetrieveAllAppRouteStateEntityDto()
        {
            ObservableSet<AppRouteStateExDto> aSet = new ObservableSet<AppRouteStateExDto>();
            EntityCollection<AppRouteStateEntity> list = RetrieveAllAppRouteStateEntity();
            foreach (var o in list)
            {
                AppRouteStateExDto aDto = AppRouteStateConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        public static AppRouteStateExDto RetrieveOneAppRouteStateExDto(object Id)
        {
            AppRouteStateEntity aAppRouteStateEntity = RetrieveOneAppRouteStateEntity(Id);
            AppRouteStateExDto aAppRouteStateExDto = AppRouteStateConverter.ConvertEntityToExDto(aAppRouteStateEntity);

          

            return aAppRouteStateExDto;
        }

        public static OperationCallResult<AppRouteStateExDto> SaveAllAppRouteStateEntityDto(ObservableSet<AppRouteStateExDto> aSet)
        {
            OperationCallResult<AppRouteStateExDto> aOperationCallResult = new OperationCallResult<AppRouteStateExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            foreach (var ItemDto in aSet)
            {
                validationResult.Merge(ItemDto.ValidateDto());     
            }

            if (validationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = validationResult;
                return aOperationCallResult;
            }

            foreach (var newItemDto in aSet.FindNewItems())
            {
                var result = ProcessNewDto(newItemDto);
                validationResult.Merge(result);
            }

            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(DeleteOneAppRouteStateEntity(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppRouteStateExDto), "App_AppRouteStateEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.ObjectList = RetrieveAllAppRouteStateEntityDto();
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppRouteStateExDto> SaveOneAppRouteStateEntityDto(AppRouteStateExDto aAppRouteStateExDto)
        {
            OperationCallResult<AppRouteStateExDto> aOperationCallResult = new OperationCallResult<AppRouteStateExDto>();

            var aValidationResult = aAppRouteStateExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (aAppRouteStateExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(aAppRouteStateExDto));
            }
            else if (aAppRouteStateExDto.IsModified)
            {
                validationResult.Merge(ProcessDirtyDto(aAppRouteStateExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppRouteStateExDto), "App_AppRouteStateEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                var entity = AppRouteStateBL.RetrieveOneAppRouteStateEntity(aAppRouteStateExDto.Id);
                aOperationCallResult.Object = AppRouteStateConverter.ConvertEntityToExDto(entity);

            }

            return aOperationCallResult;
        }

        //  RetrieveAllAppRouteStateEntityDto(aAppRouteStateExDto.Id )

        public static OperationCallResult<object> DeleteOneAppRouteStateEntityDto(object Id)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            ValidationResult avalidationResult = DeleteOneAppRouteStateEntity(Id);
            aOperationCallResult.ValidationResult = avalidationResult;
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = Id;
            }

            return aOperationCallResult;
        }

        private static ValidationResult DeleteOneAppRouteStateEntity(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppRouteStateEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppRouteStateEntity), new RelationPredicateBucket(AppRouteStateFields.RouteStateId == Id));
                    adapter.Commit();
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppRouteStateExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessNewDto(AppRouteStateExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppRouteStateEntity aParentAppRouteStateEntity = new AppRouteStateEntity();

            AppRouteStateConverter.CopyDtoToEntity(aParentAppRouteStateEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppRouteStateEntity);

                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppRouteStateExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aDto.Id = aParentAppRouteStateEntity.RouteStateId;

                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppRouteStateExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppRouteStateExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppRouteStateEntity aAppRouteStateEntity = RetrieveOneAppRouteStateEntity(aDto.Id);

            AppRouteStateConverter.CopyDtoToEntity(aAppRouteStateEntity, aDto);

            // bug test
            // aDto.Id = 100;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppRouteStateEntity, false, true);
                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppRouteStateExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppRouteStateExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }
        
        
    }
}