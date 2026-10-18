using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using MiniGate.Controllers.Port;
using Xunit;

namespace MiniGate.Tests;

public class CommonGetDTimeTests
{
    [Fact]
    public void GetDTime_ReturnsUtcWith6FractionDigits_InFirstSysInfoRemark()
    {
        var c = new CommonControllerPort { ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() } };
        var r = Assert.IsType<OkObjectResult>(c.WA_Common_GetDTime(null));
        var resp = Assert.IsType<CommonGetDTimeResp>(r.Value);
        Assert.True(resp.Success);
        var info = Assert.Single(resp.Data!.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
        Assert.Matches(new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{6}$"), info.Remark);
        Assert.Equal("0", info.ErrorCode);
    }
}
