using System.ComponentModel;

namespace CDS.CSharpScript2.WinForms.Sample.Demos.BasicDemo;

/// <summary>
/// Provides a basic demonstration form for C# scripting functionality.
/// </summary>
public partial class FormBasicDemo : Form
{
    private readonly Settings _settings;
    private bool _isRunningOrCompiling;
    private CDS.CSharpScript2.ScintillaEditor.OsThemeWatcher? _osThemeWatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormBasicDemo"/> class.
    /// </summary>
    /// <param name="settings">The settings to use for this demo.</param>
    public FormBasicDemo(Settings settings)
    {
        InitializeComponent();
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Initializes the editor when the form loads.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        scintillaScriptEditor.API.Environment = CDS.CSharpScript2.ScriptEnvironment.Default;

        // Fold levels are computed asynchronously, so the saved fold state can only be reapplied
        // once the first analysis pass has landed — the first DiagnosticsUpdated after Script is
        // set. Restoring it any earlier would have nothing to collapse yet.
        scintillaScriptEditor.API.DiagnosticsUpdated += RestoreCollapsedFoldsOnce;
        scintillaScriptEditor.API.Script = _settings.Script;

        // Checking the radio button here (rather than from the designer) fires rbThemeSystem_CheckedChanged,
        // which starts following the OS theme, since by now InitializeComponent has already wired up
        // the event handler.
        rbThemeSystem.Checked = true;
    }

    /// <summary>
    /// Stops following the OS theme and switches to <see cref="CDS.CSharpScript2.Classification.EditorTheme.Light"/>.
    /// </summary>
    private void rbThemeLight_CheckedChanged(object sender, EventArgs e)
    {
        if (!rbThemeLight.Checked)
        {
            return;
        }

        StopFollowingSystemTheme();
        ApplyTheme(CDS.CSharpScript2.Classification.EditorTheme.Light);
    }

    /// <summary>
    /// Stops following the OS theme and switches to <see cref="CDS.CSharpScript2.Classification.EditorTheme.Dark"/>.
    /// </summary>
    private void rbThemeDark_CheckedChanged(object sender, EventArgs e)
    {
        if (!rbThemeDark.Checked)
        {
            return;
        }

        StopFollowingSystemTheme();
        ApplyTheme(CDS.CSharpScript2.Classification.EditorTheme.Dark);
    }

    /// <summary>
    /// Applies the given theme to the script editor, the output panel, and the form itself — the
    /// buttons and theme picker sit directly on the form, so its background and the group box's
    /// foreground follow the theme too rather than staying a fixed light strip.
    /// </summary>
    private void ApplyTheme(CDS.CSharpScript2.Classification.EditorTheme theme)
    {
        scintillaScriptEditor.Theme = theme;
        outputPanel.Theme = theme;
        BackColor = theme.Background;
        groupBoxTheme.ForeColor = theme.Foreground;
    }

    /// <summary>
    /// Starts following the OS theme, applying it immediately and again on every live OS toggle.
    /// </summary>
    private void rbThemeSystem_CheckedChanged(object sender, EventArgs e)
    {
        if (!rbThemeSystem.Checked)
        {
            return;
        }

        StartFollowingSystemTheme();
    }

    private void StartFollowingSystemTheme()
    {
        if (_osThemeWatcher is not null)
        {
            return;
        }

        _osThemeWatcher = new CDS.CSharpScript2.ScintillaEditor.OsThemeWatcher();
        _osThemeWatcher.ThemeChanged += OsThemeWatcher_ThemeChanged;
        ApplyOsTheme();
    }

    private void StopFollowingSystemTheme()
    {
        if (_osThemeWatcher is null)
        {
            return;
        }

        _osThemeWatcher.ThemeChanged -= OsThemeWatcher_ThemeChanged;
        _osThemeWatcher.Dispose();
        _osThemeWatcher = null;
    }

