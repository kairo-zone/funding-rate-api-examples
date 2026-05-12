using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 08 - top_funding: top/bottom 10 funding rates across the universe,
/// each shown with their annualized percentage in a column-aligned table.
/// </summary>
public static class Example08
{
    private const string RateFormat = "+0.000000;-0.000000;+0.000000";
    private const string AnnFormat = "+0.0000;-0.0000;+0.0000";

    /// <summary>Run the top/bottom funding example.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(ct: ct).ConfigureAwait(false);

        var inv = CultureInfo.InvariantCulture;
        var withAnn = snap.Data
            .Select(r => (r, ann: r.FundingRate * (24.0 / r.FundingIntervalHours) * 365.0 * 100.0))
            .ToList();

        var positives = withAnn
            .Where(x => x.r.FundingRate > 0)
            .OrderByDescending(x => x.r.FundingRate)
            .ThenBy(x => x.r.Exchange, StringComparer.Ordinal)
            .ThenBy(x => x.r.Base, StringComparer.Ordinal)
            .Take(10)
            .ToList();

        var header = $"{"exchange",-12}  {"base",-10}  {"rate",11}  {"ann%",9}";

        Console.Out.WriteLine("TOP 10 POSITIVE");
        Console.Out.WriteLine(header);
        foreach (var x in positives)
        {
            var rate = x.r.FundingRate.ToString(RateFormat, inv);
            var annStr = x.ann.ToString(AnnFormat, inv);
            Console.Out.WriteLine($"{x.r.Exchange,-12}  {x.r.Base,-10}  {rate,11}  {annStr,8}%");
        }

        var negatives = withAnn
            .Where(x => x.r.FundingRate < 0)
            .OrderBy(x => x.r.FundingRate)
            .ThenBy(x => x.r.Exchange, StringComparer.Ordinal)
            .ThenBy(x => x.r.Base, StringComparer.Ordinal)
            .Take(10)
            .ToList();

        Console.Out.WriteLine();
        Console.Out.WriteLine("BOTTOM 10 NEGATIVE");
        Console.Out.WriteLine(header);
        foreach (var x in negatives)
        {
            var rate = x.r.FundingRate.ToString(RateFormat, inv);
            var annStr = x.ann.ToString(AnnFormat, inv);
            Console.Out.WriteLine($"{x.r.Exchange,-12}  {x.r.Base,-10}  {rate,11}  {annStr,8}%");
        }
        return 0;
    }
}
