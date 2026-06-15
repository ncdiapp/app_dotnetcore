using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.IO;
//
//using APP.Persistence.Common;
using NewLookExchange;
using APP.Framework.Globalization;

using APP.Framework;
namespace App.BL
{
    public static class AppLanguageBL
    {
        public const string Label_LanguageKey = "LanguageKey";
        public const string Label_DefaultValue = "Default Value";
        public const string Label_Value = "Value";


        public static AppLanguageEntity RetrieveOneAppLanguageEntity(object languageId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetContextAdapter())
            {
                AppLanguageEntity userEntity = new AppLanguageEntity(int.Parse(languageId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppLanguageEntity);

                rootPath.Add(AppLanguageEntity.PrefetchPathAppLanguageKey);

                adpater.FetchEntity(userEntity, rootPath);
                return userEntity;
            }
        }


        public static AppLanguageEntity RetrieveOneAppLanguageEntityWithSelectedLanguageKey(object languageId, object[] selectedLanguangeKeyIds)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetContextAdapter())
            {
                AppLanguageEntity userEntity = new AppLanguageEntity(int.Parse(languageId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppLanguageEntity);
                rootPath.Add(AppLanguageEntity.PrefetchPathAppLanguageKey).Filter.Add(AppLanguageKeyFields.LanguageKeyId == selectedLanguangeKeyIds); ;

                adpater.FetchEntity(userEntity, rootPath);
                return userEntity;
            }
        }

