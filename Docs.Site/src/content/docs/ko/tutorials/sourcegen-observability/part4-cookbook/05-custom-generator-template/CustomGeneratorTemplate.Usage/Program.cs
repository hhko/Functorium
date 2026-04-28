using CustomGeneratorTemplate.Usage;

var entity = new UserEntity { Name = "Alice", Email = "alice@example.com", Age = 30 };
var dto = entity.MapToUserDto();
Console.WriteLine($"Mapped: Name={dto.Name}, Email={dto.Email}, Age={dto.Age}");
