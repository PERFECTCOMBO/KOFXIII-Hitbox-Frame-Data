using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
internal static class GuardReleaseTests {
 static int Main(string[] args){try{for(int mode=0;mode<4;mode++)Run(args[0],mode);return 0;}catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
 static void Run(string file,int mode){var t=new NativeTimingTracker();var a=new ObservedAdvantageTracker();var e=new MoveEvidenceTracker();var seen=new HashSet<long>();foreach(var line in File.ReadLines(file).Skip(1)){var f=AdvantageReplayTests.Parse(line);if(mode==1&&f.Meter.Players[1].NativeSecondaryStateId==73)f.Meter.Players[1].NativeSecondaryStateId=999;if(mode==2)f.MissingBefore=1;if(mode==3&&f.Readiness[10]==2)f.Readiness[10]=0;t.Observe(f);a.Observe(f);e.Observe(f);var c=f.Meter.Players[0];if(c.LastAttack!=null&&c.LastAttack.Action==75&&c.ExperimentalAdvantage!=null&&seen.Add(c.LastAttack.Capture)){if(c.ExperimentalAdvantage.Outcome!="block"||c.ExperimentalAdvantage.Gap!=0||c.Evidence.OnGuard!=0)throw new Exception("Unexpected Kim result");}if(c.ActionableFrame.HasValue)throw new Exception("Certified readiness was invented");}if(seen.Count!=(mode==0?9:0))throw new Exception("Kim release mode "+mode+": "+seen.Count);Console.WriteLine("PASS Kim guard release mode="+mode+" results="+seen.Count);}
}
