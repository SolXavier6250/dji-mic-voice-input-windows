# v1.1.0 — DJI 麦克风语音输入

- 单文件 EXE，双击即运行，提供中文托盘菜单。
- DJI 发射器配对键触发 Windows `Win+H` 语音输入。
- 过滤附带的音量增加事件，包括系统生成的注入事件。
- 托盘内切换登录自启动，支持退出和使用说明。

已在 Windows 11 25H2、USB 接收器 `2CA3:4011` 上验证听写与音量过滤。其他硬件未验证；详细兼容范围和时间关联过滤限制见 README。

下载 `DJI-Voice-Input-Windows.exe` 即可使用。无需 AutoHotkey 或 Python，需要 Windows .NET Framework 4.x。应用未签名。
