using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

internal sealed class GameOverlay : Form
{
    internal ViewerSettings Settings;
    readonly CompactMeter compact=new CompactMeter();
    void DrawCompact(Graphics g){if(Settings!=null)compact.Draw(g,CompactMeter.Layout(ClientSize.Width,ClientSize.Height,Settings),MeterFrames,Settings);}
    internal List<DebugFrame> MeterFrames=new List<DebugFrame>();
    internal List<Box> Boxes=new List<Box>();
    internal float[] Projection;
    internal bool[] Kinds;
    internal int PlayerFilter;
    internal System.Collections.Generic.List<Box> Highlights=new System.Collections.Generic.List<Box>();
    internal int FillAlpha=0;
    internal bool Grid,Coordinates,Ground;
    internal float OffsetX=0,Floor=0.892708f,BoxScale=1;
    internal GameOverlay(bool[] kinds) {
        Kinds=kinds;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
        BackColor=Color.Black;TransparencyKey=Color.Black;DoubleBuffered=true;
    }
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80000|0x20|0x08000000|0x80;return p;}}
    protected override void WndProc(ref Message m){if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}base.WndProc(ref m);}
    internal void Follow(int pid,bool enabled) {
        IntPtr window=IntPtr.Zero;
        Native.EnumWindows((w,p)=>{uint id;Native.GetWindowThreadProcessId(w,out id);Native.Rect r;
            if(id==pid&&Native.IsWindowVisible(w)&&!Native.IsIconic(w)&&Native.GetClientRect(w,out r)&&r.Right>300&&r.Bottom>200){window=w;return false;}return true;},IntPtr.Zero);
        uint foreground;Native.GetWindowThreadProcessId(Native.GetForegroundWindow(),out foreground);
        PipelineDiagnostics.Overlay=!enabled?"Disabled / history selected":window==IntPtr.Zero?"No visible game window":foreground!=pid?"Hidden: game is not foreground":"Following game client";if(!enabled||window==IntPtr.Zero||foreground!=pid){Hide();return;}
        Native.Rect rect;Point origin=new Point();
        if(!Native.GetClientRect(window,out rect)||!Native.ClientToScreen(window,ref origin)){Hide();return;}
        Rectangle bounds=new Rectangle(origin.X,origin.Y,rect.Right,rect.Bottom);
        if(Bounds!=bounds)Bounds=bounds;
        if(!Visible)Show();Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        float vw=Math.Min(ClientSize.Width,ClientSize.Height*16f/9),vh=vw*9/16;
        float ox=(ClientSize.Width-vw)/2,oy=(ClientSize.Height-vh)/2;
        if(Projection==null||(Settings!=null&&!Settings.Hitboxes)){DrawCompact(e.Graphics);return;}
        
        if(Ground){float y=oy+vh/2*(1-1.125f*BoxScale*Projection[13])+(Floor-0.892708f)*vh;using(var p=new Pen(Color.SlateGray))e.Graphics.DrawLine(p,ox,y,ox+vw,y);}
        if(Grid)using(var p=new Pen(Color.FromArgb(70,90,110))){for(float x=ox;x<ox+vw;x+=vw/16)e.Graphics.DrawLine(p,x,oy,x,oy+vh);for(float y=oy;y<oy+vh;y+=vh/9)e.Graphics.DrawLine(p,ox,y,ox+vw,y);}
        foreach(int kind in new[]{3,0,2,4,1})foreach(var b in Boxes) {
            if(b.Kind!=kind||!Kinds[kind]||(PlayerFilter!=0&&b.Player!=PlayerFilter))continue;
            // RTCopyNode scales the complete projected fighter layer about its center.
            float x=ox+vw/2*(1+1.125f*BoxScale*(Projection[12]+(b.Left+OffsetX)*Projection[0]));
            float y=oy+vh/2*(1-1.125f*BoxScale*(Projection[13]+(b.Bottom+b.Height)*Projection[5]))+(Floor-0.892708f)*vh;
            float width=b.Width*1.125f*Projection[0]*vw/2*BoxScale;
            float height=b.Height*1.125f*Projection[5]*vh/2*BoxScale;
            if(FillAlpha>0)using(var brush=new SolidBrush(Color.FromArgb(FillAlpha,VisualStyle.Colors[kind])))e.Graphics.FillRectangle(brush,x,y,width,height);
            if(Coordinates)TextRenderer.DrawText(e.Graphics,String.Format("P{0} [{1:0.0}, {2:0.0}]",b.Player,b.Left,b.Bottom),Font,new Point((int)x,(int)y),VisualStyle.Colors[kind],Color.Black);
            using(var pen=new Pen(Highlights.Contains(b)?Color.Yellow:VisualStyle.Colors[kind],2.5f)) {
                if(b.Player==2)pen.DashStyle=System.Drawing.Drawing2D.DashStyle.Dash;
                e.Graphics.DrawRectangle(pen,x,y,width,height);
            }
        }
        DrawCompact(e.Graphics);
    }
}

