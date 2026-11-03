# SPDX-License-Identifier: MIT
$ErrorActionPreference = 'Stop'
# Optional artwork regeneration; normal builds use the checked-in resources.
& python (Join-Path $PSScriptRoot 'make-icon.py')
if ($LASTEXITCODE -ne 0) { throw 'Logo generation failed. Install Pillow 12.2.0 in a local Python environment.' }
