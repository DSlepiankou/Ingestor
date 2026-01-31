
using System.Text.Json.Serialization;

namespace Ingestor.Models
{
    public class Energy : MeterData
    {
        public EnergyPayload Payload { get; set; }
    }

    public record EnergyPayload(
    [property: JsonPropertyName("energy")] double Energy
    );
}
