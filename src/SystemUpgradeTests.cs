using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Xml;
internal static class SystemUpgradeTests {
    static DebugFrame Sample(long n,int health,bool running,bool attack){
        var f=new DebugFrame{Number=n,Readiness=new int[20]};
        f.Readiness[4]=1000;f.Readiness[14]=health;f.Readiness[2]=2;
        for(int p=0;p<2;p++)f.Meter.Players[p].ActorIdentity=(uint)(p+1);
        var c=f.Meter.Players[0];c.TimingInProgress=running;c.NativeActionId=71;c.NativeActionTime=(int)n;c.NativeAttack=attack;
        return f;
    }
    static void SafetyChecks(){
        for(int mode=0;mode<5;mode++){
            var tracker=new MoveEvidenceTracker();tracker.Observe(Sample(0,1000,false,false));tracker.Observe(Sample(1,1000,true,false));
            var hit=Sample(2,mode==0?988:975,true,true);
            if(mode==1)hit.MissingBefore=1;
            if(mode==2)hit.Meter.Players[1].NativeAttack=true;
            if(mode==3)hit.Readiness[2]=1;
            tracker.Observe(hit);
            var end=Sample(3,mode==4?1000:hit.Readiness[14],false,false);
            end.Meter.Players[0].LastAttack=new AttackMeasurement{Capture=3,Action=71,First=2,Active=1,Total=3};tracker.Observe(end);
            var result=end.Meter.Players[0].Evidence;
            if(mode==0&&result.Damage!=12)throw new Exception("Scaled health loss replaced by base damage");
            if((mode==1||mode==2||mode==4)&&result.Damage.HasValue)throw new Exception("Ambiguous damage accepted");
            if(mode==3&&(result.Damage!=25||!result.DamageKind.Contains("chip")))throw new Exception("Chip labeling failed");
        }
        var evidence=new MoveEvidenceTracker();evidence.Observe(Sample(0,1000,false,false));evidence.Observe(Sample(1,1000,true,false));evidence.Observe(Sample(2,975,true,true));
        var first=Sample(3,975,false,false);first.Meter.Players[0].LastAttack=new AttackMeasurement{Capture=3,Action=71};first.Meter.Players[0].ExperimentalAdvantage=new ObservedAdvantage{AttackReturn=3,DefenderReturn=6,Outcome="hit"};evidence.Observe(first);
        if(first.Meter.Players[0].Evidence.OnHit!=3)throw new Exception("Observed result lost");
        var next=Sample(4,975,true,false);next.Meter.Players[0].NativeActionTime=1;evidence.Observe(next);evidence.Observe(Sample(5,975,true,true));
        var whiff=Sample(6,975,false,false);whiff.Meter.Players[0].LastAttack=new AttackMeasurement{Capture=6,Action=71};evidence.Observe(whiff);
        if(whiff.Meter.Players[0].Evidence.OnHit.HasValue||whiff.Meter.Players[0].Evidence.OnGuard.HasValue)throw new Exception("Whiff inherited previous advantage");
        if(first.Meter.Players[0].Evidence.OnHit!=3)throw new Exception("Historical snapshot mutated");
        string traced=null;var trace=new DetectionTrace(text=>traced=text);trace.Observe(first);trace.Observe(whiff);trace.Flush();
        if(traced==null||!traced.Contains("UNVERIFIED")||!traced.Contains("OBSERVED_RETURN_GAP"))throw new Exception("Trace missing explicit measurement limits");
        ValidationSession.Begin(Sample(10,1000,false,false),0);
        var frame=Sample(11,1000,false,false);frame.Meter.Players[0].LastAttack=new AttackMeasurement{Capture=11};ValidationSession.Observe(frame);
        frame=Sample(12,1000,false,false);frame.Meter.Players[0].LastAttack=new AttackMeasurement{Capture=11};ValidationSession.Observe(frame);
        frame=Sample(13,1000,false,false);frame.Meter.Players[0].LastAttack=new AttackMeasurement{Capture=13};ValidationSession.Observe(frame);ValidationSession.Stop();
        if(ValidationSession.Completions!=2||ValidationSession.Broken)throw new Exception("Trial completion counting failed");
        Console.WriteLine("PASS: scaled damage, chip, gaps, trades, refill rejection; distinct trial completion counting.");
    }
    static int Main(string[] args){try{RunMain(args);return 0;}catch(Exception e){Console.Error.WriteLine("FAIL: "+e.Message);return 1;}}
    static void RunMain(string[] args){
        SafetyChecks();bool rendered=false;int damageSamples=0;var totals=new Dictionary<int,int>();
        var t=new NativeTimingTracker();var a=new ObservedAdvantageTracker();var e=new MoveEvidenceTracker();var history=new FrameHistory();long last=-1;
        foreach(var line in File.ReadLines(args[0]).Skip(1)){
            var f=AdvantageReplayTests.Parse(line);t.Observe(f);a.Observe(f);e.Observe(f);history.Add(f);var s=f.Meter.Players[0];
            if(SevenMetrics.Values(s,false)[6]!="—")throw new Exception("Unverified stun exposed");
            if(SevenMetrics.Values(s,false)[2]!="—"&&(s.LastAttack==null||!s.LastAttack.Recovery.HasValue||String.IsNullOrEmpty(s.LastAttack.RecoverySource)))throw new Exception("Unsourced recovery exposed");
            if(s.LastAttack!=null&&s.LastAttack.Capture!=last){last=s.LastAttack.Capture;if(s.LastAttack.Action==71&&s.Evidence!=null&&s.Evidence.Damage.HasValue){int n=s.Evidence.Damage.Value;damageSamples++;if(!totals.ContainsKey(n))totals[n]=0;totals[n]++;if(n<0||n>1000)throw new Exception("Invalid health loss");}}
            if(!rendered&&s.Evidence!=null&&s.Evidence.Damage==25&&s.Evidence.OnHit==3){
                rendered=true;using(var b=new Bitmap(448,82))using(var g=Graphics.FromImage(b)){new CompactMeter().Draw(g,new Rectangle(0,0,448,82),FrameTracker.Window(history,45),new ViewerSettings());b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"seven-column-preview.png"));}
            }
        }
        if(damageSamples<10||!totals.ContainsKey(25))throw new Exception("No real effective damage validation");
        foreach(var pair in totals)Console.WriteLine("Effective damage "+pair.Key+": "+pair.Value+" completed Kyo cr.LP samples");
        var missing=new DebugFrame{Number=100000};e.Observe(missing);if(missing.Meter.Players[0].Evidence!=null)throw new Exception("Missing fighters retained evidence");
        var db=new XmlDocument();db.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"reference-data.xml"));string path=RosterValidation.Checklist(db);string coverage=RosterValidation.Coverage(db);if(File.ReadAllLines(coverage).Length!=1818)throw new Exception("Move coverage must preserve all workbook rows");int sheets=db.SelectNodes("/ReferenceDatabase/Sheet").Count;
        if(File.ReadAllLines(path).Length!=1+sheets*RosterValidation.Categories.Length)throw new Exception("Roster checklist incomplete");
        Console.WriteLine("Checklist: "+sheets+" characters; all categories NOT_RUN.");
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"system-test-results.txt"),"PASS: real effective-health-loss replay; unverified fields suppressed; missing-fighter reset; 36-sheet assisted checklist. Live R9 UI and complete roster remain unverified.");
    }
}
