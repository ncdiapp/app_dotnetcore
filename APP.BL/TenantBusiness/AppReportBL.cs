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
    public static class AppReportBL
    {
        public static EntityCollection<AppReportEntity> RetrieveAllAppReportEntity()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppReportEntity> list = new EntityCollection<AppReportEntity>();
                SortClause aSortClause = AppReportFields.ReportName | SortOperator.Ascending;
                adapter.FetchEntityCollection(list, null, 0, new SortExpression(aSortClause), null);
                return list;
            }
        }

     

        public static AppReportEntity RetrieveOneAppReportEntity(object Id)
        {
			int? reportId = ControlTypeValueConverter.ConvertValueToInt(Id); 
			if(reportId.HasValue )
			{
				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{
					AppReportEntity aAppReportEntity = new AppReportEntity(reportId.Value );
					adapter.FetchEntity(aAppReportEntity);
					return aAppReportEntity;
				}
			}
			else
			{
				return null;
			}
          
        }

        public static ObservableSet<AppReportExDto> RetrieveAllAppReportEntityDto()
        {
            ObservableSet<AppReportExDto> aSet = new ObservableSet<AppReportExDto>();
            EntityCollection<AppReportEntity> list = RetrieveAllAppReportEntity();
            foreach (var o in list)
            {
                AppReportExDto aDto = AppReportConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        public static AppReportExDto RetrieveOneAppReportExDto(object Id)
        {
            AppReportEntity aAppReportEntity = RetrieveOneAppReportEntity(Id);
            AppReportExDto aAppReportExDto = AppReportConverter.ConvertEntityToExDto(aAppReportEntity);

            if (aAppReportExDto?.Id != null)
                aAppReportExDto.ReportTemplate = AppReportTemplateBL.GetByReportId((int)aAppReportExDto.Id);

            return aAppReportExDto;
        }

        public static OperationCallResult<AppReportExDto> SaveAllAppReportEntityDto(ObservableSet<AppReportExDto> aSet)
        {
            OperationCallResult<AppReportExDto> aOperationCallResult = new OperationCallResult<AppReportExDto>();
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
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(DeleteOneAppReportEntity(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppReportExDto), "App_AppReportEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.ObjectList = RetrieveAllAppReportEntityDto();
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppReportExDto> SaveOneAppReportEntityDto(AppReportExDto aAppReportExDto)
        {
            OperationCallResult<AppReportExDto> aOperationCallResult = new OperationCallResult<AppReportExDto>();

            var aValidationResult = aAppReportExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (aAppReportExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(aAppReportExDto));
            }
            else if (aAppReportExDto.IsModified)
            {
                validationResult.Merge(ProcessDirtyDto(aAppReportExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppReportExDto), "App_AppReportEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                var entity = AppReportBL.RetrieveOneAppReportEntity(aAppReportExDto.Id);
                aOperationCallResult.Object = AppReportConverter.ConvertEntityToExDto(entity);

                if (aAppReportExDto.ReportTemplate != null)
                {
                    aAppReportExDto.ReportTemplate.ReportId = (int)aAppReportExDto.Id;
                    AppReportTemplateBL.Save(aAppReportExDto.ReportTemplate);
                    aOperationCallResult.Object.ReportTemplate = AppReportTemplateBL.GetByReportId((int)aAppReportExDto.Id);
                }
            }

            return aOperationCallResult;
        }

        //  RetrieveAllAppReportEntityDto(aAppReportExDto.Id )

        public static OperationCallResult<object> DeleteOneAppReportEntityDto(object Id)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            ValidationResult avalidationResult = DeleteOneAppReportEntity(Id);
            aOperationCallResult.ValidationResult = avalidationResult;
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = Id;
            }

            return aOperationCallResult;
        }

        private static ValidationResult DeleteOneAppReportEntity(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppReportEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppReportEntity), new RelationPredicateBucket(AppReportFields.ReportId == Id));
                    adapter.Commit();
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppReportExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessNewDto(AppReportExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppReportEntity aParentAppReportEntity = new AppReportEntity();

            AppReportConverter.CopyDtoToEntity(aParentAppReportEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppReportEntity);

                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppReportExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aDto.Id = aParentAppReportEntity.ReportId;

                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppReportExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppReportExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppReportEntity aAppReportEntity = RetrieveOneAppReportEntity(aDto.Id);

            AppReportConverter.CopyDtoToEntity(aAppReportEntity, aDto);

            // bug test
            // aDto.Id = 100;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppReportEntity, false, true);
                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppReportExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppReportExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }
        
        
    }
}