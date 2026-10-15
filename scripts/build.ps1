$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $project 'build'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The Windows x64 .NET Framework C# compiler is required.' }
New-Item -ItemType Directory -Path $output -Force | Out-Null
$sources = @(Get-ChildItem -LiteralPath "$project\src" -Filter '*.cs' | Select-Object -ExpandProperty FullName)
$references = @('/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll', '/reference:System.Web.Extensions.dll')
& $compiler /nologo /target:exe /main:IconMaker /platform:x64 @references "/out:$output\IconMaker.exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Icon build failed.' }
& "$output\IconMaker.exe" "$output\QuietPC.ico" "$output\QuietPC.png"
if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed.' }
& $compiler /nologo /target:winexe /main:QuietPC.Program /platform:x64 /optimize+ /warnaserror+ @references "/win32manifest:$project\src\app.manifest" "/win32icon:$output\QuietPC.ico" "/out:$output\QuietPC.exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Quiet PC build failed.' }
Write-Output "Built $output\QuietPC.exe"
