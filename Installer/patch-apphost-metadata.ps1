param(
    [Parameter(Mandatory = $true)]
    [string]$ExePath
)

$rceditPath = Join-Path $PSScriptRoot 'rcedit-x64.exe'

if (-not (Test-Path $ExePath)) {
    throw "Target exe not found: $ExePath"
}

if (-not (Test-Path $rceditPath)) {
    throw "rcedit not found: $rceditPath"
}

& $rceditPath $ExePath `
    --set-version-string OriginalFilename 'SonyDevBypass.exe' `
    --set-version-string InternalName 'SonyDevBypass.exe' `
    --set-version-string FileDescription 'SonyDev Bypass' `
    --set-version-string ProductName 'SonyDev Bypass' `
    --set-version-string CompanyName 'SonyDev' `
    --set-version-string LegalCopyright 'Copyright (c) 2026 SonyDev and contributors.' `
    --set-version-string Comments 'Modern desktop client for catalog sync and recursive bypass downloads.'

if ($LASTEXITCODE -ne 0) {
    throw "rcedit failed with exit code $LASTEXITCODE."
}
