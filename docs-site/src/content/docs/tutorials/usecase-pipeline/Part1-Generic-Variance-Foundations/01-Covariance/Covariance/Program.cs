using Covariance;

var dogShelter = new DogShelter();
dogShelter.Add(new Dog("Buddy", "Golden Retriever"));
dogShelter.Add(new Dog("Max", "Labrador"));

// 공변성: IAnimalShelter<Dog> → IAnimalShelter<Animal> 대입 가능
IAnimalShelter<Animal> animalShelter = dogShelter;

foreach (var animal in animalShelter.GetAll())
    Console.WriteLine(animal);
