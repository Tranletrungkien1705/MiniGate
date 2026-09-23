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

/// <summary>
/// Một dòng của báo cáo tình hình sử dụng hóa đơn (BC26/AC) theo khối K1/K2/K3.
/// K1 = tồn đầu kỳ + phát hành trong kỳ; K2 = sử dụng/xóa bỏ trong kỳ; K3 = tồn cuối kỳ.
/// </summary>
public record InvoiceUsageRow(
    string TInvoiceCode, string InvoiceType, string InvoiceTypeName, string FormNo, string Sign,
    long K1_TongSo,
    long? K1_BeginPeriod_Start, long? K1_BeginPeriod_End,
    long? K1_InPeriod_Start, long? K1_InPeriod_End,
    long? K2_TongSo_Start, long? K2_TongSo_End, long K2_Total, long K2_TotalUsed,
    long K2_TotalDel, string K2_ListInvoiceNoDel,
    long? K3_EndPeriod_Start, long? K3_EndPeriod_End, long K3_EndPeriod_Remain);

/// <summary>Kết quả báo cáo tình hình sử dụng hóa đơn (BC26/AC): các dòng + số liệu cộng dồn.</summary>
public record InvoiceUsageResult(
    DateTime From, DateTime To,
    List<InvoiceUsageRow> Rows,
    long TotalK1, long TotalUsed, long TotalDel, long TotalRemain);

/// <summary>
/// Một dòng của báo cáo hóa đơn đã sử dụng chi tiết theo loại/ký hiệu/mẫu số (Rpt_InvoiceInvoice_ResultUsed).
/// K1 = tồn đầu kỳ + phát hành trong kỳ; K2 = sử dụng/xóa bỏ/hủy trong kỳ; K3 = tồn cuối kỳ.
/// </summary>
public record InvoiceResultUsedRow(
    string TInvoiceCode, string InvoiceType, string InvoiceTypeName, string FormNo, string Sign,
    long K1_TongSo,
    string K1_BeginPeriod_Start, string K1_BeginPeriod_End,
    string K1_InPeriod_Start, string K1_InPeriod_End,
    string K2_TongSo_Start, string K2_TongSo_End, long K2_Total, long K2_TotalUsed,
    long K2_TotalDel, string K2_ListInvoiceNoDel,
    string K3_EndPeriod_Start, string K3_EndPeriod_End, long K3_EndPeriod_Remain);

/// <summary>Kết quả báo cáo hóa đơn đã sử dụng chi tiết: các dòng + số liệu cộng dồn + phạm vi MST được xem.</summary>
public record InvoiceResultUsedResult(
    DateTime From, DateTime To, string UserCode, bool IsSysAdmin, List<string> AllowedMsts,
    List<InvoiceResultUsedRow> Rows,
    long TotalK1, long TotalUsed, long TotalDel, long TotalRemain);

/// <summary>
/// Một dòng của danh sách người dùng hệ thống (RptSv_Sys_User) — kèm danh sách nhóm đã tham gia.
/// </summary>
public record SysUserRow(
    int Idx, string UserCode, string UserNick, string UserName, string BankCode,
    bool FlagSysAdmin, bool FlagDLAdmin, bool FlagActive, string MST,
    List<string> Groups);

/// <summary>Kết quả danh sách người dùng: trang hiện tại + tổng số bản ghi khớp bộ lọc.</summary>
public record SysUserListResult(
    int RecordStart, int RecordCount, long TotalCount,
    List<SysUserRow> Rows);

/// <summary>
/// Một dòng của danh sách nhóm quyền (RptSv_Sys_Group) — kèm danh sách thành viên đã tham gia.
/// </summary>
public record SysGroupRow(
    int Idx, string GroupCode, string GroupName, string MST, bool FlagActive,
    List<SysGroupMemberRow> Members);

/// <summary>Một thành viên trong nhóm (RptSv_Sys_UserInGroup join RptSv_Sys_User).</summary>
public record SysGroupMemberRow(
    string UserCode, string UserName, string BankCode, bool FlagSysAdmin, bool FlagActive);

/// <summary>Kết quả danh sách nhóm quyền: trang hiện tại + tổng số bản ghi khớp bộ lọc.</summary>
public record SysGroupListResult(
    int RecordStart, int RecordCount, long TotalCount,
    List<SysGroupRow> Rows);

/// <summary>
/// Một dòng của danh sách nhóm mẫu hóa đơn (Invoice_TempGroup) — kèm danh sách trường tùy biến.
/// </summary>
public record InvoiceTempGroupRow(
    int Idx, string InvoiceTGroupCode, string InvoiceTGroupName, string MST,
    string InvoiceTGroupBody, string FilePathThumbnail, string Spec_Prd_Type, bool FlagActive,
    List<InvoiceTempGroupFieldRow> Fields);

