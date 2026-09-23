using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DanchiManager.Models;

namespace DanchiManager.Services;

public sealed class PrintService
{
    static readonly FontFamily Font = new("Yu Gothic UI, Meiryo UI, Meiryo, sans-serif");

    // JIS 角形8号 119mm × 197mm
    const double EnvelopeWidthMm = 119;
    const double EnvelopeHeightMm = 197;

    static double Mm(double mm) => mm * 96.0 / 25.4;

    public void PrintBuilding(string title, IEnumerable<RoomRecord> rooms, BuildingStats stats, bool includePhone)
    {
        var doc = NewDoc();
        doc.Blocks.Add(BuildingSection(title, rooms, stats, includePhone));
        PreviewAndPrint(doc, title);
    }

    public void PrintBuildings(IEnumerable<(string Title, IReadOnlyList<RoomRecord> Rooms, BuildingStats Stats)> buildings)
    {
        var doc = BuildingsDocument(buildings);
        if (doc.Blocks.Count > 0)
            PreviewAndPrint(doc, "全棟情報");
    }

    static FlowDocument BuildingsDocument(IEnumerable<(string Title, IReadOnlyList<RoomRecord> Rooms, BuildingStats Stats)> buildings)
    {
        var doc = NewDoc();
        foreach (var building in buildings)
        {
            var section = BuildingSection(building.Title, building.Rooms, building.Stats, includePhone: true);
            section.BreakPageBefore = doc.Blocks.Count > 0;
            doc.Blocks.Add(section);
        }
        return doc;
    }

    static Section BuildingSection(string title, IEnumerable<RoomRecord> rooms, BuildingStats stats, bool includePhone)
    {
        var section = new Section();
        section.Blocks.Add(P(title, 18, FontWeights.Bold));
        var table = Table(includePhone
            ? ["号室", "氏名", "電話", "空", "入院", "人数", "バイク", "自転車", "備考"]
            : ["号室", "氏名", "空", "入院", "人数", "バイク", "自転車", "備考"]);
        // Give names, phone numbers and notes more room than numeric columns.
        double[] weights = includePhone
            ? [0.8, 1.8, 2.0, 0.5, 0.7, 0.7, 0.9, 1.0, 2.6]
            : [0.8, 1.8, 0.5, 0.7, 0.7, 0.9, 1.0, 2.6];
        for (var i = 0; i < weights.Length; i++)
            table.Columns[i].Width = new GridLength(weights[i], GridUnitType.Star);
        foreach (var r in rooms.OrderBy(x => x.RoomNo))
        {
            var cells = includePhone
                ? new[]
                {
                    r.RoomNo, r.Name, r.Phone,
                    r.Vacancy == VacancyFlag.Vacant ? "○" : "",
                    r.Vacancy == VacancyFlag.Hospital ? "○" : "",
                    r.People > 0 ? r.People.ToString() : "",
                    r.Bikes > 0 ? r.Bikes.ToString() : "",
                    r.Bicycles > 0 ? r.Bicycles.ToString() : "",
                    r.Note,
                }
                : new[]
                {
                    r.RoomNo, r.Name,
                    r.Vacancy == VacancyFlag.Vacant ? "○" : "",
                    r.Vacancy == VacancyFlag.Hospital ? "○" : "",
                    r.People > 0 ? r.People.ToString() : "",
                    r.Bikes > 0 ? r.Bikes.ToString() : "",
                    r.Bicycles > 0 ? r.Bicycles.ToString() : "",
                    r.Note,
                };
            AddRow(table, cells);
        }
        AddRow(table, includePhone
            ? ["合計", "", "", stats.Vacant.ToString(), stats.Hospital.ToString(), stats.People.ToString(), stats.Bikes.ToString(), stats.Bicycles.ToString(), ""]
            : ["合計", "", stats.Vacant.ToString(), stats.Hospital.ToString(), stats.People.ToString(), stats.Bikes.ToString(), stats.Bicycles.ToString(), ""]);
        section.Blocks.Add(table);
        return section;
    }

    public void PrintTotals(string title, IEnumerable<(string Name, BuildingStats Stats)> rows, BuildingStats all)
    {
        var doc = NewDoc(title);
        var table = Table(["棟", "戸数", "空き", "入院", "人数", "バイク", "自転車"]);
        foreach (var (name, s) in rows)
            AddRow(table, [name, s.Units.ToString(), s.Vacant.ToString(), s.Hospital.ToString(), s.People.ToString(), s.Bikes.ToString(), s.Bicycles.ToString()]);
        AddRow(table, ["合計", all.Units.ToString(), all.Vacant.ToString(), all.Hospital.ToString(), all.People.ToString(), all.Bikes.ToString(), all.Bicycles.ToString()]);
        doc.Blocks.Add(table);
        PreviewAndPrint(doc, title);
    }

