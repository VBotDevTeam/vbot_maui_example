namespace VBotMauiApp.Models;

/// <summary>
/// Cấu hình kết nối VBot SDK
/// </summary>
public record VBotCallConfig(
    string Token,
    string Environment = "PRODUCTION",  // PRODUCTION | STAGING | SANDBOX
    string? BaseUrl = null
);
