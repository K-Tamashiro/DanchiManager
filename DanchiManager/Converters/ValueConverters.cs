using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DanchiManager.Models;

namespace DanchiManager.Converters;

public sealed class VacancyToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) =>
        value is VacancyFlag.Vacant ? "空" : value is VacancyFlag.Hospital ? "入院" : "";

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        var b = value is true;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
}

public sealed class FloorHeaderConverter : IMultiValueConverter
{
    static readonly Color[] Floors =
    [
        Color.FromRgb(0xD8, 0xB8, 0xF0),
        Color.FromRgb(0xE0, 0xB8, 0x88),
        Color.FromRgb(0xF0, 0x98, 0x98),
        Color.FromRgb(0x98, 0xD8, 0xB0),
        Color.FromRgb(0x88, 0xB8, 0xE8),
        Color.FromRgb(0xE8, 0xC8, 0x70),
        Color.FromRgb(0xC0, 0xA0, 0xE0),
        Color.FromRgb(0xA0, 0xD0, 0x90),
    ];

    public object Convert(object[] values, Type t, object p, CultureInfo c)
    {
        if (values.Length < 3) return Brushes.Gray;
        var vacancy = values[0] is VacancyFlag v ? v : VacancyFlag.Occupied;
        var gender = values[1] is GenderFlag g ? g : GenderFlag.Unset;
        var roomNo = values[2] as string ?? "101";
        if (vacancy == VacancyFlag.Hospital)
            return Brush(Color.FromRgb(0xF0, 0x70, 0x90), Color.FromRgb(0xD0, 0x50, 0x70));
        if (vacancy == VacancyFlag.Vacant)
            return Brush(Color.FromRgb(0x5A, 0x5A, 0x5A), Color.FromRgb(0x3A, 0x3A, 0x3A));
        if (vacancy != VacancyFlag.Vacant && gender == GenderFlag.Male)
            return Brush(Color.FromRgb(0x90, 0xC8, 0xF0), Color.FromRgb(0x58, 0x90, 0xC0));
        if (vacancy != VacancyFlag.Vacant && gender == GenderFlag.Female)
            return Brush(Color.FromRgb(0xF0, 0xA0, 0xC8), Color.FromRgb(0xD0, 0x70, 0x98));
        var floor = 1;
        if (int.TryParse(roomNo, out var n)) floor = Math.Max(1, n / 100);
        var col = Floors[(floor - 1) % Floors.Length];
        var lo = Color.FromRgb((byte)(col.R * 0.75), (byte)(col.G * 0.75), (byte)(col.B * 0.75));
        return Brush(col, lo);
    }

    static LinearGradientBrush Brush(Color hi, Color lo) => new(hi, lo, 90);

    public object[] ConvertBack(object value, Type[] t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is not true;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => value is not true;
}

public sealed class NonEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) =>
        value is string s && s.Trim().Length > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
}

public sealed class ScaleConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        var n = value is double d ? d : value is int i ? i : 16d;
        var m = 1d;
        if (p is not null)
            double.TryParse(p.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out m);
        return n * m;
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
}

public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) =>
        value is not null && p is not null && value.ToString() == p.ToString();

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
    {
        if (value is not true || p is null) return Binding.DoNothing;
        var name = p.ToString();
        if (string.IsNullOrEmpty(name)) return Binding.DoNothing;
        if (Enum.TryParse(typeof(VacancyFlag), name, out var vac)) return vac;
        if (Enum.TryParse(typeof(GenderFlag), name, out var gen)) return gen;
        var enumType = Nullable.GetUnderlyingType(t) ?? t;
        if (enumType.IsEnum) return Enum.Parse(enumType, name);
        return Binding.DoNothing;
    }
}

public sealed class RoomRowWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[1] is not int count || count <= 0) return 0d;
        var viewport = values[0] is double width && double.IsFinite(width) ? Math.Max(0, width) : 0;
        // カード本体174 + 左右のMargin各2。横に見せる列数は最大9列。
        var slotWidth = Math.Max(178d, viewport / Math.Min(count, 9));
        return slotWidth * count;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}