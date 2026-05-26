using System.IO.Compression;
using ZeroParades.LanguageToggle.Setup;
using ZeroParades.LanguageToggle.Setup.Installation;

namespace ZeroParades.LanguageToggle.Setup.Tests;

internal static class Program
{
    private static int Main()
    {
        string tempRoot = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "temp", "installer-tests"));

        try
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }

            Directory.CreateDirectory(tempRoot);
            VerifyDirectoryValidation(tempRoot);
            Console.WriteLine("PASS: validation");
            VerifyInstallationScenarios(tempRoot);
            Console.WriteLine("PASS: install and upgrade");
            VerifyConflictAndRunningGameRejection(tempRoot);
            Console.WriteLine("PASS: conflicting BepInEx and running-game guard");
            VerifyArchiveTraversalRejection(tempRoot);
            Console.WriteLine("PASS: archive traversal guard");
            VerifyUninstall(tempRoot);
            Console.WriteLine("PASS: uninstall");
            VerifySteamDiscovery(tempRoot);
            Console.WriteLine("PASS: Steam discovery");
            VerifyCommandLineInspection(tempRoot);
            Console.WriteLine("PASS: command line inspection");
            VerifyEmbeddedPayloads();
            Console.WriteLine("PASS: embedded payloads");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception.Message}");
            return 1;
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static void VerifyDirectoryValidation(string tempRoot)
    {
        string root = Path.Combine(tempRoot, "validation");
        Directory.CreateDirectory(root);

        AssertFalse(GameDirectoryService.Validate(root).IsValid, "Empty directory must fail validation.");

        File.WriteAllBytes(Path.Combine(root, "ZeroParades.exe"), []);
        File.WriteAllBytes(Path.Combine(root, "GameAssembly.dll"), []);
        Directory.CreateDirectory(Path.Combine(root, "ZeroParades_Data", "il2cpp_data"));

        AssertTrue(GameDirectoryService.Validate(root).IsValid, "Game marker files must validate.");
    }

    private static void VerifyInstallationScenarios(string tempRoot)
    {
        byte[] pluginBytes = "new plugin payload"u8.ToArray();
        TestPayloadProvider payloadProvider = new(pluginBytes);
        InstallerService service = CreateService(tempRoot, payloadProvider);

        string newInstallRoot = CreateGameRoot(tempRoot, "new-install");
        OperationResult newInstallResult = service.Install(newInstallRoot);
        AssertTrue(newInstallResult.Success, newInstallResult.Message);
        AssertTrue(
            File.Exists(Path.Combine(newInstallRoot, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll")),
            "New installation must extract BepInEx.");
        AssertBytesEqual(pluginBytes, File.ReadAllBytes(PluginPath(newInstallRoot)), "Plugin payload must be installed.");
        AssertTrue(File.Exists(newInstallResult.LogPath), "Install result must expose an existing log.");

        string existingRoot = CreateGameRoot(tempRoot, "existing-bepinex");
        CreateCompleteBepInEx(existingRoot, "existing core");
        OperationResult existingResult = service.Install(existingRoot);
        AssertTrue(existingResult.Success, existingResult.Message);
        AssertEqual(
            "existing core",
            File.ReadAllText(Path.Combine(existingRoot, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll")),
            "Existing BepInEx files must not be overwritten.");

        string upgradeRoot = CreateGameRoot(tempRoot, "upgrade");
        CreateCompleteBepInEx(upgradeRoot, "core remains");
        Directory.CreateDirectory(Path.GetDirectoryName(PluginPath(upgradeRoot))!);
        File.WriteAllBytes(PluginPath(upgradeRoot), "old plugin"u8.ToArray());
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath(upgradeRoot))!);
        File.WriteAllText(ConfigPath(upgradeRoot), "ShortcutKey = F8");
        Directory.CreateDirectory(LegacyBackupPath(upgradeRoot));
        File.WriteAllBytes(Path.Combine(LegacyBackupPath(upgradeRoot), "legacy.dll"), "legacy backup"u8.ToArray());

        OperationResult upgradeResult = service.Install(upgradeRoot);
        AssertTrue(upgradeResult.Success, upgradeResult.Message);
        AssertBytesEqual(pluginBytes, File.ReadAllBytes(PluginPath(upgradeRoot)), "Upgrade must replace plugin DLL.");
        AssertEqual("ShortcutKey = F8", File.ReadAllText(ConfigPath(upgradeRoot)), "Upgrade must preserve config.");
        AssertTrue(
            Directory.EnumerateFiles(SafeBackupPath(upgradeRoot), "*.dll").Any(),
            "Upgrade must create a backup outside BepInEx's plugin scan path.");
        AssertTrue(
            File.Exists(Path.Combine(SafeBackupPath(upgradeRoot), "legacy.dll")),
            "Upgrade must migrate legacy backups outside BepInEx's plugin scan path.");
        AssertFalse(
            Directory.EnumerateFiles(LegacyBackupPath(upgradeRoot), "*.dll").Any(),
            "Upgrade must leave no legacy plugin DLL backups under the scanned plugin directory.");
    }

    private static void VerifyConflictAndRunningGameRejection(string tempRoot)
    {
        TestPayloadProvider payloadProvider = new("plugin"u8.ToArray());
        InstallerService service = CreateService(tempRoot, payloadProvider);

        string partialRoot = CreateGameRoot(tempRoot, "partial-bepinex");
        Directory.CreateDirectory(Path.Combine(partialRoot, "BepInEx"));
        OperationResult partialResult = service.Install(partialRoot);
        AssertFalse(partialResult.Success, "Partial BepInEx must be rejected.");
        AssertFalse(File.Exists(PluginPath(partialRoot)), "Rejected installation must not write a plugin DLL.");

        string runningRoot = CreateGameRoot(tempRoot, "running-game");
        InstallerService runningService = new(
            payloadProvider,
            () => true,
            () => Path.Combine(tempRoot, "logs", "running.log"));
        OperationResult runningResult = runningService.Install(runningRoot);
        AssertFalse(runningResult.Success, "Running game must prevent installation.");
        AssertFalse(File.Exists(PluginPath(runningRoot)), "Process guard must prevent file writes.");
    }

    private static void VerifyArchiveTraversalRejection(string tempRoot)
    {
        string root = CreateGameRoot(tempRoot, "archive-traversal");
        string escapedPath = Path.Combine(tempRoot, "outside-installer-root.dll");
        InstallerService service = CreateService(
            tempRoot,
            new TestPayloadProvider("plugin"u8.ToArray(), includeTraversalEntry: true));

        OperationResult result = service.Install(root);

        AssertFalse(result.Success, "Archive entries that escape the game root must be rejected.");
        AssertFalse(File.Exists(escapedPath), "Rejected archive entries must not write outside the game root.");
    }

    private static void VerifyUninstall(string tempRoot)
    {
        string root = CreateGameRoot(tempRoot, "uninstall");
        CreateCompleteBepInEx(root, "preserved");
        Directory.CreateDirectory(Path.GetDirectoryName(PluginPath(root))!);
        File.WriteAllBytes(PluginPath(root), "installed"u8.ToArray());
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath(root))!);
        File.WriteAllText(ConfigPath(root), "TargetLanguageA = zh_cn");
        Directory.CreateDirectory(SafeBackupPath(root));
        File.WriteAllBytes(Path.Combine(SafeBackupPath(root), "old.dll"), "backup"u8.ToArray());

        InstallerService service = CreateService(tempRoot, new TestPayloadProvider("unused"u8.ToArray()));
        OperationResult result = service.Uninstall(root);

        AssertTrue(result.Success, result.Message);
        AssertFalse(File.Exists(PluginPath(root)), "Uninstall must delete only the active plugin DLL.");
        AssertTrue(File.Exists(ConfigPath(root)), "Uninstall must preserve configuration.");
        AssertTrue(File.Exists(Path.Combine(SafeBackupPath(root), "old.dll")), "Uninstall must preserve backups.");
        AssertTrue(
            File.Exists(Path.Combine(root, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll")),
            "Uninstall must preserve BepInEx.");
    }

    private static void VerifySteamDiscovery(string tempRoot)
    {
        string steamRoot = Path.Combine(tempRoot, "steam-root");
        string libraryRoot = Path.Combine(tempRoot, "secondary-library");
        string gameRoot = Path.Combine(libraryRoot, "steamapps", "common", GameDirectoryService.GameDirectoryName);
        Directory.CreateDirectory(Path.Combine(steamRoot, "steamapps"));
        CreateValidGameRoot(gameRoot);

        string escapedLibraryRoot = libraryRoot.Replace(@"\", @"\\", StringComparison.Ordinal);
        File.WriteAllText(
            Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf"),
            $"\"libraryfolders\"\n{{\n  \"1\"\n  {{\n    \"path\" \"{escapedLibraryRoot}\"\n  }}\n}}\n");

        string? detected = GameDirectoryService.FindGameDirectory(steamRoot);
        AssertEqual(gameRoot, detected ?? string.Empty, "Steam secondary library game directory must be discovered.");
    }

    private static void VerifyCommandLineInspection(string tempRoot)
    {
        string root = CreateGameRoot(tempRoot, "cli-inspect");
        InstallerService service = CreateService(tempRoot, new TestPayloadProvider("plugin"u8.ToArray()));
        using StringWriter output = new();

        int exitCode = CommandLineApplication.Run(["--inspect", root], service, output);

        AssertTrue(exitCode == 0, "Valid --inspect command must succeed.");
        AssertTrue(
            output.ToString().Contains("已识别 ZERO PARADES", StringComparison.Ordinal),
            "Inspection command must print validated game status.");
    }

    private static void VerifyEmbeddedPayloads()
    {
        EmbeddedPayloadProvider provider = new();
        using Stream archiveStream = provider.OpenBepInExArchive();
        using ZipArchive archive = new(archiveStream, ZipArchiveMode.Read);
        AssertTrue(
            archive.Entries.Any(entry =>
                string.Equals(
                    entry.FullName.Replace('\\', '/'),
                    "BepInEx/core/BepInEx.Unity.IL2CPP.dll",
                    StringComparison.OrdinalIgnoreCase)),
            "Embedded BepInEx archive must contain the IL2CPP core assembly.");

        using Stream pluginStream = provider.OpenPluginDll();
        AssertTrue(pluginStream.Length > 0, "Embedded plugin DLL must not be empty.");
    }

    private static InstallerService CreateService(string tempRoot, IPayloadProvider payloadProvider) =>
        new(
            payloadProvider,
            () => false,
            () => Path.Combine(tempRoot, "logs", $"{Guid.NewGuid():N}.log"));

    private static string CreateGameRoot(string tempRoot, string name)
    {
        string root = Path.Combine(tempRoot, name);
        CreateValidGameRoot(root);
        return root;
    }

    private static void CreateValidGameRoot(string root)
    {
        Directory.CreateDirectory(root);
        File.WriteAllBytes(Path.Combine(root, "ZeroParades.exe"), []);
        File.WriteAllBytes(Path.Combine(root, "GameAssembly.dll"), []);
        Directory.CreateDirectory(Path.Combine(root, "ZeroParades_Data", "il2cpp_data"));
    }

    private static void CreateCompleteBepInEx(string root, string coreContents)
    {
        Directory.CreateDirectory(Path.Combine(root, "BepInEx", "core"));
        File.WriteAllText(Path.Combine(root, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll"), coreContents);
        File.WriteAllText(Path.Combine(root, "winhttp.dll"), "doorstop");
        File.WriteAllText(Path.Combine(root, "doorstop_config.ini"), "targetAssembly=BepInEx/core/BepInEx.Unity.IL2CPP.dll");
    }

    private static string PluginPath(string root) =>
        Path.Combine(root, "BepInEx", "plugins", "ZeroParades.LanguageToggle", "ZeroParades.LanguageToggle.dll");

    private static string LegacyBackupPath(string root) =>
        Path.Combine(root, "BepInEx", "plugins", "ZeroParades.LanguageToggle", "backup");

    private static string SafeBackupPath(string root) =>
        Path.Combine(root, "BepInEx", "config", "ZeroParades.LanguageToggle", "backup");

    private static string ConfigPath(string root) =>
        Path.Combine(root, "BepInEx", "config", "com.codexscholar.zeroparades.languagetoggle.cfg");

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertFalse(bool condition, string message) => AssertTrue(!condition, message);

    private static void AssertEqual(string expected, string actual, string message)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertBytesEqual(byte[] expected, byte[] actual, string message)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class TestPayloadProvider : IPayloadProvider
    {
        private readonly byte[] _pluginBytes;
        private readonly byte[] _bepInExArchiveBytes;

        public TestPayloadProvider(byte[] pluginBytes, bool includeTraversalEntry = false)
        {
            _pluginBytes = pluginBytes;
            using MemoryStream stream = new();
            using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                AddEntry(archive, "BepInEx/core/BepInEx.Unity.IL2CPP.dll", "embedded core");
                AddEntry(archive, "winhttp.dll", "embedded doorstop");
                AddEntry(archive, "doorstop_config.ini", "targetAssembly=BepInEx/core/BepInEx.Unity.IL2CPP.dll");
                if (includeTraversalEntry)
                {
                    AddEntry(archive, "../outside-installer-root.dll", "escaped");
                }
            }

            _bepInExArchiveBytes = stream.ToArray();
        }

        public Stream OpenBepInExArchive() => new MemoryStream(_bepInExArchiveBytes, writable: false);

        public Stream OpenPluginDll() => new MemoryStream(_pluginBytes, writable: false);

        private static void AddEntry(ZipArchive archive, string name, string contents)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name);
            using StreamWriter writer = new(entry.Open());
            writer.Write(contents);
        }
    }
}
