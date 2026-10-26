# Windows 构建 / 发布脚本
# 用法:
#   .\scripts\build-windows.ps1           # 运行（Debug）
#   .\scripts\build-windows.ps1 publish   # 发布到 publish\win-x64

param(
    [string]$Mode = "run"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if ($Mode -eq "publish") {
    Write-Host "发布 win-x64 …" -ForegroundColor Cyan
    dotnet publish NetRadio.csproj -c Release -r win-x64 --self-contained false `
        -o publish/win-x64

    Write-Host "完成：publish\win-x64\NetRadio.exe" -ForegroundColor Green
}
else {
    Write-Host "构建并运行（Debug）…" -ForegroundColor Cyan
    dotnet build NetRadio.csproj -c Debug
    dotnet run --project NetRadio.csproj --no-build
}
