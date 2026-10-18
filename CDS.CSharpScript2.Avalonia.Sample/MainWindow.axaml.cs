using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using CDS.CSharpScript2.Editors;
using CDS.CSharpScript2.Output;
using Microsoft.CodeAnalysis;

namespace CDS.CSharpScript2.Avalonia.Sample;

public partial class MainWindow : Window
{
    private const string InitialScript =
        """
        // Press F5 (or Script → Run) to run this script.
        var squares = Enumerable.Range(1, 5).Select(i => i * i).ToList();
        Console.WriteLine($"Squares: {string.Join(", ", squares)}");

        // The script's final expression is its result.
        squares.Sum()

        """;

    private bool _isRunning;

    public MainWindow()
    {
        InitializeComponent();

        // Default references only System.Console; add LINQ so the sample script resolves.
        // The non-generic overload is required because Enumerable is a static type.
        ScriptEditor.Environment = ScriptEnvironment.Default
            .WithAdditionalNamespaceName("System.Linq")
            .WithAdditionalReferenceForType(typeof(Enumerable));
        ScriptEditor.Script = InitialScript;
        ScriptEditor.DiagnosticsUpdated += OnDiagnosticsUpdated;

        Closed += (_, _) => ScriptEditor.Dispose();
    }

    private void OnDiagnosticsUpdated(object? sender, DiagnosticsUpdatedEventArgs e)
    {
        var problems = e.Diagnostics
            .Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToList();

        ProblemsList.ItemsSource = problems.Select(FormatDiagnostic).ToList();

        var errors = problems.Count(d => d.Severity == DiagnosticSeverity.Error);
        var warnings = problems.Count - errors;

        ProblemsTab.Header = problems.Count == 0 ? "Problems" : $"Problems ({problems.Count})";
        StatusText.Text = problems.Count == 0
            ? "No problems"
            : $"{errors} error(s), {warnings} warning(s)";
    }

    private static string FormatDiagnostic(Diagnostic diagnostic)
    {
        var position = diagnostic.Location.GetLineSpan().StartLinePosition;
        var severity = diagnostic.Severity == DiagnosticSeverity.Error ? "error" : "warning";
        return $"({position.Line + 1},{position.Character + 1}) {severity} {diagnostic.Id}: {diagnostic.GetMessage()}";
    }

    private async void OnRunClick(object? sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        OutputBox.Text = string.Empty;
        BottomTabs.SelectedIndex = 0;

        try
        {
            var compiled = await ScriptEditor.CompileAsync();

            if (compiled.HasErrors)
            {
                AppendLine("Compilation failed:");
                foreach (var message in compiled.CompilationOutput.Messages)
                {
                    AppendLine(message);
                }

                return;
            }

            // Script console output may be written from any thread; marshal it to the UI.
            using var redirect = new ScriptConsoleRedirect(
                text => Dispatcher.UIThread.Post(() => Append(text ?? string.Empty)));

            var result = await compiled.RunAsync<object?>();

            // Let any queued console output land before the result line.
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

            if (result is not null)
            {
                AppendLine($"Result: {result}");
            }

            AppendLine("Finished.");
        }
        catch (Exception ex)
        {
            AppendLine($"Error: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
        }
    }

    private void Append(string text)
    {
        OutputBox.Text += text;
        OutputBox.CaretIndex = OutputBox.Text?.Length ?? 0;
    }

    private void AppendLine(string text)
    {
        Append(text + System.Environment.NewLine);
    }

    private void OnThemeSystemClick(object? sender, RoutedEventArgs e)
    {
        SetThemeVariant(ThemeVariant.Default);
    }

    private void OnThemeLightClick(object? sender, RoutedEventArgs e)
    {
        SetThemeVariant(ThemeVariant.Light);
    }

    private void OnThemeDarkClick(object? sender, RoutedEventArgs e)
    {
        SetThemeVariant(ThemeVariant.Dark);
    }

    // The editor follows the app's theme variant, so switching it here recolours both the
    // window chrome and the script editor together.
    private static void SetThemeVariant(ThemeVariant variant)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = variant;
        }
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
