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
  for (const row of sorted) {
    process.stdout.write(
      `${row.exchange}  rate=${row.fundingRate}  ann=${annualizedPct(row).toFixed(4)}%  ` +
        `interval=${row.fundingIntervalHours}h\n`,
    );
  }

  const min = sorted[0]!;
  const max = sorted[sorted.length - 1]!;
  const spread = max.fundingRate - min.fundingRate;
  process.stdout.write(
    `spread = ${spread} (max ${max.exchange} @ ${max.fundingRate}, ` +
      `min ${min.exchange} @ ${min.fundingRate})\n`,
  );
  return 0;
}

cliEntry(main);
