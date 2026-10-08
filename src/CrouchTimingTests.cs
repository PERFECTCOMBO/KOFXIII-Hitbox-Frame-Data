using System;
using System.Collections.Generic;
internal static class CrouchTimingTests {
    static long sequence;
    static DebugFrame Frame(int action,int timer,int primary,int secondary,bool attack=false,bool stop=false){
        var boxes=new List<Box>();if(attack)boxes.Add(new Box{Group=1});
        return new DebugFrame{Number=++sequence,Meter=GameStateReader.FromNative(new[]{primary,secondary,stop?8:0,action,0,timer,1,1,0,1,0,1},boxes)};
    }
    static void Check(bool b,string msg){if(!b)throw new Exception("Crouch timing: "+msg);}
    internal static void Run(){
        foreach(bool moving in new[]{false,true}){
            var t=new NativeTimingTracker();t.Observe(moving?Frame(50,11,41,41):Frame(26,7,2,2));
            for(int n=1;n<=15;n++){t.Observe(Frame(71,n,51,51,n>=5&&n<=8));if(n==5)for(int j=0;j<12;j++)t.Observe(Frame(71,n,51,51,true,true));}
            var end=Frame(71,16,51,2);t.Observe(end);var s=end.Meter.Players[0];
            Check(s.ObservedFirstActive==5&&s.ObservedActiveTicks==4&&s.ObservedTailTicks==7&&s.ObservedTotalTicks==15,"complete crouching return from rest/movement");
            Check(s.NativeNeutral==false&&!s.ActionableFrame.HasValue&&!s.StartupFrames.HasValue,"crouching marker must not become native neutral or certified frame data");
            var idle=Frame(26,1,2,2);t.Observe(idle);Check(idle.Meter.Players[0].ObservedTotalTicks==15,"retain result on return to crouch");
            t.Observe(Frame(26,30,2,2));idle=Frame(26,1,2,2);t.Observe(idle);Check(idle.Meter.Players[0].ObservedTotalTicks==15,"crouch idle loop retains last result");
            var replaced=Frame(26,2,2,2);replaced.Meter.Players[0].ActorIdentity=1234;t.Observe(replaced);Check(!replaced.Meter.Players[0].ObservedTotalTicks.HasValue,"fighter replacement still clears crouching result");
        }
        var interrupted=new NativeTimingTracker();interrupted.Observe(Frame(26,7,2,2));
        for(int n=1;n<=6;n++)interrupted.Observe(Frame(71,n,51,51,n>=5));
        var next=Frame(76,1,47,47);interrupted.Observe(next);
        Check(!next.Meter.Players[0].ObservedTotalTicks.HasValue&&next.Meter.Players[0].ObservedActionId==76,"cancelled move cannot publish total or contaminate next action");
        var gap=Frame(76,4,47,47,true);gap.MissingBefore=2;interrupted.Observe(gap);
        Check(!gap.Meter.Players[0].ObservedFirstActive.HasValue,"missing start cannot invent timing");
        for(int n=5;n<=20;n++)interrupted.Observe(Frame(76,n,47,47,n<10));
        next=Frame(76,21,47,2);interrupted.Observe(next);Check(!next.Meter.Players[0].ObservedTotalTicks.HasValue,"gap must invalidate crouching completion");
        var midAction=new NativeTimingTracker();for(int n=6;n<=15;n++)midAction.Observe(Frame(71,n,51,51,n<=8));
        next=Frame(71,16,51,2);midAction.Observe(next);Check(!next.Meter.Players[0].ObservedTotalTicks.HasValue,"attach mid-action must not invent startup");
    }
}
