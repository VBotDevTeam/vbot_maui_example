using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using VBot.Phone.SDK.Maui;
using VBotMauiApp.Services;
using VBotMauiApp.Views;

namespace VBotMauiApp.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IVBotPhoneService _phoneService;

    [ObservableProperty]
    private string _token = string.Empty; // Nhập JWT Token từ trang quản trị VBot

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _voipTokenStatus = string.Empty;

    [ObservableProperty]
    private string _logText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isCalling;

    [ObservableProperty]
    private VBotHotline? _selectedHotline;

    [ObservableProperty]
    private List<VBotHotline> _hotlines = [];

    // Độ dài dưới 6 ký tự là mã nhánh nội bộ (không qua Hotline)
    public bool IsMemberCall => !string.IsNullOrWhiteSpace(PhoneNumber) && PhoneNumber.Trim().Length < 6;

    public string CallButtonText => IsCalling
        ? "Đang gọi..."
        : (IsMemberCall ? "Gọi thành viên" : "Gọi điện");

    public string PhoneFieldLabel => IsMemberCall ? "Mã nhánh thành viên" : "Số điện thoại / Mã nhánh";

    public string PhoneHelperText => IsMemberCall ? "Tự động gọi nội bộ (không qua Hotline)" : string.Empty;

    public MainViewModel(IVBotPhoneService phoneService)
    {
        _phoneService = phoneService;
        Title = "VBot Phone MAUI Demo";

        _phoneService.CallStateChanged += OnCallStateChanged;

        LogText = AppLogger.GetAllLogs();
        AppLogger.LogAdded += (entry) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LogText = AppLogger.GetAllLogs();
            });
        };

#if IOS
        AppDelegate.OnVoipTokenReceived += (token) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                VoipTokenStatus = $"VoIP Token: {token[..Math.Min(12, token.Length)]}... (Đã sẵn sàng)";
            });
        };
