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
$sourceArchiveName = 'BepInEx-source-3fab71a1914132a1ce3a545caf3192da603f2258.zip'
$sourceArchivePath = Join-Path $assetsDirectory $sourceArchiveName
$sourceArchiveUri = 'https://github.com/BepInEx/BepInEx/archive/3fab71a1914132a1ce3a545caf3192da603f2258.zip'
$sourceArchiveHash = '12781A0A4564E99F8B31F290C28199CE30F81AA2A236991A4A7DF4111AC0D45C'
$publishPath = Join-Path $projectRoot 'installer\ZeroParades.LanguageToggle.Setup\bin\Release\net8.0-windows\win-x64\publish'
$distPath = Join-Path $projectRoot 'dist'
$distExecutable = Join-Path $distPath 'ZeroParades.LanguageToggle.Setup.exe'
$distChecksums = Join-Path $distPath 'SHA256SUMS.txt'
$distReleaseNotes = Join-Path $distPath 'RELEASE_NOTES_v0.2.2.md'
$distThirdPartyNotices = Join-Path $distPath 'THIRD-PARTY-NOTICES.md'
$distBepInExLicense = Join-Path $distPath 'BepInEx-LICENSE.txt'
$distBepInExSource = Join-Path $distPath $sourceArchiveName
$pluginUiTest = Join-Path $projectRoot 'tests\verify-plugin-settings-ui.ps1'
$releaseNotes = Join-Path $projectRoot 'RELEASE_NOTES_v0.2.2.md'
$thirdPartyNotices = Join-Path $assetsDirectory 'THIRD-PARTY-NOTICES.md'
$bepInExLicense = Join-Path $assetsDirectory 'BepInEx-LICENSE.txt'
$releaseVersion = '0.2.2'

function Invoke-DotNet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed: dotnet $($Arguments -join ' ')"
    }
}

function Assert-ReleaseProductVersion {
    param(
        [string]$ArtifactName,
        [string]$ActualVersion
    )

    $matchesRelease = [string]::Equals($ActualVersion, $releaseVersion, [StringComparison]::Ordinal) -or
        $ActualVersion.StartsWith("$releaseVersion+", [StringComparison]::Ordinal)
    if (-not $matchesRelease) {
        throw "$ArtifactName product version mismatch. Expected $releaseVersion, found $ActualVersion."
    }
}

Write-Host 'Building plugin payload...'
& $pluginUiTest
Invoke-DotNet @('build', $pluginProject, '-c', 'Release', '-t:Rebuild')
$pluginOutput = Join-Path $projectRoot 'bin\Release\ZeroParades.LanguageToggle.dll'
$pluginVersion = (Get-Item -LiteralPath $pluginOutput).VersionInfo.ProductVersion
Assert-ReleaseProductVersion -ArtifactName 'Plugin' -ActualVersion $pluginVersion

New-Item -ItemType Directory -Force -Path $assetsDirectory | Out-Null
if (-not (Test-Path -LiteralPath $archivePath)) {
    Write-Host 'Downloading pinned official BepInEx payload...'
    Invoke-WebRequest -UseBasicParsing -Uri $archiveUri -OutFile $archivePath
}

$actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath).Hash
if (-not [string]::Equals($actualHash, $archiveHash, [StringComparison]::OrdinalIgnoreCase)) {
    throw "BepInEx payload checksum mismatch. Expected $archiveHash, found $actualHash."
}

if (-not (Test-Path -LiteralPath $sourceArchivePath)) {
    Write-Host 'Downloading pinned BepInEx source archive for Release compliance assets...'
    Invoke-WebRequest -UseBasicParsing -Uri $sourceArchiveUri -OutFile $sourceArchivePath
}

$actualSourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $sourceArchivePath).Hash
if (-not [string]::Equals($actualSourceHash, $sourceArchiveHash, [StringComparison]::OrdinalIgnoreCase)) {
    throw "BepInEx source archive checksum mismatch. Expected $sourceArchiveHash, found $actualSourceHash."
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
Copy-Item -LiteralPath $sourceArchivePath -Destination $distBepInExSource

$output = Get-Item -LiteralPath $distExecutable
$outputVersion = $output.VersionInfo.ProductVersion
Assert-ReleaseProductVersion -ArtifactName 'Installer' -ActualVersion $outputVersion
$releaseAssets = @(
    $distExecutable,
    $distReleaseNotes,
    $distThirdPartyNotices,
    $distBepInExLicense,
    $distBepInExSource
)
$checksumLines = foreach ($asset in $releaseAssets) {
    $assetItem = Get-Item -LiteralPath $asset
    $assetHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $asset).Hash
    "$assetHash  $($assetItem.Name)"
}
Set-Content -LiteralPath $distChecksums -Encoding ASCII -Value $checksumLines
Write-Host "Installer ready: $($output.FullName)"
Write-Host "Installer bytes: $($output.Length)"
Write-Host "Release assets ready: $distPath"
