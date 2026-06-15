//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.IO;
//using System.Data.OleDb;
//using System.Data;
//using APP.Components.Dto;
////using APP.Persistence.Common;
//using System.Text.RegularExpressions;
//using APP.Framework;
//using Microsoft.Office.Interop.Word;

//namespace App.BL
//{
//	public class ExcelImportHelper
//	{
//		public static string CovertExcelToDataBaseTable(string excelFilePath, string fileName,string extentionNname)
//		{

//            System.Data.DataTable dataTable;

//			if (extentionNname.ToLower () ==".csv")
//			{
//				dataTable = FileTools.ReadCSVFiletoDataTable(excelFilePath, true, ",");

//			}
//			else
//			{
//				// dataTable = CovertExcelToDataable(excelFilePath);



//				dataTable = ExcelImportExportBL.ExcelToDataTable(excelFilePath);

				
//				//Column_Remove

//			}


//			string datName = fileName.Substring(0, fileName.Length - extentionNname.Length);

//			//datName = DataSoureHelp.TableRegex.Replace(datName, "");


//			string dataBaseTableName = "UDF_" + datName;


//			DataColumn PrimaryKeyColumn = new DataColumn();
//			PrimaryKeyColumn.AutoIncrement = true;
//			PrimaryKeyColumn.ColumnName = datName + "ID";
//			dataTable.Columns.Add (PrimaryKeyColumn);
//			PrimaryKeyColumn.Caption = "PrimaryKey";
		
//			PrimaryKeyColumn.AutoIncrementSeed = 1;
//			PrimaryKeyColumn.AutoIncrementStep = 1;


//			DataColumn CraeteByIdColumn = new DataColumn();
//			CraeteByIdColumn.DataType = typeof(int);
//			CraeteByIdColumn.ColumnName = EmAppTableBuiltInColumn.AppCreatedByID.ToString();
//			dataTable.Columns.Add(CraeteByIdColumn);

//			DataColumn CreateDateColumn = new DataColumn();
//			CreateDateColumn.DataType = typeof(DateTime);
//			CreateDateColumn.ColumnName = EmAppTableBuiltInColumn.AppCreatedDate.ToString();
//			dataTable.Columns.Add(CreateDateColumn);


//			DataColumn ModifyedByIdColumn = new DataColumn();
//			ModifyedByIdColumn.DataType = typeof(int);
//			ModifyedByIdColumn.ColumnName = EmAppTableBuiltInColumn.AppModifiedByID.ToString();
//			dataTable.Columns.Add(ModifyedByIdColumn);


//			DataColumn ModifyedDateColumn = new DataColumn();
//			ModifyedDateColumn.DataType = typeof(DateTime);
//			ModifyedDateColumn.ColumnName = EmAppTableBuiltInColumn.AppModifiedDate.ToString();
//			dataTable.Columns.Add(ModifyedDateColumn);

//			foreach ( DataRow row in dataTable.Rows)
//			{

//				row[EmAppTableBuiltInColumn.AppCreatedByID.ToString()] = ServerContext.Instance.CurrentUid;
//				row[EmAppTableBuiltInColumn.AppCreatedDate.ToString()] = System.DateTime.UtcNow;
			

//			}







//			string createAble = DataSoureHelp. GetCreateTableSqlScript(dataBaseTableName, dataTable);
//			new DBInteractionBase().ExecuteNonQuery(createAble);

//			 string createPk =DataSoureHelp.GetPrimayKeyCreate(dataBaseTableName, dataTable);
//			if(! string.IsNullOrWhiteSpace (createPk))
//			{
//				new DBInteractionBase().ExecuteNonQuery(createPk);

//			}


//			// need to drop auto key

//			dataTable.Columns.Remove(PrimaryKeyColumn);

//			List<string> isnerList = DataSoureHelp.GenerateDataRowInsertStatement(dataBaseTableName, dataTable.AsEnumerable().ToList());

//			foreach (var rowinsert in isnerList)
//			{



//				new DBInteractionBase().ExecuteNonQuery(rowinsert);

//			}



//			DeleteTempFiles(excelFilePath);




