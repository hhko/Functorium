namespace FirstArchitectureTest.Domains;

public sealed class Employee
{
    public string Name { get; }
    public string Department { get; }

    private Employee(string name, string department)
    {
        Name = name;
        Department = department;
    }

    public static Employee Create(string name, string department)
        => new(name, department);
}
