param([string]$RegisterPath, [string]$RegisterAnchor)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
. "$PSScriptRoot\startup-register.ps1"
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\QuietPC'
$dataDir = Join-Path $env:LOCALAPPDATA 'QuietPC'
$app = Join-Path $installDir 'QuietPC.exe'
$new = Join-Path $project 'build\QuietPC.exe'
if (-not (Test-Path -LiteralPath $new)) { & "$PSScriptRoot\build.ps1" }
$upgrading = Test-Path -LiteralPath $app
$run = Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -ErrorAction SilentlyContinue
$startup = if ($upgrading) { $null -ne $run.QuietPC } else { $true }
New-Item -ItemType Directory -Path $installDir, $dataDir -Force | Out-Null
if ($RegisterPath) {
    if (-not $RegisterAnchor) { throw '-RegisterAnchor is required with -RegisterPath.' }
    $registerText = [IO.File]::ReadAllText($RegisterPath)
    if (-not $registerText.Contains($RegisterAnchor)) { throw 'Register anchor was not found.' }
    @{ RegisterPath = [IO.Path]::GetFullPath($RegisterPath); InsertBefore = $RegisterAnchor; SourceDirectory = $project } |
        ConvertTo-Json | Set-Content -LiteralPath "$dataDir\integration.json" -Encoding UTF8
} elseif (Test-Path -LiteralPath "$dataDir\integration.json") {
    $integration = Get-Content -LiteralPath "$dataDir\integration.json" -Raw | ConvertFrom-Json
    $integration.SourceDirectory = $project
    $integration | ConvertTo-Json | Set-Content -LiteralPath "$dataDir\integration.json" -Encoding UTF8
}
# Only stop this installed executable. Saved recovery state stays untouched.
$running = @(Get-Process QuietPC -ErrorAction SilentlyContinue | Where-Object Path -EQ $app)
if ($running.Count -gt 0) {
    if ([version](Get-Item $app).VersionInfo.FileVersion -ge [version]'1.2.0.0') {
        $handoff = Start-Process -FilePath $app -ArgumentList '--handoff' -WindowStyle Hidden -PassThru -Wait
        if ($handoff.ExitCode -ne 0) { throw 'Could not stop Quiet PC safely for update.' }
    } else {
        # Versions before 1.2 have no update handoff. Their durable snapshot resumes safely.
        $running | Stop-Process
    }
    foreach ($process in $running) { if (-not $process.WaitForExit(5000)) { throw 'Quiet PC did not stop for update.' } }
}
if ($upgrading) { Copy-Item -LiteralPath $app -Destination "$installDir\QuietPC.previous.exe" -Force }
try {
    Copy-Item -LiteralPath $new -Destination $app -Force
    Copy-Item -LiteralPath "$project\README.md", "$project\LICENSE" -Destination $installDir -Force
    Set-QuietStartup $startup $app
    $shell = New-Object -ComObject WScript.Shell
    foreach ($folder in @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'))) {
        $shortcut = $shell.CreateShortcut((Join-Path $folder 'Quiet PC.lnk'))
        $shortcut.TargetPath = $app; $shortcut.Arguments = '--toggle'; $shortcut.WorkingDirectory = $installDir
        $shortcut.Description = 'Toggle notifications and playback audio'; $shortcut.IconLocation = "$app,0"; $shortcut.Save()
    }
    Start-Process -FilePath $app -ArgumentList '--background' -WindowStyle Hidden
    Start-Sleep -Milliseconds 1200
    $installed = @(Get-Process QuietPC -ErrorAction SilentlyContinue | Where-Object Path -EQ $app)
    if ($installed.Count -ne 1) { throw 'The new Quiet PC process did not stay running.' }
} catch {
    if ($upgrading -and (Test-Path -LiteralPath "$installDir\QuietPC.previous.exe")) {
        Get-Process QuietPC -ErrorAction SilentlyContinue | Where-Object Path -EQ $app | Stop-Process
        Copy-Item -LiteralPath "$installDir\QuietPC.previous.exe" -Destination $app -Force
        Start-Process -FilePath $app -ArgumentList '--background' -WindowStyle Hidden
    }
    throw
}
Write-Output "Installed: $app"
Write-Output "Start at sign-in: $startup. Existing quiet state and timer were preserved."
