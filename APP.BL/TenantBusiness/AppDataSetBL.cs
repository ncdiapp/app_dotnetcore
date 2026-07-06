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
using System.Text;
#if NETFRAMEWORK
using System.Xml.Serialization.Configuration;
using Microsoft.SqlServer.ReportingServices2010;
#endif
using System.Dynamic;

using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;

using APP.Framework;
namespace App.BL
{
    public static class AppDataSetBL
    {


        public static EntityCollection<AppDataSetEntity> RetrieveAllAppDataSetEntity()
        {

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppDataSetEntity> list = new EntityCollection<AppDataSetEntity>();
                //SortClause aSortClause = AppDataSetFields.Name | SortOperator.Ascending;
                //adapter.FetchEntityCollection(list, null, 0, new SortExpression(aSortClause), null);

                RelationPredicateBucket filter = new RelationPredicateBucket(AppDataSetFields.UsageTypeId == DBNull.Value | AppDataSetFields.UsageTypeId == (int)EmAppDataSetUsageType.Default | AppDataSetFields.UsageTypeId == (int)EmAppDataSetUsageType.ConvertSimpleObjectToList);

                adapter.FetchEntityCollection(list, filter, 0);
                return list;
            }
        }

        public static EntityCollection<AppSearchViewEntity> GetAllSearchViewForOneDataSet(int dataSetId)
        {
            EntityCollection<AppSearchViewEntity> aListEntity = new EntityCollection<AppSearchViewEntity>();

            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                List<int> dataSetIds = RetrieveChildDataSetIDListByBaseDataSetId(dataSetId);

                dataSetIds.Add(dataSetId);


                RelationPredicateBucket filter = new RelationPredicateBucket(AppSearchViewFields.DataSetId == dataSetIds.ToArray());
                adpater.FetchEntityCollection(aListEntity, filter);



                //return list;
            }

