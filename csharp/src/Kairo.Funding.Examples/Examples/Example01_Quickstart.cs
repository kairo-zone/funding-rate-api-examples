using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 01 - quickstart: one GET to <c>/v1/funding</c>, print version,
/// count, and the first 5 rows.
/// </summary>
public static class Example01
{
    /// <summary>Run the quickstart example.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(ct: ct).ConfigureAwait(false);

        Console.Out.WriteLine(string.Create(CultureInfo.InvariantCulture, $"version={snap.Version}  count={snap.Count}"));
        var limit = Math.Min(5, snap.Data.Count);
        for (var i = 0; i < limit; i++)
        {
            var r = snap.Data[i];
            Console.Out.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{r.Exchange}  {r.Base}  rate={r.FundingRate}  next={r.NextFundingTimeMs}  interval={r.FundingIntervalHours}h"));
        }
        return 0;
    }
}
