using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
// Edge-triggered diagnostics. The capture counter is deliberately NOT called a game frame.
internal sealed class DetectionTrace {
 readonly Queue<string> rows=new Queue<string>();readonly Action<string> save;DebugFrame previous;int sinceSave;
 internal string LastError;internal const string Header="capture,game_frame,player,character_id,actor,action,primary,secondary,native_timer,event,category,command,collision_result,hitstop,landing,actionable_frame,observed_advantage,outcome,timing_note,recovery_status";
 internal DetectionTrace(Action<string> output){save=output;}
 internal DetectionTrace():this(null){string path=Path.Combine(AppPaths.DataDirectory,"detection-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".csv");save=text=>File.WriteAllText(path,text);}
 internal void Observe(DebugFrame f){
  if(f.Mock)return;
  for(int p=0;p<2;p++){
   var c=f.Meter.Players[p];var old=previous==null?null:previous.Meter.Players[p];var events=new List<string>();
   if(old==null||old.ActorIdentity!=c.ActorIdentity)events.Add("FIGHTER_INSTANCE");
   if(old==null||old.NativeActionId!=c.NativeActionId)events.Add("ACTION");
   if(old==null||old.NativeStateId!=c.NativeStateId||old.NativeSecondaryStateId!=c.NativeSecondaryStateId)events.Add("STATE");
   if(old==null||old.NativeAttack!=c.NativeAttack)events.Add(c.NativeAttack?"ATTACK_ON":"ATTACK_OFF");
   if(old==null||old.NativeHitstop!=c.NativeHitstop)events.Add(c.NativeHitstop?"HITSTOP_START":"HITSTOP_END");
   if(f.MissingBefore>0||f.Truncated)events.Add("CAPTURE_INVALID");
   if(MoveTransitions.InactiveLoop(previous,f,p))events.Add("INACTIVE_TIMER_LOOP");
   bool landing=MoveTransitions.Landing(previous,f,p);if(landing)events.Add("LANDING_ENDPOINT");
   if(old==null||old.TimingNote!=c.TimingNote)events.Add("TIMING_STATUS");
   var adv=c.ExperimentalAdvantage;var prior=old==null?null:old.ExperimentalAdvantage;
   if(adv!=null&&(prior==null||adv.AttackReturn!=prior.AttackReturn))events.Add("OBSERVED_RETURN_GAP");
   bool raw=f.Readiness!=null&&f.Readiness.Length==20;
   if(raw&&previous!=null&&previous.Readiness!=null&&previous.Readiness.Length==20&&f.Readiness[p*10+2]!=previous.Readiness[p*10+2])events.Add("COLLISION_RESULT_CHANGE");
   if(events.Count==0)continue;
   var values=new[]{f.Number.ToString(),f.Meter.GameFrame.HasValue?f.Meter.GameFrame.ToString():"UNVERIFIED",(p+1).ToString(),"UNMAPPED",c.ActorIdentity.ToString(),c.NativeActionId.ToString(),c.NativeStateId.ToString(),c.NativeSecondaryStateId.ToString(),c.NativeActionTime.ToString(),String.Join("+",events),raw?f.Readiness[p*10].ToString():"",raw?f.Readiness[p*10+1].ToString():"",raw?f.Readiness[p*10+2].ToString():"",c.NativeHitstop.ToString(),landing.ToString(),c.ActionableFrame.HasValue?c.ActionableFrame.ToString():"UNVERIFIED",adv==null?"":adv.Gap.ToString(),adv==null?"":adv.Outcome,c.TimingNote,FrameMeterMetrics.RecoveryStatus(c)};
   for(int i=0;i<values.Length;i++)values[i]=RosterValidation.Csv(values[i]);rows.Enqueue(String.Join(",",values));while(rows.Count>20000)rows.Dequeue();
  }
  previous=f;if(++sinceSave>=600)Flush();
 }
 internal void Flush(){try{var b=new StringBuilder(Header).AppendLine();foreach(var row in rows)b.AppendLine(row);save(b.ToString());LastError=null;}catch(Exception ex){LastError=ex.Message;DebugLog.Write("Detection trace: "+LastError);}sinceSave=0;}
}

