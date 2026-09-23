using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniGate.Data;
using MiniGate.Models;
using MiniGate.Services;

namespace MiniGate.Controllers;

public class HomeController(IGateAdminService svc) : Controller
{
    public async Task<IActionResult> Index() { ViewBag.Dash = await svc.DashboardAsync(); return View(); }
}

public class RouteController(IGateAdminService svc) : Controller
{
    public async Task<IActionResult> Index() => View(await svc.RoutesAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id, string name, string prefix, string upstreamBaseUrl, int timeoutSeconds, bool requireAuth)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(upstreamBaseUrl))
        { TempData["Error"] = "Cần tên, prefix và upstream URL."; return RedirectToAction(nameof(Index)); }
        await svc.SaveRouteAsync(new GwRoute { Id = id, Name = name.Trim(), Prefix = prefix, UpstreamBaseUrl = upstreamBaseUrl, TimeoutSeconds = timeoutSeconds <= 0 ? 30 : timeoutSeconds, RequireAuth = requireAuth });
        TempData["Success"] = "Đã lưu tuyến.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id) { await svc.ToggleRouteAsync(id); return RedirectToAction(nameof(Index)); }
}

public class ClientController(IGateAdminService svc) : Controller
{
    public async Task<IActionResult> Index() => View(await svc.ClientsAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, int rateLimit)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Cần tên client."; return RedirectToAction(nameof(Index)); }
        await svc.CreateClientAsync(name, rateLimit);
        TempData["Success"] = "Đã cấp API key cho client.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id) { await svc.ToggleClientAsync(id); return RedirectToAction(nameof(Index)); }
}

public class LogController(IGateAdminService svc) : Controller
{
    public async Task<IActionResult> Index() => View(await svc.LogsAsync(150));
}

public class PlaygroundController(IGateAdminService svc, IHttpClientFactory httpFactory) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Routes = await svc.RoutesAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string prefix, string path, string method, string? apiKey)
    {
        ViewBag.Routes = await svc.RoutesAsync();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl}/gw/{prefix}/{(path ?? "").TrimStart('/')}";
        var http = httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(60);
        try
        {
            var req = new HttpRequestMessage(new HttpMethod(string.IsNullOrWhiteSpace(method) ? "GET" : method), url);
            if (!string.IsNullOrWhiteSpace(apiKey)) req.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var resp = await http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            ViewBag.Result = new { Url = url, Status = (int)resp.StatusCode, Latency = sw.ElapsedMilliseconds, Route = resp.Headers.TryGetValues("X-Gateway-Route", out var v) ? string.Join(",", v) : "", Body = body.Length > 4000 ? body[..4000] + "…" : body };
        }
        catch (Exception ex) { ViewBag.Result = new { Url = url, Status = 0, Latency = 0L, Route = "", Body = "Lỗi: " + ex.Message }; }
        ViewBag.Prefix = prefix; ViewBag.Path = path; ViewBag.Method = method; ViewBag.ApiKey = apiKey;
        return View(nameof(Index));
    }
}

