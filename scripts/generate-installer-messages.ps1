# SPDX-License-Identifier: MIT
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$catalogs = Get-Content (Join-Path $root 'web/locales.json') -Raw | ConvertFrom-Json
$languages = [ordered]@{ english='en-US'; russian='ru-RU'; german='de-DE'; arabic='ar'; chinese='zh-CN' }
$keys = 'shellOpen','shellDescription','setupContext','setupDesktop','setupLaunch','setupDotNet','webviewFailed','setupVersionBlocked','setupRunning','setupGuardFailed','profilePreserved','profileCleanupFailed'
$lines = @('; Generated from web/locales.json by scripts/generate-installer-messages.ps1', '[CustomMessages]')
foreach ($language in $languages.GetEnumerator()) {
    foreach ($key in $keys) {
        $value = $catalogs.($language.Value).$key
        if ([string]::IsNullOrWhiteSpace($value)) { throw "Missing installer translation: $($language.Value)/$key" }
        $lines += "$($language.Key).$key=$($value.Replace('%','%%').Replace("`n",'%n'))"
    }
}
$lines += '[Messages]'
foreach ($language in $languages.GetEnumerator()) {
    $value = $catalogs.($language.Value).'uninstallConfirm'
    if ([string]::IsNullOrWhiteSpace($value) -or !$value.Contains('%1')) { throw "Missing uninstall confirmation: $($language.Value)" }
    $lines += "$($language.Key).ConfirmUninstall=$value"
}
New-Item -ItemType Directory -Force (Join-Path $root 'artifacts') | Out-Null
$lines | Set-Content (Join-Path $root 'artifacts/installer-messages.iss') -Encoding utf8
