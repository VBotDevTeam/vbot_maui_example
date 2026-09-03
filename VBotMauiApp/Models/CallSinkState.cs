namespace VBotMauiApp.Models;

/// <summary>
/// Trạng thái cuộc gọi được đồng bộ từ Native SDK
/// </summary>
public record CallSinkState(
    string Name,
    string State,        // "calling" | "incoming" | "connecting" | "confirmed" | "disconnected" | "none"
    bool IsIncoming,
    bool IsMute,
    bool OnHold
)
{
    public static CallSinkState Empty => new(
        Name: string.Empty,
        State: "none",
        IsIncoming: false,
        IsMute: false,
        OnHold: false
    );
}
