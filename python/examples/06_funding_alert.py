"""Threshold-based alert with an optional webhook sink."""

from __future__ import annotations

import json
import os
import sys
from pathlib import Path

import httpx

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from kairo_funding import ClientLogicError, FundingClient, cli_entry  # noqa: E402


def main() -> int:
    raw_threshold = os.environ.get("KAIRO_THRESHOLD", "0.001")
    try:
        threshold = float(raw_threshold)
    except ValueError as exc:
        raise ClientLogicError(f"invalid KAIRO_THRESHOLD={raw_threshold!r}: {exc}") from exc
    webhook_url = os.environ.get("KAIRO_WEBHOOK_URL")

    with FundingClient() as client:
        snapshot = client.get_snapshot()
        rows = FundingClient.parse_compact_rows(snapshot.get("data", []))
        total = len(rows)
        matched = 0
        with httpx.Client(timeout=5.0) as webhook:
            for row in rows:
                if abs(row["funding_rate"]) < threshold:
                    continue
                matched += 1
                print(
                    f"ALERT  {row['exchange']}  {row['base']}  "
                    f"rate={row['funding_rate']:.6f}  "
                    f"next={row['next_funding_time_ms']}"
                )
                if not webhook_url:
                    continue
                payload = {
                    "exchange": row["exchange"],
                    "base": row["base"],
                    "funding_rate": row["funding_rate"],
                    "next_funding_time_ms": row["next_funding_time_ms"],
                }
                try:
                    resp = webhook.post(
                        webhook_url,
                        content=json.dumps(payload),
                        headers={"Content-Type": "application/json"},
                    )
                    if resp.status_code >= 400:
                        print(
                            f"webhook failed for {row['base']}: HTTP {resp.status_code}",
                            file=sys.stderr,
                        )
                except httpx.HTTPError as exc:
                    print(f"webhook failed for {row['base']}: {exc}", file=sys.stderr)
    print(f"matched {matched}/{total} rows above threshold {threshold}")
    return 0


if __name__ == "__main__":
    cli_entry(main)
