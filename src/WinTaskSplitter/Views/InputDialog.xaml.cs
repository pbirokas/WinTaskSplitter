using System.Windows;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Key = System.Windows.Input.Key;

namespace WinTaskSplitter.Views;

public partial class InputDialog : Window
{
    public string Result { get; private set; } = string.Empty;

    public InputDialog(string title, string label, string defaultValue = "")
    {
        InitializeComponent();
        Title           = title;
        LabelText.Text  = label;
        InputBox.Text   = defaultValue;
        InputBox.SelectAll();
        Loaded += (_, _) => InputBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Result      = InputBox.Text;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)  { Result = InputBox.Text; DialogResult = true; }
        if (e.Key == Key.Escape) DialogResult = false;
    }
}
