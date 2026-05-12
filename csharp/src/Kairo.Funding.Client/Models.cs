using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kairo.Funding.Client;

/// <summary>
/// Single funding row in caller-friendly form. The wire format (compact tuple)
/// is normalized by <see cref="FundingClient.ParseCompactRows"/>.
/// </summary>
public sealed record FundingEntry(
    string Exchange,
    string Base,
    double FundingRate,
    long NextFundingTimeMs,
    int FundingIntervalHours,
    long EventTimeMs);

/// <summary>Single symbol-universe row as returned by <c>/v1/symbols</c>.</summary>
public sealed record SymbolEntry(
    [property: JsonPropertyName("exchange")] string Exchange,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("base")] string Base,
    [property: JsonPropertyName("quote")] string Quote,
    [property: JsonPropertyName("native")] string Native,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("funding_interval_hours")] int FundingIntervalHours,
    [property: JsonPropertyName("is_active")] bool IsActive);

/// <summary>Response envelope for <c>/v1/funding</c> (and its delta variants).</summary>
public sealed record SnapshotResponse(
    long Version,
    long? TimestampMs,
    int Count,
    IReadOnlyList<FundingEntry> Data);

/// <summary>Response envelope for <c>/v1/symbols</c>.</summary>
public sealed record SymbolsResponse(
    int Count,
    IReadOnlyList<SymbolEntry> Data);

/// <summary>
/// Low-level response returned by <see cref="FundingClient.GetRawAsync"/>.
/// Captures both the wire-byte count (Content-Length) and the decoded body bytes.
/// </summary>
public sealed record RawResponse(
    int Status,
    IReadOnlyDictionary<string, string> Headers,
    byte[] Body,
    long WireBytes);
