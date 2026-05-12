using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 05 - delta_polling: bootstrap with a full snapshot, then poll
/// <c>?since=&lt;version&gt;</c> in a 30-second loop for up to 5 ticks. Each
/// non-empty tick prints a column-aligned table of changed rows.
/// </summary>
public static class Example05
{
    private const string RateFormat = "+0.000000;-0.000000;+0.000000";

    /// <summary>Run the delta-polling loop.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(ct: ct).ConfigureAwait(false);
        var cursor = snap.Version;
        var inv = CultureInfo.InvariantCulture;
        Console.Out.WriteLine(string.Create(
            inv,
            $"bootstrap: version={cursor}  count={snap.Count}"));

        for (var i = 1; i <= 5; i++)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return 0;
            }

            var delta = await client.GetDeltaAsync(cursor, ct: ct).ConfigureAwait(false);
            if (delta.Count == 0)
            {
                Console.Out.WriteLine(string.Create(
                    inv,
                    $"tick {i}: no change (version={delta.Version})"));
            }
            else
            {
                Console.Out.WriteLine(string.Create(
                    inv,
                    $"tick {i}: {delta.Count} changes, version={delta.Version}"));
                PrintRows(delta.Data);
            }
            cursor = delta.Version;
        }
        return 0;
    }

    private static void PrintRows(IReadOnlyList<FundingEntry> rows)
    {
        var inv = CultureInfo.InvariantCulture;
        Console.Out.WriteLine($"{"exchange",-12}  {"base",-10}  {"rate",11}  {"next_ms",13}  {"intv",4}");
        foreach (var r in rows)
        {
            var rate = r.FundingRate.ToString(RateFormat, inv);
            var nextMs = r.NextFundingTimeMs.ToString(inv);
            var intv = r.FundingIntervalHours.ToString(inv);
            Console.Out.WriteLine($"{r.Exchange,-12}  {r.Base,-10}  {rate,11}  {nextMs,13}  {intv,3}h");
        }
    }
}
