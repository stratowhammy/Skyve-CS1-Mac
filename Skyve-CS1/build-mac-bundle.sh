#!/usr/bin/env bash
set -euo pipefail

# Script to build and package Skyve for macOS as a native self-contained .app bundle

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ARCH="${1:-arm64}" # arm64 or x64
RID="osx-$ARCH"
CONFIG="Release"
DIST_DIR="$ROOT_DIR/dist"
APP_BUNDLE="$DIST_DIR/Skyve.app"
APP_CONTENTS="$APP_BUNDLE/Contents"
APP_MACOS="$APP_CONTENTS/MacOS"
APP_RESOURCES="$APP_CONTENTS/Resources"

export PATH="$HOME/.dotnet:$PATH"

echo "=========================================================="
echo " Building Skyve for macOS ($ARCH / $CONFIG)"
echo "=========================================================="

# 1. Clean previous bundle
rm -rf "$APP_BUNDLE"
mkdir -p "$APP_MACOS"
mkdir -p "$APP_RESOURCES"

# 2. Publish single-file self-contained executable
echo "Publishing Skyve.App.CS1.Mac for $RID..."
PUBLISH_DIR="/tmp/skyve-publish-$ARCH"
rm -rf "$PUBLISH_DIR"

dotnet publish "$SCRIPT_DIR/Skyve.App.CS1.Mac/Skyve.App.CS1.Mac.csproj" \
    -c "$CONFIG" \
    -r "$RID" \
    -o "$PUBLISH_DIR"

# 3. Copy executable and native dylibs to bundle MacOS directory
echo "Copying published binaries to $APP_MACOS..."
cp "$PUBLISH_DIR/Skyve.App.CS1.Mac" "$APP_MACOS/"
chmod +x "$APP_MACOS/Skyve.App.CS1.Mac"
if ls "$PUBLISH_DIR"/*.dylib >/dev/null 2>&1; then
    cp "$PUBLISH_DIR"/*.dylib "$APP_MACOS/"
fi

# 4. Copy Icon
if [ -f "$SCRIPT_DIR/Skyve.App.CS1.Mac/icon.icns" ]; then
    echo "Copying icon.icns..."
    cp "$SCRIPT_DIR/Skyve.App.CS1.Mac/icon.icns" "$APP_RESOURCES/icon.icns"
fi

# 5. Create Info.plist
echo "Generating Info.plist..."
cat << 'EOF' > "$APP_CONTENTS/Info.plist"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>Skyve.App.CS1.Mac</string>
    <key>CFBundleIconFile</key>
    <string>icon.icns</string>
    <key>CFBundleIdentifier</key>
    <string>com.jadhajjar.skyve</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>Skyve</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>3.3.0</string>
    <key>CFBundleSignature</key>
    <string>????</string>
    <key>CFBundleVersion</key>
    <string>3.3.0</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSRequiresAquaSystemAppearance</key>
    <false/>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
</dict>
</plist>
EOF

# 6. Ad-hoc codesign the bundle
echo "Ad-hoc code signing Skyve.app..."
codesign --force --deep -s - "$APP_BUNDLE"

echo "Verifying code signature..."
codesign --verify --deep --strict --verbose=2 "$APP_BUNDLE"

# 7. Copy to local mod folder if it exists
MOD_APP_DIR="$HOME/Library/Application Support/Colossal Order/Cities_Skylines/Addons/Mods/Skyve/App"
if [ -d "$HOME/Library/Application Support/Colossal Order/Cities_Skylines/Addons/Mods/Skyve" ]; then
    echo "Deploying Skyve.app to in-game mod directory ($MOD_APP_DIR)..."
    mkdir -p "$MOD_APP_DIR"
    rm -rf "$MOD_APP_DIR/Skyve.app"
    cp -R "$APP_BUNDLE" "$MOD_APP_DIR/"
fi

echo "=========================================================="
echo " Successfully created: $APP_BUNDLE"
echo "=========================================================="
