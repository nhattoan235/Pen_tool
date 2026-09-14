#define MyAppName "Screen Ink"
#define MyAppExeName "ScreenInk.exe"
#ifndef MyAppVersion
  #define MyAppVersion "0.9.0-beta.1"
#endif

[Setup]
AppId={{F6C90EC8-02B7-4D5F-9526-594255F5BB61}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=nhattoan235
AppPublisherURL=https://github.com/nhattoan235/Pen_tool
AppSupportURL=https://github.com/nhattoan235/Pen_tool/issues
AppUpdatesURL=https://github.com/nhattoan235/Pen_tool/releases
DefaultDirName={localappdata}\Programs\Screen Ink
DefaultGroupName=Screen Ink
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installer
OutputBaseFilename=ScreenInk-Setup-{#MyAppVersion}-win-x64
SetupIconFile=..\src\ScreenInk.App\Assets\screenink.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter={#MyAppExeName}
RestartApplications=no
MinVersion=10.0.17763
VersionInfoVersion=0.9.0.1
VersionInfoCompany=nhattoan235
VersionInfoDescription=Screen Ink single-user installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion=0.9.0.1

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Screen Ink"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\Screen Ink"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "ScreenInk"; Flags: uninsdeletevalue dontcreatekey

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Screen Ink"; Flags: nowait postinstall skipifsilent
