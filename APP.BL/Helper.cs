using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Components.Dto;
using System.Diagnostics;
using Newtonsoft.Json;

namespace App.BL
{

    public static class StreamHelper
    {
        public static Image ByteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = null;
            if (ms.Length > 0)
            {
                returnImage = Image.FromStream(ms);
            }

            return returnImage;
        }

        public static byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                return ms.ToArray();
            }
        }

        public static byte[] FileToByteArray(System.Drawing.Image imageIn)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                return ms.ToArray();
            }
        }

        public static byte[] StreamToByteArray(System.IO.Stream stream)
        {
            stream.Position = 0;

            int streamLength = Convert.ToInt32(stream.Length);

            byte[] fileData = new byte[streamLength + 1];

            stream.Read(fileData, 0, streamLength);

            stream.Position = 0;

            return fileData;
        }

        public static byte[] FileToByteArray(string fileName)
        {
            try
            {
                return File.ReadAllBytes(fileName);
            }
            catch
            {
                return null;
            }

        }



        public static Stream ByteArrayToStream(byte[] byteArrayIn)
        {
            MemoryStream stream = new MemoryStream();

            stream.Write(byteArrayIn, 0, byteArrayIn.Length);

            stream.Position = 0;

            return stream;
        }

        //http://codex.wordpress.org/Function_Reference/get_allowed_mime_types
        public static EmAppDocumentType GetImageTypeFromImage(Image image)
        {

            // System.Drawing.Image image = StreamHelper.ByteArrayToImage(sketchDto.SketchImage);

            //  if( image.RawFormat.Guid  

            // ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())

            // image.
            //    ImageCodecInfo.GetImageDecoders()

            string codeFormat = string.Empty;

            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())
            {
                if (codec.FormatID == image.RawFormat.Guid)
                {
                    codeFormat = codec.MimeType;
                }


            }

            //JPG = 1, GIF = 2, BMP = 3, TIF
            if (codeFormat != string.Empty)
            {
                if (codeFormat.ToLowerInvariant() == "image/jpeg")
                {
                    return EmAppDocumentType.JPG;
                }
                if (codeFormat.ToLowerInvariant() == "image/gif")
                {
                    return EmAppDocumentType.GIF;
                }
                if (codeFormat.ToLowerInvariant() == "image/png")
                {
                    return EmAppDocumentType.PNG;
                }
                if (codeFormat.ToLowerInvariant() == "image/bmp")
                {
                    return EmAppDocumentType.BMP;
                }
                if (codeFormat.ToLowerInvariant() == "image/tiff")
                {
                    return EmAppDocumentType.TIF;
                }



            }
            return EmAppDocumentType.Unknown;




        }





        public static String ConvertImageToBase64String(String imagePath)
        {
            using (var img = System.Drawing.Image.FromFile(imagePath))
            {
                using (var memStream = new MemoryStream())
                {
                    img.Save(memStream, img.RawFormat);
                    byte[] imageBytes = memStream.ToArray();

                    // Convert byte[] to Base64 String
                    string base64String = Convert.ToBase64String(imageBytes);
                    return base64String;
                }
            }
        }

        public static System.Drawing.Image Base64StringToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            var memStream = new MemoryStream(imageBytes, 0, imageBytes.Length);

            memStream.Write(imageBytes, 0, imageBytes.Length);
            var image = System.Drawing.Image.FromStream(memStream);
            return image;
        }


    }

    public class ImageResizeHelper
    {
        private Size _newSize;
        private Size _fitSize;
        private Image _originalImage;

        public ImageResizeHelper()
        {
        }

        public ImageResizeHelper(Image originalImage, Size newSize)
        {
            _originalImage = originalImage;
            _newSize = newSize;
        }

        public static Stream ByteArrayToStream(byte[] byteArrayIn)
        {
            MemoryStream stream = new MemoryStream();

            stream.Write(byteArrayIn, 0, byteArrayIn.Length);

            stream.Position = 0;

            return stream;
        }

        public Size FitSize
        {
            get
            {
                if (this._fitSize.IsEmpty)
                {
                    this._fitSize = new Size();
                    double imgRatio = (double)_originalImage.Width / _originalImage.Height;
                    double W_Ratio = (double)_originalImage.Width / _newSize.Width;
                    double H_Ratio = (double)_originalImage.Height / _newSize.Height;

                    if (W_Ratio < 1 && H_Ratio < 1)
                    {
                        _fitSize.Width = _originalImage.Width;
                        _fitSize.Height = _originalImage.Height;
                    }
                    else if (W_Ratio > H_Ratio)
                    {
                        _fitSize.Width = _newSize.Width;
                        _fitSize.Height = (int)(_newSize.Width * (1 / imgRatio));
                    }
                    else if (W_Ratio < H_Ratio)
                    {
                        _fitSize.Width = (int)(_newSize.Height * imgRatio);
                        _fitSize.Height = _newSize.Height;
                    }
                    else
                    {
                        _fitSize.Width = _newSize.Width;
                        _fitSize.Height = _newSize.Height;
                    }
                }
                return _fitSize;
            }
        }

        public byte[] GetNewResolution()
        {
            byte[] blob;
            using (Bitmap newImg = new Bitmap(this.FitSize.Width, this.FitSize.Height))
            {
                using (Graphics aGraphics = Graphics.FromImage(newImg))
                {
                    aGraphics.Clear(Color.White);

                    aGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;


                    aGraphics.DrawImage(
                        this._originalImage,
                        new Rectangle(new Point(0, 0), this.FitSize),
                        new Rectangle(new Point(0, 0), this._originalImage.Size),
                        GraphicsUnit.Pixel);

                }
                using (MemoryStream aStream = new MemoryStream())
                {
                    newImg.Save(aStream, ImageFormat.Jpeg);
                    blob = aStream.GetBuffer();
                }
                return blob;
            }
        }



    }


    public static class EntityHelper
    {
        //private EntityCloneHelper()
        //{
        //}

        internal static object CloneObject(object o)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(o);
            return Newtonsoft.Json.JsonConvert.DeserializeObject(json, o.GetType());
        }

        internal static void ResetEntityAsNew(IEntity2 entity)
        {
            entity.IsNew = true;
            entity.IsDirty = true;
            entity.Fields.IsDirty = true;
            for (int f = 0; f < entity.Fields.Count; f++)
            {
                entity.Fields[f].IsChanged = true;
            }
        }




        //internal static IEntity2 CloneEntity(IEntity2 Entity)
        //{
        //    IEntity2 newEntity;

        //    newEntity = (IEntity2)CloneObject(Entity);
        //    ObjectGraphUtils ogu = new ObjectGraphUtils();
        //    List<IEntity2> flatList = ogu.ProduceTopologyOrderedList(newEntity);

        //    for (int f = 0; f < flatList.Count; f++)
        //        ResetEntityAsNew(flatList[f]);

        //    return newEntity;
        //}


        public static List<List<object>> Split(List<object> source)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / 3)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public static readonly int MaxPassParamter = 2100;


        public static List<List<int>> SplitEntityIds(List<int> source)
        {
            return source
                 .Select((x, i) => new { Index = i, Value = x })
                 .GroupBy(x => x.Index / 2100)
                 .Select(x => x.Select(v => v.Value).ToList())
                 .ToList();
        }

        public static string ConvertLookupListToString(List<LookupItemDto> lookUpList)
        {

            if (lookUpList == null)
            {
                return string.Empty;
            }

            //return string.Join(",", list.Select(o => o.Display));

            return string.Join(", ", lookUpList.Select(o =>
            {
                if (o == null || o.Display == null)
                {
                    return "";
                }

                var index = o.Display.IndexOf("|");

                if (index > 0)
                {
                    return o.Display.Substring(0, index);
                }
                else
                {
                    return o.Display;
                }
            }));
        }

    }
    public static class DataTableHelp
    {

        public static IEnumerable<IEnumerable<T>> ToChunks<T>(IEnumerable<T> enumerable,
                                                int chunkSize)
        {
            chunkSize = chunkSize - 1;
            int itemsReturned = 0;
            var list = enumerable.ToList(); // Prevent multiple execution of IEnumerable.
            int count = list.Count;
            while (itemsReturned < count)
            {
                int currentChunkSize = Math.Min(chunkSize, count - itemsReturned);
                yield return list.GetRange(itemsReturned, currentChunkSize);
                itemsReturned += currentChunkSize;
            }
        }
        public static DataTable GroupDataTable(List<string> selectColumnIdList, DataTable dataTable, List<string> concantneationColumnList, string[] groupids)
        {

            string GroupBycolumn = "GroupBy";
            dataTable.Columns.Add(GroupBycolumn);

            foreach (DataRow row in dataTable.Rows)
            {
                string groupByString = string.Empty;
                foreach (string groupcolumnid in groupids)
                {

                    groupByString = groupByString + row[groupcolumnid.Trim()] + StringHelper.UnderscoreToken;

                }
                row[GroupBycolumn] = groupByString;
            }


            //Group !
            var gridItemQuery = from row in dataTable.AsEnumerable()
                                group row by new
                                {
                                    GroupKey = row[GroupBycolumn] // row.Field<int>(GridColumnConstantName.ProductReferenceID)
                                } into grp
                                orderby grp.Key.GroupKey
                                select new
                                {
                                    Key = grp.Key.GroupKey,
                                    Row = grp.FirstOrDefault(),

                                    DictConcatennation = CreateGroupResult(grp, concantneationColumnList), //() => { return true; } //   grp.Select(r => r[concatennationColumn].ToString()).Aggregate((current, next) => current + ", " + next)
                                };

            DataTable newDataTable = dataTable.Clone();

            foreach (var groupRow in gridItemQuery)
            {
                DataRow newrow = newDataTable.NewRow();
                newDataTable.Rows.Add(newrow);

                var dictColumnConcatennation = groupRow.DictConcatennation;

                foreach (string columnId in selectColumnIdList)
                {
                    newrow[columnId.ToString()] = groupRow.Row[columnId.ToString()];
                    if (dictColumnConcatennation.ContainsKey(columnId.ToString()))
                    {

                        newrow[columnId.ToString()] = dictColumnConcatennation[columnId.ToString()];
                    }

                }

            }

            dataTable = newDataTable;
            return dataTable;
        }

        private static Dictionary<string, string> CreateGroupResult(IGrouping<object, DataRow> grp, List<string> concantneationColumnList)
        {

            Dictionary<string, string> toReturn = new Dictionary<string, string>();

            foreach (string columnId in concantneationColumnList)
            {
                toReturn.Add(columnId, grp.Select(r => r[columnId].ToString()).Aggregate((current, next) => current + ", " + next));

            }

            return toReturn;

            // throw new NotImplementedException();
        }


        public static Dictionary<string, DataColumn> DictDataColumn(this DataTable datatable)
        {

            Dictionary<string, DataColumn> dictToReturn = new Dictionary<string, DataColumn>();

            foreach (DataColumn column in datatable.Columns)
            {
                dictToReturn.Add(column.ColumnName, column);
            }

            return dictToReturn;


        }

    }

    public static class SendMailFromSMTP
    {
        public static void SmtpGamilSend(string smtpUsarename, string smtpPassword, string fromEamilAddress, string toEmailAdress)
        {
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(smtpUsarename, smtpPassword),
                EnableSsl = true
            };
            client.Send(fromEamilAddress, toEmailAdress, "test", "testbody");
            //  Console.WriteLine("Sent");
            //    Console.ReadLine();

        }


        public static List<string> CreateProcess(string program, string arguments)
        {

            List<string> toRetrun = new List<string>();
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = program;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = arguments;

            try
            {
                // 1 | 90 

                // 2 | 80 
                using (Process exeProcess = Process.Start(startInfo))
                {

                    //while (!exeProcess.StandardOutput.EndOfStream)
                    //{
                    //    string line = exeProcess.StandardOutput.ReadLine();
                    //    toRetrun.Add(line);
                    //    // do something with line
                    //}

                    exeProcess.WaitForExit();


                }
            }
            catch (Exception ex)
            {
                // Log error.
            }

            return toRetrun;
            //Process process = new Process();

            //process.StartInfo.FileName = program;
            //process.StartInfo.Arguments = arguments;
            //process.StartInfo.UseShellExecute = true;
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            //process.StartInfo.CreateNoWindow = false;

            //process.Start();

            //process.StandardOutput.ReadToEnd();
            //process.WaitForExit();




        }

    }

    public static class ProcessHelper
    {

        //Dictionary<string, string> dictArguments = new Dictionary<string, string>();
        //dictArguments[MlConstKeys.useCase] = EnumMlUseCase.ImageSearch_By_ONNX.ToString();
        //    dictArguments[MlConstKeys.action] = MlConstValues.action_Train;

        //    string ourPutFile = $@"{Guid.NewGuid()}_{MlConstKeys.outputFile}.txt ";
        //dictArguments[MlConstKeys.outputFile] = ourPutFile;

        //    mlImageSearchCommandPath = $@" ""{mlImageSearchCommandPath}"" ";

        //    string jsonString = JsonConvert.SerializeObject(dictArguments);

        //jsonString = jsonString.Replace(@"""", @"\""");


        //    string arguments = $@"""{jsonString}""";

        //ProcessHelp.CreateProcess(mlImageSearchCommandPath, arguments);
        //C:\Mdevspace\ConsoleApp1\ConsoleApp1\ConsoleApp1\bin\Release\FBDBExport.exe
        //Dictionary
        //string folderPath = $@"C:\temp\kepjson\AllFolder";
        // string csvFileName = "AllCsvImageList";
        // string jsonFileName = "AllJsonFile";

        public static List<string> CreateProcess(string program, string jsonString)
        {

            // string jsonString = JsonConvert.SerializeObject(dictArguments);

            var dictObject = JsonConvert.DeserializeObject(jsonString);
            var settings = new JsonSerializerSettings { Converters = { new ReplacingStringWritingConverter("\n", "") } };


            jsonString = JsonConvert.SerializeObject(dictObject, Formatting.None, settings);


            jsonString = jsonString.Replace(@"""", @"\""");


            string arguments = $@"""{jsonString}""";



            List<string> toRetrun = new List<string>();

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = program;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = arguments;

            try
            {

                using (Process exeProcess = Process.Start(startInfo))
                {

                    exeProcess.WaitForExit();

                }
            }
            catch (Exception ex)
            {
                // Log error.
            }

            return toRetrun;





        }

        public class ReplacingStringWritingConverter : JsonConverter
        {
            readonly string oldValue;
            readonly string newValue;

            public ReplacingStringWritingConverter(string oldValue, string newValue)
            {
                if (string.IsNullOrEmpty(oldValue))
                    throw new ArgumentException("string.IsNullOrEmpty(oldValue)");
                if (newValue == null)
                    throw new ArgumentNullException("newValue");
                this.oldValue = oldValue;
                this.newValue = newValue;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(string);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override bool CanRead { get { return false; } }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var s = ((string)value).Replace(oldValue, newValue);
                writer.WriteValue(s);
            }
        }



        public static bool ExecuteCommand(string fileName, string arguments, ref string errorMessage)
        {           
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (process.ExitCode == 0)
                    {
                        errorMessage = output;
                        return true;
                    }
                    else
                    {
                        errorMessage = "Error: " + error;                        
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Exception: " + ex.Message;                
                return false;
            }
        }

    }


}