/// <summary>Một trường tùy biến của nhóm mẫu (Invoice_TempGroupField join Invoice_CustomField/Invoice_DtlCustomField).</summary>
public record InvoiceTempGroupFieldRow(
    string DBFieldName, string TCFType, string NetworkId, bool FlagActive,
    string InvoiceCustomFieldName, string InvoiceDtlCustomFieldName);

/// <summary>Kết quả danh sách nhóm mẫu: trang hiện tại + tổng số bản ghi khớp bộ lọc.</summary>
public record InvoiceTempGroupListResult(
    int RecordStart, int RecordCount, long TotalCount,
    List<InvoiceTempGroupRow> Rows);

/// <summary>
/// Một dòng của báo cáo hoa hồng đơn hàng license (RptSv_InosLicOrder_Commission) —
/// thông tin đơn hàng + giá trị chiết khấu + hoa hồng theo từng vai trò.
/// </summary>
public record LicOrderCommissionRow(
    int Idx, long OrderId, string OrgName, string DiscountCode,
    decimal TotalCost, decimal Price, decimal DiscountVal,
    string PaymentCode, string PaymentStatusDesc, string OrderStatus, string PaymentStatus,
    DateTime CreateDTime, DateTime? ApproveDTime, string CreateUserId, string Remark,
    long InosOrgId, long InosNetworkId, string MST, string DLCode,
    string CommissionStatus, string CommissionRemark,
    string Presenter1, string Presenter2, string Telesale, string Consultants, string Implementer,
    decimal CommissionPresenter1, decimal CommissionPresenter2, decimal CommissionTelesale,
    decimal CommissionConsultants, decimal CommissionImplementer, decimal CommissionTotal);

/// <summary>Kết quả báo cáo hoa hồng đơn hàng license: các dòng + số liệu cộng dồn.</summary>
public record LicOrderCommissionResult(
    DateTime From, DateTime To, string? DLCode, string? MST, string? CommissionStatus,
    List<LicOrderCommissionRow> Rows,
    long TotalOrders, decimal TotalPrice, decimal TotalDiscount, decimal TotalCommission);

/// <summary>
/// Một dòng của danh sách cấu hình chiết khấu đại lý (Map_DealerDiscount) —
/// ánh xạ mã đại lý ↔ mã chiết khấu kèm trạng thái và thông tin cập nhật cuối.
/// </summary>
public record MapDealerDiscountRow(
    int Idx, string DLCode, string DiscountCode, bool FlagActive,
    DateTime LogLUDTimeUTC, string LogLUBy);

/// <summary>Kết quả danh sách cấu hình chiết khấu đại lý: trang hiện tại + tổng số bản ghi khớp bộ lọc.</summary>
public record MapDealerDiscountListResult(
    int RecordStart, int RecordCount, long TotalCount,
    List<MapDealerDiscountRow> Rows);

public interface IReportService
{
    Task<InvoiceSummaryResult> InvoiceSummaryAsync(DateTime from, DateTime to,
        string? invoiceType = null, string? sign = null, string? formNo = null, string? mst = null);

    Task<InvoiceDashboardResult> InvoiceDashboardAsync(DateTime from, DateTime to);

    Task<InvoiceUsageResult> InvoiceUsageAsync(DateTime from, DateTime to,
        string? invoiceType = null, string? sign = null, string? formNo = null);

    Task<InvoiceResultUsedResult> InvoiceResultUsedAsync(DateTime from, DateTime to,
        string? invoiceType = null, string? sign = null, string? formNo = null,
        string? mst = null, string? userCode = null);

    Task<SysUserListResult> SysUserListAsync(int recordStart = 0, int recordCount = 50,
        string? userCode = null, string? groupCode = null, bool? flagActive = null);

    Task<SysGroupListResult> SysGroupListAsync(int recordStart = 0, int recordCount = 50,
        string? groupCode = null, string? userCode = null, bool? flagActive = null);

    Task<InvoiceTempGroupListResult> InvoiceTempGroupListAsync(int recordStart = 0, int recordCount = 50,
        string? invoiceTGroupCode = null, string? mst = null, bool? flagActive = null);

    Task<LicOrderCommissionResult> LicOrderCommissionAsync(DateTime from, DateTime to,
        string? dlCode = null, string? mst = null, string? commissionStatus = null);

    Task<MapDealerDiscountListResult> MapDealerDiscountListAsync(int recordStart = 0, int recordCount = 50,
        string? dlCode = null, string? discountCode = null, bool? flagActive = null);
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

