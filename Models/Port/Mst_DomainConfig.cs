using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniGate.Data;

namespace MiniGate.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2019.9.MobileGate — Mst_DomainConfig (hạng mục #405 InBrandController/WA_ELTS_eTEM_Search).
//
// Nguồn tra cứu (chỉ đọc):
//   - Query nguồn: ElasticSearch.cs:2215-2228 (WAS_ELTS_eTEM_Search_New20210813):
//       select t.* from Mst_DomainConfig t --//[mylock]
//       where (1=1) and t.FlagActive = '1' and t.DomainValues = @strDomainValues;
//   - Model nguồn: KHÔNG có trong source_excerpt.txt (excerpt chỉ ghi "khong thay dinh nghia").
//     Vì vậy entity dưới đây CHỈ khai 2 cột mà câu SQL nguồn thực sự dùng (FlagActive, DomainValues)
//     + khóa chính kỹ thuật; KHÔNG bịa thêm cột nghiệp vụ nào khác.
// ===========================================================================================

/// <summary>
/// Thực thể bảng Mst_DomainConfig — cấu hình tên miền (ElasticSearch.cs:2215-2228).
/// </summary>
public class Mst_DomainConfig
{
    public string DomainConfigID { get; set; } = "";        // Khóa chính kỹ thuật (DDL nguồn không có trong excerpt)
    public string? DomainValues { get; set; }               // Tên miền (ElasticSearch.cs:2221)
    public string? FlagActive { get; set; }                 // Trạng thái hoạt động (ElasticSearch.cs:2220)
}

/// <summary>Cấu hình EF Core cho thực thể <see cref="Mst_DomainConfig"/>.</summary>
public class Mst_DomainConfigConfig : IEntityTypeConfiguration<Mst_DomainConfig>
{
    public void Configure(EntityTypeBuilder<Mst_DomainConfig> b)
    {
        b.ToTable("Mst_DomainConfig");
        b.HasKey(x => x.DomainConfigID);
        b.Property(x => x.DomainConfigID).HasMaxLength(50).IsRequired();
        b.Property(x => x.DomainValues).HasMaxLength(200);
        b.Property(x => x.FlagActive).HasMaxLength(1);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho bảng Mst_DomainConfig (#405).
/// Order => 57 (danh mục Mst_* dùng 10–99; 11, 50, 55, 56 đã dùng). Idempotent.
/// </summary>
public class Mst_DomainConfigSeeder : IPortSeeder
{
    public int Order => 57;   // Thứ tự seeder danh mục Mst_* (10–99, chưa dùng)

    public async Task SeedAsync(AppDbContext db)
    {
        // Idempotent: nếu đã có dữ liệu thì bỏ qua
        if (await db.Set<Mst_DomainConfig>().AnyAsync()) return;

        db.Set<Mst_DomainConfig>().AddRange(
            new Mst_DomainConfig
            {
                DomainConfigID = "DOM_TEST_01",
                DomainValues = "etem.test.vn",
                FlagActive = "1"
            },
            new Mst_DomainConfig
            {
                DomainConfigID = "DOM_TEST_02",
                DomainValues = "etem.inactive.vn",
                FlagActive = "0"
            }
        );

        await db.SaveChangesAsync();
    }
}
