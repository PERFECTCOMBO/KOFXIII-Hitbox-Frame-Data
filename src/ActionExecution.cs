using System;
internal sealed class ActionExecution {
 internal long Capture;internal int Action,Category,Command;internal uint Actor;
}
// Execution evidence does not depend on contact, boxes, or completion of timing.
internal sealed class ActionExecutionTracker {
 DebugFrame previous;readonly ActionExecution[] last=new ActionExecution[2];
 internal void Observe(DebugFrame f){
  for(int p=0;p<2;p++){
   var c=f.Meter.Players[p];var old=previous==null?null:previous.Meter.Players[p];
   if(!c.ActorIdentity.HasValue||c.ActorIdentity.Value==0||old==null||old.ActorIdentity!=c.ActorIdentity)last[p]=null;
   bool raw=!f.Mock&&f.Readiness!=null&&f.Readiness.Length==20;
   bool contiguous=previous!=null&&f.Number==previous.Number+1&&f.MissingBefore==0&&!f.Truncated;
   if(raw&&contiguous&&old.ActorIdentity==c.ActorIdentity&&c.ActorIdentity.GetValueOrDefault()!=0&&c.NativeActionTime==1&&(c.NativeActionId!=old.NativeActionId||old.NativeActionTime!=1)&&c.NativeActionId.HasValue&&(f.Readiness[p*10]==1||f.Readiness[p*10]==3)){
    last[p]=new ActionExecution{Capture=f.Number,Action=c.NativeActionId.Value,Category=f.Readiness[p*10],Command=f.Readiness[p*10+1],Actor=c.ActorIdentity.Value};
   }
   c.LastExecution=last[p];
  }
  previous=f;
 }
}
