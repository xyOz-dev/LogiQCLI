using System.Runtime.InteropServices;

namespace LogiQCLI.Tools.SystemOperations.Objects
{
    internal static class ShellProvider
    {
        public static ShellInfo GetShellInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new ShellInfo
                {
                    FileName = "cmd.exe",
                    ArgumentFormat = "/c \"{0}\"",
                    PersistentArguments = "/k"
                };
            }
            
            return new ShellInfo
            {
                FileName = "/bin/bash",
                ArgumentFormat = "-c \"{0}\"",
                PersistentArguments = "-i"
            };
        }
    }
}