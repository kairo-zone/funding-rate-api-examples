"""Per-base countdown to the next funding event for a watchlist."""

from __future__ import annotations

import os
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from kairo_funding import ClientLogicError, FundingClient, cli_entry  # noqa: E402


def main() -> int:
    watchlist = [b.strip() for b in os.environ.get("KAIRO_WATCHLIST", "BTC,ETH,SOL").split(",") if b.strip()]
    if not watchlist:
        raise ClientLogicError("KAIRO_WATCHLIST is empty")
    try:
        alert_minutes = int(os.environ.get("KAIRO_ALERT_MINUTES", "10"))
    except ValueError as exc:
        raise ClientLogicError(f"invalid KAIRO_ALERT_MINUTES: {exc}") from exc
    alert_seconds = alert_minutes * 60

    with FundingClient() as client:
        snapshot = client.get_snapshot(base=",".join(watchlist))
    rows = FundingClient.parse_compact_rows(snapshot.get("data", []))

    now_ms = int(time.time() * 1000)
    picked: dict[str, dict] = {}
    for row in rows:
        b = row["base"]
        if b not in picked or row["next_funding_time_ms"] < picked[b]["next_funding_time_ms"]:
            picked[b] = row

    lines: list[tuple[float, str]] = []
    for b in watchlist:
        row = picked.get(b)
        if row is None:
            lines.append((float("inf"), f"{b}: no upcoming funding"))
            continue
        remaining_s = (row["next_funding_time_ms"] - now_ms) / 1000
        if remaining_s < 0:
            lines.append((float("inf"), f"{b}: no upcoming funding"))
            continue
        minutes = int(remaining_s) // 60
        seconds = int(remaining_s) % 60
        prefix = "[ALERT] " if remaining_s <= alert_seconds else ""
        lines.append(
            (
                remaining_s,
                f"{prefix}{b} on {row['exchange']}: in {minutes}m {seconds}s, rate={row['funding_rate']}",
            )
        )
    lines.sort(key=lambda x: x[0])
    for _, line in lines:
        print(line)
    return 0


if __name__ == "__main__":
    cli_entry(main)
