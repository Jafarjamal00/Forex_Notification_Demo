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
        public virtual DbSet<ForexSymbol> ForexSymbols => Set<ForexSymbol>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Configure ForexSymbol
            modelBuilder.Entity<ForexSymbol>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Symbol).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => e.Symbol).IsUnique();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });
        }
    }
}