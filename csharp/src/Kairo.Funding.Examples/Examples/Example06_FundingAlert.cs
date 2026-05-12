using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 06 - funding_alert: print alerts for rows whose absolute funding
/// rate exceeds a threshold, optionally forwarding each alert to a webhook.
/// The table header is emitted lazily only when at least one row matches.
/// </summary>
public static class Example06
{
    private const string RateFormat = "+0.000000;-0.000000;+0.000000";

    /// <summary>Run the threshold-alert example.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        var inv = CultureInfo.InvariantCulture;

        var thresholdRaw = Environment.GetEnvironmentVariable("KAIRO_THRESHOLD");
        if (string.IsNullOrWhiteSpace(thresholdRaw)) thresholdRaw = "0.001";
        if (!double.TryParse(thresholdRaw, NumberStyles.Float, inv, out var threshold))
            throw new ClientLogicException($"KAIRO_THRESHOLD is not a number: {thresholdRaw}");

        var webhook = Environment.GetEnvironmentVariable("KAIRO_WEBHOOK_URL");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        using var client = new FundingClient();

        var snap = await client.GetSnapshotAsync(ct: ct).ConfigureAwait(false);
        int matched = 0;
        bool headerPrinted = false;
        foreach (var r in snap.Data)
        {
            if (Math.Abs(r.FundingRate) < threshold) continue;
            if (!headerPrinted)
            {
                Console.Out.WriteLine($"{"status",-7}  {"exchange",-12}  {"base",-10}  {"rate",11}  {"next_ms",13}");
                headerPrinted = true;
            }
            matched++;
            var rate = r.FundingRate.ToString(RateFormat, inv);
            var nextMs = r.NextFundingTimeMs.ToString(inv);
            Console.Out.WriteLine($"{"ALERT",-7}  {r.Exchange,-12}  {r.Base,-10}  {rate,11}  {nextMs,13}");

            if (!string.IsNullOrWhiteSpace(webhook))
            {
                var payload = string.Create(inv,
                    $"{{\"exchange\":\"{r.Exchange}\",\"base\":\"{r.Base}\",\"funding_rate\":{r.FundingRate},\"next_funding_time_ms\":{r.NextFundingTimeMs}}}");
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Post, webhook)
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                    };
                    using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        await Console.Error.WriteLineAsync(
                            $"webhook failed for {r.Base}: HTTP {(int)resp.StatusCode}").ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"webhook failed for {r.Base}: {ex.Message}").ConfigureAwait(false);
                }
            }
        }

        Console.Out.WriteLine(string.Create(
            inv,
            $"matched {matched}/{snap.Data.Count} rows above threshold {threshold}"));
        return 0;
    }
}
