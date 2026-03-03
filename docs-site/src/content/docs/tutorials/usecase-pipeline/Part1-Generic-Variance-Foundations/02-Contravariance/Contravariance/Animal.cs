namespace Contravariance;

public class Animal
{
    public string Name { get; }
    public Animal(string name) => Name = name;
    public override string ToString() => $"Animal({Name})";
}

public class Dog : Animal
{
    public string Breed { get; }
    public Dog(string name, string breed) : base(name) => Breed = breed;
    public override string ToString() => $"Dog({Name}, {Breed})";
}

public class Cat : Animal
{
    public Cat(string name) : base(name) { }
    public override string ToString() => $"Cat({Name})";
}
