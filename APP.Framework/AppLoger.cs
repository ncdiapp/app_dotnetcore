using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Framework
{
    //Trace - very detailed logs, which may include high-volume information such as protocol payloads. This log level is typically only enabled during development
    //    Debug - debugging information, less detailed than trace, typically not enabled in production environment.
    //Info - information messages, which are normally enabled in production environment
    //Warn - warning messages, typically for non-critical issues, which can be recovered or which are temporary failures
    //Error - error messages - most of the time these are Exceptions
    //Fatal - very serious errors
    public static  class AppLogger
    {
        private static readonly NLog.Logger _Logger = NLog.LogManager.GetCurrentClassLogger();

       
        public static void Fatal(string info)
        {
            _Logger.Fatal(info);
        }

        public static void Error(string error)
        {
            _Logger.Error(error);
        }


        public static void Warn(string info)
        {
            _Logger.Warn(info);
        }


        public static void Info(string info)
        {
            _Logger.Error(info);
        }
        public static void Debug(string info)
        {
            _Logger.Debug(info);
        }
        // Writes the diagnostic message at the Trace level.
        public static void Trace(string info)
        {
            _Logger.Trace(info);
        }

    }
}
