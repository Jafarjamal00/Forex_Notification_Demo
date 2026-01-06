namespace ForexNotificationSystem.Models
{
    public class SubscriptionAudit
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Symbol { get; set; }
        public string? Action { get; set; }
        public DateTime At { get; set; }
    }
}