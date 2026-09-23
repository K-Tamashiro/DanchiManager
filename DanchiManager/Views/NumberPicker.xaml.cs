using System.Windows;
using System.Windows.Controls;

namespace DanchiManager.Views;

public partial class NumberPicker : UserControl
{
    public TextBox? Target { get; set; }
    public bool DigitsOnly { get; set; }
    public event EventHandler? NextRequested;
    public event EventHandler? ClearRequested;

    public NumberPicker()
    {
        InitializeComponent();
    }

    void Key_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string key }) return;
        if (key == "ENT")
        {
            Target?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            NextRequested?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (key == "C")
        {
            ClearRequested?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (Target is null) return;
        var text = Target.Text ?? "";
        switch (key)
        {
            case "AC":
                text = DigitsOnly ? "0" : "";
                break;
            case "-":
                if (DigitsOnly || text.Length >= 20) return;
                text += "-";
                break;
            default:
                if (DigitsOnly)
                {
                    if (text == "0") text = key;
                    else if (text.Length < 2) text += key;
                }
                else if (text.Length < 20)
                {
                    text += key;
                }
                break;
        }
        Target.Text = text;
        Target.CaretIndex = text.Length;
        Target.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }
}
