using System;

// Experimental return-marker gap on the collision-update axis, not certified
// ActionableFrame data. Supports only the ordinary reactions validated in R4.
internal sealed class ObservedAdvantage {
    internal long AttackReturn,DefenderReturn;
    internal string Outcome;
    internal long Gap {get{return DefenderReturn-AttackReturn;}}
}
internal sealed class ObservedAdvantageTracker {
    sealed class Pending {
        internal int Player,Action,Timer;
        internal long Contact;
        internal long? AttackReturn,DefenderReturn;
        internal bool SawReaction;
        internal string Outcome;
    }
    Pending pending;
    DebugFrame previous;
    readonly ObservedAdvantage[] saved=new ObservedAdvantage[2];
    static bool Rest(CharacterState s){return s.NativeStateId==1||s.NativeStateId==2||s.NativeSecondaryStateId==1||s.NativeSecondaryStateId==2;}
    static int Raw(DebugFrame f,int p,int field){return f.Readiness[p*10+field];}
    static bool Valid(DebugFrame f){return f!=null&&!f.Mock&&f.Readiness!=null&&f.Readiness.Length==20&&f.Meter.Players[0].ActorIdentity.GetValueOrDefault()!=0&&f.Meter.Players[1].ActorIdentity.GetValueOrDefault()!=0&&f.Meter.Players[0].NativeStateId.HasValue&&f.Meter.Players[1].NativeStateId.HasValue;}
    internal void Observe(DebugFrame f){
        if(!Valid(f)){pending=null;previous=null;saved[0]=saved[1]=null;return;}
        bool identity=previous!=null&&(previous.Meter.Players[0].ActorIdentity!=f.Meter.Players[0].ActorIdentity||previous.Meter.Players[1].ActorIdentity!=f.Meter.Players[1].ActorIdentity);
        if(identity){saved[0]=saved[1]=null;previous=null;pending=null;}
        bool contiguous=previous!=null&&f.Number>previous.Number&&unchecked((uint)f.Number-(uint)previous.Number)==1&&f.MissingBefore==0&&!f.Truncated;
        if(!contiguous)pending=null;
        if(contiguous){
            bool contact=false;
            for(int p=0;p<2;p++){
                var a=f.Meter.Players[p];int d=1-p,result=Raw(f,p,2);
                bool hit=Raw(f,d,4)<Raw(previous,d,4)&&result==2;
                bool block=(Raw(f,d,3)&32)!=0&&(Raw(previous,d,3)&32)==0&&result==1;
                if(!hit&&!block)continue;
                contact=true;
                // Continue repeated contacts only inside the same uninterrupted move.
                if(pending!=null){
                    if(pending.Player==p&&pending.Action==a.NativeActionId&&!pending.AttackReturn.HasValue&&pending.Outcome==(block?"block":"hit")&&a.NativeActionTime>=pending.Timer&&a.NativeActionTime<=pending.Timer+1&&!f.Meter.Players[d].NativeAttack){
                        pending.Timer=a.NativeActionTime.Value;pending.DefenderReturn=null;pending.SawReaction=false;
                    }else pending=null;
                    break;
                }
                if((Raw(f,p,0)!=1&&Raw(f,p,0)!=3)||!a.NativeAttack||!a.TimingInProgress||!a.NativeActionTime.HasValue||f.Meter.Players[d].NativeAttack)continue;
                pending=new Pending{Player=p,Action=a.NativeActionId.Value,Timer=a.NativeActionTime.Value,Contact=f.Number,Outcome=block?"block":"hit"};
            }
            if(pending!=null&&!contact){
                var t=pending;int p=t.Player,d=1-p;var a=f.Meter.Players[p];var defender=f.Meter.Players[d];
                bool bad=unchecked((uint)f.Number-(uint)t.Contact)>180||defender.NativeAttack||Raw(f,d,4)<Raw(previous,d,4)||Raw(f,p,4)<Raw(previous,p,4);
                if(!t.AttackReturn.HasValue){
                    int timer=a.NativeActionTime.GetValueOrDefault(-1);
                    if(a.NativeActionId!=t.Action&&MoveTransitions.Landing(previous,f,p)){t.Action=a.NativeActionId.Value;t.Timer=0;}
                    bool internalLoop=MoveTransitions.InactiveLoop(previous,f,p);
                    bad|=a.NativeActionId!=t.Action||(!internalLoop&&(timer<t.Timer||timer>t.Timer+1));
                    t.Timer=timer;
                    if(Rest(a)&&!a.NativeHitstop&&!a.NativeAttack&&a.LastAttack!=null&&a.LastAttack.Capture==f.Number)t.AttackReturn=f.Number;
                }else if(a.NativeAttack||((Raw(f,p,0)==1||Raw(f,p,0)==3)&&(a.NativeActionId!=t.Action||a.NativeActionTime<t.Timer)))bad=true;
                if(t.Outcome=="block"){
                    bool rigid=(Raw(f,d,3)&1024)!=0;
                    if(rigid){
                        // Guard category + rigid flag identify the reaction; individual animation states vary.
                        if(Raw(f,d,0)!=2)bad=true;
                        t.SawReaction=true;
                    }
                    if(t.SawReaction&&!rigid&&!t.DefenderReturn.HasValue&&!defender.NativeHitstop&&Raw(f,d,5)<=0){
                        if((defender.NativeSecondaryStateId==72||defender.NativeSecondaryStateId==73)&&Raw(f,d,0)==2&&!defender.NativeHitstop)t.DefenderReturn=f.Number;
                        else if(defender.NativeStateId!=defender.NativeSecondaryStateId||Raw(f,d,0)!=2)bad=true;
                    }
                }else {
                    if(defender.NativeStateId==110&&Raw(f,d,1)==24)t.SawReaction=true;
                    if(t.SawReaction&&!t.DefenderReturn.HasValue){
                        if(Rest(defender)&&!defender.NativeHitstop)t.DefenderReturn=f.Number;
                        else if(defender.NativeStateId!=110)bad=true;
                    }
                }
                if(bad)pending=null;
                else if(t.AttackReturn.HasValue&&t.DefenderReturn.HasValue){
                    saved[p]=new ObservedAdvantage{AttackReturn=t.AttackReturn.Value,DefenderReturn=t.DefenderReturn.Value,Outcome=t.Outcome};pending=null;
                }
            }
        }
        for(int p=0;p<2;p++){
            var s=f.Meter.Players[p];var value=saved[p];
            // Never attach the previous attack's advantage to a newer measurement.
            s.ExperimentalAdvantage=value!=null&&s.LastAttack!=null&&s.LastAttack.Capture==value.AttackReturn?value:null;
        }
        previous=f;
    }
}
