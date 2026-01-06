namespace ForexNotificationSystem.Models
{
    public class PriceTick
    {
        public int id { get; set; }
        public string? symbol { get; set; }
        public decimal price { get; set; }
        public decimal bid { get; set; }
        public decimal ask { get; set; }
        public long ts { get; set; }
    }
}