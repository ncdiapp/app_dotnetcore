using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System;

using APP.Framework;
namespace App.BL
{
    public static class AppWebApiConfigBL
    {
        public static readonly string App_WebApiConfigEntity_Save_OK = "App_WebApiConfigEntity_Save_OK";
        public static readonly string App_WebApiConfigEntity_Save_Failed = "App_WebApiConfigEntity_Save_Failed";
        public static readonly string App_WebApiConfigEntityUILayout_Save_OK = "App_WebApiConfigEntityUILayout_Save_OK";
        public static readonly string App_WebApiConfigEntityUILayout_Save_Failed = "App_WebApiConfigEntityUILayout_Save_Failed";
        public static readonly string App_WebApiConfigEntity_Delete_Ok = "App_WebApiConfigEntity_Delete_Ok";

       

        public static readonly string App_WebApiConfigEntity_Delete_Failed = "App_WebApiConfigEntity_Delete_Failed";

        public static ObservableSet<AppWebApiConfigDto> RetrieveAllAppWebApiConfigDto()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppWebApiConfigEntity> list = new EntityCollection<AppWebApiConfigEntity>();

                SortExpression expression = new SortExpression(AppWebApiConfigFields.WebApiName | SortOperator.Ascending);
                adapter.FetchEntityCollection(list, null, 0, expression);

                var aDtoList = new ObservableSet<AppWebApiConfigDto>();

                foreach (var o in list)
                {
                    aDtoList.Add(AppWebApiConfigConverter.ConvertEntityToDto(o));
                }

