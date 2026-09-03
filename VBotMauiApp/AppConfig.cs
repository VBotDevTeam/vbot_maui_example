namespace VBotMauiApp;

/// <summary>
/// Cấu hình môi trường kết nối cho ứng dụng VBot MAUI.
/// </summary>
public static class AppConfig
{
    /// <summary>
    /// Môi trường kết nối: "PRODUCTION", "STAGING", hoặc "SANDBOX".
    /// </summary>
    public const string Environment = "PRODUCTION";

    /// <summary>
    /// Base URL tùy chỉnh (để null nếu sử dụng Base URL mặc định của SDK).
    /// </summary>
    public const string? CustomBaseUrl = null;
}
