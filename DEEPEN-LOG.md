# DEEPEN-LOG

- 2026-XX-XX: Port nghiệp vụ **Báo cáo tình hình sử dụng hóa đơn (BC26/AC)** từ MobileGate (`Rpt_InvoiceSummary_01` / `RptSvRptInvoiceSummary01Controller`) sang MiniGate. Thêm entity `InvoiceTemplate` + `Invoice`, service `IReportService.InvoiceSummaryAsync` (gộp các bảng tạm K1/K2/K3 thành LINQ: tổng phát hành, đã dùng, đã xóa, còn lại theo mẫu/ký hiệu/kỳ), `ReportController` + view `/Report`, seed dữ liệu mẫu, nav link. Build Release 0 error; smoke test `/Report` = 200, có dữ liệu.
