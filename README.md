# Zero Parades Language Toggle

[简体中文](README.md) | [English](README.en.md)

`ZERO PARADES: For Dead Spies` 的 BepInEx IL2CPP 插件，可在任意两种游戏内文字语言之间一键切换，并在 `Settings > Gameplay` 页面中修改切换按键。

## 功能

- 选择任意两种游戏已提供的文本语言作为切换目标。
- 默认切换组合：简体中文（`zh_cn`）和 English（`en`）。
- 默认快捷键：`Q`。
- 在游戏的 `Settings > Gameplay` 中设置目标语言与快捷键。
- 语言切换通过游戏原生设置系统保存。

本插件切换的是完整显示语言，不提供双语同时显示。

## 推荐安装：单文件安装器

从 [GitHub Releases](../../releases) 下载：

```text
ZeroParades.LanguageToggle.Setup.exe
```

安装器是 `.NET 8 WinForms` 自包含单文件程序，玩家无需安装 .NET，也无需单独下载 BepInEx。

1. 退出游戏。
2. 双击运行 `ZeroParades.LanguageToggle.Setup.exe`。
3. 确认自动识别出的游戏目录，或点击 `浏览...` 选择包含 `ZeroParades.exe` 的游戏根目录。
4. 点击 `安装/更新`。
5. 安装完成后启动游戏，进入 `Settings > Gameplay` 设置语言组合与快捷键。

安装器会检测 Steam 默认库以及 `libraryfolders.vdf` 中记录的其他 Steam 库目录。

### 安装器如何处理已有环境

- 未安装 BepInEx：安装器会内嵌安装官方 `BepInEx Unity IL2CPP Windows x64 6.0.0-be.755+3fab71a`，再安装插件。
- 已安装完整 BepInEx IL2CPP：安装器不会覆盖 BepInEx 或其他 Mod，只安装/更新本插件。
- 已安装旧版插件：旧 `ZeroParades.LanguageToggle.dll` 会备份到 `BepInEx\config\ZeroParades.LanguageToggle\backup`，再写入新版 DLL；升级时会自动迁移旧版产生的插件目录备份，避免被 BepInEx 重复扫描。
- 已存在配置：语言组合和快捷键配置会保留。
- 检测到不完整或冲突的 BepInEx 文件：安装器停止写入并提示处理现有环境，避免破坏其他 Mod。

配置文件位置：

```text
<游戏目录>\BepInEx\config\com.codexscholar.zeroparades.languagetoggle.cfg
```

安装日志位置：

```text
%TEMP%\ZeroParades.LanguageToggle.Setup\
```

## 卸载

运行安装器并点击 `卸载插件`。安装器只删除：

```text
<游戏目录>\BepInEx\plugins\ZeroParades.LanguageToggle\ZeroParades.LanguageToggle.dll
```

它不会删除 BepInEx、其他插件、旧 DLL 备份或语言/快捷键配置。备份位于 `BepInEx\config\ZeroParades.LanguageToggle\backup`。

## 怎么使用

### 直接使用默认设置

安装插件并进入游戏后，按 `Q` 可在以下两种语言之间切换：

- 简体中文（`zh_cn`）
- English（`en`）

### 设置任意两个语言

1. 在游戏中打开 `Settings > Gameplay`。
2. 在游戏原有的 `Language` 下拉选项中选择第一种目标语言。
3. 在新增的 `LANGUAGE TOGGLE SHORTCUT` 面板中，点击 `Target A` 右侧的 `Use current`。
4. 使用原有 `Language` 下拉选项切换到第二种目标语言。
5. 点击 `Target B` 右侧的 `Use current`。
6. 点击 `Switch now` 立即验证切换效果。

`Target A` 与 `Target B` 必须不同；若设置为相同语言，切换会被禁用。

### 修改快捷键

1. 打开 `Settings > Gameplay`。
2. 在 `LANGUAGE TOGGLE SHORTCUT` 面板中点击 `Rebind`。
3. 按下新的快捷键。

等待按键输入时按 `Esc` 可取消重新映射并保留原快捷键。

## 手动安装备选方案

只有在不使用安装器时才需要按以下步骤操作：

1. 安装 BepInEx Unity IL2CPP Windows x64 到游戏根目录。
2. 首次启动游戏后退出，使 BepInEx 生成运行目录。
3. 创建插件目录：

   ```text
   <游戏目录>\BepInEx\plugins\ZeroParades.LanguageToggle
   ```

4. 将 `ZeroParades.LanguageToggle.dll` 放入上述目录。
5. 启动游戏。

## 从源码构建

插件构建依赖已安装并运行过一次的 BepInEx IL2CPP 游戏目录，因为构建需要 `BepInEx\core` 与 `BepInEx\interop` 中的程序集。

```powershell
dotnet build .\ZeroParades.LanguageToggle.csproj -c Release
```

默认游戏路径：

```text
C:\Program Files (x86)\Steam\steamapps\common\Zero Parades
```

游戏安装在其他目录时：

```powershell
dotnet build .\ZeroParades.LanguageToggle.csproj -c Release `
  -p:GameRoot="D:\SteamLibrary\steamapps\common\Zero Parades"
```

构建单文件安装器：

```powershell
.\scripts\build-installer.ps1
```

安装器输出位置：

```text
dist\ZeroParades.LanguageToggle.Setup.exe
```

## 第三方依赖

安装器内嵌的 BepInEx 构建及 SHA-256 记录在：

```text
installer\ZeroParades.LanguageToggle.Setup\Assets\THIRD-PARTY-NOTICES.md
```

官方来源：

- <https://builds.bepinex.dev/projects/bepinex_be>
- <https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html?tabs=tabid-win>

BepInEx 使用 GNU Lesser General Public License version 2.1，许可证文本位于：

```text
installer\ZeroParades.LanguageToggle.Setup\Assets\BepInEx-LICENSE.txt
```

## 发布校验

`v0.2.2` 安装包的 SHA-256：

```text
66BB340005D8BF8586543F060322BB56ECD7FAF4DCC036EEB352B72F88F36DF1
```

Release 页面同时提供 `SHA256SUMS.txt`、`THIRD-PARTY-NOTICES.md` 与 `BepInEx-LICENSE.txt`。

## 常见问题

### 安装器提示目录无效

请选择包含 `ZeroParades.exe`、`GameAssembly.dll` 和 `ZeroParades_Data` 的游戏根目录，而不是 `BepInEx` 或插件目录。

### 设置页面没有出现插件面板

- 确认打开的是 `Settings > Gameplay` 页面。
- 确认插件 DLL 位于 `BepInEx\plugins\ZeroParades.LanguageToggle`。
- 查看 BepInEx 日志是否存在插件加载或兼容性错误。

### 快捷键没有切换语言

- 确认 `Target A` 与 `Target B` 是不同语言。
- 点击 `Switch now` 检查语言切换本身是否正常。
- 重新绑定快捷键，避免与游戏或其他插件冲突。

### 语言列表中没有想要的语言

插件仅使用游戏原有 `Language` 下拉选项；只有当前游戏版本实际提供的语言才能成为切换目标。
