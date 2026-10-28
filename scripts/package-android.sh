#!/usr/bin/env bash
#
# 打包 Android 制品（APK）
#
# 用法:
#   ./scripts/package-android.sh [version] [abi]
#   ./scripts/package-android.sh 1.2.0
#   ./scripts/package-android.sh 1.2.0 android-arm64
#
# 版本号：优先取第 1 个参数，其次取环境变量 VERSION，最后回退到
#         Android/NetRadio.Android.csproj 里的 <Version>（读不到则用 0.0.0）。
#
# ABI（第 2 个参数，默认 android-arm64;android-arm = 通用包）:
#   android-arm64            仅 64 位 ARM（体积最小，2016 年后的手机基本都支持）
#   android-arm;android-arm64 通用包（同时支持 32/64 位）
#   android-x64              模拟器
#
# 产物（文件名带版本号）:
#   dist/NetRadio-<version>-android.apk
#   dist/NetRadio-<version>-android.apk.sha256
#
# 签名说明：
#   未提供密钥库时使用 .NET Android 的调试密钥（debug.keystore）签名，
#   可直接安装用于测试，但不适合上架应用商店。
#   正式发布可传入以下环境变量改用自有密钥：
#     ANDROID_KEYSTORE_PATH / ANDROID_KEYSTORE_PASSWORD /
#     ANDROID_KEY_ALIAS / ANDROID_KEY_PASSWORD
#
# 依赖：.NET 10 SDK + `dotnet workload install android`、Android SDK（platform 36）、JDK 21
#
# 与 CI 共用同一份逻辑，见 .github/workflows/build.yml
#
set -euo pipefail

VERSION="${1:-${VERSION:-}}"
ABI="${2:-${ANDROID_ABI:-android-arm64;android-arm}}"
CONFIG="${CONFIG:-Release}"

cd "$(dirname "$0")/.."
ROOT="$(pwd)"
PROJECT="Android/NetRadio.Android.csproj"

# ---- 版本号：参数 > 环境变量 > csproj <Version> > 0.0.0 ----
if [ -z "$VERSION" ]; then
    VERSION="$(sed -n 's:.*<Version>[[:space:]]*\([^<[:space:]]*\)[[:space:]]*</Version>.*:\1:p' \
        "$PROJECT" 2>/dev/null | head -n 1)"
fi
if [ -z "$VERSION" ]; then
    VERSION="0.0.0"
fi
VERSION="${VERSION#v}"
VERSION="${VERSION#V}"

# Android 的 versionCode 必须是递增整数：1.2.3 -> 10203
VERSION_CODE="$(echo "$VERSION" | awk -F. '{printf "%d%02d%02d", $1, $2, $3}')"
if [ -z "$VERSION_CODE" ] || [ "$VERSION_CODE" = "000" ]; then
    VERSION_CODE=1
fi

NAME="NetRadio-$VERSION-android"
DIST="$ROOT/dist"
OUT_BIN="$ROOT/Android/bin/$CONFIG"

echo "==> 发布 Android（配置 $CONFIG，版本 $VERSION / code $VERSION_CODE，ABI $ABI）"

mkdir -p "$DIST"

PUBLISH_ARGS=(
    -c "$CONFIG" --nologo
    -p:Version="$VERSION"
    -p:ApplicationDisplayVersion="$VERSION"
    -p:ApplicationVersion="$VERSION_CODE"
    -p:AndroidPackageFormat=apk
    -p:RuntimeIdentifiers="$ABI"
    -p:RunAOTCompilation=false
    -p:AndroidEnableProfiledAot=false
    -p:DebugType=none -p:DebugSymbols=false
)

# 可选：使用自有密钥库签名（缺失则退回调试密钥）
if [ -n "${ANDROID_KEYSTORE_PATH:-}" ]; then
    echo "==> 使用自定义密钥库签名：$ANDROID_KEYSTORE_PATH"
    PUBLISH_ARGS+=(
        -p:AndroidKeyStore=true
        -p:AndroidSigningKeyStore="$ANDROID_KEYSTORE_PATH"
        -p:AndroidSigningStorePass="${ANDROID_KEYSTORE_PASSWORD:-}"
        -p:AndroidSigningKeyAlias="${ANDROID_KEY_ALIAS:-}"
        -p:AndroidSigningKeyPass="${ANDROID_KEY_PASSWORD:-}"
    )
fi

dotnet publish "$PROJECT" "${PUBLISH_ARGS[@]}"

# 取最新生成的那个 APK（多次构建可能残留其他 ABI 的旧产物）
APK="$(find "$OUT_BIN" -name '*-Signed.apk' -type f -exec ls -t {} + 2>/dev/null | head -n 1)"
if [ -z "$APK" ]; then
    APK="$(find "$OUT_BIN" -name '*.apk' -type f -exec ls -t {} + 2>/dev/null | head -n 1)"
fi
if [ -z "$APK" ]; then
    echo "!! 没找到 APK，请检查上面 dotnet publish 的输出" >&2
    exit 1
fi

cp "$APK" "$DIST/$NAME.apk"
echo "==> 已生成 $DIST/$NAME.apk"

(
    cd "$DIST"
    sha256sum "$NAME.apk" > "$NAME.apk.sha256"
)
echo "==> 已生成 $DIST/$NAME.apk.sha256"
echo "==> 完成，大小：$(du -h "$DIST/$NAME.apk" | cut -f1)"
