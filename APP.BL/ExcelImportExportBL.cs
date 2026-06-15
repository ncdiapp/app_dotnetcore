using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using APP.Components.Dto;
using APP.Framework;
using System.Data;
#if NETFRAMEWORK
using SpreadsheetGear;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif
using NewLookExchange;
using System.Text.RegularExpressions;
//using APP.Persistence.Common;
using APP.LBL.DatabaseSpecific;
using System.Data.SqlClient;
using APP.Components.EntityDto;
using APP.LBL.EntityClasses;
using DatabaseSchemaMrg;
using GemBox.Spreadsheet;
using System.Runtime.InteropServices.ComTypes;


namespace App.BL
{
    public class ExcelImportExportBL
    {
        public static Byte[] DataTableToExcel()
        {
#if NETFRAMEWORK
            SpreadsheetGear.IWorkbookSet workbookSet = SpreadsheetGear.Factory.GetWorkbookSet();

            SpreadsheetGear.IWorkbook workbook = workbookSet.Workbooks.Add();



            throw new NotImplementedException();

            //   return workbook.SaveToMemory(SpreadsheetGear.FileFormat.Excel8);
#else
            // TODO-PHASE4: Replace with .NET 10 equivalent
            throw new PlatformNotSupportedException("TODO-PHASE4: Migrate to .NET 10 equivalent");
#endif
        }


        //public static DataTable ExcelToDataTable(string excelFilePath)
        //{
        //    byte[] rawFileData = File.ReadAllBytes(excelFilePath);
        //    return ExcelToDataTable(rawFileData);



        //    //   return workbook.SaveToMemory(SpreadsheetGear.FileFormat.Excel8);
        //}

        //public static DataTable ExcelToDataTable(Stream inputStream)
        //{
        //    byte[] rawFileData = ReadFullStreamToBytes(inputStream);

        //    return ExcelToDataTable(rawFileData);
        //    //   return workbook.SaveToMemory(SpreadsheetGear.FileFormat.Excel8);
        //}

        public static byte[] ReadFullStreamToBytes(Stream inputStream)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static DataTable ExcelToDataTable(byte[] rawFileData)
        {
#if NETFRAMEWORK
            DataTable dataTable = new DataTable();

            IWorkbook workbook = SpreadsheetGear.Factory.GetWorkbookSet().Workbooks.OpenFromMemory(rawFileData);

            if (workbook != null && workbook.Worksheets != null)
            {
                foreach (IWorksheet worksheet in workbook.Worksheets)
                {
                    IRange range = worksheet.Cells;

                    //   int unitsCol = 0;

                    for (int i = 1; i <= range.RowCount - 1; i++)
                    {
                        bool isRowHidden = true;

                        foreach (SpreadsheetGear.IRange units in range[i, 0, i, 30])
                        {
                            string val = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(units.Value);

                            if (!string.IsNullOrEmpty(val))
                            {
                                //units.Rows.Hidden = true;
                                isRowHidden = false;
                                break;
                            }
                        }

                        foreach (SpreadsheetGear.IRange units in range[i, 0, i, 0])
                        {
                            units.Rows.Hidden = isRowHidden;
                        }
                    }


                    dataTable = GetDataTable(range);
                    break;
                }
            }

            return dataTable;
#else
            // TODO-PHASE4: Replace with .NET 10 equivalent
            throw new PlatformNotSupportedException("TODO-PHASE4: Migrate to .NET 10 equivalent");
#endif
        }

        private static DataTable CsvToDataTable(byte[] rawFileData, char delimiter = ',')
        {
            DataTable dataTable = new DataTable();

            using (MemoryStream stream = new MemoryStream(rawFileData))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                bool isHeaderRow = true;
                string line;

                while ((line = reader.ReadLine()) != null)
                {                   
                    var fields = ParseCsvLine(line, delimiter);

                    if (isHeaderRow)
                    {                       
                        foreach (string field in fields)
                        {
                            dataTable.Columns.Add(field.Trim());
                        }
                        isHeaderRow = false;
                    }
                    else
                    {                     
                        DataRow row = dataTable.NewRow();
                        for (int i = 0; i < fields.Length; i++)
                        {
                            row[i] = fields[i];
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }

            return dataTable;
        }

        private static string[] ParseCsvLine(string line, char delimiter)
        {
            var pattern = new StringBuilder();
            pattern.Append(@"(?:^|");
            pattern.Append(Regex.Escape(delimiter.ToString()));
            pattern.Append(@")(?:\""([^\""]*)\""|([^""]*))");

            var regex = new Regex(pattern.ToString(), RegexOptions.Compiled);
            var matches = regex.Matches(line);

            var fields = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                fields[i] = matches[i].Groups[1].Success ? matches[i].Groups[1].Value : matches[i].Groups[2].Value;
            }

            return fields;
        }

        public static string ImportExcelContentToDbTable(byte[] fileContent, string fileName, int? importSettingDataSetId, int? dataSourceRegisterId)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);
            DataTable excelDataTable = new DataTable();

