; Inno Setup 6 Script for Strata Scene v0.1.0
; Non-intrusive Per-User installation (No admin rights required)

#define MyAppName "Strata Scene"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "featlis"
#define MyAppExeName "StrataScene.App.exe"

[Setup]
AppId={{5D9D461E-23E4-4BC5-85A4-F2D74AE29D10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\StrataScene
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=StrataScene-Setup-{#MyAppVersion}
Compression=lzma2/ultra
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startupicon"; Description: "Windows 起動時に自動実行する"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Helper function to check if .NET 10 Desktop Runtime is installed
function IsDotNet10DesktopInstalled: Boolean;
var
  ResultCode: Integer;
  OutputText: AnsiString;
begin
  Result := False;

  // Check 1: Registry check for Microsoft.WindowsDesktop.App 10.x
  if RegKeyExists(HKLM64, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App') or
     RegKeyExists(HKCU64, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App') then
  begin
    Result := True;
    Exit;
  end;

  // Check 2: Run dotnet --list-runtimes check
  if Exec('cmd.exe', '/c dotnet --list-runtimes > "%TEMP%\strata_dotnet_check.txt"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if LoadStringFromFile(ExpandConstant('{tmp}\..\strata_dotnet_check.txt'), OutputText) then
    begin
      if Pos('Microsoft.WindowsDesktop.App 10.', String(OutputText)) > 0 then
      begin
        Result := True;
      end;
    end;
    DeleteFile(ExpandConstant('{tmp}\..\strata_dotnet_check.txt'));
  end;
end;

function InitializeSetup: Boolean;
var
  DownloadUrl: string;
  InstallerPath: string;
  ResultCode: Integer;
begin
  Result := True;

  if not IsDotNet10DesktopInstalled then
  begin
    if MsgBox('Strata Scene の実行には .NET 10 Desktop Runtime (x64) が必要です。'#13#10 +
              '今すぐダウンロードしてインストールしますか？', mbConfirmation, MB_YESNO) = IDYES then
    begin
      DownloadUrl := 'https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe';
      InstallerPath := ExpandConstant('{tmp}\dotnet-runtime-installer.exe');

      try
        DownloadTemporaryFile(DownloadUrl, 'dotnet-runtime-installer.exe', '', nil);
        if Exec(InstallerPath, '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
        begin
          Result := True;
        end
        else
        begin
          MsgBox('.NET 10 Runtime のインストールに失敗しました。手動でインストールしてください。', mbError, MB_OK);
        end;
      except
        MsgBox('.NET 10 Runtime のダウンロードに失敗しました。ブラウザから手動でインストールしてください。', mbError, MB_OK);
      end;
    end
    else
    begin
      Result := True; // Proceed anyway in case user will install it later
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    if MsgBox('Strata Scene のユーザー設定データ (%APPDATA%\StrataScene) も完全に削除しますか？', mbConfirmation, MB_YESNO) = IDYES then
    begin
      DelTree(ExpandConstant('{userappdata}\StrataScene'), True, True, True);
    end;
  end;
end;
