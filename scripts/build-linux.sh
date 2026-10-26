#!/usr/bin/env bash
# Linux 构建 / 发布脚本
# 用法:
#   ./scripts/build-linux.sh            # 运行（Debug）
#   ./scripts/build-linux.sh publish    # 发布到 publish/linux-x64
set -euo pipefail

cd "$(dirname "$0")/.."

MODE="${1:-run}"

if [ "$MODE" = "publish" ]; then
    echo "发布 linux-x64 …"
    dotnet publish NetRadio.csproj -c Release -r linux-x64 --self-contained false \
        -o publish/linux-x64

    chmod +x publish/linux-x64/NetRadio

    # 附带桌面入口，方便放进 ~/.local/share/applications
    mkdir -p publish/linux-x64
    cp packaging/NetRadio.desktop publish/linux-x64/
    cp packaging/NetRadio.svg publish/linux-x64/ 2>/dev/null || true

    echo "完成：publish/linux-x64/NetRadio"
else
    echo "构建并运行（Debug）…"
    dotnet build NetRadio.csproj -c Debug
    dotnet run --project NetRadio.csproj --no-build
fi
