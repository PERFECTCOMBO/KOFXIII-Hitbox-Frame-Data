using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
internal static class JumpReplayTests {
    static int Main(string[] args){try{RunMain(args);return 0;}catch(Exception e){Console.Error.WriteLine("FAIL: "+e.Message);return 1;}}
    static void RunMain(string[] args){
        var timing=new NativeTimingTracker();var adv=new ObservedAdvantageTracker();var seen=new HashSet<long>();var gaps=new HashSet<long>();int lows=0,jumps=0;var measured=new HashSet<int>();var air=new HashSet<int>(new[]{69,74,79,80,85,86,89,90,503});
        foreach(string line in File.ReadLines(args[0]).Skip(1)){
            var f=AdvantageReplayTests.Parse(line);if(args.Length>2&&f.Meter.Players[0].NativeActionId==8)f.Meter.Players[0].NativeActionId=999;timing.Observe(f);adv.Observe(f);var s=f.Meter.Players[0];var m=s.LastAttack;
            if(m==null)continue;
            if(args.Length>2&&air.Contains(m.Action))throw new Exception("Unknown landing measured");
            // These specific attempts have no hit/block collision in this fixture. Other attempts with the same action ID were guarded.
            if(args.Length>=2&&(m.Capture==15052||m.Capture==15289)&&s.ExperimentalAdvantage!=null)throw new Exception("Whiff inherited stale advantage");
            if(seen.Add(m.Capture)&&(m.Action==75||air.Contains(m.Action))){Console.WriteLine("move="+m.Action+" first="+m.First+" active="+m.Active+" tail="+m.Tail+" total="+m.Total+" return="+m.Capture);if(m.Action!=75){jumps++;measured.Add(m.Action);}}
            if(s.ExperimentalAdvantage!=null&&gaps.Add(m.Capture)){
                Console.WriteLine("Adv move="+m.Action+" gap="+s.ExperimentalAdvantage.Gap+" "+s.ExperimentalAdvantage.Outcome);
                if(m.Action==75){if(s.ExperimentalAdvantage.Gap!=1)throw new Exception("Crouching LK return gap mismatch");lows++;}
            }
        }
        if(args.Length==2&&!air.SetEquals(measured))throw new Exception("Missing jump variants: "+String.Join(",",air.Except(measured)));
        if(args.Length==1&&(lows<3||jumps!=2))throw new Exception("Missing jump/low recordings");
    }
}
