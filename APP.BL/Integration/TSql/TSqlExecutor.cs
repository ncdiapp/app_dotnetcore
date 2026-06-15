using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data;
using System.Dynamic;
using NJsonSchema;

namespace ExchangeBL
{
    /// <summary>
    ///
    /// </summary>
    public class TSqlExecutor
    {
        /// <summary>Generates the Tsql parameters for the given JSON data.</summary>
        /// <returns>The JSON Schema.</returns>
        private readonly JsonSchema _runtimeSchema;
        private readonly TSqlGeneratorSettings _settings;
        private readonly TSqlTypeResolver _resolver;

        private Dictionary<string, object> _dictNodes = new Dictionary<string, object>();
        private List<TableNameParameterDTO> _convertResults = new List<TableNameParameterDTO>();

        public TSqlExecutor()
        {
            _settings = new TSqlGeneratorSettings();
            _resolver = new TSqlTypeResolver(_settings);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="runtimeSchema"></param>
        public TSqlExecutor(JsonSchema runtimeSchema) : this()
        {
            _runtimeSchema = runtimeSchema;
            _resolver.Resolve(runtimeSchema, false, runtimeSchema.Title); // register root type
        }

        public TSqlExecutor(JsonSchema mappingDataSetSchema, TSqlGeneratorSettings settings)
        {
            _settings = settings;
            _resolver = new TSqlTypeResolver(_settings);
            _runtimeSchema = mappingDataSetSchema;
            _resolver.Resolve(mappingDataSetSchema, false, mappingDataSetSchema.Title); // register root type
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="jsonData"></param>
        /// <returns></returns>
        public List<TableNameParameterDTO> ConvertJsonData2SqlParameterValue(string jsonData, bool isSimulate = false)
        {
            var token = JsonConvert.DeserializeObject<JToken>(jsonData, new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });

            var schema = _runtimeSchema;

            _resolver.Types.TryGetValue(schema, out string rootTypeName);

            if (rootTypeName != null && !_settings.ExcludedTypeNames.Contains(rootTypeName))
            {
                AddRoot(schema, rootTypeName);
            }

            GenerateWithoutReference(token, schema, schema, "Anonymous", isSimulate);

            return _convertResults;
        }

        /// <summary>
        /// Get typeName from schema
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public string GetTypeName(JsonSchema schema)
        {
            return _resolver.Types[schema];
        }
        /// <summary>
        /// get internalkeyId from datarow
        /// </summary>
        /// <param name="dataRow"></param>
        /// <returns></returns>
        public KeyValuePair<string, string> GetInternalKeyNameId(DataRow dataRow)
        {
            string keyId = null;
            try
            {
                keyId = dataRow[_settings.MappingInternalKeyId]?.ToString();
            }
            catch (Exception ex)
            {

            }
            return new KeyValuePair<string, string>(_settings.MappingInternalKeyId, keyId);
        }
        /// <summary>
        /// get internal foregin keyId from datarow
        /// </summary>
        /// <param name="dataRow"></param>
        /// <returns></returns>
        public string GetInternalForeginKeyId(DataRow dataRow)
        {
            string keyId = null;
            try
            {
                keyId = dataRow[_settings.MappingInternalForeginKeyId]?.ToString();
            }
            catch (Exception ex)
            {

            }
            return keyId;
        }
        /// <summary>
        /// get key name & id
        /// </summary>
        /// <param name="dataRow"></param>
        /// <param name="schema"></param>
        /// <returns></returns>

        public KeyValuePair<string, string> GetKeyNameId(JsonSchema schema, DataRow dataRow)
        {
            var mappingKeyName = schema.Properties.Keys.FirstOrDefault(key => key.StartsWith(_settings.MappingKeyPrefix));

            if (mappingKeyName != null)
            {
                mappingKeyName = mappingKeyName.Replace(_settings.MappingKeyPrefix, "");

                return new KeyValuePair<string, string>(mappingKeyName, dataRow[mappingKeyName]?.ToString());
            }
            else
            {
                return new KeyValuePair<string, string>(null, null);
            }
        }
        /// <summary>
        ///  get foregin key id
        /// </summary>
        /// <param name="dataRow"></param>
        /// <returns></returns>

        public KeyValuePair<string, string> GetForeginKeyNameId(DataRow dataRow)
        {
            string keyId = null;
            try
            {
                keyId = dataRow[_settings.MappingForeginKeyId]?.ToString();
            }
            catch (Exception ex)
            {

            }
            return new KeyValuePair<string, string>(_settings.MappingForeginKeyId, keyId);
        }
        /// <summary>
        ///  get uniqe values for update  after push new json
        /// </summary>
        /// <param name="dataRow"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public List<string> GetUnique(JsonSchema schema, DataRow dataRow)
        {
            string typeName = GetTypeName(schema);

            string[] unique = _settings.GetUniqeFromUserDefinesOption(_runtimeSchema, typeName);

            List<string> lstUnique = new List<string>();

            foreach (string oneColumn in unique)
            {
                try
                {
                    lstUnique.Add(dataRow[oneColumn].ToString());
                }
                catch (Exception ex)
                {
                    lstUnique.Add(string.Empty);
                }
            }

            return lstUnique;
        }
        /// <summary>
        /// if key is null or foreign key is null without key
        /// </summary>
        /// <param name="dataRow"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public bool IsNewRow(JsonSchema schema, DataRow dataRow)
        {
            var mappingKeyName = schema.Properties.Keys.FirstOrDefault(key => key.StartsWith(_settings.MappingKeyPrefix));

            if (mappingKeyName != null)
            {
                string keyId = GetKeyNameId(schema, dataRow).Value;

                return string.IsNullOrWhiteSpace(keyId);
            }
            else
            {
                string ForeginKeyId = GetForeginKeyNameId(dataRow).Value;

                return string.IsNullOrWhiteSpace(ForeginKeyId);
            }
        }
        /// <summary>
        /// get key from json
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public string GetKeyId(JsonSchema schema)
        {
            var mappingKeyName = schema.Properties.Keys.FirstOrDefault(key => key.StartsWith(_settings.MappingKeyPrefix));

            if (mappingKeyName != null)
            {
                return schema.Properties[mappingKeyName].Value.ToString();
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// get foregin key from json
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public string GetForeginKeyId(JsonSchema schema)
        {
            if (schema.Properties.ContainsKey(_settings.MappingForeginKeyId))
            {
                return schema.Properties[_settings.MappingForeginKeyId].Value.ToString();
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// get unique from json
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public List<string> GetUnique(JsonSchema schema, string typeName)
        {
            string[] unique = _settings.GetUniqeFromUserDefinesOption(_runtimeSchema, typeName);

            List<string> lstUnique = new List<string>();

            foreach (string oneProperty in unique)
            {
                string strUnique = schema.Properties[oneProperty].Value != null ? schema.Properties[oneProperty].Value.ToString() : string.Empty;

                lstUnique.Add(Regex.Replace(strUnique, "\\[[0-9]*\\]", ""));
            }

            if (typeName == _settings.MappingSingleField) // change singlefield path from single schema path to collection schema path --- products.options.values.. product.options.values
            {
                IDictionary<string, string> dictSingleFieldPath = _settings.GetSingleFieldPathFromUserDefinesOption(_runtimeSchema);

                if (dictSingleFieldPath.ContainsKey(lstUnique[1]))
                {
                    lstUnique[1] = dictSingleFieldPath[lstUnique[1]];
                }
            }

            return lstUnique;
        }
        /// <summary>
        /// build where for select
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="ids">internal id or internal foregin key</param>
        /// <returns></returns>
        public string BuildWhere(JsonSchema schema, string ids)
        {
            List<string> lstWhere = new List<string>();

            List<string> propertyNames = schema.Properties.Keys.ToList();

            //internal foregin key
            if (propertyNames.Contains(_settings.MappingInternalForeginKeyId))
            {
                lstWhere.Add($" {_settings.MappingInternalForeginKeyId} in ({ids}) ");
            }
            // internal key id
            else if (propertyNames.Any(p => p.StartsWith(_settings.MappingKeyPrefix)))
            {
                if (ids.Equals("null", StringComparison.InvariantCultureIgnoreCase))
                {
                    lstWhere.Add($" InternalKeyId is null  ");
                }
                else
                {
                    lstWhere.Add($" InternalKeyId in ({ids})  ");
                }
            }
            else if (propertyNames.Any(p => p.StartsWith(_settings.MappingUniquePrefix)))
            {
                var realKeyNames = propertyNames.FirstOrDefault(p => p.StartsWith(_settings.MappingUniquePrefix))
                    .Replace(_settings.MappingUniquePrefix, "")
                    .Split(new string[] { _settings.MappingSplit }, StringSplitOptions.None);

                var arrayId = ids.Split(',');

                int j = 0;

                for (int i = 0; i < arrayId.Length; i++)
                {
                    lstWhere.Add($" {realKeyNames[j]} = '{arrayId[i]}'  ");

                    j++;
                    if (j == realKeyNames.Length)
                    {
                        j = 0;
                    }
                }
            }

            return $" WHERE  {string.Join(" AND ", lstWhere)} ";
        }
        /// <summary>
        /// convert data row to json
        /// </summary>
        /// <param name="dataRow"></param>
        /// <returns></returns>
        public dynamic ConvertDataRowToJsonObject(DataRow dataRow)
        {
            dynamic rootDynamicData = new ExpandoObject();
            var dictRootData = ((IDictionary<String, Object>)rootDynamicData);

            DataTable dt = dataRow.Table;

            List<string> mappingColumns = new List<string>() {
                _settings.MappingPath,
                _settings.MappingSort,
                _settings.MappingForeginKeyId,
                _settings.MappingInternalKeyId,
                _settings.MappingInternalForeginKeyId,
                _settings.MappingExternalKeyId,
                _settings.MappingExternalForeginKeyId,
            };

            foreach (DataColumn colum in dt.Columns)
            {
                string fullColumnName = colum.ColumnName;

                if (mappingColumns.Contains(fullColumnName))
                {
                    continue;
                }

                string[] path = fullColumnName.Split(new string[] { _settings.MappingSplit }, StringSplitOptions.None);

                object propertyValue = dataRow[fullColumnName];

                if (propertyValue == DBNull.Value)
                {
                    propertyValue = null;
                }

                // do something else

                if (path.Length == 1)
                {
                    dictRootData[path[0]] = propertyValue;
                }
                else // mutiple level
                {
                    IDictionary<string, object> dictDatalevel = GetOneLevelDict(dictRootData, path, 0);
                    dictDatalevel[path[path.Length - 1]] = propertyValue;
                    //Customer  path[0]
                    //Customer.Account  path[1]
                    //Customer.Account.AddressInfo path[2]

                }
            }


            return rootDynamicData;
        }

        #region private
        private void Generate(JToken token, JsonSchema schema, JsonSchema rootSchema, string typeNameHint, bool isSimulate = false)
        {
            if (schema != rootSchema && token.Type == JTokenType.Object)
            {
                JsonSchema referencedSchema = FindSchema(schema);

                if (referencedSchema == null)
                {
                    if (schema.Type == JsonObjectType.String)
                    {
                        schema.Value = token.ToString();
                    }
                    else
                    {
                        ParseToken(token);
                    }
                    return;
                }

                referencedSchema.Path = schema.Path;
                referencedSchema.Sort = schema.Sort;
                referencedSchema.PathId = schema.PathId;
                referencedSchema.PathTypeName = schema.PathTypeName;
                referencedSchema.ForeginKeyId = GetForeginKey(schema.PathId);
                referencedSchema.ForeginKeyTypeName = GetForeginKeyTypeName(schema.PathTypeName);

                schema.Reference = referencedSchema;

                GenerateWithoutReference(token, referencedSchema, rootSchema, typeNameHint, isSimulate);
                return;
            }

            GenerateWithoutReference(token, schema, rootSchema, typeNameHint, isSimulate);
        }

        private void GenerateWithoutReference(JToken token, JsonSchema schema, JsonSchema rootSchema, string typeNameHint, bool isSimulate = false)
        {
            if (token == null)
            {
                return;
            }

            switch (token.Type)
            {
                case JTokenType.Object:
                    GenerateObject(token, schema, rootSchema, isSimulate);
                    break;

                case JTokenType.Array:
                    GenerateArray(token, schema, rootSchema, typeNameHint, isSimulate);
                    break;

                case JTokenType.Date:
                    schema.Type = JsonObjectType.String;
                    schema.Format = token.Value<DateTime>() == token.Value<DateTime>().Date
                        ? JsonFormatStrings.Date
                        : JsonFormatStrings.DateTime;
                    schema.Value = token.Value<DateTime>();
                    break;

                case JTokenType.String:
                    schema.Type = JsonObjectType.String;
                    schema.Value = token.Value<string>();
                    break;

                case JTokenType.Boolean:
                    schema.Type = JsonObjectType.Boolean;
                    schema.Value = token.Value<bool>();
                    break;

                case JTokenType.Integer:
                    schema.Type = JsonObjectType.Integer;
                    schema.Value = token.Value<Int64>();
                    break;

                case JTokenType.Float:
                    schema.Type = JsonObjectType.Number;
                    schema.Value = token.Value<string>();
                    break;

                case JTokenType.Bytes:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.Byte;
                    schema.Value = token.Value<byte>();
                    break;

                case JTokenType.TimeSpan:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.Duration;
                    schema.Value = token.Value<DateTime>();
                    break;

                case JTokenType.Guid:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.Guid;
                    schema.Value = token.Value<Guid>();
                    break;

                case JTokenType.Uri:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.Uri;
                    schema.Value = token.Value<string>();
                    break;
            }

        }

        private void GenerateObject(JToken token, JsonSchema schema, JsonSchema rootSchema, bool isSimulate = false)
        {
            schema.Type = JsonObjectType.Object;
            foreach (var property in ((JObject)token).Properties().Where(p => p.Value.Type != JTokenType.Object && p.Value.Type != JTokenType.Array))
            {
                var propertySchema = new JsonSchemaProperty();
                var propertyName = property.Name; // property.Value.Type == JTokenType.Array ? ConversionUtilities.Singularize(property.Name) : property.Name;
                var typeNameHint = propertyName;// ConversionUtilities.ConvertToUpperCamelCase(propertyName, true);

                Generate(property.Value, propertySchema, rootSchema, typeNameHint, isSimulate);

                if (schema.Properties.ContainsKey(property.Name))
                {
                    schema.Properties[property.Name] = propertySchema;
                }

                if (!schema.IsPathIdReady)
                {
                    ProcessKeyPath(schema);
                }
            }

            foreach (var property in ((JObject)token).Properties().Where(p => p.Value.Type == JTokenType.Object || p.Value.Type == JTokenType.Array))
            {
                var propertySchema = new JsonSchemaProperty();
                var propertyName = property.Name; // property.Value.Type == JTokenType.Array ? ConversionUtilities.Singularize(property.Name) : property.Name;
                var typeNameHint = propertyName;// ConversionUtilities.ConvertToUpperCamelCase(propertyName, true);

                if (property.Value.Type == JTokenType.Object || property.Value.Type == JTokenType.Array)
                {
                    if (property.Value.ToString().Length < 3) // {},[]
                    {
                        continue;
                    }

                    if (schema.Properties.ContainsKey(property.Name))
                    {
                        propertySchema = schema.Properties[property.Name];
                    }
                    propertySchema.Sort = 1;
                    propertySchema.Path = property.Value.Path;
                    propertySchema.PathId = schema.PathId;
                    propertySchema.PathTypeName = schema.PathTypeName;
                    propertySchema.ForeginKeyId = GetForeginKey(schema.PathId);
                    propertySchema.ForeginKeyTypeName = GetForeginKeyTypeName(schema.PathTypeName);
                }

                Generate(property.Value, propertySchema, rootSchema, typeNameHint, isSimulate);

                if (schema.Properties.ContainsKey(property.Name))
                {
                    schema.Properties[property.Name] = propertySchema;
                }

                if (!schema.IsPathIdReady)
                {
                    ProcessKeyPath(schema);
                }
            }

            ProcessMappingProperty(schema);
        }

        private void GenerateArray(JToken token, JsonSchema schema, JsonSchema rootSchema, string typeNameHint, bool isSimulate = false)
        {
            if (schema.Item != null)
            {
                schema.Type = JsonObjectType.Array;
                int sort = 0;

                var tokenList = ((JArray)token).ToList();

                if (isSimulate)
                {
                    tokenList = tokenList.Take(20).ToList();
                }

                bool isSingleField = true;


                tokenList.ForEach(item =>
                {
                    JsonSchema itemSchema = FindSchema(schema.Item);

                    if (itemSchema != null)
                    {
                        itemSchema.Sort = sort;
                        itemSchema.Path = item.Path;//use json path
                        itemSchema.PathId = schema.PathId;
                        itemSchema.PathTypeName = schema.PathTypeName;
                        itemSchema.ForeginKeyId = GetForeginKey(schema.PathId);
                        itemSchema.ForeginKeyTypeName = GetForeginKeyTypeName(schema.PathTypeName);
                        if (item.Type == JTokenType.Array)
                        {
                            isSingleField = false;

                            JsonSchemaProperty arrayItemSchema = itemSchema.Properties.FirstOrDefault().Value;

                            GenerateWithoutReference(item, arrayItemSchema, rootSchema, typeNameHint, isSimulate);

                            ProcessKeyPath(itemSchema);
                        }
                        else if (item.Type == JTokenType.Object)
                        {
                            isSingleField = false;
                            GenerateWithoutReference(item, itemSchema, rootSchema, typeNameHint, isSimulate);
                        }
                    }
                    sort++;

                });

                if (isSingleField)
                {
                    schema.Type = JsonObjectType.String;
                    schema.Value = string.Join(", ", tokenList.Where(o => o != null && o.ToString().Length > 0).Select(o => o.ToString()));

                }
            }
            else
            {
                if (schema.Type == JsonObjectType.String)
                {
                    schema.Value = token.ToString();
                }
                //else
                //{
                //    ParseToken(token);
                //}
                //return;
            }
        }

        private void ProcessMappingProperty(JsonSchema schema)
        {
            List<string> mappingKeys = schema.Properties.Keys.Where(key => key.StartsWith(_settings.MappingPrefix)).ToList();

            foreach (string oneMappingKey in mappingKeys)
            {
                JsonSchemaProperty property = schema.Properties[oneMappingKey];

                string propertyKey = oneMappingKey.Replace(_settings.MappingPrefix, "").Replace(_settings.MappingSplit, ".");

                if (!string.IsNullOrWhiteSpace(schema.Path))
                {
                    propertyKey = schema.Path + "." + propertyKey;
                }

                if (_dictNodes.ContainsKey(propertyKey))
                {
                    property.Value = _dictNodes[propertyKey];
                }
            }
        }
        private void ProcessKeyPath(JsonSchema schema)
        {
            if (schema.Properties.Keys.Any(key => key.StartsWith(_settings.MappingKeyPrefix)))
            {
                var mappingKeyName = schema.Properties.Keys.FirstOrDefault(key => key.StartsWith(_settings.MappingKeyPrefix));

                var realKeyName = mappingKeyName.Replace(_settings.MappingKeyPrefix, "");

                var keySchema = schema.Properties[realKeyName];

                if (keySchema.Value != null && !string.IsNullOrWhiteSpace(keySchema.Value.ToString()))
                {
                    schema.Properties[mappingKeyName].Value = keySchema.Value; // set key value

                    if (string.IsNullOrWhiteSpace(schema.PathId))
                    {
                        schema.PathId = keySchema.Value.ToString();
                        schema.PathTypeName = schema.Title;
                    }
                    else
                    {
                        schema.PathId += "." + keySchema.Value;
                        schema.PathTypeName += "." + schema.Title;
                    }

                    schema.IsPathIdReady = true;
                }
            }
            else
            {
                schema.IsPathIdReady = true;
            }
            // set path, pathId, foreignKey
            if (schema.Properties.ContainsKey(_settings.MappingPath))
            {
                schema.Properties[_settings.MappingPath].Value = schema.Path;
            }
            if (schema.Properties.ContainsKey(_settings.MappingSort))
            {
                schema.Properties[_settings.MappingSort].Value = schema.Sort;
            }
            if (schema.Properties.ContainsKey(_settings.MappingForeginKeyId))
            {
                schema.Properties[_settings.MappingForeginKeyId].Value = schema.ForeginKeyId;
            }
        }
        private JsonSchema FindSchema(JsonSchema schema)
        {
            JsonSchema foundSchema = schema.Reference;

            if (foundSchema != null)
            {
                var properties = foundSchema.Properties;

                var typeName = _resolver.Types[foundSchema];


                var model = new TSqlTableModel(typeName, _settings, _resolver, foundSchema, _runtimeSchema);

                foundSchema = new JsonSchema() { Type = JsonObjectType.Object, Title = typeName };

                properties.All(property =>
                {
                    foundSchema.Properties.Add(property.Key, new JsonSchemaProperty()
                    {
                        Value = property.Value.Value,
                        Type = property.Value.Type,
                        Reference = property.Value.Reference,// object reference schema
                        Item = property.Value.Item, // arrray reference schema
                    }); return true;
                });

                _convertResults.Add(new TableNameParameterDTO() { TableName = typeName, JsonData = foundSchema, TableSchema = model });
            }
            return foundSchema;
        }
        private void ParseToken(JToken token)
        {
            foreach (var property in ((JObject)token).Properties())
            {
                if (property.Value.Type == JTokenType.Object)
                {
                    ParseToken(property.Value);
                }
                else
                {
                    _dictNodes.Add(property.Value.Path, property.Value?.ToString());
                }
            }
        }
        private string GetForeginKey(string pathId)
        {
            if (string.IsNullOrWhiteSpace(pathId))
            {
                return "";
            }
            else
            {
                return pathId; // pathId.Substring(pathId.LastIndexOf(".")+1);
            }
        }

        private string GetForeginKeyTypeName(string pathIdTypeName)
        {
            if (string.IsNullOrWhiteSpace(pathIdTypeName))
            {
                return "";
            }
            else
            {
                return pathIdTypeName.Substring(pathIdTypeName.LastIndexOf(".") + 1);
            }
        }

        private void AddRoot(JsonSchema rootSchema, string typeName)
        {
            var tableTemplateModel = new TSqlTableModel(typeName, _settings, _resolver, rootSchema, _runtimeSchema);

            _convertResults.Add(new TableNameParameterDTO() { TableName = typeName, JsonData = rootSchema, TableSchema = tableTemplateModel });
        }

        private IDictionary<string, object> GetOneLevelDict(IDictionary<string, object> dictRootData, string[] path, int level)
        {
            if (!dictRootData.ContainsKey(path[level]))
            {
                dictRootData[path[level]] = new ExpandoObject();
            }

            var dictDatalevel = ((IDictionary<String, Object>)dictRootData[path[level]]);

            level++;

            if (level < path.Length - 1)
            {
                dictDatalevel = GetOneLevelDict(dictDatalevel, path, level);
            }

            return dictDatalevel;
        }

        #endregion
    }
}
