# VBot Phone SDK - .NET MAUI Example

Dự án mẫu minh họa cách tích hợp và sử dụng thư viện NuGet **`VBot.Phone.SDK.Maui`** cho ứng dụng .NET MAUI (.NET 9.0), hỗ trợ gọi thoại VoIP 2 chiều trên Android và iOS.

---

## Cài đặt qua NuGet

Thư viện đã được phát hành chính thức trên nuget.org:

```bash
dotnet add package VBot.Phone.SDK.Maui
```

Hoặc thêm trực tiếp vào file `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="VBot.Phone.SDK.Maui" Version="1.0.1" />
</ItemGroup>
```

---

## Tính năng

- Gọi thoại VoIP 2 chiều (cuộc gọi đến và cuộc gọi đi).
- Gọi ra ngoài qua đầu số Hotline hoặc gọi nội bộ theo số máy nhánh (extension).
- Tích hợp giao diện cuộc gọi hệ thống:
  - iOS: Apple CallKit và PushKit (VoIP Push), nhận cuộc gọi từ màn hình khóa khi ứng dụng đang đóng.
  - Android: Foreground Service, Notification toàn màn hình, Firebase Cloud Messaging (FCM).
- Điều khiển cuộc gọi: bật/tắt micro (Mute), bật/tắt loa ngoài (Speaker), gửi tín hiệu DTMF.
- Hỗ trợ các môi trường: Production, Staging, Sandbox.

---

## Cấu trúc dự án mẫu

```
vbot_maui_example/
├── VBotMauiApp.sln                   # Visual Studio Solution
└── VBotMauiApp/                      # Dự án ứng dụng MAUI (.NET 9.0)
    ├── AppConfig.cs                  # Cấu hình môi trường (Production/Staging/Sandbox)
    ├── MauiProgram.cs                # Đăng ký SDK: builder.Services.AddVBotPhone()
    ├── Services/AppLogger.cs         # Logger hiển thị log trên màn hình demo
    ├── ViewModels/                   # MainViewModel, CallViewModel
    ├── Views/                        # MainPage.xaml, CallPage.xaml
    └── Platforms/
        ├── Android/                  # FCM Service, Notification, Permissions
        └── iOS/                      # PushKit Registry, CallKit, AudioSession
```

---

## Khởi chạy nhanh

### Yêu cầu môi trường

- .NET 9.0 SDK
- .NET MAUI Workload (`dotnet workload install maui`)
- Android SDK (API 34/35) cho Android
- Xcode 16.0+ cho iOS

### Cài đặt và build

```bash
# Clone repository
git clone https://github.com/VBotDevTeam/vbot_maui_example.git
cd vbot_maui_example

# Restore dependencies từ nuget.org
dotnet restore

# Chạy trên Android (thiết bị thật hoặc emulator)
dotnet build VBotMauiApp/VBotMauiApp.csproj -f net9.0-android -t:Run

# Chạy trên iOS Simulator
dotnet build VBotMauiApp/VBotMauiApp.csproj -f net9.0-ios -t:Run

# Chạy trên iPhone thật (để kiểm tra PushKit và CallKit)
dotnet build VBotMauiApp/VBotMauiApp.csproj -f net9.0-ios -t:Run -p:RuntimeIdentifier=ios-arm64
```

---

## Hướng dẫn tích hợp vào ứng dụng của bạn

### 1. Đăng ký Service trong MauiProgram.cs

```csharp
using VBot.Phone.SDK.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Đăng ký VBot Phone Service (Singleton)
        builder.Services.AddVBotPhone();

        return builder.Build();
    }
}
```

### 2. Sử dụng IVBotPhoneService trong ViewModel / Page

```csharp
using VBot.Phone.SDK.Maui;

public class MyCallViewModel
{
    private readonly IVBotPhoneService _phoneService;

    public MyCallViewModel(IVBotPhoneService phoneService)
    {
        _phoneService = phoneService;
        _phoneService.CallStateChanged += OnCallStateChanged;
    }

    public async Task ConnectAsync()
    {
        var config = new VBotCallConfig
        {
            Token = "JWT_TOKEN_HERE",
            Environment = "PRODUCTION" // hoặc "STAGING", "SANDBOX"
        };
        var displayName = await _phoneService.ConnectAsync(config);
    }

    public async Task MakeCallAsync(string phoneNumber, string hotline)
    {
        await _phoneService.MakeCallAsync(phoneNumber, hotline);
    }

    public async Task HangupAsync()
    {
        await _phoneService.HangupAsync();
    }

    private void OnCallStateChanged(object? sender, CallSinkState state)
    {
        // Xử lý thay đổi trạng thái: Calling, Ringing, Connected, Ended, v.v.
    }
}
```

---

## Bản quyền và Giấy phép

SDK và mã nguồn thuộc bản quyền của **VBot**. Xem chi tiết tại [LICENSE.txt](LICENSE.txt).
