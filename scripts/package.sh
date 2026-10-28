#!/usr/bin/env bash
#
# 打包发布制品（Linux / macOS 宿主）
#
# 用法:
#   ./scripts/package.sh [rid] [version]
#   ./scripts/package.sh linux-x64 1.2.0
#
# 版本号：优先取第 2 个参数，其次取环境变量 VERSION，最后回退到
#         NetRadio.csproj 里的 <Version>（读不到则用 0.0.0）。
#
# 产物（文件名带版本号）:
#   dist/NetRadio-<version>-<rid>.tar.gz
#   dist/NetRadio-<version>-<rid>.sha256
#
# 发布形态：单文件（PublishSingleFile），制品里只有可执行文件与文档。
# 框架依赖：目标机器需安装 .NET 10 运行时；Linux 另需系统 libvlc。
#
# 与 CI 共用同一份逻辑，见 .github/workflows/build.yml
#
set -euo pipefail

RID="${1:-linux-x64}"
VERSION="${2:-${VERSION:-}}"
CONFIG="${CONFIG:-Release}"

cd "$(dirname "$0")/.."
ROOT="$(pwd)"

# ---- 版本号：参数 > 环境变量 > csproj <Version> > 0.0.0 ----
if [ -z "$VERSION" ]; then
    VERSION="$(sed -n 's:.*<Version>[[:space:]]*\([^<[:space:]]*\)[[:space:]]*</Version>.*:\1:p' \
        NetRadio.csproj 2>/dev/null | head -n 1)"
fi
if [ -z "$VERSION" ]; then
    VERSION="0.0.0"
fi
VERSION="${VERSION#v}"
VERSION="${VERSION#V}"

RAW="$ROOT/publish/$RID/raw"   # dotnet publish 原始输出（单文件模式下仍会带出被内嵌的原生库）
OUT="$ROOT/publish/$RID/app"   # 最终入包目录：只放单文件可执行程序 + 文档
DIST="$ROOT/dist"
NAME="NetRadio-$VERSION-$RID"

echo "==> 发布 $RID（配置 $CONFIG，版本 $VERSION，单文件）"
rm -rf "$ROOT/publish/$RID"
mkdir -p "$DIST" "$OUT"

PUBLISH_ARGS=(
    -c "$CONFIG" -r "$RID" --self-contained false -o "$RAW"
    -p:PublishSingleFile=true
    -p:IncludeNativeLibrariesForSelfExtract=true
    -p:Version="$VERSION"
    -p:DebugType=none -p:DebugSymbols=false
)

# Windows 需要把 libvlc 原生库与插件（Content）一并内嵌进单文件
case "$RID" in
    win-*) PUBLISH_ARGS+=(-p:IncludeAllContentForSelfExtract=true) ;;
esac

dotnet publish NetRadio.csproj "${PUBLISH_ARGS[@]}"

# 只保留单文件可执行程序：单文件发布时原生库/托管库已内嵌，
# 逐文件散落的副本不需要随包分发。
if [ -f "$RAW/NetRadio.exe" ]; then
    cp "$RAW/NetRadio.exe" "$OUT/"
else
    cp "$RAW/NetRadio" "$OUT/"
    chmod +x "$OUT/NetRadio"
fi

# 附带文档与桌面入口
for f in README.md LICENSE CHANGELOG.md packaging/NetRadio.desktop packaging/NetRadio.svg; do
    if [ -f "$f" ]; then cp "$f" "$OUT/"; fi
done
if [ -f scripts/install-linux-deps.sh ]; then
    cp scripts/install-linux-deps.sh "$OUT/"
fi

ARCHIVE="$DIST/$NAME.tar.gz"
rm -f "$ARCHIVE"
tar -czf "$ARCHIVE" -C "$OUT" .
echo "==> 已生成 $ARCHIVE"

(
    cd "$DIST"
    sha256sum "$(basename "$ARCHIVE")" > "$NAME.sha256"
)
echo "==> 已生成 $DIST/$NAME.sha256"
echo "==> 完成，大小：$(du -h "$ARCHIVE" | cut -f1)"
