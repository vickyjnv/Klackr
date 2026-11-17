# Klackr for Windows

Mechanical keyboard sounds for your Windows PC. Klackr plays satisfying Cherry MX Brown and Cherry MX Blue mechanical keyboard sounds as you type, running quietly in your Windows system tray.

---

## Features

- **Cherry MX Brown & Blue sound packs** with natural pitch & volume variation.
- **Ultra-low latency** polyphonic audio engine.
- **System Tray application**: Runs quietly in the notification area with a right-click menu.
- **Global Hotkey toggle**: `Alt + Ctrl + K` to quickly mute/unmute anytime.
- **Volume Control**: Easily adjust sound level.
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
