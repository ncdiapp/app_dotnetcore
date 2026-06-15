using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.Models;

namespace ExchangeBL
{
    /// <summary>The TSql class template model.</summary>
    public class TSqlTableModel : ClassTemplateModelBase
    {
        private readonly TSqlTypeResolver _resolver;
        private readonly JsonSchema _schema;
        private readonly TSqlGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="TSqlTableModel"/> class.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        public TSqlTableModel(string typeName, TSqlGeneratorSettings settings,
            TSqlTypeResolver resolver, JsonSchema schema, object rootObject)
            : base(resolver, schema, rootObject)
        {
            _resolver = resolver;
            _schema = schema;
            _settings = settings;

            ClassName = typeName;
            Properties = _schema.ActualProperties.Values
                .Where(p => !p.IsInheritanceDiscriminator)
                .Select(property => new TSqlPropertyModel(this, property, _resolver, _settings))
                .ToArray();

            if (schema.InheritedSchema != null)
            {
                BaseClass = new TSqlTableModel(BaseClassName, settings, resolver, schema.InheritedSchema, rootObject);
                AllProperties = Properties.Concat(BaseClass.AllProperties).ToArray();
            }
            else
            {
                AllProperties = Properties;
            };

            TableName = string.IsNullOrWhiteSpace(_schema.TableName) ? "": _settings.MappingTablePrefix + _schema.TableName;

            List<string> lstWhere = new List<string>();

            List<string> propertyNames = Properties.Select(p => p.PropertyName).ToList();

            //foregin key
            if (propertyNames.Contains(_settings.MappingForeginKeyId))
            {
                lstWhere.Add($" {_settings.MappingForeginKeyId} = @{_settings.MappingForeginKeyId} ");
            }
            // id
            if (propertyNames.Any(p => p.StartsWith(_settings.MappingKeyPrefix)))
            {
                var realKeyName = propertyNames.FirstOrDefault(p => p.StartsWith(_settings.MappingKeyPrefix)).Replace(_settings.MappingKeyPrefix, "");

                lstWhere.Add($" [{realKeyName}] = @{realKeyName} ");
            }
            else if (propertyNames.Any(p => p.StartsWith(_settings.MappingUniquePrefix)))
            {
                var realPropertyNames = propertyNames.FirstOrDefault(p => p.StartsWith(_settings.MappingUniquePrefix))
                    .Replace(_settings.MappingUniquePrefix, "")
                    .Split(new string[] { _settings.MappingSplit }, StringSplitOptions.None);

                realPropertyNames.All(name => { lstWhere.Add($" [{name}] = @{name}"); return true; });
            }
            else
            {
                // path
                if (propertyNames.Contains(_settings.MappingPath))
                {
                    lstWhere.Add($" [{_settings.MappingPath}] = @{_settings.MappingPath} ");
                }
                // sort
                if (propertyNames.Contains(_settings.MappingSort))
                {
                    lstWhere.Add($" [{_settings.MappingSort}] = @{_settings.MappingSort} ");
                }
            }

            WhereClauseFormat = string.Join(" AND ", lstWhere);
        }

        /// <summary>Gets a value indicating whether to use System.Text.Json</summary>
        public bool UseSystemTextJson => false;


        /// <summary>Gets or sets the class name.</summary>
        public override string ClassName { get; }

        /// <summary>Gets the namespace.</summary>
        public string Namespace => _settings.Namespace;

        /// <summary>Gets a value indicating whether the C#8 nullable reference types are enabled for this file.</summary>
        public bool GenerateNullableReferenceTypes => _settings.GenerateNullableReferenceTypes;

        /// <summary>Gets a value indicating whether an additional properties type is available.</summary>
        public bool HasAdditionalPropertiesType =>
            HasAdditionalPropertiesTypeInBaseClass || // if the base class has them, inheritance dictates that this class will have them to
            !_schema.IsDictionary &&
            !_schema.ActualTypeSchema.IsDictionary &&
            !_schema.IsArray &&
            !_schema.ActualTypeSchema.IsArray &&
            (_schema.ActualTypeSchema.AllowAdditionalProperties ||
             _schema.ActualTypeSchema.AdditionalPropertiesSchema != null);

        /// <summary>Gets a value indicating whether an additional properties type is available in the base class.</summary>
        public bool HasAdditionalPropertiesTypeInBaseClass => BaseClass?.HasAdditionalPropertiesType ?? false;

        /// <summary> Gets a value indicating if the "Additional properties" property should be generated. </summary>
        public bool GenerateAdditionalPropertiesProperty => HasAdditionalPropertiesType && !HasAdditionalPropertiesTypeInBaseClass;

