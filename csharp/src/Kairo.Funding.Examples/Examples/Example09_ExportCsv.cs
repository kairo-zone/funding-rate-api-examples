using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 09 - export_csv: write the snapshot to
/// <c>funding_&lt;version&gt;.csv</c> in the current working directory.
/// </summary>
public static class Example09
{
    /// <summary>Run the CSV-export example.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(ct: ct).ConfigureAwait(false);

        var path = Path.Combine(Directory.GetCurrentDirectory(),
            string.Create(CultureInfo.InvariantCulture, $"funding_{snap.Version}.csv"));

        await using (var writer = File.CreateText(path))
        {
            await writer.WriteLineAsync("exchange,base,funding_rate,next_funding_time_ms,funding_interval_hours,event_time_ms").ConfigureAwait(false);
            foreach (var r in snap.Data)
            {
                var line = string.Create(
                    CultureInfo.InvariantCulture,
                    $"{r.Exchange},{r.Base},{r.FundingRate},{r.NextFundingTimeMs},{r.FundingIntervalHours},{r.EventTimeMs}");
                await writer.WriteLineAsync(line).ConfigureAwait(false);
            }
        }

        Console.Out.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"wrote {snap.Data.Count} rows to {path}"));
        return 0;
    }
}
