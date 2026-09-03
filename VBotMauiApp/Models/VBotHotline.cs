namespace VBotMauiApp.Models;

/// <summary>
/// Hotline đại diện cho số tổng đài thực hiện cuộc gọi
/// </summary>
public record VBotHotline(
    string Name,
    string PhoneNumber
)
{
    public string DisplayText =>
        (!string.IsNullOrEmpty(Name) && Name != PhoneNumber)
            ? $"{Name} - {PhoneNumber}"
            : (!string.IsNullOrEmpty(Name) ? Name : PhoneNumber);
}
