using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace ZeroParades.LanguageToggle.Setup.Installation;

public sealed class InstallerService
{
    private const string PluginRelativePath =
        @"BepInEx\plugins\ZeroParades.LanguageToggle\ZeroParades.LanguageToggle.dll";
    private const string PluginBackupRelativePath =
        @"BepInEx\config\ZeroParades.LanguageToggle\backup";
    private const string LegacyPluginBackupRelativePath =
        @"BepInEx\plugins\ZeroParades.LanguageToggle\backup";

    private readonly IPayloadProvider _payloadProvider;
    private readonly Func<bool> _isGameRunning;
    private readonly Func<string> _logPathFactory;

    public InstallerService(
        IPayloadProvider payloadProvider,
        Func<bool>? isGameRunning = null,
        Func<string>? logPathFactory = null)
    {
        _payloadProvider = payloadProvider;
        _isGameRunning = isGameRunning ?? IsGameProcessRunning;
        _logPathFactory = logPathFactory ?? CreateDefaultLogPath;
    }

    public OperationResult Install(string gameRoot, IProgress<string>? progress = null)
    {
        string logPath = _logPathFactory();
        try
        {
            using StreamWriter log = CreateLog(logPath);
            TargetInspection inspection = GameDirectoryService.Inspect(gameRoot);
            Report(log, progress, inspection.Validation.Message);

            if (!inspection.Validation.IsValid)
            {
                return Failure(logPath, log, progress, inspection.Validation.Message);
            }

            if (_isGameRunning())
            {
                return Failure(logPath, log, progress, "请先关闭 ZERO PARADES，再执行安装或更新。");
            }

            Report(log, progress, inspection.Summary);
            if (inspection.BepInExState == BepInExState.IncompleteOrConflicting)
            {
                return Failure(logPath, log, progress, "安装已停止：请先备份并处理当前不完整的 BepInEx 安装。");
            }

            if (inspection.BepInExState == BepInExState.NotInstalled)
            {
                Report(log, progress, "正在安装内嵌的 BepInEx IL2CPP...");
                ExtractBepInExArchive(gameRoot);
            }

            string pluginPath = Path.Combine(gameRoot, PluginRelativePath);
            MigrateLegacyPluginBackups(gameRoot, log, progress);
            if (File.Exists(pluginPath))
            {
                string backupPath = CreatePluginBackup(gameRoot, pluginPath);
                Report(log, progress, $"旧插件 DLL 已备份到：{backupPath}");
            }

            Report(log, progress, "正在写入 ZeroParades.LanguageToggle.dll...");
            using (Stream plugin = _payloadProvider.OpenPluginDll())
            {
                WriteAtomically(pluginPath, plugin);
            }

            const string message = "安装/更新完成。启动游戏后可在 Settings > Gameplay 中配置语言切换。";
            Report(log, progress, message);
            return new OperationResult(true, message, logPath);
        }
        catch (Exception exception)
        {
            return FailureAfterException(logPath, progress, exception);
        }
    }

    public OperationResult Uninstall(string gameRoot, IProgress<string>? progress = null)
    {
        string logPath = _logPathFactory();
        try
        {
            using StreamWriter log = CreateLog(logPath);
            DirectoryValidation validation = GameDirectoryService.Validate(gameRoot);
            Report(log, progress, validation.Message);
            if (!validation.IsValid)
            {
                return Failure(logPath, log, progress, validation.Message);
            }

            if (_isGameRunning())
            {
                return Failure(logPath, log, progress, "请先关闭 ZERO PARADES，再卸载插件。");
            }

            string pluginPath = Path.Combine(gameRoot, PluginRelativePath);
            if (File.Exists(pluginPath))
            {
                File.Delete(pluginPath);
                Report(log, progress, "已删除本插件 DLL。BepInEx、配置文件与备份均已保留。");
            }
            else
            {
                Report(log, progress, "未发现当前插件 DLL，无需删除。");
            }

            const string message = "插件卸载完成；BepInEx 与用户配置未被修改。";
            Report(log, progress, message);
            return new OperationResult(true, message, logPath);
        }
        catch (Exception exception)
        {
            return FailureAfterException(logPath, progress, exception);
        }
    }

