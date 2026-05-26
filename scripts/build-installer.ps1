[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$pluginProject = Join-Path $projectRoot 'ZeroParades.LanguageToggle.csproj'
$installerProject = Join-Path $projectRoot 'installer\ZeroParades.LanguageToggle.Setup\ZeroParades.LanguageToggle.Setup.csproj'
$testProject = Join-Path $projectRoot 'tests\ZeroParades.LanguageToggle.Setup.Tests\ZeroParades.LanguageToggle.Setup.Tests.csproj'
$assetsDirectory = Join-Path $projectRoot 'installer\ZeroParades.LanguageToggle.Setup\Assets'
$archiveName = 'BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.755+3fab71a.zip'
$archivePath = Join-Path $assetsDirectory $archiveName
$archiveUri = 'https://builds.bepinex.dev/projects/bepinex_be/755/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.755%2B3fab71a.zip'
$archiveHash = '3616D6A67F5F595973EC4AA7BD7EDAF7F799D5BB9926F7146A6DCC7B4ABF478F'
$publishPath = Join-Path $projectRoot 'installer\ZeroParades.LanguageToggle.Setup\bin\Release\net8.0-windows\win-x64\publish'
$distPath = Join-Path $projectRoot 'dist'
$distExecutable = Join-Path $distPath 'ZeroParades.LanguageToggle.Setup.exe'
$distChecksums = Join-Path $distPath 'SHA256SUMS.txt'
$distReleaseNotes = Join-Path $distPath 'RELEASE_NOTES_v0.2.2.md'
$distThirdPartyNotices = Join-Path $distPath 'THIRD-PARTY-NOTICES.md'
$distBepInExLicense = Join-Path $distPath 'BepInEx-LICENSE.txt'
$pluginUiTest = Join-Path $projectRoot 'tests\verify-plugin-settings-ui.ps1'
$releaseNotes = Join-Path $projectRoot 'RELEASE_NOTES_v0.2.2.md'
$thirdPartyNotices = Join-Path $assetsDirectory 'THIRD-PARTY-NOTICES.md'
$bepInExLicense = Join-Path $assetsDirectory 'BepInEx-LICENSE.txt'

function Invoke-DotNet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed: dotnet $($Arguments -join ' ')"
    }
}

Write-Host 'Building plugin payload...'
& $pluginUiTest
Invoke-DotNet @('build', $pluginProject, '-c', 'Release', '-t:Rebuild')

New-Item -ItemType Directory -Force -Path $assetsDirectory | Out-Null
if (-not (Test-Path -LiteralPath $archivePath)) {
    Write-Host 'Downloading pinned official BepInEx payload...'
    Invoke-WebRequest -UseBasicParsing -Uri $archiveUri -OutFile $archivePath
}

$actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath).Hash
if (-not [string]::Equals($actualHash, $archiveHash, [StringComparison]::OrdinalIgnoreCase)) {
    throw "BepInEx payload checksum mismatch. Expected $archiveHash, found $actualHash."
}

Write-Host 'Running installer service tests...'
Invoke-DotNet @('run', '--project', $testProject, '-c', 'Release')

Write-Host 'Publishing self-contained single-file installer...'
Invoke-DotNet @('publish', $installerProject, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true')

$normalizedRoot = $projectRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$normalizedDist = [IO.Path]::GetFullPath($distPath)
if (-not $normalizedDist.StartsWith($normalizedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Distribution path escaped project root: $normalizedDist"
}

if (Test-Path -LiteralPath $distPath) {
    Remove-Item -LiteralPath $distPath -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $distPath | Out-Null
Copy-Item -LiteralPath (Join-Path $publishPath 'ZeroParades.LanguageToggle.Setup.exe') -Destination $distExecutable
Copy-Item -LiteralPath $releaseNotes -Destination $distReleaseNotes
Copy-Item -LiteralPath $thirdPartyNotices -Destination $distThirdPartyNotices
Copy-Item -LiteralPath $bepInExLicense -Destination $distBepInExLicense

$output = Get-Item -LiteralPath $distExecutable
$outputHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $distExecutable).Hash
Set-Content -LiteralPath $distChecksums -Encoding ASCII -Value "$outputHash  $($output.Name)"
Write-Host "Installer ready: $($output.FullName)"
Write-Host "Installer bytes: $($output.Length)"
Write-Host "Release assets ready: $distPath"
