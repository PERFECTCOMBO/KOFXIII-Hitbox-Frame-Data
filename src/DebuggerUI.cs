using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
internal sealed class DebuggerWindow : Form {
    readonly ViewerSettings settings=ViewerSettings.Load();readonly FrameHistory history=new FrameHistory();
    readonly FrameMeterControl meter;readonly TrainingModeController training;
    readonly SceneCanvas canvas;readonly GameOverlay overlay;IGameProvider provider;
    readonly Timer timer=new Timer();readonly Stopwatch clock=Stopwatch.StartNew();
    readonly TrackBar timeline=new TrackBar{Dock=DockStyle.Fill,Minimum=0,Maximum=0,TickStyle=TickStyle.None};
    readonly Label status=new Label{Dock=DockStyle.Bottom,Height=52,Padding=new Padding(12,5,5,5)};
    readonly Label technical=new Label{Dock=DockStyle.Bottom,Height=62,Padding=new Padding(12,5,5,5)};
    readonly TextBox data=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None};
    readonly SplitContainer split=new SplitContainer{Dock=DockStyle.Fill,FixedPanel=FixedPanel.Panel2};
    readonly ComboBox speed=new ComboBox{FlatStyle=FlatStyle.Flat,DropDownStyle=ComboBoxStyle.DropDownList,Width=85};
    readonly ListBox moves=new ListBox{Dock=DockStyle.Top,Height=65};
    readonly NumericUpDown jump=new NumericUpDown{Width=110,Maximum=UInt32.MaxValue};
    readonly ToolTip tips=new ToolTip();
    CheckBox overlayCheck;
    bool updating,historyPlayback;long lastPoll,lastPlayback,statsStart;int polls;double fps;long missing;Box selected;
    internal DebuggerWindow(bool mock){
        Text="KOF XIII • Collision Debugger + Frame Meter";Size=new Size(1380,960);MinimumSize=new Size(1180,840);StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(22,27,35);ForeColor=Color.WhiteSmoke;KeyPreview=true;
        training=new TrainingModeController(history);meter=new FrameMeterControl(settings);canvas=new SceneCanvas(settings);overlay=new GameOverlay(settings.Kinds){Settings=settings};
        var menu=new MenuStrip{Dock=DockStyle.Top};var options=new ToolStripMenuItem("Settings");menu.Items.Add(options);
        options.DropDownItems.Add("Save settings",null,(s,e)=>Save());options.DropDownItems.Add("Diagnostics",null,(s,e)=>PipelineDiagnostics.Show());options.DropDownItems.Add("Spreadsheet reference",null,(s,e)=>new ReferenceBrowser().Show());options.DropDownItems.Add("Reset alignment",null,(s,e)=>{settings.Floor=89.27m;settings.OffsetX=0;settings.Scale=100;Save();MessageBox.Show("Alignment reset. Reopen the debugger to refresh its numeric controls.");});
        options.DropDownItems.Add("Open instructions",null,(s,e)=>Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"README.txt")){UseShellExecute=true}));
        var toolbar=Row(46);AddButton(toolbar,"Mock / Debug",()=>Switch(true));AddButton(toolbar,"Attach Global Match",()=>Switch(false));AddButton(toolbar,"Detach",()=>Detach());
        overlayCheck=AddCheck(toolbar,"Overlay (F8)",settings.Overlay,v=>settings.Overlay=v);AddButton(toolbar,"Export history CSV",Export);
        var controls=Row(46);AddButton(controls,"Pause view",()=>{training.Pause();historyPlayback=false;});AddButton(controls,"Resume live",()=>{training.Resume();historyPlayback=false;RefreshFrame();});
        AddButton(controls,"◀ Previous",()=>Step(-1));AddButton(controls,"Next ▶",()=>Step(1));AddButton(controls,"Play history",()=>{history.Live=false;historyPlayback=!historyPlayback;});
        speed.Items.AddRange(new object[]{"0.25×","0.5×","1×"});speed.SelectedIndex=2;controls.Controls.Add(speed);controls.Controls.Add(jump);AddButton(controls,"Jump #",Jump);
        tips.SetToolTip(controls,"These controls operate the viewer history. They do not pause, rewind, or slow the attached game.");
        var filters=Row(42);var players=new ComboBox{FlatStyle=FlatStyle.Flat,DropDownStyle=ComboBoxStyle.DropDownList,Width=170};players.Items.AddRange(new object[]{"All / both players","Player 1 only","Player 2 only","Projectiles (mock only)"});players.SelectedIndex=settings.Player;players.SelectedIndexChanged+=(s,e)=>{settings.Player=players.SelectedIndex;RefreshFrame();};filters.Controls.Add(players);
        AddCheck(filters,"Grid",settings.Grid,v=>settings.Grid=v);AddCheck(filters,"Coordinates",settings.Coordinates,v=>settings.Coordinates=v);AddCheck(filters,"Origins",settings.Origins,v=>settings.Origins=v);AddCheck(filters,"Ground",settings.Ground,v=>settings.Ground=v);AddCheck(filters,"Frame data",settings.FrameData,v=>{settings.FrameData=v;split.Panel2Collapsed=!v;});AddCheck(filters,"Technical",settings.Technical,v=>{settings.Technical=v;technical.Visible=v;});
        filters.Controls.Add(new Label{Text="Canvas fill",AutoSize=true,Padding=new Padding(8,5,0,0)});var opacity=new TrackBar{Minimum=0,Maximum=180,Value=settings.Alpha,Width=140,Height=30,TickStyle=TickStyle.None};opacity.ValueChanged+=(s,e)=>{settings.Alpha=opacity.Value;RefreshFrame();};filters.Controls.Add(opacity);
        var types=Row(72);types.WrapContents=true;for(int i=0;i<9;i++){int k=i;var c=AddCheck(types,VisualStyle.Names[i],settings.Kinds[i],v=>settings.Kinds[k]=v);c.ForeColor=VisualStyle.Colors[i];}
        var align=Row(40);AddNumber(align,"Overlay opacity %",settings.OverlayOpacity,15,100,v=>settings.OverlayOpacity=(int)v);AddNumber(align,"Floor %",settings.Floor,50,110,v=>settings.Floor=v);AddNumber(align,"X offset",settings.OffsetX,-1000,1000,v=>settings.OffsetX=v);AddNumber(align,"Scale %",settings.Scale,50,200,v=>settings.Scale=v);
        tips.SetToolTip(align,"Experimental Global Match alignment correction. Defaults match the traced fighter-layer composition; live visual verification is still required.");
        var meterControls=Row(40);
        AddCheck(meterControls,"Frame meter",settings.Meter,v=>{settings.Meter=v;meter.Visible=v;});
        AddCheck(meterControls,"Hitboxes",settings.Hitboxes,v=>settings.Hitboxes=v);AddCheck(meterControls,"Advantage",settings.Advantage,v=>settings.Advantage=v);
        AddCheck(meterControls,"Input",settings.Input,v=>settings.Input=v);AddCheck(meterControls,"Frame numbers",settings.FrameNumbers,v=>settings.FrameNumbers=v);
        AddCheck(meterControls,"P1 meter",settings.MeterP1,v=>settings.MeterP1=v);AddCheck(meterControls,"P2 meter",settings.MeterP2,v=>settings.MeterP2=v);
        var meterPosition=Row(38);AddCheck(meterPosition,"Bottom dock",settings.CompactDock,v=>settings.CompactDock=v);AddNumber(meterPosition,"Meter X %",settings.MeterX,0,90,v=>settings.MeterX=v);AddNumber(meterPosition,"Meter Y %",settings.MeterY,0,90,v=>settings.MeterY=v);AddNumber(meterPosition,"Meter width %",settings.MeterScale,50,150,v=>settings.MeterScale=v);
        split.Panel1.Controls.Add(canvas);split.Panel1.Controls.Add(meter);split.Panel2.Controls.Add(data);split.Panel2.Controls.Add(moves);
        var search=new TextBox{Dock=DockStyle.Top};search.TextChanged+=(s,e)=>UpdateMoves(search.Text);tips.SetToolTip(search,"Search mock moves or available provider entries. Live character / move IDs are not mapped yet.");split.Panel2.Controls.Add(search);
        var heading=new Label{Text="FRAME DATA / click a box to inspect",Dock=DockStyle.Top,Height=28,Padding=new Padding(5)};split.Panel2.Controls.Add(heading);
        moves.SelectedIndexChanged+=(s,e)=>RefreshFrame();canvas.Selected=b=>{selected=b;RefreshFrame();};
        var trackPanel=new Panel{Dock=DockStyle.Bottom,Height=44};trackPanel.Controls.Add(timeline);timeline.ValueChanged+=(s,e)=>{if(updating)return;history.Select(timeline.Value);historyPlayback=false;RefreshFrame();};
        tips.SetToolTip(timeline,"Scrub captured records. A gap label means game frames were missed; they are not synthesized.");
        Controls.Add(split);Controls.Add(trackPanel);Controls.Add(technical);Controls.Add(status);Controls.Add(meterPosition);Controls.Add(meterControls);Controls.Add(align);Controls.Add(types);Controls.Add(filters);Controls.Add(controls);Controls.Add(toolbar);Controls.Add(menu);
        filters.Visible=types.Visible=align.Visible=meterPosition.Visible=settings.Advanced;
        var advanced=new ToolStripMenuItem("Advanced controls"){CheckOnClick=true,Checked=settings.Advanced};
        advanced.CheckedChanged+=(s,e)=>{settings.Advanced=advanced.Checked;filters.Visible=types.Visible=align.Visible=meterPosition.Visible=advanced.Checked;};options.DropDownItems.Add(advanced);
        ApplyTheme(this);data.Font=new Font("Consolas",9);technical.Visible=settings.Technical;split.Panel2Collapsed=!settings.FrameData;meter.Visible=settings.Meter;
        Shown+=(s,e)=>{split.SplitterDistance=Math.Max(550,split.Width-355);Native.RegisterHotKey(Handle,8,0x4000,0x77);if(mock)Switch(true);};
        timer.Interval=16;timer.Tick+=(s,e)=>Tick();timer.Start();
        KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Space){history.Live=!history.Live;historyPlayback=false;RefreshFrame();e.Handled=true;}if(e.KeyCode==Keys.Left)Step(-1);if(e.KeyCode==Keys.Right)Step(1);};
        FormClosing+=(s,e)=>{if(!Detach()){e.Cancel=true;return;}Save();Native.UnregisterHotKey(Handle,8);overlay.Dispose();timer.Stop();};
        status.Text="Start with Mock / Debug to explore the debugger, or attach to offline Training.";UpdateMoves("");
    }
    protected override void WndProc(ref Message m){if(m.Msg==0x312&&m.WParam.ToInt32()==8){overlayCheck.Checked=!overlayCheck.Checked;return;}base.WndProc(ref m);}
    FlowLayoutPanel Row(int h){return new FlowLayoutPanel{Dock=DockStyle.Top,Height=h,Padding=new Padding(10,5,5,2),WrapContents=false};}
    void AddButton(FlowLayoutPanel p,string name,Action a){var b=new Button{Text=name,AutoSize=true,FlatStyle=FlatStyle.Flat,Margin=new Padding(3)};b.Click+=(s,e)=>a();p.Controls.Add(b);}
    CheckBox AddCheck(FlowLayoutPanel p,string name,bool value,Action<bool> a){var c=new CheckBox{Text=name,Checked=value,AutoSize=true,Margin=new Padding(6,6,8,3)};c.CheckedChanged+=(s,e)=>{a(c.Checked);RefreshFrame();};p.Controls.Add(c);return c;}
    void AddNumber(FlowLayoutPanel p,string title,decimal value,decimal min,decimal max,Action<decimal> a){p.Controls.Add(new Label{Text=title,AutoSize=true,Padding=new Padding(5,5,0,0)});var n=new NumericUpDown{Width=85,DecimalPlaces=2,Increment=.25m,Minimum=min,Maximum=max,Value=value};n.ValueChanged+=(s,e)=>{a(n.Value);RefreshFrame();};p.Controls.Add(n);}
    void ApplyTheme(Control c){c.BackColor=c is SceneCanvas?Color.FromArgb(14,18,25):Color.FromArgb(25,31,41);if(!(c is CheckBox))c.ForeColor=Color.WhiteSmoke;foreach(Control child in c.Controls)ApplyTheme(child);}
    void Save(){try{settings.Save();}catch(Exception ex){DebugLog.Write(ex.ToString());status.Text="Could not save settings: "+ex.Message;}}
    bool Detach(){if(provider==null){overlay.Hide();return true;}try{provider.Dispose();provider=null;overlay.Hide();status.Text="Detached. Capture call restored.";return true;}catch(Exception ex){status.Text="Detach failed. Keep this viewer open until you close the game. "+ex.Message;DebugLog.Write(ex.ToString());return false;}}
    void Switch(bool mock){if(!Detach())return;PipelineDiagnostics.Begin(mock);try{provider=mock?(IGameProvider)new MockProvider():new GlobalMatchProvider();timer.Start();history.Clear();missing=0;selected=null;historyPlayback=false;UpdateMoves("");DebugLog.Write("Connected "+provider.Name);status.Text=provider.Name;RefreshFrame();}catch(Exception ex){status.Text=ex.Message;PipelineDiagnostics.Fail(ex);}}
    void UpdateMoves(string query){moves.Items.Clear();string[] entries=provider!=null&&provider.IsMock?new[]{"Mock P1 / simulated moves","Mock P2 / simulated stun"}:new[]{"P1 / move data unavailable","P2 / move data unavailable"};foreach(var entry in entries)if(entry.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)moves.Items.Add(entry);}
    void Step(int delta){historyPlayback=false;if(delta>0&&provider!=null&&provider.IsMock&&history.Selected==history.Frames.Count-1){var f=provider.Poll();history.Add(f);}history.Select(history.Selected+delta);RefreshFrame();}
    void Jump(){for(int i=0;i<history.Frames.Count;i++)if(history.Frames[i].Number==(long)jump.Value){history.Select(i);RefreshFrame();return;}status.Text="That frame number is not in captured history. No frame was invented.";}
    void Tick(){long now=clock.ElapsedMilliseconds;
        if(provider!=null&&(!provider.IsMock||history.Live)&&now-lastPoll>=16){lastPoll=now;try{bool changed=false;for(int batch=0;batch<(provider.IsMock?1:256);batch++){var f=provider.Poll();if(f==null)break;missing+=f.MissingBefore;history.Add(f);polls++;changed=true;}if(changed)RefreshFrame();}catch(Exception ex){status.Text=ex.Message;PipelineDiagnostics.Fail(ex);timer.Stop();overlay.Hide();}}
        double rate=speed.SelectedIndex==0?.25:speed.SelectedIndex==1?.5:1;
        if(historyPlayback&&now-lastPlayback>=1000/(60*rate)){lastPlayback=now;if(history.Selected<history.Frames.Count-1){history.Select(history.Selected+1);RefreshFrame();}else historyPlayback=false;}
        if(now-statsStart>=1000){fps=polls*1000.0/Math.Max(1,now-statsStart);polls=0;statsStart=now;}
        if(provider!=null&&!provider.IsMock){overlay.Follow(provider.ProcessId,settings.Overlay&&history.Live);}else overlay.Hide();
    }
    internal void PreparePreview(){split.SplitterDistance=Math.Max(550,split.Width-355);Switch(true);for(int i=0;i<15;i++)history.Add(provider.Poll());RefreshFrame();}
    void RefreshFrame(){meter.Frames=FrameTracker.Window(history,45);overlay.MeterFrames=meter.Frames;bool hasProjectile=false;foreach(var entry in meter.Frames)if(entry.Meter.Projectile!=null)hasProjectile=true;meter.Height=hasProjectile?194:148;meter.Invalidate();var f=history.Current;canvas.Frame=f;canvas.Invalidate();data.Text=FrameDataPanel.Describe(f,selected);
        updating=true;timeline.Maximum=Math.Max(0,history.Frames.Count-1);timeline.Value=Math.Max(0,history.Selected);updating=false;
        overlay.PlayerFilter=settings.Player;overlay.FillAlpha=0;overlay.Opacity=settings.OverlayOpacity/100.0;overlay.Grid=settings.Grid;overlay.Coordinates=settings.Coordinates;overlay.Ground=settings.Ground;overlay.Floor=(float)settings.Floor/100;overlay.OffsetX=(float)settings.OffsetX;overlay.BoxScale=(float)settings.Scale/100;
        if(f!=null){overlay.Boxes=f.Boxes;overlay.Projection=f.Projection;overlay.Highlights=CollisionAnalysis.Overlaps(f);overlay.Invalidate();
            int attacks=0,hurts=0;foreach(var b in f.Boxes){if(CollisionAnalysis.IsAttack(b))attacks++;if(CollisionAnalysis.IsHurt(b))hurts++;}
            status.Text=(f.Mock?"MOCK FRAME #":"CAPTURE #")+f.Number+"  •  "+(history.Live?"LIVE":"HISTORY (game continues)")+"  •  "+history.Frames.Count+" records  •  "+f.Boxes.Count+" regions\n"+CollisionAnalysis.Event(f);
            technical.Text=String.Format("Capture rate: {0:0.0}/s  |  attack: {1}  hurt: {2}  |  buffer overrun losses: {3}  |  gap before this record: {4}\nGame frame / FPS / character velocity / facing / IDs: N/A — data unavailable. Overlay client: {5} × {6}",fps,attacks,hurts,missing,f.MissingBefore,overlay.ClientSize.Width,overlay.ClientSize.Height);
        }
    }
    void Export(){if(history.Frames.Count==0)return;using(var dialog=new SaveFileDialog{Filter="CSV history|*.csv",FileName="collision-history.csv"})if(dialog.ShowDialog()==DialogResult.OK){try{var b=new StringBuilder("capture,utc,mock,gap,player,kind,left,bottom,width,height\r\n");foreach(var f in history.Frames)foreach(var r in f.Boxes)b.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1:o},{2},{3},{4},{5},{6:R},{7:R},{8:R},{9:R}\r\n",f.Number,f.Time,f.Mock,f.MissingBefore,r.Player,r.Kind,r.Left,r.Bottom,r.Width,r.Height);File.WriteAllText(dialog.FileName,b.ToString());status.Text="Exported captured region history.";}catch(Exception ex){status.Text=ex.Message;}}}
}
