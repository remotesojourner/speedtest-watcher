using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// How much is stored.
/// </summary>
public sealed record StorageInfoResponse
{
    /// <summary>
    /// The size of the SQLite database, in bytes.
    /// </summary>
    /// <example>45056</example>
    public required long Size { get; init; }

    /// <summary>
    /// How many results are stored.
    /// </summary>
    /// <example>41</example>
    public required int TestCount { get; init; }

    public static StorageInfoResponse From(StorageInfoDto info) => new() { Size = info.Size, TestCount = info.TestCount };
}
