using DatabaseSchemaMrg.DataSchema;
using System;

namespace DatabaseSchemaMrg
{


    /*.NET Type	    MSSQL Type		MySQL Type		Oracle Type
	System.Boolean	TINYINT	    	BIT				NUMBER
	System.Char	    CHAR(1)			CHAR(1)			CHAR(1)
	System.SByte	SMALLINT		TINYINT			NUMBER
	System.Byte	    SMALLINT		SMALLINT		NUMBER
	System.Int16	SMALLINT		SMALLINT		NUMBER
	System.UInt16	INT				INT				NUMBER
	System.Int32	INT				INT				NUMBER
	System.UInt32	BIGINT			BIGINT			NUMBER(19)
	System.Int64	BIGINT			BIGINT			NUMBER(19)
	System.UInt64	DECIMAL(20,0)	DECIMAL(20,0)	NUMBER
	System.Single	REAL			FLOAT			FLOAT(63)
	System.Double	FLOAT			DOUBLE			FLOAT(126)
	System.String	VARCHAR(255)	VARCHAR(255)	VARCHAR2(255)
	System.DateTime	DATETIME		DATETIME		TIMESTAMP(9)
	System.Decimal	NUMERIC(20,10)	NUMERIC(20,10)	NUMBER(20,10)
	System.Guid	    UNIQUEIDENTIFIER VARCHAR(40)	VARCHAR2(40)
	Byte[]	       IMAGE	 		 LONGBLOB	     BLOB
	 * 
	https://docs.telerik.com/data-access/deprecated/developers-guide/data-access-domain-model/domain-class-mapping/default-mapping/developer-guide-domain-model-class-mapping-type-mapping
	 * 
	 * 
	 * ***/



    //bigint	-9,223,372,036,854,775,808 to 9,223,372,036,854,775,807	-2^63 to 2^63-1	8 Bytes
    //int	-2,147,483,648 to 2,147,483,647	-2^31 to 2^31-1	4 Bytes
    //smallint	-32,768 to 32,767	-2^15 to 2^15-1	2 Bytes
    //tinyint 0 to 255	2^0-1 to 2^8-1	1 Byte


    public static class AppMetaDataSqlTypeConvertBL
    {
        private enum EmAppDataType
        {
            String = 1,


            Integer = 2, // int 32


            Decimal = 3,

            Date = 4,


            Time = 5,


            DateTime = 6,


            Boolean = 7,

            Blob = 8,


            Tinyint = 9,
            Smallint = 10,
            BigInt = 11,


            //Mysql:
            UInt8 = 12,
            UInt16 = 13, // (2 byte 16)
            UInt32 = 14, // int (4 byte)
            UInt64 = 15, // big int (8 byte)

            // need to match sqlserver nvarch(max) or mysql longText
            LongString = 16,

            Guid = 17,
        }
        public static void ConvertNetTypeToSqlType(DatabaseColumn column, EmSqlType? SqlServerType)
        {


            if (SqlServerType.Value == EmSqlType.SqlServer)
            {
                ConvertNetTypeToMsSqlServerType(column);
            }
            else if (SqlServerType.Value == EmSqlType.Oracle)
            {
                ConvertNetTypeToOracleType(column);
            }

            else if (SqlServerType.Value == EmSqlType.MySql)
            {
                ConvertNetTypeToMysqlType(column);
            }


        }

