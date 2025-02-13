using System;
using System.Text;

namespace Launcher.Settings
{
    class SettingsException : Exception
    {
        public SettingsException(string message)
            : base(message)
        {
            
        }

        public SettingsException(params string[] messages)
            : base(GetString(messages))
        {
            
        }

        public SettingsException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        public SettingsException(Exception innerException, params string[] messages)
            : base(GetString(messages), innerException)
        {

        }

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
