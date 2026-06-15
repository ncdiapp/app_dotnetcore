using APP.LBL.DatabaseSpecific;
using ExchangeBL.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Generation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using APP.Components.EntityDto;
#if NETFRAMEWORK
using System.Management.Automation.Language;
using System.Management.Automation;
#endif
using APP.Components.Dto;
using App.BL;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using System.Data.Common;
using Google.Protobuf.WellKnownTypes;

using APP.Framework;
namespace ExchangeBL
{
    public class JsonSchemaBL
    {
        /// <summary>
        /// from setting table, get sample data , then create schema, save schema 
        /// </summary>
        /// <param name="actionId"></param>
        public static string GenerateSchemaFromSampleAsync(int actionId)
        {
            AppIntergrationSettingParameterExDto dataExchangeSetting = DataExchangeSettingBL.GetSetting(actionId);

            string jsonStringData = dataExchangeSetting.JsonSampleData;
            if (IsValidJson(jsonStringData))
            {
                JsonSchema jsonSchema = GenerateSchemFromSample(dataExchangeSetting.JsonSampleData);

                string strSchema = jsonSchema.ToJson();


                dataExchangeSetting.JsonSchema = strSchema;

                DataExchangeSettingBL.SaveSetting(dataExchangeSetting);

                TSqlGeneratorSettings settings = new TSqlGeneratorSettings();

                settings.CreateUserDefinesTemplate(jsonSchema);

                return jsonSchema.ToUserDefineJson();
            }
            else
            {
                return "";

            }

        }


        public static string GenerateSchemaFromSampleJson(string jsonStringData)
        {
            if (IsValidJson(jsonStringData))
            {
                JsonSchema jsonSchema = GenerateSchemFromSample(jsonStringData);

                string strSchema = jsonSchema.ToJson();


                TSqlGeneratorSettings settings = new TSqlGeneratorSettings();

                settings.CreateUserDefinesTemplate(jsonSchema);

                return jsonSchema.ToUserDefineJson();
            }
            else
            {
                return "";

            }

        }

        public static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    // Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    //Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// save mapping schema to setting, return runtime schema
        /// </summary>
        /// <param name="actionId"></param>
        /// <param name="mappingSchema"></param>
        public static string NeedToDrop_SaveSchemaDataSetMapping(int actionId, string mappingSchema)
        {
            DataExchangeSettingBL.UpdateSchemaMapping(actionId, mappingSchema);

            return GenerateRuntimeSchemaFromDataSetMapping(mappingSchema).GetAwaiter().GetResult();
        }


        /// <summary>
        /// save mapping schema to setting, return runtime schema
        /// </summary>
        /// <param name="actionId"></param>
        /// <param name="mappingSchema"></param>
        public static async Task<string> GenerateRuntimeSchemaFromDataSetMapping(string mappingSchema, List<string> typeNames_NotToCreateStagingTable = null) // TODO-PHASE2
        {

            JsonSchema schema = await JsonSchema.FromJsonAsync(mappingSchema); // TODO-PHASE2

            TSqlGeneratorSettings settings = new TSqlGeneratorSettings();

            settings.ApplyUserDefines(schema, typeNames_NotToCreateStagingTable);

            return schema.ToUserDefineJson();
        }

        public static string CreateOrAlterDatabaseTableFromSchema(int actionId)
        {
            AppIntergrationSettingParameterExDto dataExchangeSetting = DataExchangeSettingBL.GetSetting(actionId);

            TSqlGeneratorSettings settings = new TSqlGeneratorSettings(dataExchangeSetting.TablePrefix);
            List<string> tableNames = GetTableNamesFromIntegrationSetting(dataExchangeSetting).GetAwaiter().GetResult(); // TODO-PHASE2: convert caller to async


            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataExchangeSetting.DataSourceId.Value);

            string errorMsg = "";

            //if (dataExchangeSetting.OtherSettingsDto != null && dataExchangeSetting.OtherSettingsDto.IsForceRecreateStagingTables)
            //{
            //    string dropTableQuery = "";             

            //    foreach (string tableName in tableNames)
            //    {
            //        string tableNameWithPrefix = settings.MappingTablePrefix + tableName;

            //        string query = new SqlWriter(tableNameWithPrefix, dataBaseFixture.SqlServerType.Value).DropTableIfExist() + ";\n";

            //        dropTableQuery += query;
            //    }

            //    // Replace Belgrade Command
            //    errorMsg += AppMetaDataBL.ExecSQlCommand(dataBaseFixture, dropTableQuery);
            //}

            string createTableScript = GenerateTableFromSchema(dataExchangeSetting.SchemaFromDataSetMapping, settings, dataExchangeSetting).GetAwaiter().GetResult(); // TODO-PHASE2: convert caller to async
            errorMsg += AppMetaDataBL.ExecSQlCommand(dataBaseFixture, createTableScript);

            return errorMsg;
        }



