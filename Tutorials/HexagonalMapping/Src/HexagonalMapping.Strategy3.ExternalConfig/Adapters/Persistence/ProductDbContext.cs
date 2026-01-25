using HexagonalMapping.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HexagonalMapping.Strategy3.ExternalConfig.Adapters.Persistence;

/// <summary>
/// External Configuration 방식의 DbContext입니다.
/// Domain 엔티티를 직접 사용하며, 매핑은 Fluent API로 정의합니다.
///
/// 참고: EF Core는 XML 매핑을 지원하지 않으므로,
/// Fluent API가 "외부 설정"의 역할을 대신합니다.
/// NHibernate를 사용하면 실제 XML 매핑이 가능합니다.
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
        // External Configuration: 매핑을 코드로 분리 (Fluent API)
        // 이것이 XML 매핑의 EF Core 대안입니다.
        ConfigureProductMapping(modelBuilder);
    }

    /// <summary>
    /// Domain 엔티티에 대한 ORM 매핑을 외부(여기서는 Fluent API)에서 정의합니다.
    /// Domain Product 클래스는 어떠한 기술 어노테이션도 갖지 않습니다.
    ///
    /// 참고: 실제 관계형 DB 사용 시 ToTable(), HasColumnName() 등
    /// 추가적인 관계형 매핑을 적용할 수 있습니다.
    /// </summary>
    private static void ConfigureProductMapping(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            // Primary Key - ProductId 값 객체 처리
            entity.HasKey("Id");
            entity.Property<ProductId>("Id")
                .HasConversion(
                    id => id.Value,
                    value => ProductId.From(value));

            // Properties
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            // Value Object: Money (Owned Entity)
            entity.OwnsOne(e => e.Price, money =>
            {
                money.Property(m => m.Amount);
                money.Property(m => m.Currency)
                    .HasMaxLength(3);
            });
        });
    }
}
