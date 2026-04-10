using ValidationGenerator.Usage;

var valid = new CreateUserRequest { Name = "Alice", Email = "alice@test.com", Age = 25 };
Console.WriteLine($"Valid: errors={valid.Validate().Length}");

var invalid = new CreateUserRequest { Name = "", Email = "x@x.commmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm", Age = 200 };
foreach (var error in invalid.Validate())
    Console.WriteLine($"  - {error}");
