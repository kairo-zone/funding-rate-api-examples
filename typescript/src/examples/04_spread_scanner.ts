/**
 * 04_spread_scanner: cross-exchange spread for a single base asset, sorted
 * by funding rate ascending, plus a summary line.
 */

import { FundingClient } from "../client.js";
import { cliEntry } from "../runner.js";
import type { FundingEntry } from "../types.js";

function annualizedPct(row: FundingEntry): number {
  return row.fundingRate * (24 / row.fundingIntervalHours) * 365 * 100;
}

function signed(value: number, decimals: number): string {
  return (value >= 0 ? "+" : "") + value.toFixed(decimals);
}

async function main(): Promise<number> {
  const base = process.env.KAIRO_BASE ?? "BTC";
  const client = new FundingClient();
  const snap = await client.getSnapshot({ base });

  if (snap.count === 0 || snap.data.length === 0) {
    process.stdout.write(`no rows for base=${base}\n`);
    return 4;
  }

  const sorted = [...snap.data].sort(
    (a, b) => a.fundingRate - b.fundingRate || a.exchange.localeCompare(b.exchange),
  );
  process.stdout.write(
    `${"exchange".padEnd(12)}  ${"rate".padStart(11)}  ` +
      `${"ann%".padStart(9)}  ${"intv".padStart(4)}\n`,
  );
  for (const row of sorted) {
    const ann = annualizedPct(row);
    process.stdout.write(
      `${row.exchange.padEnd(12)}  ` +
        `${signed(row.fundingRate, 6).padStart(11)}  ` +
        `${signed(ann, 4).padStart(8)}%  ` +
        `${String(row.fundingIntervalHours).padStart(3)}h\n`,
    );
  }

  const min = sorted[0]!;
  const max = sorted[sorted.length - 1]!;
  const spread = max.fundingRate - min.fundingRate;
  process.stdout.write("\n");
  process.stdout.write(
    `spread = ${signed(spread, 6)}  ` +
      `(max ${max.exchange} @ ${signed(max.fundingRate, 6)}, ` +
      `min ${min.exchange} @ ${signed(min.fundingRate, 6)})\n`,
  );
  return 0;
}

cliEntry(main);
