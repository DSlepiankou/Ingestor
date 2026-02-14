namespace Ingestor.Models
{
    public record MeterDataMessage(string JsonPayload, DateTime CapturedAt);
}
