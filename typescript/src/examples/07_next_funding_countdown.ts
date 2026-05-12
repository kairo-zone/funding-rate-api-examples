/**
 * 07_next_funding_countdown: per-base countdown to the next funding event,
 * sorted by time remaining ascending.
 */

import { FundingClient } from "../client.js";
import { cliEntry } from "../runner.js";
import type { FundingEntry } from "../types.js";

interface Soonest {
  base: string;
  row: FundingEntry;
  remainingSec: number;
}

async function main(): Promise<number> {
  const watchlist = (process.env.KAIRO_WATCHLIST ?? "BTC,ETH,SOL")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);
  const alertMinutes = Number(process.env.KAIRO_ALERT_MINUTES ?? "10");
  const alertThresholdSec = (Number.isFinite(alertMinutes) ? alertMinutes : 10) * 60;

  const client = new FundingClient();
  const snap = await client.getSnapshot({ base: watchlist });
  const now = Date.now();

  const soonest: Soonest[] = [];
  const skipped: string[] = [];
  for (const base of watchlist) {
    const rows = snap.data.filter((r) => r.base === base);
    if (rows.length === 0) {
      skipped.push(base);
      continue;
    }
    const pick = rows.reduce((a, b) => (a.nextFundingTimeMs <= b.nextFundingTimeMs ? a : b));
    const remainingSec = (pick.nextFundingTimeMs - now) / 1000;
    if (remainingSec < 0) {
      process.stdout.write(`${base}: no upcoming funding\n`);
      continue;
    }
    soonest.push({ base, row: pick, remainingSec });
  }

  soonest.sort((a, b) => a.remainingSec - b.remainingSec);
  for (const s of soonest) {
    const m = Math.floor(s.remainingSec / 60);
    const sec = Math.floor(s.remainingSec % 60);
    const prefix = s.remainingSec <= alertThresholdSec ? "[ALERT] " : "";
    process.stdout.write(
      `${prefix}${s.base} on ${s.row.exchange}: in ${m}m ${sec}s, rate=${s.row.fundingRate}\n`,
    );
  }
  for (const b of skipped) process.stdout.write(`${b}: no upcoming funding\n`);
  return 0;
}

cliEntry(main);
