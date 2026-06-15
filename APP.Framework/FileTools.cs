//using Com.Visual2000.SystemFramework;
using APP.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

public class FileTools
{
	// will override all file
	public static void WriteOveWriteFile(List<string> listString, string filepath)
	{
		string createText = listString.Aggregate((i, j) => i + Environment.NewLine + j);
		File.WriteAllText(filepath, createText);
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
				catch (Exception )
				{
					string errorMessage = strFilePath + " Invalid format  Line: " + firstLine + " at line: " + lineNumber;

                    AppLogger.Error(errorMessage);
				}

				//  double check still there not column header
				if (returnDataTable.Columns.Count == 0)
				{
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

                            AppLogger.Error(errorMessage);
						}
					}
					catch (Exception ex)
					{
						string errorMessage = fileShorName + " Invalid format  Line: " + lineString + " at line: " + lineNumber;
                        AppLogger.Error(errorMessage);
					}
				}
			}
		}
		catch (Exception ex)
		{
		}

		return returnDataTable;
	}

	public static DataTable ReadCSVStreamDateDataTable(string strFilePath, byte[] bytes, bool isFirstRowAsheaderColumn, string delimte)
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
				
				}

				//  double check still there not column header
				if (returnDataTable.Columns.Count == 0)
				{
					//	string errorMessage = strFilePath + " Cannot read first line as header columns, file processing  exit ! ";

					string errorMessage = fileShorName + " is empty file or cannot parse the first line  ";

                    //If you remember we agreed to accept empty files – so that they can track that files were always sent at the agreed times – even if they were empty – to track any missing time slots

                    AppLogger.Error(errorMessage);

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

                            AppLogger.Error(errorMessage);
						}
					}
					catch (Exception ex)
					{
						//strFilePath + " Line: " + lineString

						string errorMessage = fileShorName + " Invalid format  Line: " + lineString + " at line: " + lineNumber;

                        AppLogger.Error(errorMessage);
					}
				}
			}
		}
		catch (Exception ex)
		{
		}

		return returnDataTable;
	}

	public static List<string> ReadAllFileInOneFolder(string folderpath, string extension)
	{
		// Summary:
		//     Returns an enumerable collection of file names that match a search pattern
		//     in a specified path.
		//
		// Parameters:
		//   path:
		//     The directory to search.
		//
		//   searchPattern:
		//     The search string to match against the names of directories in path.
		//
		// Returns:
		//     An enumerable collection of file names in the directory specified by path
		//     and that match searchPattern.
		return Directory.EnumerateFiles(folderpath, extension).ToList();
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

	private static void MainTest()
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

	private static void MoveFileTest()
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