using System;
internal static class RingBufferTests {
    static void Check(bool ok,string reason){if(!ok)throw new Exception("Ring: "+reason);}
    static byte[] memory;
    static void Put(long offset,uint value){Array.Copy(BitConverter.GetBytes(value),0,memory,offset,4);}
    static byte[] Read(long offset,int length){var b=new byte[length];Array.Copy(memory,offset,b,0,length);return b;}
    static void Publish(uint id){long slot=RingLayout.Slot(id);Put(slot,unchecked(id*2));Put(slot+4,0);Put(slot+8,id);Put(slot+0x1540,id);Put(0,id);}
    internal static void Run(){
        memory=new byte[RingLayout.AllocationBytes];var reader=new RingReader(Read);
        for(uint i=1;i<=240;i++)Publish(i); // four seconds of records before first UI read
        for(uint i=1;i<=240;i++){var p=reader.Next();Check(p!=null&&p.Id==i&&p.Missing==0,"delayed reads lose no retained updates");}
        Check(reader.Next()==null,"caught up");
        for(uint i=241;i<=1000;i++)Publish(i);
        var first=reader.Next();Check(first.Id==489&&first.Missing==248,"overrun counts only overwritten records");
        for(uint i=490;i<=1000;i++)Check(reader.Next().Id==i,"overrun recovery order");
        Publish(1001);long slot=RingLayout.Slot(1001);Put(slot,2003);Check(reader.Next()==null&&reader.Cursor==1000,"odd slot not consumed");Put(slot,2002);
        Check(reader.Next().Id==1001,"retry completed slot");
        reader.Cursor=UInt32.MaxValue-1;Publish(UInt32.MaxValue);Publish(0);Publish(1);
        Check(reader.Next().Id==UInt32.MaxValue&&reader.Next().Id==0&&reader.Next().Id==1,"32-bit wrap");
        reader.Cursor=10;Publish(11);bool changed=false;
        var torn=new RingReader((offset,length)=>{var b=Read(offset,length);if(offset==RingLayout.Slot(11)&&length==RingLayout.PayloadBytes&&!changed){Put(offset,25);changed=true;}return b;});torn.Cursor=10;
        Check(torn.Next()==null&&torn.Cursor==10,"overwritten during read rejected");
        reader.Cursor=11;Publish(12);slot=RingLayout.Slot(12);Put(slot+28,0x34474452);
        for(uint i=0;i<20;i++)Put(slot+0x1570+i*4,100+i);
        var diagnostic=reader.Next();Check(diagnostic.Readiness!=null&&diagnostic.Readiness[0]==100&&diagnostic.Readiness[19]==119,"both readiness records share accepted seqlock");
        Publish(13);Check(reader.Next().Readiness==null,"unversioned data stays unavailable");
        memory=null;
    }
}
