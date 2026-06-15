using NJsonSchema;

namespace ExchangeBL
{
    internal static class JsonObjectTypeExtensions
    {
        public static bool IsString(this JsonObjectType type) => (type & JsonObjectType.String) != 0;
        public static bool IsArray(this JsonObjectType type) => (type & JsonObjectType.Array) != 0;
        public static bool IsObject(this JsonObjectType type) => (type & JsonObjectType.Object) != 0;
        public static bool IsNumber(this JsonObjectType type) => (type & JsonObjectType.Number) != 0;
        public static bool IsInteger(this JsonObjectType type) => (type & JsonObjectType.Integer) != 0;
        public static bool IsBoolean(this JsonObjectType type) => (type & JsonObjectType.Boolean) != 0;
        public static bool IsNone(this JsonObjectType type) => type == JsonObjectType.None;
    }
}
