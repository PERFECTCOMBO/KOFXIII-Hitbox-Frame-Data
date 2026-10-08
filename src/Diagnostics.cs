using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
internal static class PipelineDiagnostics {
    internal static bool Attached,Memory,Mock;
    internal static long Captures,Losses;
    internal static DateTime LastCapture,LastRender;
    internal static DebugFrame Latest;
    internal static string Error="None",Stage="Detached",Overlay="Not requested";
    static long lastReport;
    internal static void Begin(bool mock){Attached=false;Memory=false;Mock=mock;Captures=Losses=lastReport=0;Latest=null;LastCapture=LastRender=DateTime.MinValue;Error="None";Set(mock?"Mock provider; no live attachment":"Process detection");}
    internal static void Set(string s){Stage=s;DebugLog.Write("PIPELINE "+s);}
    internal static void Fail(Exception e){Error=e.Message;DebugLog.Write("PIPELINE ERROR "+e);}
    internal static void Captured(DebugFrame f){Latest=f;Memory=true;Captures++;Losses+=f.MissingBefore;LastCapture=DateTime.UtcNow;
        if(Captures==1)Set("Buffered data received / state reader and timing tracker active");
        if(Captures==1||Captures-lastReport>=300){lastReport=Captures;DebugLog.Write("CAPTURE #"+f.Number+" total="+Captures+" losses="+Losses+" "+Player(f,0)+" "+Player(f,1));}
    }
    internal static string Player(DebugFrame f,int p){var s=f.Meter.Players[p];return "P"+(p+1)+" detected="+(s.NativeStateId.HasValue?"YES":"NO")+" actor="+(s.ActorIdentity.HasValue?s.ActorIdentity.Value.ToString("X8"):"--")+" action="+s.NativeActionId+" timer="+s.NativeActionTime+" state="+s.NativeStateId+" / "+s.NativeSecondaryStateId+" hitstop="+s.NativeHitstop+" observedAdv="+(s.ExperimentalAdvantage==null?"--":s.ExperimentalAdvantage.Gap+" "+s.ExperimentalAdvantage.Outcome+" updates");}
    internal static void Rendered(){LastRender=DateTime.UtcNow;}
    internal static string Report(){var b=new StringBuilder();bool game=false;foreach(var p in Process.GetProcessesByName("game"))using(p){try{if(FrameCapture.IsSupportedPath(p.MainModule.FileName))game=true;}catch(Exception e){DebugLog.Write("PROCESS INSPECTION "+e.Message);}}
        b.AppendLine("Mode: "+(Mock?"MOCK / simulated":"LIVE / native observations"));b.AppendLine("Game detected: "+(game?"YES":"NO"));b.AppendLine("Process attached: "+(Attached?"YES":"NO"));b.AppendLine("Memory access: "+(Memory&&!Mock?"YES":"NO / not checked"));b.AppendLine("Stage: "+Stage);
        b.AppendLine("Captured updates: "+Captures+"  Lost: "+Losses);b.AppendLine("Last capture age: "+(LastCapture==DateTime.MinValue?"--":(DateTime.UtcNow-LastCapture).TotalSeconds.ToString("0.0")+"s"));
        if(Latest!=null){b.AppendLine(Player(Latest,0));b.AppendLine(Player(Latest,1));b.AppendLine("Capture counter: "+Latest.Number);for(int p=0;p<2;p++)b.AppendLine("P"+(p+1)+" timing: "+(Latest.Meter.Players[p].TimingNote??"Waiting for complete action"));}
        else b.AppendLine("Player 1 / Player 2 detected: NO / no capture received");
        b.AppendLine("Simulation frame counter: UNMAPPED (capture counter is not game time)");b.AppendLine("Readiness recording: "+(ReadinessRecorder.LastPath??"Waiting for R4 live capture"));b.AppendLine("Readiness write error: "+(ReadinessRecorder.LastError??"None"));b.AppendLine("HUD rendered recently: "+((DateTime.UtcNow-LastRender).TotalSeconds<2?"YES":"NO"));b.AppendLine("Overlay: "+Overlay);b.AppendLine("Last error: "+Error);b.AppendLine("Log write error: "+(DebugLog.LastError??"None"));
        b.AppendLine("Stale captures can mean an unfocused/paused game, menus, or capture failure. Do not infer a pause state from elapsed wall time.");return b.ToString();
    }
    internal static string Export(){string path=Path.Combine(AppPaths.DataDirectory,"diagnostic-report.txt");File.WriteAllText(path,Report());return path;}
    internal static void Show(){var form=new Form{Text="Capture diagnostics",Width=820,Height=500};var text=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,Font=new System.Drawing.Font("Consolas",10)};var button=new Button{Text="Save report",Dock=DockStyle.Bottom};button.Click+=(s,e)=>{try{Export();}catch(Exception ex){Fail(ex);}};form.Controls.Add(text);form.Controls.Add(button);var timer=new Timer{Interval=500};timer.Tick+=(s,e)=>text.Text=Report();form.FormClosed+=(s,e)=>timer.Dispose();text.Text=Report();timer.Start();form.Show();}
}
internal static class ReadOnlyDiagnostic {
    internal static void Run(){var b=new StringBuilder("Global Match READ-ONLY probe: no capture installed, no game writes.\r\n");
        foreach(var p in Process.GetProcessesByName("game"))using(p){IntPtr h=IntPtr.Zero;try{string path=p.MainModule.FileName;b.AppendLine("PID "+p.Id+" path "+path);if(!FrameCapture.IsSupportedPath(path))continue;
            string hash;using(var sha=SHA256.Create())using(var file=File.OpenRead(path))hash=BitConverter.ToString(sha.ComputeHash(file)).Replace("-","");b.AppendLine("SHA256 "+hash);if(hash!=FrameCapture.Fingerprint){b.AppendLine("UNSUPPORTED BUILD; no offsets read");continue;}
            h=Native.OpenProcess(0x410,false,p.Id);if(h==IntPtr.Zero)throw Native.Error("Read access denied");var pe=File.ReadAllBytes(path);int peoff=BitConverter.ToInt32(pe,0x3c);b.AppendLine("PE machine 0x"+BitConverter.ToUInt16(pe,peoff+4).ToString("X4")+" (014C = x86)");
            b.AppendLine("Capture site: "+BitConverter.ToString(Read(h,0x5D7551,5)));
            for(uint player=0;player<2;player++){uint actor=U(h,0xDB6FE8+4*player);uint wrapper=actor==0?0:U(h,actor+0x30);uint core=wrapper==0?0:U(h,wrapper+8);if(core==0||U(h,core)!=0x8AE8BC){b.AppendLine("P"+(player+1)+" not available / vtable mismatch");continue;}uint body=U(h,core+8);if(body==0)continue;b.AppendLine("P"+(player+1)+" body="+body.ToString("X8")+" state="+U(h,body+0xA0)+" secondary="+U(h,body+0xA4)+" action="+U(h,body+0x88)+" timer="+U(h,body+0x9C));}
        }catch(Exception e){b.AppendLine("ERROR "+e);}finally{if(h!=IntPtr.Zero)Native.CloseHandle(h);}}
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"read-only-diagnostic.txt"),b.ToString());
    }
    static byte[] Read(IntPtr h,uint address,int length){byte[] b=new byte[length];UIntPtr got;if(!Native.ReadProcessMemory(h,new IntPtr((long)address),b,(UIntPtr)length,out got)||got.ToUInt64()!=(ulong)length)throw Native.Error("Cannot read 0x"+address.ToString("X"));return b;}
    static uint U(IntPtr h,uint address){return BitConverter.ToUInt32(Read(h,address,4),0);}
}
