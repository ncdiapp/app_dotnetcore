using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace APP.Components.Dto
{
    public static class ControlTypeValueConverter
    {

        public static object ConvertObjectValueToSpecificDataType(object sourceValue, EmAppDataType toDataType)
        {
            int controlTpye = (int)ConvertDataTypeToDefaultControlType((int)toDataType);

            return ConvertValueToObject(sourceValue, controlTpye);
        }

        public static EmAppControlType ConvertDataTypeToDefaultControlType(int? dataType)
        {
            EmAppControlType controlType = EmAppControlType.TextBox;

            if (dataType.HasValue)
            {
                switch ((EmAppDataType)dataType.Value)
                {
                    case EmAppDataType.String:
                        controlType = EmAppControlType.TextBox;
                        break;
                    case EmAppDataType.Integer:
                        controlType = EmAppControlType.Numeric;
                        break;
                    case EmAppDataType.Decimal:
                        controlType = EmAppControlType.Numeric;
                        break;
                    case EmAppDataType.Date:
                        controlType = EmAppControlType.Date;
                        break;
                    case EmAppDataType.Time:
                        controlType = EmAppControlType.Time;
                        break;
                    case EmAppDataType.DateTime:
                        controlType = EmAppControlType.Date;
                        break;
                    case EmAppDataType.Boolean:
                        controlType = EmAppControlType.CheckBox;
                        break;
                    case EmAppDataType.Blob:
                        controlType = EmAppControlType.ImageBinary;
                        break;
                    default:
                        controlType = EmAppControlType.TextBox;
                        break;
                }
            }

            return controlType;
        }


        public static DataTable TryConvertDataTableToMatchDataType(DataTable excelDataTable)
        {
            // try to match the data type by parsing 

            Dictionary<string, EmAppDataType> dictColumnType = new Dictionary<string, EmAppDataType>();

            if (excelDataTable.Rows.Count > 0)
            {
                Random rnd = new Random();



                foreach (DataColumn colum in excelDataTable.Columns)
                {

                    int randomeRow = rnd.Next(0, excelDataTable.Rows.Count);

                    var cellValue = excelDataTable.Rows[randomeRow][colum] as string;

                    // try twice
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {

                        randomeRow = rnd.Next(0, excelDataTable.Rows.Count);

                        cellValue = excelDataTable.Rows[randomeRow][colum] as string;
                    }
                    // if still null
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {

                        dictColumnType[colum.ColumnName] = EmAppDataType.String;

                    }
                    else // not null try to parse to concrete type 
                    {

                        int intResut;
                        Int64 bigIntresut;
                        UInt64 ubigIntresut;

                        Decimal deresut;

                        DateTime datetimeResut;
                        
                        if (int.TryParse(cellValue, out intResut))
                        {
                            dictColumnType[colum.ColumnName] = EmAppDataType.Integer;
                        }
                        else if (Int64.TryParse(cellValue, out bigIntresut))
                        {
                            dictColumnType[colum.ColumnName] = EmAppDataType.BigInt;
                        }
                        else if (UInt64.TryParse(cellValue, out ubigIntresut))
                        {
                            dictColumnType[colum.ColumnName] = EmAppDataType.BigInt;
                        }
                        else if (Decimal.TryParse(cellValue, out deresut))
                        {
                            dictColumnType[colum.ColumnName] = EmAppDataType.Decimal;
                        }                       
                        else if (DateTime.TryParse(cellValue, out datetimeResut))
                        {
                            if (cellValue.ToString().ToLower().EndsWith("am") || cellValue.ToString().ToLower().EndsWith("pm"))
                            {
                                dictColumnType[colum.ColumnName] = EmAppDataType.String;
                            }
                            else
                            {
                                dictColumnType[colum.ColumnName] = EmAppDataType.DateTime;
                            }                            
                        }
                        else
                        {
                            dictColumnType[colum.ColumnName] = EmAppDataType.String;
                        }
                    }




                    //Contr
                }
            }
            //            float   4 bytes Stores fractional numbers. Sufficient for storing 6 to 7 decimal digits
            //double  8 bytes Stores fractional numbers.Sufficient for storing 15 decimal digits

            DataTable matchDataTable = new DataTable();
            foreach (string colunName in dictColumnType.Keys)
            {
                DataColumn dataColumn = new DataColumn(colunName);
                dataColumn.AllowDBNull = true;

                matchDataTable.Columns.Add(dataColumn);
                if (dictColumnType[colunName] == EmAppDataType.DateTime)
                {
                    dataColumn.DataType = typeof(DateTime);


                }
                else if (dictColumnType[colunName] == EmAppDataType.Date)
                {
                    dataColumn.DataType = typeof(DateTime);


                }
                else if (dictColumnType[colunName] == EmAppDataType.Boolean)
                {
                    dataColumn.DataType = typeof(bool);



                }
                else if (dictColumnType[colunName] == EmAppDataType.BigInt)
                {
                    dataColumn.DataType = typeof(Int64);


                }

                else if (dictColumnType[colunName] == EmAppDataType.Integer)
                {
                    dataColumn.DataType = typeof(int);



                }
                else if (dictColumnType[colunName] == EmAppDataType.UInt16)
                {
                    dataColumn.DataType = typeof(UInt16);


                }
                else if (dictColumnType[colunName] == EmAppDataType.UInt32)
                {
                    dataColumn.DataType = typeof(UInt32);


                }
                else if (dictColumnType[colunName] == EmAppDataType.UInt64)
                {
                    dataColumn.DataType = typeof(UInt64);


                }
                else if (dictColumnType[colunName] == EmAppDataType.Decimal)
                {
                    dataColumn.DataType = typeof(Decimal);


                    // dataColumn.le


                }
                else if (dictColumnType[colunName] == EmAppDataType.Blob)
                {
                    dataColumn.DataType = typeof(byte[]);


                }
            }
            foreach (DataRow row in excelDataTable.Rows)
            {
                DataRow newRow = matchDataTable.NewRow();
                matchDataTable.Rows.Add(newRow);
                foreach (string column in dictColumnType.Keys)
                {
                    if (row[column] != null)
                    {
                        newRow[column] = ControlTypeValueConverter.ConverStringValueToSpecificDataType(row[column].ToString(), dictColumnType[column]);
                    }


                }
            }

            return matchDataTable;
        }

        public static object ConverStringValueToSpecificDataType(string stringVale, EmAppDataType emAppDataType, CultureInfo cultureInfo =null)
        {
            if (emAppDataType == EmAppDataType.String)
            {
                return stringVale;
            }
            if (emAppDataType == EmAppDataType.Integer)
            {
                int intResut;
                if (int.TryParse(stringVale, out intResut))
                {
                    return intResut;
                }
            }
            if (emAppDataType == EmAppDataType.BigInt)
            {
                long intResut;
                if (long.TryParse(stringVale, out intResut))
                {
                    return intResut;
                }
            }
            if (emAppDataType == EmAppDataType.UInt16)
            {
                UInt16 intResut;
                if (UInt16.TryParse(stringVale, out intResut))
                {
                    return intResut;
                }
            }
            if (emAppDataType == EmAppDataType.UInt32)
            {
                UInt32 intResut;
                if (UInt32.TryParse(stringVale, out intResut))
                {
                    return intResut;
                }
            }
            if (emAppDataType == EmAppDataType.UInt64)
            {
                UInt64 intResut;
                if (UInt64.TryParse(stringVale, out intResut))
                {
                    return intResut;
                }
            }
            if (emAppDataType == EmAppDataType.Decimal)
            {
                decimal intResut;
                if (decimal.TryParse(stringVale, out intResut))
                {
                    return intResut;
                }
            }

            //https://stackoverflow.com/questions/1368636/why-cant-datetime-parseexact-parse-9-1-2009-using-m-d-yyyy
            //https://learn.microsoft.com/en-us/dotnet/api/system.datetime.parseexact?view=net-8.0#system-datetime-parseexact(system-string-system-string-system-iformatprovider)
            //var cultureInfo = new CultureInfo("en-US");
            //string[] dateStrings1 = { "Oct 21, 2023 9:12:02 PM" };
            //string[] dateStrings = { "Friday, April 10, 2009" };
            if (emAppDataType == EmAppDataType.Date )
            {
                DateTime intResut;
                if (DateTime.TryParse(stringVale, out intResut))
                {
                    if (!(intResut.Date > new DateTime(9999, 12, 31)))
                    {
                        return intResut;
                    }
                }
            }

            //    //1/1/1753 12:00:00  1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.
            if ( emAppDataType == EmAppDataType.DateTime)
            {
                DateTime intResut;
                if (DateTime.TryParse(stringVale, out intResut))
                {
                    if (! (intResut.Date < new DateTime(1753,1,1) || intResut.Date > new DateTime(9999, 12, 31)))
                    {
                        return intResut;
                    }
                  
                }

               // return intResut;
            }

        

            if (emAppDataType == EmAppDataType.Boolean)
            {
                bool intResut;
                if (bool.TryParse(stringVale, out intResut))
                {
                    return intResut;
                }
            }
            if (emAppDataType == EmAppDataType.Guid)
            {
                return stringVale;
            }

            return DBNull.Value;



        }

        public static string ConvertDataTableSelectFiledString(object sourceValue, int aControlType, IFormatProvider sourceCulture = null)
        {
            string toReturn = string.Empty;

            if (aControlType == (int)EmAppControlType.Date)
            {
                DateTime? date = null;
                if (sourceCulture != null)
                {
                    date = ConvertValueToDateWithCulture(sourceValue, sourceCulture);
                }
                else
                {
                    date = ConvertValueToDate(sourceValue);
                }

                if (date.HasValue)
                {
                    toReturn = string.Format("'#{0}#'", date);

                }




            }
            else if (aControlType == (int)EmAppControlType.CheckBox)
            {
                var value = ConvertValueToBoolean(sourceValue);


                if ((bool)value)
                {
                    toReturn = "'true'";
                }
                else
                {
                    toReturn = "'false'";
                }


            }

            else if (aControlType == (int)EmAppControlType.DDL
                || aControlType == (int)EmAppControlType.AutoComplete
                || aControlType == (int)EmAppControlType.SearchAbleDDL
                || aControlType == (int)EmAppControlType.RadioButtons
                || aControlType == (int)EmAppControlType.Progress
                || aControlType == (int)EmAppControlType.File
                || aControlType == (int)EmAppControlType.Image
                 || aControlType == (int)EmAppControlType.Video
                  || aControlType == (int)EmAppControlType.Audio
                || aControlType == (int)EmAppControlType.Image
                || aControlType == (int)EmAppControlType.RichText)
            {
                int? valueId = ConvertValueToInt(sourceValue);

                if (valueId.HasValue)
                {
                    var valueIdstring = valueId.ToString();

                    toReturn = string.Format("'{0}'", valueIdstring.ToString());
                }



            }

            else if (aControlType == (int)EmAppControlType.Numeric)
            {
                double? valuedouble = ConvertValueToDouble(sourceValue);
                if (valuedouble.HasValue)
                {

                    toReturn = string.Format("'{0}'", valuedouble.ToString());
                }
                //return ConvertValueToDecimal(sourceValue);
            }

            else
            {
                if (sourceValue != null && sourceValue.ToString() != string.Empty)
                {
                    var stringValue = sourceValue.ToString().Replace("'", "''");

                    toReturn = string.Format("'{0}'", stringValue.ToString());

                }

            }

            return toReturn;
        }

        public static List<LookupItemDto> GenerateLookupList<T>()
           where T : struct
        {
            var result = new List<LookupItemDto>();

            Type enumType = typeof(T);

            foreach (var item in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                Enum key = item.GetValue(null) as Enum;

                var enumId = Convert.ToInt32(item.GetRawConstantValue());

                string display = key.ToString();

                result.Add(new LookupItemDto { Id = enumId, Display = display });
            }

            return result.OrderBy(o => o.Display).ToList();
        }

        public static object ConvertValueToObject(object sourceValue, int aControlType, IFormatProvider sourceCulture = null)
        {
            if (aControlType == (int)EmAppControlType.Date || aControlType == (int)EmAppControlType.DateTimeDetail)
            {
                if (sourceCulture != null)
                {
                    return ConvertValueToDateWithCulture(sourceValue, sourceCulture);
                }
                else
                {
                    return ConvertValueToDate(sourceValue);
                }
            }
            else if (aControlType == (int)EmAppControlType.CheckBox)
            {
                return ConvertValueToBoolean(sourceValue);
            }
            else if (aControlType == (int)EmAppControlType.DDL
               || aControlType == (int)EmAppControlType.AutoComplete
               || aControlType == (int)EmAppControlType.SearchAbleDDL
               )
            {
                return sourceValue;
            }

            else if (aControlType == (int)EmAppControlType.RadioButtons
                || aControlType == (int)EmAppControlType.Progress
                || aControlType == (int)EmAppControlType.File
                || aControlType == (int)EmAppControlType.Image
                 || aControlType == (int)EmAppControlType.Video
                  || aControlType == (int)EmAppControlType.Audio
                || aControlType == (int)EmAppControlType.Image
                || aControlType == (int)EmAppControlType.RichText)
            {
                return ConvertValueToInt(sourceValue);
            }

            else if (aControlType == (int)EmAppControlType.Numeric)
            {
                return ConvertValueToDouble(sourceValue);
                //return ConvertValueToDecimal(sourceValue);
            }
            else if (aControlType == (int)EmAppControlType.RGBColorDisplay)
            {
                if (sourceValue == null || sourceValue.ToString() == string.Empty)
                    return "255|255|255";

                return sourceValue.ToString();
            }
            else
            {
                if (sourceValue == null || sourceValue.ToString() == string.Empty)
                    return string.Empty;
                return sourceValue.ToString();
            }
        }

        // if the ControlType is Numeric, need to check Nbdecimal , if decimal is zero it is integer, else it is decial
        public static Type GetDataTypeByControlType(int aControlType)
        {
            if (aControlType == (int)EmAppControlType.Date)
            {
                return typeof(DateTime);
            }
            else if (aControlType == (int)EmAppControlType.CheckBox)
            {
                return typeof(bool);
            }

            else if (aControlType == (int)EmAppControlType.DDL
                || aControlType == (int)EmAppControlType.AutoComplete
                || aControlType == (int)EmAppControlType.SearchAbleDDL
                || aControlType == (int)EmAppControlType.RadioButtons
                || aControlType == (int)EmAppControlType.Progress
                || aControlType == (int)EmAppControlType.File
                || aControlType == (int)EmAppControlType.Image
                || aControlType == (int)EmAppControlType.RichText)
            {
                return typeof(int);
            }

            else if (aControlType == (int)EmAppControlType.Numeric)
            {
                return typeof(decimal);
            }

            else
            {
                return typeof(string);
            }
        }

        public static double? ConvertValueToDouble(object sourceValue)
        {
            if (sourceValue == null) return null;

            double outvalue;
            if (double.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }

            return null;
        }

        public static decimal? ConvertValueToDeciamlWithPosition(object sourceValue, int numberOfDeciamlPosition)
        {
            if (sourceValue == null) return null;

            decimal outvalue;
            if (decimal.TryParse(sourceValue.ToString(), out outvalue))
            {
                outvalue = Math.Round(outvalue, numberOfDeciamlPosition);
                return outvalue;
            }

            return null;
        }



        public static DateTime? ConvertValueToDate(object sourceValue)
        {
            if (sourceValue == null) return null;

            DateTime outvalue;
            if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }

            return null;
        }

        public static TimeSpan? ConvertValueToTimeSpan(object sourceValue)
        {
            if (sourceValue == null) return null;

            TimeSpan outvalue;

            if (TimeSpan.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }
            else
            {
                DateTime? aDateTime = ConvertValueToDate(sourceValue);
                if (aDateTime.HasValue)
                {
                    return aDateTime.Value.TimeOfDay;

                }

            }

            return null;

        }


        public static DateTime? ConvertValueToDateWithCulture(object sourceValue, IFormatProvider sourceCulture)
        {
            if (sourceValue == null) return null;

            DateTime outvalue;

            if (DateTime.TryParse(sourceValue.ToString(), sourceCulture, DateTimeStyles.None, out outvalue))
            {
                return outvalue;
            }

            return null;
        }


        public static Boolean? ConvertValueToBoolean(object sourceValue)
        {
            if (sourceValue == null) return null;

            Boolean outvalue;
            if (Boolean.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }
            else
            {
                int? intVlaue = ConvertValueToInt(sourceValue);
                if (intVlaue.HasValue)
                {
                    return intVlaue.Value == 1;
                }
            }

            return null;
        }
        //int and Int32 are indeed synonymous
        public static int? ConvertValueToInt(object sourceValue)
        {
            if (sourceValue == null) return null;

            int outvalue;
            if (int.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }

            return null;
        }

        public static long ConvertDateTimeToYMDHMS(DateTime date)
        {
            // //expressed as a value between 0 and 999. between 0 and 59.
            // The seconds component, expressed as a value between 0 and 59.
            return (int.Parse(date.Year.ToString().Substring(2))) * 100000000L + date.Month * 1000000L + date.Day * 10000 + date.Minute * 100 + date.Second;

        }

        public static string ConvertBlobToText(byte[] byteArray)
        {
            if (byteArray == null)
            {
                return string.Empty;
            }

            else
            {

                return System.Text.Encoding.UTF8.GetString(byteArray);
            }

        }
        public static byte []  ConvertTextToBlob(string aText)
        {
            if (string.IsNullOrWhiteSpace(aText) )
            {
                return null;
            }

            else
            {

                return  System.Text.Encoding.UTF8.GetBytes(aText);
            }

        }

        public static Int16? ConvertValueToInt16(object sourceValue)
        {


            if (sourceValue == null) return null;

            Int16 outvalue;
            if (Int16.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }

            return null;
        }

        //Int64?
        public static Int64? ConvertValueToInt64(object sourceValue)
        {
            if (sourceValue == null) return null;

            Int64 outvalue;
            if (Int64.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }

            return null;
        }

        public static long? ConvertValueToLong(object sourceValue)
        {
            if (sourceValue == null) return null;

            long outvalue;
            if (long.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }

            return null;
        }



        public static Guid? ConvertValueToGuid(object sourceValue)
        {
            if (sourceValue == null) return null;

            Guid outvalue;
            if (Guid.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }

            return null;
        }

        public static byte[] ConvertValueToByteArray(object sourceValue)
        {
            return sourceValue as byte[];
        }

        public static decimal? ConvertValueToDecimal(object sourceValue)
        {
            if (sourceValue == null) return null;

            decimal outvalue;
            if (decimal.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }
            return null;
        }

        public static decimal? ConvertValueToDecimal(object sourceValue, int noofDeciaml)
        {
            if (sourceValue == null) return null;

            decimal outvalue;
            if (decimal.TryParse(sourceValue.ToString(), out outvalue))
            {
                outvalue = System.Math.Round(outvalue, noofDeciaml);
                return outvalue;
            }
            return null;
        }

        public static decimal ConvertValueToDecimalWithDefautZero(object sourceValue)
        {
            if (sourceValue == null) return 0;

            decimal outvalue;
            if (decimal.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }
            return 0;
        }

        public static string ConvertValueToStringWithDefaultEmptyString(object sourceValue)
        {
            if (sourceValue == null)
                return string.Empty;

            return sourceValue.ToString();
        }

        public static string ConvertDecimalToCultureFormat(object adoubleValue, CultureInfo aCultureInfo, int nbDecimal)
        {
            if (adoubleValue == null || string.IsNullOrEmpty(adoubleValue.ToString()))
                return string.Empty;

            try
            {
                if (nbDecimal == 0)
                {
                    return adoubleValue.ToString();
                }
                else
                {
                    double doubleNumber = Convert.ToDouble(adoubleValue.ToString());

                    string format = doubleNumber.ToString("N", aCultureInfo);

                    if (nbDecimal == 1)
                    {
                        format = doubleNumber.ToString("N1", aCultureInfo);
                    }
                    if (nbDecimal == 2)
                    {
                        format = doubleNumber.ToString("N2", aCultureInfo);
                    }
                    if (nbDecimal == 3)
                    {
                        format = doubleNumber.ToString("N3", aCultureInfo);
                    }
                    if (nbDecimal == 4)
                    {
                        format = doubleNumber.ToString("N4", aCultureInfo);
                    }

                    if (nbDecimal == 5)
                    {
                        format = doubleNumber.ToString("N5", aCultureInfo);
                    }

                    if (nbDecimal == 6)
                    {
                        format = doubleNumber.ToString("N6", aCultureInfo);
                    }

                    return format;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string ConvertCultureTextToDBMSDecimalFormat(int? nbDecimal, object valueText, CultureInfo aCultureInfo)
        {
            if (valueText == null || string.IsNullOrEmpty(valueText.ToString()))
                return string.Empty;
            try
            {
                decimal aValue = Convert.ToDecimal(valueText, aCultureInfo);
                if (nbDecimal.HasValue)
                {
                    string format = string.Format("{0:0}", aValue);

                    if (nbDecimal.Value == 1)
                    {
                        format = string.Format("{0:0.0}", aValue); // this will just cut the number off at the 3rd decimal place
                    }
                    if (nbDecimal.Value == 2)
                    {
                        format = string.Format("{0:0.00}", aValue);
                    }
                    if (nbDecimal.Value == 3)
                    {
                        format = string.Format("{0:0.000}", aValue);
                    }
                    if (nbDecimal.Value == 4)
                    {
                        format = string.Format("{0:0.0000}", aValue);
                    }

                    if (nbDecimal.Value == 5)
                    {
                        format = string.Format("{0:0.00000}", aValue);
                    }

                    if (nbDecimal.Value == 6)
                    {
                        format = string.Format("{0:0.000000}", aValue);
                    }

                    return format;
                }
                else
                {
                    return aValue.ToString();
                }

            }
            catch
            {
                return string.Empty;
            }
        }



        public static string GetStringFormatByNumberOfDecimal(int? nbDecimal)
        {
            string stringFormat = "0.00";

            if (nbDecimal.Value == 1)
            {
                stringFormat = "0.0";
            }
            if (nbDecimal.Value == 2)
            {
                stringFormat = "0.00";
            }
            if (nbDecimal.Value == 3)
            {
                stringFormat = "0.000";
            }
            if (nbDecimal.Value == 4)
            {
                stringFormat = "0.0000";
            }

            if (nbDecimal.Value == 5)
            {
                stringFormat = "0.00000";
            }

            return stringFormat;
        }

        //public static string ConvertNumericStringFormatByCulture(int? nbDecimal, CultureInfo aCultureInfo)
        //{
        //    string enPrefix="{0:###,###,##0";
        //   // string frPrefix="{0:### ### ##0";
        //    string frPrefix = "{0:### ### ##0";

        //    string TrPrefix = "{0:###.###.##0";

        //    //'#,###.00##
        //    string format = "{0:0}";

        //    if (nbDecimal.HasValue)
        //        {


        //            if (nbDecimal.Value == 1)
        //            {
        //                if( aCultureInfo.Name.StartsWith ("en"))
        //                {
        //                    format = enPrefix+".#}";                            
        //                }

        //                if( aCultureInfo.Name.StartsWith ("fr"))
        //                {
        //                    format = frPrefix + ".#}";
        //                }

        //            }
        //            if (nbDecimal.Value == 2)
        //            {
        //                if( aCultureInfo.Name.StartsWith ("en"))
        //                {
        //                    format = enPrefix + ".##}";                            
        //                }

        //                if( aCultureInfo.Name.StartsWith ("fr"))
        //                {
        //                    format = frPrefix + ".##}";
        //                }
        //            }
        //            if (nbDecimal.Value == 3)
        //            {
        //                if( aCultureInfo.Name.StartsWith ("en"))
        //                {
        //                    format = enPrefix + ".###}";                            
        //                }

        //                if( aCultureInfo.Name.StartsWith ("fr"))
        //                {
        //                    format = frPrefix + ".###}";
        //                }
        //            }
        //            if (nbDecimal.Value == 4)
        //            {
        //                if( aCultureInfo.Name.StartsWith ("en"))
        //                {
        //                    format = enPrefix + ".####}";                            
        //                }

        //                if( aCultureInfo.Name.StartsWith ("fr"))
        //                {
        //                    format = frPrefix + ".####}";
        //                }
        //            }

        //            if (nbDecimal.Value == 5)
        //            {
        //                if( aCultureInfo.Name.StartsWith ("en"))
        //                {
        //                    format = enPrefix + ".#####}";                            
        //                }

        //                if( aCultureInfo.Name.StartsWith ("fr"))
        //                {
        //                    format = frPrefix + ".#####}";
        //                }
        //            }

        //            if (nbDecimal.Value == 6)
        //            {
        //                if( aCultureInfo.Name.StartsWith ("en"))
        //                {
        //                    format = enPrefix + ".######}";                            
        //                }

        //                if( aCultureInfo.Name.StartsWith ("fr"))
        //                {
        //                    format = frPrefix + ".######}";
        //                }
        //            }


        //        }
        //    return format;               
        //}


        public static bool IsObjectNaNOrInfinity(object value)
        {

            if (value is double)
            {
                return double.IsNaN((double)value);
            }

            else if (value is float)
            {
                return float.IsNaN((float)value) || float.IsInfinity((float)value);
            }


            return false;
        }


    }

    public static class InchCMUnitConvert
    {
        private static char minusSign = '-';
        public static int nbDecimal = 4;

        public static string NotNumber = "NaN";

        private static decimal cmToInch(decimal dec)
        {
            double d = decimal.ToDouble(dec) / INCH_CM;
            return Convert.ToDecimal(d);
        }

        public static string DoubleToInch(double aDouble)
        {
            return DecimalToInch(Convert.ToDecimal(aDouble));
        }

        public static string DecimalStringToInch(string decimalCMstring)
        {
            if (string.IsNullOrEmpty(decimalCMstring))
                return "0";
            decimal outDecial = 0;
            if (decimal.TryParse(decimalCMstring, out outDecial))
            {
                return DecimalToInch(outDecial);
            }
            return "0";
        }

        public static string DecimalToInch(decimal cm)
        {
            if ((double)cm == 0.75)
            {

            }

            decimal orgCm = cm;

            cm = Math.Abs(cm);

            string toReturn = "0";

            decimal dec1 = cmToInch(cm);

            if (dec1 != 0)
            {
                if (dec1.ToString(CultureInfo.InvariantCulture).IndexOf('.') > 0)
                {
                    string strinch = dec1.ToString(CultureInfo.InvariantCulture);
                    //  string[] str = strinch.Split(".".ToCharArray(), 2);

                    string[] str = strinch.Split(".".ToCharArray());

                    double d = Convert.ToDouble("0." + str[1], CultureInfo.InvariantCulture);

                    int inch = (int)Math.Round((double)d / ONE_OF_SIXTYFOUR_INCH);
                    //					int inch = (int)Math.Round( (double) d / ONE_OF_THIRTYTWO_INCH );

                    if (str[0] != "0")
                    {
                        if (inch != 0)
                        {
                            if (inch == 64) // 32)
                            {
                                int approx = int.Parse(str[0]) + 1;
                                toReturn = approx.ToString();
                            }
                            else
                                toReturn = str[0] + Inchs(inch);
                        }
                        else
                            toReturn = str[0];
                    }
                    else
                    {
                        if (inch == 64) // 32)
                            return "1";
                        else
                            toReturn = Inchs(inch);
                    }
                }
                else
                    toReturn = dec1.ToString(CultureInfo.InvariantCulture);
            }

            if (orgCm < 0)
            {
                toReturn = "-" + toReturn;
            }
            return toReturn;
        }

        public static double InchToDouble(string inch)
        {
            return Convert.ToDouble(InchToDecimal(inch));
        }

        public static decimal CMToDecimal(string cm)
        {
            Decimal output = 0;

            try
            {
                output = Decimal.Parse(cm);
            }
            catch
            {
            }

            return output;
        }

        public static decimal InchToDecimal(string inch)
        {
            if (String.IsNullOrEmpty(inch))
                return 0;

            decimal returnValue = 0;

            string orgInch = inch;

            if (orgInch.IndexOf(minusSign) != -1)
            {
                inch = inch.Substring(orgInch.IndexOf(minusSign) + 1);
            }

            if (inch.IndexOf('/') > 0)
            {
                try
                {
                    double db;
                    string[] strInch = inch.Trim().Split(" ".ToCharArray());
                    if (strInch.Length > 1)
                    {
                        string[] rem = strInch[1].Trim().Split("/".ToCharArray());
                        db = double.Parse(strInch[0]) + (double.Parse(rem[0]) / double.Parse(rem[1]));
                    }
                    else
                    {
                        string[] rem = inch.Trim().Split("/".ToCharArray());
                        db = (double.Parse(rem[0]) / double.Parse(rem[1]));
                    }
                    returnValue = inchToCm(Convert.ToDecimal(db));
                }
                catch
                {
                    // return 0;
                }
            }
            else
            {
                try
                {
                    returnValue = inchToCm(decimal.Parse(inch.Trim()));
                }
                catch
                {
                    //return 0;
                }
            }

            if (orgInch.IndexOf(minusSign) != -1)
            {
                // keep the minuse value
                returnValue = returnValue * (-1);
            }

            return Math.Round(returnValue, nbDecimal);
        }


        private static int DivRem(int a, int b, out int result)
        {
            int c = a / b;
            result = a - (b * c);
            return c;
        }

        private static string Inchs(int inch)
        {
            int index, rem;

            // SL does not support Math.DivRem
            // index = Math.DivRem(inch, 2, out rem) - 1;
            // Math.DivRem (

            index = DivRem(inch, 2, out rem) - 1;
            if (rem == 0 && 0 <= index && index < 31)
                return special64[index];
            else
                return (" " + inch.ToString() + "/64");
        }

        private static decimal inchToCm(decimal dec)
        {
            double d = decimal.ToDouble(dec) * INCH_CM;

            return Math.Round(Convert.ToDecimal(d), nbDecimal);
        }

        private static readonly double INCH_CM = 2.54;

        //private static readonly double ONE_OF_THIRTYTWO_INCH = 0.03125;
        private static readonly string[] special32 = { " 1/16", " 1/8",  " 3/16", " 1/4",  " 5/16",
                                                        " 3/8",  " 7/16", " 1/2",  " 9/16", " 5/8",
                                                        " 11/16"," 3/4",  " 13/16"," 7/8",  " 15/16" };

        private static readonly double ONE_OF_SIXTYFOUR_INCH = 0.015625;
        private static readonly string[] special64 = {  " 1/32", " 1/16", " 3/32",  " 1/8",
                                                        " 5/32", " 3/16", " 7/32",  " 1/4",
                                                        " 9/32", " 5/16", " 11/32", " 3/8",
                                                        " 13/32"," 7/16", " 15/32", " 1/2",
                                                        " 17/32"," 9/16", " 19/32", " 5/8",
                                                        " 21/32"," 11/16"," 23/32", " 3/4",
                                                        " 25/32"," 13/16"," 27/32", " 7/8",
                                                        " 29/32"," 15/16"," 31/32", };

        //
    }
}