    private void OsThemeWatcher_ThemeChanged(object? sender, EventArgs e) => ApplyOsTheme();

    private void ApplyOsTheme()
    {
        if (_osThemeWatcher is null)
        {
            return;
        }

        ApplyTheme(_osThemeWatcher.IsDarkThemeActive
            ? CDS.CSharpScript2.Classification.EditorTheme.Dark
            : CDS.CSharpScript2.Classification.EditorTheme.Light);
    }

    /// <summary>
    /// Stops watching the OS theme once the form is actually closing.
    /// </summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        StopFollowingSystemTheme();
        base.OnFormClosed(e);
    }

    /// <summary>
    /// Applies the saved fold state once the script has been analysed for the first time, then
    /// stops listening — later analysis passes must not re-apply a now-stale snapshot over folds
    /// the user has since changed by hand.
    /// </summary>
    private void RestoreCollapsedFoldsOnce(object? sender, CDS.CSharpScript2.Editors.DiagnosticsUpdatedEventArgs e)
    {
        scintillaScriptEditor.API.DiagnosticsUpdated -= RestoreCollapsedFoldsOnce;
        scintillaScriptEditor.API.CollapsedFoldLines = _settings.CollapsedFoldLines;
    }

    /// <summary>
    /// Saves the current script and fold state to settings when the form is closing.
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isRunningOrCompiling)
        {
            e.Cancel = true;
            return;
        }

        _settings.Script = scintillaScriptEditor.API.Script;
        _settings.CollapsedFoldLines = [.. scintillaScriptEditor.API.CollapsedFoldLines];

        base.OnFormClosing(e);
    }

    /// <summary>
    /// Executes a script-related action with consistent state management and exception handling.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns><see langword="true"/> when the action completed successfully; otherwise, <see langword="false"/>.</returns>
    private async Task<bool> PerformScriptActionAsync(Func<Task> action)
    {
        if(action == null) { throw new ArgumentNullException(nameof(action)); } 

        if (_isRunningOrCompiling)
        {
            return false;
        }

        _isRunningOrCompiling = true;
        outputPanel.Clear();

        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            outputPanel.AppendLine($"Error: {ex.Message}");
            return false;
        }
        finally
        {
            _isRunningOrCompiling = false;
        }
    }

    /// <summary>
    /// Handles the Run button click event to compile and execute the script.
    /// </summary>
    private async void btnRun_Click(object sender, EventArgs e)
    {
        using var consoleHook = new CDS.CSharpScript2.Output.ScriptConsoleRedirect(text => outputPanel.Append(text ?? string.Empty));

        await PerformScriptActionAsync(async () =>
        {
            var compiled = await scintillaScriptEditor.API.CompileAsync();
            await compiled.RunAsync();
        });
    }

    /// <summary>
    /// Handles the Compile button click event to compile the script.
    /// </summary>
    private async void btnCompile_Click(object sender, EventArgs e)
    {
        await PerformScriptActionAsync(async () =>
        {
            var compiled = await scintillaScriptEditor.API.CompileAsync();
            var output = compiled.CompilationOutput;

            outputPanel.AppendLine("Compilation complete");

            foreach (var message in output.Messages)
            {
                outputPanel.AppendLine(message);
            }

            outputPanel.AppendLine($"\t{output.WarningCount} warnings");
            outputPanel.AppendLine($"\t{output.ErrorCount} errors");
        });
    }

    /// <summary>
    /// Handles the Expand All button click event to expand every folded region in the editor.
    /// </summary>
    private void btnExpandAllFolds_Click(object sender, EventArgs e) =>
        scintillaScriptEditor.API.ExpandAllFolds();

    /// <summary>
    /// Handles the Collapse All button click event to collapse every foldable region in the editor.
    /// </summary>
    private void btnCollapseAllFolds_Click(object sender, EventArgs e) =>
        scintillaScriptEditor.API.CollapseAllFolds();
}
