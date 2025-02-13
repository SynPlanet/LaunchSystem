using System;
using System.Runtime.Serialization;
using System.Text;

namespace Launcher
{
    [Serializable]
    class LauncherException : Exception, ISerializable
    {
        public LauncherException(params string[] message)
            : base(GetString(message)) { }

        public LauncherException(Exception innerException, params string[] messages)
            : base(GetString(messages), innerException) { }

        protected LauncherException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        private static string GetString(params string[] messages)
        {
            StringBuilder str = new StringBuilder();
            foreach (var item in messages)
                str.Append(item);
            return str.ToString();
        }

        public override string ToString()
        {
            return base.ToString();
        }
        
    }
}
