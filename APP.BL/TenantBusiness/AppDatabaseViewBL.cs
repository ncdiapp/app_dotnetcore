using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
////using APP.Persistence.Common;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.Conversion;
using DatabaseSchemaMrg.DataSchema;
using DatabaseSchemaMrg.SqlGen;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.IO;
using System.Text.RegularExpressions;

using System.IdentityModel.Tokens;

using APP.Framework;
namespace App.BL
{
    public static class AppDatabaseViewBL
    {
        public static readonly int startX = 20;
        public static readonly int startY = 20;
        public static readonly int tableWidth = 200;
        public static readonly int tableHeight = 200;
        public static readonly int totalDiagramColumns = 3;


        public static DatabaseViewDto UpdateDatabaseViewDtoFromQuery(DatabaseViewDto orgViewDto)
        {


            if (orgViewDto != null)
            {
                try
                {
                    DatabaseViewDto newViewDto = ConvertQueryToViewDto(orgViewDto.QueryString, orgViewDto.DataSourceRegisterId, orgViewDto);


                    if (orgViewDto.SelectedColumnsList != null && orgViewDto.SelectedColumnsList.Count > 0)
                    {
                        foreach (DatabaseViewColumnDto selectedColumn in newViewDto.SelectedColumnsList)
                        {
                            DatabaseViewColumnDto existingOrgColumn = orgViewDto.SelectedColumnsList.FirstOrDefault(o =>
                            o.UniqTableOrAliasName.ToLower() == selectedColumn.UniqTableOrAliasName.ToLower() && o.ColumnDisplayName.ToLower() == selectedColumn.ColumnDisplayName.ToLower());

                            if (existingOrgColumn != null)
                            {
                                selectedColumn.EntityId = existingOrgColumn.EntityId;
                                selectedColumn.IsUsedAsSearchField = existingOrgColumn.IsUsedAsSearchField;
                                selectedColumn.IsUsedAsViewField = existingOrgColumn.IsUsedAsViewField;
                            }
                        }
                    }


                    return newViewDto;
                }
                catch (Exception ex)
                {
                    orgViewDto.ErrorMessage = ex.ToString();
                }

            }


            return orgViewDto;
        }



        //Environment Variables
        public static KeyValuePair<string, string> GetDataSetColumnRefereceTable(string dataSetColumnName, int? dataSetId)
        {


            KeyValuePair<string, string> toReturn = new KeyValuePair<string, string>();

            var datasetEntity = AppDataSetBL.RetrieveOneAppDataSetEntity(dataSetId);
            int dataSourceRegisterId = datasetEntity.DataSourceFrom.Value;


            var dbFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);


            string queryString = datasetEntity.QueryText;



            if (string.IsNullOrEmpty(queryString))
            {
                return toReturn;



            }

            queryString = dbFixture.MatchSQLServerQuerySyntax(queryString);

            TSql100Parser parser = new TSql100Parser(false);
            TextReader rd = new StringReader(queryString);
            IList<ParseError> errors;


            var fragments = parser.Parse(rd, out errors);



            if (errors.Count > 0)
            {

                return toReturn;
            }


            Dictionary<string, string> dictAliasNameTableName = new Dictionary<string, string>();
            HashSet<string> baseTableNameList = new HashSet<string>();

            QueryExpression query = ExtractDatabaseTable(fragments, dictAliasNameTableName, baseTableNameList);


            Dictionary<string, string> dictAliasNameColumnName = new Dictionary<string, string>();
            Dictionary<string, string> dictColumnNameTableName = new Dictionary<string, string>();

            ProcessSelectColumn(query, dictAliasNameColumnName, dictColumnNameTableName);


            string trueDbFiledName = dataSetColumnName;
            if (dictAliasNameColumnName.ContainsKey(dataSetColumnName))
            {
                trueDbFiledName = dictAliasNameColumnName[dataSetColumnName];

            }

            if (dictColumnNameTableName.ContainsKey(trueDbFiledName))
            {

                string tableName = dictColumnNameTableName[trueDbFiledName];
                if (dictAliasNameTableName.ContainsKey(tableName))
                {
                    tableName = dictAliasNameTableName[tableName];


                }

                if (string.IsNullOrEmpty(tableName))
                {
                    tableName = baseTableNameList.ElementAt(0);
                }


                toReturn = dbFixture.GetOneTableOneFkCoumnReferenceParentTables(tableName, trueDbFiledName);

            }



            return toReturn;

        }

        private static QueryExpression ExtractDatabaseTable(TSqlFragment fragments, Dictionary<string, string> dictAliasNameTableName, HashSet<string> baseTableNameList)
        {
            var query = ((fragments as TSqlScript).Batches[0].Statements[0] as SelectStatement).QueryExpression;

            //(query as QuerySpecification).

            var qualifiedJoinOrNamedTableReference = (query as QuerySpecification).FromClause.TableReferences[0];

            //or NamedTableReference

            // QualifiedJoin fistTab = qualifiedJoin.FirstTableReference as QualifiedJoin;

            if (qualifiedJoinOrNamedTableReference is QualifiedJoin)
            {
                ExtracOneJoin(dictAliasNameTableName, baseTableNameList, (QualifiedJoin)qualifiedJoinOrNamedTableReference);
            }
            else // it is NamedTableReference
            {
                var schemaObjectTableReference = (NamedTableReference)qualifiedJoinOrNamedTableReference;

                string baseTbleName = schemaObjectTableReference.SchemaObject.BaseIdentifier.Value;
                baseTableNameList.Add(baseTbleName);


            }

            return query;
        }


        private static void ProcessSelectColumn(QueryExpression query, Dictionary<string, string> dictAliasNameColumnName, Dictionary<string, string> dictColumnNameTableName)
        {
            IList<SelectElement> selectElements = (query as QuerySpecification).SelectElements;

            foreach (SelectElement fragment in selectElements)
            {

                var selectColumn = fragment as SelectScalarExpression;
                if (selectColumn != null)
                {
                    var exp = selectColumn.Expression as ColumnReferenceExpression;
                    var identifiers = exp?.MultiPartIdentifier?.Identifiers;
                    if (identifiers != null)
                    {
                        string columnName = "";
                        if (identifiers.Count == 2)
                        {
                            string TableNameName = identifiers[identifiers.Count - 2].Value;
                            columnName = identifiers[identifiers.Count - 1].Value;

                            dictColumnNameTableName[columnName] = TableNameName;
                        }
                        else if (identifiers.Count == 1)
                        {
                            // string TableNameName = identifiers[identifiers.Count - 2].Value;
                            columnName = identifiers[identifiers.Count - 1].Value;

                            dictColumnNameTableName[columnName] = "";
                        }


                        if (selectColumn.ColumnName != null && !string.IsNullOrEmpty(selectColumn.ColumnName.Value))
                        {
                            string aliasname = selectColumn.ColumnName.Value;
                            dictAliasNameColumnName[aliasname] = columnName;
                        }
                    }



                }

            }
        }

        private static void ExtracOneJoin(Dictionary<string, string> dictAliasNameTableName, HashSet<string> baseTableName, QualifiedJoin qualifiedJoin)
        {
            if (qualifiedJoin.FirstTableReference is NamedTableReference)
            {
                var schemaObjectTableReference = qualifiedJoin.FirstTableReference as NamedTableReference;
                string baseTbleName = schemaObjectTableReference.SchemaObject.BaseIdentifier.Value;
                baseTableName.Add(baseTbleName);

                if (schemaObjectTableReference.Alias != null)
                {
                    string tableAlisName = schemaObjectTableReference.Alias.Value;

                    dictAliasNameTableName[tableAlisName] = baseTbleName;

                }

            }
            {
                var first = qualifiedJoin.FirstTableReference as QualifiedJoin;
                if (first != null)
                {
                    ExtracOneJoin(dictAliasNameTableName, baseTableName, first);
                }

            }


            if (qualifiedJoin.SecondTableReference is NamedTableReference)
            {
                var schemaObjectTableReference = qualifiedJoin.SecondTableReference as NamedTableReference;
                string baseTbleName = schemaObjectTableReference.SchemaObject.BaseIdentifier.Value;
                baseTableName.Add(baseTbleName);

                if (schemaObjectTableReference.Alias != null)
                {
                    string tableAlisName = schemaObjectTableReference.Alias.Value;

                    dictAliasNameTableName[tableAlisName] = baseTbleName;

                }

            }
            else
            {
                var second = qualifiedJoin.SecondTableReference as QualifiedJoin;
                if (second != null)
                {
                    ExtracOneJoin(dictAliasNameTableName, baseTableName, second);
                }
            }
        }

        public static DatabaseViewDto ConvertQueryToViewDto(string queryString, int? dataSourceRegisterId, DatabaseViewDto orgViewDto)
        {
            DatabaseViewDto viewDto = new DatabaseViewDto();

            viewDto.QueryString = queryString;
            viewDto.DictTables = new Dictionary<string, DatabaseViewTableDto>(StringComparer.OrdinalIgnoreCase);
            viewDto.Joins = new List<DatabaseViewJoinDto>();
            viewDto.DictAllColumns = new Dictionary<string, Dictionary<string, bool>>();
            viewDto.SelectedColumnsList = new List<DatabaseViewColumnDto>();
            viewDto.WhereConditionFilterColumns = new List<DatabaseViewColumnDto>();

            DatabaseFixture databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);

            queryString = databaseFixture.MatchSQLServerQuerySyntax(queryString);
            viewDto.ErrorMessage = databaseFixture.ValidateQueryText(queryString);
            viewDto.DataSourceRegisterId = dataSourceRegisterId;

            if (!string.IsNullOrWhiteSpace(viewDto.ErrorMessage))
            {
                return viewDto;
            }

            if (!string.IsNullOrEmpty(queryString))
            {
                TSql100Parser parser = new TSql100Parser(false);
                TextReader rd = new StringReader(queryString);
                IList<ParseError> errors;

                var fragments = parser.Parse(rd, out errors);

                string fields = string.Empty;
                string from = string.Empty;
                string groupby = string.Empty;
                string where = string.Empty;
                string orderby = string.Empty;
                string having = string.Empty;

                if (errors.Count > 0)
                {
                    var retMessage = string.Empty;
                    foreach (var error in errors)
                    {
                        retMessage += error.Message + " - line: " + error.Line + " column: " + error.Column + " - position: " + error.Offset + "; ";
                    }

                    viewDto.ErrorMessage = retMessage;
                    return viewDto;
                }

                try
                {
                    var query = ((fragments as TSqlScript).Batches[0].Statements[0] as SelectStatement).QueryExpression;

                    TableReference fromSource = (query as QuerySpecification).FromClause.TableReferences[0];
                    IList<SelectElement> selectElements = (query as QuerySpecification).SelectElements;

                    ParseQueryFromSource(fromSource, viewDto, null);
                    ParseQuerySelectFields(selectElements, viewDto);


                    List<string> ownertableNameList = new List<string>();

                    foreach (DatabaseViewTableDto aDatabaseViewTableDto in viewDto.DictTables.Values)
                    {
                        string aOwnerTableKey = AppMetaDataBL.GetOwnerTableKey(aDatabaseViewTableDto.SchemaOwner, aDatabaseViewTableDto.TableName);

                        if (!ownertableNameList.Contains(aOwnerTableKey))
                        {
                            ownertableNameList.Add(aOwnerTableKey);
                        }
                    }

                    //List<KeyValuePair<string, string>> ownertableNameList = new List<KeyValuePair<string, string>>();
                    //foreach (DatabaseViewTableDto aDatabaseViewTableDto in viewDto.DictTables.Values)
                    //{
                    //    KeyValuePair<string, string> ownertableNameKey = new KeyValuePair<string, string>(aDatabaseViewTableDto.SchemaOwner, aDatabaseViewTableDto.TableName);

                    //    if (!ownertableNameList.Contains(ownertableNameKey))
                    //    {
                    //        ownertableNameList.Add(ownertableNameKey);
                    //    }
                    //}


                    //	// Key: schemOnwer_TableName,
                    Dictionary<string, DatabaseTable> dictDBTables = AppMetaDataBL.GetDatabaseTableBySchmaOnerTableNames(ownertableNameList, dataSourceRegisterId);

                    Dictionary<string, DatabaseViewTableDto> dictUniqTableOrAliasNameAndTable = new Dictionary<string, DatabaseViewTableDto>();

                    foreach (DatabaseViewTableDto viewTable in viewDto.DictTables.Values)
                    {
                        string aOwnerTableKey = AppMetaDataBL.GetOwnerTableKey(viewTable.SchemaOwner, viewTable.TableName);

                        if (dictDBTables.ContainsKey(aOwnerTableKey))
                        {

                            var dbTable = dictDBTables[aOwnerTableKey];

                            Dictionary<string, bool> dictTableColumn = dbTable.Columns.ToDictionary(o => o.Name, o => false);

                            var selectedColumns = viewDto.SelectedColumnsList.Where(o => o.UniqTableOrAliasName.ToLower() == viewTable.UniqTableOrAliasName.ToLower()).ToList();

                            foreach (var column in selectedColumns)
                            {
                                if (dictTableColumn.ContainsKey(column.ColumnName))
                                {
                                    dictTableColumn[column.ColumnName] = true;
                                }
                            }

                            viewDto.DictAllColumns.Add(viewTable.UniqTableOrAliasName.ToLower(), dictTableColumn);

                        }
                    }



                    //// Constructs the From clause with the optional joins
                    //from = (query as QuerySpecification).FromClause.TableReferences[0].GetString();

                    //// Extract the where clause
                    //where = (query as QuerySpecification).WhereClause.GetString();

                    //// Get the field list
                    //var fieldList = new List<string>();
                    //foreach (var f in (query as QuerySpecification).SelectElements)
                    //    fieldList.Add((f as SelectColumn).GetString());
                    //fields = string.Join(", ", fieldList.ToArray());

                    //// Get The group by clause
                    //groupby = (query as QuerySpecification).GroupByClause.GetString();

                    //// Get the having clause of the query
                    //having = (query as QuerySpecification).HavingClause.GetString();

                    //// Get the order by clause
                    //orderby = ((fragments as TSqlScript).Batches[0].Statements[0] as SelectStatement).OrderByClause.GetString();


                    foreach (var joinDto in viewDto.Joins)
                    {

                        string conditionDisplay = string.Empty;

                        if (joinDto.JoinConditionList != null)
                        {
                            foreach (DatabaseViewJoinConditionDto conditionDto in joinDto.JoinConditionList)
                            {
                                string oneConditionString = string.Empty;

                                if (!string.IsNullOrWhiteSpace(conditionDisplay))
                                {
                                    oneConditionString = " AND ";
                                }

                                //oneConditionString += string.Format("[{0}].[{1}].[{2}] {3} [{4}].[{5}].[{6}]",
                                //    conditionDto.LeftSideSchemaOwner,
                                //    conditionDto.LeftSideTable,
                                //    conditionDto.LeftSideColumn,
                                //    conditionDto.JoinOperationType,
                                //    conditionDto.RightSideSchemaOwner,
                                //    conditionDto.RightSideTable,
                                //    conditionDto.RightSideColumn);

                                oneConditionString += string.Format("[{0}].[{1}] {2} [{3}].[{4}]",
                                   conditionDto.LeftSideTable,
                                   conditionDto.LeftSideColumn,
                                   conditionDto.JoinOperationType,
                                   conditionDto.RightSideTable,
                                   conditionDto.RightSideColumn);

                                conditionDisplay += oneConditionString;
                            }
                        }


                        joinDto.JoinConditionDisplay = conditionDisplay;

                    }

                }
                catch (Exception ex)
                {
                    //return ex.ToString();      

                    viewDto.ErrorMessage = ex.ToString();
                    return viewDto;
                }



            }

            RecalculateTablesPositions(viewDto, orgViewDto);

            if (viewDto.DictTables != null && viewDto.DataSourceRegisterId.HasValue)
            {
                AppDatabaseErDiagramBL.PrepareFkReferenceTableNameList(viewDto.DataSourceRegisterId.Value, viewDto);
            }


