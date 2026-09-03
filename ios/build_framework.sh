#!/bin/bash
# Build VBotIosWrapper.xcframework từ Swift source code
# Yêu cầu: Xcode command line tools

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
FRAMEWORK_NAME="VBotIosWrapper"
BUILD_DIR="$SCRIPT_DIR/.build"
OUTPUT_DIR="$PROJECT_ROOT/VBot.iOS.Binding"
VBOT_SDK="$OUTPUT_DIR/VBotPhoneSDK.xcframework"

echo "Building $FRAMEWORK_NAME.xcframework..."

# Kiểm tra VBotPhoneSDK.xcframework
if [ ! -d "$VBOT_SDK" ]; then
    echo "VBotPhoneSDK.xcframework not found. Downloading..."
    git clone --depth 1 --branch 1.1.9 https://github.com/VBotDevTeam/VBotPhoneSDKiOS-Public.git /tmp/vbot_ios_sdk
    cp -R /tmp/vbot_ios_sdk/iOS/VBotPhoneSDK.xcframework "$OUTPUT_DIR/"
    rm -rf /tmp/vbot_ios_sdk
fi

# Clean build
rm -rf "$BUILD_DIR"

cd "$SCRIPT_DIR"

# Build cho device (arm64)
echo "Building for iOS device (arm64)..."
xcodebuild build \
    -scheme VBotIosWrapper \
    -destination 'generic/platform=iOS' \
    -derivedDataPath "$BUILD_DIR/device" \
    -configuration Release \
    BUILD_LIBRARY_FOR_DISTRIBUTION=YES \
    SKIP_INSTALL=NO \
    2>&1 | grep -E "(BUILD|error:)" || true

# Build cho simulator
echo "Building for iOS simulator..."
xcodebuild build \
    -scheme VBotIosWrapper \
    -destination 'generic/platform=iOS Simulator' \
    -derivedDataPath "$BUILD_DIR/simulator" \
    -configuration Release \
    BUILD_LIBRARY_FOR_DISTRIBUTION=YES \
    SKIP_INSTALL=NO \
    2>&1 | grep -E "(BUILD|error:)" || true

# Tìm framework paths
DEVICE_FW=$(find "$BUILD_DIR/device" -path "*/PackageFrameworks/$FRAMEWORK_NAME.framework" -type d | head -1)
SIM_FW=$(find "$BUILD_DIR/simulator" -path "*/PackageFrameworks/$FRAMEWORK_NAME.framework" -type d | head -1)

if [ -z "$DEVICE_FW" ] || [ -z "$SIM_FW" ]; then
    echo "Build failed - could not find framework"
    exit 1
fi

# Thêm headers vào framework (SPM không tự thêm)
for FW_PATH in "$DEVICE_FW" "$SIM_FW"; do
    mkdir -p "$FW_PATH/Headers" "$FW_PATH/Modules"
    ARCH_DIR=$(dirname "$FW_PATH" | sed 's|PackageFrameworks||')
    HEADER=$(find "$BUILD_DIR" -path "*$(basename $ARCH_DIR)*/*arm64*/$FRAMEWORK_NAME-Swift.h" | head -1)
    if [ -n "$HEADER" ]; then
        cp "$HEADER" "$FW_PATH/Headers/"
    fi
    echo "#import \"$FRAMEWORK_NAME-Swift.h\"" > "$FW_PATH/Headers/$FRAMEWORK_NAME.h"
    cat > "$FW_PATH/Modules/module.modulemap" << EOF
framework module $FRAMEWORK_NAME {
  umbrella header "$FRAMEWORK_NAME.h"
  export *
  module * { export * }
}
EOF
done

# Tạo xcframework
echo "Creating xcframework..."
rm -rf "$OUTPUT_DIR/$FRAMEWORK_NAME.xcframework"
xcodebuild -create-xcframework \
    -framework "$DEVICE_FW" \
    -framework "$SIM_FW" \
    -output "$OUTPUT_DIR/$FRAMEWORK_NAME.xcframework"

# Cleanup
rm -rf "$BUILD_DIR"

echo ""
echo "Done! Output: $OUTPUT_DIR/$FRAMEWORK_NAME.xcframework"
