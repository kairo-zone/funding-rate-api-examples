# Funding API — TypeScript examples

Runnable, dependency-free TypeScript examples for the
[kairo.zone Funding API](https://kairo.zone). Each example follows the
shared contract in [`../EXAMPLES.md`](../EXAMPLES.md), so the stdout
format matches the Python, Go, and C# implementations row-for-row.

## Requirements

- **Node.js 20+** (uses the built-in `fetch` and `node:zlib` Brotli APIs).
- **TypeScript 5.x**, installed as a dev dependency.
- An API key from kairo.zone.

No runtime dependencies — everything uses Node built-ins. The
`devDependencies` are limited to `typescript`, `tsx`, and `@types/node`.

## Quickstart (30 seconds)

```bash
cd typescript
npm install
export KAIRO_FUNDING_API_KEY=your_key_here
npm run example:01
```

Run the others by index, for example:

```bash
npm run example:04          # spread scanner
KAIRO_BASE=ETH npm run example:04
```

Or invoke `tsx` directly:

```bash
npx tsx src/examples/07_next_funding_countdown.ts
```

## Docker

The Dockerfile expects the build context to be the `typescript/`
directory (i.e. run `docker build` from inside `typescript/`):

```bash
cd typescript
docker build -t kairo-funding-ts .
docker run --rm -e KAIRO_FUNDING_API_KEY=your_key_here kairo-funding-ts
```

Override the entry-point to run a different example:

```bash
docker run --rm -e KAIRO_FUNDING_API_KEY=your_key_here \
  kairo-funding-ts npx tsx src/examples/04_spread_scanner.ts
```

## Examples

| #  | Script                              | npm alias          | Demonstrates                                  |
|----|-------------------------------------|--------------------|-----------------------------------------------|
| 01 | `01_quickstart.ts`                  | `example:01`       | One GET, print the first 5 rows.              |
| 02 | `02_filter_by_exchange.ts`          | `example:02`       | `?exchange=` filter, sort by base.            |
| 03 | `03_get_one_symbol.ts`              | `example:03`       | Single-row lookup, exit 4 on miss.            |
| 04 | `04_spread_scanner.ts`              | `example:04`       | Cross-exchange spread for one base.           |
| 05 | `05_delta_polling.ts`               | `example:05`       | `?since=<version>` cursor loop with SIGINT.   |
| 06 | `06_funding_alert.ts`               | `example:06`       | Threshold alerts + optional webhook fan-out.  |
| 07 | `07_next_funding_countdown.ts`      | `example:07`       | Per-base countdown for a watchlist.           |
| 08 | `08_top_funding.ts`                 | `example:08`       | Top/bottom 10 funding rates.                  |
| 09 | `09_export_csv.ts`                  | `example:09`       | Stream snapshot to `funding_<version>.csv`.   |
| 10 | `10_brotli_etag_client.ts`          | `example:10`       | Brotli decoding + `If-None-Match` 304 flow.   |

## Environment variables

The full list lives in [`../.env.example`](../.env.example). The
required and most common ones:

| Variable                | Required | Default                     | Used by         |
|-------------------------|----------|-----------------------------|-----------------|
| `KAIRO_FUNDING_API_KEY` | yes      | —                           | all             |
| `KAIRO_FUNDING_BASE_URL`| no       | `https://api.kairo.zone`    | all             |
| `KAIRO_EXCHANGE`        | no       | `bybit`                     | 02, 03          |
| `KAIRO_BASE`            | no       | `BTC`                       | 03, 04          |
| `KAIRO_THRESHOLD`       | no       | `0.001`                     | 06              |
| `KAIRO_WEBHOOK_URL`     | no       | —                           | 06              |
| `KAIRO_WATCHLIST`       | no       | `BTC,ETH,SOL`               | 07              |
| `KAIRO_ALERT_MINUTES`   | no       | `10`                        | 07              |

## Exit codes

The shared contract from `EXAMPLES.md`:

| Code | Meaning                                                      |
|------|--------------------------------------------------------------|
| 0    | success                                                      |
| 1    | transient error (5xx, DNS failure, connection reset, timeout)|
| 2    | authentication error (HTTP 401)                              |
| 3    | rate-limited (HTTP 429)                                      |
| 4    | client logic error (missing env var, bad input, no data)     |

## Type-check

```bash
npm install
npx tsc --noEmit
```

## Project layout

```
typescript/
├── README.md
├── package.json
├── tsconfig.json
├── Dockerfile
└── src/
    ├── client.ts      # FundingClient (~150 lines, single class)
    ├── errors.ts      # AuthError, RateLimitError, TransientError, ClientLogicError
    ├── runner.ts      # cliEntry(main) — uniform exit-code wrapper
    ├── types.ts       # FundingEntry, SnapshotResponse, SymbolsResponse, RawResponse
    └── examples/
        ├── 01_quickstart.ts
        ├── 02_filter_by_exchange.ts
        ├── 03_get_one_symbol.ts
        ├── 04_spread_scanner.ts
        ├── 05_delta_polling.ts
        ├── 06_funding_alert.ts
        ├── 07_next_funding_countdown.ts
        ├── 08_top_funding.ts
        ├── 09_export_csv.ts
        └── 10_brotli_etag_client.ts
```
