/**
 * 06_funding_alert: print ALERT lines for rows above |threshold| and
 * (optionally) fan them out to a webhook.
 */

import { FundingClient } from "../client.js";
import { ClientLogicError } from "../errors.js";
import { cliEntry } from "../runner.js";
import type { FundingEntry } from "../types.js";

async function postWebhook(url: string, row: FundingEntry): Promise<void> {
  const body = JSON.stringify({
    exchange: row.exchange,
    base: row.base,
    funding_rate: row.fundingRate,
    next_funding_time_ms: row.nextFundingTimeMs,
  });
  const res = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body,
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
}

async function main(): Promise<number> {
  const rawThreshold = process.env.KAIRO_THRESHOLD ?? "0.001";
  const threshold = Number(rawThreshold);
  if (!Number.isFinite(threshold)) {
    throw new ClientLogicError(`KAIRO_THRESHOLD is not a valid decimal: ${rawThreshold}`);
  }
  const webhookUrl = process.env.KAIRO_WEBHOOK_URL;

  const client = new FundingClient();
  const snap = await client.getSnapshot();

  let matched = 0;
  for (const row of snap.data) {
    if (Math.abs(row.fundingRate) < threshold) continue;
    matched++;
    process.stdout.write(
      `ALERT  ${row.exchange}  ${row.base}  rate=${row.fundingRate}  next=${row.nextFundingTimeMs}\n`,
    );
    if (webhookUrl) {
      try {
        await postWebhook(webhookUrl, row);
      } catch (err) {
        const msg = err instanceof Error ? err.message : String(err);
        process.stderr.write(`webhook failed for ${row.base}: ${msg}\n`);
      }
    }
  }
  process.stdout.write(`matched ${matched}/${snap.count} rows above threshold ${threshold}\n`);
  return 0;
}

cliEntry(main);
