#
# 打包 Android 制品（APK，Windows 宿主）
#
# 用法:
#   pwsh -File ./scripts/package-android.ps1
#   pwsh -File ./scripts/package-android.ps1 -Version 1.2.0
#   pwsh -File ./scripts/package-android.ps1 -Version 1.2.0 -Abi "android-arm64"
#
# 版本号：-Version > 环境变量 VERSION > Android/NetRadio.Android.csproj 的 <Version> > 0.0.0
#
# 产物（文件名带版本号）:
#   dist\NetRadio-<version>-android.apk
#   dist\NetRadio-<version>-android.apk.sha256
#
# 默认生成通用包（arm64 + arm32）。仅 64 位可用 -Abi "android-arm64" 缩小体积。
#
param(
    [string]$Version = $env:VERSION,
    [string]$Abi = "android-arm64;android-arm",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$project = "Android/NetRadio.Android.csproj"

# ---- 版本号：参数 > 环境变量 > csproj <Version> > 0.0.0 ----
if (-not $Version) {
    $csproj = Get-Content $project -Raw
    if ($csproj -match '<Version>\s*([^<\s]+)\s*</Version>') {
        $Version = $Matches[1]
    }
}
if (-not $Version) { $Version = "0.0.0" }
$Version = $Version.Trim().TrimStart('v', 'V')

# Android versionCode 必须是递增整数：1.2.3 -> 10203
$parts = $Version.Split('.')
$major = 0; $minor = 0; $patch = 0
if ($parts.Count -ge 1) { [void][int]::TryParse($parts[0], [ref]$major) }
if ($parts.Count -ge 2) { [void][int]::TryParse($parts[1], [ref]$minor) }
if ($parts.Count -ge 3) { [void][int]::TryParse(($parts[2] -replace '\D.*$', ''), [ref]$patch) }
$versionCode = $major * 10000 + $minor * 100 + $patch
if ($versionCode -lt 1) { $versionCode = 1 }

$name = "NetRadio-$Version-android"
$dist = Join-Path $root "dist"
$outBin = Join-Path $root "Android/bin/$Configuration"

Write-Host "==> 发布 Android（配置 $Configuration，版本 $Version / code $versionCode，ABI $Abi）"

if (-not (Test-Path $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }

$publishArgs = @(
    "-c", $Configuration, "--nologo",
    "-p:Version=$Version",
    "-p:ApplicationDisplayVersion=$Version",
    "-p:ApplicationVersion=$versionCode",
    "-p:AndroidPackageFormat=apk",
    "-p:RuntimeIdentifiers=$Abi",
    "-p:RunAOTCompilation=false",
    "-p:AndroidEnableProfiledAot=false",
    "-p:DebugType=none", "-p:DebugSymbols=false"
)

# 可选：使用自有密钥库签名（缺失则退回调试密钥）
if ($env:ANDROID_KEYSTORE_PATH) {
    Write-Host "==> 使用自定义密钥库签名：$env:ANDROID_KEYSTORE_PATH"
    $publishArgs += @(
        "-p:AndroidKeyStore=true",
        "-p:AndroidSigningKeyStore=$env:ANDROID_KEYSTORE_PATH",
        "-p:AndroidSigningStorePass=$env:ANDROID_KEYSTORE_PASSWORD",
        "-p:AndroidSigningKeyAlias=$env:ANDROID_KEY_ALIAS",
        "-p:AndroidSigningKeyPass=$env:ANDROID_KEY_PASSWORD"
    )
}

& dotnet publish $project @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败（退出码 $LASTEXITCODE）" }

$apk = Get-ChildItem -Path $outBin -Recurse -Filter "*-Signed.apk" -File -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $apk) {
    $apk = Get-ChildItem -Path $outBin -Recurse -Filter "*.apk" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
}
if (-not $apk) { throw "没找到 APK，请检查上面 dotnet publish 的输出" }

$target = Join-Path $dist "$name.apk"
Copy-Item $apk.FullName $target -Force
Write-Host "==> 已生成 $target"

$hash = (Get-FileHash $target -Algorithm SHA256).Hash.ToLower()
"$hash  $name.apk" | Out-File -FilePath "$target.sha256" -Encoding ascii -NoNewline
Write-Host "==> 已生成 $target.sha256"

$sizeMb = [math]::Round((Get-Item $target).Length / 1MB, 1)
Write-Host "==> 完成，大小：$sizeMb MB"
