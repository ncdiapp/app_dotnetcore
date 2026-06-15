#if NETFRAMEWORK
using Microsoft.AnalysisServices.AdomdClient;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif
using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;

namespace App.BL
{
#if NETFRAMEWORK
    public static class AppADOMDBL
    {
        // TODO-PHASE4: Replace with .NET 10 equivalent
        // get failed
        // private static readonly string connectionString = "DataSource=http://lab-plmsbackup/olap/msmdpump.dll;Catalog=AdventureWorksDW14";

        //private static readonly string connectionString = "Data Source=srv-bigdata;Catalog=ADW2014MultidimensionalEE;";

        private static readonly string connectionString = "Data Source=LAB-PLMSBACKUP\\SQL2012;Catalog=AdventureWorksDW14;";

        public static DataTable GetDBSCubeList()
        {
            string query = @"SELECT [CATALOG_NAME] AS [DATABASE],CUBE_CAPTION AS [CUBE/PERSPECTIVE],BASE_CUBE_NAME
							FROM $system.MDSchema_Cubes
							WHERE CUBE_SOURCE=1	 ";

            //string connstr = Connect(@"LAB-PLMSBACKUP\SQL2012", "Analysis Services Tutorial", @"visual-2000\sean", @"v2K201810");

            var dt = ConvertDataReaderToDataTable(query, connectionString);

            return dt;
        }

        public static string Connect(string serverId, string dbName, string user, string passwod)
        {
            //var conn = new AdomdConnection();
            //string connstr = "";
            //;
            return string.Format(@"Data Source={0};Initial Catalog={1};User ID={2};Password={3}", serverId, dbName, user, passwod);

            //return conn;
        }

        #region Convert Datareader toDatatable

        /// <summary>
        /// Convert Datareader toDatatable
        /// </summary>
        /// <param name="query"></param>
        /// <param name="myConnection"></param>
        public static DataTable ConvertDataReaderToDataTable(string query, string myConnection)
        {
            AdomdConnection conn = new AdomdConnection(myConnection);
            try
            {
                try
                {
                    conn.Open();
                }
                catch (Exception)
                {
                }
                using (AdomdCommand cmd = new AdomdCommand(query, conn))
                {
                    AdomdDataReader rdr;
                    //cmd.CommandTimeout = connectionTimeout;
                    using (AdomdDataAdapter ad = new AdomdDataAdapter(cmd))
                    {
                        DataTable dtData = new DataTable("Data");
                        DataTable dtSchema = new DataTable("Schema");
                        rdr = cmd.ExecuteReader();
                        if (rdr != null)
                        {
                            dtSchema = rdr.GetSchemaTable();
                            foreach (DataRow schemarow in dtSchema.Rows)
                            {
                                dtData.Columns.Add(schemarow.ItemArray[0].ToString(), System.Type.GetType(schemarow.ItemArray[5].ToString()));
                            }
                            while (rdr.Read())
                            {
                                object[] ColArray = new object[rdr.FieldCount];
                                for (int i = 0; i < rdr.FieldCount; i++)
                                {
                                    if (rdr[i] != null) ColArray[i] = rdr[i];
                                }
                                dtData.LoadDataRow(ColArray, true);
                            }
                            rdr.Close();
                        }
                        return dtData;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                conn.Close(false);
            }
        }

        #endregion Convert Datareader toDatatable

        public static string Read(PivotDataSourceRequest request)
        {
            using (var connection = new AdomdConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = request.Statement;

                using (var reader = command.ExecuteXmlReader())
                {
                    return (reader.ToXmlaResult());
                }
            }
        }

        public static string Discover(PivotDataSourceRequest request)
        {
            using (var connection = new AdomdConnection(connectionString))
            {
                connection.Open();

                var restrictions = new AdomdRestrictionCollection();
                foreach (var restriction in request.Restrictions)
                {
                    restrictions.Add(restriction.Key, restriction.Value);
                }

                var result = connection.GetSchemaDataSet(request.Command, restrictions);

                return result.ToXmlaDiscoverResult();
            }
        }
    }
#else
    public static class AppADOMDBL
    {
        // TODO-PHASE4: Replace with .NET 10 equivalent
        public static DataTable GetDBSCubeList()
            => throw new PlatformNotSupportedException("TODO-PHASE4: Migrate to .NET 10 equivalent");
        public static string Connect(string serverId, string dbName, string user, string passwod)
            => throw new PlatformNotSupportedException("TODO-PHASE4: Migrate to .NET 10 equivalent");
        public static DataTable ConvertDataReaderToDataTable(string query, string myConnection)
            => throw new PlatformNotSupportedException("TODO-PHASE4: Migrate to .NET 10 equivalent");
        public static string Read(PivotDataSourceRequest request)
            => throw new PlatformNotSupportedException("TODO-PHASE4: Migrate to .NET 10 equivalent");
        public static string Discover(PivotDataSourceRequest request)
            => throw new PlatformNotSupportedException("TODO-PHASE4: Migrate to .NET 10 equivalent");
    }
#endif

    public class PivotDataSourceRequest
    {
        public PivotDataSourceRequest()
        {
            Restrictions = new Dictionary<string, string>();
        }

        public string Statement { get; set; }

        public string Command { get; set; }
        public Dictionary<string, string> Restrictions { get; set; }
    }

    public static class PivotDataSourceExtensions
    {
        private static readonly string XMLA_WRAP = @"<soap:Envelope><soap:Body><ExecuteResponse><return>{0}</return></ExecuteResponse></soap:Body></soap:Envelope>";
        private static readonly string XMLA_DISCOVER_WRAP = @"<soap:Envelope><soap:Body><DiscoverResponse><return>{0}</return></DiscoverResponse></soap:Body></soap:Envelope>";

        public static string ToXmlaResult(this XmlReader reader)
        {
            return string.Format(XMLA_WRAP, reader.ReadOuterXml());
        }

        public static string ToXmlaDiscoverResult(this DataSet dataSet)
        {
            return string.Format(XMLA_DISCOVER_WRAP, dataSet.GetXml().Replace("NewDataSet", "root").Replace("rowsetTable", "row"));
        }
    }
}

// $(document).ready(function ()
//{
//	var pivotgrid = $("#pivotgrid").kendoPivotGrid({
//		height: 550,
//            dataSource:
//		{
//			type: "xmla",
//                columns: [{ name: "[Date].[Calendar]", expand: true }, { name: "[Product].[Category]" }],
//                rows: [{ name: "[Geography].[City]" }],
//                measures: ["[Measures].[Internet Sales Amount]"],
//                transport:
//			{
//				connection:
//				{
//					catalog: "Adventure Works DW 2008R2",
//                        cube: "Adventure Works"

//					},
//                    read:
//				{
//					url: "@Url.Action("Read")",
//                        dataType: "text",
//                        contentType: "text/xml",
//                        type: "POST"

//					},
//                    discover:
//				{
//					url: "@Url.Action("Discover")",
//                        dataType: "text",
//                        contentType: "text/xml",
//                        type: "POST"

//					}
//			},
//                schema:
//			{
//				type: "xmla"

//				},
//                error: function(e) {
//				alert("error: " + kendo.stringify(e.errors[0]));
//			}
//		}
//	}).data("kendoPivotGrid");

//        $("#configurator").kendoPivotConfigurator({
//		dataSource: pivotgrid.dataSource

//		});
//});