            char? csvSpecialDelimiter = null;

            if (fileName.ToLower().EndsWith(".csv"))
            {
                csvSpecialDelimiter = DetectCsvFileSpecialDelimiter(fileContent);
            }
           
            if (csvSpecialDelimiter.HasValue)
            {
                excelDataTable = ExcelImportExportBL.CsvToDataTable(fileContent, csvSpecialDelimiter.Value);
            }
            else
            {
                excelDataTable = ExcelImportExportBL.ExcelToDataTable(fileContent);
            }

            string tableName = Regex.Replace(fileName, @"[^0-9a-zA-Z]+", "_");
            foreach (DataColumn column in excelDataTable.Columns)
            {
                column.ColumnName = Regex.Replace(column.ColumnName, @"[^0-9a-zA-Z]+", "_");
            }



            //string tableScript = DataSoureHelp.GetCreateTableSqlScript(tableName, excelDataTable);


            if (importSettingDataSetId.HasValue)
            {
                DatabaseTableImportSettingDto importSettingDto = AppDatabaseTableImportBL.RetrieveOneTableImportSettingDto(importSettingDataSetId.Value);

                DataTable matchDataTable = ConvertDataTableByImportSetting(importSettingDto, excelDataTable);



                if (databaseFixtureInstance.IsTableExist(importSettingDto.OrgTempTableName))
                {
                    string truncateTableScript = new SqlWriter(importSettingDto.OrgTempTableName, databaseFixtureInstance.SqlServerType.Value).TruncateTable();

                    databaseFixtureInstance.ExecuteNonQueryResult(truncateTableScript, new List<System.Data.Common.DbParameter>());

                    databaseFixtureInstance.BulkCopyDataTable(matchDataTable, importSettingDto.OrgTempTableName);
                }
                else
                {
                    var sqlWriter = new SqlWriter(importSettingDto.OrgTempTableName, databaseFixtureInstance.SqlServerType.Value);

                    string tableScript = sqlWriter.GenerateCreateTableQueryFromDataTable(matchDataTable);

                    ImportExcelContentToDbTable_ExecuteTableCreateQuery(importSettingDto.OrgTempTableName, tableScript, databaseFixtureInstance);

                    databaseFixtureInstance.BulkCopyDataTable(matchDataTable, importSettingDto.OrgTempTableName);
                }



                return importSettingDto.OrgTempTableName;
            }
            else
            {
                tableName = CreateNewTableAndImportData(databaseFixtureInstance, excelDataTable, tableName);

                return tableName;
            }



        }

        private static char? DetectCsvFileSpecialDelimiter(byte[] fileContent)
        {
            char? csvSpecialDelimiter = null;


            using (MemoryStream stream = new MemoryStream(fileContent))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line = reader.ReadLine();

                if (line.IndexOf(',') > 0)
                {
                    csvSpecialDelimiter = null;
                }
                else if (line.IndexOf(';') > 0)
                {
                    csvSpecialDelimiter = ';';
                }
                else if (line.IndexOf('\t') > 0)
                {
                    csvSpecialDelimiter = '\t';
                }
                else if (line.IndexOf('|') > 0)
                {
                    csvSpecialDelimiter = '|';
                }
            }


