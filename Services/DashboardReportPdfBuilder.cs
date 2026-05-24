using System.Globalization;
using System.Text;
using BuildWise.Models;

namespace BuildWise.Services;

public sealed class DashboardReportPdfBuilder
{
    private readonly PdfCanvas _pdf = new();
    private readonly DashboardReportData _data;
    private readonly DashboardReportOptions _options;

    public DashboardReportPdfBuilder(DashboardReportData data, DashboardReportOptions options)
    {
        _data = data;
        _options = options;
    }

    public byte[] Build()
    {
        _pdf.NewPage();
        DrawCover();
        DrawSummary();

        if (_options.IncludeProperty) DrawProperty();
        if (_options.IncludeWorkforce) DrawWorkforce();
        if (_options.IncludeBudget) DrawBudget();
        if (_options.IncludeExpenses) DrawExpenses();
        if (_options.IncludeMaterials) DrawMaterials();
        if (_options.IncludeUsed) DrawUsage();
        if (_options.IncludeCharts) DrawCharts();

        DrawFooter();
        return _pdf.Save();
    }

    private void DrawCover()
    {
        _pdf.FillRect(0, 0, PdfCanvas.PageWidth, PdfCanvas.PageHeight, 248, 250, 252);
        _pdf.FillRect(0, PdfCanvas.PageHeight - 124, PdfCanvas.PageWidth, 124, 15, 23, 42);
        _pdf.FillRect(42, PdfCanvas.PageHeight - 116, 5, 92, 37, 99, 235);
        _pdf.Text("BUILDWISE", 58, PdfCanvas.PageHeight - 48, 10, true, 147, 197, 253);
        _pdf.Text("Project Metrics Report", 58, PdfCanvas.PageHeight - 76, 23, true, 255, 255, 255);
        _pdf.Text(_data.ScopeName, 58, PdfCanvas.PageHeight - 100, 12, false, 226, 232, 240);
        _pdf.Text($"Generated {DateTime.Now:MMM dd, yyyy h:mm tt}", 390, PdfCanvas.PageHeight - 48, 9, false, 203, 213, 225);
    }

    private void DrawSummary()
    {
        _pdf.Y = PdfCanvas.PageHeight - 162;
        _pdf.SectionTitle("Executive Summary");
        var remaining = Math.Max(0, _data.TotalBudget - _data.TotalExpenses);
        var spentPct = _data.TotalBudget > 0 ? _data.TotalExpenses / _data.TotalBudget * 100 : 0;
        _pdf.CardRow(new[]
        {
            ("Total Budget", Money(_data.TotalBudget)),
            ("Total Spent", Money(_data.TotalExpenses)),
            ("Remaining", Money(remaining)),
            ("Budget Used", $"{spentPct:0}%")
        });
    }

    private void DrawProperty()
    {
        _pdf.SectionTitle("Property & Project");
        if (_data.Properties.Count == 0)
        {
            _pdf.Note("No property information is available for this scope.");
            return;
        }

        _pdf.Table(
            new[] { "Property", "Type", "Location", "Area", "Status" },
            _data.Properties.Select(p => new[]
            {
                p.Name,
                p.Type,
                p.Location,
                p.Area,
                p.Status
            }).Take(8).ToList());
    }

    private void DrawWorkforce()
    {
        _pdf.SectionTitle("Workforce");
        _pdf.CardRow(new[]
        {
            ("Workers", _data.TotalWorkers.ToString("N0", CultureInfo.InvariantCulture)),
            ("Active", _data.ActiveWorkers.ToString("N0", CultureInfo.InvariantCulture)),
            ("Inactive", Math.Max(0, _data.TotalWorkers - _data.ActiveWorkers).ToString("N0", CultureInfo.InvariantCulture)),
            ("Avg Wage", Money(_data.AverageDailyWage))
        });

        if (_data.WorkerSkills.Count > 0)
        {
            _pdf.Table(
                new[] { "Skill", "Workers" },
                _data.WorkerSkills.Select(s => new[] { s.Name, s.Count.ToString("N0", CultureInfo.InvariantCulture) }).Take(6).ToList());
        }
    }

