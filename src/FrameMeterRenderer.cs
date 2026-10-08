using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
internal static class FrameMeterRenderer {
    static string Tick(int? n){return n.HasValue?n.Value.ToString():"--";}
    internal static Color ColorFor(FramePhase p){switch(p){case FramePhase.ObservedState:return Color.FromArgb(55,100,115);case FramePhase.ObservedAttack:return Color.OrangeRed;case FramePhase.Startup:return Color.FromArgb(60,180,205);case FramePhase.Active:return Color.FromArgb(245,80,75);case FramePhase.Recovery:return Color.FromArgb(70,125,225);case FramePhase.Hitstun:return Color.FromArgb(230,190,65);case FramePhase.Blockstun:return Color.FromArgb(155,110,225);case FramePhase.Hitstop:return Color.FromArgb(225,225,235);case FramePhase.Throw:return Color.Orange;case FramePhase.ProjectileTravel:return Color.LimeGreen;case FramePhase.Knockdown:return Color.SandyBrown;case FramePhase.Wakeup:return Color.LightPink;case FramePhase.Neutral:return Color.FromArgb(50,65,75);default:return Color.FromArgb(45,45,53);}}
    internal static string Code(FramePhase p){switch(p){case FramePhase.ObservedState:return "?";case FramePhase.ObservedAttack:return "A*";case FramePhase.Startup:return "S";case FramePhase.Active:return "A";case FramePhase.Recovery:return "R";case FramePhase.Hitstun:return "HS";case FramePhase.Blockstun:return "BS";case FramePhase.Hitstop:return "H";case FramePhase.Throw:return "T";case FramePhase.ProjectileTravel:return "P";case FramePhase.Neutral:return "N";case FramePhase.Knockdown:return "KD";case FramePhase.Wakeup:return "W";default:return "--";}}
    internal static void Draw(Graphics g,Rectangle bounds,List<DebugFrame> frames,ViewerSettings settings){
        if(!settings.Meter||bounds.Width<250)return;PipelineDiagnostics.Rendered();
        using(var fill=new SolidBrush(Color.FromArgb(22,26,35)))g.FillRectangle(fill,bounds);
        using(var border=new Pen(Color.FromArgb(65,78,96)))g.DrawRectangle(border,bounds.X,bounds.Y,bounds.Width-1,bounds.Height-1);
        using(var title=new Font("Segoe UI",10,FontStyle.Bold))using(var small=new Font("Segoe UI",8)){
            var f=frames.Count>0?frames[frames.Count-1]:null;var state=f!=null?f.Meter:GameStateReader.Unavailable();
            string caption=f!=null&&f.Mock?"MOCK FRAME METER • SIMULATED VALUES":"KOF XIII • NATIVE ACTION TIMER • R2";
            if(f!=null&&f.MissingBefore>0)caption+=" • BUFFER OVERRUN";
            if(f!=null&&f.Truncated)caption+=" • REGION LIMIT";
            TextRenderer.DrawText(g,caption,title,new Point(bounds.X+12,bounds.Y+8),f!=null&&f.Mock?Color.Gold:Color.WhiteSmoke);
            bool projectile=false;foreach(var entry in frames)if(entry.Meter.Projectile!=null)projectile=true;
            int rows=(settings.MeterP1?1:0)+(settings.MeterP2?1:0)+(projectile?1:0);int top=bounds.Y+28;
            int count=Math.Max(13,Math.Min(45,frames.Count));float area=Math.Max(100,bounds.Width-110);float cell=Math.Min(27,area/count);
            float start=bounds.X+85;int startIndex=Math.Max(0,frames.Count-count);
            int row=0;
            for(int player=0;player<(projectile?3:2);player++){
                if(player==0&&!settings.MeterP1||player==1&&!settings.MeterP2)continue;
                var c=player<2?state.Players[player]:state.Projectile??new CharacterState();int y=top+row*42;
                string rowName=player<2?"P"+(player+1):"PR";
                string detail=TimingPresentation.Summary(c,player,f!=null&&f.Mock);
                TextRenderer.DrawText(g,detail,small,new Rectangle(bounds.X+12,y,bounds.Width-20,20),Color.WhiteSmoke,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
                TextRenderer.DrawText(g,rowName,title,new Point(bounds.X+18,y+19),player==0?Color.Cyan:Color.MediumPurple);
                for(int i=0;i<count;i++){
                    int index=startIndex+i;bool available=index<frames.Count;var entry=available?frames[index]:null;var actor=entry!=null?(player<2?entry.Meter.Players[player]:entry.Meter.Projectile):null;
                    var phase=actor!=null?actor.Phase:FramePhase.Unknown;
                    var rect=new RectangleF(start+i*cell,y+19,Math.Max(3,cell-1),18);
                    using(var b=new SolidBrush(available?ColorFor(phase):Color.FromArgb(28,32,40)))g.FillRectangle(b,rect);
                    using(var p=new Pen(index==frames.Count-1?Color.White:Color.FromArgb(65,75,88),index==frames.Count-1?2:1))g.DrawRectangle(p,rect.X,rect.Y,rect.Width,rect.Height);
                    if(available&&entry.MissingBefore>0)using(var p=new Pen(Color.Orange,2))g.DrawLine(p,rect.X,rect.Y,rect.Right,rect.Bottom);
                    string label=available?(settings.FrameNumbers&&entry.Meter.InteractionFrame.HasValue?entry.Meter.InteractionFrame.Value.ToString():Code(phase)):"";
                    if(available&&!entry.Mock&&actor!=null&&phase==FramePhase.ObservedState&&actor.NativeStateId.HasValue)label=actor.NativeStateId.Value.ToString();
                    if(cell>=16)TextRenderer.DrawText(g,label,small,Rectangle.Round(rect),phase==FramePhase.Hitstop?Color.Black:Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
                }
                row++;
            }
            int legendY=top+rows*42+1;
            TextRenderer.DrawText(g,(f!=null&&!f.Mock?"N native neutral / A* attack region / H hitstop / numbers: state IDs / orange slash: buffer overrun":"S startup   A active   R recovery   HS hitstun   BS blockstun   H hitstop   N actionable   -- unavailable"),small,new Point(bounds.X+12,legendY),Color.Silver);
            if(state.Projectile!=null)TextRenderer.DrawText(g,"Projectile: "+state.Projectile.Phase+" • frame "+(state.Projectile.CurrentFrame.HasValue?state.Projectile.CurrentFrame.Value.ToString():"--"),small,new Point(bounds.X+12,legendY+18),Color.LimeGreen);
            else if(f!=null&&!f.Mock)TextRenderer.DrawText(g,"Native ticks only; tail is NOT verified recovery. On hit / on block: -- (unmapped clock and actionability).",small,new Point(bounds.X+12,legendY+18),Color.Gray);
        }
    }
}
internal sealed class FrameMeterControl : Control {
    internal readonly ViewerSettings Settings;internal List<DebugFrame> Frames=new List<DebugFrame>();
    internal FrameMeterControl(ViewerSettings settings){Settings=settings;DoubleBuffered=true;Dock=DockStyle.Bottom;Height=242;BackColor=Color.FromArgb(14,18,25);}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);FrameMeterRenderer.Draw(e.Graphics,new Rectangle(8,4,Math.Max(0,Width-16),Math.Max(0,Height-8)),Frames,Settings);}
}
