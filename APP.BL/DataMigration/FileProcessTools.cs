using Com.Visual2000.SystemFramework;
using GemBox.Spreadsheet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace APP.BL
{
      
        public class FileProcessTools
    {

        private static void UploadImageToServer()
        {
            var directories = Directory.GetDirectories("C:\\Users\\CGS.REM\\Downloads\\testzip");


            var url = @"https://ws.api/files ";
            HttpClient httpClient = new HttpClient();

            foreach (string directory in directories)
            {



                string[] fileFullPaths = System.IO.Directory.GetFiles(directory, "*.jpg");

                // List<string> jsonFileList = new List<string>();
                foreach (string filePath in fileFullPaths)
                {

                    using (var requestContenct = new MultipartFormDataContent())
                    {
                        using (var fileStream = File.OpenRead(filePath))
                        {
                            string fileName = System.IO.Path.GetFileName(filePath);

                            var streamContent = new StreamContent(fileStream);
                            requestContenct.Add(streamContent, fileName, fileName);
                            httpClient.PostAsync(url, streamContent).Wait();
                        }
                    }

                }






            }



        }


        private static void MergerSingleJsonFileToOneJsonFile()
        {
            string[] files = System.IO.Directory.GetFiles(@"C:\UsersDownloads\testzip", "*.json");

            List<string> jsonFileList = new List<string>();
            foreach (string file in files)
            {
                string contents = File.ReadAllText(file);
                jsonFileList.Add(contents);


            }


            string profile = $@"
            {{
	            profiles:
                [
	 
	               {jsonFileList.Aggregate((i, j) => i + "," + System.Environment.NewLine + j)}
	 
	
	            ]
	
            }}
            ";

            File.WriteAllText(@"C:\Users\Downloads\allList.json", profile);
        }
        // will override all file
        public static void WriteOveWriteFile(List<string> listString, string filepath)
            {
                //  listJson = listJson.Select(str => str + Environment.NewLine).ToList ();

                //if (!File.Exists(filepath))
                //{
                //    File.Create(filepath);
                //}




                string createText = listString.Aggregate((i, j) => i + Environment.NewLine + j);


                File.WriteAllText(filepath, createText);

                // Open the file to read from. 
                // string readText = File.ReadAllText(path);

            }

            // will append the file

            public static void WriteAppendFile(List<string> listString, string filepath)
            {
                //  listJson = listJson.Select(str => str + Environment.NewLine).ToList ();

                if (!File.Exists(filepath))
                {
                    File.Create(filepath);
                }

                string createText = listString.Aggregate((i, j) => i + Environment.NewLine + j);

                File.AppendAllText(filepath, createText + Environment.NewLine); ;

                // File.WriteAllText(filepath, createText);

                // Open the file to read from. 
                // string readText = File.ReadAllText(path);

            }


            public static void WriteAppendFile(string valeuString, string filepath)
            {
                //  listJson = listJson.Select(str => str + Environment.NewLine).ToList ();

                if (!File.Exists(filepath))
                {
                    File.Create(filepath);
                }

                //   string createText = listString.Aggregate((i, j) => i + Environment.NewLine + j);

                File.AppendAllText(filepath, valeuString + Environment.NewLine); ;

                // File.WriteAllText(filepath, createText);

                // Open the file to read from. 
                // string readText = File.ReadAllText(path);

            }


            public static void ReadAllFileAsOneJson(string filepath)
            {


                JObject o1 = JObject.Parse(File.ReadAllText(filepath));

                using (StreamReader file = File.OpenText(filepath))
                {
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        JObject o2 = (JObject)JToken.ReadFrom(reader);
                    }

                }
            }

            public static IEnumerable<string> ReadFileAsLineList(string file)
            {
                string line;
                using (var reader = File.OpenText(file))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        yield return line;
                    }
                }
            }

            public static string ReadAllContext(string file)
            {
                string contents = File.ReadAllText(file);
                return contents;
            }

            public static void Movefile(string sourceFilePath, string destinationFile)
            {
                if (File.Exists(sourceFilePath))
                {
                    //if (File.Exists(destinationFile))
                    //{
                    //	File.Delete(destinationFile);

                    //}// remove guid
                    System.IO.File.Move(sourceFilePath, destinationFile);
                }





            }

            public static string CreateCsvFileFromDataTable(DataTable productDataTable,string filePath)
            {
                SpreadsheetInfo.SetLicense("E1H5-CMM5-01EP-4OKK");

                ExcelFile ef = new ExcelFile();

                ExcelWorksheet ws = ef.Worksheets.Add(productDataTable.TableName);


                // Insert DataTable into an Excel worksheet.

                InsertDataTableOptions aInsertDataTableOptions = new InsertDataTableOptions();
                aInsertDataTableOptions.ColumnHeaders = true;
                aInsertDataTableOptions.StartRow = 0;
                ws.InsertDataTable(productDataTable, aInsertDataTableOptions);

                var datetime = System.DateTime.Now;
            //77165 ( 
            string fileName = string.Format(productDataTable.TableName + ".csv");
                ef.Save(filePath + "\\" + fileName);







                return string.Format("{0} successfully generated ", fileName); ;
            }

            public static DataTable ReadCSVFiletoDataTable(string strFilePath, bool isFirstRowAsheaderColumn, string delimte)
            {

                string fileShorName = FileTools.GetFileName(strFilePath);

                DataTable returnDataTable = new DataTable();

                try
                {


                    using (StreamReader sr = new StreamReader(strFilePath))
                    {


                        int lineNumber = 1;
                        //Read first line
                        string[] headers = null;
                        string firstLine = string.Empty;
                        try
                        {
                            firstLine = sr.ReadLine();
                            headers = firstLine.Split(delimte.ToCharArray());

                            if (isFirstRowAsheaderColumn)
                            {

                                foreach (string header in headers)
                                {
                                    returnDataTable.Columns.Add(header);
                                }

                            }
                            else //not include first row
                            {
                                for (int i = 1; i <= headers.Length; i++)
                                {
                                    returnDataTable.Columns.Add("Column" + i);
                                }

                                DataRow dataRow = returnDataTable.NewRow();
                                returnDataTable.Rows.Add(dataRow);
                                for (int i = 0; i < headers.Length; i++)
                                {
                                    dataRow[i] = headers[i];

                                }

                            }

                        }
                        catch (Exception ex)
                        {
                            //string errorMessage = strFilePath + " Invalid format  Line: " + firstLine + " at line: " + lineNumber;
                            //PdmIntergrationTrackEntity entity = new PdmIntergrationTrackEntity();

                            //entity.LogDate = System.DateTime.UtcNow;


                            //entity.CriticalLevel = EmCriticalLevel.Error.ToString();
                            //entity.ErrorMessage = errorMessage;
                            //listTrackEntity.Add(entity);


                            //ApplicationLog.WriteError(errorMessage);
                        }

                        //  double check still there not column header 
                        if (returnDataTable.Columns.Count == 0)
                        {

                            //	string errorMessage = strFilePath + " Cannot read first line as header columns, file processing  exit ! ";



                         
                            return returnDataTable;
                        }

                        while (!sr.EndOfStream)
                        {
                            string lineString = string.Empty;


                            try
                            {
                                lineString = sr.ReadLine();

                                lineNumber++;

                                //   string[] cells = Regex.Split(lineString,   string.Format ( "{0}(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)" , delimte));

                                string[] cells = lineString.Split(delimte.ToCharArray());
                                DataRow dr = returnDataTable.NewRow();

                                if (cells.Length == headers.Length)
                                {
                                    for (int i = 0; i < headers.Length; i++)
                                    {
                                        dr[i] = cells[i];
                                    }
                                    returnDataTable.Rows.Add(dr);
                                }
                                else

                                {
                                    string errorMessage = fileShorName + " Invalid format  Line: " + lineString + " at line: " + lineNumber;
                                  

                                    ApplicationLog.WriteError(errorMessage);
                                }
                            }
                            catch (Exception ex)
                            {
                                


                                //strFilePath + " Line: " + lineString

                                string errorMessage = fileShorName + " Invalid format  Line: " + lineString + " at line: " + lineNumber;

                                

                                ApplicationLog.WriteError(errorMessage);

                            }



                        }

                    }

                }
                catch (Exception ex)
                {

                  ;

                }




                return returnDataTable;
            }


            public static DataTable ReadCSVStreamDateDataTable(string strFilePath, byte[] bytes, bool isFirstRowAsheaderColumn,  string delimte)
            {

                string fileShorName = FileTools.GetFileName(strFilePath);

                DataTable returnDataTable = new DataTable();

                try
                {

                    MemoryStream ms = new MemoryStream(bytes);

                    using (StreamReader sr = new StreamReader(ms))
                    {


                        int lineNumber = 1;
                        //Read first line
                        string[] headers = null;
                        string firstLine = string.Empty;
                        try
                        {
                            firstLine = sr.ReadLine();
                            headers = firstLine.Split(delimte.ToCharArray());

                            if (isFirstRowAsheaderColumn)
                            {

                                foreach (string header in headers)
                                {
                                    returnDataTable.Columns.Add(header);
                                }

                            }
                            else //not include first row
                            {
                                for (int i = 1; i <= headers.Length; i++)
                                {
                                    returnDataTable.Columns.Add("Column" + i);
                                }

                                DataRow dataRow = returnDataTable.NewRow();
                                returnDataTable.Rows.Add(dataRow);
                                for (int i = 0; i < headers.Length; i++)
                                {
                                    dataRow[i] = headers[i];

                                }

                            }

                        }
                        catch (Exception ex)
                        {
                            //string errorMessage = strFilePath + " Invalid format  Line: " + firstLine + " at line: " + lineNumber;
                            //PdmIntergrationTrackEntity entity = new PdmIntergrationTrackEntity();

                            //entity.LogDate = System.DateTime.UtcNow;


                            //entity.CriticalLevel = EmCriticalLevel.Error.ToString();
                            //entity.ErrorMessage = errorMessage;
                            //listTrackEntity.Add(entity);


                            //ApplicationLog.WriteError(errorMessage);
                        }

                        //  double check still there not column header 
                        if (returnDataTable.Columns.Count == 0)
                        {

                            //	string errorMessage = strFilePath + " Cannot read first line as header columns, file processing  exit ! ";



                            string errorMessage = fileShorName + " is empty file or cannot parse the first line  ";
                         
                            ApplicationLog.WriteError(errorMessage);

                            return returnDataTable;
                        }

                        while (!sr.EndOfStream)
                        {
                            string lineString = string.Empty;


                            try
                            {
                                lineString = sr.ReadLine();

                                lineNumber++;

                                //   string[] cells = Regex.Split(lineString,   string.Format ( "{0}(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)" , delimte));

                                string[] cells = lineString.Split(delimte.ToCharArray());
                                DataRow dr = returnDataTable.NewRow();

                                if (cells.Length == headers.Length)
                                {
                                    for (int i = 0; i < headers.Length; i++)
                                    {
                                        dr[i] = cells[i];
                                    }
                                    returnDataTable.Rows.Add(dr);
                                }
                                else

                                {
                                    string errorMessage = fileShorName + " Invalid format  Line: " + lineString + " at line: " + lineNumber;
                                  

                                    ApplicationLog.WriteError(errorMessage);
                                }
                            }
                            catch (Exception ex)
                            {
                              
                                //strFilePath + " Line: " + lineString

                                string errorMessage = fileShorName + " Invalid format  Line: " + lineString + " at line: " + lineNumber;

                              

                                ApplicationLog.WriteError(errorMessage);

                            }



                        }

                    }

                }
                catch (Exception ex)
                {

                   
                }




                return returnDataTable;
            }

        internal static DataTable ConvertExcelByteStreamToDataTable(string extension, int totalColumnCount, bool isUsedFirstRowAsCoumn, byte[] fielbyes)
        {
            DataTable dt = new DataTable();


            //  using (StreamReader sr = new StreamReader(strFilePath))

            Stream stm =  new  MemoryStream ( fielbyes);
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
                //var result = ExtractImage(ws,"I");

                dt = ConvertWorkSheetToDataTable(totalColumnCount, isUsedFirstRowAsCoumn, ws, true);

            }



            return dt;
        }

        private static DataTable ConvertWorkSheetToDataTable(int totalColumnCount, bool isUsedFirstRowAsCoumn, ExcelWorksheet ws, bool needToRemoveEmptyRow = true)
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
                    string firstRowAsColName = firstRow[i].ToString();

                    // for exsting Column if column name already exstis, 

                    if (dt.Columns.Contains(firstRowAsColName))
                    {

                        dt.Columns[i].ColumnName = firstRowAsColName + i;

                    }

                    else
                    {
                        dt.Columns[i].ColumnName = firstRowAsColName;
                    }


                }

                dt.Rows.Remove(firstRow);

            }

            if (needToRemoveEmptyRow)
            {
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
            }

            // need to remove empty data Row
            return dt;
        }

        public static string GetFileName(string filepath)
            {
                // Path.GetExtension(yourPath); // returns .exe
                //Path.GetFileNameWithoutExtension(yourPath); // returns File
                return Path.GetFileName(filepath);
            }

            public static string Getxtension(string filepath)
            {
                return Path.GetExtension(filepath); // returns .exe
                                                    //Path.GetFileNameWithoutExtension(yourPath); // returns File
                                                    // return Path.GetFileName(filepath);

                //string combinedPath = Path.Combine(basePath, filePath); 
            }

            public static string PathCombine(string basePath, string filePath)
            {


                return Path.Combine(basePath, filePath);
            }

            static void MainTest()
            {
                string fileName = "test.txt";
                string sourcePath = @"C:\Users\Public\TestFolder";
                string targetPath = @"C:\Users\Public\TestFolder\SubDir";

                // Use Path class to manipulate file and directory paths.
                string sourceFile = System.IO.Path.Combine(sourcePath, fileName);
                string destFile = System.IO.Path.Combine(targetPath, fileName);

                // To copy a folder's contents to a new location:
                // Create a new target folder, if necessary.
                if (!System.IO.Directory.Exists(targetPath))
                {
                    System.IO.Directory.CreateDirectory(targetPath);
                }

                // To copy a file to another location and 
                // overwrite the destination file if it already exists.
                System.IO.File.Copy(sourceFile, destFile, true);

                // To copy all the files in one directory to another directory.
                // Get the files in the source folder. (To recursively iterate through
                // all subfolders under the current directory, see
                // "How to: Iterate Through a Directory Tree.")
                // Note: Check for target path was performed previously
                //       in this code example.
                if (System.IO.Directory.Exists(sourcePath))
                {
                    string[] files = System.IO.Directory.GetFiles(sourcePath);

                    // Copy the files and overwrite destination files if they already exist.
                    foreach (string s in files)
                    {
                        // Use static Path methods to extract only the file name from the path.
                        fileName = System.IO.Path.GetFileName(s);
                        destFile = System.IO.Path.Combine(targetPath, fileName);
                        System.IO.File.Copy(s, destFile, true);
                    }
                }
                else
                {
                    Console.WriteLine("Source path does not exist!");
                }

                // Keep console window open in debug mode.
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }

            static void MoveFileTest()
            {
                string sourceFile = @"C:\Users\Public\public\test.txt";
                string destinationFile = @"C:\Users\Public\private\test.txt";

                // To move a file or folder to a new location:
                System.IO.File.Move(sourceFile, destinationFile);

                // To move an entire directory. To programmatically modify or combine
                // path strings, use the System.IO.Path class.
                System.IO.Directory.Move(@"C:\Users\Public\public\test\", @"C:\Users\Public\private");
            }


            public static void WriteAllBytesToFile(string fileName, byte[] arrBytes)
            {

                File.WriteAllBytes(fileName, arrBytes);

            }

            public static void DeleteFolder(string FolderName)
            {

                try
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(FolderName);

                    foreach (FileInfo file in directoryInfo.GetFiles())
                    {
                        file.Delete();
                    }

                    directoryInfo.Delete();
                }
                catch (Exception ex)
                {
                }



                /**
                 
                 EmptyFolder(new DirectoryInfo(@"C:\your Path"))


using System.IO; // dont forget to use this header

//Method to delete all files in the folder and subfolders

private void EmptyFolder(DirectoryInfo directoryInfo)
{
        foreach (FileInfo file in directoryInfo.GetFiles())
        {       
           file.Delete();
         }

        foreach (DirectoryInfo subfolder in directoryInfo.GetDirectories())
        {
          EmptyFolder(subfolder);
        }
}
                 
                 **/

            }

            public static double ConvertToUnixTimestamp(DateTime value)
            {
                //create Timespan by subtracting the value provided from
                //the Unix Epoch
                TimeSpan span = (value - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());

                //return the total seconds (which is a UNIX timestamp)
                return (double)span.TotalSeconds;
            }


            public static string GetCurrnetUnixTimestampString()
            {



                return ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();
            }
        }
   
}
