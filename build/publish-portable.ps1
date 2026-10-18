# Portable-сборка Windows: один exe без установки.
# dotnet publish single-file: runtime .NET 8, DLL, нативные SDL2/OpenAL
# и контент .xnb упаковываются внутрь exe (self-extract во временную
# папку при запуске). Сжатие включено (EnableCompressionInSingleFile).
# Результат: build\out\portable\Lines.exe
# Использование: powershell -File build\publish-portable.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\Lines.Windows\Lines.Windows.csproj"
$out = Join-Path $root "build\out\portable"

dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# рядом с exe остаются pdb и прочий мусор публикации - убираем
Get-ChildItem $out -File | Where-Object { $_.Name -ne "Lines.Windows.exe" } |
    Remove-Item -Force
Rename-Item (Join-Path $out "Lines.Windows.exe") "Lines.exe" -Force

$exe = Join-Path $out "Lines.exe"
"portable: $exe ({0:N1} МБ)" -f ((Get-Item $exe).Length / 1MB)
