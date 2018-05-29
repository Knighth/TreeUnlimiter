using ICities;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using ColossalFramework.Packaging;
using ColossalFramework;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;
using TreeUnlimiter.OptionsFramework;

namespace TreeUnlimiter
{
    public class Logger
    {
        public enum LoggingLevel
        {
            [Description("None")]
            None = 0,
            [Description("Basic")]
            Basic = 1,
            [Description("Developer")]
            Developer = 2
        }

        //should be enough for most log messages and we want this guy in the HFHeap.
        private static StringBuilder logSB = new System.Text.StringBuilder(512); 

        /// <summary>
        /// Our LogWrapper...used everywhere.
        /// </summary>
        /// <param name="sText">Text to log</param>
        /// <param name="ex">An Exception - if not null it's basic data will be printed.</param>
        /// <param name="bDumpStack">If an Exception was passed do you want the stack trace?</param>
        /// <param name="bNoIncMethod">If for some reason you don't want the method name prefaced with the log line.</param>
        public static void dbgLog(string sText, Exception ex = null, bool bDumpStack = false, bool bNoIncMethod = false) 
        {
            try
            {
                logSB.Length = 0;
                string sPrefix = string.Concat("[", Mod.MOD_DBG_Prefix);
                if (bNoIncMethod) { sPrefix = string.Concat(sPrefix, "] "); }
                else
                {
                    System.Diagnostics.StackFrame oStack = new System.Diagnostics.StackFrame(1); //pop back one frame, ie our caller.
                    sPrefix = string.Concat(sPrefix, ":", oStack.GetMethod().DeclaringType.Name, ".", oStack.GetMethod().Name, "] ");
                }
                logSB.Append(string.Concat(sPrefix, sText));

                if (ex != null)
                {
                    logSB.Append(string.Concat("\r\nException: ", ex.Message.ToString()));
                }
                if (bDumpStack)
                {
                    logSB.Append(string.Concat("\r\nStackTrace: ", ex.StackTrace.ToString()));
                }
                if (OptionsWrapper<Configuration>.Options != null && OptionsWrapper<Configuration>.Options.UseCustomLogFile == true)
                {
                    string strPath = System.IO.Directory.Exists(Path.GetDirectoryName(OptionsWrapper<Configuration>.Options.CustomLogFilePath)) ? OptionsWrapper<Configuration>.Options.CustomLogFilePath.ToString() : Path.Combine(DataLocation.executableDirectory.ToString(), OptionsWrapper<Configuration>.Options.CustomLogFilePath);
                    using (StreamWriter streamWriter = new StreamWriter(strPath, true))
                    {
                        streamWriter.WriteLine(logSB.ToString());
                    }
                }
                else 
                {
                    Debug.Log(logSB.ToString());
                }
            }
            catch (Exception Exp)
            {
                Debug.Log(string.Concat("[TreeUnlimiter.Logger.dbgLog()] Error in log attempt!  ", Exp.Message.ToString()));
            }
            logSB.Length = 0;
            if (logSB.Capacity > 4096) { logSB.Capacity = 4096;}
        }
    }

}
