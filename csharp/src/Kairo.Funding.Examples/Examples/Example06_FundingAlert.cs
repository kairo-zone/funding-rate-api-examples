using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 06 - funding_alert: print alerts for rows above an absolute
/// threshold, optionally forwarding each alert to a webhook.
/// </summary>
public static class Example06
{
    /// <summary>Run the threshold-alert example.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        var thresholdRaw = Environment.GetEnvironmentVariable("KAIRO_THRESHOLD");
        if (string.IsNullOrWhiteSpace(thresholdRaw)) thresholdRaw = "0.001";
        if (!double.TryParse(thresholdRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold))
            throw new ClientLogicException($"KAIRO_THRESHOLD is not a number: {thresholdRaw}");

        var webhook = Environment.GetEnvironmentVariable("KAIRO_WEBHOOK_URL");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        using var client = new FundingClient();

        var snap = await client.GetSnapshotAsync(ct: ct).ConfigureAwait(false);
        int matched = 0;
        foreach (var r in snap.Data)
        {
            if (Math.Abs(r.FundingRate) < threshold) continue;
            matched++;
            Console.Out.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"ALERT  {r.Exchange}  {r.Base}  rate={r.FundingRate}  next={r.NextFundingTimeMs}"));

            if (!string.IsNullOrWhiteSpace(webhook))
            {
                var payload = string.Create(CultureInfo.InvariantCulture,
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
                            $"webhook failed for {r.Base}: status {(int)resp.StatusCode}").ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"webhook failed for {r.Base}: {ex.Message}").ConfigureAwait(false);
                }
            }
        }

        Console.Out.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"matched {matched}/{snap.Data.Count} rows above threshold {threshold}"));
        return 0;
    }
}
