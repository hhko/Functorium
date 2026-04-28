using TestScenarios.Usage;

// Valid
var valid = new SensorReading { Temperature = 25, Humidity = 60 };
var validErrors = valid.Validate();
Console.WriteLine($"Valid reading errors: {validErrors.Length}");

// Invalid — boundary testing
var invalid = new SensorReading { Temperature = -50, Humidity = 110 };
var invalidErrors = invalid.Validate();
Console.WriteLine($"Invalid reading errors: {invalidErrors.Length}");
foreach (var error in invalidErrors)
    Console.WriteLine($"  - {error}");

// Edge cases
var edge = new SensorReading { Temperature = -40, Humidity = 100 };
var edgeErrors = edge.Validate();
Console.WriteLine($"Edge case (min/max bounds) errors: {edgeErrors.Length}");
