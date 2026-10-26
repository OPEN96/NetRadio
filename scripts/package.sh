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
# 与 CI 共用同一份逻辑，见 .github/workflows/build.yml
#
set -euo pipefail

RID="${1:-linux-x64}"
VERSION="${2:-}"
CONFIG="${CONFIG:-Release}"

cd "$(dirname "$0")/.."
ROOT="$(pwd)"

OUT="$ROOT/publish/$RID"
DIST="$ROOT/dist"
NAME="NetRadio-$RID"

echo "==> 发布 $RID（配置 $CONFIG，版本 ${VERSION:-默认}）"
rm -rf "$OUT"
mkdir -p "$DIST"

PUBLISH_ARGS=(-c "$CONFIG" -r "$RID" --self-contained false -o "$OUT")
if [ -n "$VERSION" ]; then
    PUBLISH_ARGS+=(-p:Version="$VERSION")
fi

dotnet publish NetRadio.csproj "${PUBLISH_ARGS[@]}"

chmod +x "$OUT/NetRadio"

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
