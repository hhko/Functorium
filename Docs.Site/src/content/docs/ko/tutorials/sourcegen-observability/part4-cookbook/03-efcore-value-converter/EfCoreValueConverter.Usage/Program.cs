using EfCoreValueConverter.Usage;

var money = Money.From("100.50");
var converter = new MoneyConverter();

var dbValue = converter.ConvertToProvider(money);
Console.WriteLine($"To DB: {dbValue}");

var restored = converter.ConvertFromProvider(dbValue);
Console.WriteLine($"From DB: {restored.Value}");
Console.WriteLine($"Round-trip: {money.Value == restored.Value}");
