param([switch]$Native)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if ($Native -and (Get-Process QuietPC -ErrorAction SilentlyContinue)) { throw 'Exit Quiet PC before running live tests. They briefly change notifications and playback mute.' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Path "$project\build" -Force | Out-Null
$sources = @("$project\src\NotificationControl.cs", "$project\src\AudioControl.cs", "$project\src\QuietEngine.cs", "$project\tests\EngineTests.cs")
& $compiler /nologo /target:exe /platform:x64 /optimize+ /warnaserror+ /reference:System.Web.Extensions.dll "/out:$project\build\Tests.exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
if ($Native) { & "$project\build\Tests.exe" --native } else { & "$project\build\Tests.exe" }
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
if (Test-Path -LiteralPath "$project\build\QuietPC.exe") { & "$project\tests\CommandLine.Tests.ps1" -Executable "$project\build\QuietPC.exe" }
