# SPDX-License-Identifier: MIT
param([switch]$Installer)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$toolsDir = Join-Path $root '.tools'
New-Item -ItemType Directory -Force $toolsDir | Out-Null
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
function Get-VerifiedDownload([string]$Url, [string]$Path, [string]$Sha256) {
    if (!(Test-Path $Path)) { Invoke-WebRequest -Uri $Url -OutFile $Path -UseBasicParsing }
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Sha256) { throw "Checksum mismatch: $Path. Download was not used." }
}
$sdkZip = Join-Path $toolsDir 'webview2.zip'
Get-VerifiedDownload 'https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.4258.31/microsoft.web.webview2.1.0.4258.31.nupkg' $sdkZip '56F7F4B8BF9AEE4B8EFEFBBDD4F67D5F74EBD1B100ED0806DA71BF76AF481AA9'
if (!(Test-Path (Join-Path $toolsDir 'webview2\lib\net462\Microsoft.Web.WebView2.Core.dll'))) {
    Expand-Archive -LiteralPath $sdkZip -DestinationPath (Join-Path $toolsDir 'webview2') -Force
}
if ($Installer) {
    $setup = Join-Path $toolsDir 'innosetup.exe'
    Get-VerifiedDownload 'https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe' $setup '9C73C3BAE7ED48D44112A0F48E66742C00090BDB5BEF71D9D3C056C66E97B732'
    $signature = Get-AuthenticodeSignature $setup
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notlike '*Pyrsys B.V.*') { throw 'Inno Setup signature validation failed.' }
    $innoDir = Join-Path $toolsDir 'inno'
    if (!(Test-Path (Join-Path $innoDir 'ISCC.exe'))) {
        $process = Start-Process -FilePath $setup -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/CURRENTUSER','/PORTABLE=1',('/DIR="' + $innoDir + '"') -WindowStyle Hidden -PassThru -Wait
        if ($process.ExitCode -ne 0) { throw "Inno Setup extraction failed: $($process.ExitCode)" }
    }
    Get-VerifiedDownload 'https://raw.githubusercontent.com/jrsoftware/issrc/is-6_7_3/Files/Languages/Unofficial/ChineseSimplified.isl' (Join-Path $innoDir 'Languages\ChineseSimplified.isl') '7D544B9BB1D142CFA11F2E5D3CC8ABE2E55F8E066C5124E3772675AA236E1278'
}
Write-Output 'Build tools ready. Run scripts\build.ps1 -Installer.'
