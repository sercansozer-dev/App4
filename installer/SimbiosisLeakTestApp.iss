; â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
;  Simbiosis Mekatronik â€” Leak Test App
;  Inno Setup Script
;
;  KullanÄ±m:
;    1) Projeyi publish et:
;         dotnet publish App4\App4\App4.csproj -c Release -r win-x64 --self-contained -o publish
;    2) Bu script'i derle:
;         "C:\...\Inno Setup 6\ISCC.exe" installer\SimbiosisLeakTestApp.iss
;
;  Veya tek komutta:
;       powershell -ExecutionPolicy Bypass -File build-setup.ps1
; â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

#define AppName         "Simbiosis Leak Test App"
#define AppPublisher    "Simbiosis Mekatronik"
#define AppURL          "https://simbiosismekatronik.com"
#define AppExeName      "SimbiosisLeakTestApp.exe"
#define AppVersion "1.0.18"
#define AppId           "{{8B4F8A2E-5C1B-4A3D-9E7F-2F6A1D3B8C9A}"

; Projenin kÃ¶k dizini (bu .iss dosyasÄ±nÄ±n olduÄŸu klasÃ¶rÃ¼n Ã¼stÃ¼)
#define RepoRoot        SourcePath + "..\"
#define PublishDir      RepoRoot + "publish"
#define OutputDir       RepoRoot + "dist"

; â”€â”€â”€ Derleme-zamanÄ± kontrolÃ¼ â”€â”€â”€
; Publish dizini yoksa ISCC derlemeyi DURDURUR (runtime kontrolÃ¼ yok,
; end-user PC'de bu kontrole gerek yok Ã§Ã¼nkÃ¼ dosyalar Setup.exe iÃ§inde gÃ¶mÃ¼lÃ¼)
#if !FileExists(PublishDir + "\" + AppExeName)
  #error Publish dizini bulunamadi: PublishDir\AppExeName yok. Once 'build-setup.ps1' calistirin veya 'dotnet publish App4\App4\App4.csproj -c Release -r win-x64 --self-contained -o publish' komutunu kullanin.
#endif

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}

; Kurulum dizini â€” "Simbiosis" klasÃ¶rÃ¼ altÄ±nda
DefaultDirName={autopf}\Simbiosis\LeakTestApp
DefaultGroupName=Simbiosis
DisableProgramGroupPage=yes
DisableDirPage=no