    /// <summary>
    /// Báo cáo tình hình sử dụng hóa đơn (BC26/AC) theo khối K1/K2/K3.
    /// Port từ Rpt_Invoice_ResultUsed (MobileGate) — gộp các bảng tạm #tbl_K1/#tbl_K2/#tbl_K3 thành LINQ.
    ///   K1: tồn đầu kỳ (số HĐ có ngày lập &lt; đầu kỳ) + phát hành trong kỳ (mẫu có EffDateStart trong kỳ).
    ///   K2: số sử dụng (ISSUED trong kỳ), số xóa bỏ (DELETED/CANCELED trong kỳ) + danh sách số HĐ xóa.
    ///   K3: tồn cuối kỳ = từ số → đến số còn lại sau khi trừ số đã dùng/xóa.
    /// </summary>
    public async Task<InvoiceUsageResult> InvoiceUsageAsync(DateTime from, DateTime to,
        string? invoiceType = null, string? sign = null, string? formNo = null)
    {
        var dFrom = from.Date;
        var dTo = to.Date;

        // B1: các mẫu hóa đơn đang hoạt động, có ngày hiệu lực <= cuối kỳ (lọc theo loại/ký hiệu/mẫu số).
        var templates = await db.InvoiceTemplates
            .Where(t => t.FlagActive && t.EffDateStart <= dTo)
            .Where(t => string.IsNullOrEmpty(invoiceType) || t.InvoiceType == invoiceType)
            .Where(t => string.IsNullOrEmpty(sign) || t.Sign == sign)
            .Where(t => string.IsNullOrEmpty(formNo) || t.FormNo == formNo)
            .OrderBy(t => t.InvoiceType).ThenBy(t => t.Sign)
            .ToListAsync();

        var codes = templates.Select(t => t.TInvoiceCode).ToList();
        var invoices = await db.Invoices
            .Where(i => codes.Contains(i.TInvoiceCode) && i.InvoiceNo != null)
            .ToListAsync();

        var rows = new List<InvoiceUsageRow>();
        foreach (var t in templates)
        {
            var mine = invoices.Where(i => i.TInvoiceCode == t.TInvoiceCode).ToList();

            // ── K1: tồn đầu kỳ + phát hành trong kỳ ──
            // Số HĐ đã lập trước đầu kỳ (dùng để tính tồn đầu kỳ).
            var beforePeriod = mine.Where(i => i.InvoiceDate.Date < dFrom).ToList();
            long? k1BeginStart = null, k1BeginEnd = null;
            if (t.EffDateStart.Date < dFrom)
            {
                // Mẫu đã phát hành trước kỳ → tồn đầu kỳ = (max số HĐ trước kỳ + 1) → đến số của mẫu.
                var maxBefore = beforePeriod.Any() ? beforePeriod.Max(i => i.InvoiceNo!.Value) : (long?)null;
                k1BeginStart = maxBefore.HasValue ? maxBefore.Value + 1 : t.StartInvoiceNo;
                k1BeginEnd = t.EndInvoiceNo;
            }
            // Phát hành trong kỳ: mẫu có ngày hiệu lực nằm trong kỳ.
            long? k1InStart = null, k1InEnd = null;
            if (t.EffDateStart.Date >= dFrom && t.EffDateStart.Date <= dTo)
            {
                k1InStart = t.StartInvoiceNo;
                k1InEnd = t.EndInvoiceNo;
            }
            // Tổng số (5) = A + B (A = phát hành trong kỳ, B = tồn đầu kỳ).
            long a = (k1InStart.HasValue && k1InEnd.HasValue) ? k1InEnd.Value - k1InStart.Value + 1 : 0;
            long b = (k1BeginStart.HasValue && k1BeginEnd.HasValue) ? k1BeginEnd.Value - k1BeginStart.Value + 1 : 0;
            var k1TongSo = a + b;

            // ── K2: sử dụng / xóa bỏ trong kỳ ──
            var inPeriod = mine.Where(i => i.InvoiceDate.Date >= dFrom && i.InvoiceDate.Date <= dTo
                        && (i.InvoiceStatus == "ISSUED" || i.InvoiceStatus == "DELETED" || i.InvoiceStatus == "CANCELED")).ToList();
            long? k2Start = inPeriod.Any() ? inPeriod.Min(i => i.InvoiceNo!.Value) : null;
            long? k2End = inPeriod.Any() ? inPeriod.Max(i => i.InvoiceNo!.Value) : null;
            var k2Total = (k2Start.HasValue && k2End.HasValue) ? k2End.Value - k2Start.Value + 1 : 0;
            var k2Used = inPeriod.Count(i => i.InvoiceStatus == "ISSUED");
            var deleted = inPeriod.Where(i => i.InvoiceStatus == "DELETED" || i.InvoiceStatus == "CANCELED")
                        .OrderBy(i => i.InvoiceNo).ToList();
            var k2Del = deleted.Count;
            var k2ListDel = string.Join(",", deleted.Select(i => i.InvoiceNo));

            // ── K3: tồn cuối kỳ ──
            // Từ số cuối kỳ = max số HĐ đã lập (mọi kỳ) + 1; nếu chưa lập gì thì lấy từ số của mẫu.
            var maxAll = mine.Any() ? mine.Max(i => i.InvoiceNo!.Value) : (long?)null;
            long? k3Start = maxAll.HasValue ? maxAll.Value + 1 : t.StartInvoiceNo;
            long? k3End = t.EndInvoiceNo;
            var k3Remain = (k3Start.HasValue && k3End.HasValue && k3End.Value >= k3Start.Value)
                ? k3End.Value - k3Start.Value + 1 : 0;

            rows.Add(new InvoiceUsageRow(
                t.TInvoiceCode, t.InvoiceType, t.InvoiceType, t.FormNo, t.Sign,
                k1TongSo,
                k1BeginStart, k1BeginEnd, k1InStart, k1InEnd,
                k2Start, k2End, k2Total, k2Used, k2Del, k2ListDel,
                k3Start, k3End, k3Remain));
        }

        return new InvoiceUsageResult(dFrom, dTo, rows,
            rows.Sum(r => r.K1_TongSo), rows.Sum(r => r.K2_TotalUsed),
            rows.Sum(r => r.K2_TotalDel), rows.Sum(r => r.K3_EndPeriod_Remain));
    }