    private void DrawBudget()
    {
        _pdf.SectionTitle("Budget");
        var usedPct = _data.TotalBudget > 0 ? _data.TotalExpenses / _data.TotalBudget : 0;
        _pdf.Progress("Budget utilization", usedPct, $"{usedPct * 100:0}% used");
        _pdf.Table(
            new[] { "Metric", "Value" },
            new List<string[]>
            {
                new[] { "Approved / allocated budget", Money(_data.TotalBudget) },
                new[] { "Recorded spending", Money(_data.TotalExpenses) },
                new[] { "Remaining allocation", Money(Math.Max(0, _data.TotalBudget - _data.TotalExpenses)) }
            });
    }

    private void DrawExpenses()
    {
        _pdf.SectionTitle("Expenses");
        if (_data.ExpenseCategories.Count > 0)
        {
            _pdf.Table(
                new[] { "Category", "Amount" },
                _data.ExpenseCategories.Select(e => new[] { e.Name, Money(e.Amount) }).Take(10).ToList());
        }

        if (_data.RecentExpenses.Count > 0)
        {
            _pdf.Subtitle("Recent Expense Activity");
            _pdf.Table(
                new[] { "Date", "Category", "Amount" },
                _data.RecentExpenses.Select(e => new[] { e.Date, e.Category, Money(e.Amount) }).Take(8).ToList());
        }
    }

    private void DrawMaterials()
    {
        _pdf.SectionTitle("Materials");
        if (_data.Materials.Count == 0)
        {
            _pdf.Note("No material purchases or usage records are available for this scope.");
            return;
        }

        _pdf.Table(
            new[] { "Material", "Purchased", "Used", "Balance", "Cost" },
            _data.Materials.Select(m => new[]
            {
                m.Name,
                $"{m.Purchased:0.##} {m.Unit}",
                $"{m.Used:0.##} {m.Unit}",
                $"{m.Balance:0.##} {m.Unit}",
                Money(m.Cost)
            }).Take(10).ToList());
    }

    private void DrawUsage()
    {
        _pdf.SectionTitle("Progress & Usage");
        _pdf.Progress("Construction progress", _data.ConstructionProgress / 100m, $"{_data.ConstructionProgress:0}% complete");
        var materialUsedPct = _data.TotalMaterialPurchased > 0 ? _data.TotalMaterialUsed / _data.TotalMaterialPurchased : 0;
        _pdf.Progress("Material usage", materialUsedPct, $"{materialUsedPct * 100:0}% of purchased quantity used");
    }

    private void DrawCharts()
    {
        _pdf.SectionTitle("Charts");
        _pdf.Subtitle("Expense Distribution");
        var max = _data.ExpenseCategories.Count > 0 ? _data.ExpenseCategories.Max(c => c.Amount) : 0;
        foreach (var item in _data.ExpenseCategories.Take(7))
        {
            _pdf.Bar(item.Name, max > 0 ? item.Amount / max : 0, Money(item.Amount));
        }

        if (_data.MonthlyTrend.Count > 0)
        {
            _pdf.Subtitle("Monthly Spending Trend");
            var trendMax = _data.MonthlyTrend.Max(m => m.Amount);
            foreach (var item in _data.MonthlyTrend.TakeLast(6))
            {
                _pdf.Bar(item.Label, trendMax > 0 ? item.Amount / trendMax : 0, Money(item.Amount));
            }
        }
    }

    private void DrawFooter()
    {
        _pdf.DrawFooters("BuildWise project metrics report");
    }

    private static string Money(decimal value) => $"PKR {value.ToString("N0", new CultureInfo("en-IN"))}";
}

