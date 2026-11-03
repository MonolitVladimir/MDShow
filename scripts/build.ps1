# SPDX-License-Identifier: MIT
param([switch]$Installer)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $sdk = Join-Path $root '.tools\webview2'
    if (!(Test-Path "$sdk\lib\net462\Microsoft.Web.WebView2.Core.dll")) { throw 'Run scripts\bootstrap.ps1 first.' }
    foreach ($asset in 'assets\app.ico','web\logo.png') {
        if (!(Test-Path -LiteralPath $asset)) { throw "Missing checked-in asset: $asset. See scripts/make-icon.py for optional regeneration." }
    }
    $archives = @()
    foreach ($edition in 'installed','full','en') {
        $directory = if ($edition -eq 'installed') { 'dist\public-1.0\installed' } elseif ($edition -eq 'full') { 'dist\public-1.0\portable' } else { 'dist\public-1.0\portable-en' }
        $generated = Join-Path 'artifacts\public-1.0' $edition
        New-Item -ItemType Directory -Force $directory,$generated | Out-Null
        $cscArgs = @('/nologo','/target:winexe','/platform:x64','/optimize+','/utf8output','/codepage:65001','/warnaserror+',
            "/out:$directory\MDShow.exe",'/win32manifest:src\app.manifest','/win32icon:assets\app.ico',
            '/r:System.dll','/r:System.Core.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Web.Extensions.dll',
            "/r:$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\Microsoft.VisualBasic.dll",
            "/r:$sdk\lib\net462\Microsoft.Web.WebView2.Core.dll", "/r:$sdk\lib\net462\Microsoft.Web.WebView2.WinForms.dll")
        if ($edition -eq 'en') { $cscArgs += '/define:ENGLISH_ONLY' }
        if ($edition -eq 'installed') { $cscArgs += '/define:INSTALLED' }
        foreach ($file in Get-ChildItem web -Recurse -File) {
            $relative = $file.FullName.Substring((Join-Path $root 'web').Length + 1).Replace('\','.')
            $resource = $file.FullName
            if ($relative -eq 'locales.json') {
                $catalogs = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
                if ($edition -eq 'en') { $catalogs = [ordered]@{ 'en-US' = $catalogs.'en-US' } }
                $resource = Join-Path $generated 'locales.json'
                # Compact only the embedded copy; source translations remain readable.
                $catalogs | ConvertTo-Json -Depth 5 -Compress | Set-Content -LiteralPath $resource -Encoding utf8
            } elseif ($relative -eq 'index.html' -and $edition -eq 'en') {
                $resource = Join-Path $generated 'index.html'
                $html = Get-Content -LiteralPath $file.FullName -Raw
                $html = $html -replace '<select id="language">.*?</select>', '<select id="language"><option value="en-US">English</option></select>'
                $html | Set-Content -LiteralPath $resource -Encoding utf8
            }
            $cscArgs += "/resource:$resource,web.$relative"
        }
        $cscArgs += @('src\Document.cs','src\Localization.cs','src\Profile.cs','src\Dialogs.cs','src\StartupTrace.cs','src\Program.cs')
        & $compiler @cscArgs
        if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $edition" }
        Copy-Item "$sdk\lib\net462\Microsoft.Web.WebView2.Core.dll","$sdk\lib\net462\Microsoft.Web.WebView2.WinForms.dll","$sdk\runtimes\win-x64\native\WebView2Loader.dll" $directory -Force
        Copy-Item src\MDShow.exe.config,THIRD-PARTY-NOTICES.txt,LICENSE,BRANDING.md,assets\Welcome.md $directory -Force
        Copy-Item -LiteralPath assets\DISTRIBUTION-README_EN.md -Destination (Join-Path $directory 'README.md') -Force
        $names = @('MDShow.exe','MDShow.exe.config','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll','THIRD-PARTY-NOTICES.txt','README.md','Welcome.md','LICENSE','BRANDING.md')
        if ($edition -ne 'en') {
            Copy-Item -LiteralPath assets\DISTRIBUTION-README_RU.md -Destination (Join-Path $directory 'README_RU.md') -Force
            $names += 'README_RU.md'
        }
        if ($edition -eq 'installed') {
            & $compiler /nologo /target:winexe /platform:x64 /optimize+ /utf8output /codepage:65001 /warnaserror+ /define:INSTALLED "/out:$directory\MDShow.SetupGuard.exe" /win32manifest:src\app.manifest /win32icon:assets\app.ico src\Profile.cs src\SetupGuard.cs
            if ($LASTEXITCODE -ne 0) { throw 'Setup guard compilation failed' }
            # Installed payload uses the read-only gate created by Setup in Program Files.
            if (!(Test-Path -LiteralPath "$directory\installation.lock")) { [IO.File]::WriteAllBytes((Join-Path $root "$directory\installation.lock"), [byte[]]@()) }
            continue
        }
        # Enumerate release files explicitly; private data and build outputs are excluded.
        $files = $names | ForEach-Object { Get-Item -LiteralPath (Join-Path $directory $_) }
        $archive = if ($edition -eq 'full') { 'dist\MDShow-1.0-portable-x64.zip' } else { 'dist\MDShow-1.0-portable-en-x64.zip' }
        Compress-Archive -LiteralPath $files.FullName -DestinationPath $archive -Force
        $archives += Get-Item -LiteralPath $archive
    }
    if ($Installer) {
        $iscc = Join-Path $root '.tools\inno\ISCC.exe'
        if (!(Test-Path $iscc)) { throw 'Run scripts\bootstrap.ps1 -Installer first.' }
        & "$PSScriptRoot\generate-installer-messages.ps1"
        & $iscc installer\MDShow.iss
        if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
        $archives += Get-Item -LiteralPath dist\MDShow-1.0-Setup-x64.exe
    }
    $archives | Get-FileHash -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLower())  $([IO.Path]::GetFileName($_.Path))" } | Set-Content -LiteralPath dist\public-1.0\SHA256SUMS.txt -Encoding ascii
    $archives | Select-Object Name,Length
}
finally { Pop-Location }
