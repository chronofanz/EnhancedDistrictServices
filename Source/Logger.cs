﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        private static readonly MyLogger m_instance = new MyLogger();

        public static void Log(string format, params object[] args)
        {
            m_instance.WriteLine(format, args);
        }

        public static void LogWarning(string msg)
        {
            m_instance.WriteLine($"[WARNING] {msg}");
        }

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
