using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kairo.Funding.Client;

namespace Kairo.Funding.Examples.Examples;

/// <summary>
/// Example 10 - brotli_etag_client: three calls demonstrating Brotli body
/// compression and <c>If-None-Match</c>/<c>ETag</c> conditional GET.
/// </summary>
public static class Example10
{
    /// <summary>Run the Brotli + ETag demo.</summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        using var client = new FundingClient();

        // Call A: GET /v1/funding with Accept-Encoding: br
        var a = await client.GetRawAsync("/v1/funding", query: null, headers: null, acceptBrotli: true, ct).ConfigureAwait(false);
        var etagA = a.Headers.TryGetValue("ETag", out var e1) ? e1 : "";
        long version = 0;
        if (a.Body.Length > 0)
        {
            using var doc = JsonDocument.Parse(a.Body);
            version = doc.RootElement.GetProperty("version").GetInt64();
        }
        Console.Out.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"A: status={a.Status}  bytes_compressed={a.WireBytes}  bytes_decoded={a.Body.Length}  version={version}  etag={etagA}"));

        // Call B: GET /v1/funding with Accept-Encoding: br + If-None-Match: <etag>
        var bHeaders = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(etagA)) bHeaders["If-None-Match"] = etagA;
        var b = await client.GetRawAsync("/v1/funding", query: null, headers: bHeaders, acceptBrotli: true, ct).ConfigureAwait(false);
        string etagNow;
        if (b.Status == 304)
        {
            etagNow = "unchanged";
        }
        else
        {
            etagNow = b.Headers.TryGetValue("ETag", out var e2) ? e2 : "";
        }
        Console.Out.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"B: status={b.Status}  etag_now={etagNow}"));

        // Call C: GET /v1/funding?since=<version>
        var cQuery = new Dictionary<string, string>
        {
            ["since"] = version.ToString(CultureInfo.InvariantCulture),
        };
        var c = await client.GetRawAsync("/v1/funding", cQuery, headers: null, acceptBrotli: false, ct).ConfigureAwait(false);
        long newVersion = 0;
        int count = 0;
        if (c.Status == 200 && c.Body.Length > 0)
        {
            using var doc = JsonDocument.Parse(c.Body);
            newVersion = doc.RootElement.GetProperty("version").GetInt64();
            count = doc.RootElement.GetProperty("count").GetInt32();
        }
        Console.Out.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"C: since={version}  count={count}  version={newVersion}"));
        return 0;
    }
}
