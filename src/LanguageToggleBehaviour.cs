using UnityEngine;
using ZAUM.C4.GameSettings;
using ZAUM.C4.UI.Settings;
using ZAUM.FELD.Data.Localization;

namespace ZeroParades.LanguageToggle;

public sealed class LanguageToggleBehaviour : MonoBehaviour
{
    private const float PanelWidth = 380f;
    private const float PanelHeight = 210f;
    private const float PanelMargin = 42f;
    private const float PanelPadding = 12f;
    private const float RowHeight = 24f;
    private const float RowGap = 6f;
    private const float ValueWidth = 230f;
    private const float ButtonWidth = 112f;
    private const float SettingsControllerLookupIntervalSeconds = 0.5f;
    private static readonly KeyCode[] RebindableKeys = Enum.GetValues<KeyCode>();

    private bool _isCapturingKey;
    private bool _uiDisabledAfterError;
    private SettingsUIController? _settingsController;
    private float _nextSettingsControllerLookupTime;

    public LanguageToggleBehaviour(IntPtr pointer)
        : base(pointer)
    {
    }

    public void Update()
    {
        if (_isCapturingKey)
        {
            CaptureNextKey();
            return;
        }

        if (Input.GetKeyDown(LanguageTogglePlugin.ToggleKey.Value))
        {
            LanguageTogglePlugin.ToggleLanguage();
        }
    }

    public void OnGUI()
    {
        if (_uiDisabledAfterError)
        {
            return;
        }

        try
        {
            DrawSettingsPanel();
        }
        catch (Exception exception)
        {
            _uiDisabledAfterError = true;
            _isCapturingKey = false;
            LanguageTogglePlugin.PluginLog.LogError(
                $"Settings panel disabled after an unexpected UI error: {exception}");
        }
    }

    private void DrawSettingsPanel()
    {
        if (!IsGameplaySettingsOpen())
        {
            _isCapturingKey = false;
            return;
        }

        Rect panelRect = new(
            Mathf.Max(PanelMargin, Screen.width - PanelWidth - PanelMargin),
            Mathf.Max(PanelMargin, Screen.height - PanelHeight - PanelMargin),
            PanelWidth,
            PanelHeight);

        GUI.Box(panelRect, "LANGUAGE TOGGLE SHORTCUT");

        float x = panelRect.x + PanelPadding;
        float y = panelRect.y + 30f;
        float contentWidth = panelRect.width - (PanelPadding * 2f);
        float buttonX = x + ValueWidth + RowGap;

        GUI.Label(new Rect(x, y, contentWidth, RowHeight), "Choose Language above, then assign a target.");
        y += RowHeight + RowGap;
        DrawTargetRow(x, buttonX, y, "Target A", LanguageTogglePlugin.PrimaryLanguage.Value, true);
        y += RowHeight + RowGap;
        DrawTargetRow(x, buttonX, y, "Target B", LanguageTogglePlugin.SecondaryLanguage.Value, false);
        y += RowHeight + RowGap;

        GUI.Label(new Rect(x, y, ValueWidth, RowHeight), $"Shortcut: {GetShortcutLabel()}");
        if (GUI.Button(
                new Rect(buttonX, y, ButtonWidth, RowHeight),
                _isCapturingKey ? "Cancel" : "Rebind"))
        {
            _isCapturingKey = !_isCapturingKey;
        }

        y += RowHeight + RowGap;
        bool pairIsValid =
            LanguageTogglePlugin.PrimaryLanguage.Value != LanguageTogglePlugin.SecondaryLanguage.Value;
        Rect switchRect = new(x, y, contentWidth, RowHeight);
        if (pairIsValid && !_isCapturingKey)
        {
            if (GUI.Button(switchRect, "Switch now"))
            {
                LanguageTogglePlugin.ToggleLanguage();
            }
        }
        else
        {
            GUI.Box(switchRect, _isCapturingKey ? "Press a key to finish rebinding" : "Targets must differ");
        }
    }

    private static void DrawTargetRow(
        float labelX,
        float buttonX,
        float y,
        string label,
        LanguageIsoCode language,
        bool primaryTarget)
    {
        GUI.Label(new Rect(labelX, y, ValueWidth, RowHeight), $"{label}: {language}");
        if (GUI.Button(new Rect(buttonX, y, ButtonWidth, RowHeight), "Use current"))
        {
            LanguageTogglePlugin.CaptureCurrentLanguage(primaryTarget);
        }
    }

    private string GetShortcutLabel()
    {
        return _isCapturingKey
            ? "press a key (Esc cancels)"
            : LanguageTogglePlugin.ToggleKey.Value.ToString();
    }

    private void CaptureNextKey()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _isCapturingKey = false;
            return;
        }

        foreach (KeyCode key in RebindableKeys)
        {
            if (key == KeyCode.None || key == KeyCode.Escape || !Input.GetKeyDown(key))
            {
                continue;
            }

            LanguageTogglePlugin.SetToggleKey(key);
            _isCapturingKey = false;
            return;
        }
    }

    private bool IsGameplaySettingsOpen()
    {
        SettingsUIController? controller = GetActiveSettingsController();
        if (controller == null)
        {
            return false;
        }

        SettingsSubMenuBase? currentMenu = controller.GetCurrentSubMenu();
        return currentMenu != null &&
            currentMenu.IsMenuOpen &&
            currentMenu.CategoryType == SettingsCategoryType.Gameplay;
    }

    private SettingsUIController? GetActiveSettingsController()
    {
        if (_settingsController != null && _settingsController.gameObject.activeInHierarchy)
        {
            return _settingsController;
        }

        _settingsController = null;
        if (Time.unscaledTime < _nextSettingsControllerLookupTime)
        {
            return null;
        }

        _nextSettingsControllerLookupTime =
            Time.unscaledTime + SettingsControllerLookupIntervalSeconds;

        foreach (SettingsUIController controller in Resources.FindObjectsOfTypeAll<SettingsUIController>())
        {
            if (!controller.gameObject.activeInHierarchy)
            {
                continue;
            }

            _settingsController = controller;
            return controller;
        }

        return null;
    }
}
