using System;
using System.Collections.Generic;
using System.Linq;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.Models;

namespace ExchangeBL
{
    /// <summary>The TSql table, CRUD scripts generator.</summary>
    public class TSqlGenerator : GeneratorBase
    {
        private readonly TSqlTypeResolver _resolver;
        private readonly string suffixTable = "Table";

        /// <summary>Initializes a new instance of the <see cref="TSqlGenerator"/> class.</summary>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        public TSqlGenerator(object rootObject)
            : this(rootObject, new TSqlGeneratorSettings())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TSqlGenerator"/> class.</summary>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        /// <param name="settings">The generator settings.</param>
        public TSqlGenerator(object rootObject, TSqlGeneratorSettings settings)
            : this(rootObject, settings, new TSqlTypeResolver(settings))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TSqlGenerator"/> class.</summary>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        /// <param name="settings">The generator settings.</param>
        /// <param name="resolver">The resolver.</param>
        public TSqlGenerator(object rootObject, TSqlGeneratorSettings settings, TSqlTypeResolver resolver)
            : base(rootObject, resolver, settings)
        {
            _resolver = resolver;
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public TSqlGeneratorSettings Settings { get; }

        /// <inheritdoc />
        public override IEnumerable<CodeArtifact> GenerateTypes()
        {
            var baseArtifacts = base.GenerateTypes();
            var artifacts = new List<CodeArtifact>();

            if (baseArtifacts.Any(r => r.Code.Contains("JsonInheritanceConverter")))
            {
                if (Settings.ExcludedTypeNames?.Contains("JsonInheritanceAttribute") != true)
                {
                    var template = Settings.TemplateFactory.CreateTemplate("CSharp", "JsonInheritanceAttribute", new JsonInheritanceConverterTemplateModel(Settings));
                    artifacts.Add(new CodeArtifact("JsonInheritanceAttribute", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Utility, template));
                }

                if (Settings.ExcludedTypeNames?.Contains("JsonInheritanceConverter") != true)
                {
                    var template = Settings.TemplateFactory.CreateTemplate("CSharp", "JsonInheritanceConverter", new JsonInheritanceConverterTemplateModel(Settings));
                    artifacts.Add(new CodeArtifact("JsonInheritanceConverter", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Utility, template));
                }
            }

            if (baseArtifacts.Any(r => r.Code.Contains("DateFormatConverter")))
            {
                if (Settings.ExcludedTypeNames?.Contains("DateFormatConverter") != true)
                {
                    var template = Settings.TemplateFactory.CreateTemplate("CSharp", "DateFormatConverter", new DateFormatConverterTemplateModel(Settings));
                    artifacts.Add(new CodeArtifact("DateFormatConverter", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Utility, template));
                }
            }

            return baseArtifacts.Concat(artifacts);
        }

        /// <inheritdoc />
        protected override string GenerateFile(IEnumerable<CodeArtifact> artifactCollection)
        {
            var model = new FileTemplateModel
            {
                Namespace = Settings.Namespace ?? string.Empty,
                GenerateNullableReferenceTypes = Settings.GenerateNullableReferenceTypes,
                TypesCode = artifactCollection.Concatenate()
            };

            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "TSqlFile", model);
            return ConversionUtilities.TrimWhiteSpaces(template.Render());
        }

        /// <summary>Generates the type.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The code.</returns>
        protected override CodeArtifact GenerateType(JsonSchema schema, string typeNameHint)
        {
            var typeName = _resolver.GetOrGenerateTypeName(schema, typeNameHint);

            if (schema.IsEnumeration)
            {
                return GenerateEnum(schema, typeName);
            }
            else
            {
                return GenerateTable(schema, typeName);
            }
        }

        /// <summary>
        /// generate insert/update or select script
        /// </summary>
        /// <param name="scriptType">Insert or Select</param>
        /// <returns></returns>
        public Dictionary<string, string> GenerateScripts(EnumSqlScriptCRUDType scriptType)
        {
            var scripts = new Dictionary<string, string>();

            var schema = (JsonSchema)RootObject;

            _resolver.Resolve(schema, false, schema.Title); // register root type

            foreach (var onePair in _resolver.Types)
            {
                var typeName = _resolver.GetOrGenerateTypeName(onePair.Key, onePair.Value);

                var result = GenerateScript(onePair.Key, typeName, scriptType);

                if (!Settings.ExcludedTypeNames.Contains(result.TypeName))
                {
                    scripts.Add(result.TypeName, result.Code);
                }
            }
            return scripts;
        }

