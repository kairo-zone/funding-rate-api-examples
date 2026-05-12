using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 08 - top_funding: top/bottom 10 funding rates across the universe,
/// each shown with their annualized percentage.
/// </summary>
public static class Example08
{
    /// <summary>Run the top/bottom funding example.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        using var client = new FundingClient();
        var snap = await client.GetSnapshotAsync(ct: ct).ConfigureAwait(false);

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

        Console.Out.WriteLine("TOP 10 POSITIVE");
        foreach (var x in positives)
        {
            Console.Out.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{x.r.Exchange}  {x.r.Base}  rate={x.r.FundingRate}  ann={x.ann}%"));
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
        foreach (var x in negatives)
        {
            Console.Out.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{x.r.Exchange}  {x.r.Base}  rate={x.r.FundingRate}  ann={x.ann}%"));
        }
        return 0;
    }
}
