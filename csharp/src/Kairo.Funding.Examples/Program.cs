using System;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;
using Kairo.Funding.Examples.Examples;

namespace Kairo.Funding.Examples;

/// <summary>
/// Dispatcher entry point. The first positional argument selects which
/// example to run; the remaining arguments are forwarded to that example.
/// </summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintUsage();
            return args.Length == 0 ? 4 : 0;
        }

        var key = NormalizeKey(args[0]);
        var rest = args.Length > 1 ? args[1..] : Array.Empty<string>();

        Func<string[], CancellationToken, Task<int>>? entry = key switch
        {
            "01" or "01_quickstart" or "quickstart" => Example01.RunAsync,
            "02" or "02_filter_by_exchange" or "filter_by_exchange" => Example02.RunAsync,
            "03" or "03_get_one_symbol" or "get_one_symbol" => Example03.RunAsync,
            "04" or "04_spread_scanner" or "spread_scanner" => Example04.RunAsync,
            "05" or "05_delta_polling" or "delta_polling" => Example05.RunAsync,
            "06" or "06_funding_alert" or "funding_alert" => Example06.RunAsync,
            "07" or "07_next_funding_countdown" or "next_funding_countdown" => Example07.RunAsync,
            "08" or "08_top_funding" or "top_funding" => Example08.RunAsync,
            "09" or "09_export_csv" or "export_csv" => Example09.RunAsync,
            "10" or "10_brotli_etag_client" or "brotli_etag_client" => Example10.RunAsync,
            _ => null,
        };

        if (entry is null)
        {
            await Console.Error.WriteLineAsync($"error: unknown example '{args[0]}'").ConfigureAwait(false);
            PrintUsage();
            return 4;
        }

        return await CliEntry.RunAsync(ct => entry(rest, ct)).ConfigureAwait(false);
    }

    private static string NormalizeKey(string raw)
    {
        var k = raw.Trim().ToLowerInvariant();
        if (k.StartsWith("--")) k = k[2..];
        return k;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("usage: kairo-funding <example> [args...]");
        Console.Error.WriteLine("examples:");
        Console.Error.WriteLine("  01_quickstart              one GET, print first 5 rows");
        Console.Error.WriteLine("  02_filter_by_exchange      filter snapshot by exchange");
        Console.Error.WriteLine("  03_get_one_symbol          single-row lookup");
        Console.Error.WriteLine("  04_spread_scanner          cross-exchange spread");
        Console.Error.WriteLine("  05_delta_polling           poll loop using since=<version>");
        Console.Error.WriteLine("  06_funding_alert           threshold + optional webhook");
        Console.Error.WriteLine("  07_next_funding_countdown  per-base countdown");
        Console.Error.WriteLine("  08_top_funding             top/bottom 10 annualized");
        Console.Error.WriteLine("  09_export_csv              write snapshot to CSV");
        Console.Error.WriteLine("  10_brotli_etag_client      Brotli + If-None-Match demo");
    }
}
