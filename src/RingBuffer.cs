using System;
using System.Collections.Generic;
internal static class RingLayout {
    internal const uint Capacity=512,SlotBytes=8192,HeaderBytes=64;
    internal const uint AllocationBytes=HeaderBytes+Capacity*SlotBytes;
    internal const int PayloadBytes=0x15C0;
    internal static long Slot(uint id){return HeaderBytes+(long)(id&(Capacity-1))*SlotBytes;}
}
internal sealed class CapturePacket {
    internal int[] Readiness;
    internal uint[] Actors=new uint[2];
    internal uint Id;internal int Missing;internal bool Truncated;
    internal List<Box> Boxes=new List<Box>();internal int[] States=new int[12];internal float[] Projection=new float[16];
}
internal sealed class RingReader {
    readonly Func<long,int,byte[]> read;
    internal uint Cursor;
    internal RingReader(Func<long,int,byte[]> read){this.read=read;}
    internal CapturePacket Next(){
        uint latest=BitConverter.ToUInt32(read(0,4),0),distance=unchecked(latest-Cursor);
        if(distance==0)return null;
        uint lost=distance>RingLayout.Capacity?distance-RingLayout.Capacity:0;
        uint wanted=unchecked(Cursor+lost+1);long slot=RingLayout.Slot(wanted);
        byte[] b=read(slot,RingLayout.PayloadBytes);uint token=BitConverter.ToUInt32(b,0);
        if(token!=unchecked(wanted*2)||BitConverter.ToUInt32(b,8)!=wanted)return null;
        if(BitConverter.ToUInt32(read(slot,4),0)!=token)return null;
        uint count=BitConverter.ToUInt32(b,4);if(count>256)throw new InvalidOperationException("Invalid buffered region count.");
        var packet=new CapturePacket{Id=wanted,Missing=(int)Math.Min(lost,Int32.MaxValue),Truncated=(BitConverter.ToUInt32(b,16)&1)!=0};
        for(int i=0;i<count;i++){
            int o=64+i*20;var box=new Box{Group=BitConverter.ToInt32(b,o),Left=BitConverter.ToSingle(b,o+4),Bottom=BitConverter.ToSingle(b,o+8),Width=BitConverter.ToSingle(b,o+12),Height=BitConverter.ToSingle(b,o+16)};
            if(box.Group<0||box.Group>=10||!Finite(box.Left)||!Finite(box.Bottom)||!Finite(box.Width)||!Finite(box.Height)||box.Width<=0||box.Height<=0||box.Width>4096||box.Height>4096)continue;
            packet.Boxes.Add(box);
        }
        for(int i=0;i<16;i++)packet.Projection[i]=BitConverter.ToSingle(b,0x1500+i*4);
        for(int i=0;i<12;i++)packet.States[i]=BitConverter.ToInt32(b,0x1540+i*4);
        for(int i=0;i<2;i++)packet.Actors[i]=BitConverter.ToUInt32(b,20+i*4);
        if(BitConverter.ToUInt32(b,28)==0x34474452){packet.Readiness=new int[20];for(int i=0;i<20;i++)packet.Readiness[i]=BitConverter.ToInt32(b,0x1570+i*4);}
        Cursor=wanted;return packet;
    }
    static bool Finite(float n){return !Single.IsNaN(n)&&!Single.IsInfinity(n)&&Math.Abs(n)<100000;}
}
