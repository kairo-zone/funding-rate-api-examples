# Python examples for the kairo.zone Funding API

Ten runnable scripts that demonstrate the public Funding API. Every example
shares the same tiny client in `kairo_funding/` and mirrors the contract in
the repository root [`EXAMPLES.md`](../EXAMPLES.md).

## Requirements

- Python 3.10 or newer
- An API key from kairo.zone (sent as the `X-Api-Key` header)

The two environment variables every example reads:

| Variable                 | Required | Default                   |
|--------------------------|----------|---------------------------|
| `KAIRO_FUNDING_API_KEY`  | yes      | (none)                    |
| `KAIRO_FUNDING_BASE_URL` | no       | `https://api.kairo.zone`  |

See the root [`.env.example`](../.env.example) for the full list, including
per-example overrides like `KAIRO_EXCHANGE`, `KAIRO_BASE`, `KAIRO_THRESHOLD`,
`KAIRO_WATCHLIST`, `KAIRO_ALERT_MINUTES`, and `KAIRO_WEBHOOK_URL`.

## 30-second quickstart

```bash
cd python
pip install -e .
export KAIRO_FUNDING_API_KEY=your_key_here
python examples/01_quickstart.py
```

Every example is a self-contained script and can be invoked the same way:

```bash
python examples/04_spread_scanner.py
KAIRO_THRESHOLD=0.002 python examples/06_funding_alert.py
```

### A note on the digit-prefix filenames

The filenames keep a numeric prefix (`01_`, `02_`, ...) so they sort in the
natural order shown in `EXAMPLES.md`. Python module names cannot start with a
digit, so the scripts are run directly via `python examples/<file>.py` rather
than `python -m examples.<name>`. Each script inserts the `python/` directory
onto `sys.path`, so it works without an editable install too -- though
`pip install -e .` is still the cleanest setup.

## Or with Docker

```bash
docker build -t kairo-funding-py -f Dockerfile .
docker run --rm \
    -e KAIRO_FUNDING_API_KEY=your_key_here \
    kairo-funding-py \
    python examples/04_spread_scanner.py
```

The default `CMD` runs `examples/01_quickstart.py`; pass any other example as
the command to switch.

## Examples

| File                                 | What it demonstrates                                                  |
|--------------------------------------|-----------------------------------------------------------------------|
| `examples/01_quickstart.py`          | One GET, print version + first five rows.                             |
| `examples/02_filter_by_exchange.py`  | Filter the snapshot by `exchange`, sort by base, print first ten.     |
| `examples/03_get_one_symbol.py`      | Single-row lookup for one `(exchange, base)` pair.                    |
| `examples/04_spread_scanner.py`      | Cross-exchange funding spread for one base, with annualized rate.     |
| `examples/05_delta_polling.py`       | Long-running poll loop using the `since` version cursor (Ctrl+C OK).  |
| `examples/06_funding_alert.py`       | Threshold alert with optional webhook POST.                           |
| `examples/07_next_funding_countdown.py` | Per-base countdown for a watchlist, sorted by remaining time.       |
| `examples/08_top_funding.py`         | Top 10 positive and bottom 10 negative funding rates.                 |
| `examples/09_export_csv.py`          | Persist a snapshot to `funding_<version>.csv` in the working dir.     |
| `examples/10_brotli_etag_client.py`  | Brotli decoding plus conditional GET with `If-None-Match`.            |

## Exit codes

Every script uses the same exit codes (from `EXAMPLES.md`):

| Code | Meaning                                                              |
|------|----------------------------------------------------------------------|
| 0    | Success                                                              |
| 1    | Transient error (5xx, network, timeout)                              |
| 2    | Authentication error (HTTP 401)                                      |
| 3    | Rate-limited (HTTP 429)                                              |
| 4    | Client logic error (missing env var, malformed response, no rows)    |

## Library choice

The shared client uses [`httpx`](https://www.python-httpx.org/) with the
`[brotli,http2]` extras. httpx gives us one synchronous client with automatic
Brotli decoding for example 10, a small typed `Response` object, and a clean
mapping from HTTP errors to the exit-code-coded exceptions in
`kairo_funding.client`.