        public static string ConvertSqlTypeToNetType(DatabaseColumn column, EmSqlType? SqlServerType)
        {
            string toReturn = "Object";

            if (SqlServerType.Value == EmSqlType.SqlServer)
            {
                toReturn = ConvertMsSQLserverToNetType(column);
            }
            else if (SqlServerType.Value == EmSqlType.Oracle)
            {
                toReturn = ConvertOracleServerToNetType(column);
            }

            else if (SqlServerType.Value == EmSqlType.MySql)
            {
                toReturn = ConvertMysqlServerToNetType(column);
            }
            return toReturn;
        }
        /// <summary>
        /// https://dev.mysql.com/doc/refman/5.7/en/data-types.html
        /// 11.1 Data Type Overview     
        //11.2 Numeric Types     
        //11.3 Date and Time Types     
        //11.4 String Types     
        //11.5 Spatial Data Types     
        //11.6 The JSON Data Type
        //11.7 Data Type Default Values
        //11.8 Data Type Storage Requirements
        //11.9 Choosing the Right Type for a Column
        //11.10 Using Data Types from Other Database Engines
        /// </summary>
        /// <param name="column"></param>

        private static void ConvertNetTypeToMysqlType(DatabaseColumn column)
        {
            if (!string.IsNullOrEmpty(column.Tag.ToString()))
            {
                string dataType = column.Tag.ToString();



                if (dataType == EmAppDataType.String.ToString())
                {
                    column.DbDataType = "NVARCHAR";
                    //if (!column.Length.HasValue || column.Length.Value <= 0)
                    //{
                    //    column.Length = 100;
                    //}

                    if (column.Length.HasValue && column.Length.Value > 0)
                    {
                        column.Length = column.Length.Value;
                    }
                    else
                    {
                        column.Length = 500;
                    }

                    int? length = column.Length;

                    //https://stackoverflow.com/questions/13932750/tinytext-text-mediumtext-and-longtext-maximum-storage-sizes
                    if (length == -1) //MAX
                    {
                        column.DbDataType = "LONGTEXT";
                        column.Length = 16777215;
                    }
                    else if (length > 500 && length < 65536)
                    {
                        column.DbDataType = "TEXT";
                        column.Length = 65535;
                    }
                    else if (length >= 65536 && length < 16777216)
                    {
                        column.DbDataType = "MEDIUMTEXT";
                        column.Length = 16777215;
                    }
                    else if (length >= 16777216)
                    {
                        column.DbDataType = "LONGTEXT";
                        // column.Length = 999999999;
                    }

                }
                else if (dataType == EmAppDataType.LongString.ToString())
                {
                    column.DbDataType = "LONGTEXT";
                    // column.Length = 999999999;
                }
                else if (dataType == EmAppDataType.Tinyint.ToString())
                {
                    column.DbDataType = "Tinyint";
                }
                else if (dataType == EmAppDataType.Smallint.ToString())
                {
                    column.DbDataType = "Smallint";
                }
                else if (dataType == EmAppDataType.Integer.ToString())
                {
                    column.DbDataType = "INT";
                }
                else if (dataType == EmAppDataType.BigInt.ToString())
                {
                    column.DbDataType = "BigInt";
                }


                else if (dataType == EmAppDataType.UInt8.ToString())
                {
                    column.DbDataType = "Tinyint unsigned";
                }
                else if (dataType == EmAppDataType.UInt16.ToString())
                {
                    column.DbDataType = "Smallint unsigned";
                }
                else if (dataType == EmAppDataType.UInt32.ToString())
                {
                    column.DbDataType = "int unsigned";
                }
                else if (dataType == EmAppDataType.UInt64.ToString())
                {
                    column.DbDataType = "BigInt unsigned";
                }

                else if (dataType == EmAppDataType.Decimal.ToString())
                {
                    column.DbDataType = "DECIMAL";

                    if (column.Precision.HasValue && column.Precision.Value > 0 && column.Precision.Value <= 65)
                    {
                        column.Precision = column.Precision.Value;
                    }
                    else
                    {
                        column.Precision = 18;
                    }

                    // column.Length = column.Precision;

                    if (column.Scale.HasValue && column.Scale.Value >= 0 && column.Scale.Value <= column.Precision)
                    {
                        column.Scale = column.Scale.Value;
                    }
                    else
                    {
                        if (column.Precision >= 4)
                        {
                            column.Scale = 4;
                        }
                        else
                        {
                            column.Scale = 0;
                        }

                    }
                }

                //  as of MySQL 8.0.19 the DATETIME supports time zone offsets, so there's even less reason to use TIMESTAMP now.
                //https://dev.mysql.com/doc/refman/8.0/en/date-and-time-types.html
                //The DATE type is used for values with a date part but no time part. MySQL retrieves and displays DATE values in 'YYYY-MM-DD' format. The supported range is '1000-01-01' to '9999-12-31'.
                else if (dataType == EmAppDataType.Date.ToString())
                {
                    column.DbDataType = "DATE";

                }
                //MySQL retrieves and displays TIME values in 'HH:MM:SS' format (or 'HHH:MM:SS' format for large hours values). TIME values may range from '-838:59:59' to '838:59:59'. The hours part may be so large because the TIME type can be used not only to represent a time of day (which must be less than 24 hours), but also elapsed time or a time interval between two events (which may be much greater than 24 hours, or even negative).
                //imestamps in MySQL generally used to track changes to records, and are often updated every time the record is changed. If you want to store a specific value you should use a datetime field.

                //If you meant that you want to decide between using a UNIX timestamp or a native MySQL datetime field, go with the native format.You can do calculations within MySQL that way  ("SELECT DATE_ADD(my_datetime, INTERVAL 1 DAY)") and it is simple to change the format of the value to a UNIX timestamp("SELECT UNIX_TIMESTAMP(my_datetime)")
                else if (dataType == EmAppDataType.DateTime.ToString())
                {
                    column.DbDataType = "DateTime";
                }
                else if (dataType == EmAppDataType.Time.ToString())
                {
                    column.DbDataType = "TIME";
                }
                //The BIT data type is used to store binary values in the column and accepts either 0 or 1. The range of bit values for the column goes from 1 to 64. If the range is not set, the default value will be 1.
                else if (dataType == EmAppDataType.Boolean.ToString()) // "y/n" AS INDICATOR
                {
                    column.DbDataType = "BIT";

                    column.Length = 1;
                }
                //A BLOB can be 65535 bytes (64 KB) maximum. If you need more consider using: a MEDIUMBLOB for 16777215 bytes (16 MB) a LONGBLOB for 4294967295 bytes (4 GB)
                //https://dev.mysql.com/doc/refman/8.0/en/blob.html#:~:text=A%20BLOB%20is%20a%20binary,TEXT%20%2C%20MEDIUMTEXT%20%2C%20and%20LONGTEXT%20.
                else if (dataType == EmAppDataType.Blob.ToString())
                {
                    column.DbDataType = "Blob";
                }
                else if (dataType == EmAppDataType.Guid.ToString())
                {
                    column.DbDataType = "VARCHAR";
                    column.Length = 36;
                }
                else // default NVARCHAR 
                {
                    column.DbDataType = "NVARCHAR";
                    column.Length = 500;
                }
            }

        }
        /// <summary>
        /// Overview of Character Datatypes

