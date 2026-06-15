using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using APP.Framework.Globalization;

namespace APP.Framework
{
    /// <summary>
    /// Helper class for enum types
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// Toes the dictionary.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <param name="localizationPrefix">The localization prefix.</param>
        /// <returns>
        /// Returns the list of values of the enum
        /// </returns>
        public static EnumLookupCollection<TEnum> ToDictionary<TEnum>(string localizationPrefix)
               where TEnum : struct
        {
            EnumLookupCollection<TEnum> dictionary = new EnumLookupCollection<TEnum>();

            GetValues<TEnum>()
                .Select(key => CreateLookup<TEnum>(key, localizationPrefix))
                .OrderBy(o => o.Value)
                .ForAll(o => dictionary.Add(o));

            return dictionary;
        }

        private readonly static Random _Random = new Random();
        public static string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[_Random.Next(s.Length)]).ToArray());
        }

        public static string GetRandomIntString(int length)
        {
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[_Random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Toes the dictionary.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <param name="localizationPrefix">The localization prefix.</param>
        /// <param name="excludedValue">The excluded value.</param>
        /// <returns>
        /// Returns the list of values of the enum
        /// </returns>
        public static EnumLookupCollection<TEnum> ToDictionary<TEnum>(string localizationPrefix, TEnum? excludedValue)
            where TEnum : struct
        {
            return ToDictionary<TEnum>(localizationPrefix, o => !object.Equals(o, excludedValue));
        }

        /// <summary>
        /// Toes the dictionary.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <param name="localizationPrefix">The localization prefix.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>
        /// Returns the list of values of the enum
        /// </returns>
        public static EnumLookupCollection<TEnum> ToDictionary<TEnum>(string localizationPrefix, Func<TEnum, bool> predicate)
            where TEnum : struct
        {
            EnumLookupCollection<TEnum> dictionary = new EnumLookupCollection<TEnum>();

            GetValues<TEnum>(predicate)
                .Select(key => CreateLookup<TEnum>(key, localizationPrefix))
                .OrderBy(o => o.Value)
                .ForAll(o => dictionary.Add(o));

            return dictionary;
        }

        /// <summary>
        /// Toes the dictionary.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="localizationPrefix">The localization prefix.</param>
        /// <returns>
        /// Returns the list of values of the enum
        /// </returns>
        public static EnumLookupCollection<object> ToDictionary(Type enumType, string localizationPrefix)
        {
            EnumLookupCollection<object> dictionary = new EnumLookupCollection<object>();

            GetValues(enumType)
                .Select(key => CreateLookup<object>(key, localizationPrefix))
                .OrderBy(o => o.Value)
                .ForAll(o => dictionary.Add(o));

            return dictionary;
        }

        /// <summary>
        /// Toes the dictionary.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="localizationPrefix">The localization prefix.</param>
        /// <param name="excludedValue">The excluded value.</param>
        /// <returns>
        /// Returns the list of values of the enum
        /// </returns>
        public static EnumLookupCollection<object> ToDictionary(Type enumType, string localizationPrefix, object excludedValue)
        {
            return ToDictionary(enumType, localizationPrefix, o => !object.Equals(o, excludedValue));
        }

        /// <summary>
        /// Toes the dictionary.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="localizationPrefix">The localization prefix.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>
        /// Returns the list of values of the enum
        /// </returns>
        public static EnumLookupCollection<object> ToDictionary(Type enumType, string localizationPrefix, Func<object, bool> predicate)
        {
            EnumLookupCollection<object> dictionary = new EnumLookupCollection<object>();

            GetValues(enumType, predicate)
                .Select(key => CreateLookup<object>(key, localizationPrefix))
                .OrderBy(o => o.Value)
                .ForAll(o => dictionary.Add(o));

            return dictionary;
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <param name="predicate">The predicate.</param>
        /// <returns>
        /// Returns the list of values of the enum
        /// </returns>
        public static IEnumerable<TEnum> GetValues<TEnum>(Func<TEnum, bool> predicate)
            where TEnum : struct
        {
            return GetValues<TEnum>().Where(predicate);
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>
        /// Returns the list of values of the enum
        /// </returns>
        public static IEnumerable<object> GetValues(Type enumType, Func<object, bool> predicate)
        {
            return GetValues(enumType).Where(predicate);
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <returns>
        /// Returns the list of values of the enum
        /// </returns>
        public static IEnumerable<TEnum> GetValues<TEnum>()
            where TEnum : struct
        {
            Type enumType = typeof(TEnum);

            return GetValues(enumType).Cast<TEnum>();
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <returns>
        /// Returns the list of values of the enum
        /// </returns>
        public static IEnumerable<object> GetValues(Type enumType)
        {
            return enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(o => o.GetValue(null));
        }

        /// <summary>
        /// Creates the lookup.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="localizationPrefix">The localization prefix.</param>
        /// <returns>
        /// Returns the lookup for the specified enum key
        /// </returns>
        public static EnumLookup<TEnum> CreateLookup<TEnum>(object key, string localizationPrefix)
        {
            string text = StringLocalizer.LocalizeEnumValue(localizationPrefix, (Enum)key);

            string descriptionLocalizationKey = localizationPrefix + key.GetType().Name + "_" + key.ToString() + "description";

            string description = StringLocalizer.Localize(descriptionLocalizationKey, key.ToString());

            return new EnumLookup<TEnum>()
            {
                Key = (TEnum)key,
                Value = text,
                ValueDescription = description,
            };
        }
    }
}