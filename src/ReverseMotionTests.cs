using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
internal static class ReverseMotionTests {
    static void Run(string path,bool corrupt){
        var timing=new NativeTimingTracker();var tracker=new ObservedAdvantageTracker();var seen=new HashSet<long>();var gaps=new HashSet<long>();int light=0,heavy=0;
        foreach(string line in File.ReadLines(path).Skip(1)){
            var f=AdvantageReplayTests.Parse(line);var s=f.Meter.Players[0];
            if(corrupt&&(s.NativeActionId==491||s.NativeActionId==493))f.Readiness[1]=999;
            timing.Observe(f);tracker.Observe(f);var m=s.LastAttack;
            if(m!=null&&(m.Action==490||m.Action==492)&&seen.Add(m.Capture)){
                if(corrupt)throw new Exception("Unknown landing accepted");
                Console.WriteLine("action="+m.Action+" first="+m.First+" active="+m.Active+" tail="+m.Tail+" total="+m.Total+" capture="+m.Capture);
                if(m.Action==490){if(m.First!=10||m.Active!=5||m.Tail!=23||m.Total!=37)throw new Exception("Timing mismatch");light++;}else {if(m.First!=13||m.Active!=11||m.Tail!=39||m.Total!=92)throw new Exception("Looping heavy variant mismatch");heavy++;}
            }
            if(m!=null&&(m.Action==490||m.Action==492)&&s.ExperimentalAdvantage!=null&&gaps.Add(m.Capture)){if(s.ExperimentalAdvantage.Gap!=(m.Action==490?-11:-63)||s.ExperimentalAdvantage.Outcome!="block")throw new Exception("Block gap mismatch");Console.WriteLine("Adv="+s.ExperimentalAdvantage.Gap+" "+s.ExperimentalAdvantage.Outcome);}
        }
        if(!corrupt&&(light!=7||heavy!=1||gaps.Count!=4))throw new Exception("Missing recorded move completions");
    }
    static int Main(string[] args){try{RunMain(args);return 0;}catch(Exception e){Console.Error.WriteLine("FAIL: "+e.Message);return 1;}}
    static void RunMain(string[] args){Run(args[0],false);Run(args[0],true);Console.WriteLine("PASS: recorded reverse-motion landing chains and invalid landing-command rejection");}
}
