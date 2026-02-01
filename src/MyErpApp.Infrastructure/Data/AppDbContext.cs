using Microsoft.EntityFrameworkCore;
using MyErpApp.Core.Domain;

namespace MyErpApp.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UiPage> UiPages { get; set; } = null!;
        public DbSet<UiComponent> UiComponents { get; set; } = null!;
        public DbSet<EntitySnapshot> EntitySnapshots { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UiPage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<UiComponent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne<UiPage>()
                    .WithMany(p => p.Components)
                    .HasForeignKey(c => c.UiPageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EntitySnapshot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
            });
        }
    }
}
