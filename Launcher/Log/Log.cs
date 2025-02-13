using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Launcher
{
    class LogManager
    {
        static LogManager instance;

        public static LogManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LogManager();
                    instance.Launcher = instance.GetInterface("Launcher");
                }
                return instance;
            }
        }
        
        List<Log> logCollection;
        const string LogFileName = "Message.log";

        private LogManager()
        {
            logCollection = new List<Log>();
            Trace.Listeners.Clear();
            File.Delete(LogFileName);
            Trace.Listeners.Add(new TextWriterTraceListener(LogFileName));
            Trace.AutoFlush = true;
        }

        public IntPtr GetPtr(string name)
        {
            if (name == null)
                return IntPtr.Zero;
            string str = (new StringBuilder()).Append("(").Append(name).Append(")").ToString();
            Log log = logCollection.Find(x => x.Name == str);
            if (log == null)
            {
                log = new Log(str);
                log.Ptr = Marshal.GetComInterfaceForObject(log, typeof(ILog));
                logCollection.Add(log);
            }
            return log.Ptr;
        }

        public ILog GetInterface(string name)
        {
            if (name == null)
                return null;
            string str = (new StringBuilder()).Append("(").Append(name).Append(")").ToString();
            Log log = logCollection.Find(x => x.Name == str);
            if (log == null)
            {
                log = new Log(str);
                log.Ptr = Marshal.GetComInterfaceForObject(log, typeof(ILog));
                logCollection.Add(log);
            }
            return log;
        }

        long _released;

        public void Terminate()
        {
            foreach (var item in logCollection)
                item.Terminate();
            for (int i =0; i < Trace.Listeners.Count; i++)
                Trace.Listeners[i].Close();
            Trace.Listeners.Clear();
            Trace.Close();
            instance = null;
            Launcher = null;
        }

        public ILog Launcher { get; private set; }



        class Log : MarshalByRefObject, ILog
        {
            const string Info = "<|INFORMATION|>";
            const string Error = "<|ERROR|>";
            LogLevel level;

            public Log(string name)
            {
                Name = name;
                try { level = Settings.Settings.Instance.LogLevel; }
                catch { level = LogLevel.Basic; }
            }

            public IntPtr Ptr { get; set; }

            public string Name { get; private set; }


            public void Terminate()
            {
                Name = string.Empty;
                if (Ptr != IntPtr.Zero)
                    Marshal.Release(Ptr);
                Ptr = IntPtr.Zero;
            }

            public void Exception(LogLevel level, string prefix, Exception e, params string[] messages)
            {
                if (this.level.HasFlag(level))
                {
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < messages.Length; builder.Append(messages[i++])) ;
                    WriteHeader(builder.ToString(), Name, prefix == null ? string.Empty : prefix, Error);
                    WriteException(e);
                    Trace.WriteLine(string.Empty.PadRight(80, '-'));
                }
            }

            public void Exception(LogLevel level, Exception e,  string[] messages)
            {
                Exception(level, string.Empty, e, messages);    
            }

            public void Exception(string prefix, Exception e,  string[] messages)
            {
                Exception(LogLevel.Basic, prefix, e, messages);
            }

            public void Exception(Exception e,  string[] messages)
            {
                Exception(LogLevel.Basic, string.Empty, e, messages);
            }

            public void Exception(LogLevel level, string prefix, Exception e, string message)
            {
                if (this.level.HasFlag(level))
                {
                    WriteHeader(message, Name, prefix == null ? string.Empty : prefix, Error);
                    WriteException(e);
                    Trace.WriteLine(string.Empty.PadRight(80, '-'));
                }
            }

            public void Exception(LogLevel level, Exception e, string message)
            {
                Exception(level, string.Empty, e, message);
            }

            public void Exception(string prefix, Exception e, string message)
            {
                Exception(LogLevel.Basic, prefix, e, message);
            }

            public void Exception(Exception e, string message)
            {
                Exception(LogLevel.Basic, string.Empty, e, message);
            }

            public void Exception(LogLevel level, string prefix, Exception e)
            {
                if (this.level.HasFlag(level))
                {
                    WriteException(e, Name, prefix == null ? string.Empty : prefix);
                    Trace.WriteLine(string.Empty.PadRight(80, '-'));
                }
            }

            public void Exception(LogLevel level, Exception e)
            {
                Exception(level, string.Empty, e);
            }

            public void Exception(string prefix, Exception e)
            {
                Exception(LogLevel.Basic, prefix, e);
            }

            public void Exception(Exception e)
            {
                Exception(LogLevel.Basic, string.Empty, e);
            }

            public void Exception(LogLevel level, string prefix, string[] messages)
            {
                if (this.level.HasFlag(level))
                {
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < messages.Length; builder.Append(messages[i++])) ;
                    WriteHeader(builder.ToString(), Name, prefix == null ? string.Empty : prefix, Error);
                }
            }

            public void Exception(LogLevel level, string[] messages)
            {
                Exception(level, string.Empty, messages);
            }

            public void Exception(string prefix, string[] messages)
            {
                Exception(LogLevel.Basic, prefix, messages);
            }

            public void Exception(string[] messages)
            {
                Exception(LogLevel.Basic, string.Empty, messages);
            }

            public void Exception(LogLevel level, string prefix, string message)
            {
                if (this.level.HasFlag(level))
                    WriteHeader(message, Name, prefix == null ? string.Empty : prefix, Error);
            }

            public void Exception(LogLevel level, string message)
            {
                Exception(level, string.Empty, message);
            }

            public void Exception(string prefix, string message)
            {
                Exception(LogLevel.Basic, prefix, message);
            }

            public void Exception(string message)
            {
                Exception(LogLevel.Basic, string.Empty, message);
            }

            public void Information(LogLevel level, string prefix, string[] messages)
            {
                if (this.level.HasFlag(level))
                {
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < messages.Length; builder.Append(messages[i++])) ;
                    WriteHeader(builder.ToString(), Name, prefix == null ? string.Empty : prefix, Info);
                }
            }

            public void Information(LogLevel level, string[] messages)
            {
                Information(level, string.Empty, messages);
            }

            public void Information(string prefix, string[] messages)
            {
                Information(LogLevel.Basic, prefix, messages);
            }

            public void Information(string[] messages)
            {
                Information(LogLevel.Basic, string.Empty, messages);
            }

            public void Information(LogLevel level, string prefix, string message)
            {
                if (this.level.HasFlag(level))
                    WriteHeader(message.ToString(), Name, prefix == null ? string.Empty : prefix, Info);
            }

            public void Information(LogLevel level, string message)
            {
                Information(level, string.Empty, message);
            }

            public void Information(string prefix, string message)
            {
                Information(LogLevel.Basic, prefix, message);
            }

            public void Information(string message)
            {
                Information(LogLevel.Basic, string.Empty, message);
            }

            private static void WriteException(Exception e, string name, string prefix)
            {
                WriteHeader(e.Message, name, prefix, Error);
                WriteExceptionTail(e);
            }

            private static void WriteException(Exception e)
            {
                Trace.WriteLine(e.Message);
                WriteExceptionTail(e);
            }

            private static void WriteExceptionTail(Exception e)
            {
                if (e.StackTrace != null) Trace.WriteLine(e.StackTrace);
                Trace.WriteLine(string.Empty);
                if (e.InnerException != null) WriteException(e.InnerException);
            }

            private static void WriteHeader(string message, string name, string prefix, string type)
            {
                Trace.WriteLine(message, (new StringBuilder()).Append(DateTime.Now.ToString()).Append(type).Append(name).Append(prefix).ToString());
            }
        }
    }
}
