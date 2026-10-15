param(
    [ValidateSet('status','on','off','toggle','for','cancel-timer','settings','timer','exit','diagnose')][string]$Command = 'status',
    [ValidateRange(1,10080)][int]$Minutes = 30,
    [string]$Executable = (Join-Path $env:LOCALAPPDATA 'Programs\QuietPC\QuietPC.exe')
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Executable)) { throw 'Install Quiet PC first, or pass -Executable.' }
$output = Join-Path ([IO.Path]::GetTempPath()) ('quietpc-reply-' + [guid]::NewGuid().ToString('N') + '.json')
try {
    $arguments = '--' + $Command
    if ($Command -eq 'for') { $arguments += ' ' + $Minutes }
    $arguments += ' --output "' + $output + '"'
    $process = Start-Process -FilePath $Executable -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if (-not (Test-Path -LiteralPath $output)) { throw 'Quiet PC returned no reply. Check its local log.' }
    $reply = Get-Content -LiteralPath $output -Raw
    Write-Output $reply
    if ($process.ExitCode -ne 0) { exit $process.ExitCode }
} finally { if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output } }
