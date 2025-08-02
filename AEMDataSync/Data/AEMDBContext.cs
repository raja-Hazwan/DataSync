using Microsoft.EntityFrameworkCore;
using AEMDataSync.Models;

namespace AEMDataSync.Data
{
    public class AEMDbContext : DbContext
    {
        private readonly string _connectionString;
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Well> Wells { get; set; }

        public AEMDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Platform entity
            modelBuilder.Entity<Platform>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // We set the ID from API
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Code).HasMaxLength(50);

                // Configure Latitude and Longitude with increased precision
            
                entity.Property(e => e.Latitude)
                    .HasColumnType("decimal(18,10)")
                    .IsRequired(false);

                entity.Property(e => e.Longitude)
                    .HasColumnType("decimal(18,10)")
                    .IsRequired(false);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Code).IsUnique(false);
                entity.ToTable("Platforms");
            });

            // Configure Well entity
            modelBuilder.Entity<Well>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // We set the ID from API
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Code).HasMaxLength(50);

                
                entity.Property(e => e.Latitude)
                    .HasColumnType("decimal(18,10)")
                    .IsRequired(false);

                entity.Property(e => e.Longitude)
                    .HasColumnType("decimal(18,10)")
                    .IsRequired(false);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Code).IsUnique(false);
                entity.ToTable("Wells");

                // Configure foreign key relationship
                entity.HasOne(w => w.Platform)
                      .WithMany(p => p.Wells)
                      .HasForeignKey(w => w.PlatformId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}