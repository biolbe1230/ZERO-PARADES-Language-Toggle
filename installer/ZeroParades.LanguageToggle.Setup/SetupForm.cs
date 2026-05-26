using System.Diagnostics;
using ZeroParades.LanguageToggle.Setup.Installation;

namespace ZeroParades.LanguageToggle.Setup;

public sealed class SetupForm : Form
{
    private const string BuildsUrl = "https://builds.bepinex.dev/projects/bepinex_be";

    private readonly InstallerService _service;
    private readonly TextBox _directoryTextBox = new();
    private readonly Label _inspectionLabel = new();
    private readonly TextBox _logTextBox = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Button _browseButton = new();
    private readonly Button _installButton = new();
    private readonly Button _uninstallButton = new();

    public SetupForm(InstallerService service)
    {
        _service = service;
        InitializeWindow();

        string? discoveredDirectory = GameDirectoryService.FindGameDirectory();
        if (discoveredDirectory != null)
        {
            _directoryTextBox.Text = discoveredDirectory;
        }

        RefreshInspection();
    }

    private void InitializeWindow()
    {
        Text = "ZERO PARADES Language Toggle 安装器";
        Width = 760;
        Height = 560;
        MinimumSize = new Size(720, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);

        TableLayoutPanel main = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 8,
            ColumnCount = 1,
        };
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label title = new()
        {
            AutoSize = true,
            Font = new Font(Font.FontFamily, 16F, FontStyle.Bold),
            Text = "ZERO PARADES Language Toggle",
            Margin = new Padding(0, 0, 0, 10),
        };
        main.Controls.Add(title);

        Label instruction = new()
        {
            AutoSize = true,
            Text = "选择游戏目录后点击“安装/更新”。安装器会保留现有 BepInEx、其他插件和本插件配置。",
            Margin = new Padding(0, 0, 0, 12),
        };
        main.Controls.Add(instruction);

        TableLayoutPanel pathRow = new()
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            Margin = new Padding(0, 0, 0, 10),
        };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _directoryTextBox.Dock = DockStyle.Fill;
        _directoryTextBox.TextChanged += (_, _) => RefreshInspection();
        _browseButton.AutoSize = true;
        _browseButton.Text = "浏览...";
        _browseButton.Margin = new Padding(8, 0, 0, 0);
        _browseButton.Click += BrowseClicked;
        pathRow.Controls.Add(_directoryTextBox, 0, 0);
        pathRow.Controls.Add(_browseButton, 1, 0);
        main.Controls.Add(pathRow);

        _inspectionLabel.AutoSize = true;
        _inspectionLabel.Margin = new Padding(0, 0, 0, 10);
        main.Controls.Add(_inspectionLabel);

        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.Multiline = true;
        _logTextBox.ReadOnly = true;
        _logTextBox.ScrollBars = ScrollBars.Vertical;
        _logTextBox.BackColor = Color.White;
        main.Controls.Add(_logTextBox);

        _progressBar.Dock = DockStyle.Top;
        _progressBar.Margin = new Padding(0, 12, 0, 12);
        main.Controls.Add(_progressBar);

        FlowLayoutPanel actionRow = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 12),
        };
        _installButton.AutoSize = true;
        _installButton.Text = "安装/更新";
        _installButton.Click += async (_, _) => await RunOperationAsync(install: true);
        _uninstallButton.AutoSize = true;
        _uninstallButton.Text = "卸载插件";
        _uninstallButton.Margin = new Padding(10, 0, 0, 0);
        _uninstallButton.Click += async (_, _) => await RunOperationAsync(install: false);
        actionRow.Controls.Add(_installButton);
        actionRow.Controls.Add(_uninstallButton);
        main.Controls.Add(actionRow);

        LinkLabel sourceLink = new()
        {
            AutoSize = true,
            Text = "内嵌 BepInEx Unity IL2CPP win-x64 6.0.0-be.755+3fab71a - 查看官方来源",
        };
        sourceLink.LinkClicked += (_, _) =>
            Process.Start(new ProcessStartInfo(BuildsUrl) { UseShellExecute = true });
        main.Controls.Add(sourceLink);
        Controls.Add(main);
    }

    private void BrowseClicked(object? sender, EventArgs eventArgs)
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "选择包含 ZeroParades.exe 的游戏根目录",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_directoryTextBox.Text) ? _directoryTextBox.Text : string.Empty,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _directoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private void RefreshInspection()
    {
        TargetInspection inspection = GameDirectoryService.Inspect(_directoryTextBox.Text.Trim());
        _inspectionLabel.Text = inspection.Validation.IsValid
            ? $"{inspection.Validation.Message} {inspection.Summary}"
            : inspection.Validation.Message;
        _inspectionLabel.ForeColor = inspection.Validation.IsValid ? Color.DarkGreen : Color.Firebrick;
        _installButton.Enabled = inspection.Validation.IsValid
            && inspection.BepInExState != BepInExState.IncompleteOrConflicting;
        _uninstallButton.Enabled = inspection.Validation.IsValid && inspection.PluginInstalled;
    }

    private async Task RunOperationAsync(bool install)
    {
        SetBusy(true);
        _logTextBox.Clear();
        Progress<string> progress = new(message => AppendLog(message));

        OperationResult result = await Task.Run(() => install
            ? _service.Install(_directoryTextBox.Text.Trim(), progress)
            : _service.Uninstall(_directoryTextBox.Text.Trim(), progress));

        AppendLog($"日志：{result.LogPath}");
        SetBusy(false);
        RefreshInspection();
        MessageBox.Show(
            this,
            result.Message,
            result.Success ? "操作完成" : "操作未完成",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private void AppendLog(string message)
    {
        _logTextBox.AppendText(message + Environment.NewLine);
    }

    private void SetBusy(bool isBusy)
    {
        _browseButton.Enabled = !isBusy;
        _directoryTextBox.Enabled = !isBusy;
        _installButton.Enabled = !isBusy;
        _uninstallButton.Enabled = !isBusy;
        _progressBar.Style = isBusy ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
    }
}
