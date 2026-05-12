using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 03 - get_one_symbol: single-row lookup using both
/// <c>exchange</c> and <c>base</c> filters.
/// </summary>
public static class Example03
{
    /// <summary>Run the single-symbol lookup example.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        var exchange = Environment.GetEnvironmentVariable("KAIRO_EXCHANGE");
        if (string.IsNullOrWhiteSpace(exchange)) exchange = "bybit";
        var @base = Environment.GetEnvironmentVariable("KAIRO_BASE");
        if (string.IsNullOrWhiteSpace(@base)) @base = "BTC";

        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(exchange: exchange, @base: @base, ct: ct).ConfigureAwait(false);

        if (snap.Count == 0 || snap.Data.Count == 0)
        {
            Console.Out.WriteLine($"no row for {exchange}/{@base}");
            return 4;
        }

        var r = snap.Data[0];
        Console.Out.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{r.Exchange}  {r.Base}  rate={r.FundingRate}  next={r.NextFundingTimeMs}  interval={r.FundingIntervalHours}h  event={r.EventTimeMs}"));
        return 0;
    }
}