#endif
    }

    partial void OnPhoneNumberChanged(string value)
    {
        OnPropertyChanged(nameof(IsMemberCall));
        OnPropertyChanged(nameof(CallButtonText));
        OnPropertyChanged(nameof(PhoneFieldLabel));
        OnPropertyChanged(nameof(PhoneHelperText));
    }

    public async Task InitializeAsync()
    {
        await _phoneService.InitializeAsync();
        UpdateVoipStatus();
        await CheckConnectionStatusAsync();

        if (!IsConnected)
        {
            var savedToken = Preferences.Get("SavedVBotToken", string.Empty);
            if (!string.IsNullOrWhiteSpace(savedToken))
            {
                Token = savedToken;
                await ConnectAsync();
            }
        }
    }

    private void UpdateVoipStatus()
    {
#if IOS
        VoipTokenStatus = !string.IsNullOrEmpty(AppDelegate.CachedPushKitToken)
            ? $"VoIP Token: {AppDelegate.CachedPushKitToken[..Math.Min(12, AppDelegate.CachedPushKitToken.Length)]}..."
            : "VoIP PushKit: Đang chờ cấp Token từ Apple...";
#else
        VoipTokenStatus = string.Empty;
#endif
    }

    private async Task CheckConnectionStatusAsync()
    {
        UpdateVoipStatus();
        bool connected = await _phoneService.IsUserConnectedAsync();
        if (connected)
        {
            DisplayName = await _phoneService.GetUserDisplayNameAsync() ?? string.Empty;
            IsConnected = true;
            await LoadHotlinesAsync();
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(Token)) return;

        IsLoading = true;
        try
        {
            var trimmedToken = Token.Trim();
            Preferences.Set("SavedVBotToken", trimmedToken);
            var config = new VBotCallConfig(trimmedToken, AppConfig.Environment, AppConfig.CustomBaseUrl);
            var result = await _phoneService.ConnectAsync(config);

            if (!string.IsNullOrEmpty(result))
            {
                DisplayName = result;
                IsConnected = true;
                await LoadHotlinesAsync();
            }
            else
            {
                DisplayName = "Lỗi kết nối";
                IsConnected = false;
            }
        }
        catch (Exception ex)
        {
            DisplayName = $"Lỗi: {ex.Message}";
            IsConnected = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        IsLoading = true;
        try
        {
            Preferences.Remove("SavedVBotToken");
            await _phoneService.DisconnectAsync();
        }
        finally
        {
            IsConnected = false;
            DisplayName = string.Empty;
            Hotlines = [];
            SelectedHotline = null;
            IsLoading = false;
        }
    }

    private async Task LoadHotlinesAsync()
    {
        var list = await _phoneService.GetHotlinesAsync();
        if (list != null && list.Count > 0)
        {
            Hotlines = list;
            SelectedHotline = list[0];
        }
    }

    [RelayCommand]
    private async Task CallAsync()
    {
        var input = PhoneNumber.Trim();
        if (string.IsNullOrEmpty(input)) return;

        IsCalling = true;
        try
        {
            string hotlineNumber = IsMemberCall ? string.Empty : (SelectedHotline?.PhoneNumber ?? string.Empty);
            await _phoneService.StartCallAsync(input, input, hotlineNumber);
        }
        finally
        {
            IsCalling = false;
        }
    }

    [RelayCommand]
    private void ClearPhoneNumber()
    {
        PhoneNumber = string.Empty;
    }

    [RelayCommand]
    private async Task CopyLogsAsync()
    {
        var logs = AppLogger.GetAllLogs();
        if (!string.IsNullOrEmpty(logs))
        {
            await Clipboard.Default.SetTextAsync(logs);
            AppLogger.Log("UI", "Đã sao chép toàn bộ log vào Clipboard");
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        AppLogger.ClearLogs();
        LogText = string.Empty;
    }

    private int _navigatingToCallPage = 0;

    private async void OnCallStateChanged(object? sender, CallSinkState state)
    {
        AppLogger.Log("MainVM", $"OnCallStateChanged: State={state.State}, Name={state.Name}, IsIncoming={state.IsIncoming}, IsMute={state.IsMute}");
        
        if (state.State == "disconnected" || state.State == "none")
        {
            AppLogger.Log("MainVM", "Cuộc gọi kết thúc, reset navigation lock");
            _navigatingToCallPage = 0;
            return;
        }

        bool isIncomingRinging = (state.State == "incoming" || state.State == "early" || state.State == "calling" || state.State == "connecting" || state.State == "confirmed") && state.IsIncoming;
        bool isOutgoingCalling = (state.State == "calling" || state.State == "early") && !state.IsIncoming;

        if (isIncomingRinging || isOutgoingCalling)
        {
            if (System.Threading.Interlocked.Exchange(ref _navigatingToCallPage, 1) == 1)
            {
                AppLogger.Log("MainVM", "Navigation lock đang bật, bỏ qua");
                return;
            }

            AppLogger.Log("MainVM", $"Chuyển sang CallPage (Incoming={isIncomingRinging}, Outgoing={isOutgoingCalling})");
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (Shell.Current != null && Shell.Current.CurrentPage is not CallPage)
                    {
                        await Shell.Current.GoToAsync(nameof(CallPage));
                        AppLogger.Log("MainVM", "Chuyển sang CallPage thành công");
                    }
                    else
                    {
                        AppLogger.Log("MainVM", $"Bỏ qua chuyển trang: CurrentPage={Shell.Current?.CurrentPage?.GetType().Name ?? "null"}");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log("MainVM", $"Lỗi chuyển sang CallPage: {ex.Message}");
                }
                finally
                {
                    await Task.Delay(500);
                    _navigatingToCallPage = 0;
                }
            });
        }
        else
        {
            AppLogger.Log("MainVM", $"Trạng thái '{state.State}' không kích hoạt chuyển trang");
        }
    }
}
