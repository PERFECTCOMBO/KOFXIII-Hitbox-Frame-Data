#include "imgui.h"
#include "imgui_impl_win32.h"
#include "imgui_impl_dx11.h"
#include <d3d11.h>
#include <cstring>
#include <map>
#include <string>
#define API extern "C" __declspec(dllexport)
static ID3D11Device* device;
static ID3D11DeviceContext* context;
static IDXGISwapChain* swapchain;
static ID3D11RenderTargetView* target;
static int width,height,canvasId;
static bool ready;
static std::map<std::string,ImVec2> items;
static bool testPending=false,testDown=false;static ImVec2 testMouse;
static void Remember(const char* label){auto a=ImGui::GetItemRectMin(),b=ImGui::GetItemRectMax();items[label]=ImVec2((a.x+b.x)/2,(a.y+b.y)/2);}
API int ui_test_click(const char* label,int down){auto it=items.find(label);if(it==items.end())return 0;ImGui::GetIO().ConfigInputTrickleEventQueue=false;testMouse=it->second;testDown=down!=0;testPending=true;return 1;}

extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND,UINT,WPARAM,LPARAM);
static bool MakeTarget(){ID3D11Texture2D* texture=nullptr;if(FAILED(swapchain->GetBuffer(0,IID_PPV_ARGS(&texture))))return false;HRESULT hr=device->CreateRenderTargetView(texture,nullptr,&target);texture->Release();return SUCCEEDED(hr);}
API void ui_shutdown(){if(ready){ImGui_ImplDX11_Shutdown();ImGui_ImplWin32_Shutdown();ImGui::DestroyContext();ready=false;}if(target){target->Release();target=nullptr;}if(swapchain){swapchain->Release();swapchain=nullptr;}if(context){context->Release();context=nullptr;}if(device){device->Release();device=nullptr;}}
API int ui_init(HWND hwnd){DXGI_SWAP_CHAIN_DESC d={};d.BufferCount=2;d.BufferDesc.Format=DXGI_FORMAT_R8G8B8A8_UNORM;d.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;d.OutputWindow=hwnd;d.SampleDesc.Count=1;d.Windowed=TRUE;d.SwapEffect=DXGI_SWAP_EFFECT_DISCARD;D3D_FEATURE_LEVEL obtained;D3D_FEATURE_LEVEL levels[]={D3D_FEATURE_LEVEL_11_0,D3D_FEATURE_LEVEL_10_0};HRESULT hr=D3D11CreateDeviceAndSwapChain(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,0,levels,2,D3D11_SDK_VERSION,&d,&swapchain,&device,&obtained,&context);if(FAILED(hr))hr=D3D11CreateDeviceAndSwapChain(nullptr,D3D_DRIVER_TYPE_WARP,nullptr,0,levels,2,D3D11_SDK_VERSION,&d,&swapchain,&device,&obtained,&context);if(FAILED(hr)||!MakeTarget()){ui_shutdown();return 0;}IMGUI_CHECKVERSION();ImGui::CreateContext();auto& io=ImGui::GetIO();io.IniFilename=nullptr;io.ConfigFlags|=ImGuiConfigFlags_NavEnableKeyboard;io.Fonts->AddFontFromFileTTF("C:\\Windows\\Fonts\\segoeui.ttf",17);ImGui::StyleColorsDark();auto& s=ImGui::GetStyle();s.WindowRounding=7;s.FrameRounding=4;s.ChildRounding=5;s.FramePadding=ImVec2(9,6);s.ItemSpacing=ImVec2(9,7);s.WindowPadding=ImVec2(14,12);s.Colors[ImGuiCol_WindowBg]=ImVec4(.055f,.07f,.095f,1);s.Colors[ImGuiCol_Button]=ImVec4(.12f,.23f,.30f,1);s.Colors[ImGuiCol_CheckMark]=ImVec4(.22f,.85f,.83f,1);s.Colors[ImGuiCol_SliderGrab]=s.Colors[ImGuiCol_CheckMark];if(!ImGui_ImplWin32_Init(hwnd)){ImGui::DestroyContext();ui_shutdown();return 0;}if(!ImGui_ImplDX11_Init(device,context)){ImGui_ImplWin32_Shutdown();ImGui::DestroyContext();ui_shutdown();return 0;}ready=true;return 1;}
API intptr_t ui_message(HWND h,UINT m,WPARAM w,LPARAM l){return ready?ImGui_ImplWin32_WndProcHandler(h,m,w,l):0;}
API int ui_begin(int w,int h){if(!ready||w<=0||h<=0)return 0;if(w!=width||h!=height){context->OMSetRenderTargets(0,nullptr,nullptr);if(target){target->Release();target=nullptr;}if(FAILED(swapchain->ResizeBuffers(0,w,h,DXGI_FORMAT_UNKNOWN,0))||!MakeTarget())return 0;width=w;height=h;}ImGui_ImplDX11_NewFrame();ImGui_ImplWin32_NewFrame();if(testPending){auto& io=ImGui::GetIO();io.AddFocusEvent(true);io.AddMousePosEvent(testMouse.x,testMouse.y);io.AddMouseButtonEvent(0,testDown);testPending=false;}ImGui::NewFrame();canvasId=0;ImGui::SetNextWindowPos(ImVec2(0,0));ImGui::SetNextWindowSize(ImVec2((float)w,(float)h));ImGui::Begin("KOF XIII Debugger",nullptr,ImGuiWindowFlags_NoTitleBar|ImGuiWindowFlags_NoResize|ImGuiWindowFlags_NoMove|ImGuiWindowFlags_NoSavedSettings);return 1;}
API int ui_end(){ImGui::End();ImGui::Render();float clear[]={.035f,.045f,.06f,1};context->OMSetRenderTargets(1,&target,nullptr);context->ClearRenderTargetView(target,clear);ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());return SUCCEEDED(swapchain->Present(1,0));}
API int ui_button(const char* label){bool result=ImGui::Button(label);Remember(label);return result;}
API int ui_check(const char* label,int* value){bool v=*value!=0;bool changed=ImGui::Checkbox(label,&v);Remember(label);*value=v;return changed;}
API int ui_slider(const char* label,float* value,float min,float max){return ImGui::SliderFloat(label,value,min,max,"%.2f");}
API int ui_int(const char* label,int* value,int min,int max){return ImGui::SliderInt(label,value,min,max);}
API void ui_text(const char* text){ImGui::TextUnformatted(text);}
API void ui_wrapped(const char* text){ImGui::PushTextWrapPos(0);ImGui::TextUnformatted(text);ImGui::PopTextWrapPos();}
API void ui_same(){ImGui::SameLine();}
API void ui_sep(){ImGui::Separator();}
API int ui_tabs(){return ImGui::BeginTabBar("views");}
API void ui_endtabs(){ImGui::EndTabBar();}
API int ui_tab(const char* name){bool result=ImGui::BeginTabItem(name);Remember(name);return result;}
API void ui_endtab(){ImGui::EndTabItem();}
API void ui_space(float h){ImGui::Dummy(ImVec2(1,h));}
API float ui_width(){return ImGui::GetContentRegionAvail().x;}
API float ui_height(){return ImGui::GetContentRegionAvail().y;}
API void ui_canvas(float h,float* x,float* y,float* w){auto p=ImGui::GetCursorScreenPos();*x=p.x;*y=p.y;*w=ImGui::GetContentRegionAvail().x;ImGui::PushID(canvasId++);ImGui::InvisibleButton("canvas",ImVec2(*w,h));ImGui::PopID();}
API int ui_hover(){return ImGui::IsItemHovered();}
API int ui_click(float* x,float* y){auto p=ImGui::GetIO().MousePos;*x=p.x;*y=p.y;return ImGui::IsItemHovered()&&ImGui::IsMouseClicked(0);}
API void ui_rect(float x,float y,float w,float h,unsigned color,int fill,float thickness){auto d=ImGui::GetWindowDrawList();if(fill)d->AddRectFilled(ImVec2(x,y),ImVec2(x+w,y+h),color);else d->AddRect(ImVec2(x,y),ImVec2(x+w,y+h),color,0,0,thickness);}
API void ui_line(float x,float y,float x2,float y2,unsigned color){ImGui::GetWindowDrawList()->AddLine(ImVec2(x,y),ImVec2(x2,y2),color);}
API void ui_label(float x,float y,unsigned color,const char* text){ImGui::GetWindowDrawList()->AddText(ImVec2(x,y),color,text);}
API void ui_clip(float x,float y,float w,float h){ImGui::GetWindowDrawList()->PushClipRect(ImVec2(x,y),ImVec2(x+w,y+h),true);}
API void ui_unclip(){ImGui::GetWindowDrawList()->PopClipRect();}
API int ui_pixels(unsigned char* bytes,int size){if(!target||size<width*height*4)return 0;ID3D11Resource* resource=nullptr;target->GetResource(&resource);ID3D11Texture2D* source=nullptr;resource->QueryInterface(IID_PPV_ARGS(&source));resource->Release();if(!source)return 0;D3D11_TEXTURE2D_DESC d;source->GetDesc(&d);d.Usage=D3D11_USAGE_STAGING;d.BindFlags=0;d.CPUAccessFlags=D3D11_CPU_ACCESS_READ;d.MiscFlags=0;ID3D11Texture2D* staging=nullptr;if(FAILED(device->CreateTexture2D(&d,nullptr,&staging))){source->Release();return 0;}context->CopyResource(staging,source);source->Release();D3D11_MAPPED_SUBRESOURCE m;HRESULT hr=context->Map(staging,0,D3D11_MAP_READ,0,&m);if(SUCCEEDED(hr)){for(int y=0;y<height;y++){auto src=(unsigned char*)m.pData+y*m.RowPitch;auto dst=bytes+y*width*4;for(int x=0;x<width;x++){dst[x*4]=src[x*4+2];dst[x*4+1]=src[x*4+1];dst[x*4+2]=src[x*4];dst[x*4+3]=255;}}context->Unmap(staging,0);}staging->Release();return SUCCEEDED(hr);}

API int ui_input(const char* name,int* value){return ImGui::InputInt(name,value);}

API int ui_test_info(int n){auto& io=ImGui::GetIO();return n==0?io.MouseDown[0]:n==1?ImGui::IsMouseClicked(0):n==2?(int)io.MousePos.x:(int)io.MousePos.y;}
