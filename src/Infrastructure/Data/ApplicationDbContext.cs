using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<UrlMapping> UrlMappings { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UrlMapping>(entity =>
        {
            entity.HasKey(e => e.Id);


            entity.HasIndex(e => e.ShortCode)
                .IsUnique()
                .HasFilter("\"IsActive\" = true"); // Use escaped quotes for PostgreSQL standard SQL

            entity.HasIndex(e => e.OriginalUrl);
            entity.HasIndex(e => e.ExpiresAt);

            // Column configurations
            entity.Property(e => e.OriginalUrl).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.ShortCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);

        });
    }
    
}