; YÃ¼kleme iznine (yÃ¶netici) ihtiyaÃ§ var â€” Program Files'a yazÄ±yoruz
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; 64-bit mod
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Ã‡Ä±ktÄ±
OutputDir={#OutputDir}
OutputBaseFilename=Setup_SimbiosisLeakTestApp_v{#AppVersion}

; SÄ±kÄ±ÅŸtÄ±rma
Compression=lzma2/ultra64
SolidCompression=yes

; GÃ¶rsel
WizardStyle=modern
SetupIconFile={#RepoRoot}App4\App4\Assets\SBS_Logo.ico
; WizardImageFile=assets\wizard.bmp       ; Ä°steÄŸe baÄŸlÄ± (164x314 bmp)
; WizardSmallImageFile=assets\wizard-small.bmp  ; Ä°steÄŸe baÄŸlÄ± (55x58 bmp)

; Uninstaller kayÄ±t bilgisi
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}

; Log
SetupLogging=yes

; Kurulum sÄ±rasÄ±nda Ã§alÄ±ÅŸan sÃ¼reÃ§leri kapat
CloseApplications=force
RestartApplications=no

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Dirs]
; UygulamanÄ±n runtime'da kullandÄ±ÄŸÄ± sabit config klasÃ¶rÃ¼
; (GlobalData.ConfigBaseDir ile birebir aynÄ± â€” deÄŸiÅŸtirirseniz GlobalData.cs'te de deÄŸiÅŸtirin)
Name: "{commonappdata}\..\..\Simbiosis\SimbiosisLeakTestApp\Config"; Permissions: users-modify
Name: "C:\Simbiosis\SimbiosisLeakTestApp\Config"; Permissions: users-modify
Name: "C:\Simbiosis\SimbiosisLeakTestApp\Models"; Permissions: users-modify

[Files]
; â•â•â• PUBLISH Ã‡IKTISI â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
; TÃ¼m yayÄ±n dizini (.exe, .dll, .glb, .html, runtime'lar...)
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; â•â•â• RUNTIME GEREKSÄ°NÄ°MLERÄ° â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
; WebView2 Runtime (yoksa kurulur â€” Code bÃ¶lÃ¼mÃ¼ne bakÄ±n)
Source: "redist\MicrosoftEdgeWebView2RuntimeInstaller.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: WebView2RuntimeMissing

; â•â•â• SEED KONFIGÃœRASYON DOSYALARI â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
; GeliÅŸtirici PC'sindeki mevcut durum (klima tipleri, job mapping'leri,
; PLC deÄŸiÅŸkenleri, robot deÄŸiÅŸkenleri, referanslar, RFID listesi, vs.)
; Fabrika default'u olarak daÄŸÄ±tÄ±lÄ±yor.
;
; onlyifdoesntexist flag'i SAYESÄ°NDE:
;   - Yeni PC'ye kurulumda â†’ dosyalar kopyalanÄ±r (fabrika default)
;   - Mevcut kuruluma re-install/upgrade â†’ kullanÄ±cÄ± verisi EZILMEZ
;   - KullanÄ±cÄ± "sÄ±fÄ±rlama" isterse Config klasÃ¶rÃ¼nÃ¼ silip tekrar kurabilir
; KORUMA MODU (varsayilan): sadece eksik dosyalar eklenir, mevcut ayar JSON'lari EZILMEZ.
Source: "seed\Config\*"; DestDir: "C:\Simbiosis\SimbiosisLeakTestApp\Config"; \
    Flags: onlyifdoesntexist uninsneveruninstall recursesubdirs createallsubdirs; Permissions: users-modify; Check: KeepExistingConfig
; SIFIRLAMA MODU (kullanici acikca secerse): tum seed dosyalari uzerine yazilir (fabrika ayarlari).
; CurStepChanged icinde Config klasoru once silinir, sonra bu satir fabrika ayarlarini yukler.
Source: "seed\Config\*"; DestDir: "C:\Simbiosis\SimbiosisLeakTestApp\Config"; \
    Flags: uninsneveruninstall recursesubdirs createallsubdirs; Permissions: users-modify; Check: ResetConfigToDefaults

; 3D model dosyalari (.glb) - Klima Editoru ve RobotArmViewer bunlari okuyor.
; ModelsPathHelper.cs C:\Simbiosis\SimbiosisLeakTestApp\Models lokasyonunu kullanir.
; onlyifdoesntexist sayesinde kullanicinin ekledigi/duzenledigi modeller korunur.
Source: "seed\Models\*"; DestDir: "C:\Simbiosis\SimbiosisLeakTestApp\Models"; \
    Flags: onlyifdoesntexist uninsneveruninstall recursesubdirs createallsubdirs; \
    Permissions: users-modify

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
; WebView2 Runtime kurulumu (yoksa)
Filename: "{tmp}\MicrosoftEdgeWebView2RuntimeInstaller.exe"; Parameters: "/silent /install"; \
    StatusMsg: "WebView2 Runtime yukleniyor (gerekli bilesen)..."; \
    Check: WebView2RuntimeMissing; Flags: waituntilterminated

; â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
; WINDOWS FIREWALL KURALI â€” Gocator & PLC & Robot veri akisi icin gerekli
; â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
; Gocator GDP data port'u dinamik (sensor atar) â€” bu yuzden App4.exe'nin TUM
; baglantilarini izin ver (in+out, private+public profil). Olmazsa:
;   - Tetik gider, Gocator alir, ama snapshot/measurement verisi geri donmez
;   - PLC okumalari yavasi veya tekrar kurulur
;   - Robot KUKAVARPROXY bildirimleri ulasmaz
Filename: "netsh.exe"; \
    Parameters: "advfirewall firewall delete rule name=""Simbiosis Leak Test App"""; \
    Flags: runhidden; StatusMsg: "Eski firewall kurali temizleniyor..."

Filename: "netsh.exe"; \
    Parameters: "advfirewall firewall add rule name=""Simbiosis Leak Test App"" dir=in action=allow program=""{app}\{#AppExeName}"" enable=yes profile=any"; \
    Flags: runhidden; StatusMsg: "Firewall (gelen) kurali ekleniyor..."

Filename: "netsh.exe"; \
    Parameters: "advfirewall firewall add rule name=""Simbiosis Leak Test App"" dir=out action=allow program=""{app}\{#AppExeName}"" enable=yes profile=any"; \
    Flags: runhidden; StatusMsg: "Firewall (giden) kurali ekleniyor..."

; Kurulum sonu uygulamayi calistir (opsiyonel)
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; \
    Flags: nowait postinstall skipifsilent

[UninstallRun]
; Uninstall sirasinda firewall kuralini temizle
Filename: "netsh.exe"; \
    Parameters: "advfirewall firewall delete rule name=""Simbiosis Leak Test App"""; \
    Flags: runhidden; RunOnceId: "RemoveFirewallRule"

[UninstallDelete]
; Uninstall'da geride kalan dosyalarÄ± temizle (config KORUNUR â€” kullanÄ±cÄ± silmek isterse manuel)
Type: filesandordirs; Name: "{app}"

[Code]
{ â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  WebView2 Runtime kurulu mu? (HKLM / HKCU altÄ±nda kayÄ±t anahtarÄ± arar)
  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ }
function WebView2RuntimeInstalled(): Boolean;
var
  Value: string;
begin
  Result := False;
  // 64-bit makinede WebView2 registry anahtarÄ±
  if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Value) then
  begin
    if (Value <> '') and (Value <> '0.0.0.0') then
      Result := True;
  end;
  // Per-user kurulum (HKCU)
  if (not Result) and RegQueryStringValue(HKCU, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Value) then
  begin
    if (Value <> '') and (Value <> '0.0.0.0') then
      Result := True;
  end;
end;

function WebView2RuntimeMissing(): Boolean;
begin
  Result := not WebView2RuntimeInstalled();
end;

{ ─────────────────────────────────────────────────────────────────────────────
  AYAR KORUMA SECIMI
  Kurulum sirasinda kullaniciya sorar:
    Secenek 0 = Mevcut ayarlari KORU (onerilen) -> C:\Simbiosis ayar JSON EZILMEZ
    Secenek 1 = VARSAYILAN ayarlarla kur -> Config klasoru silinip fabrika ayari yuklenir
  Ilk kurulumda (mevcut Config klasoru yoksa) bu sayfa atlanir ve seed dogrudan yuklenir.
  ───────────────────────────────────────────────────────────────────────────── }
var
  ConfigChoicePage: TInputOptionWizardPage;

procedure InitializeWizard();
begin
  ConfigChoicePage := CreateInputOptionPage(wpSelectDir,
    'Uygulama Ayarlari',
    'Mevcut ayarlar nasil islensin?',
    'Bu bilgisayarda daha once yapilandirilmis ayarlar (PLC, RFID, robot, recete vb.) bulunabilir.' + #13#10 +
    'Yeni surumu nasil kurmak istersiniz?',
    True, False);
  ConfigChoicePage.Add('Mevcut ayarlari KORU (onerilen)  -  C:\Simbiosis ayarlariniz aynen kalir');
  ConfigChoicePage.Add('VARSAYILAN ayarlarla kur  -  mevcut ayarlar silinip fabrika degerlerine doner');
  ConfigChoicePage.SelectedValueIndex := 0;  { Varsayilan secim: KORU }
end;

function ConfigDirExists(): Boolean;
begin
  Result := DirExists('C:\Simbiosis\SimbiosisLeakTestApp\Config');
end;

{ Mevcut config yoksa (ilk kurulum) secim sayfasini gizle }
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if PageID = ConfigChoicePage.ID then
    Result := not ConfigDirExists();
end;

{ KORUMA MODU: ilk kurulumda DA true (seed eksikleri doldurur) }
function KeepExistingConfig(): Boolean;
begin
  if not ConfigDirExists() then
    Result := True
  else
    Result := (ConfigChoicePage.SelectedValueIndex = 0);
end;

{ SIFIRLAMA MODU: yalnizca mevcut config varken ve kullanici acikca secince true }
function ResetConfigToDefaults(): Boolean;
begin
  if not ConfigDirExists() then
    Result := False
  else
    Result := (ConfigChoicePage.SelectedValueIndex = 1);
end;

{ SIFIRLAMA secildiyse, dosyalar kopyalanmadan ONCE Config klasorunu tamamen temizle }
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) and ResetConfigToDefaults() then
  begin
    if ConfigDirExists() then
      DelTree('C:\Simbiosis\SimbiosisLeakTestApp\Config', True, True, True);
  end;
end;

{ â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  NOT: Publish dizini kontrolÃ¼ DERLEME zamaninda preprocessor ile
  yapiliyor (asagidaki #if blogu). Runtime'da (end-user PC'de) kontrol
  etmenin anlami yok â€” dosyalar zaten Setup.exe'nin icinde gomulu.
  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ }

{ â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  Uninstall sÄ±rasÄ±nda config klasÃ¶rÃ¼nÃ¼ soralÄ±m (default=KORU)
  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ConfigDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    ConfigDir := 'C:\Simbiosis\SimbiosisLeakTestApp';
    if DirExists(ConfigDir) then
    begin
      if MsgBox('Kullanici config dosyalarini da silmek istiyor musunuz?' + #13#10 +
                '(' + ConfigDir + ')' + #13#10#13#10 +
                'HAYIR secerseniz ayarlar ve gecmis kayitlar korunur.',
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      begin
        DelTree(ConfigDir, True, True, True);
      end;
    end;
  end;
end;
