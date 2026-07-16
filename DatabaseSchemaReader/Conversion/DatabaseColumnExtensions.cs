using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg.Conversion
{
    internal static class DatabaseColumnExtensions
    {
        public static bool IsTimestamp(this DatabaseColumn column)
        {
            if (column == null) return false;

            if (column.DataType != null)
            {
                //if it's a timestamp, you can't insert it
                if (column.DataType.ProviderDbType == (int)SqlDbType.Timestamp
                    //double check as could be Oracle type with same provider code
                    && column.DataType.GetNetType() == typeof(byte[]))
                {
                    return true;
                }
            }

            // Fallback when schema cache has DbDataType but DataType is not linked.
            // SQL Server rowversion/timestamp cannot be INSERTed explicitly;
            // Form Save uses GetColumnsExcludeAutoTimeStampAndComputed() which depends on this.
            var dbType = NormalizeDbDataTypeName(column.DbDataType);
            if (dbType == "ROWVERSION")
                return true;

            // SQL Server "timestamp" is rowversion (byte[]). Oracle/PG "timestamp" is datetime —
            // only treat as concurrency token when NetType is byte[] or DataType is missing.
            if (dbType == "TIMESTAMP")
            {
                if (column.DataType == null)
                    return true;
                return column.DataType.GetNetType() == typeof(byte[]);
            }

            return false;
        }

        private static string NormalizeDbDataTypeName(string dbDataType)
        {
            if (string.IsNullOrEmpty(dbDataType)) return null;
            var dataType = dbDataType.Trim().ToUpperInvariant();
            var brace = dataType.IndexOf('(');
            if (brace > 0)
                dataType = dataType.Substring(0, brace).Trim();
            return dataType;
        }

        /// <summary>
        /// Static method to convert a list of columns into a generic DataTable for binding
        /// </summary>
        public static DataTable ToDataTable(List<DatabaseColumn> list)
        {
            var table = new DataTable("Columns");
            table.Locale = CultureInfo.InvariantCulture;
            table.Columns.Add("Name", typeof(String));
            table.Columns.Add("TableName", typeof(String));
            table.Columns.Add("DataType", typeof(String));
            table.Columns.Add("Ordinal", typeof(Int32));
            table.Columns.Add("Precision", typeof(Int32));
            table.Columns.Add("Scale", typeof(Int32));
            table.Columns.Add("Length", typeof(Int32));
            table.Columns.Add("Nullable", typeof(Boolean));
            table.Columns.Add("IsPrimaryKey", typeof(Boolean));
            table.Columns.Add("IsForeignKey", typeof(Boolean));
            table.Columns.Add("IsUniqueKey", typeof(Boolean));
            table.Columns.Add("IsAutoNumber", typeof(Boolean));
            table.Columns.Add("IsIndexed", typeof(Boolean));
            foreach (DatabaseColumn item in list)
            {
                DataRow row = table.NewRow();
                row["Name"] = item.Name;
                row["TableName"] = item.TableName;
                row["DataType"] = item.DbDataType;
                row["Ordinal"] = item.Ordinal;
                row["Precision"] = item.Precision.HasValue ? (object)item.Precision.Value : DBNull.Value;
                row["Scale"] = item.Scale.HasValue ? (object)item.Scale.Value : DBNull.Value;
                row["Length"] = item.Length.HasValue ? (object)item.Length.Value : DBNull.Value;
                row["Nullable"] = item.Nullable;
                row["IsPrimaryKey"] = item.IsPrimaryKey;
                row["IsForeignKey"] = item.IsForeignKey;
                row["IsUniqueKey"] = item.IsUniqueKey;
                row["IsAutoNumber"] = item.IsAutoNumber;
                row["IsIndexed"] = item.IsIndexed;
                table.Rows.Add(row);
            }
            return table;
        }
    }
}
