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
    print()
    print(f"{'exchange':<12}  {'base':<10}  {'rate':>11}  {'next_ms':>13}  {'intv':>4}")
    for row in rows[:5]:
        print(
            f"{row['exchange']:<12}  {row['base']:<10}  "
            f"{row['funding_rate']:>+11.6f}  "
            f"{row['next_funding_time_ms']:>13}  "
            f"{row['funding_interval_hours']:>3}h"
        )
    return 0


if __name__ == "__main__":
    cli_entry(main)
