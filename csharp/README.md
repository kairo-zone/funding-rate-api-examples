# C# / .NET examples for the kairo.zone Funding API

Ten runnable examples that talk to the [kairo.zone Funding API](https://kairo.zone)
using a thin, dependency-free .NET client built on `HttpClient` and
`System.Text.Json`. Brotli decoding uses the built-in `System.IO.Compression.BrotliStream`.

All ten examples are bundled into a **single multi-command console app**
(`kairo-funding`) so you do not need to build ten executables. The first
argument is the example key.

## Requirements

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) (LTS).
- A Funding API key.

## 30-second quickstart

```sh
cd csharp

export KAIRO_FUNDING_API_KEY=your_key_here
# Optional override; defaults to https://api.kairo.zone
# export KAIRO_FUNDING_BASE_URL=https://api.kairo.zone

dotnet run --project src/Kairo.Funding.Examples -- 01_quickstart
```

Build all examples once and call them by their short keys:

```sh
dotnet build -c Release
dotnet src/Kairo.Funding.Examples/bin/Release/net8.0/kairo-funding.dll 04
```

## Docker

```sh
# Build from the repository root so the Dockerfile can see the csharp/ tree.
docker build -t kairo-funding-cs -f csharp/Dockerfile .

docker run --rm -e KAIRO_FUNDING_API_KEY=your_key_here \
  kairo-funding-cs 04_spread_scanner
```

The default `CMD` is `01`, so `docker run kairo-funding-cs` runs the
quickstart example.

## Examples

Every example reads `KAIRO_FUNDING_API_KEY` and (optionally)
`KAIRO_FUNDING_BASE_URL`. See the root `.env.example` for the complete list
of environment variables and `EXAMPLES.md` for the cross-language
behavioral contract.

| Key                          | Short | What it does                                           |
|------------------------------|-------|--------------------------------------------------------|
| `01_quickstart`              | `01`  | One GET; prints version, count, and the first 5 rows.  |
| `02_filter_by_exchange`      | `02`  | Filters the snapshot by `KAIRO_EXCHANGE`.              |
| `03_get_one_symbol`          | `03`  | Single-row lookup for `KAIRO_EXCHANGE` / `KAIRO_BASE`. |
| `04_spread_scanner`          | `04`  | Cross-exchange spread for one base asset.              |
| `05_delta_polling`           | `05`  | Bootstrap + `?since=<version>` poll loop (5 ticks).    |
| `06_funding_alert`           | `06`  | Threshold alert with optional webhook POST.            |
| `07_next_funding_countdown`  | `07`  | Per-base countdown for the watchlist.                  |
| `08_top_funding`             | `08`  | Top/bottom 10 funding rates with annualized %.         |
| `09_export_csv`              | `09`  | Writes `funding_<version>.csv` to the cwd.             |
| `10_brotli_etag_client`      | `10`  | Brotli decode + `If-None-Match` conditional GET.       |

## Exit codes

The same exit codes apply across all language implementations:

- `0` success
- `1` transient error (5xx, DNS, connection reset, timeout)
- `2` authentication error (HTTP 401)
- `3` rate-limited (HTTP 429)
- `4` client logic error (missing env var, bad CLI input, malformed response)

## Layout

```
csharp/
  KairoFundingExamples.sln
  Dockerfile
  src/
    Kairo.Funding.Client/      # Thin HttpClient + System.Text.Json wrapper
    Kairo.Funding.Examples/    # Dispatcher + ten ExampleNN classes
```
