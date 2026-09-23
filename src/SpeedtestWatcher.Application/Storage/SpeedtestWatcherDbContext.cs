using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Storage;

internal class SpeedtestWatcherDbContext : DbContext
{
    public SpeedtestWatcherDbContext(DbContextOptions<SpeedtestWatcherDbContext> options) : base(options)
    {
    }

    public DbSet<Speedtest> Speedtests => Set<Speedtest>();
    public DbSet<ConfigEntry> Configs => Set<ConfigEntry>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<IntegrationData> Integrations => Set<IntegrationData>();
    public DbSet<ProbeRound> ProbeRounds => Set<ProbeRound>();
    public DbSet<Outage> Outages => Set<Outage>();
    public DbSet<WatchSession> WatchSessions => Set<WatchSession>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<TestStatus>().HaveConversion<EnumNameConverter<TestStatus>>();
        configurationBuilder.Properties<TestType>().HaveConversion<EnumNameConverter<TestType>>();
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
            entity.Property(e => e.PacketLoss).HasColumnName("packetLoss");
            entity.Property(e => e.Bufferbloat).HasColumnName("bufferbloat");
            entity.Property(e => e.LatencyIdle).HasColumnName("latencyIdle");
            entity.Property(e => e.LatencyLoaded).HasColumnName("latencyLoaded");
            entity.Property(e => e.LatencyLoadedTail).HasColumnName("latencyLoadedTail");
            entity.Property(e => e.DownloadBytes).HasColumnName("downloadBytes");
            entity.Property(e => e.UploadBytes).HasColumnName("uploadBytes");
            entity.Property(e => e.PublicIp).HasColumnName("publicIp");
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue(TestStatus.Completed);
            entity.Property(e => e.Healthy).HasColumnName("healthy");
            entity.Property(e => e.ThresholdPing).HasColumnName("thresholdPing");
            entity.Property(e => e.ThresholdDownload).HasColumnName("thresholdDownload");
            entity.Property(e => e.ThresholdUpload).HasColumnName("thresholdUpload");
            entity.Property(e => e.Type).HasColumnName("type").HasDefaultValue(TestType.Auto);
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

        modelBuilder.Entity<ProbeRound>(entity =>
        {
            entity.ToTable("probe_rounds");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.At).HasColumnName("at");
            entity.Property(e => e.Passed).HasColumnName("passed");
            entity.Property(e => e.Answered).HasColumnName("answered");
            entity.Property(e => e.Asked).HasColumnName("asked");
            entity.Property(e => e.FastestMilliseconds).HasColumnName("fastestMs");
            entity.Property(e => e.DuringTest).HasColumnName("duringTest");
            entity.HasIndex(e => e.At);
        });

        modelBuilder.Entity<Outage>(entity =>
        {
            entity.ToTable("outages");
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.Length);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StartedAt).HasColumnName("startedAt");
            entity.Property(e => e.EndedAt).HasColumnName("endedAt");
            entity.HasIndex(e => e.StartedAt);
        });

        modelBuilder.Entity<WatchSession>(entity =>
        {
            entity.ToTable("watch_sessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StartedAt).HasColumnName("startedAt");
            entity.Property(e => e.LastSeenAt).HasColumnName("lastSeenAt");
            entity.HasIndex(e => e.LastSeenAt);
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
