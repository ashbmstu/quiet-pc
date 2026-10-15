param([Parameter(Mandatory=$true)][string]$Executable)
$ErrorActionPreference = 'Stop'
$cases = @('--for 0', '--for 10081', '--for nope', '--for', '--unknown')
foreach ($arguments in $cases) {
    $path = Join-Path ([IO.Path]::GetTempPath()) ('quietpc-validation-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        $process = Start-Process -FilePath $Executable -ArgumentList ($arguments + ' --output "' + $path + '"') -WindowStyle Hidden -PassThru -Wait
        if ($process.ExitCode -ne 1) { throw "Expected exit 1 for $arguments" }
        if (-not (Test-Path -LiteralPath $path)) { throw "No structured error for $arguments" }
        $reply = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
        if ($reply.Ok -ne $false -or -not $reply.Error) { throw "Invalid error reply for $arguments" }
        Write-Output "PASS CLI rejects $arguments with JSON error"
    } finally { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path } }
}
