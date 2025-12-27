using Microsoft.EntityFrameworkCore;

namespace OrmIntegration.Tests.Unit;

/// <summary>
/// OwnsOne 패턴 테스트
///
/// 테스트 목적:
/// 1. 값 객체가 엔티티와 함께 저장되는지 확인
/// 2. 값 객체가 엔티티와 함께 로드되는지 확인
/// 3. 복합 값 객체 매핑 검증
/// </summary>
[Trait("Part4-ORM-Integration", "OwnsOnePatternTests")]
public class OwnsOnePatternTests
{
    private static DbContextOptions<AppDbContext> CreateOptions(string dbName)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
    }

    [Fact]
    public async Task User_SavesAndLoads_WithEmailValueObject()
    {
        // Arrange
        var options = CreateOptions(nameof(User_SavesAndLoads_WithEmailValueObject));
        var userId = Guid.NewGuid();
        var email = Email.CreateFromValidated("user@example.com");

        // Act - Save
        await using (var context = new AppDbContext(options))
        {
            var user = new User
            {
                Id = userId,
                Name = "테스트 사용자",
                Email = email,
                Address = new Address("서울", "강남구", "06234")
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        // Assert - Load
        await using (var context = new AppDbContext(options))
        {
            var loaded = await context.Users.FirstAsync(u => u.Id == userId);
            loaded.Email.Value.ShouldBe("user@example.com");
        }
    }

    [Fact]
    public async Task User_SavesAndLoads_WithAddressValueObject()
    {
        // Arrange
        var options = CreateOptions(nameof(User_SavesAndLoads_WithAddressValueObject));
        var userId = Guid.NewGuid();

        // Act - Save
        await using (var context = new AppDbContext(options))
        {
            var user = new User
            {
                Id = userId,
                Name = "테스트 사용자",
                Email = Email.CreateFromValidated("test@example.com"),
                Address = new Address("서울", "테헤란로 123", "06234")
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        // Assert - Load
        await using (var context = new AppDbContext(options))
        {
            var loaded = await context.Users.FirstAsync(u => u.Id == userId);
            loaded.Address.City.ShouldBe("서울");
            loaded.Address.Street.ShouldBe("테헤란로 123");
            loaded.Address.PostalCode.ShouldBe("06234");
        }
    }
}
