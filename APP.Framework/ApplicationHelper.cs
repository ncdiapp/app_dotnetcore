using System;
using System.IO;
using System.Linq;


namespace APP.Framework
{
    /// <summary>
    ///   This class contains some helper methods for an application
    /// </summary>
    public static class ApplicationHelper
    {
        /// <summary>
        ///   Gets the DLL folder path.
        /// </summary>
        /// <param name = "isWebApplication">
        ///   <c>true</c> if it is web a application; otherwise <c>false</c>.
        /// </param>
        /// <returns>Return the folder path of the folder Bin or the DLL of the application</returns>
        public static string GetDllFolderPath(bool isWebApplication)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;

            if (isWebApplication)
            {
                path = Path.Combine(path, "bin");
            }

            return path;
        }
    }
}