#define MyAppName "Barcode Studio"
#ifndef MyAppVersion
#define MyAppVersion "1.1.0"
#endif
[Setup]
AppId={{9E65CFD9-5E58-4A8D-8B72-5CE3E7E0B640}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Barcode Studio
DefaultDirName={localappdata}\Programs\Barcode Studio
DefaultGroupName=Barcode Studio
OutputDir=..\installer-output
OutputBaseFilename=BarcodeStudio-Setup-{#MyAppVersion}-win-x64
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\BarcodeStudio.exe
WizardStyle=modern
PrivilegesRequired=lowest
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Barcode Studio"; Filename: "{app}\BarcodeStudio.exe"
Name: "{autodesktop}\Barcode Studio"; Filename: "{app}\BarcodeStudio.exe"; Tasks: desktopicon
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
[Run]
Filename: "{app}\BarcodeStudio.exe"; Description: "Launch Barcode Studio"; Flags: nowait postinstall skipifsilent
