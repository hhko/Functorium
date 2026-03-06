using TestScenarios.Generated;

namespace TestScenarios.Usage;

[ValidateRange]
public partial class SensorReading
{
    [Range(-40, 85)]
    public int Temperature { get; set; }

    [Range(0, 100)]
    public int Humidity { get; set; }
}
