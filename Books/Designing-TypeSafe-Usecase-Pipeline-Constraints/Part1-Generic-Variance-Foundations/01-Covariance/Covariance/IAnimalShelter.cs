namespace Covariance;

/// <summary>
/// 공변 인터페이스 - 동물을 읽기만 하는 쉼터
/// out T: T는 출력 위치에서만 사용 가능
/// </summary>
public interface IAnimalShelter<out T> where T : Animal
{
    T GetAnimal(int index);
    IEnumerable<T> GetAll();
}

public class DogShelter : IAnimalShelter<Dog>
{
    private readonly List<Dog> _dogs = [];

    public void Add(Dog dog) => _dogs.Add(dog);
    public Dog GetAnimal(int index) => _dogs[index];
    public IEnumerable<Dog> GetAll() => _dogs;
}
