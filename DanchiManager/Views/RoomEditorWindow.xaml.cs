using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DanchiManager.Services;
using DanchiManager.ViewModels;

namespace DanchiManager.Views;

public partial class RoomEditorWindow : Window
{
    readonly TextBox[] _padTargets;
    readonly TextBox[] _fields;
    bool _keepCaret;
    static readonly Brush IdleBorder = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
    static readonly Brush ActiveBorder = new SolidColorBrush(Color.FromRgb(0xC0, 0x40, 0x10));

    public RoomEditorWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
        _padTargets = [PeopleBox, BikesBox, BicyclesBox, PhoneBox];
        _fields = [NameBox, FuriganaBox, PeopleBox, BikesBox, BicyclesBox, PhoneBox, NoteBox];
        foreach (var box in _fields)
        {
            box.GotKeyboardFocus += SelectAllOnFocus;
            box.PreviewMouseLeftButtonDown += SelectAllOnMouse;
        }
        Picker.NextRequested += (_, _) => MoveNext();
        Picker.ClearRequested += (_, _) => ClearFocusedText();
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += (_, _) =>
        {
            ImeReadingHelper.Attach(NameBox, reading =>
            {
                if (DataContext is RoomEditorViewModel vm)
                    vm.AppendFurigana(reading);
            });
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.FocusedElement is not TextBox box) return;
        if (box == NoteBox)
        {
            if (Keyboard.Modifiers != ModifierKeys.None) return;
            var text = box.Text ?? "";
            var caret = box.CaretIndex;
            if (caret > 0 && (text[caret - 1] == '\n' || text[caret - 1] == '\r'))
            {
                var after = text.IndexOf('\n', caret);
                if (after < 0) after = text.Length;
                if (text[caret..after].Trim('\r').Length == 0)
                {
                    e.Handled = true;
                    var remove = text[caret - 1] == '\n' && caret >= 2 && text[caret - 2] == '\r' ? 2 : 1;
                    box.Text = text.Remove(caret - remove, remove);
                    box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                    MoveNext();
                }
            }
            return;
        }
        e.Handled = true;
        MoveNext();
    }

    void SelectAllOnFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox box) return;
        _lastText = box;
        if (box != PeopleBox && box != BikesBox && box != BicyclesBox && box != PhoneBox)
            ClearPadTarget();
        box.Dispatcher.BeginInvoke(() =>
        {
            if (_keepCaret)
            {
                _keepCaret = false;
                box.CaretIndex = box.Text.Length;
                return;
            }
            box.SelectAll();
        }, DispatcherPriority.Input);
    }

    void SelectAllOnMouse(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBox box || box.IsKeyboardFocusWithin) return;
        e.Handled = true;
        box.Focus();
    }

    void ClearFocusedText()
    {
        var box = Keyboard.FocusedElement as TextBox;
        if (box is null || Array.IndexOf(_fields, box) < 0)
            box = _lastText;
        if (box is null) return;
        box.Text = box == PeopleBox || box == BikesBox || box == BicyclesBox ? "0" : "";
        box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        _keepCaret = true;
        box.Focus();
    }

    TextBox? _lastText;

    void MoveNext()
    {
        var current = Keyboard.FocusedElement as TextBox;
        if (current is null || Array.IndexOf(_fields, current) < 0)
            current = Picker.Target;
        var index = current is null ? -1 : Array.IndexOf(_fields, current);
        var next = _fields[index < 0 ? 0 : (index + 1) % _fields.Length];
        next.Focus();
    }

    void ClearPadTarget()
    {
        foreach (var item in _padTargets)
        {
            item.BorderBrush = IdleBorder;
            item.BorderThickness = new Thickness(1);
        }
        Picker.Target = null;
        TargetCaption.Text = "数字欄を選んでください";
    }

    void Count_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box)
            Activate(box, box.Tag as string ?? "", digitsOnly: true);
    }

    void Phone_GotFocus(object sender, RoutedEventArgs e) => Activate(PhoneBox, "電話", digitsOnly: false);

    void Activate(TextBox box, string label, bool digitsOnly)
    {
        foreach (var item in _padTargets)
        {
            item.BorderBrush = item == box ? ActiveBorder : IdleBorder;
            item.BorderThickness = new Thickness(item == box ? 2 : 1);
        }
        Picker.Target = box;
        Picker.DigitsOnly = digitsOnly;
        TargetCaption.Text = "入力先：" + label;
    }

    void PhonePrefix_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string prefix } && DataContext is RoomEditorViewModel vm)
        {
            vm.ApplyPhonePrefix(prefix);
            Activate(PhoneBox, "電話", digitsOnly: false);
            _keepCaret = true;
            PhoneBox.Focus();
        }
    }

    void NoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string text } && DataContext is RoomEditorViewModel vm)
        {
            vm.AppendNoteCommand.Execute(text);
            _keepCaret = true;
            NoteBox.Focus();
        }
    }
}
