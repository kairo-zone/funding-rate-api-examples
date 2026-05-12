using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 02 - filter_by_exchange: demonstrates the <c>exchange</c> query
/// parameter; prints the first 10 rows sorted by base ascending.
/// </summary>
public static class Example02
{
    /// <summary>Run the exchange-filter example.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        var exchange = Environment.GetEnvironmentVariable("KAIRO_EXCHANGE");
        if (string.IsNullOrWhiteSpace(exchange)) exchange = "bybit";

        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(exchange: exchange, ct: ct).ConfigureAwait(false);

        Console.Out.WriteLine(string.Create(CultureInfo.InvariantCulture, $"exchange={exchange}  rows={snap.Count}"));

        var rows = snap.Data
            .OrderBy(r => r.Base, StringComparer.Ordinal)
            .ThenBy(r => r.Exchange, StringComparer.Ordinal)
            .Take(10);
        foreach (var r in rows)
        {
            Console.Out.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{r.Base}  rate={r.FundingRate}  interval={r.FundingIntervalHours}h"));
        }
        return 0;
    }
}
