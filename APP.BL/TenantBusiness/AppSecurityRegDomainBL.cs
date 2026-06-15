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
    public static class AppSecurityRegDomainBL
    {

      //  public static readonly List<int> EntityInfoIds = GetDomainLinkInfoEntity();
        //static AppSecurityRegDomainBL()
        //{ 
          
        //}
        public static EntityCollection<AppSecurityRegDomainEntity> RetrieveAllAppSecurityRegDomainEntity()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecurityRegDomainEntity> list = new EntityCollection<AppSecurityRegDomainEntity>();
                SortClause aSortClause = AppSecurityRegDomainFields.DomainCode | SortOperator.Ascending;

                adapter.FetchEntityCollection(list, null, 0, new SortExpression(aSortClause), null);

                return list;
            }
        }

        public static AppSecurityRegDomainEntity GetOrganizationDomainEntity() {
            return RetrieveAllAppSecurityRegDomainEntity()
				.FirstOrDefault(o=>o.DomainType.HasValue && o.DomainType.Value == (int)EmAppUserType.Employee);
        }

        //private static List<int> GetDomainLinkInfoEntity()
        //{

        //    return RetrieveAllAppSecurityRegDomainEntity().Where(o => o.EntityInfoId.HasValue).Select(o => o.EntityInfoId.Value).ToList();   
           
        //}



        public static AppSecurityRegDomainEntity RetrieveOneAppSecurityRegDomainEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSecurityRegDomainEntity aAppSecurityRegDomainEntity = new AppSecurityRegDomainEntity(int.Parse(Id.ToString()));
                adapter.FetchEntity(aAppSecurityRegDomainEntity);
                return aAppSecurityRegDomainEntity;
            }
        }

        public static ObservableSet<AppSecurityRegDomainExDto> RetrieveAllAppSecurityRegDomainEntityDto()
        {
            ObservableSet<AppSecurityRegDomainExDto> aSet = new ObservableSet<AppSecurityRegDomainExDto>();
            EntityCollection<AppSecurityRegDomainEntity> list = RetrieveAllAppSecurityRegDomainEntity();
            foreach (var o in list.OrderBy(p => p.DomainType).ThenBy(q => q.DomainCode))
            {
                AppSecurityRegDomainExDto aDto = AppSecurityRegDomainConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        public static AppSecurityRegDomainExDto RetrieveOneAppSecurityRegDomainExDto(object Id)
        {
            AppSecurityRegDomainEntity aAppSecurityRegDomainEntity = RetrieveOneAppSecurityRegDomainEntity(Id);
            AppSecurityRegDomainExDto aAppSecurityRegDomainExDto = AppSecurityRegDomainConverter.ConvertEntityToExDto(aAppSecurityRegDomainEntity);

            //if (aAppSecurityRegDomainExDto.EntityInfoId.HasValue) 
            //{
            //    int entityInfoId = aAppSecurityRegDomainExDto.EntityInfoId.Value;
            //    var entityDto = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(entityInfoId);
            //    if (entityDto != null)
            //    {
            //        entityDto.EntityDataList = AppEntityInfoBL.GetLookupItemList(entityInfoId, string.Empty);
            //        aAppSecurityRegDomainExDto.ForeignAppEntityInfoExDto = entityDto;
            //    }
            //}

            return aAppSecurityRegDomainExDto;
        }

        public static OperationCallResult<AppSecurityRegDomainExDto> SaveAllAppSecurityRegDomainEntityDto(ObservableSet<AppSecurityRegDomainExDto> aSet)
        {
            OperationCallResult<AppSecurityRegDomainExDto> aOperationCallResult = new OperationCallResult<AppSecurityRegDomainExDto>();
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
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(DeleteOneAppSecurityRegDomainEntity(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_AppSecurityRegDomainEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.ObjectList = RetrieveAllAppSecurityRegDomainEntityDto();
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppSecurityRegDomainExDto> SaveOneAppSecurityRegDomainEntityDto(AppSecurityRegDomainExDto aAppSecurityRegDomainExDto)
        {
            OperationCallResult<AppSecurityRegDomainExDto> aOperationCallResult = new OperationCallResult<AppSecurityRegDomainExDto>();

            var aValidationResult = aAppSecurityRegDomainExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

           



            if (aAppSecurityRegDomainExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(aAppSecurityRegDomainExDto));
            }
            else if (aAppSecurityRegDomainExDto.IsModified)
            {
                validationResult.Merge(ProcessDirtyDto(aAppSecurityRegDomainExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_AppSecurityRegDomainEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                var entity = AppSecurityRegDomainBL.RetrieveOneAppSecurityRegDomainEntity(aAppSecurityRegDomainExDto.Id);
                aOperationCallResult.Object = AppSecurityRegDomainConverter.ConvertEntityToExDto(entity);

            }

            return aOperationCallResult;
        }

        //  RetrieveAllAppSecurityRegDomainEntityDto(aAppSecurityRegDomainExDto.Id )

        public static OperationCallResult<object> DeleteOneAppSecurityRegDomainEntityDto(object Id)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            ValidationResult avalidationResult = DeleteOneAppSecurityRegDomainEntity(Id);
            aOperationCallResult.ValidationResult = avalidationResult;
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = Id;
            }

            return aOperationCallResult;
        }

        private static ValidationResult DeleteOneAppSecurityRegDomainEntity(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppSecurityRegDomainEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityRegDomainEntity), new RelationPredicateBucket(AppSecurityRegDomainFields.DomainId == Id));
                    adapter.Commit();
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessNewDto(AppSecurityRegDomainExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppSecurityRegDomainEntity aParentAppSecurityRegDomainEntity = new AppSecurityRegDomainEntity();

            AppSecurityRegDomainConverter.CopyDtoToEntity(aParentAppSecurityRegDomainEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppSecurityRegDomainEntity);

                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aDto.Id = aParentAppSecurityRegDomainEntity.DomainId;

                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppSecurityRegDomainExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppSecurityRegDomainEntity aAppSecurityRegDomainEntity = RetrieveOneAppSecurityRegDomainEntity(aDto.Id);

            AppSecurityRegDomainConverter.CopyDtoToEntity(aAppSecurityRegDomainEntity, aDto);

            // bug test
            // aDto.Id = 100;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSecurityRegDomainEntity, false, true);
                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }
        
        
    }
}