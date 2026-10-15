$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\startup-register.ps1"
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\QuietPC'
$app = Join-Path $installDir 'QuietPC.exe'
if (Test-Path -LiteralPath $app) {
    $result = Start-Process -FilePath $app -ArgumentList '--exit' -WindowStyle Hidden -Wait -PassThru
    if ($result.ExitCode -ne 0) { throw 'Restoration is pending. Reconnect audio devices and retry. Nothing was uninstalled.' }
    $running = @(Get-Process QuietPC -ErrorAction SilentlyContinue | Where-Object Path -EQ $app)
    foreach ($process in $running) { if (-not $process.WaitForExit(5000)) { throw 'Quiet PC is still running.' } }
}
$statePath = Join-Path $env:LOCALAPPDATA 'QuietPC\state.json'
if (Test-Path -LiteralPath $statePath) {
    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    if ($state.Active -or $state.RestoreNotifications -or @($state.Devices.PSObject.Properties).Count -gt 0) { throw 'Recovery state still needs restoration. Nothing was uninstalled.' }
}
Set-QuietStartup $false $app
foreach ($folder in @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'))) {
    $shortcut = Join-Path $folder 'Quiet PC.lnk'
    if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut }
}
foreach ($name in @('QuietPC.exe', 'QuietPC.previous.exe', 'README.md', 'README.txt', 'LICENSE')) {
    $path = Join-Path $installDir $name
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
}
Write-Output 'Quiet PC removed. Source, preferences and recovery history were kept.'
