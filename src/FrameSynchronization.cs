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

internal sealed class Assembler
{
    readonly List<byte> bytes=new List<byte>();
    readonly Dictionary<string,int> labels=new Dictionary<string,int>();
    readonly List<KeyValuePair<int,string>> fixups=new List<KeyValuePair<int,string>>();
    internal void Emit(params byte[] b) {bytes.AddRange(b);}
    internal void Dword(uint n) {Emit(BitConverter.GetBytes(n));}
    internal void Label(string name) {labels.Add(name,bytes.Count);}
    internal void Jump(byte condition,string label) {Emit(0x0F,condition);fixups.Add(new KeyValuePair<int,string>(bytes.Count,label));Dword(0);}
    internal byte[] Finish(uint codeAddress,uint target)
    {
        foreach(var f in fixups) {byte[] b=BitConverter.GetBytes(labels[f.Value]-(f.Key+4));for(int i=0;i<4;i++)bytes[f.Key+i]=b[i];}
        Emit(0xE9);Dword(unchecked(target-(codeAddress+(uint)bytes.Count+4)));
        return bytes.ToArray();
    }
    internal static byte[] Capture(uint code,uint snapshot,uint target)
    {
        var a=new Assembler();
        a.Emit(0x9C,0x60); // preserve flags and all integer registers
        a.Emit(0xA1);a.Dword(snapshot);a.Emit(0x40); // next producer ID
        a.Emit(0x89,0xC2,0x81,0xE2);a.Dword(RingLayout.Capacity-1);
        a.Emit(0xC1,0xE2,0x0D,0x8D,0xAA);a.Dword(snapshot+RingLayout.HeaderBytes);
        a.Emit(0x89,0x45,0x08,0xD1,0xE0,0x83,0xC8,0x01,0x89,0x45,0x00); // slot ID and odd write token
        a.Emit(0xC7,0x45,0x10);a.Dword(0); // flags
        a.Emit(0xC7,0x45,0x04);a.Dword(0); // count=0
        a.Emit(0x8D,0x59,0x18,0x8D,0x7D,0x40,0x31,0xD2); // ebx=engine+18, edi=records, edx=group
        a.Label("group");a.Emit(0x8B,0x04,0xD3,0x89,0x45,0x0C,0x8B,0x30); // eax=head; save head; esi=head.next
        a.Label("node");a.Emit(0x3B,0x75,0x0C);a.Jump(0x84,"next"); // stop at sentinel
        a.Emit(0x81,0x7D,0x04);a.Dword(256);a.Jump(0x83,"truncated"); // bound total traversal
        a.Emit(0x89,0x17); // record.group=edx
        a.Emit(0x8B,0x46,0x0C,0x89,0x47,0x04); // left
        a.Emit(0x8B,0x46,0x10,0x89,0x47,0x08); // bottom
        a.Emit(0x8B,0x46,0x14,0x89,0x47,0x0C); // width
        a.Emit(0x8B,0x46,0x18,0x89,0x47,0x10); // height
        a.Emit(0x83,0xC7,0x14,0xFF,0x45,0x04,0x8B,0x36); // advance record; count++; node=node.next
        // cmp edx,edx / je provides an unconditional label relocation.
        a.Emit(0x39,0xD2);a.Jump(0x84,"node");
        a.Label("next");a.Emit(0x42,0x83,0xFA,0x0A);a.Jump(0x82,"group");
        a.Emit(0x39,0xD2);a.Jump(0x84,"done");
        a.Label("truncated");a.Emit(0xC7,0x45,0x10);a.Dword(1);
        a.Label("done");
        // Snapshot the camera under the same sequence lock as the box list.
        a.Emit(0x8D,0xBD);a.Dword(0x1500);a.Emit(0x31,0xC0,0xB9);a.Dword(16);a.Emit(0xFC,0xF3,0xAB);
        a.Emit(0xA1);a.Dword(0xD9A060); // shader uniform pointer
        a.Emit(0x85,0xC0);a.Jump(0x84,"states");
        a.Emit(0x8D,0x70,0x08,0x8D,0xBD);a.Dword(0x1500);
        a.Emit(0xB9);a.Dword(16);a.Emit(0xFC,0xF3,0xA5);
        a.Label("states");
        // Versioned raw readiness diagnostics. Same seqlock as states and regions.
        a.Emit(0xC7,0x45,0x1C);a.Dword(0x34474452); // RDG4
        for(int player=0;player<2;player++) {
            uint diagnostic=(uint)(0x1570+player*40);
            for(uint field=0;field<40;field+=4){a.Emit(0xC7,0x85);a.Dword(diagnostic+field);a.Dword(UInt32.MaxValue);}
            a.Emit(0xC7,0x45,(byte)(20+player*4));a.Dword(0); // fighter identity, same seqlock
            uint dest=(uint)(0x1540+player*24);
            a.Emit(0xC7,0x85);a.Dword(dest);a.Dword(UInt32.MaxValue);
            a.Emit(0xC7,0x85);a.Dword(dest+4);a.Dword(UInt32.MaxValue);
            a.Emit(0xC7,0x85);a.Dword(dest+8);a.Dword(0);
            for(uint extra=12;extra<24;extra+=4){a.Emit(0xC7,0x85);a.Dword(dest+extra);a.Dword(UInt32.MaxValue);}
            a.Emit(0xA1);a.Dword((uint)(0xDB6FE8+player*4));
            a.Emit(0x85,0xC0);a.Jump(0x84,"stateNext"+player);
            a.Emit(0x89,0xC3); // retain actor in EBX until core type validated
            a.Emit(0x8B,0x40,0x30,0x85,0xC0);a.Jump(0x84,"stateNext"+player);
            a.Emit(0x8B,0x40,0x08,0x85,0xC0);a.Jump(0x84,"stateNext"+player);
            a.Emit(0x81,0x38);a.Dword(0x8AE8BC);a.Jump(0x85,"stateNext"+player);
            a.Emit(0x8B,0x70,0x08,0x85,0xF6);a.Jump(0x84,"stateNext"+player);
            a.Emit(0x89,0x75,(byte)(20+player*4)); // body address identifies fighter instance
            uint diagnosticDest=diagnostic;
            foreach(uint off in new uint[]{0x114,0xA8,0x110}){
                a.Emit(0x8B,0x86);a.Dword(off);a.Emit(0x89,0x85);a.Dword(diagnosticDest);diagnosticDest+=4;
            }
            a.Emit(0xA1);a.Dword((uint)(0x994350+player*4));a.Emit(0x89,0x85);a.Dword(diagnosticDest);diagnosticDest+=4;
            foreach(uint off in new uint[]{0x48,0xEC,0xF4,0x16C}){
                a.Emit(0x8B,0x83);a.Dword(off);a.Emit(0x89,0x85);a.Dword(diagnosticDest);diagnosticDest+=4;
            }
            foreach(uint off in new uint[]{0x198,0x19A}){
                a.Emit(0x0F,0xB6,0x83);a.Dword(off);a.Emit(0x89,0x85);a.Dword(diagnosticDest);diagnosticDest+=4;
            }
            foreach(uint off in new uint[]{0xA0,0xA4,0x1E4,0x88,0x8C,0x9C}){
                a.Emit(0x8B,0x86);a.Dword(off);a.Emit(0x89,0x85);a.Dword(dest);dest+=4;
            }
            a.Label("stateNext"+player);
        }
        a.Label("publish");a.Emit(0x8B,0x45,0x08,0x89,0xC2,0xD1,0xE0,0x89,0x45,0x00);
        a.Emit(0x89,0x15);a.Dword(snapshot); // publish producer ID only after the completed slot
        a.Emit(0x61,0x9D); // publish even sequence; restore registers/flags
        return a.Finish(code,target);
    }
}

