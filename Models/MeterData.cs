using System.Text.Json.Serialization;

namespace Ingestor.Models
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(AirQuality), "air_quality")]
    [JsonDerivedType(typeof(Motion), "motion")]
    [JsonDerivedType(typeof(Energy), "energy")]
    public class MeterData
    {
        public string Name { get; set; }
    }
}
