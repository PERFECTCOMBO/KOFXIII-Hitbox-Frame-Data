using System;
internal static class TimingPresentation {
    internal static string N(int? n){return n.HasValue?n.Value.ToString():"--";}
    internal static string Summary(CharacterState s,int p,bool mock){
        if(mock)return "P"+(p+1)+" MOCK  Startup "+N(s.StartupFrames)+"  Active "+N(s.ActiveFrames)+"  Recovery "+N(s.RecoveryFrames)+"  Total "+N(s.TotalFrames);
        if(s.LastAttack!=null)return CompactMeter.Summary(s,p,false);
        return "P"+(p+1)+"  Action "+N(s.NativeActionId)+" @"+N(s.NativeActionTime)+" | "+(s.TimingInProgress?"Measuring":"Last #"+N(s.ObservedActionId))+"  First attack "+N(s.ObservedFirstActive)+"  Active "+N(s.ObservedActiveTicks)+"  Tail "+N(s.ObservedTailTicks)+"  Total "+N(s.ObservedTotalTicks);
    }
}
