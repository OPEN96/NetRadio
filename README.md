# NetRadio 网络收音机

一款基于 **.NET 10** 与 **Avalonia 12** 的跨平台网络收音机，支持 **Windows** 与 **Linux**（桌面端 X11 / Wayland）。

[![Build & Release](https://github.com/OPEN96/NetRadio/actions/workflows/build.yml/badge.svg)](https://github.com/OPEN96/NetRadio/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/OPEN96/NetRadio?label=release)](https://github.com/OPEN96/NetRadio/releases/latest)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux-blue)](https://github.com/OPEN96/NetRadio/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## 功能

| 能力 | 说明 |
| --- | --- |
| 在线电台目录 | 接入 [radio-browser.info](https://www.radio-browser.info) 社区目录，收录全球数万个网络电台，内置多镜像自动容错 |
| 分类浏览 | 热门推荐 / 华语电台 / 音乐 / 新闻资讯 / 爵士古典 / 电子舞曲 |
| 搜索 | 按名称搜索，输入即查（防抖 450ms） |
| 收藏与自定义 | 一键收藏；手动添加任意 http(s) / m3u8 流地址，支持编辑与删除 |
| 播放控制 | 播放 / 暂停 / 停止、上一曲 / 下一曲（在当前列表中循环切换）、音量、静音 |
| 系统托盘 | 最小化 / 关闭到托盘，托盘图标单击显示或隐藏主界面，托盘菜单提供播放 · 暂停 · 上一曲 · 下一曲 · 停止 · 退出 |
| 小窗口模式 | 独立的迷你播放窗口：常驻置顶、可拖拽、内置播放控制与音量，与主界面状态实时同步 |
| 正在播放 | 读取 ICY / Shoutcast 元数据，实时显示当前曲目 |
| 界面 | 珊瑚橙渐变强调色，浅色 / 深色双主题；方块台标卡片、标签胶囊、播放中的跳动电平条 |
| 录制 | 把正在收听的电台录制成 mp3 保存到本地 |
| 睡眠定时器 | 15 / 30 / 60 / 120 分钟后自动停止播放 |
| 主题 | 跟随系统 / 浅色 / 深色 |
| 断网可用 | 在线目录不可用时自动回退到内置稳定电台 |
| 断点续听 | 记住上一次收听的电台与音量，启动自动续播（可关闭） |

### 系统托盘与小窗口

- **最小化到托盘**：点击最小化按钮（或关闭按钮）时窗口收起到系统托盘，播放不中断。
  行为可在「设置 → 托盘与窗口」中分别开关「最小化时隐藏到托盘」「点击关闭按钮时最小化到托盘」。
- **托盘图标**：单击切换主界面的显示 / 隐藏；右键菜单提供
  显示 / 隐藏主界面、打开 / 关闭小窗口、播放 / 暂停、上一曲、下一曲、停止、退出。
- **小窗口模式**：点击顶部栏的「小窗口播放模式」按钮（或托盘菜单）即可把主界面切换为独立小窗口，
  小窗口与主界面共享同一播放状态，可拖拽、可置顶、可调节音量；点击右上角的放大按钮或关闭按钮即返回主界面。
- 若桌面环境不支持托盘（部分 Linux 缺少 AppIndicator），会自动降级为普通最小化 / 关闭，不会出现窗口失踪。

## 下载

从 [Releases](https://github.com/OPEN96/NetRadio/releases/latest) 页面下载对应平台的压缩包：

文件名格式为 `NetRadio-<版本>-<架构>.zip` / `.tar.gz`，版本号取自 tag（如 `v1.2.0` → `NetRadio-1.2.0-…`）：

| 平台 | 文件 | 运行方式 |
| --- | --- | --- |
| Windows x64 | `NetRadio-<版本>-win-x64.zip` | 解压后双击 `NetRadio.exe` |
| Linux x64 | `NetRadio-<版本>-linux-x64.tar.gz` | 解压后运行 `./NetRadio` |
| Linux ARM64 | `NetRadio-<版本>-linux-arm64.tar.gz` | 同上（需 .NET 10 ARM64 运行时） |

每个制品都附带同名的 `.sha256` 校验文件，例如
`sha256sum -c NetRadio-1.2.0-linux-x64.sha256`。

**单文件发布**：程序主体只有一个可执行文件（Windows 为 `NetRadio.exe`，Linux 为 `NetRadio`），
Avalonia / SkiaSharp / libvlc 等原生依赖已内嵌其中。首次启动时会把内嵌组件解压到系统临时目录缓存
（`%TEMP%\.net\NetRadio\…` 或 `/tmp/.net/NetRadio/…`），之后启动不再重复解压。

### Windows

需要 Windows 10 1809 或更高版本（x64）。libvlc 原生库与插件已内嵌在单个 `NetRadio.exe` 内，无需额外安装。

### Linux

Linux 构建为**框架依赖型**，运行前需先安装 **.NET 10 运行时**（x64 装 x64 运行时，ARM64 装 arm64 运行时），并安装**系统 libvlc**：

```bash
# .NET 10 运行时（以 x64 为例，ARM64 请选择对应架构包）
# 详见 https://dotnet.microsoft.com/download/dotnet/10.0

# libvlc 依赖
sudo apt install vlc libvlc-dev   # Debian / Ubuntu
# sudo dnf install vlc vlc-devel   # Fedora / RHEL
# sudo pacman -S vlc               # Arch
```

也可以直接用仓库里的脚本自动识别发行版：

```bash
chmod +x scripts/install-linux-deps.sh && ./scripts/install-linux-deps.sh
```

然后运行（将 `linux-x64` 换成对应架构，如 `linux-arm64`）：

```bash
# 压缩包内即为程序本体，直接解压到当前目录（用 -C 指定目录时需先创建）
tar -xzf NetRadio-1.2.0-linux-x64.tar.gz
chmod +x NetRadio
./NetRadio
```

安装到应用菜单（可选）：

```bash
sudo install -m 755 NetRadio /usr/local/bin/NetRadio
sudo install -m 644 NetRadio.svg /usr/share/pixmaps/NetRadio.svg
sudo install -m 644 NetRadio.desktop /usr/share/applications/NetRadio.desktop
```

## 从源码构建

需要 [.NET 10 SDK](https://dotnet.microsoft.com/download)。

```bash
# 运行（Debug）
dotnet run --project NetRadio.csproj

# 打包 Windows 制品 → dist/NetRadio-1.2.0-win-x64.zip
pwsh ./scripts/package.ps1 -Rid win-x64 -Version 1.2.0

# 打包 Linux 制品 → dist/NetRadio-1.2.0-linux-x64.tar.gz
./scripts/package.sh linux-x64 1.2.0

# 省略版本号时自动读取 NetRadio.csproj 的 <Version>
./scripts/package.sh linux-arm64
```

两个脚本都使用**单文件发布**（`PublishSingleFile`）：Windows 会把 libvlc 原生库与插件一并内嵌进
`NetRadio.exe`，Linux 则内嵌 SkiaSharp 等原生库，压缩包里只保留可执行文件与文档。
同一份工程也做了 libvlc 体积优化（按架构只带一份、剔除视频/字幕/投流等无关插件），
细节见 `NetRadio.csproj` 中的 `NetRadioTrimVlcPlugins` 目标。

打包脚本与 CI 共用同一份逻辑（见 `.github/workflows/build.yml`），本地产物与 GitHub Release 上的完全一致。

## 技术栈

- **.NET 10**（`net10.0`）
- **Avalonia UI 12.1** + Fluent 主题 + 编译型绑定
- **CommunityToolkit.Mvvm 8.4**（`ObservableProperty` / `RelayCommand` 源生成器）
- **LibVLCSharp 3.10** + libvlc 原生解码（Windows 随包发布，Linux 使用系统 libvlc）
- **System.Text.Json** 本地持久化

## 目录结构

```
NetRadio/
├── App.axaml / App.axaml.cs       应用入口、主题与依赖装配
├── Program.cs
├── Assets/Icons.axaml             全量 SVG 图标（无第三方图标依赖）
├── Models/                        RadioStation / PlaybackStatus / CategoryItem
├── Services/
│   ├── VlcRadioPlayer.cs          libvlc 播放后端（ICY 元数据、录制）
│   ├── NullRadioPlayer.cs         后端缺失时的降级占位
│   ├── RadioBrowserService.cs     在线电台目录（多镜像容错）
│   ├── FallbackStations.cs        离线内置电台
│   ├── StationLibrary.cs          收藏 / 自定义电台持久化
│   ├── SettingsService.cs         设置持久化
│   ├── AppPaths.cs                跨平台数据目录
│   └── DialogService.cs           对话框
├── ViewModels/                    MainViewModel / StationEditor / Settings
├── Views/                         MainWindow / StationEditorWindow / SettingsWindow
├── Converters/                    头像、图标等 XAML 转换器
├── packaging/                     Linux .desktop 与图标
├── scripts/                       构建、打包与依赖安装脚本
└── .github/workflows/             CI 自动构建与发布
```

## 数据存放位置

| 平台 | 路径 |
| --- | --- |
| Windows | `%APPDATA%\NetRadio\` |
| Linux | `~/.config/NetRadio/` |

- `settings.json` — 音量、主题、缓冲、录音目录、上次收听的电台
- `favorites.json` — 收藏
- `stations.json` — 自定义电台

录音默认输出到 `~/Music/NetRadio/`（可在设置中修改）。

## 发版流程

项目使用 GitHub Actions 自动构建并发布制品：

1. 更新 `CHANGELOG.md`
2. 提交并合并到 `main`
3. 打标签并推送：

   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

4. CI 先在独立的 `version` 任务里解析版本号（tag `v1.2.0` → `1.2.0`，非 tag 构建为 `0.0.0-ci`），
   再在 `windows-latest` 与 `ubuntu-latest` 上分别构建。Actions 构建制品（Artifact）按
   `NetRadio-<版本>-<架构>` 命名，例如 `NetRadio-1.2.0-win-x64`。
5. tag 构建会把 `NetRadio-<版本>-win-x64.zip`、`NetRadio-<版本>-linux-x64.tar.gz`、
   `NetRadio-<版本>-linux-arm64.tar.gz` 及各自的 `.sha256` 自动附加到新建的 GitHub Release 上。

推送到 `main` 或提交 PR 时只做构建验证，不会创建 Release。也可以在 Actions 页面手动触发。

## 常见问题

**播放没有声音 / 提示音频引擎初始化失败**
Windows：确认 `libvlc\win-x64` 目录与 `libvlc.dll` 存在于程序目录。
Linux：未安装系统 libvlc，见上方 Linux 依赖说明。

**一直显示"正在连接…"**
通常是网络不通或该电台流地址已失效；可切换电台试试。已经开始播放后若网络卡顿，界面会显示"正在播放（缓冲中…）"。

**电台列表为空**
应用已内置离线电台兜底；若提示"获取电台失败"，检查网络能否访问 `https://de1.api.radio-browser.info`。

**正在播放的曲目一直不显示**
部分电台不下发 ICY 元数据，属正常现象。

**录音文件无法播放**
录制依赖 libvlc 的 mp3 编码器；个别 AAC / OGG 流需要编码器可用才会成功。

**Windows 发布包体积较大**
约 430 MB，绝大部分是 `libvlc/win-x64/plugins`（完整解码插件）。如需瘦身，可删除用不到的插件目录（如 `video_output`、`video_filter`）。

## 许可

本项目源码采用 [MIT 许可证](LICENSE)。

Windows 发布包内附带 libvlc 的动态链接库与插件，遵循 **LGPL 2.1**（VideoLAN），
本应用仅以动态链接方式调用、未做修改：https://code.videolan.org/videolan/vlc

在线电台目录数据来自 [radio-browser.info](https://www.radio-browser.info)，由其社区维护。
