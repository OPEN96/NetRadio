#!/usr/bin/env bash
#
# 打包发布制品（Linux / macOS 宿主）
#
# 用法:
#   ./scripts/package.sh [rid] [version]
#   ./scripts/package.sh linux-x64 1.0.0
#
# 产物:
#   dist/NetRadio-<rid>.tar.gz
#   dist/NetRadio-<rid>.sha256
#
# 发布形态：单文件（PublishSingleFile），制品里只有可执行文件与文档。
# 框架依赖：目标机器需安装 .NET 10 运行时；Linux 另需系统 libvlc。
#
# 与 CI 共用同一份逻辑，见 .github/workflows/build.yml
#
set -euo pipefail

RID="${1:-linux-x64}"
VERSION="${2:-}"
CONFIG="${CONFIG:-Release}"

cd "$(dirname "$0")/.."
ROOT="$(pwd)"

RAW="$ROOT/publish/$RID/raw"   # dotnet publish 原始输出（单文件模式下仍会带出被内嵌的原生库）
OUT="$ROOT/publish/$RID/app"   # 最终入包目录：只放单文件可执行程序 + 文档
DIST="$ROOT/dist"
NAME="NetRadio-$RID"

echo "==> 发布 $RID（配置 $CONFIG，版本 ${VERSION:-默认}，单文件）"
rm -rf "$ROOT/publish/$RID"
mkdir -p "$DIST" "$OUT"

PUBLISH_ARGS=(
    -c "$CONFIG" -r "$RID" --self-contained false -o "$RAW"
    -p:PublishSingleFile=true
    -p:IncludeNativeLibrariesForSelfExtract=true
    -p:DebugType=none -p:DebugSymbols=false
)
[ -n "$VERSION" ] && PUBLISH_ARGS+=(-p:Version="$VERSION")

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
    [ -f "$f" ] && cp "$f" "$OUT/"
done
[ -f scripts/install-linux-deps.sh ] && cp scripts/install-linux-deps.sh "$OUT/"

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
