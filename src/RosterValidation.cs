using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
internal static class ValidationSession {
    internal static long Start,Count;internal static uint? Actor;internal static int Player;internal static bool Armed,Broken;
    internal static string Trace;internal static int Completions;static long lastCompletion;
    static ReadinessRecorder recorder;
    internal static void Begin(DebugFrame f,int p){
        if(f==null||f.Mock||!f.Meter.Players[p].ActorIdentity.HasValue)throw new InvalidOperationException("Attach live fighters first.");
        Player=p;Start=f.Number;Actor=f.Meter.Players[p].ActorIdentity;Count=0;Completions=0;lastCompletion=Start;Broken=false;Armed=true;
        string dir=Path.Combine(AppPaths.DataDirectory,"validation");Directory.CreateDirectory(dir);
        Trace=Path.Combine(dir,"trial-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".csv");
        string path=Trace;recorder=new ReadinessRecorder(text=>{try{File.WriteAllText(path,text);}catch{Broken=true;throw;}},3600,300);
    }
    internal static void Observe(DebugFrame f){if(!Armed)return;Count++;Broken|=f.Mock||f.MissingBefore>0||f.Truncated||f.Meter.Players[Player].ActorIdentity!=Actor||Count>3600;var completed=f.Meter.Players[Player].LastAttack;if(completed!=null&&completed.Capture>lastCompletion){lastCompletion=completed.Capture;Completions++;}recorder.Observe(f);}
    internal static void Stop(){if(recorder!=null)recorder.Flush();Armed=false;}
}
internal sealed class RosterValidation:Form {
    readonly ComboBox character=new ComboBox(),move=new ComboBox(),category=new ComboBox(),player=new ComboBox(),scenario=new ComboBox();
    readonly TextBox report=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill};
    readonly XmlDocument db=new XmlDocument();string armedCharacter,armedMove,armedCategory,armedScenario;XmlNode armedNode;
    internal static readonly string[] Categories={"Standing normals","Crouching normals","Jump normals","Command normals","Specials","EX specials","Supers / EX / Neo Max","Multihit","Projectiles","Landing / recovery"};
    internal RosterValidation(){
        Text="Roster validation - assisted tests (labels supplied by tester)";Width=1000;Height=600;
        db.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"reference-data.xml"));
        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=145};
        foreach(var c in new[]{character,move,category,player,scenario}){c.DropDownStyle=ComboBoxStyle.DropDownList;c.Width=c==move?400:180;top.Controls.Add(c);}
        foreach(XmlNode sheet in db.SelectNodes("/ReferenceDatabase/Sheet"))character.Items.Add(sheet.Attributes["name"].Value);
        category.Items.AddRange(Categories);category.SelectedIndex=0;player.Items.AddRange(new[]{"Player 1","Player 2"});player.SelectedIndex=0;scenario.Items.AddRange(new[]{"Unspecified","Whiff","Ground hit","Guard all","Air-to-air","Cross-up","Cancel / follow-up"});scenario.SelectedIndex=0;
        var arm=new Button{Text="Start trial",AutoSize=true};var save=new Button{Text="Finish + compare",AutoSize=true};var matrix=new Button{Text="Export roster checklist",AutoSize=true};
        top.Controls.Add(arm);top.Controls.Add(save);top.Controls.Add(matrix);Controls.Add(report);Controls.Add(top);
        character.SelectedIndexChanged+=(s,e)=>{move.Items.Clear();foreach(XmlNode sheet in db.SelectNodes("/ReferenceDatabase/Sheet"))if(sheet.Attributes["name"].Value==(string)character.SelectedItem)foreach(XmlNode m in sheet.SelectNodes("Move"))move.Items.Add(new Item{Node=m});if(move.Items.Count>0)move.SelectedIndex=0;};
        arm.Click+=(s,e)=>Safe(()=>{if(ValidationSession.Armed)throw new InvalidOperationException("Finish the current trial first.");var item=move.SelectedItem as Item;if(item==null)return;ValidationSession.Begin(PipelineDiagnostics.Latest,player.SelectedIndex);armedCharacter=(string)character.SelectedItem;armedMove=item.ToString();armedCategory=(string)category.SelectedItem;armedNode=item.Node;armedScenario=(string)scenario.SelectedItem;report.Text="Recording. Return to Training and perform ONE selected move, then wait until the move ends. Return here and Finish + compare.\r\nThe character/move label is manual, not native identification. Use separate trials for hit, block, whiff, jump/hop direction and cancels.";});
        save.Click+=(s,e)=>Safe(Finish);matrix.Click+=(s,e)=>Safe(()=>{string path=Coverage(db);report.Text="Checklist written: "+path+"\r\nMove coverage includes saved trials. Observed fields do not certify actionability or full character completion.";});
        FormClosed+=(s,e)=>ValidationSession.Stop();character.SelectedIndex=0;
        report.Text="Select the actual character, move, category and player. Start trial, perform one move, then finish.\r\nUnknown fields remain unverified. No gameplay inputs are injected.\r\nOnHit/OnGuard comparisons use the CURRENT trial outcome only; earlier cached outcomes are excluded.";
    }
    sealed class Item {internal XmlNode Node;public override string ToString(){return Node.Attributes["name"].Value;}}
    void Safe(Action a){try{a();}catch(Exception e){report.Text=e.Message;}}
    internal static string Csv(string value){return "\""+(value??"").Replace("\"","\"\"")+"\"";}
    void Finish(){
        if(!ValidationSession.Armed)throw new InvalidOperationException("Start a trial first.");ValidationSession.Stop();var f=PipelineDiagnostics.Latest;var c=f==null?null:f.Meter.Players[ValidationSession.Player];
        bool valid=f!=null&&!f.Mock&&!ValidationSession.Broken&&ValidationSession.Completions==1&&!c.TimingInProgress&&c.ActorIdentity==ValidationSession.Actor&&c.LastAttack!=null&&c.LastAttack.Capture>ValidationSession.Start;
        string[] values=valid?SevenMetrics.Values(c,false):new[]{"—","—","—","—","—","—","—"};
        // Never compare a previous hit/block result retained for the same action ID.
        if(valid){var a=c.ExperimentalAdvantage;values[3]=a!=null&&a.Outcome=="hit"?SevenMetrics.Sign(a.Gap):"—";values[4]=a!=null&&a.Outcome=="block"?SevenMetrics.Sign(a.Gap):"—";}
        string[] keys={"startup","active","recovery","on_hit","on_block","damage","stun_damage"};
        var ledger=new XmlDocument();var trial=ledger.CreateElement("Trial");ledger.AppendChild(trial);trial.SetAttribute("character",armedCharacter);trial.SetAttribute("move",armedMove);trial.SetAttribute("row",armedNode.Attributes["row"].Value);trial.SetAttribute("scenario",armedScenario);trial.SetAttribute("valid",valid.ToString());trial.SetAttribute("nativeAction",valid?c.LastAttack.Action.ToString():"");trial.SetAttribute("identitySource","MANUAL_TESTER_LABEL");trial.SetAttribute("trace",ValidationSession.Trace);
        var b=new StringBuilder("Character,Move,Category,Metric,Measured,Reference,Difference,Status,Trace,SourceRow\r\n");
        for(int i=0;i<7;i++){
            var node=armedNode.SelectSingleNode(keys[i]);string reference=node==null?"—":node.Attributes["number"]!=null?node.Attributes["number"].Value:node.InnerText;decimal observed,expected;string difference="—",status=!valid?(ValidationSession.Completions>1?"MULTIPLE_MOVES":ValidationSession.Broken?"INVALID_TRACE":"MISSING_MEASUREMENT"):values[i]=="—"?"UNVERIFIED":"NOT_COMPARABLE";
            if(valid&&i!=0&&i!=5&&Decimal.TryParse(values[i].Replace('−','-'),NumberStyles.Number,CultureInfo.InvariantCulture,out observed)&&Decimal.TryParse(reference,NumberStyles.Number,CultureInfo.InvariantCulture,out expected)){
                difference=(observed-expected).ToString(CultureInfo.InvariantCulture);status=observed==expected?"OBSERVED_MATCH":"DISCREPANCY";
            }
            var metric=ledger.CreateElement("Metric");metric.SetAttribute("name",keys[i]);metric.SetAttribute("measured",values[i]);metric.SetAttribute("status",status);trial.AppendChild(metric);
            // Startup uses pre-active ticks; effective damage can be scaled/chip.
            b.AppendLine(String.Join(",",new[]{Csv(armedCharacter),Csv(armedMove),Csv(armedCategory),Csv(SevenMetrics.Headers[i]),Csv(values[i]),Csv(reference),Csv(difference),Csv(status),Csv(ValidationSession.Trace),Csv(armedNode.Attributes["row"].Value)}));
        }
        string path=Path.ChangeExtension(ValidationSession.Trace,"report.csv");File.WriteAllText(path,b.ToString());ledger.Save(Path.ChangeExtension(ValidationSession.Trace,"report.xml"));Coverage(db);report.Text=b+"\r\nSaved "+path+"\r\nOBSERVED_MATCH is numerical agreement, not proof of actionability or reference applicability.";
    }
    internal static string Coverage(XmlDocument db){
        var trials=new List<XmlElement>();string dir=Path.Combine(AppPaths.DataDirectory,"validation");
        if(Directory.Exists(dir))foreach(string file in Directory.GetFiles(dir,"*.report.xml")){try{var doc=new XmlDocument();doc.Load(file);trials.Add(doc.DocumentElement);}catch{}}
        var b=new StringBuilder("Character,Move,SourceRow,ReferencePresent,MoveIdentified,Executed,Startup,Active,Recovery,OnHit,OnGuard,Damage,Stun,Compared,Status\r\n");
        foreach(XmlNode sheet in db.SelectNodes("/ReferenceDatabase/Sheet"))foreach(XmlNode move in sheet.SelectNodes("Move")){
            string name=sheet.Attributes["name"].Value,row=move.Attributes["row"].Value;var matching=trials.FindAll(x=>x.GetAttribute("character")==name&&x.GetAttribute("row")==row);var values=new List<string>{name,move.Attributes["name"].Value,row,"YES",matching.Count>0?"MANUAL_LABEL":"UNMAPPED",matching.Count>0?"TRIAL_RECORDED":"NOT_RUN"};
            foreach(string key in new[]{"startup","active","recovery","on_hit","on_block","damage","stun_damage"}){bool found=false;foreach(var trial in matching){var metric=trial.SelectSingleNode("Metric[@name='"+key+"']");if(trial.GetAttribute("valid")=="True"&&metric!=null&&metric.Attributes["measured"].Value!="—")found=true;}values.Add(found?"OBSERVED":"UNVERIFIED");}
            values.Add(matching.Count>0?"SEE_TRIAL_REPORT":"NOT_RUN");values.Add(matching.Count>0?"PARTIAL_NOT_CERTIFIED":"NOT_RUN");for(int i=0;i<values.Count;i++)values[i]=Csv(values[i]);b.AppendLine(String.Join(",",values));
        }
        string path=Path.Combine(AppPaths.DataDirectory,"move-coverage.csv");File.WriteAllText(path,b.ToString());return path;
    }
    internal static string Checklist(XmlDocument db){
        var b=new StringBuilder("Character,Category,Status\r\n");foreach(XmlNode sheet in db.SelectNodes("/ReferenceDatabase/Sheet"))foreach(string cat in Categories)b.AppendLine(Csv(sheet.Attributes["name"].Value)+","+Csv(cat)+",NOT_RUN");
        string path=Path.Combine(AppPaths.DataDirectory,"roster-checklist.csv");File.WriteAllText(path,b.ToString());return path;
    }
}
