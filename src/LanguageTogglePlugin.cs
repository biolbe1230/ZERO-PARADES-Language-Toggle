using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using ZAUM.C4.Game;
using ZAUM.C4.GameSettings.Management;
using ZAUM.C4.GameSettings.Visuals.Settings;
using ZAUM.FELD.Data.Localization;

namespace ZeroParades.LanguageToggle;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class LanguageTogglePlugin : BasePlugin
{
    public const string PluginGuid = "com.codexscholar.zeroparades.languagetoggle";
    public const string PluginName = "Zero Parades Language Toggle";
    public const string PluginVersion = "0.2.2";

    private LanguageToggleBehaviour? _behaviour;

    internal static ManualLogSource PluginLog { get; private set; } = null!;
    internal static ConfigEntry<LanguageIsoCode> PrimaryLanguage { get; private set; } = null!;
    internal static ConfigEntry<LanguageIsoCode> SecondaryLanguage { get; private set; } = null!;
    internal static ConfigEntry<KeyCode> ToggleKey { get; private set; } = null!;

    public override void Load()
    {
        PluginLog = Log;
        PrimaryLanguage = Config.Bind(
            "Toggle",
            "TargetLanguageA",
            LanguageIsoCode.zh_cn,
            "First language in the one-key toggle pair. Set from the in-game Gameplay settings panel.");
        SecondaryLanguage = Config.Bind(
            "Toggle",
            "TargetLanguageB",
            LanguageIsoCode.en,
            "Second language in the one-key toggle pair. Set from the in-game Gameplay settings panel.");
        ToggleKey = Config.Bind(
            "Toggle",
            "ShortcutKey",
            KeyCode.Q,
            "Key used to switch between TargetLanguageA and TargetLanguageB.");

        _behaviour = AddComponent<LanguageToggleBehaviour>();

        Log.LogInfo(
            $"Loaded. Press {ToggleKey.Value} to toggle between {PrimaryLanguage.Value} and {SecondaryLanguage.Value}.");
    }

    public override bool Unload()
    {
        if (_behaviour != null)
        {
            UnityEngine.Object.Destroy(_behaviour);
        }

        return true;
    }

    internal static bool TryGetCurrentLanguage(out LanguageIsoCode language)
    {
        GameSettingsManager? settingsManager = GameControl.instance?.gameSettingsManager;
        if (settingsManager == null)
        {
            language = default;
            return false;
        }

        language = settingsManager.GetSettingValue<LanguageSetting, LanguageIsoCode>();
        return true;
    }

    internal static void CaptureCurrentLanguage(bool primaryTarget)
    {
        try
        {
            if (!TryGetCurrentLanguage(out LanguageIsoCode current))
            {
                PluginLog.LogWarning("Cannot capture language because game settings are not initialized yet.");
                return;
            }

            ConfigEntry<LanguageIsoCode> target = primaryTarget ? PrimaryLanguage : SecondaryLanguage;
            target.Value = current;
            PluginLog.LogInfo($"Set language toggle target {(primaryTarget ? "A" : "B")} to {current}.");
        }
        catch (Exception exception)
        {
            PluginLog.LogError($"Could not capture the current language: {exception}");
        }
    }

    internal static void SetToggleKey(KeyCode key)
    {
        ToggleKey.Value = key;
        PluginLog.LogInfo($"Language toggle shortcut changed to {key}.");
    }

    internal static void ToggleLanguage()
    {
        try
        {
            GameSettingsManager? settingsManager = GameControl.instance?.gameSettingsManager;
            if (settingsManager == null)
            {
                PluginLog.LogWarning("Language toggle skipped because game settings are not initialized yet.");
                return;
            }

            LanguageIsoCode current = settingsManager.GetSettingValue<LanguageSetting, LanguageIsoCode>();
            LanguageIsoCode first = PrimaryLanguage.Value;
            LanguageIsoCode second = SecondaryLanguage.Value;
            if (first == second)
            {
                PluginLog.LogWarning("Language toggle skipped because targets A and B are identical.");
                return;
            }

            LanguageIsoCode next = current == first ? second : first;

            settingsManager.SetSettingValue<LanguageSetting, LanguageIsoCode>(next);
            settingsManager.RequestSaveSettings();

            PluginLog.LogInfo($"Language changed from {current} to {next}.");
        }
        catch (Exception exception)
        {
            PluginLog.LogError($"Could not change language: {exception}");
        }
    }
}
