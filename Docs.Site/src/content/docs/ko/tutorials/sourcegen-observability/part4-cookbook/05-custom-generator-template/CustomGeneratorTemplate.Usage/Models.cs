using CustomGeneratorTemplate.Generated;

namespace CustomGeneratorTemplate.Usage;

[AutoMapper(typeof(UserDto))]
public partial class UserEntity
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
}

public class UserDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
}