    /// <summary>
    /// Báo cáo hóa đơn đã sử dụng chi tiết theo loại/ký hiệu/mẫu số (Rpt_InvoiceInvoice_ResultUsed).
    /// Port từ Rpt_InvoiceInvoice_ResultUsedX_New20200131 (MobileGate) — gộp các bảng tạm #tbl_K1/#tbl_K2/#tbl_K3 thành LINQ.
    ///   K1: tồn đầu kỳ (số HĐ có ngày lập &lt; đầu kỳ) + phát hành trong kỳ (mẫu có EffDateStart trong kỳ).
    ///   K2: số sử dụng (ISSUED), số xóa bỏ (DELETED) + danh sách số HĐ xóa, số hủy (CANCELED).
    ///   K3: tồn cuối kỳ = từ số → đến số còn lại sau khi trừ số đã dùng/xóa.
    /// PHÂN QUYỀN THEO MST (Mst_NNT_ViewAbility): nếu user là SysAdmin → xem toàn bộ MST;
    /// ngược lại chỉ xem các MST khớp MSTBUPattern của NNT gắn với user (LIKE).
    /// </summary>
    public async Task<InvoiceResultUsedResult> InvoiceResultUsedAsync(DateTime from, DateTime to,
        string? invoiceType = null, string? sign = null, string? formNo = null,
        string? mst = null, string? userCode = null)
    {
        var dFrom = from.Date;
        var dTo = to.Date;

        // ── Phân quyền theo MST (Mst_NNT_ViewAbility) ──
        // Lấy user hiện hành (mặc định "admin" nếu không chỉ định).
        var user = await db.SysUsers.FirstOrDefaultAsync(u => u.UserCode == (userCode ?? "admin"));
        var isSysAdmin = user?.FlagSysAdmin ?? true;

        // Tập MST được phép xem: SysAdmin → tất cả NNT đang hoạt động; ngược lại → NNT khớp MSTBUPattern của user.
        var nnts = await db.MstNnts.Where(n => n.FlagActive).ToListAsync();
        List<string> allowedMsts;
        if (isSysAdmin)
        {
            allowedMsts = nnts.Select(n => n.MST).ToList();
        }
        else
        {
            // MSTBUPattern là mẫu LIKE (vd "ALL.0101234567%") — so khớp tiền tố trước '%'.
            var pattern = nnts.FirstOrDefault(n => n.MST == user!.MST)?.MSTBUPattern ?? "";
            var prefix = pattern.Contains('%') ? pattern[..pattern.IndexOf('%')] : pattern;
            allowedMsts = nnts.Where(n => n.MSTBUPattern.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                                          || n.MST == user!.MST)
                              .Select(n => n.MST).ToList();
        }

        // B1: các mẫu hóa đơn đang hoạt động, có ngày hiệu lực <= cuối kỳ (lọc theo loại/ký hiệu/mẫu số/MST).
        var templates = await db.InvoiceTemplates
            .Where(t => t.FlagActive && t.EffDateStart <= dTo)
            .Where(t => allowedMsts.Contains(t.MST))
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

        // Tên loại hóa đơn (Mst_InvoiceType.InvoiceTypeName) — suy ra từ dữ liệu mẫu.
        var typeNames = templates.GroupBy(t => t.InvoiceType)
            .ToDictionary(g => g.Key, g => g.First().TInvoiceName);

        var rows = new List<InvoiceResultUsedRow>();
        foreach (var t in templates)
        {
            var mine = invoices.Where(i => i.TInvoiceCode == t.TInvoiceCode).ToList();

            // ── K1: tồn đầu kỳ + phát hành trong kỳ ──
            var beforePeriod = mine.Where(i => i.InvoiceDate.Date < dFrom).ToList();
            long? k1BeginStart = null, k1BeginEnd = null;
            if (t.EffDateStart.Date < dFrom)
            {
                var maxBefore = beforePeriod.Any() ? beforePeriod.Max(i => i.InvoiceNo!.Value) : (long?)null;
                k1BeginStart = maxBefore.HasValue ? maxBefore.Value + 1 : t.StartInvoiceNo;
                k1BeginEnd = t.EndInvoiceNo;
            }
            long? k1InStart = null, k1InEnd = null;
            if (t.EffDateStart.Date >= dFrom && t.EffDateStart.Date <= dTo)
            {
                k1InStart = t.StartInvoiceNo;
                k1InEnd = t.EndInvoiceNo;
            }
            long a = (k1InStart.HasValue && k1InEnd.HasValue) ? k1InEnd.Value - k1InStart.Value + 1 : 0;
            long b = (k1BeginStart.HasValue && k1BeginEnd.HasValue) ? k1BeginEnd.Value - k1BeginStart.Value + 1 : 0;
            var k1TongSo = a + b;

            // ── K2: sử dụng / xóa bỏ / hủy trong kỳ ──
            var inPeriod = mine.Where(i => i.InvoiceDate.Date >= dFrom && i.InvoiceDate.Date <= dTo
                        && (i.InvoiceStatus == "ISSUED" || i.InvoiceStatus == "DELETED" || i.InvoiceStatus == "CANCELED")).ToList();
            long? k2Start = inPeriod.Any() ? inPeriod.Min(i => i.InvoiceNo!.Value) : null;
            long? k2End = inPeriod.Any() ? inPeriod.Max(i => i.InvoiceNo!.Value) : null;
            var k2Total = (k2Start.HasValue && k2End.HasValue) ? k2End.Value - k2Start.Value + 1 : 0;
            var k2Used = inPeriod.Count(i => i.InvoiceStatus == "ISSUED");
            var deleted = inPeriod.Where(i => i.InvoiceStatus == "DELETED" || i.InvoiceStatus == "CANCELED")
                        .OrderBy(i => i.InvoiceNo).ToList();
            var k2Del = deleted.Count;
            var k2ListDel = string.Join(",", deleted.Select(i => i.InvoiceNo));

            // ── K3: tồn cuối kỳ ──
            var maxAll = mine.Any() ? mine.Max(i => i.InvoiceNo!.Value) : (long?)null;
            long? k3Start = maxAll.HasValue ? maxAll.Value + 1 : t.StartInvoiceNo;
            long? k3End = t.EndInvoiceNo;
            var k3Remain = (k3Start.HasValue && k3End.HasValue && k3End.Value >= k3Start.Value)
                ? k3End.Value - k3Start.Value + 1 : 0;

            // Định dạng số 7 chữ số (REPLICATE('0',7-LEN(...)) trong SQL gốc).
            string Pad(long? v) => v.HasValue ? v.Value.ToString("D7") : "";

            rows.Add(new InvoiceResultUsedRow(
                t.TInvoiceCode, t.InvoiceType, typeNames.GetValueOrDefault(t.InvoiceType, t.InvoiceType),
                t.FormNo, t.Sign,
                k1TongSo,
                Pad(k1BeginStart), Pad(k1BeginEnd), Pad(k1InStart), Pad(k1InEnd),
                Pad(k2Start), Pad(k2End), k2Total, k2Used, k2Del, k2ListDel,
                Pad(k3Start), Pad(k3End), k3Remain));
        }

        return new InvoiceResultUsedResult(dFrom, dTo, user?.UserCode ?? (userCode ?? "admin"), isSysAdmin, allowedMsts,
            rows, rows.Sum(r => r.K1_TongSo), rows.Sum(r => r.K2_TotalUsed),
            rows.Sum(r => r.K2_TotalDel), rows.Sum(r => r.K3_EndPeriod_Remain));
    }