public class ReportController(IReportService svc) : Controller
{
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, string? invoiceType, string? sign, string? formNo, string? mst)
    {
        var f = from ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var t = to ?? DateTime.Now;
        ViewBag.From = f.ToString("yyyy-MM-dd");
        ViewBag.To = t.ToString("yyyy-MM-dd");
        ViewBag.InvoiceType = invoiceType; ViewBag.Sign = sign; ViewBag.FormNo = formNo; ViewBag.MST = mst;
        return View(await svc.InvoiceSummaryAsync(f, t, invoiceType, sign, formNo, mst));
    }

    public async Task<IActionResult> Dashboard(DateTime? from, DateTime? to)
    {
        var f = from ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var t = to ?? DateTime.Now;
        ViewBag.From = f.ToString("yyyy-MM-dd");
        ViewBag.To = t.ToString("yyyy-MM-dd");
        return View(await svc.InvoiceDashboardAsync(f, t));
    }

    public async Task<IActionResult> Usage(DateTime? from, DateTime? to, string? invoiceType, string? sign, string? formNo)
    {
        var f = from ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var t = to ?? DateTime.Now;
        ViewBag.From = f.ToString("yyyy-MM-dd");
        ViewBag.To = t.ToString("yyyy-MM-dd");
        ViewBag.InvoiceType = invoiceType; ViewBag.Sign = sign; ViewBag.FormNo = formNo;
        return View(await svc.InvoiceUsageAsync(f, t, invoiceType, sign, formNo));
    }

    public async Task<IActionResult> ResultUsed(DateTime? from, DateTime? to, string? invoiceType, string? sign, string? formNo, string? mst, string? userCode)
    {
        var f = from ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var t = to ?? DateTime.Now;
        ViewBag.From = f.ToString("yyyy-MM-dd");
        ViewBag.To = t.ToString("yyyy-MM-dd");
        ViewBag.InvoiceType = invoiceType; ViewBag.Sign = sign; ViewBag.FormNo = formNo; ViewBag.MST = mst;
        ViewBag.UserCode = userCode ?? "admin";
        return View(await svc.InvoiceResultUsedAsync(f, t, invoiceType, sign, formNo, mst, userCode));
    }

    public async Task<IActionResult> Users(int? recordStart, int? recordCount, string? userCode, string? groupCode, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.UserCode = userCode; ViewBag.GroupCode = groupCode; ViewBag.FlagActive = flagActive;
        return View(await svc.SysUserListAsync(start, count, userCode, groupCode, active));
    }

    public async Task<IActionResult> Groups(int? recordStart, int? recordCount, string? groupCode, string? userCode, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.GroupCode = groupCode; ViewBag.UserCode = userCode; ViewBag.FlagActive = flagActive;
        return View(await svc.SysGroupListAsync(start, count, groupCode, userCode, active));
    }

    public async Task<IActionResult> TempGroups(int? recordStart, int? recordCount, string? invoiceTGroupCode, string? mst, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.InvoiceTGroupCode = invoiceTGroupCode; ViewBag.MST = mst; ViewBag.FlagActive = flagActive;
        return View(await svc.InvoiceTempGroupListAsync(start, count, invoiceTGroupCode, mst, active));
    }

    public async Task<IActionResult> Commissions(DateTime? from, DateTime? to, string? dlCode, string? mst, string? commissionStatus)
    {
        var f = from ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var t = to ?? DateTime.Now;
        ViewBag.From = f.ToString("yyyy-MM-dd");
        ViewBag.To = t.ToString("yyyy-MM-dd");
        ViewBag.DLCode = dlCode; ViewBag.MST = mst; ViewBag.CommissionStatus = commissionStatus;
        return View(await svc.LicOrderCommissionAsync(f, t, dlCode, mst, commissionStatus));
    }

    public async Task<IActionResult> DealerDiscounts(int? recordStart, int? recordCount, string? dlCode, string? discountCode, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.DLCode = dlCode; ViewBag.DiscountCode = discountCode; ViewBag.FlagActive = flagActive;
        return View(await svc.MapDealerDiscountListAsync(start, count, dlCode, discountCode, active));
    }

    public async Task<IActionResult> Access(int? recordStart, int? recordCount, string? groupCode, string? objectCode)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.GroupCode = groupCode; ViewBag.ObjectCode = objectCode;
        return View(await svc.SysAccessListAsync(start, count, groupCode, objectCode));
    }

    public async Task<IActionResult> Objects(int? recordStart, int? recordCount, string? objectCode, string? serviceCode, string? objectType, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.ObjectCode = objectCode; ViewBag.ServiceCode = serviceCode; ViewBag.ObjectType = objectType; ViewBag.FlagActive = flagActive;
        return View(await svc.SysObjectListAsync(start, count, objectCode, serviceCode, objectType, active));
    }

    public async Task<IActionResult> VatRates(int? recordStart, int? recordCount, string? vatRateCode, string? networkId, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.VATRateCode = vatRateCode; ViewBag.NetworkID = networkId; ViewBag.FlagActive = flagActive;
        return View(await svc.VatRateListAsync(start, count, vatRateCode, networkId, active));
    }

    public async Task<IActionResult> GovIdTypes(int? recordStart, int? recordCount, string? govIdType, string? networkId, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.GovIDType = govIdType; ViewBag.NetworkID = networkId; ViewBag.FlagActive = flagActive;
        return View(await svc.GovIdTypeListAsync(start, count, govIdType, networkId, active));
    }
    public async Task<IActionResult> Provinces(int? recordStart, int? recordCount, string? provinceCode, string? provinceName, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.ProvinceCode = provinceCode; ViewBag.ProvinceName = provinceName; ViewBag.FlagActive = flagActive;
        return View(await svc.ProvinceListAsync(start, count, provinceCode, provinceName, active));
    }

    public async Task<IActionResult> Districts(int? recordStart, int? recordCount, string? provinceCode, string? districtCode, string? districtName, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.ProvinceCode = provinceCode; ViewBag.DistrictCode = districtCode;
        ViewBag.DistrictName = districtName; ViewBag.FlagActive = flagActive;
        return View(await svc.DistrictListAsync(start, count, provinceCode, districtCode, districtName, active));
    }

    public async Task<IActionResult> ObjectInModules(int? recordStart, int? recordCount, string? objectCode, string? moduleCode)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.ObjectCode = objectCode; ViewBag.ModuleCode = moduleCode;
        return View(await svc.SysObjectInModuleListAsync(start, count, objectCode, moduleCode));
    }

    public async Task<IActionResult> Nnts(int? recordStart, int? recordCount, string? mst, string? nntFullName, string? govTaxId, string? provinceCode, string? dlCode, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.MST = mst; ViewBag.NNTFullName = nntFullName; ViewBag.GovTaxID = govTaxId;
        ViewBag.ProvinceCode = provinceCode; ViewBag.DLCode = dlCode; ViewBag.FlagActive = flagActive;
        return View(await svc.MstNntListAsync(start, count, mst, nntFullName, govTaxId, provinceCode, dlCode, active));
    }

    public async Task<IActionResult> Dealers(int? recordStart, int? recordCount, string? dlCode, string? dlName, string? provinceCode, string? dlType, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.DLCode = dlCode; ViewBag.DLName = dlName; ViewBag.ProvinceCode = provinceCode;
        ViewBag.DLType = dlType; ViewBag.FlagActive = flagActive;
        return View(await svc.MstDealerListAsync(start, count, dlCode, dlName, provinceCode, dlType, active));
    }

    public async Task<IActionResult> PaymentMethods(int? recordStart, int? recordCount, string? paymentMethodCode, string? networkId, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.PaymentMethodCode = paymentMethodCode; ViewBag.NetworkID = networkId; ViewBag.FlagActive = flagActive;
        return View(await svc.PaymentMethodListAsync(start, count, paymentMethodCode, networkId, active));
    }

    public async Task<IActionResult> Modules(int? recordStart, int? recordCount, string? moduleCode, string? solutionCode, string? networkId, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.ModuleCode = moduleCode; ViewBag.SolutionCode = solutionCode; ViewBag.NetworkID = networkId; ViewBag.FlagActive = flagActive;
        return View(await svc.SysModuleListAsync(start, count, moduleCode, solutionCode, networkId, active));
    }

    public async Task<IActionResult> InvoiceTypes(int? recordStart, int? recordCount, string? invoiceType, string? networkId, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.InvoiceType = invoiceType; ViewBag.NetworkID = networkId; ViewBag.FlagActive = flagActive;
        return View(await svc.InvoiceTypeListAsync(start, count, invoiceType, networkId, active));
    }

    public async Task<IActionResult> InosOrgs(int? recordStart, int? recordCount, string? mst, string? name, string? bizType, string? bizField, string? orgSize, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.MST = mst; ViewBag.Name = name; ViewBag.BizType = bizType;
        ViewBag.BizField = bizField; ViewBag.OrgSize = orgSize; ViewBag.FlagActive = flagActive;
        return View(await svc.InosOrgListAsync(start, count, mst, name, bizType, bizField, orgSize, active));
    }

    public async Task<IActionResult> Solutions(int? recordStart, int? recordCount, string? solutionCode, string? networkId, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.SolutionCode = solutionCode; ViewBag.NetworkID = networkId; ViewBag.FlagActive = flagActive;
        return View(await svc.SysSolutionListAsync(start, count, solutionCode, networkId, active));
    }

    public async Task<IActionResult> NntTypes(int? recordStart, int? recordCount, string? nntType, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.NNTType = nntType; ViewBag.FlagActive = flagActive;
        return View(await svc.NntTypeListAsync(start, count, nntType, active));
    }

    public async Task<IActionResult> UserSummary(string? userCode)
    {
        ViewBag.UserCode = userCode;
        return View(await svc.SysUserSummaryAsync(userCode));
    }

    public async Task<IActionResult> GovTaxIds(int? recordStart, int? recordCount, string? govTaxId, string? govTaxName, string? networkId, string? provinceCode, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.GovTaxID = govTaxId; ViewBag.GovTaxName = govTaxName;
        ViewBag.NetworkID = networkId; ViewBag.ProvinceCode = provinceCode; ViewBag.FlagActive = flagActive;
        return View(await svc.GovTaxIdListAsync(start, count, govTaxId, govTaxName, networkId, provinceCode, active));
    }

    public async Task<IActionResult> Networks(int? recordStart, int? recordCount, string? networkId, string? networkName, string? groupNetworkId, string? mst, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.NetworkID = networkId; ViewBag.NetworkName = networkName;
        ViewBag.GroupNetworkID = groupNetworkId; ViewBag.MST = mst; ViewBag.FlagActive = flagActive;
        return View(await svc.MstNetworkListAsync(start, count, networkId, networkName, groupNetworkId, mst, active));
    }

    public async Task<IActionResult> InosUsers(int? recordStart, int? recordCount, string? mst, string? email, string? name, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.MST = mst; ViewBag.Email = email; ViewBag.Name = name; ViewBag.FlagActive = flagActive;
        return View(await svc.InosUserListAsync(start, count, mst, email, name, active));
    }

    public async Task<IActionResult> Notifications(int? recordStart, int? recordCount, string? notifyNo, string? notifyType, string? userCode, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.NotifyNo = notifyNo; ViewBag.NotifyType = notifyType; ViewBag.UserCode = userCode; ViewBag.FlagActive = flagActive;
        return View(await svc.NotifyListAsync(start, count, notifyNo, notifyType, userCode, active));
    }

    public async Task<IActionResult> Countries(int? recordStart, int? recordCount, string? countryCode, string? countryName, string? flagActive)
    {
        var start = recordStart ?? 0;
        var count = recordCount ?? 50;
        bool? active = flagActive switch { "1" => true, "0" => false, _ => null };
        ViewBag.RecordStart = start; ViewBag.RecordCount = count;
        ViewBag.CountryCode = countryCode; ViewBag.CountryName = countryName; ViewBag.FlagActive = flagActive;
        return View(await svc.CountryListAsync(start, count, countryCode, countryName, active));
    }
}

