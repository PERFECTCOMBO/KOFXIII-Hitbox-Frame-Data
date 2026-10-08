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

internal static class Native
{
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool ReadProcessMemory(IntPtr h,IntPtr a,byte[] b,UIntPtr n,out UIntPtr got);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool WriteProcessMemory(IntPtr h,IntPtr a,byte[] b,UIntPtr n,out UIntPtr got);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr a,UIntPtr n,uint type,uint protect);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool VirtualFreeEx(IntPtr h,IntPtr a,UIntPtr n,uint type);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool VirtualProtectEx(IntPtr h,IntPtr a,UIntPtr n,uint protect,out uint old);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool FlushInstructionCache(IntPtr h,IntPtr a,UIntPtr n);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern IntPtr OpenThread(uint access,bool inherit,int tid);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern uint SuspendThread(IntPtr h);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern uint ResumeThread(IntPtr h);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern bool Wow64GetThreadContext(IntPtr h,byte[] context);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
    internal delegate bool EnumProc(IntPtr w,IntPtr p);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc callback,IntPtr p);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr w,out uint pid);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr w);
    [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr w);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [StructLayout(LayoutKind.Sequential)] internal struct Rect {internal int Left,Top,Right,Bottom;}
    [DllImport("user32.dll")] internal static extern bool GetClientRect(IntPtr w,out Rect r);
    [DllImport("user32.dll")] internal static extern bool ClientToScreen(IntPtr w,ref Point p);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr w,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr w,int id);
    internal static Exception Error(string text) {return new Win32Exception(Marshal.GetLastWin32Error(),text);}
}

