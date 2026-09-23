using Microsoft.EntityFrameworkCore;
using MiniGate.Data;
using MiniGate.Models;

namespace MiniGate.Services;

/// <summary>Một dòng của báo cáo tình hình sử dụng hóa đơn (BC26/AC).</summary>
public record InvoiceSummaryRow(
    string InvoiceType, string FormNo, string Sign,
    long StartInvoiceNo, long EndInvoiceNo, long TotalIssued,
    long UsedCount, long DeletedCount, long Remaining,
    string DeletedInvoiceNos, string MST, string TInvoiceName, string NNTFullName, string NNTAddress);

/// <summary>Kết quả tổng hợp: các dòng + số liệu cộng dồn.</summary>
public record InvoiceSummaryResult(
    DateTime From, DateTime To, string Month,
    List<InvoiceSummaryRow> Rows,
    long TotalIssued, long TotalUsed, long TotalDeleted, long TotalRemaining);

public interface IReportService
{
    Task<InvoiceSummaryResult> InvoiceSummaryAsync(DateTime from, DateTime to,
        string? invoiceType = null, string? sign = null, string? formNo = null, string? mst = null);
}

/// <summary>
/// Tổng hợp tình hình sử dụng hóa đơn theo mẫu/ký hiệu trong kỳ.
/// Port từ Rpt_InvoiceSummary_01 (MobileGate) — gộp các bảng tạm K1/K2/K3 thành truy vấn LINQ.
/// </summary>
public class ReportService(AppDbContext db) : IReportService
{
    public async Task<InvoiceSummaryResult> InvoiceSummaryAsync(DateTime from, DateTime to,
        string? invoiceType = null, string? sign = null, string? formNo = null, string? mst = null)
    {
        var dFrom = from.Date;
        var dTo = to.Date;

        // Mẫu hóa đơn phát hành trong kỳ (lọc theo loại/ký hiệu/mẫu số/MST nếu có).
        var templates = await db.InvoiceTemplates
            .Where(t => t.FlagActive && t.EffDateStart >= dFrom && t.EffDateStart <= dTo)
            .Where(t => string.IsNullOrEmpty(invoiceType) || t.InvoiceType == invoiceType)
            .Where(t => string.IsNullOrEmpty(sign) || t.Sign == sign)
            .Where(t => string.IsNullOrEmpty(formNo) || t.FormNo == formNo)
            .Where(t => string.IsNullOrEmpty(mst) || t.MST == mst)
            .OrderBy(t => t.InvoiceType).ThenBy(t => t.Sign)
            .ToListAsync();

        var codes = templates.Select(t => t.TInvoiceCode).ToList();
        var invoices = await db.Invoices
            .Where(i => codes.Contains(i.TInvoiceCode) && i.InvoiceNo != null)
            .ToListAsync();

        var rows = new List<InvoiceSummaryRow>();
        foreach (var t in templates)
        {
            var mine = invoices.Where(i => i.TInvoiceCode == t.TInvoiceCode).ToList();
            // Số đã dùng: hóa đơn ISSUED/CANCELED trong kỳ (theo ngày lập).
            var used = mine.Where(i => i.InvoiceDate >= dFrom && i.InvoiceDate <= dTo
                        && (i.InvoiceStatus == "ISSUED" || i.InvoiceStatus == "CANCELED")).ToList();
            // Số bị xóa: DELETED trong kỳ.
            var deleted = mine.Where(i => i.InvoiceDate >= dFrom && i.InvoiceDate <= dTo
                        && i.InvoiceStatus == "DELETED").ToList();

            var totalIssued = t.EndInvoiceNo - t.StartInvoiceNo + 1;
            var usedCount = used.Count;
            var deletedCount = deleted.Count;
            var remaining = totalIssued - usedCount - deletedCount;

            rows.Add(new InvoiceSummaryRow(
                t.InvoiceType, t.FormNo, t.Sign,
                t.StartInvoiceNo, t.EndInvoiceNo, totalIssued,
                usedCount, deletedCount, remaining,
                string.Join(", ", deleted.Select(i => i.InvoiceNo).OrderBy(n => n)),
                t.MST, t.TInvoiceName, t.MST, ""));
        }

        return new InvoiceSummaryResult(dFrom, dTo, dFrom.ToString("MM/yyyy"), rows,
            rows.Sum(r => r.TotalIssued), rows.Sum(r => r.UsedCount),
            rows.Sum(r => r.DeletedCount), rows.Sum(r => r.Remaining));
    }
}
