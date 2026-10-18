using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniGate.Data;

namespace MiniGate.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2019.9.MobileGate — Common (#011).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model: idN.MobileGate.Common/Models/Common.cs:9-12 (class Common rỗng, 0 thuộc tính)
//   - DDL: Nguồn không có DDL và không có bảng CSDL nào cho Common.
//   - BIZ: Common.cs:755-766 (Cm_GetDTime), Common.cs:1131-1186 (WAS_Cm_GetDTime)
//
// ĐẶC ĐIỂM BẢNG STUB:
//   Nguồn gốc MobileGate không có bảng CSDL nào cho Common (model Common.cs:9-12 rỗng 0 thuộc tính;
//   Cm_GetDTime chỉ sinh DataSet bộ nhớ đệm thời gian DateTime.UtcNow). Tuy nhiên, theo quy ước kiến trúc
//   của port 1:1 (PORT_RULES_MiniGate.md §2.5 và đề bài #011), cần tạo file entity stub Models/Port/Common.cs
//   (có PK Id string) kèm CommonSeeder : IPortSeeder (Order => 11) để đảm bảo cơ chế quét tự động của
//   AppDbContext.ApplyConfigurationsFromAssembly và PortSchema.EnsureAsync hoạt động đồng nhất.
// ===========================================================================================

/// <summary>
/// Entity stub Common — port 1:1 từ MobileGate Common.cs:9-12 (nguồn rỗng 0 thuộc tính, không có bảng CSDL).
/// </summary>
public class Common
{
    public string Id { get; set; } = "";                  // Khóa chính stub (không có DDL nguồn)
    public string? Remark { get; set; }                   // Ghi chú stub
}

/// <summary>Cấu hình EF cho <see cref="Common"/> — bảng stub Common.</summary>
public class CommonConfig : IEntityTypeConfiguration<Common>
{
    public void Configure(EntityTypeBuilder<Common> b)
    {
        b.ToTable("Common");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).IsRequired();
    }
}

/// <summary>
/// Seeder dữ liệu stub cho bảng Common (#011).
/// Order => 11 (danh mục Mst_* / stub dùng 10–99). Idempotent.
/// </summary>
public class CommonSeeder : IPortSeeder
{
    public int Order => 11;   // Thứ tự seeder danh mục/stub (10–99)

    public async Task SeedAsync(AppDbContext db)
    {
        // Idempotent: đã có dòng thì bỏ qua
        if (await db.Set<Common>().AnyAsync()) return;

        db.Set<Common>().Add(new Common
        {
            Id = "1",
            Remark = "Common Stub"
        });
        await db.SaveChangesAsync();
    }
}