    /// <summary>
    /// Danh sách người dùng hệ thống (RptSv_Sys_User_Get) — endpoint tổng hợp: trả về danh sách user
    /// (có phân trang + lọc theo mã user / nhóm / trạng thái) kèm danh sách nhóm đã tham gia.
    /// Port từ RptSv_Sys_User_Get (MobileGate) — gộp các bảng tạm #tbl_RptSv_Sys_User_Filter_Draft/#tbl_RptSv_Sys_User_Filter
    /// và hai khối select RptSv_Sys_User / RptSv_Sys_UserInGroup thành truy vấn LINQ.
    /// </summary>
    public async Task<SysUserListResult> SysUserListAsync(int recordStart = 0, int recordCount = 50,
        string? userCode = null, string? groupCode = null, bool? flagActive = null)
    {
        if (recordStart < 0) recordStart = 0;
        if (recordCount <= 0) recordCount = 50;

        // B1: lọc user theo mã / trạng thái (tương ứng #tbl_RptSv_Sys_User_Filter_Draft).
        var users = await db.SysUsers
            .Where(u => string.IsNullOrEmpty(userCode) || u.UserCode == userCode)
            .Where(u => flagActive == null || u.FlagActive == flagActive)
            .OrderBy(u => u.UserCode)
            .ToListAsync();

        // B2: lọc theo nhóm (join RptSv_Sys_UserInGroup) nếu có.
        var memberships = await db.SysUserInGroups.ToListAsync();
        if (!string.IsNullOrEmpty(groupCode))
        {
            var inGroup = memberships.Where(m => m.GroupCode == groupCode).Select(m => m.UserCode).ToHashSet();
            users = users.Where(u => inGroup.Contains(u.UserCode)).ToList();
        }

        // B3: phân trang (tương ứng #tbl_RptSv_Sys_User_Filter với MyIdxSeq).
        var total = users.Count;
        var page = users.Skip(recordStart).Take(recordCount).ToList();

        // B4: gắn danh sách nhóm của từng user (tương ứng khối select RptSv_Sys_UserInGroup).
        var groupsByUser = memberships
            .GroupBy(m => m.UserCode)
            .ToDictionary(g => g.Key, g => g.Select(m => m.GroupCode).OrderBy(c => c).ToList());

        var rows = page.Select((u, i) => new SysUserRow(
            recordStart + i + 1, u.UserCode, u.UserNick, u.UserName, u.BankCode,
            u.FlagSysAdmin, u.FlagDLAdmin, u.FlagActive, u.MST,
            groupsByUser.GetValueOrDefault(u.UserCode, new List<string>()))).ToList();

        return new SysUserListResult(recordStart, recordCount, total, rows);
    }

