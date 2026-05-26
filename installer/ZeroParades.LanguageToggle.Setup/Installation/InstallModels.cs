namespace ZeroParades.LanguageToggle.Setup.Installation;

public enum BepInExState
{
    NotInstalled,
    InstalledIl2Cpp,
    IncompleteOrConflicting,
}

public sealed record DirectoryValidation(bool IsValid, string Message);

public sealed record TargetInspection(
    string RootPath,
    DirectoryValidation Validation,
    BepInExState BepInExState,
    bool PluginInstalled,
    string Summary);

public sealed record OperationResult(bool Success, string Message, string LogPath);
