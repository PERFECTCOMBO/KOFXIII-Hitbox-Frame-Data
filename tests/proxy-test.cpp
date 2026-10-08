#define WIN32_LEAN_AND_MEAN
#define DIRECTINPUT_VERSION 0x0800
#include <windows.h>
#include <dinput.h>
#include <stdio.h>
int wmain(){
    HMODULE dll=LoadLibraryW(L".\\dinput8.dll");if(!dll)return 1;
    const char* names[]={"DirectInput8Create","DllCanUnloadNow","DllGetClassObject","DllRegisterServer","DllUnregisterServer","GetdfDIJoystick"};
    for(int i=0;i<6;i++)if(!GetProcAddress(dll,names[i])||GetProcAddress(dll,names[i])!=GetProcAddress(dll,MAKEINTRESOURCEA(i+1)))return 2;
    using Fn=HRESULT(WINAPI*)(HINSTANCE,DWORD,REFIID,LPVOID*,LPUNKNOWN);
    auto create=reinterpret_cast<Fn>(GetProcAddress(dll,"DirectInput8Create"));
    for(int i=0;i<10;i++){
        IDirectInput8W* input=nullptr;
        HRESULT hr=create(GetModuleHandleW(nullptr),DIRECTINPUT_VERSION,IID_IDirectInput8W,(void**)&input,nullptr);
        if(FAILED(hr)||!input)return 3;
        IDirectInputDevice8W* keyboard=nullptr;
        hr=input->CreateDevice(GUID_SysKeyboard,&keyboard,nullptr);
        if(FAILED(hr)||!keyboard)return 4;
        keyboard->Release(); input->Release();
    }
    using FormatFn=LPCDIDATAFORMAT(WINAPI*)();
    if(!reinterpret_cast<FormatFn>(GetProcAddress(dll,"GetdfDIJoystick"))())return 5;
    using ClassFn=HRESULT(WINAPI*)(REFCLSID,REFIID,LPVOID*);
    void* factory=nullptr;
    HRESULT hr=reinterpret_cast<ClassFn>(GetProcAddress(dll,"DllGetClassObject"))(CLSID_DirectInput8,IID_IClassFactory,&factory);
    if(FAILED(hr)||!factory)return 6;
    reinterpret_cast<IUnknown*>(factory)->Release();
    Sleep(1600);FreeLibrary(dll);
    puts("PASS: six name/ordinal exports; ten DirectInput objects and keyboard devices; joystick format; COM factory. No input sent.");
    return 0;
}
