#define AppName "SonyDev Bypass"
#define AppVersion "1.2.4-beta"
#define AppPublisher "SonyDev"
#define AppExeName "SonyDevBypass.exe"
#define InstallDir "C:\SonyDev\Bypass"
#define InnerSetupFile "SonyDevBypass-win-x64-beta-Setup.exe"
#define InnerSetupSource "..\Releases\" + InnerSetupFile
#define AppIcon "..\SonyDevBypass.App\Assets\icon.ico"

[Setup]
AppId={{1BBD63A7-4C9A-4A7D-B9D6-DF8E8F594A12}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/SonyDew/SonyDev-Bypass
AppSupportURL=https://github.com/SonyDew/SonyDev-Bypass/issues
DefaultDirName={#InstallDir}
UsePreviousAppDir=no
CreateAppDir=no
DisableProgramGroupPage=yes
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
OutputDir=Output
OutputBaseFilename=SonyDevBypass-{#AppVersion}-Installer
SetupIconFile={#AppIcon}
CreateUninstallRegKey=no
Uninstallable=no
DisableWelcomePage=no
DisableDirPage=yes
DisableReadyMemo=yes
LicenseFile=..\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "chinese"; MessagesFile: "ChineseSimplified.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[CustomMessages]
EmbeddedSetupMissing=The bundled SonyDev Bypass setup could not be extracted.
InnerSetupFailed=The bundled SonyDev Bypass setup failed with exit code %1.
InstallingInnerSetup=Installing SonyDev Bypass...
LaunchApp=Launch SonyDev Bypass
ReadyToInstall=The installer will launch the SonyDev Bypass setup.
russian.EmbeddedSetupMissing=Не удалось извлечь встроенный установщик SonyDev Bypass.
russian.InnerSetupFailed=Встроенный установщик SonyDev Bypass завершился с кодом ошибки %1.
russian.InstallingInnerSetup=Установка SonyDev Bypass...
russian.LaunchApp=Запустить SonyDev Bypass
russian.ReadyToInstall=Установщик запустит установку SonyDev Bypass.
ukrainian.EmbeddedSetupMissing=Не вдалося видобути вбудований інсталятор SonyDev Bypass.
ukrainian.InnerSetupFailed=Вбудований інсталятор SonyDev Bypass завершився з кодом помилки %1.
ukrainian.InstallingInnerSetup=Встановлення SonyDev Bypass...
ukrainian.LaunchApp=Запустити SonyDev Bypass
ukrainian.ReadyToInstall=Інсталятор запустить установку SonyDev Bypass.
spanish.EmbeddedSetupMissing=No se pudo extraer el instalador integrado de SonyDev Bypass.
spanish.InnerSetupFailed=El instalador integrado de SonyDev Bypass finalizó con el código %1.
spanish.InstallingInnerSetup=Instalando SonyDev Bypass...
spanish.LaunchApp=Iniciar SonyDev Bypass
spanish.ReadyToInstall=El instalador iniciará la instalación de SonyDev Bypass.
french.EmbeddedSetupMissing=Impossible d'extraire le programme d'installation intégré de SonyDev Bypass.
french.InnerSetupFailed=Le programme d'installation intégré de SonyDev Bypass s'est terminé avec le code %1.
french.InstallingInnerSetup=Installation de SonyDev Bypass...
french.LaunchApp=Lancer SonyDev Bypass
french.ReadyToInstall=Le programme d'installation lancera l'installation de SonyDev Bypass.
german.EmbeddedSetupMissing=Das integrierte Setup von SonyDev Bypass konnte nicht extrahiert werden.
german.InnerSetupFailed=Das integrierte Setup von SonyDev Bypass wurde mit dem Code %1 beendet.
german.InstallingInnerSetup=SonyDev Bypass wird installiert...
german.LaunchApp=SonyDev Bypass starten
german.ReadyToInstall=Das Setup startet die Installation von SonyDev Bypass.
italian.EmbeddedSetupMissing=Impossibile estrarre il programma di installazione integrato di SonyDev Bypass.
italian.InnerSetupFailed=Il programma di installazione integrato di SonyDev Bypass è terminato con il codice %1.
italian.InstallingInnerSetup=Installazione di SonyDev Bypass...
italian.LaunchApp=Avvia SonyDev Bypass
italian.ReadyToInstall=Il programma di installazione avvierà l'installazione di SonyDev Bypass.
portuguese.EmbeddedSetupMissing=Não foi possível extrair o instalador incorporado do SonyDev Bypass.
portuguese.InnerSetupFailed=O instalador incorporado do SonyDev Bypass terminou com o código %1.
portuguese.InstallingInnerSetup=A instalar o SonyDev Bypass...
portuguese.LaunchApp=Iniciar o SonyDev Bypass
portuguese.ReadyToInstall=O instalador irá iniciar a instalação do SonyDev Bypass.
japanese.EmbeddedSetupMissing=埋め込みのSonyDev Bypassセットアップを展開できませんでした。
japanese.InnerSetupFailed=埋め込みのSonyDev Bypassセットアップは終了コード %1 で終了しました。
japanese.InstallingInnerSetup=SonyDev Bypass をインストールしています...
japanese.LaunchApp=SonyDev Bypass を起動
japanese.ReadyToInstall=インストーラーは SonyDev Bypass のセットアップを開始します。
korean.EmbeddedSetupMissing=내장된 SonyDev Bypass 설치 프로그램을 추출할 수 없습니다.
korean.InnerSetupFailed=내장된 SonyDev Bypass 설치 프로그램이 종료 코드 %1 로 종료되었습니다.
korean.InstallingInnerSetup=SonyDev Bypass 설치 중...
korean.LaunchApp=SonyDev Bypass 실행
korean.ReadyToInstall=설치 프로그램이 SonyDev Bypass 설치를 시작합니다.
chinese.EmbeddedSetupMissing=无法提取内置的 SonyDev Bypass 安装程序。
chinese.InnerSetupFailed=内置的 SonyDev Bypass 安装程序返回了错误代码 %1。
chinese.InstallingInnerSetup=正在安装 SonyDev Bypass...
chinese.LaunchApp=启动 SonyDev Bypass
chinese.ReadyToInstall=安装程序将启动 SonyDev Bypass 的安装。
polish.EmbeddedSetupMissing=Nie można wypakować dołączonego instalatora SonyDev Bypass.
polish.InnerSetupFailed=Dołączony instalator SonyDev Bypass zakończył działanie z kodem %1.
polish.InstallingInnerSetup=Instalowanie SonyDev Bypass...
polish.LaunchApp=Uruchom SonyDev Bypass
polish.ReadyToInstall=Instalator uruchomi instalację SonyDev Bypass.
turkish.EmbeddedSetupMissing=Gomulu SonyDev Bypass kurulum paketi cikarilamadi.
turkish.InnerSetupFailed=Gomulu SonyDev Bypass kurulum paketi %1 koduyla sona erdi.
turkish.InstallingInnerSetup=SonyDev Bypass yukleniyor...
turkish.LaunchApp=SonyDev Bypass'i baslat
turkish.ReadyToInstall=Yukleyici SonyDev Bypass kurulumunu baslatacak.
arabic.EmbeddedSetupMissing=تعذر استخراج مثبت SonyDev Bypass المضمن.
arabic.InnerSetupFailed=انتهى مثبت SonyDev Bypass المضمن برمز الخطأ %1.
arabic.InstallingInnerSetup=جار تثبيت SonyDev Bypass...
arabic.LaunchApp=تشغيل SonyDev Bypass
arabic.ReadyToInstall=سيقوم المثبت ببدء تثبيت SonyDev Bypass.

[Files]
Source: "{#InnerSetupSource}"; DestDir: "{tmp}"; Flags: deleteafterinstall ignoreversion

[Run]
Filename: "{tmp}\{#InnerSetupFile}"; Parameters: "--installto ""{#InstallDir}"""; StatusMsg: "{cm:InstallingInnerSetup}"; Flags: nowait skipifsilent
