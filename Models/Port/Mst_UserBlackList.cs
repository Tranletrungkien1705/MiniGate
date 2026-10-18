using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniGate.Data;

namespace MiniGate.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2019.9.MobileGate — Mst_UserBlackList (#050, #065).
//
// Nguồn tra cứu (chỉ đọc):
//   - DDL: 05.Refs/Migrate/20231115.Mst_UserBlackList_CreateTable/20231115.Mst_UserBlackList_CreateTable.sql:4-23
//   - Model: idN.MobileGate.Common/Models/Mst_UserBlackList.cs:9-23
//   - BIZ CheckDB: zTemp.cs:42607-42664 (Mst_UserBlackList_CheckDB)
//   - BIZ sử dụng: Ecore.cs:678-745 (Inos_CheckUserExist_New20231115), zTemp.cs:42673-42900 (Mst_UserBlackList_Save)
// ===========================================================================================

/// <summary>
/// Thực thể bảng Mst_UserBlackList — danh sách đen người dùng không cho phép đăng nhập/tồn tại.
/// Port 1:1 từ script DDL 20231115.Mst_UserBlackList_CreateTable.sql và model Mst_UserBlackList.cs.
/// </summary>
public class Mst_UserBlackList
{
    public string UserCode { get; set; } = "";             // Varchar(400) NOT NULL PRIMARY KEY (Ecore.cs:721, 736)
    public string? NetworkID { get; set; }                 // Varchar(50)
    public string? ProviderCode { get; set; }              // Varchar(50)
    public string? UserName { get; set; }                  // Varchar(200)
    public string? UserPassword { get; set; }              // Varchar(50)
    public string? PhoneNo { get; set; }                   // Varchar(50)
    public string? EMail { get; set; }                     // Varchar(50)
    public string? UUID { get; set; }                      // Varchar(50)
    public string? FlagActive { get; set; }                // MyStatus (Varchar(1))
    public string? LogLUDTimeUTC { get; set; }             // MyDateTime (Varchar(50))
    public string? LogLUBy { get; set; }                   // Varchar(50)
}

/// <summary>Cấu hình EF Core cho thực thể <see cref="Mst_UserBlackList"/>.</summary>
public class Mst_UserBlackListConfig : IEntityTypeConfiguration<Mst_UserBlackList>
{
    public void Configure(EntityTypeBuilder<Mst_UserBlackList> b)
    {
        b.ToTable("Mst_UserBlackList");
        b.HasKey(x => x.UserCode);
        b.Property(x => x.UserCode).HasMaxLength(400).IsRequired();
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.ProviderCode).HasMaxLength(50);
        b.Property(x => x.UserName).HasMaxLength(200);
        b.Property(x => x.UserPassword).HasMaxLength(50);
        b.Property(x => x.PhoneNo).HasMaxLength(50);
        b.Property(x => x.EMail).HasMaxLength(50);
        b.Property(x => x.UUID).HasMaxLength(50);
        b.Property(x => x.FlagActive).HasMaxLength(1);
        b.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho bảng Mst_UserBlackList (#050).
/// Order => 50 (danh mục Mst_* dùng 10–99). Idempotent.
/// </summary>
public class Mst_UserBlackListSeeder : IPortSeeder
{
    public int Order => 50;   // Thứ tự seeder danh mục Mst_* (10–99)

    public async Task SeedAsync(AppDbContext db)
    {
        // Idempotent: nếu đã có dữ liệu thì bỏ qua
        if (await db.Set<Mst_UserBlackList>().AnyAsync()) return;

        db.Set<Mst_UserBlackList>().AddRange(
            new Mst_UserBlackList
            {
                UserCode = "blocked@idocnet.com",
                NetworkID = "1001",
                UserName = "Người dùng bị khoá 01 (Email)",
                EMail = "blocked@idocnet.com",
                PhoneNo = "0987654321",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            },
            new Mst_UserBlackList
            {
                UserCode = "0912345678",
                NetworkID = "1001",
                UserName = "Người dùng bị khoá 02 (Phone)",
                EMail = "blockedphone@idocnet.com",
                PhoneNo = "0912345678",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            },
            new Mst_UserBlackList
            {
                UserCode = "999999",
                NetworkID = "1001",
                UserName = "Người dùng bị khoá 03 (Numeric ID)",
                EMail = "blockedid@idocnet.com",
                PhoneNo = "0900000003",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            }
        );

        await db.SaveChangesAsync();
    }
}
