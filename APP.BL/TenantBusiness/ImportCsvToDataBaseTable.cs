using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;


using System.IO;
using GemBox.Spreadsheet;
using App.BL;

namespace NewLookExchange
{

    public class AppImportExportExcelToDataTableBL
    {

        public static DataTable ConvertCsvFileToDataTable(byte[] fielbyes, int totalColumnCount, string extension, bool isUsedFirstRowAsCoumn)
        {
            DataTable dt = new DataTable();




            Stream stm = new MemoryStream(fielbyes);//        StreamHelper.ByteArrayToStream(fielbyes);
                                                    //https://www.gemboxsoftware.com/spreadsheet/help/html/T_GemBox_Spreadsheet_ExtractToDataTableOptions.htm
                                                    //xlsx



            ExcelFile ef = null;

            SpreadsheetInfo.SetLicense("E1H5-CMM5-01EP-4OKK");

            if (extension.EndsWith("xlsx", StringComparison.InvariantCultureIgnoreCase))
            {
                ef = ExcelFile.Load(stm, GemBox.Spreadsheet.LoadOptions.XlsxDefault);
            }
            else if (extension.EndsWith("xls", StringComparison.InvariantCultureIgnoreCase))
            {
                ef = ExcelFile.Load(stm, GemBox.Spreadsheet.LoadOptions.XlsDefault);
            }
            else if (extension.EndsWith("csv", StringComparison.InvariantCultureIgnoreCase))
            {
                ef = ExcelFile.Load(stm, GemBox.Spreadsheet.LoadOptions.CsvDefault);
            }

            if (ef != null)
            {
                ExcelWorksheet ws = ef.Worksheets[0];
                dt = ConvertWorkSheetToDataTable(totalColumnCount, isUsedFirstRowAsCoumn, ws);

            }

            return dt;
        }

        public static DataTable ConvertCsvFileToDataTable(string strFilePath, int totalColumnCount, string extension, bool isUsedFirstRowAsCoumn)
        {
            byte[] fielbytes = StreamHelper.FileToByteArray(strFilePath);

            return ConvertCsvFileToDataTable(fielbytes, totalColumnCount, extension, isUsedFirstRowAsCoumn);
        }


        public static byte[] ExportDataTableToCSv(DataTable dt)
        {
            // If using Professional version, put your serial key below.
            SpreadsheetInfo.SetLicense("E1H5-CMM5-01EP-4OKK");

            ExcelFile ef = new ExcelFile();
            ExcelWorksheet ws = ef.Worksheets.Add("DataTable to Sheet");

            //DataTable dt = new DataTable();

            //dt.Columns.Add("ID", typeof(int));
            //dt.Columns.Add("FirstName", typeof(string));
            //dt.Columns.Add("LastName", typeof(string));

            //dt.Rows.Add(new object[] { 100, "John", "Doe" });
            //dt.Rows.Add(new object[] { 101, "Fred", "Nurk" });
            //dt.Rows.Add(new object[] { 103, "Hans", "Meier" });
            //dt.Rows.Add(new object[] { 104, "Ivan", "Horvat" });
            //dt.Rows.Add(new object[] { 105, "Jean", "Dupont" });
            //dt.Rows.Add(new object[] { 106, "Mario", "Rossi" });

            //ws.Cells[0, 0].Value = "DataTable insert example:";

            // Insert DataTable into an Excel worksheet.
            ws.InsertDataTable(dt,
                new InsertDataTableOptions()
                {
                    ColumnHeaders = true,
                    StartRow = 0
                });

            using (MemoryStream ms = new MemoryStream())
            {
                ef.Save(ms, SaveOptions.XlsDefault);


                return ms.ToArray();
            }




            //ef.Save("DataTable to Sheet.xlsx");


        }

        public static DataTable ConvertWorkSheetToDataTable(int totalColumnCount, bool isUsedFirstRowAsCoumn, ExcelWorksheet ws)
        {

            DataTable dt = new DataTable();

            for (int j = 0; j < totalColumnCount; j++)
            {
                dt.Columns.Add(j.ToString());

            }




            ExtractToDataTableOptions options = new ExtractToDataTableOptions(0, 0, ws.Rows.Count);
            ws.ExtractToDataTable(dt, options);


            if (dt.Rows.Count > 0 && isUsedFirstRowAsCoumn)
            {
                DataRow firstRow = dt.Rows[0];
                for (int i = 0; i < totalColumnCount; i++)
                {
                    dt.Columns[i].ColumnName = firstRow[i].ToString();

                }

                dt.Rows.Remove(firstRow);

            }

            List<DataRow> emptyRowList = new List<DataRow>();
            foreach (DataRow row in dt.Rows)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    sb.Append(row[i]);

                }

                string allValue = sb.ToString();
                if (string.IsNullOrWhiteSpace(allValue))
                {
                    emptyRowList.Add(row);
                }


            }

            foreach (DataRow emptyRow in emptyRowList)
            {
                dt.Rows.Remove(emptyRow);
            }
            // need to remove empty data Row
            return dt;
        }




    }

}
