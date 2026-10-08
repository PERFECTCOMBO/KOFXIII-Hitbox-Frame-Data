using System;
using System.Drawing;
using System.Windows.Forms;
internal static class FrameDataPanel {
    internal const string NA="N/A — data unavailable";
    internal static string Describe(DebugFrame f,Box selected){if(f==null)return "No captured frame.";
        string text=(f.Mock?"MOCK VALUES / NOT KOF FRAME DATA":"GLOBAL MATCH / VERIFIED FIELDS ONLY")+"\r\n\r\nCharacter: "+f.Character+"\r\nMove: "+f.Move+"\r\nState: "+f.State+"\r\nAnimation: "+NA+"\r\nAnimation frame: "+Value(f.AnimationFrame)+"\r\nStartup: "+Value(f.Startup)+"\r\nActive: "+Value(f.Active)+"\r\nRecovery: "+Value(f.Recovery)+"\r\nDuration: "+(f.Mock?"13 (mock move; hitstop shown separately)":NA)+"\r\nDamage: "+Value(f.Damage);
        foreach(var name in new[]{"Hitstun","Blockstun","Knockback / pushback","Invulnerability frames","Armor frames","Cancel windows","Facing","Animation / state ID","Attack ID","Character velocity","Attack origin"})text+="\r\n"+name+": "+NA;
        if(!f.Mock){text="LIVE NATIVE OBSERVATIONS\r\n";for(int p=0;p<2;p++){var c=f.Meter.Players[p];text+="\r\n"+TimingPresentation.Summary(c,p,false)+"\r\n"+PipelineDiagnostics.Player(f,p)+"\r\n"+(c.TimingNote??"Awaiting a complete action")+"\r\n";}var e0=f.Meter.Players[0].Evidence;if(e0!=null)text+="\r\nEffective damage: "+e0.Damage+" ("+e0.DamageKind+")\r\nActive spans: "+e0.ActivePattern+" (parentheses are inactive native ticks)\r\nOnHit / OnGuard: "+SevenMetrics.Sign(e0.OnHit)+" / "+SevenMetrics.Sign(e0.OnGuard)+" (last observations for the same actor/action ID)\r\nStartup column counts native ticks BEFORE first attack; older First attack values include the first attack tick.\r\n";text+="\r\nCharacter / move names: "+NA+"\r\nVerified startup / recovery / advantage: "+NA+"\r\nFirst attack = native timer at first attack region. Active = distinct attack timer ticks. Tail = last attack to standing/crouching return marker, NOT verified recovery. Shared simulation clock and actionable states are unmapped.\r\n";}if(f.Origins==null)text+="\r\nCharacter pivot: "+NA;
        else for(int i=0;i<f.Origins.Length;i++)text+=String.Format("\r\nP{0} mock origin: {1:0.0}, {2:0.0}",i+1,f.Origins[i].X,f.Origins[i].Y);
        if(selected!=null)text+=String.Format("\r\n\r\nSELECTED COLLISION REGION\r\nPlayer: {0}\r\nType: {1}\r\nLeft / bottom: {2:0.000}, {3:0.000}\r\nWidth / height: {4:0.000}, {5:0.000}",selected.Player,VisualStyle.Names[selected.Kind],selected.Left,selected.Bottom,selected.Width,selected.Height);
        return text;
    }
    static string Value(int? v){return v.HasValue?v.Value.ToString():NA;}
}
