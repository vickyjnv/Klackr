@echo off
echo ===================================================
echo   Building Klackr for Windows (x64 Self-Contained)
echo ===================================================

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ../dist/windows

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ===================================================
    echo   Build succeeded!
    echo   Output executable: dist\windows\Klackr.exe
    echo ===================================================
) else (
    echo.
    echo Build failed with error code %ERRORLEVEL%.
)
pause
