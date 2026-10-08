using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
internal static class SpecialReplayTests {
    static void Run(string path,int mode){
        var timing=new NativeTimingTracker();var adv=new ObservedAdvantageTracker();var seen=new HashSet<long>();int light=0,heavy=0;
        foreach(string line in File.ReadLines(path).Skip(1)){
            var f=AdvantageReplayTests.Parse(line);var s=f.Meter.Players[0];
            if(mode==1)f.MissingBefore=1;
            if(mode==2&&(s.NativeActionId==479||s.NativeActionId==481))f.Readiness[1]=999;
            timing.Observe(f);adv.Observe(f);
            if(s.LastAttack==null||s.ExperimentalAdvantage==null||!seen.Add(s.LastAttack.Capture))continue;
            int action=s.LastAttack.Action;long gap=s.ExperimentalAdvantage.Gap;
            if(action==478||action==480){
                Console.WriteLine("mode="+mode+" action="+action+" return="+f.Number+" first="+s.LastAttack.First+" active="+s.LastAttack.Active+" tail="+s.LastAttack.Tail+" total="+s.LastAttack.Total+" gap="+gap);
                if(mode!=0)throw new Exception("Unsupported/gapped transition measured");
                if(action==478){if(gap!=-30||s.LastAttack.First!=4||s.LastAttack.Active!=9)throw new Exception("LP DP mismatch");light++;}
                if(action==480){if(gap!=-43||s.LastAttack.First!=7||s.LastAttack.Active!=15)throw new Exception("HP DP mismatch");heavy++;}
            }
        }
        if(mode==0&&(light<2||heavy<2))throw new Exception("Missing complete DP samples: "+light+" / "+heavy);
    }
    static int Main(string[] args){try{RunMain(args);return 0;}catch(Exception e){Console.Error.WriteLine("FAIL: "+e.Message);return 1;}}
    static void RunMain(string[] args){Run(args[0],0);Run(args[0],1);Run(args[0],2);Console.WriteLine("PASS: recorded multi-contact DPs, landing transitions, gaps and invalid landing-command rejection");}
}
