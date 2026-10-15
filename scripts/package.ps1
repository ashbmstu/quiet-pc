$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$app = Join-Path $project 'build\QuietPC.exe'
if (-not (Test-Path -LiteralPath $app)) { throw 'Run scripts/build.ps1 first.' }
$version = (Get-Item $app).VersionInfo.FileVersion
$dist = Join-Path $project 'dist'
$stage = Join-Path $dist ('stage-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path "$stage\build", "$stage\scripts", "$stage\docs", "$stage\assets" -Force | Out-Null
Copy-Item -LiteralPath $app -Destination "$stage\build"
Copy-Item -LiteralPath "$PSScriptRoot\Install.cmd" -Destination "$stage\Install.cmd"
foreach ($name in @('install.ps1','uninstall.ps1','startup-register.ps1','quietpc.ps1')) { Copy-Item -LiteralPath "$PSScriptRoot\$name" -Destination "$stage\scripts" }
Copy-Item -LiteralPath "$project\LICENSE", "$project\README.md", "$project\CHANGELOG.md", "$project\CONTRIBUTING.md", "$project\SECURITY.md", "$project\THIRD_PARTY_NOTICES.md" -Destination $stage
Get-ChildItem -LiteralPath "$project\docs" -Filter '*.md' | Copy-Item -Destination "$stage\docs"
Get-ChildItem -LiteralPath "$project\assets" -File | Copy-Item -Destination "$stage\assets"
if (Test-Path -LiteralPath "$project\assets\screenshots") { Copy-Item -LiteralPath "$project\assets\screenshots" -Destination "$stage\assets" -Recurse }
$archive = Join-Path $dist "QuietPC-$version-win-x64.zip"
Compress-Archive -Path "$stage\*" -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($archive + '.sha256', $hash + '  ' + [IO.Path]::GetFileName($archive) + [Environment]::NewLine)
# Keep staging inspectable; dist is ignored by Git.
Write-Output $archive
Write-Output "SHA256: $hash"
