using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
internal static class RecoveryAudit {
 static int Main(string[] args){try{Run(args);return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
 static void Run(string[] args){
  if(FrameMeterMetrics.Headers.Length!=5||String.Join("|",FrameMeterMetrics.Headers)!="Startup*|Active*|Recovery|OnHit*|OnGuard*")throw new Exception("Incorrect visible columns");
  var t=new NativeTimingTracker();var a=new ObservedAdvantageTracker();var e=new MoveEvidenceTracker();var x=new ActionExecutionTracker();var h=new FrameHistory();
  var rows=new SortedDictionary<long,string>();var trace=new DetectionTrace(text=>File.WriteAllText(args[2]+"-events.csv",text));bool rendered=false;
  foreach(var line in File.ReadLines(args[0]).Skip(1)){
   var f=AdvantageReplayTests.Parse(line);x.Observe(f);t.Observe(f);a.Observe(f);e.Observe(f);h.Add(f);trace.Observe(f);var c=f.Meter.Players[0];var m=c.LastAttack;
   if(FrameMeterMetrics.Values(c,false).Length!=5)throw new Exception("Display contract broken");
   if(m==null)continue;
   if(m.Recovery.HasValue&&String.IsNullOrEmpty(m.RecoverySource))throw new Exception("Unsourced recovery");
   var ev=c.Evidence;string contact=ev==null?"UNKNOWN":ev.OnHit.HasValue?"OBSERVED_HIT":ev.OnGuard.HasValue?"OBSERVED_GUARD":ev.Damage==0?"NO_OBSERVED_CONTACT":"UNRESOLVED_CONTACT";
   rows[m.Capture]=String.Join(",",new[]{args[1],m.Capture.ToString(),m.Action.ToString(),contact,Math.Max(0,m.First-1).ToString(),m.Active.ToString(),m.Tail.ToString(),m.Total.ToString(),m.Recovery.HasValue?"TESTED_RETURN_ENDPOINT":"UNVERIFIED_ACTIONABILITY",FrameMeterMetrics.Values(c,false)[2],FrameMeterMetrics.RecoveryStatus(c)}.Select(RosterValidation.Csv));
   if(!rendered&&contact=="NO_OBSERVED_CONTACT"){
    foreach(int width in new[]{960,1280,1920}){int height=width*9/16;var rect=CompactMeter.Layout(width,height,new ViewerSettings());using(var b=new Bitmap(rect.Width,rect.Height))using(var g=Graphics.FromImage(b)){new CompactMeter().Draw(g,new Rectangle(0,0,b.Width,b.Height),FrameTracker.Window(h,45),new ViewerSettings());b.Save(args[2]+"-meter-"+width+".png");}}
    rendered=true;
   }
  }
  trace.Flush();File.WriteAllLines(args[2]+"-recovery.csv",new[]{"Character,Capture,NativeAction,ContactEvidence,ObservedStartup,ObservedActive,UnverifiedTail,ObservedTotal,RecoveryStatus,Recovery,Reason"}.Concat(rows.Values));
  Console.WriteLine(args[1]+": "+rows.Count+" retained completions; recovery provenance and layout assertions passed.");
 }
}
