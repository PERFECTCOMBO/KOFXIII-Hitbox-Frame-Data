using System;
using System.Collections.Generic;
internal sealed class MoveEvidence {
    internal int? Damage;
    internal long? OnHit,OnGuard;
    internal string DamageKind="Unavailable",ActivePattern="";
}
// Measured health loss is effective damage (including scaling), never raw base damage.
internal sealed class MoveEvidenceTracker {
    sealed class Track {internal bool Tracking,Invalid,Chip;internal int Damage;internal long LastCapture=-1;internal uint? Actor;internal readonly Dictionary<int,MoveEvidence> Results=new Dictionary<int,MoveEvidence>();}
    readonly Track[] tracks={new Track(),new Track()};DebugFrame previous;
    internal void Observe(DebugFrame f){
        for(int p=0;p<2;p++){
            var t=tracks[p];var s=f.Meter.Players[p];int d=1-p;
            if(t.Actor!=s.ActorIdentity||!s.ActorIdentity.HasValue){t.Results.Clear();t.Tracking=false;t.LastCapture=-1;t.Actor=s.ActorIdentity;}
            bool valid=!f.Mock&&f.Readiness!=null&&f.Readiness.Length==20&&s.ActorIdentity.GetValueOrDefault()!=0;
            bool contiguous=valid&&previous!=null&&previous.Readiness!=null&&f.Number==previous.Number+1&&f.MissingBefore==0&&!f.Truncated&&previous.Meter.Players[d].ActorIdentity==f.Meter.Players[d].ActorIdentity;
            if(!contiguous)t.Invalid=true;
            bool start=s.TimingInProgress&&s.NativeActionTime==1&&(previous==null||previous.Meter.Players[p].NativeActionTime!=1||previous.Meter.Players[p].NativeActionId!=s.NativeActionId)&&!MoveTransitions.Landing(previous,f,p);
            if(start){t.Tracking=true;t.Damage=0;t.Chip=false;t.Invalid=!contiguous;}
            if(t.Tracking&&contiguous){
                int before=previous.Readiness[d*10+4],now=f.Readiness[d*10+4],loss=before-now;
                if(before<0||now<0||now>before)t.Invalid=true;
                if(loss>0){int result=f.Readiness[p*10+2];if(!s.NativeAttack||(result!=1&&result!=2)||f.Meter.Players[d].NativeAttack)t.Invalid=true;else {t.Damage+=loss;t.Chip|=result==1;}}
                if(f.Readiness[p*10+4]<previous.Readiness[p*10+4])t.Invalid=true;
            }
            var m=s.LastAttack;
            if(m!=null&&m.Capture!=t.LastCapture){
                t.LastCapture=m.Capture;// A whiff/new contact must not inherit advantage from a previous attempt.
                var result=new MoveEvidence{OnHit=null,OnGuard=null,ActivePattern=m.ActivePattern??m.Active.ToString()};
                if(t.Tracking&&!t.Invalid&&m.Capture==f.Number){result.Damage=t.Damage;result.DamageKind=t.Chip?"Effective chip damage":"Effective damage (scaling included)";}
                t.Results[m.Action]=result;t.Tracking=false;
            }
            if(!s.TimingInProgress)t.Tracking=false;
            if(m!=null){MoveEvidence result;if(t.Results.TryGetValue(m.Action,out result)){
                var a=s.ExperimentalAdvantage;var updated=new MoveEvidence{Damage=result.Damage,DamageKind=result.DamageKind,ActivePattern=result.ActivePattern,OnHit=result.OnHit,OnGuard=result.OnGuard};
                if(a!=null&&a.AttackReturn==m.Capture){if(a.Outcome=="hit")updated.OnHit=a.Gap;else if(a.Outcome=="block")updated.OnGuard=a.Gap;}
                t.Results[m.Action]=updated;s.Evidence=updated;
            }}
        }
        previous=f;
    }
}
internal static class SevenMetrics {
    internal static readonly string[] Headers={"Startup*","Active*","Recovery","OnHit*","OnGuard*","Damage","Stun"};
    internal static string Sign(long? n){return !n.HasValue?"—":n.Value<0?"−"+(-n.Value):n.Value>0?"+"+n.Value:"0";}
    internal static string[] Values(CharacterState s,bool mock){
        if(mock)return new[]{TimingPresentation.N(s.StartupFrames),TimingPresentation.N(s.ActiveFrames),TimingPresentation.N(s.RecoveryFrames),"—","—","—","—"};
        var m=s.LastAttack;var e=s.Evidence;
        return new[]{m==null?"—":Math.Max(0,m.First-1).ToString(),m==null?"—":m.Active.ToString(),m==null||!m.Recovery.HasValue?"—":m.Recovery.Value.ToString(),Sign(e==null?null:e.OnHit),Sign(e==null?null:e.OnGuard),e==null||!e.Damage.HasValue?"—":e.Damage.Value.ToString(),"—"};
    }
}
