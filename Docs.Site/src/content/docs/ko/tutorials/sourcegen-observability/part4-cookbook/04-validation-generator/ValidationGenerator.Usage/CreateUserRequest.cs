using ValidationGenerator.Generated;

namespace ValidationGenerator.Usage;

[AutoValidate]
public partial class CreateUserRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = "";

    [Range(0, 150)]
    public int Age { get; set; }
}
