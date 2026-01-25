using HexagonalMapping.Strategy4.WeakenedBoundary.Domain;
using Microsoft.EntityFrameworkCore;

namespace HexagonalMapping.Strategy4.WeakenedBoundary.Adapters.Persistence;

/// <summary>
/// DbContext: Domain 엔티티를 직접 사용합니다.
/// 어노테이션이 이미 Domain에 있으므로 추가 설정이 거의 필요 없습니다.
///
/// 이것이 이 방식의 "매력"이지만, 동시에 함정입니다.
/// </summary>
public class ProductDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();

    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Domain 엔티티에 이미 어노테이션이 있으므로
        // 추가 설정이 거의 필요 없습니다.
        // 이것이 이 방식의 "간편함"이지만...

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}
