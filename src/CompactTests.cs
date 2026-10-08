using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
internal static class CompactTests {
    static void Main(){NativeTimingTests.Run();var tracker=new NativeTimingTracker();var history=new FrameHistory();var hud=new CompactMeter();var settings=new ViewerSettings();string dir=AppDomain.CurrentDomain.BaseDirectory;
        foreach(var line in File.ReadAllLines(Path.Combine(dir,"live-check.csv"))){var n=line.Split(',').Select(Int32.Parse).ToArray();var boxes=new List<Box>();if(n[13]!=0)boxes.Add(new Box{Group=1});if(n[14]!=0)boxes.Add(new Box{Group=6});var f=new DebugFrame{Number=n[0],Meter=GameStateReader.FromNative(n.Skip(1).Take(12).ToArray(),boxes)};tracker.Observe(f);history.Add(f);using(var b=new Bitmap(448,82))using(var g=Graphics.FromImage(b))hud.Draw(g,new Rectangle(0,0,448,82),FrameTracker.Window(history,45),settings);}
        foreach(int width in new[]{960,1280,1920}){int height=width*9/16;var r=CompactMeter.Layout(width,height,settings);if(r.Left<width*.32||r.Right>width*.68||r.Bottom>height||r.Height>height*.14)throw new Exception("Compact HUD overlaps power bar bounds");
            using(var b=new Bitmap(width,height))using(var g=Graphics.FromImage(b)){g.Clear(Color.FromArgb(55,55,55));g.FillRectangle(Brushes.DarkGreen,0,height*.88f,width*.318f,height*.06f);g.FillRectangle(Brushes.DarkGreen,width*.684f,height*.88f,width*.316f,height*.06f);hud.Draw(g,r,FrameTracker.Window(history,45),settings);b.Save(Path.Combine(dir,"compact-layout-"+width+".png"));}
        }
        using(var b=new Bitmap(448,82))using(var g=Graphics.FromImage(b)){hud.Draw(g,new Rectangle(0,0,448,82),FrameTracker.Window(history,45),settings);b.Save(Path.Combine(dir,"compact-preview.png"));}
        File.WriteAllText(Path.Combine(dir,"compact-test-results.txt"),"PASS: saved/current measurements separated; walking, missing captures, menu and replacement checks; real-recording replay; dock bounds at 960/1280/1920. Live R3 overlay placement awaits user confirmation.");
    }
}