            return csvSpecialDelimiter;
        }

        private static string CreateNewTableAndImportData(DatabaseFixture databaseFixtureInstance, DataTable excelDataTable, string tableName)
        {
            tableName = "ImportExcelStaging_" + ExtensionMethodhelper.RandomId() + "_" + tableName;

            DataTable matchDataTable = ControlTypeValueConverter.TryConvertDataTableToMatchDataType(excelDataTable);

            var sqlWriter = new SqlWriter(tableName, databaseFixtureInstance.SqlServerType.Value);

            string tableScript = sqlWriter.GenerateCreateTableQueryFromDataTable(matchDataTable);

            ImportExcelContentToDbTable_ExecuteTableCreateQuery(tableName, tableScript, databaseFixtureInstance);

            databaseFixtureInstance.BulkCopyDataTable(matchDataTable, tableName);
            return tableName;
        }

        private static DataTable ConvertDataTableByImportSetting(DatabaseTableImportSettingDto importSettingDto, DataTable excelDataTable)
        {

            DataTable matchDataTable = new DataTable();

            if (!string.IsNullOrWhiteSpace(importSettingDto.OrgTempTableName) && importSettingDto.SourceColumns != null)
            {
                Dictionary<string, object> dictColumnAndTag = importSettingDto.SourceColumns.ToDictionary(o => o.Name, o => o.Tag);


                foreach (var sourceColumn in importSettingDto.SourceColumns)
                {
                    DataColumn dataColumn = new DataColumn(sourceColumn.Name);
                    matchDataTable.Columns.Add(dataColumn);
                    dataColumn.DataType = typeof(string);

                    if (sourceColumn.Tag != null && !string.IsNullOrWhiteSpace(sourceColumn.Tag.ToString()))
                    {
                        if (sourceColumn.Tag.ToString() == EmAppDataType.DateTime.ToString())
                        {
                            dataColumn.DataType = typeof(DateTime);
                        }
                        else if (sourceColumn.Tag.ToString() == EmAppDataType.Date.ToString())
                        {
                            dataColumn.DataType = typeof(DateTime);
                        }
                        else if (sourceColumn.Tag.ToString() == EmAppDataType.Boolean.ToString())
                        {
                            dataColumn.DataType = typeof(bool);
                        }
                        else if (sourceColumn.Tag.ToString() == EmAppDataType.BigInt.ToString())
                        {
                            dataColumn.DataType = typeof(Int64);
                        }
                        else if (sourceColumn.Tag.ToString() == EmAppDataType.Integer.ToString())
                        {
                            dataColumn.DataType = typeof(int);
                        }
                        else if (sourceColumn.Tag.ToString() == EmAppDataType.UInt16.ToString())
                        {
                            dataColumn.DataType = typeof(UInt16);
                        }
                        else if (sourceColumn.Tag.ToString() == EmAppDataType.UInt32.ToString())
                        {
                            dataColumn.DataType = typeof(UInt32);
                        }
                        else if (sourceColumn.Tag.ToString() == EmAppDataType.UInt64.ToString())
                        {
                            dataColumn.DataType = typeof(UInt64);
                        }
                        else if (sourceColumn.Tag.ToString() == EmAppDataType.Decimal.ToString())
                        {
                            dataColumn.DataType = typeof(Decimal);
                        }
                        else if (sourceColumn.Tag.ToString() == EmAppDataType.Blob.ToString())
                        {
                            dataColumn.DataType = typeof(byte[]);
                        }
                    }
                }
                foreach (DataRow row in excelDataTable.Rows)
                {
                    DataRow newRow = matchDataTable.NewRow();
                    matchDataTable.Rows.Add(newRow);
                    foreach (DataColumn column in matchDataTable.Columns)
                    {
                        if (row[column.ColumnName] != null)
                        {
                            if (Enum.TryParse(dictColumnAndTag[column.ColumnName].ToString(), out EmAppDataType parsedEnum))
                            {
                                newRow[column.ColumnName] = ControlTypeValueConverter.ConverStringValueToSpecificDataType(row[column.ColumnName].ToString(), parsedEnum);
                            }

                        }
                    }
                }


            }

            return matchDataTable;
        }

        private static void ImportExcelContentToDbTable_ExecuteTableCreateQuery(string tableName, string tableScript, DatabaseFixture databaseFixtureInstance)
        {

            string droptable = new SqlWriter(tableName, databaseFixtureInstance.SqlServerType.Value).DropTableIfExist();


            databaseFixtureInstance.ExecuteNonQueryResult(droptable, new List<System.Data.Common.DbParameter>());
            databaseFixtureInstance.ExecuteNonQueryResult(tableScript, new List<System.Data.Common.DbParameter>());

        }


        private static void ImportExcelContentToDbTable_ExecuteTableCreateQuery(string tableName, string tableScript, DataAccessAdapter adapter)
        {
            string droptable = "IF OBJECT_ID('" + tableName + "', 'U') IS NOT NULL   DROP TABLE " + tableName;
            adapter.ExecuteExecuteNonQuery(droptable, new List<SqlParameter>());
            adapter.ExecuteExecuteNonQuery(tableScript, new List<SqlParameter>());
        }



