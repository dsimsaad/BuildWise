using System.Globalization;
using BuildWise.Models;

namespace BuildWise.Services;

public sealed class TransactionReportPdfBuilder : IReportPdfBuilder
{
    private readonly PdfCanvas _pdf = new();
    private readonly IReadOnlyList<TransactionLog> _transactions;
    private readonly string _rangeLabel;

    public TransactionReportPdfBuilder(IReadOnlyList<TransactionLog> transactions, string rangeLabel)
    {
        _transactions = transactions;
        _rangeLabel = rangeLabel;
    }

    public byte[] Build()
    {
        _pdf.NewPage();
        DrawHeader();
        DrawSummary();
        DrawTransactionTable();
        _pdf.DrawFooters("BuildWise transaction report");
        return _pdf.Save();
    }

    private void DrawHeader()
    {
        _pdf.FillRect(0, 0, PdfCanvas.PageWidth, PdfCanvas.PageHeight, 248, 250, 252);
        _pdf.FillRect(0, PdfCanvas.PageHeight - 110, PdfCanvas.PageWidth, 110, 15, 23, 42);
        _pdf.FillRect(42, PdfCanvas.PageHeight - 100, 5, 78, 37, 99, 235);
        _pdf.Text("BUILDWISE", 58, PdfCanvas.PageHeight - 43, 10, true, 147, 197, 253);
        _pdf.Text("Transaction Ledger Report", 58, PdfCanvas.PageHeight - 72, 23, true, 255, 255, 255);
        _pdf.Text(_rangeLabel, 58, PdfCanvas.PageHeight - 94, 11, false, 226, 232, 240);
        _pdf.Text($"Generated {DateTime.Now:MMM dd, yyyy h:mm tt}", 392, PdfCanvas.PageHeight - 43, 9, false, 203, 213, 225);
        _pdf.Y = PdfCanvas.PageHeight - 148;
    }

    private void DrawSummary()
    {
        var totalAmount = _transactions.Sum(t => t.Amount);
        var addedCount = _transactions.Count(t => string.Equals(t.TransactionType, "Added", StringComparison.OrdinalIgnoreCase));
        var updatedCount = _transactions.Count(t => string.Equals(t.TransactionType, "Updated", StringComparison.OrdinalIgnoreCase));
        var deletedCount = _transactions.Count(t => string.Equals(t.TransactionType, "Deleted", StringComparison.OrdinalIgnoreCase));

        _pdf.SectionTitle("Summary");
        _pdf.CardRow(new[]
        {
            ("Logs", _transactions.Count.ToString("N0", CultureInfo.InvariantCulture)),
            ("Total Value", Money(totalAmount)),
            ("Added", addedCount.ToString("N0", CultureInfo.InvariantCulture)),
            ("Updated / Deleted", $"{updatedCount:N0} / {deletedCount:N0}")
        });
    }

    private void DrawTransactionTable()
    {
        _pdf.SectionTitle("Recent Ledger Entries");
        if (_transactions.Count == 0)
        {
            _pdf.Note("No transactions matched the selected filters.");
            return;
        }

        _pdf.Table(
            new[] { "Date", "Type", "Category", "Amount", "Effect", "Description" },
            _transactions
                .OrderByDescending(t => t.TransactionDate)
                // The PDF stays readable by showing the latest ledger entries first.
                .Take(22)
                .Select(t => new[]
                {
                    t.TransactionDate.ToString("MMM dd, yyyy"),
                    t.TransactionType,
                    t.Category,
                    Money(t.Amount),
                    $"{t.BudgetEffect:0.##}%",
                    t.Description
                })
                .ToList());

        if (_transactions.Count > 22)
            _pdf.Note($"Showing latest 22 of {_transactions.Count:N0} matching transactions.");
    }

    private static string Money(decimal value) => $"PKR {value.ToString("N0", new CultureInfo("en-IN"))}";
}
