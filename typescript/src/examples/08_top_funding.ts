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

function signed(value: number, decimals: number): string {
  return (value >= 0 ? "+" : "") + value.toFixed(decimals);
}

function formatRow(row: FundingEntry): string {
  const ann = annualizedPct(row);
  return (
    `${row.exchange.padEnd(12)}  ${row.base.padEnd(10)}  ` +
    `${signed(row.fundingRate, 6).padStart(11)}  ${signed(ann, 4).padStart(8)}%`
  );
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

  const header =
    `${"exchange".padEnd(12)}  ${"base".padEnd(10)}  ` +
    `${"rate".padStart(11)}  ${"ann%".padStart(9)}`;

  process.stdout.write("TOP 10 POSITIVE\n");
  process.stdout.write(`${header}\n`);
  for (const row of positives) process.stdout.write(`${formatRow(row)}\n`);
  process.stdout.write("\n");
  process.stdout.write("BOTTOM 10 NEGATIVE\n");
  process.stdout.write(`${header}\n`);
  for (const row of negatives) process.stdout.write(`${formatRow(row)}\n`);
}

cliEntry(main);
