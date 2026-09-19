namespace PharmaCare.Services
{
    public class AISettings
    {
        public bool Enabled { get; set; } = false;
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string Model { get; set; } = "gpt-5.6-luna";
        public int MaxOutputTokens { get; set; } = 700;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