            return viewDto;

        }



        public static DatabaseViewDto AddOneFkLineToErDiagram(ViewJoinUpdateDto viewJoinUpdateDto)
        {
            DatabaseViewDto viewDto = viewJoinUpdateDto as DatabaseViewDto;

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(viewDto.DataSourceRegisterId.Value);



            if (viewJoinUpdateDto != null && viewDto != null && viewJoinUpdateDto.NeedToAddJoinCondition != null)
            {
                var fkDto = viewJoinUpdateDto.NeedToAddJoinCondition;

                if (viewDto.DictTables.ContainsKey(fkDto.RightSideTable))
                {
                    string rightPkName = viewDto.DictTables[fkDto.RightSideTable].PkNames.FirstOrDefault();
                    string leftPkName = viewDto.DictTables[fkDto.LeftSideTable].PkNames.FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(rightPkName))
                    {
                        // https://www.w3schools.com/sql/sql_foreignkey.asp

                        // need to remove not exsting ID in reference table from currentable
                        //Left side: currenttable, Right side: Reference Table
                        // https://stackoverflow.com/questions/63195125/mysql-error-code-1175-you-are-using-safe-update-mode-even-with-primary-key
                        // You are using safe update mode and you tried to update a table without a WHERE that uses a KEY column.  To disable safe mode, toggle the option in Preferences -> SQL Editor and reconnect.

                        // need to commet force update null?

                        // databaseFixtureInstance.SetForeceUpdateCurrentTableForeighKeyAsNull()

                        //https://www.w3schools.com/sql/sql_ref_drop_constraint.asp

                        // need to drop contrain as well
                        // AddOneFkLineToErDiagram
                        // Identifier Type Maximum Length(characters)
                        //Column  64
                        //Index   64
                        //Constraint  64
                        //Stored Program  64
                        // need to drop contrain as well 64 character!
                        // FK_engine4_sitecrowdfunding_photos_project_id_engine4_sitecrowdfunding_projects_project_id ' is too long

                        string fkConstraiName = $@"FK_{fkDto.LeftSideTable}-{fkDto.LeftSideColumn}-{fkDto.RightSideTable}-{rightPkName} ";
                        if (fkConstraiName.Length > 64)
                        {
                            fkConstraiName = fkConstraiName.Substring(0, 63);
                        }


                        string query = $@"ALTER TABLE [{fkDto.LeftSideSchemaOwner}].[{fkDto.LeftSideTable}] ADD CONSTRAINT [{fkConstraiName}]
                                            FOREIGN KEY ([{fkDto.LeftSideColumn}]) REFERENCES [{fkDto.RightSideSchemaOwner}].[{fkDto.RightSideTable}] ([{rightPkName}])";

                        try
                        {

                            databaseFixtureInstance.ExecuteNonQueryResult(query, new List<DbParameter>());



                            RebuildErDiagramFkLinks(viewDto);
                        }
                        catch (Exception ex)
                        {
                            viewDto.ErrorMessage = viewDto.ErrorMessage + System.Environment.NewLine + ex.Message;
                        }

                    }
                }



            }




            return viewDto;
        }

        public static DatabaseViewDto RemoveFkLinesFromErDiagram(ViewJoinUpdateDto viewJoinUpdateDto)
        {
            DatabaseViewDto viewDto = viewJoinUpdateDto as DatabaseViewDto;

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(viewDto.DataSourceRegisterId.Value);

            if (viewJoinUpdateDto != null && viewDto != null && viewJoinUpdateDto.NeedToRemoveJoinConditionGUIDs != null && viewJoinUpdateDto.NeedToRemoveJoinConditionGUIDs.Count > 0)
            {
                string query = "";

                foreach (var needToRemoveLineGuid in viewJoinUpdateDto.NeedToRemoveJoinConditionGUIDs)
                {
                    foreach (var join in viewDto.Joins)
                    {
                        DatabaseViewJoinConditionDto fkDto = join.JoinConditionList.FirstOrDefault(o => o.GUID == needToRemoveLineGuid);
                        if (fkDto != null)
                        {

                            string dropConstrail = "";
                            // for oracle and sql server
                            if (databaseFixtureInstance.SqlServerType == EmSqlType.SqlServer || databaseFixtureInstance.SqlServerType == EmSqlType.Oracle)
                            {
                                dropConstrail = $@" ALTER TABLE [{fkDto.LeftSideSchemaOwner}].[{fkDto.LeftSideTable}] DROP CONSTRAINT [{join.ContraintName}]";

                            }

                            else if (databaseFixtureInstance.SqlServerType == EmSqlType.MySql)
                            {
                                dropConstrail = $@" ALTER TABLE [{fkDto.LeftSideSchemaOwner}].[{fkDto.LeftSideTable}]   DROP  FOREIGN KEY [{join.ContraintName}]";
                            }




                            query += "\n" + dropConstrail;
                            //+ @"ALTER TABLE [" + fkDto.LeftSideSchemaOwner + "].[" + fkDto.LeftSideTable +
                            //       "] DROP CONSTRAINT  [" + join.JoinConditionDisplay + "]";




                        }
                    }
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        databaseFixtureInstance.ExecuteNonQueryResult(query, new List<DbParameter>());
                    }

                    RebuildErDiagramFkLinks(viewDto);
                }
                catch (Exception ex)
                {
                    viewDto.ErrorMessage = ex.Message;
                }


                //using (DataAccessAdapter adpter = AppTenantAdapterBL.GetTenantAdapter())
                //{
                //    try
                //    {
                //        DataTable dataTableResult = adpter.ExecuteDataTableRetrievalQuery(query, new List<System.Data.SqlClient.SqlParameter>());

                //    }
                //    catch (Exception ex)
                //    {
                //        viewDto.ErrorMessage = ex.Message;

                //    }

                //}
            }

            return viewDto;
        }

        public static DatabaseViewDto ConvertViewDtoToQuery(DatabaseViewDto viewDto)
        {
            string queryString = string.Empty;

            if (viewDto != null && !viewDto.IsErDiagram)
            {
                viewDto.ErrorMessage = string.Empty;
                try
                {
                    viewDto.DictTables.Values.ForAll(o => BuildTableQueryFullNamePath(o));

                    queryString = ConvertViewDtoToQuery_BuildSelectString(viewDto);
                    queryString += ConvertViewDtoToQuery_BuildFromString(viewDto);

                    if (viewDto.WhereConditionFilterColumns.Count > 0)
                    {
                        queryString += " " + System.Environment.NewLine;
                        queryString += "WHERE (";

                        foreach (DatabaseViewColumnDto whereCondition in viewDto.WhereConditionFilterColumns.OrderBy(o => o.SortOrder))
                        {
                            string conditionTableFullNamePath = AddSquareBrackets(whereCondition.UniqTableOrAliasName);

                            if (viewDto.DictTables.ContainsKey(whereCondition.UniqTableOrAliasName))
                            {
                                DatabaseViewTableDto conditionTable = viewDto.DictTables[whereCondition.UniqTableOrAliasName];
                                if (conditionTable != null && !string.IsNullOrWhiteSpace(conditionTable.TableFullNamePath))
                                {
                                    conditionTableFullNamePath = conditionTable.TableFullNamePath;
                                }
                            }

                            queryString += conditionTableFullNamePath + "." + whereCondition.ColumnDisplayName + whereCondition.WhereConditionExpression + " ";

                        }
                        queryString += ")";
                    }

                    viewDto.QueryString = queryString;
                }
                catch (Exception ex)
                {
                    viewDto.ErrorMessage = ex.ToString();
                    return viewDto;
                }
            }

            return viewDto;
        }



        public static DatabaseViewDto UpdateDatabaseViewSelectedColumns(DatabaseViewDto viewDto)
        {
            if (viewDto != null)
            {
                try
                {
                    viewDto.ErrorMessage = string.Empty;
                    // add new columns
                    foreach (string uniqTableOrAliasName in viewDto.DictAllColumns.Keys)
                    {
                        var selectedColumns = viewDto.DictAllColumns[uniqTableOrAliasName].Where(o => o.Value == true).ToList();
                        foreach (var aColumn in selectedColumns)
                        {
                            AddSelectedColumns(viewDto, uniqTableOrAliasName, aColumn.Key);
                        }
                    }

                    //delete columns
                    List<DatabaseViewColumnDto> needToDeleteColumns = new List<DatabaseViewColumnDto>();
                    foreach (var column in viewDto.SelectedColumnsList)
                    {
                        if (!(viewDto.DictAllColumns.ContainsKey(column.UniqTableOrAliasName.ToLower())
                                && viewDto.DictAllColumns[column.UniqTableOrAliasName.ToLower()].ContainsKey(column.ColumnName)
                                && viewDto.DictAllColumns[column.UniqTableOrAliasName.ToLower()][column.ColumnName] == true))
                        {
                            needToDeleteColumns.Add(column);
                        }
                    }
                    foreach (var column in needToDeleteColumns)
                    {
                        viewDto.SelectedColumnsList.Remove(column);
                    }

                    int sortOrder = 1;
                    foreach (var column in viewDto.SelectedColumnsList.OrderBy(o => o.SortOrder))
                    {
                        if (!string.IsNullOrEmpty(column.ColumnAlias))
                        {
                            column.ColumnDisplayName = column.ColumnAlias;
                        }
                        column.SortOrder = sortOrder;
                        sortOrder++;
                    }

                    viewDto.SelectedColumnsList = viewDto.SelectedColumnsList.OrderBy(o => o.SortOrder).ToList();



                    ConvertViewDtoToQuery(viewDto);
                }
                catch (Exception ex)
                {
                    viewDto.ErrorMessage = ex.ToString();
                    return viewDto;
                }
            }

            return viewDto;
        }

        private static void AddSelectedColumns(DatabaseViewDto viewDto, string uniqTableOrAliasName, string columnName)
        {
            string columnDisplayName = columnName;
            string columnAlias = string.Empty;

            var existingColumn = viewDto.SelectedColumnsList.FirstOrDefault(o => o.UniqTableOrAliasName.ToLower() == uniqTableOrAliasName.ToLower() && o.ColumnName.ToLower() == columnName.ToLower());
            var existingColumnDisplayName = viewDto.SelectedColumnsList.FirstOrDefault(o => o.ColumnDisplayName.ToLower() == columnDisplayName.ToLower());
            if (existingColumn == null)
            {
                int aliasIndex = 1;
                while (existingColumnDisplayName != null)
                {
                    columnDisplayName = columnAlias = columnName + "_" + aliasIndex.ToString();
                    existingColumnDisplayName = viewDto.SelectedColumnsList.FirstOrDefault(o => o.ColumnDisplayName.ToLower() == columnDisplayName.ToLower());
                    aliasIndex++;
                }

                viewDto.SelectedColumnsList.Add(new DatabaseViewColumnDto()
                {
                    UniqTableOrAliasName = uniqTableOrAliasName,
                    ColumnName = columnName,
                    ColumnDisplayName = columnDisplayName,
                    ColumnAlias = columnAlias,
                    IsUsedAsSearchField = false,
                    IsUsedAsViewField = true,
                    IsFolderIdKeyField = true,
                    SortOrder = viewDto.SelectedColumnsList.Count > 0 ? viewDto.SelectedColumnsList.Max(o => o.SortOrder) + 1 : 1,
                });
            }

            //if (viewDto.DictAllColumns.ContainsKey(uniqTableOrAliasName) && viewDto.DictAllColumns[uniqTableOrAliasName].ContainsKey(columnName))
            //{
            //    viewDto.DictAllColumns[uniqTableOrAliasName][columnName] = true;
            //}
        }

        //public static DatabaseViewDto AddTablesToDatabaseView(DatabaseViewAddTableDto addTableDto)
        //{
        //    DatabaseViewDto viewDto = addTableDto as DatabaseViewDto;

        //    if (addTableDto != null && addTableDto.NeedToAddOwnerTablePairList != null && addTableDto.NeedToAddOwnerTablePairList.Count > 0)
        //    {               

        //        Dictionary<string, DatabaseTable> dictDBTables = AppMetaDataBL.GetDatabaseTableSchemaDictionaryByTableNames(addTableDto.NeedToAddOwnerTablePairList, addTableDto.DataSourceRegisterId);

        //        foreach (KeyValuePair<string, string> ownerTablePair in addTableDto.NeedToAddOwnerTablePairList)
        //        {
        //            string ownerTableKey = AppMetaDataBL.GetOwnerTableKey(ownerTablePair.Key, ownerTablePair.Value);

        //            if (dictDBTables.ContainsKey(ownerTableKey))
        //            {
        //                var dbTable = dictDBTables[ownerTableKey];
        //                string tableName = dbTable.Name;

        //                string uniqTableOrAliasName = dbTable.Name;

        //                int aliasIndex = 1;

        //                while (viewDto.DictTables.ContainsKey(uniqTableOrAliasName))
        //                {
        //                    uniqTableOrAliasName = tableName + "_" + aliasIndex.ToString();
        //                    aliasIndex++;
        //                }


        //                int positionX = startX + (viewDto.DictTables.Count % 4) * 300;
        //                int positionY = startY + (viewDto.DictTables.Count / 4) * 250;

        //                DatabaseViewTableDto viewTable = new DatabaseViewTableDto() { Width = tableWidth, Height = tableHeight, PositionX = positionX, PositionY = positionY };

        //                viewTable.SortOrder = viewDto.DictTables.Count + 1;
        //                viewTable.TableName = tableName;
        //                viewTable.UniqTableOrAliasName = uniqTableOrAliasName;

        //                if (uniqTableOrAliasName != tableName)
        //                {
        //                    viewTable.TableAlias = uniqTableOrAliasName;
        //                }
        //                else
        //                {
        //                    viewTable.TableAlias = string.Empty;
        //                }

        //                viewDto.DictTables.Add(uniqTableOrAliasName, viewTable);

        //                Dictionary<string, bool> dictTableColumn = dbTable.Columns.ToDictionary(o => o.Name, o => false);
        //                viewDto.DictAllColumns.Add(viewTable.UniqTableOrAliasName, dictTableColumn);

        //            }
        //        }

        //        ConvertViewDtoToQuery(viewDto);
        //    }

        //    return viewDto;
        //}

        //public static DatabaseViewDto AddTablesToDatabaseViewOld(DatabaseViewUpdateDto databaseViewUpdateDto)
        //{
        //    DatabaseViewDto viewDto = null;
        //    if (databaseViewUpdateDto != null && databaseViewUpdateDto.OrgViewDto != null && databaseViewUpdateDto.NeedToAddOwnerTablePairList != null && databaseViewUpdateDto.NeedToAddOwnerTablePairList.Count > 0)
        //    {
        //        viewDto = databaseViewUpdateDto.OrgViewDto;

        //        Dictionary<string, DatabaseTable> dictDBTables = AppMetaDataBL.GetDatabaseTableSchemaDictionaryByTableNames(databaseViewUpdateDto.NeedToAddOwnerTablePairList, databaseViewUpdateDto.DataSourceRegisterId);

        //        foreach (KeyValuePair<string, string> ownerTablePair in databaseViewUpdateDto.NeedToAddOwnerTablePairList)
        //        {
        //            string ownerTableKey = AppMetaDataBL.GetOwnerTableKey(ownerTablePair.Key, ownerTablePair.Value);

        //            if (dictDBTables.ContainsKey(ownerTableKey))
        //            {
        //                var dbTable = dictDBTables[ownerTableKey];
        //                string tableName = dbTable.Name;

        //                string uniqTableOrAliasName = dbTable.Name;

        //                int aliasIndex = 1;

        //                while (viewDto.DictTables.ContainsKey(uniqTableOrAliasName))
        //                {
        //                    uniqTableOrAliasName = tableName + "_" + aliasIndex.ToString();
        //                    aliasIndex++;
        //                }


        //                int positionX = startX + (viewDto.DictTables.Count % 4) * 300;
        //                int positionY = startY + (viewDto.DictTables.Count / 4) * 250;

        //                DatabaseViewTableDto viewTable = new DatabaseViewTableDto() { Width = tableWidth, Height = tableHeight, PositionX = positionX, PositionY = positionY };

        //                viewTable.SortOrder = viewDto.DictTables.Count + 1;
        //                viewTable.TableName = tableName;
        //                viewTable.UniqTableOrAliasName = uniqTableOrAliasName;

        //                if (uniqTableOrAliasName != tableName)
        //                {
        //                    viewTable.TableAlias = uniqTableOrAliasName;
        //                }
        //                else
        //                {
        //                    viewTable.TableAlias = string.Empty;
        //                }

        //                viewDto.DictTables.Add(uniqTableOrAliasName, viewTable);

        //                Dictionary<string, bool> dictTableColumn = dbTable.Columns.ToDictionary(o => o.Name, o => false);
        //                viewDto.DictAllColumns.Add(viewTable.UniqTableOrAliasName, dictTableColumn);

        //            }
        //        }

        //        ConvertViewDtoToQuery(viewDto);
        //    }

        //    return viewDto;
        //}

        public static DatabaseViewDto AddTablesToDatabaseView(ViewTableAddRemoveDto viewTableAddRemoveDto)
        {
            DatabaseViewDto viewDto = viewTableAddRemoveDto as DatabaseViewDto;

            if (viewTableAddRemoveDto != null && viewDto != null && viewTableAddRemoveDto.NeedToAddOwnerTablePairList != null && viewTableAddRemoveDto.NeedToAddOwnerTablePairList.Count > 0)
            {

                Dictionary<string, DatabaseTable> dictDBTables = AppMetaDataBL.GetDatabaseTableSchemaDictionaryBySchemaOwnerTableNames(viewTableAddRemoveDto.NeedToAddOwnerTablePairList, viewTableAddRemoveDto.DataSourceRegisterId);


                var dbfixture = AppCacheManagerBL.GetOneDatabaseFixture(viewTableAddRemoveDto.DataSourceRegisterId.Value);

                List<string> allTablesList = dictDBTables.Values.Select(o => o.Name).ToList();

                var dictTableNameRefParentFkList = dbfixture.GetMutipleTableReferenceParentTables(allTablesList);
                var dictTableNameRefedChildList = dbfixture.GetMutipleTableReferencedChildTables(allTablesList);

                //  var dictTableNameRefParentFkList = dbfixture.GetMutipleTableReferenceParentTables(allTablesList);

                //   var dictTableNameRefedChildList = dbfixture.GetMutipleTableReferencedChildTables(allTablesList);

                List<DatabaseViewJoinConditionDto> needToAddJoinList = new List<DatabaseViewJoinConditionDto>();

                foreach (KeyValuePair<string, string> ownerTablePair in viewTableAddRemoveDto.NeedToAddOwnerTablePairList)
                {
                    string ownerTableKey = AppMetaDataBL.GetOwnerTableKey(ownerTablePair.Key, ownerTablePair.Value);

                    if (dictDBTables.ContainsKey(ownerTableKey))
                    {
                        var dbTable = dictDBTables[ownerTableKey];
                        string tableName = dbTable.Name;

                        if (viewDto.IsErDiagram)
                        {
                            if (viewDto.DictTables.ContainsKey(tableName))
                            {
                                continue;
                            }
                        }

                        string uniqTableOrAliasName = dbTable.Name;

                        int aliasIndex = 1;

                        while (viewDto.DictTables.ContainsKey(uniqTableOrAliasName))
                        {
                            uniqTableOrAliasName = tableName + "_" + aliasIndex.ToString();
                            aliasIndex++;
                        }


                        int positionIndex = GetNextFreePositionIndex(viewDto);

                        int positionX = startX + (positionIndex % totalDiagramColumns) * 300;
                        int positionY = startY + (positionIndex / totalDiagramColumns) * 250;

                        DatabaseViewTableDto viewTable = new DatabaseViewTableDto() { Width = tableWidth, Height = tableHeight, PositionX = positionX, PositionY = positionY };

                        viewTable.SortOrder = viewDto.DictTables.Count + 1;
                        viewTable.SchemaOwner = dbTable.SchemaOwner;
                        viewTable.TableName = tableName;
                        viewTable.UniqTableOrAliasName = uniqTableOrAliasName;

                        if (dictTableNameRefParentFkList.ContainsKey(tableName))
                        {
                            viewTable.FKRefTables = dictTableNameRefParentFkList[tableName].Distinct().ToList();
                        }


                        if (dictTableNameRefedChildList.ContainsKey(tableName))
                        {
                            viewTable.FKRefedTables = dictTableNameRefedChildList[tableName].Distinct().ToList();
                        }



                        viewTable.PkNames = new List<string>();
                        if (dbTable.PrimaryKeyColumnList != null)
                        {
                            viewTable.PkNames = dbTable.PrimaryKeyColumnList.Select(o => o.Name).ToList();
                        }

                        if (uniqTableOrAliasName != tableName)
                        {
                            viewTable.TableAlias = uniqTableOrAliasName;
                        }
                        else
                        {
                            viewTable.TableAlias = string.Empty;
                        }

                        viewDto.DictTables.Add(uniqTableOrAliasName, viewTable);

                        Dictionary<string, bool> dictTableColumn = new Dictionary<string, bool>();

                        if (viewDto.IsErDiagram)
                        {
                            dictTableColumn = dbTable.Columns.ToDictionary(
                                o => o.Name,
                                o => viewTable.PkNames.Contains(o.Name) ? true : false);
                        }
                        else
                        {
                            dictTableColumn = dbTable.Columns.ToDictionary(o => o.Name, o => false);
                        }

                        viewDto.DictAllColumns.Add(viewTable.UniqTableOrAliasName.ToLower(), dictTableColumn);


                        //if (!string.IsNullOrWhiteSpace(viewTableAddRemoveDto.AddFkRefTableFromTableName))
                        //{
                        //    string fromTableName = viewTableAddRemoveDto.AddFkRefTableFromTableName;

                        //    string fromTable_ownerTableKey = AppMetaDataBL.GetOwnerTableKey(ownerTablePair.Key, fromTableName);

                        //    if (dictDBTables.ContainsKey(fromTable_ownerTableKey))
                        //    {
                        //        var fromTableDto = dictDBTables[fromTable_ownerTableKey];


                        //        var foundFk = dbTable.ForeignKeys.FirstOrDefault(o => o.RefersToTable.ToLower() == fromTableName.ToLower());
                        //        if (foundFk != null)
                        //        {
                        //            DatabaseViewJoinConditionDto joinDto = new DatabaseViewJoinConditionDto();
                        //            joinDto.LeftSideSchemaOwner = dbTable.SchemaOwner;
                        //            joinDto.LeftSideTable = dbTable.Name;
                        //            joinDto.LeftSideColumn = foundFk.Columns.First();

                        //        }
                        //        else
                        //        { 

                        //        }
                        //    }                   

                        //}
                    }
                }

                if (viewDto.IsErDiagram)
                {
                    RebuildErDiagramFkLinks(viewDto);
                }
                else
                {
                    ConvertViewDtoToQuery(viewDto);
                }
            }

            return viewDto;
        }


        //public static DatabaseViewDto AddNewTableToErDiagram(ViewTableAddRemoveDto viewTableAddRemoveDto)
        //{
        //    DatabaseViewDto viewDto = viewTableAddRemoveDto as DatabaseViewDto;

        //    if (viewTableAddRemoveDto != null && viewDto != null && viewTableAddRemoveDto.NeedToAddOwnerTablePairList != null && viewTableAddRemoveDto.NeedToAddOwnerTablePairList.Count > 0)
        //    {

        //        string tableName = "NewTable" + ExtensionMethodhelper.RandomId();                              

        //        string uniqTableOrAliasName = tableName;

        //        int positionIndex = GetNextFreePositionIndex(viewDto);

        //        int positionX = startX + (positionIndex % totalDiagramColumns) * 300;
        //        int positionY = startY + (positionIndex / totalDiagramColumns) * 250;

        //        DatabaseViewTableDto viewTable = new DatabaseViewTableDto() { Width = tableWidth, Height = tableHeight, PositionX = positionX, PositionY = positionY };

        //        viewTable.SortOrder = viewDto.DictTables.Count + 1;
        //        viewTable.SchemaOwner = dbTable.SchemaOwner;
        //        viewTable.TableName = tableName;
        //        viewTable.UniqTableOrAliasName = uniqTableOrAliasName;

        //        viewTable.PkNames = new List<string>();
        //        if (dbTable.PrimaryKeyColumnList != null)
        //        {
        //            viewTable.PkNames = dbTable.PrimaryKeyColumnList.Select(o => o.Name).ToList();
        //        }

        //        if (uniqTableOrAliasName != tableName)
        //        {
        //            viewTable.TableAlias = uniqTableOrAliasName;
        //        }
        //        else
        //        {
        //            viewTable.TableAlias = string.Empty;
        //        }

        //        viewDto.DictTables.Add(uniqTableOrAliasName, viewTable);

        //        Dictionary<string, bool> dictTableColumn = new Dictionary<string, bool>();

        //        if (viewDto.IsErDiagram)
        //        {
        //            dictTableColumn = dbTable.Columns.ToDictionary(
        //                o => o.Name,
        //                o => viewTable.PkNames.Contains(o.Name) ? true : false);
        //        }
        //        else
        //        {
        //            dictTableColumn = dbTable.Columns.ToDictionary(o => o.Name, o => false);
        //        }

        //        viewDto.DictAllColumns.Add(viewTable.UniqTableOrAliasName, dictTableColumn);


        //        RebuildErDiagramFkLinks(viewDto);
        //    }


        //    return viewDto;
        //}


        public static DatabaseViewDto RemoveTablesFromDatabaseView(ViewTableAddRemoveDto viewTableAddRemoveDto)
        {
            DatabaseViewDto viewDto = viewTableAddRemoveDto as DatabaseViewDto;

            try
            {
                if (viewTableAddRemoveDto != null && viewDto != null && viewTableAddRemoveDto.NeedToRemoveUniqTableOrAliasNames != null && viewTableAddRemoveDto.NeedToRemoveUniqTableOrAliasNames.Count > 0)
                {

                    foreach (var uniqTableOrAliasName in viewTableAddRemoveDto.NeedToRemoveUniqTableOrAliasNames)
                    {



                        if (viewDto.DictTables.ContainsKey(uniqTableOrAliasName))
                        {
                            var tableObj = viewDto.DictTables[uniqTableOrAliasName];
                            string schemaOwner = tableObj.SchemaOwner;
                            string tableName = tableObj.TableName;
                            string tableAlias = tableObj.TableAlias;

                            if (viewTableAddRemoveDto.IsNeedToDropTableFromDb)
                            {
                                var dbTableDto = AppMetaDataBL.GetOneDatabaseTableSchema(tableName, viewTableAddRemoveDto.DataSourceRegisterId, schemaOwner);
                                string errorMsg = AppMetaDataBL.DropDatabaseTable(dbTableDto);

                                if (!string.IsNullOrWhiteSpace(errorMsg))
                                {
                                    viewDto.ErrorMessage = errorMsg;
                                    return viewDto;
                                }
                            }

                            viewDto.DictTables.Remove(uniqTableOrAliasName);


                            List<string> allTablesList = viewDto.DictTables.Values.Select(o => o.TableName).ToList();
                            var dbfixture = AppCacheManagerBL.GetOneDatabaseFixture(viewTableAddRemoveDto.DataSourceRegisterId.Value);


                            var dictTableNameRefParentFkList = dbfixture.GetMutipleTableReferenceParentTables(allTablesList);
                            var dictTableNameRefedChildList = dbfixture.GetMutipleTableReferencedChildTables(allTablesList);

                            //  var dictTableNameRefParentFkList = dbfixture.GetMutipleTableReferenceParentTables(allTablesList);

                            //   var dictTableNameRefedChildList = dbfixture.GetMutipleTableReferencedChildTables(allTablesList);


                            foreach (DatabaseViewTableDto DatabaseViewTableDto in viewDto.DictTables.Values)
                            {
                                if (dictTableNameRefParentFkList.ContainsKey(DatabaseViewTableDto.TableName))
                                {
                                    DatabaseViewTableDto.FKRefTables = dictTableNameRefParentFkList[DatabaseViewTableDto.TableName].Distinct().ToList();
                                }
                                if (dictTableNameRefedChildList.ContainsKey(DatabaseViewTableDto.TableName))
                                {
                                    DatabaseViewTableDto.FKRefedTables = dictTableNameRefedChildList[DatabaseViewTableDto.TableName].Distinct().ToList();
                                }

                            }



                            if (viewDto.DictAllColumns.ContainsKey(uniqTableOrAliasName.ToLower()))
                            {
                                viewDto.DictAllColumns.Remove(uniqTableOrAliasName.ToLower());
                            }

                            foreach (var join in viewDto.Joins)
                            {
                                List<DatabaseViewJoinConditionDto> needToRemoveConditions = new List<DatabaseViewJoinConditionDto>();
                                foreach (var joinCondition in join.JoinConditionList)
                                {
                                    if (string.IsNullOrEmpty(joinCondition.LeftSideSchemaOwner) && string.IsNullOrEmpty(schemaOwner) || joinCondition.LeftSideSchemaOwner == schemaOwner)
                                    {
                                        if (joinCondition.LeftSideTable == uniqTableOrAliasName)
                                        {
                                            needToRemoveConditions.Add(joinCondition);
                                        }
                                    }
                                    else if (string.IsNullOrEmpty(joinCondition.RightSideSchemaOwner) && string.IsNullOrEmpty(schemaOwner) || joinCondition.RightSideSchemaOwner == schemaOwner)
                                    {
                                        if (joinCondition.RightSideTable == uniqTableOrAliasName)
                                        {
                                            needToRemoveConditions.Add(joinCondition);
                                        }
                                    }
                                }

                                needToRemoveConditions.ForAll(o => join.JoinConditionList.Remove(o));
                            }

                            List<DatabaseViewJoinDto> needToRemoveJoins = viewDto.Joins.Where(o => o.JoinConditionList.Count == 0).ToList();
                            needToRemoveJoins.ForAll(o => viewDto.Joins.Remove(o));


                            if (viewDto.SelectedColumnsList != null)
                            {
                                var needdToRemoveSelectedColumns = viewDto.SelectedColumnsList.Where(o => o.UniqTableOrAliasName.ToLower() == uniqTableOrAliasName.ToLower()).ToList();
                                needdToRemoveSelectedColumns.ForAll(o => viewDto.SelectedColumnsList.Remove(o));
                            }

                            if (viewDto.WhereConditionFilterColumns != null)
                            {
                                var needdToRemoveWhereColumns = viewDto.WhereConditionFilterColumns.Where(o => o.UniqTableOrAliasName.ToLower() == uniqTableOrAliasName.ToLower()).ToList();
                                needdToRemoveWhereColumns.ForAll(o => viewDto.WhereConditionFilterColumns.Remove(o));
                            }
                        }
                    }

                    if (viewDto.IsErDiagram)
                    {
                        RebuildErDiagramFkLinks(viewDto);

                    }
                    else
                    {
                        ConvertViewDtoToQuery(viewDto);
                    }
                }
            }
            catch (Exception ex)
            {
                viewDto.ErrorMessage = ex.ToString();
                return viewDto;
            }

            return viewDto;
        }



        public static DatabaseViewDto AddOneJoinConditionLineToDatabaseView(ViewJoinUpdateDto viewJoinUpdateDto)
        {


            if (viewJoinUpdateDto != null && viewJoinUpdateDto.NeedToAddJoinCondition != null)
            {

                var needToAddJoinCondition = viewJoinUpdateDto.NeedToAddJoinCondition;

                needToAddJoinCondition.GUID = Guid.NewGuid();
                needToAddJoinCondition.JoinOperationType = JoinConditionOperationType.EqualTo;


                var tempJoin = new DatabaseViewJoinDto();
                tempJoin.GUID = Guid.NewGuid();
                tempJoin.JoinConditionList = new List<DatabaseViewJoinConditionDto>();
                tempJoin.JoinMethod = JoinMethod.InnerJoin;
                tempJoin.SortOrder = 1;

                viewJoinUpdateDto.Joins.ForAll(o => o.SortOrder++);



                tempJoin.JoinConditionList = new List<DatabaseViewJoinConditionDto>();
                tempJoin.JoinConditionList.Add(needToAddJoinCondition);



                viewJoinUpdateDto.Joins.Add(tempJoin);

                if (viewJoinUpdateDto.SelectedColumnsList.Count == 0)
                {
                    AddSelectedColumns(viewJoinUpdateDto, needToAddJoinCondition.LeftSideTable, needToAddJoinCondition.LeftSideColumn);
                }

                ConvertViewDtoToQuery(viewJoinUpdateDto);

                DatabaseViewDto newViewDto = ConvertQueryToViewDto(viewJoinUpdateDto.QueryString, viewJoinUpdateDto.DataSourceRegisterId, viewJoinUpdateDto);
                newViewDto.SelectedColumnsList = viewJoinUpdateDto.SelectedColumnsList;

                return newViewDto;
            }

            return viewJoinUpdateDto;
        }
        public static DatabaseViewDto AddOneJoinConditionLineToDatabaseViewOld(ViewJoinUpdateDto viewJoinUpdateDto)
        {
            DatabaseViewDto viewDto = viewJoinUpdateDto as DatabaseViewDto;

            if (viewJoinUpdateDto != null && viewDto != null && viewJoinUpdateDto.NeedToAddJoinCondition != null)
            {
                var tempJoin = new DatabaseViewJoinDto();
                tempJoin.GUID = Guid.NewGuid();
                tempJoin.JoinConditionList = new List<DatabaseViewJoinConditionDto>();
                tempJoin.JoinMethod = JoinMethod.InnerJoin;
                tempJoin.SortOrder = 1;
                viewDto.Joins.ForAll(o => o.SortOrder++);


                viewJoinUpdateDto.NeedToAddJoinCondition.GUID = Guid.NewGuid();
                viewJoinUpdateDto.NeedToAddJoinCondition.JoinOperationType = JoinConditionOperationType.EqualTo;
                tempJoin.JoinConditionList = new List<DatabaseViewJoinConditionDto>();
                tempJoin.JoinConditionList.Add(viewJoinUpdateDto.NeedToAddJoinCondition);

                viewDto.Joins.Add(tempJoin);

                if (viewDto.SelectedColumnsList.Count == 0)
                {
                    AddSelectedColumns(viewDto, viewJoinUpdateDto.NeedToAddJoinCondition.LeftSideTable, viewJoinUpdateDto.NeedToAddJoinCondition.LeftSideColumn);
                }

                ConvertViewDtoToQuery(viewDto);
                var newViewDto = ConvertQueryToViewDto(viewDto.QueryString, viewJoinUpdateDto.DataSourceRegisterId, viewDto);
                newViewDto.SelectedColumnsList = viewDto.SelectedColumnsList;
                viewDto = newViewDto;
            }

            return viewDto;
        }

        public static DatabaseViewDto RemoveJoinConditionLinesFromDatabaseView(ViewJoinUpdateDto viewJoinUpdateDto)
        {
            DatabaseViewDto viewDto = viewJoinUpdateDto as DatabaseViewDto;

            if (viewJoinUpdateDto != null && viewDto != null && viewJoinUpdateDto.NeedToRemoveJoinConditionGUIDs != null && viewJoinUpdateDto.NeedToRemoveJoinConditionGUIDs.Count > 0)
            {
                foreach (var needToRemoveLineGuid in viewJoinUpdateDto.NeedToRemoveJoinConditionGUIDs)
                {
                    foreach (var join in viewDto.Joins)
                    {
                        DatabaseViewJoinConditionDto needToRemoveCondition = join.JoinConditionList.FirstOrDefault(o => o.GUID == needToRemoveLineGuid);
                        if (needToRemoveCondition != null)
                        {
                            join.JoinConditionList.Remove(needToRemoveCondition);
                        }
                    }
                }

                List<DatabaseViewJoinDto> needToRemoveJoins = viewDto.Joins.Where(o => o.JoinConditionList.Count == 0).ToList();
                needToRemoveJoins.ForAll(o => viewDto.Joins.Remove(o));

                ConvertViewDtoToQuery(viewDto);
                var newViewDto = ConvertQueryToViewDto(viewDto.QueryString, viewJoinUpdateDto.DataSourceRegisterId, viewDto);
                newViewDto.SelectedColumnsList = viewDto.SelectedColumnsList;
                viewDto = newViewDto;

            }

            return viewDto;
        }

        public static DatabaseViewDto UpdateDatabaseViewJoinMethod(ViewJoinUpdateDto viewJoinUpdateDto)
        {
            DatabaseViewDto viewDto = viewJoinUpdateDto as DatabaseViewDto;

            if (viewJoinUpdateDto != null && viewDto != null
                && viewJoinUpdateDto.NeedToUpdateJoinMethodConditionDto != null && viewJoinUpdateDto.NeedToUpdateJoinMethodConditionDto.GUID != null)
            {
                foreach (var join in viewDto.Joins)
                {
                    DatabaseViewJoinConditionDto condition = join.JoinConditionList.FirstOrDefault(o => o.GUID == viewJoinUpdateDto.NeedToUpdateJoinMethodConditionDto.GUID);
                    if (condition != null)
                    {
                        if (viewJoinUpdateDto.IsUpdateJoinMethodForLeftTable)
                        {
                            if (join.JoinMethod == JoinMethod.InnerJoin)
                            {
                                join.JoinMethod = JoinMethod.LeftOuterJoin;
                            }
                            else if (join.JoinMethod == JoinMethod.LeftOuterJoin)
                            {
                                join.JoinMethod = JoinMethod.InnerJoin;
                            }
                            else if (join.JoinMethod == JoinMethod.RightOuterJoin)
                            {
                                join.JoinMethod = JoinMethod.FullOuterJoin;
                            }
                            else if (join.JoinMethod == JoinMethod.FullOuterJoin)
                            {
                                join.JoinMethod = JoinMethod.RightOuterJoin;
                            }
                        }
                        else
                        {
                            if (join.JoinMethod == JoinMethod.InnerJoin)
                            {
                                join.JoinMethod = JoinMethod.RightOuterJoin;
                            }
                            else if (join.JoinMethod == JoinMethod.LeftOuterJoin)
                            {
                                join.JoinMethod = JoinMethod.FullOuterJoin;
                            }
                            else if (join.JoinMethod == JoinMethod.RightOuterJoin)
                            {
                                join.JoinMethod = JoinMethod.InnerJoin;
                            }
                            else if (join.JoinMethod == JoinMethod.FullOuterJoin)
                            {
                                join.JoinMethod = JoinMethod.LeftOuterJoin;
                            }
                        }
                    }
                }

                List<DatabaseViewJoinDto> needToRemoveJoins = viewDto.Joins.Where(o => o.JoinConditionList.Count == 0).ToList();
                needToRemoveJoins.ForAll(o => viewDto.Joins.Remove(o));

                ConvertViewDtoToQuery(viewDto);
                var newViewDto = ConvertQueryToViewDto(viewDto.QueryString, viewJoinUpdateDto.DataSourceRegisterId, viewDto);
                newViewDto.SelectedColumnsList = viewDto.SelectedColumnsList;
                viewDto = newViewDto;

            }

            return viewDto;
        }

        public static OperationCallResult<DatabaseViewUpdateDto> QuickGenerateTransactionDefaultSeachNavigation(int transactionId, DatabaseTableImportSettingDto importSettingDto = null, int? importSettingDataSetId = null)
        {
            OperationCallResult<DatabaseViewUpdateDto> aOperationCallResult = new OperationCallResult<DatabaseViewUpdateDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;



            var formResult = EnsureTransactionDefaultFlexFormLayout(transactionId);
            validationResult.Merge(formResult.ValidationResult);
            if (validationResult.HasErrors)
                return aOperationCallResult;

            var existingNavigationDto = AppTransactionNavigationBL.RetrieveOneTransactionDefaultNavigationDto(transactionId, false);

            if (existingNavigationDto != null && existingNavigationDto.DefaultSearchDto != null)
            {
                var searchDto = existingNavigationDto.DefaultSearchDto;

                if (importSettingDto != null)
                {
                    importSettingDto.DefaultSearchId = ControlTypeValueConverter.ConvertValueToInt(searchDto.Id);
                }

                if (existingNavigationDto.DefaultMenuDto != null)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SaveDataSetAndCreateSearchView_OK", ValidationItemType.Warning,
                     "App menu already exists. \n" + " Menu path: " + existingNavigationDto.DefaultMenuDto.MenuPath));
                }
                else
                {
                    QuickGenerateTransactionDefaultSeachNavigation_GenerateAppMenu(validationResult, (int)searchDto.Id, searchDto.SaasApplicationId, searchDto.Name, searchDto.Description);
                }
            }
            else
            {
                var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
                OperationCallResult<DatabaseViewUpdateDto> dbviewResult = BuildAdvancedDBViewDtoFromTransaction(transactionId, true);

                if (!dbviewResult.ValidationResult.HasErrors && dbviewResult.Object != null)
                {
                    var dbViewDto = dbviewResult.Object;

                    bool isDraft = importSettingDto != null && importSettingDto.IsDraft;

                    bool isWorkflow = transactionExDto.BusinessScopeId.HasValue && transactionExDto.BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation;

                    if (isWorkflow)
                    {
                        if (dbViewDto.OrgViewDto != null && !string.IsNullOrWhiteSpace(dbViewDto.OrgViewDto.QueryString))
                        {
                            dbViewDto.OrgViewDto.QueryString = "SELECT * FROM (" + dbViewDto.OrgViewDto.QueryString + ") as ResultTable WHERE WorkflowTransactionId = " + transactionId;
                        }
                    }



                    var saveSearchViewResult = SaveDataSetAndCreateSearchView(dbViewDto, null, importSettingDataSetId, isDraft);

                    if (!saveSearchViewResult.ValidationResult.HasErrors && saveSearchViewResult.Object != null)
                    {
                        dbViewDto = saveSearchViewResult.Object;

                        if (dbViewDto.SearchId.HasValue && dbViewDto.DataSetDto != null)
                        {
                            //AppTransactionNavigationBL.RetrieveTransactionDefaultNavigationDto

                            QuickGenerateTransactionDefaultSeachNavigation_GenerateAppMenu(validationResult,
                                dbViewDto.SearchId.Value, dbViewDto.DataSetDto.SaasApplicationId, dbViewDto.DataSetDto.Name, dbViewDto.DataSetDto.Description);

                            if (importSettingDto != null)
                            {
                                importSettingDto.DefaultSearchId = dbViewDto.SearchId.Value;
                            }
                        }

                    }
                    else
                    {
                        validationResult.Merge(saveSearchViewResult.ValidationResult);
                    }
                }
                else
                {
                    validationResult.Merge(dbviewResult.ValidationResult);
                }

            }


            return aOperationCallResult;
        }


        public static OperationCallResult<AppDataSetExDto> ResetTransactionDefaultNavigationSearchDataSet(int transactionId)
        {
            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;



            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            if (transactionExDto.DefaultNavigationSearchId.HasValue)
            {
                int searchId = transactionExDto.DefaultNavigationSearchId.Value;

                var searrchEntity = AppSearchConfigBL.RetrieveOneAppSearchEntity(searchId);

                if (searrchEntity.DataSetId.HasValue)
                {
                    var orgDataSetExDto = AppDataSetBL.RetrieveOneAppDataSetExDto(searrchEntity.DataSetId.Value);

                    OperationCallResult<DatabaseViewUpdateDto> dbviewResult = BuildAdvancedDBViewDtoFromTransaction(transactionId, true);

                    if (!dbviewResult.ValidationResult.HasErrors && dbviewResult.Object != null)
                    {
                        var databaseViewUpdateDto = dbviewResult.Object;

                        if (databaseViewUpdateDto != null && databaseViewUpdateDto.OrgViewDto != null)
                        {
                            var dbviewDto = databaseViewUpdateDto.OrgViewDto;

                            orgDataSetExDto.QueryText = dbviewDto.QueryString;
                            orgDataSetExDto.IsModified = true;

                            OperationCallResult<AppDataSetExDto> dataSetSaveResult = AppDataSetBL.SaveOneAppDataSetEntityDto(orgDataSetExDto);
                            if (dataSetSaveResult.IsSuccessfulWithResult)
                            {
                                return dataSetSaveResult;
                            }
                            else
                            {
                                validationResult.Merge(dataSetSaveResult.ValidationResult);
                            }
                        }
                    }
                    else
                    {
                        validationResult.Merge(dbviewResult.ValidationResult);
                    }
                }
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<DatabaseViewUpdateDto> SaveDataSetAndCreateSearchView(DatabaseViewUpdateDto databaseViewUpdateDto, int? folderTransactionId = null, int? importSettingid = null, bool isDraft = false)
        {
            OperationCallResult<DatabaseViewUpdateDto> aOperationCallResult = new OperationCallResult<DatabaseViewUpdateDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (databaseViewUpdateDto != null && databaseViewUpdateDto.OrgViewDto != null && databaseViewUpdateDto.DataSetDto != null)
            {
                var dbviewDto = databaseViewUpdateDto.OrgViewDto;
                var dataSetDto = databaseViewUpdateDto.DataSetDto;
                AppSearchViewExDto searchViewDto = null;
                AppSearchExDto searchDto = null;

                dataSetDto.QueryText = dbviewDto.QueryString;
                dataSetDto.IsModified = true;

                OperationCallResult<AppDataSetExDto> dataSetSaveResult = AppDataSetBL.SaveOneAppDataSetEntityDto(dataSetDto);
                if (dataSetSaveResult.IsSuccessfulWithResult)
                {
                    dataSetDto = dataSetSaveResult.Object;
                }
                else
                {
                    validationResult.Merge(dataSetSaveResult.ValidationResult);
                }

                if (!validationResult.HasErrors)
                {
                    searchViewDto = new AppSearchViewExDto();
                    searchViewDto.Name = dataSetDto.Name;
                    searchViewDto.Description = dataSetDto.Description;
                    searchViewDto.DataSetId = ControlTypeValueConverter.ConvertValueToInt(dataSetDto.Id);
                    searchViewDto.ViewType = (int)EmAppViewType.GridView;
                    searchViewDto.AppSearchViewFieldList = new ObservableSet<AppSearchViewFieldExDto>();
                    searchViewDto.OtherSettingsDto = new AppSearchViewOtherSettingsDto();
                    searchViewDto.OtherSettingsDto.ImportSettingId = importSettingid;
                    searchViewDto.OtherSettingsDto.IsDraft = isDraft;

                    foreach (var column in dbviewDto.SelectedColumnsList.OrderBy(o => o.SortOrder))
                    {
                        if (column.IsUsedAsViewField)
                        {
                            AppSearchViewFieldExDto newSearchViewField = new AppSearchViewFieldExDto();
                            newSearchViewField.IsModified = true;
                            newSearchViewField.IsVisible = true;
                            newSearchViewField.SysTableFiledPath = column.ColumnDisplayName;
                            newSearchViewField.DisplayText = column.ColumnDisplayName;

                            if (!string.IsNullOrEmpty(column.SearchViewDisplayName))
                            {
                                newSearchViewField.DisplayText = column.SearchViewDisplayName;
                            }

                            if (column.EntityId.HasValue)
                            {
                                newSearchViewField.ControlType = (int)EmAppControlType.DDL;
                                newSearchViewField.EntityId = column.EntityId;
                            }
                            else
                            {
                                if (column.ControlType.HasValue)
                                {
                                    newSearchViewField.ControlType = column.ControlType.Value;
                                }
                                else
                                {
                                    newSearchViewField.ControlType = (int)EmAppControlType.TextBox;
                                }
                            }


                            searchViewDto.AppSearchViewFieldList.Add(newSearchViewField);
                        }
                    }

                    OperationCallResult<AppSearchViewExDto> searchViewSaveResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewDto);
                    if (searchViewSaveResult.IsSuccessfulWithResult)
                    {
                        searchViewDto = searchViewSaveResult.Object;

                        databaseViewUpdateDto.SearchViewId = ControlTypeValueConverter.ConvertValueToInt(searchViewDto.Id);

                        if (databaseViewUpdateDto.TransactionId.HasValue && databaseViewUpdateDto.SearchViewId.HasValue
                            && databaseViewUpdateDto.NeedToCreateLinkTargetActions != null && databaseViewUpdateDto.NeedToCreateLinkTargetActions.Count > 0
                            && !string.IsNullOrEmpty(databaseViewUpdateDto.TransactionRootPrimaryKey))
                        {
                            var primaryKeySearchViewField = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == databaseViewUpdateDto.TransactionRootPrimaryKey);

                            if (primaryKeySearchViewField != null)
                            {
                                ObservableSet<AppFormLinkTargetDto> aAppFormLinkTargetDtoSet = new ObservableSet<AppFormLinkTargetDto>();

                                foreach (EmAppLinkTargetActionType action in databaseViewUpdateDto.NeedToCreateLinkTargetActions)
                                {
                                    AppFormLinkTargetDto aAppFormLinkTargetDto = new AppFormLinkTargetDto();
                                    aAppFormLinkTargetDto.ActionType = (int)action;
                                    aAppFormLinkTargetDto.IsPopup = true;
                                    aAppFormLinkTargetDto.LinkTargetUsageType = (int)EmAppLinkTargetUsageType.SearchViewLinkToForm;
                                    aAppFormLinkTargetDto.NavigationActionName = action.ToString();
                                    if (aAppFormLinkTargetDto.NavigationActionName == EmAppLinkTargetActionType.Edit.ToString())
                                    {
                                        aAppFormLinkTargetDto.NavigationActionName = "Open";
                                    }
                                    aAppFormLinkTargetDto.SearchViewId = databaseViewUpdateDto.SearchViewId.Value;
                                    aAppFormLinkTargetDto.LinkTargetTransactionId = databaseViewUpdateDto.TransactionId.Value;
                                    aAppFormLinkTargetDto.SourceViewColumnId1 = ControlTypeValueConverter.ConvertValueToInt(primaryKeySearchViewField.Id.ToString());
                                    aAppFormLinkTargetDto.SourceColumnType = (int)EmAppLinkTargetSourceColumnType.SearchViewField;
                                    aAppFormLinkTargetDtoSet.Add(aAppFormLinkTargetDto);
                                }

                                OperationCallResult<AppFormLinkTargetDto> linkTargetResult = LinkTragetBL.SaveOneAppFormLinkTargetList((int)EmAppLinkTargetSourceType.SearchView, databaseViewUpdateDto.SearchViewId.Value, aAppFormLinkTargetDtoSet);

                                if (linkTargetResult.IsSuccessfulWithResult)
                                {

                                }
                                else
                                {
                                    validationResult.Merge(linkTargetResult.ValidationResult);
                                }

                            }

                        }

                    }
                    else
                    {
                        validationResult.Merge(searchViewSaveResult.ValidationResult);
                    }
                }

                if (!validationResult.HasErrors)
                {
                    searchDto = new AppSearchExDto();
                    searchDto.Name = dataSetDto.Name;
                    searchDto.Description = dataSetDto.Description;
                    searchDto.DataSetId = ControlTypeValueConverter.ConvertValueToInt(dataSetDto.Id);
                    searchDto.SearchViewId = (int)searchViewDto.Id;
                    searchDto.Type = (int)EmAppSearchUsageType.Management;
                    searchDto.IsAutoExecute = true;
                    searchDto.FolderTransactionId = folderTransactionId;
                    searchDto.SaasApplicationId = dataSetDto.SaasApplicationId;

                    searchDto.BusinessScopeId = null;




                    //if (folderTransactionId.HasValue)
                    //{
                    //    searchDto.Description = folderTransactionId.Value.ToString();
                    //}

                    searchDto.AppSearchFieldList = new ObservableSet<AppSearchFieldExDto>();

                    int searchFieldIndex = 1;
                    foreach (var column in dbviewDto.SelectedColumnsList.OrderBy(o => o.SortOrder))
                    {
                        if (column.IsUsedAsSearchField)
                        {
                            AppSearchFieldExDto newSearchField = new AppSearchFieldExDto();
                            newSearchField.IsModified = true;
                            newSearchField.IsVisible = true;
                            newSearchField.SysTableFiledPath = column.ColumnDisplayName;
                            newSearchField.DisplayText = column.ColumnDisplayName;

                            if (!string.IsNullOrEmpty(column.SearchViewDisplayName))
                            {
                                newSearchField.DisplayText = column.SearchViewDisplayName;
                            }

                            if (column.EntityId.HasValue)
                            {
                                newSearchField.ControlType = (int)EmAppControlType.DDL;
                                newSearchField.EntityId = column.EntityId;
                            }
                            else if (column.ColumnName == "FolderID" && folderTransactionId.HasValue)
                            {
                                newSearchField.ControlType = (int)EmAppControlType.FolderTree;
                            }
                            else
                            {
                                if (column.ControlType.HasValue)
                                {
                                    newSearchField.ControlType = column.ControlType.Value;
                                }
                                else
                                {
                                    newSearchField.ControlType = (int)EmAppControlType.TextBox;
                                }
                            }

                            newSearchField.OperationId = 0;
                            newSearchField.Sort = searchFieldIndex;
                            newSearchField.PositionRow = (searchFieldIndex - 1) / 3 + 1;
                            newSearchField.PositionColumn = ((searchFieldIndex - 1) % 3) + 1;

                            searchDto.AppSearchFieldList.Add(newSearchField);

                            searchFieldIndex++;
                        }
                    }

                    OperationCallResult<AppSearchExDto> searchSaveResult = AppSearchConfigBL.SaveAppSearchExDto(searchDto);
                    if (searchSaveResult.IsSuccessfulWithResult)
                    {
                        searchDto = searchSaveResult.Object;
                        if (databaseViewUpdateDto.TransactionId.HasValue)
                        {
                            AppTransactionNavigationBL.DeleteOneTransactionAllNavigations(databaseViewUpdateDto.TransactionId.Value);

                            ObservableSet<AppTransactionNavigationExDto> aSet = new ObservableSet<AppTransactionNavigationExDto>();
                            aSet.Add(new AppTransactionNavigationExDto()
                            {
                                TransactionId = databaseViewUpdateDto.TransactionId,
                                QuickSearchId = (int)searchDto.Id,
                            });

                            AppTransactionNavigationBL.SaveQuickSearchNavigationListExDto(aSet, databaseViewUpdateDto.TransactionId.Value);
                        }
                    }
                    else
                    {
                        validationResult.Merge(searchSaveResult.ValidationResult);
                    }

                }


                if (!validationResult.HasErrors)
                {
                    databaseViewUpdateDto.SearchId = ControlTypeValueConverter.ConvertValueToInt(searchDto.Id);
                    databaseViewUpdateDto.DataSetDto = dataSetDto;
                    aOperationCallResult.Object = databaseViewUpdateDto;

                    validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SaveDataSetAndCreateSearchView_OK", ValidationItemType.Message, "Save DataSet And Create Search View Successfully"));
                }
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<DatabaseViewUpdateDto> SaveDataSetAndCreateFolderViewNavigation(DatabaseViewUpdateDto databaseViewUpdateDto)
        {
            OperationCallResult<DatabaseViewUpdateDto> aOperationCallResult = new OperationCallResult<DatabaseViewUpdateDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (databaseViewUpdateDto != null && databaseViewUpdateDto.OrgViewDto != null && databaseViewUpdateDto.DataSetDto != null
                && databaseViewUpdateDto.TransactionId.HasValue && databaseViewUpdateDto.RootFolderId.HasValue)
            {

                var existingNavigationDto = AppTransactionNavigationBL.RetrieveOneTransactionDefaultNavigationDto(databaseViewUpdateDto.TransactionId.Value, true);

                if (existingNavigationDto != null && existingNavigationDto.DefaultFolderViewDto != null)
                {
                    var folderViewDto = existingNavigationDto.DefaultFolderViewDto;

                    if (existingNavigationDto.DefaultMenuDto != null)
                    {
                        validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SaveDataSetAndCreateSearchView_OK", ValidationItemType.Warning,
                         "App menu already exists. \n" + " Menu path: " + existingNavigationDto.DefaultMenuDto.MenuPath));
                    }
                    else
                    {
                        QuickGenerateTransactionDefaultFolderNavigation_GenerateAppMenu(validationResult, databaseViewUpdateDto.TransactionId.Value,
                            databaseViewUpdateDto.DataSetDto.SaasApplicationId, databaseViewUpdateDto.DataSetDto.Name, databaseViewUpdateDto.DataSetDto.Description);
                    }
                }
                else
                {
                    var dbviewDto = databaseViewUpdateDto.OrgViewDto;
                    var dataSetDto = databaseViewUpdateDto.DataSetDto;
                    AppSearchViewExDto searchViewDto = null;

                    dataSetDto.Name = dataSetDto.Name + " By Folder";
                    dataSetDto.QueryText = dbviewDto.QueryString;
                    dataSetDto.IsModified = true;

                    OperationCallResult<AppDataSetExDto> dataSetSaveResult = AppDataSetBL.SaveOneAppDataSetEntityDto(dataSetDto);
                    if (dataSetSaveResult.IsSuccessfulWithResult)
                    {
                        dataSetDto = dataSetSaveResult.Object;
                    }
                    else
                    {
                        validationResult.Merge(dataSetSaveResult.ValidationResult);
                    }

                    if (!validationResult.HasErrors)
                    {
                        searchViewDto = new AppSearchViewExDto();
                        searchViewDto.Name = dataSetDto.Name;
                        searchViewDto.Description = dataSetDto.Description;
                        searchViewDto.DataSetId = ControlTypeValueConverter.ConvertValueToInt(dataSetDto.Id);
                        searchViewDto.ViewType = (int)EmAppViewType.GridView;

                        searchViewDto.AppSearchViewFieldList = new ObservableSet<AppSearchViewFieldExDto>();

                        foreach (var column in dbviewDto.SelectedColumnsList.OrderBy(o => o.SortOrder))
                        {
                            if (column.IsUsedAsViewField)
                            {
                                AppSearchViewFieldExDto newSearchViewField = new AppSearchViewFieldExDto();
                                newSearchViewField.IsModified = true;
                                newSearchViewField.IsVisible = true;
                                newSearchViewField.SysTableFiledPath = column.ColumnDisplayName;
                                newSearchViewField.DisplayText = column.ColumnDisplayName;
                                if (!string.IsNullOrEmpty(column.SearchViewDisplayName))
                                {
                                    newSearchViewField.DisplayText = column.SearchViewDisplayName;
                                }


                                if (column.EntityId.HasValue)
                                {
                                    newSearchViewField.ControlType = (int)EmAppControlType.DDL;
                                    newSearchViewField.EntityId = column.EntityId;
                                }
                                else
                                {
                                    if (column.ControlType.HasValue)
                                    {
                                        newSearchViewField.ControlType = column.ControlType.Value;
                                    }
                                    else
                                    {
                                        newSearchViewField.ControlType = (int)EmAppControlType.TextBox;
                                    }
                                }

                                if (column.IsFolderIdKeyField)
                                {
                                    newSearchViewField.IsFileFoderId = true;
                                    newSearchViewField.IsVisible = false;
                                }

                                if (newSearchViewField.SysTableFiledPath.ToLower() == "folderid")
                                {
                                    if (dbviewDto.SelectedColumnsList.FirstOrDefault(o => o.IsFolderIdKeyField) == null)
                                    {
                                        newSearchViewField.IsFileFoderId = true;
                                        newSearchViewField.IsVisible = false;
                                    }
                                }


                                if (!string.IsNullOrEmpty(databaseViewUpdateDto.TransactionRootPrimaryKey)
                                    && newSearchViewField.SysTableFiledPath.ToLower() == databaseViewUpdateDto.TransactionRootPrimaryKey.ToLower())
                                {
                                    newSearchViewField.IsTransRootId = true;
                                }


                                searchViewDto.AppSearchViewFieldList.Add(newSearchViewField);
                            }
                        }

                        if (searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.IsFileFoderId.HasValue && o.IsFileFoderId.Value) == null)
                        {
                            validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SaveDataSetAndCreateSearchView_FolderIdKeyIsNotSelected", ValidationItemType.Error, "FolderId Key is Not Selected."));
                            return aOperationCallResult;
                        }


                        OperationCallResult<AppSearchViewExDto> searchViewSaveResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewDto);
                        if (searchViewSaveResult.IsSuccessfulWithResult)
                        {
                            searchViewDto = searchViewSaveResult.Object;

                            databaseViewUpdateDto.SearchViewId = ControlTypeValueConverter.ConvertValueToInt(searchViewDto.Id);

                            if (databaseViewUpdateDto.SearchViewId.HasValue
                                && databaseViewUpdateDto.NeedToCreateLinkTargetActions != null && databaseViewUpdateDto.NeedToCreateLinkTargetActions.Count > 0
                                && !string.IsNullOrEmpty(databaseViewUpdateDto.TransactionRootPrimaryKey))
                            {
                                var primaryKeySearchViewField = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == databaseViewUpdateDto.TransactionRootPrimaryKey);

                                if (primaryKeySearchViewField != null)
                                {
                                    ObservableSet<AppFormLinkTargetDto> aAppFormLinkTargetDtoSet = new ObservableSet<AppFormLinkTargetDto>();

                                    foreach (EmAppLinkTargetActionType action in databaseViewUpdateDto.NeedToCreateLinkTargetActions)
                                    {
                                        AppFormLinkTargetDto aAppFormLinkTargetDto = new AppFormLinkTargetDto();
                                        aAppFormLinkTargetDto.ActionType = (int)action;
                                        aAppFormLinkTargetDto.IsPopup = true;
                                        aAppFormLinkTargetDto.LinkTargetUsageType = (int)EmAppLinkTargetUsageType.SearchViewLinkToForm;
                                        aAppFormLinkTargetDto.NavigationActionName = action.ToString();
                                        if (aAppFormLinkTargetDto.NavigationActionName == EmAppLinkTargetActionType.Edit.ToString())
                                        {
                                            aAppFormLinkTargetDto.NavigationActionName = "Open";
                                        }
                                        aAppFormLinkTargetDto.SearchViewId = databaseViewUpdateDto.SearchViewId.Value;
                                        aAppFormLinkTargetDto.LinkTargetTransactionId = databaseViewUpdateDto.TransactionId.Value;
                                        aAppFormLinkTargetDto.SourceColumnType = (int)EmAppLinkTargetSourceColumnType.SearchViewField;
                                        aAppFormLinkTargetDto.SourceViewColumnId1 = ControlTypeValueConverter.ConvertValueToInt(primaryKeySearchViewField.Id.ToString());
                                        aAppFormLinkTargetDtoSet.Add(aAppFormLinkTargetDto);

                                    }

                                    OperationCallResult<AppFormLinkTargetDto> linkTargetResult = LinkTragetBL.SaveOneAppFormLinkTargetList((int)EmAppLinkTargetSourceType.SearchView, databaseViewUpdateDto.SearchViewId.Value, aAppFormLinkTargetDtoSet);

                                    if (linkTargetResult.IsSuccessfulWithResult)
                                    {

                                    }
                                    else
                                    {
                                        validationResult.Merge(linkTargetResult.ValidationResult);
                                    }

                                }

                            }

                            if (databaseViewUpdateDto.TransactionId.HasValue && databaseViewUpdateDto.SearchViewId.HasValue)
                            {
                                ObservableSet<AppTransactionNavigationExDto> aSet = new ObservableSet<AppTransactionNavigationExDto>();
                                aSet.Add(new AppTransactionNavigationExDto()
                                {
                                    TransactionId = databaseViewUpdateDto.TransactionId,
                                    FolderViewId = databaseViewUpdateDto.SearchViewId.Value,
                                    IsDefaultView = true,
                                });

                                AppTransactionNavigationBL.SaveFolderViewNavigationListExDto(aSet, databaseViewUpdateDto.TransactionId.Value, databaseViewUpdateDto.RootFolderId.Value, databaseViewUpdateDto.IsEnableFolderSecurity);
                            }

                        }
                        else
                        {
                            validationResult.Merge(searchViewSaveResult.ValidationResult);
                        }
                    }



                    if (!validationResult.HasErrors)
                    {
                        databaseViewUpdateDto.DataSetDto = dataSetDto;
                        aOperationCallResult.Object = databaseViewUpdateDto;

                        QuickGenerateTransactionDefaultFolderNavigation_GenerateAppMenu(validationResult, databaseViewUpdateDto.TransactionId.Value,
                            databaseViewUpdateDto.DataSetDto.SaasApplicationId, databaseViewUpdateDto.DataSetDto.Name, databaseViewUpdateDto.DataSetDto.Description);

                        validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SaveDataSetAndCreateSearchView_OK", ValidationItemType.Message, "Save DataSet And Create Search View Successfully"));
                    }

                }


            }

            return aOperationCallResult;
        }


        public static OperationCallResult<DatabaseViewUpdateDto> CreateSimpleSearchViewFromMasterDetailTransactoin(int transactionId, int? folderTransactionId = null)
        {
            OperationCallResult<DatabaseViewUpdateDto> aOperationCallResult = new OperationCallResult<DatabaseViewUpdateDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            AppTransactionExDto transactionDto = AppTransactionBL.RetrieveOneAppTransactionExDto(transactionId);

            if (transactionDto != null && transactionDto.TransactionOrganizedType.HasValue && (transactionDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail))
            {
                DatabaseViewDto viewDto;
                AppDataSetExDto dataSetDto;
                AppTransactionUnitExDto rootUnit = transactionDto.RootMasterUnit;

                BuildSimpleDBViewDtoFromTransaction(validationResult, transactionDto, out viewDto, out dataSetDto);

                if (!validationResult.HasErrors)
                {

                    DatabaseViewUpdateDto databaseViewUpdateDto = new DatabaseViewUpdateDto();
                    databaseViewUpdateDto.DataSourceRegisterId = transactionDto.DataSourceFrom;
                    databaseViewUpdateDto.OrgViewDto = viewDto;
                    databaseViewUpdateDto.DataSetDto = dataSetDto;
                    databaseViewUpdateDto.TransactionId = transactionId;

                    if (transactionDto.IsReadOnly.HasValue && transactionDto.IsReadOnly.Value)
                    {
                        databaseViewUpdateDto.NeedToCreateLinkTargetActions = new List<EmAppLinkTargetActionType>()
                        {
                            EmAppLinkTargetActionType.Edit,
                        };
                    }
                    else
                    {
                        databaseViewUpdateDto.NeedToCreateLinkTargetActions = new List<EmAppLinkTargetActionType>()
                        {
                            EmAppLinkTargetActionType.Create,
                            EmAppLinkTargetActionType.Edit,
                            EmAppLinkTargetActionType.Delete,
                            //EmAppLinkTargetActionType.Preview
                        };
                    }


                    var rootUnitPrimaryKeyField = rootUnit.AppTransactionFieldList.FirstOrDefault(o => o.IsPrimaryKey);
                    if (rootUnitPrimaryKeyField != null)
                    {
                        databaseViewUpdateDto.TransactionRootPrimaryKey = rootUnitPrimaryKeyField.DataBaseFieldName;
                    }

                    return AppDatabaseViewBL.SaveDataSetAndCreateSearchView(databaseViewUpdateDto, folderTransactionId);

                }

            }
            return aOperationCallResult;
        }




        public static OperationCallResult<DatabaseViewUpdateDto> BuildAdvancedDBViewDtoFromTransaction(int transactionId, bool isOnlyUseRootTable = false)
        {
            OperationCallResult<DatabaseViewUpdateDto> aOperationCallResult = new OperationCallResult<DatabaseViewUpdateDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            AppTransactionExDto transactionDto = AppTransactionBL.RetrieveOneAppTransactionExDto(transactionId);
            DatabaseViewUpdateDto databaseViewUpdateDto = new DatabaseViewUpdateDto();

            if (transactionDto != null
                && transactionDto.TransactionOrganizedType.HasValue
                && (transactionDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail))
            {
                DatabaseViewDto viewDto;
                AppDataSetExDto dataSetDto;
                AppTransactionUnitExDto rootUnit = transactionDto.RootMasterUnit;
                var rootUnitPrimaryKeyField = rootUnit.AppTransactionFieldList.FirstOrDefault(o => o.IsPrimaryKey);


                BuildSimpleDBViewDtoFromTransaction(validationResult, transactionDto, out viewDto, out dataSetDto);

                if (!validationResult.HasErrors && !(transactionDto.IsReadOnly.HasValue && transactionDto.IsReadOnly.Value))
                {
                    ViewTableAddRemoveDto viewTableAddDto = new ViewTableAddRemoveDto(viewDto);

                    viewTableAddDto.NeedToAddOwnerTablePairList = new List<KeyValuePair<string, string>>();

                    foreach (var aUnit in transactionDto.AppTransactionUnitList)
                    {
                        if (aUnit.IsMasterSiblingUnit.HasValue && aUnit.IsMasterSiblingUnit.Value)
                        {
                            viewTableAddDto.NeedToAddOwnerTablePairList.Add(new KeyValuePair<string, string>(aUnit.SchemaOwner, aUnit.DataBaseTableName));
                        }

                        if (aUnit.ParentTransactionUnitId.HasValue && !isOnlyUseRootTable)
                        {
                            viewTableAddDto.NeedToAddOwnerTablePairList.Add(new KeyValuePair<string, string>(aUnit.SchemaOwner, aUnit.DataBaseTableName));
                        }
                    }

                    if (viewTableAddDto.NeedToAddOwnerTablePairList.Count > 0)
                    {
                        viewDto = AppDatabaseViewBL.AddTablesToDatabaseView(viewTableAddDto);

                        if (string.IsNullOrEmpty(viewDto.ErrorMessage))
                        {
                            foreach (var ownerTableNamePair in viewTableAddDto.NeedToAddOwnerTablePairList)
                            {
                                string schemaOwner = ownerTableNamePair.Key;
                                string linkToTableName = ownerTableNamePair.Value;

                                var aUnit = transactionDto.AppTransactionUnitList.FirstOrDefault(o => o.DataBaseTableName == linkToTableName && o.SchemaOwner == schemaOwner);
                                if (aUnit != null)
                                {
                                    var fkField = aUnit.AppTransactionFieldList.FirstOrDefault(o => o.IsLinkToParentPrimaryKey);
                                    if (fkField != null)
                                    {
                                        DatabaseViewJoinDto joinDto = new DatabaseViewJoinDto();
                                        joinDto.SortOrder = viewDto.Joins.Count + 1;
                                        joinDto.GUID = Guid.NewGuid();
                                        joinDto.JoinMethod = JoinMethod.LeftOuterJoin;
                                        joinDto.JoinConditionList = new List<DatabaseViewJoinConditionDto>();
                                        //ParseJoinSearchExpression(aQualifiedJoin.SearchCondition, joinDto);

                                        var joinCondition = new DatabaseViewJoinConditionDto()
                                        {
                                            LeftSideSchemaOwner = rootUnit.SchemaOwner,
                                            LeftSideTable = rootUnit.DataBaseTableName,
                                            LeftSideColumn = rootUnitPrimaryKeyField.DataBaseFieldName,
                                            RightSideSchemaOwner = aUnit.SchemaOwner,
                                            RightSideTable = aUnit.DataBaseTableName,
                                            RightSideColumn = fkField.DataBaseFieldName,
                                            GUID = Guid.NewGuid(),
                                            JoinOperationType = JoinConditionOperationType.EqualTo
                                        };
                                        joinDto.JoinConditionList.Add(joinCondition);
                                        viewDto.Joins.Add(joinDto);
                                    }
                                }
                            }

                        }

                        ConvertViewDtoToQuery(viewDto);


                        if (!string.IsNullOrEmpty(viewDto.ErrorMessage))
                        {
                            validationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CreateSearchViewFromMasterDetailTransactoin_Error", ValidationItemType.Error, viewDto.ErrorMessage));
                        }
                    }
                }

                if (!validationResult.HasErrors)
                {
                    databaseViewUpdateDto.DataSetDto = dataSetDto;
                    databaseViewUpdateDto.OrgViewDto = viewDto;
                    databaseViewUpdateDto.TransactionId = transactionId;
                    databaseViewUpdateDto.NeedToCreateLinkTargetActions = new List<EmAppLinkTargetActionType>() {
                            EmAppLinkTargetActionType.Create,
                            EmAppLinkTargetActionType.Edit,
                            EmAppLinkTargetActionType.Delete,
                            //EmAppLinkTargetActionType.Preview
                        };
                    databaseViewUpdateDto.OrgViewDto = viewDto;

                    if (rootUnitPrimaryKeyField != null)
                    {
                        databaseViewUpdateDto.TransactionRootPrimaryKey = rootUnitPrimaryKeyField.DataBaseFieldName;
                    }

                    aOperationCallResult.Object = databaseViewUpdateDto;
                }
            }

            return aOperationCallResult;
        }








        #region  ------ 1 ConvertQueryToViewDto Help Method


        private static void ParseQuerySelectFields(IList<SelectElement> selectElements, DatabaseViewDto viewDto)
        {
            string defaultTableName = string.Empty;

            if (viewDto.DictTables.Count == 1)
            {
                defaultTableName = viewDto.DictTables.First().Key;
            }

            foreach (var element in selectElements)
            {
                if (element is SelectScalarExpression)
                {
                    SelectScalarExpression aSelectColumn = element as SelectScalarExpression;

                    if (aSelectColumn.Expression is ColumnReferenceExpression)
                    {
                        ColumnReferenceExpression aColumn = aSelectColumn.Expression as ColumnReferenceExpression;
                        var identifiers = aColumn.MultiPartIdentifier?.Identifiers;

                        if (identifiers != null && identifiers.Count > 0)
                        {
                            DatabaseViewColumnDto viewColumnDto = new DatabaseViewColumnDto();
                            viewColumnDto.ColumnName = identifiers.Last().Value;
                            viewColumnDto.ColumnDisplayName = viewColumnDto.ColumnName;
                            viewColumnDto.SortOrder = viewDto.SelectedColumnsList.Count + 1;
                            viewColumnDto.IsUsedAsViewField = true;
                            viewColumnDto.IsUsedAsSearchField = false;
                            if (identifiers.Count >= 2)
                            {
                                viewColumnDto.UniqTableOrAliasName = identifiers[identifiers.Count - 2].Value;
                            }
                            else
                            {
                                viewColumnDto.UniqTableOrAliasName = defaultTableName;
                            }

                            if (aSelectColumn.ColumnName != null && !string.IsNullOrEmpty(aSelectColumn.ColumnName.Value))
                            {
                                viewColumnDto.ColumnAlias = aSelectColumn.ColumnName.Value;
                                viewColumnDto.ColumnDisplayName = viewColumnDto.ColumnAlias;
                            }

                            viewDto.SelectedColumnsList.Add(viewColumnDto);


                        }

                    }


                }
            }

            // need to add more expression for "ColumnA + COlumnB as [SUM Of AB]"
        }

        private static void ParseQueryFromSource(TableReference fromSource, DatabaseViewDto viewDto, DatabaseViewJoinDto parentJoinDto)
        {
            if (fromSource is QualifiedJoin)
            {
                QualifiedJoin aQualifiedJoin = fromSource as QualifiedJoin;
                if (aQualifiedJoin.FirstTableReference != null && aQualifiedJoin.SecondTableReference != null)
                {
                    DatabaseViewJoinDto joinDto = new DatabaseViewJoinDto();
                    joinDto.GUID = Guid.NewGuid();
                    joinDto.SortOrder = viewDto.Joins.Count + 1;
                    joinDto.JoinMethod = ConvertQualifiedJoinTypeToString(aQualifiedJoin.QualifiedJoinType);
                    joinDto.JoinConditionList = new List<DatabaseViewJoinConditionDto>();
                    ParseJoinSearchExpression(aQualifiedJoin.SearchCondition, joinDto);
                    joinDto.ParentJoin = parentJoinDto;
                    viewDto.Joins.Add(joinDto);

                    ParseQueryFromSource(aQualifiedJoin.FirstTableReference, viewDto, joinDto);

                    ParseQueryFromSource(aQualifiedJoin.SecondTableReference, viewDto, joinDto);
                }
            }
            else if (fromSource is UnqualifiedJoin)
            {
                UnqualifiedJoin aUnqualifiedJoin = fromSource as UnqualifiedJoin;
                if (aUnqualifiedJoin.FirstTableReference != null && aUnqualifiedJoin.SecondTableReference != null)
                {
                    DatabaseViewJoinDto joinDto = new DatabaseViewJoinDto();
                    joinDto.GUID = Guid.NewGuid();
                    joinDto.SortOrder = viewDto.Joins.Count + 1;
                    joinDto.JoinMethod = JoinMethod.CrossJoin;
                    joinDto.JoinConditionList = new List<DatabaseViewJoinConditionDto>();

                    joinDto.ParentJoin = parentJoinDto;
                    viewDto.Joins.Add(joinDto);

                    ParseQueryFromSource(aUnqualifiedJoin.FirstTableReference, viewDto, joinDto);

                    ParseQueryFromSource(aUnqualifiedJoin.SecondTableReference, viewDto, joinDto);
                }

            }
            else if (fromSource is NamedTableReference)
            {
                NamedTableReference aNamedTableReference = fromSource as NamedTableReference;



                int positionX = startX + (viewDto.DictTables.Count % totalDiagramColumns) * 300;
                int positionY = startY + (viewDto.DictTables.Count / totalDiagramColumns) * 250;

                //if (viewDto.DictTables.Count % 2 == 1)
                //{
                //    positionY = positionY + 30;
                //}


                DatabaseViewTableDto tableDto = new DatabaseViewTableDto() { Width = tableWidth, Height = tableHeight, PositionX = positionX, PositionY = positionY };

                tableDto.SortOrder = viewDto.DictTables.Count + 1;
                tableDto.TableName = aNamedTableReference.SchemaObject.BaseIdentifier.Value;
                tableDto.UniqTableOrAliasName = tableDto.TableName;



                if (aNamedTableReference.SchemaObject.SchemaIdentifier != null && !string.IsNullOrWhiteSpace(aNamedTableReference.SchemaObject.SchemaIdentifier.Value))
                {
                    tableDto.SchemaOwner = aNamedTableReference.SchemaObject.SchemaIdentifier.Value;
                }
                else
                {
                    tableDto.SchemaOwner = AppMetaDataBL.GetCurrentDbConnectionDefaultSchmeOner(viewDto.DataSourceRegisterId);
                }

                if (aNamedTableReference.Alias != null && !string.IsNullOrEmpty(aNamedTableReference.Alias.Value))
                {
                    tableDto.TableAlias = aNamedTableReference.Alias.Value;
                    tableDto.UniqTableOrAliasName = tableDto.TableAlias;
                }

                viewDto.DictTables.Add(tableDto.UniqTableOrAliasName, tableDto);
            }
        }

        private static string ConvertQualifiedJoinTypeToString(QualifiedJoinType qualifiedJoinType)
        {
            string joinMethod = JoinMethod.CrossJoin;

            if (qualifiedJoinType == QualifiedJoinType.Inner)
            {
                joinMethod = JoinMethod.InnerJoin;
            }
            else if (qualifiedJoinType == QualifiedJoinType.LeftOuter)
            {
                joinMethod = JoinMethod.LeftOuterJoin;
            }
            else if (qualifiedJoinType == QualifiedJoinType.RightOuter)
            {
                joinMethod = JoinMethod.RightOuterJoin;
            }
            else if (qualifiedJoinType == QualifiedJoinType.FullOuter)
            {
                joinMethod = JoinMethod.FullOuterJoin;
            }

            return joinMethod;
        }

        private static void ParseJoinSearchExpression(object aSearchExpression, DatabaseViewJoinDto joinDto)
        {
            if (aSearchExpression is BooleanBinaryExpression aBooleanBinaryExpression)
            {
                ParseJoinSearchExpression(aBooleanBinaryExpression.FirstExpression, joinDto);
                ParseJoinSearchExpression(aBooleanBinaryExpression.SecondExpression, joinDto);
            }
            else if (aSearchExpression is BooleanComparisonExpression aComparisonExpression)
            {
                if (aComparisonExpression.FirstExpression is ColumnReferenceExpression column1
                    && aComparisonExpression.SecondExpression is ColumnReferenceExpression column2)
                {
                    var column1Identifiers = column1.MultiPartIdentifier?.Identifiers;
                    var column2Identifiers = column2.MultiPartIdentifier?.Identifiers;

                    if (column1Identifiers != null && column1Identifiers.Count >= 2
                        && column2Identifiers != null && column2Identifiers.Count >= 2)
                    {
                        DatabaseViewJoinConditionDto joinConditionDto = new DatabaseViewJoinConditionDto()
                        {
                            GUID = Guid.NewGuid(),
                            LeftSideTable = column1Identifiers[column1Identifiers.Count - 2].Value,
                            LeftSideColumn = column1Identifiers[column1Identifiers.Count - 1].Value,
                            RightSideTable = column2Identifiers[column2Identifiers.Count - 2].Value,
                            RightSideColumn = column2Identifiers[column2Identifiers.Count - 1].Value,
                            JoinOperationType = ConvertBooleanComparisonTypeToJoinOperation(aComparisonExpression.ComparisonType)
                        };

                        if (column1Identifiers.Count >= 3)
                        {
                            joinConditionDto.LeftSideSchemaOwner = column1Identifiers[column1Identifiers.Count - 3].Value;
                        }

                        if (column2Identifiers.Count >= 3)
                        {
                            joinConditionDto.RightSideSchemaOwner = column2Identifiers[column2Identifiers.Count - 3].Value;
                        }

                        joinDto.JoinConditionList.Add(joinConditionDto);
                    }
                }
            }

        }

        private static string ConvertBooleanComparisonTypeToJoinOperation(BooleanComparisonType comparisonType)
        {
            if (comparisonType == BooleanComparisonType.Equals)
            {
                return JoinConditionOperationType.EqualTo;
            }
            else if (comparisonType == BooleanComparisonType.NotEqualToBrackets
                || comparisonType == BooleanComparisonType.NotEqualToExclamation)
            {
                return JoinConditionOperationType.NotEqualTo;
            }
            else if (comparisonType == BooleanComparisonType.GreaterThan)
            {
                return JoinConditionOperationType.GreaterThan;
            }
            else if (comparisonType == BooleanComparisonType.GreaterThanOrEqualTo)
            {
                return JoinConditionOperationType.GreaterOrEqualTo;
            }

            else if (comparisonType == BooleanComparisonType.LessThan)
            {
                return JoinConditionOperationType.LessThan;
            }
            else if (comparisonType == BooleanComparisonType.LessThanOrEqualTo)
            {
                return JoinConditionOperationType.LessOrEqualTo;
            }
            else
            {
                return JoinConditionOperationType.EqualTo;
            }
        }


        #endregion




        #region  ------ 2 ConvertViewDtoToQuery  Help Method



        private static string ConvertViewDtoToQuery_BuildSelectString(DatabaseViewDto viewDto)
        {
            string queryString = "SELECT ";
            int columnIndex = 1;
            foreach (var column in viewDto.SelectedColumnsList.OrderBy(o => o.SortOrder))
            {

                if (viewDto.DictTables != null && viewDto.DictTables.ContainsKey(column.UniqTableOrAliasName))
                {
                    DatabaseViewTableDto columnTable = viewDto.DictTables[column.UniqTableOrAliasName];

                    if (columnTable != null)
                    {
                        if (columnIndex % 4 == 1)
                        {
                            queryString += System.Environment.NewLine + "        ";
                        }

                        string columnFullPathName = AddSquareBrackets(column.ColumnName);

                        if (!string.IsNullOrWhiteSpace(columnTable.TableAlias))
                        {
                            columnFullPathName = AddSquareBrackets(columnTable.TableAlias) + "." + columnFullPathName;
                        }
                        else
                        {
                            columnFullPathName = columnTable.TableFullNamePath + "." + columnFullPathName;
                        }

                        queryString += columnFullPathName;

                        if (!string.IsNullOrWhiteSpace(column.ColumnAlias))
                        {
                            queryString += " AS " + AddSquareBrackets(column.ColumnAlias);
                        }
                        queryString += ", ";

                        columnIndex++;
                    }
                }
            }

            if (queryString.EndsWith(", "))
            {
                queryString = queryString.Substring(0, queryString.Length - 2); // remove last ,
            }

            queryString += " " + System.Environment.NewLine;
            return queryString;
        }


        private static string ConvertViewDtoToQuery_BuildFromString(DatabaseViewDto viewDto)
        {
            string fromString = "FROM " + System.Environment.NewLine;



            // if there are cross joins, then it will be more than one root nodes
            //  A CROSS JOIN B, Then rootJoinNodeList = [A, B]
            //  A INNER JOIN B, Then rootJoinNodeList = [aJoin]; aJoin.FirstObject = A, aJoin.SecondObject = B
            List<object> rootJoinNodeList = new List<object>();

            foreach (var tableObj in viewDto.DictTables.Values)
            {
                tableObj.GUID = Guid.NewGuid();
                tableObj.ParentJoin = null;
                tableObj.RootJoin = null;

                rootJoinNodeList.Add(tableObj);
            }

            foreach (var join in viewDto.Joins.OrderByDescending(o => o.SortOrder))
            {
                foreach (var condition in join.JoinConditionList)
                {
                    if (viewDto.DictTables.ContainsKey(condition.LeftSideTable) && viewDto.DictTables.ContainsKey(condition.RightSideTable))
                    {
                        var firstTable = viewDto.DictTables[condition.LeftSideTable];
                        var secondTable = viewDto.DictTables[condition.RightSideTable];

                        if (firstTable.RootJoin == null && secondTable.RootJoin == null)
                        {
                            DatabaseViewJoinDto newJoin = new DatabaseViewJoinDto() { GUID = Guid.NewGuid() };
                            newJoin.JoinMethod = join.JoinMethod;
                            newJoin.FirstChild = firstTable;
                            newJoin.SecondChild = secondTable;
                            newJoin.JoinConditionList = new List<DatabaseViewJoinConditionDto>();
                            newJoin.JoinConditionList.Add(condition);

                            RemoveNodeFromRootJoinNodeList(rootJoinNodeList, firstTable.GUID);
                            RemoveNodeFromRootJoinNodeList(rootJoinNodeList, secondTable.GUID);

                            AssignParentJoinToChildNodes(newJoin, null, null);
                            rootJoinNodeList.Add(newJoin);
                        }
                        else if (firstTable.RootJoin != null && secondTable.RootJoin == null)
                        {
                            DatabaseViewJoinDto newJoin = new DatabaseViewJoinDto() { GUID = Guid.NewGuid() };
                            newJoin.JoinMethod = join.JoinMethod;
                            newJoin.FirstChild = firstTable.RootJoin;
                            newJoin.SecondChild = secondTable;
                            newJoin.JoinConditionList = new List<DatabaseViewJoinConditionDto>();
                            newJoin.JoinConditionList.Add(condition);

                            RemoveNodeFromRootJoinNodeList(rootJoinNodeList, firstTable.RootJoin.GUID);
                            RemoveNodeFromRootJoinNodeList(rootJoinNodeList, secondTable.GUID);

                            AssignParentJoinToChildNodes(newJoin, null, null);
                            rootJoinNodeList.Add(newJoin);

                        }
                        else if (firstTable.RootJoin == null && secondTable.RootJoin != null)
                        {
                            DatabaseViewJoinDto newJoin = new DatabaseViewJoinDto() { GUID = Guid.NewGuid() };
                            newJoin.JoinMethod = join.JoinMethod;
                            newJoin.FirstChild = firstTable;
                            newJoin.SecondChild = secondTable.RootJoin;
                            newJoin.JoinConditionList = new List<DatabaseViewJoinConditionDto>();
                            newJoin.JoinConditionList.Add(condition);

                            RemoveNodeFromRootJoinNodeList(rootJoinNodeList, firstTable.GUID);
                            RemoveNodeFromRootJoinNodeList(rootJoinNodeList, secondTable.RootJoin.GUID);

                            AssignParentJoinToChildNodes(newJoin, null, null);
                            rootJoinNodeList.Add(newJoin);
                        }
                        else if (firstTable.RootJoin != null && secondTable.RootJoin != null)
                        {
                            if (firstTable.RootJoin.GUID == secondTable.RootJoin.GUID)
                            {
                                // join already exist, only need to insert conditions
                                if (firstTable.ParentJoin != null && secondTable.ParentJoin != null)
                                {
                                    DatabaseViewJoinDto commonJoin = FindCommonJoin(firstTable.ParentJoin, secondTable);
                                    if (commonJoin != null)
                                    {
                                        commonJoin.JoinConditionList.Add(condition);
                                    }
                                }

                            }
                            else
                            {
                                // Join the 2 sub-joins

                                DatabaseViewJoinDto newJoin = new DatabaseViewJoinDto() { GUID = Guid.NewGuid() };
                                newJoin.JoinMethod = join.JoinMethod;
                                newJoin.FirstChild = firstTable.RootJoin;
                                newJoin.SecondChild = secondTable.RootJoin;
                                newJoin.JoinConditionList = new List<DatabaseViewJoinConditionDto>();
                                newJoin.JoinConditionList.Add(condition);

                                RemoveNodeFromRootJoinNodeList(rootJoinNodeList, firstTable.RootJoin.GUID);
                                RemoveNodeFromRootJoinNodeList(rootJoinNodeList, secondTable.RootJoin.GUID);

                                AssignParentJoinToChildNodes(newJoin, null, null);
                                rootJoinNodeList.Add(newJoin);
                            }
                        }
                    }

                }
            }

            int nodeIndex = 1;
            foreach (var aRootNode in rootJoinNodeList)
            {
                if (nodeIndex > 1)
                {
                    fromString += " CROSS JOIN ";
                    fromString += System.Environment.NewLine;
                }

                fromString += BuildJoinNodeQueryString(aRootNode);

                nodeIndex++;
            }

            return fromString;
        }

        private static void AssignParentJoinToChildNodes(object currentNode, DatabaseViewJoinDto parentJoin, DatabaseViewJoinDto rootJoin)
        {
            if (currentNode is DatabaseViewTableDto)
            {
                var currentTableObj = currentNode as DatabaseViewTableDto;
                currentTableObj.RootJoin = rootJoin;
                currentTableObj.ParentJoin = parentJoin;
            }
            else if (currentNode is DatabaseViewJoinDto)
            {
                var currentJoinObj = currentNode as DatabaseViewJoinDto;
                currentJoinObj.RootJoin = rootJoin;
                currentJoinObj.ParentJoin = parentJoin;

                if (rootJoin == null)
                {
                    rootJoin = currentJoinObj;
                }

                AssignParentJoinToChildNodes(currentJoinObj.FirstChild, currentJoinObj, rootJoin);
                AssignParentJoinToChildNodes(currentJoinObj.SecondChild, currentJoinObj, rootJoin);
            }
        }



        private static DatabaseViewJoinDto FindCommonJoin(DatabaseViewJoinDto joinNode, DatabaseViewTableDto secondTable)
        {
            if (joinNode != null)
            {
                Dictionary<Guid, DatabaseViewTableDto> dictTables = new Dictionary<Guid, DatabaseViewTableDto>();
                FindJoinNodeAllChildTables(joinNode, dictTables);
                if (dictTables.ContainsKey(secondTable.GUID))
                {
                    return joinNode;
                }
                else
                {
                    if (joinNode.ParentJoin != null)
                    {
                        return FindCommonJoin(joinNode.ParentJoin, secondTable);
                    }
                }
            }

            return null;
        }

        private static void FindJoinNodeAllChildTables(object joinNode, Dictionary<Guid, DatabaseViewTableDto> dictTables)
        {
            if (joinNode != null && joinNode is DatabaseViewJoinDto && dictTables != null)
            {
                DatabaseViewJoinDto joinDto = joinNode as DatabaseViewJoinDto;
                if (joinDto.FirstChild is DatabaseViewTableDto)
                {
                    var table = joinDto.FirstChild as DatabaseViewTableDto;
                    if (!dictTables.ContainsKey(table.GUID))
                    {
                        dictTables.Add(table.GUID, table);
                    }
                }
                else if (joinDto.FirstChild is DatabaseViewJoinDto)
                {
                    var join = joinDto.FirstChild as DatabaseViewJoinDto;
                    FindJoinNodeAllChildTables(join, dictTables);
                }

                if (joinDto.SecondChild is DatabaseViewTableDto)
                {
                    var table = joinDto.SecondChild as DatabaseViewTableDto;
                    if (!dictTables.ContainsKey(table.GUID))
                    {
                        dictTables.Add(table.GUID, table);
                    }
                }
                else if (joinDto.SecondChild is DatabaseViewJoinDto)
                {
                    var join = joinDto.SecondChild as DatabaseViewJoinDto;
                    FindJoinNodeAllChildTables(join, dictTables);
                }
            }
        }

        private static void RemoveNodeFromRootJoinNodeList(List<object> rootJoinNodeList, Guid guid)
        {
            if (guid != null)
            {
                var needToRemoveNode = rootJoinNodeList.FirstOrDefault(o => o is JoinTreeNode && ((JoinTreeNode)o).GUID == guid);
                rootJoinNodeList.Remove(needToRemoveNode);
            }
        }



        private static string BuildJoinNodeQueryString(object joinNode)
        {
            string nodeQueryString = string.Empty;

            if (joinNode is DatabaseViewTableDto)
            {
                var table = joinNode as DatabaseViewTableDto;
                BuildTableQueryFullNamePath(table);

                nodeQueryString += "        " + table.TableFullNamePath + " ";

                if (!string.IsNullOrWhiteSpace(table.TableAlias))
                {
                    string tableAlias = AddSquareBrackets(table.TableAlias);
                    nodeQueryString += "AS " + tableAlias + " ";
                }
            }
            else if (joinNode is DatabaseViewJoinDto)
            {
                var join = joinNode as DatabaseViewJoinDto;

                nodeQueryString += BuildJoinNodeQueryString(join.FirstChild);

                nodeQueryString += " " + join.JoinMethod + " " + System.Environment.NewLine;

                nodeQueryString += BuildJoinNodeQueryString(join.SecondChild);

                nodeQueryString += " ON ";

                int conditionIndex = 1;
                foreach (DatabaseViewJoinConditionDto condition in join.JoinConditionList)
                {
                    if (conditionIndex > 1)
                    {
                        nodeQueryString += "AND ";
                    }

                    string leftSideExpression = AddSquareBrackets(condition.LeftSideTable) + "." + AddSquareBrackets(condition.LeftSideColumn);
                    string rightSideExpression = AddSquareBrackets(condition.RightSideTable) + "." + AddSquareBrackets(condition.RightSideColumn);

                    if (!string.IsNullOrWhiteSpace(condition.LeftSideSchemaOwner))
                    {
                        leftSideExpression = AddSquareBrackets(condition.LeftSideSchemaOwner) + "." + leftSideExpression;
                    }

                    if (!string.IsNullOrWhiteSpace(condition.RightSideSchemaOwner))
                    {
                        rightSideExpression = AddSquareBrackets(condition.RightSideSchemaOwner) + "." + rightSideExpression;
                    }


                    nodeQueryString += leftSideExpression + condition.JoinOperationType + rightSideExpression + " ";

                    conditionIndex++;
                }

            }

            return nodeQueryString;
        }




        #endregion



        private static string GetUniqTableOrAliasNameFromSchemaObject(NamedTableReference aNamedTableReference)
        {
            string tableName = aNamedTableReference.SchemaObject.BaseIdentifier.Value;
            string uniqTableOrAliasName = tableName;

            if (aNamedTableReference.Alias != null && !string.IsNullOrEmpty(aNamedTableReference.Alias.Value))
            {
                string tableAlias = aNamedTableReference.Alias.Value;
                uniqTableOrAliasName = tableAlias;
            }

            return uniqTableOrAliasName;
        }



        private static void BuildSimpleDBViewDtoFromTransaction(ValidationResult validationResult, AppTransactionExDto transactionDto,
            out DatabaseViewDto viewDto, out AppDataSetExDto dataSetDto)
        {
            viewDto = new DatabaseViewDto();

            dataSetDto = new AppDataSetExDto();
            dataSetDto.Name = transactionDto.TransactionName;
            //dataSetDto.Description = "Transaction: " + transactionDto.Id.ToString() + " " + transactionDto.TransactionName + " at " + DateTime.UtcNow.ToString();
            dataSetDto.QueryType = (int)EmAppDataServiceType.QueryText;
            dataSetDto.QueryText = string.Empty;
            dataSetDto.DataSourceFrom = transactionDto.DataSourceFrom;
            dataSetDto.SaasApplicationId = transactionDto.SaasApplicationId;

            var customerDatabaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(transactionDto.DataSourceFrom.Value);

            var rootUnit = transactionDto.RootMasterUnit;

            if (rootUnit != null)
            {
                try
                {
                    if (transactionDto.IsReadOnly.HasValue && transactionDto.IsReadOnly.Value)
                    {
                        dataSetDto.QueryText = rootUnit.DataSourceQuery;

                        viewDto = ConvertQueryToViewDto(dataSetDto.QueryText, dataSetDto.DataSourceFrom, viewDto);

                        if (!string.IsNullOrEmpty(viewDto.ErrorMessage))
                        {
                            validationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CreateSearchViewFromMasterDetailTransactoin_Error", ValidationItemType.Error, viewDto.ErrorMessage));
                        }
                    }
                    else
                    {
                        string tableName = rootUnit.DataBaseTableName.ToLower().Trim();

                        //DatabaseViewUpdateDto databaseViewUpdateDto = new DatabaseViewUpdateDto();
                        //databaseViewUpdateDto.DataSourceRegisterId = transactionDto.DataSourceFrom;
                        //databaseViewUpdateDto.DataSetDto = dataSetDto;
                        //databaseViewUpdateDto.OrgViewDto = viewDto;
                        //databaseViewUpdateDto.NeedToAddOwnerTablePairList = new List<KeyValuePair<string, string>>();
                        //databaseViewUpdateDto.NeedToAddOwnerTablePairList.Add(new KeyValuePair<string, string>(rootUnit.SchemaOwner, rootUnit.DataBaseTableName));

                        ViewTableAddRemoveDto viewTableAddDto = new ViewTableAddRemoveDto();
                        viewTableAddDto.QueryString = string.Empty;
                        viewTableAddDto.DictTables = new Dictionary<string, DatabaseViewTableDto>(StringComparer.OrdinalIgnoreCase);
                        viewTableAddDto.Joins = new List<DatabaseViewJoinDto>();
                        viewTableAddDto.DictAllColumns = new Dictionary<string, Dictionary<string, bool>>();
                        viewTableAddDto.SelectedColumnsList = new List<DatabaseViewColumnDto>();
                        viewTableAddDto.WhereConditionFilterColumns = new List<DatabaseViewColumnDto>();
                        viewTableAddDto.ErrorMessage = string.Empty;
                        viewTableAddDto.DataSourceRegisterId = transactionDto.DataSourceFrom;
                        viewTableAddDto.NeedToAddOwnerTablePairList = new List<KeyValuePair<string, string>>();
                        viewTableAddDto.NeedToAddOwnerTablePairList.Add(new KeyValuePair<string, string>(rootUnit.SchemaOwner, rootUnit.DataBaseTableName));

                        viewDto = AppDatabaseViewBL.AddTablesToDatabaseView(viewTableAddDto);
                        viewDto.QueryString = string.Format(" SELECT * FROM {0}", AppMetaDataBL.GetQulifiedTableName(rootUnit.SchemaOwner, rootUnit.DataBaseTableName, customerDatabaseFixture.SqlServerType.Value));

                        if (viewDto != null && string.IsNullOrEmpty(viewDto.ErrorMessage)
                            && viewDto.DictAllColumns.Count > 0 && viewDto.DictAllColumns.ContainsKey(tableName) && viewDto.DictAllColumns[tableName].Count > 0 && rootUnit.AppTransactionFieldList.Count > 0)
                        {

                            foreach (var transactionField in rootUnit.AppTransactionFieldList.OrderBy(o => o.SortOrder))
                            {
                                string columnName = transactionField.DataBaseFieldName;

                                DatabaseViewColumnDto columnDto = new DatabaseViewColumnDto();

                                columnDto.UniqTableOrAliasName = tableName;
                                columnDto.ColumnName = columnName;
                                columnDto.ColumnDisplayName = columnName;
                                columnDto.SortOrder = viewDto.SelectedColumnsList.Count + 1;


                                columnDto.IsUsedAsSearchField = false;
                                columnDto.IsUsedAsViewField = true;

                                //if (transactionField.DisplayName != columnDto.ColumnName)
                                //{
                                //    columnDto.ColumnAlias = "[" + transactionField.DisplayName + "]";
                                //    columnDto.ColumnDisplayName = columnDto.ColumnAlias;

                                //}

                                columnDto.ControlType = transactionField.ControlType;

                                if (transactionField.EntityId.HasValue)
                                {
                                    columnDto.EntityId = transactionField.EntityId;
                                }

                                viewDto.SelectedColumnsList.Add(columnDto);

                                if (viewDto.DictAllColumns.ContainsKey(tableName) && viewDto.DictAllColumns[tableName].ContainsKey(columnName))
                                {
                                    viewDto.DictAllColumns[tableName][columnName] = true;
                                }
                            }



                            // ConvertViewDtoToQuery(viewDto);
                        }

                        if (!string.IsNullOrEmpty(viewDto.ErrorMessage))
                        {
                            validationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CreateSearchViewFromMasterDetailTransactoin_Error", ValidationItemType.Error, viewDto.ErrorMessage));
                        }

                    }

                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppDatabaseView_CreateSearchViewFromMasterDetailTransactoin_Error", ValidationItemType.Error, ex.ToString()));
                }
            }
        }



        public static string AddSquareBrackets(string aString)
        {
            if (!string.IsNullOrWhiteSpace(aString))
            {
                return "[" + aString + "]";
            }

            return aString;
        }

        public static string RemoveAllSquareBrackets(string aString)
        {
            return aString.Replace("[", string.Empty).Replace("]", string.Empty);
        }

        public static void BuildTableQueryFullNamePath(DatabaseViewTableDto table)
        {
            string schemaOwner = AddSquareBrackets(table.SchemaOwner);

            string tableFullNamePath = AddSquareBrackets(table.TableName);

            if (!string.IsNullOrWhiteSpace(schemaOwner))
            {
                tableFullNamePath = schemaOwner + "." + tableFullNamePath;
            }

            table.TableFullNamePath = tableFullNamePath;
        }

        //TODO!?? need to add joininfo dto
        //public static List<string> GetOneTableAllReferenceTables(string oneTable, int dataSourceRegistraionId)
        //{
        //    List<string> tables = new List<string>();

        //    DatabaseFixture databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegistraionId);

        //    var fkDatatable =databaseFixture.GetOneTableAllForeighKeyConstrain(oneTable);

        //    tables = fkDatatable.AsEnumerable ()
        //        .Select (o=> $@"{o["CURRENT_COLUMN"]}->{o["REFERENCED_TABLE"]}.{o["REFERENCED_COLUMN"]}").ToList ();

        //    return tables;
        //}
        private static List<int> CalculateExistingTableOccupiedPositoinIndexList(DatabaseViewDto viewDto)
        {
            List<int> occupiedPositionIndexList = new List<int>();

            foreach (var existTable in viewDto.DictTables.Values)
            {
                bool isOccupiedPoistionIndex = existTable.PositionX > 0 && existTable.PositionY > 0 && existTable.PositionX < (totalDiagramColumns * 300 + startX - 50);

                if (isOccupiedPoistionIndex)
                {
                    int startIndexY = (existTable.PositionY - startY + 25) / 250;
                    int endIndexY = (existTable.PositionY + existTable.Height - startY + 25) / 250;

                    int startIndexX = (existTable.PositionX - startX + 50) / 300;
                    int endIndexX = (existTable.PositionX + existTable.Width - startX + 50) / 300;

                    if (endIndexX > (totalDiagramColumns - 1))
                    {
                        endIndexX = totalDiagramColumns - 1;
                    }

                    for (int y = startIndexY; y <= endIndexY; y++)
                    {
                        for (int x = startIndexX; x <= endIndexX; x++)
                        {
                            int positionIndex = y * totalDiagramColumns + x;
                            occupiedPositionIndexList.Add(positionIndex);
                        }
                    }
                }
            }

            occupiedPositionIndexList = occupiedPositionIndexList.Distinct().ToList();

            return occupiedPositionIndexList;
        }

        private static int GetNextFreePositionIndex(DatabaseViewDto viewDto)
        {
            List<int> occupiedPositionIndexList = CalculateExistingTableOccupiedPositoinIndexList(viewDto);

            int positionIndex = 0;

            while (occupiedPositionIndexList.Contains(positionIndex))
            {
                positionIndex++;
            }

            return positionIndex;
        }

        private static void RecalculateTablesPositions(DatabaseViewDto viewDto, DatabaseViewDto orgViewDto)
        {
            if (viewDto != null && orgViewDto != null && viewDto.DictTables != null && orgViewDto.DictTables != null)
            {
                foreach (string key in viewDto.DictTables.Keys)
                {
                    var tableDto = viewDto.DictTables[key];

                    if (tableDto != null)
                    {
                        if (orgViewDto.DictTables.ContainsKey(key))
                        {
                            var orgTableDto = orgViewDto.DictTables[key];

                            tableDto.PositionX = orgTableDto.PositionX;
                            tableDto.PositionY = orgTableDto.PositionY;
                            tableDto.Width = orgTableDto.Width;
                            tableDto.Height = orgTableDto.Height;
                        }
                        else
                        {
                            tableDto.PositionX = -1;
                            tableDto.PositionY = -1;
                        }
                    }
                }

                foreach (string key in viewDto.DictTables.Keys)
                {
                    if (!orgViewDto.DictTables.ContainsKey(key))
                    {
                        var tableDto = viewDto.DictTables[key];
                        int positionIndex = GetNextFreePositionIndex(viewDto);

                        tableDto.PositionX = startX + (positionIndex % totalDiagramColumns) * 300;
                        tableDto.PositionY = startY + (positionIndex / totalDiagramColumns) * 250;
                    }
                }
            }
        }

        internal static void RebuildErDiagramFkLinks(DatabaseViewDto viewDto)
        {
            if (viewDto != null && viewDto.DictTables != null && viewDto.DataSourceRegisterId.HasValue)
            {
                viewDto.Joins = new List<DatabaseViewJoinDto>();

                // why pass eom empty table name to  viewDto.DictTables?
                List<string> tableNameList = viewDto.DictTables.Values.Select(o => o.TableName).ToList();


                //  string connInfo = AppMetaDataBL.GetConnectInfo(viewDto.DataSourceRegisterId.Value);

                // DatabaseFixture databaseFixture = AppCacheManagerBL.GetDatabaseFixtureFromCache(connInfo);

                var dbFixture = AppCacheManagerBL.GetOneDatabaseFixture(viewDto.DataSourceRegisterId.Value);


                try
                {


                    var dataTAble = dbFixture.GetTablesForeighKeyConstrain(tableNameList);

                    if (dataTAble.Rows.Count > 0)
                    {
                        foreach (DataRow row in dataTAble.Rows)
                        {
                            Dictionary<string, object> dictRow = new Dictionary<string, object>();
                            foreach (DataColumn column in dataTAble.Columns)
                            {
                                object cellVAlue = row[column.ColumnName];
                                if (cellVAlue == DBNull.Value)
                                {
                                    dictRow[column.ColumnName] = "";
                                }
                                else
                                {
                                    dictRow[column.ColumnName] = cellVAlue;
                                }
                            }


                            DatabaseViewJoinDto joinDto = new DatabaseViewJoinDto();
                            joinDto.SortOrder = viewDto.Joins.Count + 1;
                            joinDto.GUID = Guid.NewGuid();
                            joinDto.JoinMethod = JoinMethod.LeftOuterJoin;
                            joinDto.JoinConditionList = new List<DatabaseViewJoinConditionDto>();

                            string joinDisplay = $@"{row["CURRENT_TABLE"]}.{row["CURRENT_COLUMN"]}-->{row["REFERENCED_TABLE"]}.{row["REFERENCED_COLUMN"]}{System.Environment.NewLine}{row["Contraint_Name"]}";
                            joinDto.JoinConditionDisplay = joinDisplay;
                            joinDto.ContraintName = row["Contraint_Name"] as string;

                            var joinCondition = new DatabaseViewJoinConditionDto()
                            {
                                //LeftSideSchemaOwner = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["SchemaName"]),
                                //LeftSideTable = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["REFERENCED_TABLE"]),
                                //LeftSideColumn = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["REFERENCED_COLUMN"]),

                                RightSideSchemaOwner = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["SchemaName"]),
                                RightSideTable = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["REFERENCED_TABLE"]),
                                RightSideColumn = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["REFERENCED_COLUMN"]),


                                LeftSideSchemaOwner = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["SchemaName"]),
                                LeftSideTable = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["CURRENT_TABLE"]),
                                LeftSideColumn = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["CURRENT_COLUMN"]),
                                GUID = Guid.NewGuid(),
                                JoinOperationType = JoinConditionOperationType.EqualTo
                            };

                            joinDto.JoinConditionList.Add(joinCondition);
                            viewDto.Joins.Add(joinDto);

                        }
                    }
                }
                catch (Exception ex)
                {
                    viewDto.ErrorMessage = "System Error: " + ex.Message;
                }

            }
        }


        // need t odrop
        internal static void RebuildErDiagramFkLinksOld(DatabaseViewDto viewDto)
        {
            if (viewDto != null && viewDto.DictTables != null && viewDto.DataSourceRegisterId.HasValue)
            {
                viewDto.Joins = new List<DatabaseViewJoinDto>();

                string pramTables = "";

                foreach (DatabaseViewTableDto tableDto in viewDto.DictTables.Values.Where(o => o != null).Distinct())
                {
                    if (pramTables.Length > 0)
                    {
                        pramTables += ", '" + tableDto.TableName + "'";
                    }
                    else
                    {
                        pramTables += "'" + tableDto.TableName + "'";
                    }
                }

                if (pramTables.Length > 0)
                {




                    DatabaseFixture databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(viewDto.DataSourceRegisterId.Value);



                    string queryToExecute = "";


                    //tab2: is curret table (Pirmarytable, CURRENT_COLUMN )
                    if (databaseFixture.SqlServerType == EmSqlType.SqlServer)
                    {
                        queryToExecute = $@"SELECTÂ Â obj.name AS Contraint_Name,Â Â Â Â sch.name AS [SchemaName],Â Â Â Â tab1.name AS [CURRENT_TABLE],Â Â Â Â col1.name AS [CURRENT_COLUMN],Â Â Â 
tab2.name AS [REFERENCED_TABLE],Â Â Â Â col2.name AS [REFERENCED_COLUMN]
                            FROM sys.foreign_key_columns fkc
                            INNER JOIN sys.objects objÂ Â Â Â ON obj.object_id = fkc.constraint_object_id
                            INNER JOIN sys.tables tab1Â Â Â Â ON tab1.object_id = fkc.parent_object_id
                            INNER JOIN sys.schemas schÂ Â Â Â ON tab1.schema_id = sch.schema_id
                            INNER JOIN sys.columns col1Â Â Â Â ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id
                            INNER JOIN sys.tables tab2Â Â Â Â ON tab2.object_id = fkc.referenced_object_id
                            INNER JOIN sys.columns col2Â Â Â Â ON col2.column_id = referenced_column_id AND col2.object_id = tab2.object_id

 Â Â              Â             whereÂ Â Â tab2.name in (" + pramTables + ") " +
                         " andÂ Â Â tab1.name in (" + pramTables + ")";

                    }
                    else if (databaseFixture.SqlServerType == EmSqlType.MySql)
                    {
                        queryToExecute = $@"SELECT 
                                         CONSTRAINT_NAME as Contraint_Name,
                                          '{databaseFixture.CurrentOwner}' as SchemaName,

                                          REFERENCED_TABLE_NAME as REFERENCED_TABLE ,
                                          REFERENCED_COLUMN_NAME as  REFERENCED_COLUMN,
                                            TABLE_NAME as CURRENT_TABLE,
                                          COLUMN_NAME as CURRENT_COLUMN
                                        FROM
                                          INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                                        WHERE
                                          REFERENCED_TABLE_SCHEMA = '{databaseFixture.CurrentOwner}' AND
                                           TABLE_NAME in ({pramTables})
                                           and 
                                            REFERENCED_TABLE_NAME in ({pramTables})";


                    }

                    else if (databaseFixture.SqlServerType == EmSqlType.Oracle)
                    {
                        queryToExecute = $@"SELECT a.table_name as CURRENT_TABLE, a.column_name as CURRENT_COLUMN, a.constraint_name as Contraint_Name, c.owner, 
                                           -- referenced pk
                                           c.r_owner, c_pk.table_name r_table_name as REFERENCED_TABLE , c_pk.constraint_name r_pk as REFERENCED_COLUMN

                                      FROM all_cons_columns a
                                      JOIN all_constraints c ON a.owner = c.owner
                                                            AND a.constraint_name = c.constraint_name
                                      JOIN all_constraints c_pk ON c.r_owner = c_pk.owner
                                                               AND c.r_constraint_name = c_pk.constraint_name
                                     WHERE c.constraint_type = 'R'
                                       AND a.table_name  in  ({pramTables})
                                       AND c_pk.table_name r_table_name  in  ({pramTables})

                                         ";
                    }


                    try
                    {
                        string text = queryToExecute;

                        text = Regex.Replace(text, "\\bgo\\b", System.Environment.NewLine, RegexOptions.IgnoreCase);

                        var dataTAble = databaseFixture.RetriveDataTable(text, new List<DbParameter>());

                        if (dataTAble.Rows.Count > 0)
                        {
                            foreach (DataRow row in dataTAble.Rows)
                            {
                                Dictionary<string, object> dictRow = new Dictionary<string, object>();
                                foreach (DataColumn column in dataTAble.Columns)
                                {
                                    object cellVAlue = row[column.ColumnName];
                                    if (cellVAlue == DBNull.Value)
                                    {
                                        dictRow[column.ColumnName] = "";
                                    }
                                    else
                                    {
                                        dictRow[column.ColumnName] = cellVAlue;
                                    }
                                }


                                DatabaseViewJoinDto joinDto = new DatabaseViewJoinDto();
                                joinDto.SortOrder = viewDto.Joins.Count + 1;
                                joinDto.GUID = Guid.NewGuid();
                                joinDto.JoinMethod = JoinMethod.LeftOuterJoin;
                                joinDto.JoinConditionList = new List<DatabaseViewJoinConditionDto>();

                                string joinDisplay = $@"{row["CURRENT_TABLE"]}.{row["CURRENT_COLUMN"]}-->{row["REFERENCED_TABLE"]}.{row["REFERENCED_COLUMN"]}{System.Environment.NewLine}{row["Contraint_Name"]}";
                                joinDto.JoinConditionDisplay = joinDisplay;
                                joinDto.ContraintName = row["Contraint_Name"] as string;

                                var joinCondition = new DatabaseViewJoinConditionDto()
                                {
                                    //LeftSideSchemaOwner = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["SchemaName"]),
                                    //LeftSideTable = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["REFERENCED_TABLE"]),
                                    //LeftSideColumn = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["REFERENCED_COLUMN"]),

                                    RightSideSchemaOwner = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["SchemaName"]),
                                    RightSideTable = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["REFERENCED_TABLE"]),
                                    RightSideColumn = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["REFERENCED_COLUMN"]),


                                    LeftSideSchemaOwner = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["SchemaName"]),
                                    LeftSideTable = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["CURRENT_TABLE"]),
                                    LeftSideColumn = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row["CURRENT_COLUMN"]),
                                    GUID = Guid.NewGuid(),
                                    JoinOperationType = JoinConditionOperationType.EqualTo
                                };

                                joinDto.JoinConditionList.Add(joinCondition);
                                viewDto.Joins.Add(joinDto);

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        viewDto.ErrorMessage = "System Error: " + ex.Message;
                    }
                }
            }
        }



        /// <summary>Creates Flex form + default layout when missing (PLM import / quick generate).</summary>
        public static OperationCallResult<object> EnsureTransactionDefaultFlexFormLayout(int transactionId, int? numberOfLayoutColumns = null)
        {
            return EnsureTransactionDefaultFlexFormLayout(transactionId, migrationFastPath: false, numberOfLayoutColumns);
        }

        /// <summary>
        /// <paramref name="migrationFastPath"/> loads the transaction once and skips redundant form/layout reads (bulk PLM import).
        /// When <paramref name="numberOfLayoutColumns"/> is provided, the generated form layout always uses that exact column count.
        /// </summary>
        public static OperationCallResult<object> EnsureTransactionDefaultFlexFormLayout(int transactionId, bool migrationFastPath, int? numberOfLayoutColumns = null)
        {
            if (migrationFastPath)
                return EnsureTransactionDefaultFlexFormLayoutMigration(transactionId, numberOfLayoutColumns);

            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            if (transactionExDto == null)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionExDto), "App_TransactionEntity_NotFound_Error",
                    ValidationItemType.Error, "Transaction not found: " + transactionId));
                return aOperationCallResult;
            }

            bool needToReBuildDefaultFormLayout = false;

            if (transactionExDto.FormId.HasValue)
            {
                AppFormEntity formEntity = AppFormBL.RetrieveSimpleAppFormEntity(transactionExDto.FormId);

                if (!formEntity.LayoutType.HasValue || formEntity.LayoutType.Value != (int)EmAppFormLayoutType.Flex)
                {
                    needToReBuildDefaultFormLayout = true;
                }
                else
                {
                    var flexFormExDto = AppFormFlexLayoutBL.RetrieveOneAppFormFlexLayoutExDto(transactionExDto.FormId);

                    if (!(flexFormExDto.AppFormLayoutItemList != null && flexFormExDto.AppFormLayoutItemList.Count > 0))
                        needToReBuildDefaultFormLayout = true;
                }
            }
            else
            {
                AppFormExDto formDto = AppFormBL.CreateNewTranactionForm(transactionId, (int)EmAppFormLayoutType.Flex, transactionExDto.SaasApplicationId, false);
                transactionExDto.FormId = (int)formDto.Id;
                needToReBuildDefaultFormLayout = true;
            }

            if (needToReBuildDefaultFormLayout)
            {
                if (AppFormFlexLayoutBL.FormHasLayoutItems(transactionExDto.FormId.Value))
                {
                    AppFormBL.EnsureAppFormLayoutTypeFlex(transactionExDto.FormId.Value);
                }
                else
                {
                    var rebuiltFormDto = AppFormFlexLayoutBL.BuildAppFormDefaultLayout(transactionExDto.FormId.Value, numberOfLayoutColumns);
                    rebuiltFormDto.LayoutType = (int)EmAppFormLayoutType.Flex;
                    var saveResult = AppFormFlexLayoutBL.SaveAppFormFlexLayoutExDto(rebuiltFormDto);
                    if (saveResult.ValidationResult.HasErrors)
                        validationResult.Merge(saveResult.ValidationResult);
                }
            }

            return aOperationCallResult;
        }

        private static OperationCallResult<object> EnsureTransactionDefaultFlexFormLayoutMigration(int transactionId, int? numberOfLayoutColumns = null)
        {
            OperationCallResult<object> result = new OperationCallResult<object>();
            ValidationResult validationResult = new ValidationResult();
            result.ValidationResult = validationResult;

            var transactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            if (transactionExDto == null)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionExDto), "App_TransactionEntity_NotFound_Error",
                    ValidationItemType.Error, "Transaction not found: " + transactionId));
                return result;
            }

            int? formId = transactionExDto.FormId;
            if (!formId.HasValue)
            {
                AppFormExDto formDto = AppFormBL.CreateNewTranactionForm(
                    transactionId, (int)EmAppFormLayoutType.Flex, transactionExDto.SaasApplicationId, false);
                formId = (int)formDto.Id;
            }

            if (AppFormFlexLayoutBL.FormHasLayoutItems(formId.Value))
            {
                AppFormBL.EnsureAppFormLayoutTypeFlex(formId.Value);
                return result;
            }

            var builtFormDto = AppFormFlexLayoutBL.BuildAppFormDefaultLayout(formId.Value, transactionExDto, numberOfLayoutColumns);
            builtFormDto.LayoutType = (int)EmAppFormLayoutType.Flex;
            var saveResult = AppFormFlexLayoutBL.SaveAppFormFlexLayoutExDto(builtFormDto);
            if (saveResult.ValidationResult.HasErrors)
                validationResult.Merge(saveResult.ValidationResult);

            return result;
        }

        /// <summary>Adds a search to the application main menu (MasterDataManagement route).</summary>
        public static OperationCallResult<object> AddSearchToApplicationMainMenu(int searchId, int? saasApplicationId, string menuName, string menuDescription)
        {
            OperationCallResult<object> result = new OperationCallResult<object>();
            ValidationResult validationResult = new ValidationResult();
            result.ValidationResult = validationResult;
            AddSearchToApplicationMainMenu(validationResult, searchId, saasApplicationId, menuName, menuDescription);
            return result;
        }

        private static void AddSearchToApplicationMainMenu(ValidationResult validationResult, int searchId, int? applicationId, string menuName, string menuDescription)
        {
            QuickGenerateTransactionDefaultSeachNavigation_GenerateAppMenu(validationResult, searchId, applicationId, menuName, menuDescription);
        }

        private static void QuickGenerateTransactionDefaultSeachNavigation_GenerateAppMenu(ValidationResult validationResult, int searchId, int? applicationId, string menuName, string menuDescription)
        {
            var menuData = AppTreeListMenuBL.RetrieveListMenuHairarchyDto(false, applicationId).ToList();

            var rootMenu = menuData[0];

            AppListMenuExDto existSearchMenu = null;
            int maxSort = 0;

            if (rootMenu.AppListMenu_List != null)
            {
                for (int i = 0; i < rootMenu.AppListMenu_List.Count; i++)
                {
                    var aMenu = rootMenu.AppListMenu_List.ToList()[i];
                    if (aMenu.Sort.HasValue && aMenu.Sort > maxSort)
                    {
                        maxSort = aMenu.Sort.Value;
                    }

                    if (aMenu.RouteCode == "MasterDataManagement" && aMenu.Link == searchId.ToString())
                    {
                        existSearchMenu = aMenu;
                        break;
                    }
                }
            }

            if (existSearchMenu != null)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SaveDataSetAndCreateSearchView_OK", ValidationItemType.Warning,
                    "App menu already exists. \n" + "Menu path: " + existSearchMenu.MenuPath));
            }
            else
            {
                AppListMenuExDto menuDto = new AppListMenuExDto();
                menuDto.EmAppMenuItemCategory = 1;
                menuDto.EmDeviceMenuShowMode = 3;
                menuDto.LinkType = 1;
                menuDto.RouteCode = "MasterDataManagement";
                menuDto.IconName = "";
                menuDto.Sort = maxSort + 1;
                menuDto.Name = menuName;
                menuDto.Description = menuDescription;
                menuDto.Link = searchId.ToString();
                menuDto.ParentId = (int)rootMenu.Id;

                var createMenuResult = AppTreeListMenuBL.SaveOneAppListMenuTreeNode(menuDto);
                if (createMenuResult.IsSuccessfulWithResult)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SaveDataSetAndCreateSearchView_OK", ValidationItemType.Message,
                        "New App menu created. \n" + "Menu path: " + rootMenu.Name + " / " + menuName));
                }
                else
                {
                    validationResult.Merge(createMenuResult.ValidationResult);
                }
            }
        }


        private static void QuickGenerateTransactionDefaultFolderNavigation_GenerateAppMenu(ValidationResult validationResult, int transactionId, int? applicationId, string menuName, string menuDescription)
        {
            var menuData = AppTreeListMenuBL.RetrieveListMenuHairarchyDto(false, applicationId).ToList();

            var rootMenu = menuData[0];

            AppListMenuExDto existSearchMenu = null;
            int maxSort = 0;

            if (rootMenu.AppListMenu_List != null)
            {
                for (int i = 0; i < rootMenu.AppListMenu_List.Count; i++)
                {
                    var aMenu = rootMenu.AppListMenu_List.ToList()[i];
                    if (aMenu.Sort.HasValue && aMenu.Sort > maxSort)
                    {
                        maxSort = aMenu.Sort.Value;
                    }

                    if (aMenu.RouteCode == "FolderNavigation" && aMenu.Link == transactionId.ToString())
                    {
                        existSearchMenu = aMenu;
                        break;
                    }
                }
            }

            if (existSearchMenu != null)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SaveDataSetAndCreateSearchView_OK", ValidationItemType.Warning,
                    "App menu already exists. \n" + "Menu path: " + existSearchMenu.MenuPath));
            }
            else
            {
                AppListMenuExDto menuDto = new AppListMenuExDto();
                menuDto.EmAppMenuItemCategory = 1;
                menuDto.EmDeviceMenuShowMode = 3;
                menuDto.LinkType = 1;
                menuDto.RouteCode = "FolderNavigation";
                menuDto.IconName = "";
                menuDto.Sort = maxSort + 1;
                menuDto.Name = menuName;
                menuDto.Description = menuDescription;
                menuDto.Link = transactionId.ToString();
                menuDto.ParentId = (int)rootMenu.Id;

                var createMenuResult = AppTreeListMenuBL.SaveOneAppListMenuTreeNode(menuDto);
                if (createMenuResult.IsSuccessfulWithResult)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SaveDataSetAndCreateSearchView_OK", ValidationItemType.Message,
                        "New App menu created. \n" + "Menu path: " + rootMenu.Name + " / " + menuName));
                }
                else
                {
                    validationResult.Merge(createMenuResult.ValidationResult);
                }
            }
        }

    }


    //public static class Extension
    //{
    //    /// <summary>
    //    /// Get a string representing the SQL source fragment
    //    /// </summary>
    //    /// <param name="statement">The SQL Statement to get the string from, can be any derived class</param>
    //    /// <returns>The SQL that represents the object</returns>
    //    public static string GetString(this TSqlFragment statement)
    //    {
    //        string s = string.Empty;
    //        if (statement == null) return string.Empty;

    //        for (int i = statement.FirstTokenIndex; i <= statement.LastTokenIndex; i++)
    //        {
    //            s += statement.ScriptTokenStream[i].Text;
    //        }

    //        return s;
    //    }
    //}

}