        // Numeric Datatypes

        // DATE Datatype

        // LOB Datatypes

        // RAW and LONG RAW Datatypes

        //	VARCHAR2 and VARCHAR Datatypes
        //The VARCHAR2 datatype stores variable-length character strings. When you create a table with a VARCHAR2 column, you specify a maximum string length (in bytes or characters) between 1 and 4000 bytes for the VARCHAR2 column. For each row, Oracle Database stores each value in the column as a variable-length field unless a value exceeds the column's maximum length, in which case Oracle Database returns an error. Using VARCHAR2 and VARCHAR saves on space used by the table.

        //For example, assume you declare a column VARCHAR2 with a maximum size of 50 characters. In a single-byte character set, if only 10 characters are given for the VARCHAR2 column value in a particular row, the column in the row's row piece stores only the 10 characters (10 bytes), not 50.
        //NCHAR and NVARCHAR2 are Unicode datatypes that store Unicode character data.
        //Oracle Database compares VARCHAR2 values using nonpadded comparison semantics
        //https://docs.oracle.com/cd/B28359_01/server.111/b28318/datatype.htm#CNCPT1832
        //f ROWID and UROWID Datatypes
        /// </summary>
        /// <param name="column"></param>
        /// 

        private static void ConvertNetTypeToOracleType(DatabaseColumn column)
        {

            //System.UInt16
            if (column.Tag != null && !string.IsNullOrEmpty(column.Tag.ToString()))
            {
                string dataType = column.Tag.ToString();

                if (dataType != "String")
                {
                    column.Length = null;
                }

                if (dataType == "String")
                {
                    column.DbDataType = "NVARCHAR2";
                    //if (!column.Length.HasValue || column.Length.Value <= 0)
                    //{
                    //    column.Length = 100;
                    //}

                    column.Length = 1000;
                }
                else if (dataType == "Integer")
                {
                    column.DbDataType = "NUMBER";
                    column.Length = 6;
                    column.Scale = 0;
                }
                else if (dataType == "Decimal")
                {
                    column.DbDataType = "NUMBER";

                    if (column.Precision.HasValue && column.Precision.Value > 0 && column.Precision.Value <= 38)
                    {
                        column.Precision = column.Precision.Value;
                    }
                    else
                    {
                        column.Precision = 18;
                    }

                    //column.Length = column.Precision;

                    if (column.Scale.HasValue && column.Scale.Value >= 0 && column.Scale.Value <= column.Precision)
                    {
                        column.Scale = column.Scale.Value;
                    }
                    else
                    {
                        if (column.Precision >= 4)
                        {
                            column.Scale = 4;
                        }
                        else
                        {
                            column.Scale = 0;
                        }

                    }
                }
                else if (dataType == "Date")
                {
                    column.DbDataType = "date";
                    column.DateTimePrecision = 0;
                }
                ///
                //DATE and TIMESTAMP have the same size(7 bytes).Those bytes are used to store century, decade, year, month, day, hour, minute and seconds. But TIMESTAMP allows to store additional info such as fractional seconds(11 bytes) and fractional seconds with timezone(13 bytes).

                //TIMESTAMP was added as an ANSI compliant to Oracle. Before that, it had DATE only.
                //Timestamp is an ANSI thing, for compliance. 

                //In general cases you should use DATE.But if precision in time is a requirement, use TIMESTAMP.
                else if (dataType == "Time")
                {
                    column.DbDataType = "TIMESTAMP";
                }
                else if (dataType == "DateTime")
                {
                    column.DbDataType = "TIMESTAMP";
                }
                else if (dataType == "Boolean") // "y/n" AS INDICATOR
                {
                    column.DbDataType = "NCHAR";

                    column.Length = 1;
                }
                else if (dataType == "Blob")
                {
                    column.DbDataType = "image";
                }
                else if (dataType == EmAppDataType.Guid.ToString())
                {
                    column.DbDataType = "NVARCHAR2";
                    column.Length = 40;
                }
            }
            else
            {
                column.DbDataType = "NVARCHAR2";
                column.Length = 1000;
            }
        }


