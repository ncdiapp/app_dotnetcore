using System.Collections;
using System.Collections.Generic;

namespace System
{
    /// <summary>
    /// Extensions methods for the class Type
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Determines whether the specified instance has attribute.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance">The instance.</param>
        /// <param name="inherit">if set to <c>true</c> it include the inherited attributes.</param>
        /// <returns>
        ///   <c>true</c> if the specified instance has attribute; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasAttribute<T>(this Type instance, bool inherit)
            where T : Attribute
        {
            return HasAttribute(instance, typeof(T), inherit);
        }

        /// <summary>
        /// Determines whether the specified instance has attribute.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="attributeType">Type of the attribute.</param>
        /// <param name="inherit">if set to <c>true</c> it include the inherited attributes.</param>
        /// <returns>
        ///   <c>true</c> if the specified instance has attribute; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasAttribute(this Type instance, Type attributeType, bool inherit)
        {
            return instance != null
                && !instance.GetCustomAttributes(attributeType, inherit).IsEmpty();
        }

        /// <summary>
        /// Determines whether the specified instance is boolean type.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified instance is boolean type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBooleanType(this Type instance)
        {
            return instance != null
                && instance == typeof(bool);
        }

        /// <summary>
        /// Determines whether the specified type is a date time type .
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is a date time type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDateTimeType(this Type type)
        {
            TypeCode code = Type.GetTypeCode(type);
            return code == TypeCode.DateTime;
        }

        /// <summary>
        /// Determines whether the specified instance is an enumerable type.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified instance is an enumerable type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEnumerableType(this Type instance)
        {
            return instance != null
                && typeof(IEnumerable).IsAssignableFrom(instance);
        }

        /// <summary>
        /// Determines whether the specified instance is nullable boolean type.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified instance is nullable boolean type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullableBooleanType(this Type instance)
        {
            return instance != null
                && instance == typeof(bool?);
        }

        /// <summary>
        /// Determines whether the specified instance is nullable date time type.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified instance is nullable date time type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullableDateTimeType(this Type instance)
        {
            return instance != null
                && instance == typeof(DateTime?);
        }

        /// <summary>
        /// Determines whether the specified instance is nullable numeric type.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified instance is nullable numeric type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullableNumericType(this Type instance)
        {
            return instance != null
                && instance.IsNullableType()
                && instance.GetGenericArguments()[0].IsNumericType();
        }

        /// <summary>
        /// Determines whether the specified instance is nullable type.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified instance is nullable type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullableType(this Type instance)
        {
            return instance != null
                && instance.IsGenericType
                && instance.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Determines whether the specified type is a numeric type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is a numeric type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNumericType(this Type type)
        {
            if (type.IsValueType)
            {
                TypeCode code = Type.GetTypeCode(type);
                return code == TypeCode.Byte
                    || code == TypeCode.Decimal
                    || code == TypeCode.Double
                    || code == TypeCode.Int16
                    || code == TypeCode.Int32
                    || code == TypeCode.Int64
                    || code == TypeCode.SByte
                    || code == TypeCode.Single
                    || code == TypeCode.UInt16
                    || code == TypeCode.UInt32
                    || code == TypeCode.UInt64;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified type is a string type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is a string type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsStringType(this Type type)
        {
            TypeCode code = Type.GetTypeCode(type);
            return code == TypeCode.String
                || code == TypeCode.Char;
        }

        /// <summary>
        /// Determines whether the specified type is an unsigned numeric type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is unsigned numeric type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsUnsignedNumericType(this Type type)
        {
            if (type.IsValueType)
            {
                TypeCode code = Type.GetTypeCode(type);
                return code == TypeCode.Byte
                    || code == TypeCode.UInt16
                    || code == TypeCode.UInt32
                    || code == TypeCode.UInt64;
            }

            return false;
        }
    }
}