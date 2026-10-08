using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
internal static class ReplayReport {
 static void Main(string[] args){
  var timing=new NativeTimingTracker();var advantage=new ObservedAdvantageTracker();var evidence=new MoveEvidenceTracker();var records=new SortedDictionary<long,string>();
  var trace=new DetectionTrace(text=>File.WriteAllText(args[2]+"-events.csv",text));
  foreach(string line in File.ReadLines(args[0]).Skip(1)){
   var f=AdvantageReplayTests.Parse(line);timing.Observe(f);advantage.Observe(f);evidence.Observe(f);trace.Observe(f);var c=f.Meter.Players[0];var m=c.LastAttack;if(m==null)continue;
   var vals=SevenMetrics.Values(c,false);var columns=new List<string>{args[1],"USER_LABELLED_RECORDING",m.Capture.ToString(),c.ActorIdentity.ToString(),m.Action.ToString(),"REFERENCE_MOVE_UNMAPPED"};columns.AddRange(vals);columns.Add(m.Tail.ToString());columns.Add(m.Total.ToString());columns.Add(m.ActivePattern);columns.Add("OBSERVED_NATIVE_TIMING_NOT_CERTIFIED_ACTIONABILITY");columns.Add(args[0]);
   records[m.Capture]=String.Join(",",columns.Select(RosterValidation.Csv));
  }
  trace.Flush();File.WriteAllLines(args[2]+"-measurements.csv",new[]{"Character,CharacterSource,Capture,Actor,NativeAction,ReferenceMapping,Startup,Active,Recovery,OnHit,OnGuard,Damage,Stun,UnverifiedTail,NativeTotal,ActivePattern,Status,Trace"}.Concat(records.Values));Console.WriteLine(args[1]+": "+records.Count+" completed measurements exported; reference mappings not asserted.");
 }
}
