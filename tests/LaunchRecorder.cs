using System;
using System.Diagnostics;
using System.IO;
class LaunchRecorder {
 static int Main(string[] args){
  if(args.Length!=3||args[0]!="--autoload")return 2;
  using(var p=Process.GetProcessById(int.Parse(args[1]))){
   if(p.StartTime.ToUniversalTime().ToFileTimeUtc()!=long.Parse(args[2]))return 3;
   File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launches.txt"),String.Join(" ",args)+Environment.NewLine);
  }return 0;
 }
}