        private static void ConvertNetTypeToMsSqlServerType(DatabaseColumn column)
        {
            if (column.Tag != null && !string.IsNullOrEmpty(column.Tag.ToString()))
            {
                string dataType = column.Tag.ToString();

                if (dataType != "String")
                {
                    column.Length = null;
                }

                if (dataType == "String")
                {
                    column.DbDataType = "nvarchar";

                    if (column.Length.HasValue && column.Length.Value > 0)
                    {
                        column.Length = column.Length.Value;
                    }
                    else
                    {
                        //Gets or sets the length if this is string (VARCHAR) or character (CHAR) type data. In SQLServer, a length of -1 indicates VARCHAR(MAX)
                        column.Length = -1;
                    }
                }
                else if (dataType == EmAppDataType.Tinyint.ToString())
                {
                    column.DbDataType = "tinyint";
                }
                else if (dataType == EmAppDataType.Smallint.ToString() || dataType == EmAppDataType.UInt8.ToString())
                {
                    column.DbDataType = "smallint";
                }

                else if (dataType == "Integer" || dataType == EmAppDataType.UInt16.ToString())
                {
                    column.DbDataType = "int";
                }
                else if (dataType == EmAppDataType.BigInt.ToString()
                    || dataType == EmAppDataType.UInt32.ToString() ||
                    dataType == EmAppDataType.UInt64.ToString())
                {
                    column.DbDataType = "bigint";
                }

                else if (dataType == "Decimal")
                {
                    column.DbDataType = "decimal";


                    if (column.Precision.HasValue && column.Precision.Value > 0 && column.Precision.Value <= 38)
                    {
                        column.Precision = column.Precision.Value;
                    }
                    else
                    {
                        column.Precision = 18;
                    }

                    // column.Length = column.Precision;

                    if (column.Scale.HasValue && column.Scale.Value >= 0 && column.Scale.Value <= column.Precision)
                    {
                        column.Scale = column.Scale.Value;
                    }
                    else
                    {
                        if (column.Precision >= 4)
                        {
                            column.Scale = 4;
                        }
                        else
                        {
                            column.Scale = 0;
                        }

                    }

                }
                else if (dataType == "Date")
                {
                    column.DbDataType = "date";
                    column.DateTimePrecision = 0;
                }
                else if (dataType == "Time")
                {
                    column.DbDataType = "time";
                }
                else if (dataType == "DateTime")
                {
                    column.DbDataType = "datetime";
                }
                else if (dataType == "Boolean")
                {
                    column.DbDataType = "bit";
                }
                else if (dataType == "Blob")
                {
                    column.DbDataType = "varbinary";
                    column.Length = -1;
                }
                else if (dataType == EmAppDataType.Guid.ToString())
                {
                    column.DbDataType = "uniqueidentifier";                    
                }
            }
            else
            {
                column.DbDataType = "nvarchar";

                if (column.Length.HasValue && column.Length.Value > 0)
                {
                    column.Length = column.Length.Value;
                }
                else
                {
                    column.Length = 4000;
                }
            }
        }

  