    /// <summary>
    /// Danh sách nhóm quyền (RptSv_Sys_Group_Get) — endpoint tổng hợp: trả về danh sách nhóm
    /// (có phân trang + lọc theo mã nhóm / mã user / trạng thái) kèm danh sách thành viên đã tham gia.
    /// Port từ RptSv_Sys_Group_Get (MobileGate) — gộp các bảng tạm #tbl_RptSv_Sys_Group_Filter_Draft/#tbl_RptSv_Sys_Group_Filter
    /// và hai khối select RptSv_Sys_Group / RptSv_Sys_UserInGroup thành truy vấn LINQ.
    /// </summary>
    public async Task<SysGroupListResult> SysGroupListAsync(int recordStart = 0, int recordCount = 50,
        string? groupCode = null, string? userCode = null, bool? flagActive = null)
    {
        if (recordStart < 0) recordStart = 0;
        if (recordCount <= 0) recordCount = 50;

        // B1: lọc nhóm theo mã / trạng thái (tương ứng #tbl_RptSv_Sys_Group_Filter_Draft).
        var groups = await db.SysGroups
            .Where(g => string.IsNullOrEmpty(groupCode) || g.GroupCode == groupCode)
            .Where(g => flagActive == null || g.FlagActive == flagActive)
            .OrderBy(g => g.GroupCode)
            .ToListAsync();

        // B2: lọc theo thành viên (join RptSv_Sys_UserInGroup) nếu có.
        var memberships = await db.SysUserInGroups.ToListAsync();
        if (!string.IsNullOrEmpty(userCode))
        {
            var groupsOfUser = memberships.Where(m => m.UserCode == userCode).Select(m => m.GroupCode).ToHashSet();
            groups = groups.Where(g => groupsOfUser.Contains(g.GroupCode)).ToList();
        }

        // B3: phân trang (tương ứng #tbl_RptSv_Sys_Group_Filter với MyIdxSeq).
        var total = groups.Count;
        var page = groups.Skip(recordStart).Take(recordCount).ToList();

        // B4: gắn danh sách thành viên của từng nhóm (tương ứng khối select RptSv_Sys_UserInGroup join RptSv_Sys_User).
        var users = await db.SysUsers.ToListAsync();
        var userByCode = users.ToDictionary(u => u.UserCode, u => u);
        var membersByGroup = memberships
            .GroupBy(m => m.GroupCode)
            .ToDictionary(g => g.Key, g => g.Select(m => m.UserCode).OrderBy(c => c).ToList());

        var rows = page.Select((g, i) =>
        {
            var members = membersByGroup.GetValueOrDefault(g.GroupCode, new List<string>())
                .Select(uc =>
                {
                    var u = userByCode.GetValueOrDefault(uc);
                    return new SysGroupMemberRow(uc, u?.UserName ?? "", u?.BankCode ?? "",
                        u?.FlagSysAdmin ?? false, u?.FlagActive ?? false);
                }).ToList();
            return new SysGroupRow(recordStart + i + 1, g.GroupCode, g.GroupName, g.MST, g.FlagActive, members);
        }).ToList();

        return new SysGroupListResult(recordStart, recordCount, total, rows);
    }

