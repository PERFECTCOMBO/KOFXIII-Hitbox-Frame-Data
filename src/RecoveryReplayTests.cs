using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Expectations come from controlled held-input trials, not workbook values.
internal static class RecoveryReplayTests {
 static int Main(string[] args){try{Run(args);return 0;}catch(Exception e){Console.Error.WriteLine("FAIL: "+e);return 1;}}
 static void Run(string[] args){
  string root=Path.GetDirectoryName(args[0]);int trials=0,held=0,negative=0;
  var files=new Dictionary<string,string[]>();
  foreach(string line in File.ReadLines(args[0]).Skip(1)){
   var c=line.Split(',');string file=Path.Combine(root,c[0]);
   if(!files.ContainsKey(file))files[file]=File.ReadAllLines(file).Skip(1).ToArray();
   long start=Int64.Parse(c[3]),end=Int64.Parse(c[4]),follow=Int64.Parse(c[6]);int expected=Int32.Parse(c[5]);
   var rows=files[file].Where(x=>{long n=Int64.Parse(x.Substring(0,x.IndexOf(',')));return n>=start-10&&n<=end+30;}).ToArray();
   if(follow!=0){if(follow!=end+1||Int64.Parse(c[7])>=end-2)throw new Exception("Invalid held-input evidence");held++;}
   foreach(bool swap in new[]{false,true}){
    var t=new NativeTimingTracker();AttackMeasurement complete=null;int p=swap?1:0;
    foreach(var row in rows){var f=AdvantageReplayTests.Parse(row);if(swap)Swap(f);t.Observe(f);var s=f.Meter.Players[p];
     if(f.Number==end){complete=s.LastAttack;if(complete==null||complete.Capture!=end||complete.Recovery!=expected)throw new Exception(c[1]+" trial "+c[2]+" expected recovery "+expected+", got "+(complete==null?"no attack":complete.Recovery.ToString()));}
     if(complete!=null&&(s.LastAttack!=complete||FrameMeterMetrics.Values(s,false)[2]!=expected.ToString()))throw new Exception("Completed recovery not retained in meter");
    }
    if(complete==null||String.IsNullOrEmpty(complete.RecoverySource))throw new Exception("Missing provenance");
   }
   // Each damaged recording must fail closed, even if the old Tail can finish.
   foreach(string mode in new[]{"gap","skip","truncated","category","command","cancel","identity","missing-start","return-category","return-command","landing-command"}){
    var t=new NativeTimingTracker();bool exposed=false;
    foreach(string row in rows){var f=AdvantageReplayTests.Parse(row);var s=f.Meter.Players[0];
     if(mode=="missing-start"&&f.Number==start)continue;
     if(f.Number==start+2){
      if(mode=="skip")continue;
      if(mode=="gap")f.MissingBefore=1;
      if(mode=="truncated")f.Truncated=true;
      if(mode=="category")f.Readiness[0]=99;
      if(mode=="command")f.Readiness[1]=999;
      if(mode=="cancel")s.NativeActionId=999;
      if(mode=="identity")s.ActorIdentity=999;
     }
     if(f.Number==end&&mode=="return-category")f.Readiness[0]=99;
     if(f.Number==end&&mode=="return-command")f.Readiness[1]=999;
     bool landingCase=c[1].Contains("landing")||c[1].Contains("whiff");
     if(mode=="landing-command"&&landingCase&&f.Number<=end&&f.Readiness[0]==0&&f.Readiness[1]==173)f.Readiness[1]=999;
     if(mode=="landing-command"&&!landingCase&&f.Number==end)f.Readiness[1]=999;
     t.Observe(f);if(s.LastAttack!=null&&s.LastAttack.Capture==end&&s.LastAttack.Recovery.HasValue)exposed=true;
    }
    if(exposed)throw new Exception("Invalid recovery accepted: "+c[1]+" "+c[2]+" "+mode);negative++;
   }
   trials++;
  }
  Console.WriteLine("PASS: "+trials+" controlled trials, mirrored player replay, retained five-field values; "+held+" held-input endpoint checks; "+negative+" corrupted-capture rejection cases.");
 }
 static void Swap(DebugFrame f){var s=f.Meter.Players[0];f.Meter.Players[0]=f.Meter.Players[1];f.Meter.Players[1]=s;for(int i=0;i<10;i++){int n=f.Readiness[i];f.Readiness[i]=f.Readiness[10+i];f.Readiness[10+i]=n;}}
}