        private static string ConvertMysqlServerToNetType(DatabaseColumn column)
        {
            string sqlDataType = column.DbDataType;
            if (
                 sqlDataType.IndexOf("char", StringComparison.InvariantCultureIgnoreCase) != -1
                 || sqlDataType.IndexOf("text", StringComparison.InvariantCultureIgnoreCase) != -1
                  || sqlDataType.IndexOf("enum", StringComparison.InvariantCultureIgnoreCase) != -1
                )
            {

                return EmAppDataType.String.ToString();


            }
            else if (
                     (sqlDataType.IndexOf("timestamp", StringComparison.InvariantCultureIgnoreCase) != -1)
                )
            {
                return EmAppDataType.DateTime.ToString();
            }
            else if (
                    (sqlDataType.IndexOf("Date", StringComparison.InvariantCultureIgnoreCase) != -1)

                )
            {
                return EmAppDataType.Date.ToString();
            }


            else if ((sqlDataType.IndexOf("tinyint", StringComparison.InvariantCultureIgnoreCase) != -1) )
            {
                return EmAppDataType.Tinyint.ToString();
            }


            else if (sqlDataType.ToLower() == "smallint")
            {
                return EmAppDataType.Smallint.ToString();
            }
            else if (sqlDataType.ToLower() == "int")
            {
                return EmAppDataType.Integer.ToString();
            }

            else if (sqlDataType.ToLower() == "bigint")
            {
                return EmAppDataType.BigInt.ToString();
            }


            else if (sqlDataType.ToLower() == "bigint unsigned")
            {
                return EmAppDataType.UInt64.ToString();
            }
            else if (sqlDataType.ToLower() == "int unsigned")
            {
                return EmAppDataType.UInt32.ToString();
            }
            else if (sqlDataType.ToLower() == "smallint unsigned")
            {
                return EmAppDataType.UInt16.ToString();
            }
            else if (sqlDataType.ToLower() == "tinyint unsigned")
            {
                return EmAppDataType.UInt8.ToString();
            }

            else if (sqlDataType.ToLower() == "blob")
            {
                return EmAppDataType.Blob.ToString();
            }

            //Fixed - Point Types(Exact Value) - DECIMAL, NUMERIC.
            //Floating - Point Types(Approximate Value) - FLOAT, DOUBLE.
            else if ( //
                  (sqlDataType.IndexOf("DECIMAL", StringComparison.InvariantCultureIgnoreCase) != -1)
                    || (sqlDataType.IndexOf("NUMERIC", StringComparison.InvariantCultureIgnoreCase) != -1)
                    || (sqlDataType.IndexOf("FLOAT", StringComparison.InvariantCultureIgnoreCase) != -1)
                    || (sqlDataType.IndexOf("DOUBLE", StringComparison.InvariantCultureIgnoreCase) != -1)


                     )
            {
                return "Decimal";
            }
            else if (  sqlDataType.IndexOf("bit", StringComparison.InvariantCultureIgnoreCase) != -1
                    && (column.Precision .HasValue && column.Precision.Value == 1)

                   )
            {
                return "Boolean";
            }

            return "";
        }

