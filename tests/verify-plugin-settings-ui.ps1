[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sourcePath = Join-Path $projectRoot 'src\LanguageToggleBehaviour.cs'
$source = Get-Content -Raw -Encoding UTF8 -LiteralPath $sourcePath

if ($source -match '\bGUILayout\.') {
    throw 'Settings panel still uses GUILayout, which fails at runtime in the game IL2CPP build.'
}

if ($source -notmatch '\bGUI\.Label\s*\(' -or $source -notmatch '\bGUI\.Button\s*\(') {
    throw 'Settings panel must use fixed-position GUI labels and buttons.'
}

if ($source -notmatch '\bGetCurrentSubMenu\s*\(' -or $source -notmatch '\bSettingsCategoryType\.Gameplay\b') {
    throw 'Settings panel visibility must be based on the currently selected Gameplay submenu.'
}

if ($source -match 'FindObjectsOfTypeAll<GameplaySettingsSubMenu>') {
    throw 'Settings panel must not scan every Gameplay submenu during OnGUI rendering.'
}

if ($source -notmatch '\b_uiDisabledAfterError\b' -or $source -notmatch '\bcatch\s*\(\s*Exception\b') {
    throw 'Settings panel must disable itself after a rendering failure instead of logging an exception every frame.'
}

if ($source -match 'foreach\s*\(\s*KeyCode\s+\w+\s+in\s+Enum\.GetValues<KeyCode>\(\)\s*\)') {
    throw 'Key rebinding must not allocate the KeyCode list every capture frame.'
}

Write-Output 'PASS: plugin settings panel uses supported GUI APIs and current Gameplay submenu visibility'