#if NETFRAMEWORK
        public static DataTable GetDataTable(SpreadsheetGear.IRange range)
        {
            // TODO-PHASE4: Replace with .NET 10 equivalent
            SpreadsheetGear.IWorksheet worksheet = range.Worksheet;
            SpreadsheetGear.IRange cells = worksheet.Cells;
            DataTable dataTable = new DataTable();

            int rowCount = range.RowCount;
            int row1 = range.Row;
            int row2 = row1 + rowCount - 1;
            int col1 = range.Column;
            int col2 = col1 + range.ColumnCount - 1;
            int colCount = 0;

            for (int col = col1; col <= col2; col++)
            {
                string colName = cells[row1, col].Text;

                if (!string.IsNullOrEmpty(colName))
                {
                    dataTable.Columns.Add(colName);
                    colCount++;
                }
                else
                {
                    break;
                }
            }

            col2 = col1 + colCount - 1;

            foreach (System.Data.DataColumn dataCol in dataTable.Columns)
            {
                dataCol.DataType = typeof(string);
            }

            string[] rowData = new string[colCount];

            for (int row = 1; row <= row2; row++)
            {
                if (!cells[row, 0].Rows.Hidden)
                {
                    for (int col = col1; col <= col2; col++)
                    {
                        string text = cells[row, col].Text;
                        rowData[col - col1] = text;
                    }
                    dataTable.Rows.Add(rowData);
                }
            }

            return dataTable;
        }