public class OrgController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var orgs = await db.Orgs.IgnoreQueryFilters().OrderBy(o => o.CreatedAt).ToListAsync();
        Request.Cookies.TryGetValue(TenantContext.CookieName, out var curKey);
        ViewBag.CurrentKey = curKey ?? TenantContext.DefaultApiKey;
        return View(orgs);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Cần tên tổ chức."; return RedirectToAction(nameof(Index)); }
        var org = new Org { Name = name.Trim(), ApiKey = "gate_" + Guid.NewGuid().ToString("N") };
        db.Orgs.Add(org); await db.SaveChangesAsync();
        SetCookies(org.ApiKey, org.Name);
        TempData["Success"] = $"Đã tạo & chuyển sang \"{org.Name}\".";
        return RedirectToAction("Index", "Home");
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Switch(string apiKey)
    {
        var org = await db.Orgs.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.ApiKey == apiKey);
        if (org == null) { TempData["Error"] = "Không tìm thấy."; return RedirectToAction(nameof(Index)); }
        SetCookies(org.ApiKey, org.Name);
        return RedirectToAction("Index", "Home");
    }
    public IActionResult Reset()
    {
        Response.Cookies.Delete(TenantContext.CookieName); Response.Cookies.Delete("org_name");
        return RedirectToAction("Index", "Home");
    }
    private void SetCookies(string k, string n)
    {
        var o = new CookieOptions { IsEssential = true, Expires = DateTimeOffset.UtcNow.AddDays(30) };
        Response.Cookies.Append(TenantContext.CookieName, k, o); Response.Cookies.Append("org_name", n, o);
    }
}