        public static string DropDatabaseTableFromSchema(int actionId)
        {
            AppIntergrationSettingParameterExDto dataExchangeSetting = DataExchangeSettingBL.GetSetting(actionId);

            TSqlGeneratorSettings settings = new TSqlGeneratorSettings(dataExchangeSetting.TablePrefix);
            List<string> tableNames = GetTableNamesFromIntegrationSetting(dataExchangeSetting).GetAwaiter().GetResult(); // TODO-PHASE2: convert caller to async


            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataExchangeSetting.DataSourceId.Value);

            string errorMsg = "";


            string dropTableQuery = "";

            foreach (string tableName in tableNames)
            {
                string tableNameWithPrefix = settings.MappingTablePrefix + tableName;

                string query = new SqlWriter(tableNameWithPrefix, dataBaseFixture.SqlServerType.Value).DropTableIfExist() + ";\n";

                dropTableQuery += query;
            }

            // Replace Belgrade Command
            string msgDropOneTable = AppMetaDataBL.ExecSQlCommand(dataBaseFixture, dropTableQuery);


            errorMsg += msgDropOneTable;


            return errorMsg;
        }



        /// <summary>
        /// build insert/update, select script, save script to jsonQuery
        /// </summary>
        /// <param name="actionId"></param>
        /// <param name="scriptType"></param>
        public static void GenerateScriptFromSchema(int actionId, EnumSqlScriptCRUDType scriptType)
        {
            AppIntergrationSettingParameterExDto dataExchangeSetting = DataExchangeSettingBL.GetSetting(actionId);

            TSqlGeneratorSettings settings = new TSqlGeneratorSettings(dataExchangeSetting.TablePrefix);

            Dictionary<string, string> dictionaryScript = GenerateScriptFromSchemaByType(dataExchangeSetting.SchemaFromDataSetMapping, scriptType, settings, dataExchangeSetting).GetAwaiter().GetResult(); // TODO-PHASE2: convert caller to async

            string strScript = Newtonsoft.Json.JsonConvert.SerializeObject(dictionaryScript);

            if (scriptType == EnumSqlScriptCRUDType.UpdateKey)
            {
                DataExchangeSettingBL.UpdatePostProcessScript(actionId, strScript);
            }
            else
            {
                DataExchangeSettingBL.UpdateJsonQuery(actionId, strScript);
            }
        }
        /// <summary>
        /// call api , get data , insert data to database
        /// </summary>
        /// <param name="actionId"></param>
        /// <returns></returns>
        public static async Task FromAPIToDatabaseAsync(int actionId)
        {
            await FromAPIToDatabaseByParametersAsync(actionId, null, null, null);
        }
        /// <summary>
        /// call api with parameters, insert data to database
        /// </summary>
        /// <param name="actionId"></param>
        /// <param name="headers"></param>
        /// <param name="queryParams"></param>
        /// <param name="pathParams"></param>
        /// <returns></returns>
        public static async Task FromAPIToDatabaseByParametersAsync(int actionId, Dictionary<string, string> headers, Dictionary<string, string> queryParams, Dictionary<string, string> pathParams, bool isSimulate = false)
        {
            AppIntergrationSettingParameterExDto dataExchangeSettingDTO = DataExchangeSettingBL.GetSetting(actionId);

            string strSchema = dataExchangeSettingDTO.SchemaFromDataSetMapping;

            Dictionary<string, string> dictInsertScript = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dataExchangeSettingDTO.JsonQuery);

            TSqlGeneratorSettings sqlGeneratorSettings = new TSqlGeneratorSettings(dataExchangeSettingDTO.TablePrefix);

            if (headers != null && headers.Count > 0)
            {
                foreach (KeyValuePair<string, string> oneKV in headers)
                {
                    dataExchangeSettingDTO.APIConfigParameters.Headers.Add(oneKV.Key, oneKV.Value);
                }
            }

            if (pathParams != null && pathParams.Count > 0)
            {
                foreach (KeyValuePair<string, string> oneKV in pathParams)
                {
                    if (dataExchangeSettingDTO.APIConfigParameters.PathParams.ContainsKey(oneKV.Key))
                    {
                        dataExchangeSettingDTO.APIConfigParameters.PathParams[oneKV.Key] = oneKV.Value;
                    }
                    else
                    {
                        //dataExchangeSettingDTO.APIConfigParameters.PathParams.Add(oneKV.Key, oneKV.Value);
                    }

                }
            }

            if (queryParams != null && queryParams.Count > 0)
            {
                foreach (KeyValuePair<string, string> oneKV in queryParams)
                {
                    if (dataExchangeSettingDTO.APIConfigParameters.QueryParams.ContainsKey(oneKV.Key))
                    {
                        dataExchangeSettingDTO.APIConfigParameters.QueryParams[oneKV.Key] = oneKV.Value;
                    }
                    else
                    {
                        //dataExchangeSettingDTO.APIConfigParameters.QueryParams.Add(oneKV.Key, oneKV.Value);
                    }
                }
            }

            List<string> lstResponse = await Helper.CallAPIAsync(dataExchangeSettingDTO.APIConfigParameters, null, dataExchangeSettingDTO.IntergrationSettingId).ConfigureAwait(false);

            if (dataExchangeSettingDTO.APIConfigParameters.Method == EnumHttpMethod.Get) // if get, then insert data to table
            {
                foreach (string response in lstResponse.Where(r => !string.IsNullOrWhiteSpace(r)))
                {
                    List<TableNameParameterDTO> lstConvertResult = await BuildSqlParameterValueFromJsonData(strSchema, response, isSimulate); // TODO-PHASE2

                    ExecApiOperationScriptWithParameter(dataExchangeSettingDTO, dictInsertScript, sqlGeneratorSettings, lstConvertResult);
                }
            }
        }


        public static void FromJsonToDatabaseByParameters(int actionId)
        {
            AppIntergrationSettingParameterExDto importSetting = DataExchangeSettingBL.GetSetting(actionId);
            ImportJsonToStagingTable(importSetting, importSetting.JsonSampleData);

            // return false;

        }

        public static void ImportJsonToStagingTable(AppIntergrationSettingParameterExDto importSetting, string jsonData)
        {
            string strSchema = importSetting.SchemaFromDataSetMapping;

            Dictionary<string, string> dictInsertScript = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(importSetting.JsonQuery);

            TSqlGeneratorSettings sqlGeneratorSettings = new TSqlGeneratorSettings(importSetting.TablePrefix);


            if (!string.IsNullOrWhiteSpace(jsonData))
            {
                List<TableNameParameterDTO> lstConvertResult = BuildSqlParameterValueFromJsonData(strSchema, jsonData).GetAwaiter().GetResult(); // TODO-PHASE2: convert caller to async

                ExecApiOperationScriptWithParameter(importSetting, dictInsertScript, sqlGeneratorSettings, lstConvertResult);

            }
        }

        public static void ExecApiOperationScriptWithParameter(AppIntergrationSettingParameterExDto dataExchangeSetting, Dictionary<string, string> dictInsertScript, TSqlGeneratorSettings schemaSettings, List<TableNameParameterDTO> lstConvertResult)
        {

            DatabaseFixture dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataExchangeSetting.DataSourceId.Value);


            Dictionary<string, string> dictKey2InternalKey = new Dictionary<string, string>();

            lstConvertResult.RemoveAll(oneResult => !dictInsertScript.ContainsKey(oneResult.TableName)); // remove no script.

            lstConvertResult.RemoveAll(oneResult => string.IsNullOrWhiteSpace(dictInsertScript[oneResult.TableName])); // remove empty script

            foreach (var oneResult in lstConvertResult)
            {
                string sqlScript = dictInsertScript[oneResult.TableName];

                JsonSchema jsonInstance = oneResult.JsonData;

                TSqlTableModel tableStructure = oneResult.TableSchema;

                List<DbParameter> parameterList = new List<DbParameter>();
                //List<SqlParameter> sqlParameters = new List<SqlParameter>();

                if (jsonInstance.Properties.ContainsKey(schemaSettings.MappingPath)) // clean path, for mapping proterty need json path.
                {
                    jsonInstance.Properties[schemaSettings.MappingPath].Value = Regex.Replace(jsonInstance.Properties[schemaSettings.MappingPath].Value.ToString(), "\\[[0-9]*\\]", "");// remove [0]...
                }

                // fill in internal foregin key by foreginkey's type name and foreginkey from dictionary
                string dictKey = string.Empty;

                string keyName = jsonInstance.Properties.Keys.FirstOrDefault(key => key.StartsWith(schemaSettings.MappingKeyPrefix));// check if key

                if (keyName != null) // or jsonInstance.Properties.Keys.Any(key => key.StartsWith(schemaSettings.MappingUniquePrefix)) unique 
                {
                    dictKey = $"{oneResult.TableName}:{jsonInstance.PathId}"; // pathid has all keys
                }

                if (jsonInstance.Properties.ContainsKey(schemaSettings.MappingInternalForeginKeyId) && !string.IsNullOrWhiteSpace(jsonInstance.ForeginKeyId))
                {
                    string dictForeginKey = $"{jsonInstance.ForeginKeyTypeName}:{jsonInstance.ForeginKeyId}";

                    jsonInstance.Properties[schemaSettings.MappingInternalForeginKeyId].Value = dictKey2InternalKey[dictForeginKey];
                }

                foreach (var property in jsonInstance.Properties)
                {
                    var propertySchema = property.Value;

                    var propertyModel = tableStructure.Properties.FirstOrDefault(p => p.Name == propertySchema.Name);

                    if (!propertyModel.IsSqlColumn) // not sql column, don't create parameter
                    {
                        continue;
                    }

                    object value = propertySchema.Value ?? DBNull.Value;

                    //sqlParameters.Add(new SqlParameter($"@{propertyModel.ColumnName}", value));

                    DbParameter parameter = dataBaseFixture.CreateParameter($"{propertyModel.ColumnName}");
                    parameter.Value = value;
                    parameterList.Add(parameter);

                }

                //SqlParameter returnParameter = new SqlParameter($"@{schemaSettings.MappingInternalKeyId}", DbType.Int32) { Direction = ParameterDirection.Output };

                //sqlParameters.Add(returnParameter);

                DbParameter returnParameter = dataBaseFixture.CreateParameter($"{schemaSettings.MappingInternalKeyId}");
                returnParameter.DbType = DbType.Int32;
                returnParameter.Direction = ParameterDirection.Output;
                parameterList.Add(returnParameter);


                //ICommand sqlClientCommand = new Command(connectionString).Sql(sqlScript);

                //sqlClientCommand.Params(sqlParameters.ToArray());
                //await sqlClientCommand.ExecAsync().ConfigureAwait(false);

                List<object> resultSet = new List<object>();
                string errorMsg = AppMetaDataBL.ExecSQlCommand(dataBaseFixture, sqlScript, false, parameterList, resultSet);

                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    errorMsg = "Table: " + oneResult.TableName + "\n\nError Details:\n" + errorMsg;
                    throw new Exception(errorMsg);
                }

                if (returnParameter.Value != null && returnParameter.Value != DBNull.Value && !string.IsNullOrWhiteSpace(dictKey))
                {
                    if (!dictKey2InternalKey.ContainsKey(dictKey))
                    {
                        dictKey2InternalKey.Add(dictKey, returnParameter.Value.ToString());
                    }
                }
                else if (resultSet.Count > 0 && resultSet.Last() != null && resultSet.Last() != DBNull.Value && !string.IsNullOrWhiteSpace(dictKey))
                {
                    if (!dictKey2InternalKey.ContainsKey(dictKey))
                    {
                        dictKey2InternalKey.Add(dictKey, resultSet.Last().ToString());
                    }
                }


            }
        }

        /// <summary>
        /// select data from database by internalKeyId, post json to web site, update keys if need.
        /// </summary>
        /// <param name="actionId"></param>
        /// <param name="headers"></param>
        /// <param name="queryParams"></param>
        /// <param name="pathParams"></param>
        /// <param name="internalKeyId"></param>
        /// <returns></returns>
        public static async Task<string> FromDatabaseToAPIAsync(int actionId, Dictionary<string, string> headers, Dictionary<string, string> queryParams, Dictionary<string, string> pathParams, int internalKeyId, bool isSimulate = false)
        {
            AppIntergrationSettingParameterExDto dataExchangeSettingDTO = DataExchangeSettingBL.GetSetting(actionId);

            Dictionary<string, string> dictSelectScript = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dataExchangeSettingDTO.JsonQuery);

            Dictionary<string, string> dictUpdateKeyScript = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dataExchangeSettingDTO.PostProcessScript);

            JsonSchema schema = await JsonSchema.FromJsonAsync(dataExchangeSettingDTO.SchemaFromDataSetMapping); // TODO-PHASE2

            schema.AllowAdditionalProperties = false;

            TSqlExecutor executor = new TSqlExecutor(schema);

            if (headers != null && headers.Count > 0)
            {
                foreach (KeyValuePair<string, string> oneKV in headers)
                {
                    dataExchangeSettingDTO.APIConfigParameters.Headers.Add(oneKV.Key, oneKV.Value);
                }
            }

            if (pathParams != null && pathParams.Count > 0)
            {
                foreach (KeyValuePair<string, string> oneKV in pathParams)
                {
                    if (dataExchangeSettingDTO.APIConfigParameters.PathParams.ContainsKey(oneKV.Key))
                    {
                        dataExchangeSettingDTO.APIConfigParameters.PathParams[oneKV.Key] = oneKV.Value;
                    }
                }
            }

            if (queryParams != null && queryParams.Count > 0)
            {
                foreach (KeyValuePair<string, string> oneKV in headers)
                {
                    if (dataExchangeSettingDTO.APIConfigParameters.QueryParams.ContainsKey(oneKV.Key))
                    {
                        dataExchangeSettingDTO.APIConfigParameters.QueryParams[oneKV.Key] = oneKV.Value;
                    }
                }
            }

            PushDataDTO pushDataDTO = SelectObjectByInternalKeyId(schema, internalKeyId.ToString(), dataExchangeSettingDTO.ConnectionString, executor, dictSelectScript, schema);



            List<string> lstResponse = await Helper.CallAPIAsync(dataExchangeSettingDTO.APIConfigParameters, pushDataDTO.RequestJson, dataExchangeSettingDTO.IntergrationSettingId).ConfigureAwait(false);

            if (pushDataDTO.IsNew && lstResponse.Count() > 0) // if new json, then update keys
            {
                pushDataDTO.ResponseJson = lstResponse.FirstOrDefault();

                UpdateJsonKeys(pushDataDTO, schema, dataExchangeSettingDTO, dictUpdateKeyScript, isSimulate);


            }

            return pushDataDTO.ResponseJson;


        }

        public static List<PushDataDTO> GetJsonFromDatabase(List<string> internalKeyIds, string connectionString, JsonSchema schema, Dictionary<string, string> selectScripts)
        {
            List<PushDataDTO> pushDataDTOs = new List<PushDataDTO>();

            TSqlExecutor executor = new TSqlExecutor(schema);

            foreach (string oneInternalKeyId in internalKeyIds)
            {
                try
                {
                    PushDataDTO pushDataDTO = SelectObjectByInternalKeyId(schema, oneInternalKeyId, connectionString, executor, selectScripts, schema);

                    pushDataDTOs.Add(pushDataDTO);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return pushDataDTOs;
        }

        public static void UpdateJsonKeys(PushDataDTO pushDataDTO, JsonSchema schema, AppIntergrationSettingParameterExDto dataExchangeSetting, Dictionary<string, string> dictUpdateKeyScript, bool isSimulate = false)
        {
            TSqlExecutor executor = new TSqlExecutor(schema);

            List<TableNameParameterDTO> lstConvertResult = executor.ConvertJsonData2SqlParameterValue(pushDataDTO.ResponseJson, isSimulate);

            foreach (var oneResult in lstConvertResult)
            {
                string tableName = oneResult.TableName;
                JsonSchema jsonInstance = oneResult.JsonData;

                string keyId = executor.GetKeyId(jsonInstance);
                string foreginKeyId = executor.GetForeginKeyId(jsonInstance);
                List<string> unique = executor.GetUnique(jsonInstance, tableName);

                List<RowInfoDTO> newRows = pushDataDTO.RowInfos.Where(r => r.TableName == tableName && r.Unique.SequenceEqual(unique)).ToList();

                if (newRows.Count() != 1)
                {
                    throw new Exception($"Find {newRows.Count()} row on {tableName} with {string.Join(" ", unique)}");
                }

                RowInfoDTO rowInfo = newRows.FirstOrDefault();

                if (rowInfo.IsNew)
                {
                    rowInfo.KeyNameId = new KeyValuePair<string, string>(rowInfo.KeyNameId.Key, keyId);
                    rowInfo.ForeginKeyNameId = new KeyValuePair<string, string>(rowInfo.ForeginKeyNameId.Key, foreginKeyId);
                }
            }

            if (pushDataDTO.RowInfos.Any(r => r.IsNew))
            {
                UpdateNewJsonKeysOnTable(dataExchangeSetting, dictUpdateKeyScript, pushDataDTO);
            }
        }


        public static string GetJsonFromDatabase(string ids, string connectionString, JsonSchema schema, Dictionary<string, string> selectScripts)
        {
            TSqlExecutor tSqlExecutor = new TSqlExecutor(schema);

            dynamic rootJsonObject = null;

            PushDataDTO pushDataDTO = new PushDataDTO() { RowInfos = new List<RowInfoDTO>() };

            if (schema.Type == JsonObjectType.Array)
            {
                rootJsonObject = SelectArray(schema.Item.Reference, ids, connectionString, tSqlExecutor, selectScripts, schema.Item.Reference, pushDataDTO);
            }
            else if (schema.Type == JsonObjectType.Object)
            {
                rootJsonObject = SelectObject(schema, ids, connectionString, tSqlExecutor, selectScripts, schema, pushDataDTO);
            }

            string strReturn = Newtonsoft.Json.JsonConvert.SerializeObject(rootJsonObject, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                //DefaultValueHandling = DefaultValueHandling.Include,
            });
            return strReturn;
        }

        #region private
        private static PushDataDTO SelectObjectByInternalKeyId(JsonSchema schema, string internalKeyId, string connectionString, TSqlExecutor tSqlExecutor, Dictionary<string, string> selectScripts, JsonSchema rootSchema)
        {
            PushDataDTO pushDataDTO = new PushDataDTO() { InternalKeyId = internalKeyId, RowInfos = new List<RowInfoDTO>() };

            DataTable dataTable = SelectDataFromDB(schema, internalKeyId, connectionString, tSqlExecutor, selectScripts);

            if (dataTable.Rows.Count != 1)
            {
                throw new Exception($"Return {dataTable.Rows.Count} row, not 1 row ");
            }
            else
            {
                DataRow dataRow = dataTable.Rows[0];

                dynamic jsonData = SelectData(schema, internalKeyId, connectionString, tSqlExecutor, selectScripts, dataRow, rootSchema, pushDataDTO);

                pushDataDTO.RequestJson = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);
            }

            return pushDataDTO;
        }

        private static dynamic SelectObject(JsonSchema schema, string foregianKey, string connectionString, TSqlExecutor tSqlExecutor, Dictionary<string, string> selectScripts, JsonSchema rootSchema, PushDataDTO pushDataDTO)
        {
            dynamic rowData = null;

            DataTable dataTable = SelectDataFromDB(schema, foregianKey, connectionString, tSqlExecutor, selectScripts);

            if (dataTable.Rows.Count == 1)
            {
                DataRow dataRow = dataTable.Rows[0];

                rowData = SelectData(schema, foregianKey, connectionString, tSqlExecutor, selectScripts, dataRow, rootSchema, pushDataDTO);
            }

            return rowData;
        }

        private static dynamic SelectArray(JsonSchema schema, string foregianKey, string connectionString, TSqlExecutor tSqlExecutor, Dictionary<string, string> selectScripts, JsonSchema rootSchema, PushDataDTO pushDataDTO)
        {
            DataTable dataTable = SelectDataFromDB(schema, foregianKey, connectionString, tSqlExecutor, selectScripts);

            dynamic lstData = new List<dynamic>();

            foreach (DataRow oneRow in dataTable.Rows)
            {
                dynamic rowData = SelectData(schema, foregianKey, connectionString, tSqlExecutor, selectScripts, oneRow, rootSchema, pushDataDTO);

                lstData.Add(rowData);
            }

            return lstData;
        }

        private static dynamic SelectData(JsonSchema schema, string foregianKey, string connectionString, TSqlExecutor tSqlExecutor, Dictionary<string, string> selectScripts, DataRow dataRow, JsonSchema rootSchema, PushDataDTO pushDataDTO)
        {
            string newForegianKey = tSqlExecutor.GetInternalKeyNameId(dataRow).Value ?? foregianKey;

            RowInfoDTO internalKeyDTO = new RowInfoDTO()
            {
                TableName = tSqlExecutor.GetTypeName(schema),
                KeyNameId = tSqlExecutor.GetKeyNameId(schema, dataRow),
                ForeginKeyNameId = tSqlExecutor.GetForeginKeyNameId(dataRow),
                InternalKeyNameId = tSqlExecutor.GetInternalKeyNameId(dataRow),
                InternalForeginKeyId = tSqlExecutor.GetInternalForeginKeyId(dataRow),
                Unique = tSqlExecutor.GetUnique(schema, dataRow),
                IsNew = tSqlExecutor.IsNewRow(schema, dataRow),
            };

            if (selectScripts.Keys.Contains(internalKeyDTO.TableName))
            {
                pushDataDTO.RowInfos.Add(internalKeyDTO);

                if (pushDataDTO.RowInfos.Count() == 1)
                {
                    pushDataDTO.id = internalKeyDTO.KeyNameId.Value;
                }
            }

            dynamic rowData = tSqlExecutor.ConvertDataRowToJsonObject(dataRow);

            IDictionary<String, Object> dictData = (IDictionary<String, Object>)rowData;

            List<string> propertyNames = dictData.Keys.ToList();

            // compare with schema
            foreach (string onePropertyName in propertyNames)
            {
                if (schema.Properties.ContainsKey(onePropertyName))
                {
                    JsonSchemaProperty jsonSchemaProperty = schema.Properties[onePropertyName];

                    if (jsonSchemaProperty.Reference != null)
                    {
                        dictData[onePropertyName] = SelectObject(jsonSchemaProperty.Reference, newForegianKey, connectionString, tSqlExecutor, selectScripts, rootSchema, pushDataDTO);
                    }
                    else if (jsonSchemaProperty.Item != null && jsonSchemaProperty.Item.Reference != null)
                    {
                        dictData[onePropertyName] = SelectArray(jsonSchemaProperty.Item.Reference, newForegianKey, connectionString, tSqlExecutor, selectScripts, rootSchema, pushDataDTO);
                    }
                }
            }

            if (dictData.Count == 1 && schema != rootSchema)
            {
                // ?????????????
                // rowData = dictData.FirstOrDefault().Value;//array string
            }

            return rowData;
        }

        private static DataTable SelectDataFromDB(JsonSchema schema, string foregianKey, string connectionString, TSqlExecutor tSqlExecutor, Dictionary<string, string> selectScripts)
        {
            DataTable dataTable = null;

            string entityTypeName = tSqlExecutor.GetTypeName(schema);

            if (!selectScripts.Keys.Contains(entityTypeName))
            {
                dataTable = new DataTable();

                foreach (string key in schema.Properties.Keys)
                {
                    DataColumn dataColumn = new DataColumn() { ColumnName = key, DataType = typeof(string) };
                    dataTable.Columns.Add(dataColumn);
                }

                DataRow dataRow = dataTable.NewRow();

                dataTable.Rows.Add(dataRow);
            }
            else
            {
                StringBuilder sbQuery = new StringBuilder();

                sbQuery.AppendLine(selectScripts[entityTypeName]);

                if (!string.IsNullOrWhiteSpace(foregianKey))
                {
                    sbQuery.AppendLine(tSqlExecutor.BuildWhere(schema, foregianKey));
                }

                using (DataAccessAdapter adapter = new DataAccessAdapter(connectionString))
                {
                    //adapter.StartTransaction(IsolationLevel.ReadCommitted, "SelectData");

                    dataTable = adapter.ExecuteDataTableRetrievalQuery(sbQuery.ToString(), new System.Collections.Generic.List<System.Data.SqlClient.SqlParameter>());

                    //adapter.Commit();
                }
            }

            return dataTable;
        }

        internal static JsonSchema GenerateSchemFromSample(string jsonData)
        {
            var generator = new TSqlSampleJsonSchemaGenerator();

            return generator.Generate(jsonData);

        }

        private static async Task<string> GenerateTableFromSchema(string jsonSchema, TSqlGeneratorSettings generatorSettings, AppIntergrationSettingParameterExDto apiOperationSetting) // TODO-PHASE2
        {
            var schema = await JsonSchema.FromJsonAsync(jsonSchema); // TODO-PHASE2

            PrepareTableGenerationUserDefinedProperties(generatorSettings, apiOperationSetting, schema);

            schema.AllowAdditionalProperties = false;

            var generator = new TSqlGenerator(schema, generatorSettings);

            return generator.GenerateFile();
        }


        private static async Task<Dictionary<string, string>> GenerateScriptFromSchemaByType(string jsonSchema, EnumSqlScriptCRUDType scriptType, TSqlGeneratorSettings generatorSettings, AppIntergrationSettingParameterExDto apiOperationSetting) // TODO-PHASE2
        {
            var schema = await JsonSchema.FromJsonAsync(jsonSchema); // TODO-PHASE2

            PrepareTableSchemaScriptUserDefinedProperties(generatorSettings, apiOperationSetting, schema);

            schema.AllowAdditionalProperties = false;

            var generator = new TSqlGenerator(schema, generatorSettings);

            return generator.GenerateScripts(scriptType);
        }



        public static async Task<List<TableNameParameterDTO>> BuildSqlParameterValueFromJsonData(string jsonSchema, string jsonData, bool isSimulate = false) // TODO-PHASE2
        {
            JsonSchema schema = await JsonSchema.FromJsonAsync(jsonSchema); // TODO-PHASE2

            schema.AllowAdditionalProperties = false;

            var executor = new TSqlExecutor(schema);

            return executor.ConvertJsonData2SqlParameterValue(jsonData, isSimulate);
        }

        private static void UpdateNewJsonKeysOnTable(AppIntergrationSettingParameterExDto dataExchangeSetting, Dictionary<string, string> dictUpdateKeyScript, PushDataDTO pushDataDTO)
        {
            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataExchangeSetting.DataSourceId.Value);

            foreach (RowInfoDTO rowInfo in pushDataDTO.RowInfos.Where(r => r.IsNew))
            {
                if (dictUpdateKeyScript.ContainsKey(rowInfo.TableName))
                {
                    string updateScript = dictUpdateKeyScript[rowInfo.TableName];


                    List<DbParameter> parameterList = new List<DbParameter>();

                    if (!string.IsNullOrWhiteSpace(rowInfo.KeyNameId.Value))
                    {
                        //sqlParameters.Add(new SqlParameter($"@{rowInfo.KeyNameId.Key}", rowInfo.KeyNameId.Value));

                        DbParameter parameter = dataBaseFixture.CreateParameter($"{rowInfo.KeyNameId.Key}");
                        parameter.Value = rowInfo.KeyNameId.Value;
                        parameterList.Add(parameter);
                    }

                    if (!string.IsNullOrWhiteSpace(rowInfo.ForeginKeyNameId.Value))
                    {
                        //sqlParameters.Add(new SqlParameter($"@{rowInfo.ForeginKeyNameId.Key}", rowInfo.ForeginKeyNameId.Value));

                        DbParameter parameter = dataBaseFixture.CreateParameter($"{rowInfo.ForeginKeyNameId.Key}");
                        parameter.Value = rowInfo.ForeginKeyNameId.Value;
                        parameterList.Add(parameter);
                    }

                    //sqlParameters.Add(new SqlParameter($"@{rowInfo.InternalKeyNameId.Key}", rowInfo.InternalKeyNameId.Value));

                    DbParameter parameterInternalKey = dataBaseFixture.CreateParameter($"{rowInfo.InternalKeyNameId.Key}");
                    parameterInternalKey.Value = rowInfo.InternalKeyNameId.Value;
                    parameterList.Add(parameterInternalKey);

                    //ICommand sqlClientCommand = new Command(connectionString).Sql(updateScript);

                    //sqlClientCommand.Params(sqlParameters.ToArray());

                    //await sqlClientCommand.ExecAsync().ConfigureAwait(false);

                    List<object> resultSet = new List<object>();

                    string errorMsg = AppMetaDataBL.ExecSQlCommand(dataBaseFixture, updateScript, false, parameterList, resultSet);
                }

            }
        }


        public static async Task<List<string>> GetTableNamesFromIntegrationSetting(AppIntergrationSettingParameterExDto dataExchangeSetting) // TODO-PHASE2
        {
            List<string> tableNames = new List<string>();

            string tablePrefix = dataExchangeSetting.TablePrefix;

            JsonSchema schema = await JsonSchema.FromJsonAsync(dataExchangeSetting.SchemaFromDataSetMapping); // TODO-PHASE2

            var userDefines = schema.UserDefines;

            if (userDefines != null && userDefines.ExtensionData != null)
            {
                // List<string> rollupTypeNameList = new List<string>();

                userDefines.ExtensionData.TryGetValue(TSqlGeneratorSettings.UserDefineKeyOptions, out object userDefineOptions);

                //if (lstRollup != null)
                //{
                //    string strLstRollup = Newtonsoft.Json.JsonConvert.SerializeObject(lstRollup);
                //    rollupTypeNameList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(strLstRollup);
                //}                


                if (userDefineOptions != null)
                {
                    string strTypeSetup = Newtonsoft.Json.JsonConvert.SerializeObject(userDefineOptions);

                    List<ObjectTypeMappingDTO> lstMapping = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ObjectTypeMappingDTO>>(strTypeSetup);

                    foreach (ObjectTypeMappingDTO mapping in lstMapping)
                    {
                        if (mapping.is_single_field)
                        {

                        }
                        //else if (rollupTypeNameList.Contains(mapping.object_type_definition_name))
                        //{ 

                        //}
                        else
                        {
                            string tableName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(mapping.mapping_db_table_name);

                            if (!string.IsNullOrWhiteSpace(tableName) && !tableNames.Contains(tableName))
                            {
                                tableNames.Add(tableName);
                            }
                        }
                    }
                }
            }

            return tableNames.OrderBy(o => o).ToList();
        }

        private static void PrepareTableGenerationUserDefinedProperties(TSqlGeneratorSettings generatorSettings, AppIntergrationSettingParameterExDto apiOperationSetting, JsonSchema schema)
        {
            if (apiOperationSetting.DataSourceId.HasValue)
            {
                var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(apiOperationSetting.DataSourceId.Value);
                if (dataBaseFixture.SqlServerType.HasValue)
                {
                    schema.JsonSqlServerTypeName = dataBaseFixture.SqlServerType.Value.ToString();
                }

                if (schema.JsonSqlServerTypeName == EnumJsonSqlServerType.MySql.ToString())
                {
                    generatorSettings.AnyType = "TEXT";
                    generatorSettings.ArrayType = "TEXT";
                    generatorSettings.LongStringType = "LONGTEXT";
                }
                else if (schema.JsonSqlServerTypeName == EnumJsonSqlServerType.Oracle.ToString())
                {

                }



                Dictionary<string, List<string>> dictExistTableNameAndColumnNameList = new Dictionary<string, List<string>>();

                foreach (var nodeSetting in apiOperationSetting.SchemaDataSetMappingDto.NodeSettingDtoList)
                {
                    if (!string.IsNullOrWhiteSpace(nodeSetting.MappingToTableName))
                    {
                        string tableName = apiOperationSetting.TablePrefix + generatorSettings.MappingSplit + nodeSetting.MappingToTableName;

                        DatabaseTable tableDto = dataBaseFixture.Table(tableName);

                        if (tableDto != null)
                        {
                            if (!dictExistTableNameAndColumnNameList.ContainsKey(tableDto.Name))
                            {
                                dictExistTableNameAndColumnNameList.Add(tableDto.Name, tableDto.Columns.Select(o => o.Name).ToList());
                            }
                        }
                    }
                }

                schema.DictExistTableNameAndColumnNameList = dictExistTableNameAndColumnNameList;
            }
        }

        private static void PrepareTableSchemaScriptUserDefinedProperties(TSqlGeneratorSettings generatorSettings, AppIntergrationSettingParameterExDto apiOperationSetting, JsonSchema schema)
        {
            if (apiOperationSetting.DataSourceId.HasValue)
            {
                var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(apiOperationSetting.DataSourceId.Value);
                if (dataBaseFixture.SqlServerType.HasValue)
                {
                    schema.JsonSqlServerTypeName = dataBaseFixture.SqlServerType.Value.ToString();
                }

                if (schema.JsonSqlServerTypeName == EnumJsonSqlServerType.MySql.ToString())
                {
                    generatorSettings.AnyType = "TEXT";
                    generatorSettings.ArrayType = "TEXT";
                    generatorSettings.LongStringType = "LONGTEXT";
                }
                else if (schema.JsonSqlServerTypeName == EnumJsonSqlServerType.Oracle.ToString())
                {

                }
            }
        }
        private class ObjectTypeMappingDTO
        {
            public string object_type_definition_name { get; set; }
            public string mapping_db_table_name { get; set; }
            public string key { get; set; }
            public string[] unique { get; set; }
            public string[] serialization { get; set; }
            public bool is_single_field { get; set; }
            public string mapping_single_field_type_db_path { get; set; }

            public ObjectTypeMappingDTO()
            {
                object_type_definition_name = string.Empty;
                mapping_db_table_name = string.Empty;
                key = string.Empty;
                unique = new string[] { };
                serialization = new string[] { };
                is_single_field = false;
                mapping_single_field_type_db_path = string.Empty;
            }
        }
        #endregion
    }


}
