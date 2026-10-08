using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
internal sealed class CompactMeter {
    readonly List<FramePhase>[] retained={new List<FramePhase>(),new List<FramePhase>()};
    readonly AttackMeasurement[] last=new AttackMeasurement[2];
    internal static Rectangle Layout(int width,int height,ViewerSettings settings){
        float vw=Math.Min(width,height*16f/9),vh=vw*9/16,ox=(width-vw)/2,oy=(height-vh)/2;
        int w=(int)(vw*.35f*(float)settings.MeterScale/100),h=(int)(82*vw/1280);
        w=Math.Min(width-8,Math.Max(250,w));h=Math.Max(68,h);
        int x=settings.CompactDock?(int)(ox+(vw-w)/2):(int)(width*(float)settings.MeterX/100);
        int y=settings.CompactDock?(int)(oy+vh-h-vh*.015f):(int)(height*(float)settings.MeterY/100);
        return new Rectangle(Math.Max(0,Math.Min(width-w,x)),Math.Max(0,Math.Min(height-h,y)),w,h);
    }
    internal static string Summary(CharacterState s,int player,bool mock){
        if(mock)return "P"+(player+1)+" MOCK   Startup "+TimingPresentation.N(s.StartupFrames)+" / Active "+TimingPresentation.N(s.ActiveFrames)+" / Total "+TimingPresentation.N(s.TotalFrames);
        var m=s.LastAttack;if(m==null)return "P"+(player+1)+"   Waiting for a complete attack";
        var adv=s.ExperimentalAdvantage;
        string gap=adv==null?"--":(adv.Gap>0?"+":"")+adv.Gap+" "+adv.Outcome;
        return "P"+(player+1)+" LAST  Startup* "+m.First+" / Active "+m.Active+" / Recovery "+TimingPresentation.N(m.Recovery)+" / Tail* "+m.Tail+" / Total "+m.Total+" / Adv* "+gap;
    }
    internal void Draw(Graphics g,Rectangle r,List<DebugFrame> frames,ViewerSettings settings){
        if(!settings.Meter||frames.Count==0)return;PipelineDiagnostics.Rendered();var current=frames[frames.Count-1];
        float scale=r.Height/82f,pad=5*scale,label=20*scale,cw=(r.Width-pad*2-label)/FrameMeterMetrics.Headers.Length;
        using(var bg=new SolidBrush(Color.FromArgb(19,23,29)))g.FillRectangle(bg,r);
        using(var header=new Font("Segoe UI",9.5f*scale,FontStyle.Regular,GraphicsUnit.Pixel))
        using(var font=new Font("Segoe UI",12*scale,FontStyle.Bold,GraphicsUnit.Pixel))
        using(var small=new Font("Segoe UI",9*scale,FontStyle.Regular,GraphicsUnit.Pixel)){
            for(int k=0;k<FrameMeterMetrics.Headers.Length;k++)TextRenderer.DrawText(g,FrameMeterMetrics.Headers[k],header,new Rectangle((int)(r.X+pad+label+k*cw),r.Y+(int)(4*scale),(int)cw,(int)(14*scale)),Color.Silver,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine|TextFormatFlags.HorizontalCenter);
            for(int p=0;p<2;p++){
                if(p==0&&!settings.MeterP1||p==1&&!settings.MeterP2)continue;
                var s=current.Meter.Players[p];
                if(s.LastAttack==null){last[p]=null;retained[p].Clear();}
                if(current.Mock||s.LastAttack!=last[p]){last[p]=s.LastAttack;retained[p].Clear();foreach(var f in frames)retained[p].Add(f.Meter.Players[p].Phase);}
                int y=r.Y+(int)((19+p*24)*scale);var color=p==0?Color.LightCyan:Color.Plum;
                TextRenderer.DrawText(g,"P"+(p+1),header,new Point(r.X+(int)pad,y),color,TextFormatFlags.NoPadding);
                var values=FrameMeterMetrics.Values(s,current.Mock);
                for(int k=0;k<FrameMeterMetrics.Headers.Length;k++)TextRenderer.DrawText(g,values[k],font,new Rectangle((int)(r.X+pad+label+k*cw),y,(int)cw,(int)(15*scale)),color,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine|TextFormatFlags.HorizontalCenter);
                if(settings.ObservedTimeline){float tw=(r.Width-pad*2-label)/45;var phases=retained[p];for(int i=0;i<45;i++)using(var fill=new SolidBrush(i<phases.Count?FrameMeterRenderer.ColorFor(phases[i]):Color.FromArgb(38,42,47)))g.FillRectangle(fill,r.X+pad+label+i*tw,y+16*scale,System.Math.Max(1,tw-1),4*scale);}
            }
            TextRenderer.DrawText(g,current.Mock?"MOCK / simulated":"LAST | * observed ticks / return gaps | — unverified",small,new Rectangle(r.X+(int)pad,r.Bottom-(int)(15*scale),r.Width-(int)(pad*2),(int)(14*scale)),Color.Silver,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
        }
    }
}

