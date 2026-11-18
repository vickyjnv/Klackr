# Klackr

Mechanical keyboard and mouse click sound simulator for macOS and Windows. Klackr plays satisfying tactile keystrokes and microswitch mouse clicks as you type and navigate, running quietly in your menu bar or system tray.

<p align="center">
  <img src="assets/klackr_icon_1024.png" alt="Klackr App Icon" width="160">
</p>

<p align="center">
  <a href="https://github.com/vickyjnv/Klackr/releases/latest/download/Klackr-Release.dmg">
    <img src="https://img.shields.io/badge/Download%20for%20macOS-Klackr--Release.dmg-007AFF?style=for-the-badge&logo=apple&logoColor=white" alt="Download for macOS">
  </a>
  <a href="https://github.com/vickyjnv/Klackr/releases/latest/download/Klackr-Windows-x64.zip">
    <img src="https://img.shields.io/badge/Download%20for%20Windows-Klackr--Windows--x64.zip-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Download for Windows">
  </a>
</p>

<p align="center">
  <a href="https://github.com/vickyjnv/Klackr/releases/latest"><img src="https://img.shields.io/github/v/release/vickyjnv/Klackr?label=Latest%20Release&color=success" alt="Latest Release"></a>
  <img src="https://img.shields.io/badge/macOS-14.0%2B%20(Universal)-blue" alt="macOS 14+">
  <img src="https://img.shields.io/badge/Windows-10%2F11%20(x64)-0078D6" alt="Windows 10/11">
  <img src="https://img.shields.io/badge/license-MIT-green" alt="MIT License">
</p>

---

## Direct Download & Installation

