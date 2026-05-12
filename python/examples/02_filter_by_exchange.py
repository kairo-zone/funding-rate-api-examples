"""Filter the snapshot by a single exchange and print the first 10 rows sorted by base."""

from __future__ import annotations

import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from kairo_funding import FundingClient, cli_entry  # noqa: E402


def main() -> int:
    exchange = os.environ.get("KAIRO_EXCHANGE", "bybit")
    with FundingClient() as client:
        snapshot = client.get_snapshot(exchange=exchange)
    rows = FundingClient.parse_compact_rows(snapshot.get("data", []))
    count = snapshot.get("count", len(rows))
    print(f"exchange={exchange}  rows={count}")
    rows.sort(key=lambda r: r["base"])
    for row in rows[:10]:
        print(
            f"{row['base']}  rate={row['funding_rate']:.6f}  "
            f"interval={row['funding_interval_hours']}h"
        )
    return 0


if __name__ == "__main__":
    cli_entry(main)
