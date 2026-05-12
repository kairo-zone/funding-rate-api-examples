using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 02 - filter_by_exchange: demonstrates the <c>exchange</c> query
/// parameter; prints the first 10 rows sorted by base ascending in a
/// column-aligned table.
/// </summary>
public static class Example02
{
    private const string RateFormat = "+0.000000;-0.000000;+0.000000";

    /// <summary>Run the exchange-filter example.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        var exchange = Environment.GetEnvironmentVariable("KAIRO_EXCHANGE");
        if (string.IsNullOrWhiteSpace(exchange)) exchange = "bybit";

        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(exchange: exchange, ct: ct).ConfigureAwait(false);

        var inv = CultureInfo.InvariantCulture;
        Console.Out.WriteLine(string.Create(inv, $"exchange={exchange}  rows={snap.Count}"));
        Console.Out.WriteLine();
        Console.Out.WriteLine($"{"base",-16}  {"rate",11}  {"intv",4}");

        var rows = snap.Data
            .OrderBy(r => r.Base, StringComparer.Ordinal)
            .ThenBy(r => r.Exchange, StringComparer.Ordinal)
            .Take(10);
        foreach (var r in rows)
        {
            var rate = r.FundingRate.ToString(RateFormat, inv);
            var intv = r.FundingIntervalHours.ToString(inv);
            Console.Out.WriteLine($"{r.Base,-16}  {rate,11}  {intv,3}h");
        }
        return 0;
    }
}
