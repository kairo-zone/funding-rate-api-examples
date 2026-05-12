using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 04 - spread_scanner: cross-exchange spread for one base asset.
/// Computes annualized percentage from funding rate and interval; prints
/// a column-aligned table sorted by funding rate ascending.
/// </summary>
public static class Example04
{
    private const string RateFormat = "+0.000000;-0.000000;+0.000000";
    private const string AnnFormat = "+0.0000;-0.0000;+0.0000";

    /// <summary>Run the cross-exchange spread scanner.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        var @base = Environment.GetEnvironmentVariable("KAIRO_BASE");
        if (string.IsNullOrWhiteSpace(@base)) @base = "BTC";

        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(@base: @base, ct: ct).ConfigureAwait(false);

        if (snap.Data.Count == 0)
            throw new ClientLogicException($"no rows for base={@base}");

        var sorted = snap.Data
            .OrderBy(r => r.FundingRate)
            .ThenBy(r => r.Exchange, StringComparer.Ordinal)
            .ToList();

        var inv = CultureInfo.InvariantCulture;
        Console.Out.WriteLine($"{"exchange",-12}  {"rate",11}  {"ann%",9}  {"intv",4}");
        foreach (var r in sorted)
        {
            var ann = r.FundingRate * (24.0 / r.FundingIntervalHours) * 365.0 * 100.0;
            var rate = r.FundingRate.ToString(RateFormat, inv);
            var annStr = ann.ToString(AnnFormat, inv);
            var intv = r.FundingIntervalHours.ToString(inv);
            Console.Out.WriteLine($"{r.Exchange,-12}  {rate,11}  {annStr,8}%  {intv,3}h");
        }

        var min = sorted[0];
        var max = sorted[^1];
        var spread = max.FundingRate - min.FundingRate;
        Console.Out.WriteLine();
        Console.Out.WriteLine(string.Create(
            inv,
            $"spread = {spread.ToString(RateFormat, inv)}  (max {max.Exchange} @ {max.FundingRate.ToString(RateFormat, inv)}, min {min.Exchange} @ {min.FundingRate.ToString(RateFormat, inv)})"));
        return 0;
    }
}
