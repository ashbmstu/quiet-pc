# Optional integration for machines that maintain a Markdown automation register.
function Set-QuietStartup([bool]$Enabled, [string]$Executable) {
    $configPath = Join-Path $env:LOCALAPPDATA 'QuietPC\integration.json'
    $original = $null
    if (Test-Path -LiteralPath $configPath) {
        $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
        $original = [IO.File]::ReadAllText($config.RegisterPath)
        $marker = '<!-- live-state:begin -->'
        $index = $original.IndexOf($marker)
        if ($index -lt 0) { $manual = $original; $generated = '' } else { $manual = $original.Substring(0, $index); $generated = $original.Substring($index) }
        $manual = [regex]::Replace($manual, '(?m)^\| HKCU Run \| `QuietPC` \|.*\r?\n', '')
        if ($Enabled) {
            $at = $manual.IndexOf([string]$config.InsertBefore)
            if ($at -lt 0) { throw 'The startup register insertion point was not found.' }
            $row = '| HKCU Run | `QuietPC` | Quiet PC tray switch at sign-in; startup configurable in Settings | `' + $Executable + ' --background`; source `' + $config.SourceDirectory + '`; remove with `scripts\uninstall.ps1` |'
            $manual = $manual.Insert($at, $row + [Environment]::NewLine)
        }
        [IO.File]::WriteAllText($config.RegisterPath, $manual + $generated, [Text.UTF8Encoding]::new($false))
    }
    try {
        $run = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
        if ($Enabled) {
            New-Item -Path $run -Force | Out-Null
            New-ItemProperty -Path $run -Name QuietPC -Value ('"' + $Executable + '" --background') -PropertyType String -Force | Out-Null
        } else { Remove-ItemProperty -Path $run -Name QuietPC -ErrorAction SilentlyContinue }
    } catch {
        if ($null -ne $original) { [IO.File]::WriteAllText($config.RegisterPath, $original, [Text.UTF8Encoding]::new($false)) }
        throw
    }
}
