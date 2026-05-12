using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 07 - next_funding_countdown: countdown to the next funding event
/// for each base in <c>KAIRO_WATCHLIST</c>.
/// </summary>
public static class Example07
{
    /// <summary>Run the watchlist countdown example.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        var watchRaw = Environment.GetEnvironmentVariable("KAIRO_WATCHLIST");
        if (string.IsNullOrWhiteSpace(watchRaw)) watchRaw = "BTC,ETH,SOL";
        var watch = watchRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (watch.Length == 0) throw new ClientLogicException("KAIRO_WATCHLIST is empty");

        var alertMinRaw = Environment.GetEnvironmentVariable("KAIRO_ALERT_MINUTES");
        if (string.IsNullOrWhiteSpace(alertMinRaw)) alertMinRaw = "10";
        if (!int.TryParse(alertMinRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var alertMin))
            throw new ClientLogicException($"KAIRO_ALERT_MINUTES is not an integer: {alertMinRaw}");
        long alertSeconds = alertMin * 60L;

        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(@base: string.Join(",", watch), ct: ct).ConfigureAwait(false);

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var picks = new List<(string Base, FundingEntry? Row, long Remaining)>();
        foreach (var b in watch)
        {
            var bestRow = snap.Data
                .Where(r => string.Equals(r.Base, b, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.NextFundingTimeMs)
                .FirstOrDefault();
            var remaining = bestRow is null ? long.MaxValue : (bestRow.NextFundingTimeMs - nowMs) / 1000;
            picks.Add((b, bestRow, remaining));
        }

        foreach (var p in picks.OrderBy(p => p.Remaining))
        {
            if (p.Row is null || p.Remaining < 0)
            {
                Console.Out.WriteLine($"{p.Base}: no upcoming funding");
                continue;
            }
            var m = p.Remaining / 60;
            var s = p.Remaining % 60;
            var prefix = p.Remaining <= alertSeconds ? "[ALERT] " : "";
            Console.Out.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{prefix}{p.Row.Base} on {p.Row.Exchange}: in {m}m {s}s, rate={p.Row.FundingRate}"));
        }
        return 0;
    }
}
