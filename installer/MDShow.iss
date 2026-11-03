#ifndef AppVersion
#define AppVersion "1.0"
#endif
[Setup]
AppId={{D4F607D5-40AE-496C-9124-1161A6DE4A11}
AppName=MD Show
AppVersion={#AppVersion}
AppPublisher=MONOLIT Agency LLC / ООО «Агентство «МОНОЛИТ»
AppCopyright=Copyright (c) 2026 ООО «Агентство «МОНОЛИТ»
DefaultDirName={autopf}\MD Show
UsePreviousAppDir=no
DefaultGroupName=MD Show
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir=..\dist
OutputBaseFilename=MDShow-{#AppVersion}-Setup-x64
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\MDShow.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ChangesAssociations=yes
CloseApplications=no
RestartApplications=no
SetupLogging=yes
VersionInfoVersion=1.0.0.0
LicenseFile=..\LICENSE
UninstallDisplayName=MD Show

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "chinese"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

#include "..\artifacts\installer-messages.iss"

[Tasks]
Name: "contextmenu"; Description: "{cm:setupContext}"; Flags: checkedonce
Name: "desktopicon"; Description: "{cm:setupDesktop}"; Flags: unchecked

[Files]
Source: "..\dist\public-1.0\installed\MDShow.SetupGuard.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\public-1.0\installed\MDShow.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\public-1.0\installed\MDShow.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\public-1.0\installed\Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\public-1.0\installed\Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\public-1.0\installed\WebView2Loader.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\public-1.0\installed\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\public-1.0\installed\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\public-1.0\installed\README_RU.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\public-1.0\installed\Welcome.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\public-1.0\installed\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\public-1.0\installed\BRANDING.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\MD Show"; Filename: "{app}\MDShow.exe"
Name: "{autodesktop}\MD Show"; Filename: "{app}\MDShow.exe"; Tasks: desktopicon

[Registry]
Root: HKLM; Subkey: "Software\Classes\MDShow.Markdown"; ValueType: string; ValueData: "Markdown document"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\MDShow.Markdown\DefaultIcon"; ValueType: string; ValueData: "{app}\MDShow.exe,0"
Root: HKLM; Subkey: "Software\Classes\MDShow.Markdown\shell\open\command"; ValueType: string; ValueData: """{app}\MDShow.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\MDShow.Markdown\shell\edit\command"; ValueType: string; ValueData: """{app}\MDShow.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\.md\OpenWithProgids"; ValueType: string; ValueName: "MDShow.Markdown"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\Classes\.markdown\OpenWithProgids"; ValueType: string; ValueName: "MDShow.Markdown"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\Classes\.mdown\OpenWithProgids"; ValueType: string; ValueName: "MDShow.Markdown"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\Classes\Applications\MDShow.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "MD Show"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\Applications\MDShow.exe\shell\open\command"; ValueType: string; ValueData: """{app}\MDShow.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\Applications\MDShow.exe\SupportedTypes"; ValueType: string; ValueName: ".md"; ValueData: ""
Root: HKLM; Subkey: "Software\Classes\Applications\MDShow.exe\SupportedTypes"; ValueType: string; ValueName: ".markdown"; ValueData: ""
Root: HKLM; Subkey: "Software\Classes\Applications\MDShow.exe\SupportedTypes"; ValueType: string; ValueName: ".mdown"; ValueData: ""
Root: HKLM; Subkey: "Software\MDShow\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "MD Show"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\MDShow\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "{cm:shellDescription}"
Root: HKLM; Subkey: "Software\MDShow\Capabilities"; ValueType: string; ValueName: "ApplicationIcon"; ValueData: "{app}\MDShow.exe,0"
Root: HKLM; Subkey: "Software\MDShow\Capabilities\FileAssociations"; ValueType: string; ValueName: ".md"; ValueData: "MDShow.Markdown"
Root: HKLM; Subkey: "Software\MDShow\Capabilities\FileAssociations"; ValueType: string; ValueName: ".markdown"; ValueData: "MDShow.Markdown"
Root: HKLM; Subkey: "Software\MDShow\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mdown"; ValueData: "MDShow.Markdown"
Root: HKLM; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "MD Show"; ValueData: "Software\MDShow\Capabilities"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.md\shell\MDShow"; ValueType: string; ValueData: "{cm:shellOpen}"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.md\shell\MDShow"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\MDShow.exe,0"; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.md\shell\MDShow\command"; ValueType: string; ValueData: """{app}\MDShow.exe"" ""%1"""; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.markdown\shell\MDShow"; ValueType: string; ValueData: "{cm:shellOpen}"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.markdown\shell\MDShow\command"; ValueType: string; ValueData: """{app}\MDShow.exe"" ""%1"""; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.mdown\shell\MDShow"; ValueType: string; ValueData: "{cm:shellOpen}"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.mdown\shell\MDShow\command"; ValueType: string; ValueData: """{app}\MDShow.exe"" ""%1"""; Tasks: contextmenu

[Run]
Filename: "{app}\MDShow.exe"; Parameters: """{app}\Welcome.md"""; Description: "{cm:setupLaunch}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Only known app data is removed; user-created Markdown files beside the EXE survive.
Type: files; Name: "{app}\smoke-result.json"
Type: files; Name: "{app}\integration-result.json"
Type: filesandordirs; Name: "{app}\test-profile"
Type: filesandordirs; Name: "{app}\qa-profile"

[Code]
var
  InstallationGate: THandle;
  UninstallGuard: String;

function CreateFileW(Name: String; Access, Sharing: Cardinal; Security: THandle;
  Creation, Flags: Cardinal; Template: THandle): THandle;
  external 'CreateFileW@kernel32.dll stdcall';
function CloseHandle(Handle: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';

procedure ReleaseInstallationGate();
begin
  if InstallationGate <> 0 then begin
    CloseHandle(InstallationGate);
    InstallationGate := 0;
  end;
end;

function AcquireInstallationGate(): Boolean;
begin
  Result := InstallationGate <> 0;
  if Result then exit;
  { OPEN_ALWAYS; no sharing. A file handle works across sessions without a global name/ACL. }
  InstallationGate := CreateFileW(ExpandConstant('{app}\installation.lock'), $C0000000, 0, 0, 4, $80, 0);
  if InstallationGate = THandle(-1) then InstallationGate := 0;
  Result := InstallationGate <> 0;
end;

function CheckInstalledApplication(Guard: String): String;
var
  Code: Integer;
begin
  Result := CustomMessage('setupGuardFailed');
  if not Exec(Guard, '--check-app "' + ExpandConstant('{app}\MDShow.exe') + '"', '', SW_HIDE, ewWaitUntilTerminated, Code) then exit;
  Log('MD Show: Restart Manager guard result=' + IntToStr(Code));
  if Code = 0 then Result := ''
  else if Code = 1 then Result := CustomMessage('setupRunning');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  ExtractTemporaryFile('MDShow.SetupGuard.exe');
  Result := CheckInstalledApplication(ExpandConstant('{tmp}\MDShow.SetupGuard.exe'));
  if Result <> '' then exit;
  if not ForceDirectories(ExpandConstant('{app}')) or not AcquireInstallationGate() then begin
    Result := CustomMessage('setupRunning');
    exit;
  end;
  Result := CheckInstalledApplication(ExpandConstant('{tmp}\MDShow.SetupGuard.exe'));
  if Result <> '' then ReleaseInstallationGate();
end;

procedure CurStepChanged(Step: TSetupStep);
begin
  if Step = ssPostInstall then ReleaseInstallationGate();
end;

procedure DeinitializeSetup();
begin
  ReleaseInstallationGate();
end;

function InitializeUninstall(): Boolean;
var
  Problem: String;
begin
  Result := False;
  UninstallGuard := ExpandConstant('{tmp}\MDShow.SetupGuard.exe');
  if not CopyFile(ExpandConstant('{app}\MDShow.SetupGuard.exe'), UninstallGuard, False) then begin
    SuppressibleMsgBox(CustomMessage('setupGuardFailed'), mbError, MB_OK, IDOK);
    exit;
  end;
  if not AcquireInstallationGate() then Problem := CustomMessage('setupRunning')
  else Problem := CheckInstalledApplication(UninstallGuard);
  if Problem <> '' then begin
    SuppressibleMsgBox(Problem, mbError, MB_OK, IDOK);
    ReleaseInstallationGate();
    exit;
  end;
  Result := True;
end;

procedure CurUninstallStepChanged(Step: TUninstallStep);
var
  Problem: String;
  Code: Integer;
begin
  if Step = usUninstall then begin
    Problem := CheckInstalledApplication(UninstallGuard);
    if Problem <> '' then RaiseException(Problem);
    { Cleanup uses a filtered child from the protected install directory before
      program files are removed. Other-account elevation keeps all profiles. }
    if not Exec(ExpandConstant('{app}\MDShow.SetupGuard.exe'), '--clean-profile', '', SW_HIDE, ewWaitUntilTerminated, Code) then Code := 2;
    Log('MD Show: consented Installed profile cleanup result=' + IntToStr(Code));
    if Code = 3 then SuppressibleMsgBox(CustomMessage('profilePreserved'), mbInformation, MB_OK, IDOK)
    else if Code <> 0 then RaiseException(CustomMessage('profileCleanupFailed'));
  end;
  if Step = usPostUninstall then begin
    ReleaseInstallationGate();
    DeleteFile(ExpandConstant('{app}\installation.lock'));
    RemoveDir(ExpandConstant('{app}'));
  end;
end;

procedure DeinitializeUninstall();
begin
  ReleaseInstallationGate();
end;

function InitializeSetup(): Boolean;
var
  Version, InstalledVersion: String;
  Existing, Requested: Int64;
  Release: Cardinal;
begin
  Result := False;
  { Match the all-users uninstall entry explicitly; log each failed check separately. }
  if RegKeyExists(HKLM64, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{D4F607D5-40AE-496C-9124-1161A6DE4A11}_is1') then begin
    if not RegQueryStringValue(HKLM64, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{D4F607D5-40AE-496C-9124-1161A6DE4A11}_is1', 'DisplayVersion', InstalledVersion) then begin
      Log('MD Show: installed DisplayVersion cannot be read');
      SuppressibleMsgBox(CustomMessage('setupVersionBlocked'), mbError, MB_OK, IDOK);
      exit;
    end;
    InstalledVersion := Trim(InstalledVersion);
    Log('MD Show: installed version=' + InstalledVersion + '; requested version={#AppVersion}');
    if not StrToVersion(InstalledVersion, Existing) then begin
      Log('MD Show: installed version cannot be parsed');
      SuppressibleMsgBox(CustomMessage('setupVersionBlocked'), mbError, MB_OK, IDOK);
      exit;
    end;
    if not StrToVersion('{#AppVersion}', Requested) then begin
      Log('MD Show: requested version cannot be parsed');
      SuppressibleMsgBox(CustomMessage('setupVersionBlocked'), mbError, MB_OK, IDOK);
      exit;
    end;
    if ComparePackedVersion(Existing, Requested) > 0 then begin
      SuppressibleMsgBox(CustomMessage('setupVersionBlocked'), mbError, MB_OK, IDOK);
      exit;
    end;
  end;
  if not RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) or (Release < 528040) then begin
    MsgBox(CustomMessage('setupDotNet'), mbError, MB_OK);
    exit;
  end;
  if not (RegQueryStringValue(HKLM32, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) or
          RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) or
          RegQueryStringValue(HKLM64, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version)) or (Version = '') or (Version = '0.0.0.0') then begin
    MsgBox(CustomMessage('webviewFailed'), mbError, MB_OK);
    exit;
  end;
  Result := True;
end;
