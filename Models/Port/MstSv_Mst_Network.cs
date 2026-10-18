using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniGate.Data;

namespace MiniGate.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2019.9.MobileGate — MstSv_Mst_Network (hạng mục #108 EQRController/WA_MstSv_Mst_Network_GetMinVersion).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model nguồn: idN.MobileGate.Common/Models/MstSv_Mst_Network.cs:9-36
//   - Query nguồn: EQR.cs:13758-13766 (select msmnw.MinVersion from MstSv_Mst_Network msmnw where msmnw.NetworkID = @strNetworkID)
//   - Biz LIVE: EQR.cs:13610-13705 (EQR_WAS_MstSv_Mst_Network_GetMinVersion), EQR.cs:13707-13828 (EQR_MstSv_Mst_Network_GetMinVersion)
// ===========================================================================================

/// <summary>
/// Thực thể bảng MstSv_Mst_Network — thông tin cấu hình mạng của Master Server.
/// Port 1:1 từ model MstSv_Mst_Network.cs và truy vấn tại EQR.cs:13758-13766.
/// </summary>
public class MstSv_Mst_Network
{
    public string NetworkID { get; set; } = "";             // Khóa chính Varchar(50) (EQR.cs:13762)
    public string? NetworkName { get; set; }                // Tên mạng Varchar(200)
    public string? GroupNetworkID { get; set; }             // Nhóm mạng Varchar(50)
    public string? CoreAddr { get; set; }                   // Địa chỉ Core Varchar(200)
    public string? PingAddr { get; set; }                   // Địa chỉ Ping Varchar(200)
    public string? XSysAddr { get; set; }                   // Địa chỉ XSys Varchar(200)
    public string? WSUrlAddr { get; set; }                  // Địa chỉ WebService Varchar(200)
    public string? DBUrlAddr { get; set; }                  // Địa chỉ Database Varchar(200)
    public string? FlagActive { get; set; }                 // Trạng thái hoạt động Varchar(1)
    public string? MinVersion { get; set; }                 // Phiên bản tối thiểu Varchar(50) (EQR.cs:13760)
    public string? LogLUDTimeUTC { get; set; }              // Thời gian cập nhật Varchar(50)
    public string? LogLUBy { get; set; }                    // Người cập nhật Varchar(50)
    public string? MST { get; set; }                        // Mã số thuế Varchar(50)
}

/// <summary>Cấu hình EF Core cho thực thể <see cref="MstSv_Mst_Network"/>.</summary>
public class MstSv_Mst_NetworkConfig : IEntityTypeConfiguration<MstSv_Mst_Network>
{
    public void Configure(EntityTypeBuilder<MstSv_Mst_Network> b)
    {
        b.ToTable("MstSv_Mst_Network");
        b.HasKey(x => x.NetworkID);
        b.Property(x => x.NetworkID).HasMaxLength(50).IsRequired();
        b.Property(x => x.NetworkName).HasMaxLength(200);
        b.Property(x => x.GroupNetworkID).HasMaxLength(50);
        b.Property(x => x.CoreAddr).HasMaxLength(200);
        b.Property(x => x.PingAddr).HasMaxLength(200);
        b.Property(x => x.XSysAddr).HasMaxLength(200);
        b.Property(x => x.WSUrlAddr).HasMaxLength(200);
        b.Property(x => x.DBUrlAddr).HasMaxLength(200);
        b.Property(x => x.FlagActive).HasMaxLength(1);
        b.Property(x => x.MinVersion).HasMaxLength(50);
        b.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
        b.Property(x => x.MST).HasMaxLength(50);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho bảng MstSv_Mst_Network (#108).
/// Order => 55 (danh mục Mst_* dùng 10–99). Idempotent.
/// </summary>
public class MstSv_Mst_NetworkSeeder : IPortSeeder
{
    public int Order => 55;   // Thứ tự seeder danh mục Mst_* (10–99, chưa dùng)

    public async Task SeedAsync(AppDbContext db)
    {
        // Idempotent: nếu đã có dữ liệu thì bỏ qua
        if (await db.Set<MstSv_Mst_Network>().AnyAsync()) return;

        db.Set<MstSv_Mst_Network>().AddRange(
            new MstSv_Mst_Network
            {
                NetworkID = "NET_TEST_01",
                NetworkName = "Mạng thử nghiệm 01",
                MinVersion = "1.0.0",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            },
            new MstSv_Mst_Network
            {
                NetworkID = "NET_TEST_02",
                NetworkName = "Mạng thử nghiệm 02",
                MinVersion = "2.3.4",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            }
        );

        await db.SaveChangesAsync();
    }
}
