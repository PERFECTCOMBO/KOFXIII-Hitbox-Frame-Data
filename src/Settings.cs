using System;
using System.IO;
using System.Xml.Serialization;
public sealed class ViewerSettings {
    public bool[] Kinds=new[]{true,true,true,true,true,true,true,true,true};
    public bool Overlay=true,Grid=false,Coordinates=false,Origins=true,Ground=true,Technical=true,FrameData=true;
    public bool Advanced=true;
    public bool CompactDock=true;
    public bool ObservedTimeline=true;
    public bool Meter=true,Hitboxes=true,Advantage=true,Input=false,FrameNumbers=true,MeterP1=true,MeterP2=true;
    public decimal MeterX=12,MeterY=26,MeterScale=100;
    public int Player=0,Alpha=35,OverlayOpacity=90;public decimal Floor=89.27m,OffsetX=0,Scale=100;
    internal static string FilePath {get{return Path.Combine(AppPaths.DataDirectory,"settings.xml");}}
    internal static ViewerSettings Load(){try{using(var f=File.OpenRead(FilePath)){var s=(ViewerSettings)new XmlSerializer(typeof(ViewerSettings)).Deserialize(f);if(s.Kinds==null||s.Kinds.Length!=9)s.Kinds=new[]{true,true,true,true,true,true,true,true,true};s.Alpha=Math.Max(0,Math.Min(180,s.Alpha));s.MeterX=Math.Max(0,Math.Min(90,s.MeterX));s.MeterY=Math.Max(0,Math.Min(90,s.MeterY));s.MeterScale=Math.Max(50,Math.Min(150,s.MeterScale));s.OverlayOpacity=Math.Max(15,Math.Min(100,s.OverlayOpacity));s.Player=Math.Max(0,Math.Min(3,s.Player));s.Floor=Math.Max(50,Math.Min(110,s.Floor));s.OffsetX=Math.Max(-1000,Math.Min(1000,s.OffsetX));s.Scale=Math.Max(50,Math.Min(200,s.Scale));return s;}}catch{return new ViewerSettings();}}
    internal void Save(){string tmp=FilePath+".tmp";using(var f=File.Create(tmp))new XmlSerializer(typeof(ViewerSettings)).Serialize(f,this);if(File.Exists(FilePath)){try{File.Replace(tmp,FilePath,null);}catch(UnauthorizedAccessException){File.Copy(tmp,FilePath,true);File.Delete(tmp);}catch(IOException){File.Copy(tmp,FilePath,true);File.Delete(tmp);}}else File.Move(tmp,FilePath);}
}
internal static class DebugLog {
    internal static string LastError;
    internal static void Write(string text){try{var path=Path.Combine(AppPaths.DataDirectory,"debug.log");if(File.Exists(path)&&new FileInfo(path).Length>5000000)File.WriteAllText(path,DateTime.UtcNow.ToString("o")+" Log rotated\r\n");File.AppendAllText(path,DateTime.UtcNow.ToString("o")+" "+text+Environment.NewLine);LastError=null;}catch(Exception ex){LastError=ex.Message;}}
}
