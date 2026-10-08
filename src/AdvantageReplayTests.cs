using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
internal static class AdvantageReplayTests {
    internal static DebugFrame Parse(string line){
        var n=line.Split(',').Select(x=>x==""?0:Int64.Parse(x)).ToArray();var states=new int[12];var boxes=new List<Box>();var f=new DebugFrame{Number=n[0],MissingBefore=(int)n[1],Truncated=n[2]!=0,Readiness=new int[20]};
        for(int p=0;p<2;p++){int o=3+p*17;states[p*6]=(int)n[o+1];states[p*6+1]=(int)n[o+2];states[p*6+2]=n[o+6]!=0?8:0;states[p*6+3]=(int)n[o+3];states[p*6+4]=0;states[p*6+5]=(int)n[o+4];if(n[o+5]!=0)boxes.Add(new Box{Group=p*5+1});for(int i=0;i<10;i++)f.Readiness[p*10+i]=(int)n[o+7+i];}
        f.Boxes=boxes;f.Meter=GameStateReader.FromNative(states,boxes);f.Meter.Players[0].ActorIdentity=(uint)n[3];f.Meter.Players[1].ActorIdentity=(uint)n[20];return f;
    }
    static int Replay(string file,string outcome,int expected,bool corrupt,bool swap){
        var timing=new NativeTimingTracker();var tracker=new ObservedAdvantageTracker();var seen=new HashSet<long>();var history=new FrameHistory();int count=0;
        foreach(string line in File.ReadLines(file).Skip(1)){
            var f=Parse(line);
            if(swap){var c=f.Meter.Players[0];f.Meter.Players[0]=f.Meter.Players[1];f.Meter.Players[1]=c;for(int i=0;i<10;i++){int v=f.Readiness[i];f.Readiness[i]=f.Readiness[10+i];f.Readiness[10+i]=v;}}
            if(corrupt)f.MissingBefore=1;
            timing.Observe(f);tracker.Observe(f);history.Add(f);
            var a=f.Meter.Players[swap?1:0].ExperimentalAdvantage;
            if(a!=null&&a.Outcome==outcome&&seen.Add(a.AttackReturn)){if(a.Gap!=expected)throw new Exception("Unexpected measured gap "+a.Gap);count++;}
            if(f.Meter.GameFrame.HasValue||f.Meter.Players[0].ActionableFrame.HasValue)throw new Exception("Experimental data polluted certified timing");
            if(a!=null&&outcome=="hit"&&!swap&&!corrupt&&count==1){using(var b=new Bitmap(448,82))using(var g=Graphics.FromImage(b)){new CompactMeter().Draw(g,new Rectangle(0,0,448,82),FrameTracker.Window(history,45),new ViewerSettings());b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"r5-advantage-preview.png"));}}
        }
        if(corrupt&&count!=0)throw new Exception("Gap produced advantage");
        if(!corrupt&&count<10)throw new Exception("Too few measured interactions: "+count);
        Console.WriteLine(outcome+" swap="+swap+" gaps="+corrupt+" measured="+count);return count;
    }
    static int Main(string[] args){try{RunMain(args);return 0;}catch(Exception e){Console.Error.WriteLine("FAIL: "+e.Message);return 1;}}
    static void RunMain(string[] args){
        Replay(args[0],"block",1,false,false);Replay(args[1],"hit",3,false,false);
        Replay(args[0],"block",1,false,true);Replay(args[1],"hit",3,false,true);
        Replay(args[0],"block",1,true,false);Replay(args[1],"hit",3,true,false);
        Console.WriteLine("PASS: real recordings, mirrored sides, gaps, separation from certified fields");
    }
}
