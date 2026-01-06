using ForexNotificationSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ForexNotificationSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
        {

        }

        public virtual DbSet<PriceTick> PriceTicks => Set<PriceTick>();
        public virtual DbSet<SubscriptionAudit> SubscriptionAudits => Set<SubscriptionAudit>();
    }
}