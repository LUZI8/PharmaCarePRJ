namespace PharmaCare.Services
{
    public sealed class AIChatMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }

    public sealed class AIRequest
    {
        public string Message { get; set; } = string.Empty;
        public string SiteContext { get; set; } = string.Empty;
        public string? UserContext { get; set; }
        public IReadOnlyList<AIChatMessage> History { get; set; } = Array.Empty<AIChatMessage>();
    }

    public sealed class AIResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    public interface IAIService
    {
        bool IsConfigured { get; }
        Task<AIResult> AskAsync(AIRequest request, CancellationToken cancellationToken = default);
    }
}