    public void PrintEnvelopes(int year, IEnumerable<RoomRecord> members)
    {
        var settings = PathService.LoadSettings();
        var startMonth = settings.EnvelopeStartMonth is >= 1 and <= 12 ? settings.EnvelopeStartMonth : 4;
        var months = Enumerable.Range(0, 12).Select(i => $"{(startMonth - 1 + i) % 12 + 1}月").ToArray();
        var title = string.IsNullOrWhiteSpace(settings.EnvelopeTitle) ? AppConstants.AssociationTitle : settings.EnvelopeTitle;
        var monthly = settings.MonthlyFee > 0 ? settings.MonthlyFee : AppConstants.DefaultFee;
        var yearly = monthly * 12;
        var doc = new FlowDocument
        {
            FontFamily = Font,
            FontSize = 12,
            PagePadding = new Thickness(Mm(10), Mm(35), Mm(10), Mm(5)),
            TextAlignment = TextAlignment.Center,
        };
        foreach (var r in members)
        {
            var sec = new Section { BreakPageBefore = doc.Blocks.Count > 0 };
            sec.Blocks.Add(P(title, 16, FontWeights.Bold, TextAlignment.Center, 4));
            sec.Blocks.Add(P($"令和 {year} 年度分    {r.Name}  様", 16, FontWeights.Bold, TextAlignment.Center, 4));
            sec.Blocks.Add(P($"{r.BuildingName}  {r.RoomNo}  号", 12, FontWeights.Normal, TextAlignment.Center, 4));
            sec.Blocks.Add(P($"自治会費 毎月{monthly}円です。年間{yearly}円です。", 10, FontWeights.Normal, TextAlignment.Center, 6));

            var pair = new Table { CellSpacing = 0 };
            pair.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            pair.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var group = new TableRowGroup();
            pair.RowGroups.Add(group);
            var row = new TableRow();
            row.Cells.Add(new TableCell(MonthStampTable(months[..6]))
            {
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0, 4, 3, 0),
            });
            row.Cells.Add(new TableCell(MonthStampTable(months[6..]))
            {
                BorderThickness = new Thickness(0),
                Padding = new Thickness(3, 4, 0, 0),
            });
            group.Rows.Add(row);
            sec.Blocks.Add(pair);
            doc.Blocks.Add(sec);
        }
        PreviewAndPrint(doc, "会費封筒", EnvelopeWidthMm, EnvelopeHeightMm);
    }

    static Table MonthStampTable(string[] months)
    {
        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(1.1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.45, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.45, GridUnitType.Star) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        var header = new TableRow();
        foreach (var h in new[] { "月", "班長印", "自治会印" })
            header.Cells.Add(StampCell(h, header: true));
        group.Rows.Add(header);
        foreach (var month in months)
        {
            var row = new TableRow();
            row.Cells.Add(StampCell(month, header: false));
            row.Cells.Add(StampCell("", header: false));
            row.Cells.Add(StampCell("", header: false));
            group.Rows.Add(row);
        }
        return table;
    }

    static TableCell StampCell(string text, bool header)
    {
        var p = new Paragraph(new Run(text))
        {
            Margin = new Thickness(0),
            TextAlignment = TextAlignment.Center,
            FontSize = header ? 10 : 12,
            FontWeight = header ? FontWeights.SemiBold : FontWeights.Normal,
        };
        return new TableCell(p)
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1.2),
            Padding = new Thickness(3, header ? 4 : 20, 3, header ? 4 : 20),
            TextAlignment = TextAlignment.Center,
        };
    }

    public void PrintFeeList(int year, IEnumerable<(string Building, string RoomNo, string Name, int Total, bool Member, int Sort)> rows)
    {
        var doc = NewDoc($"令和{year}年度 会費集金一覧");
        var table = Table(["棟", "号室", "氏名", "会費合計", "会員"]);
        var sum = 0;
        foreach (var r in rows)
        {
            sum += r.Total;
            AddRow(table, [r.Building, r.RoomNo, r.Name, r.Total.ToString("N0"), r.Member ? "○" : ""]);
        }
        AddRow(table, ["合計", "", "", sum.ToString("N0"), ""]);
        doc.Blocks.Add(table);
        PreviewAndPrint(doc, "会費一覧");
    }

    public void PrintParking(IEnumerable<ParkingLot> lots)
    {
        var doc = NewDoc();
        foreach (var lot in lots)
        {
            var section = ParkingSection(lot);
            section.BreakPageBefore = doc.Blocks.Count > 0;
            doc.Blocks.Add(section);
        }
        if (doc.Blocks.Count > 0)
            PreviewAndPrint(doc, "駐車場管理");
    }

    static Section ParkingSection(ParkingLot lot)
    {
        var occupied = lot.OccupiedCount;
        var usable = lot.Slots.Count(s => !s.IsInvalid);
        var section = new Section();
        section.Blocks.Add(P($"{lot.Name}  （月額 {lot.Fee:N0} 円）", 18, FontWeights.Bold));
        section.Blocks.Add(P($"使用中 {occupied} 台 / 利用可能 {usable} 枠 / 全 {lot.Slots.Count} 枠", 13, FontWeights.Normal));
        var table = Table(["番号", "状態", "使用者", "車番"]);
        double[] weights = [0.7, 1.0, 3.0, 2.3];
        for (var i = 0; i < weights.Length; i++)
            table.Columns[i].Width = new GridLength(weights[i], GridUnitType.Star);
        foreach (var s in lot.Slots)
        {
            var status = s.IsInvalid ? "利用不可" : s.IsVacant ? "空き" : "使用中";
            AddRow(table, [
                s.No.ToString(),
                status,
                s.IsInvalid ? "" : s.Occupant,
                s.IsInvalid ? "" : s.CarNo,
            ]);
        }
        section.Blocks.Add(table);
        return section;
    }

    static FlowDocument NewDoc(string? title = null)
    {
        var doc = new FlowDocument
        {
            FontFamily = Font,
            FontSize = 12,
            PagePadding = new Thickness(24),
        };
        if (title is not null)
            doc.Blocks.Add(P(title, 18, FontWeights.Bold));
        return doc;
    }

    static Paragraph P(string text, double size, FontWeight w, TextAlignment align = TextAlignment.Left, double bottom = 8) => new(new Run(text))
    {
        FontSize = size,
        FontWeight = w,
        TextAlignment = align,
        Margin = new Thickness(0, 0, 0, bottom),
    };

    static Table Table(string[] headers)
    {
        var table = new Table { CellSpacing = 0 };
        foreach (var _ in headers)
            table.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        var hr = new TableRow();
        foreach (var h in headers)
            hr.Cells.Add(Cell(h, true));
        group.Rows.Add(hr);
        return table;
    }

    static void AddRow(Table table, string[] cells)
    {
        var row = new TableRow();
        foreach (var c in cells) row.Cells.Add(Cell(c, false));
        table.RowGroups[0].Rows.Add(row);
    }

    static TableCell Cell(string text, bool header) => new(new Paragraph(new Run(text)) { Margin = new Thickness(2) })
    {
        BorderBrush = Brushes.Gray,
        BorderThickness = new Thickness(0.5),
        Padding = new Thickness(4, 2, 4, 2),
        FontWeight = header ? FontWeights.Bold : FontWeights.Normal,
        Background = header ? new SolidColorBrush(Color.FromRgb(0xE8, 0xE4, 0xD4)) : Brushes.White,
    };


    static void PreviewAndPrint(
        FlowDocument doc,
        string description,
        double? widthMm = null,
        double? heightMm = null)
    {
        // 封筒は指定サイズ、通常帳票はA4縦でプレビューする
        var width = widthMm is double wmm ? Mm(wmm) : Mm(210);
        var height = heightMm is double hmm ? Mm(hmm) : Mm(297);
        ConfigurePage(doc, width, height);

        // 用紙そのものは白のまま表示する
        doc.Background = Brushes.White;

        var viewer = new FlowDocumentReader
        {
            Document = doc,
            Margin = new Thickness(8),
            ViewingMode = FlowDocumentReaderViewingMode.Page,
        };

        var printButton = new Button
        {
            Content = "印刷",
            Width = 110,
            Height = 36,
            Margin = new Thickness(6),
            IsDefault = true,
        };

        var closeButton = new Button
        {
            Content = "閉じる",
            Width = 110,
            Height = 36,
            Margin = new Thickness(6),
            IsCancel = true,
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(8, 0, 8, 8),
        };
        buttons.Children.Add(printButton);
        buttons.Children.Add(closeButton);

        var root = new DockPanel
        {
            // プレビューの用紙外をグレーにして、白い用紙範囲を見分けやすくする
            Background = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xD8)),
        };
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(viewer);

        var window = new Window
        {
            Title = $"印刷プレビュー - {description}",
            Width = 760,
            Height = 900,
            MinWidth = 560,
            MinHeight = 650,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = root,
        };

        if (Application.Current?.MainWindow is Window owner && owner != window)
            window.Owner = owner;

        printButton.Click += (_, _) =>
        {
            var padding = doc.PagePadding;
            printButton.IsEnabled = false;
            try
            {
                if (widthMm.HasValue && heightMm.HasValue)
                    Print(doc, description, widthMm.Value, heightMm.Value);
                else
                    DirectPrint(doc, description, 210, 297);
            }
            catch (Exception ex)
            {
                MessageBox.Show(window,
                    $"印刷できませんでした。プリンターと用紙設定を確認してください。\n\n{ex.Message}",
                    "印刷エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                doc.PagePadding = padding;
                ConfigurePage(doc, width, height);
                printButton.IsEnabled = true;
            }
        };

        // Ctrl+Pも同じ設定・エラー処理を通す。
        viewer.CommandBindings.Add(new System.Windows.Input.CommandBinding(
            System.Windows.Input.ApplicationCommands.Print, (_, e) =>
            {
                printButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
            }));
        closeButton.Click += (_, _) => window.Close();
        window.ShowDialog();
    }

    static void DirectPrint(FlowDocument doc, string description, double widthMm, double heightMm)
    {
        using var server = new LocalPrintServer();
        using var queue = server.DefaultPrintQueue
            ?? throw new InvalidOperationException("既定のプリンターが設定されていません。");
        var ticket = queue.DefaultPrintTicket?.Clone() ?? new PrintTicket();
        ticket.PageMediaSize = new PageMediaSize(Mm(widthMm), Mm(heightMm));
        ticket = PreparePrint(doc, queue, ticket, widthMm, heightMm);
        queue.CurrentJobSettings.Description = description;
        var writer = PrintQueue.CreateXpsDocumentWriter(queue);
        writer.Write(((IDocumentPaginatorSource)doc).DocumentPaginator, ticket);
    }

    static void Print(FlowDocument doc, string description, double widthMm, double heightMm)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;
        var queue = dlg.PrintQueue
            ?? throw new InvalidOperationException("プリンターが選択されていません。");
        // 選択した封筒用紙を保持し、検証後のチケットを実際の印刷にも使う。
        var ticket = dlg.PrintTicket?.Clone() ?? new PrintTicket();
        dlg.PrintTicket = PreparePrint(doc, queue, ticket, widthMm, heightMm);
        dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, description);
    }

    static PrintTicket PreparePrint(FlowDocument doc, PrintQueue queue, PrintTicket ticket,
        double widthMm, double heightMm)
    {
        ticket.PageOrientation = PageOrientation.Portrait;
        var validated = queue.MergeAndValidatePrintTicket(queue.DefaultPrintTicket, ticket).ValidatedPrintTicket;
        var size = validated.PageMediaSize;
        var width = Mm(widthMm);
        var height = Mm(heightMm);
        if (size?.Width is not double actualWidth || size.Height is not double actualHeight ||
            Math.Abs(actualWidth - width) > Mm(1) || Math.Abs(actualHeight - height) > Mm(1) ||
            validated.PageOrientation != PageOrientation.Portrait)
            throw new InvalidOperationException(
                $"用紙を縦向き {widthMm} × {heightMm} mm に設定してください。プリンターがこの設定に対応しているか確認してください。");

        var area = queue.GetPrintCapabilities(validated).PageImageableArea
            ?? throw new InvalidOperationException("プリンターの印刷可能領域を取得できませんでした。");
        var padding = doc.PagePadding;
        var safePadding = new Thickness(
            Math.Max(padding.Left, area.OriginWidth),
            Math.Max(padding.Top, area.OriginHeight),
            Math.Max(padding.Right, width - area.OriginWidth - area.ExtentWidth),
            Math.Max(padding.Bottom, height - area.OriginHeight - area.ExtentHeight));
        if (safePadding.Left + safePadding.Right >= width || safePadding.Top + safePadding.Bottom >= height)
            throw new InvalidOperationException("用紙の印刷可能領域が小さすぎます。用紙設定を確認してください。");
        doc.PagePadding = safePadding;
        ConfigurePage(doc, width, height);
        return validated;
    }
    static void ConfigurePage(FlowDocument doc, double width, double height)
    {
        // Use one full-width column instead of FlowDocument's default newspaper columns.
        doc.ColumnWidth = double.PositiveInfinity;
        doc.PageWidth = width;
        doc.PageHeight = height;
    }
}
