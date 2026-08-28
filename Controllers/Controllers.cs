using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniGate.Data;
using MiniGate.Models;
using MiniGate.Services;

namespace MiniGate.Controllers;

public class HomeController : Controller
{
    // SPA React (admin) ở "/". Proxy /gw/{**path} giữ nguyên (đứng trước cả fleet).
    public IActionResult Index() => Redirect("/index.html");
}

public class LegacyController(IGateAdminService svc) : Controller
{
    public async Task<IActionResult> Index() { ViewBag.Dash = await svc.DashboardAsync(); return View("~/Views/Home/Index.cshtml"); }
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
