using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
internal static class CrouchReplayCheck {
    static void Main(string[] args){var tracker=new NativeTimingTracker();var history=new FrameHistory();CharacterState prev=null;int standing=0,crouching=0;
        foreach(var line in File.ReadAllLines(args[0])){var n=line.Split(',').Select(Int32.Parse).ToArray();var boxes=new List<Box>();if(n[13]!=0)boxes.Add(new Box{Group=1});if(n[14]!=0)boxes.Add(new Box{Group=6});var f=new DebugFrame{Number=n[0],Meter=GameStateReader.FromNative(n.Skip(1).Take(12).ToArray(),boxes)};
            f.Meter.Players[0].ActorIdentity=(uint)n[15];f.Meter.Players[1].ActorIdentity=(uint)n[16];tracker.Observe(f);history.Add(f);var s=f.Meter.Players[0];
            if(prev!=null&&prev.TimingInProgress&&!s.TimingInProgress&&s.ObservedTotalTicks.HasValue){
                if(s.ObservedActionId==71){crouching++;if(s.ObservedFirstActive!=5||s.ObservedActiveTicks!=4||s.ObservedTailTicks!=7||s.ObservedTotalTicks!=15)throw new Exception("Crouching regression: "+TimingPresentation.Summary(s,0,false));
                    if(args.Length>1)using(var bmp=new Bitmap(1200,148))using(var g=Graphics.FromImage(bmp)){FrameMeterRenderer.Draw(g,new Rectangle(0,0,1200,148),FrameTracker.Window(history,45),new ViewerSettings());bmp.Save(args[1]);}
                }else if(s.ObservedActionId==68){standing++;if(s.ObservedFirstActive!=4||s.ObservedActiveTicks!=4||s.ObservedTailTicks!=6||s.ObservedTotalTicks!=13)throw new Exception("Standing regression");}
            }prev=s;
        }
        Console.WriteLine("Recorded updates="+history.Frames.Count+" standing completions="+standing+" crouching completions="+crouching);
        if(standing==0||args.Length>1&&crouching==0)throw new Exception("Missing expected recorded coverage");
    }
}
