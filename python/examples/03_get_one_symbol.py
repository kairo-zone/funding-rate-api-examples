"""Single-row lookup for one (exchange, base) pair."""

from __future__ import annotations

import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from kairo_funding import FundingClient, cli_entry  # noqa: E402


def main() -> int:
    exchange = os.environ.get("KAIRO_EXCHANGE", "bybit")
    base = os.environ.get("KAIRO_BASE", "BTC")
    with FundingClient() as client:
        snapshot = client.get_snapshot(exchange=exchange, base=base)
    rows = FundingClient.parse_compact_rows(snapshot.get("data", []))
    if not rows:
        print(f"no row for {exchange}/{base}")
        return 4
    row = rows[0]
    print(
        f"{row['exchange']}  {row['base']}  "
        f"rate={row['funding_rate']:.6f}  "
        f"next={row['next_funding_time_ms']}  "
        f"interval={row['funding_interval_hours']}h  "
        f"event={row['event_time_ms']}"
    )
    return 0


if __name__ == "__main__":
    cli_entry(main)
