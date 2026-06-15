using System;
using System.Globalization;
using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg.Data
{
    class DateConverter
    {
        private readonly EmSqlType _sqlType;

        public DateConverter(EmSqlType sqlType)
        {
            _sqlType = sqlType;
        }

        public string Convert(DateTime dateTime, string dbType)
        {
            if (_sqlType == EmSqlType.SqlServer || _sqlType == EmSqlType.SqlServerCe)
            {
                //try to use ISO 8601 formats
                switch (dbType)
                {
                    case "DATE":
                        //ISO 8601 unseparated
                        return "'" + dateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "'";
                    case "TIME":
                        return "'" + dateTime.ToString("HH:mm:ss.ffffff", CultureInfo.InvariantCulture) + "'";
                    case "DATETIME":
                        return "'" + dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) + "'";
                    case "SMALLDATETIME":
                    case "DATETIME2":
                        return "'" + dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "'";
                }
            }

            if (_sqlType == EmSqlType.Oracle)
            {
                if (dbType == "TIMESTAMP")
                    return "TIMESTAMP '" + dateTime.ToString("yyyy-MM-dd HH:mm:ss.ff", CultureInfo.InvariantCulture) + "'";
                if (dbType == "DATE" && dateTime == dateTime.Date)
                    return "DATE '" + dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "'";
                if (dbType == "DATE")
                    return "TO_DATE('" + dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "', 'yyyy-mm-dd hh24:mi:ss')";
                return "TO_TIMESTAMP('" + dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffff", CultureInfo.InvariantCulture) + "', 'yyyy-mm-dd hh24:mi:ss.FF')";
            }

            if (_sqlType == EmSqlType.PostgreSql && dbType == "TIMESTAMP")
            {
                return "to_timestamp('" + dateTime.ToString("yyyy-MM-dd HH:mm:ss.ff", CultureInfo.InvariantCulture) + "', 'YYYY-MM-DD HH:MI:SS')";
            }

            //most dbms will understand the ISO8601 formatted string date
            if (dbType == "TIME")
                return "'" + dateTime.ToString("HH:mm:ss.ff", CultureInfo.InvariantCulture) + "'";

            return "'" + dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "'";
        }
    }
}
