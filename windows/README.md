# Klackr for Windows

Mechanical keyboard and mouse click sounds for your Windows PC. Klackr plays satisfying tactile keystrokes and microswitch clicks as you type and navigate, running quietly in your Windows system tray.

---

## Features

- **12 Mechanical Keyboard Packs**: Cherry MX Brown, Blue, Red, Black, Topre Purple, Everglide Oreo, Holy Panda, Kailh Box White, NK Cream, Tealios V2, IBM Buckling Spring, and SteelSeries Apex Pro.
- **12 Mouse Switch Packs**: Classic Omron, Logitech Superlight, Razer Optical, Glorious Kailh, Silent Office, Microsoft IntelliMouse, Logitech G203, A4Tech Bloody Optical, Rapture Venom, Trust GXT, Apple Magic Mouse, and Kailh GM 8.0.
- **Ultra-low latency** polyphonic audio engine with dedicated channels for typing and mouse clicks.
- **System Tray application**: Runs quietly in the notification area with a right-click menu.
- **Global Hotkey toggle**: `Alt + Ctrl + K` to quickly mute/unmute anytime.
- **Independent Volume Controls**: Easily adjust sound levels for keyboard and mouse.
- **Launch on Startup**: Optional one-click toggle to launch with Windows.

---

## Running & Building

### Option 1: Native C# .NET (Recommended)

Requires [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

#### Build & Run Directly:
```cmd
cd windows
dotnet run
```

#### Publish Standalone Single-File Executable (`Klackr.exe`):
Double-click `build.bat` or run:
```cmd
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ../dist/windows
```
This produces a single, self-contained `Klackr.exe` in `dist/windows/` that requires no .NET runtime installation.

---

### Option 2: Portable Python Version

Requires [Python 3.10+](https://www.python.org/downloads/).

Double-click `run_portable.bat` or run:
```cmd
cd windows
pip install -r requirements.txt
python klackr_portable.py
```

To compile into a standalone `.exe`:
```cmd
pip install pyinstaller
pyinstaller --noconsole --onefile --add-data "Sounds;Sounds" --icon=app.ico klackr_portable.py
```
