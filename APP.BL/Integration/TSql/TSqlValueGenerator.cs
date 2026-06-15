using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace ExchangeBL
{
    internal class TSqlValueGenerator : ValueGeneratorBase
    {
        public TSqlValueGenerator(TSqlGeneratorSettings settings) : base(settings) { }

        public override string GetDefaultValue(JsonSchema schema, bool allowsNull, string targetType,
            string typeNameHint, bool useSchemaDefault, TypeResolverBase typeResolver) => null;

        public override string GetNumericValue(JsonObjectType type, object value, string format) => null;
    }
}
