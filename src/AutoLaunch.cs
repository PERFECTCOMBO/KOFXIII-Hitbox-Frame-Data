using System;
using System.Diagnostics;
using System.IO;

internal static class AppPaths {
    internal static string DataDirectory=AppDomain.CurrentDomain.BaseDirectory;
    internal static void UseUserData(){
        DataDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"KOF13HITBOX","v0.1");
        Directory.CreateDirectory(DataDirectory);
    }
}
// Bind to the exact game lifetime supplied by the loader, not a recycled PID
// or a different copy of game.exe. All validation is read-only.
internal sealed class AutoLaunch : IDisposable {
    readonly Process game;
    internal readonly int ProcessId;
    internal static AutoLaunch Current;
    internal static bool Parse(string[] args,out int pid,out long stamp){
        pid=0;stamp=0;
        return args.Length==3&&args[0]=="--autoload"&&Int32.TryParse(args[1],out pid)&&pid>0&&Int64.TryParse(args[2],out stamp)&&stamp>0;
    }
    internal AutoLaunch(int pid,long stamp){
        game=Process.GetProcessById(pid);
        try{
            if(game.HasExited||game.StartTime.ToUniversalTime().ToFileTimeUtc()!=stamp)
                throw new InvalidOperationException("The original game session has ended.");
            if(!FrameCapture.IsSupportedPath(game.MainModule.FileName))
                throw new InvalidOperationException("Unsupported Global Match build. The viewer did not attach.");
            ProcessId=pid;
        }catch{game.Dispose();throw;}
    }
#if AUTOLOAD_TESTS
    // Only present in the separate test harness, never in the release binary.
    internal AutoLaunch(Process testProcess){game=testProcess;ProcessId=testProcess.Id;}
#endif
    internal bool Alive {get{try{return !game.HasExited;}catch{return false;}}}
    internal bool Foreground {get{uint pid;Native.GetWindowThreadProcessId(Native.GetForegroundWindow(),out pid);return pid==(uint)ProcessId;}}
    internal Process OpenForCapture(){
        if(!Alive)throw new InvalidOperationException("The original game session has ended.");
        var p=Process.GetProcessById(ProcessId);
        if(p.StartTime.ToUniversalTime()!=game.StartTime.ToUniversalTime()){p.Dispose();throw new InvalidOperationException("Game session changed.");}
        return p;
    }
    public void Dispose(){game.Dispose();}
}