            return aListEntity;
        }

        internal static List<int> RetrieveChildDataSetIDListByBaseDataSetId(int? baseDataSetId)
        {
            List<int> childDataSetIds = new List<int>();

            if (baseDataSetId.HasValue)
            {
                using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
                {


                    EntityCollection<AppDataSetEntity> aListEntity = new EntityCollection<AppDataSetEntity>();
                    RelationPredicateBucket filter = new RelationPredicateBucket(AppDataSetFields.BaseDataSetId == baseDataSetId);
                    adpater.FetchEntityCollection(aListEntity, filter);


                    foreach (var entity in aListEntity)
                    {
                        childDataSetIds.Add(entity.DataSetId);
                    }


                }
            }

            return childDataSetIds.Distinct().ToList();
        }

           //  test Result string dynColumnss = @"
            //max( (case when UnitExtendFiledID = 32392 then ValueText end ) ) as  c1,
            //max( (case when UnitExtendFiledID = 32393 then ValueText end ) ) as  c2,
            // max( (case when UnitExtendFiledID = 32394 then ValueText end ) ) as  c3,
            //max( (case when UnitExtendFiledID = 32395 then ValueText end ) )as  c4,
            //max( ( case when UnitExtendFiledID = 32396 then ValueText end ) ) as  c5";
        public static  string  GetTransactionRootDatesetQuery(int? transactionId)
        {
            string toRetrun = "";
            AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(aAppTransactionExDto.DataSourceFrom.Value );

            string tableName = aAppTransactionExDto.RootMasterUnit.DataBaseTableName;

            DatabaseTable databaseTable = AppCacheManagerBL.GetDatabaseTable(aAppTransactionExDto.RootMasterUnit);
            //  DatabaseTable rootDatabaseTable = null;
            SqlWriter sqlWriter = new SqlWriter(databaseTable, databaseFixtureInstance.SqlServerType.Value);

            //List<SqlParameter> listParamters = new List<SqlParameter>();

            string rootSelectall = sqlWriter.SelectAllSql () ;
            string pkColumnName = sqlWriter.PrimaryKeys[0];

            var dictExtendNameId = aAppTransactionExDto.RootMasterUnit.DictExtenId_DbField;
            string dynColumns = "";
            if (! dictExtendNameId.IsEmpty ())
            {
               
                foreach( var keyValuepau in dictExtendNameId)
                {
                    dynColumns += $@" max((case when UnitExtendFiledID = {keyValuepau.Key} then ValueText end) ) as {keyValuepau.Value}{System.Environment.NewLine } ,";
                }
            }

            if(!string.IsNullOrEmpty(dynColumns))
            {
                dynColumns = dynColumns.Substring(0, dynColumns.Length - 1);

                toRetrun = $@" {rootSelectall} left join 

                             ( SELECT  UnitPKValue, {dynColumns}
                               FROM Apptransactionunitextendfieldvalue
  
                                where TransactionUnitID={aAppTransactionExDto.RootMasterUnit.Id} 
                                group by UnitPKValue
                             ) as p 
                             on p.UnitPKValue= {tableName}.{pkColumnName} ";
            }
            else
            {
                toRetrun = rootSelectall;
            }
         

           



            return toRetrun;
           
        }

        public static string GenerateQueryFromDataModel(int dataSetId)
        {
            AppDataSetExDto dataSetExDto = RetrieveOneAppDataSetExDto(dataSetId);
            if (dataSetExDto == null)
            {
                return string.Empty;
            }

            int? transactionId = ResolveTransactionIdFromDataSet(dataSetExDto);
            if (transactionId.HasValue)
            {
                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
                if (transactionExDto?.RootMasterUnit != null
                    && transactionExDto.TransactionOrganizedType.HasValue
                    && transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                {
                    return BuildTransactionMasterSiblingQuery(transactionExDto);
                }
            }

            return GenerateQueryFromDataSetRootTableOnly(dataSetExDto);
        }

        private static int? ResolveTransactionIdFromDataSet(AppDataSetExDto dataSetExDto)
        {
            if (dataSetExDto?.Id == null)
            {
                return null;
            }

            int dataSetId = (int)dataSetExDto.Id;

            int? transactionIdFromDataLoad = ResolveTransactionIdFromDataLoad(dataSetId);
            if (transactionIdFromDataLoad.HasValue)
            {
                return transactionIdFromDataLoad;
            }

            int? transactionIdFromFolderNav = ResolveTransactionIdFromFolderNavigation(dataSetId);
            if (transactionIdFromFolderNav.HasValue)
            {
                return transactionIdFromFolderNav;
            }

            HashSet<string> tableNames = ExtractTableNamesFromDataSet(dataSetExDto);
            if (tableNames.Count == 0)
            {
                return ResolveTransactionIdByNameHint(dataSetExDto.Name, null);
            }

            List<AppTransactionDto> masterDetailTransactions = AppTransactionBL.RetrieveAllAppTransactionDto(null, (int)EmTransactionOrganizedType.MasterDetail);
            List<int> matchedTransactionIds = new List<int>();

            foreach (AppTransactionDto transactionDto in masterDetailTransactions)
            {
                if (transactionDto.Id == null)
                {
                    continue;
                }

                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionDto.Id);
                if (transactionExDto?.RootMasterUnit == null)
                {
                    continue;
                }

                if (tableNames.Contains(transactionExDto.RootMasterUnit.DataBaseTableName))
                {
                    matchedTransactionIds.Add((int)transactionDto.Id);
                    continue;
                }

                bool siblingMatched = transactionExDto.AppTransactionUnitList != null
                    && transactionExDto.AppTransactionUnitList.Any(unit =>
                        unit.IsMasterSiblingUnit.HasValue
                        && unit.IsMasterSiblingUnit.Value
                        && tableNames.Contains(unit.DataBaseTableName));

                if (siblingMatched)
                {
                    matchedTransactionIds.Add((int)transactionDto.Id);
                }
            }

            matchedTransactionIds = matchedTransactionIds.Distinct().ToList();
            if (matchedTransactionIds.Count == 1)
            {
                return matchedTransactionIds[0];
            }

            int? transactionIdByName = ResolveTransactionIdByNameHint(dataSetExDto.Name, matchedTransactionIds);
            if (transactionIdByName.HasValue)
            {
                return transactionIdByName;
            }

            int siblingTableMatchId = matchedTransactionIds.FirstOrDefault(candidateTransactionId =>
            {
                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(candidateTransactionId);
                return transactionExDto?.AppTransactionUnitList?.Any(unit =>
                    unit.IsMasterSiblingUnit.HasValue
                    && unit.IsMasterSiblingUnit.Value
                    && tableNames.Contains(unit.DataBaseTableName)) == true;
            });

            if (siblingTableMatchId > 0)
            {
                return siblingTableMatchId;
            }

            return null;
        }

        private static int? ResolveTransactionIdFromDataLoad(int dataSetId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionDataLoadEntity> loadEntities = new EntityCollection<AppTransactionDataLoadEntity>();
                adapter.FetchEntityCollection(loadEntities, new RelationPredicateBucket(AppTransactionDataLoadFields.DataSetId == dataSetId));
                if (loadEntities.Count >= 1)
                {
                    return loadEntities[0].TransactionId;
                }
            }

            return null;
        }

        private static int? ResolveTransactionIdFromFolderNavigation(int dataSetId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSearchViewEntity> searchViewEntities = new EntityCollection<AppSearchViewEntity>();
                adapter.FetchEntityCollection(searchViewEntities, new RelationPredicateBucket(AppSearchViewFields.DataSetId == dataSetId));
                if (searchViewEntities.Count == 0)
                {
                    return null;
                }

                HashSet<int> transactionIds = new HashSet<int>();
                foreach (AppSearchViewEntity searchViewEntity in searchViewEntities)
                {
                    EntityCollection<AppTransactionNavigationEntity> navigationEntities = new EntityCollection<AppTransactionNavigationEntity>();
                    IRelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionNavigationFields.FolderViewId == searchViewEntity.SearchViewId);
                    filter.PredicateExpression.AddWithAnd(AppTransactionNavigationFields.QuickSearchId == DBNull.Value);
                    adapter.FetchEntityCollection(navigationEntities, filter);

                    foreach (AppTransactionNavigationEntity navigationEntity in navigationEntities)
                    {
                        if (navigationEntity.TransactionId.HasValue)
                        {
                            transactionIds.Add(navigationEntity.TransactionId.Value);
                        }
                    }
                }

                if (transactionIds.Count == 1)
                {
                    return transactionIds.First();
                }
            }

            return null;
        }

        private static int? ResolveTransactionIdByNameHint(string nameHint, List<int> candidateTransactionIds)
        {
            if (string.IsNullOrWhiteSpace(nameHint))
            {
                return null;
            }

            string normalizedHint = NormalizeModelName(nameHint);
            if (string.IsNullOrWhiteSpace(normalizedHint))
            {
                return null;
            }

            List<AppTransactionDto> masterDetailTransactions = AppTransactionBL.RetrieveAllAppTransactionDto(null, (int)EmTransactionOrganizedType.MasterDetail);
            IEnumerable<AppTransactionDto> transactionsToScore = masterDetailTransactions;
            if (candidateTransactionIds != null && candidateTransactionIds.Count > 0)
            {
                HashSet<int> candidateSet = new HashSet<int>(candidateTransactionIds);
                transactionsToScore = masterDetailTransactions.Where(transactionDto =>
                    transactionDto.Id != null && candidateSet.Contains((int)transactionDto.Id));
            }

            int? bestTransactionId = null;
            int bestScore = 0;
            foreach (AppTransactionDto transactionDto in transactionsToScore)
            {
                if (transactionDto.Id == null)
                {
                    continue;
                }

                string normalizedTransactionName = NormalizeModelName(transactionDto.TransactionName);
                if (string.IsNullOrWhiteSpace(normalizedTransactionName))
                {
                    continue;
                }

                int score = 0;
                if (string.Equals(normalizedHint, normalizedTransactionName, StringComparison.OrdinalIgnoreCase))
                {
                    score = 100;
                }
                else if (normalizedHint.IndexOf(normalizedTransactionName, StringComparison.OrdinalIgnoreCase) >= 0
                    || normalizedTransactionName.IndexOf(normalizedHint, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score = 50;
                }
                else if (string.Equals(
                    normalizedHint.TrimEnd('s'),
                    normalizedTransactionName.TrimEnd('s'),
                    StringComparison.OrdinalIgnoreCase))
                {
                    score = 80;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTransactionId = (int)transactionDto.Id;
                }
            }

            return bestScore > 0 ? bestTransactionId : null;
        }

        private static string NormalizeModelName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            string normalized = Regex.Replace(name, @"^\d+\s*-\s*", string.Empty);
            normalized = Regex.Replace(normalized, @"\s+references?\s*$", string.Empty, RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, @"[\s_\-]+", string.Empty, RegexOptions.IgnoreCase);
            return normalized.ToLowerInvariant();
        }

        private static string GenerateQueryFromDataSetRootTableOnly(AppDataSetExDto dataSetExDto)
        {
            List<LookupItemDto> colList = RetrieveDataSetQueryColumnList(dataSetExDto);
            string selectCols = colList.Count > 0
                ? string.Join(",\n", colList
                    .Select(o => o?.Id?.ToString())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => $"[{name}]"))
                : "*";

            string fromClause = ResolveDataSetFromClause(dataSetExDto);
            return $"SELECT\n{selectCols}\nFROM {fromClause}";
        }

        private static string ResolveDataSetFromClause(AppDataSetExDto dataSetExDto)
        {
            if (!string.IsNullOrWhiteSpace(dataSetExDto.QueryText))
            {
                Match fromMatch = Regex.Match(dataSetExDto.QueryText, @"FROM\s+(\[[^\]]+\](?:\.\[[^\]]+\])?|\S+)", RegexOptions.IgnoreCase);
                if (fromMatch.Success)
                {
                    return fromMatch.Groups[1].Value.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(dataSetExDto.BaseTableName))
            {
                return dataSetExDto.BaseTableName.Contains("[")
                    ? dataSetExDto.BaseTableName
                    : $"[{dataSetExDto.BaseTableName}]";
            }

            return "[Table]";
        }

        private static HashSet<string> ExtractTableNamesFromDataSet(AppDataSetExDto dataSetExDto)
        {
            HashSet<string> tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(dataSetExDto.BaseTableName))
            {
                tableNames.Add(StripSqlIdentifierBrackets(dataSetExDto.BaseTableName));
            }

            if (!string.IsNullOrWhiteSpace(dataSetExDto.QueryText))
            {
                foreach (Match match in Regex.Matches(dataSetExDto.QueryText, @"(?:FROM|JOIN)\s+((?:\[[^\]]+\]\.?)?(?:\[[^\]]+\]|\w+))", RegexOptions.IgnoreCase))
                {
                    tableNames.Add(StripSqlIdentifierBrackets(match.Groups[1].Value));
                }
            }

            if (dataSetExDto.UsageTypeId.HasValue
                && dataSetExDto.UsageTypeId.Value == (int)EmAppDataSetUsageType.DatabaseTable
                && !string.IsNullOrWhiteSpace(dataSetExDto.Name))
            {
                tableNames.Add(dataSetExDto.Name.Trim());
            }

            return tableNames;
        }

        private static string StripSqlIdentifierBrackets(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return identifier;
            }

            string value = identifier.Trim();
            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                value = value.Substring(1, value.Length - 2);
            }

            int dotIndex = value.LastIndexOf('.');
            if (dotIndex >= 0 && dotIndex < value.Length - 1)
            {
                value = value.Substring(dotIndex + 1).Trim();
                if (value.StartsWith("[") && value.EndsWith("]"))
                {
                    value = value.Substring(1, value.Length - 2);
                }
            }

            return value;
        }

        private static string BuildTransactionMasterSiblingQuery(AppTransactionExDto transactionExDto)
        {
            AppTransactionUnitExDto rootUnit = transactionExDto.RootMasterUnit;
            AppTransactionFieldExDto rootPrimaryKeyField = rootUnit.AppTransactionFieldList.FirstOrDefault(field => field.IsPrimaryKey);
            if (rootPrimaryKeyField == null || string.IsNullOrWhiteSpace(rootPrimaryKeyField.DataBaseFieldName))
            {
                return string.Empty;
            }

            DatabaseFixture databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(transactionExDto.DataSourceFrom.Value);
            EmSqlType sqlType = databaseFixture.SqlServerType.Value;
            string rootTableName = rootUnit.DataBaseTableName;

            List<string> selectColumns = new List<string>();
            foreach (AppTransactionFieldExDto field in rootUnit.AppTransactionFieldList.OrderBy(field => field.SortOrder))
            {
                if (IsPersistedTransactionField(field))
                {
                    selectColumns.Add(FormatSelectColumn(rootTableName, field.DataBaseFieldName));
                }
            }

            List<string> joinClauses = new List<string>();
            IEnumerable<AppTransactionUnitExDto> siblingUnits = transactionExDto.AppTransactionUnitList
                ?.Where(unit => unit.IsMasterSiblingUnit.HasValue && unit.IsMasterSiblingUnit.Value)
                ?? Enumerable.Empty<AppTransactionUnitExDto>();

            foreach (AppTransactionUnitExDto siblingUnit in siblingUnits)
            {
                AppTransactionFieldExDto linkField = siblingUnit.AppTransactionFieldList
                    .FirstOrDefault(field => field.IsLinkToParentPrimaryKey && !string.IsNullOrWhiteSpace(field.DataBaseFieldName));

                if (linkField == null)
                {
                    continue;
                }

                string siblingTableName = siblingUnit.DataBaseTableName;
                foreach (AppTransactionFieldExDto field in siblingUnit.AppTransactionFieldList.OrderBy(field => field.SortOrder))
                {
                    if (!IsPersistedTransactionField(field))
                    {
                        continue;
                    }

                    if (field.IsLinkToParentPrimaryKey
                        || string.Equals(field.DataBaseFieldName, linkField.DataBaseFieldName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(field.DataBaseFieldName, rootPrimaryKeyField.DataBaseFieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    selectColumns.Add(FormatSelectColumn(siblingTableName, field.DataBaseFieldName));
                }

                string siblingTableQualified = AppMetaDataBL.GetQulifiedTableName(siblingUnit.SchemaOwner, siblingTableName, sqlType);
                joinClauses.Add(string.Format(
                    "inner join {0} on [{1}].{2} = [{3}].{4}",
                    siblingTableQualified,
                    rootTableName,
                    rootPrimaryKeyField.DataBaseFieldName,
                    siblingTableName,
                    linkField.DataBaseFieldName));
            }

            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.AppendLine("SELECT");
            queryBuilder.AppendLine(string.Join(",\n", selectColumns));
            queryBuilder.Append("FROM ");
            queryBuilder.AppendLine(AppMetaDataBL.GetQulifiedTableName(rootUnit.SchemaOwner, rootTableName, sqlType));
            foreach (string joinClause in joinClauses)
            {
                queryBuilder.AppendLine(joinClause);
            }

            return queryBuilder.ToString().TrimEnd();
        }

        private static bool IsPersistedTransactionField(AppTransactionFieldExDto field)
        {
            return field != null
                && !string.IsNullOrWhiteSpace(field.DataBaseFieldName)
                && !(field.IsTempVariable.HasValue && field.IsTempVariable.Value)
                && !(field.IsStoreToExtendTable.HasValue && field.IsStoreToExtendTable.Value);
        }

        private static string FormatSelectColumn(string tableName, string columnName)
        {
            return $"[{tableName}].{columnName}";
        }

        public static List<LookupItemDto> RetrieveQueryColumnList(int dataSetId)
        {

            var dataSetExDto = RetrieveOneAppDataSetExDto(dataSetId);
            return RetrieveDataSetQueryColumnList(dataSetExDto);


            // throw new NotImplementedException();
        }

        public static List<LookupItemDto> RetrieveDataSetQueryColumnList(AppDataSetExDto dataSetExDto)
        {
            List<LookupItemDto> list = new List<LookupItemDto>();
            List<string> childVewFiledList = null;

            if (dataSetExDto.Id != null && dataSetExDto.BaseDataSetId.HasValue)
            {
                dataSetExDto = RetrieveOneAppDataSetExDto(dataSetExDto.BaseDataSetId);

                var childVewnEntity = AppDataSetExtractViewConfigBL.RetrieveOneAppDataSetEntity(dataSetExDto.Id);
                childVewFiledList = childVewnEntity.AppDateSetDataExtractView.Select(o => o.DbfiledName).ToList();
            }





            if (dataSetExDto.QueryType.HasValue && dataSetExDto.QueryType.Value == (int)EmAppDataServiceType.QueryText)
            {
                var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSetExDto.DataSourceFrom.Value);
                var dictColumNameDataType = dataBaseFixture.GetQuerySchemeColumnNameDataType(dataSetExDto.QueryText);


                foreach (var pair in dictColumNameDataType)
                {
                    LookupItemDto aLookupItemDto = new LookupItemDto();
                    aLookupItemDto.Id = pair.Key;
                    aLookupItemDto.Display = pair.Value;
                    list.Add(aLookupItemDto);
                }

            }
            else if (dataSetExDto.QueryType.HasValue && dataSetExDto.QueryType.Value == (int)EmAppDataServiceType.StoredProcedure)
            {

                DataTable dt = CallDataSetStoreProcedure(dataSetExDto);

                foreach (DataColumn pair in dt.Columns)
                {
                    LookupItemDto aLookupItemDto = new LookupItemDto();
                    aLookupItemDto.Id = pair.ColumnName;
                    aLookupItemDto.Display = pair.DataType.Name;
                    list.Add(aLookupItemDto);
                }


            }
            else if (dataSetExDto.QueryType.HasValue && dataSetExDto.QueryType.Value == (int)EmAppDataServiceType.PluginWebApiCall)
            {
                foreach (var datasetParameter in dataSetExDto.AppDataSetParameterList)
                {
                    LookupItemDto aLookupItemDto = new LookupItemDto();
                    aLookupItemDto.Id = datasetParameter.ParameterName;
                    aLookupItemDto.Display = datasetParameter.ParameterName + " (Parameter)";
                    aLookupItemDto.ItemType = 1; // Input
                    list.Add(aLookupItemDto);
                }

                if (!string.IsNullOrWhiteSpace(dataSetExDto.QueryText))
                {
                    list.AddRange(AppPluginClient.GetAppPluginClientMethodResultColumns(dataSetExDto.QueryText));
                }
            }
            else if (dataSetExDto.QueryType.HasValue && dataSetExDto.QueryType.Value == (int)EmAppDataServiceType.IntegrationWebApiCall)
            {
                BuildIntegrationWebApiCallAvailableFields(list, dataSetExDto);
            }

            if (!childVewFiledList.IsEmpty())
            {
                list = list.Where(o => childVewFiledList.Contains(o.Id.ToString())).ToList();
            }

            return list;
        }

        public static AppDataSetEntity RetrieveOneAppDataSetEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppDataSetEntity aAppDataSetEntity = new AppDataSetEntity(int.Parse(Id.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppDataSetEntity);

                rootPath.Add(AppDataSetEntity.PrefetchPathAppDataSetParameter);


                adapter.FetchEntity(aAppDataSetEntity, rootPath);
                return aAppDataSetEntity;
            }
        }

        public static ObservableSet<AppDataSetExDto> RetrieveAllAppDataSetEntityDto()
        {
            ObservableSet<AppDataSetExDto> aSet = new ObservableSet<AppDataSetExDto>();
            EntityCollection<AppDataSetEntity> list = RetrieveAllAppDataSetEntity();
            foreach (var o in list)
            {
                AppDataSetExDto aDto = AppDataSetConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        public static List<AppDataSetExDto> RetrieveSaasApplicationDataSetList(int? applicationId)
        {
            List<AppDataSetExDto> toReturn = new List<AppDataSetExDto>();

            if (applicationId.HasValue)
            {
                List<AppDataSetExDto> allDataSetList = RetrieveAllAppDataSetEntityDto().ToList();

                List<int> appliationDataSetIdList = AppSearchConfigBL.RetrieveSaasApplicationSearchList(applicationId).Where(o => o.DataSetId.HasValue).Select(o => o.DataSetId.Value).Distinct().ToList();

                toReturn = allDataSetList.Where(o => (o.SaasApplicationId.HasValue && o.SaasApplicationId.Value == applicationId.Value) || appliationDataSetIdList.Contains((int)o.Id)).ToList();

            }

            return toReturn;
        }


        public static AppDataSetExDto RetrieveOneAppDataSetExDto(object Id, bool isGetDbDiagram = false)
        {
            AppDataSetEntity aAppDataSetEntity = RetrieveOneAppDataSetEntity(Id);
            AppDataSetExDto aAppDataSetExDto = AppDataSetConverter.ConvertEntityToExDto(aAppDataSetEntity);
            aAppDataSetExDto.OtherSettings = "";


            if (!isGetDbDiagram)
            {
                if (aAppDataSetExDto.OtherSettingsDto != null)
                {
                    aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo = null;
                }
            }

            foreach (var parameterentity in aAppDataSetEntity.AppDataSetParameter)
            {

                aAppDataSetExDto.AppDataSetParameterList.Add(AppDataSetParameterConverter.ConvertEntityToExDto(parameterentity));

            }

            if (aAppDataSetExDto.QueryType == (int)EmAppDataServiceType.IntegrationWebApiCall)
            {
                var apiOperationDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(aAppDataSetExDto.QueryText);

                if (apiOperationDto != null && !string.IsNullOrWhiteSpace(apiOperationDto.JsonSchema))
                {
                    PrepareDataSetApiDataStructure(aAppDataSetExDto, apiOperationDto);




                    PrepareApiAvailableFetchDataNodeStructure(aAppDataSetExDto, apiOperationDto);
                }

            }

            return aAppDataSetExDto;
        }


        public static OperationCallResult<AppDataSetExDto> SaveAllAppDataSetEntityDto(ObservableSet<AppDataSetExDto> aSet)
        {
            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            foreach (var newItemDto in aSet.FindNewItems())
            {
                var result = ProcessNewDto(newItemDto);
                validationResult.Merge(result);
            }

            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(DeleteOneAppDataSetEntity(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveAllAppDataSetEntityDto();
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppDataSetExDto> SaveOneAppDataSetEntityDto(AppDataSetExDto aAppDataSetExDto)
        {
            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();

            var aValidationResult = aAppDataSetExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            Dictionary<string, int> dictDbObjNameAndUsageType = new Dictionary<string, int>();

            if (aAppDataSetExDto.QueryType.HasValue && aAppDataSetExDto.QueryType.Value == (int)EmAppDataServiceType.QueryText)
            {
                string queryText = aAppDataSetExDto.QueryText;

                if (string.IsNullOrWhiteSpace(queryText))
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_InValidQueryTex_Error", ValidationItemType.Error, " Empty Query Text"));
                    if (validationResult.HasErrors)
                    {
                        return aOperationCallResult;
                    }
                }


                var dbFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);

                string errorMessage = dbFixture.ValidateQueryText(queryText);



                if (string.IsNullOrEmpty(errorMessage))
                {
                    if (aAppDataSetExDto.SaasApplicationId.HasValue)
                    {
                        var databaseViewDto = AppDatabaseViewBL.ConvertQueryToViewDto(queryText, aAppDataSetExDto.DataSourceFrom.Value, null);
                        if (databaseViewDto.DictTables != null && databaseViewDto.DictTables.Count > 0)
                        {
                            foreach (string key in databaseViewDto.DictTables.Keys)
                            {
                                var dbObj = databaseViewDto.DictTables[key];

                                int? usageType = (int)EmAppDataSetUsageType.DatabaseTable;

                                //if (dbObj.TableType == DatabaseViewTableType.Table)
                                //{
                                //    usageType = (int)EmAppDataSetUsageType.DatabaseTable;
                                //}
                                //else if (dbObj.TableType == DatabaseViewTableType.View)
                                //{
                                //    usageType = (int)EmAppDataSetUsageType.DatabaseView;
                                //}

                                //if (usageType.HasValue)
                                //{
                                string objName = key.ToLower().Trim();
                                if (!string.IsNullOrWhiteSpace(objName) && !dictDbObjNameAndUsageType.ContainsKey(objName))
                                {
                                    dictDbObjNameAndUsageType.Add(objName, usageType.Value);
                                }
                                //}
                            }
                        }
                    }
                }
                else
                {

                    validationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_InValidQueryTex_Error", ValidationItemType.Error, errorMessage));
                    if (validationResult.HasErrors)
                    {
                        return aOperationCallResult;
                    }
                }







            }
            else if (aAppDataSetExDto.QueryType.HasValue && aAppDataSetExDto.QueryType.Value == (int)EmAppDataServiceType.StoredProcedure)
            {

                try
                {
                    CallDataSetStoreProcedure(aAppDataSetExDto);

                    if (aAppDataSetExDto.SaasApplicationId.HasValue)
                    {
                        string spName = aAppDataSetExDto.QueryText.Trim().ToLower();

                        if (!string.IsNullOrWhiteSpace(spName))
                        {
                            dictDbObjNameAndUsageType.Add(spName, (int)EmAppDataSetUsageType.DatabaseStoredProcedure);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = StringLocalizer.Localize("App_InvalidQueryText", "Invalid Query Text");
                    if (!string.IsNullOrWhiteSpace(ex.Message))
                    {
                        errorMessage = ex.Message;
                    }

                    validationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_InValidQueryTex_Error", ValidationItemType.Error, errorMessage));
                    if (validationResult.HasErrors)
                    {
                        return aOperationCallResult;
                    }
                }


            }
            else if (aAppDataSetExDto.QueryType.HasValue && aAppDataSetExDto.QueryType.Value == (int)EmAppDataServiceType.PluginWebApiCall)
            {

            }
            else if (aAppDataSetExDto.QueryType.HasValue && aAppDataSetExDto.QueryType.Value == (int)EmAppDataServiceType.IntegrationWebApiCall)
            {
                InitIntegrationApiDefaultRootJsonNodePath(aAppDataSetExDto);
            }


            var fxiture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);

            if (fxiture.SqlServerType == DatabaseSchemaMrg.DataSchema.EmSqlType.MySql)
            {
                aAppDataSetExDto.QueryText = aAppDataSetExDto.QueryText.Replace($@"[{fxiture.CurrentOwner}].", " ");
                aAppDataSetExDto.QueryText = aAppDataSetExDto.QueryText.Replace($@"`{fxiture.CurrentOwner}`.", " ");
            }


            if (aAppDataSetExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(aAppDataSetExDto));
            }
            else if (aAppDataSetExDto.IsModified)
            {
                validationResult.Merge(ProcessDirtyDto(aAppDataSetExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                // var entity = AppDataSetBL.RetrieveOneAppDataSetEntity(aAppDataSetExDto.Id);
                aOperationCallResult.Object = RetrieveOneAppDataSetExDto(aAppDataSetExDto.Id);



                if (dictDbObjNameAndUsageType.Count > 0 && aAppDataSetExDto.SaasApplicationId.HasValue)
                {
                    AddDatabaseObjectsToApplication(dictDbObjNameAndUsageType, aAppDataSetExDto.SaasApplicationId.Value);
                }
            }

            return aOperationCallResult;
        }

        //
        public static DataTable CallDataSetStoreProcedure(AppDataSetExDto aAppDataSetExDto)
        {
            var dbFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);

            string queryStorcname = aAppDataSetExDto.QueryText;
            List<DbParameter> sqlParamterList = new List<DbParameter>();

            foreach (var paramterSet in aAppDataSetExDto.AppDataSetParameterList)
            {
                var aSqlParameter = dbFixture.CreateParameter(paramterSet.ParameterName);
                // aSqlParameter.IsNullable = true;
                //	aSqlParameter.ParameterName = paramterSet.ParameterName;
                // need to convert?
                aSqlParameter.Value = paramterSet.SqlParameterValue;
                sqlParamterList.Add(aSqlParameter);

            }



            //
            //string conenctInfo = AppMetaDataBL.GetConnectInfo(aAppDataSetExDto.DataSourceFrom);

            //using (DataAccessAdapter aDataAccessAdapterWithDataSource = new DataAccessAdapter(conenctInfo))
            //         {
            //                aDataAccessAdapterWithDataSource.CallRetrievalStoredProcedure(aAppDataSetExDto.QueryText, sqlParamterList.ToArray(), fillDataTable);


            //         }

            DataTable fillDataTable = dbFixture.RetriveStorProcDataTable(queryStorcname, sqlParamterList);

            return fillDataTable;
        }

        //  RetrieveAllAppDataSetEntityDto(aAppDataSetExDto.Id )


        public static DataTable FilterDataSet(AppDataSetEntity aAppDataSetExDto, string whereClasue)
        {

            string queryDataSet = aAppDataSetExDto.QueryText;


            List<DbParameter> sqlParamterList = new List<DbParameter>();


            if (!string.IsNullOrWhiteSpace(whereClasue))
            {

                queryDataSet = string.Format(@"select  * from 
				  (
					 {0}
				  ) as temp

				  WHERE {1} ", aAppDataSetExDto.QueryText, whereClasue);

            }

            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);
            return fixture.RetriveDataTable(queryDataSet, sqlParamterList);




        }
        public static OperationCallResult<object> DeleteOneAppDataSetEntityDto(object Id, bool needToDeleteOrUpdateRelatedSearchAndView = true)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            int? needToUpdateTransactionId = null;

            List<string> needToDropTempTables = new List<string>();

            AppDataSetExDto dataSetExDto = AppDatabaseErDiagramBL.RetrieveOneErDiagramExDto(Id);

            if (dataSetExDto.OtherSettingsDto != null && dataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                var importSettingDto = dataSetExDto.OtherSettingsDto.TableImportSettingDto;
                needToUpdateTransactionId = importSettingDto.NeedToUpdateTransactionId;

                if (!string.IsNullOrWhiteSpace(importSettingDto.OrgTempTableName))
                {
                    needToDropTempTables.Add(importSettingDto.OrgTempTableName);
                }

                if (!string.IsNullOrWhiteSpace(importSettingDto.TempTableName))
                {
                    needToDropTempTables.Add(importSettingDto.TempTableName);
                }
            }


            ValidationResult avalidationResult = DeleteOneAppDataSetEntity(Id, needToDeleteOrUpdateRelatedSearchAndView);
            aOperationCallResult.ValidationResult = avalidationResult;
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = Id;

                if (needToUpdateTransactionId.HasValue)
                {
                    try
                    {
                        var transactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(needToUpdateTransactionId.Value);

                        if (transactionExDto.OtherOptions != null && transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId.HasValue)
                        {
                            transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId = null;

                            var saveTransactionResult = AppTransactionBL.SaveAppTransactionExDto(transactionExDto);
                        }
                    }
                    catch (Exception ex)
                    {

                    }


                }

                if (needToDropTempTables.Count > 0)
                {
                    foreach (string needToDropTableName in needToDropTempTables)
                    {
                        AppDatabaseTableImportBL.DeleteTempTable(dataSetExDto.DataSourceFrom, needToDropTableName, aOperationCallResult.ValidationResult);
                    }
                }
            }

            return aOperationCallResult;
        }

        private static ValidationResult DeleteOneAppDataSetEntity(object Id, bool needToDeleteOrUpdateRelatedSearchAndView = true)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppDataSetEntity");

                    if (needToDeleteOrUpdateRelatedSearchAndView)
                    {
                        AppSearchEntity appSearchEntity = new AppSearchEntity();
                        appSearchEntity.DataSetId = null;
                        adapter.UpdateEntitiesDirectly(appSearchEntity, new RelationPredicateBucket(AppSearchFields.DataSetId == Id));

                        AppSearchViewEntity appSearchViewEntity = new AppSearchViewEntity();
                        appSearchViewEntity.DataSetId = null;
                        adapter.UpdateEntitiesDirectly(appSearchViewEntity, new RelationPredicateBucket(AppSearchViewFields.DataSetId == Id));

                        //

                        //adapter.DeleteEntitiesDirectly(typeof(AppSearchParameterEntity), new RelationPredicateBucket(AppSearchParameterFields.DataSetId == Id));


                    }

                    adapter.DeleteEntitiesDirectly(typeof(AppDataSetParameterEntity), new RelationPredicateBucket(AppDataSetParameterFields.DataSetId == Id));
                    adapter.DeleteEntitiesDirectly(typeof(AppDataSetEntity), new RelationPredicateBucket(AppDataSetFields.DataSetId == Id));




                    adapter.Commit();
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        public static OperationCallResult<bool> AddDatabaseObjectsToApplication(Dictionary<string, int> dictDbObjNameAndUsageType, int applicationId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            List<string> existObjNames = RetrieveAllApplicationDatabaseObject(applicationId).Select(o => o.Name.ToLower().Trim()).ToList();

            foreach (string dbObjName in dictDbObjNameAndUsageType.Keys)
            {

                int usageType = dictDbObjNameAndUsageType[dbObjName];

                if (usageType == (int)EmAppDataSetUsageType.DatabaseTable
                     || usageType == (int)EmAppDataSetUsageType.DatabaseView
                     || usageType == (int)EmAppDataSetUsageType.DatabaseStoredProcedure)
                {
                    string newObjName = dbObjName.ToLower().Trim();


                    if (!existObjNames.Contains(newObjName))
                    {
                        AppDataSetExDto aDto = new AppDataSetExDto();
                        aDto.Name = newObjName;
                        aDto.SaasApplicationId = applicationId;
                        aDto.UsageTypeId = usageType;

                        var result = ProcessNewDto(aDto);

                        if (result.HasErrors)
                        {
                            validationResult.Merge(result);
                        }
                    }
                }
            }

            if (!validationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
                validationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<bool> RemoveDatabaseObjectsFromApplication(Dictionary<string, int> dictDbObjNameAndUsageType, int? applicationId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {



                    List<int> usageTypes = new List<int>() {
                            (int)EmAppDataSetUsageType.DatabaseTable,
                            (int)EmAppDataSetUsageType.DatabaseView,
                            (int)EmAppDataSetUsageType.DatabaseStoredProcedure
                        };

                    List<string> dbObjNames = dictDbObjNameAndUsageType.Keys.Select(o => o.ToLower().Trim()).Distinct().Where(o => !string.IsNullOrWhiteSpace(o)).ToList();

                    if (dbObjNames.Count > 0)
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppDataSetEntity");

                        if (applicationId.HasValue)
                        {
                            adapter.DeleteEntitiesDirectly(typeof(AppDataSetEntity), new RelationPredicateBucket(
                                AppDataSetFields.UsageTypeId == usageTypes
                                & AppDataSetFields.Name == dbObjNames
                                & AppDataSetFields.SaasApplicationId == applicationId.Value));
                        }
                        else
                        {
                            adapter.DeleteEntitiesDirectly(typeof(AppDataSetEntity), new RelationPredicateBucket(
                                AppDataSetFields.UsageTypeId == usageTypes
                                & AppDataSetFields.Name == dbObjNames));
                        }

                        adapter.Commit();

                        aOperationCallResult.Object = true;
                        validationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }


                }
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<bool> RemoveDatabaseObjectsFromAllApplication(Dictionary<string, int> dictDbObjNameAndUsageType)
        {
            return RemoveDatabaseObjectsFromApplication(dictDbObjNameAndUsageType, null);
        }

        public static OperationCallResult<bool> RenameDatabaseObjectsFromAllApplication(string orgName, string newName)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {



                    List<int> usageTypes = new List<int>() {
                            (int)EmAppDataSetUsageType.DatabaseTable,
                            (int)EmAppDataSetUsageType.DatabaseView,
                            (int)EmAppDataSetUsageType.DatabaseStoredProcedure
                        };

                    orgName = orgName.ToLower().Trim();
                    newName = newName.ToLower().Trim();

                    if (!string.IsNullOrWhiteSpace(orgName) && !string.IsNullOrWhiteSpace(newName) && orgName != newName)
                    {
                        AppDataSetEntity entity = new AppDataSetEntity();
                        entity.Name = newName;

                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppDataSetEntity");
                        adapter.UpdateEntitiesDirectly(entity, new RelationPredicateBucket(
                                AppDataSetFields.UsageTypeId == usageTypes
                                & AppDataSetFields.Name == orgName));
                        adapter.Commit();

                        aOperationCallResult.Object = true;
                        validationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }




                }
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aOperationCallResult;
        }


        private static EntityCollection<AppDataSetEntity> RetrieveAllApplicationDatabaseObject(int? applicationId)
        {
            List<int> usageTypes = new List<int>() {
                            (int)EmAppDataSetUsageType.DatabaseTable,
                            (int)EmAppDataSetUsageType.DatabaseView,
                            (int)EmAppDataSetUsageType.DatabaseStoredProcedure
                        };

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppDataSetEntity> list = new EntityCollection<AppDataSetEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppDataSetFields.UsageTypeId == usageTypes);

                if (applicationId.HasValue)
                {
                    filter.PredicateExpression.AddWithAnd(AppDataSetFields.SaasApplicationId == applicationId.Value);
                }

                adapter.FetchEntityCollection(list, filter, 0);
                return list;
            }
        }

        private static ValidationResult ProcessNewDto(AppDataSetExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppDataSetEntity aParentAppDataSetEntity = new AppDataSetEntity();
            AppDataSetConverter.CopyDtoToEntity(aParentAppDataSetEntity, aDto);

            foreach (var paramterDto in aDto.AppDataSetParameterList)
            {
                AppDataSetParameterEntity parameterEntity = new AppDataSetParameterEntity();
                AppDataSetParameterConverter.CopyDtoToEntity(parameterEntity, paramterDto);
                aParentAppDataSetEntity.AppDataSetParameter.Add(parameterEntity);
            }



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppDataSetEntity);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aDto.Id = aParentAppDataSetEntity.DataSetId;

                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static void InitIntegrationApiDefaultRootJsonNodePath(AppDataSetExDto aDto)
        {
            if (aDto.QueryType.HasValue && aDto.QueryType.Value == (int)EmAppDataServiceType.IntegrationWebApiCall)
            {
                if (string.IsNullOrWhiteSpace(aDto.BaseTableName))
                {
                    int? operationId = ControlTypeValueConverter.ConvertValueToInt(aDto.QueryText);

                    if (operationId.HasValue)
                    {
                        var apiOperationDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(operationId.Value);

                        var rootNodeDto = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(apiOperationDto.JsonSchema);

                        if (rootNodeDto.Children.Count == 1 && (rootNodeDto.Children[0].IsObject || rootNodeDto.Children[0].IsArray))
                        {
                            aDto.BaseTableName = rootNodeDto.Children[0].Name;
                        }
                    }
                }
            }
        }

        private static ValidationResult ProcessDirtyDto(AppDataSetExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppDataSetEntity aAppDataSetEntity = RetrieveOneAppDataSetEntity(aDto.Id);

            AppDataSetConverter.CopyDtoToEntity(aAppDataSetEntity, aDto);

            Dictionary<int, AppDataSetParameterEntity> dictAppDataSetParameterFromDbms = aAppDataSetEntity.AppDataSetParameter.ToDictionary(o => o.ParameterId, o => o);


            // new Items
            foreach (AppDataSetParameterDto aChildDto in aDto.AppDataSetParameterList.FindNewItems())
            {
                AppDataSetParameterEntity aNewChildEntity = new AppDataSetParameterEntity();
                AppDataSetParameterConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppDataSetEntity.AppDataSetParameter.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aDto.AppDataSetParameterList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppDataSetParameterFromDbms.ContainsKey(dtoKey))
                {
                    AppDataSetParameterConverter.CopyDtoToEntity(dictAppDataSetParameterFromDbms[dtoKey], modifyitem);
                }
            }

            // deletedIDs
            int[] deleteAppDataSetParameterIDs = aDto.AppDataSetParameterList.FindDeletedItemIds().Cast<int>().ToArray();


          
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppDataSetEntity, false, true);

                    // Need to delete SearchTemplate subitems
                    if (deleteAppDataSetParameterIDs.Count() > 0)
                    {

                        adapter.DeleteEntitiesDirectly(typeof(AppSearchParameterEntity), new RelationPredicateBucket(AppSearchParameterFields.ParameterId == deleteAppDataSetParameterIDs));

                        adapter.DeleteEntitiesDirectly(typeof(AppDataSetParameterEntity), new RelationPredicateBucket(AppDataSetParameterFields.ParameterId == deleteAppDataSetParameterIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }



        private static void BuildIntegrationWebApiCallAvailableFields(List<LookupItemDto> list, AppDataSetExDto dataSetExDto)
        {
            var operationDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(dataSetExDto.QueryText);

            if (operationDto != null
                && operationDto.APIConfigParameters != null
                && !string.IsNullOrWhiteSpace(operationDto.JsonSchema))
            {
                foreach (var pathParameter in operationDto.APIConfigParameters.PathParams)
                {
                    LookupItemDto aLookupItemDto = new LookupItemDto();
                    aLookupItemDto.Id = pathParameter.Key;
                    aLookupItemDto.Display = pathParameter.Key;
                    aLookupItemDto.ItemType = 1; // Input
                    aLookupItemDto.UserDefinedValue1 = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(pathParameter.Value);
                    list.Add(aLookupItemDto);
                }

                foreach (var pathParameter in operationDto.APIConfigParameters.QueryParams)
                {
                    LookupItemDto aLookupItemDto = new LookupItemDto();
                    aLookupItemDto.Id = pathParameter.Key;
                    aLookupItemDto.Display = pathParameter.Key;
                    aLookupItemDto.ItemType = 1; // Input
                    aLookupItemDto.UserDefinedValue1 = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(pathParameter.Value);
                    list.Add(aLookupItemDto);
                }

                if (dataSetExDto.UsageTypeId.HasValue && dataSetExDto.UsageTypeId.Value == (int)EmAppDataSetUsageType.ConvertSimpleObjectToList)
                {
                    LookupItemDto key_LookupItemDto = new LookupItemDto();
                    key_LookupItemDto.ItemType = 2; // Output
                    key_LookupItemDto.Id = "Key";
                    key_LookupItemDto.Display = "Key";

                    LookupItemDto value_LookupItemDto = new LookupItemDto();
                    value_LookupItemDto.ItemType = 2; // Output
                    value_LookupItemDto.Id = "Value";
                    value_LookupItemDto.Display = "Value";

                    list.Add(key_LookupItemDto);
                    list.Add(value_LookupItemDto);
                }
                else
                {
                    var rootNodeDto = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(operationDto.JsonSchema);

                    string rootNodePath = dataSetExDto.BaseTableName;

                    if (!string.IsNullOrWhiteSpace(rootNodePath))
                    {
                        string[] levelPathList = rootNodePath.Split(new string[] { "." }, StringSplitOptions.None);
                        var actualRootNode = FindChildNodeDtoFromPathLevel(rootNodeDto, levelPathList, 0);

                        if (actualRootNode != null)
                        {
                            rootNodeDto = actualRootNode;
                        }
                    }

                    ProcessChildNodeAsAvailableField(list, rootNodeDto);
                }


            }
        }




        internal static ApiDataStructureNodeDto FindChildNodeDtoFromPathLevel(ApiDataStructureNodeDto nodeDto, string[] levelPathList, int level)
        {

            string nodeName = levelPathList[level];
            var childNodeDto = nodeDto.Children.FirstOrDefault(o => o.Name == nodeName);

            level++;

            if (childNodeDto != null)
            {
                if (level == levelPathList.Length)
                {
                    return childNodeDto;
                }
                else
                {
                    return FindChildNodeDtoFromPathLevel(childNodeDto, levelPathList, level);
                }
            }

            return null;
        }

        private static void ProcessChildNodeAsAvailableField(List<LookupItemDto> list, ApiDataStructureNodeDto nodeDto)
        {
            foreach (var nodeProperty in nodeDto.Children.Where(o => !o.IsArray && !o.IsObject))
            {
                LookupItemDto aLookupItemDto = new LookupItemDto();
                aLookupItemDto.ItemType = 2; // Output

                if (!string.IsNullOrWhiteSpace(nodeDto.NodePath))
                {
                    aLookupItemDto.Id = nodeDto.NodePath + "." + nodeProperty.Name;
                }
                else
                {
                    aLookupItemDto.Id = nodeProperty.Name;

                }

                aLookupItemDto.Display = aLookupItemDto.Id.ToString();

                list.Add(aLookupItemDto);
            }


            foreach (var childNodeDto in nodeDto.Children.Where(o => o.IsObject || o.IsArray))
            {
                if (!string.IsNullOrWhiteSpace(nodeDto.NodePath))
                {
                    childNodeDto.NodePath = nodeDto.NodePath + "." + childNodeDto.Name;
                }
                else
                {
                    childNodeDto.NodePath = childNodeDto.Name;
                }

                ProcessChildNodeAsAvailableField(list, childNodeDto);
            }

        }

        private static void PrepareDataSetApiDataStructure(AppDataSetExDto aAppDataSetExDto, AppIntergrationSettingParameterExDto apiOperationDto)
        {
            var rootNodeDto = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(apiOperationDto.JsonSchema);

            if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsArray)
            {
                rootNodeDto = rootNodeDto.Children[0];
                rootNodeDto.Display = "[ ]";
            }

            string rootArrayJsonNodePath = aAppDataSetExDto.BaseTableName;

            if (!string.IsNullOrWhiteSpace(rootArrayJsonNodePath))
            {

                string[] levelPathList = rootArrayJsonNodePath.Split(new string[] { "." }, StringSplitOptions.None);
                var actualRootNode = FindChildNodeDtoFromPathLevel(rootNodeDto, levelPathList, 0);

                if (actualRootNode != null)
                {
                    rootNodeDto = actualRootNode;
                }
            }

            RemoveChildArrayNode(rootNodeDto);


            AppIntergrationSettingBL.InitApiNodeAbolutePath(rootNodeDto, null);

            aAppDataSetExDto.ApiDataStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };
        }

        private static void RemoveChildArrayNode(ApiDataStructureNodeDto parentNodeDto)
        {
            if (parentNodeDto != null && parentNodeDto.Children != null)
            {
                var childArrayNodes = parentNodeDto.Children.Where(o => o.IsArray).ToList();
                childArrayNodes.ForAll(o => parentNodeDto.Children.Remove(o));

                var childObjNodes = parentNodeDto.Children.Where(o => o.IsObject && o.Children != null).ToList();
                childObjNodes.ForAll(o => RemoveChildArrayNode(o));
            }
        }

        private static void PrepareApiAvailableFetchDataNodeStructure(AppDataSetExDto aAppDataSetExDto, AppIntergrationSettingParameterExDto apiOperationDto)
        {
            var rootNodeDto = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(apiOperationDto.JsonSchema, false, false);

            if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsArray && !rootNodeDto.HasSimpleProperties)
            {
                rootNodeDto = rootNodeDto.Children[0];
                rootNodeDto.Display = "[ ]";
            }


            AppIntergrationSettingBL.InitApiNodeAbolutePath(rootNodeDto, null);

            aAppDataSetExDto.ApiAvailableFetcheDataJsonNodeStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };
        }


    }

    public class QueryViewDTableDto
    {
        public const string SELECT = "SELECT";
        public const string FROM = "FROM ";
        public const string WHERE = "WHERE ";
        public const string ORDER_BY = "ORDER BY ";



        public string TableName
        {
            get; set;
        }

        public int Order
        {
            get; set;
        }

        // Crosss
        // Inner
        // Left Outer
        // Right Outer
        public string JoinRelation
        {
            get; set;
        }


        public List<string> JoinCodtion
        {
            get; set;
        }




    }

    public class QueryMainViewDTo
    {



        public List<QueryViewDTableDto> QueryViewDTableDtoList
        {
            get; set;

        }

        public Dictionary<string, Dictionary<string, string>> DictTableSelectColumns
        {
            get; set;

        }

        public Dictionary<string, List<WhereCondition>> WhereClause
        {
            get; set;

        }


        public Dictionary<string, List<string>> DictTableOrderBy
        {
            get; set;

        }


    }


    public class WhereCondition
    {

        public string ColumnName
        {
            get; set;
        }

        // = > <>
        public string Express
        {
            get; set;
        }

        public string PrefixAndOrOR
        {
            get; set;
        }

        //public string Value
        //{
        //	get; set;
        //}
    }

}