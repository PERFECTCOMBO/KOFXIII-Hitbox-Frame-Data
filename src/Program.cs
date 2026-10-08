using System;
using System.Threading;
using System.Windows.Forms;
internal static class DebuggerEntry {
    [STAThread] static void Main(string[] args){
        if(args.Length>0&&args[0]=="--diagnose"){ReadOnlyDiagnostic.Run();return;}
        if(args.Length>0&&args[0]=="--self-test"){DebuggerTests.Run();return;}
        bool fresh;using(var mutex=new Mutex(true,"Kof13GlobalMatchLiveCapture",out fresh)){
            if(!fresh){MessageBox.Show("Close the previous KOF viewer normally before opening the debugger.");return;}
            Native.SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DebuggerWindow(true));
        }
    }
}
