internal static class MoveTransitions {
    // Shared semantic landing and internal-loop guards; no character-name lookup.
    // Do not treat arbitrary animation changes or cancels as recovery continuations.
    internal static bool Landing(DebugFrame before,DebugFrame after,int p){
        if(before==null||after==null||before.Readiness==null||after.Readiness==null||before.Readiness.Length!=20||after.Readiness.Length!=20)return false;
        var a=before.Meter.Players[p];var b=after.Meter.Players[p];int o=p*10;
        if(!a.ActorIdentity.HasValue||a.ActorIdentity.Value==0||a.ActorIdentity!=b.ActorIdentity||b.NativeActionTime!=1||after.Readiness[o]!=0||after.Readiness[o+1]!=173)return false;
        // Common normal-attack landing endpoint, observed in both Kyo and Ryo.
        // Source action/state IDs are animation-specific and must not be a Kyo whitelist.
        // Keep endpoint/category/command guards: arbitrary cancels are NOT continuations.
        if(before.Readiness[o]==1&&b.NativeActionId==8&&b.NativeStateId==37
            &&!b.NativeAttack&&!b.NativeHitstop)return true;
        // Command 173/category 0 is the common landing endpoint. The landing
        // animation index is character-specific (Kyo 479/481; Kim 480/481).
        return before.Readiness[o]==3 && !b.NativeAttack && !b.NativeHitstop
            && a.NativeActionId!=b.NativeActionId;
    }
    internal static bool InactiveLoop(DebugFrame before,DebugFrame after,int p){
        if(before==null||after==null||before.Readiness==null||after.Readiness==null||before.Readiness.Length!=20||after.Readiness.Length!=20)return false;
        var a=before.Meter.Players[p];var b=after.Meter.Players[p];int o=p*10;
        return a.ActorIdentity.HasValue&&a.ActorIdentity==b.ActorIdentity&&a.NativeActionId==b.NativeActionId
            &&a.NativeStateId==b.NativeStateId&&a.NativeSecondaryStateId==b.NativeSecondaryStateId
            &&before.Readiness[o]==3&&after.Readiness[o]==3&&before.Readiness[o+1]==after.Readiness[o+1]
            &&b.NativeActionTime>1&&b.NativeActionTime<a.NativeActionTime
            &&!a.NativeAttack&&!b.NativeAttack&&!a.NativeHitstop&&!b.NativeHitstop
            &&after.MissingBefore==0&&!after.Truncated&&after.Number==before.Number+1;
    }
}
