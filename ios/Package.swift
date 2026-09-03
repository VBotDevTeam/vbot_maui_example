// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "VBotIosWrapper",
    platforms: [.iOS(.v14)],
    products: [
        .library(
            name: "VBotIosWrapper",
            type: .dynamic,
            targets: ["VBotIosWrapper"]
        ),
    ],
    targets: [
        .binaryTarget(
            name: "VBotPhoneSDK",
            path: "../VBot.iOS.Binding/VBotPhoneSDK.xcframework"
        ),
        .binaryTarget(
            name: "VoiceLib",
            path: "../VBot.iOS.Binding/VoiceLib.xcframework"
        ),
        .target(
            name: "VBotIosWrapper",
            dependencies: ["VBotPhoneSDK", "VoiceLib"],
            path: "Sources/VBotIosWrapper",
            swiftSettings: [
                .unsafeFlags(["-suppress-warnings"])
            ]
        ),
    ]
)
