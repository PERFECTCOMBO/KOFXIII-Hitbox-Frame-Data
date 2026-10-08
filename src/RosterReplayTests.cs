using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
internal static class RosterReplayTests {
 static int Main(string[] args){try{Run(args);return 0;}catch(Exception e){Console.Error.WriteLine("FAIL: "+e.Message);return 1;}}
 static void Run(string[] args){
  string label=args.Length>2?args[2]:"Ryo";var t=new NativeTimingTracker();var a=new ObservedAdvantageTracker();var e=new MoveEvidenceTracker();var complete=new HashSet<long>();var gaps=new HashSet<long>();int newAction=0,results=0,blockResults=0;var counts=new Dictionary<int,int>();
  foreach(var line in File.ReadLines(args[0]).Skip(1)){
   var f=AdvantageReplayTests.Parse(line);if(f.Number<31438)continue;
   if(args.Length>1&&args[1]=="badguard"&&f.Readiness[10]==2)f.Readiness[10]=0;
   if(args.Length>1&&args[1]=="gaps")f.MissingBefore=1;
   if(args.Length>1&&args[1]=="badlanding"&&f.Meter.Players[0].NativeActionId==8)f.Readiness[1]=999;
   t.Observe(f);a.Observe(f);e.Observe(f);var c=f.Meter.Players[0];var m=c.LastAttack;
   if(m!=null&&complete.Add(m.Capture)){if(!counts.ContainsKey(m.Action))counts[m.Action]=0;counts[m.Action]++;if(m.Action==73)newAction++;}
   if(c.ExperimentalAdvantage!=null&&gaps.Add(c.ExperimentalAdvantage.AttackReturn)){results++;if(c.ExperimentalAdvantage.Outcome=="block")blockResults++;Console.WriteLine(label+" native action="+m.Action+" return="+m.Capture+" outcome="+c.ExperimentalAdvantage.Outcome+" gap="+c.ExperimentalAdvantage.Gap);}
   if(c.ActionableFrame.HasValue||f.Meter.GameFrame.HasValue)throw new Exception("Unverified actionable clock exposed");
  }
  foreach(var kv in counts)Console.WriteLine("Completed action="+kv.Key+" count="+kv.Value);
  Console.WriteLine("Missing-in-R9 action 73: "+newAction+"; observed results="+results);
  if(args.Length>1&&(args[1]=="gaps"||args[1]=="badlanding")&&newAction!=0)throw new Exception("Invalid landing accepted");
  if(args.Length>1&&args[1]=="badguard"&&blockResults!=0)throw new Exception("Non-guard reaction accepted");
  if(args.Length==1&&(newAction<10||results<10))throw new Exception("Ryo repair failed");
 }
}
