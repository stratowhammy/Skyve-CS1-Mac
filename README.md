# Skyve for Cities: Skylines 1 — Native macOS Port

[![Platform](https://img.shields.io/badge/Platform-macOS%20(Apple%20Silicon%20%7C%20Intel)-brightgreen.svg)]()
[![Target Framework](https://img.shields.io/badge/.NET-8.0%20(Desktop)%20%7C%203.5%20(In--Game)-blue.svg)]()
[![UI Framework](https://img.shields.io/badge/UI-Avalonia%2011-purple.svg)]()
[![License](https://img.shields.io/badge/License-MIT-green.svg)]()

A native macOS port of **Skyve** (formerly Load Order Mod) for **Cities: Skylines 1**.

This port runs natively on Apple Silicon (`arm64`) and Intel (`x64`) without Wine, Rosetta, or Windows Registry dependencies. It features a modern **Avalonia UI** desktop application, native POSIX path resolution, and full compatibility with Unity 5.6's embedded Mono runtime.

---

## Architecture

Skyve operates across two distinct runtime environments:

1. **In-Game Assemblies (`net35` / Unity 5.6 Mono):**
   - **`SkyveIPatch`**: Patch Loader (Doorstop) pre-boot patcher that excludes inactive mods and assets before Unity loads them.
   - **`SkyveInjections`**: Mono.Cecil injection definitions.
   - **`SkyveMod`**: In-game `IUserMod` implementation with main menu button integration. Launches the native `Skyve.app` via `/usr/bin/open -a`.

2. **Standalone Desktop Application & Core Libraries (`net8.0` macOS):**
   - **`Skyve.App.CS1.Mac`**: Cross-platform desktop interface built with **Avalonia UI 11** using MVVM architecture.
   - **`Skyve.Domain` & `Skyve.Domain.CS1`**: Domain models for packages, playsets, DLCs, and compatibility rules.
   - **`Skyve.Systems` & `Skyve.Systems.CS1`**: Business logic, package scanning, Steam integration, and compatibility analysis.
   - **`Extensions`**: Cross-platform utilities, file handling, and symbolic linking.
   - **`MacPatchLoaderService`**: Automatically resolves Doorstop, Gatekeeper quarantine, `SYSTEM_VERSION_COMPAT=1`, and Paradox Launcher routing.

---

## Native macOS Features & Fixes

- **No Wine Required:** Eliminates all `Z:\` path mismatches, Windows Registry lookups, and Win32 P/Invokes.
- **Native POSIX Paths:**
  - Game Root: `~/Library/Application Support/Steam/steamapps/common/Cities_Skylines`
  - Managed Assemblies: `Cities.app/Contents/Resources/Data/Managed`
  - User Data: `~/Library/Application Support/Colossal Order/Cities_Skylines`
  - Workshop Content: `~/Library/Application Support/Steam/steamapps/workshop/content/255710`
  - Player Log: `~/Library/Logs/Unity/Player.log`
- **Patch Loader Mod & Doorstop Integration:**
  - Injects `export SYSTEM_VERSION_COMPAT=1;` into `Cities_Loader.sh` to resolve the Unity 5.6 Mono `Invalid macOS version` crash on modern macOS (Big Sur through Sequoia).
  - Automatically patches `Cities.app/Contents/Launcher/launcher-settings.json` to route Paradox Launcher launches through `Cities_Loader.sh`.
  - Automatically strips macOS Gatekeeper quarantine (`xattr -dr com.apple.quarantine`).
- **Steam Integration:** Parses `~/Library/Application Support/Steam/config/loginusers.vdf` for active user detection.
- **macOS Metadata Filtering:** Ignores `.DS_Store`, `.localized`, and `._*` AppleDouble files during directory scanning.

---

## Prerequisites

- macOS 11.0 (Big Sur) or higher (macOS 12 Monterey, 13 Ventura, 14 Sonoma, 15 Sequoia supported).
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Mono Framework MDK (for compiling in-game `net35` assemblies):
  ```bash
  brew install mono-mdk
  ```
  *(Or install Mono from the official [Mono Project](https://www.mono-project.com/download/stable/) package installer).*

---

## Building from Source

### 1. Build the Unified Solution
```bash
dotnet build Skyve-CS1-Mac.sln
```

### 2. Package Self-Contained `Skyve.app` Bundle
To build, ad-hoc codesign, and package the self-contained macOS `.app` bundle:

```bash
# For Apple Silicon (M1/M2/M3/M4)
./Skyve-CS1/build-mac-bundle.sh arm64

# For Intel Macs
./Skyve-CS1/build-mac-bundle.sh x64
```

The bundle is output to `dist/Skyve.app` and automatically deployed to `~/Applications/Skyve.app` and your Cities: Skylines local mod folder (`~/Library/Application Support/Colossal Order/Cities_Skylines/Addons/Mods/Skyve/App/Skyve.app`).

---

## CLI & Verification Commands

The `Skyve.app` executable supports standalone CLI verification:

```bash
# Check Doorstop & Patch Loader status on your system:
dist/Skyve.app/Contents/MacOS/Skyve.App.CS1.Mac --status

# Automatically repair Cities_Loader.sh and launcher-settings.json:
dist/Skyve.app/Contents/MacOS/Skyve.App.CS1.Mac --fix-patch-loader
```

---

## Upstream & Acknowledgements

- Original Skyve by **[JadHajjar](https://github.com/JadHajjar)**:
  - [Skyve-CS1](https://github.com/JadHajjar/Skyve-CS1)
  - [Skyve](https://github.com/JadHajjar/Skyve)
  - [Extensions](https://github.com/JadHajjar/Extensions)
- Krzychu1245 for **Patch Loader Mod** & Doorstop support.
- Avalonia UI team for the cross-platform UI framework.

---

## License

This project is licensed under the [MIT License](LICENSE).
