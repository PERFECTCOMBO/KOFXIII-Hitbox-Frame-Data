using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
class AutoLifecycleTests {
 sealed class SessionProvider : IGameProvider {
  internal bool Disposed;
  public string Name {get{return "Test session";}} public bool IsMock {get{return false;}}
  public int ProcessId {get{return Process.GetCurrentProcess().Id;}}
  public DebugFrame Poll(){return null;} public void Dispose(){Disposed=true;}
 }
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static object Field(object o,string name){return o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 [STAThread] static int Main(string[] args){
  if(args.Length==1&&args[0]=="--child"){Thread.Sleep(12000);return 0;}
  string result=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"autoload-tests.txt");
  try{
   int pid;long stamp;
   Check(!AutoLaunch.Parse(new[]{"--autoload","0","4"},out pid,out stamp),"zero PID accepted");
   Check(!AutoLaunch.Parse(new[]{"--autoload","1","-4"},out pid,out stamp),"bad timestamp accepted");
   Check(!AutoLaunch.Parse(new[]{"--autoload","1"},out pid,out stamp),"partial args accepted");
   using(var own=Process.GetCurrentProcess()){
    bool rejected=false;try{using(var bad=new AutoLaunch(own.Id,1)){} }catch(InvalidOperationException){rejected=true;}Check(rejected,"recycled PID not rejected");
    rejected=false;try{using(var bad=new AutoLaunch(own.Id,own.StartTime.ToUniversalTime().ToFileTimeUtc())){} }catch(InvalidOperationException){rejected=true;}Check(rejected,"unsupported image not rejected");
   }
   AppPaths.DataDirectory=AppDomain.CurrentDomain.BaseDirectory;
   var child=Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location,"--child"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
   AutoLaunch.Current=new AutoLaunch(child);
   Native.SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   bool finished=false;int step=0;IntPtr originalHandle=IntPtr.Zero;
   using(var form=new ImGuiWindow(false))using(var timer=new System.Windows.Forms.Timer{Interval=250}){
    timer.Tick+=(s,e)=>{
     try{
      step++;
      if(step==1){originalHandle=form.Handle;Check(!form.Visible,"autoload flashed a panel");Check(!(bool)Field(form,"allowShow"),"panel initially allowed");Check((bool)Field(form,"ready"),"hidden initialization failed");Check(Field(form,"provider")==null,"attached before F8");Check(!((ViewerSettings)Field(form,"settings")).Overlay,"overlay initially on");}
      if(step==2){typeof(ImGuiWindow).GetMethod("ShowPanel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,null);Check(form.Visible,"F9 panel path failed");}
      if(step==3){Check(form.Handle==originalHandle,"settings panel replaced the hotkey/render HWND");Check(!(bool)Field(form,"faulted"),"settings panel rendering failed");form.Hide();Check(!form.Visible,"panel failed to hide");}
      if(step==4){typeof(ImGuiWindow).GetMethod("ToggleOverlay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,null);Check(Field(form,"provider")==null,"unsupported process attached");Check(!((ViewerSettings)Field(form,"settings")).Overlay,"failed attach enabled overlay");}
      if(step>80)throw new Exception("did not auto-exit after game lifetime ended");
     }catch(Exception ex){File.WriteAllText(result,"FAIL: "+ex);Environment.Exit(1);}
    };
    form.FormClosed+=(s,e)=>finished=true;timer.Start();Application.Run(form);
    Check(finished&&step>=4,"lifecycle did not finish checks");Check(child.HasExited,"viewer closed before target process exit");
   }
   AutoLaunch.Current.Dispose();AutoLaunch.Current=new AutoLaunch(Process.GetCurrentProcess());
   using(var form=new ImGuiWindow(false)){
    form.Show();var handle=form.Handle;var session=new SessionProvider();
    form.GetType().GetField("provider",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,session);
    var show=typeof(ImGuiWindow).GetMethod("ShowPanel",BindingFlags.Instance|BindingFlags.NonPublic);
    var toggle=typeof(ImGuiWindow).GetMethod("ToggleOverlay",BindingFlags.Instance|BindingFlags.NonPublic);
    for(int i=0;i<3;i++){
     show.Invoke(form,null);form.Close();
     Check(!form.Visible&&!form.IsDisposed,"X exited instead of hiding");
     Check(!session.Disposed&&Object.ReferenceEquals(Field(form,"provider"),session),"X detached the provider");
     Check(((System.Windows.Forms.Timer)Field(form,"timer")).Enabled,"X stopped capture polling");
     Check(handle==form.Handle,"X changed the hotkey window");
     var settings=(ViewerSettings)Field(form,"settings");bool before=settings.Overlay;
     toggle.Invoke(form,null);Check(settings.Overlay!=before,"overlay toggle failed after X");
     toggle.Invoke(form,null);Check(settings.Overlay==before,"second toggle failed after X");
    }
    show.Invoke(form,null);Check(form.Visible,"panel could not reopen after X");
    var tray=(NotifyIcon)Field(form,"tray");((ToolStripMenuItem)tray.ContextMenuStrip.Items[2]).PerformClick();
    Check(form.IsDisposed&&session.Disposed,"explicit tray exit did not detach and close");
   }
   AutoLaunch.Current.Dispose();AutoLaunch.Current=null;
   File.WriteAllText(result,"PASS: arguments; PID lifetime identity; unsupported-image refusal; silent hidden startup; no hook before F8; failed attach stays off; automatic exit with target; repeated X hides without detaching or stopping timer; same HWND preserved; overlay toggles after X; panel reopens; explicit tray exit detaches and closes. Test-only fake sessions; no game writes.");return 0;
  }catch(Exception ex){File.WriteAllText(result,"FAIL: "+ex);return 1;}
 }
}
