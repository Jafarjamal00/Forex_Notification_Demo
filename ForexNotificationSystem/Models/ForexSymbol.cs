namespace ForexNotificationSystem.Models
{
    public class ForexSymbol
    {
        public int Id { get; set; }
        public string? Symbol { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; }
    }
}