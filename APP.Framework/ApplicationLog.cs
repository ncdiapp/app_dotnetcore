
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Com.Visual2000.SystemFramework
{

    /// <summary>
    ///     Logging class to provide tracing and logging support. 
    ///     <remarks>
    ///         There are four different logging levels (error, warning, info, trace) 
    ///         that produce output to either the system event log or a tracing 
    ///         file as specified in the current ApplicationConfiguration settings.
    ///          // Summary:
        //     Output no tracing and debugging messages.
        //Off = 0,
        ////
        //// Summary:
        ////     Output error-handling messages.
        //Error = 1,
        ////
        //// Summary:
        ////     Output warnings and error-handling messages.
        //Warning = 2,
        ////
        //// Summary:
        ////     Output informational messages, warnings, and error-handling messages.
        //Info = 3,
        ////
        //// Summary:
        ////     Output all debugging and tracing messages.
        //Verbose = 4,
    ///     </remarks>
    /// </summary>
    /// 
    // <add key="Tracing.Enabled" value="True" />
    //<add key="Tracing.TraceFile" value="PDMTraceResult.txt" />
    //<add key="Tracing.TraceLevel" value="2" />
    //<add key="Tracing.SwitchName" value="PDMTraceSwitch" />
    //<add key="Tracing.SwitchDescription" value="Error and information tracing for Visual PDM" />
    //<add key="EventLog.Enabled" value="True" />
    //<add key="EventLog.Machine" value="." />
    //<add key="EventLog.SourceName" value="PDM.Net Log" />
    //<add key="EventLog.LogLevel" value="3" />

    public class ApplicationLog
    {

#if NETFRAMEWORK
        private static readonly bool TracingEnabled = System.Configuration.ConfigurationManager.AppSettings["Tracing.Enabled"].ToUpperInvariant() == "TRUE";
        private static readonly string TracingTraceFile = System.Configuration.ConfigurationManager.AppSettings["Tracing.TraceFile"].ToString();
        private static readonly int TracingTraceLevel = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Tracing.TraceLevel"].ToString());
        private static readonly string TracingSwitchName = System.Configuration.ConfigurationManager.AppSettings["Tracing.SwitchName"].ToString();
        private static readonly string TracingSwitchDescription = System.Configuration.ConfigurationManager.AppSettings["Tracing.SwitchDescription"].ToString();
#else
        // NLog (configured via appsettings.json NLog section) replaces this legacy tracing system on .NET 10.
        private static readonly bool TracingEnabled = false;
        private static readonly string TracingTraceFile = string.Empty;
        private static readonly int TracingTraceLevel = 0;
        private static readonly string TracingSwitchName = string.Empty;
        private static readonly string TracingSwitchDescription = string.Empty;
#endif
     
        //private static readonly int EventLogTraceLevel = 1;// System.Configuration.ConfigurationManager.AppSettings["ADPassword"].ToString();
        //private static readonly bool EventLogEnabled = true; //System.Configuration.ConfigurationManager.AppSettings["ADUser"].ToString();
        //private static readonly string EventLogMachineName = System.Configuration.ConfigurationManager.AppSettings["ADPassword"].ToString();
        //private static readonly string EventLogSourceName = System.Configuration.ConfigurationManager.AppSettings["ADUser"].ToString();
             

        static ApplicationLog()
        {

            Type myType = typeof(ApplicationLog);

            try
            {

                if (!Monitor.TryEnter(myType)) //true  if the current thread acquires the exlusive lock; otherwise, false. 
                {
                    Monitor.Enter(myType); //Acquires an exclusive lock on the specified object. 
                    return;
                }

                bool clearSettings = true;
                if (Monitor.TryEnter(myType))
                {

                    try
                    {
                        //Check if we're enabled

                        if (TracingEnabled )
                        {
                            //Make sure we have a TraceSettings file.
                            String tracingFile = TracingTraceFile;
                            if (tracingFile != String.Empty)
                            {

                                //Read in the tracing switch name and create the switch.
                                String switchName = TracingSwitchName;

                                //Create the new switch
                                if (switchName != String.Empty)
                                {
                                    //Create a debug listener and add it as a debug listener

                                    FileInfo file = new FileInfo(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\\" + tracingFile);
                                    if (!file.Exists)
                                    {

                                        file.Create().Close();

                                    }

                                    _StreamDebugWriter = new StreamWriter(file.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
                                    TextWriterTraceListener aTextWriterTraceListener = new TextWriterTraceListener(_StreamDebugWriter);
                                  //  Debug.Listeners.Add(aTextWriterTraceListener);
                                    Trace.Listeners.Add(aTextWriterTraceListener);
                                    _TraceSwitch = new TraceSwitch(switchName, TracingSwitchDescription);
                                    _TraceSwitch.Level =  (TraceLevel ) TracingTraceLevel;
                                }
                                clearSettings = false;
                            }
                        }
                    }
                    catch //( Exception ex)
                    {
                        //string s = ex.StackTrace;
                    }

                }// end if // any Exception it will give the True;

                //See if there is a debug configuration file specified and set up the
                //  tracing variables.
                // this 


                //Use default (empty) values if something went wrong
                if (clearSettings)
                {
                    //Tracing information is off or not properly specified, clear it
                    _TraceSwitch = null;
                    _StreamDebugWriter = null;
                }

                //if (EventLogEnabled)
                //    _EventLogTraceLevel =(TraceLevel)  EventLogTraceLevel;
                //else
                //    _EventLogTraceLevel = TraceLevel.Off;
            }
            finally
            {
                //Remove the lock from the class object
                Monitor.Exit(myType);
            }
        }

        private static TraceSwitch _TraceSwitch;
        //This object is added as a debug listener.
        private static StreamWriter _StreamDebugWriter;

     
        /// <summary>
        ///     Write at the Error level to the event log and/or tracing file.
        ///     <param name="message">The text to write to the log file or event log.</param>
        /// </summary>
        public static void WriteError(String message)
        {
            //Defer to the helper function to log the message.
            WriteLog(TraceLevel.Error, message);
        }


        public static void WriteWarning(String message)
        {
            //Defer to the helper function to log the message.
            WriteLog(TraceLevel.Warning, message);
        }

        /// <summary>
        ///     Write at the Info level to the event log and/or tracing file.
        ///     <param name="message">The text to write to the log file or event log.</param>
        /// </summary>
        public static void WriteInfo(String message)
        {
            //Defer to the helper function to log the message.
            WriteLog(TraceLevel.Info, message);
        }

        /// <summary>
        ///     Write at the Verbose level to the event log and/or tracing file.
        ///     <param name="message">The text to write to the log file or event log.</param>
        /// </summary>
        public static void WriteTrace(String message)
        {
            //Defer to the helper function to log the message.
            WriteLog(TraceLevel.Verbose, message);
        }

        /// <summary>
        ///     Write at the Verbose level to the event log and/or tracing file.
        ///     <param name="ex">The Exception object to format</param>
        ///     <param name="catchInfo">The string to prepend to the exception information.</param>
        ///     <retvalue>
        ///         <para>A nicely format exception string, including message and StackTrace information.</para>
        ///     </retvalue>
        /// </summary>
        public static String FormatException(Exception ex, String catchInfo)
        {
            StringBuilder strBuilder = new StringBuilder();
            if (catchInfo != String.Empty)
            {
                strBuilder.Append(catchInfo).Append("\r\n");
            }
            strBuilder.Append(ex.Message).Append("\r\n").Append(ex.StackTrace);
            return strBuilder.ToString();
        }


        private static void WriteLog(TraceLevel level, String messageText)
        {
            //
            // Be very careful by putting a Try/Catch around the entire routine.
            //   We should never throw an exception while logging.
            //
            try
            {

                //debugSwitch = new TraceSwitch(switchName, ApplicationConfiguration.TracingSwitchDescription);
                //debugSwitch.Level = ApplicationConfiguration.TracingTraceLevel;
                // debugSwitch Write the message to the trace file
                // 0 = Off   1 = Error   2 = Warning    3 = Info     4 = Verbose 

                if (_StreamDebugWriter != null)
                {
                    //Log based on switch level.
                    if (level <= _TraceSwitch.Level)
                    {
                        lock (_StreamDebugWriter)
                        {
                            //Debug.WriteLine(messageText);
                            Trace.WriteLine("LogDate-- " + System.DateTime.Now.ToString() + "--" + messageText);
                            _StreamDebugWriter.Flush();
                            // do not close but Flussh debugWriter
                        }
                    }
                }


               
            }
            catch { } //Ignore any exceptions.
        }



    } //class ApplicationLog
}
