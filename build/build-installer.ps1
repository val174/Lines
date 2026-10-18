# Сборка установщика Windows: self-contained publish (игра несёт .NET
# 8 с собой, на машине пользователя ничего ставить не нужно) + Inno
# Setup (installer.iss) -> build\out\LinesSetup-<версия>.exe.
# Требует установленный Inno Setup 6 (winget install JRSoftware.InnoSetup).
# Использование: powershell -File build\build-installer.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\Lines.Windows\Lines.Windows.csproj"
$out = Join-Path $root "build\out\windows"

$iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (!(Test-Path $iscc)) { $iscc = "$env:ProgramFiles\Inno Setup 6\ISCC.exe" }
# winget ставит per-user в %LocalAppData%\Programs
if (!(Test-Path $iscc)) { $iscc = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" }
if (!(Test-Path $iscc)) {
    throw "Inno Setup 6 не найден. Установи: winget install JRSoftware.InnoSetup"
}

dotnet publish $project -c Release -r win-x64 --self-contained true -o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $iscc (Join-Path $PSScriptRoot "installer.iss")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Get-ChildItem (Join-Path $root "build\out") -Filter "LinesSetup-*.exe" |
    ForEach-Object { "установщик: $($_.FullName) ({0:N1} МБ)" -f ($_.Length / 1MB) }
