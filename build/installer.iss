; Установщик игры «Шарики» (Inno Setup 6).
; Собирается скриптом build\build-installer.ps1 (сначала делает
; self-contained publish в build\out\windows, затем вызывает ISCC.exe).
; Установка per-user в %LocalAppData%\Programs\Шарики - без прав
; администратора. Настройки/рекорды/сейвы живут в %AppData%\Lines и
; установщиком не трогаются.

#define AppName "Шарики"
#define AppVersion "1.0.0"
#define AppPublisher "Val + Kimi"
#define AppExe "Lines.Windows.exe"

[Setup]
AppId={{6F2C8A1D-3E4B-4C5A-9D2E-7B1A5F0C3D8E}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\{#AppName}
PrivilegesRequired=lowest
OutputDir=out
OutputBaseFilename=LinesSetup-{#AppVersion}
SetupIconFile=..\src\Lines.Windows\Icon.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; выбор языка мастера по локали Windows с возможностью сменить
ShowLanguageDialog=auto
DefaultGroupName={#AppName}

[Languages]
; язык мастера выбирается по локали Windows, пользователь может
; сменить в диалоге (ShowLanguageDialog)
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
russian.DesktopIconTask=Ярлык на рабочем столе
english.DesktopIconTask=Create a desktop shortcut
russian.AdditionalTasksGroup=Дополнительно:
english.AdditionalTasksGroup=Additional:
russian.RunAfterInstall=Запустить игру
english.RunAfterInstall=Launch the game

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIconTask}"; GroupDescription: "{cm:AdditionalTasksGroup}"

[Files]
; нативные библиотеки MonoGame для linux/osx/arm64 в Windows-установщике не нужны
Source: "out\windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "runtimes\linux-*,runtimes\osx*,runtimes\win-arm64,runtimes\win-x86"

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:RunAfterInstall}"; Flags: nowait postinstall skipifsilent
