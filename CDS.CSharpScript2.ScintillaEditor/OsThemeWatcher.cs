using Microsoft.Win32;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// Watches the Windows app theme (Settings → Personalization → Colors → "Choose your mode") and
/// reports whether dark mode is active, raising <see cref="ThemeChanged"/> when the user toggles
/// it while this watcher is alive. Editor controls never follow the OS theme on their own — a
/// host that wants that behavior creates an <see cref="OsThemeWatcher"/> itself and assigns
/// <c>Classification.EditorTheme.Light</c>/<c>Dark</c> in response.
/// </summary>
public sealed class OsThemeWatcher : IDisposable
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";

    private bool _disposed;

    /// <summary>Raised when the OS app theme changes while this watcher is active.</summary>
    public event EventHandler? ThemeChanged;

    /// <summary>Gets whether the OS app theme is currently dark.</summary>
    public bool IsDarkThemeActive { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="OsThemeWatcher"/>, capturing the current OS theme
    /// and subscribing to live changes. Dispose the instance to stop watching.
    /// </summary>
    public OsThemeWatcher()
    {
        IsDarkThemeActive = ReadIsDarkThemeActive();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    /// <summary>
    /// Reads whether the OS app theme is currently dark, without subscribing to further changes.
    /// </summary>
    /// <returns><see langword="true"/> if the OS app theme is dark; otherwise <see langword="false"/>.</returns>
    public static bool ReadIsDarkThemeActive()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);

        // AppsUseLightTheme is 1 for light, 0 for dark. Older Windows builds without the Personalize
        // key predate the dark theme feature entirely, so a missing key/value defaults to light.
        return key?.GetValue(AppsUseLightThemeValueName) is int value && value == 0;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        // The app theme toggle surfaces through the General category alongside several unrelated
        // settings; re-reading the registry only here avoids a registry hit on every preference
        // change (mouse speed, screensaver, etc.) while still catching the one we care about.
        if (e.Category != UserPreferenceCategory.General)
        {
            return;
        }

        var isDarkThemeActive = ReadIsDarkThemeActive();
        if (isDarkThemeActive == IsDarkThemeActive)
        {
            return;
        }

        IsDarkThemeActive = isDarkThemeActive;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Stops watching for OS theme changes.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _disposed = true;
    }
}
