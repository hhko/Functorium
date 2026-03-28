using Shouldly;

namespace Covariance.Tests.Unit;

public class CovarianceTests
{
    [Fact]
    public void Assign_Succeeds_WhenDogShelterAssignedToAnimalShelter()
    {
        // Arrange
        var dogShelter = new DogShelter();
        dogShelter.Add(new Dog("Buddy", "Golden Retriever"));

        // Act
        IAnimalShelter<Animal> animalShelter = dogShelter;

        // Assert
        animalShelter.GetAnimal(0).ShouldBeOfType<Dog>();
    }

    [Fact]
    public void GetAll_ReturnsDogs_WhenAccessedAsAnimalShelter()
    {
        // Arrange
        var dogShelter = new DogShelter();
        dogShelter.Add(new Dog("Buddy", "Golden Retriever"));
        dogShelter.Add(new Dog("Max", "Labrador"));

        IAnimalShelter<Animal> animalShelter = dogShelter;

        // Act
        var actual = animalShelter.GetAll().ToList();

        // Assert
        actual.Count.ShouldBe(2);
        actual.ShouldAllBe(a => a is Dog);
    }

    [Fact]
    public void Assign_Succeeds_WhenIEnumerableDogAssignedToIEnumerableAnimal()
    {
        // Arrange
        IEnumerable<Dog> dogs = new List<Dog> { new("Buddy", "Golden Retriever") };

        // Act
        IEnumerable<Animal> animals = dogs;

        // Assert
        animals.First().ShouldBeOfType<Dog>();
    }
}
