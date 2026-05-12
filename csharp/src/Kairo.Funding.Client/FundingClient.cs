using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kairo.Funding.Client;

/// <summary>
/// Thin asynchronous wrapper around the kairo.zone Funding API. Reads the API
/// key from <c>KAIRO_FUNDING_API_KEY</c> and base URL from
/// <c>KAIRO_FUNDING_BASE_URL</c> (default <c>https://api.kairo.zone</c>).
/// Maps HTTP errors to typed <see cref="FundingApiException"/> subclasses.
/// </summary>
public sealed class FundingClient : IDisposable
{
    /// <summary>Default base URL used when <c>KAIRO_FUNDING_BASE_URL</c> is unset.</summary>
    public const string DefaultBaseUrl = "https://api.kairo.zone";

    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly Uri _baseUri;

    /// <summary>
    /// Construct using an internally-created <see cref="HttpClient"/>. Reads
    /// credentials and base URL from environment variables.
    /// </summary>
    public FundingClient() : this(null) { }

    /// <summary>
    /// Construct from an externally-supplied <see cref="HttpClient"/>. The
    /// client is treated as borrowed and is not disposed by this instance.
    /// </summary>
    public FundingClient(HttpClient? http)
    {
        var apiKey = Environment.GetEnvironmentVariable("KAIRO_FUNDING_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ClientLogicException("KAIRO_FUNDING_API_KEY is not set");

        var baseUrl = Environment.GetEnvironmentVariable("KAIRO_FUNDING_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = DefaultBaseUrl;
        _baseUri = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);

        if (http is null)
        {
            // Disable automatic decompression so we can observe wire bytes and decode Brotli explicitly.
            var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.None };
            _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
            _ownsHttp = true;
        }
        else
        {
            _http = http;
            _ownsHttp = false;
        }

        _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", apiKey);
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>Fetch the current funding snapshot.</summary>
    public Task<SnapshotResponse> GetSnapshotAsync(
        string? exchange = null,
        string? @base = null,
        bool compact = true,
        CancellationToken ct = default)
    {
        return GetSnapshotInternalAsync(FundingParams(exchange, @base, compact, since: null), ct);
    }

    /// <summary>Fetch a delta starting after the supplied <paramref name="since"/> version cursor.</summary>
    public Task<SnapshotResponse> GetDeltaAsync(
        long since,
        string? exchange = null,
        string? @base = null,
        bool compact = true,
        CancellationToken ct = default)
    {
        return GetSnapshotInternalAsync(FundingParams(exchange, @base, compact, since: since), ct);
    }

    /// <summary>Fetch the symbol universe, optionally filtered by exchange.</summary>
    public async Task<SymbolsResponse> GetSymbolsAsync(string? exchange = null, CancellationToken ct = default)
    {
        var q = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(exchange)) q["exchange"] = exchange!;
        var raw = await GetRawAsync("/v1/symbols", q, headers: null, acceptBrotli: false, ct).ConfigureAwait(false);
        if (raw.Status != 200)
            throw new ClientLogicException($"unexpected status {raw.Status} from /v1/symbols");

        try
        {
            using var doc = JsonDocument.Parse(raw.Body);
            var root = doc.RootElement;
            var count = root.GetProperty("count").GetInt32();
            var list = new List<SymbolEntry>(count);
            if (root.TryGetProperty("data", out var data))
            {
                foreach (var el in data.EnumerateArray())
                    list.Add(JsonSerializer.Deserialize<SymbolEntry>(el.GetRawText())!);
            }
            return new SymbolsResponse(count, list);
        }
        catch (JsonException ex)
        {
            throw new ClientLogicException($"malformed JSON body: {ex.Message}");
        }
    }