    private static StreamWriter CreateLog(string logPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        return new StreamWriter(logPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private void ExtractBepInExArchive(string gameRoot)
    {
        string normalizedRoot = Path.GetFullPath(gameRoot).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        using Stream archiveStream = _payloadProvider.OpenBepInExArchive();
        using ZipArchive archive = new(archiveStream, ZipArchiveMode.Read);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string destination = Path.GetFullPath(
                Path.Combine(gameRoot, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
            if (!destination.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("BepInEx 安装资源包含无效路径。");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destination);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using Stream source = entry.Open();
            using FileStream target = new(destination, FileMode.Create, FileAccess.Write, FileShare.None);
            source.CopyTo(target);
        }
    }

    private static string CreatePluginBackup(string gameRoot, string pluginPath)
    {
        string backupDirectory = Path.Combine(gameRoot, PluginBackupRelativePath);
        Directory.CreateDirectory(backupDirectory);
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        string backupPath = Path.Combine(backupDirectory, $"ZeroParades.LanguageToggle-{timestamp}.dll");
        File.Copy(pluginPath, backupPath, overwrite: false);
        return backupPath;
    }

    private static void MigrateLegacyPluginBackups(
        string gameRoot,
        StreamWriter log,
        IProgress<string>? progress)
    {
        string legacyDirectory = Path.Combine(gameRoot, LegacyPluginBackupRelativePath);
        if (!Directory.Exists(legacyDirectory))
        {
            return;
        }

        string backupDirectory = Path.Combine(gameRoot, PluginBackupRelativePath);
        int migratedCount = 0;
        foreach (string sourcePath in Directory.EnumerateFiles(legacyDirectory, "*.dll", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(backupDirectory);
            string destinationPath = GetAvailableBackupPath(backupDirectory, Path.GetFileName(sourcePath));
            File.Move(sourcePath, destinationPath);
            migratedCount++;
        }

        if (migratedCount > 0)
        {
            Report(log, progress, $"已迁移 {migratedCount} 个旧备份 DLL，避免 BepInEx 重复扫描。");
        }
    }

    private static string GetAvailableBackupPath(string backupDirectory, string fileName)
    {
        string destinationPath = Path.Combine(backupDirectory, fileName);
        if (!File.Exists(destinationPath))
        {
            return destinationPath;
        }

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        return Path.Combine(
            backupDirectory,
            $"{baseName}-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}{extension}");
    }

    private static void WriteAtomically(string destinationPath, Stream content)
    {
        string directory = Path.GetDirectoryName(destinationPath)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (FileStream target = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                content.CopyTo(target);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void Report(StreamWriter log, IProgress<string>? progress, string message)
    {
        log.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        log.Flush();
        progress?.Report(message);
    }

    private static OperationResult Failure(
        string logPath,
        StreamWriter log,
        IProgress<string>? progress,
        string message)
    {
        Report(log, progress, message);
        return new OperationResult(false, message, logPath);
    }

    private static OperationResult FailureAfterException(
        string logPath,
        IProgress<string>? progress,
        Exception exception)
    {
        string message = $"操作失败：{exception.Message}";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}", Encoding.UTF8);
        }
        catch
        {
            // Do not hide the primary installation failure if diagnostic logging also fails.
        }

        progress?.Report(message);
        return new OperationResult(false, message, logPath);
    }

    private static bool IsGameProcessRunning() =>
        Process.GetProcessesByName("ZeroParades").Length > 0;

    private static string CreateDefaultLogPath() =>
        Path.Combine(
            Path.GetTempPath(),
            "ZeroParades.LanguageToggle.Setup",
            $"setup-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log");
}
