using System;
// Visible meter contract. Internal seven-field evidence remains available to
// contact detection and reference comparisons, but is not a display contract.
internal static class FrameMeterMetrics {
    internal static readonly string[] Headers={"Startup*","Active*","Recovery","OnHit*","OnGuard*"};
    internal static string[] Values(CharacterState s,bool mock){
        var all=SevenMetrics.Values(s,mock);
        return new[]{all[0],all[1],all[2],all[3],all[4]};
    }
    internal static string RecoveryStatus(CharacterState s){
        if(s.LastAttack!=null&&s.LastAttack.Recovery.HasValue)return "Recovery "+s.LastAttack.Recovery.Value+"F: "+s.LastAttack.RecoverySource;
        if(s.LastAttack!=null)return "Recovery unavailable: last action completed at a stance marker; first actionable frame is unverified.";
        if(s.TimingInProgress)return "Recovery pending: action still being tracked.";
        if(s.LastExecution!=null)return "Recovery unavailable: execution captured without complete conventional attack timing.";
        return "Recovery unavailable: no completed attack captured.";
    }
}
