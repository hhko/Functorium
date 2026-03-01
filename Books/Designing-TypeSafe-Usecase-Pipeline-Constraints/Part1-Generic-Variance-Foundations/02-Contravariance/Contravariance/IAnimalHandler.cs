namespace Contravariance;

/// <summary>
/// 반공변 인터페이스 - 동물을 처리(입력으로 받음)하는 핸들러
/// in T: T는 입력 위치에서만 사용 가능
/// </summary>
public interface IAnimalHandler<in T> where T : Animal
{
    void Handle(T animal);
}

public class AnimalHandler : IAnimalHandler<Animal>
{
    public List<string> HandledNames { get; } = [];
    public void Handle(Animal animal) => HandledNames.Add(animal.Name);
}

public class DogHandler : IAnimalHandler<Dog>
{
    public List<string> HandledBreeds { get; } = [];
    public void Handle(Dog dog) => HandledBreeds.Add(dog.Breed);
}