                return aDtoList;
            }
        }

        public static List<AppWebApiConfigDto> RetrieveAppWebApiConfigDtoList(int? providerId)
        {
            List<AppWebApiConfigDto> allAppWebApiConfigDtoList = RetrieveAllAppWebApiConfigDto().ToList();

            if (providerId.HasValue)
            {
                return allAppWebApiConfigDtoList.Where(o => o.WebApiProviderId.HasValue && o.WebApiProviderId.Value == providerId.Value).ToList();
            }
            else
            {
                return allAppWebApiConfigDtoList;
            }
        }

        public static AppWebApiConfigExDto RetrieveOneAppWebApiConfigExDto(object configId)
        {
            AppWebApiConfigEntity aAppWebApiConfigEntity = RetrieveOneAppWebApiConfigEntity(configId);
            AppWebApiConfigExDto aAppWebApiConfigExDto = AppWebApiConfigConverter.ConvertEntityToExDto(aAppWebApiConfigEntity);

            Dictionary<int, AppListMenuExDto> dictListMenuIdAndDto = new Dictionary<int, AppListMenuExDto>();

            var menuList = AppSecurityManagementBL.RetrieveUserMenuFlatList();
            foreach (var menu in menuList)
            {
                dictListMenuIdAndDto.Add((int)menu.Id, menu);
            }


            foreach (var o in aAppWebApiConfigEntity.AppWebApiParamsHeaderSettig)
            {
                AppWebApiParamsHeaderSettigExDto aAppWebApiConfigKeyExDto = AppWebApiParamsHeaderSettigConverter.ConvertEntityToExDto(o);

               
                aAppWebApiConfigExDto.AppWebApiParamsHeaderSettigList.Add(aAppWebApiConfigKeyExDto);
            }


            return aAppWebApiConfigExDto;
        }


        public static AppWebApiConfigEntity RetrieveOneAppWebApiConfigEntity(object webConfigId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppWebApiConfigEntity WebApiConfigEntity = new AppWebApiConfigEntity(int.Parse(webConfigId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppWebApiConfigEntity);




                rootPath.Add(AppWebApiConfigEntity.PrefetchPathAppWebApiParamsHeaderSettig);

                adpater.FetchEntity(WebApiConfigEntity, rootPath);
                return WebApiConfigEntity;
            }
        }

        public static OperationCallResult<AppWebApiConfigExDto> SaveAppWebApiConfigExDto(AppWebApiConfigExDto aAppWebApiConfigExDto)
        {
            OperationCallResult<AppWebApiConfigExDto> aOperationCallResult = new OperationCallResult<AppWebApiConfigExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppWebApiConfigEntity aAppWebApiConfigEntity;

            // prepare Data
            if (aAppWebApiConfigExDto.IsNew)
            {
                aAppWebApiConfigEntity = new AppWebApiConfigEntity();
                AppWebApiConfigConverter.CopyDtoToEntity(aAppWebApiConfigEntity, aAppWebApiConfigExDto);



                foreach (var templatefieldDto in aAppWebApiConfigExDto.AppWebApiParamsHeaderSettigList)
                {
                    AppWebApiParamsHeaderSettigEntity aAppWebApiParamsHeaderSettigEntity = new AppWebApiParamsHeaderSettigEntity();
                    AppWebApiParamsHeaderSettigConverter.CopyDtoToEntity(aAppWebApiParamsHeaderSettigEntity, templatefieldDto);
                    aAppWebApiConfigEntity.AppWebApiParamsHeaderSettig.Add(aAppWebApiParamsHeaderSettigEntity);
                }
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppWebApiConfigEntity);
                        adapter.Commit();

                        aAppWebApiConfigExDto.Id = aAppWebApiConfigEntity.WebApiConfigId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppWebApiConfigEntity), "App_WebApiConfigEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppWebApiConfigEntity), "App_WebApiConfigEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppWebApiConfigEntity), "App_WebApiConfigEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppWebApiConfigExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppWebApiConfigExDto(aAppWebApiConfigExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppWebApiConfigExDto(aAppWebApiConfigExDto.Id);
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<object> DeleteOneAppWebApiConfig(object webApiConfigId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppWebApiParamsHeaderSettigEntity), new RelationPredicateBucket(AppWebApiParamsHeaderSettigFields.WebApiConfigId == webApiConfigId));
                    adapter.DeleteEntitiesDirectly(typeof(AppWebApiConfigEntity), new RelationPredicateBucket(AppWebApiConfigFields.WebApiConfigId == webApiConfigId));
                    string message = StringLocalizer.Localize(App_WebApiConfigEntity_Delete_Ok, "Desktop Delete Successful");
                    aValidationResult.Items.Add(new ValidationItem(null, App_WebApiConfigEntity_Delete_Ok, ValidationItemType.Message, message));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize(App_WebApiConfigEntity_Delete_Failed, "Desktop Delete Failed" + ex.ToString());
                    aValidationResult.Items.Add(new ValidationItem(null, App_WebApiConfigEntity_Delete_Failed, ValidationItemType.Error, message));
                }
            }

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = webApiConfigId;
            }

            return aOperationCallResult;
        }

        private static ValidationResult ProcessDirtyAppWebApiConfigExDto(AppWebApiConfigExDto aAppWebApiConfigExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            int[] dirtyFieldIds = aAppWebApiConfigExDto.AppWebApiParamsHeaderSettigList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppWebApiConfigEntity aAppWebApiConfigEntity = RetrieveOneAppWebApiConfigEntity(aAppWebApiConfigExDto.Id);

            Dictionary<int, AppWebApiParamsHeaderSettigEntity> dictAppWebApiParamsHeaderSettigFromDbms = aAppWebApiConfigEntity.AppWebApiParamsHeaderSettig.ToDictionary(o => o.ParamHeaderId, o => o);

            AppWebApiConfigConverter.CopyDtoToEntity(aAppWebApiConfigEntity, aAppWebApiConfigExDto);
            //  aAppWebApiConfigEntity.ModifiedDate = System.DateTime.UtcNow;
            //  aAppWebApiConfigEntity.ModifiedBy = (int)ServerContext.Instance.CurrentUid;

            //------- check  AppWebApiParamsHeaderSettig

            // new Items
            foreach (var aChildDto in aAppWebApiConfigExDto.AppWebApiParamsHeaderSettigList.FindNewItems())
            {
                AppWebApiParamsHeaderSettigEntity aNewChildEntity = new AppWebApiParamsHeaderSettigEntity();
                AppWebApiParamsHeaderSettigConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppWebApiConfigEntity.AppWebApiParamsHeaderSettig.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aAppWebApiConfigExDto.AppWebApiParamsHeaderSettigList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppWebApiParamsHeaderSettigFromDbms.ContainsKey(dtoKey))
                {
                    AppWebApiParamsHeaderSettigConverter.CopyDtoToEntity(dictAppWebApiParamsHeaderSettigFromDbms[dtoKey], modifyitem);
                }
            }

            // deletedIDs
            int[] deleteFieldIDs = aAppWebApiConfigExDto.AppWebApiParamsHeaderSettigList.FindDeletedItemIds().Cast<int>().ToArray();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppWebApiConfigEntity);

                    // Need to delete AppWebApiParamsHeaderSettigFields
                    if (deleteFieldIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppWebApiParamsHeaderSettigEntity), new RelationPredicateBucket(AppWebApiParamsHeaderSettigFields.ParamHeaderId == deleteFieldIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppWebApiConfigEntity), "App_WebApiConfigEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

              

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppWebApiConfigEntity), "App_WebApiConfigEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

    }
}