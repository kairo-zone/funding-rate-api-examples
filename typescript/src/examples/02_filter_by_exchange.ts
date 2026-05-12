/**
 * 02_filter_by_exchange: demonstrates the `exchange` query filter and
 * prints the first 10 rows sorted by base.
 */

import { FundingClient } from "../client.js";
import { cliEntry } from "../runner.js";

async function main(): Promise<void> {
  const exchange = process.env.KAIRO_EXCHANGE ?? "bybit";
  const client = new FundingClient();
  const snap = await client.getSnapshot({ exchange });

  process.stdout.write(`exchange=${exchange}  rows=${snap.count}\n`);

  const sorted = [...snap.data].sort((a, b) => a.base.localeCompare(b.base));
  for (const row of sorted.slice(0, 10)) {
    process.stdout.write(
      `${row.base}  rate=${row.fundingRate}  interval=${row.fundingIntervalHours}h\n`,
    );
  }
}

cliEntry(main);
