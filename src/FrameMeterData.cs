using System;
using System.Collections.Generic;
internal enum FramePhase {Unknown,ObservedState,ObservedAttack,Neutral,Startup,Active,Recovery,Hitstun,Blockstun,Hitstop,Knockdown,Wakeup,Throw,ProjectileTravel}
internal sealed class CharacterState {
    internal ActionExecution LastExecution;
    internal MoveEvidence Evidence;
    internal ObservedAdvantage ExperimentalAdvantage;
    internal AttackMeasurement LastAttack;
    internal uint? ActorIdentity; internal bool TimingInProgress;
    internal string CharacterId,CurrentMove,Input,Outcome;
    internal int? NativeStateId,NativeSecondaryStateId,NativeActionId,NativeCellIndex,NativeActionTime;
    internal FramePhase Phase=FramePhase.Unknown;
    internal int? CurrentFrame,StartupFrames,ActiveFrames,RecoveryFrames,TotalFrames,HitstunFrames,BlockstunFrames,HitstopFrames;
    internal long? ActionableFrame;
    internal bool? NativeNeutral; internal bool NativeAttack,NativeHitstop;
    internal int? ObservedActionId,ObservedFirstActive,ObservedActiveTicks,ObservedTailTicks,ObservedTotalTicks;
    internal string TimingNote;
    internal bool? Invulnerable,Armored;internal string CancelWindow;
}
internal sealed class AttackMeasurement {
    internal string ActivePattern;
    internal int? Recovery;
    internal string RecoverySource;
    internal int Action,First,Active,Tail,Total;internal long Capture;
}
internal sealed class MeterState {
    internal CharacterState[] Players={new CharacterState(),new CharacterState()};
    internal CharacterState Projectile;internal long? GameFrame,InteractionId;internal int? InteractionFrame;
    internal bool IsMock;
}
internal static class GameStateReader {
    // Native neutral is the exact IsPlayerNeutralAction predicate, not a cancel-window flag.
    internal static MeterState Unavailable(){return new MeterState();}
    internal static MeterState FromNative(int[] values,System.Collections.Generic.List<Box> boxes){
        var meter=new MeterState();
        for(int player=0;player<2;player++){
            int stride=values.Length>=12?6:3;
            int id=values[player*stride];if(id<0)continue;
            var state=meter.Players[player];state.NativeStateId=id;state.NativeSecondaryStateId=values[player*stride+1];
            state.CurrentMove="State "+id+" (move name --)";
            if(stride==6){state.NativeActionId=values[player*stride+3];state.NativeCellIndex=values[player*stride+4];state.NativeActionTime=values[player*stride+5];}
            bool attack=false;foreach(var box in boxes)if(box.Player==player+1&&box.Kind==1)attack=true;
            state.NativeNeutral=id==1||state.NativeSecondaryStateId==1;
            state.NativeAttack=attack;state.NativeHitstop=(values[player*stride+2]&8)!=0;
            state.Phase=state.NativeHitstop?FramePhase.Hitstop:attack?FramePhase.ObservedAttack:state.NativeNeutral==true?FramePhase.Neutral:FramePhase.ObservedState;
        }
        return meter;
    }
}
internal static class MoveDetector {
    internal static string Name(CharacterState s){return s.CurrentMove??"--";}
}
internal static class FrameDataCalculator {
    internal static string Frames(int? n){return n.HasValue?n.Value+"F":"--";}
    internal static string Total(CharacterState s){return Frames(s.TotalFrames);}
}
internal static class AdvantageCalculator {
    // Absolute actionable timestamps must share the same game clock and include hitstop.
    // Never infer these from animation duration or overlap alone.
    internal static long? Calculate(CharacterState attacker,CharacterState defender){
        if(!attacker.ActionableFrame.HasValue||!defender.ActionableFrame.HasValue)return null;
        return defender.ActionableFrame.Value-attacker.ActionableFrame.Value;
    }
    internal static string Label(long? n){return n.HasValue?(n.Value>0?"+":"")+n.Value+"F":"--";}
}
internal static class FrameTracker {
    // Shared column axis for both players. Live entries are capture updates until game
    // clock mapping exists, and they retain unknown/gap flags rather than guessing states.
    internal static List<DebugFrame> Window(FrameHistory history,int limit){
        var result=new List<DebugFrame>();if(history.Selected<0)return result;
        var current=history.Current;int start=Math.Max(0,history.Selected-limit+1);
        if(current.Meter!=null&&current.Meter.InteractionId.HasValue){while(start<history.Selected&&history.Frames[start].Meter.InteractionId!=current.Meter.InteractionId)start++;}
        for(int i=start;i<=history.Selected;i++)result.Add(history.Frames[i]);return result;
    }
}
internal sealed class TrainingModeController {
    internal readonly FrameHistory History;
    internal TrainingModeController(FrameHistory h){History=h;}
    internal void Pause(){History.Live=false;}
    internal void Resume(){History.Live=true;History.Selected=History.Frames.Count-1;}
    internal void Select(int i){History.Select(i);}
    internal bool CanControlGame {get{return false;}}
}
