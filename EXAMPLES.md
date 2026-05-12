# Examples contract

This document is the **shared contract** that every language implementation
(`python/`, `typescript/`, `go/`, `csharp/`) must follow. The same example
number has the same filename stem, the same input environment variables,
and the same stdout shape across all four languages, so you can diff them
side-by-side.

If you are implementing a language client: this file is your source of
truth. If you are reading the examples: this file tells you what each
program does and how to drive it.

## Common environment variables

Every example reads these two variables. They are not repeated in each
example's table below.

| Variable                 | Required | Default                   | Purpose                                                  |
|--------------------------|----------|---------------------------|----------------------------------------------------------|
| `KAIRO_FUNDING_API_KEY`  | yes      | (none)                    | API key sent as the `X-Api-Key` request header.          |
| `KAIRO_FUNDING_BASE_URL` | no       | `https://api.kairo.zone`  | Base URL for the Funding API. Override for staging/dev.  |

When `KAIRO_FUNDING_API_KEY` is missing the program must exit with code `4`
("client logic error: bad input"). When the server responds `401` the
program must exit `2`; when it responds `429` it must exit `3`.

## Conventions for language implementers

- **Exit codes** are uniform across languages:
  - `0` success
  - `1` transient error (5xx, DNS failure, connection reset, timeout)
  - `2` authentication error (HTTP `401`)
  - `3` rate-limited (HTTP `429`)
  - `4` client logic error (missing env var, bad CLI input, malformed response)
- **API key transport** is always the `X-Api-Key` header. Do not use the
  query-string fallback in example code.
- **Base URL** must come from `KAIRO_FUNDING_BASE_URL`, defaulting to
  `https://api.kairo.zone`. Never hard-code the host.
- **Output** must be human-readable plain text on stdout. Structured logs
  and JSON output are not required. Errors and progress chatter belong on
  stderr.
- **Compact mode** is the default for `/v1/funding`. Treat each row as the
  positional tuple `(exchange, base, funding_rate, next_funding_time_ms,
  funding_interval_hours, event_time_ms)` unless an example explicitly
  asks for object mode.
- **Brotli decoding** is *required* for example 10. Other examples may
  request plain JSON.
- **Annualized rate** formula, used by examples 04 and 08:
  `annualized_pct = funding_rate * (24 / funding_interval_hours) * 365 * 100`.
- **Time format** for human-readable output: prefer ISO-8601 in UTC
  (e.g. `2026-05-12T09:30:00Z`), but countdowns may use `Xm Ys`.
- **Sorting** must be stable. When two rows tie, break ties by exchange
  name ascending, then by base ascending.

### Table layout

Examples that print **multiple rows** of funding data (01, 02, 04, 05,
06, 08) render a column-aligned table with a header row. Single-row
examples (03), text-heavy ones (07), and file writers (09) keep their
existing `key=value` or single-line formats. Example 10 prints its own
A / B / C summary lines and is unaffected.

Every language must produce byte-equivalent stdout. The cell widths and
formats below are the canonical Python output (see
`python/examples/04_spread_scanner.py` for the reference implementation).

| Cell             | Width | Alignment | Format                                                                |
|------------------|-------|-----------|------------------------------------------------------------------------|
| `exchange`       | 12    | left      | string                                                                 |
| `base` (01,05,06,08) | 10  | left    | string                                                                 |
| `base` (02)      | 16    | left      | string (per-exchange listings can have very long bases)                |
| `rate`           | 11    | right     | signed float, 6 decimals (`+0.000017`, `-0.001234`)                    |
| `ann%`           | 9     | right     | signed float, 4 decimals; literal `%` is **outside** the cell, after   |
| `intv`           | 3     | right     | integer; literal `h` is **outside** the cell, after                    |
| `next_ms`, `event_ms` | 13 | right    | integer (Unix milliseconds)                                            |
| `status` (06)    | 7     | left      | string `"ALERT"`                                                       |

Cell separator: exactly **two spaces** (`"  "`). The header text uses
the bare cell name (e.g. `exchange`, `rate`, `intv`); for left-aligned
string columns the header is padded with trailing spaces to match the
cell width, and for right-aligned number columns the header is padded
with leading spaces so its right edge aligns with the data.

