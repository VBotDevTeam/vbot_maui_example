using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VBotMauiApp.Models;

namespace VBotMauiApp.Services;

/// <summary>
/// Partial class triển khai logic dùng chung của IVBotPhoneService
/// </summary>
public partial class VBotPhoneService : IVBotPhoneService
{
    public event EventHandler<CallSinkState>? CallStateChanged;

    private CallSinkState? _currentCallState;
    public CallSinkState? CurrentCallState => _currentCallState;

    protected void EmitCallState(CallSinkState state)
    {
        var prevState = _currentCallState;
        AppLogger.Log("PhoneService", $"EmitCallState: [{prevState?.State ?? "null"}] -> [{state.State}] | Name={state.Name}, IsIncoming={state.IsIncoming}");
        _currentCallState = state;
        CallStateChanged?.Invoke(this, state);
    }

    /// <summary>
    /// Reset call state khi disconnect để tránh stale state khi reconnect
    /// </summary>
    protected void ResetCallState()
    {
        _currentCallState = null;
    }
}
