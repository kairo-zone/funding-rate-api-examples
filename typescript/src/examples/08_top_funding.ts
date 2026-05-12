/**
 * 08_top_funding: TOP 10 POSITIVE and BOTTOM 10 NEGATIVE funding rows by
 * funding rate, with annualized percentages.
 */

import { FundingClient } from "../client.js";
import { cliEntry } from "../runner.js";
import type { FundingEntry } from "../types.js";

function annualizedPct(row: FundingEntry): number {
  return row.fundingRate * (24 / row.fundingIntervalHours) * 365 * 100;
}

function formatRow(row: FundingEntry): string {
  return `${row.exchange}  ${row.base}  rate=${row.fundingRate}  ann=${annualizedPct(row).toFixed(4)}%`;
}

async function main(): Promise<void> {
  const client = new FundingClient();
  const snap = await client.getSnapshot();

  const positives = snap.data
    .filter((r) => r.fundingRate > 0)
    .sort(
      (a, b) =>
        b.fundingRate - a.fundingRate ||
        a.exchange.localeCompare(b.exchange) ||
        a.base.localeCompare(b.base),
    )
    .slice(0, 10);

  const negatives = snap.data
    .filter((r) => r.fundingRate < 0)
    .sort(
      (a, b) =>
        a.fundingRate - b.fundingRate ||
        a.exchange.localeCompare(b.exchange) ||
        a.base.localeCompare(b.base),
    )
    .slice(0, 10);

  process.stdout.write("TOP 10 POSITIVE\n");
  for (const row of positives) process.stdout.write(`${formatRow(row)}\n`);
  process.stdout.write("\nBOTTOM 10 NEGATIVE\n");
  for (const row of negatives) process.stdout.write(`${formatRow(row)}\n`);
}

cliEntry(main);
