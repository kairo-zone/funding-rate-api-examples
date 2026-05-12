/**
 * 09_export_csv: persist the current funding snapshot to
 * `funding_<version>.csv` in the current working directory.
 */

import { createWriteStream } from "node:fs";
import { resolve } from "node:path";
import { FundingClient } from "../client.js";
import { cliEntry } from "../runner.js";

const HEADER =
  "exchange,base,funding_rate,next_funding_time_ms,funding_interval_hours,event_time_ms";

async function main(): Promise<void> {
  const client = new FundingClient();
  const snap = await client.getSnapshot();

  const path = resolve(process.cwd(), `funding_${snap.version}.csv`);
  const stream = createWriteStream(path, { encoding: "utf8" });

  await new Promise<void>((res, rej) => {
    stream.on("error", rej);
    stream.write(HEADER + "\n");
    for (const row of snap.data) {
      stream.write(
        [
          row.exchange,
          row.base,
          row.fundingRate,
          row.nextFundingTimeMs,
          row.fundingIntervalHours,
          row.eventTimeMs,
        ].join(",") + "\n",
      );
    }
    stream.end(() => res());
  });

  process.stdout.write(`wrote ${snap.count} rows to ${path}\n`);
}

cliEntry(main);
