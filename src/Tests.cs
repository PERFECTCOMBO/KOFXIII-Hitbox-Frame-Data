using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
internal static class DebuggerTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    internal static void Run(){string dir=AppDomain.CurrentDomain.BaseDirectory;
        try{
            RingBufferTests.Run();FrameMeterTests.Run();NativeTimingTests.Run();ReadinessTests.Run();
            using(var mock=new MockProvider()){
                var history=new FrameHistory();bool sawAttack=false,sawThrow=false,sawProjectile=false,sawInvulnerability=false,sawOverlap=false;
                var outcomes=new System.Collections.Generic.HashSet<string>();
                for(int i=0;i<420;i++){var f=mock.Poll();Check(f.Number==i&&f.Mock,"Mock frame sequence");history.Add(f);if(f.MockOutcome!=null)outcomes.Add(f.MockOutcome);foreach(var region in f.Boxes){sawAttack|=region.Kind==1;sawThrow|=region.Kind==5;sawProjectile|=region.Kind==6;sawInvulnerability|=region.Kind==8;}sawOverlap|=CollisionAnalysis.Overlaps(f).Count>0;}
                Check(outcomes.Contains("MOCK HIT")&&outcomes.Contains("MOCK WHIFF")&&outcomes.Contains("MOCK BLOCK")&&outcomes.Contains("MOCK TRADE")&&outcomes.Contains("MOCK THROW"),"Mock outcome scenarios");
                Check(sawAttack&&sawThrow&&sawProjectile&&sawInvulnerability&&sawOverlap,"Mock coverage / overlap");
                history.Select(20);Check(history.Current.Number==20,"Scrub");history.Select(19);Check(history.Current.Number==19,"Previous");history.Select(20);Check(history.Current.Number==20,"Next");
                for(int i=0;i<1900;i++)history.Add(mock.Poll());Check(history.Frames.Count==FrameHistory.Capacity,"Bounded history");
            }
            var a=new Box{Left=0,Bottom=0,Width=10,Height=10};var b=new Box{Left=10,Bottom=0,Width=10,Height=10};Check(!CollisionAnalysis.Intersects(a,b),"Edge contact is not overlap");b.Left=9;Check(CollisionAnalysis.Intersects(a,b),"Overlap");
            var live=new DebugFrame();Check(FrameDataPanel.Describe(live,null).Contains(FrameDataPanel.NA),"Unavailable fields");Check(!CollisionAnalysis.Event(live).Contains("MOCK HIT"),"No fabricated live outcome");
            using(var provider=new MockProvider())using(var canvas=new SceneCanvas(new ViewerSettings{Grid=true,Coordinates=true})){DebugFrame frame=null;for(int i=0;i<15;i++)frame=provider.Poll();canvas.Frame=frame;canvas.Size=new Size(960,540);using(var bitmap=new Bitmap(960,540)){canvas.DrawToBitmap(bitmap,new Rectangle(0,0,960,540));bitmap.Save(Path.Combine(dir,"mock-preview.png"));}}
            using(var window=new DebuggerWindow(false)){window.StartPosition=System.Windows.Forms.FormStartPosition.Manual;window.Location=new Point(-30000,-30000);window.Show();System.Windows.Forms.Application.DoEvents();window.PreparePreview();System.Windows.Forms.Application.DoEvents();using(var bitmap=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(bitmap,new Rectangle(0,0,window.Width,window.Height));bitmap.Save(Path.Combine(dir,"debugger-preview.png"));}window.Close();}
            using(var demo=new MockProvider()){
                var h=new FrameHistory();for(int i=0;i<19;i++)h.Add(demo.Poll());
                using(var bitmap=new Bitmap(1200,190))using(var graphics=Graphics.FromImage(bitmap)){
                    FrameMeterRenderer.Draw(graphics,new Rectangle(0,0,1200,190),FrameTracker.Window(h,45),new ViewerSettings());bitmap.Save(Path.Combine(dir,"frame-meter-preview.png"));
                }
            }
            File.WriteAllBytes(Path.Combine(dir,"capture-stub.bin"),Assembler.Capture(0x10000000,0x10010000,0x5D8AB0));
            File.WriteAllText(Path.Combine(dir,"test-results.txt"),"PASS: ring buffering/order/overrun/wrap/torn-read, frame-meter phases/advantage/hitstop/multi-hit, mock categories, frame sequence, overlap checks, bounded history, previous/next/scrub, unavailable live fields, mock scene rendering.\r\nLive game alignment / game pause / every-frame capture are not verified by these tests.\r\n");
        }catch(Exception ex){File.WriteAllText(Path.Combine(dir,"test-results.txt"),"FAIL: "+ex);Environment.ExitCode=1;}
    }
}


