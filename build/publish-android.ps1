# Релизная сборка Android: dotnet publish -c Release (APK + AAB).
# Требования:
#   - android workload: dotnet workload install android
#   - Android SDK (если нет - установится через:
#     dotnet msbuild src\Lines.Android\Lines.Android.csproj
#       -t:InstallAndroidDependencies -p:AcceptAndroidSDKLicenses=true
#       "-p:AndroidSdkDirectory=$env:LOCALAPPDATA\Android\Sdk")
# Результат: build/out/android/com.valkimi.lines-Signed.apk (подписан
# отладочным ключом шаблона; для релиза Google Play нужен свой keystore
# через /p:AndroidKeyStore=True и AndroidSigningKeyStore/...).
# Использование: powershell -File build/publish-android.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\Lines.Android\Lines.Android.csproj"
$out = Join-Path $root "build\out\android"

$workloads = dotnet workload list
# -match к массиву возвращает подходящие строки; пустой результат = нет workload
if (-not ($workloads -match "android")) {
    "android workload не установлен. Установите: dotnet workload install android"
    exit 1
}

# SDK: предпочитаем ANDROID_HOME, иначе стандартный путь установщика
$androidHome = [Environment]::GetEnvironmentVariable("ANDROID_HOME", "Process")
if (-not $androidHome) { $androidHome = Join-Path $env:LOCALAPPDATA "Android\Sdk" }
if (-not (Test-Path (Join-Path $androidHome "platform-tools"))) {
    "Android SDK не найден в $androidHome. Установите зависимости см. шапку скрипта."
    exit 1
}

dotnet publish $project -c Release -o $out "-p:AndroidSdkDirectory=$androidHome"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

"опубликовано: $out"
Get-ChildItem $out -Include *.apk, *.aab | ForEach-Object { "  $($_.Name) ($([math]::Round($_.Length/1MB, 1)) МБ)" }
