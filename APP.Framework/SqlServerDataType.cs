using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace APP.Persistence.Common
{
    public class SqlServerDataType
    {
        public static readonly string char_ = "char";
        public static readonly string varchar_ = "varchar";
        public static readonly string text_ = "text";

        public static readonly string nchar_ = "nchar";
        public static readonly string nvarchar_ = "nvarchar";
        public static readonly string ntext_ = "ntext";



        public static readonly string date_ = "date";
        public static readonly string datetime2_ = "datetime2";
        public static readonly string datetime_ = "datetime";
        public static readonly string smalldatetime_ = "smalldatetime";
        public static readonly string datetimeoffset_ = "datetimeoffset";
        public static readonly string time_ = "time";


        //Exact Numerics
        public static readonly string bigint = "bigint";
        public static readonly string bit_ = "bit";
        public static readonly string decimal_ = "decimal";
        public static readonly string int_ = "int";
        public static readonly string money_ = "money";
        public static readonly string numeric_ = "numeric";


        public static readonly string smallint_ = "smallint";
        public static readonly string smallmoney_ = "smallmoney";
        public static readonly string tinyint_ = "tinyint";

        //Approximate Numerics

        public static readonly string float_ = "float";
        public static readonly string real_ = "real";

        //Binary Strings

        public static readonly string binary_ = "binary";
        public static readonly string varbinary_ = "varbinary";
        public static readonly string image_ = "image";


        //Other Data Types

        public static readonly string cursor_ = "cursor";
        public static readonly string timestamp_ = "timestamp";
        public static readonly string hierarchyid_ = "hierarchyid";

        public static readonly string sql_variant_ = "sql_variant";
        public static readonly string table_ = "table";

        public static readonly string uniqueidentifier_ = "uniqueidentifier";

        public static readonly string xml_ = "xml";
        public static readonly string Spatial_Types_ = "Spatial Types";





    }
  

   
}