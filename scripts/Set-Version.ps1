<#
.SYNOPSIS
    Bumps the <Version> in src/Taciturn/Taciturn.csproj.

.PARAMETER Bump
    Which part to increment: patch (default), minor, or major.
    Bumping minor or major resets the parts below it to 0.

.EXAMPLE
    .\Set-Version.ps1
    0.1.0 -> 0.1.1

.EXAMPLE
    .\Set-Version.ps1 -Bump minor
    0.1.0 -> 0.2.0
#>
[CmdletBinding()]
param(
    [ValidateSet('patch', 'minor', 'major')]
    [string]$Bump = 'patch'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $root 'src\Taciturn\Taciturn.csproj'

if (-not (Test-Path -LiteralPath $csproj)) {
    throw "Could not find $csproj"
}

$content = Get-Content -LiteralPath $csproj -Raw
$match = [regex]::Match($content, '<Version>(\d+)\.(\d+)\.(\d+)</Version>')
if (-not $match.Success) {
    throw "Could not find a <Version>X.Y.Z</Version> element in $csproj"
}

$major = [int]$match.Groups[1].Value
$minor = [int]$match.Groups[2].Value
$patch = [int]$match.Groups[3].Value
$oldVersion = "$major.$minor.$patch"

switch ($Bump) {
    'major' { $major++; $minor = 0; $patch = 0 }
    'minor' { $minor++; $patch = 0 }
    'patch' { $patch++ }
}

$newVersion = "$major.$minor.$patch"
$newContent = $content.Remove($match.Index, $match.Length).Insert($match.Index, "<Version>$newVersion</Version>")
Set-Content -LiteralPath $csproj -Value $newContent -NoNewline

Write-Host "Version bumped ($Bump): $oldVersion -> $newVersion"
Write-Output $newVersion
