# Релизная сборка Web (Blazor WASM + KNI BlazorGL): dotnet publish -c Release.
# Результат - статический сайт build/out/web/wwwroot (можно хостить на
# любом статическом хостинге, включая GitHub Pages).
# Использование: powershell -File build/publish-web.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\Lines.Web\Lines.Web.csproj"
$out = Join-Path $root "build\out\web"

dotnet publish $project -c Release -o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$site = Join-Path $out "wwwroot"
"опубликовано: $site"
"локальная проверка: dotnet serve -d `"$site`" (или любой статический сервер)"
