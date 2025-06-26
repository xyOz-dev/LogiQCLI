using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.SystemOperations.Objects
{
    public class ExecuteCommandArguments
    {
        [JsonPropertyName("command")]
        public string Command { get; set; }

        [JsonPropertyName("cwd")]
        public string WorkingDirectory { get; set; }

        [JsonPropertyName("timeout")]
        public int? Timeout { get; set; }

        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("keep_alive")]
        public bool? KeepAlive { get; set; }

        [JsonPropertyName("kill_session")]
        public bool? KillSession { get; set; }
    }
}
