using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace ExchangeBL
{
    internal class JsonInheritanceConverterTemplateModel
    {
        public JsonInheritanceConverterTemplateModel(CodeGeneratorSettingsBase settings) { }
    }

    internal class DateFormatConverterTemplateModel
    {
        public DateFormatConverterTemplateModel(CodeGeneratorSettingsBase settings) { }
    }

    internal class FileTemplateModel
    {
        public string Namespace { get; set; }
        public bool GenerateNullableReferenceTypes { get; set; }
        public string TypesCode { get; set; }
    }

    internal class EnumTemplateModel
    {
        public EnumTemplateModel(string typeName, JsonSchema schema, CodeGeneratorSettingsBase settings) { }
    }
}
