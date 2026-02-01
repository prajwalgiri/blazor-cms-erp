using Microsoft.EntityFrameworkCore;
using Accounting.Plugin.Domain;

namespace Accounting.Plugin.Data
{
    public class AccountingDbContext : DbContext
    {
        public AccountingDbContext(DbContextOptions<AccountingDbContext> options) : base(options)
        {
        }

        public DbSet<LedgerEntry> LedgerEntries { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<LedgerEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AccountCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
            });
        }
    }
}
