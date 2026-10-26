# 打包发布制品（Windows 宿主）
#
# 用法:
#   .\scripts\package.ps1 [-Rid win-x64] [-Version 1.0.0]
#
# 产物:
#   dist\NetRadio-<rid>.zip
#   dist\NetRadio-<rid>.sha256
#
# 与 CI 共用同一份逻辑，见 .github/workflows/build.yml

param(
    [string]$Rid = "win-x64",
    [string]$Version = "",
    [string]$Config = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$out  = Join-Path $root "publish/$Rid"
$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Path $dist -Force | Out-Null

Write-Host "==> 发布 $Rid（配置 $Config，版本 $(if ($Version) { $Version } else { '默认' })）" -ForegroundColor Cyan

if (Test-Path $out) { Remove-Item -Recurse -Force $out }

$publishArgs = @("publish", "NetRadio.csproj", "-c", $Config, "-r", $Rid, "--self-contained", "false", "-o", $out)
if ($Version) { $publishArgs += "-p:Version=$Version" }

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败，退出码 $LASTEXITCODE" }

foreach ($f in @("README.md", "LICENSE", "CHANGELOG.md")) {
    if (Test-Path $f) { Copy-Item $f $out -Force }
}

$name = "NetRadio-$Rid"
$zip  = Join-Path $dist "$name.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }

Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zip
Write-Host "==> 已生成 $zip" -ForegroundColor Green

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
Set-Content -Path (Join-Path $dist "$name.sha256") -Value "$hash  $name.zip" -Encoding ascii
Write-Host "==> 已生成 $dist\$name.sha256" -ForegroundColor Green

$size = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "==> 完成，大小：$size MB" -ForegroundColor Green

