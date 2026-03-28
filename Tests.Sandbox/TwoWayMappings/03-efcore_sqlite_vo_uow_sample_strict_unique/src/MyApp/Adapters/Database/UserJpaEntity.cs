using System.ComponentModel.DataAnnotations;

namespace MyApp.Adapters.Database;

public sealed class UserJpaEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(320)]
    public string Email { get; set; } = "";

    [Required, MaxLength(320)]
    public string NormalizedEmail { get; set; } = "";

    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = "";
}
