using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
internal static class VisualStyle {
    internal static readonly Color[] Colors={Color.LimeGreen,Color.OrangeRed,Color.Cyan,Color.DodgerBlue,Color.Magenta,Color.Gold,Color.Tomato,Color.MediumPurple,Color.White};
    internal static readonly string[] Names={"Push / collision","Attack","Guard / armor (provisional)","Hurt","Proximity","Throw (mock only)","Projectile attack (mock only)","Projectile hurt (mock only)","Invulnerability (mock only)"};
}
internal sealed class SceneCanvas : Control {
    internal DebugFrame Frame;internal ViewerSettings Settings;
    internal Action<Box> Selected; internal RectangleF[] Rectangles;
    internal SceneCanvas(ViewerSettings s){Settings=s;DoubleBuffered=true;Dock=DockStyle.Fill;BackColor=Color.FromArgb(14,18,25);Font=new Font("Segoe UI",9);}
    internal PointF Project(float x,float y){
        float w=ClientSize.Width,h=ClientSize.Height;
        if(Frame!=null&&!Frame.Mock&&Frame.Projection!=null){var m=Frame.Projection;float vw=Math.Min(w,h*16/9),vh=vw*9/16;float scale=(float)Settings.Scale/100;
            return new PointF((w-vw)/2+vw/2*(1+1.125f*scale*(m[12]+(x+(float)Settings.OffsetX)*m[0])),(h-vh)/2+vh/2*(1-1.125f*scale*(m[13]+y*m[5]))+((float)Settings.Floor/100-0.892708f)*vh);
        }
        float k=Math.Min(w/900f,h/400f);return new PointF(w/2+(x-450)*k,h*.86f-y*k);
    }
    internal bool Included(Box b){return Settings.Kinds[b.Kind]&&(Settings.Player==0||Settings.Player==b.Player||Settings.Player==3&&(b.Kind==6||b.Kind==7));}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;
        if(Frame==null){TextRenderer.DrawText(g,"Choose Mock / Debug or attach to Global Match",Font,ClientRectangle,Color.Silver,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
        if(Settings.Grid)using(var p=new Pen(Color.FromArgb(35,50,64))){for(int x=0;x<ClientSize.Width;x+=50)g.DrawLine(p,x,0,x,ClientSize.Height);for(int y=0;y<ClientSize.Height;y+=50)g.DrawLine(p,0,y,ClientSize.Width,y);}
        if(Settings.Ground){var ground=Project(0,0);using(var p=new Pen(Color.FromArgb(85,105,125)))g.DrawLine(p,0,ground.Y,ClientSize.Width,ground.Y);}
        var overlaps=CollisionAnalysis.Overlaps(Frame);Rectangles=new RectangleF[Frame.Boxes.Count];
        for(int i=0;i<Frame.Boxes.Count;i++){var b=Frame.Boxes[i];if(!Settings.Hitboxes||!Included(b))continue;
            var tl=Project(b.Left,b.Bottom+b.Height);var br=Project(b.Left+b.Width,b.Bottom);var r=new RectangleF(tl.X,tl.Y,br.X-tl.X,br.Y-tl.Y);Rectangles[i]=r;
            if(r.Width<=0||r.Height<=0)continue;
            using(var brush=new SolidBrush(Color.FromArgb(Settings.Alpha,VisualStyle.Colors[b.Kind])))g.FillRectangle(brush,r);
            using(var p=new Pen(overlaps.Contains(b)?Color.Yellow:VisualStyle.Colors[b.Kind],overlaps.Contains(b)?3:2)){if(b.Player==2)p.DashStyle=DashStyle.Dash;g.DrawRectangle(p,r.X,r.Y,r.Width,r.Height);}
            if(Settings.Coordinates)TextRenderer.DrawText(g,String.Format("P{0} [{1:0.0}, {2:0.0}]",b.Player,b.Left,b.Bottom),Font,new Point((int)r.X,(int)r.Y-18),VisualStyle.Colors[b.Kind]);
        }
        if(Settings.Origins&&Frame.Origins!=null)for(int i=0;i<Frame.Origins.Length;i++){if(Settings.Player!=0&&Settings.Player!=i+1)continue;var p=Project(Frame.Origins[i].X,Frame.Origins[i].Y);using(var pen=new Pen(Color.White,2)){g.DrawLine(pen,p.X-8,p.Y,p.X+8,p.Y);g.DrawLine(pen,p.X,p.Y-8,p.X,p.Y+8);}TextRenderer.DrawText(g,"P"+(i+1)+" mock origin",Font,new Point((int)p.X+10,(int)p.Y),Color.White);}
        TextRenderer.DrawText(g,(Frame.Mock?"MOCK — SIMULATED DATA":"GLOBAL MATCH — CAPTURED COLLISION DATA")+"\n"+CollisionAnalysis.Event(Frame),Font,new Point(16,14),Frame.Mock?Color.Gold:Color.Silver);
    }
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(Frame==null||Rectangles==null)return;for(int i=Frame.Boxes.Count-1;i>=0;i--)if(Rectangles[i].Contains(e.Location)){if(Selected!=null)Selected(Frame.Boxes[i]);break;}}
}
