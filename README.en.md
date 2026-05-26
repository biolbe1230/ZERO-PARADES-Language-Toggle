# Zero Parades Language Toggle

[简体中文](README.md) | [English](README.en.md)

A BepInEx IL2CPP plugin for `ZERO PARADES: For Dead Spies` that switches the game's display language between any two available choices with one key, and lets players configure the pair and shortcut in `Settings > Gameplay`.

## Features

- Choose any two text languages provided by the game.
- Default pair: Simplified Chinese (`zh_cn`) and English (`en`).
- Default shortcut: `Q`.
- Configure languages and the shortcut in `Settings > Gameplay`.
- Language changes are saved through the game's native settings system.

This plugin switches the complete display language. It does not show two languages simultaneously.

## Recommended Installation: Single-File Installer

Download from [GitHub Releases](../../releases):

```text
ZeroParades.LanguageToggle.Setup.exe
```

The installer is a self-contained `.NET 8 WinForms` single-file executable. Players do not need to install .NET or download BepInEx separately.

1. Exit the game.
2. Run `ZeroParades.LanguageToggle.Setup.exe`.
3. Confirm the detected game directory, or select the directory containing `ZeroParades.exe`.
4. Click `Install / Update`.
5. Start the game and open `Settings > Gameplay` to configure the language pair and shortcut.

The installer detects the default Steam library and additional Steam library directories recorded in `libraryfolders.vdf`.

### Handling Existing Installations

- If BepInEx is absent, the installer deploys the embedded official `BepInEx Unity IL2CPP Windows x64 6.0.0-be.755+3fab71a` build and then installs the plugin.
- If a complete BepInEx IL2CPP installation exists, the installer preserves it and other mods, installing or upgrading only this plugin.
- If an older plugin DLL exists, it is backed up to `BepInEx\config\ZeroParades.LanguageToggle\backup` before replacement. Legacy backups under the scanned plugin directory are migrated automatically.
- Existing language and shortcut configuration is preserved.
- If incomplete or conflicting BepInEx files are detected, the installer aborts before writing files.

Configuration file:

```text
<game directory>\BepInEx\config\com.codexscholar.zeroparades.languagetoggle.cfg
```

Installer logs:

```text
%TEMP%\ZeroParades.LanguageToggle.Setup\
```

## Usage

### Default Setup

After installation, press `Q` in game to switch between:

- Simplified Chinese (`zh_cn`)
- English (`en`)

### Choose Any Two Languages

1. Open `Settings > Gameplay`.
2. Use the game's existing `Language` dropdown to select the first target language.
3. In the `LANGUAGE TOGGLE SHORTCUT` panel, click `Use current` beside `Target A`.
4. Select the second language in the existing `Language` dropdown.
5. Click `Use current` beside `Target B`.
6. Click `Switch now` to test the result.

`Target A` and `Target B` must be different.

### Rebind the Shortcut

1. Open `Settings > Gameplay`.
2. Click `Rebind` in the `LANGUAGE TOGGLE SHORTCUT` panel.
3. Press the new shortcut key.

Press `Esc` while waiting for a key to cancel rebinding.

The plugin panel appears only while `Settings > Gameplay` is active.

## Uninstall

Run the installer and select `Uninstall Plugin`. It removes only:

```text
<game directory>\BepInEx\plugins\ZeroParades.LanguageToggle\ZeroParades.LanguageToggle.dll
```

BepInEx, other plugins, configuration, and backups are preserved.

## Build From Source

Building the plugin requires a game directory where BepInEx IL2CPP has already run once, because the build references assemblies in `BepInEx\core` and `BepInEx\interop`.

```powershell
dotnet build .\ZeroParades.LanguageToggle.csproj -c Release
```

With a non-default Steam library:

```powershell
dotnet build .\ZeroParades.LanguageToggle.csproj -c Release `
  -p:GameRoot="D:\SteamLibrary\steamapps\common\Zero Parades"
```

Build the self-contained installer:

```powershell
.\scripts\build-installer.ps1
```

Output:

```text
dist\ZeroParades.LanguageToggle.Setup.exe
```

The build script downloads the pinned official BepInEx payload only when it is not already present and validates its SHA-256 checksum.

## Third-Party Component

The installer redistributes BepInEx. Version, source, official download, checksum, and license details are recorded in:

```text
installer\ZeroParades.LanguageToggle.Setup\Assets\THIRD-PARTY-NOTICES.md
installer\ZeroParades.LanguageToggle.Setup\Assets\BepInEx-LICENSE.txt
```

BepInEx is distributed under the GNU Lesser General Public License version 2.1.

## Release Verification

Use the `SHA256SUMS.txt` Release asset to verify the installer and redistributed third-party files.

The Release also provides `THIRD-PARTY-NOTICES.md`, `BepInEx-LICENSE.txt`, and the corresponding BepInEx source archive.

## Troubleshooting

### The Plugin Panel Is Missing

- Confirm that `Settings > Gameplay` is currently open.
- Confirm the plugin DLL is located under `BepInEx\plugins\ZeroParades.LanguageToggle`.
- Check the BepInEx log for plugin load or compatibility errors.

### The Shortcut Does Not Change Language

- Ensure `Target A` and `Target B` are different.
- Click `Switch now` to verify language switching itself.
- Rebind the shortcut if it conflicts with the game or another plugin.

### A Language Is Not Listed

The plugin uses the game's original `Language` dropdown. Only languages shipped by the current game version can be configured.