        private CodeArtifact GenerateTable(JsonSchema schema, string typeName)
        {
            var model = new TSqlTableModel(typeName, Settings, _resolver, schema, RootObject);

            var rootSchema = (JsonSchema)RootObject;

            if (!string.IsNullOrWhiteSpace(schema.TableName) && rootSchema.DictExistTableNameAndColumnNameList != null)
            {
                string tableName = Settings.MappingTablePrefix + schema.TableName;

                if (rootSchema.DictExistTableNameAndColumnNameList.ContainsKey(tableName))
                {
                    model.IsTableExist = true;

                    var existColumnNameList = rootSchema.DictExistTableNameAndColumnNameList[tableName];
                    foreach (var property in model.Properties)
                    {
                        if (property.IsSqlColumn)
                        {
                            bool exists = existColumnNameList.Any(item => string.Equals(item, property.ColumnName, StringComparison.OrdinalIgnoreCase));

                            if (exists)
                            {
                                property.IsColumnExist = true;
                            }
                        }
                    }
                }
            }

            //RenamePropertyWithSameNameAsClass(typeName, model.Properties);
            string tableCreationTemplateName = GetTableCreationTemplateNameByDbServerType();

            var template = Settings.TemplateFactory.CreateTemplate("CSharp", tableCreationTemplateName, model);

            return new CodeArtifact(typeName, model.BaseClassName, CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Contract, template);
        }


        private CodeArtifact GenerateScript(JsonSchema schema, string typeName, EnumSqlScriptCRUDType scriptType)
        {
            var model = new TSqlTableModel(typeName, Settings, _resolver, schema, RootObject);

            //RenamePropertyWithSameNameAsClass(typeName, model.Properties);

            string scriptTypeName = GetScriptTypeNameByDbServerType(scriptType);

            var template = Settings.TemplateFactory.CreateTemplate("CSharp", scriptTypeName, model); // insert or select
            return new CodeArtifact(typeName, model.BaseClassName, CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Contract, template);
        }

        private static void RenamePropertyWithSameNameAsClass(string typeName, IEnumerable<TSqlPropertyModel> properties)
        {
            var propertyModels = properties as TSqlPropertyModel[] ?? properties.ToArray();
            TSqlPropertyModel propertyWithSameNameAsClass = null;
            foreach (var p in propertyModels)
            {
                if (p.PropertyName == typeName)
                {
                    propertyWithSameNameAsClass = p;
                    break;
                }
            }

            if (propertyWithSameNameAsClass != null)
            {
                var number = 1;
                var candidate = typeName + number;
                while (propertyModels.Any(p => p.PropertyName == candidate))
                {
                    number++;
                }

                propertyWithSameNameAsClass.PropertyName = propertyWithSameNameAsClass.PropertyName + number;
            }
        }

        private CodeArtifact GenerateEnum(JsonSchema schema, string typeName)
        {
            var model = new EnumTemplateModel(typeName, schema, Settings);
            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "Enum", model);
            return new CodeArtifact(typeName, CodeArtifactType.Enum, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Contract, template);
        }



        private string GetTableCreationTemplateNameByDbServerType()
        {
            var rootSchema = (JsonSchema)RootObject;

            string tableCreationTemplateName = "";

            string prefix = EnumJsonSqlServerType.SqlServer.ToString(); ;

            if (!string.IsNullOrWhiteSpace(rootSchema.JsonSqlServerTypeName))
            {
                prefix = rootSchema.JsonSqlServerTypeName;
            }

            tableCreationTemplateName = prefix + suffixTable;


            return tableCreationTemplateName;
        }

        private string GetScriptTypeNameByDbServerType(EnumSqlScriptCRUDType scriptType)
        {
            var rootSchema = (JsonSchema)RootObject;


            string prefix = EnumJsonSqlServerType.SqlServer.ToString(); ;

            if (!string.IsNullOrWhiteSpace(rootSchema.JsonSqlServerTypeName))
            {
                prefix = rootSchema.JsonSqlServerTypeName;
            }


            string scriptTypeName = prefix + scriptType.ToString();
            return scriptTypeName;
        }

    }
}
