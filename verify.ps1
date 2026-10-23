$ErrorActionPreference = 'Stop'
$brewRoot = $PSScriptRoot
$brewApp = Join-Path $brewRoot 'dist\BrewLog.exe'
$brewChecks = Join-Path $brewRoot 'checks'
New-Item -ItemType Directory -Force -Path $brewChecks | Out-Null
$proc = Start-Process -FilePath $brewApp -ArgumentList @('--self-test',$brewChecks) -WindowStyle Hidden -Wait -PassThru
if ($proc.ExitCode -ne 0) { Get-Content (Join-Path $brewChecks 'self-test.txt'); throw 'Self-test failed' }
Get-Content (Join-Path $brewChecks 'self-test.txt')
Add-Type -AssemblyName System.IO.Compression
$assembly = [Reflection.Assembly]::LoadFile((Join-Path $brewRoot 'dist\BrewLogCalculator-Setup.exe'))
$stream = $assembly.GetManifestResourceStream('payload.zip')
$zip = [IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read)
try {
    foreach ($entry in $zip.Entries) {
        $inputStream = $entry.Open()
        $hasher = [Security.Cryptography.SHA256]::Create()
        try {
            $actual = [BitConverter]::ToString($hasher.ComputeHash($inputStream)).Replace('-','')
            $path = Join-Path (Join-Path $brewRoot 'dist') $entry.Name
            $expected = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
            if ($actual -ne $expected) { throw ('Installer payload differs: ' + $entry.Name) }
            Write-Output ('Installer payload verified: ' + $entry.Name)
        } finally { $inputStream.Dispose(); $hasher.Dispose() }
    }
} finally { $zip.Dispose(); $stream.Dispose() }
