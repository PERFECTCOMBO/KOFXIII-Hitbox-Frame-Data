using System;
using System.Collections.Generic;

// Measures the native action timer, not elapsed capture samples. Hitstop repeats
// a timer value and must not inflate active duration. Unsupported transitions
// discard the measurement instead of inventing recovery or frame advantage.
internal sealed class NativeTimingTracker {
    sealed class Track {
        internal CharacterState Previous;
        internal AttackMeasurement Saved;internal uint? SavedActor;
        internal int Action,StartAction,TimerOffset,LastTimer,FirstActive,LastActive,ActiveTicks;
        internal readonly HashSet<int> AttackTicks=new HashSet<int>();
        internal bool Tracking,RecoveryEligible,RecoveryLanding;
        internal int? StartState,StartCommand,StartCategory;
        internal int? RecoveryState,RecoveryCategory,RecoveryCommand;internal int RecoveryAction;
        internal int? First,Active,Tail,Total;
        internal string Note;
        internal void Clear(string note){Tracking=false;RecoveryEligible=false;RecoveryLanding=false;First=Active=Tail=Total=null;AttackTicks.Clear();Note=note;}
    }
    readonly Track[] players={new Track(),new Track()};
    DebugFrame previousFrame;
    static string Pattern(HashSet<int> ticks){
        var v=new System.Collections.Generic.List<int>(ticks);v.Sort();if(v.Count==0)return "";
        var b=new System.Text.StringBuilder();int run=1;
        for(int i=1;i<v.Count;i++){if(v[i]==v[i-1]+1)run++;else {b.Append(run).Append(" (").Append(v[i]-v[i-1]-1).Append(") ");run=1;}}
        return b.Append(run).ToString();
    }
    // Matches AI IsNeutralAction at 0x4401B0: primary/secondary in {1,2}.
    // This does not establish cancel availability or guard recovery readiness.
    static bool RestMarker(CharacterState s){return s.NativeNeutral==true||s.NativeStateId==2||s.NativeSecondaryStateId==2;}
    internal void Observe(DebugFrame frame){
        for(int i=0;i<2;i++){
            var t=players[i];var s=frame.Meter.Players[i];var previous=t.Previous;
            if(t.Saved!=null&&t.SavedActor!=s.ActorIdentity)t.Saved=null;
            bool unavailable=!s.NativeActionTime.HasValue||!s.NativeActionId.HasValue||!s.NativeNeutral.HasValue;
            if(unavailable||frame.MissingBefore>0||frame.Truncated||(previousFrame!=null&&frame.Number!=previousFrame.Number+1)){
                if(unavailable)t.Saved=null;
                s.LastAttack=t.Saved;
                t.Clear(unavailable?"Awaiting fighter data":"Incomplete capture; timing discarded");
                t.Previous=null;s.TimingNote=t.Note;continue;
            }
            int timer=s.NativeActionTime.Value,action=s.NativeActionId.Value;
            bool replaced=previous!=null&&s.ActorIdentity.HasValue&&previous.ActorIdentity!=s.ActorIdentity;
            // Idle animations routinely loop their timer. They do not invalidate
            // the explicitly labelled LAST measurement. Identity/gaps still do.
            if(replaced){t.Saved=null;t.Clear("Fighter instance changed; timing reset");previous=null;t.Previous=null;}
            bool landing=t.Tracking&&MoveTransitions.Landing(previousFrame,frame,i);
            bool begins=!landing&&previous!=null&&!RestMarker(s)&&timer==1&&!s.NativeHitstop&&
                (RestMarker(previous)||previous.NativeActionId!=action);
            if(begins){
                t.Clear("Measuring native action");t.Tracking=true;t.Action=t.StartAction=action;t.TimerOffset=0;
                t.LastTimer=0;t.FirstActive=t.LastActive=t.ActiveTicks=0;
                t.StartCategory=frame.Readiness!=null&&frame.Readiness.Length==20?(int?)frame.Readiness[i*10]:null;
                t.StartState=s.NativeStateId;t.StartCommand=frame.Readiness!=null&&frame.Readiness.Length==20?(int?)frame.Readiness[i*10+1]:null;
                t.RecoveryState=t.StartState;t.RecoveryCategory=t.StartCategory;t.RecoveryCommand=t.StartCommand;t.RecoveryAction=action;
                t.RecoveryEligible=s.ActorIdentity.GetValueOrDefault()!=0&&frame.Readiness!=null&&frame.Readiness.Length==20
                    &&(t.StartCategory==1||t.StartCategory==3)&&s.NativeStateId==s.NativeSecondaryStateId;
            }
            if(t.Tracking){
                bool recoveryLanding=t.RecoveryEligible&&!t.RecoveryLanding&&t.StartCategory==3&&t.TimerOffset==0
                    &&action!=t.Action&&MoveTransitions.Landing(previousFrame,frame,i)
                    &&s.NativeStateId==s.NativeSecondaryStateId;
                if(recoveryLanding){
                    t.RecoveryLanding=true;t.RecoveryState=s.NativeStateId;
                    t.RecoveryCategory=0;t.RecoveryCommand=173;t.RecoveryAction=action;
                }
                // Accept only a stable action or one semantically identified
                // special landing. Arbitrary cancels and timer loops stay unknown.
                t.RecoveryEligible &= frame.Readiness!=null&&frame.Readiness.Length==20
                    &&frame.Readiness[i*10]==t.RecoveryCategory&&frame.Readiness[i*10+1]==t.RecoveryCommand
                    &&s.NativeStateId==t.RecoveryState&&action==t.RecoveryAction;
                if(action!=t.Action&&MoveTransitions.Landing(previousFrame,frame,i)){t.TimerOffset=t.LastTimer;t.Action=action;}
                if(action==t.Action&&MoveTransitions.InactiveLoop(previousFrame,frame,i)){
                    t.RecoveryEligible=false;
                    t.TimerOffset+=previous.NativeActionTime.Value-s.NativeActionTime.Value+1;
                    DebugLog.Write("TIMING P"+(i+1)+" inactive internal timer loop; elapsed ticks continued");
                }
                timer+=t.TimerOffset;
                if(action!=t.Action||timer<t.LastTimer||timer>t.LastTimer+1||timer>4096){
                    t.Clear("Action changed or cancelled; timing unavailable");
                }else if(RestMarker(s)){
                    if(t.FirstActive>0&&timer==t.LastTimer+1&&!s.NativeAttack&&!s.NativeHitstop){
                        t.First=t.FirstActive;t.Active=t.ActiveTicks;t.Tail=timer-1-t.LastActive;t.Total=timer-1;
                        t.SavedActor=s.ActorIdentity;
                        t.Saved=new AttackMeasurement{Action=t.StartAction,First=t.First.Value,Active=t.Active.Value,Tail=t.Tail.Value,Total=t.Total.Value,Capture=frame.Number,ActivePattern=Pattern(t.AttackTicks)};
                        // Held Up/Left experiments start the follow-up on the update
                        // AFTER this marker. Include this final locked tick; the
                        // legacy Tail deliberately stopped one tick earlier.
                        if(t.RecoveryEligible&&(s.NativeSecondaryStateId==1||s.NativeSecondaryStateId==2)&&previous!=null
                            &&previous.NativeSecondaryStateId==t.RecoveryState&&timer>t.LastActive){
                            t.Saved.Recovery=timer-t.LastActive;
                            t.Saved.RecoverySource=t.RecoveryLanding?"Special landing return; recovery includes airborne tail and landing ticks; held-input endpoint tested on Kyo DP.":"Single-action return; native ticks excluding hitstop; endpoint tested on Kyo/Iori normals and Iori DP.";
                        }
                        t.Note=t.Saved.Recovery.HasValue?"Completed action; recovery through tested return marker":s.NativeNeutral==true?"Last completed action; native timer ticks":"Observed crouching return (state 2); native ticks, not verified recovery";
                        DebugLog.Write("TIMING P"+(i+1)+" action="+t.Action+" first="+t.First+" active="+t.Active+" tail="+t.Tail+" total="+t.Total+" recovery="+(t.Saved.Recovery.HasValue?t.Saved.Recovery.ToString():"unavailable")+" native ticks");
                    }else t.Clear("No complete attack measurement");
                    t.Tracking=false;
                }else{
                    if(s.NativeAttack&&t.AttackTicks.Add(timer)){
                        if(t.FirstActive==0)t.FirstActive=timer;
                        t.LastActive=timer;t.ActiveTicks++;
                    }
                    t.LastTimer=timer;
                }
            }
            // Keep the last completed measurement visible during idle, but clear
            // it when another action starts without a complete captured beginning.
            if(!t.Tracking&&!begins&&previous!=null&&RestMarker(previous)&&!RestMarker(s))
                t.Clear("Action beginning unavailable");
            s.ObservedActionId=t.Total.HasValue?(int?)t.Action:null;s.ObservedFirstActive=t.First;s.ObservedActiveTicks=t.Active;
            s.ObservedTailTicks=t.Tail;s.ObservedTotalTicks=t.Total;s.TimingNote=t.Note;
            s.TimingInProgress=t.Tracking;
            if(t.Tracking){s.ObservedActionId=t.Action;s.ObservedFirstActive=t.FirstActive>0?(int?)t.FirstActive:null;s.ObservedActiveTicks=t.FirstActive>0?(int?)t.ActiveTicks:null;}
            t.Previous=s;
            s.LastAttack=t.Saved;
        }
        previousFrame=frame;
    }
}
