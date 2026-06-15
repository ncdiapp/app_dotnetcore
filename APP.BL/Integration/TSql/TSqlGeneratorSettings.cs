using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace ExchangeBL
{
    public class TSqlGeneratorSettings : CodeGeneratorSettingsBase
    {
        public TSqlGeneratorSettings()
        {
            AnyType = "nvarchar(4000)";
            LongStringType = "nvarchar(max)";
            Namespace = "MyNamespace";

            DateType = "date";
            DateTimeType = "datetime";
            TimeType = "time";
            TimeSpanType = "time";

            ByteType = "byte";
            DecimalType = "decimal(18,4)";
            DoubleType = "float";
            FloatType = "float";
            GuidType = "uniqueidentifier";
            LongType = "bigint";

            ArrayType = "nvarchar(4000)";
            ArrayInstanceType = "System.Collections.ObjectModel.Collection";
            ArrayBaseType = "System.Collections.ObjectModel.Collection";

            DictionaryType = "System.Collections.Generic.IDictionary";
            DictionaryInstanceType = "System.Collections.Generic.Dictionary";
            DictionaryBaseType = "System.Collections.Generic.Dictionary";

            RequiredPropertiesMustBeDefined = true;
            GenerateDataAnnotations = true;
            GenerateOptionalPropertiesAsNullable = true;

            ValueGenerator = new TSqlValueGenerator(this);
            PropertyNameGenerator = new TSqlPropertyNameGenerator();
            TemplateFactory = new DefaultTemplateFactory(this, new Assembly[]
            {
                typeof(TSqlGeneratorSettings).GetTypeInfo().Assembly
            });

            MappingPrefix = "Mapping___";
            MappingKeyPrefix = "Mapping___Key___";
            MappingUniquePrefix = "Mapping___Unique___";
            MappingSplit = "___";
            MappingPath = "Path";
            MappingSort = "Sort";
            MappingForeginKeyId = "ForeginKeyId";
            MappingInternalKeyId = "InternalKeyId";
            MappingInternalForeginKeyId = "InternalForeginKeyId";
            MappingExternalKeyId = "ExternalKeyId";
            MappingExternalForeginKeyId = "ExternalForeginKeyId";
            MappingTablePrefix = "Shopify___";
            MappingSingleField = "single_field";
            ExcludedTypeNames = new string[] { "Anonymous" };
            TypeNameGenerator = new TSqlTypeNameGenerator();
        }

        public TSqlGeneratorSettings(string mappingTablePrefix) : this()
        {
            MappingTablePrefix = mappingTablePrefix + MappingSplit;
        }

        public string Namespace { get; set; }
        public string AnyType { get; set; }
        public string LongStringType { get; set; }
        public string DateType { get; set; }
        public string DateTimeType { get; set; }
        public string TimeType { get; set; }
        public string TimeSpanType { get; set; }
        public string ArrayType { get; set; }
        public string ArrayInstanceType { get; set; }
        public string ArrayBaseType { get; set; }
        public string DictionaryType { get; set; }
        public string DictionaryInstanceType { get; set; }
        public string DictionaryBaseType { get; set; }
        public bool GenerateOptionalPropertiesAsNullable { get; set; }
        public bool RequiredPropertiesMustBeDefined { get; set; }
        public bool GenerateDataAnnotations { get; set; }
        public bool GenerateNullableReferenceTypes { get; set; }
        public bool GenerateImmutableArrayProperties { get; set; }
        public bool GenerateImmutableDictionaryProperties { get; set; }
        public bool InlineNamedArrays { get; set; }
        public bool InlineNamedDictionaries { get; set; }
        public bool InlineNamedTuples { get; set; }

        public string ByteType { get; set; }
        public string DecimalType { get; set; }
        public string DoubleType { get; set; }
        public string FloatType { get; set; }
        public string GuidType { get; set; }
        public string LongType { get; set; }
        public string MappingKeyPrefix { get; set; }
        public string MappingUniquePrefix { get; set; }
        public string MappingPrefix { get; set; }
        public string MappingSplit { get; set; }
        public string MappingPath { get; set; }
        public string MappingSort { get; set; }
        public string MappingForeginKeyId { get; set; }
        public string MappingInternalKeyId { get; set; }
        public string MappingInternalForeginKeyId { get; set; }
        public string MappingExternalKeyId { get; set; }
        public string MappingExternalForeginKeyId { get; set; }
        public string MappingTablePrefix { get; set; }
        public string MappingSingleField { get; set; }

        public const string UserDefineKeyAnonymous = "anonymous_object_type_to_table_name";
        public const string UserDefineKeyTitle = "root_object_type_to_table_name";
        public const string UserDefineKeyRootName = "root_type_name";
        public const string UserDefineKeyOptions = "object_type_map_to_table_option";
        public const string UserDefineKeyRollupParents = "rollup_child_to_parent_for_one_table_parent_node_list";
        public const string MappingSingleFieldItemName = "item";

        public string[] GetUniqeFromUserDefinesOption(JsonSchema schema, string typeName)
        {
            JsonSchema userDefines = schema.UserDefines;
            if (userDefines != null && userDefines.ExtensionData != null)
            {
                userDefines.ExtensionData.TryGetValue(UserDefineKeyOptions, out object userDefineOptions);
                if (userDefineOptions != null)
                {
                    string strTypeSetup = Newtonsoft.Json.JsonConvert.SerializeObject(userDefineOptions);
                    List<ObjectTypeMappingDTO> lstMapping = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ObjectTypeMappingDTO>>(strTypeSetup);
                    ObjectTypeMappingDTO mapping = lstMapping.FirstOrDefault(m => m.object_type_definition_name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
                    if (mapping != null && mapping.unique != null)
                        return mapping.unique;
                    if (mapping == null && typeName == MappingSingleField)
                        return new string[] { MappingSingleFieldItemName, MappingPath };
                }
            }
            return new string[] { };
        }

        public IDictionary<string, string> GetSingleFieldPathFromUserDefinesOption(JsonSchema schema)
        {
            JsonSchema userDefines = schema.UserDefines;
            if (userDefines != null && userDefines.ExtensionData != null)
            {
                userDefines.ExtensionData.TryGetValue(UserDefineKeyOptions, out object userDefineOptions);
                if (userDefineOptions != null)
                {
                    string strTypeSetup = Newtonsoft.Json.JsonConvert.SerializeObject(userDefineOptions);
                    List<ObjectTypeMappingDTO> lstMapping = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ObjectTypeMappingDTO>>(strTypeSetup);
                    lstMapping.RemoveAll(mapping => !mapping.is_single_field);
                    return lstMapping.ToDictionary(mapping => mapping.object_type_definition_name.Replace(MappingSplit, "."), mapping => mapping.mapping_single_field_type_db_path);
                }
            }
            return new Dictionary<string, string>();
        }

        public void ApplyUserDefines(JsonSchema schema, List<string> typeNames_NotToCreateStagingTable = null)
        {
            JsonSchema userDefines = schema.UserDefines;
            if (userDefines != null && userDefines.ExtensionData != null)
            {
                string rootName = string.Empty;
                foreach (var oneExensionData in userDefines.ExtensionData)
                {
                    switch (oneExensionData.Key)
                    {
                        case UserDefineKeyTitle:
                            schema.Title = oneExensionData.Value as string;
                            break;
                        case UserDefineKeyAnonymous:
                            ChangeAnonymousName(schema, oneExensionData.Value as string);
                            break;
                        case UserDefineKeyRootName:
                            rootName = oneExensionData.Value as string;
                            break;
                        case UserDefineKeyRollupParents:
                            break;
                        case UserDefineKeyOptions:
                            ProcessObjectTypeMapping(schema, oneExensionData.Value, rootName);
                            break;
                        default:
                            throw new Exception("no process for user defines:" + oneExensionData.Key);
                    }
                }
                if (!string.IsNullOrWhiteSpace(rootName))
                    AddForeignKey(schema, rootName);
            }
        }

        public void CreateUserDefinesTemplate(JsonSchema schema)
        {
            if (schema.UserDefines == null)
            {
                JsonSchema userDefines = new JsonSchema() { ExtensionData = new Dictionary<string, object>() };
                List<ObjectTypeMappingDTO> lstMapping = new List<ObjectTypeMappingDTO>();

                if (schema.Definitions.Keys.Contains("Anonymous"))
                {
                    userDefines.ExtensionData.Add(UserDefineKeyAnonymous, "");
                }
                else if (schema.Type == JsonObjectType.Object && schema.Properties.Any(p => p.Value.Type != JsonObjectType.None && p.Value.Type != JsonObjectType.Array))
                {
                    userDefines.ExtensionData.Add(UserDefineKeyTitle, "");
                    lstMapping.Add(new ObjectTypeMappingDTO());
                }

                userDefines.ExtensionData.Add(UserDefineKeyRootName, "");

                schema.Definitions.All(d =>
                {
                    ObjectTypeMappingDTO objectTypeMappingDTO = new ObjectTypeMappingDTO() { object_type_definition_name = d.Key, mapping_db_table_name = d.Key };
                    objectTypeMappingDTO.is_single_field = d.Value.Properties.Count == 0;
                    lstMapping.Add(objectTypeMappingDTO);
                    return true;
                });

                userDefines.ExtensionData.Add(UserDefineKeyOptions, lstMapping);
                schema.UserDefines = userDefines;
            }
        }

        private void ChangeAnonymousName(JsonSchema schema, string newName)
        {
            if (schema.Definitions.Keys.Contains("Anonymous") && !string.IsNullOrWhiteSpace(newName))
            {
                JsonSchema anonymousSchema = schema.Definitions["Anonymous"];
                schema.Definitions.Remove("Anonymous");
                schema.Definitions.Add(newName, anonymousSchema);
            }
        }

        private void ProcessObjectTypeMapping(JsonSchema schema, object value, string rootName)
        {
            string strTypeSetup = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            List<ObjectTypeMappingDTO> lstMapping = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ObjectTypeMappingDTO>>(strTypeSetup);

            bool hasSingleFieldType = false;

            foreach (ObjectTypeMappingDTO oneMapping in lstMapping)
            {
                schema.Definitions.TryGetValue(oneMapping.object_type_definition_name, out JsonSchema definitionSchema);
                if (definitionSchema == null)
                {
                    if (!string.IsNullOrWhiteSpace(schema.Title) && schema.Title.Equals(oneMapping.object_type_definition_name, StringComparison.InvariantCultureIgnoreCase))
                        definitionSchema = schema;
                    if (definitionSchema == null)
                        continue;
                }

                if (!string.IsNullOrWhiteSpace(oneMapping.mapping_db_table_name))
                    definitionSchema.TableName = oneMapping.mapping_db_table_name;

                if (!string.IsNullOrWhiteSpace(oneMapping.key))
                {
                    JsonSchemaProperty jsonSchemaProperty = new JsonSchemaProperty() { Type = JsonObjectType.String };
                    definitionSchema.Properties.Add(MappingKeyPrefix + oneMapping.key, jsonSchemaProperty);
                    definitionSchema.Properties.Add(MappingExternalKeyId, new JsonSchemaProperty() { Type = JsonObjectType.String });
                }

                if (oneMapping.unique != null && oneMapping.unique.Length > 0)
                {
                    string unique = string.Join(MappingSplit, oneMapping.unique);
                    if (!string.IsNullOrWhiteSpace(unique))
                        definitionSchema.Properties.Add(MappingUniquePrefix + unique, new JsonSchemaProperty() { Type = JsonObjectType.String });
                }

                if (oneMapping.serialization != null && oneMapping.serialization.Length > 0)
                {
                    oneMapping.serialization = oneMapping.serialization.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    List<KeyValuePair<string, JsonSchemaProperty>> properties = definitionSchema.Properties.Where(p => oneMapping.serialization.Contains(p.Key)).ToList();
                    List<string> removeKeys = schema.Definitions.Where(d => properties.Any(p => (p.Value.Reference != null && p.Value.Reference == d.Value) || (p.Value.Item != null && p.Value.Item.Reference != null && p.Value.Item.Reference == d.Value)))
                                                                .Select(d => d.Key).ToList();
                    removeKeys.All(key => { DeleteChildFromDefinitions(key, schema); return true; });
                    properties.All(p =>
                    {
                        p.Value.Reference = null;
                        p.Value.Item = null;
                        p.Value.Type = JsonObjectType.String;
                        p.Value.Format = TSqlJsonFormatStrings.Max.ToString();
                        return true;
                    });
                }

                if (oneMapping.rollup_path != null && oneMapping.rollup_path.Length > 0)
                {
                    oneMapping.rollup_path = oneMapping.rollup_path.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    if (oneMapping.rollup_path.Length > 0)
                    {
                        JsonSchema parentSchema = definitionSchema;
                        List<string> objectKeys = parentSchema.Properties.Where(p => p.Value.Type == JsonObjectType.None && p.Value.Reference != null).Select(p => p.Key).ToList();
                        List<string> childArrayTypeNameList = new List<string>();

                        foreach (string oneObjectKey in objectKeys)
                        {
                            RollupChild(oneMapping.object_type_definition_name, oneObjectKey, parentSchema.Properties[oneObjectKey], schema, out Dictionary<string, JsonSchemaProperty> newProperties, childArrayTypeNameList, oneMapping.rollup_path.ToList());
                            if (newProperties.Count > 0)
                            {
                                foreach (var oneNewProperty in newProperties)
                                    parentSchema.Properties.Add(MappingPrefix + oneNewProperty.Key, oneNewProperty.Value);
                            }
                            parentSchema.Properties.Remove(oneObjectKey);
                        }

                        List<string> needToRemoveChildDefinitionKeys = new List<string>();
                        foreach (var kvPair in schema.Definitions)
                        {
                            string key = kvPair.Key;
                            if (key.StartsWith(oneMapping.object_type_definition_name + MappingSplit, StringComparison.InvariantCultureIgnoreCase))
                            {
                                bool isChildDefinitionNeedToCreateTable = lstMapping.Select(o => o.object_type_definition_name.ToLower()).Contains(key.ToLower());
                                if (!isChildDefinitionNeedToCreateTable)
                                    needToRemoveChildDefinitionKeys.Add(key);
                            }
                        }

                        foreach (string definitionKey in needToRemoveChildDefinitionKeys)
                        {
                            if (!childArrayTypeNameList.Contains(definitionKey))
                                schema.Definitions.Remove(definitionKey);
                        }
                    }
                }

                if (oneMapping.is_single_field)
                {
                    JsonSchema singleFieldSchema = null;
                    if (hasSingleFieldType)
                    {
                        singleFieldSchema = schema.Definitions[MappingSingleField];
                    }
                    else
                    {
                        singleFieldSchema = new JsonSchema() { Type = JsonObjectType.Object };
                        JsonSchemaProperty itemProperty = new JsonSchemaProperty() { Type = JsonObjectType.String };
                        JsonSchemaProperty pathProperty = new JsonSchemaProperty() { Type = JsonObjectType.String };
                        singleFieldSchema.Properties.Add(MappingSingleFieldItemName, itemProperty);
                        singleFieldSchema.Properties.Add(MappingPath, pathProperty);
                        schema.Definitions.Add(MappingSingleField, singleFieldSchema);
                        hasSingleFieldType = true;
                    }

                    var referencedProperties = schema.Definitions.SelectMany(t => t.Value.Properties).Where(p => p.Value.Type == JsonObjectType.Array && p.Value.Item.Reference == definitionSchema).ToList();
                    referencedProperties.All(p => { p.Value.Item.Reference = singleFieldSchema; return true; });
                    schema.Definitions.Remove(oneMapping.object_type_definition_name);
                }
            }
        }

        private void DeleteChildFromDefinitions(string key, JsonSchema schema)
        {
            JsonSchema deleteSchema = schema.Definitions[key];
            List<KeyValuePair<string, JsonSchemaProperty>> properties = deleteSchema.Properties.Where(p => p.Value.Type == JsonObjectType.Array || p.Value.Type == JsonObjectType.None).ToList();
            List<string> removeKeys = schema.Definitions.Where(d => properties.Any(p => (p.Value.Reference != null && p.Value.Reference == d.Value) || (p.Value.Item != null && p.Value.Item.Reference != null && p.Value.Item.Reference == d.Value)))
                                                        .Select(d => d.Key).ToList();
            removeKeys.All(k => { DeleteChildFromDefinitions(k, schema); return true; });
            schema.Definitions.Remove(key);
        }

        private void RollupChild(string parentTypeName, string typeNameHint, JsonSchemaProperty parentProperty, JsonSchema rootSchema, out Dictionary<string, JsonSchemaProperty> newProperties, List<string> childArrayTypeNameList, List<string> typeNames_NeedToRollUp)
        {
            string childTypeName = parentTypeName + MappingSplit + parentProperty.Name;
            newProperties = new Dictionary<string, JsonSchemaProperty>();
            JsonSchema childSchema = parentProperty.Reference;

            if (childSchema != null && childSchema.Properties.Any(p => p.Value.Type == JsonObjectType.Array))
            {
                childArrayTypeNameList.Add(childTypeName);
            }
            else
            {
                if (childSchema != null)
                {
                    foreach (var oneKV in childSchema.Properties)
                    {
                        string newKey = typeNameHint + MappingSplit + oneKV.Key;
                        JsonSchemaProperty newProperty = oneKV.Value;
                        if (newProperty.Type != JsonObjectType.None)
                        {
                            if (typeNames_NeedToRollUp != null && typeNames_NeedToRollUp.Contains(childTypeName))
                                newProperties.Add(newKey, newProperty);
                        }
                        else
                        {
                            RollupChild(childTypeName, newKey, newProperty, rootSchema, out Dictionary<string, JsonSchemaProperty> childProperties, childArrayTypeNameList, typeNames_NeedToRollUp);
                            foreach (var oneChildProperty in childProperties)
                                newProperties.Add(oneChildProperty.Key, oneChildProperty.Value);
                        }
                    }
                }
            }
        }

        private void AddForeignKey(JsonSchema schema, string rootName)
        {
            foreach (var oneDefinition in schema.Definitions.Where(d => d.Key != rootName))
            {
                JsonSchemaProperty jsonSchemaProperty = new JsonSchemaProperty() { Type = JsonObjectType.String };
                oneDefinition.Value.Properties.Add(MappingForeginKeyId, jsonSchemaProperty);
                oneDefinition.Value.Properties.Add(MappingInternalForeginKeyId, new JsonSchemaProperty() { Type = JsonObjectType.String });
                oneDefinition.Value.Properties.Add(MappingExternalForeginKeyId, new JsonSchemaProperty() { Type = JsonObjectType.String });

                var referencedProperties = schema.Definitions.SelectMany(t => t.Value.Properties).Where(p => p.Value.Type == JsonObjectType.Array && p.Value.Item.Reference == oneDefinition.Value).ToList();
                if (referencedProperties.Count > 0)
                    oneDefinition.Value.Properties.Add(MappingSort, new JsonSchemaProperty() { Type = JsonObjectType.String });
            }
        }

        private class ObjectTypeMappingDTO
        {
            public string object_type_definition_name { get; set; }
            public string mapping_db_table_name { get; set; }
            public string key { get; set; }
            public string[] unique { get; set; }
            public string[] serialization { get; set; }
            public string[] rollup_path { get; set; }
            public bool is_single_field { get; set; }
            public string mapping_single_field_type_db_path { get; set; }

            public ObjectTypeMappingDTO()
            {
                object_type_definition_name = string.Empty;
                mapping_db_table_name = string.Empty;
                key = string.Empty;
                unique = new string[] { };
                serialization = new string[] { };
                rollup_path = new string[] { };
                is_single_field = false;
                mapping_single_field_type_db_path = string.Empty;
            }
        }
    }
}
