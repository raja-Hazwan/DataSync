using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AEMDataSync.Models;
using System.IO;

namespace AEMDataSync.Data
{
    public class AEMDbContext : DbContext
    {
        private readonly string _connectionString;
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Well> Wells { get; set; }

        // Constructor for migrations 
        public AEMDbContext()
        {
            _connectionString = string.Empty;
        }

        public AEMDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Constructor for dependency injection for later use 
        public AEMDbContext(DbContextOptions<AEMDbContext> options) : base(options)
        {
            _connectionString = string.Empty;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                if (!string.IsNullOrEmpty(_connectionString))
                {
                    optionsBuilder.UseSqlServer(_connectionString);
                }
                else
                {
                    // Read connection string from appsettings.json for migrations
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .Build();

                    var connectionString = configuration.GetConnectionString("DefaultConnection");
                    
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        optionsBuilder.UseSqlServer(connectionString);
                    }
                    else
                    {
                        throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json");
                    }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Platform entity - matching database column order
            modelBuilder.Entity<Platform>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // We set the ID from API
                
                // Column order: [Id], [UniqueName], [Latitude], [Longitude], [CreatedAt], [UpdatedAt]
                entity.Property(e => e.UniqueName).IsRequired().HasMaxLength(255);
                
                entity.Property(e => e.Latitude)
                    .HasColumnType("decimal(19,10)")
                    .IsRequired(false);

                entity.Property(e => e.Longitude)
                    .HasColumnType("decimal(19,10)")
                    .IsRequired(false);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.ToTable("Platforms");
            });

            // Configure Well entity - matching database column order
            modelBuilder.Entity<Well>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // We set the ID from API
                
                // Column order: [Id], [PlatformId], [UniqueName], [Latitude], [Longitude], [CreatedAt], [UpdatedAt]
                entity.Property(e => e.PlatformId).IsRequired();
                entity.Property(e => e.UniqueName).IsRequired().HasMaxLength(255);
                
                entity.Property(e => e.Latitude)
                    .HasColumnType("decimal(19,10)")
                    .IsRequired(false);

                entity.Property(e => e.Longitude)
                    .HasColumnType("decimal(19,10)")
                    .IsRequired(false);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
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