    /// <summary>
    /// Danh sách nhóm mẫu hóa đơn (RptSv_Invoice_TempGroup_Get) — endpoint tổng hợp: trả về danh sách
    /// nhóm mẫu (có phân trang + lọc theo mã nhóm / MST / trạng thái) kèm danh sách trường tùy biến.
    /// Port từ RptSv_Invoice_TempGroup_Get (MobileGate) — gộp các bảng tạm #tbl_Invoice_TempGroup_Filter_Draft/#tbl_Invoice_TempGroup_Filter
    /// và hai khối select Invoice_TempGroup / Invoice_TempGroupField thành truy vấn LINQ.
    /// </summary>
    public async Task<InvoiceTempGroupListResult> InvoiceTempGroupListAsync(int recordStart = 0, int recordCount = 50,
        string? invoiceTGroupCode = null, string? mst = null, bool? flagActive = null)
    {
        if (recordStart < 0) recordStart = 0;
        if (recordCount <= 0) recordCount = 50;

        // B1: lọc nhóm mẫu theo mã / MST / trạng thái (tương ứng #tbl_Invoice_TempGroup_Filter_Draft).
        var groups = await db.InvoiceTempGroups
            .Where(g => string.IsNullOrEmpty(invoiceTGroupCode) || g.InvoiceTGroupCode == invoiceTGroupCode)
            .Where(g => string.IsNullOrEmpty(mst) || g.MST == mst)
            .Where(g => flagActive == null || g.FlagActive == flagActive)
            .OrderBy(g => g.InvoiceTGroupCode)
            .ToListAsync();

        // B2: phân trang (tương ứng #tbl_Invoice_TempGroup_Filter với MyIdxSeq).
        var total = groups.Count;
        var page = groups.Skip(recordStart).Take(recordCount).ToList();

        // B3: gắn danh sách trường tùy biến của từng nhóm (tương ứng khối select Invoice_TempGroupField).
        var fields = await db.InvoiceTempGroupFields.ToListAsync();
        var fieldsByGroup = fields
            .GroupBy(f => f.InvoiceTGroupCode)
            .ToDictionary(g => g.Key, g => g.OrderBy(f => f.DBFieldName).ToList());

        var rows = page.Select((g, i) =>
        {
            var flds = fieldsByGroup.GetValueOrDefault(g.InvoiceTGroupCode, new List<InvoiceTempGroupField>())
                .Select(f => new InvoiceTempGroupFieldRow(
                    f.DBFieldName, f.TCFType, f.NetworkId, f.FlagActive,
                    f.DBFieldName, f.DBFieldName)).ToList();
            return new InvoiceTempGroupRow(recordStart + i + 1, g.InvoiceTGroupCode, g.InvoiceTGroupName, g.MST,
                g.InvoiceTGroupBody, g.FilePathThumbnail, g.Spec_Prd_Type, g.FlagActive, flds);
        }).ToList();

        return new InvoiceTempGroupListResult(recordStart, recordCount, total, rows);
    }