//			return dataBaseTableName;

//		}

//		public static System.Data.DataTable CovertExcelToDataTable(string excelFilePath, int worksheetNumber = 1)
//		{
//			var cnnStr = String.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + excelFilePath + ";Extended Properties=Excel 12.0;");
//			var cnn = new OleDbConnection(cnnStr);

//			// get schema, then data
//			var dt = new System.Data.DataTable();
//			try
//			{
//				cnn.Open();
//				var schemaTable = cnn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
//				if (schemaTable.Rows.Count < worksheetNumber) throw new ArgumentException("The worksheet number provided cannot be found in the spreadsheet");
//				string worksheet = schemaTable.Rows[worksheetNumber - 1]["table_name"].ToString().Replace("'", "");
//				string sql = String.Format("select * from [{0}]", worksheet);
//				var da = new OleDbDataAdapter(sql, cnn);
//				da.Fill(dt);
//			}
//			catch (Exception e)
//			{
//				// ???
//				throw e;
//			}
//			finally
//			{
//				// free resources
//				cnn.Close();
//			}

//			return dt;

//		}
        
//		private static void WrtieDataTableToCsv(string csvOutputFile, System.Data.DataTable dt)
//		{
//			// write out CSV data
//			using (var wtr = new StreamWriter(csvOutputFile))
//			{
//				foreach (DataRow row in dt.Rows)
//				{
//					bool firstLine = true;
//					foreach (DataColumn col in dt.Columns)
//					{
//						if (firstLine)
//						{
//							wtr.Write(",");
//						}

//						else { firstLine = false; }
//						var data = row[col.ColumnName].ToString().Replace("\"", "\"\"");
//						wtr.Write(String.Format("\"{0}\"", data));
//					}
//					wtr.WriteLine();
//				}
//			}
//		}
//		private static void DeleteTempFiles(string folderPath)
//		{
//			try
//			{
//				DirectoryInfo dir = new DirectoryInfo(folderPath);

//				if (dir.Exists)
//				{
//					foreach (FileInfo file in dir.GetFiles())
//					{
//						try
//						{
//							file.Delete();
//						}
//						catch
//						{
//							continue;
//						}
//					}

//					dir.Delete();
//				}
//			}
//			catch (Exception ex)
//			{
//			}
//		}
//	}


//    public class OfficeDocumentHelper
//    {
//        public static void SaveWordDocumentAsHtml(object wordFilePath, object htmlFilePath)
//        {
//            //object missingType = Type.Missing;

//            //object readOnly = true;

//            //object isVisible = false;

//            //object documentFormat = 8;

//            //string randomName = DateTime.Now.Ticks.ToString();

//            //Microsoft.Office.Interop.Word.Application wordApp = new Microsoft.Office.Interop.Word.Application();

//            //wordApp.Documents.Open(ref wordFilePath,

//            //                                ref readOnly,

//            //                                ref missingType, ref missingType, ref missingType,

//            //                                ref missingType, ref missingType, ref missingType,

//            //                                ref missingType, ref missingType, ref isVisible,

//            //                                ref missingType, ref missingType, ref missingType,

//            //                                ref missingType, ref missingType);

//            //wordApp.Visible = false;

//            //Document document = wordApp.ActiveDocument;

//            //document.SaveAs(ref htmlFilePath, ref documentFormat, ref missingType,

//            //                ref missingType, ref missingType, ref missingType,

//            //                ref missingType, ref missingType, ref missingType,

//            //                ref missingType, ref missingType, ref missingType,

//            //                ref missingType, ref missingType, ref missingType,

//            //                ref missingType);

//            //document.Close(ref missingType, ref missingType, ref missingType);


//            //////Read the Html File as Byte Array and Display it on browser

//            ////byte[] bytes;

//            ////using (FileStream fs = new FileStream(htmlFilePath.ToString(), FileMode.Open, FileAccess.Read))
//            ////{
//            ////    BinaryReader reader = new BinaryReader(fs);
//            ////    bytes = reader.ReadBytes((int)fs.Length);
//            ////    fs.Close();
//            ////}          

//        }

//    }
//}
