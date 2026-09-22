using System.Windows;
using System.Windows.Controls;
using DanchiManager.Services;
using DanchiManager.ViewModels;

namespace DanchiManager.Views;

public partial class RoomEditorWindow : Window
{
    public RoomEditorWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
        Loaded += (_, _) =>
        {
            ImeReadingHelper.Attach(NameBox, reading =>
            {
                if (DataContext is RoomEditorViewModel vm)
                    vm.AppendFurigana(reading);
            });
        };
    }

    void PhonePrefix_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string prefix } && DataContext is RoomEditorViewModel vm)
            vm.Draft.Phone += prefix;
    }
}
