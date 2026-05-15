# DC Traveler CLI

DC Traveler CLI 是一个用于 FF14 国服超域传送的命令行工具。它会帮助你完成登录会话获取、角色发现、目标服务器选择、提交订单和状态跟踪，但最终提交传送订单前仍需要你明确确认。

## 功能

- 一条命令完成常规超域传送流程。
- 支持保存登录会话，后续运行优先复用。
- 支持通过盛趣官方页面进入 WeGame 登录。
- 自动扫描可直接传送的角色，并显示可搜索的角色列表。
- 支持选择旅行中的角色：先返回原服，再刷新角色和目标数据后继续传送。
- 提供单独的返回原服命令。
- 使用方向键选择，Enter 确认；列表中可以直接输入关键词筛选。
- Ctrl+C 会先尝试优雅取消；如果卡在交互提示中，再按一次会强制退出。

## 安装

从 [GitHub Releases](https://github.com/Cookiekira/DCTravelerCli/releases) 下载适合系统的压缩包：

- Windows x64: `DCTravelerCli-v1.0.0-win-x64.zip`
- Windows x64 AOT: `DCTravelerCli-v1.0.0-win-x64-aot.zip`
- Linux x64: `DCTravelerCli-v1.0.0-linux-x64.tar.gz`
- macOS arm64: `DCTravelerCli-v1.0.0-osx-arm64.tar.gz`

解压后在终端运行：

```powershell
.\DCTravelerCli.exe --help
```

Linux 或 macOS:

```bash
chmod +x ./DCTravelerCli
./DCTravelerCli --help
```

## 快速开始

运行默认传送流程：

```bash
DCTravelerCli
```

使用 WeGame 登录入口：

```bash
DCTravelerCli --wegame
```

只刷新登录会话：

```bash
DCTravelerCli login
```

只返回旅行中的角色：

```bash
DCTravelerCli return
```

跳过 CLI 自己的提交前确认：

```bash
DCTravelerCli --yes
```

`--yes` 不会跳过官网要求的二次确认。

## 常用选项

```text
--wegame                  通过盛趣官方跳转页进入 WeGame 登录
--yes                     跳过 CLI 提交前确认
--verbose                 显示诊断细节
--keep-browser-open        流程结束后保留浏览器窗口
--browser-path <path>      手动指定 Chromium 浏览器路径
--default-browser-profile  使用默认浏览器 profile
--profile-dir <path>       指定专用浏览器 profile 目录
--debug-port <port>        指定浏览器 DevTools 调试端口
--login-timeout <seconds>  等待登录成功的秒数
--discovery-concurrency N  角色发现并发数，范围 1 到 16
```

## 工作方式

DC Traveler CLI 使用真实的 Chromium 系浏览器完成登录，然后提取官网会话 cookie。登录完成后，传送流程通过 .NET HTTP 客户端调用官方接口，不依赖浏览器页面脚本或 DOM 点击自动化。

保存的会话用于后续运行。Windows 上会话数据使用当前用户 DPAPI 保护。

## 本地开发

需要 .NET 10 SDK。

```bash
dotnet restore DCTravelerCli.slnx --locked-mode
dotnet test DCTravelerCli.slnx
dotnet run -- --help
```

发布产物由 GitHub Actions 的 `Release` 工作流生成。推送 `v*` tag 会运行测试、构建平台包、生成校验和，并创建 GitHub Release。
