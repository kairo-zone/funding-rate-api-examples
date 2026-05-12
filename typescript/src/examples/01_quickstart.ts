/**
 * 01_quickstart: smallest possible client — one GET, print the version line
 * and the first five funding rows.
 */

import { FundingClient } from "../client.js";
import { cliEntry } from "../runner.js";

async function main(): Promise<void> {
  const client = new FundingClient();
  const snap = await client.getSnapshot();

  process.stdout.write(`version=${snap.version}  count=${snap.count}\n`);
  for (const row of snap.data.slice(0, 5)) {
    process.stdout.write(
      `${row.exchange}  ${row.base}  rate=${row.fundingRate}  ` +
        `next=${row.nextFundingTimeMs}  interval=${row.fundingIntervalHours}h\n`,
    );
  }
}

cliEntry(main);
