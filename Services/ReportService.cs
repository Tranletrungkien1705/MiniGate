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

/// <summary>Một dòng của bảng điều khiển hóa đơn: số lượng/doanh số theo loại kỳ + hạn mức.</summary>
public record InvoiceDashboardRow(
    string MST, string ReportType,
    long TotalQtyInvoice, decimal TotalAmountAfterVAT,
    long TotalQty, long TotalQtyIssued, long TotalQtyUsed, long TotalQtyCancel, long QtyRemain);

/// <summary>Hạn mức phát hành theo MST (bảng Invoice_license).</summary>
public record InvoiceLicenseRow(
    string MST, string NetworkId,
    long TotalQty, long TotalQtyIssued, long TotalQtyUsed, long TotalQtyCancel, long QtyRemain);

/// <summary>Kết quả bảng điều khiển: các dòng tổng hợp + hạn mức theo MST.</summary>
public record InvoiceDashboardResult(
    DateTime From, DateTime To,
    List<InvoiceDashboardRow> Rows, List<InvoiceLicenseRow> Licenses);

public interface IReportService
{
    Task<InvoiceSummaryResult> InvoiceSummaryAsync(DateTime from, DateTime to,
        string? invoiceType = null, string? sign = null, string? formNo = null, string? mst = null);

    Task<InvoiceDashboardResult> InvoiceDashboardAsync(DateTime from, DateTime to);
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

    /// <summary>
    /// Bảng điều khiển hóa đơn: đếm số lượng + doanh số sau VAT theo từng loại kỳ
    /// (trong ngày tạo / chờ phát hành / theo ngày / tháng / quý / năm lập hóa đơn),
    /// kèm hạn mức phát hành theo MST.
    /// Port từ Rpt_InvoiceForDashboard (MobileGate) — gộp các bảng tạm thành truy vấn LINQ.
    /// </summary>
    public async Task<InvoiceDashboardResult> InvoiceDashboardAsync(DateTime from, DateTime to)
    {
        var dFrom = from.Date;
        var dTo = to.Date;
        var monthFrom = new DateTime(dFrom.Year, dFrom.Month, 1);
        var monthTo = monthFrom.AddMonths(1).AddDays(-1);
        var quarterStartMonth = ((dFrom.Month - 1) / 3) * 3 + 1;
        var quarterFrom = new DateTime(dFrom.Year, quarterStartMonth, 1);
        var quarterTo = quarterFrom.AddMonths(3).AddDays(-1);
        var yearFrom = new DateTime(dFrom.Year, 1, 1);
        var yearTo = new DateTime(dFrom.Year, 12, 31);

        // Nạp hóa đơn trong phạm vi rộng nhất (năm) để tổng hợp theo từng loại kỳ.
        var invoices = await db.Invoices
            .Where(i => i.InvoiceDate >= yearFrom && i.InvoiceDate <= yearTo)
            .ToListAsync();

        // Các tập hợp mã hóa đơn theo từng loại kỳ (tương ứng các bảng tạm trong SQL gốc).
        var inDayCreate = invoices.Where(i => i.CreatedAt.Date >= dFrom && i.CreatedAt.Date <= dTo).ToList();
        var pending = invoices.Where(i => i.InvoiceStatus == "PENDING").ToList();
        var inDayInvoice = invoices.Where(i => i.InvoiceStatus == "ISSUED" && i.InvoiceDate.Date >= dFrom && i.InvoiceDate.Date <= dTo).ToList();
        var inMonthInvoice = invoices.Where(i => i.InvoiceStatus == "ISSUED" && i.InvoiceDate.Date >= monthFrom && i.InvoiceDate.Date <= monthTo).ToList();
        var inQuarterInvoice = invoices.Where(i => i.InvoiceStatus == "ISSUED" && i.InvoiceDate.Date >= quarterFrom && i.InvoiceDate.Date <= quarterTo).ToList();
        var inYearInvoice = invoices.Where(i => i.InvoiceStatus == "ISSUED" && i.InvoiceDate.Date >= yearFrom && i.InvoiceDate.Date <= yearTo).ToList();

        var rows = new List<InvoiceDashboardRow>();
        void AddGroup(string reportType, List<Invoice> set)
        {
            foreach (var g in set.GroupBy(i => i.MST))
                rows.Add(new InvoiceDashboardRow(g.Key, reportType, g.Count(), g.Sum(i => i.AmountAfterVAT),
                    0, 0, 0, 0, 0));
        }
        AddGroup("InvoiceCreateDate", inDayCreate);
        AddGroup("InvoicePending", pending);
        AddGroup("InvoiceDate", inDayInvoice);
        AddGroup("InvoiceMonth", inMonthInvoice);
        AddGroup("InvoiceQuarter", inQuarterInvoice);
        AddGroup("InvoiceYear", inYearInvoice);

        // Gắn hạn mức (Invoice_license) vào từng dòng theo MST.
        var licenses = await db.InvoiceLicenses.ToListAsync();
        var licByMst = licenses.ToDictionary(l => l.MST, l => l);
        var enriched = rows.Select(r =>
        {
            if (licByMst.TryGetValue(r.MST, out var l))
                return r with { TotalQty = l.TotalQty, TotalQtyIssued = l.TotalQtyIssued, TotalQtyUsed = l.TotalQtyUsed, TotalQtyCancel = l.TotalQtyCancel, QtyRemain = l.TotalQtyIssued - l.TotalQtyUsed - l.TotalQtyCancel };
            return r;
        }).ToList();

        var licenseRows = licenses.Select(l => new InvoiceLicenseRow(
            l.MST, l.NetworkId, l.TotalQty, l.TotalQtyIssued, l.TotalQtyUsed, l.TotalQtyCancel,
            l.TotalQtyIssued - l.TotalQtyUsed - l.TotalQtyCancel)).ToList();

        return new InvoiceDashboardResult(dFrom, dTo, enriched, licenseRows);
    }
}
