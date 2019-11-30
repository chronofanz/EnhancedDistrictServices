using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Yet another logger class ...
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// The private inner class that writes logs that are output by this mod, as well as the game itself, to the 
        /// log file.
        /// </summary>
        private class MyLogger : MonoBehaviour
        {
            private static readonly string m_logFilename = Path.Combine(Application.dataPath, "EDS.log");
            private readonly StreamWriter m_logFile = null;

            public MyLogger()
            {
                m_logFile = File.CreateText(m_logFilename);
                m_logFile.AutoFlush = true;

                // Write messages that are written by the game as well, for debugging purposes.
                Application.logMessageReceived += (string condition, string stackTrace, LogType type) =>
                {
                    if (string.IsNullOrEmpty(stackTrace))
                    {
                        WriteLine(condition);
                    }
                    else
                    {
                        WriteLine($"{condition}, stackTrace={stackTrace}");
                    }
                };
            }

            public void WriteLine(string format, params object[] args)
            {
                var now = DateTime.Now;
                lock (m_logFile)
                {
                    m_logFile.WriteLine($"{now}: {string.Format(format, args)}");
                }
            }
        }

        /// <summary>
        /// Singleton instance of MyLogger.
        /// </summary>
        private static readonly MyLogger m_instance = new MyLogger();

        /// <summary>
        /// Logs the message only if the dll was compiled with "VERBOSE" and if <paramref name="logIf"/> is true.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="logIf"></param>
        [Conditional("VERBOSE")]
        public static void LogVerbose(string msg, bool logIf = true)
        {
            if (logIf)
            {
                m_instance.WriteLine(msg);
            }
        }

        /// <summary>
        /// Log a regular message.
        /// </summary>
        /// <param name="msg"></param>
        public static void Log(string msg)
        {
            m_instance.WriteLine(msg);
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        /// <param name="msg"></param>
        public static void LogWarning(string msg)
        {
            m_instance.WriteLine($"[WARNING] {msg}");
        }

        /// <summary>
        /// In case of an exception, dump the stack trace to help us debug the problem.
        /// </summary>
        /// <param name="ex"></param>
        public static void LogException(Exception ex)
        {
            m_instance.WriteLine("[CRITICAL] Exception");
            m_instance.WriteLine($"ex.Message={ex.Message}");
            m_instance.WriteLine($"ex.StackTrace={ex.StackTrace}");

            int lineNumber = (new StackTrace(ex, true)).GetFrame(0).GetFileLineNumber();
            m_instance.WriteLine($"ex.LineNumber={lineNumber}");
        }
    }
}
