using System;
using System.Collections.Generic;
internal static class NativeTimingTests {
    static long sequence;
    static void Check(bool condition,string name){if(!condition)throw new Exception("Native timing: "+name);}
    static DebugFrame Sample(int timer,bool neutral,bool attack,bool stop){
        var boxes=new List<Box>();if(attack)boxes.Add(new Box{Group=1});
        return new DebugFrame{Number=++sequence,Meter=GameStateReader.FromNative(new[]{45,neutral?1:45,stop?8:0,68,2,timer,1,1,0,1,1,1},boxes)};
    }
    static CharacterState Complete(NativeTimingTracker tracker,bool stopped){
        tracker.Observe(Sample(10,true,false,false));
        for(int n=1;n<=13;n++){
            tracker.Observe(Sample(n,false,n>=4&&n<=7,false));
            if(stopped&&n==4)for(int j=0;j<7;j++)tracker.Observe(Sample(n,false,true,true));
        }
        var end=Sample(14,true,false,false);tracker.Observe(end);return end.Meter.Players[0];
    }
    internal static void Run(){
        var retained=new NativeTimingTracker();Complete(retained,false);
        var walk=new DebugFrame{Number=++sequence,Meter=GameStateReader.FromNative(new[]{16,16,0,2,0,1,1,1,0,1,0,1},new List<Box>())};retained.Observe(walk);
        Check(walk.Meter.Players[0].LastAttack!=null&&walk.Meter.Players[0].LastAttack.Total==13&&!walk.Meter.Players[0].ObservedTotalTicks.HasValue,"walking keeps labelled last attack separately from current measurement");
        var broken=Sample(5,false,true,false);broken.MissingBefore=3;retained.Observe(broken);
        Check(broken.Meter.Players[0].LastAttack!=null&&!broken.Meter.Players[0].ObservedFirstActive.HasValue,"gap retains historical result but cannot fabricate current timing");
        var absent=new DebugFrame{Number=++sequence};retained.Observe(absent);Check(absent.Meter.Players[0].LastAttack==null,"missing fighters clear historical result");
        retained=new NativeTimingTracker();Complete(retained,false);var changed=Sample(15,true,false,false);changed.Meter.Players[0].ActorIdentity=999;retained.Observe(changed);Check(changed.Meter.Players[0].LastAttack==null,"fighter change clears historical result");
        CrouchTimingTests.Run();
        var resetTracker=new NativeTimingTracker();Complete(resetTracker,false);
        var replaced=Sample(15,true,false,false);replaced.Meter.Players[0].ActorIdentity=1234;resetTracker.Observe(replaced);
        Check(!replaced.Meter.Players[0].ObservedTotalTicks.HasValue,"fighter replacement clears stale timing");
        resetTracker=new NativeTimingTracker();Complete(resetTracker,false);var idleReset=Sample(1,true,false,false);resetTracker.Observe(idleReset);
        Check(idleReset.Meter.Players[0].ObservedTotalTicks==13,"ordinary idle animation loop retains last measurement");
        var inProgress=new NativeTimingTracker();inProgress.Observe(Sample(10,true,false,false));
        for(int n=1;n<=4;n++)inProgress.Observe(Sample(n,false,false,false));
        var onset=Sample(4,false,true,true);inProgress.Observe(onset);
        Check(onset.Meter.Players[0].TimingInProgress&&onset.Meter.Players[0].ObservedFirstActive==4&&onset.Meter.Players[0].ObservedActiveTicks==1,"attack onset in repeated hitstop timer counted once and shown immediately");
        onset=Sample(4,false,true,true);inProgress.Observe(onset);Check(onset.Meter.Players[0].ObservedActiveTicks==1,"repeated hitstop does not inflate active ticks");
        foreach(bool stop in new[]{false,true}){
            var result=Complete(new NativeTimingTracker(),stop);
            Check(result.ObservedFirstActive==4&&result.ObservedActiveTicks==4&&result.ObservedTailTicks==6&&result.ObservedTotalTicks==13,"completed action excludes repeated hitstop ticks");
            Check(!result.ActionableFrame.HasValue&&!result.StartupFrames.HasValue,"observations do not invent shared clock or certified move data");
        }
        var tracker=new NativeTimingTracker();tracker.Observe(Sample(1,true,false,false));
        tracker.Observe(Sample(1,false,false,false));tracker.Observe(Sample(2,false,false,false));
        var cancel=Sample(1,false,false,false);tracker.Observe(cancel);
        for(int n=2;n<=13;n++)tracker.Observe(Sample(n,false,n>=4&&n<=7,false));
        var end=Sample(14,true,false,false);tracker.Observe(end);Check(!end.Meter.Players[0].ObservedTotalTicks.HasValue,"cancel invalidates measurement");
        tracker=new NativeTimingTracker();tracker.Observe(Sample(1,true,false,false));tracker.Observe(Sample(1,false,false,false));
        var gap=Sample(4,false,true,false);gap.MissingBefore=2;tracker.Observe(gap);
        for(int n=5;n<=13;n++)tracker.Observe(Sample(n,false,n<=7,false));
        end=Sample(14,true,false,false);tracker.Observe(end);Check(!end.Meter.Players[0].ObservedTotalTicks.HasValue,"gap invalidates measurement");
        var missing=new DebugFrame{Number=++sequence};tracker.Observe(missing);Check(!missing.Meter.Players[0].ObservedTotalTicks.HasValue,"menu clears measurement");
        tracker=new NativeTimingTracker();tracker.Observe(Sample(1,true,false,false));
        for(int n=1;n<=13;n++)tracker.Observe(Sample(n,false,n==4||n==5||n==8,false));
        end=Sample(14,true,false,false);tracker.Observe(end);Check(end.Meter.Players[0].ObservedActiveTicks==3&&end.Meter.Players[0].ObservedTailTicks==5,"multi-hit counts active ticks not span");
    }
}
