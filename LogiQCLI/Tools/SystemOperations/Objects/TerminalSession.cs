using System.Diagnostics;

namespace LogiQCLI.Tools.SystemOperations.Objects
{
    public class TerminalSession : IDisposable
    {
        public string Id { get; set; }
        public Process Process { get; set; }
        public StreamWriter Input { get; set; }
        public StreamReader Output { get; set; }
        public StreamReader Error { get; set; }

        public void Dispose()
        {
            try
            {
                Input?.Close();
                Output?.Close();
                Error?.Close();
                
                if (Process != null && !Process.HasExited)
                {
                    Process.Kill();
                    Process.WaitForExit(1000);
                }
                
                Process?.Dispose();
            }
            catch
            {
            }
        }
    }
}