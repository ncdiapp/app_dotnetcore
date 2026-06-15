using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using System.Reflection;

namespace APP.Components.Dto
{
    public static class ExtensionMethodhelper
    {


      
            public static T ToObject<T>(this IDictionary<string, object> source)
                where T : class, new()
            {
                var someObject = new T();
                var someObjectType = someObject.GetType();

                foreach (var item in source)
                {
                    someObjectType
                             .GetProperty(item.Key)
                             .SetValue(someObject, item.Value, null);
                }

                return someObject;
            }

            public static IDictionary<string, object> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
            {
                return source.GetType().GetProperties(bindingAttr).ToDictionary
                (
                    propInfo => propInfo.Name,
                    propInfo => propInfo.GetValue(source, null)
                );

            }
        
        public static T DeepCopy<T>(this T oSource)
        {
            T oClone;

            DataContractSerializer dcs = new DataContractSerializer(typeof(T));

            using (MemoryStream ms = new MemoryStream())
            {
                dcs.WriteObject(ms, oSource);
                ms.Position = 0;
                oClone = (T)dcs.ReadObject(ms);
            }

            return oClone;
        }


		private static readonly  Random _Random = new Random();
		public static string RandomId()
		{
			const string chars1 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			string s1 = new string(Enumerable.Repeat(chars1, 4)
			  .Select(s => s[_Random.Next(s.Length)]).ToArray());


			const string chars2 = "0123456789";
			string s2 = new string(Enumerable.Repeat(chars2, 2)
			  .Select(s => s[_Random.Next(s.Length)]).ToArray());

			return s1 + s2;

		}



		public static List<T> EnumToList<T>()
        {
            Type enumType = typeof(T);

            // Can't use type constraints on value types, so have to do check like this
            if (enumType.BaseType != typeof(Enum))
                throw new ArgumentException("T must be of type System.Enum");

            Array enumValArray = Enum.GetValues(enumType);

            List<T> enumValList = new List<T>(enumValArray.Length);

            foreach (int val in enumValArray)
            {
                enumValList.Add((T)Enum.Parse(enumType, val.ToString()));
            }

            return enumValList;
        }

    }

  
}