$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$taskCompiler="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$taskFixture=Join-Path $taskRoot 'build\proxy-fixture'
New-Item -ItemType Directory -Force "$taskFixture\KOF13HITBOX" | Out-Null
$taskVs=& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
$taskBatch=Join-Path $taskRoot 'build\test-compile.cmd'
[IO.File]::WriteAllText($taskBatch,@"
@echo off
call "$taskVs\VC\Auxiliary\Build\vcvars32.bat" >nul
cd /d "$taskFixture"
cl /nologo /MT /O2 /EHsc "$taskRoot\tests\proxy-test.cpp" /link /OUT:"$taskFixture\game.exe" dxguid.lib ole32.lib
"@,[Text.Encoding]::ASCII)
& $env:ComSpec /c $taskBatch
if($LASTEXITCODE -ne 0){throw 'Proxy harness build failed'}
Copy-Item "$taskRoot\dinput8.dll" $taskFixture
& $taskCompiler /nologo /target:winexe /platform:x64 "/out:$taskFixture\KOF13HITBOX\KOF13HITBOX.exe" "$taskRoot\tests\LaunchRecorder.cs"
if($LASTEXITCODE -ne 0){throw 'Recorder build failed'}
$taskLog=Join-Path $taskFixture 'KOF13HITBOX\launches.txt'
if(Test-Path -LiteralPath $taskLog){Remove-Item -LiteralPath $taskLog}
Push-Location $taskFixture
try{
 & .\game.exe
 if($LASTEXITCODE -ne 0){throw "Proxy integration failed: $LASTEXITCODE"}
 if(!(Test-Path -LiteralPath $taskLog) -or @(Get-Content -LiteralPath $taskLog).Count -ne 1){throw 'Expected exactly one automatic launch'}
 Set-Content -LiteralPath "$taskFixture\KOF13HITBOX\disabled.txt" -Value 'test'
 & .\game.exe
 if($LASTEXITCODE -ne 0 -or @(Get-Content -LiteralPath $taskLog).Count -ne 1){throw 'Disabled-marker test failed'}
 Remove-Item -LiteralPath "$taskFixture\KOF13HITBOX\disabled.txt"
 Copy-Item .\game.exe .\other.exe
 & .\other.exe
 if($LASTEXITCODE -ne 0 -or @(Get-Content -LiteralPath $taskLog).Count -ne 1){throw 'Non-game host test failed'}
 Move-Item -LiteralPath "$taskFixture\KOF13HITBOX\KOF13HITBOX.exe" -Destination "$taskFixture\KOF13HITBOX\recorder.exe"
 & .\game.exe
 if($LASTEXITCODE -ne 0 -or @(Get-Content -LiteralPath $taskLog).Count -ne 1){throw 'Missing-viewer fallback failed'}
 Move-Item -LiteralPath "$taskFixture\KOF13HITBOX\recorder.exe" -Destination "$taskFixture\KOF13HITBOX\KOF13HITBOX.exe"
}finally{Pop-Location}
$taskSources=Get-ChildItem "$taskRoot\src" -Filter *.cs | Select-Object -ExpandProperty FullName
& $taskCompiler /nologo /target:winexe /main:AutoLifecycleTests /define:AUTOLOAD_TESTS /platform:x64 /optimize+ "/out:$taskRoot\AutoLifecycleTests.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.dll $taskSources "$taskRoot\tests\AutoLifecycleTests.cs"
if($LASTEXITCODE -ne 0){throw 'Lifecycle tests build failed'}
$taskTest=Start-Process -FilePath "$taskRoot\AutoLifecycleTests.exe" -WindowStyle Hidden -PassThru
if(!$taskTest.WaitForExit(30000)){throw 'Lifecycle tests timed out'}
if($taskTest.ExitCode -ne 0){Get-Content "$taskRoot\autoload-tests.txt";throw 'Lifecycle tests failed'}
Get-Content "$taskRoot\autoload-tests.txt"
Set-Content -LiteralPath "$taskRoot\proxy-tests.txt" -Value 'PASS: real system DirectInput forwarding and keyboard device creation; all six named/ordinal exports; single automatic launch with validated PID/start timestamp across ten calls; disabled marker; non-game host; missing viewer preserves input. No game input sent.'
Get-Content "$taskRoot\proxy-tests.txt"
