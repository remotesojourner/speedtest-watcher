using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Infrastructure.Data;

public class SpeedtestWatcherDbContext : DbContext
{
    public SpeedtestWatcherDbContext(DbContextOptions<SpeedtestWatcherDbContext> options) : base(options)
    {
    }

    public DbSet<Speedtest> Speedtests => Set<Speedtest>();
    public DbSet<ConfigEntry> Configs => Set<ConfigEntry>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<IntegrationData> Integrations => Set<IntegrationData>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Speedtest>(entity =>
        {
            entity.ToTable("speedtests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ServerId).HasColumnName("serverId").HasDefaultValue(0);
            entity.Property(e => e.ServerName).HasColumnName("serverName");
            entity.Property(e => e.ServerHost).HasColumnName("serverHost");
            entity.Property(e => e.Ping).HasColumnName("ping").IsRequired();
            entity.Property(e => e.Jitter).HasColumnName("jitter");
            entity.Property(e => e.Download).HasColumnName("download").IsRequired();
            entity.Property(e => e.Upload).HasColumnName("upload").IsRequired();
            entity.Property(e => e.Error).HasColumnName("error");
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue("completed");
            entity.Property(e => e.Healthy).HasColumnName("healthy");
            entity.Property(e => e.ThresholdPing).HasColumnName("thresholdPing");
            entity.Property(e => e.ThresholdDownload).HasColumnName("thresholdDownload");
            entity.Property(e => e.ThresholdUpload).HasColumnName("thresholdUpload");
            entity.Property(e => e.Type).HasColumnName("type").HasDefaultValue("auto");
            entity.Property(e => e.ResultId).HasColumnName("resultId");
            entity.Property(e => e.Time).HasColumnName("time").HasDefaultValue(0);
            entity.Property(e => e.Created).HasColumnName("created");
        });

        modelBuilder.Entity<ConfigEntry>(entity =>
        {
            entity.ToTable("config");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Value).HasColumnName("value").IsRequired();
        });

        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.ToTable("recommendations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Ping).HasColumnName("ping").IsRequired();
            entity.Property(e => e.Download).HasColumnName("download").IsRequired();
            entity.Property(e => e.Upload).HasColumnName("upload").IsRequired();
        });

        modelBuilder.Entity<IntegrationData>(entity =>
        {
            entity.ToTable("integration_data");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisplayName).HasColumnName("displayName").HasDefaultValue("Untitled");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Data).HasColumnName("data").HasDefaultValue("{}");
            entity.Property(e => e.LastActivity).HasColumnName("lastActivity");
            entity.Property(e => e.ActivityFailed).HasColumnName("activityFailed").HasDefaultValue(false);
        });
    }
}
