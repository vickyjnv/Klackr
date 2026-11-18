; Klackr Installer Script for NSIS
; Compile with: makensis Klackr_Installer.nsi

!include "MUI2.nsh"
!include "LogicLib.nsh"

; General settings
Name "Klackr"
OutFile "..\dist\Klackr-Setup-1.0.0-x64.exe"
InstallDir "$LOCALAPPDATA\Klackr"
InstallDirRegKey HKCU "Software\Klackr" "Install_Dir"
RequestExecutionLevel user
Icon "app.ico"
UninstallIcon "app.ico"

; Modern UI
!define MUI_ABORTWARNING
!define MUI_ICON "app.ico"
!define MUI_UNICON "app.ico"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
!insertmacro MUI_LANGUAGE "English"

; Variables
Var StartupCheckboxState
Var DesktopCheckboxState
Var LaunchAfterInstall

; Component page for additional options
!define MUI_COMPONENTSPAGE_SMALLDESC
!define MUI_COMPONENTSPAGE_NODESC

Section "Main Files" SEC_MAIN
  SectionIn RO
  SetOutPath "$INSTDIR"

  ; Copy main executable
  File "..\dist\windows\Klackr.exe"

  ; Copy sound files
  SetOutPath "$INSTDIR\Sounds"
  File /r "..\dist\windows\Sounds\*.*"

  ; Write uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  ; Write registry keys for uninstaller
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Klackr" "DisplayName" "Klackr"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Klackr" "DisplayVersion" "1.0.0"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Klackr" "Publisher" "Vicky"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Klackr" "URLInfoAbout" "https://github.com/vickyjnv/Klackr"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Klackr" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Klackr" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Klackr" "DisplayIcon" "$INSTDIR\Klackr.exe"

  ; Write install directory for future upgrades
  WriteRegStr HKCU "Software\Klackr" "Install_Dir" "$INSTDIR"

  ; Create Start Menu shortcuts
  CreateDirectory "$SMPROGRAMS\Klackr"
  CreateShortCut "$SMPROGRAMS\Klackr\Klackr.lnk" "$INSTDIR\Klackr.exe" "" "$INSTDIR\app.ico" 0
  CreateShortCut "$SMPROGRAMS\Klackr\Uninstall.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\app.ico" 0
SectionEnd

Section "Create Desktop Shortcut" SEC_DESKTOP
  CreateShortCut "$DESKTOP\Klackr.lnk" "$INSTDIR\Klackr.exe" "" "$INSTDIR\app.ico" 0
SectionEnd

Section "Launch at Windows Startup" SEC_STARTUP
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "Klackr" '"$INSTDIR\Klackr.exe"'
SectionEnd

Section "Launch Klackr after Installation" SEC_LAUNCH
  StrCpy $LaunchAfterInstall 1
SectionEnd

Function .onInit
  ; Check if Klackr is already running
  FindWindow $0 "Klackr"
  ${If} $0 <> 0
    MessageBox MB_YESNO|MB_ICONQUESTION "Klackr is currently running. Please close it before installing.$\n$\nWould you like the installer to close it for you?" IDYES CloseApp IDNO Abort
    Abort:
    Abort
    CloseApp:
    SendMessage $0 ${WM_CLOSE} 0 0
    Sleep 1000
  ${EndIf}
FunctionEnd

Function .onInstSuccess
  ; Launch Klackr after installation if requested
  ${If} $LaunchAfterInstall == 1
    Exec '"$INSTDIR\Klackr.exe"'
  ${EndIf}
FunctionEnd

; Uninstaller section
Section "Uninstall"
  ; Remove startup registry entry
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "Klackr"

  ; Remove files
  Delete "$INSTDIR\Klackr.exe"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir /r "$INSTDIR\Sounds"
  RMDir "$INSTDIR"

  ; Remove shortcuts
  Delete "$SMPROGRAMS\Klackr\Klackr.lnk"
  Delete "$SMPROGRAMS\Klackr\Uninstall.lnk"
  RMDir "$SMPROGRAMS\Klackr"
  Delete "$DESKTOP\Klackr.lnk"

  ; Remove registry keys
  DeleteRegKey /ifempty HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Klackr"
  DeleteRegKey /ifempty HKCU "Software\Klackr"
SectionEnd