using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundation;
using VBot.iOS.SDK;
using VBotMauiApp.Models;

namespace VBotMauiApp.Services;

public partial class VBotPhoneService : IVBotPhoneService
{
    private bool _isMuted = false;
    private bool _isSpeakerOn = false;
    private string _displayName = string.Empty;
    private string _lastCallName = string.Empty;
    private IOSWrapperDelegate? _wrapperDelegate;

    // === Tracking cuộc gọi đến ===
    private DateTime? _incomingCallStartTime;
    private bool _callWasConnected;
    private string? _lastTransId;
    private string? _lastCaller;
    private string? _lastProjectName;
    private string? _lastProjectCode;
    private bool _lastIsCampaign;
    private string? _checkCallExistResult;

    public void TrackIncomingPushReceived(string? transId, string? caller, string? projectName, string? projectCode, bool isCampaign)
    {
        _incomingCallStartTime = DateTime.Now;
        _callWasConnected = false;
        _lastTransId = transId;
        _lastCaller = caller;
        _lastProjectName = projectName;
        _lastProjectCode = projectCode;
        _lastIsCampaign = isCampaign;
        _checkCallExistResult = null;
    }

    public void TrackCheckCallExistResult(string result)
    {
        _checkCallExistResult = result;
    }

    public static VBotPhoneService? Instance { get; private set; }

