using System;
using System.Runtime.InteropServices;
internal static class Ig {
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_init(IntPtr hwnd);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_shutdown();
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern IntPtr ui_message(IntPtr hwnd,uint msg,IntPtr w,IntPtr l);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_begin(int w,int h);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_end();
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_button(string label);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_check(string label,ref int value);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_slider(string label,ref float value,float min,float max);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_int(string label,ref int value,int min,int max);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_text(string text);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_wrapped(string text);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_same();
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_sep();
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_tabs();
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_endtabs();
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_tab(string name);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_endtab();
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_space(float h);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern float ui_width();
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern float ui_height();
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_canvas(float h,out float x,out float y,out float w);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_hover();
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_click(out float x,out float y);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_rect(float x,float y,float w,float h,uint color,int fill,float thickness);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_line(float x,float y,float x2,float y2,uint color);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_label(float x,float y,uint color,string text);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_clip(float x,float y,float w,float h);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern void ui_unclip();
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_pixels([Out] byte[] pixels,int size);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_test_click(string label,int down);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)] internal static extern int ui_input(string label,ref int value);
[DllImport("Kof13ImGui.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern int ui_test_info(int n);
internal static void Check(string label,ref bool value){int v=value?1:0;ui_check(label,ref v);value=v!=0;}
internal static void Decimal(string label,ref decimal value,float min,float max){float v=(float)value;if(ui_slider(label,ref v,min,max)!=0)value=(decimal)v;}
}
