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
using System.Text.RegularExpressions;
using System;
using DatabaseSchemaMrg.DataSchema;
using System.Data.SqlClient;
using DatabaseSchemaMrg;
//using APP.Persistence.Common;

using APP.Framework;
namespace App.BL
{

    public static class AppTransactionTemplateDataLoadSetupBL
    {
        private static Regex FormulaRegex = new Regex(@"\[.+?\]");
        //public static string[] FormulaConstString = { "(", ")", "=", "==", "!=", ">", "<", ">=", "<=", "+", "-", "*", "/", "%", "&&", "||", "&", "|", "true", "false", "!", "::",
        //                                            ".TotalMinutes", ".TotalHours", ".TotalDays", ".AddMinutes", ".AddHours", ".AddDays"};
        public static string TransactionFieldFormulaPrefix = "transactionfieldid_";
        public static string DatabaseColumnPrefix = "databasecolumn_";


        public static string TransactionFieldUIPrefix = "Field_";
        public static string DatabaseColumnUIPrefix = "DBColumn_";

        public static readonly string[] ConditionConstString = { "=", "<>", ">", "<", ">=", "<=", " AND ", " OR ", };


        internal static AppTransactionDataLoadEntity RetrieveOneAppTransactionDataLoadEntity(object dataLoadID)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionDataLoadEntity aEntity = new AppTransactionDataLoadEntity(int.Parse(dataLoadID.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppTransactionDataLoadEntity);

                rootPath.Add(AppTransactionDataLoadEntity.PrefetchPathAppDataSet);
                rootPath.Add(AppTransactionDataLoadEntity.PrefetchPathAppTranscationDataLoadFieldMapping);


                adpater.FetchEntity(aEntity, rootPath);
                return aEntity;
            }
        }


