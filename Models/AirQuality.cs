using System.Text.Json.Serialization;

namespace Ingestor.Models
{
    public class AirQuality : MeterData
    {
        public AirQualityPayload Payload { get; set; }
    }

    public record AirQualityPayload(
    [property: JsonPropertyName("co2")] int Co2,
    [property: JsonPropertyName("pm25")] int Pm25,
    [property: JsonPropertyName("humidity")] int Humidity
    );
}
