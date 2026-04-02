; Hat Windows Installer - Inno Setup Script
; Generates a professional .exe installer for Hat
; Run with: iscc hat-installer.iss

#define MyAppName "Hat"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "JGSimi"
#define MyAppURL "https://github.com/JGSimi/HatWindows"
#define MyAppExeName "Hat.exe"

[Setup]
AppId={{B8E7F3A1-4C2D-4E5F-9A1B-3C7D8E9F0A2B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Output installer file
OutputDir=..\dist
OutputBaseFilename=Hat-Setup-{#MyAppVersion}
; Installer appearance
SetupIconFile=..\src\Hat\Assets\Icons\hat-icon.ico
WizardStyle=modern
WizardSizePercent=110
; Compression
Compression=lzma2/ultra64
SolidCompression=yes
; Privileges - install per user (no admin required)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Uninstaller
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
; Architecture
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Misc
DisableProgramGroupPage=yes
LicenseFile=
; Minimum Windows version (Windows 10 1809+)
MinVersion=10.0.17763

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na area de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked
Name: "autostart"; Description: "Iniciar Hat com o Windows"; GroupDescription: "Opcoes:"; Flags: unchecked

[Files]
; Main application files from publish output
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu shortcut
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
; Desktop shortcut (optional)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Auto-start with Windows (optional task)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Hat"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
; Launch after install
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir Hat agora"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up app data on uninstall (optional - commented out to preserve settings)
; Type: filesandordirs; Name: "{userappdata}\Hat"

[Code]
// Close Hat if running before install/uninstall
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  // Try to close Hat gracefully
  if Exec('taskkill', '/f /im Hat.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Sleep(500); // Wait for process to fully exit
  end;
end;

function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  Exec('taskkill', '/f /im Hat.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(500);
end;
