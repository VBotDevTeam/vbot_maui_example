using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Android.Content;
using Android.Media;
using Firebase.Messaging;
using VBotMauiApp.Models;
using VBotMauiApp.Platforms.Android;
using VBot.Android.SDK;
using VBot.Android.SDK.Enums;

namespace VBotMauiApp.Services;

public partial class VBotPhoneService : IVBotPhoneService
{
    private static VBotPhoneService? _instance;
    private VBotClient? _vbotClient;
    private AndroidClientListener? _clientListener;
    private bool _isMuted = false;
    private bool _isSpeakerOn = false;
    private string _displayName = string.Empty;
    private string _lastCallName = string.Empty;
    private bool _isConnected = false;
    private bool _isIncomingCall = false;
    private bool _answerRequested = false;
    private TaskCompletionSource<string?>? _connectTcs;
    private string _currentToken = string.Empty;
    private string _currentEnv = AppConfig.Environment;
    private string? _customBaseUrl = AppConfig.CustomBaseUrl;



    public Task InitializeAsync()
    {
        try
        {
            _instance = this;
            if (_vbotClient == null)
            {
                var context = global::Android.App.Application.Context;
                _vbotClient = new VBotClient(context);

                var env = ResolveEnvironment(_currentEnv);
                var config = _customBaseUrl != null
                    ? new VBotConfig(env, _customBaseUrl)
                    : new VBotConfig(env);

                _vbotClient.Setup(config);
                VBotLogger.A = true;
                VBotLogger.Instance.DebugMode = true;

                _clientListener = new AndroidClientListener(this);
                _vbotClient.AddListener(_clientListener);
                global::Android.Util.Log.Info("VBotPhone", "VBotClient initialized successfully.");
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("VBotPhone", $"InitializeAsync error: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private static VBotEnvironment ResolveEnvironment(string envStr)
    {
        return envStr?.ToUpperInvariant() switch
        {
            "STAGING" => VBotEnvironment.Staging,
            "SANDBOX" => VBotEnvironment.Sandbox,
            _ => VBotEnvironment.Production
        } ?? VBotEnvironment.Production;
    }

    public Task<bool> IsUserConnectedAsync()
    {
        if (_vbotClient != null)
        {
            _isConnected = _vbotClient.IsUserConnected || _vbotClient.StateAccount == AccountRegistrationState.Ok;
        }
        return Task.FromResult(_isConnected);
    }

    public Task<string?> GetUserDisplayNameAsync()
    {
        if (_vbotClient != null)
        {
            var name = _vbotClient.UserDisplayName();
            if (!string.IsNullOrEmpty(name))
            {
                _displayName = name;
            }
            else if (!string.IsNullOrEmpty(_vbotClient.AccountUsername))
            {
                _displayName = _vbotClient.AccountUsername;
            }
        }
        return Task.FromResult<string?>(_displayName);
    }

    public async Task<string?> ConnectAsync(VBotCallConfig config)
    {
        try
        {
            _instance = this;
            _currentToken = config.Token?.Trim() ?? string.Empty;
            _currentEnv = config.Environment ?? "PRODUCTION";
            _customBaseUrl = !string.IsNullOrEmpty(config.BaseUrl) ? config.BaseUrl : null;

            if (_vbotClient == null)
            {
                await InitializeAsync();
            }

            if (_vbotClient != null)
            {
                var env = ResolveEnvironment(_currentEnv);
                var sdkConfig = _customBaseUrl != null
                    ? new VBotConfig(env, _customBaseUrl)
                    : new VBotConfig(env);

                _vbotClient.Setup(sdkConfig);
                VBotLogger.A = true;
                VBotLogger.Instance.DebugMode = true;

                _connectTcs = new TaskCompletionSource<string?>();

                var completion = new VBotCompletionCallback((result, error) =>
                {
                    if (error != null)
                    {
                        global::Android.Util.Log.Error("VBotPhone", $"Connect error callback: {error.Message}");
                        _connectTcs?.TrySetException(new Exception(error.Message));
                        return;
                    }

                    var name = result?.ToString();
                    global::Android.Util.Log.Info("VBotPhone", $"Connect callback success: {name}");
                    
                    try
                    {
                        var sp = global::Android.App.Application.Context.GetSharedPreferences("PREFS_NAME", global::Android.Content.FileCreationMode.Private);
                        if (sp != null)
                        {
                            var keys = string.Join(", ", sp.All?.Keys ?? Array.Empty<string>());
                            global::Android.Util.Log.Info("VBotPhone", $"PREFS_NAME after connect ({sp.All?.Count}): [{keys}]");
                            if (sp.All != null)
                            {
                                foreach (var kv in sp.All)
                                {
                                    if (kv.Key != "PASSWORD" && kv.Key != "TOKEN" && kv.Key != "TOKEN_F")
                                    {
                                        global::Android.Util.Log.Info("VBotPhone", $"   [{kv.Key}] = '{kv.Value}'");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        global::Android.Util.Log.Error("VBotPhone", $"Check prefs error: {ex.Message}");
                    }

                    _displayName = string.IsNullOrEmpty(name) ? (_vbotClient.UserDisplayName() ?? "VBot User") : name;
                    _isConnected = true;
                    _connectTcs?.TrySetResult(_displayName);
                });

                string fcmToken = await GetFirebaseTokenAsync();
                global::Android.Util.Log.Info("VBotPhone", $"Calling VBotClient.Connect with token length={_currentToken.Length}, fcmToken={fcmToken}");
                _vbotClient.Connect(_currentToken, fcmToken, completion);

                return await _connectTcs.Task;
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("VBotPhone", $"Connect exception: {ex.Message}");
        }

        _isConnected = true;
        _displayName = "VBot User";
        return _displayName;
    }

    public Task<bool> DisconnectAsync()
    {
        try
        {
            if (CurrentCallState != null && CurrentCallState.State != "disconnected")
            {
                EmitCallState(CurrentCallState with { State = "disconnected" });
            }

            _connectTcs?.TrySetCanceled();
            _connectTcs = null;

            bool result = false;
            if (_vbotClient != null)
            {
                result = _vbotClient.Disconnect();
            }

            _isConnected = false;
            _displayName = string.Empty;
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("VBotPhone", $"Disconnect error: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<string?> StartCallAsync(string displayName, string phoneNumber, string hotline)
    {
        string targetHotline = hotline?.Trim() ?? string.Empty;
        string targetNumber = phoneNumber?.Trim() ?? string.Empty;
        _isIncomingCall = false;
        _lastCallName = string.IsNullOrEmpty(displayName) ? targetNumber : displayName;

        EmitCallState(new CallSinkState(
            Name: _lastCallName,
            State: "calling",
            IsIncoming: false,
            IsMute: false,
            OnHold: false
        ));

        try
        {
            if (_vbotClient != null)
            {
                AppLogger.Log("VBotPhone", $"StartOutgoingCall: hotline={targetHotline}, number={targetNumber}, externalCallId={_lastCallName}");
                _vbotClient.StartOutgoingCall(targetHotline, targetNumber, _lastCallName);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log("VBotPhone", $"StartCall error: {ex.Message}");
        }

        var context = global::Android.App.Application.Context;
        OngoingCallNotification.Show(context, _lastCallName);

        return Task.FromResult<string?>(phoneNumber);
    }

    private int _isAnswering = 0;
    private bool _isCallAnswered = false;

    public async Task AnswerAsync()
    {
        if (_isCallAnswered || CurrentCallState?.State == "confirmed")
        {
            AppLogger.Log("VBotPhone", "AnswerAsync: call already answered, skipping.");
            return;
        }

        if (Interlocked.CompareExchange(ref _isAnswering, 1, 0) != 0)
        {
            AppLogger.Log("VBotPhone", "AnswerAsync already in progress, skipping duplicate call.");
            return;
        }

        try
        {
            AppLogger.Log("VBotPhone", "AnswerAsync requested by user.");
            _answerRequested = true;

            const int maxAttempts = 40;
            const int delayMs = 250;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Dừng vòng lặp ngay nếu cuộc gọi đã được trả lời (confirmed) hoặc đã kết thúc
                if (!_answerRequested || _isCallAnswered || CurrentCallState?.State == "confirmed")
                {
                    AppLogger.Log("VBotPhone", $"AnswerAsync: loop terminated because call is answered (attempt {attempt + 1})");
                    return;
                }

                try
                {
                    if (_vbotClient != null)
                    {
                        bool hasActiveCall = _vbotClient.HasActiveCall || _vbotClient.IsCall;
                        if (hasActiveCall)
                        {
                            AppLogger.Log("VBotPhone", $"AnswerAsync: executing AnswerCall() at attempt {attempt + 1} (HasActiveCall=true)");
                            OngoingCallNotification.Cancel(global::Android.App.Application.Context);
                            _answerRequested = false;
                            _isCallAnswered = true;
                            try
                            {
                                _vbotClient.AnswerCall();
                            }
                            catch (Exception answerEx)
                            {
                                AppLogger.Log("VBotPhone", $"AnswerCall exception ignored: {answerEx.Message}");
                            }
                            return;
                        }
                        else
                        {
                            AppLogger.Log("VBotPhone", $"AnswerAsync: attempt {attempt + 1}/{maxAttempts} — waiting for active call object...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log("VBotPhone", $"AnswerAsync error at attempt {attempt + 1}: {ex.Message}");
                }

                await Task.Delay(delayMs);
            }

            if (_vbotClient != null && _answerRequested && !_isCallAnswered && CurrentCallState?.State != "confirmed")
            {
                AppLogger.Log("VBotPhone", "AnswerAsync: fallback executing AnswerCall() after timeout");
                OngoingCallNotification.Cancel(global::Android.App.Application.Context);
                _answerRequested = false;
                _isCallAnswered = true;
                try
                {
                    _vbotClient.AnswerCall();
                }
                catch (Exception fallbackEx)
                {
                    AppLogger.Log("VBotPhone", $"Fallback AnswerCall exception ignored: {fallbackEx.Message}");
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isAnswering, 0);
        }
    }

    public Task HangupAsync()
    {
        global::Android.Util.Log.Info("VBotPhone", "HangupAsync requested.");
        _answerRequested = false;
        try
        {
            _isMuted = false;
            _isSpeakerOn = false;
            OngoingCallNotification.Cancel(global::Android.App.Application.Context);

            _vbotClient?.EndCall();
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("VBotPhone", $"HangupAsync error: {ex.Message}");
        }

        EmitCallState(new CallSinkState(
            Name: _lastCallName,
            State: "disconnected",
            IsIncoming: false,
            IsMute: false,
            OnHold: false
        ));

        return Task.CompletedTask;
    }

    public Task MuteAsync()
    {
        _isMuted = !_isMuted;
        try
        {
            var context = global::Android.App.Application.Context;
            var audioManager = (AudioManager?)context.GetSystemService(Context.AudioService);
            if (audioManager != null)
            {
                audioManager.MicrophoneMute = _isMuted;
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("VBotPhone", $"MuteAsync error: {ex.Message}");
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
            _vbotClient?.OnOffSpeaker(_isSpeakerOn);
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("VBotPhone", $"SpeakerAsync error: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public async Task<List<VBotHotline>> GetHotlinesAsync()
    {
        var list = new List<VBotHotline>();
        try
        {
            if (string.IsNullOrEmpty(_currentToken))
                return list;

            var env = ResolveEnvironment(_currentEnv);
            var baseUrl = _customBaseUrl ?? (env == VBotEnvironment.Staging
                ? "https://open-api-staging.vbot.vn/v3.0/"
                : env == VBotEnvironment.Sandbox
                    ? "https://apivbottest.vpmedia.vn/open-api-v3/"
                    : "https://open-api-h01.vbot.vn/v3.0/");

            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            var url = $"{baseUrl}api/sdk/getHotline";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", _currentToken);
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataElem.EnumerateArray())
                    {
                        var name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
                        var phone = item.TryGetProperty("phoneNumber", out var p) ? p.GetString() : null;
                        if (!string.IsNullOrEmpty(phone))
                        {
                            list.Add(new VBotHotline(name ?? phone, phone));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("VBotPhone", $"GetHotlinesAsync error: {ex.Message}");
        }

        return list;
    }

    public static string CleanCallerName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var str = raw.Trim();

        // 1. Nếu có ngoặc kép "Tên Người Gọi" <sip:...> -> lấy phần trong ngoặc kép
        if (str.Contains('\"'))
        {
            var firstQuote = str.IndexOf('\"');
            var secondQuote = str.IndexOf('\"', firstQuote + 1);
            if (firstQuote >= 0 && secondQuote > firstQuote)
            {
                var insideQuotes = str.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Trim();
                if (!string.IsNullOrWhiteSpace(insideQuotes))
                {
                    return insideQuotes;
                }
            }
        }

        // 2. Nếu có dạng SIP URI <sip:username@domain>
        if (str.Contains("sip:", StringComparison.OrdinalIgnoreCase))
        {
            var sipIdx = str.IndexOf("sip:", StringComparison.OrdinalIgnoreCase);
            var afterSip = str.Substring(sipIdx + 4);
            var atIdx = afterSip.IndexOf('@');
            if (atIdx > 0)
            {
                return afterSip.Substring(0, atIdx).Trim();
            }
            var endIdx = afterSip.IndexOfAny(new[] { '>', ';', ':', ' ' });
            if (endIdx > 0)
            {
                return afterSip.Substring(0, endIdx).Trim();
            }
            return afterSip.Trim('>', '<', ' ', '\"');
        }

        return str.Trim('\"', '<', '>', ' ');
    }

    public static void HandleIncomingPushNotification(Dictionary<string, string> payload)
    {
        try
        {
            AppLogger.Log("VBotPhone", $"FCM Push notification received: {JsonSerializer.Serialize(payload)}");

            string caller = string.Empty;
            if (payload.TryGetValue("name", out var n) && !string.IsNullOrWhiteSpace(n))
            {
                caller = n.Trim();
            }

            if (string.IsNullOrEmpty(caller) && payload.TryGetValue("caller", out var c) && !string.IsNullOrWhiteSpace(c))
            {
                caller = c.Trim();
            }

            if (_instance != null)
            {
                _instance._isIncomingCall = true;
                _instance._isCallAnswered = false;
                if (!string.IsNullOrEmpty(caller))
                {
                    _instance._lastCallName = CleanCallerName(caller);
                }
                else if (string.IsNullOrEmpty(_instance._lastCallName))
                {
                    _instance._lastCallName = "Cuộc gọi đến";
                }
                AppLogger.Log("VBotPhone", $"Set incoming caller: {_instance._lastCallName}");
            }

            var javaMap = new global::Java.Util.HashMap();
            foreach (var kv in payload)
            {
                javaMap.Put(kv.Key, kv.Value);
            }

            VBotClient clientToUse;
            if (_instance?._vbotClient != null)
            {
                clientToUse = _instance._vbotClient;
            }
            else
            {
                var context = global::Android.App.Application.Context;
                clientToUse = new VBotClient(context);
                VBotLogger.A = true;
                VBotLogger.Instance.DebugMode = true;
                var env = ResolveEnvironment(AppConfig.Environment);
                var config = AppConfig.CustomBaseUrl != null
                    ? new VBotConfig(env, AppConfig.CustomBaseUrl)
                    : new VBotConfig(env);
                clientToUse.Setup(config);
            }

            try
            {
                var sp = global::Android.App.Application.Context.GetSharedPreferences("PREFS_NAME", global::Android.Content.FileCreationMode.Private);
                if (sp != null)
                {
                    var keys = string.Join(", ", sp.All?.Keys ?? Array.Empty<string>());
                    AppLogger.Log("VBotPhone", $"PREFS_NAME before notificationCall ({sp.All?.Count}): [{keys}]");
                    if (sp.All != null)
                    {
                        foreach (var kv in sp.All)
                        {
                            if (kv.Key != "PASSWORD" && kv.Key != "TOKEN" && kv.Key != "TOKEN_F")
                            {
                                AppLogger.Log("VBotPhone", $"   [{kv.Key}] = '{kv.Value}'");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("VBotPhone", $"Check prefs error: {ex.Message}");
            }

            IntPtr methodId = global::Android.Runtime.JNIEnv.GetMethodID(
                clientToUse.Class.Handle,
                "notificationCall",
                "(Ljava/util/HashMap;)V"
            );
            global::Android.Runtime.JNIEnv.CallVoidMethod(
                clientToUse.Handle,
                methodId,
                new global::Android.Runtime.JValue(javaMap)
            );
            AppLogger.Log("VBotPhone", "Executed notificationCall with Java HashMap via JNI");
        }
        catch (Exception ex)
        {
            AppLogger.Log("VBotPhone", $"HandleIncomingPushNotification error: {ex.Message}");
        }
    }

    private async Task<string> GetFirebaseTokenAsync()
    {
        try
        {
            var task = FirebaseMessaging.Instance.GetToken();
            var result = await Task.Run(() =>
            {
                while (!task.IsComplete)
                {
                    System.Threading.Thread.Sleep(50);
                }
                return task.IsSuccessful ? task.Result?.ToString() : null;
            });

            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Warn("VBotPhone", $"GetFirebaseTokenAsync failed: {ex.Message}");
        }

        return "dummy_fcm_token_sample";
    }

    public void Dispose()
    {
        try
        {
            if (_clientListener != null && _vbotClient != null)
            {
                _vbotClient.RemoveListener(_clientListener);
            }
        }
        catch { }
    }

    private class VBotHotlineDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }
    }

    private class VBotCompletionCallback : Java.Lang.Object, IVBotCompletion
    {
        private readonly Action<Java.Lang.Object?, VBotError?> _callback;

        public VBotCompletionCallback(Action<Java.Lang.Object?, VBotError?> callback)
        {
            _callback = callback;
        }

        public void OnResult(Java.Lang.Object? result, VBotError? error)
        {
            _callback(result, error);
        }
    }

    private class AndroidClientListener : ClientListener
    {
        private readonly VBotPhoneService _service;

        public AndroidClientListener(VBotPhoneService service)
        {
            _service = service;
        }

        public override void OnUserConnected(string displayName)
        {
            base.OnUserConnected(displayName);
            global::Android.Util.Log.Info("VBotPhone", $">>> OnUserConnected: {displayName}");
            _service._isConnected = true;
            _service._displayName = displayName;
            _service._connectTcs?.TrySetResult(displayName);
        }

        public override void OnAccountRegistrationState(AccountRegistrationState status, string reason)
        {
            base.OnAccountRegistrationState(status, reason);
            global::Android.Util.Log.Info("VBotPhone", $">>> OnAccountRegistrationState: {status?.Name()}, reason={reason}");
        }

        public override void OnExternalCallId(string externalCallId)
        {
            base.OnExternalCallId(externalCallId);
            global::Android.Util.Log.Info("VBotPhone", $">>> OnExternalCallId: {externalCallId}");
        }

        public override void OnCallState(CallState state)
        {
            base.OnCallState(state);

            if (_service._vbotClient == null)
            {
                return;
            }

            string stateStr = "none";

            if (state == CallState.Calling)
            {
                stateStr = "calling";
                if (_service._isIncomingCall)
                {
                    AppLogger.Log("VBotPhone", "OnCallState(Calling) received during incoming call");
                }
                else
                {
                    _service._isIncomingCall = false;
                }
            }
            else if (state == CallState.Early)
            {
                stateStr = "early";
            }
            else if (state == CallState.Incoming)
            {
                stateStr = "incoming";
                _service._isIncomingCall = true;

                // Nếu user đã bấm Trả lời trên UI trước đó, trả lời ngay
                if (_service._answerRequested && !_service._isCallAnswered)
                {
                    AppLogger.Log("VBotPhone", "OnCallState(Incoming) received and UI Answer was requested -> auto answering now");
                    _ = _service.AnswerAsync();
                }
            }
            else if (state == CallState.Connecting)
            {
                stateStr = "connecting";
                if (_service._answerRequested && !_service._isCallAnswered)
                {
                    _ = _service.AnswerAsync();
                }
            }
            else if (state == CallState.Confirmed)
            {
                stateStr = "confirmed";
                _service._answerRequested = false;
                _service._isCallAnswered = true;
                OngoingCallNotification.Show(
                    global::Android.App.Application.Context,
                    !string.IsNullOrEmpty(_service._lastCallName) ? _service._lastCallName : "Cuộc gọi"
                );
            }
            else if (state == CallState.Disconnected || state == CallState.Null)
            {
                stateStr = "disconnected";
                _service._answerRequested = false;
                _service._isCallAnswered = false;
                _service._isMuted = false;
                _service._isSpeakerOn = false;
                OngoingCallNotification.Cancel(global::Android.App.Application.Context);
            }

            // Cập nhật tên người gọi nếu SDK cung cấp và chưa có tên hợp lệ
            if (state != CallState.Disconnected && state != CallState.Null)
            {
                try
                {
                    if (string.IsNullOrEmpty(_service._lastCallName) || _service._lastCallName == "Cuộc gọi đến" || _service._lastCallName == "Cuộc gọi")
                    {
                        var callName = _service._vbotClient.CallName();
                        var cleaned = CleanCallerName(callName);
                        if (!string.IsNullOrEmpty(cleaned))
                        {
                            _service._lastCallName = cleaned;
                        }
                        else
                        {
                            _service._lastCallName = _service._isIncomingCall ? "Cuộc gọi đến" : "Cuộc gọi";
                        }
                    }
                    else
                    {
                        _service._lastCallName = CleanCallerName(_service._lastCallName);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log("VBotPhone", $"CallName() error: {ex.Message}");
                }
            }

            AppLogger.Log("VBotPhone", $">>> OnCallState mapped: state={stateStr} (raw={state?.Name()}), name={_service._lastCallName}, isIncoming={_service._isIncomingCall}");

            _service.EmitCallState(new CallSinkState(
                Name: _service._lastCallName,
                State: stateStr,
                IsIncoming: _service._isIncomingCall,
                IsMute: _service._isMuted,
                OnHold: false
            ));

            if (state == CallState.Disconnected || state == CallState.Null)
            {
                _service._lastCallName = string.Empty;
                _service._isIncomingCall = false;
                _service._answerRequested = false;
            }
        }

        public override void OnCallEnded(VBotEndCallReason reason, VBotCallEndParty endedBy)
        {
            base.OnCallEnded(reason, endedBy);
            global::Android.Util.Log.Info("VBotPhone", $">>> OnCallEnded: reason={reason?.Name()}, endedBy={endedBy?.Name()}");
        }

        public override void OnErrorCode(int erCode, string message)
        {
            base.OnErrorCode(erCode, message);
            global::Android.Util.Log.Error("VBotPhone", $">>> OnErrorCode: {erCode} - {message}");
        }
    }
}
