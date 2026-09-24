using System.Text.Json;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Resources;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Storage;

public sealed class DataBackupService
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDataBackupRepository _repository;
    private readonly ICurrentAccess _access;

    public DataBackupService(IDataBackupRepository repository, ICurrentAccess access)
    {
        _repository = repository;
        _access = access;
    }

    public async Task<OperationResult<ExportFile>> ExportAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        var backup = new DataBackupDto
        {
            Speedtests = (await _repository.ListAllSpeedtestsAsync(cancellationToken)).Select(SpeedtestImportRow.From).ToList(),
            Outages = (await _repository.ListAllOutagesAsync(cancellationToken))
                .Select(outage => new DataBackupOutageRow { StartedAt = outage.StartedAt, EndedAt = outage.EndedAt })
                .ToList(),
            WatchSessions = (await _repository.ListAllWatchSessionsAsync(cancellationToken))
                .Select(session => new DataBackupWatchSessionRow { StartedAt = session.StartedAt, LastSeenAt = session.LastSeenAt })
                .ToList(),
            ProbeRounds = (await _repository.ListAllProbeRoundsAsync(cancellationToken))
                .Select(round => new DataBackupProbeRoundRow
                {
                    At = round.At,
                    Passed = round.Passed,
                    Answered = round.Answered,
                    Asked = round.Asked,
                    FastestMilliseconds = round.FastestMilliseconds,
                    DuringTest = round.DuringTest
                })
                .ToList()
        };

        return OperationResult.Ok(new ExportFile(
            JsonSerializer.SerializeToUtf8Bytes(backup, _jsonOptions),
            "application/json",
            "speedtest-watcher-data.json"));
    }

    public async Task<OperationResult<DataImportResultDto>> ImportAsync(DataBackupDto? backup, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (backup == null || backup.IsEmpty) return OperationResult.Invalid(ApplicationStrings.NoDataProvided);
        if (backup.Version != DataBackupDto.CurrentVersion)
            return OperationResult.Invalid(ApplicationStrings.Format(ApplicationStrings.DataBackupUnknownVersion, ProjectInfo.Name));
        if (backup.Speedtests.Any(row => row.Created == null)) return OperationResult.Invalid(ApplicationStrings.SpeedtestMissingCreated);

        var speedtests = backup.Speedtests.Select(row => row.ToSpeedtest()).ToList();
        var outages = backup.Outages
            .Where(row => row.EndedAt is { } endedAt && endedAt >= row.StartedAt)
            .Select(row => new Outage { StartedAt = row.StartedAt, EndedAt = row.EndedAt })
            .ToList();
        var watchSessions = backup.WatchSessions
            .Where(row => row.LastSeenAt >= row.StartedAt)
            .Select(row => new WatchSession { StartedAt = row.StartedAt, LastSeenAt = row.LastSeenAt })
            .ToList();
        var probeRounds = backup.ProbeRounds
            .Select(row => new ProbeRound
            {
                At = row.At,
                Passed = row.Passed,
                Answered = row.Answered,
                Asked = row.Asked,
                FastestMilliseconds = row.FastestMilliseconds,
                DuringTest = row.DuringTest
            })
            .ToList();

        var imported = await _repository.ImportAsync(speedtests, outages, watchSessions, probeRounds, cancellationToken);

        return OperationResult.Ok(new DataImportResultDto
        {
            Speedtests = imported.Speedtests,
            Outages = imported.Outages,
            WatchSessions = imported.WatchSessions,
            ProbeRounds = imported.ProbeRounds,
            Skipped = (backup.Speedtests.Count - imported.Speedtests)
                + (backup.Outages.Count - imported.Outages)
                + (backup.WatchSessions.Count - imported.WatchSessions)
                + (backup.ProbeRounds.Count - imported.ProbeRounds)
        });
    }

    public async Task<OperationResult> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        await _repository.DeleteAllAsync(cancellationToken);
        return OperationResult.Ok();
    }
}
