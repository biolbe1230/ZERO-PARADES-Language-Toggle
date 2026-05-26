using ZeroParades.LanguageToggle.Setup.Installation;

namespace ZeroParades.LanguageToggle.Setup;

public static class CommandLineApplication
{
    public static int Run(string[] args, InstallerService service, TextWriter output)
    {
        if (args.Length != 2)
        {
            output.WriteLine("用法：--inspect <游戏目录> | --install <游戏目录> | --uninstall <游戏目录>");
            return 2;
        }

        string command = args[0];
        string gameRoot = args[1];
        if (string.Equals(command, "--inspect", StringComparison.OrdinalIgnoreCase))
        {
            TargetInspection inspection = GameDirectoryService.Inspect(gameRoot);
            output.WriteLine(inspection.Validation.Message);
            output.WriteLine(inspection.Summary);
            return inspection.Validation.IsValid ? 0 : 1;
        }

        TextWriterProgress progress = new(output);
        OperationResult result = string.Equals(command, "--install", StringComparison.OrdinalIgnoreCase)
            ? service.Install(gameRoot, progress)
            : string.Equals(command, "--uninstall", StringComparison.OrdinalIgnoreCase)
                ? service.Uninstall(gameRoot, progress)
                : new OperationResult(false, "未知命令。", string.Empty);

        output.WriteLine(result.Message);
        if (!string.IsNullOrWhiteSpace(result.LogPath))
        {
            output.WriteLine($"日志：{result.LogPath}");
        }

        return result.Success ? 0 : 1;
    }

    private sealed class TextWriterProgress(TextWriter output) : IProgress<string>
    {
        public void Report(string value) => output.WriteLine(value);
    }
}