    /// <summary>
    /// Issue a raw GET. Returns the response with wire-byte size preserved and
    /// the body already Brotli-decoded when <paramref name="acceptBrotli"/> is set.
    /// </summary>
    public async Task<RawResponse> GetRawAsync(
        string path,
        IReadOnlyDictionary<string, string>? query = null,
        IReadOnlyDictionary<string, string>? headers = null,
        bool acceptBrotli = false,
        CancellationToken ct = default)
    {
        var uri = BuildUri(path, query);
        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        if (acceptBrotli)
            req.Headers.TryAddWithoutValidation("Accept-Encoding", "br");
        if (headers is not null)
        {
            foreach (var kv in headers)
                req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }

        HttpResponseMessage resp;
        try
        {
            resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new TransientException($"network error: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TransientException($"request timed out: {ex.Message}", ex);
        }

        try
        {
            int status = (int)resp.StatusCode;
            if (status == 401) throw new AuthException("unauthorized (401)");
            if (status == 429) throw new RateLimitException("rate limited (429)");
            if (status >= 500) throw new TransientException($"server error ({status})");
            if (status != 200 && status != 304)
                throw new ClientLogicException($"unexpected status {status}");

            // Read raw wire bytes first to preserve compressed length.
            byte[] wire = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            long wireBytes = wire.LongLength;

            byte[] body = wire;
            if (status == 200 && acceptBrotli)
            {
                var contentEncoding = resp.Content.Headers.ContentEncoding;
                if (contentEncoding.Any(e => string.Equals(e, "br", StringComparison.OrdinalIgnoreCase)))
                {
                    using var ms = new MemoryStream(wire, writable: false);
                    using var br = new BrotliStream(ms, CompressionMode.Decompress);
                    using var outMs = new MemoryStream();
                    await br.CopyToAsync(outMs, ct).ConfigureAwait(false);
                    body = outMs.ToArray();
                }
            }
            if (status == 304)
                body = Array.Empty<byte>();

            var hdrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in resp.Headers) hdrs[h.Key] = string.Join(",", h.Value);
            foreach (var h in resp.Content.Headers) hdrs[h.Key] = string.Join(",", h.Value);

            return new RawResponse(status, hdrs, body, wireBytes);
        }
        finally
        {
            resp.Dispose();
        }
    }

    /// <summary>
    /// Decode a compact <c>data</c> array (positional tuples) into typed
    /// <see cref="FundingEntry"/> records. Also accepts object-shaped rows
    /// (when the caller passed <c>compact=false</c>).
    /// </summary>
    public static IReadOnlyList<FundingEntry> ParseCompactRows(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Array)
            throw new ClientLogicException("expected JSON array for `data`");

        var list = new List<FundingEntry>(data.GetArrayLength());
        foreach (var row in data.EnumerateArray())
        {
            if (row.ValueKind == JsonValueKind.Object)
            {
                list.Add(new FundingEntry(
                    row.GetProperty("exchange").GetString() ?? string.Empty,
                    row.GetProperty("base").GetString() ?? string.Empty,
                    row.GetProperty("funding_rate").GetDouble(),
                    row.GetProperty("next_funding_time_ms").GetInt64(),
                    row.GetProperty("funding_interval_hours").GetInt32(),
                    row.GetProperty("event_time_ms").GetInt64()));
                continue;
            }
            if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() < 6)
                throw new ClientLogicException($"malformed compact row: {row}");

            list.Add(new FundingEntry(
                row[0].GetString() ?? string.Empty,
                row[1].GetString() ?? string.Empty,
                row[2].GetDouble(),
                row[3].GetInt64(),
                row[4].GetInt32(),
                row[5].GetInt64()));
        }
        return list;
    }

    /// <summary>Release the internally-owned <see cref="HttpClient"/>, if any.</summary>
    public void Dispose()
    {
        if (_ownsHttp)
            _http.Dispose();
    }

    // ---- internals ---------------------------------------------------------

    private static IReadOnlyDictionary<string, string> FundingParams(string? exchange, string? @base, bool compact, long? since)
    {
        var q = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(exchange)) q["exchange"] = exchange!;
        if (!string.IsNullOrEmpty(@base)) q["base"] = @base!;
        if (!compact) q["compact"] = "false";
        if (since.HasValue) q["since"] = since.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return q;
    }

    private Uri BuildUri(string path, IReadOnlyDictionary<string, string>? query)
    {
        var rel = path.TrimStart('/');
        if (query is null || query.Count == 0)
            return new Uri(_baseUri, rel);
        var qs = string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        return new Uri(_baseUri, $"{rel}?{qs}");
    }

    private async Task<SnapshotResponse> GetSnapshotInternalAsync(IReadOnlyDictionary<string, string> q, CancellationToken ct)
    {
        var raw = await GetRawAsync("/v1/funding", q, headers: null, acceptBrotli: false, ct).ConfigureAwait(false);
        if (raw.Status != 200)
            throw new ClientLogicException($"unexpected status {raw.Status} from /v1/funding");

        try
        {
            using var doc = JsonDocument.Parse(raw.Body);
            return SnapshotFromJson(doc.RootElement);
        }
        catch (JsonException ex)
        {
            throw new ClientLogicException($"malformed JSON body: {ex.Message}");
        }
    }

    /// <summary>Convert a parsed <c>/v1/funding</c> root JSON element into a typed snapshot.</summary>
    public static SnapshotResponse SnapshotFromJson(JsonElement root)
    {
        var version = root.GetProperty("version").GetInt64();
        long? tsMs = root.TryGetProperty("timestamp_ms", out var ts) && ts.ValueKind == JsonValueKind.Number
            ? ts.GetInt64()
            : null;
        var count = root.GetProperty("count").GetInt32();
        var data = root.TryGetProperty("data", out var d)
            ? ParseCompactRows(d)
            : Array.Empty<FundingEntry>();
        return new SnapshotResponse(version, tsMs, count, data);
    }
}
