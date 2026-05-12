/**
 * 05_delta_polling: bootstrap with a full snapshot, then poll `?since=<v>`
 * for up to 5 iterations or until SIGINT.
 */

import { FundingClient } from "../client.js";
import { cliEntry } from "../runner.js";
import type { FundingEntry } from "../types.js";

const MAX_ITERATIONS = 5;
const SLEEP_MS = 30_000;

function signed(value: number, decimals: number): string {
  return (value >= 0 ? "+" : "") + value.toFixed(decimals);
}

function printRows(rows: readonly FundingEntry[]): void {
  process.stdout.write(
    `${"exchange".padEnd(12)}  ${"base".padEnd(10)}  ${"rate".padStart(11)}  ` +
      `${"next_ms".padStart(13)}  ${"intv".padStart(4)}\n`,
  );
  for (const row of rows) {
    process.stdout.write(
      `${row.exchange.padEnd(12)}  ${row.base.padEnd(10)}  ` +
        `${signed(row.fundingRate, 6).padStart(11)}  ` +
        `${String(row.nextFundingTimeMs).padStart(13)}  ` +
        `${String(row.fundingIntervalHours).padStart(3)}h\n`,
    );
  }
}

/** Returns a promise that resolves after `ms` or when the abort signal fires. */
function sleep(ms: number, signal: AbortSignal): Promise<void> {
  return new Promise((resolve) => {
    if (signal.aborted) return resolve();
    let timer: NodeJS.Timeout | undefined;
    const onAbort = (): void => {
      if (timer !== undefined) clearTimeout(timer);
      resolve();
    };
    timer = setTimeout(() => {
      signal.removeEventListener("abort", onAbort);
      resolve();
    }, ms);
    signal.addEventListener("abort", onAbort, { once: true });
  });
}

async function main(): Promise<number> {
  const ac = new AbortController();
  process.on("SIGINT", () => ac.abort());

  const client = new FundingClient();
  const bootstrap = await client.getSnapshot();
  let cursor = bootstrap.version;
  process.stdout.write(`bootstrap: version=${cursor}  count=${bootstrap.count}\n`);

  for (let i = 1; i <= MAX_ITERATIONS; i++) {
    await sleep(SLEEP_MS, ac.signal);
    if (ac.signal.aborted) return 0;

    const delta = await client.getDelta(cursor);
    if (delta.count === 0) {
      process.stdout.write(`tick ${i}: no change (version=${delta.version})\n`);
    } else {
      process.stdout.write(`tick ${i}: ${delta.count} changes, version=${delta.version}\n`);
      printRows(delta.data);
    }
    cursor = delta.version;
  }
  return 0;
}

cliEntry(main);
