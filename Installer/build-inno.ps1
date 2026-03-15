param(
    [string]$IsccPath
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$issPath = Join-Path $scriptDir 'SonyDevBypass.iss'

if (-not $IsccPath) {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    )

    $IsccPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $IsccPath -or -not (Test-Path $IsccPath)) {
    throw "ISCC.exe not found. Install Inno Setup 6 or pass -IsccPath explicitly."
}

& $IsccPath $issPath
if ($LASTEXITCODE -ne 0) {
    throw "ISCC exited with code $LASTEXITCODE."
}
