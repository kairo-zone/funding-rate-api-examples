using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 01 - quickstart: one GET to <c>/v1/funding</c>, print version,
/// count, and the first 5 rows in a column-aligned table.
/// </summary>
public static class Example01
{
    private const string RateFormat = "+0.000000;-0.000000;+0.000000";

    /// <summary>Run the quickstart example.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(ct: ct).ConfigureAwait(false);

        var inv = CultureInfo.InvariantCulture;
        Console.Out.WriteLine(string.Create(inv, $"version={snap.Version}  count={snap.Count}"));
        Console.Out.WriteLine();
        Console.Out.WriteLine($"{"exchange",-12}  {"base",-10}  {"rate",11}  {"next_ms",13}  {"intv",4}");

        var limit = Math.Min(5, snap.Data.Count);
        for (var i = 0; i < limit; i++)
        {
            var r = snap.Data[i];
            var rate = r.FundingRate.ToString(RateFormat, inv);
            var nextMs = r.NextFundingTimeMs.ToString(inv);
            var intv = r.FundingIntervalHours.ToString(inv);
            Console.Out.WriteLine($"{r.Exchange,-12}  {r.Base,-10}  {rate,11}  {nextMs,13}  {intv,3}h");
        }
        return 0;
    }
}
