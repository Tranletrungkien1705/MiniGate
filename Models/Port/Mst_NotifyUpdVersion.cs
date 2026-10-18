using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniGate.Data;

namespace MiniGate.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2019.9.MobileGate — Mst_NotifyUpdVersion (hạng mục #174 EQRController/WA_Mst_NotifyUpdVersion_Get).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model nguồn: idN.MobileGate.Common/Models/InBrand/Mst_NotifyUpdVersion.cs:9-24
//   - Query nguồn: EQR.cs:22007-22046, InBrand.cs:24449-24523
//     (left join Mst_NotifyUpdVersion f on t.NotifyNo = f.NotifyNo lấy NotifyContent, LinkUpdUrl)
//   - Biz LIVE: EQR.cs:21864-21950 (EQR_WAS_Mst_NotifyUpdVersion_Get), EQR.cs:21952-22090 (EQR_Mst_NotifyUpdVersion_Get)
// ===========================================================================================

/// <summary>
/// Thực thể bảng Mst_NotifyUpdVersion — cấu hình thông báo cập nhật phiên bản ứng dụng.
/// Vừa là thực thể ánh xạ bảng CSDL Mst_NotifyUpdVersion, vừa chứa các cờ tính toán
/// FlagNewVersion, FlagForcedUpdate, FlagStrongForcedUpdate trả về cho client.
/// </summary>
public class Mst_NotifyUpdVersion
{
    // --- Các cột CSDL bảng Mst_NotifyUpdVersion ---
    [JsonIgnore]
    public string NotifyNo { get; set; } = "";             // Khóa chính: 'FORCEDUPD', 'OPTIONALUPD' (EQR.cs:22021-22024, 22034)

    [JsonIgnore]
    public string? NetworkID { get; set; }                 // Mã mạng Varchar(50)

    [JsonIgnore]
    public string? NotifyName { get; set; }                // Tên thông báo Varchar(200)

    public string? NotifyContent { get; set; }             // Nội dung thông báo (EQR.cs:22032, Mst_NotifyUpdVersion.cs:16)

    public string? LinkUpdUrl { get; set; }                // Đường dẫn cập nhật (EQR.cs:22032, Mst_NotifyUpdVersion.cs:17)

    [JsonIgnore]
    public string? Remark { get; set; }                    // Ghi chú Varchar(400)

    [JsonIgnore]
    public string? FlagActive { get; set; }                 // Trạng thái hoạt động Varchar(1)

    [JsonIgnore]
    public string? LogLUDTimeUTC { get; set; }              // Thời gian cập nhật Varchar(50)

    [JsonIgnore]
    public string? LogLUBy { get; set; }                    // Người cập nhật Varchar(50)

    // --- Các thuộc tính tính toán động (không lưu trong CSDL) ---
    [NotMapped]
    public string? FlagNewVersion { get; set; }            // Cờ có bản mới: "1" hoặc "0" (EQR.cs:22018, 22032)

    [NotMapped]
    public string? FlagForcedUpdate { get; set; }          // Cờ bắt buộc cập nhật: "1" hoặc "0" (EQR.cs:22019, 22032)

    [NotMapped]
    public string? FlagStrongForcedUpdate { get; set; }    // Cờ bắt buộc cập nhật mạnh: "1" hoặc "0" (EQR.cs:22025, 22032)
}

/// <summary>Cấu hình EF Core cho thực thể <see cref="Mst_NotifyUpdVersion"/>.</summary>
public class Mst_NotifyUpdVersionConfig : IEntityTypeConfiguration<Mst_NotifyUpdVersion>
{
    public void Configure(EntityTypeBuilder<Mst_NotifyUpdVersion> b)
    {
        b.ToTable("Mst_NotifyUpdVersion");
        b.HasKey(x => x.NotifyNo);
        b.Property(x => x.NotifyNo).HasMaxLength(50).IsRequired();
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.NotifyName).HasMaxLength(200);
        b.Property(x => x.NotifyContent);
        b.Property(x => x.LinkUpdUrl).HasMaxLength(500);
        b.Property(x => x.Remark).HasMaxLength(400);
        b.Property(x => x.FlagActive).HasMaxLength(1);
        b.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);

        b.Ignore(x => x.FlagNewVersion);
        b.Ignore(x => x.FlagForcedUpdate);
        b.Ignore(x => x.FlagStrongForcedUpdate);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho bảng Mst_NotifyUpdVersion (#174).
/// Order => 56 (danh mục Mst_* dùng 10–99). Idempotent.
/// </summary>
public class Mst_NotifyUpdVersionSeeder : IPortSeeder
{
    public int Order => 56;   // Thứ tự seeder danh mục Mst_* (10–99, chưa dùng)

    public async Task SeedAsync(AppDbContext db)
    {
        // Idempotent: nếu đã có dữ liệu thì bỏ qua
        if (await db.Set<Mst_NotifyUpdVersion>().AnyAsync()) return;

        db.Set<Mst_NotifyUpdVersion>().AddRange(
            new Mst_NotifyUpdVersion
            {
                NotifyNo = "FORCEDUPD",
                NotifyName = "Bắt buộc cập nhật",
                NotifyContent = "Phiên bản ứng dụng đã quá cũ, vui lòng cập nhật lên bản mới nhất.",
                LinkUpdUrl = "https://app.idocnet.com/download/forced",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            },
            new Mst_NotifyUpdVersion
            {
                NotifyNo = "OPTIONALUPD",
                NotifyName = "Tùy chọn cập nhật",
                NotifyContent = "Đã có phiên bản mới, vui lòng cập nhật để trải nghiệm tính năng mới.",
                LinkUpdUrl = "https://app.idocnet.com/download/optional",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            }
        );

        await db.SaveChangesAsync();
    }
}
