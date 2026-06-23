using AutoPilot.Api.Models;
using Microsoft.EntityFrameworkCore;
using MonitorEntity = AutoPilot.Api.Models.Monitor;

namespace AutoPilot.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<MonitorEntity> Monitors => Set<MonitorEntity>();
    public DbSet<MonitorCheckRun> MonitorCheckRuns => Set<MonitorCheckRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("user_accounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(160).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<MonitorEntity>(entity =>
        {
            entity.ToTable("monitors");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(140).IsRequired();
            entity.Property(x => x.TargetUrl).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.Method).HasMaxLength(16).IsRequired();
            entity.Property(x => x.CheckIntervalSeconds).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.LastErrorMessage).HasMaxLength(1000);
            entity.Property(x => x.ConsecutiveSuccessCount).IsRequired();
            entity.Property(x => x.ConsecutiveFailureCount).IsRequired();
            entity.HasIndex(x => new { x.OwnerUserId, x.Name });
            entity.HasIndex(x => new { x.IsActive, x.LastCheckedAtUtc });

            entity.HasOne(x => x.OwnerUser)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MonitorCheckRun>(entity =>
        {
            entity.ToTable("monitor_check_runs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExecutedAtUtc).IsRequired();
            entity.Property(x => x.ResponseTimeMs);
            entity.Property(x => x.StatusCode);
            entity.Property(x => x.IsSuccess).IsRequired();
            entity.Property(x => x.ErrorMessage).HasMaxLength(1000);

            entity.HasIndex(x => new { x.MonitorId, x.ExecutedAtUtc });

            entity.HasOne(x => x.Monitor)
                .WithMany(x => x.CheckRuns)
                .HasForeignKey(x => x.MonitorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}