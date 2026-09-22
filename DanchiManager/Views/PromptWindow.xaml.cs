using System.Windows;

namespace DanchiManager.Views;

public partial class PromptWindow : Window
{
    public string Value => InputBox.Text;

    public PromptWindow(string message, string title)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        Loaded += (_, _) => InputBox.Focus();
    }

    void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
