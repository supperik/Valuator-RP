namespace EventsLoggerNamespace.Events
{
    public class SimilarityCalculatedEvent
    {
        public string EventType { get; set; }
        public string? Id { get; set; }
        public string? Similarity { get; set; }
    }
}
