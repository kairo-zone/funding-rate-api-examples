"""Cross-exchange funding spread for one base asset, sorted by funding rate."""

from __future__ import annotations

import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from kairo_funding import ClientLogicError, FundingClient, cli_entry  # noqa: E402


def annualized_pct(rate: float, interval_hours: int) -> float:
    return rate * (24 / interval_hours) * 365 * 100


def main() -> int:
    base = os.environ.get("KAIRO_BASE", "BTC")
    with FundingClient() as client:
        snapshot = client.get_snapshot(base=base)
    rows = FundingClient.parse_compact_rows(snapshot.get("data", []))
    if not rows:
        raise ClientLogicError(f"no rows for base={base}")
    rows.sort(key=lambda r: (r["funding_rate"], r["exchange"]))
    for row in rows:
        ann = annualized_pct(row["funding_rate"], row["funding_interval_hours"])
        print(
            f"{row['exchange']}  rate={row['funding_rate']}  "
            f"ann={ann:.4f}%  interval={row['funding_interval_hours']}h"
        )
    min_row = rows[0]
    max_row = rows[-1]
    spread = max_row["funding_rate"] - min_row["funding_rate"]
    print(
        f"spread = {spread} (max {max_row['exchange']} @ {max_row['funding_rate']}, "
        f"min {min_row['exchange']} @ {min_row['funding_rate']})"
    )
    return 0


if __name__ == "__main__":
    cli_entry(main)