public sealed class DashboardReportData
{
    public string ScopeName { get; init; } = "Project";
    public decimal TotalBudget { get; init; }
    public decimal TotalExpenses { get; init; }
    public int TotalWorkers { get; init; }
    public int ActiveWorkers { get; init; }
    public decimal AverageDailyWage { get; init; }
    public decimal ConstructionProgress { get; init; }
    public decimal TotalMaterialPurchased { get; init; }
    public decimal TotalMaterialUsed { get; init; }
    public List<PropertyReportRow> Properties { get; init; } = new();
    public List<NameCount> WorkerSkills { get; init; } = new();
    public List<NameAmount> ExpenseCategories { get; init; } = new();
    public List<RecentExpenseReportRow> RecentExpenses { get; init; } = new();
    public List<MaterialReportRow> Materials { get; init; } = new();
    public List<NameAmount> MonthlyTrend { get; init; } = new();
}

public sealed record PropertyReportRow(string Name, string Type, string Location, string Area, string Status);
public sealed record NameCount(string Name, int Count);
public sealed record NameAmount(string Name, decimal Amount)
{
    public string Label => Name;
}
public sealed record RecentExpenseReportRow(string Date, string Category, decimal Amount);
public sealed record MaterialReportRow(string Name, string Unit, decimal Purchased, decimal Used, decimal Balance, decimal Cost);

internal sealed class PdfCanvas
{
    public const float PageWidth = 612;
    public const float PageHeight = 792;
    private const float Margin = 42;
    private readonly List<StringBuilder> _pages = new();
    private StringBuilder _content = new();
    private readonly List<Action<int, StringBuilder>> _footerWriters = new();
    private int _pageNo;
    public float Y { get; set; } = PageHeight - Margin;

    public void NewPage()
    {
        if (_pageNo > 0)
            _pages.Add(_content);

        _pageNo++;
        _content = new StringBuilder();
        Y = PageHeight - Margin;
    }

    public void SectionTitle(string text)
    {
        EnsureSpace(48);
        Text(text, Margin, Y, 15, true, 15, 23, 42);
        FillRect(Margin, Y - 8, 38, 3, 37, 99, 235);
        Y -= 30;
    }

    public void Subtitle(string text)
    {
        EnsureSpace(28);
        Text(text, Margin, Y, 11, true, 51, 65, 85);
        Y -= 18;
    }

    public void Note(string text)
    {
        EnsureSpace(32);
        FillRect(Margin, Y - 24, PageWidth - Margin * 2, 28, 248, 250, 252);
        StrokeRect(Margin, Y - 24, PageWidth - Margin * 2, 28, 226, 232, 240);
        Text(text, Margin + 10, Y - 12, 9, false, 100, 116, 139);
        Y -= 40;
    }

    public void CardRow(IReadOnlyList<(string Label, string Value)> cards)
    {
        EnsureSpace(74);
        var gap = 10f;
        var width = (PageWidth - Margin * 2 - gap * (cards.Count - 1)) / cards.Count;
        var x = Margin;
        foreach (var card in cards)
        {
            FillRect(x, Y - 56, width, 56, 255, 255, 255);
            StrokeRect(x, Y - 56, width, 56, 226, 232, 240);
            FillRect(x, Y - 56, 3, 56, 37, 99, 235);
            Text(card.Label, x + 12, Y - 20, 8, true, 100, 116, 139);
            Text(card.Value, x + 12, Y - 41, 12, true, 15, 23, 42);
            x += width + gap;
        }
        Y -= 78;
    }

    public void Table(string[] headers, List<string[]> rows)
    {
        if (rows.Count == 0)
        {
            Note("No records available.");
            return;
        }

        var columnWidth = (PageWidth - Margin * 2) / headers.Length;
        EnsureSpace(34);
        FillRect(Margin, Y - 24, PageWidth - Margin * 2, 24, 15, 23, 42);
        for (var i = 0; i < headers.Length; i++)
            Text(headers[i], Margin + i * columnWidth + 8, Y - 15, 8, true, 255, 255, 255);
        Y -= 24;

        var alternate = false;
        foreach (var row in rows)
        {
            EnsureSpace(24);
            FillRect(Margin, Y - 22, PageWidth - Margin * 2, 22, alternate ? (byte)248 : (byte)255, alternate ? (byte)250 : (byte)255, alternate ? (byte)252 : (byte)255);
            StrokeRect(Margin, Y - 22, PageWidth - Margin * 2, 22, 241, 245, 249);
            for (var i = 0; i < headers.Length && i < row.Length; i++)
                Text(Trim(row[i], 28), Margin + i * columnWidth + 8, Y - 14, 8, false, 51, 65, 85);
            Y -= 22;
            alternate = !alternate;
        }
        Y -= 18;
    }

