using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
// Standalone regression entry point; never attaches to the game.
internal static class LiveReplayCheck {
    static void Main(string[] args){string root=args.Length>0?args[0]:AppDomain.CurrentDomain.BaseDirectory;var track=new NativeTimingTracker();var history=new FrameHistory();int complete=0,attacks=0,stop=0;CharacterState previous=null;
        foreach(var line in File.ReadAllLines(Path.Combine(root,"live-check.csv"))){var n=line.Split(',').Select(Int32.Parse).ToArray();var boxes=new List<Box>();if(n[13]!=0)boxes.Add(new Box{Group=1});if(n[14]!=0)boxes.Add(new Box{Group=6});var f=new DebugFrame{Number=n[0],Meter=GameStateReader.FromNative(n.Skip(1).Take(12).ToArray(),boxes)};
            f.Meter.Players[0].ActorIdentity=(uint)n[15];f.Meter.Players[1].ActorIdentity=(uint)n[16];track.Observe(f);history.Add(f);var c=f.Meter.Players[0];if(c.NativeAttack)attacks++;if(c.NativeHitstop)stop++;
            if(previous!=null&&previous.NativeNeutral==false&&c.NativeNeutral==true&&c.ObservedTotalTicks.HasValue){complete++;Console.WriteLine(TimingPresentation.Summary(c,0,false));using(var bmp=new Bitmap(1200,148))using(var g=Graphics.FromImage(bmp)){FrameMeterRenderer.Draw(g,new Rectangle(0,0,1200,148),FrameTracker.Window(history,45),new ViewerSettings());bmp.Save(Path.Combine(root,"repair-live-preview.png"));}}
            previous=c;
        }
        if(complete==0||attacks==0||stop==0)throw new Exception("Fresh live recording did not exercise complete attacks and hitstop");
        Console.WriteLine("PASS: "+history.Frames.Count+" fresh live records; "+complete+" complete measurements; "+attacks+" attack-region samples; "+stop+" hitstop samples. No spreadsheet values used.");
    }
}
