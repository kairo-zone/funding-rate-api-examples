"""Top 10 positive and bottom 10 negative funding rates across the universe."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from kairo_funding import FundingClient, cli_entry  # noqa: E402


def annualized_pct(rate: float, interval_hours: int) -> float:
    return rate * (24 / interval_hours) * 365 * 100


def main() -> int:
    with FundingClient() as client:
        snapshot = client.get_snapshot()
    rows = FundingClient.parse_compact_rows(snapshot.get("data", []))
    positives = [r for r in rows if r["funding_rate"] > 0]
    negatives = [r for r in rows if r["funding_rate"] < 0]
    positives.sort(key=lambda r: (-r["funding_rate"], r["exchange"], r["base"]))
    negatives.sort(key=lambda r: (r["funding_rate"], r["exchange"], r["base"]))

    print("TOP 10 POSITIVE")
    for row in positives[:10]:
        ann = annualized_pct(row["funding_rate"], row["funding_interval_hours"])
        print(
            f"{row['exchange']}  {row['base']}  "
            f"rate={row['funding_rate']}  ann={ann:.4f}%"
        )
    print()
    print("BOTTOM 10 NEGATIVE")
    for row in negatives[:10]:
        ann = annualized_pct(row["funding_rate"], row["funding_interval_hours"])
        print(
            f"{row['exchange']}  {row['base']}  "
            f"rate={row['funding_rate']}  ann={ann:.4f}%"
        )
    return 0


if __name__ == "__main__":
    cli_entry(main)
