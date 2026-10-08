using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
internal static class ImGuiEntry {
 [STAThread] static void Main(string[] args){
  bool automatic=args.Length>0&&args[0]=="--autoload";
  if(automatic){
   try{int pid;long stamp;if(!AutoLaunch.Parse(args,out pid,out stamp))return;AppPaths.UseUserData();AutoLaunch.Current=new AutoLaunch(pid,stamp);}
   catch(Exception ex){DebugLog.Write("Autoload refused: "+ex.Message);return;}
  }
  if(args.Length>0&&args[0]=="--diagnose"){ReadOnlyDiagnostic.Run();return;}
  Native.SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  bool test=args.Length>0&&args[0]=="--self-test";
  if(test){RingBufferTests.Run();FrameMeterTests.Run();NativeTimingTests.Run();ReadinessTests.Run();}
  bool fresh;using(var mutex=new Mutex(true,test?"Kof13ImGuiSelfTest":"Kof13GlobalMatchLiveCapture",out fresh)){
   if(!fresh){if(automatic){DebugLog.Write("Another viewer is already running; autoload skipped.");AutoLaunch.Current.Dispose();return;}MessageBox.Show("Close the previous KOF viewer normally before opening the ImGui debugger.");return;}
   try{Application.Run(new ImGuiWindow(test));}catch(Exception ex){DebugLog.Write(ex.ToString());if(test){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"imgui-test-results.txt"),"FAIL: "+ex);Environment.ExitCode=1;}else if(automatic)DebugLog.Write("Autoload stopped: "+ex.Message);else MessageBox.Show(ex.Message,"ImGui debugger");}finally{if(AutoLaunch.Current!=null)AutoLaunch.Current.Dispose();}
  }
 }
}
internal sealed class ImGuiWindow:Form {
 readonly bool automatic=AutoLaunch.Current!=null;bool allowShow,exitRequested;NotifyIcon tray;
 protected override bool ShowWithoutActivation {get{return automatic&&!allowShow;}}
 protected override void SetVisibleCore(bool value){
  if(value&&automatic&&!allowShow){if(!IsHandleCreated){var h=Handle;}InitializeViewer();base.SetVisibleCore(false);return;}
  base.SetVisibleCore(value);
 }
 void InitializeViewer(){
  if(ready)return;
  if(Ig.ui_init(Handle)==0)throw new InvalidOperationException("Could not initialize Dear ImGui / DirectX 11.");ready=true;
  if(!selfTest){
   if(!Native.RegisterHotKey(Handle,8,0x4000,0x77))status="F8 is in use. Close the older viewer; use the tray menu for now.";
   if(automatic){
    Native.RegisterHotKey(Handle,9,0x4000,0x78);settings.Overlay=false;
    tray=new NotifyIcon{Icon=SystemIcons.Application,Text="KOF13HITBOX - F8 overlay / F9 settings",Visible=true};
    var menu=new ContextMenuStrip();menu.Items.Add("Show / hide overlay",null,(s,e)=>ToggleOverlay());menu.Items.Add("Settings (F9)",null,(s,e)=>ShowPanel());menu.Items.Add("Exit viewer",null,(s,e)=>ExitViewer());
    tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>ShowPanel();
    status="Ready. Enter offline Training and press F8. F9 opens settings.";
    DebugLog.Write("Autoload ready for game PID "+AutoLaunch.Current.ProcessId+". Waiting for F8; no capture installed.");
   }
  }
  if(!automatic)Connect(true);timer.Interval=16;timer.Start();
 }
 // Keep ShowInTaskbar fixed: changing it recreates the HWND and would
 // invalidate both the DirectX target and the registered hotkeys.
 void ShowPanel(){allowShow=true;WindowState=FormWindowState.Normal;Show();Activate();}
 void ExitViewer(){exitRequested=true;Close();}
 void ToggleOverlay(){
  if(automatic&&(provider==null||provider.IsMock||faulted)){
   Connect(false);if(provider==null||provider.IsMock||faulted){settings.Overlay=false;if(tray!=null){tray.BalloonTipTitle="KOF13HITBOX could not attach";tray.BalloonTipText=status;tray.ShowBalloonTip(5000);}return;}
   settings.Overlay=true;DebugLog.Write("F8: attached and overlay enabled.");
  }else{settings.Overlay=!settings.Overlay;DebugLog.Write("F8: overlay "+settings.Overlay);}
 }
 readonly ViewerSettings settings=ViewerSettings.Load();readonly FrameHistory history=new FrameHistory();
 readonly GameOverlay overlay;readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();readonly Stopwatch clock=Stopwatch.StartNew();
 IGameProvider provider;bool ready,playback,faulted;readonly bool selfTest;int ticks,rate=2,selectedBox=-1,jump;bool initialGrid;string currentTab;long lastPoll,lastPlay,missing;string status="Mock simulation ready. Attach Global Match for live data.";
 internal ImGuiWindow(bool test){selfTest=test;Text="KOF13HITBOXv0.1 - Autoload";ClientSize=new Size(1340,920);MinimumSize=new Size(960,720);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(12,16,22);SetStyle(ControlStyles.Opaque,true);
  overlay=new GameOverlay(settings.Kinds){Settings=settings};
  if(automatic)ShowInTaskbar=false;Shown+=(s,e)=>InitializeViewer();
  timer.Tick+=(s,e)=>Tick();FormClosing+=(s,e)=>{if(automatic&&!exitRequested&&e.CloseReason==CloseReason.UserClosing&&AutoLaunch.Current.Alive){e.Cancel=true;Hide();Save();return;}timer.Stop();if(!Detach()){e.Cancel=true;timer.Start();return;}if(!selfTest)Save();Native.UnregisterHotKey(Handle,8);Native.UnregisterHotKey(Handle,9);if(tray!=null){tray.Visible=false;var menu=tray.ContextMenuStrip;tray.Dispose();menu.Dispose();}overlay.Dispose();if(ready){Ig.ui_shutdown();ready=false;}};
 }
 protected override void WndProc(ref Message m){if(m.Msg==0x312){if(automatic&&!AutoLaunch.Current.Foreground&&!ContainsFocus)return;if(m.WParam.ToInt32()==8){ToggleOverlay();return;}if(automatic&&m.WParam.ToInt32()==9){ShowPanel();return;}}if(ready&&Ig.ui_message(Handle,(uint)m.Msg,m.WParam,m.LParam)!=IntPtr.Zero){m.Result=(IntPtr)1;return;}base.WndProc(ref m);}
 protected override void OnPaintBackground(PaintEventArgs e){} protected override void OnPaint(PaintEventArgs e){}
 bool Detach(){if(provider==null)return true;try{provider.Dispose();provider=null;overlay.Hide();status="Detached. Original game capture call restored.";return true;}catch(Exception ex){status="Detach failed: keep this debugger open until you close the game. "+ex.Message;DebugLog.Write(ex.ToString());return false;}}
 void Connect(bool mock){if(!Detach())return;PipelineDiagnostics.Begin(mock);try{provider=mock?(IGameProvider)new MockProvider():new GlobalMatchProvider();history.Clear();missing=0;selectedBox=-1;playback=false;faulted=false;status=provider.Name;}catch(Exception ex){status=ex.Message;PipelineDiagnostics.Fail(ex);}}
 void Save(){try{settings.Save();status="Settings saved.";}catch(Exception ex){status="Settings could not be saved: "+ex.Message;}}
 void Step(int d){playback=false;if(d>0&&provider!=null&&provider.IsMock&&history.Selected==history.Frames.Count-1){history.Add(provider.Poll());}history.Select(history.Selected+d);selectedBox=-1;}
 void Tick(){if(!ready)return;try{
  if(automatic&&!AutoLaunch.Current.Alive){ExitViewer();return;}
  long now=clock.ElapsedMilliseconds;
  if(!faulted&&provider!=null&&(!provider.IsMock||history.Live)&&now-lastPoll>=16){lastPoll=now;for(int n=0;n<(provider.IsMock?1:256);n++){var f=provider.Poll();if(f==null)break;history.Add(f);missing+=f.MissingBefore;}}
  if(playback&&now-lastPlay>=1000/(60*(rate==0?.25:rate==1?.5:1))){lastPlay=now;if(history.Selected<history.Frames.Count-1)history.Select(history.Selected+1);else playback=false;}
  UpdateOverlay();
  if(Visible&&WindowState!=FormWindowState.Minimized){if(Ig.ui_begin(ClientSize.Width,ClientSize.Height)==0)throw new InvalidOperationException("DirectX render target unavailable.");Draw();if(Ig.ui_end()==0)throw new InvalidOperationException("DirectX presentation failed. Close and reopen the debugger.");PipelineDiagnostics.Rendered();}
  if(selfTest)TestTick();
 }catch(Exception ex){status=ex.Message;faulted=true;overlay.Hide();PipelineDiagnostics.Fail(ex);if(selfTest){try{Screenshot("imgui-test-failure.png");}catch{}File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"imgui-test-results.txt"),"FAIL: "+ex);Environment.ExitCode=1;Close();}}}
 void UpdateOverlay(){if(provider==null||provider.IsMock||faulted){overlay.Hide();return;}var f=history.Current;overlay.MeterFrames=FrameTracker.Window(history,45);overlay.PlayerFilter=settings.Player;overlay.FillAlpha=0;overlay.Opacity=settings.OverlayOpacity/100.0;overlay.Grid=settings.Grid;overlay.Coordinates=settings.Coordinates;overlay.Ground=settings.Ground;overlay.Floor=(float)settings.Floor/100;overlay.OffsetX=(float)settings.OffsetX;overlay.BoxScale=(float)settings.Scale/100;
  if(f!=null){overlay.Boxes=f.Boxes;overlay.Projection=f.Projection;overlay.Highlights=CollisionAnalysis.Overlaps(f);overlay.Invalidate();}overlay.Follow(provider.ProcessId,settings.Overlay&&history.Live);
 }
 void Button(string label,Action action,bool same){if(same)Ig.ui_same();if(Ig.ui_button(label)!=0)action();}
 void Draw(){
  Ig.ui_text("KOF13HITBOXv0.1  /  HITBOX + FRAME METER");Ig.ui_same();Ig.ui_text(provider==null?"DETACHED":provider.IsMock?"MOCK - SIMULATED DATA":"GLOBAL MATCH - LIVE CAPTURE");
  if(automatic)Button("Return to game",()=>Hide(),false);
  Button("Attach Global Match",()=>Connect(false),false);Button("Mock / Debug",()=>Connect(true),true);Button("Detach",()=>Detach(),true);Button("Export boxes",ExportBoxes,true);Button("Export timings",Export,true);Button("Reference",()=>new ReferenceBrowser().Show(),true);Button("Special tests",()=>new SpecialQueueForm().Show(),true);Button("Roster tests",()=>new RosterValidation().Show(),true);Button("Save settings",Save,true);Ig.ui_same();Ig.Check("Overlay (F8)",ref settings.Overlay);
  Ig.ui_sep();
  Button(history.Live?"Pause view":"Resume live",()=>{history.Live=!history.Live;if(history.Live)history.Selected=history.Frames.Count-1;playback=false;},false);Button("< Previous",()=>Step(-1),true);Button("Next >",()=>Step(1),true);Button(playback?"Stop history":"Play history",()=>{history.Live=false;playback=!playback;},true);Ig.ui_same();Ig.ui_text("History controls affect the viewer; the game keeps running.");
  int selected=Math.Max(0,history.Selected);if(Ig.ui_int("History record",ref selected,0,Math.Max(0,history.Frames.Count-1))!=0){history.Select(selected);playback=false;selectedBox=-1;}
  var f=history.Current;Ig.ui_text((f==null?"No capture":(f.Mock?"Mock #":"Capture #")+f.Number)+"   |   "+history.Frames.Count+" retained   |   "+(history.Live?"LIVE":"HISTORY")+"   |   Buffer losses: "+missing);
  if(Ig.ui_tabs()!=0){
   if(Ig.ui_tab("Live view")!=0){currentTab="Live view";DrawScene(f,Math.Max(150,Ig.ui_height()-(settings.Meter?190:75)));if(settings.Meter)DrawMeter();Ig.ui_endtab();}
   if(Ig.ui_tab("Filters & display")!=0){currentTab="Filters & display";DrawFilters();Ig.ui_endtab();}
   if(Ig.ui_tab("Alignment & overlay")!=0){DrawAlignment();Ig.ui_endtab();}
   if(Ig.ui_tab("Frame inspector")!=0){DrawInspector(f);Ig.ui_endtab();}
   if(Ig.ui_tab("Diagnostics")!=0){Ig.ui_wrapped(PipelineDiagnostics.Report());Button("Save diagnostic report",()=>{status=PipelineDiagnostics.Export();},false);Ig.ui_int("History playback: 0=0.25x, 1=0.5x, 2=1x",ref rate,0,2);Ig.ui_endtab();}
   Ig.ui_endtabs();
  }
  Ig.ui_sep();Ig.ui_wrapped(status);
 }
 void DrawFilters(){Ig.Check("Hitboxes",ref settings.Hitboxes);Ig.ui_same();Ig.Check("Frame meter",ref settings.Meter);Ig.Check("Observed timeline",ref settings.ObservedTimeline);Ig.ui_same();Ig.Check("Coordinates",ref settings.Coordinates);Ig.ui_same();Ig.Check("Ground",ref settings.Ground);Ig.Check("Grid",ref settings.Grid);Ig.ui_same();Ig.Check("Origins (mock)",ref settings.Origins);Ig.ui_same();Ig.Check("Show advantage",ref settings.Advantage);Ig.Check("P1 meter",ref settings.MeterP1);Ig.ui_same();Ig.Check("P2 meter",ref settings.MeterP2);Ig.ui_same();Ig.Check("Frame numbers (mock)",ref settings.FrameNumbers);Ig.Check("Input field",ref settings.Input);Ig.ui_int("Player: 0=both, 1=P1, 2=P2, 3=projectiles (mock)",ref settings.Player,0,3);Ig.ui_int("Canvas fill opacity",ref settings.Alpha,0,180);Ig.ui_sep();for(int i=0;i<settings.Kinds.Length;i++)Ig.Check(VisualStyle.Names[i],ref settings.Kinds[i]);}
 void DrawAlignment(){Ig.Check("Dock compact meter between power bars",ref settings.CompactDock);Ig.ui_wrapped("These settings also apply to the in-game overlay. F8 toggles it. Use windowed or borderless game mode.");Ig.ui_int("Overlay opacity (%)",ref settings.OverlayOpacity,15,100);Ig.Decimal("Floor (%)",ref settings.Floor,50,110);Ig.Decimal("Horizontal offset",ref settings.OffsetX,-1000,1000);Ig.Decimal("Box scale (%)",ref settings.Scale,50,200);Ig.Decimal("Meter X (%)",ref settings.MeterX,0,90);Ig.Decimal("Meter Y (%)",ref settings.MeterY,0,90);Ig.Decimal("Meter width (%)",ref settings.MeterScale,50,150);Button("Reset alignment",()=>{settings.Floor=89.27m;settings.OffsetX=0;settings.Scale=100;},false);}
 void DrawInspector(DebugFrame f){if(f==null){Ig.ui_text("No captured frame.");return;}Ig.ui_input("Capture / mock number",ref jump);Button("Jump to capture",()=>{for(int i=0;i<history.Frames.Count;i++)if(history.Frames[i].Number==jump){history.Select(i);playback=false;selectedBox=-1;return;}status="That capture is not in retained history.";},false);Ig.ui_text("Click a region in Live view, or choose one below.");if(f.Boxes.Count>0){selectedBox=Math.Max(0,Math.Min(f.Boxes.Count-1,selectedBox));Ig.ui_int("Region index",ref selectedBox,0,f.Boxes.Count-1);}Ig.ui_wrapped(FrameDataPanel.Describe(f,selectedBox>=0&&selectedBox<f.Boxes.Count?f.Boxes[selectedBox]:null));}
 static uint Rgba(Color c,int alpha){return (uint)(c.R|(c.G<<8)|(c.B<<16)|(alpha<<24));}
 static uint White=0xffeeeeee;
 PointF Project(DebugFrame f,float x,float y,float w,float h){if(!f.Mock&&f.Projection!=null){var m=f.Projection;float vw=Math.Min(w,h*16/9),vh=vw*9/16,scale=(float)settings.Scale/100;return new PointF((w-vw)/2+vw/2*(1+1.125f*scale*(m[12]+(x+(float)settings.OffsetX)*m[0])),(h-vh)/2+vh/2*(1-1.125f*scale*(m[13]+y*m[5]))+((float)settings.Floor/100-.892708f)*vh);}float k=Math.Min(w/900,h/400);return new PointF(w/2+(x-450)*k,h*.86f-y*k);}
 bool Included(Box b){return settings.Kinds[b.Kind]&&(settings.Player==0||settings.Player==b.Player||settings.Player==3&&(b.Kind==6||b.Kind==7));}
 void DrawScene(DebugFrame f,float h){float x,y,w;Ig.ui_canvas(h,out x,out y,out w);float mx,my;bool click=Ig.ui_click(out mx,out my)!=0;Ig.ui_clip(x,y,w,h);Ig.ui_rect(x,y,w,h,0xff1c1510,1,1);
  if(settings.Grid){for(float gx=x;gx<x+w;gx+=50)Ig.ui_line(gx,y,gx,y+h,0xff352c23);for(float gy=y;gy<y+h;gy+=50)Ig.ui_line(x,gy,x+w,gy,0xff352c23);}
  if(f!=null){Ig.ui_label(x+12,y+10,f.Mock?0xff5bdbff:White,f.Mock?"MOCK / SIMULATED COLLISION DATA":"GLOBAL MATCH / CAPTURED COLLISION DATA");
   if(settings.Ground){var p=Project(f,0,0,w,h);Ig.ui_line(x,y+p.Y,x+w,y+p.Y,0xff78654f);}
   var overlap=CollisionAnalysis.Overlaps(f);for(int i=0;i<f.Boxes.Count;i++){var b=f.Boxes[i];if(!settings.Hitboxes||!Included(b))continue;var a=Project(f,b.Left,b.Bottom+b.Height,w,h);var z=Project(f,b.Left+b.Width,b.Bottom,w,h);float rw=z.X-a.X,rh=z.Y-a.Y;if(rw<=0||rh<=0)continue;uint color=Rgba(VisualStyle.Colors[b.Kind],255);Ig.ui_rect(x+a.X,y+a.Y,rw,rh,Rgba(VisualStyle.Colors[b.Kind],settings.Alpha),1,1);Ig.ui_rect(x+a.X,y+a.Y,rw,rh,overlap.Contains(b)?0xff00ffff:color,0,i==selectedBox?3:2);
    if(settings.Coordinates)Ig.ui_label(x+a.X,y+a.Y-20,color,String.Format("P{0} [{1:0.0}, {2:0.0}]",b.Player,b.Left,b.Bottom));if(click&&mx>=x+a.X&&mx<=x+z.X&&my>=y+a.Y&&my<=y+z.Y)selectedBox=i;
   }
   if(settings.Origins&&f.Origins!=null)for(int i=0;i<f.Origins.Length;i++){if(settings.Player!=0&&settings.Player!=i+1)continue;var p=Project(f,f.Origins[i].X,f.Origins[i].Y,w,h);Ig.ui_line(x+p.X-7,y+p.Y,x+p.X+7,y+p.Y,White);Ig.ui_line(x+p.X,y+p.Y-7,x+p.X,y+p.Y+7,White);}
  }Ig.ui_unclip();
 }
 static string Num(int? n){return n.HasValue?n.Value.ToString():"--";}
 void DrawMeter(){Ig.ui_sep();var frames=FrameTracker.Window(history,45);if(frames.Count==0){Ig.ui_text("Frame meter: awaiting data");return;}var current=frames[frames.Count-1];Ig.ui_text(current.Mock?"FRAME METER / MOCK":"Startup* | Active* | Recovery | OnHit* | OnGuard*");
  for(int p=0;p<3;p++){if(p==0&&!settings.MeterP1||p==1&&!settings.MeterP2||p==2&&current.Meter.Projectile==null)continue;var c=p<2?current.Meter.Players[p]:current.Meter.Projectile;string name=p<2?"P"+(p+1):"Projectile";
   string detail=name+" | "+String.Join(" | ",FrameMeterMetrics.Values(c,current.Mock));Ig.ui_text(detail);if(!current.Mock)Ig.ui_text(FrameMeterMetrics.RecoveryStatus(c));
   if(!settings.ObservedTimeline)continue;float x,y,w;Ig.ui_canvas(19,out x,out y,out w);float cell=w/45;Ig.ui_clip(x,y,w,19);for(int j=0;j<45;j++){var a=j<frames.Count?(p<2?frames[j].Meter.Players[p]:frames[j].Meter.Projectile):null;var phase=a!=null?a.Phase:FramePhase.Unknown;Ig.ui_rect(x+j*cell,y,cell-1,17,Rgba(FrameMeterRenderer.ColorFor(phase),255),1,1);if(j==frames.Count-1)Ig.ui_rect(x+j*cell,y,cell-1,17,White,0,2);if(j<frames.Count&&frames[j].MissingBefore>0)Ig.ui_line(x+j*cell,y,x+(j+1)*cell-2,y+17,0xff00aaff);string label=a==null?"":!current.Mock&&phase==FramePhase.ObservedState?Num(a.NativeStateId):FrameMeterRenderer.Code(phase);if(current.Mock&&settings.FrameNumbers&&j<frames.Count)label=Num(frames[j].Meter.InteractionFrame);Ig.ui_label(x+j*cell+3,y+3,phase==FramePhase.Hitstop?0xff111111:White,label);}Ig.ui_unclip();
  }
  Ig.ui_text(current.Mock?"S startup / A active / R recovery / HS hitstun / BS blockstun / H hitstop / N neutral":"* Observed startup = ticks BEFORE first attack; advantage = return gap. Recovery includes tested return/landing endpoints; unsupported transitions show --.");
 }
 void ExportBoxes(){if(history.Frames.Count==0){status="No captures to export.";return;}using(var dialog=new SaveFileDialog{Filter="CSV|*.csv",FileName="collision-history.csv"})if(dialog.ShowDialog()==DialogResult.OK){var b=new StringBuilder("capture,utc,mock,gap,player,kind,left,bottom,width,height\r\n");foreach(var f in history.Frames)foreach(var r in f.Boxes)b.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1:o},{2},{3},{4},{5},{6:R},{7:R},{8:R},{9:R}\r\n",f.Number,f.Time,f.Mock,f.MissingBefore,r.Player,r.Kind,r.Left,r.Bottom,r.Width,r.Height);File.WriteAllText(dialog.FileName,b.ToString());status="Exported captured collision regions.";}}
 void Export(){if(history.Frames.Count==0){status="No captured records to export.";return;}using(var dialog=new SaveFileDialog{Filter="CSV|*.csv",FileName="kof13-history.csv"})if(dialog.ShowDialog()==DialogResult.OK){var s=new StringBuilder("capture,mock,missing_before,player,state,action,timer,phase,first_active,active_ticks,tail_ticks,total_ticks,actor,secondary,neutral,hitstop,truncated,in_progress\r\n");foreach(var f in history.Frames)for(int p=0;p<2;p++){var c=f.Meter.Players[p];s.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}\r\n",f.Number,f.Mock,f.MissingBefore,p+1,c.NativeStateId,c.NativeActionId,c.NativeActionTime,c.Phase,c.ObservedFirstActive,c.ObservedActiveTicks,c.ObservedTailTicks,c.ObservedTotalTicks,c.ActorIdentity,c.NativeSecondaryStateId,c.NativeNeutral,c.NativeHitstop,f.Truncated,c.TimingInProgress);}File.WriteAllText(dialog.FileName,s.ToString());status="Exported "+history.Frames.Count+" captured records.";}}
 void TestClick(string label,int down){if(Ig.ui_test_click(label,down)==0)throw new Exception("Missing ImGui test item: "+label);}
 void Screenshot(string name){int w=ClientSize.Width,h=ClientSize.Height;var pixels=new byte[w*h*4];if(Ig.ui_pixels(pixels,pixels.Length)==0)throw new Exception("DirectX readback failed");using(var bmp=new Bitmap(w,h,PixelFormat.Format32bppArgb)){var data=bmp.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);for(int y=0;y<h;y++)Marshal.Copy(pixels,y*w*4,IntPtr.Add(data.Scan0,y*data.Stride),w*4);bmp.UnlockBits(data);bmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,name));}}
 void TestTick(){ticks++;
  if(ticks==3)TestClick("Pause view",1);
  if(ticks==4)TestClick("Pause view",0);
  if(ticks==5){if(history.Live)throw new Exception("ImGui pause button input failed");while(history.Frames.Count<15)history.Add(provider.Poll());history.Select(14);Step(-1);if(history.Selected!=13)throw new Exception("History previous failed");Step(1);if(history.Selected!=14)throw new Exception("History next failed");ClientSize=new Size(1180,840);}
  if(ticks==7){ClientSize=new Size(1340,920);TestClick("Filters & display",0);}
  if(ticks==9)TestClick("Filters & display",1);
  if(ticks==10)TestClick("Filters & display",0);
  if(ticks==12){if(currentTab!="Filters & display")throw new Exception("ImGui tab input failed");initialGrid=settings.Grid;TestClick("Grid",0);}
  if(ticks==14)TestClick("Grid",1);
  if(ticks==15)TestClick("Grid",0);
  if(ticks==17){if(settings.Grid==initialGrid)throw new Exception("ImGui checkbox input failed");settings.Grid=initialGrid;Screenshot("imgui-filters-preview.png");TestClick("Live view",0);}
  if(ticks==19)TestClick("Live view",1);
  if(ticks==20)TestClick("Live view",0);
  if(ticks==23){if(currentTab!="Live view")throw new Exception("Return to live view failed");Screenshot("imgui-preview.png");File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"imgui-test-results.txt"),"PASS: ring / meter / native timing regression tests; native Dear ImGui + DX11 rendering; GPU readback; two window resizes; actual ImGui input events for pause button, tab selection and checkbox toggle; history previous/next; clean shutdown. No live attachment attempted in self-test.\r\n");Close();}
 }
}