#endif


        //public static DataTable GetDataTable(SpreadsheetGear.IRange range, SpreadsheetGear.Data.GetDataFlags flags)
        //{
        //    // Get a reference to the worksheet.
        //    SpreadsheetGear.IWorksheet worksheet = range.Worksheet;

        //    // Get a reference to all the worksheet cells.
        //    SpreadsheetGear.IRange cells = worksheet.Cells;

        //    // Get a reference to the advanced API.
        //    SpreadsheetGear.Advanced.Cells.IValues values =
        //        (SpreadsheetGear.Advanced.Cells.IValues)worksheet;

        //    // Create a new DataTable.
        //    DataTable dataTable = new DataTable();

        //    // Determine the row and column coordinates of the range.
        //    int row1 = range.Row;
        //    int col1 = range.Column;
        //    int rowCount = range.RowCount;
        //    //int colCount = range.ColumnCount;
        //    int colCount = 200;
        //    int row2 = row1 + rowCount - 1;
        //    int col2 = col1 + colCount - 1;
        //    int row = row1;

        //    // If the first row is not used for column headers...
        //    if ((flags & SpreadsheetGear.Data.GetDataFlags.NoColumnHeaders) != 0)
        //    {
        //        // Create columns using simple column names.
        //        for (int col = col1; col <= col2; col++)
        //        {
        //            string colName = "Column_Remove" + (col - col1 + 1);
        //            dataTable.Columns.Add(colName);
        //        }
        //    }
        //    else
        //    {
        //        // Create columns using the first row in the range for column names.

        //        int notEmptyColumnCount = 0;

        //        for (int col = col1; col <= col2; col++)
        //        {
        //            // Use the IRange API to get formatted text.
        //            string colName = cells[row, col].Text;

        //            if (!string.IsNullOrEmpty(colName))
        //            {
        //                notEmptyColumnCount++;
        //                dataTable.Columns.Add(colName);
        //            }
        //            else {
        //                break;
        //            }

        //        }
        //        colCount = notEmptyColumnCount;
        //        col2 = col1 + notEmptyColumnCount - 1;
        //        row++;
        //    }

        //    // If the DataTable column data types should be set...
        //    if ((flags & SpreadsheetGear.Data.GetDataFlags.NoColumnTypes) == 0 && row <= row2)
        //    {
        //        for (int col = col1; col <= col2; col++)
        //        {
        //            // Get a reference to the DataTable column.
        //            System.Data.DataColumn dataCol = dataTable.Columns[col - col1];

        //            // If formatted text is to be used for all cell values...
        //            if ((flags & SpreadsheetGear.Data.GetDataFlags.FormattedText) != 0)
        //            {
        //                // Set the data type to a string.
        //                dataCol.DataType = typeof(string);
        //            }
        //            else
        //            {
        //                // Set the data type based on the type of data in the cell.
        //                //
        //                // Note that this will cause problems if a column does not contain
        //                // consistent data types - for example a column of formulas where
        //                // the first is numeric but one of the following is an error.
        //                SpreadsheetGear.Advanced.Cells.IValue value = values[row, col];
        //                if (value != null)
        //                {
        //                    switch (value.Type)
        //                    {
        //                        case SpreadsheetGear.Advanced.Cells.ValueType.Number:
        //                            dataCol.DataType = typeof(double);
        //                            break;
        //                        case SpreadsheetGear.Advanced.Cells.ValueType.Text:
        //                        case SpreadsheetGear.Advanced.Cells.ValueType.Error:
        //                            dataCol.DataType = typeof(string);
        //                            break;
        //                        case SpreadsheetGear.Advanced.Cells.ValueType.Logical:
        //                            dataCol.DataType = typeof(bool);
        //                            break;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    // If formatted text is to be used for all cell values...
        //    if ((flags & SpreadsheetGear.Data.GetDataFlags.FormattedText) != 0)
        //    {
        //        // Create the row data as an array of strings.
        //        string[] rowData = new string[colCount];
        //        for (row = 0; row <= row2; row++)
        //        {
        //            // If the row is not hidden...
        //            if (!cells[row, 0].Rows.Hidden)
        //            {
        //                for (int col = col1; col <= col2; col++)
        //                {
        //                    // Use the IRange API to get formatted text.
        //                    string text = cells[row, col].Text;
        //                    rowData[col - col1] = text;
        //                }

        //                // Add a new row using the array of formatted strings.
        //                dataTable.Rows.Add(rowData);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // Create the row data as an array of objects.
        //        object[] rowData = new object[colCount];
        //        for (; row <= row2; row++)
        //        {
        //            // If the row is not hidden...
        //            if (!cells[row, 0].Rows.Hidden)
        //            {
        //                for (int col = col1; col <= col2; col++)
        //                {
        //                    // Use the advanced API to get the raw data values.
        //                    SpreadsheetGear.Advanced.Cells.IValue value = values[row, col];
        //                    object obj = null;
        //                    if (value != null)
        //                    {
        //                        switch (value.Type)
        //                        {
        //                            case SpreadsheetGear.Advanced.Cells.ValueType.Number:
        //                                obj = value.Number;
        //                                break;
        //                            case SpreadsheetGear.Advanced.Cells.ValueType.Text:
        //                                obj = value.Text;
        //                                break;
        //                            case SpreadsheetGear.Advanced.Cells.ValueType.Logical:
        //                                obj = value.Logical;
        //                                break;
        //                            case SpreadsheetGear.Advanced.Cells.ValueType.Error:
        //                                // This will create problems if it is a column type
        //                                // of double or bool.
        //                                obj = "#" + value.Error.ToString().ToUpper() + "!";
        //                                break;
        //                        }
        //                    }
        //                    rowData[col - col1] = obj;
        //                }

        //                // Add a new row using the array of objects.
        //                dataTable.Rows.Add(rowData);
        //            }
        //        }
        //    }

        //    // Return the DataTable.
        //    return dataTable;
        //}

        //https://msdn.microsoft.com/en-us/library/yfxbc3by%28v=vs.110%29.aspx
        public DataTable ReadXML(string file, List<string> columnNames)
        {
            //create the DataTable that will hold the data
            DataTable table = new DataTable("XmlData");

            foreach (string columName in columnNames)
            {
                table.Columns.Add(columName);
            }


            try
            {
                //open the file using a Stream
                using (Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {


                    //use ReadXml to read the XML stream
                    table.ReadXml(stream);

                    //return the results
                    return table;
                }
            }
            catch (Exception ex)
            {
                return table;
            }
        }



        public static List<LookupItemDto> GetExcelFileColumnNameList(string excelFilePath)
        {
            List<LookupItemDto> columnNameList = new List<LookupItemDto>();

            if (!string.IsNullOrWhiteSpace(excelFilePath))
            {
                DataTable dt = AppImportExportExcelToDataTableBL.ConvertCsvFileToDataTable(excelFilePath, 3, "xls", true);

                if (dt.Rows.Count > 0 && dt.Columns.Count > 0)
                {

                    foreach (DataColumn aColumn in dt.Columns)
                    {
                        LookupItemDto lookupItem = new LookupItemDto();
                        lookupItem.Id = aColumn.ColumnName;
                        lookupItem.Display = aColumn.ColumnName;
                        columnNameList.Add(lookupItem);
                    }
                }
            }

            return columnNameList;
        }

    }
}
