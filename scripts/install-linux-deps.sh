#!/usr/bin/env bash
# 安装 NetRadio 在 Linux 上所需的系统依赖（libvlc + X11/Wayland 运行库）。
set -euo pipefail

if command -v apt-get >/dev/null 2>&1; then
    echo "检测到 Debian / Ubuntu 系，使用 apt 安装…"
    sudo apt-get update
    sudo apt-get install -y vlc libvlc-dev \
        libx11-6 libice6 libsm6 libfontconfig1 libcairo2 libgtk-3-0
elif command -v dnf >/dev/null 2>&1; then
    echo "检测到 Fedora / RHEL 系，使用 dnf 安装…"
    sudo dnf install -y vlc vlc-devel \
        libX11 libICE libSM fontconfig cairo gtk3
elif command -v pacman >/dev/null 2>&1; then
    echo "检测到 Arch 系，使用 pacman 安装…"
    sudo pacman -S --needed vlc \
        libx11 libice libsm fontconfig cairo gtk3
else
    echo "未识别的包管理器，请手动安装 vlc / libvlc-dev 以及 X11 运行库。"
    exit 1
fi

echo "依赖安装完成。可使用 ./scripts/build-linux.sh 构建并运行 NetRadio。"
