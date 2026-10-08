using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

// Raw engine observations for validation, deliberately separate from frame advantage.
internal sealed class ReadinessRecorder {
    readonly Queue<string> rows=new Queue<string>();
    readonly Action<string> save;
    readonly int capacity,interval;
    int sinceSave;
    bool dirty;
    internal static string LastPath,LastError;
    internal ReadinessRecorder():this(null,7200,300){}
    internal ReadinessRecorder(Action<string> output,int retained,int every){
        capacity=retained;interval=every;
        if(output!=null)save=output;
        else {
            string path=Path.Combine(AppPaths.DataDirectory,"readiness-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff",CultureInfo.InvariantCulture)+".csv");
            save=text=>{File.WriteAllText(path,text);LastPath=path;};
        }
    }
    internal static string Header(){
        var b=new StringBuilder("capture,missing,truncated");
        for(int p=1;p<=2;p++)foreach(string field in new[]{"body","primary","secondary","action","timer","attack_region","hitstop_flag","category","command","attack_result","ai_flags","health","actor_ec","actor_f4","actor_16c","actor_198","actor_19a"})b.Append(",p").Append(p).Append('_').Append(field);
        return b.ToString();
    }
    static void Value(StringBuilder b,object value){b.Append(',');if(value!=null)b.Append(Convert.ToString(value,CultureInfo.InvariantCulture));}
    internal void Observe(DebugFrame f){
        if(f.Mock||f.Readiness==null||f.Readiness.Length!=20)return;
        var b=new StringBuilder(f.Number.ToString(CultureInfo.InvariantCulture));Value(b,f.MissingBefore);Value(b,f.Truncated?1:0);
        for(int p=0;p<2;p++){
            var s=f.Meter.Players[p];Value(b,s.ActorIdentity);Value(b,s.NativeStateId);Value(b,s.NativeSecondaryStateId);Value(b,s.NativeActionId);Value(b,s.NativeActionTime);Value(b,s.NativeAttack?1:0);Value(b,s.NativeHitstop?1:0);
            for(int i=0;i<10;i++)Value(b,f.Readiness[p*10+i]);
        }
        rows.Enqueue(b.ToString());while(rows.Count>capacity)rows.Dequeue();dirty=true;
        if(++sinceSave>=interval||rows.Count==1)Flush();
    }
    internal void Flush(){
        if(!dirty)return;
        try{var b=new StringBuilder(Header()).AppendLine();foreach(string row in rows)b.AppendLine(row);save(b.ToString());dirty=false;sinceSave=0;LastError=null;}
        catch(Exception e){LastError=e.Message;sinceSave=0;DebugLog.Write("Readiness recording: "+e.Message);}
    }
}

internal static class ReadinessTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("Readiness: "+message);}
    internal static void Run(){
        string saved=null;int saves=0;var recorder=new ReadinessRecorder(s=>{saved=s;saves++;},2,2);
        var f=new DebugFrame{Number=1,Readiness=new int[20]};f.Meter.Players[0].NativeStateId=45;f.Readiness[0]=1;f.Readiness[10]=2;
        recorder.Observe(f);Check(saved.Contains("p2_actor_19a"),"complete schema");
        var lines=saved.Trim().Split('\n');Check(lines[0].Split(',').Length==37&&lines[1].Split(',').Length==37,"field counts");
        Check(lines[1].Split(',')[10]=="1"&&lines[1].Split(',')[27]=="2","player categories separated");
        f.Number=2;recorder.Observe(f);f.Number=3;recorder.Observe(f);
        lines=saved.Trim().Split('\n');Check(lines.Length==3&&lines[1].StartsWith("2,")&&lines[2].StartsWith("3,"),"bounded recording retains newest");
        f.Mock=true;recorder.Observe(f);recorder.Flush();Check(saves==2,"mock not recorded");
        f.Mock=false;f.Readiness=null;recorder.Observe(f);recorder.Flush();Check(saves==2,"legacy captures not mislabeled");
        Check(!f.Meter.GameFrame.HasValue&&!f.Meter.Players[0].ActionableFrame.HasValue,"raw recording cannot certify clock/readiness");
    }
}