        /// <summary>Gets the type of the additional properties.</summary>
        public string AdditionalPropertiesType => HasAdditionalPropertiesType ? "object" : null; // TODO: Find a way to use typed dictionaries

        /// <summary>Gets the property models.</summary>
        public IEnumerable<TSqlPropertyModel> Properties { get; }

        /// <summary>Gets the property models with inherited properties.</summary>
        public IEnumerable<TSqlPropertyModel> AllProperties { get; }

        /// <summary>Gets a value indicating whether the class has description.</summary>
        public bool HasDescription => !(_schema is JsonSchemaProperty) &&
            (!string.IsNullOrEmpty(_schema.Description) ||
             !string.IsNullOrEmpty(_schema.ActualTypeSchema.Description));

        /// <summary>Gets the description.</summary>
        public string Description => !string.IsNullOrEmpty(_schema.Description) ?
            _schema.Description : _schema.ActualTypeSchema.Description;

        /// <summary>Gets a value indicating whether the class style is INPC.</summary>
        public bool RenderInpc => false;

        /// <summary>Gets a value indicating whether the class style is Prism.</summary>
        public bool RenderPrism => false;

        /// <summary>Gets a value indicating whether the class style is Record.</summary>
        public bool RenderRecord => false;

        /// <summary>Gets a value indicating whether to generate records as C# 9.0 records.</summary>
        public bool GenerateNativeRecords => false;

        /// <summary>Gets a value indicating whether to render ToJson() and FromJson() methods.</summary>
        public bool GenerateJsonMethods => false;

        /// <summary>Gets a value indicating whether the class has discriminator property.</summary>
        public bool HasDiscriminator => !string.IsNullOrEmpty(_schema.ActualDiscriminator);

        /// <summary>Gets the discriminator property name.</summary>
        public string Discriminator => _schema.ActualDiscriminator;

        /// <summary>Gets a value indicating whether this class represents a tuple.</summary>
        public bool IsTuple => _schema.ActualTypeSchema.IsTuple;

        /// <summary>Gets the tuple types.</summary>
        public string[] TupleTypes => _schema.ActualTypeSchema.Items
            .Select(i => _resolver.Resolve(i, i.IsNullable(_settings.SchemaType), string.Empty, false))
            .ToArray();

        /// <summary>Gets a value indicating whether the class has a parent class.</summary>
        public bool HasInheritance => _schema.InheritedTypeSchema != null;

        /// <summary>Gets the base class name.</summary>
        public string BaseClassName => HasInheritance ? _resolver.Resolve(_schema.InheritedTypeSchema, false, string.Empty, false)
                .Replace(_settings.ArrayType + "<", _settings.ArrayBaseType + "<")
                .Replace(_settings.DictionaryType + "<", _settings.DictionaryBaseType + "<") : null;

        /// <summary>Gets the base class model.</summary>
        public TSqlTableModel BaseClass { get; }

        /// <summary>Gets a value indicating whether the class inherits from exception.</summary>
        public bool InheritsExceptionSchema => _resolver.ExceptionSchema != null &&
                                               _schema?.InheritsSchema(_resolver.ExceptionSchema) == true;

        /// <summary>Gets a value indicating whether to use the DateFormatConverter.</summary>
        public bool UseDateFormatConverter => _settings.DateType.StartsWith("System.Date");

        /// <summary>Gets or sets the access modifier of generated classes and interfaces.</summary>
        public string TypeAccessModifier => string.Empty;

        /// <summary>Gets the access modifier of property setters (default: '').</summary>
        public string PropertySetterAccessModifier => !string.IsNullOrEmpty(string.Empty) ? "" + " " : "";

        /// <summary>Gets the JSON serializer parameter code.</summary>
        public string JsonSerializerParameterCode => "";

        /// <summary>Gets the JSON converters array code.</summary>
        public string JsonConvertersArrayCode => "";

        /// <summary>Gets a value indicating whether the class is deprecated.</summary>
        public bool IsDeprecated => _schema.IsDeprecated;

        /// <summary>Gets a value indicating whether the class has a deprecated message.</summary>
        public bool HasDeprecatedMessage => !string.IsNullOrEmpty(_schema.DeprecatedMessage);

        /// <summary>Gets the deprecated message.</summary>
        public string DeprecatedMessage => _schema.DeprecatedMessage;
        /// <summary>
        /// database table name
        /// </summary>
        public string TableName { get; }
        /// <summary>
        /// condition for select data
        /// </summary>
        public string WhereClauseFormat { get; }

        public bool IsTableExist
        {
            get;
            set;
        }

    }
}
