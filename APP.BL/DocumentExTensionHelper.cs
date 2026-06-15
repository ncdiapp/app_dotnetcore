using GemBox.Document;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace APP.Components.Dto
{
    public static class DocumentHelper
    {
        public static string GetHttpUrlContentString(string fullUrl, AppClientIdentity aAppClientIdentity)
        {

            Uri uri = new Uri(fullUrl);
            Uri baseAddress = new Uri(string.Format("{0}://{1}:{2}/", uri.Scheme, uri.Host, uri.Port));
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                AddCookie(aAppClientIdentity, baseAddress, cookieContainer);
                var streamresult = client.GetStringAsync(fullUrl);
                return streamresult.Result;
            }


        }
        public static byte[] GetHttpUrlBytes(string fullUrl, AppClientIdentity aAppClientIdentity)
        {

            Uri uri = new Uri(fullUrl);
            Uri baseAddress = new Uri(string.Format("{0}://{1}:{2}/", uri.Scheme, uri.Host, uri.Port));
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {

                AddCookie(aAppClientIdentity, baseAddress, cookieContainer);


                var streamresult = client.GetByteArrayAsync(fullUrl);

                return streamresult.Result;



            }


        }

        public static byte[] CovnertHttpUrlToPdf(string baseAddressPath, string path, AppClientIdentity aAppClientIdentity)
        {


            Uri baseAddress = new Uri(baseAddressPath);
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {

                AddCookie(aAppClientIdentity, baseAddress, cookieContainer);


                var streamresult = client.GetStreamAsync(baseAddressPath + path);

                var stream = streamresult.Result;

                DocumentModel document3 = DocumentModel.Load(stream, GemBox.Document.LoadOptions.HtmlDefault);

                MemoryStream pdfMo = new MemoryStream();
                document3.Save(pdfMo, GemBox.Document.SaveOptions.PdfDefault);
                return pdfMo.ToArray();


            }


        }

        public static byte[] CovnertHttpUrlToWord(string baseAddressPath, string path, AppClientIdentity aAppClientIdentity)
        {


            Uri baseAddress = new Uri(baseAddressPath);
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {

                AddCookie(aAppClientIdentity, baseAddress, cookieContainer);


                var streamresult = client.GetStreamAsync(baseAddressPath + path);

                var stream = streamresult.Result;

                DocumentModel document3 = DocumentModel.Load(stream, GemBox.Document.LoadOptions.HtmlDefault);

                MemoryStream pdfMo = new MemoryStream();
                document3.Save(pdfMo, GemBox.Document.SaveOptions.DocxDefault);
                return pdfMo.ToArray();


            }


        }
        private static void AddCookie(AppClientIdentity aAppClientIdentity, Uri baseAddress, CookieContainer cookieContainer)
        {
            cookieContainer.Add(baseAddress, new Cookie("CurrentUserSessionId", aAppClientIdentity.SessionId.ToString()));
            cookieContainer.Add(baseAddress, new Cookie("CurrentSelectedCompanyId", aAppClientIdentity.CurrentWorkingCompanyId.ToString()));
            cookieContainer.Add(baseAddress, new Cookie("IsOutofBroserCall", aAppClientIdentity.IsExternalApp.ToString()));
            cookieContainer.Add(baseAddress, new Cookie("UserId", aAppClientIdentity.UserId.ToString()));
        }


        static async void DownloadPageAsync(string url)
        {

            // ... Use HttpClient.
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(url))
            using (HttpContent content = response.Content)
            {
                // ... Read the string.
                string result = await content.ReadAsStringAsync();


            }
        }


        public static EmAppDocumentType GetDocumentTypeByExtensionName(string extension)
        {
            if (!string.IsNullOrEmpty(extension))
            {
                if (IsPDF(extension))
                {
                    return EmAppDocumentType.PDF;
                }
                else if (IsGIF(extension))
                {
                    return EmAppDocumentType.GIF;
                }
                else if (IsTXT(extension))
                {
                    return EmAppDocumentType.TXT;
                }
                else if (IsVideo(extension))
                {
                    return EmAppDocumentType.Video;
                }
                else if (IsWord(extension))
                {
                    return EmAppDocumentType.WORD;
                }
                else if (IsExcel(extension))
                {
                    return EmAppDocumentType.EXCEL;
                }
                else if (IsCompressed(extension))
                {
                    return EmAppDocumentType.Compressed;
                }
                else if (IsJPG(extension))
                {
                    return EmAppDocumentType.JPG;
                }
                else if (IsBMP(extension))
                {
                    return EmAppDocumentType.BMP;
                }
                else if (IsTIF(extension))
                {
                    return EmAppDocumentType.TIF;
                }
                else if (IsDWG(extension))
                {
                    return EmAppDocumentType.DWG;
                }
                else if (IsAI(extension))
                {
                    return EmAppDocumentType.AI;
                }
                else if (IsPNG(extension))
                {
                    return EmAppDocumentType.PNG;
                }
                else if (IsPSD(extension))
                {
                    return EmAppDocumentType.PSD;
                }
                else if (ISSVG(extension))
                {
                    return EmAppDocumentType.SVG;
                }
                else if (ISINDD(extension))
                {
                    return EmAppDocumentType.INDD;
                }

                else if (IsPPT(extension))
                {
                    return EmAppDocumentType.PPT;
                }

            }

            return EmAppDocumentType.Unknown;
        }

        public static string GetMimeContentType(string extension)
        {
            string contentType = string.Empty;

            //switch (extension)
            //{
            //    case "htm":
            //    case "html":
            //    case "log":
            //        contentType = "text/HTML";
            //        break;
            //    case "txt":
            //        contentType = "text/plain";
            //        break;
            //    case "doc":
            //        contentType = "application/ms-word";
            //        break;
            //    case "tiff":
            //    case "tif":
            //        contentType = "image/tiff";
            //        break;
            //    case "asf":
            //        contentType = "video/x-ms-asf";
            //        break;
            //    case "avi":
            //        contentType = "video/avi";
            //        break;
            //    case "zip":
            //        contentType = "application/zip";
            //        break;
            //    case "xls":
            //    case "csv":
            //        contentType = "application/vnd.ms-excel";
            //        break;
            //    case "gif":
            //        contentType = "image/gif";
            //        break;
            //    case "jpg":
            //    case "jpeg":
            //        contentType = "image/jpeg";
            //        break;
            //    case "bmp":
            //        contentType = "image/bmp";
            //        break;
            //    case "wav":
            //        contentType = "audio/wav";
            //        break;
            //    case "mp3":
            //        contentType = "audio/mpeg3";
            //        break;
            //    case "mpg":
            //    case "mpeg":
            //        contentType = "video/mpeg";
            //        break;
            //    case "rtf":
            //        contentType = "application/rtf";
            //        break;
            //    case "asp":
            //        contentType = "text/asp";
            //        break;
            //    case "pdf":
            //        contentType = "application/pdf";
            //        break;
            //    case "fdf":
            //        contentType = "application/vnd.fdf";
            //        break;
            //    case "ppt":
            //        contentType = "application/mspowerpoint";
            //        break;
            //    case "dwg":
            //        contentType = "image/vnd.dwg";
            //        break;
            //    case "msg":
            //        contentType = "application/msoutlook";
            //        break;
            //    case "xml":
            //    case "sdxl":
            //        contentType = "application/xml";
            //        break;
            //    case "xdp":
            //        contentType = "application/vnd.adobe.xdp+xml";
            //        break;
            //    default:
            //        contentType = "application/octet-stream";
            //        break;
            //}

            contentType = "application/octet-stream";
            return contentType;
        }


        public static string GetSpecifiedMimeContentType(string extension)
        {
            string contentType = string.Empty;

            extension = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(extension).ToLower();

            extension = extension.Replace(".", "");

            switch (extension)
            {
                case "htm":
                case "html":
                case "log":
                    contentType = "text/HTML";
                    break;
                case "txt":
                    contentType = "text/plain";
                    break;
                case "doc":
                    contentType = "application/ms-word";
                    break;
                case "tiff":
                case "tif":
                    contentType = "image/tiff";
                    break;
                case "asf":
                    contentType = "video/x-ms-asf";
                    break;
                case "avi":
                    contentType = "video/avi";
                    break;
                case "zip":
                    contentType = "application/zip";
                    break;
                case "xls":
                case "csv":
                    contentType = "application/vnd.ms-excel";
                    break;
                case "gif":
                    contentType = "image/gif";
                    break;
                case "jpg":
                case "jpeg":
                    contentType = "image/jpeg";
                    break;
                case "bmp":
                    contentType = "image/bmp";
                    break;
                case "wav":
                    contentType = "audio/wav";
                    break;
                case "mp3":
                    contentType = "audio/mpeg3";
                    break;
                case "mpg":
                case "mpeg":
                    contentType = "video/mpeg";
                    break;
                case "rtf":
                    contentType = "application/rtf";
                    break;
                case "asp":
                    contentType = "text/asp";
                    break;
                case "pdf":
                    contentType = "application/pdf";
                    break;
                case "fdf":
                    contentType = "application/vnd.fdf";
                    break;
                case "ppt":
                    contentType = "application/mspowerpoint";
                    break;
                case "dwg":
                    contentType = "image/vnd.dwg";
                    break;
                case "msg":
                    contentType = "application/msoutlook";
                    break;
                case "xml":
                case "sdxl":
                    contentType = "application/xml";
                    break;
                case "xdp":
                    contentType = "application/vnd.adobe.xdp+xml";
                    break;
                default:
                    contentType = "application/octet-stream";
                    break;
            }
          
            return contentType;
        }



        private static bool ISINDD(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".indd")
            {
                return true;
            }

            return false;
        }

        private static bool ISSVG(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".svg")
            {
                return true;
            }

            return false;
        }

        private static bool IsPDF(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".pdf")
            {
                return true;
            }

            return false;
        }

        private static bool IsGIF(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".gif")
            {
                return true;
            }

            return false;
        }

        private static bool IsTXT(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".txt" || extension == ".html" || extension == ".htm"
                || extension == ".mht" || extension == ".mhtml" || extension == ".xml")
            {
                return true;
            }

            return false;
        }

        private static bool IsVideo(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".asx" || extension == ".wax" || extension == ".m3u"
                || extension == ".wpl" || extension == ".wvx" || extension == ".wmx"
                || extension == ".dvr-ms" || extension == ".mid" || extension == ".rmi"
                || extension == ".midi" || extension == ".mpeg" || extension == ".mpg"
                || extension == ".m1v" || extension == ".mp2" || extension == ".mpa"
                || extension == ".mpe" || extension == ".wav" || extension == ".snd"
                || extension == ".au" || extension == ".aif" || extension == ".aifc"
                || extension == ".aiff" || extension == ".wma" || extension == ".mp3"
                || extension == ".asf" || extension == ".wm" || extension == ".wmv"
                || extension == ".wmd" || extension == ".avi"

                  || extension == ".mp4" || extension == ".ogg"
                )
            {
                return true;
            }

            return false;
        }

        private static bool IsWord(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".docx" || extension == ".docm" || extension == ".dotx"
                || extension == ".dotm" || extension == ".doc" || extension == ".dot"
                || extension == ".rtf")
            {
                return true;
            }

            return false;
        }

        private static bool IsExcel(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".xl" || extension == ".xlsx" || extension == ".xlsm" || extension == ".csv"
                || extension == ".xlsb" || extension == ".xlam" || extension == ".xltx"
                || extension == ".xltm" || extension == ".xls" || extension == ".xlt"
                || extension == ".xla" || extension == ".xlm" || extension == ".xlw"
                || extension == ".odc" || extension == ".uxdc")
            {
                return true;
            }

            return false;
        }


        private static bool IsPPT(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".ppt" || extension == ".pptx")
            {
                return true;
            }

            return false;
        }

        private static bool IsCompressed(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".rar" || extension == ".zip" || extension == ".cab"
                || extension == ".arj" || extension == ".lzh" || extension == ".ace"
                || extension == ".7-zip" || extension == ".tar" || extension == ".gzip"
                || extension == ".uue" || extension == ".bz2" || extension == ".jar"
                || extension == ".iso" || extension == ".z")
            {
                return true;
            }

            return false;
        }

        private static bool IsJPG(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".jpg" || extension == ".jpeg")
            {
                return true;
            }

            return false;
        }

        private static bool IsBMP(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".bmp")
            {
                return true;
            }

            return false;
        }

        private static bool IsTIF(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".tif")
            {
                return true;
            }

            return false;
        }

        private static bool IsDWG(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".dwg")
            {
                return true;
            }

            return false;
        }

        private static bool IsAI(string extension)
        {
            extension = extension.ToLower();

            if (extension == ".ai")
            {
                return true;
            }

            return false;
        }

        private static bool IsPNG(string extension)
        {
            return extension.ToLower().Equals(".png");
        }

        private static bool IsPSD(string extension)
        {
            return extension.ToLower().Equals(".psd");
        }


    }


    public static class ZipArchiveExtensions
    {
        public static string ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            string messageToReturn = "";
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                return messageToReturn;
            }

            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
            string destinationDirectoryFullPath = di.FullName;

            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName));

                if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                }

                if (file.Name == "")
                {// Assuming Empty for Directory
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    continue;
                }

                try
                {
                    file.ExtractToFile(completeFileName, true);
                }
                catch (Exception ex)
                {
                    messageToReturn = messageToReturn + ex.Message + System.Environment.NewLine;
                }


                //FileInfo fileInfo = new FileInfo(completeFileName);
                //if(fileInfo.Exists)
                //{
                //    file.ExtractToFile(completeFileName, true);
                //}
                //else
                //{
                //    file.ExtractToFile(completeFileName, false);
                //}

            }

            return messageToReturn;
        }
    }
}