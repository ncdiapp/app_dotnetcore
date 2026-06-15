using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
#if NETFRAMEWORK
using System.Management.Automation;
using System.Management.Automation.Runspaces;
#endif
using System.IO;

#if NETFRAMEWORK
using Microsoft.Exchange.WebServices.Data;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif

using DatabaseSchemaMrg;
using System.Diagnostics;


using APP.Framework;
namespace App.BL
{

    public static class IISHelper
    {



        private static readonly string _RecycelApplctionPoolCommand = @"If not ""%1""==""am_admin"" (powershell.exe start -verb runas '%0' am_admin & exit /b)
            ""%windir%\system32\inetsrv\appcmd"" recycle APPPOOL ""{0}""
             ";



        private static readonly string _RestartIISServerCommand = @"If not ""%1""==""am_admin"" (powershell.exe start -verb runas '%0' am_admin & exit /b)

               net stop WAS /y
                net start W3SVC
             ";


        // %windir%\System32\inetsrv\appcmd add app /site.name:"Default Web Site" /path:/test123 /physicalPath:"I:\DevTest\App\PlmApplication" /applicationPool:"DefaultAppPool"
        private static readonly string _CreateApplicationCommand = @"If not ""%1""==""am_admin"" (powershell.exe start -verb runas '%0' am_admin & exit /b)

              ""%windir%\System32\inetsrv\appcmd"" add app /site.name:""{0}"" /path:/{1} /physicalPath:""{2}"" /applicationPool:""{3}""
             ";

        private static readonly string _ChangeApplicationPhysicalPath = @"If not ""%1""==""am_admin"" (powershell.exe start -verb runas '%0' am_admin & exit /b)

              ""%windir%\System32\inetsrv\appcmd"" set vdir ""{0}/{1}/"" -physicalPath:""{2}""
             ";

        private static readonly string _DeleteApplicationCommand = @"If not ""%1""==""am_admin"" (powershell.exe start -verb runas '%0' am_admin & exit /b)

              ""%windir%\System32\inetsrv\appcmd"" delete app ""{0}/{1}""
             ";


        public static bool RestartIISServer()
        {
            bool toReturn = true;

            string filePath = WriteTexttoBatFile(_RestartIISServerCommand);
            try
            {
                CreateProcess(filePath, "");
            }
            catch (Exception ex)
            {

            }
            //  DeleteTempFiles(filePath);

            return toReturn;
        }

        public static bool RecycelApplicationPool()
        {
            bool toReturn = true;

            string applcaitonPoolName = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationPoolName);



            string recyclecmd = string.Format(_RecycelApplctionPoolCommand, applcaitonPoolName);
            string filePath = WriteTexttoBatFile(recyclecmd);

            try
            {
                CreateProcess(filePath, "");



            }
            catch (Exception ex)
            {



            }



            //  DeleteTempFiles(filePath);



            return toReturn;
        }

        public static bool CreateApplicatoin(string websiteName, string applicationName, string physicalPath, string applcaitonPoolName)
        {
            if (string.IsNullOrWhiteSpace(websiteName))
            {
                websiteName = "Default Web Site";
            }

            if (string.IsNullOrWhiteSpace(applcaitonPoolName))
            {
                applcaitonPoolName = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationPoolName);
            }

            if (string.IsNullOrWhiteSpace(applcaitonPoolName))
            {
                applcaitonPoolName = "DefaultAppPool";
            }

            if (physicalPath != null && physicalPath.EndsWith("\\"))
            {
                physicalPath = physicalPath.Substring(0, physicalPath.Length - 1);
            }

            bool toReturn = true;


            string recyclecmd = string.Format(_CreateApplicationCommand, websiteName, applicationName, physicalPath, applcaitonPoolName);
            string filePath = WriteTexttoBatFile(recyclecmd);

            try
            {
                CreateProcess(filePath, "");
            }
            catch (Exception ex)
            {
                toReturn = false;
            }

            //  DeleteTempFiles(filePath);
            return toReturn;
        }


        public static bool ChangeApplicatoinPhysicalPath(string websiteName, string applicationName, string physicalPath)
        {
            if (string.IsNullOrWhiteSpace(websiteName))
            {
                websiteName = "Default Web Site";
            }      

            if (physicalPath != null && physicalPath.EndsWith("\\"))
            {
                physicalPath = physicalPath.Substring(0, physicalPath.Length - 1);
            }

            bool toReturn = true;

            string recyclecmd = string.Format(_ChangeApplicationPhysicalPath, websiteName, applicationName, physicalPath);
            string filePath = WriteTexttoBatFile(recyclecmd);

            try
            {
                CreateProcess(filePath, "");
            }
            catch (Exception ex)
            {
                toReturn = false;
            }




            //  DeleteTempFiles(filePath);
            return toReturn;
        }


        public static bool DeleteApplicatoin(string websiteName, string applicationName)
        {


            bool toReturn = true;


            string recyclecmd = string.Format(_DeleteApplicationCommand, websiteName, applicationName);
            string filePath = WriteTexttoBatFile(recyclecmd);

            try
            {
                CreateProcess(filePath, "");
            }
            catch (Exception ex)
            {

            }




            //  DeleteTempFiles(filePath);
            return toReturn;
        }


        private static void CreateProcess(string program, string arguments)
        {



            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = program;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = arguments;




            using (Process exeProcess = Process.Start(startInfo))
            {




                exeProcess.WaitForExit();




            }

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

        private static string WriteTexttoBatFile(string recyclecmd)
        {

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string convertTempPath = string.Format(@"{0}FileRepository\temp\", baseDirectory);

            //Filename
            var batFilename = Guid.NewGuid().ToString() + ".bat";

            //FilePath
            string originFilePath = Path.Combine(convertTempPath, batFilename);

            File.WriteAllText(originFilePath, recyclecmd);

            return originFilePath;
        }



        private static void DeleteTempFiles(string folderPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(folderPath);



                if (dir.Exists)
                {
                    foreach (FileInfo file in dir.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch
                        {
                            continue;
                        }
                    }



                    dir.Delete();
                }
            }
            catch (Exception ex)
            {
            }
        }







    }
}