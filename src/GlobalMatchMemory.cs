using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

internal sealed class FrameCapture : IDisposable
{
    internal const string Fingerprint="E7718F5C553DE852135DDEF5833A7BB6EAFD05508D1206AC6F3CE7767B474835";
    internal static bool IsSupportedPath(string path){
        if(!String.Equals(Path.GetFileName(path),"game.exe",StringComparison.OrdinalIgnoreCase))return false;
        using(var sha=SHA256.Create())using(var file=File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","")==Fingerprint;
    }
    readonly Process process;
    IntPtr handle,code,buffer,site;
    byte[] original,patch;
    bool installed;
    internal uint LastSequence;
    internal int ProcessId {get{return process.Id;}}
    internal int[] FighterStates=new int[12];
    internal uint[] FighterActors=new uint[2];
    internal int[] Readiness;
    RingReader reader;
    internal int LastMissing;internal bool LastTruncated;
    internal float[] Projection=new float[16];
    internal bool Alive {get{return !process.HasExited;}}
    internal FrameCapture(Process p)
    {
        process=p;
        PipelineDiagnostics.Set("Build validation: PID "+p.Id+" / "+p.MainModule.FileName);
        using(var sha=SHA256.Create()) using(var f=File.OpenRead(p.MainModule.FileName))
            if(BitConverter.ToString(sha.ComputeHash(f)).Replace("-","")!=Fingerprint)
                throw new InvalidOperationException("Unsupported game build. No changes were made.");
        handle=Native.OpenProcess(0x438,false,p.Id);
        PipelineDiagnostics.Set("Memory access / hook validation");
        if(handle==IntPtr.Zero)throw Native.Error("Windows denied memory access. Close the viewer, then right-click KOF13HITBOXv0.1.exe and select Run as administrator.");
        try {
            long image=p.MainModule.BaseAddress.ToInt64();site=new IntPtr(image+0x1D7551);
            original=Read(site,5);
            byte[] expected={0xE8,0x5A,0x15,0x00,0x00};
            if(!Equal(original,expected))throw new InvalidOperationException("The capture site differs from the supported build, or another viewer is attached. Restart the game before retrying.");
            byte[] boundary=Read(new IntPtr(image+0x1D7556),7);
            if(!Equal(boundary,new byte[]{0x8B,0xCB,0xE8,0xE3,0xFB,0xFF,0xFF}))throw new InvalidOperationException("Frame boundary signature mismatch.");
            buffer=Native.VirtualAllocEx(handle,IntPtr.Zero,(UIntPtr)RingLayout.AllocationBytes,0x3000,4);
            code=Native.VirtualAllocEx(handle,IntPtr.Zero,(UIntPtr)4096,0x3000,4);
            if(buffer==IntPtr.Zero||code==IntPtr.Zero)throw Native.Error("Cannot allocate the frame capture buffer.");
            reader=new RingReader((offset,length)=>Read(new IntPtr(buffer.ToInt64()+offset),length));
            uint remote=checked((uint)code.ToInt64()),snapshot=checked((uint)buffer.ToInt64());
            byte[] stub=Assembler.Capture(remote,snapshot,checked((uint)(image+0x1D8AB0)));
            Write(code,stub);uint old;
            if(!Native.VirtualProtectEx(handle,code,(UIntPtr)4096,0x20,out old))throw Native.Error("Cannot protect capture code.");
            Native.FlushInstructionCache(handle,code,(UIntPtr)stub.Length);
            patch=new byte[5];patch[0]=0xE8;Array.Copy(BitConverter.GetBytes(unchecked(remote-(uint)(site.ToInt64()+5))),0,patch,1,4);
            installed=true;Replace(original,patch,false);
            PipelineDiagnostics.Attached=true;PipelineDiagnostics.Memory=true;PipelineDiagnostics.Set("Capture installed; awaiting first buffered update");
        } catch {Dispose();throw;}
    }
    internal static bool Equal(byte[] a,byte[] b) {if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    byte[] Read(IntPtr a,int n)
    {
        if(n==0)return new byte[0];
        byte[] b=new byte[n];UIntPtr got;
        if(!Native.ReadProcessMemory(handle,a,b,(UIntPtr)n,out got)||got.ToUInt64()!=(ulong)n)throw Native.Error("Cannot read game memory.");return b;
    }
    void Write(IntPtr a,byte[] b)
    {
        UIntPtr got;if(!Native.WriteProcessMemory(handle,a,b,(UIntPtr)b.Length,out got)||got.ToUInt64()!=(ulong)b.Length)throw Native.Error("Cannot write capture data.");
    }
    void Replace(byte[] before,byte[] after,bool removing)
    {
        for(int attempt=0;attempt<20;attempt++)
        {
            var threads=new List<IntPtr>();bool retry=false;
            try {
                process.Refresh();
                foreach(ProcessThread t in process.Threads)
                {
                    IntPtr th=Native.OpenThread(0xA,false,t.Id);
                    if(th==IntPtr.Zero)throw Native.Error("Cannot inspect game threads; capture was not installed.");
                    if(Native.SuspendThread(th)==UInt32.MaxValue){Native.CloseHandle(th);throw Native.Error("Cannot pause a game thread for attachment.");}
                    threads.Add(th);
                    byte[] context=new byte[716];Array.Copy(BitConverter.GetBytes(0x10001),context,4);
                    if(!Native.Wow64GetThreadContext(th,context))throw Native.Error("Cannot inspect the frame boundary.");
                    uint ip=BitConverter.ToUInt32(context,184);long s=site.ToInt64();
                    if(ip>=s&&ip<s+5)retry=true;
                    if(removing&&ip>=code.ToInt64()&&ip<code.ToInt64()+4096)retry=true;
                }
                if(!retry)
                {
                    if(!Equal(Read(site,5),before))throw new InvalidOperationException("The frame hook was changed by another program. Restart the game.");
                    uint old;if(!Native.VirtualProtectEx(handle,site,(UIntPtr)5,0x40,out old))throw Native.Error("Cannot update the frame boundary.");
                    try {
                        try {Write(site,after);Native.FlushInstructionCache(handle,site,(UIntPtr)5);}
                        catch {Write(site,before);Native.FlushInstructionCache(handle,site,(UIntPtr)5);throw;}
                    }
                    finally {uint ignored;Native.VirtualProtectEx(handle,site,(UIntPtr)5,old,out ignored);}
                    return;
                }
            }
            finally {for(int i=threads.Count-1;i>=0;i--){Native.ResumeThread(threads[i]);Native.CloseHandle(threads[i]);}}
            Thread.Sleep(2);
        }
        throw new InvalidOperationException("A game thread remained at the frame boundary. Try again.");
    }
    internal List<Box> ReadFrame()
    {
        var packet=reader.Next();if(packet==null)return null;
        LastSequence=packet.Id;LastMissing=packet.Missing;LastTruncated=packet.Truncated;
        Projection=packet.Projection;FighterStates=packet.States;FighterActors=packet.Actors;Readiness=packet.Readiness;return packet.Boxes;
    }
    internal void Detach()
    {
        if(installed&&Alive){Replace(patch,original,true);installed=false;}
        PipelineDiagnostics.Attached=false;PipelineDiagnostics.Set("Detached; capture restored or game exited");
    }
    public void Dispose()
    {
        if(handle==IntPtr.Zero)return;
        bool safe=true;
        try {Detach();}catch(Exception ex) {safe=false;PipelineDiagnostics.Fail(ex);}
        // On a failed detach, retain the code and snapshot in the game so an
        // existing call target never points to freed memory. Restart cleans it up.
        if(safe&&Alive){if(code!=IntPtr.Zero)Native.VirtualFreeEx(handle,code,UIntPtr.Zero,0x8000);if(buffer!=IntPtr.Zero)Native.VirtualFreeEx(handle,buffer,UIntPtr.Zero,0x8000);}
        Native.CloseHandle(handle);handle=IntPtr.Zero;process.Dispose();
    }
}

