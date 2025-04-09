namespace EventsLoggerNamespace.Events
{
    public class RankCalculatedEvent
    {
        public string EventType { get; set; }
        public string? Id { get; set; }
        public string? Rank { get; set; }
    }
}
