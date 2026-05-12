"""Long-running poll loop demonstrating the `since` version cursor."""

from __future__ import annotations

import signal
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from kairo_funding import FundingClient, cli_entry  # noqa: E402

POLL_INTERVAL_SECONDS = 30
MAX_ITERATIONS = 5

_stop = False


def _on_sigint(_signum: int, _frame: object) -> None:
    global _stop
    _stop = True


def _print_rows(rows: list[dict]) -> None:
    for row in rows:
        print(
            f"{row['exchange']}  {row['base']}  "
            f"rate={row['funding_rate']:.6f}  "
            f"next={row['next_funding_time_ms']}  "
            f"interval={row['funding_interval_hours']}h"
        )


def main() -> int:
    signal.signal(signal.SIGINT, _on_sigint)
    with FundingClient() as client:
        snapshot = client.get_snapshot()
        cursor = snapshot.get("version")
        count = snapshot.get("count", 0)
        print(f"bootstrap: version={cursor}  count={count}")
        for i in range(1, MAX_ITERATIONS + 1):
            slept = 0.0
            while slept < POLL_INTERVAL_SECONDS and not _stop:
                time.sleep(0.5)
                slept += 0.5
            if _stop:
                return 0
            delta = client.get_delta(since=cursor)
            new_version = delta.get("version", cursor)
            n = delta.get("count", 0)
            rows = FundingClient.parse_compact_rows(delta.get("data", []))
            if n == 0:
                print(f"tick {i}: no change (version={new_version})")
            else:
                print(f"tick {i}: {n} changes, version={new_version}")
                _print_rows(rows)
            cursor = new_version
    return 0


if __name__ == "__main__":
    cli_entry(main)
