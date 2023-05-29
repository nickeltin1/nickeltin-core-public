using UnityEngine;

namespace nickeltin.Core.Editor
{
    internal static class NickeltinCore
    {
        /// <summary>
        /// Name of core package that used by different nickeltin modules.
        /// </summary>
        public const string Name = "com.nickeltin.core";
    
        /// <summary>
        /// Will log from the name of <see cref="Name"/> without stacktrace.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="logType"></param>
        public static void Log(object msg, LogType logType = LogType.Log)
        {
            Debug.LogFormat(logType, LogOption.NoStacktrace, null, "<b>[{0}]</b> {1}", Name, msg);
        }
    }
}