		internal static EntityCollection<AppTransactionDataLoadEntity> RetrieveOneTrasactioAllLoad(object transactionId)
		{
			EntityCollection<AppTransactionDataLoadEntity> list = new EntityCollection<AppTransactionDataLoadEntity>();

			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{

				IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppTransactionDataLoadEntity);

				rootPath.Add(AppTransactionDataLoadEntity.PrefetchPathAppDataSet);
				rootPath.Add(AppTransactionDataLoadEntity.PrefetchPathAppTranscationDataLoadFieldMapping);

				adapter.FetchEntityCollection(list, new RelationPredicateBucket(AppTransactionDataLoadFields.TransactionId == transactionId), rootPath);


			}
			return list;
		}


		internal static EntityCollection<AppTransactionDataLoadEntity> RetrieveOneTrasactionDataLoadEntityCollection(object transactionId )
        {
            EntityCollection<AppTransactionDataLoadEntity> list = new EntityCollection<AppTransactionDataLoadEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                adapter.FetchEntityCollection(list, new RelationPredicateBucket(AppTransactionDataLoadFields.TransactionId == transactionId));


            }
            return list;
        }

        public static ObservableSet<AppTransactionDataLoadDto> RetrievOneAppTransactionDataLoadDto(object transactionId)
        {
            EntityCollection<AppTransactionDataLoadEntity> list = RetrieveOneTrasactionDataLoadEntityCollection(transactionId);

            var aDtoList = new ObservableSet<AppTransactionDataLoadDto>();

            foreach (var o in list.OrderBy(p=>p.LoadOrder))
            {
                aDtoList.Add(AppTransactionDataLoadConverter.ConvertEntityToDto(o));
            }

            return aDtoList;
        }



        internal static EntityCollection<AppTransactionDataLoadEntity> RetrieveOneTrasactionUnitDataLoadEntity(object transactionUnitId)
        {
            EntityCollection<AppTransactionDataLoadEntity> list = new EntityCollection<AppTransactionDataLoadEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(list, new RelationPredicateBucket(AppTransactionDataLoadFields.TransactionUnitId == transactionUnitId));
            }
            return list;
        }

        public static ObservableSet<AppTransactionDataLoadDto> RetrievOneAppTransactionUnitDataLoadDto(object transactionUnitId)
        {
            EntityCollection<AppTransactionDataLoadEntity> list = RetrieveOneTrasactionUnitDataLoadEntity(transactionUnitId);

            var aDtoList = new ObservableSet<AppTransactionDataLoadDto>();

            foreach (var o in list.OrderBy(p => p.LoadOrder))
            {
                aDtoList.Add(AppTransactionDataLoadConverter.ConvertEntityToDto(o));
            }

            return aDtoList;
        }



        internal static EntityCollection<AppTransactionDataLoadEntity> RetrieveAllTrasactionDataLoadEntity()
        {
            EntityCollection<AppTransactionDataLoadEntity> list = new EntityCollection<AppTransactionDataLoadEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                adapter.FetchEntityCollection(list, null);


            }
            return list;
        }



        public static AppTransactionDataLoadExDto RetrieveOneAppTransactionDataLoadExDto(object dataLoadID)
        {
            AppTransactionDataLoadEntity aAppTransactionDataLoadEntity = RetrieveOneAppTransactionDataLoadEntity(dataLoadID);

            AppTransactionDataLoadExDto aAppTransactionDataLoadExDto = AppTransactionDataLoadConverter.ConvertEntityToExDto(aAppTransactionDataLoadEntity);


            List<LookupItemDto> dbColumnLookupList = new List<LookupItemDto>();
            List<AppTransactionFieldExDto> transactionFieldList = new List<AppTransactionFieldExDto>();

            if (aAppTransactionDataLoadExDto.TransactionId.HasValue)
            {
                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppTransactionDataLoadExDto.TransactionId.Value);
                transactionFieldList = transactionExDto.DictRootLevelUnitTransactionField.Values.ToList();
                InitialTransactionFieldFormularDisplayName(transactionExDto, transactionFieldList);
            }

            if (aAppTransactionDataLoadExDto.DataSetId.HasValue)
            {
                dbColumnLookupList = AppDataSetBL.RetrieveQueryColumnList(aAppTransactionDataLoadExDto.DataSetId.Value);
                InitialDbColumnFormulaDisplayName(dbColumnLookupList);
            }



            foreach (var o in aAppTransactionDataLoadEntity.AppTranscationDataLoadFieldMapping)
            {
                AppTranscationDataLoadFieldMappingExDto aAppTranscationDataLoadFieldMappingExDto = AppTranscationDataLoadFieldMappingConverter.ConvertEntityToExDto(OutFormatFormulaExpressForTransactionUnit(transactionFieldList, dbColumnLookupList, o));
                aAppTransactionDataLoadExDto.AppTranscationDataLoadFieldMappingList.Add(aAppTranscationDataLoadFieldMappingExDto);
            }



            return aAppTransactionDataLoadExDto;
        }



        public static ObservableSet<AppTransactionDataLoadDto> RetrieveAllAppTransactionDataLoadDto()
        {
            EntityCollection<AppTransactionDataLoadEntity> list = RetrieveAllTrasactionDataLoadEntity();

            var aDtoList = new ObservableSet<AppTransactionDataLoadDto>();

            foreach (var o in list)
            {
                aDtoList.Add(AppTransactionDataLoadConverter.ConvertEntityToDto(o));
            }

            return aDtoList;
        }




        public static OperationCallResult<AppTransactionDataLoadExDto> SaveAppTransactionDataLoadExDto(AppTransactionDataLoadExDto aAppTransactionDataLoadExDto)
        {
            OperationCallResult<AppTransactionDataLoadExDto> aOperationCallResult = new OperationCallResult<AppTransactionDataLoadExDto>();
            var aValidationResult = aAppTransactionDataLoadExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionDataLoadEntity aAppTransactionDataLoadEntity;

            List<LookupItemDto> dbColumnLookupList = new List<LookupItemDto>();
            List<AppTransactionFieldExDto> transactionFieldList = new List<AppTransactionFieldExDto>();

            if (aAppTransactionDataLoadExDto.TransactionId.HasValue)
            {
                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppTransactionDataLoadExDto.TransactionId.Value);
                transactionFieldList = transactionExDto.DictRootLevelUnitTransactionField.Values.ToList();
                InitialTransactionFieldFormularDisplayName(transactionExDto, transactionFieldList);
            }

            if (aAppTransactionDataLoadExDto.DataSetId.HasValue)
            {
                dbColumnLookupList = AppDataSetBL.RetrieveQueryColumnList(aAppTransactionDataLoadExDto.DataSetId.Value);
                InitialDbColumnFormulaDisplayName(dbColumnLookupList);
            }





            // prepare Data
            if (aAppTransactionDataLoadExDto.IsNew)
            {
                aAppTransactionDataLoadEntity = new AppTransactionDataLoadEntity();
                AppTransactionDataLoadConverter.CopyDtoToEntity(aAppTransactionDataLoadEntity, aAppTransactionDataLoadExDto);

                foreach (var dataLoadFieldMappingDto in aAppTransactionDataLoadExDto.AppTranscationDataLoadFieldMappingList)
                {
                    AppTranscationDataLoadFieldMappingEntity aAppTranscationDataLoadFieldMappingEntity = new AppTranscationDataLoadFieldMappingEntity();
                    AppTranscationDataLoadFieldMappingConverter.CopyDtoToEntity(aAppTranscationDataLoadFieldMappingEntity, InFormatFormulaExepressForTransactionUnit(transactionFieldList, dbColumnLookupList, dataLoadFieldMappingDto));
                    aAppTransactionDataLoadEntity.AppTranscationDataLoadFieldMapping.Add(aAppTranscationDataLoadFieldMappingEntity);
                }





                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppTransactionDataLoadEntity);
                        adapter.Commit();

                        aAppTransactionDataLoadExDto.Id = aAppTransactionDataLoadEntity.DataLoadId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionDataLoadExDto), " App_TranscationDataLoadFieldMappingEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }


                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionDataLoadExDto), " App_TranscationDataLoadFieldMappingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppTransactionDataLoadExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppTransactionDataLoadExDto(aAppTransactionDataLoadExDto, transactionFieldList, dbColumnLookupList));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppTransactionDataLoadExDto(aAppTransactionDataLoadExDto.Id);
            }

            return aOperationCallResult;
        }



        private static ValidationResult ProcessDirtyAppTransactionDataLoadExDto(AppTransactionDataLoadExDto aAppTransactionDataLoadExDto, List<AppTransactionFieldExDto> transactionFieldList, List<LookupItemDto> dbColumnLookupList)
        {
            ValidationResult aValidationResult = new ValidationResult();

            // int[] deleteAppTranscationDataLoadFieldMappingIDs = aAppTransactionDataLoadExDto.AppTranscationDataLoadFieldMappingList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppTransactionDataLoadEntity aAppTransactionDataLoadEntity = RetrieveOneAppTransactionDataLoadEntity(aAppTransactionDataLoadExDto.Id);

            Dictionary<int, AppTranscationDataLoadFieldMappingEntity> dictAppTranscationDataLoadFieldMappingFromDbms = aAppTransactionDataLoadEntity.AppTranscationDataLoadFieldMapping.ToDictionary(o => o.FieldMappingId, o => o);

            AppTransactionDataLoadConverter.CopyDtoToEntity(aAppTransactionDataLoadEntity, aAppTransactionDataLoadExDto);

            // new Items
            foreach (AppTranscationDataLoadFieldMappingDto aChildDto in aAppTransactionDataLoadExDto.AppTranscationDataLoadFieldMappingList.FindNewItems())
            {
                AppTranscationDataLoadFieldMappingEntity aNewChildEntity = new AppTranscationDataLoadFieldMappingEntity();
                AppTranscationDataLoadFieldMappingConverter.CopyDtoToEntity(aNewChildEntity, InFormatFormulaExepressForTransactionUnit(transactionFieldList, dbColumnLookupList, aChildDto));
                aAppTransactionDataLoadEntity.AppTranscationDataLoadFieldMapping.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aAppTransactionDataLoadExDto.AppTranscationDataLoadFieldMappingList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppTranscationDataLoadFieldMappingFromDbms.ContainsKey(dtoKey))
                {
                    AppTranscationDataLoadFieldMappingConverter.CopyDtoToEntity(dictAppTranscationDataLoadFieldMappingFromDbms[dtoKey], InFormatFormulaExepressForTransactionUnit(transactionFieldList, dbColumnLookupList, modifyitem));
                }
            }

            // deletedIDs
            int[] deleteAppTranscationDataLoadFieldMappingIDs = aAppTransactionDataLoadExDto.AppTranscationDataLoadFieldMappingList.FindDeletedItemIds().Cast<int>().ToArray();








            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionDataLoadEntity);

                    // Need to delete SearchTemplate subitems
                    if (deleteAppTranscationDataLoadFieldMappingIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppTranscationDataLoadFieldMappingEntity), new RelationPredicateBucket(AppTranscationDataLoadFieldMappingFields.FieldMappingId == deleteAppTranscationDataLoadFieldMappingIDs));

                    }


                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionDataLoadEntity), " App_TranscationDataLoadFieldMappingEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionDataLoadEntity), " App_TranscationDataLoadFieldMappingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }




        //Delete a AppTransactionDataLoad
        public static OperationCallResult<object> DeleteAppTransactionDataLoad(object dataLaodId)
        {
            OperationCallResult<object> aValidationResult = new OperationCallResult<object>();

            string referMsg = string.Empty;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppTranscationDataLoadFieldMappingEntity), new RelationPredicateBucket(AppTranscationDataLoadFieldMappingFields.DataLoadId == dataLaodId));
                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionDataLoadEntity), new RelationPredicateBucket(AppTransactionDataLoadFields.DataLoadId == dataLaodId));
                    adapter.Commit();
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionDataLoadEntity), " App_TranscationDataLoadFieldMappingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }

                // if no any errors
                if (!aValidationResult.ValidationResult.HasErrors)
                {
                    aValidationResult.Object = dataLaodId;
                }
            }
            return aValidationResult;
        }

        public static List<LookupItemDto> RetrieveBLQueryColumnList(int dataSetId)
        {
            List<LookupItemDto> list = new List<LookupItemDto>();

			AppDataSetEntity aEntity = new AppDataSetEntity(dataSetId);

			using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                adpater.FetchEntity(aEntity);
						
            }


            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(aEntity.DataSourceFrom.Value);
			var dictColumNameDataType = dataBaseFixture.GetQuerySchemeColumnNameDataType(aEntity.QueryText);


			foreach (var pair in dictColumNameDataType)
			{
				LookupItemDto aLookupItemDto = new LookupItemDto();
				aLookupItemDto.Id = pair.Key;
				aLookupItemDto.Display = pair.Value;
				list.Add(aLookupItemDto);
			}
			return list;

			// throw new NotImplementedException();
		}

        public static OperationCallResult<object> SaveAllAppTransactionDataLoadDto(ObservableSet<AppTransactionDataLoadDto> appTransactionDataLoadDtoSet)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            foreach (var ItemDto in appTransactionDataLoadDtoSet)
            {
                validationResult.Merge(ItemDto.ValidateDto());
            }

            if (validationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = validationResult;
                return aOperationCallResult;
            }

            appTransactionDataLoadDtoSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionDataLoadExDto), "App_AppTransactionDataLoad_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }

        private static ValidationResult ProcessDirtyDto(AppTransactionDataLoadDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTransactionDataLoadEntity aAppTransactionDataLoadEntity = RetrieveOneAppTransactionDataLoadEntity(aDto.Id);

            AppTransactionDataLoadConverter.CopyDtoToEntity(aAppTransactionDataLoadEntity, aDto);

            // bug test
            // aDto.Id = 100;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionDataLoadEntity, false, true);
                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionDataLoadExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionDataLoadExDto), "App_AppTransactionDataLoadEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }



        // Where Condition Help Methods:
        public static AppTranscationDataLoadFieldMappingDto InFormatFormulaExepressForTransactionUnit(List<AppTransactionFieldExDto> transactionFieldList, List<LookupItemDto> dbColumnLookupList, AppTranscationDataLoadFieldMappingDto aDataLoadFieldMappingDto)
        {
            if (!string.IsNullOrEmpty(aDataLoadFieldMappingDto.WhereClause))
            {
                string expression = aDataLoadFieldMappingDto.WhereClause;
                MatchCollection matchList = FormulaRegex.Matches(aDataLoadFieldMappingDto.WhereClause);
                for (int i = 0; i < matchList.Count; i++)
                {
                    string fieldFormularDisplayName = matchList[i].Value.ToString().Trim();

                    AppTransactionFieldExDto aAppTransactionFieldDto = transactionFieldList.FirstOrDefault(o => o.FormulaDisplayName.ToLower().Trim() == fieldFormularDisplayName.ToLower().Trim());
                    LookupItemDto dbColumnLookup = dbColumnLookupList.FirstOrDefault(o => o.Display.ToLower().Trim() == fieldFormularDisplayName.ToLower().Trim());

                    if (aAppTransactionFieldDto != null)
                    {
                        expression = expression.Replace(fieldFormularDisplayName, TransactionFieldFormulaPrefix + aAppTransactionFieldDto.Id.ToString());
                    }
                    else if (dbColumnLookup != null)
                    {
                        expression = expression.Replace(fieldFormularDisplayName, dbColumnLookup.Id.ToString());
                    }

                }
                aDataLoadFieldMappingDto.WhereClause = expression;
            }
            return aDataLoadFieldMappingDto;
        }

        public static AppTranscationDataLoadFieldMappingEntity OutFormatFormulaExpressForTransactionUnit(List<AppTransactionFieldExDto> transactionFieldList, List<LookupItemDto> dbColumnLookupList, AppTranscationDataLoadFieldMappingEntity aDataLoadFieldMappingEntity)
        {
            if (!string.IsNullOrEmpty(aDataLoadFieldMappingEntity.WhereClause))
            {

                string expression = aDataLoadFieldMappingEntity.WhereClause;
                var members = aDataLoadFieldMappingEntity.WhereClause.Split(ConditionConstString, StringSplitOptions.RemoveEmptyEntries);
                foreach (string info in members)
                {
                    if (info.Trim().StartsWith(TransactionFieldFormulaPrefix))
                    {
                        string id = info.Replace(TransactionFieldFormulaPrefix, "").Trim();

                        AppTransactionFieldExDto aAppTransactionFieldDto = transactionFieldList.FirstOrDefault(o => object.Equals(o.Id.ToString().Trim(), id.Trim()));
                        if (aAppTransactionFieldDto != null)
                        {
                            expression = expression.Replace(info, aAppTransactionFieldDto.FormulaDisplayName);
                        }
                    }
                    else
                    {
                        string dbColumnName = info.Trim();

                        LookupItemDto dbColumnLookup = dbColumnLookupList.FirstOrDefault(o => object.Equals(o.Id.ToString().Trim(), dbColumnName.Trim()));
                        if (dbColumnLookup != null)
                        {
                            expression = expression.Replace(info, dbColumnLookup.Display);
                        }
                    }
                }
                aDataLoadFieldMappingEntity.WhereClause = expression;
            }
            return aDataLoadFieldMappingEntity;
        }

        private static void InitialTransactionFieldFormularDisplayName(AppTransactionExDto transactionExDto, List<AppTransactionFieldExDto> transactionFieldList)
        {
            transactionFieldList.ForAll(o =>
            {
                var aUnit = transactionExDto.DictAllTransactionUnitIdExDto[o.TransactionUnitId.ToString()];
                if (aUnit.IsMasterSiblingUnit.HasValue && aUnit.IsMasterSiblingUnit.Value)
                {
                    o.FormulaDisplayName = "[" + TransactionFieldUIPrefix + aUnit.DataBaseTableName + "." + o.DataBaseFieldName + "]";
                }
                else
                {
                    o.FormulaDisplayName = "[" + TransactionFieldUIPrefix + o.DataBaseFieldName + "]";
                }

            });
        }

        private static void InitialDbColumnFormulaDisplayName(List<LookupItemDto> dbColumnLookupList)
        {
            foreach (LookupItemDto dbColumn in dbColumnLookupList)
            {
                dbColumn.Display = "[" + DatabaseColumnUIPrefix + dbColumn.Id + "]";
            }
        }
    }



}