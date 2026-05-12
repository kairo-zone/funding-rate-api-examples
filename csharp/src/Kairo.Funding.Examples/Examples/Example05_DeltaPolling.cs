using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 05 - delta_polling: bootstrap with a full snapshot, then poll
/// <c>?since=&lt;version&gt;</c> in a 30-second loop for up to 5 ticks.
/// </summary>
public static class Example05
{
    /// <summary>Run the delta-polling loop.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(ct: ct).ConfigureAwait(false);
        var cursor = snap.Version;
        Console.Out.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
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
                    CultureInfo.InvariantCulture,
                    $"tick {i}: no change (version={delta.Version})"));
            }
            else
            {
                Console.Out.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"tick {i}: {delta.Count} changes, version={delta.Version}"));
                foreach (var r in delta.Data)
                {
                    Console.Out.WriteLine(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{r.Exchange}  {r.Base}  rate={r.FundingRate}  next={r.NextFundingTimeMs}  interval={r.FundingIntervalHours}h"));
                }
            }
            cursor = delta.Version;
        }
        return 0;
    }
}
