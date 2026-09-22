using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DanchiManager.Models;

namespace DanchiManager.Services;

public sealed class PrintService
{
    static readonly FontFamily Font = new("Yu Gothic UI, Meiryo UI, Meiryo, sans-serif");

    public void PrintBuilding(string title, IEnumerable<RoomRecord> rooms, BuildingStats stats, bool includePhone)
    {
        var doc = NewDoc(title);
        var table = Table(includePhone
            ? ["号室", "氏名", "電話", "空", "入院", "人数", "バイク", "自転車", "備考"]
            : ["号室", "氏名", "空", "入院", "人数", "バイク", "自転車", "備考"]);
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
        doc.Blocks.Add(table);
        Print(doc, title);
    }

    public void PrintTotals(string title, IEnumerable<(string Name, BuildingStats Stats)> rows, BuildingStats all)
    {
        var doc = NewDoc(title);
        var table = Table(["棟", "戸数", "空き", "入院", "人数", "バイク", "自転車"]);
        foreach (var (name, s) in rows)
            AddRow(table, [name, s.Units.ToString(), s.Vacant.ToString(), s.Hospital.ToString(), s.People.ToString(), s.Bikes.ToString(), s.Bicycles.ToString()]);
        AddRow(table, ["合計", all.Units.ToString(), all.Vacant.ToString(), all.Hospital.ToString(), all.People.ToString(), all.Bikes.ToString(), all.Bicycles.ToString()]);
        doc.Blocks.Add(table);
        Print(doc, title);
    }

    public void PrintEnvelopes(int year, IEnumerable<RoomRecord> members)
    {
        var doc = new FlowDocument
        {
            FontFamily = Font,
            FontSize = 16,
            PagePadding = new Thickness(40),
        };
        foreach (var r in members)
        {
            var sec = new Section { BreakPageBefore = doc.Blocks.Count > 0 };
            sec.Blocks.Add(P($"令和{year}年度 会費", 14, FontWeights.Normal));
            sec.Blocks.Add(P(r.Name + "  様", 22, FontWeights.Bold));
            sec.Blocks.Add(P($"{r.BuildingName.Replace("棟", "")}  {r.RoomNo} 号室", 16, FontWeights.Normal));
            doc.Blocks.Add(sec);
        }
        Print(doc, "会費封筒");
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
        Print(doc, "会費一覧");
    }

    static FlowDocument NewDoc(string title)
    {
        var doc = new FlowDocument
        {
            FontFamily = Font,
            FontSize = 12,
            PagePadding = new Thickness(24),
        };
        doc.Blocks.Add(P(title, 18, FontWeights.Bold));
        return doc;
    }

    static Paragraph P(string text, double size, FontWeight w) => new(new Run(text))
    {
        FontSize = size,
        FontWeight = w,
        Margin = new Thickness(0, 0, 0, 8),
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

    static void Print(FlowDocument doc, string description)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;
        doc.PageWidth = dlg.PrintableAreaWidth;
        doc.PageHeight = dlg.PrintableAreaHeight;
        IDocumentPaginatorSource src = doc;
        dlg.PrintDocument(src.DocumentPaginator, description);
    }
}
