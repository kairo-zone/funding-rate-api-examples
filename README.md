# Funding API examples

Live perpetual funding rates from 10 exchanges in one API.

[![asciicast](https://asciinema.org/a/2953FQQLHd8rbGi5.svg)](https://asciinema.org/a/2953FQQLHd8rbGi5)

## kairo.zone Funding API

- Production base URL: `https://api.kairo.zone`
- Documentation: <https://kairo.zone/docs>
- Get an API key: <https://kairo.zone>

This repository contains runnable example clients for the kairo.zone Funding API in four languages. Every example exists in every language and is mirrored against the same contract (see [EXAMPLES.md](EXAMPLES.md)), so you can compare idioms across stacks while solving the same problem.

## What's in this repo

| #  | Example                   | What it does                                                             |
|----|---------------------------|--------------------------------------------------------------------------|
| 01 | `quickstart`              | Fetch a snapshot and print the first 5 rows.                             |
| 02 | `filter_by_exchange`      | Fetch a single exchange and print sorted rows.                           |
| 03 | `get_one_symbol`          | Fetch a single `(exchange, base)` row.                                   |
| 04 | `spread_scanner`          | Cross-exchange spread for a single base, with annualized rates.          |
| 05 | `delta_polling`           | Long-running poll loop using `version` and `?since=`.                    |
| 06 | `funding_alert`           | Alert (stdout or webhook) when `|funding_rate|` exceeds a threshold.     |
| 07 | `next_funding_countdown`  | Countdown to the next funding tick for a watchlist of bases.             |
| 08 | `top_funding`             | Top 10 positive and bottom 10 negative funding rates.                    |
| 09 | `export_csv`              | Export a snapshot to a CSV file.                                         |
| 10 | `brotli_etag_client`      | Brotli compression plus `ETag` / `If-None-Match` conditional GET.        |

Each example is implemented in four languages:

- [Python](python/)
- [TypeScript](typescript/)
- [Go](go/)
- [C#](csharp/)

## Quickstart

```bash
curl -sS -H "X-Api-Key: $KAIRO_FUNDING_API_KEY" \
  "https://api.kairo.zone/v1/funding?exchange=bybit&base=BTC"
```

Expected response (compact form, truncated):

```json
{
  "version": 1712304000042,
  "timestamp_ms": 1712304000123,
  "count": 1,
  "data": [
    ["bybit", "BTC", 0.0001, 1712304000000, 8, 1712299900000]
  ]
}
```

The positional row is `[exchange, base, funding_rate, next_funding_time_ms, funding_interval_hours, event_time_ms]`. Pass `?compact=false` to receive named-object rows instead.

## Run the examples

Copy `.env.example` to `.env`, set `KAIRO_FUNDING_API_KEY`, and then run any language directory's quickstart:

```bash
cp .env.example .env
# edit .env and set KAIRO_FUNDING_API_KEY
```

Per-language entry points:

- Python: see [`python/README.md`](python/)
- TypeScript: see [`typescript/README.md`](typescript/)
- Go: see [`go/README.md`](go/)
- C#: see [`csharp/README.md`](csharp/)

## Docker

A `docker-compose.yml` is provided at the repository root that runs every example in every language against the same `.env`. See `docker-compose.yml` for usage.

## Reference

- [OpenAPI 3.1 specification](openapi.yaml)
- [Postman collection](postman/collection.json)
- [Per-example contract](EXAMPLES.md)

## Authentication

Send your API key as a header:

```
X-Api-Key: <your-key>
```

Or as a query parameter (the header wins if both are present):

```
?key=<your-key>
```

## License

MIT. See [LICENSE](LICENSE).
