"""Smallest possible Funding API client: one GET, print the first 5 rows."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from kairo_funding import FundingClient, cli_entry  # noqa: E402


def main() -> int:
    with FundingClient() as client:
        snapshot = client.get_snapshot()
    version = snapshot.get("version")
    count = snapshot.get("count", 0)
    rows = FundingClient.parse_compact_rows(snapshot.get("data", []))
    print(f"version={version}  count={count}")
    for row in rows[:5]:
        print(
            f"{row['exchange']}  {row['base']}  "
            f"rate={row['funding_rate']:.6f}  "
            f"next={row['next_funding_time_ms']}  "
            f"interval={row['funding_interval_hours']}h"
        )
    return 0


if __name__ == "__main__":
    cli_entry(main)