## Examples

### 01. `quickstart`

Smallest possible client: one GET, one print.

| Input                | Value                                                 |
|----------------------|-------------------------------------------------------|
| HTTP                 | `GET /v1/funding`                                     |
| Env (extra)          | (none)                                                |

Stdout (see "Table layout" above for the cell widths and formats):

```
version=<v>  count=<n>

exchange      base          rate        next_ms  intv
<ex>          <ba>          <rate>    <nextMs>  <iv>h
...
```

Print `version=...  count=...` on the first line, a blank line, then a
header row, then the first 5 rows from `data`.

### 02. `filter_by_exchange`

Demonstrates the `exchange` filter.

| Input               | Value                                                  |
|---------------------|--------------------------------------------------------|
| HTTP                | `GET /v1/funding?exchange=<KAIRO_EXCHANGE>`            |
| Env (extra)         | `KAIRO_EXCHANGE` (default `bybit`)                     |

Stdout (table layout):

```
exchange=<x>  rows=<count>

base                     rate  intv
<base>              <rate>    <iv>h
...
```

Print `exchange=...  rows=...` on the first line, a blank line, then a
header row, then the first 10 rows sorted by `base` ascending. The
`base` column is 16 chars wide because per-exchange listings can have
very long base names.

### 03. `get_one_symbol`

Single-row lookup.

| Input               | Value                                                                      |
|---------------------|----------------------------------------------------------------------------|
| HTTP                | `GET /v1/funding?exchange=<KAIRO_EXCHANGE>&base=<KAIRO_BASE>`              |
| Env (extra)         | `KAIRO_EXCHANGE` (default `bybit`), `KAIRO_BASE` (default `BTC`)           |

Stdout: a single line `<exchange>  <base>  rate=<funding_rate>  next=<next_funding_time_ms>  interval=<funding_interval_hours>h  event=<event_time_ms>`.

If `count == 0`, print `no row for <exchange>/<base>` and exit `4`.

### 04. `spread_scanner`

Cross-exchange spread for one base asset.

| Input               | Value                                                  |
|---------------------|--------------------------------------------------------|
| HTTP                | `GET /v1/funding?base=<KAIRO_BASE>`                    |
| Env (extra)         | `KAIRO_BASE` (default `BTC`)                           |

Stdout (table layout):

```
exchange             rate       ann%  intv
<ex>           <rate>    <ann>%   <iv>h
...

spread = <max-min>  (max <maxExchange> @ <maxRate>, min <minExchange> @ <minRate>)
```

Compute `annualized_pct` per row, sort by `funding_rate` ascending,
print the header row, the table rows, a blank line, then the summary
line. The summary numbers are formatted as `+0.000000` (signed, 6
decimals).

### 05. `delta_polling`

Long-running poll loop demonstrating the `version` cursor.

| Input               | Value                                                  |
|---------------------|--------------------------------------------------------|
| HTTP                | First call: `GET /v1/funding`. Subsequent calls: `GET /v1/funding?since=<version>`. |
| Env (extra)         | (none)                                                 |

Behavior:

1. Issue a full-snapshot call, remember `version` as `cursor`.
2. Print `bootstrap: version=<cursor>  count=<count>`.
3. Loop up to 5 iterations *or* until SIGINT/Ctrl+C:
   - Sleep 30 seconds.
   - Call `?since=<cursor>`.
   - If `count == 0`, print `tick <i>: no change (version=<v>)`.
   - Otherwise print `tick <i>: <count> changes, version=<v>`, then a
     table with the same column layout as example 01 (header row +
     rows for each changed entry).
   - Update `cursor = response.version`.
4. Exit cleanly on SIGINT with code `0`.

The optional `If-None-Match` / 304 path is allowed but not required.

### 06. `funding_alert`

Threshold-based alert with an optional webhook sink.

| Input               | Value                                                                 |
|---------------------|-----------------------------------------------------------------------|
| HTTP                | `GET /v1/funding`                                                     |
| Env (extra)         | `KAIRO_THRESHOLD` (default `0.001`), `KAIRO_WEBHOOK_URL` (optional)   |

Behavior:

