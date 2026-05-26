using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ZeroParades.LanguageToggle.Setup.Installation;

public static class GameDirectoryService
{
    public const string GameDirectoryName = "Zero Parades";

    public static DirectoryValidation Validate(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return new DirectoryValidation(false, "请选择有效的游戏目录。");
        }

        string[] requiredPaths =
        [
            Path.Combine(rootPath, "ZeroParades.exe"),
            Path.Combine(rootPath, "GameAssembly.dll"),
            Path.Combine(rootPath, "ZeroParades_Data", "il2cpp_data"),
        ];

        if (requiredPaths.Any(path => !File.Exists(path) && !Directory.Exists(path)))
        {
            return new DirectoryValidation(
                false,
                "目录中没有找到 ZERO PARADES 游戏文件，请选择包含 ZeroParades.exe 的根目录。");
        }

        return new DirectoryValidation(true, "已识别 ZERO PARADES 游戏目录。");
    }

    public static TargetInspection Inspect(string rootPath)
    {
        DirectoryValidation validation = Validate(rootPath);
        string coreDllPath = Path.Combine(rootPath, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll");
        string doorstopPath = Path.Combine(rootPath, "winhttp.dll");
        string configPath = Path.Combine(rootPath, "doorstop_config.ini");
        string pluginPath = Path.Combine(
            rootPath,
            "BepInEx",
            "plugins",
            "ZeroParades.LanguageToggle",
            "ZeroParades.LanguageToggle.dll");

        bool hasCoreDll = File.Exists(coreDllPath);
        bool hasDoorstop = File.Exists(doorstopPath);
        bool hasConfig = File.Exists(configPath);
        bool hasBepInExDirectory = Directory.Exists(Path.Combine(rootPath, "BepInEx"));
        bool hasAnyBepInExMarker = hasCoreDll || hasDoorstop || hasConfig || hasBepInExDirectory;

        BepInExState state = hasCoreDll && hasDoorstop && hasConfig
            ? BepInExState.InstalledIl2Cpp
            : hasAnyBepInExMarker
                ? BepInExState.IncompleteOrConflicting
                : BepInExState.NotInstalled;

        string summary = state switch
        {
            BepInExState.NotInstalled => "未检测到 BepInEx，安装时将一并安装。",
            BepInExState.InstalledIl2Cpp => "已检测到 BepInEx IL2CPP，仅更新本插件。",
            _ => "检测到不完整或不兼容的 BepInEx 文件，安装已被保护性阻止。",
        };

        if (File.Exists(pluginPath))
        {
            summary += " 已检测到旧插件，将在更新前备份 DLL。";
        }

        return new TargetInspection(rootPath, validation, state, File.Exists(pluginPath), summary);
    }

    public static string? FindGameDirectory(string? steamRoot = null)
    {
        string? resolvedSteamRoot = steamRoot ?? FindSteamRoot();
        if (string.IsNullOrWhiteSpace(resolvedSteamRoot) || !Directory.Exists(resolvedSteamRoot))
        {
            return null;
        }

        foreach (string libraryRoot in EnumerateLibraryRoots(resolvedSteamRoot))
        {
            string candidate = Path.Combine(libraryRoot, "steamapps", "common", GameDirectoryName);
            if (Validate(candidate).IsValid)
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateLibraryRoots(string steamRoot)
    {
        HashSet<string> returned = new(StringComparer.OrdinalIgnoreCase);
        if (returned.Add(steamRoot))
        {
            yield return steamRoot;
        }

        string libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFile))
        {
            yield break;
        }

        string text = File.ReadAllText(libraryFile);
        foreach (Match match in Regex.Matches(text, "\"path\"\\s+\"(?<path>(?:\\\\.|[^\"])*)\""))
        {
            string libraryRoot = match.Groups["path"].Value.Replace(@"\\", @"\", StringComparison.Ordinal);
            if (returned.Add(libraryRoot))
            {
                yield return libraryRoot;
            }
        }
    }

    private static string? FindSteamRoot()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        if (key?.GetValue("SteamPath") is string steamPath && Directory.Exists(steamPath))
        {
            return steamPath.Replace('/', Path.DirectorySeparatorChar);
        }

        string fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam");
        return Directory.Exists(fallback) ? fallback : null;
    }
}
