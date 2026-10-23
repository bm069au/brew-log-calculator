$ErrorActionPreference = 'Stop'
$brewRoot = $PSScriptRoot
& (Join-Path $brewRoot 'build-icon.ps1') -Kind Brew
$brewCompiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$brewDist = Join-Path $brewRoot 'dist'
New-Item -ItemType Directory -Force -Path $brewDist | Out-Null
& $brewCompiler /nologo /target:winexe /platform:x64 /optimize+ "/win32icon:$brewRoot\assets\app.ico" "/win32manifest:$brewRoot\app.manifest" "/out:$brewDist\BrewLog.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Xml.dll "$brewRoot\BrewLog.cs" "$brewRoot\Branding.cs"
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed' }
Copy-Item -LiteralPath (Join-Path $brewRoot 'README.txt') -Destination $brewDist -Force
$brewPayload = Join-Path $brewRoot 'payload.zip'
Compress-Archive -LiteralPath @((Join-Path $brewDist 'BrewLog.exe'),(Join-Path $brewDist 'README.txt')) -DestinationPath $brewPayload -Force
& $brewCompiler /nologo /target:winexe /platform:x64 /optimize+ "/win32icon:$brewRoot\assets\app.ico" "/win32manifest:$brewRoot\installer.manifest" "/resource:$brewPayload,payload.zip" "/out:$brewDist\BrewLogCalculator-Setup.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "$brewRoot\Installer.cs" "$brewRoot\Branding.cs"
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
Get-Item (Join-Path $brewDist 'BrewLog.exe'),(Join-Path $brewDist 'BrewLogCalculator-Setup.exe') | Select-Object Name,Length