| Platform | Download Link | Format | Architecture |
| :--- | :--- | :--- | :--- |
| **macOS** | [**`Klackr-Release.dmg`**](https://github.com/vickyjnv/Klackr/releases/latest/download/Klackr-Release.dmg) | `.dmg` installer | Universal (`Apple Silicon` + `Intel x86_64`) |
| **Windows** | [**`Klackr-Windows-x64.zip`**](https://github.com/vickyjnv/Klackr/releases/latest/download/Klackr-Windows-x64.zip) | Standalone `.zip` | Windows 10/11 64-bit |

> [!TIP]
> You can also browse all version tags and changelogs on the [**GitHub Releases Page**](https://github.com/vickyjnv/Klackr/releases).

### macOS Quick Setup

1. **Download**: Click [**`Klackr-Release.dmg`**](https://github.com/vickyjnv/Klackr/releases/latest/download/Klackr-Release.dmg).
2. **Install**: Open the DMG file and drag **Klackr.app** into your `/Applications` folder.
3. **First-Time Gatekeeper Permission**:
   Since Klackr is built with an ad-hoc / developer certificate, clear macOS quarantine before launching:
   ```bash
   xattr -cr /Applications/Klackr.app
   ```
4. **Grant Input Monitoring**:
   - Open Klackr from Applications.
   - When prompted, click **Grant Input Monitoring** (or go to *System Settings > Privacy & Security > Input Monitoring* and enable **Klackr**).
   - Click the Klackr menu bar icon and tap **Restart to Apply Permission**.

### Windows Quick Setup

1. **Download**: Click [**`Klackr-Windows-x64.zip`**](https://github.com/vickyjnv/Klackr/releases/latest/download/Klackr-Windows-x64.zip).
2. **Extract**: Right-click the `.zip` archive and choose **Extract All**.
3. **Run**: Double-click `Klackr.exe`. Klackr will start immediately in your Windows system tray.

---

## Features

- **12 Bundled Mechanical Keyboard Profiles (100% Offline)**:
  - **Cherry MX Brown** (Tactile, warm)
  - **Cherry MX Blue** (Clicky, sharp)
  - **Cherry MX Red** (Smooth, light linear)
  - **Cherry MX Black** (Vintage, heavy linear)
  - **Topre Purple Hybrid** (Deep electro-capacitive thock)
  - **Everglide Oreo** (Snappy modern tactile)
  - **Holy Panda** (Iconic round tactile bump, deep thock)
  - **Kailh Box White** (Sharp, clean click-bar actuation)
  - **NovelKeys Cream** (All-POM smooth marbly clack)
  - **Tealios V2** (Boutique ultra-smooth linear)
  - **IBM Buckling Spring** (Model M vintage heavy acoustic legend)
  - **SteelSeries Apex Pro** (Magnetic OmniPoint smooth linear)
- **12 Bundled Mouse Switch Profiles (100% Offline)**:
  - **Classic Omron Microswitch** (Balanced, crisp dual-stage)
  - **Logitech G Pro Superlight** (Snappy esports tactile)
  - **Razer Optical Switch** (Fast punchy optical actuation)
  - **Glorious Model O** (Lightweight Kailh microswitch)
  - **Silent Office Tactile** (Muted soft focus click)
  - **Microsoft IntelliMouse** (Legendary ergonomic tactile click)
  - **Logitech G203 Prodigy** (Crisp springy budget gaming click)
  - **A4Tech Bloody LK Optical** (Ultra-fast infrared optical switch)
  - **Rapture Venom** (Heavy mechanical tactile microswitch)
  - **Trust GXT Gaming** (Solid punchy gaming click)
  - **Apple Magic Mouse** (Acoustic glass-aluminum surface click)
  - **Kailh GM 8.0 Black Mamba** (High-tension 80M esports microswitch)
- **Independent Audio Engines**: Toggle keyboard sounds and mouse clicks separately, each with individual volume sliders.
- **In-App Sound Pack Hub**: Audition switches with one-click preview audio, import custom folders, reveal your local packs directory in Finder/Explorer, and browse community packs.
- **Ultra-Low Latency**: Dedicated polyphony player pools for keyboard typing (8 voices) and mouse clicks (4 voices).
- **Universal Binary (macOS)**: Native Apple Silicon (M1/M2/M3/M4) and Intel x86_64 architecture support.
- **Windows Tray App**: Native C# .NET 8 WASAPI app with low-latency audio and system tray controls.

---

## Building from Source (macOS)

```bash
git clone https://github.com/vickyjnv/Klackr.git
cd Klackr
make build        # Build universal binary
make dist-app     # Package signed app into dist/Klackr.app
make dmg          # Generate dist/Klackr-Release.dmg
```

Requirements: macOS 14.0+ and Xcode 15+.

---

## Publishing a New GitHub Release

Klackr is configured with automated GitHub Actions workflows that automatically build and attach both the macOS DMG and Windows ZIP when a version tag is pushed:

```bash
# 1. Update version number (e.g. 1.0.0)
./scripts/bump_version.sh 1.0.0 1

# 2. Commit and tag
git commit -am "chore: release v1.0.0"
git tag v1.0.0

# 3. Push to GitHub
git push origin main --tags
```

GitHub Actions will automatically:
1. Build the Universal Binary on a macOS runner and package `Klackr-Release.dmg`.
2. Build the self-contained executable on Windows runner and package `Klackr-Windows-x64.zip`.
3. Create a GitHub Release under `v1.0.0` and attach both binary assets so they are directly downloadable via:
   - `https://github.com/vickyjnv/Klackr/releases/latest/download/Klackr-Release.dmg`
   - `https://github.com/vickyjnv/Klackr/releases/latest/download/Klackr-Windows-x64.zip`

### Manual Release Upload (Optional)

You can also create a release directly via GitHub web:
1. Go to [**github.com/vickyjnv/Klackr/releases/new**](https://github.com/vickyjnv/Klackr/releases/new).
2. Choose or create tag `v1.0.0`.
3. Drag and drop your local `dist/Klackr-Release.dmg` into the upload zone.
4. Click **Publish release**.

---

## Permissions (macOS)

Klackr requires macOS **Input Monitoring** permission to detect keystrokes and mouse clicks across applications.

Klackr operates in **listen-only mode** (`.listenOnly`). It **never** logs, stores, transmits, or intercepts any user keystrokes or private data. You can verify this in the open-source code under `Klackr/KeyboardMonitor.swift` and `Klackr/MouseMonitor.swift`.

### Resetting Duplicate Permissions (macOS)

If you rebuild from source multiple times, macOS may list duplicate entries in Input Monitoring. To clean up:
1. Open **System Settings > Privacy & Security > Input Monitoring**.
2. Select old entries and click the minus (**−**) button.
3. Relaunch Klackr to re-add the active build.

---

## License

[MIT](LICENSE)

Bundled Cherry MX Brown and Cherry MX Blue audio is derived from the MIT-licensed Mechvibes sound packs. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
