# Zero Parades Language Toggle v0.2.2

[简体中文](#简体中文) | [English](#english)

## 简体中文

### 功能

- 在游戏提供的任意两种文字语言之间一键切换。
- 在 `Settings > Gameplay` 中配置目标语言与快捷键。
- 单文件安装器自动检测 Steam 游戏目录，并在需要时安装内嵌的 BepInEx IL2CPP。

### 本版修复

- 设置面板仅在 `Settings > Gameplay` 页面显示。
- 修复 IL2CPP 环境下设置面板 UI 异常。
- 降低设置界面的每帧查询与按键重绑开销。
- 自动迁移旧 DLL 备份，避免 BepInEx 重复扫描历史插件。

### 安装

下载并运行 `ZeroParades.LanguageToggle.Setup.exe`，选择游戏目录后点击安装。

### 校验

```text
SHA-256: 0F8A33B6E986E3A13272BF6E6E3EBBA2FBC0F8AD67DDA3504C56EE725720D643
```

## English

### Features

- Switch the game display language between any two available choices with one key.
- Configure the language pair and shortcut in `Settings > Gameplay`.
- The single-file installer detects Steam game directories and deploys embedded BepInEx IL2CPP when needed.

### Fixes

- The panel now appears only in `Settings > Gameplay`.
- Fixed an IL2CPP UI exception in the settings panel.
- Reduced per-frame settings UI lookup and rebinding overhead.
- Migrates older DLL backups so BepInEx does not scan historical plugin copies.

### Installation

Download and run `ZeroParades.LanguageToggle.Setup.exe`, select the game directory, and install.

### Verification

```text
SHA-256: 0F8A33B6E986E3A13272BF6E6E3EBBA2FBC0F8AD67DDA3504C56EE725720D643
```

## Third-Party Component

The installer embeds BepInEx under the GNU Lesser General Public License version 2.1. This Release includes `THIRD-PARTY-NOTICES.md` and `BepInEx-LICENSE.txt`.
