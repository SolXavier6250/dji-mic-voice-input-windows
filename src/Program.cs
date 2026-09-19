using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;
[assembly: System.Reflection.AssemblyTitle("DJI 麦克风语音输入")]
[assembly: System.Reflection.AssemblyDescription("DJI 配对键启动 Windows 语音输入，并过滤附带的音量事件")]
[assembly: System.Reflection.AssemblyProduct("DJI 麦克风语音输入")]
[assembly: System.Reflection.AssemblyVersion("1.1.0.0")]

class Probe : Form {
    [StructLayout(LayoutKind.Sequential)] struct Device { public ushort page, usage; public uint flags; public IntPtr window; }
    [StructLayout(LayoutKind.Sequential)] struct Header { public uint type, size; public IntPtr device, param; }
    [DllImport("user32.dll", SetLastError=true)] static extern bool RegisterRawInputDevices(Device[] devices, uint count, uint size);
    [DllImport("user32.dll")] static extern uint GetRawInputData(IntPtr input, uint command, IntPtr data, ref uint size, uint headerSize);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern uint GetRawInputDeviceInfo(IntPtr device, uint command, StringBuilder data, ref uint size);
    [DllImport("user32.dll")] static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] struct KeyEvent { public uint key, scan, flags, time; public UIntPtr extra; }
    [DllImport("user32.dll", SetLastError=true)] static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint thread);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll")] static extern int GetMessageTime();
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);
    class Pending { public KeyEvent key; public long arrived; }
    readonly List<Pending> pending = new List<Pending>();
    readonly List<uint> djiTimes = new List<uint>();
    HookProc callback;
    IntPtr hook;
    Timer volumeTimer;
    static readonly UIntPtr ReplayTag = new UIntPtr(0x444A4931u);
    IntPtr FilterVolume(int code, IntPtr message, IntPtr data) {
        if(code>=0) {
            KeyEvent key=(KeyEvent)Marshal.PtrToStructure(data,typeof(KeyEvent));
            if((key.key==0xAF || key.key==0xAE) && key.extra!=ReplayTag) {
                pending.Add(new Pending { key=key, arrived=debounce.ElapsedMilliseconds });
                return new IntPtr(1);
            }
        }
        return CallNextHookEx(hook,code,message,data);
    }
    void FlushVolume(bool exiting) {
        while(pending.Count>0 && (exiting || debounce.ElapsedMilliseconds-pending[0].arrived>=100)) {
            Pending item=pending[0]; pending.RemoveAt(0);
            bool fromDji=djiTimes.Exists(delegate(uint t) { return Math.Abs((long)unchecked((int)(item.key.time-t)))<=40; });
            if(fromDji) Log("SUPPRESSED DJI volume key="+item.key.key+" flags="+item.key.flags);
            else {
                uint flags=(item.key.flags&0x80)!=0 ? 2u : 0u;
                if((item.key.flags&1)!=0) flags|=1;
                keybd_event((byte)item.key.key,(byte)item.key.scan,flags,ReplayTag);
                Log("PASSED other volume key="+item.key.key+" flags="+item.key.flags+" time="+item.key.time);
            }
        }
    }
    readonly string log;
    readonly bool map;
    NotifyIcon tray;
    bool down;
    readonly System.Diagnostics.Stopwatch debounce = System.Diagnostics.Stopwatch.StartNew();
    long lastPress = -1000;
    void Log(string text) { try { File.AppendAllText(log, DateTime.Now.ToString("o") + " " + text + Environment.NewLine); } catch { } }
    static string StartupLink { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "DJI 麦克风语音输入.lnk"); } }
    static void SetStartup(bool enabled) {
        dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
        dynamic link=shell.CreateShortcut(StartupLink);
        try {
            if(File.Exists(StartupLink) && !String.Equals((string)link.TargetPath,Application.ExecutablePath,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("同名启动项指向另一份程序，请先在 Windows 启动文件夹中检查。");
            if(enabled) {
                link.TargetPath=Application.ExecutablePath;
                link.Arguments="--map";
                link.WorkingDirectory=AppDomain.CurrentDomain.BaseDirectory;
                link.Description="DJI 麦克风语音输入：配对键启动 Windows 语音输入";
                link.IconLocation=Application.ExecutablePath+",0";
                link.Save();
            } else if(File.Exists(StartupLink)) File.Delete(StartupLink);
        } finally { Marshal.FinalReleaseComObject(link); Marshal.FinalReleaseComObject(shell); }
    }
    protected override void SetVisibleCore(bool value) { base.SetVisibleCore(false); }
    public Probe(bool enableMapping) {
        map=enableMapping;
        log=Path.Combine(AppDomain.CurrentDomain.BaseDirectory, map ? "mapping.log" : "probe.log");
        IntPtr handle = Handle;
        bool ok = RegisterRawInputDevices(new Device[] { new Device { page=12, usage=1, flags=0x100, window=handle } }, 1, (uint)Marshal.SizeOf(typeof(Device)));
        Log("START consumer-control listener registration=" + ok + " error=" + Marshal.GetLastWin32Error());
        if (!ok) throw new InvalidOperationException("Unable to register DJI listener");
        if (map) {
            callback=FilterVolume;
            hook=SetWindowsHookEx(13,callback,GetModuleHandle(null),0);
            if(hook==IntPtr.Zero) throw new InvalidOperationException("Volume hook failed: "+Marshal.GetLastWin32Error());
            volumeTimer=new Timer { Interval=20 };
            volumeTimer.Tick += delegate { FlushVolume(false); };
            volumeTimer.Start();
            Log("Version 1.1.0; Volume filtering enabled: hardware AND injected events; 100ms queue, 40ms DJI correlation");
            var menu=new ContextMenuStrip();
            menu.Items.Add("DJI 麦克风语音输入（运行中）").Enabled=false;
            var startup=new ToolStripMenuItem("登录 Windows 时自动启动") { Checked=File.Exists(StartupLink) };
            startup.Click += delegate {
                try { SetStartup(!startup.Checked); startup.Checked=File.Exists(StartupLink); }
                catch(Exception ex) { MessageBox.Show(ex.Message,"DJI 麦克风语音输入"); }
            };
            menu.Items.Add(startup);
            menu.Items.Add("使用说明",null,delegate {
                MessageBox.Show("点击文本框，再短按 DJI 发射器配对键即可启动 Windows 语音输入。\n\n接收器需通过 USB 连接。语音识别需要联网。\n\n可通过此托盘菜单设置登录自启动或退出程序。设置自启动后请保留 EXE 所在位置。\n\n版本 1.1.0 · 本机已验证 DJI 2CA3:4011", "DJI 麦克风语音输入");
            });
            menu.Items.Add("退出 DJI 麦克风语音输入",null,delegate { Application.Exit(); });
            tray=new NotifyIcon { Icon=System.Drawing.SystemIcons.Application, Text="DJI 麦克风语音输入 · 配对键 → Win+H", ContextMenuStrip=menu, Visible=true };
            Application.ApplicationExit += delegate { volumeTimer.Stop(); UnhookWindowsHookEx(hook); FlushVolume(true); Log("STOP"); tray.Visible=false; tray.Dispose(); };
            return;
        }
        var timer = new Timer { Interval=300000 };
        timer.Tick += delegate { Log("STOP timeout"); Application.Exit(); };
        timer.Start();
    }
    protected override void WndProc(ref Message m) {
        if (m.Msg == 255) {
            uint size=0, hs=(uint)Marshal.SizeOf(typeof(Header));
            GetRawInputData(m.LParam,0x10000003,IntPtr.Zero,ref size,hs);
            if(size>=hs && size<65536) {
                IntPtr ptr=Marshal.AllocHGlobal((int)size);
                try {
                    if(GetRawInputData(m.LParam,0x10000003,ptr,ref size,hs)==size) {
                        Header h=(Header)Marshal.PtrToStructure(ptr,typeof(Header));
                        uint len=0;
                        GetRawInputDeviceInfo(h.device,0x20000007,null,ref len);
                        var name=new StringBuilder((int)len+1);
                        GetRawInputDeviceInfo(h.device,0x20000007,name,ref len);
                        string device=name.ToString().ToUpperInvariant();
                        if(h.type==2 && device.Contains("VID_2CA3&PID_4011") && size>=hs+8) {
                            int reportSize=Marshal.ReadInt32(ptr,(int)hs), count=Marshal.ReadInt32(ptr,(int)hs+4);
                            long total=(long)reportSize*count;
                            if(total>0 && total<=size-hs-8) {
                                byte[] bytes=new byte[(int)total];
                                Marshal.Copy(IntPtr.Add(ptr,(int)hs+8),bytes,0,bytes.Length);
                                Log("DJI reportSize="+reportSize+" count="+count+" data="+BitConverter.ToString(bytes));
                                if(map && reportSize==3) {
                                    for(int i=0;i<bytes.Length;i+=3) {
                                        if(bytes[i]!=6 || bytes[i+2]!=0) continue;
                                        if(bytes[i+1]==0 || bytes[i+1]==1) {
                                            djiTimes.Add(unchecked((uint)GetMessageTime()));
                                            if(djiTimes.Count>64) djiTimes.RemoveAt(0);
                                        }
                                        if(bytes[i+1]==0) { down=false; continue; }
                                        if(bytes[i+1]!=1 || down) continue;
                                        down=true;
                                        long now=debounce.ElapsedMilliseconds;
                                        if(now-lastPress<500) continue;
                                        lastPress=now;
                                        bool modifier=false;
                                        foreach(int key in new int[] {16,17,18,91,92}) if((GetAsyncKeyState(key)&0x8000)!=0) modifier=true;
                                        if(modifier) { Log("SKIP modifier held"); continue; }
                                        keybd_event(0x5B,0,0,UIntPtr.Zero);
                                        keybd_event(0x48,0,0,UIntPtr.Zero);
                                        keybd_event(0x48,0,2,UIntPtr.Zero);
                                        keybd_event(0x5B,0,2,UIntPtr.Zero);
                                        Log("SENT Win+H; UI result requires user verification");
                                    }
                                }
                            }
                        }
                    }
                } finally { Marshal.FreeHGlobal(ptr); }
            }
        }
        base.WndProc(ref m);
    }
    [STAThread] static void Main(string[] args) {
        bool map=!(args.Length==1 && args[0]=="--probe");
        bool created;
        using(var mutex=new System.Threading.Mutex(true,map ? "Local\\DjiVoiceButtonMap" : "Local\\DjiVoiceButtonProbe",out created)) {
            if(!created) {
                if(args.Length==0) MessageBox.Show("DJI 麦克风语音输入已在运行，请在任务栏右下角的托盘中查看。","DJI 麦克风语音输入");
                return;
            }
            try { Application.Run(new Probe(map)); }
            catch(Exception ex) { MessageBox.Show("启动失败："+ex.Message,"DJI 麦克风语音输入"); }
        }
    }
}
