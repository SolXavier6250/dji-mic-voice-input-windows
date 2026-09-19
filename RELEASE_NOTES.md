# v1.1.1 — DJI 麦克风语音输入

- EXE 和系统托盘采用透明背景的 DJI 标志＋小扳手图标，内嵌多个尺寸，无需旁置图标文件。
- 明确实测硬件为 DJI Mic Mini 2S 发射器＋DJI Mic Series Mobile Receiver 手机版接收器，补充其他型号报文可能不同的说明。
- 保留配对键触发 `Win+H`、音量事件过滤、中文托盘菜单及登录自启动开关。
- 附带图标源文件、ICO 和转换脚本，支持从源码重新构建。

仅确认使用者手中的 **DJI Mic Mini 2S 发射器＋DJI Mic Series Mobile Receiver（大疆麦克风系列手机版接收器，USB VID/PID `2CA3:4011`）**组合在 Windows 11 25H2 上可用，已验证听写与音量过滤。其他型号可以尝试，但不保证可用，也不保证返回的按键报文一致；详细兼容范围和时间关联过滤限制见 README。

下载 `DJI-Voice-Input-Windows.exe` 即可使用。无需 AutoHotkey 或 Python，需要 Windows .NET Framework 4.x。应用未签名。
