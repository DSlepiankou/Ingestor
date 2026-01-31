
using System.Text.Json.Serialization;

namespace Ingestor.Models
{
    public class Motion : MeterData
    {
        public MotionPayload Payload { get; set; }
    }
    public record MotionPayload(
    [property: JsonPropertyName("motionDetected")] bool MotionDetected
    );
}
