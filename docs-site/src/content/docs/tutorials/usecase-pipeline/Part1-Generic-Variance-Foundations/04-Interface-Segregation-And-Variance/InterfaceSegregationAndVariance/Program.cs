using InterfaceSegregationAndVariance;

// 1. IFactory<TSelf> - CRTP 팩토리
Console.WriteLine("=== CRTP 팩토리 ===");
var container = Container.Create("Hello");
Console.WriteLine($"  Container: Value={container.Value}, IsValid={container.IsValid}");

var empty = Container.CreateEmpty();
Console.WriteLine($"  Empty: Value=\"{empty.Value}\", IsValid={empty.IsValid}");

// 2. IReadable<out T> - 공변 읽기
Console.WriteLine("\n=== 공변 읽기 ===");
IReadable<string> readable = container;
Console.WriteLine($"  IReadable<string>: Value={readable.Value}, IsValid={readable.IsValid}");

// 3. IReadWrite<T> - 읽기+쓰기
Console.WriteLine("\n=== 읽기+쓰기 ===");
var mutable = new MutableContainer<string>();
Console.WriteLine($"  Before Write: IsValid={mutable.IsValid}");
mutable.Write("World");
Console.WriteLine($"  After Write: Value={mutable.Value}, IsValid={mutable.IsValid}");

// 4. static abstract 제약 활용
Console.WriteLine("\n=== static abstract 제약 ===");
var defaultContainer = CreateDefault<Container>();
Console.WriteLine($"  CreateDefault<Container>(): IsValid={defaultContainer.IsValid}");

static T CreateDefault<T>() where T : IFactory<T> => T.CreateEmpty();
