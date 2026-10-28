# 打包发布制品（Windows 宿主）
#
# 用法:
#   .\scripts\package.ps1 [-Rid win-x64] [-Version 1.2.0]
#
# 版本号：优先取 -Version，其次取环境变量 VERSION，最后回退到
#         NetRadio.csproj 里的 <Version>（读不到则用 0.0.0）。
#
# 产物（文件名带版本号）:
#   dist\NetRadio-<version>-<rid>.zip
#   dist\NetRadio-<version>-<rid>.sha256
#
# 发布形态：单文件（PublishSingleFile）。Windows 下 libvlc 原生库与插件
# 通过 IncludeAllContentForSelfExtract 一并内嵌，首次启动会解压到临时目录缓存。
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

# ---- 版本号：参数 > 环境变量 VERSION > csproj <Version> > 0.0.0 ----
if (-not $Version) { $Version = $env:VERSION }
if (-not $Version) {
    $csproj = Join-Path $root "NetRadio.csproj"
    if (Test-Path $csproj) {
        $m = [regex]::Match((Get-Content $csproj -Raw), '<Version>\s*([^<]+?)\s*</Version>')
        if ($m.Success) { $Version = $m.Groups[1].Value }
    }
}
if (-not $Version) { $Version = "0.0.0" }
$Version = $Version.Trim().TrimStart('v', 'V')

$raw  = Join-Path $root "publish/$Rid/raw"   # dotnet publish 原始输出
$out  = Join-Path $root "publish/$Rid/app"   # 最终入包目录：只放单文件 exe + 文档
$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Path $dist -Force | Out-Null

Write-Host "==> 发布 $Rid（配置 $Config，版本 $Version，单文件）" -ForegroundColor Cyan

if (Test-Path (Join-Path $root "publish/$Rid")) { Remove-Item -Recurse -Force (Join-Path $root "publish/$Rid") }
New-Item -ItemType Directory -Path $out -Force | Out-Null

$publishArgs = @(
    "publish", "NetRadio.csproj", "-c", $Config, "-r", $Rid, "--self-contained", "false",
    "-o", $raw,
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:IncludeAllContentForSelfExtract=true",
    "-p:Version=$Version",
    "-p:DebugType=none",
    "-p:DebugSymbols=false"
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败，退出码 $LASTEXITCODE" }

# 只保留单文件可执行程序：其余托管库 / 原生库 / 插件均已内嵌到 exe 中
$exe = Join-Path $raw "NetRadio.exe"
if (-not (Test-Path $exe)) { $exe = Join-Path $raw "NetRadio" }
if (-not (Test-Path $exe)) { throw "未找到发布生成的 NetRadio 可执行文件：$raw" }
Copy-Item $exe $out -Force

foreach ($f in @("README.md", "LICENSE", "CHANGELOG.md")) {
    if (Test-Path $f) { Copy-Item $f $out -Force }
}

$name = "NetRadio-$Version-$Rid"
$zip  = Join-Path $dist "$name.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }

Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zip
Write-Host "==> 已生成 $zip" -ForegroundColor Green

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
Set-Content -Path (Join-Path $dist "$name.sha256") -Value "$hash  $name.zip" -Encoding ascii
Write-Host "==> 已生成 $dist\$name.sha256" -ForegroundColor Green

$size = [math]::Round((Get-Item $zip).Length / 1MB, 1)
$exeSize = [math]::Round((Get-Item (Join-Path $out "NetRadio.exe")).Length / 1MB, 1)
Write-Host "==> 完成：exe $exeSize MB，压缩包 $size MB" -ForegroundColor Green
