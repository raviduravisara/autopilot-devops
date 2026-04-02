using AutoPilot.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPilot.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("user_accounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
        });
    }
}