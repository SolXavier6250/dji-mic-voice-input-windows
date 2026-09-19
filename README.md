# DJI 麦克风语音输入 · Windows

把 DJI Mic 发射器的配对键变成 Windows 语音输入按钮：短按配对键，调用 `Win+H`，同时过滤附带的音量增加事件。

## 下载与使用

从 [Releases](https://github.com/sumuxi6250-oss/dji-mic-voice-input-windows/releases/latest) 下载 `DJI-Voice-Input-Windows.exe`，放在固定位置后双击运行。程序名称和托盘菜单为 **DJI 麦克风语音输入**。

1. 用 USB 数据线将 DJI 接收器连接电脑，并让发射器与接收器配对。
2. 确认 Windows 使用所需麦克风，并能通过键盘 `Win+H` 正常听写。
3. 双击 EXE，点击任意支持输入的文本框，然后短按发射器配对键。
4. 右键系统托盘图标，可以查看说明、设置“登录 Windows 时自动启动”或退出。

首次运行不会自动添加启动项。启用后请保留 EXE 的位置；退出程序不取消下次登录自启动。取消自启动可通过托盘菜单，或按 `Win+R` 输入 `shell:startup`，移除“DJI 麦克风语音输入”快捷方式。

程序是单文件 Windows 应用，依赖 .NET Framework 4.x，无需 Python、AutoHotkey 或额外驱动。EXE 未进行代码签名。Windows 语音输入需要联网；识别语言、录音权限和麦克风选择由 Windows 管理。

## 验证范围

已在一台 Windows 11 25H2 电脑和一套 DJI 设备上实际验证：

- 接收器名称 `Wireless Mic Rx`，USB VID/PID `2CA3:4011`。
- 发射器短按配对键发送三字节 HID 报文：按下 `06-01-00`，释放 `06-00-00`。
- 按键能打开 Windows 语音输入，用户确认可以输入文字。
- 附带的音量增加事件被过滤；其他音量事件被放行。

**不是所有 DJI 型号都已验证。** 仅按 VID/PID 与实测报文匹配；蓝牙直连、其他接收器和其他固件未验证。不自动发送输入的文字，也不保证重复按键在所有 Windows 版本中有相同的开始/停止听写行为。

## 工作方式与限制

程序通过 Raw Input 监听 Consumer Control 接口，仅用目标 DJI 接收器的实测报文触发 `Win+H`。按下事件有 500ms 防抖，按住 Ctrl、Shift、Alt 或 Win 时跳过听写快捷键。

音量事件通过用户态低级键盘钩子暂存约 100ms，包括由系统生成的注入事件。与 DJI 报文时间戳相差不超过 40ms 时丢弃，否则重新发送；自身重发带标记，避免循环。

这是时间关联过滤，不是驱动级设备隔离。几乎同时按 DJI 按钮和其他音量键时可能误判；普通音量键会有约 100ms 延迟。同 VID/PID 的多个接收器都会匹配。

应用本身不录音、不转写、不联网。若所在目录可写，会生成 `mapping.log`，记录 DJI 报文和音量过滤结果，不记录普通文字按键。日志写入失败不会阻止应用运行。

## 从源码构建

在 Windows PowerShell 中运行：

```powershell
.\build.ps1
```

产物：`dist\DJI-Voice-Input-Windows.exe`。双击即进入托盘映射模式；`--map` 同样启动映射，`--probe` 启动五分钟只读 DJI 报文检测。

编译使用 Windows 自带 .NET Framework C# 编译器，无需下载 NuGet 包。`dist` 目录不会提交到 Git，EXE 通过 Releases 提供。

## 参考与致谢

以下项目为 DJI HID 按键与 Windows 听写实现提供了参考：

- [caezium/dji-mic-wispr-flow](https://github.com/caezium/dji-mic-wispr-flow)：DJI 按键作为 USB HID 音量事件的用法。
- [Johnixr/dji-mic-dictation](https://github.com/Johnixr/dji-mic-dictation)：macOS 上的 DJI 听写流程。
- [CYBER-CITY-MEDIA/air-mouse-remote-windows](https://github.com/CYBER-CITY-MEDIA/air-mouse-remote-windows)：Windows Raw Input 到 Win+H 的实现参考。

与 DJI、Microsoft 无隶属关系。MIT License，详见 [LICENSE](LICENSE)。
