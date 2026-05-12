"""Persist the current snapshot to funding_<version>.csv in the working directory."""

from __future__ import annotations

import csv
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from kairo_funding import FundingClient, cli_entry  # noqa: E402

COLUMNS = (
    "exchange",
    "base",
    "funding_rate",
    "next_funding_time_ms",
    "funding_interval_hours",
    "event_time_ms",
)


def main() -> int:
    with FundingClient() as client:
        snapshot = client.get_snapshot()
    version = snapshot.get("version")
    rows = FundingClient.parse_compact_rows(snapshot.get("data", []))
    out_path = Path.cwd() / f"funding_{version}.csv"
    with out_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f, quoting=csv.QUOTE_MINIMAL)
        writer.writerow(COLUMNS)
        for row in rows:
            writer.writerow([row[c] for c in COLUMNS])
    print(f"wrote {len(rows)} rows to {out_path}")
    return 0


if __name__ == "__main__":
    cli_entry(main)
