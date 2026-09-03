# VBot Phone SDK - .NET MAUI Example

Dự án mẫu và thư viện Native Binding tích hợp VBot Phone SDK cho .NET MAUI (.NET 9.0), hỗ trợ gọi thoại VoIP 2 chiều trên Android và iOS.

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

## Cấu trúc dự án

```
vbot_maui_example/
├── VBotMauiApp.sln                   # Visual Studio Solution
├── VBotMauiApp/                      # Dự án ứng dụng MAUI (.NET 9.0)
│   ├── AppConfig.cs                  # Cấu hình môi trường (Production/Staging/Sandbox)
│   ├── MauiProgram.cs                # Dependency Injection
│   ├── Models/                       # VBotCallConfig, CallSinkState, VBotHotline
│   ├── Services/                     # IVBotPhoneService, AppLogger
│   ├── ViewModels/                   # MainViewModel, CallViewModel
│   ├── Views/                        # MainPage.xaml, CallPage.xaml
│   └── Platforms/
│       ├── Android/                  # FCM Service, Notification, Permissions
│       └── iOS/                      # PushKit Registry, CallKit, AudioSession
├── VBot.Android.Binding/             # Binding Library cho Android Native SDK (AAR)
└── VBot.iOS.Binding/                 # Binding Library cho iOS Native SDK (XCFramework)
```

Các file nhị phân native (`.aar` và `.xcframework`) sẽ được tự động tải từ repository khi build lần đầu.

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

# Restore dependencies
dotnet restore

# Chạy trên Android (thiết bị thật hoặc emulator)
dotnet build VBotMauiApp/VBotMauiApp.csproj -f net9.0-android -t:Run

# Chạy trên iOS Simulator
dotnet build VBotMauiApp/VBotMauiApp.csproj -f net9.0-ios -t:Run

# Chạy trên iPhone thật (để kiểm tra PushKit và CallKit)
dotnet build VBotMauiApp/VBotMauiApp.csproj -f net9.0-ios -t:Run -p:RuntimeIdentifier=ios-arm64
```

### Cấu hình Push Notification

- **Android**: Copy file `google-services.json` vào `VBotMauiApp/Platforms/Android/google-services.json` (tham khảo file mẫu `google-services.example.json`). Chi tiết cấu hình Firebase và VBot Portal: [VBot Android Push Documentation](https://vbotdevteam.github.io/vbot-documentation/android-sdk/push-notification.html).
- **iOS**: Tạo chứng chỉ VoIP Services Certificate và cấu hình trên VBot Portal. Chi tiết cấu hình: [VBot iOS Push Documentation](https://vbotdevteam.github.io/vbot-documentation/ios-sdk/push-notification.html).

### Các bước sử dụng

1. Thiết lập môi trường kết nối trong `VBotMauiApp/AppConfig.cs`.
2. Nhập VBot User Token (JWT).
3. Bấm **Kết nối** để đăng nhập tổng đài và tải danh sách Hotline.
4. Chọn Hotline, nhập số điện thoại (hoặc số máy nhánh dưới 6 ký tự để gọi nội bộ).
5. Bấm **Gọi điện** để thực hiện cuộc gọi.

---

## Tích hợp vào dự án MAUI khác

### 1. Tham chiếu Binding Projects trong file `.csproj`

```xml
<!-- Android -->
<ItemGroup Condition="$(TargetFramework.Contains('-android'))">
    <ProjectReference Include="..\VBot.Android.Binding\VBot.Android.Binding.csproj" />
    <PackageReference Include="Xamarin.Firebase.Messaging" Version="123.4.0" />
    <GoogleServicesJson Include="Platforms\Android\google-services.json" />
</ItemGroup>

<!-- iOS -->
<ItemGroup Condition="$(TargetFramework.Contains('-ios'))">
    <ProjectReference Include="..\VBot.iOS.Binding\VBot.iOS.Binding.csproj" />
</ItemGroup>
```

### 2. Đăng ký Dependency Injection (`MauiProgram.cs`)

```csharp
builder.Services.AddSingleton<IVBotPhoneService, VBotPhoneService>();
```

### 3. Gọi API qua `IVBotPhoneService`

```csharp
// Đăng nhập tổng đài
var config = new VBotCallConfig(Token: "YOUR_JWT_TOKEN", Environment: "PRODUCTION");
string? displayName = await _phoneService.ConnectAsync(config);

// Lấy danh sách hotline
List<VBotHotline> hotlines = await _phoneService.GetHotlinesAsync();

// Gọi đi
await _phoneService.StartCallAsync(displayName: "0901234567", phoneNumber: "0901234567", hotline: "02473000000");

// Trả lời / Cúp máy
await _phoneService.AnswerAsync();
await _phoneService.HangupAsync();

// Điều khiển âm thanh
await _phoneService.MuteAsync();
await _phoneService.SpeakerAsync();

// Đăng xuất
await _phoneService.DisconnectAsync();
```

---

## Tài liệu tham khảo chính thức

- [VBot Android SDK Documentation](https://vbotdevteam.github.io/vbot-documentation/android-sdk/)
- [VBot iOS SDK Documentation](https://vbotdevteam.github.io/vbot-documentation/ios-sdk/)
- [VBot Android Push Notification](https://vbotdevteam.github.io/vbot-documentation/android-sdk/push-notification.html)
- [VBot iOS Push Notification](https://vbotdevteam.github.io/vbot-documentation/ios-sdk/push-notification.html)
