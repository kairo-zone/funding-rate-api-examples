# Funding API - Go examples

Idiomatic Go reference clients for the [kairo.zone](https://kairo.zone)
Funding API. Each example is a standalone `cmd/<NN>_<stem>/main.go` binary
that shares a thin HTTP wrapper under `internal/client/`.

The 10 examples here mirror the contract in [`../EXAMPLES.md`](../EXAMPLES.md)
line-for-line; outputs are diff-friendly against the Python, TypeScript,
and C# implementations.

## Requirements

- Go 1.23 or newer
- A Funding API key (sign up at <https://kairo.zone>)

The single external dependency is
[`github.com/andybalholm/brotli`](https://github.com/andybalholm/brotli),
used only by example 10 for Brotli decoding.

## 30-second quickstart

```sh
export KAIRO_FUNDING_API_KEY=your_key_here
# optional: export KAIRO_FUNDING_BASE_URL=https://api.kairo.zone

cd go
go run ./cmd/01_quickstart
```

Each example is its own binary. To run a different one:

```sh
go run ./cmd/04_spread_scanner
KAIRO_EXCHANGE=binance go run ./cmd/02_filter_by_exchange
```

To build everything once:

```sh
go build ./...
```

## Docker

A multi-stage Dockerfile builds every example into a static binary and
ships an `entrypoint.sh` that resolves the requested example by name.

```sh
# build the image from the repo root
docker build -f go/Dockerfile -t kairo-funding-go .

# run any example by stem (default is 01_quickstart)
docker run --rm \
    -e KAIRO_FUNDING_API_KEY=your_key_here \
    kairo-funding-go 04_spread_scanner

# alternatively, pass the example name via env
docker run --rm \
    -e KAIRO_FUNDING_API_KEY=your_key_here \
    -e EXAMPLE=08_top_funding \
    kairo-funding-go
```

## Examples

| #  | Binary                            | Description                                                  |
|----|-----------------------------------|--------------------------------------------------------------|
| 01 | `cmd/01_quickstart`               | One GET, one print. Smallest possible client.                |
| 02 | `cmd/02_filter_by_exchange`       | Filter the snapshot by `exchange`.                           |
| 03 | `cmd/03_get_one_symbol`           | Look up a single `(exchange, base)` row.                     |
| 04 | `cmd/04_spread_scanner`           | Cross-exchange spread for one base asset.                    |
| 05 | `cmd/05_delta_polling`            | Long-running poll using the `since` cursor.                  |
| 06 | `cmd/06_funding_alert`            | Threshold alerts with an optional webhook sink.              |
| 07 | `cmd/07_next_funding_countdown`   | Per-base countdown to next funding for a watchlist.          |
| 08 | `cmd/08_top_funding`              | Top 10 positive and bottom 10 negative funding rates.        |
| 09 | `cmd/09_export_csv`               | Persist a snapshot to `funding_<version>.csv`.               |
| 10 | `cmd/10_brotli_etag_client`       | Brotli body negotiation plus conditional GET / delta demo.   |

## Environment variables

The example-specific overrides are documented in
[`../.env.example`](../.env.example). The two variables read by every
example are:

| Variable                 | Default                  | Purpose                                                    |
|--------------------------|--------------------------|------------------------------------------------------------|
| `KAIRO_FUNDING_API_KEY`  | (required)               | Sent as the `X-Api-Key` request header.                    |
| `KAIRO_FUNDING_BASE_URL` | `https://api.kairo.zone` | Base URL for the API; override for staging/dev endpoints.  |

## Exit codes

The shared contract uses the same exit codes across every language:

| Code | Meaning                                                     |
|------|-------------------------------------------------------------|
| 0    | Success.                                                    |
| 1    | Transient error (5xx, DNS failure, connection reset).       |
| 2    | Authentication error (HTTP 401).                            |
| 3    | Rate limited (HTTP 429).                                    |
| 4    | Client logic error (missing env, bad CLI input, bad shape). |

The mapping lives in
[`internal/client/runner.go`](./internal/client/runner.go) and the typed
errors are declared in
[`internal/client/errors.go`](./internal/client/errors.go).

## Layout

```
go/
├── README.md
├── go.mod
├── go.sum
├── Dockerfile
├── entrypoint.sh
├── internal/
│   └── client/
│       ├── client.go
│       ├── errors.go
│       └── runner.go
└── cmd/
    ├── 01_quickstart/main.go
    ├── 02_filter_by_exchange/main.go
    ├── 03_get_one_symbol/main.go
    ├── 04_spread_scanner/main.go
    ├── 05_delta_polling/main.go
    ├── 06_funding_alert/main.go
    ├── 07_next_funding_countdown/main.go
    ├── 08_top_funding/main.go
    ├── 09_export_csv/main.go
    └── 10_brotli_etag_client/main.go
```

The examples deliberately do not implement retries, caching, telemetry, or
other SDK conveniences. They are intended to read as concise, copy-paste
references for integrating with the Funding API from your own code.
