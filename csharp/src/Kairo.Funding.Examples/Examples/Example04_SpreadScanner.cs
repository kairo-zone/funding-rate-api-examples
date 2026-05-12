using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 04 - spread_scanner: cross-exchange spread for one base asset.
/// Computes annualized percentage from funding rate and interval.
/// </summary>
public static class Example04
{
    /// <summary>Run the cross-exchange spread scanner.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        var @base = Environment.GetEnvironmentVariable("KAIRO_BASE");
        if (string.IsNullOrWhiteSpace(@base)) @base = "BTC";

        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(@base: @base, ct: ct).ConfigureAwait(false);

        if (snap.Data.Count == 0)
        {
            Console.Out.WriteLine($"no rows for base {@base}");
            return 0;
        }

        var sorted = snap.Data
            .OrderBy(r => r.FundingRate)
            .ThenBy(r => r.Exchange, StringComparer.Ordinal)
            .ThenBy(r => r.Base, StringComparer.Ordinal)
            .ToList();

        foreach (var r in sorted)
        {
            var ann = r.FundingRate * (24.0 / r.FundingIntervalHours) * 365.0 * 100.0;
            Console.Out.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{r.Exchange}  rate={r.FundingRate}  ann={ann}%  interval={r.FundingIntervalHours}h"));
        }

        var min = sorted[0];
        var max = sorted[^1];
        var spread = max.FundingRate - min.FundingRate;
        Console.Out.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"spread = {spread} (max {max.Exchange} @ {max.FundingRate}, min {min.Exchange} @ {min.FundingRate})"));
        return 0;
    }
}