    public void Progress(string label, decimal ratio, string value)
    {
        EnsureSpace(42);
        ratio = Math.Clamp(ratio, 0, 1);
        Text(label, Margin, Y, 9, true, 51, 65, 85);
        Text(value, PageWidth - Margin - 115, Y, 9, false, 100, 116, 139);
        FillRect(Margin, Y - 20, PageWidth - Margin * 2, 8, 226, 232, 240);
        FillRect(Margin, Y - 20, (PageWidth - Margin * 2) * (float)ratio, 8, 37, 99, 235);
        Y -= 38;
    }

    public void Bar(string label, decimal ratio, string value)
    {
        EnsureSpace(26);
        ratio = Math.Clamp(ratio, 0, 1);
        Text(Trim(label, 22), Margin, Y, 8, false, 51, 65, 85);
        FillRect(Margin + 130, Y - 8, 250, 8, 226, 232, 240);
        FillRect(Margin + 130, Y - 8, 250 * (float)ratio, 8, 20, 184, 166);
        Text(value, Margin + 392, Y, 8, false, 100, 116, 139);
        Y -= 22;
    }

    public void Text(string text, float x, float y, int size, bool bold, byte r, byte g, byte b)
    {
        _content.AppendLine($"BT /{(bold ? "F2" : "F1")} {size} Tf {r / 255.0:0.###} {g / 255.0:0.###} {b / 255.0:0.###} rg {x:0.##} {y:0.##} Td ({Escape(text)}) Tj ET");
    }

    public void FillRect(float x, float y, float width, float height, byte r, byte g, byte b)
    {
        _content.AppendLine($"{r / 255.0:0.###} {g / 255.0:0.###} {b / 255.0:0.###} rg {x:0.##} {y:0.##} {width:0.##} {height:0.##} re f");
    }

    public void StrokeRect(float x, float y, float width, float height, byte r, byte g, byte b)
    {
        _content.AppendLine($"{r / 255.0:0.###} {g / 255.0:0.###} {b / 255.0:0.###} RG {x:0.##} {y:0.##} {width:0.##} {height:0.##} re S");
    }

    public void DrawFooters(string label)
    {
        _footerWriters.Add((page, content) =>
        {
            content.AppendLine($"BT /F1 8 Tf 0.392 0.455 0.545 rg {Margin:0.##} 24 Td ({Escape(label)}) Tj ET");
            content.AppendLine($"BT /F1 8 Tf 0.392 0.455 0.545 rg {PageWidth - Margin - 46:0.##} 24 Td (Page {page}) Tj ET");
        });
    }

    public byte[] Save()
    {
        _pages.Add(_content);
        if (_footerWriters.Count > 0)
        {
            for (var i = 0; i < _pages.Count; i++)
                foreach (var writer in _footerWriters)
                    writer(i + 1, _pages[i]);
        }

        var objects = new List<string>();
        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        var kids = string.Join(" ", Enumerable.Range(0, _pages.Count).Select(i => $"{5 + i * 2} 0 R"));
        objects.Add($"<< /Type /Pages /Kids [{kids}] /Count {_pages.Count} >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

        for (var i = 0; i < _pages.Count; i++)
        {
            var streamId = 6 + i * 2;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {streamId} 0 R >>");
            var stream = _pages[i].ToString();
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream");
        }

        var output = new MemoryStream();
        Write(output, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            Write(output, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xref = output.Position;
        Write(output, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            Write(output, $"{offset:0000000000} 00000 n \n");
        Write(output, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }

    private void EnsureSpace(float height)
    {
        if (Y - height < 56)
            NewPage();
    }

    private static void Write(Stream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string Escape(string? value)
    {
        var safe = new string((value ?? "").Select(ch => ch is >= ' ' and <= '~' ? ch : ' ').ToArray());
        return safe.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..Math.Max(0, max - 1)] + "...";
}
