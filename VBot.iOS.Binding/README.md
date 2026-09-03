# VBot iOS Binding Library (.NET 9.0)

Thư viện Binding C# cho VBot Phone iOS Native SDK (`VBotPhoneSDKiOS-Public`).

## Cách tích hợp XCFramework:
1. Build `VBotPhoneSDK.xcframework` từ repo `VBotPhone-iOS-Public` (chạy `./build.sh` hoặc export từ Pods).
2. Copy `VBotPhoneSDK.xcframework` vào thư mục này.
3. Kiểm tra các định nghĩa export trong `ApiDefinition.cs` và `StructsAndEnums.cs`.
