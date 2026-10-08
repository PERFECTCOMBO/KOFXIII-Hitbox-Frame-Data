#define WIN32_LEAN_AND_MEAN
#define DIRECTINPUT_VERSION 0x0800
#include <windows.h>
#include <dinput.h>
#include <string>

// No loader work in DllMain. Resolve the actual Windows library lazily,
// from a normal exported call; never search the game's DLL directory.
static HMODULE selfModule, systemModule;
static INIT_ONCE systemOnce = INIT_ONCE_STATIC_INIT;
static LONG launchStarted;
static BOOL CALLBACK LoadSystem(PINIT_ONCE, PVOID, PVOID*) {
    wchar_t dir[MAX_PATH];
    UINT n = GetSystemDirectoryW(dir, MAX_PATH);
    if (n && n < MAX_PATH) {
        std::wstring path(dir); path += L"\\dinput8.dll";
        systemModule = LoadLibraryW(path.c_str());
    }
    return TRUE;
}
static FARPROC Resolve(const char* name) {
    InitOnceExecuteOnce(&systemOnce, LoadSystem, nullptr, nullptr);
    return systemModule ? GetProcAddress(systemModule, name) : nullptr;
}
static DWORD WINAPI LaunchViewer(void*) {
    wchar_t host[32768], module[32768];
    DWORD hn=GetModuleFileNameW(nullptr,host,32768), mn=GetModuleFileNameW(selfModule,module,32768);
    if (!hn || hn>=32768 || !mn || mn>=32768) return 0;
    const wchar_t* leaf=wcsrchr(host,L'\\');
    if (_wcsicmp(leaf?leaf+1:host,L"game.exe")!=0) return 0;
    std::wstring folder(module); folder.resize(folder.find_last_of(L"\\/"));
    if (GetFileAttributesW((folder+L"\\KOF13HITBOX\\disabled.txt").c_str())!=INVALID_FILE_ATTRIBUTES) return 0;
    std::wstring exe=folder+L"\\KOF13HITBOX\\KOF13HITBOX.exe";
    FILETIME created, exited, kernel, user;
    if (!GetProcessTimes(GetCurrentProcess(),&created,&exited,&kernel,&user)) return 0;
    ULARGE_INTEGER stamp; stamp.LowPart=created.dwLowDateTime; stamp.HighPart=created.dwHighDateTime;
    std::wstring command=L"\""+exe+L"\" --autoload "+std::to_wstring(GetCurrentProcessId())+L" "+std::to_wstring(stamp.QuadPart);
    STARTUPINFOW startup={sizeof(startup)}; PROCESS_INFORMATION process={};
    if (CreateProcessW(exe.c_str(),&command[0],nullptr,nullptr,FALSE,CREATE_NO_WINDOW,nullptr,(folder+L"\\KOF13HITBOX").c_str(),&startup,&process)) {
        CloseHandle(process.hThread); CloseHandle(process.hProcess);
    } else OutputDebugStringW(L"KOF13HITBOX: background viewer could not start. Check the KOF13HITBOX subfolder.\n");
    return 0;
}
static void StartOnce() {
    if (InterlockedCompareExchange(&launchStarted,1,0)!=0) return;
    // Pin this small proxy before starting a worker. No worker can execute
    // unloaded code if the host later releases its DirectInput module.
    HMODULE pinned;
    if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_PIN,
        reinterpret_cast<LPCWSTR>(&StartOnce),&pinned)) return;
    HANDLE thread=CreateThread(nullptr,0,LaunchViewer,nullptr,0,nullptr);
    if(thread) CloseHandle(thread);
}
extern "C" HRESULT WINAPI ProxyDirectInput8Create(HINSTANCE instance,DWORD version,REFIID iid,LPVOID* output,LPUNKNOWN outer) {
    using Fn=HRESULT (WINAPI*)(HINSTANCE,DWORD,REFIID,LPVOID*,LPUNKNOWN);
    auto fn=reinterpret_cast<Fn>(Resolve("DirectInput8Create"));
    HRESULT result=fn?fn(instance,version,iid,output,outer):E_FAIL;
    StartOnce(); return result;
}
extern "C" HRESULT WINAPI ProxyDllCanUnloadNow() { return S_FALSE; }
extern "C" HRESULT WINAPI ProxyDllGetClassObject(REFCLSID clsid,REFIID iid,LPVOID* output) {
    using Fn=HRESULT (WINAPI*)(REFCLSID,REFIID,LPVOID*);
    auto fn=reinterpret_cast<Fn>(Resolve("DllGetClassObject"));
    return fn?fn(clsid,iid,output):CLASS_E_CLASSNOTAVAILABLE;
}
extern "C" HRESULT WINAPI ProxyDllRegisterServer() {
    using Fn=HRESULT (WINAPI*)();auto fn=reinterpret_cast<Fn>(Resolve("DllRegisterServer"));return fn?fn():E_FAIL;
}
extern "C" HRESULT WINAPI ProxyDllUnregisterServer() {
    using Fn=HRESULT (WINAPI*)();auto fn=reinterpret_cast<Fn>(Resolve("DllUnregisterServer"));return fn?fn():E_FAIL;
}
extern "C" LPCDIDATAFORMAT WINAPI ProxyGetdfDIJoystick() {
    using Fn=LPCDIDATAFORMAT (WINAPI*)();auto fn=reinterpret_cast<Fn>(Resolve("GetdfDIJoystick"));return fn?fn():nullptr;
}
BOOL WINAPI DllMain(HINSTANCE module,DWORD reason,LPVOID) {
    if(reason==DLL_PROCESS_ATTACH) selfModule=module;
    return TRUE;
}