        private static string ConvertOracleServerToNetType(DatabaseColumn column)
        {
            string sqlDataType = column.DbDataType;
            if (sqlDataType.IndexOf("char", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                if (column.Length > 1)
                {
                    return "String";
                }
                else
                {
                    return "Boolean";
                }
            }
            else if (
                    (sqlDataType.IndexOf("Date", StringComparison.InvariantCultureIgnoreCase) != -1)
                    || (sqlDataType.IndexOf("timestamp", StringComparison.InvariantCultureIgnoreCase) != -1)
                )
            {
                return "DateTime";
            }
            else if
            (
             (
                sqlDataType.IndexOf("NUMBER", StringComparison.InvariantCultureIgnoreCase) != -1)
                && ((!column.Scale.HasValue) || column.Scale == 0)

              )
            {
                return "Integer";
            }
            else if (
                 (sqlDataType.IndexOf("NUMBER", StringComparison.InvariantCultureIgnoreCase) != -1)
                && column.Scale > 0

                     )
            {
                return "Decimal";
            }

            else if (
                 (sqlDataType.IndexOf("BLOB", StringComparison.InvariantCultureIgnoreCase) != -1)
                && column.Scale > 0

                     )
            {
                return "Blob";
            }



            return "";
        }

        private static string ConvertMsSQLserverToNetType(DatabaseColumn column)
        {
            ////column.DbDataType
            string sqlDataType = column.DbDataType;
            switch (sqlDataType)
            {
                case "bigint":
                    return "Bigint";

                case "binary":
                case "image":
                case "timestamp":
                case "varbinary":
                    return "Blob";

                case "bit":
                    return "Boolean";

                case "char":
                case "nchar":
                case "ntext":
                case "nvarchar":
                case "text":
                case "varchar":
                case "xml":
                    return "String";

                case "time":
                    return "Time";

                case "date":
                    return "Date";

                case "datetime":
                case "smalldatetime":
                case "dateiime2":
                    return "DateTime";

                case "decimal":
                case "money":
                case "smallmoney":
                    return "Decimal";

                case "float":
                    return "Decimal";

                case "int":
                    return "Integer";

                case "real":
                    return "Decimal";

                case "uniqueidentifier":
                    return "Guid";

                case "smallint":
                    return "Smallint";

                case "tinyint":
                    return "Tinyint";
               

                //case "Variant:
                //case "Udt:
                //    return typeof(object);

                //case "Structured:
                //    return typeof(DataTable);

                //case "DateTimeOffset:
                //    return typeof(DateTimeOffset?);

                default:
                    return "";
            }
        }
    }
}