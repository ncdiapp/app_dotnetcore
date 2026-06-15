using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Collections;
using NJsonSchema.Generation;
using NJsonSchema.Infrastructure;
using NJsonSchema.Validation;
using NJsonSchema.Validation.FormatValidators;

namespace NJsonSchema
{
    public partial class JsonSchema
    {
        /// <summary>
        /// value of that property
        /// </summary>
        [JsonIgnore]
        public object Value { get; set; }
        /// <summary>
        ///  path from root
        /// </summary>
        [JsonIgnore]
        public string Path { get; set; }
        /// <summary>
        /// Path's key
        /// </summary>
        [JsonIgnore]
        public string PathId { get; set; }

        /// <summary>
        /// PathTypeName
        /// </summary>
        [JsonIgnore]
        public string PathTypeName { get; set; }
        /// <summary>
        /// parent Id
        /// </summary>
        [JsonIgnore]
        public string ForeginKeyId { get; set; }
        /// <summary>
        /// foregin key's type name
        /// </summary>
        [JsonIgnore]
        public string ForeginKeyTypeName { get; set; }
        /// <summary>
        /// if PathId has all values
        /// </summary>
        [JsonIgnore]
        public bool IsPathIdReady { get; set; }
        /// <summary>
        /// index of array
        /// </summary>
        [JsonIgnore]
        public int Sort { get; set; }
        /// <summary>
        /// Path Internal key
        /// </summary>
        [JsonProperty("tablename", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string TableName { get; set; }
        /// <summary>
        /// user defines 
        /// </summary>
        [JsonProperty("userdefines", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchema UserDefines { get; set; }

        /// <summary>
        /// JsonSqlServerTypeName 
        /// </summary>
        [JsonProperty("jsonsqlserveryypename", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string JsonSqlServerTypeName { get; set; }


        /// <summary>
        /// DictExistTableNameAndColumnNameList 
        /// </summary>
        [JsonProperty("dictexisttablenameandcolumnnamelist", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public Dictionary<string, List<string>> DictExistTableNameAndColumnNameList { get; set; }

        /// <summary>Serializes the <see cref="JsonSchema" /> to a JSON string.</summary>
        /// change default ToJson,  keep [] 
        public string ToUserDefineJson()
        {
            PropertyRenameAndIgnoreSerializerContractResolver resolver = new PropertyRenameAndIgnoreSerializerContractResolver();

            AddRenamePropertyBySchemaType(SerializationSchemaType, resolver);

            var oldSchema = SchemaVersion;
            SchemaVersion = "http://json-schema.org/draft-04/schema#";

            var json = JsonSchemaSerialization.ToJson(this, SerializationSchemaType, resolver, Formatting.Indented);

            SchemaVersion = oldSchema;

            return json;
        }

        private static void AddRenamePropertyBySchemaType(SchemaType schemaType, PropertyRenameAndIgnoreSerializerContractResolver resolver)
        {
            if (schemaType == SchemaType.OpenApi3)
            {
                resolver.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readOnly");
                resolver.RenameProperty(typeof(JsonSchemaProperty), "x-writeOnly", "writeOnly");

                resolver.RenameProperty(typeof(JsonSchema), "x-nullable", "nullable");
                resolver.RenameProperty(typeof(JsonSchema), "x-example", "example");
                resolver.RenameProperty(typeof(JsonSchema), "x-deprecated", "deprecated");
            }
            else if (schemaType == SchemaType.Swagger2)
            {
                resolver.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readOnly");
                resolver.RenameProperty(typeof(JsonSchema), "x-example", "example");
            }
            else
            {
                resolver.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readonly");
            }
        }
    }
}