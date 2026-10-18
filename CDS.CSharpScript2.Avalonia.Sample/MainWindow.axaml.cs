using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CDS.CSharpScript2.Avalonia.Sample;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ScriptEditor.Text = "// C# script editing coming soon\nvar x = 42;\nx * 2\n";
        OutputBox.Text = "Ready.";
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
