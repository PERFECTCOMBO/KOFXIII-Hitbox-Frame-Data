$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$taskVswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$taskVs=& $taskVswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if(!$taskVs){throw 'Visual Studio C++ x86 Build Tools required.'}
$taskBuild=Join-Path $taskRoot 'build'
New-Item -ItemType Directory -Force $taskBuild | Out-Null
$taskBatch=Join-Path $taskBuild 'compile.cmd'
$taskCommands=@"
@echo off
call "$taskVs\VC\Auxiliary\Build\vcvars32.bat" >nul
cd /d "$taskBuild"
cl /nologo /LD /MT /O2 /EHsc /std:c++17 "$taskRoot\native\loader.cpp" /link /DEF:"$taskRoot\native\dinput8.def" /OUT:"$taskRoot\dinput8.dll" /MACHINE:X86
"@
[IO.File]::WriteAllText($taskBatch,$taskCommands,[Text.Encoding]::ASCII)
& $env:ComSpec /c $taskBatch
if($LASTEXITCODE -ne 0){throw 'Native loader build failed.'}
$taskRendererBatch=Join-Path $taskBuild 'compile-renderer.cmd'
$taskNative=Join-Path $taskRoot 'native'
$taskRendererCommands=@"
@echo off
call "$taskVs\VC\Auxiliary\Build\vcvars64.bat" >nul
cd /d "$taskBuild"
cl /nologo /LD /MT /O2 /EHsc /std:c++17 /I"$taskNative\imgui" /I"$taskNative\imgui\backends" "$taskNative\bridge.cpp" "$taskNative\imgui\imgui.cpp" "$taskNative\imgui\imgui_draw.cpp" "$taskNative\imgui\imgui_tables.cpp" "$taskNative\imgui\imgui_widgets.cpp" "$taskNative\imgui\backends\imgui_impl_win32.cpp" "$taskNative\imgui\backends\imgui_impl_dx11.cpp" /link /OUT:"$taskRoot\Kof13ImGui.dll" d3d11.lib dxgi.lib d3dcompiler.lib user32.lib gdi32.lib dwmapi.lib
"@
[IO.File]::WriteAllText($taskRendererBatch,$taskRendererCommands,[Text.Encoding]::ASCII)
& $env:ComSpec /c $taskRendererBatch
if($LASTEXITCODE -ne 0){throw 'Native renderer build failed.'}
$taskSources=Get-ChildItem "$taskRoot\src" -Filter *.cs | Select-Object -ExpandProperty FullName
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /main:ImGuiEntry /platform:x64 /optimize+ "/out:$taskRoot\KOF13HITBOX.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.dll $taskSources
if($LASTEXITCODE -ne 0){throw 'Viewer build failed.'}
