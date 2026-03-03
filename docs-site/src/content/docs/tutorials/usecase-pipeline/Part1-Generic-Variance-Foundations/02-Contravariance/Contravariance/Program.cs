using Contravariance;

var animalHandler = new AnimalHandler();

// 반공변성: IAnimalHandler<Animal> → IAnimalHandler<Dog> 대입 가능
IAnimalHandler<Dog> dogHandler = animalHandler;

dogHandler.Handle(new Dog("Buddy", "Golden Retriever"));
dogHandler.Handle(new Dog("Max", "Labrador"));

Console.WriteLine("AnimalHandler가 처리한 이름:");
foreach (var name in animalHandler.HandledNames)
    Console.WriteLine($"  - {name}");

// Action<in T> 반공변성
Action<Animal> printAnimal = a => Console.WriteLine($"Animal: {a.Name}");
Action<Dog> printDog = printAnimal;  // 반공변성 대입

Console.WriteLine("\nAction<in T> 반공변성:");
printDog(new Dog("Charlie", "Poodle"));
