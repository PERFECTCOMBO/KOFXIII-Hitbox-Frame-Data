using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;
internal static class SpecialSystemTests {
 [STAThread] static int Main(string[] args){try{for(int mode=0;mode<5;mode++)Replay(args[0],mode);Queue();using(var form=new SpecialQueueForm()){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-3000,-3000);form.Show();Application.DoEvents();form.PerformLayout();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,image.Width,image.Height));image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"special-queue-preview.png"));}}Console.WriteLine("PASS: special queue and execution evidence tests");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL: "+e);return 1;}}
 static void Replay(string file,int mode){var execution=new ActionExecutionTracker();var timing=new NativeTimingTracker();var adv=new ObservedAdvantageTracker();var evidence=new MoveEvidenceTracker();var saved=new HashSet<long>();var starts=new HashSet<long>();DebugFrame before=null;
  foreach(var line in File.ReadLines(file).Skip(1)){var f=AdvantageReplayTests.Parse(line);var c=f.Meter.Players[0];bool loop=MoveTransitions.InactiveLoop(before,f,0);if(mode==1&&loop)c.NativeAttack=true;if(mode==2)f.MissingBefore=1;if(mode==3&&c.NativeActionId==480)f.Readiness[1]=999;if(mode==4){if(c.NativeActionId==476)c.NativeActionId=876;if(c.NativeActionId==480)c.NativeActionId=880;}
   execution.Observe(f);timing.Observe(f);adv.Observe(f);evidence.Observe(f);if(c.LastExecution!=null&&(c.LastExecution.Action==476||c.LastExecution.Action==876))starts.Add(c.LastExecution.Capture);var m=c.LastAttack;if(m!=null&&saved.Add(m.Capture)){
    if(mode!=0&&mode!=4)throw new Exception("Corrupt/active loop produced a completed measurement");
    if(m.First!=4||m.Active!=6||m.Total!=48||c.Evidence.Damage!=0||c.Evidence.OnHit.HasValue||c.Evidence.OnGuard.HasValue)throw new Exception("Kim whiff measurement mismatch");
   }before=f;
  }
  if((mode==0||mode==4)&&(saved.Count!=2||starts.Count!=2))throw new Exception("Missing Kim whiff executions/completions: "+starts.Count+" / "+saved.Count);
  if(mode==1&&starts.Count!=2)throw new Exception("Failed timing erased move execution");Console.WriteLine("Kim replay mode="+mode+" executions="+starts.Count+" completed="+saved.Count);
 }
 static void Queue(){var db=new XmlDocument();db.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"special-moves.xml"));var moves=db.SelectNodes("/SpecialDatabase/Move").Cast<XmlElement>().ToArray();if(moves.Length!=1026||moves.Select(x=>x.GetAttribute("character")).Distinct().Count()!=36)throw new Exception("Incomplete move queue");if(!moves.Any(x=>x.GetAttribute("character")=="Kim Kaphwan"&&x.GetAttribute("input")=="[2]8BD"))throw new Exception("EX charge input lost");
  ValidationSession.Broken=false;SpecialTrial.Actions.Clear();if(SpecialTrial.Result("Whiff",true)!="NOT_EXECUTED")throw new Exception("Reference falsely passed");SpecialTrial.Actions.Add(476);SpecialTrial.Hit=false;SpecialTrial.Guard=false;if(SpecialTrial.Result("Whiff",true)!="EXECUTED_UNVERIFIED")throw new Exception("Unverified move falsely certified");SpecialTrial.Guard=true;if(SpecialTrial.Result("Whiff",true)!="WRONG_SCENARIO")throw new Exception("Whiff accepted guard");SpecialTrial.Guard=false;if(SpecialTrial.Result("Block",true)!="NO_CONTACT")throw new Exception("Unblocked attempt accepted");SpecialTrial.Actions.Clear();
 }
}