- Parse `KAIRO_THRESHOLD` as a decimal. Invalid input -> exit `4`.
- For every row where `abs(funding_rate) >= threshold`:
  - The first match prints a header row, then each matching row as a
    table line:
    ```
    status   exchange      base          rate        next_ms
    ALERT    <ex>          <base>      <rate>    <nextMs>
    ```
  - If `KAIRO_WEBHOOK_URL` is set, `POST` JSON
    `{"exchange": ..., "base": ..., "funding_rate": ..., "next_funding_time_ms": ...}`
    to that URL with `Content-Type: application/json`. Webhook failures
    must not crash the program; print `webhook failed for <base>: <reason>` on stderr and continue.

Final line: `matched <n>/<total> rows above threshold <threshold>`.

### 07. `next_funding_countdown`

Per-base countdown for a watchlist.

| Input               | Value                                                                                  |
|---------------------|----------------------------------------------------------------------------------------|
| HTTP                | `GET /v1/funding?base=<csv>` where `<csv>` is the watchlist                            |
| Env (extra)         | `KAIRO_WATCHLIST` (default `BTC,ETH,SOL`), `KAIRO_ALERT_MINUTES` (default `10`)        |

For each base in the watchlist, pick the row with the smallest
`next_funding_time_ms`. Compute `remaining_seconds = (next - now_ms) / 1000`
(skip negatives by printing `<base>: no upcoming funding`).

Print one line per base, sorted by remaining time ascending:
`<prefix><base> on <exchange>: in <X>m <Y>s, rate=<funding_rate>`.

The prefix is `[ALERT] ` when `remaining_seconds <= KAIRO_ALERT_MINUTES * 60`, otherwise empty (six spaces or just nothing, implementer's choice — keep it consistent within the language).

### 08. `top_funding`

Top/bottom 10 rates across the universe.

| Input               | Value                                                  |
|---------------------|--------------------------------------------------------|
| HTTP                | `GET /v1/funding`                                      |
| Env (extra)         | (none)                                                 |

Compute `annualized_pct` for every row. Print two tables separated by
a blank line:

```
TOP 10 POSITIVE
exchange      base          rate       ann%
<ex>          <base>      <rate>    <ann>%
... (up to 10 lines)

BOTTOM 10 NEGATIVE
exchange      base          rate       ann%
<ex>          <base>      <rate>    <ann>%
... (up to 10 lines)
```

If there are fewer than 10 positive (or negative) rows, print whatever
exists. Sort positives by `funding_rate` descending; negatives ascending.

### 09. `export_csv`

Persist a snapshot to disk.

| Input               | Value                                                  |
|---------------------|--------------------------------------------------------|
| HTTP                | `GET /v1/funding`                                      |
| Env (extra)         | (none)                                                 |

Write `funding_<version>.csv` in the current working directory. Columns,
in order, with a header row:

```
exchange,base,funding_rate,next_funding_time_ms,funding_interval_hours,event_time_ms
```

Numeric columns must be written as plain numbers (no thousands separators,
no quotes). Stdout: `wrote <n> rows to <path>`.

### 10. `brotli_etag_client`

Compression + conditional GET demo.

| Input               | Value                                                  |
|---------------------|--------------------------------------------------------|
| HTTP                | Three calls: see below.                                |
| Env (extra)         | (none)                                                 |

Behavior:

1. **Call A**: `GET /v1/funding` with `Accept-Encoding: br`. Decode the
   Brotli body. Remember `etag = response.headers["ETag"]` and
   `version = body.version`. Print
   `A: status=200  bytes_compressed=<wire>  bytes_decoded=<decoded>  version=<v>  etag=<etag>`.
2. **Call B**: `GET /v1/funding` with `Accept-Encoding: br` and
   `If-None-Match: <etag>`. Expect `304 Not Modified` if nothing changed,
   or `200` with a new ETag if it did. Print
   `B: status=<304|200>  etag_now=<etag-or-"unchanged">`.
3. **Call C**: `GET /v1/funding?since=<version>`. Print
   `C: since=<v>  count=<count>  version=<new-v>`.

The example demonstrates the difference between "give me a delta" (`since`)
and "tell me if anything changed" (`If-None-Match`).
