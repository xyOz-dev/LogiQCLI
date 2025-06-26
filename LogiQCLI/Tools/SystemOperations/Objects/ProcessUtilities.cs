using System.Diagnostics;

namespace LogiQCLI.Tools.SystemOperations.Objects
{
    internal static class ProcessUtilities
    {
        public static bool IsProcessAlive(Process process)
        {
            try
            {
                return process != null && !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public static void TryKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
            }
        }
    }
}
