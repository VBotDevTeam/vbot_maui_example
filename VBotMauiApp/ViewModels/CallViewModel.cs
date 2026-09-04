using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using VBot.Phone.SDK.Maui;
using VBotMauiApp.Services;
using VBotMauiApp.Views;

namespace VBotMauiApp.ViewModels;

public partial class CallViewModel : BaseViewModel
{
    private readonly IVBotPhoneService _phoneService;
    private IDispatcherTimer? _timer;
    private DateTime? _callStartTime;
    private bool _isSubscribed;

    [ObservableProperty]
    private string _callerName = "Không xác định";

    [ObservableProperty]
    private string _callStatusText = "Đang kết nối...";

    [ObservableProperty]
    private string _callDuration = "00:00";

    [ObservableProperty]
    private bool _isConfirmed;

    [ObservableProperty]
    private bool _isIncoming;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private bool _isSpeakerOn;

    public CallViewModel(IVBotPhoneService phoneService)
    {
        _phoneService = phoneService;
        Title = "Cuộc gọi VBot";
    }

    public void OnAppearing()
    {
        // Subscribe event khi page appearing, tránh trùng lặp
        if (!_isSubscribed)
        {
            _phoneService.CallStateChanged += OnCallStateChanged;
            _isSubscribed = true;
        }
        UpdateCallState(_phoneService.CurrentCallState);
    }

    public void OnDisappearing()
    {
        // Unsubscribe event khi page disappearing để tránh leak và duplicate handlers
        if (_isSubscribed)
        {
            _phoneService.CallStateChanged -= OnCallStateChanged;
            _isSubscribed = false;
        }
        StopTimer();
    }

    private int _navigationLock = 0;

    private void OnCallStateChanged(object? sender, CallSinkState state)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            Debug.WriteLine($"[CallVM] OnCallStateChanged: State={state.State}, Name={state.Name}, IsIncoming={state.IsIncoming}");

            if (state.State == "disconnected" || state.State == "none")
            {
                Debug.WriteLine("[CallVM] Cuộc gọi kết thúc, quay lại màn hình chính");
                StopTimer();
                await NavigateBackToMainAsync();
                return;
            }

            UpdateCallState(state);
        });
    }

    private void UpdateCallState(CallSinkState? state)
    {
        if (state == null) return;

        var name = !string.IsNullOrEmpty(state.Name) ? state.Name : "Không xác định";
        CallerName = CleanDisplayName(name);
        IsIncoming = state.IsIncoming && (state.State == "incoming" || state.State == "early");
        IsConfirmed = state.State == "confirmed";
        IsMuted = state.IsMute;

        CallStatusText = state.State switch
        {
            "calling" => state.IsIncoming ? "Đang kết nối..." : "Đang gọi đi...",
            "early" => state.IsIncoming ? "Cuộc gọi đến..." : "Đang đổ chuông...",
            "incoming" => "Cuộc gọi đến...",
            "connecting" => "Đang kết nối...",
            "confirmed" => "Đang trong cuộc gọi",
            "disconnected" => "Cuộc gọi kết thúc",
            _ => state.State
        };

        if (state.State == "confirmed" && _callStartTime == null)
        {
            StartTimer();
        }
    }

    private void StartTimer()
    {
        _callStartTime = DateTime.Now;
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer != null)
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                if (_callStartTime == null) return;
                var diff = DateTime.Now - _callStartTime.Value;
                CallDuration = diff.Hours > 0
                    ? $"{diff.Hours:D2}:{diff.Minutes:D2}:{diff.Seconds:D2}"
                    : $"{diff.Minutes:D2}:{diff.Seconds:D2}";
            };
            _timer.Start();
        }
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
        _callStartTime = null;
        CallDuration = "00:00";
    }

    [RelayCommand]
    private async Task AnswerAsync()
    {
        IsIncoming = false;
        CallStatusText = "Đang kết nối...";
        await _phoneService.AnswerAsync();
    }

    [RelayCommand]
    private async Task HangupAsync()
    {
        StopTimer();
        try
        {
            await _phoneService.HangupAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Hangup error: {ex.Message}");
        }
        await NavigateBackToMainAsync();
    }

    private Task NavigateBackToMainAsync()
    {
        if (System.Threading.Interlocked.Exchange(ref _navigationLock, 1) == 1)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (Shell.Current != null && Shell.Current.CurrentPage is CallPage)
                {
                    await Shell.Current.GoToAsync("..", false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NavigateBackToMainAsync error: {ex.Message}");
            }
            finally
            {
                tcs.TrySetResult();
            }
        });

        return tcs.Task;
    }

    [RelayCommand]
    private async Task ToggleMuteAsync()
    {
        await _phoneService.MuteAsync();
    }

    [RelayCommand]
    private async Task ToggleSpeakerAsync()
    {
        await _phoneService.SpeakerAsync();
        IsSpeakerOn = !IsSpeakerOn;
    }

    private static string CleanDisplayName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Không xác định";

        var str = raw.Trim();

        // 1. Lấy chuỗi trong ngoặc kép nếu có: "Tên" <sip:...>
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

        // 2. Lấy username từ sip:username@domain nếu có
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
}
