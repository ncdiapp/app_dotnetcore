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
    public static class AppWebApiProviderBL
    {
        public static EntityCollection<AppWebApiProviderEntity> RetrieveAllAppWebApiProviderEntity()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppWebApiProviderEntity> list = new EntityCollection<AppWebApiProviderEntity>();
                SortClause aSortClause = AppWebApiProviderFields.ProviderName | SortOperator.Ascending;

                adapter.FetchEntityCollection(list, null, 0, new SortExpression(aSortClause), null);



                return list;
            }
        }

        public static AppWebApiProviderEntity GetOneApiProviderEntity(string providerName)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppWebApiProviderEntity> list = new EntityCollection<AppWebApiProviderEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppWebApiProviderFields.ProviderName == providerName);

                adapter.FetchEntityCollection(list, filter);

                if(list.IsEmpty())
                {
                    return null;
                }
                else
                {
                    return list[0];
                }

                
            }
        }



        public static AppWebApiProviderEntity RetrieveOneAppWebApiProviderEntity(object Id)
        {
			int? reportId = ControlTypeValueConverter.ConvertValueToInt(Id); 
			if(reportId.HasValue )
			{
				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{
					AppWebApiProviderEntity aAppWebApiProviderEntity = new AppWebApiProviderEntity(reportId.Value );
					adapter.FetchEntity(aAppWebApiProviderEntity);
					return aAppWebApiProviderEntity;
				}
			}
			else
			{
				return null;
			}
          
        }

        public static ObservableSet<AppWebApiProviderExDto> RetrieveAllAppWebApiProviderEntityDto()
        {
            ObservableSet<AppWebApiProviderExDto> aSet = new ObservableSet<AppWebApiProviderExDto>();
            EntityCollection<AppWebApiProviderEntity> list = RetrieveAllAppWebApiProviderEntity();
            foreach (var o in list)
            {
                AppWebApiProviderExDto aDto = AppWebApiProviderConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        public static AppWebApiProviderExDto RetrieveOneAppWebApiProviderExDto(object Id)
        {
            AppWebApiProviderEntity aAppWebApiProviderEntity = RetrieveOneAppWebApiProviderEntity(Id);
            AppWebApiProviderExDto aAppWebApiProviderExDto = AppWebApiProviderConverter.ConvertEntityToExDto(aAppWebApiProviderEntity);

          


            return aAppWebApiProviderExDto;
        }

        public static OperationCallResult<AppWebApiProviderExDto> SaveAllAppWebApiProviderEntityDto(ObservableSet<AppWebApiProviderExDto> aSet)
        {
            OperationCallResult<AppWebApiProviderExDto> aOperationCallResult = new OperationCallResult<AppWebApiProviderExDto>();
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
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(DeleteOneAppWebApiProviderEntity(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppWebApiProviderExDto), "App_AppWebApiProviderEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.ObjectList = RetrieveAllAppWebApiProviderEntityDto();
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppWebApiProviderExDto> SaveOneAppWebApiProviderEntityDto(AppWebApiProviderExDto aAppWebApiProviderExDto)
        {
            OperationCallResult<AppWebApiProviderExDto> aOperationCallResult = new OperationCallResult<AppWebApiProviderExDto>();

            var aValidationResult = aAppWebApiProviderExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (aAppWebApiProviderExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(aAppWebApiProviderExDto));
            }
            else if (aAppWebApiProviderExDto.IsModified)
            {
                validationResult.Merge(ProcessDirtyDto(aAppWebApiProviderExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppWebApiProviderExDto), "App_AppWebApiProviderEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                var entity = AppWebApiProviderBL.RetrieveOneAppWebApiProviderEntity(aAppWebApiProviderExDto.Id);
                aOperationCallResult.Object = AppWebApiProviderConverter.ConvertEntityToExDto(entity);

            }

            return aOperationCallResult;
        }

        //  RetrieveAllAppWebApiProviderEntityDto(aAppWebApiProviderExDto.Id )

        public static OperationCallResult<object> DeleteOneAppWebApiProviderEntityDto(object Id)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            ValidationResult avalidationResult = DeleteOneAppWebApiProviderEntity(Id);
            aOperationCallResult.ValidationResult = avalidationResult;
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = Id;
            }

            return aOperationCallResult;
        }

        private static ValidationResult DeleteOneAppWebApiProviderEntity(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppWebApiProviderEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppWebApiProviderEntity), new RelationPredicateBucket(AppWebApiProviderFields.WebApiPorviderId == Id));
                    adapter.Commit();
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppWebApiProviderExDto), "App_WebApiProviderEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessNewDto(AppWebApiProviderExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppWebApiProviderEntity aParentAppWebApiProviderEntity = new AppWebApiProviderEntity();

            AppWebApiProviderConverter.CopyDtoToEntity(aParentAppWebApiProviderEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppWebApiProviderEntity);

                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppWebApiProviderExDto), "App_WebApiProviderEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aDto.Id = aParentAppWebApiProviderEntity.WebApiPorviderId;

                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppWebApiProviderExDto), "App_WebApiProviderEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppWebApiProviderExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppWebApiProviderEntity aAppWebApiProviderEntity = RetrieveOneAppWebApiProviderEntity(aDto.Id);

            AppWebApiProviderConverter.CopyDtoToEntity(aAppWebApiProviderEntity, aDto);

            // bug test
            // aDto.Id = 100;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppWebApiProviderEntity, false, true);
                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppWebApiProviderExDto), "App_WebApiProviderEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppWebApiProviderExDto), "App_WebApiProviderEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }
        
        
    }
}