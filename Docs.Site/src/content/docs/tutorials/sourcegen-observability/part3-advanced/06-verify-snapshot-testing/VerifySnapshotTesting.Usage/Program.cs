using VerifySnapshotTesting.Usage;

var p1 = new Point { X = 1, Y = 2 };
var p2 = new Point { X = 1, Y = 2 };
var p3 = new Point { X = 3, Y = 4 };

Console.WriteLine($"p1.Equals(p2): {p1.Equals(p2)}");
Console.WriteLine($"p1.Equals(p3): {p1.Equals(p3)}");
Console.WriteLine($"p1.GetHashCode() == p2.GetHashCode(): {p1.GetHashCode() == p2.GetHashCode()}");
