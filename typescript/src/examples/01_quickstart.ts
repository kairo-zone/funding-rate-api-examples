/**
 * 01_quickstart: smallest possible client - one GET, print the version line
 * and the first five funding rows in a column-aligned table.
 */

import { FundingClient } from "../client.js";
import { cliEntry } from "../runner.js";

function signed(value: number, decimals: number): string {
  return (value >= 0 ? "+" : "") + value.toFixed(decimals);
}

async function main(): Promise<void> {
  const client = new FundingClient();
  const snap = await client.getSnapshot();

  process.stdout.write(`version=${snap.version}  count=${snap.count}\n`);
  process.stdout.write("\n");
  process.stdout.write(
    `${"exchange".padEnd(12)}  ${"base".padEnd(10)}  ${"rate".padStart(11)}  ` +
      `${"next_ms".padStart(13)}  ${"intv".padStart(4)}\n`,
  );
  for (const row of snap.data.slice(0, 5)) {
    process.stdout.write(
      `${row.exchange.padEnd(12)}  ${row.base.padEnd(10)}  ` +
        `${signed(row.fundingRate, 6).padStart(11)}  ` +
        `${String(row.nextFundingTimeMs).padStart(13)}  ` +
        `${String(row.fundingIntervalHours).padStart(3)}h\n`,
    );
  }
}

cliEntry(main);