    public Task InitializeAsync()
    {
        try
        {
            Instance = this;
            // SDK đã được khởi tạo trong AppDelegate qua VBotWrapper.Shared.Initialize()
            // Ở đây chỉ đăng ký delegate để nhận callback cuộc gọi
            _wrapperDelegate = new IOSWrapperDelegate(this);
            VBotWrapper.Shared.Delegate = _wrapperDelegate;
            AppLogger.Log("VBotSDK", "Đã đăng ký VBotWrapper delegate thành công (SDK init từ AppDelegate)");
        }
        catch (Exception ex)
        {
            AppLogger.Log("VBotSDK", $"Lỗi đăng ký delegate: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsUserConnectedAsync()
    {
        try
        {
            return Task.FromResult(VBotWrapper.Shared.IsUserConnected);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<string?> GetUserDisplayNameAsync()
    {
        try
        {
            var name = VBotWrapper.Shared.UserDisplayName;
            return Task.FromResult<string?>(name);
        }
        catch
        {
            return Task.FromResult<string?>(_displayName);
        }
    }

    public Task<string?> ConnectAsync(VBotCallConfig config)
    {
        var tcs = new TaskCompletionSource<string?>();

        try
        {
            var pushKitToken = VBotWrapper.Shared.PushKitToken
                ?? AppDelegate.CachedPushKitToken
                ?? string.Empty;

            AppLogger.Log("VBotSDK", $"Bắt đầu Connect: Env={config.Environment}, BaseUrl={config.BaseUrl}");
            AppLogger.Log("VBotSDK", $"PushKitToken: {(string.IsNullOrEmpty(pushKitToken) ? "[TRỐNG]" : pushKitToken)}");

            // Kết nối qua Swift Wrapper
            VBotWrapper.Shared.Connect(
                token: config.Token,
                environment: config.Environment ?? "STAGING",
                customBaseUrl: !string.IsNullOrEmpty(config.BaseUrl) ? config.BaseUrl : null,
                completion: (displayName, error) =>
                {
                    if (error != null)
                    {
                        AppLogger.Log("VBotSDK", $"Connect thất bại: {error.LocalizedDescription}");
                        tcs.SetResult(null);
                    }
                    else
                    {
                        _displayName = displayName?.ToString() ?? "VBot User";
                        AppLogger.Log("VBotSDK", $"Connect thành công: {_displayName}");
                        tcs.SetResult(_displayName);
                    }
                });
        }
        catch (Exception ex)
        {
            AppLogger.Log("VBotSDK", $"Ngoại lệ khi Connect: {ex.Message}");
            tcs.SetResult(null);
        }

        return tcs.Task;
    }

    public Task<bool> DisconnectAsync()
    {
        var tcs = new TaskCompletionSource<bool>();

        try
        {
            if (CurrentCallState != null && CurrentCallState.State != "disconnected")
            {
                EmitCallState(CurrentCallState with { State = "disconnected" });
            }

            AppLogger.Log("VBotSDK", "Đang ngắt kết nối VBot SDK...");
            VBotWrapper.Shared.Disconnect(error =>
            {
                if (error != null)
                {
                    AppLogger.Log("VBotSDK", $"Lỗi Disconnect: {error.LocalizedDescription}");
                    tcs.SetResult(false);
                }
                else
                {
                    _displayName = string.Empty;
                    AppLogger.Log("VBotSDK", "Đã ngắt kết nối VBot SDK thành công.");
                    tcs.SetResult(true);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VBotPhone iOS] Disconnect exception: {ex.Message}");
            tcs.SetResult(false);
        }

        _isMuted = false;
        _isSpeakerOn = false;
        _lastCallName = string.Empty;
        ResetCallState();

        return tcs.Task;
    }

    public Task<List<Models.VBotHotline>> GetHotlinesAsync()
    {
        var tcs = new TaskCompletionSource<List<Models.VBotHotline>>();

        try
        {
            VBotWrapper.Shared.GetHotlines((hotlinesArray, error) =>
            {
                var list = new List<Models.VBotHotline>();
                if (error != null)
                {
                    Console.WriteLine($"[VBotPhone iOS] GetHotlines error: {error.LocalizedDescription}");
                }
                else if (hotlinesArray != null)
                {
                    for (nuint i = 0; i < hotlinesArray.Count; i++)
                    {
                        var item = hotlinesArray.GetItem<NSObject>(i);
                        if (item != null)
                        {
                            var name = item.ValueForKey(new NSString("name"))?.ToString() ?? "";
                            var phone = item.ValueForKey(new NSString("phoneNumber"))?.ToString() ?? "";
                            list.Add(new Models.VBotHotline(name, phone));
                        }
                    }
                }
                tcs.SetResult(list);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VBotPhone iOS] GetHotlines exception: {ex.Message}");
            tcs.SetResult(new List<Models.VBotHotline>());
        }

        return tcs.Task;
    }

    public Task<string?> StartCallAsync(string displayName, string phoneNumber, string hotline)
    {
        _lastCallName = string.IsNullOrEmpty(displayName) ? phoneNumber : displayName;

        // Phát state calling ngay lập tức để UI cập nhật tên chính xác
        EmitCallState(new CallSinkState(
            Name: _lastCallName,
            State: "calling",
            IsIncoming: false,
            IsMute: false,
            OnHold: false
        ));

        var tcs = new TaskCompletionSource<string?>();

        try
        {
            // Thực hiện cuộc gọi đi qua Swift Wrapper
            VBotWrapper.Shared.StartOutgoingCall(
                displayName: _lastCallName,
                number: phoneNumber,
                hotline: hotline,
                externalCallId: null,
                completion: (success, error) =>
                {
                    if (error != null)
                    {
                        Console.WriteLine($"[VBotPhone iOS] StartCall error: {error.LocalizedDescription}");
                        tcs.SetResult(null);
                    }
                    else
                    {
                        tcs.SetResult(phoneNumber);
                    }
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VBotPhone iOS] StartCall exception: {ex.Message}");
            tcs.SetResult(null);
        }

        return tcs.Task;
    }

    public Task AnswerAsync()
    {
        if (CurrentCallState != null)
        {
            EmitCallState(CurrentCallState with { State = "confirmed", IsIncoming = false });
        }
        return Task.CompletedTask;
    }

    public Task HangupAsync()
    {
        try
        {
            VBotWrapper.Shared.EndCall(error =>
            {
                if (error != null)
                {
                    Console.WriteLine($"[VBotPhone iOS] EndCall error: {error.LocalizedDescription}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VBotPhone iOS] EndCall exception: {ex.Message}");
        }

        if (CurrentCallState != null)
        {
            EmitCallState(CurrentCallState with { State = "disconnected" });
        }
        _lastCallName = string.Empty;
        return Task.CompletedTask;
    }

    public Task MuteAsync()
    {
        _isMuted = !_isMuted;
        try
        {
            VBotWrapper.Shared.MuteCall();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VBotPhone iOS] MuteCall exception: {ex.Message}");
        }

        if (CurrentCallState != null)
        {
            EmitCallState(CurrentCallState with { IsMute = _isMuted });
        }
        return Task.CompletedTask;
    }

    public Task SpeakerAsync()
    {
        _isSpeakerOn = !_isSpeakerOn;
        try
        {
            VBotWrapper.Shared.OnOffSpeaker();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VBotPhone iOS] OnOffSpeaker exception: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _wrapperDelegate = null;
    }

    public void OnWrapperCallStateChanged(string state, string name, bool isIncoming, bool isMute, bool onHold)
    {
        AppLogger.Log("VBotDelegate", $"CallState: {state}, Name: {name}, Incoming: {isIncoming}");

        if (state == "confirmed")
        {
            _callWasConnected = true;
        }

        if (!string.IsNullOrEmpty(name))
        {
            _lastCallName = name;
        }
        else if (string.IsNullOrEmpty(_lastCallName))
        {
            _lastCallName = isIncoming ? "Cuộc gọi đến" : "Cuộc gọi";
        }

        EmitCallState(new CallSinkState(
            Name: _lastCallName,
            State: state,
            IsIncoming: isIncoming,
            IsMute: isMute,
            OnHold: onHold
        ));

        if (state == "disconnected")
        {
            _lastCallName = string.Empty;
        }
    }

    public void OnWrapperCallEnded(string reason, string endedBy)
    {
        AppLogger.Log("CallReport", $"CallEnded | reason={reason}, endedBy={endedBy}");

        if (_incomingCallStartTime.HasValue)
        {
            var endTime = DateTime.Now;
            var duration = endTime - _incomingCallStartTime.Value;
            AppLogger.Log("CallReport", $"Tổng thời gian: {duration.TotalSeconds:F2}s, Đã nghe máy: {_callWasConnected}");

            _incomingCallStartTime = null;
            _callWasConnected = false;
            _lastTransId = null;
        }

        if (CurrentCallState != null)
        {
            EmitCallState(CurrentCallState with { State = "disconnected" });
        }
        _lastCallName = string.Empty;
    }

    /// <summary>
    /// Delegate nhận callback từ Swift Wrapper (VBotWrapperDelegate)
    /// Giống pattern Android: VBotWrapperListener
    /// </summary>
    private class IOSWrapperDelegate : VBotWrapperDelegate
    {
        private readonly VBotPhoneService _service;

        public IOSWrapperDelegate(VBotPhoneService service)
        {
            _service = service;
        }

        public override void OnCallStateChanged(string state, string name, bool isIncoming, bool isMute, bool onHold)
        {
            _service.OnWrapperCallStateChanged(state, name, isIncoming, isMute, onHold);
        }

        public override void OnCallStarted()
        {
            AppLogger.Log("VBotDelegate", "CallStarted");
        }

        public override void OnCallAccepted()
        {
            AppLogger.Log("VBotDelegate", "CallAccepted");
        }

        public override void OnCallMuteStateDidChange(bool muted)
        {
            _service._isMuted = muted;
            if (_service.CurrentCallState != null)
            {
                _service.EmitCallState(_service.CurrentCallState with { IsMute = muted });
            }
        }

        public override void OnCallEnded(string reason, string endedBy)
        {
            _service.OnWrapperCallEnded(reason, endedBy);
        }

        public override void OnPushKitTokenReceived(string token)
        {
            AppLogger.Log("PushKit", $"[Swift Wrapper] Nhận VoIP Token: {token}");
            AppDelegate.CachedPushKitToken = token;
            AppDelegate.OnVoipTokenReceived?.Invoke(token);
        }
    }
}