    /// <summary>
    /// Báo cáo hoa hồng đơn hàng license (RptSv_InosLicOrder_Commission) — endpoint tổng hợp:
    /// trả về danh sách đơn hàng license kèm giá trị chiết khấu (giá bán - giá vốn) và hoa hồng
    /// theo từng vai trò (trình bày/telesale/tư vấn/triển khai), lọc theo mã đại lý / MST / trạng thái hoa hồng.
    /// Port từ RptSv_InosLicOrder_Commission_Get (MobileGate) — gộp các bảng tạm #Tbl_RptSv_InosLicOrder_Commission
    /// và các join RptSv_InosLicOrder_Commission / MstSv_Inos_Org / Mst_NNT thành truy vấn LINQ.
    /// </summary>
    public async Task<LicOrderCommissionResult> LicOrderCommissionAsync(DateTime from, DateTime to,
        string? dlCode = null, string? mst = null, string? commissionStatus = null)
    {
        var dFrom = from.Date;
        var dTo = to.Date;

        // B1: đơn hàng license trong kỳ (tương ứng #Tbl_RptSv_InosLicOrder_Commission).
        var orders = await db.LicOrders
            .Where(o => o.CreateDTime.Date >= dFrom && o.CreateDTime.Date <= dTo)
            .Where(o => string.IsNullOrEmpty(dlCode) || o.DLCode == dlCode)
            .Where(o => string.IsNullOrEmpty(mst) || o.MST == mst)
            .OrderBy(o => o.CreateDTime).ThenBy(o => o.OrderId)
            .ToListAsync();

        // B2: hoa hồng theo đơn (tương ứng join RptSv_InosLicOrder_Commission).
        var commissions = await db.LicOrderCommissions.ToListAsync();
        var commByOrder = commissions.GroupBy(c => c.OrderId).ToDictionary(g => g.Key, g => g.First());

        var rows = new List<LicOrderCommissionRow>();
        var idx = 0;
        foreach (var o in orders)
        {
            commByOrder.TryGetValue(o.OrderId, out var c);
            var status = c?.CommissionStatus ?? "";
            // Lọc theo trạng thái hoa hồng (tương ứng @strCommissionStatus).
            if (!string.IsNullOrEmpty(commissionStatus) && status != commissionStatus) continue;

            var discountVal = o.Price - o.TotalCost;
            var commTotal = (c?.CommissionPresenter1 ?? 0) + (c?.CommissionPresenter2 ?? 0)
                          + (c?.CommissionTelesale ?? 0) + (c?.CommissionConsultants ?? 0)
                          + (c?.CommissionImplementer ?? 0);

            rows.Add(new LicOrderCommissionRow(
                ++idx, o.OrderId, o.OrgName, o.DiscountCode,
                o.TotalCost, o.Price, discountVal,
                o.PaymentCode, o.PaymentStatusDesc, o.OrderStatus, o.PaymentStatus,
                o.CreateDTime, o.ApproveDTime, o.CreateUserId, o.Remark,
                o.InosOrgId, o.InosNetworkId, o.MST, o.DLCode,
                status, c?.Remark ?? "",
                c?.Presenter1 ?? "", c?.Presenter2 ?? "", c?.Telesale ?? "", c?.Consultants ?? "", c?.Implementer ?? "",
                c?.CommissionPresenter1 ?? 0, c?.CommissionPresenter2 ?? 0, c?.CommissionTelesale ?? 0,
                c?.CommissionConsultants ?? 0, c?.CommissionImplementer ?? 0, commTotal));
        }

        return new LicOrderCommissionResult(dFrom, dTo, dlCode, mst, commissionStatus, rows,
            rows.Count, rows.Sum(r => r.Price), rows.Sum(r => r.DiscountVal), rows.Sum(r => r.CommissionTotal));
    }

    /// <summary>
    /// Danh sách cấu hình chiết khấu đại lý (RptSv_Map_DealerDiscount_Get) — endpoint tổng hợp:
    /// trả về danh sách ánh xạ mã đại lý ↔ mã chiết khấu (có phân trang + lọc theo mã đại lý /
    /// mã chiết khấu / trạng thái).
    /// Port từ RptSv_Map_DealerDiscount_Get (MobileGate) — gộp các bảng tạm
    /// #tbl_Map_DealerDiscount_Filter_Draft/#tbl_Map_DealerDiscount_Filter và khối select
    /// Map_DealerDiscount thành truy vấn LINQ.
    /// </summary>
    public async Task<MapDealerDiscountListResult> MapDealerDiscountListAsync(int recordStart = 0, int recordCount = 50,
        string? dlCode = null, string? discountCode = null, bool? flagActive = null)
    {
        if (recordStart < 0) recordStart = 0;
        if (recordCount <= 0) recordCount = 50;

        // B1: lọc theo mã đại lý / mã chiết khấu / trạng thái (tương ứng #tbl_Map_DealerDiscount_Filter_Draft).
        var items = await db.MapDealerDiscounts
            .Where(m => string.IsNullOrEmpty(dlCode) || m.DLCode == dlCode)
            .Where(m => string.IsNullOrEmpty(discountCode) || m.DiscountCode == discountCode)
            .Where(m => flagActive == null || m.FlagActive == flagActive)
            .OrderBy(m => m.DLCode).ThenBy(m => m.DiscountCode)
            .ToListAsync();

        // B2: phân trang (tương ứng #tbl_Map_DealerDiscount_Filter với MyIdxSeq).
        var total = items.Count;
        var page = items.Skip(recordStart).Take(recordCount).ToList();

        var rows = page.Select((m, i) => new MapDealerDiscountRow(
            recordStart + i + 1, m.DLCode, m.DiscountCode, m.FlagActive,
            m.LogLUDTimeUTC, m.LogLUBy)).ToList();

        return new MapDealerDiscountListResult(recordStart, recordCount, total, rows);
    }
}
