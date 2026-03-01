using Shouldly;

namespace Contravariance.Tests.Unit;

public class ContravarianceTests
{
    [Fact]
    public void Assign_Succeeds_WhenAnimalHandlerAssignedToDogHandler()
    {
        // Arrange
        var animalHandler = new AnimalHandler();

        // Act - 반공변성: IAnimalHandler<Animal> → IAnimalHandler<Dog>
        IAnimalHandler<Dog> dogHandler = animalHandler;
        dogHandler.Handle(new Dog("Buddy", "Golden Retriever"));

        // Assert
        animalHandler.HandledNames.ShouldContain("Buddy");
    }

    [Fact]
    public void Handle_ProcessesDog_WhenAnimalHandlerUsedAsDogHandler()
    {
        // Arrange
        var animalHandler = new AnimalHandler();
        IAnimalHandler<Dog> handler = animalHandler;

        // Act
        handler.Handle(new Dog("Max", "Labrador"));

        // Assert
        animalHandler.HandledNames.Count.ShouldBe(1);
        animalHandler.HandledNames[0].ShouldBe("Max");
    }

    [Fact]
    public void Assign_Succeeds_WhenActionAnimalAssignedToActionDog()
    {
        // Arrange
        var processed = new List<string>();
        Action<Animal> animalAction = a => processed.Add(a.Name);

        // Act - Action<in T> 반공변성
        Action<Dog> dogAction = animalAction;
        dogAction(new Dog("Buddy", "Golden Retriever"));

        // Assert
        processed.ShouldContain("Buddy");
    }
}
