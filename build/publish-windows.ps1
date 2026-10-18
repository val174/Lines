# Релизная сборка Windows: dotnet publish -c Release.
# Результат - самодостаточный каталог build/out/windows
# (framework-dependent; требует установленный .NET 8 Runtime).
# Использование: powershell -File build/publish-windows.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\Lines.Windows\Lines.Windows.csproj"
$out = Join-Path $root "build\out\windows"

dotnet publish $project -c Release -o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

"опубликовано: $out"
Get-ChildItem $out -Filter *.exe | ForEach-Object { "  $($_.Name)" }
