namespace LogiQCLI.Core.Models.Configuration
{
    public class ApiKeySettings
    {
        public string Nickname { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        public string GetObfuscatedKey()
        {
            if (string.IsNullOrEmpty(ApiKey) || ApiKey.Length < 8)
            {
                return string.Empty;
            }
            return $"{ApiKey.Substring(0, 4)}...{ApiKey.Substring(ApiKey.Length - 4)}";
        }
    }
}
