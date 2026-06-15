using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace APP.Components.Dto
{
    public static class EnityConstString
    {


        public const string EncryptDecryptKey = "A8F26F78-1187-4544-8FC4-BE825F707160";

        //public const string DataTypeInt16 = "Int16";
        //public const string DataTypeInt32 = "Int32";
        //public const string DataTypeInt64 = "Int64";
        //public const string DataTypeDecimal = "Decimal";
        //public const string DataTypeDouble = "Double";
        //public const string DataTypeSingle = "Single";
        //public const string DataTypeDateTime = "DateTime";
        //public const string DataTypeBoolean = "Boolean";
        //public const string DataTypeGuid = "Guid";
        //public const string DataTypeByteArray = "Byte[]";
        //public const string DataTypeUnknownDataType = "UnknownDataType";

        //public static readonly string[] DataTableColumnDataType = new string[] {
        //    DataTypeString ,
        //    DataTypeInt16,
        //    DataTypeInt32,
        //    DataTypeInt64,
        //    DataTypeDecimal,
        //    DataTypeDouble,
        //    DataTypeSingle,
        //    DataTypeDateTime,
        //    DataTypeBoolean,
        //    DataTypeGuid,
        //    DataTypeByteArray,
        //    DataTypeUnknownDataType
        //};

      //  public const int EnityIdLimit = 3000;

        public class SearchViewFiedType
        {
            public const string Static = "StaticField";
            public const string SubItem = "SubItem";
        }

        public class FormulaConstString
        {
            public const string FormulaRegex = @"\[.+?\]";

            public const string StrMathOperation = "(  )  =  ==  !=  >  <  >=  <=  +  -  *  /  &&  ||  true  false  !  ";

            public const string StrDatetimeUnit = "Now  Today  Days  Weeks  Months  Years  Hours  Minutes  Seconds  Null ";

            public const string GroupingColumnToken = "GroupingColumn";

            public const string GroupingDoubleColonToken = "::";

            // For avreage Weight
            public const string GroupingAggregationSUMToken = "SUM";
            public const string GroupingAggregationAVGToken = "AVG";
            public const string GroupingAggregationMinToken = "MIN";
            public const string GroupingAggregationMAXToken = "MAX";

            
        }
    }

    public static class StringHelper
    {
        public static readonly string DelimToken = "|";
        public static readonly string UnderscoreToken = "_";
        public static readonly string SemicolonToken = ";";
        public static readonly string CommaToken = ",";
        public static readonly string DotToken = ".";
        
        public static string GetSubString(string orgString, int fromSide, int starPosition, int requireLength)
        {
            if (string.IsNullOrEmpty(orgString))
                return string.Empty;

            if (starPosition > orgString.Length)
                return string.Empty;

            // start position ( including itself)

            string toRetun = string.Empty;

            // from left start
            if (fromSide == 1)
            {
                int expectLenth = starPosition + requireLength;
                if (expectLenth <= orgString.Length)
                    toRetun = orgString.Substring(starPosition - 1, requireLength);
                else
                {
                    toRetun = orgString.Substring(starPosition - 1, orgString.Length + 1 - starPosition);
                }
            }
            else // from right
            {
                int fromrightEndInex = orgString.Length - starPosition;

                int fromrightstartIndex = orgString.Length - (requireLength + starPosition - 1);

                if (fromrightstartIndex <= 0)
                {
                    toRetun = orgString.Substring(0, fromrightEndInex + 1);
                }
                else
                {
                    toRetun = orgString.Substring(fromrightstartIndex, requireLength);
                }
            }

            return toRetun;
        }

        public static string[] SplitStringToArray(string parseString, string token)
        {
            if (parseString.IndexOf(token) == -1)
            {
                string[] oneString = { parseString };
                return oneString;
            }
            else
            {
                string[] strArray = parseString.Split(token.ToCharArray());
                return strArray;
            }
        }

        public static int[] ConvertStringToIntArray(string parseString, string token)
        {
            return SplitStringToArray(parseString, token).Select(o => int.Parse(o)).ToArray();

            // int[] toReturn = new int[result.Length];
        }

        public static string ConvertIntArrayToString(IEnumerable<int> intList, string token)
        {
            string toRetrun = string.Empty;
            foreach (var aListItem in intList)
            {
                toRetrun = toRetrun + aListItem.ToString() + token;
            }

            if (toRetrun != string.Empty)
            {
                toRetrun = toRetrun.Substring(0, toRetrun.Length - 1);
            }

            return toRetrun;
        }

        public static string StripHtmlTag(string text)
        {
            return Regex.Replace(text, @"<(.|\n)*?>", string.Empty);
        }

        public static IEnumerable<String> SplitInParts(this String s, Int32 partLength)
        {

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }

    }
}