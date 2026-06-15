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
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.Text.RegularExpressions;
using System.Data.Common;

using APP.Framework;
namespace App.BL
{
    public static class AppDataSetExtractViewConfigBL
    {
        public static List<AppDataSetDto> RetrieveExtractDataSetList()
        {
            List<AppDataSetDto> toReturn = new List<AppDataSetDto>();
            
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppDataSetEntity> list = new EntityCollection<AppDataSetEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppDataSetFields.BaseDataSetId != DBNull.Value);

                adapter.FetchEntityCollection(list, filter, 0);
                foreach (var dataSetEntity in list)
                {
                    toReturn.Add(AppDataSetConverter.ConvertEntityToDto(dataSetEntity));
                }

            }

            return toReturn;
        }


        public static AppDataSetEntity RetrieveOneAppDataSetEntity(object extractDataSetId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppDataSetEntity aEntity = new AppDataSetEntity(int.Parse(extractDataSetId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppDataSetEntity);

                rootPath.Add(AppDataSetEntity.PrefetchPathAppDateSetDataExtractView);

                adpater.FetchEntity(aEntity, rootPath);
                return aEntity;
            }
        }



        public static AppDataSetExDto RetrieveOneAppDataSetExDto(object extractDataSetId)
        {
            AppDataSetEntity aAppDataSetEntity = RetrieveOneAppDataSetEntity(extractDataSetId);

            AppDataSetExDto aAppDataSetDto = AppDataSetConverter.ConvertEntityToExDto(aAppDataSetEntity);


            foreach (var o in aAppDataSetEntity.AppDateSetDataExtractView)
            {
                AppDateSetDataExtractViewExDto aAppDateSetDataExtractViewExDto = AppDateSetDataExtractViewConverter.ConvertEntityToExDto(o);
                aAppDataSetDto.AppDateSetDataExtractViewList.Add(aAppDateSetDataExtractViewExDto);
            }

            foreach (var o in aAppDataSetEntity.AppDataSetParameter)
            {
                AppDataSetParameterExDto aAppDateSetDataExtractViewExDto = AppDataSetParameterConverter.ConvertEntityToExDto(o);
                aAppDataSetDto.AppDataSetParameterList.Add(aAppDateSetDataExtractViewExDto);
            }

            return aAppDataSetDto;
        }





        public static OperationCallResult<AppDataSetExDto> SaveAppDataSetExDto(AppDataSetExDto aAppDataSetExDto)
        {
            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();

            var aValidationResult = aAppDataSetExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppDataSetEntity aAppDataSetEntity;



            // prepare Data
            if (aAppDataSetExDto.IsNew)
            {
                aAppDataSetEntity = new AppDataSetEntity();
                AppDataSetConverter.CopyDtoToEntity(aAppDataSetEntity, aAppDataSetExDto);

                foreach (var dateSetDataExtractDto in aAppDataSetExDto.AppDateSetDataExtractViewList)
                {
                    AppDateSetDataExtractViewEntity aAppDateSetDataExtractViewEntity = new AppDateSetDataExtractViewEntity();
                    AppDateSetDataExtractViewConverter.CopyDtoToEntity(aAppDateSetDataExtractViewEntity, dateSetDataExtractDto);
                    aAppDataSetEntity.AppDateSetDataExtractView.Add(aAppDateSetDataExtractViewEntity);
                }
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppDataSetEntity);
                        adapter.Commit();

                        aAppDataSetExDto.Id = aAppDataSetEntity.DataSetId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "app_DateSetDataExtractViewEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }


                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "app_DateSetDataExtractViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppDataSetExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppDataSetExDto(aAppDataSetExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppDataSetExDto(aAppDataSetExDto.Id);
            }

            return aOperationCallResult;
        }

        private static ValidationResult ProcessDirtyAppDataSetExDto(AppDataSetExDto aAppDataSetExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            // int[] deleteAppDateSetDataExtractViewIDs = aAppDataSetExDto.AppDateSetDataExtractViewList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppDataSetEntity aAppDataSetEntity = RetrieveOneAppDataSetEntity(aAppDataSetExDto.Id);

            Dictionary<int, AppDateSetDataExtractViewEntity> dictAppDateSetDataExtractViewFromDbms = aAppDataSetEntity.AppDateSetDataExtractView.ToDictionary(o => o.ExtractViewId, o => o);

            AppDataSetConverter.CopyDtoToEntity(aAppDataSetEntity, aAppDataSetExDto);

            // new Items
            foreach (AppDateSetDataExtractViewDto aChildDto in aAppDataSetExDto.AppDateSetDataExtractViewList.FindNewItems())
            {
                AppDateSetDataExtractViewEntity aNewChildEntity = new AppDateSetDataExtractViewEntity();
                AppDateSetDataExtractViewConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppDataSetEntity.AppDateSetDataExtractView.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aAppDataSetExDto.AppDateSetDataExtractViewList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppDateSetDataExtractViewFromDbms.ContainsKey(dtoKey))
                {
                    AppDateSetDataExtractViewConverter.CopyDtoToEntity(dictAppDateSetDataExtractViewFromDbms[dtoKey], modifyitem);
                }
            }

            // deletedIDs
            int[] deleteAppDateSetDataExtractViewIDs = aAppDataSetExDto.AppDateSetDataExtractViewList.FindDeletedItemIds().Cast<int>().ToArray();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppDataSetEntity);

                    // Need to delete SearchTemplate subitems
                    if (deleteAppDateSetDataExtractViewIDs.Count() > 0)
                    {

                        adapter.DeleteEntitiesDirectly(typeof(AppDateSetDataExtractViewEntity), new RelationPredicateBucket(AppDateSetDataExtractViewFields.ExtractViewId == deleteAppDateSetDataExtractViewIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetEntity), "app_DateSetDataExtractViewEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetEntity), "app_DateSetDataExtractViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }




        //Delete a AppDataSet
        public static OperationCallResult<object> DeleteAppDataSet(object dataSetId)
        {
            OperationCallResult<object> aValidationResult = new OperationCallResult<object>();

            string referMsg = string.Empty;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    adapter.DeleteEntitiesDirectly(typeof(AppDateSetDataExtractViewEntity), new RelationPredicateBucket(AppDateSetDataExtractViewFields.DataSetId == dataSetId));

                    adapter.DeleteEntitiesDirectly(typeof(AppDataSetEntity), new RelationPredicateBucket(AppDataSetFields.DataSetId == dataSetId));
                    adapter.Commit();
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetEntity), "app_DateSetDataExtractViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }

                // if no any errors
                if (!aValidationResult.ValidationResult.HasErrors)
                {
                    aValidationResult.Object = dataSetId;
                }
            }
            return aValidationResult;
        }








    }

}