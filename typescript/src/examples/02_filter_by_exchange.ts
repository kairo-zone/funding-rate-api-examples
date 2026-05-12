/**
 * 02_filter_by_exchange: demonstrates the `exchange` query filter and
 * prints the first 10 rows sorted by base in a column-aligned table.
 */

import { FundingClient } from "../client.js";
import { cliEntry } from "../runner.js";

function signed(value: number, decimals: number): string {
  return (value >= 0 ? "+" : "") + value.toFixed(decimals);
}

async function main(): Promise<void> {
  const exchange = process.env.KAIRO_EXCHANGE ?? "bybit";
  const client = new FundingClient();
  const snap = await client.getSnapshot({ exchange });

  process.stdout.write(`exchange=${exchange}  rows=${snap.count}\n`);
  process.stdout.write("\n");
  process.stdout.write(
    `${"base".padEnd(16)}  ${"rate".padStart(11)}  ${"intv".padStart(4)}\n`,
  );

  const sorted = [...snap.data].sort((a, b) => a.base.localeCompare(b.base));
  for (const row of sorted.slice(0, 10)) {
    process.stdout.write(
      `${row.base.padEnd(16)}  ` +
        `${signed(row.fundingRate, 6).padStart(11)}  ` +
        `${String(row.fundingIntervalHours).padStart(3)}h\n`,
    );
  }
}

cliEntry(main);