        public static AppLanguageKeyEntity RetrieveOneAppLanguageKeyEntity(object languagekeyId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetContextAdapter())
            {
                AppLanguageKeyEntity userEntity = new AppLanguageKeyEntity(int.Parse(languagekeyId.ToString()));

                adpater.FetchEntity(userEntity);
                return userEntity;
            }
        }

        public static OperationCallResult<bool> MarkLanguageAsDefault(object languagekeyId)
        {
            OperationCallResult<bool> aOpR = new OperationCallResult<bool>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
            {
                try
                {
                    AppLanguageEntity aEntity = new AppLanguageEntity();
                    aEntity.IsDefault = false;

                    adapter.UpdateEntitiesDirectly(aEntity, new RelationPredicateBucket(AppLanguageFields.LanguageId != languagekeyId));

                    aEntity.IsDefault = true;
                    adapter.UpdateEntitiesDirectly(aEntity, new RelationPredicateBucket(AppLanguageFields.LanguageId == languagekeyId));

                    aOpR.Object = true;
                }
                catch
                {
                    aOpR.Object = false;
                }
            }

            return aOpR;
        }

        public static AppLanguageEntity RetrieveDefaultAppLanguageEntity()
        {
            // During login / session-restore, identity is not registered yet so
            // GetContextAdapter() would throw.  Fall back to AppMasterDB (which has
            // the default language seeded) and let the normal path run once logged in.
            DataAccessAdapter adapter;
            try
            {
                adapter = AppTenantAdapterBL.GetContextAdapter();
            }
            catch (InvalidOperationException)
            {
                adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString);
            }

            AppLanguageEntity aAppLanguageEntity = null;
            using (adapter)
            {
                RelationPredicateBucket aFilter = new RelationPredicateBucket(AppLanguageFields.IsDefault == true);
                IPrefetchPath2 apath = new PrefetchPath2(EntityType.AppLanguageEntity);
                apath.Add(AppLanguageEntity.PrefetchPathAppLanguageKey);

                EntityCollection<AppLanguageEntity> list = new EntityCollection<AppLanguageEntity>();
                adapter.FetchEntityCollection(list, aFilter, apath);
                if (list.Count > 0)
                {
                    aAppLanguageEntity = list[0];
                }
            }

            return aAppLanguageEntity;
        }

        public static AppLanguageExDto RetrieveOneAppLanguageExDto(object languageId)
        {
            AppLanguageEntity aAppLanguageEntity = RetrieveOneAppLanguageEntity(languageId);
            AppLanguageExDto aLanguageDto = AppLanguageConverter.ConvertEntityToExDto(aAppLanguageEntity);

            foreach (var o in aAppLanguageEntity.AppLanguageKey)
            {
                AppLanguageKeyExDto aAppLanguageKeyExDto = AppLanguageKeyConverter.ConvertEntityToExDto(o);
                aLanguageDto.AppLanguageKeyList.Add(aAppLanguageKeyExDto);
            }

            return aLanguageDto;
        }

        public static AppLanguageKeyDto RetrieveOneAppLanguageKeyExDto(object languageKeyId)
        {
            AppLanguageKeyEntity aAppLanguageKeyEntity = RetrieveOneAppLanguageKeyEntity(languageKeyId);
            AppLanguageKeyDto aLanguageKeyDto = AppLanguageKeyConverter.ConvertEntityToDto(aAppLanguageKeyEntity);

            return aLanguageKeyDto;
        }

        public static AppLanguageExDto RetrieveDefaultAppLanguageExDto()
        {
            AppLanguageEntity aAppLanguageEntity = RetrieveDefaultAppLanguageEntity();
            if (aAppLanguageEntity != null)
            {
                AppLanguageExDto aLanguageDto = AppLanguageConverter.ConvertEntityToExDto(aAppLanguageEntity);
                foreach (var o in aAppLanguageEntity.AppLanguageKey)
                {
                    AppLanguageKeyExDto aAppLanguageKeyExDto = AppLanguageKeyConverter.ConvertEntityToExDto(o);
                    aLanguageDto.AppLanguageKeyList.Add(aAppLanguageKeyExDto);
                }

                return aLanguageDto;
            }

            return null;
        }

        public static ObservableSet<AppLanguageDto> RetrieveAllAppLanguageDto()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
            {
                EntityCollection<AppLanguageEntity> list = new EntityCollection<AppLanguageEntity>();
                adapter.FetchEntityCollection(list, null);

                var aDtoList = new ObservableSet<AppLanguageDto>();
                foreach (var o in list)
                {
                    aDtoList.Add(AppLanguageConverter.ConvertEntityToDto(o));
                }

                return aDtoList;
            }
        }

        public static List<AppLanguageKeyDto> RetrieveAllModuleLanguageKeys(object languageId, string moduleNameLike, int pageSize, int pageIndex)
        {
            List<AppLanguageKeyDto> result = new List<AppLanguageKeyDto>();
            EntityCollection<AppLanguageKeyEntity> aList = new EntityCollection<AppLanguageKeyEntity>();


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
            {
                //new FieldLikePredicate(CustomerFields.CompanyName, "Solution%"));

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppLanguageKeyFields.LanguageId == languageId);

                // if moduleNameLike is empty, load all modules' all languagekeys
                if (!string.IsNullOrEmpty(moduleNameLike))
                {
                    string resourceKeyLike = moduleNameLike + "%";
                    filter.PredicateExpression.AddWithAnd(AppLanguageKeyFields.ResourceKey % resourceKeyLike);
                }

                pageIndex = pageIndex + 1;
                adapter.FetchEntityCollection(aList, filter, 0, new SortExpression(AppLanguageKeyFields.LanguageKeyId | SortOperator.Ascending), pageIndex, pageSize);
            }

            foreach (var entity in aList)
            {
                result.Add(AppLanguageKeyConverter.ConvertEntityToDto(entity));
            }

            return result;
        }

        public static ObservableSet<AppLanguageKeyDto> RetrieveAllAppLanguageKeyDto(object languageId, int pageSize, int pageIndex)
        {
            ObservableSet<AppLanguageKeyDto> result = new ObservableSet<AppLanguageKeyDto>();
            EntityCollection<AppLanguageKeyEntity> aList = new EntityCollection<AppLanguageKeyEntity>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
            {
                IRelationPredicateBucket filter = new RelationPredicateBucket(AppLanguageKeyFields.LanguageId == languageId);
                pageIndex = pageIndex + 1;
                adapter.FetchEntityCollection(aList, filter, 0, new SortExpression(AppLanguageKeyFields.LanguageKeyId | SortOperator.Ascending), pageIndex, pageSize);
            }

            foreach (var entity in aList)
            {
                result.Add(AppLanguageKeyConverter.ConvertEntityToDto(entity));
            }

            return result;
        }

        public static OperationCallResult<AppLanguageExDto> SaveAppLanguageExDto(AppLanguageExDto aAppLanguageExDto)
        {
            OperationCallResult<AppLanguageExDto> aOperationCallResult = new OperationCallResult<AppLanguageExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;
            int? languageId = null;

            // prepare Data
            if (aAppLanguageExDto.IsNew)
            {
                AppLanguageEntity aAppLanguageEntity = new AppLanguageEntity();
                AppLanguageConverter.CopyDtoToEntity(aAppLanguageEntity, aAppLanguageExDto);

                foreach (var aLanguageKeyDto in aAppLanguageExDto.AppLanguageKeyList)
                {
                    AppLanguageKeyEntity aAppLanguageKeyEntity = new AppLanguageKeyEntity();
                    AppLanguageKeyConverter.CopyDtoToEntity(aAppLanguageKeyEntity, aLanguageKeyDto);



                    aAppLanguageEntity.AppLanguageKey.Add(aAppLanguageKeyEntity);
                }

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppLanguageEntity);
                        languageId = aAppLanguageEntity.LanguageId;

                        adapter.Commit();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppLanguageExDto), "app_LanguageEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppLanguageExDto), "plm_LanguageEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }
            else if (aAppLanguageExDto.IsRelatedEntitiesModified())
            {
                languageId = int.Parse(aAppLanguageExDto.Id.ToString());

                aValidationResult.Merge(ProcessDirtyAppLanguageExDto(aAppLanguageExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                if (languageId.HasValue)
                {
                    aOperationCallResult.Object = RetrieveOneAppLanguageExDto(languageId);
                }
                else
                {
                    aOperationCallResult.Object = aAppLanguageExDto;
                }

            }

            return aOperationCallResult;
        }

        private static ValidationResult ProcessDirtyAppLanguageExDto(AppLanguageExDto aAppLanguageExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            object[] dirtyLanguageKeyIds = aAppLanguageExDto.AppLanguageKeyList.FindModifiedItems().Select(o => o.Id).ToArray();

            AppLanguageEntity aAppLanguageEntity = RetrieveOneAppLanguageEntityWithSelectedLanguageKey(aAppLanguageExDto.Id, dirtyLanguageKeyIds);
            AppLanguageConverter.CopyDtoToEntity(aAppLanguageEntity, aAppLanguageExDto);


            // from DBMS entity
            Dictionary<int, AppLanguageKeyEntity> dictAppLanguageKeyEntityFromDbms = aAppLanguageEntity.AppLanguageKey.ToDictionary(o => o.LanguageKeyId, o => o);

            // new Items
            foreach (AppLanguageKeyExDto aChildDto in aAppLanguageExDto.AppLanguageKeyList.FindNewItems())
            {
                AppLanguageKeyEntity aNewChildEntity = new AppLanguageKeyEntity();
                AppLanguageKeyConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);



                aAppLanguageEntity.AppLanguageKey.Add(aNewChildEntity);
            }

            // Dirty items, only the update item remove from dbms, no need to update that itmes
            foreach (var modifyitem in aAppLanguageExDto.AppLanguageKeyList.FindModifiedItems())
            {
                if (!modifyitem.IsNew)
                {
                    int dtoKey = int.Parse(modifyitem.Id.ToString());
                    if (dictAppLanguageKeyEntityFromDbms.ContainsKey(dtoKey))
                    {
                        AppLanguageKeyConverter.CopyDtoToEntity(dictAppLanguageKeyEntityFromDbms[dtoKey], modifyitem);


                    }


                }
            }

            // deletedIDs
            object[] deleteChildsIDs = aAppLanguageExDto.AppLanguageKeyList.FindDeletedItemIds().ToArray();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppLanguageEntity, false, true);
                    // using batch Delete
                    if (deleteChildsIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppLanguageKeyEntity), new RelationPredicateBucket(AppLanguageKeyFields.LanguageKeyId == deleteChildsIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppLanguageEntity), "app_LanguageEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppLanguageEntity), "app_LanguageEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        public static OperationCallResult<AppLanguageKeyDto> SaveAppLanguageKeyExDto(AppLanguageKeyDto aAppLanguageKeyDto)
        {
            OperationCallResult<AppLanguageKeyDto> aOperationCallResult = new OperationCallResult<AppLanguageKeyDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppLanguageKeyEntity aAppLanguageKeyEntity = null;

            // prepare Data
            if (aAppLanguageKeyDto.IsNew)
            {
                aAppLanguageKeyEntity = new AppLanguageKeyEntity();
                AppLanguageKeyConverter.CopyDtoToEntity(aAppLanguageKeyEntity, aAppLanguageKeyDto);


                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppLanguageKeyEntity, true);

                        adapter.Commit();

                        aValidationResult.Items.Add(new ValidationItem(typeof(AppLanguageExDto), "app_LanguageKeyEntity_Save_OK", ValidationItemType.Message, aAppLanguageKeyEntity.ResourceKey));
                    }

                    // Database FK Exception .......
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        // ValidationItem ParseDatabaseException( ex,  aAppLanguageKeyDto);
                        aValidationResult.Items.Add(ParseDatabaseException(ex, aAppLanguageKeyDto));
                    }
                }
            }
            else if (aAppLanguageKeyDto.IsRelatedEntitiesModified())
            {
                aAppLanguageKeyEntity = RetrieveOneAppLanguageKeyEntity(aAppLanguageKeyDto.Id);
                AppLanguageKeyConverter.CopyDtoToEntity(aAppLanguageKeyEntity, aAppLanguageKeyDto);


                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppLanguageKeyEntity, true);

                        adapter.Commit();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppLanguageExDto), "app_LanguageKeyEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Database FK Exception .......
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(ParseDatabaseException(ex, aAppLanguageKeyDto));
                    }
                }
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppLanguageKeyExDto(aAppLanguageKeyEntity.LanguageKeyId);
            }

            return aOperationCallResult;
        }

        private static ValidationItem ParseDatabaseException(Exception ex, AppLanguageKeyDto aAppLanguageKeyDto)
        {
            if (ex.ToString().Contains("FK_languagekey_language"))
            {
                return new ValidationItem(typeof(AppLanguageKeyDto), "app_LanguageKeyEntity_Missing_Language", ValidationItemType.Error, aAppLanguageKeyDto.ResourceKey);
            }
            else if (ex.ToString().Contains("UC_LanguageID_ResourceKey"))
            {
                return new ValidationItem(typeof(AppLanguageKeyDto), "app_LanguageKeyEntity_Duplicated_LanguageID_ResourceKey", ValidationItemType.Error, aAppLanguageKeyDto.ResourceKey);
            }
            else
            {
                return new ValidationItem(typeof(AppLanguageKeyDto), "app_LanguageKeyEntity_UnKnowException", ValidationItemType.Error, ex.ToString());
            }
        }


        //Delete a Language
        public static OperationCallResult<object> DeleteAppLanguage(object languageId)
        {
            OperationCallResult<object> aValidationResult = new OperationCallResult<object>();
            string referMsg = string.Empty;
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppLanguageEntity), new RelationPredicateBucket(AppLanguageFields.LanguageId == languageId));
                    adapter.Commit();
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    // aValidationResult.ValidationResult.AddBussinessError(typeof(AppLanguageEntity), "app_LanguageEntity_QueryExecution_Error", ex.ToString());
                }
            }

            // if no any errors
            if (!aValidationResult.ValidationResult.HasErrors)
            {
                aValidationResult.Object = languageId;
            }

            return aValidationResult;
        }






        private static string GetLanguageName(int languageKey)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
            {
                try
                {
                    string getLanguagename = @"select  Name from AppLanguage      where LanguageID = @LanguageID     ";
                    List<SqlParameter> paramter = new List<SqlParameter>();
                    paramter.Add(new SqlParameter("@LanguageID", languageKey));

                    string Languagename = adapter.ExecuteScalarQuery(getLanguagename, paramter) as string;
                    return Languagename;
                }
                catch
                {

                }

            }

            return string.Empty;
        }





        private static DataTable GetExistingAppLanguageKeyTable(int languageKey, DataAccessAdapter adapter, string Languagename)
        {
            string queryUpdateLanguae = @"
                    select defaultLanguagekey.ResourceKey, defaultLanguagekey.Value as DefaulLanguageValue, currentLanguagekey.Value
                    from AppLanguagekey as defaultLanguagekey LEFT OUTER JOIN AppLanguagekey as currentLanguagekey on (defaultLanguagekey.ResourceKey = currentLanguagekey.ResourceKey and currentLanguagekey.LanguageID=@LanguageID )
                    where defaultLanguagekey.LanguageID = 1  ";

            List<SqlParameter> paramter = new List<SqlParameter>();
            paramter.Add(new SqlParameter("@LanguageID", languageKey));

            return adapter.ExecuteDataTableRetrievalQuery(queryUpdateLanguae, paramter);
        }




        //1:BlockSubite
        private static bool ImportFromAndTrasactionFieldLaguangeKey(byte[] rawFileData, int laguangeId, int importLanguageType)
        {
            MemoryStream fileStream = new MemoryStream(rawFileData);

            //DataTable result = ExcelParser.Excel2DataTable(fileStream);
            DataTable result = ExcelImportExportBL.ExcelToDataTable(rawFileData);

            List<string> firstRowAsColumnNameList = new List<string>();

            //BlockName	SubItemID	DefaultLanguange	NewLanguange

            if (result.Rows.Count > 0)
            {
                DataRow row = result.Rows[0];
                for (int i = 0; i < result.Columns.Count; i++)
                {
                    var cellValue = row[i];
                    string columName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(cellValue);

                    firstRowAsColumnNameList.Add(columName);
                }
            }



            List<AppSysLabelLanguageEntity> AppSysLabelLanguageEntityList = new List<AppSysLabelLanguageEntity>();

            for (int rowIndex = 1; rowIndex < result.Rows.Count; rowIndex++)
            {
                DataRow row = result.Rows[rowIndex];
                AppSysLabelLanguageEntity AppSysLabelLanguageEntity = new AppSysLabelLanguageEntity();
                AppSysLabelLanguageEntity.LanguageId = laguangeId;

                if (importLanguageType == (int)EmAppImportLanguageType.Form)
                {
                    AppSysLabelLanguageEntity.FormId = ControlTypeValueConverter.ConvertValueToInt(row[1]);
                }
                else if (importLanguageType == (int)EmAppImportLanguageType.TransactionField)
                {
                    AppSysLabelLanguageEntity.TransactionFieldId = ControlTypeValueConverter.ConvertValueToInt(row[1]);
                }


                AppSysLabelLanguageEntity.LanguageText = row[3] as string;
                AppSysLabelLanguageEntityList.Add(AppSysLabelLanguageEntity);
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
            {
                try
                {
                    // need to delte
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    RelationPredicateBucket filter = new RelationPredicateBucket();

                    if (importLanguageType == (int)EmAppImportLanguageType.Form)
                    {
                        // AppSysLabelLanguageEntity.SubItemId = ControlTypeValueConverter.ConvertValueToInt(row["SubItemID"]);  System.DBNull.Value

                        filter.PredicateExpression.Add(AppSysLabelLanguageFields.LanguageId == laguangeId & (AppSysLabelLanguageFields.FormId != System.DBNull.Value));
                        // filter.



                    }
                    else if (importLanguageType == (int)EmAppImportLanguageType.TransactionField)
                    {
                        filter.PredicateExpression.Add(AppSysLabelLanguageFields.LanguageId == laguangeId & (AppSysLabelLanguageFields.TransactionFieldId != System.DBNull.Value));
                    }




                    adapter.DeleteEntitiesDirectly(typeof(AppSysLabelLanguageEntity), filter);

                    foreach (var entity in AppSysLabelLanguageEntityList)
                    {
                        adapter.SaveEntity(entity);
                    }
                    adapter.Commit();
                    return true;
                }
                catch { }
                {
                    adapter.Rollback();

                    return false;
                }
            }

            //return toReturn;
        }







        private static bool ImportPdmLaguangeKey(byte[] rawFileData, int laguangeId, int importLanguageType)
        {
            //MemoryStream fileStream = new MemoryStream(rawFileData);

            //DataTable result = ExcelParser.Excel2DataTable(fileStream);

            DataTable result = ExcelImportExportBL.ExcelToDataTable(rawFileData);

            List<string> firstRowAsColumnNameList = new List<string>();

            if (result.Rows.Count > 0)
            {
                DataRow row = result.Rows[0];
                for (int i = 0; i < result.Columns.Count; i++)
                {
                    var cellValue = row[i];
                    string columName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(cellValue);

                    firstRowAsColumnNameList.Add(columName);
                }
            }

            List<AppLanguageKeyEntity> AppLanguageKeyEntityList = new List<AppLanguageKeyEntity>();
            int currentUserId = AppSecurityUserBL.CurrentUserEntity.UserId;

            for (int rowIndex = 1; rowIndex < result.Rows.Count; rowIndex++)
            {
                DataRow row = result.Rows[rowIndex];
                AppLanguageKeyEntity AppLanguageKeyEntity = new AppLanguageKeyEntity();
                AppLanguageKeyEntity.LanguageId = laguangeId;


                if (row[1] != null && !string.IsNullOrEmpty(row[1].ToString()))
                {
                    AppLanguageKeyEntity.ResourceKey = row[0].ToString();
                    AppLanguageKeyEntity.Value = row[2] as string;

                    AppLanguageKeyEntityList.Add(AppLanguageKeyEntity);
                }
                else
                {

                }


            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
            {
                try
                {
                    // need to delte
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    RelationPredicateBucket filter = new RelationPredicateBucket();

                    filter.PredicateExpression.Add(AppLanguageKeyFields.LanguageId == laguangeId);

                    adapter.DeleteEntitiesDirectly(typeof(AppLanguageKeyEntity), filter);

                    foreach (var entity in AppLanguageKeyEntityList)
                    {
                        adapter.SaveEntity(entity);
                    }
                    adapter.Commit();
                    return true;
                }
                catch { }
                {
                    adapter.Rollback();

                    return false;
                }
            }

            //return toReturn;
        }


        public static List<AppSysLabelLanguageDto> RetrieveOneLanguageAllAppSysLabelLanguageDtoList(int languageId)
        {
            List<AppSysLabelLanguageDto> toReturn = new List<AppSysLabelLanguageDto>();
            EntityCollection<AppSysLabelLanguageEntity> list = new EntityCollection<AppSysLabelLanguageEntity>();
            RelationPredicateBucket filter = new RelationPredicateBucket(AppSysLabelLanguageFields.LanguageId == languageId);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
            {
                adapter.FetchEntityCollection(list, filter);
            }

            foreach (var entity in list)
            {
                var dto = AppSysLabelLanguageConverter.ConvertEntityToDto(entity);
                InitialSysLabelLanguageKeyType(dto);
                toReturn.Add(dto);
            }

            return toReturn;
        }


        public static Dictionary<int, string> GetDictTransactionFieldLanguage(int[] transactionFieldIds)
        {

            Dictionary<int, string> toReturn = new Dictionary<int, string>();
            if (!AppSecurityUserBL.CurrentUserEntity.LanguageId.HasValue)
            {
                return toReturn;
            }


            int LanguageId = AppSecurityUserBL.CurrentUserEntity.LanguageId.Value;

            EntityCollection<AppSysLabelLanguageEntity> list = new EntityCollection<AppSysLabelLanguageEntity>();
            RelationPredicateBucket filter = new RelationPredicateBucket(AppSysLabelLanguageFields.TransactionFieldId == transactionFieldIds & AppSysLabelLanguageFields.LanguageId == LanguageId);
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetContextAdapter())
            {
                adapter.FetchEntityCollection(list, filter);



            }


            return list.ToDictionary(o => o.TransactionFieldId.Value, o => o.LanguageText);





        }





        public static List<LanguageKeyByTypeDto> RetrieveOneLanguageAllLanguageKeys(int languageId, int? languageKeyType = null)
        {
            List<LanguageKeyByTypeDto> toReturn = new List<LanguageKeyByTypeDto>();

            AppLanguageEntity defaultLanguage = RetrieveDefaultAppLanguageEntity();

            if (defaultLanguage != null)
            {
                // 1. AppLanguageKey
                if (!languageKeyType.HasValue || languageKeyType.Value == (int)EmAppLanguageKeyType.SystemLabel)
                {
                    AppLanguageEntity aLanguageEntity = RetrieveOneAppLanguageEntity(languageId);
                    if (aLanguageEntity != null)
                    {
                        foreach (var aDefaultLanguageKey in defaultLanguage.AppLanguageKey)
                        {
                            LanguageKeyByTypeDto aDto = new LanguageKeyByTypeDto();
                            aDto.KeyType = (int)EmAppLanguageKeyType.SystemLabel;
                            aDto.KeyTypeDisplay = EmAppLanguageKeyType.SystemLabel.ToString();
                            aDto.LanguageId = languageId;
                            aDto.ResourceKey = aDefaultLanguageKey.ResourceKey;
                            aDto.DefaultLanguageText = aDefaultLanguageKey.Value;

                            var matchedTargetKey = aLanguageEntity.AppLanguageKey.FirstOrDefault(o => o.ResourceKey == aDto.ResourceKey);
                            if (matchedTargetKey != null)
                            {
                                aDto.LanguageKeyId = matchedTargetKey.LanguageKeyId;
                                aDto.LanguageText = matchedTargetKey.Value;
                            }

                            toReturn.Add(aDto);
                        }
                    }
                }

                // 2. AppSysLabelLanguage
                if (!languageKeyType.HasValue || languageKeyType.Value != (int)EmAppLanguageKeyType.SystemLabel)
                {
                    List<AppSysLabelLanguageDto> defaultSysLabelLanguageDtoList = RetrieveOneLanguageAllAppSysLabelLanguageDtoList(defaultLanguage.LanguageId);
                    List<AppSysLabelLanguageDto> targetSysLabelLanguageDtoList = RetrieveOneLanguageAllAppSysLabelLanguageDtoList(languageId);

                    if (languageKeyType.HasValue)
                    {
                        defaultSysLabelLanguageDtoList = defaultSysLabelLanguageDtoList.Where(o => o.KeyType == languageKeyType.Value).ToList();
                        targetSysLabelLanguageDtoList = targetSysLabelLanguageDtoList.Where(o => o.KeyType == languageKeyType.Value).ToList();
                    }

                    if (defaultSysLabelLanguageDtoList != null && targetSysLabelLanguageDtoList != null)
                    {
                        foreach (var aDefaultSysLabelLanguageDto in defaultSysLabelLanguageDtoList)
                        {
                            LanguageKeyByTypeDto aDto = new LanguageKeyByTypeDto();
                            aDto.LanguageId = languageId;
                            aDto.DefaultLanguageText = aDefaultSysLabelLanguageDto.LanguageText;
                            aDto.KeyType = aDefaultSysLabelLanguageDto.KeyType;
                            aDto.KeyTypeDisplay = ((EmAppLanguageKeyType)aDefaultSysLabelLanguageDto.KeyType).ToString();
                            aDto.TargetTypeSystemFieldId = aDefaultSysLabelLanguageDto.TargetTypeSystemFieldId;

                            if (aDto.TargetTypeSystemFieldId.HasValue)
                            {
                                var matchedTargetKey = targetSysLabelLanguageDtoList.FirstOrDefault(o => o.TargetTypeSystemFieldId.HasValue && o.TargetTypeSystemFieldId.Value == aDto.TargetTypeSystemFieldId.Value);
                                if (matchedTargetKey != null)
                                {
                                    aDto.SysLableLanguageKeyId = (int)matchedTargetKey.Id;
                                    aDto.LanguageText = matchedTargetKey.LanguageText;
                                }
                            }
                            toReturn.Add(aDto);
                        }
                    }
                }

            }

            return toReturn;
        }


        public static OperationCallResult<LanguageKeyByTypeDto> SaveOneLanguageAllLanguageKeys(List<LanguageKeyByTypeDto> languageKeyByTypeDtoList, int languageId, int? languageKeyType = null)
        {
            OperationCallResult<LanguageKeyByTypeDto> aOperationCallResult = new OperationCallResult<LanguageKeyByTypeDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            try
            {
                EntityCollection<AppLanguageKeyEntity> toSaveAppLanguageKeyEntitys = new EntityCollection<AppLanguageKeyEntity>();
                EntityCollection<AppSysLabelLanguageEntity> toSaveAppSysLabelLanguageEntitys = new EntityCollection<AppSysLabelLanguageEntity>();

                var modifiedLanguageKeys = languageKeyByTypeDtoList.Where(o => o.IsModified).ToList();

                foreach (var aModifiedKeyDto in modifiedLanguageKeys)
                {
                    if (aModifiedKeyDto.LanguageId.HasValue)
                    {
                        if (aModifiedKeyDto.KeyType == (int)EmAppLanguageKeyType.SystemLabel)
                        {
                            AppLanguageKeyEntity appLanguageKeyEntity = new AppLanguageKeyEntity();

                            if (aModifiedKeyDto.LanguageKeyId.HasValue)
                            {
                                appLanguageKeyEntity = new AppLanguageKeyEntity(aModifiedKeyDto.LanguageKeyId.Value);
                                appLanguageKeyEntity.IsNew = false;
                            }

                            appLanguageKeyEntity.LanguageId = aModifiedKeyDto.LanguageId.Value;
                            appLanguageKeyEntity.ResourceKey = aModifiedKeyDto.ResourceKey;
                            appLanguageKeyEntity.Value = aModifiedKeyDto.LanguageText;

                            toSaveAppLanguageKeyEntitys.Add(appLanguageKeyEntity);
                        }
                        else
                        {
                            AppSysLabelLanguageDto appSysLabelLanguageDto = ConvertLanguageKeyByTypeDtoToAppSysLabelLanguageDto(aModifiedKeyDto);
                            if (appSysLabelLanguageDto != null)
                            {
                                AppSysLabelLanguageEntity appSysLabelLanguageEntity = new AppSysLabelLanguageEntity();

                                int? sysLableLanguageId = ControlTypeValueConverter.ConvertValueToInt(appSysLabelLanguageDto.Id);
                                if (sysLableLanguageId.HasValue)
                                {
                                    appSysLabelLanguageEntity = new AppSysLabelLanguageEntity(sysLableLanguageId.Value);
                                    appSysLabelLanguageEntity.IsNew = false;
                                }

                                AppSysLabelLanguageConverter.CopyDtoToEntity(appSysLabelLanguageEntity, appSysLabelLanguageDto);
                                toSaveAppSysLabelLanguageEntitys.Add(appSysLabelLanguageEntity);
                            }
                        }
                    }
                }

                // AppLanguageKey lives in master DB
                if (toSaveAppLanguageKeyEntitys.Count > 0)
                {
                    using (DataAccessAdapter masterAdapter = AppTenantAdapterBL.GetContextAdapter())
                    {
                        masterAdapter.SaveEntityCollection(toSaveAppLanguageKeyEntitys);
                    }
                }

                // AppSysLabelLanguage lives in tenant DB
                if (toSaveAppSysLabelLanguageEntitys.Count > 0)
                {
                    using (DataAccessAdapter tenantAdapter = AppTenantAdapterBL.GetContextAdapter())
                    {
                        tenantAdapter.SaveEntityCollection(toSaveAppSysLabelLanguageEntitys);
                    }
                }

                validationResult.Items.Add(new ValidationItem(typeof(AppLanguageExDto), "app_LanguageKeyEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
            }

            // Database FK Exception .......
            catch (ORMQueryExecutionException ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppLanguageExDto), "plm_LanguageEntity_BLValidation_Error", ValidationItemType.Error, "Save Failed"));


            }


            if (!validationResult.HasErrors)
            {
                RefreshAllLanguageKeyDictionaries();

                aOperationCallResult.ObjectList = RetrieveOneLanguageAllLanguageKeys(languageId, languageKeyType);
            }

            return aOperationCallResult;

        }

        private static void RefreshAllLanguageKeyDictionaries()
        {
            AppLocalizeSystemLableBL.RefreshAppSystemLableLanguageKeyDictionaries();
            StringLocalizer.RefreshAppLanguageKeyDictionary();
        }


        private static void InitialSysLabelLanguageKeyType(AppSysLabelLanguageDto aSysLabelLanguageDto)
        {
            if (aSysLabelLanguageDto != null)
            {
                if (aSysLabelLanguageDto.MenuId.HasValue)
                {
                    aSysLabelLanguageDto.KeyType = (int)EmAppLanguageKeyType.Menu;
                    aSysLabelLanguageDto.TargetTypeSystemFieldId = aSysLabelLanguageDto.MenuId;
                }
                else if (aSysLabelLanguageDto.TransactionUnitId.HasValue)
                {
                    aSysLabelLanguageDto.KeyType = (int)EmAppLanguageKeyType.TransactionUnit;
                    aSysLabelLanguageDto.TargetTypeSystemFieldId = aSysLabelLanguageDto.TransactionUnitId;
                }
                else if (aSysLabelLanguageDto.TransactionFieldId.HasValue)
                {
                    aSysLabelLanguageDto.KeyType = (int)EmAppLanguageKeyType.TransactionField;
                    aSysLabelLanguageDto.TargetTypeSystemFieldId = aSysLabelLanguageDto.TransactionFieldId;
                }
                else if (aSysLabelLanguageDto.LinkTargetId.HasValue)
                {
                    aSysLabelLanguageDto.KeyType = (int)EmAppLanguageKeyType.LinkTarget;
                    aSysLabelLanguageDto.TargetTypeSystemFieldId = aSysLabelLanguageDto.LinkTargetId;
                }
                else if (aSysLabelLanguageDto.TransactionUnitLinkedSearchId.HasValue)
                {
                    aSysLabelLanguageDto.KeyType = (int)EmAppLanguageKeyType.LinkedSearch;
                    aSysLabelLanguageDto.TargetTypeSystemFieldId = aSysLabelLanguageDto.TransactionUnitLinkedSearchId;
                }
                else if (aSysLabelLanguageDto.SearchId.HasValue)
                {
                    aSysLabelLanguageDto.KeyType = (int)EmAppLanguageKeyType.Search;
                    aSysLabelLanguageDto.TargetTypeSystemFieldId = aSysLabelLanguageDto.SearchId;
                }
                else if (aSysLabelLanguageDto.SearchFieldId.HasValue)
                {
                    aSysLabelLanguageDto.KeyType = (int)EmAppLanguageKeyType.SearchField;
                    aSysLabelLanguageDto.TargetTypeSystemFieldId = aSysLabelLanguageDto.SearchFieldId;
                }
                else if (aSysLabelLanguageDto.SearchViewId.HasValue)
                {
                    aSysLabelLanguageDto.KeyType = (int)EmAppLanguageKeyType.SearchView;
                    aSysLabelLanguageDto.TargetTypeSystemFieldId = aSysLabelLanguageDto.SearchViewId;
                }
                else if (aSysLabelLanguageDto.SearchViewFieldId.HasValue)
                {
                    aSysLabelLanguageDto.KeyType = (int)EmAppLanguageKeyType.SearchViewField;
                    aSysLabelLanguageDto.TargetTypeSystemFieldId = aSysLabelLanguageDto.SearchViewFieldId;
                }
            }
        }

        private static AppSysLabelLanguageDto ConvertLanguageKeyByTypeDtoToAppSysLabelLanguageDto(LanguageKeyByTypeDto aLanguageKeyByTypeDto)
        {
            if (aLanguageKeyByTypeDto != null)
            {
                AppSysLabelLanguageDto toReturn = new AppSysLabelLanguageDto();

                toReturn.Id = aLanguageKeyByTypeDto.SysLableLanguageKeyId;
                toReturn.LanguageId = aLanguageKeyByTypeDto.LanguageId;
                toReturn.LanguageText = aLanguageKeyByTypeDto.LanguageText;

                if (aLanguageKeyByTypeDto.KeyType == (int)EmAppLanguageKeyType.Menu)
                {
                    toReturn.MenuId = aLanguageKeyByTypeDto.TargetTypeSystemFieldId;
                }
                else if (aLanguageKeyByTypeDto.KeyType == (int)EmAppLanguageKeyType.TransactionUnit)
                {
                    toReturn.TransactionUnitId = aLanguageKeyByTypeDto.TargetTypeSystemFieldId;
                }
                else if (aLanguageKeyByTypeDto.KeyType == (int)EmAppLanguageKeyType.TransactionField)
                {
                    toReturn.TransactionFieldId = aLanguageKeyByTypeDto.TargetTypeSystemFieldId;
                }
                else if (aLanguageKeyByTypeDto.KeyType == (int)EmAppLanguageKeyType.LinkTarget)
                {
                    toReturn.LinkTargetId = aLanguageKeyByTypeDto.TargetTypeSystemFieldId;
                }
                else if (aLanguageKeyByTypeDto.KeyType == (int)EmAppLanguageKeyType.LinkedSearch)
                {
                    toReturn.TransactionUnitLinkedSearchId = aLanguageKeyByTypeDto.TargetTypeSystemFieldId;
                }
                else if (aLanguageKeyByTypeDto.KeyType == (int)EmAppLanguageKeyType.Search)
                {
                    toReturn.SearchId = aLanguageKeyByTypeDto.TargetTypeSystemFieldId;
                }
                else if (aLanguageKeyByTypeDto.KeyType == (int)EmAppLanguageKeyType.SearchField)
                {
                    toReturn.SearchFieldId = aLanguageKeyByTypeDto.TargetTypeSystemFieldId;
                }
                else if (aLanguageKeyByTypeDto.KeyType == (int)EmAppLanguageKeyType.SearchView)
                {
                    toReturn.SearchViewId = aLanguageKeyByTypeDto.TargetTypeSystemFieldId;
                }
                else if (aLanguageKeyByTypeDto.KeyType == (int)EmAppLanguageKeyType.SearchViewField)
                {
                    toReturn.SearchViewFieldId = aLanguageKeyByTypeDto.TargetTypeSystemFieldId;
                }

                return toReturn;
            }

            return null;
        }

        public static OperationCallResult<bool> GenerateAppSysLabelDefaultLanguageKeys()
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            string ProcedureName = "SP_GenerateAppSysLabelDefaultLanguageKeys";

            List<SqlParameter> paramtersList = new List<SqlParameter>();

            using (var conn = new SqlConnection(ServerContext.Instance.CurrentUserDataBaseName))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    AppTransactionDataDeleteBL.ExcuteNonQuery(ProcedureName, paramtersList, conn, trans, true);
                    trans.Commit();
                    aValidationResult.Items.Add(new ValidationItem(null, "app_LanguageEntity_GenerateDefaultKeySuccsessful", ValidationItemType.Message, "Default Languagekey Generation Completed."));
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppLanguageExDto), "app_LanguageEntity_GenerateDefaultKeyFailed", ValidationItemType.Error, "Default Languagekey Generation Failed"));
                }
            }

            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }
            else
            {
                aOperationCallResult.Object = false;
            }

            return aOperationCallResult;
        }

        #region For Excel Import Export

        // For Import
        public static OperationCallResult<bool> ImportLanguageKeyFromExcel(List<string> filePathList)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (filePathList != null && filePathList.Count > 0)
            {
                foreach (string strFilePath in filePathList)
                {
                    DataTable dt = AppImportExportExcelToDataTableBL.ConvertCsvFileToDataTable(strFilePath, 3, "xls", true);

                    if (dt.Rows.Count > 0 && dt.Columns.Count == 3)
                    {                        
                        string udLanguageKeyColName = dt.Columns[0].ColumnName;

                        int? languageId = null;
                        int? languageKeyType = null;

                        GetLangaugeIdAndKeyTypeFromExcelImportColumnHeader(udLanguageKeyColName, ref languageId, ref languageKeyType);

                        if (languageId.HasValue && languageKeyType.HasValue)
                        {
                            List<LanguageKeyByTypeDto> orgKeyList = RetrieveOneLanguageAllLanguageKeys(languageId.Value, languageKeyType.Value);

                            if (orgKeyList != null)
                            {
                                for (int rowIndex = 0; rowIndex < dt.Rows.Count; rowIndex++)
                                {
                                    DataRow row = dt.Rows[rowIndex];

                                    string udLanguageKey = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row[0]);
                                    string languageValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row[2]);

                                    if (!string.IsNullOrEmpty(udLanguageKey))
                                    {
                                        if (languageKeyType.Value == (int)EmAppLanguageKeyType.SystemLabel)
                                        {
                                            var orgKey = orgKeyList.FirstOrDefault(o => o.ResourceKey == udLanguageKey);
                                            orgKey.LanguageText = languageValue;
                                            orgKey.IsModified = true;
                                        }
                                        else
                                        {
                                            var orgKey = orgKeyList.FirstOrDefault(o => o.TargetTypeSystemFieldId.HasValue && o.TargetTypeSystemFieldId.Value.ToString() == udLanguageKey);
                                            orgKey.LanguageText = languageValue;
                                            orgKey.IsModified = true;
                                        }
                                    }
                                }

                                var saveResult = SaveOneLanguageAllLanguageKeys(orgKeyList, languageId.Value, languageKeyType.Value);
                                validationResult.Merge(saveResult.ValidationResult);
                            }
                        }
                    }

                }

                if (!validationResult.HasErrors)
                {
                    aOperationCallResult.Object = true;
                }
            }

            return aOperationCallResult;
        }

        private static void GetLangaugeIdAndKeyTypeFromExcelImportColumnHeader(string udLanguageKeyColName, ref int? langaugeId, ref int? languageKeyType)
        {
            int Index_Label_LanguageKey = udLanguageKeyColName.IndexOf(" " + Label_LanguageKey);
            if (Index_Label_LanguageKey > 0)
            {
                string keyTypeString = udLanguageKeyColName.Substring(0, Index_Label_LanguageKey);
                languageKeyType = (int)Enum.Parse(typeof(EmAppLanguageKeyType), keyTypeString);

                int languageIdIndex = Index_Label_LanguageKey + Label_LanguageKey.Length + 1;
                int languageIdLength = udLanguageKeyColName.Length - languageIdIndex;

                langaugeId = null;
                if (languageIdLength > 0)
                {
                    string languageIdString = udLanguageKeyColName.Substring(languageIdIndex, languageIdLength);
                    langaugeId = ControlTypeValueConverter.ConvertValueToInt(languageIdString);
                }

            }
        }

        // For Export
        public static string GenerateLanguageKeyExcelExportFileName(int? languageId, int? languageKeyType)
        {
            if (languageId.HasValue && languageKeyType.HasValue)
            {
                string languageName = GetLanguageName(languageId.Value);

                string languageTypeDisplay = ((EmAppLanguageKeyType)languageKeyType.Value).ToString();


                string fileName = Label_LanguageKey + "_" + languageName + "_" + languageTypeDisplay + ".xls";
                return fileName;
            }

            return "LanguageKey.xls";
        }

        public static DataTable GenerateLanguageKeyExcelExportDataTable(int? languageId, int? languageKeyType)
        {
            if (languageId.HasValue && languageKeyType.HasValue)
            {
                List<LanguageKeyByTypeDto> lanKeyList = RetrieveOneLanguageAllLanguageKeys(languageId.Value, languageKeyType.Value);
                string languageTypeDisplay = ((EmAppLanguageKeyType)languageKeyType.Value).ToString();
                string languageName = GetLanguageName(languageId.Value);

                //LanguageKeyByTypeDto aDto = new LanguageKeyByTypeDto();
                //aDto.KeyType = (int)EmAppLanguageKeyType.SystemLabel;
                //aDto.KeyTypeDisplay = EmAppLanguageKeyType.SystemLabel.ToString();
                //aDto.LanguageId = languageId;
                //aDto.ResourceKey = aDefaultLanguageKey.ResourceKey;
                //aDto.DefaultLanguageText = aDefaultLanguageKey.Value;

                //var matchedTargetKey = aLanguageEntity.AppLanguageKey.FirstOrDefault(o => o.ResourceKey == aDto.ResourceKey);
                //if (matchedTargetKey != null)
                //{
                //    aDto.LanguageKeyId = matchedTargetKey.LanguageKeyId;
                //    aDto.LanguageText = matchedTargetKey.Value;
                //}


                DataTable dt = new DataTable();
                dt.Columns.Add(languageTypeDisplay + " " + Label_LanguageKey + " " + languageId.Value.ToString(), typeof(string));
                dt.Columns.Add(Label_DefaultValue, typeof(string));
                dt.Columns.Add(languageName + " " + Label_Value, typeof(string));

                foreach (LanguageKeyByTypeDto aLanKeyDto in lanKeyList)
                {
                    PrepareDataRow(languageKeyType, aLanKeyDto);

                    var row = PrepareDataRow(languageKeyType, aLanKeyDto);

                    if (row != null)
                    {
                        dt.Rows.Add(row);
                    }
                }

                return dt;
            }

            return null;
        }

        private static object[] PrepareDataRow(int? languageKeyType, LanguageKeyByTypeDto aLanKeyDto)
        {
            if (languageKeyType.HasValue)
            {
                string udLanguageKey = string.Empty;

                int keyType = languageKeyType.Value;

                if (languageKeyType.Value == (int)EmAppLanguageKeyType.SystemLabel)
                {
                    udLanguageKey = aLanKeyDto.ResourceKey;
                }
                else
                {
                    if (aLanKeyDto.TargetTypeSystemFieldId.HasValue)
                    {
                        udLanguageKey = aLanKeyDto.TargetTypeSystemFieldId.Value.ToString();
                    }
                }

                //if (keyType == (int)EmAppLanguageKeyType.SystemLabel)
                //{
                //    if (aLanKeyDto.SysLableLanguageKeyId.HasValue)
                //    {
                //        udLanguageKey = aLanKeyDto.SysLableLanguageKeyId.Value.ToString();
                //    }
                //}
                //else if (keyType == (int)EmAppLanguageKeyType.Menu)
                //{
                //    if (aLanKeyDto.MenuId.HasValue)
                //    {
                //        udLanguageKey = aLanKeyDto.MenuId.Value.ToString();
                //    }
                //}
                //else if (keyType == (int)EmAppLanguageKeyType.TransactionUnit)
                //{
                //    if (aLanKeyDto.TransactionUnitId.HasValue)
                //    {
                //        udLanguageKey = aLanKeyDto.TransactionUnitId.Value.ToString();
                //    }
                //}
                //else if (keyType == (int)EmAppLanguageKeyType.TransactionField)
                //{
                //    if (aLanKeyDto.TransactionFieldId.HasValue)
                //    {
                //        udLanguageKey = aLanKeyDto.TransactionFieldId.Value.ToString();
                //    }
                //}
                //else if (keyType == (int)EmAppLanguageKeyType.LinkTarget)
                //{
                //    if (aLanKeyDto.LinkTargetId.HasValue)
                //    {
                //        udLanguageKey = aLanKeyDto.LinkTargetId.Value.ToString();
                //    }
                //}
                //else if (keyType == (int)EmAppLanguageKeyType.LinkedSearch)
                //{
                //    if (aLanKeyDto.TransactionUnitLinkedSearchId.HasValue)
                //    {
                //        udLanguageKey = aLanKeyDto.TransactionUnitLinkedSearchId.Value.ToString();
                //    }
                //}
                //else if (keyType == (int)EmAppLanguageKeyType.Search)
                //{
                //    if (aLanKeyDto.SearchId.HasValue)
                //    {
                //        udLanguageKey = aLanKeyDto.SearchId.Value.ToString();
                //    }
                //}
                //else if (keyType == (int)EmAppLanguageKeyType.SearchField)
                //{
                //    if (aLanKeyDto.SearchFieldId.HasValue)
                //    {
                //        udLanguageKey = aLanKeyDto.SearchFieldId.Value.ToString();
                //    }
                //}
                //else if (keyType == (int)EmAppLanguageKeyType.SearchView)
                //{
                //    if (aLanKeyDto.SearchViewId.HasValue)
                //    {
                //        udLanguageKey = aLanKeyDto.SearchViewId.Value.ToString();
                //    }
                //}
                //else if (keyType == (int)EmAppLanguageKeyType.SearchViewField)
                //{
                //    if (aLanKeyDto.SearchViewFieldId.HasValue)
                //    {
                //        udLanguageKey = aLanKeyDto.SearchViewFieldId.Value.ToString();
                //    }
                //}



                if (!string.IsNullOrEmpty(udLanguageKey))
                {
                    string defaultValue = aLanKeyDto.DefaultLanguageText;
                    string value = aLanKeyDto.LanguageText;

                    var dataRow = new object[] { udLanguageKey, defaultValue, value };
                    return dataRow;

                }


            }


            return null;
        }





        #endregion




    }
}



