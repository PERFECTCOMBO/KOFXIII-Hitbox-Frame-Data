using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
internal sealed class ReferenceBrowser:Form {
    readonly ComboBox characters=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly ListBox moves=new ListBox{Dock=DockStyle.Left,Width=370};
    readonly TextBox detail=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical};
    readonly TextBox search=new TextBox{Dock=DockStyle.Top};readonly XmlDocument db=new XmlDocument();
    internal ReferenceBrowser(){Text="Spreadsheet reference - manual selection, NOT live move identification";Width=1050;Height=620;
        db.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"reference-data.xml"));
        Controls.Add(detail);Controls.Add(moves);Controls.Add(search);Controls.Add(characters);
        Controls.Add(new Label{Dock=DockStyle.Top,Height=44,Text="REFERENCE ONLY - Choose a character and move to compare. Native character/move names are unmapped. Workbook revision and Global Match applicability are unverified."});
        foreach(XmlNode s in db.SelectNodes("/ReferenceDatabase/Sheet"))characters.Items.Add(s.Attributes["name"].Value);
        characters.SelectedIndexChanged+=(s,e)=>Filter();search.TextChanged+=(s,e)=>Filter();moves.SelectedIndexChanged+=(s,e)=>Describe();characters.SelectedIndex=0;
    }
    sealed class Entry{internal XmlNode Node;public override string ToString(){return Node.Attributes["name"].Value;}}
    static bool Matches(XmlNode n,string text){if(n.Attributes["name"].Value.IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0)return true;foreach(XmlNode a in n.SelectNodes("Alias"))if(a.InnerText.IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0)return true;return false;}
    void Filter(){moves.Items.Clear();foreach(XmlNode s in db.SelectNodes("/ReferenceDatabase/Sheet"))if(s.Attributes["name"].Value==(string)characters.SelectedItem)foreach(XmlNode m in s.SelectNodes("Move"))if(Matches(m,search.Text))moves.Items.Add(new Entry{Node=m});if(moves.Items.Count>0)moves.SelectedIndex=0;else detail.Text="No matches";}
    void Describe(){var entry=moves.SelectedItem as Entry;if(entry==null)return;var n=entry.Node;var b=new StringBuilder();b.AppendLine("REFERENCE: "+characters.SelectedItem+" / "+entry);b.AppendLine("Source: KOFXIII BOT Framedata.xlsx / row "+n.Attributes["row"].Value);b.AppendLine("Issues: "+n.Attributes["issues"].Value);b.AppendLine();foreach(XmlNode v in n.ChildNodes)b.AppendLine(v.Name+": "+v.InnerText);b.AppendLine();b.AppendLine("LIVE native observations (manual comparison; no match asserted):");var f=PipelineDiagnostics.Latest;if(f!=null&&!f.Mock)for(int p=0;p<2;p++)b.AppendLine(TimingPresentation.Summary(f.Meter.Players[p],p,false));else b.AppendLine("No live capture available");b.AppendLine("Native timer origin / neutral predicate differ from spreadsheet frame conventions. Tail is not verified recovery. Stun column is not a live stun counter.");detail.Text=b.ToString();}
}
