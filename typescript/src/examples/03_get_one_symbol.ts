/**
 * 03_get_one_symbol: filter by both `exchange` and `base` to fetch a single
 * funding row. Exits 4 when no matching row is found.
 */

import { FundingClient } from "../client.js";
import { cliEntry } from "../runner.js";

async function main(): Promise<number> {
  const exchange = process.env.KAIRO_EXCHANGE ?? "bybit";
  const base = process.env.KAIRO_BASE ?? "BTC";
  const client = new FundingClient();
  const snap = await client.getSnapshot({ exchange, base });

  if (snap.count === 0 || snap.data.length === 0) {
    process.stdout.write(`no row for ${exchange}/${base}\n`);
    return 4;
  }
  const row = snap.data[0]!;
  process.stdout.write(
    `${row.exchange}  ${row.base}  rate=${row.fundingRate}  ` +
      `next=${row.nextFundingTimeMs}  interval=${row.fundingIntervalHours}h  ` +
      `event=${row.eventTimeMs}\n`,
  );
  return 0;
}

cliEntry(main);
