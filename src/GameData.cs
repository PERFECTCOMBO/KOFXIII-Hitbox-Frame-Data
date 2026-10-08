using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

internal sealed class DebugFrame {
    internal long Number; internal DateTime Time=DateTime.UtcNow;
    internal bool Mock,Truncated; internal int MissingBefore;
    internal List<Box> Boxes=new List<Box>(); internal float[] Projection;
    internal int[] Readiness;
    internal string Character="N/A — data unavailable",Move="N/A — data unavailable",State="N/A — data unavailable";
    internal int? AnimationFrame,Startup,Active,Recovery,Damage;
    internal string MockOutcome;
    internal PointF[] Origins;
    internal MeterState Meter=GameStateReader.Unavailable();
}
internal interface IGameProvider : IDisposable {
    string Name {get;} bool IsMock {get;} int ProcessId {get;} DebugFrame Poll();
}
internal sealed class GlobalMatchProvider : IGameProvider {
    readonly FrameCapture capture;
    readonly ActionExecutionTracker executions=new ActionExecutionTracker();
    readonly NativeTimingTracker timing=new NativeTimingTracker();
    readonly ObservedAdvantageTracker advantage=new ObservedAdvantageTracker();
    readonly MoveEvidenceTracker evidence=new MoveEvidenceTracker();
    readonly DetectionTrace trace=new DetectionTrace();
    readonly ReadinessRecorder readiness=new ReadinessRecorder();
    internal GlobalMatchProvider(){
        PipelineDiagnostics.Begin(false);
        if(AutoLaunch.Current!=null){capture=new FrameCapture(AutoLaunch.Current.OpenForCapture());return;}
        var matches=new List<Process>();
        foreach(var p in Process.GetProcessesByName("game")){
            bool supported=false;
            try{supported=FrameCapture.IsSupportedPath(p.MainModule.FileName);}catch(Exception ex){DebugLog.Write("Game detection: "+ex.Message);}
            if(supported)matches.Add(p);else p.Dispose();
        }
        if(matches.Count!=1){int count=matches.Count;foreach(var p in matches)p.Dispose();
            throw new InvalidOperationException(count==0?"No supported Global Match build found. Launch the game in offline Training first. Other game versions are not supported by v0.1.":"More than one supported game is running. Close the extra game instance and try again.");}
        Process selected=matches[0];
        capture=new FrameCapture(selected);
    }
    public string Name {get{return "Global Match / experimental adapter";}}
    public bool IsMock {get{return false;}} public int ProcessId {get{return capture.ProcessId;}}
    public DebugFrame Poll(){
        if(!capture.Alive)throw new InvalidOperationException("Game closed. Detach or switch to mock mode.");
        var boxes=capture.ReadFrame();if(boxes==null)return null;
        long number=capture.LastSequence;
        var f=new DebugFrame{Number=number,Boxes=boxes,Meter=GameStateReader.FromNative(capture.FighterStates,boxes),Projection=(float[])capture.Projection.Clone(),MissingBefore=capture.LastMissing,Truncated=capture.LastTruncated};
        for(int p=0;p<2;p++)f.Meter.Players[p].ActorIdentity=capture.FighterActors[p];
        f.Readiness=capture.Readiness;
        executions.Observe(f);timing.Observe(f);advantage.Observe(f);evidence.Observe(f);ValidationSession.Observe(f);SpecialTrial.Observe(f);PipelineDiagnostics.Captured(f);
        readiness.Observe(f);trace.Observe(f);
        f.Move=f.Meter.Players[0].CurrentMove??"N/A — data unavailable";f.State=f.Meter.Players[0].Phase.ToString();
        return f;
    }
    public void Dispose(){capture.Detach();capture.Dispose();readiness.Flush();trace.Flush();}
}
internal sealed class MockProvider : IGameProvider {
    long frame;
    public string Name {get{return "MOCK / frame meter simulation";}}public bool IsMock {get{return true;}}public int ProcessId {get{return 0;}}
    public DebugFrame Poll(){
        long n=frame++;int e=(int)(n%60),variant=(int)(n/60)%7;bool contact=variant!=2&&variant!=5;bool stopped=contact&&(e==5||e==6);
        int t=contact&&e>=7?e-2:e;float x=360,target=520;bool multi=variant==4;
        FramePhase phase=t<4?FramePhase.Startup:(multi?(t==4||t==5||t==8):t<7)?FramePhase.Active:t<13?FramePhase.Recovery:FramePhase.Neutral;
        string outcome=variant==0?"HIT":variant==1?"BLOCK":variant==2?"WHIFF":variant==3?"TRADE":variant==4?"HIT":variant==5?"PROJECTILE":"THROW";
        int defenderReady=variant==1?12:variant==3?13:16;
        FramePhase defender=e<4||!contact?FramePhase.Neutral:t<defenderReady?(variant==1?FramePhase.Blockstun:FramePhase.Hitstun):FramePhase.Neutral;
        if(stopped){phase=FramePhase.Hitstop;defender=FramePhase.Hitstop;}
        var p1=new CharacterState{CharacterId="Mock P1",CurrentMove=multi?"Mock multi-hit":variant==5?"Mock projectile":variant==6?"Mock throw":"Mock jab",Input="Mock A",Phase=phase,CurrentFrame=t+1,StartupFrames=4,ActiveFrames=3,RecoveryFrames=6,TotalFrames=13,HitstopFrames=stopped?7-e:0,Outcome=e>=(variant==2?13:4)?outcome:null};
        var p2=new CharacterState{CharacterId="Mock P2",CurrentMove="Mock defender",Phase=defender,CurrentFrame=t+1,HitstopFrames=stopped?7-e:0,HitstunFrames=defender==FramePhase.Hitstun?Math.Max(0,defenderReady-t):(int?)null,BlockstunFrames=defender==FramePhase.Blockstun?Math.Max(0,defenderReady-t):(int?)null};
        if(contact&&e>=4&&variant!=5&&variant!=6){p1.ActionableFrame=n-e+15;p2.ActionableFrame=n-e+defenderReady+2;}
        if(variant==3&&e>=4&&t<13&&!stopped){p1.Phase=FramePhase.Hitstun;p1.HitstunFrames=13-t;p1.TotalFrames=null;}
        var meter=new MeterState{IsMock=true,GameFrame=n,InteractionId=n/60,InteractionFrame=e+1,Players=new[]{p1,p2}};
        var f=new DebugFrame{Number=n,Mock=true,Meter=meter,Character=p1.CharacterId,Move=p1.CurrentMove,State=phase.ToString(),AnimationFrame=t+1,Startup=4,Active=3,Recovery=6,Damage=30,Origins=new[]{new PointF(x,0),new PointF(target,0)},MockOutcome=e>=(variant==2?13:4)&&t<=16?"MOCK "+outcome:null};
        f.Boxes.Add(new Box{Group=0,Left=x-23,Bottom=0,Width=46,Height=150});f.Boxes.Add(new Box{Group=3,Left=x-32,Bottom=0,Width=64,Height=165});
        f.Boxes.Add(new Box{Group=5,Left=target-23,Bottom=0,Width=46,Height=150});f.Boxes.Add(new Box{Group=8,Left=target-32,Bottom=0,Width=64,Height=165});
        if(variant!=5&&variant!=6&&(phase==FramePhase.Active||stopped))f.Boxes.Add(new Box{Group=1,Left=x+24,Bottom=85,Width=variant==2?30:200,Height=35});
        if(variant==3&&(phase==FramePhase.Active||stopped))f.Boxes.Add(new Box{Group=6,Left=x-10,Bottom=85,Width=target-x+20,Height=35});
        if(variant==1&&defender!=FramePhase.Neutral)f.Boxes.Add(new Box{Group=7,Left=target-32,Bottom=0,Width=64,Height=165});
        if(variant==6&&t>=4&&t<7)f.Boxes.Add(new Box{Group=0,ExtraKind=5,Left=x+20,Bottom=0,Width=170,Height=140});
        if(variant==5&&t>=7&&t<30){float px=x+50+(t-7)*9;f.Boxes.Add(new Box{Group=1,ExtraKind=6,Left=px,Bottom=70,Width=30,Height=30});f.Boxes.Add(new Box{Group=3,ExtraKind=7,Left=px,Bottom=70,Width=30,Height=30});meter.Projectile=new CharacterState{CurrentMove="Mock projectile",Phase=t<11?FramePhase.Active:FramePhase.ProjectileTravel,CurrentFrame=t-6};}
        if(variant==5&&meter.Projectile==null)meter.Projectile=new CharacterState{CharacterId="Mock object",CurrentMove="Mock projectile",Phase=t<7?FramePhase.Startup:FramePhase.Neutral,CurrentFrame=t+1};
        if(t>=30&&t<35){f.Boxes.Add(new Box{Group=0,ExtraKind=8,Left=x-32,Bottom=0,Width=64,Height=165});p1.Invulnerable=true;}
        if(t>=35&&t<40){f.Boxes.Add(new Box{Group=2,Left=x-32,Bottom=0,Width=64,Height=165});p1.Armored=true;}
        return f;
    }
    public void Dispose(){}
}
internal sealed class FrameHistory {
    internal readonly List<DebugFrame> Frames=new List<DebugFrame>();
    internal int Selected=-1; internal bool Live=true; internal const int Capacity=1800;
    internal void Add(DebugFrame f){Frames.Add(f);if(Frames.Count>Capacity){Frames.RemoveAt(0);Selected=Math.Max(0,Selected-1);}if(Live)Selected=Frames.Count-1;}
    internal DebugFrame Current {get{return Selected>=0&&Selected<Frames.Count?Frames[Selected]:null;}}
    internal void Select(int i){Live=false;Selected=Math.Max(0,Math.Min(Frames.Count-1,i));}
    internal void Clear(){Frames.Clear();Selected=-1;Live=true;}
}
internal static class CollisionAnalysis {
    internal static bool Intersects(Box a,Box b){return a.Left<b.Left+b.Width&&a.Left+a.Width>b.Left&&a.Bottom<b.Bottom+b.Height&&a.Bottom+a.Height>b.Bottom;}
    internal static bool IsAttack(Box b){return b.Kind==1||b.Kind==6;}
    internal static bool IsHurt(Box b){return b.Kind==3||b.Kind==7;}
    internal static List<Box> Overlaps(DebugFrame f){var list=new List<Box>();if(f==null)return list;
        foreach(var a in f.Boxes)if(IsAttack(a))foreach(var b in f.Boxes)if(IsHurt(b)&&a.Player!=b.Player&&Intersects(a,b)){if(!list.Contains(a))list.Add(a);if(!list.Contains(b))list.Add(b);}return list;
    }
    internal static string Event(DebugFrame f){if(f==null)return "No frame";var overlaps=Overlaps(f);if(!f.Mock)return overlaps.Count>0?"Geometric overlap (hit result unavailable)":"Hit / block / whiff / trade / throw: N/A — data unavailable";
        if(f.MockOutcome!=null)return f.MockOutcome;
        bool p1=false,p2=false;foreach(var b in overlaps)if(IsAttack(b)){if(b.Player==1)p1=true;else p2=true;}
        if(p1&&p2)return "MOCK TRADE";if(p1||p2)return "MOCK HIT";
        if(f.AnimationFrame==18)return "MOCK attack ended — outcome unavailable";
        return "MOCK: no attack/hurt overlap";
    }
}

